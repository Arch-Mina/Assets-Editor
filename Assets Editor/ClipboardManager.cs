using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;


namespace Assets_Editor;

// System.Windows.Clipboard is known to have issues with accessing the clipboard
// This class bypasses the library and makes direct calls to Windows API instead

public static class ClipboardManager {
    private const uint CF_WAVE = 0x0003;
    private const uint CF_BITMAP = 2;
    private const uint CF_HDROP = 15;

    #region Win32 Imports
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);
    #endregion

    #region Text Methods

    public static void SetText(string text) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        SetTextInternal(text);
    }

    private static void SetTextInternal(string text) {
        IntPtr hGlobal = IntPtr.Zero;
        IntPtr lpGlobal = IntPtr.Zero;

        try {
            // Allocate global memory
            hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)((text.Length + 1) * 2));
            if (hGlobal == IntPtr.Zero) throw new ExternalException("GlobalAlloc failed");

            lpGlobal = GlobalLock(hGlobal);
            if (lpGlobal == IntPtr.Zero) throw new ExternalException("GlobalLock failed");

            // Copy string to unmanaged memory
            Marshal.Copy(text.ToCharArray(), 0, lpGlobal, text.Length);
            Marshal.WriteInt16(lpGlobal, text.Length * 2, 0); // Null terminator
            GlobalUnlock(hGlobal);
            lpGlobal = IntPtr.Zero;

            // Open clipboard
            if (!OpenClipboard(IntPtr.Zero)) throw new ExternalException("Cannot open clipboard");

            EmptyClipboard();
            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                throw new ExternalException("SetClipboardData failed");

            hGlobal = IntPtr.Zero; // system owns the memory now
        } finally {
            if (lpGlobal != IntPtr.Zero) GlobalUnlock(lpGlobal);
            if (hGlobal != IntPtr.Zero) Marshal.FreeHGlobal(hGlobal);
            CloseClipboard();
        }
    }

    public static string GetText() {
        if (!OpenClipboard(IntPtr.Zero)) return string.Empty;
        try {
            IntPtr handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero) return string.Empty;

            IntPtr ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero) return string.Empty;

            try {
                return Marshal.PtrToStringUni(ptr) ?? string.Empty;
            } finally {
                GlobalUnlock(handle);
            }
        } finally {
            CloseClipboard();
        }
    }

    #endregion

    #region Clear/Contains

    public static void Clear() {
        if (!OpenClipboard(IntPtr.Zero)) return;
        try {
            EmptyClipboard();
        } finally {
            CloseClipboard();
        }
    }

    public static bool ContainsText() {
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try {
            return GetClipboardData(CF_UNICODETEXT) != IntPtr.Zero;
        } finally {
            CloseClipboard();
        }
    }

    #endregion

    #region Flush
    public static void Flush() { /* no-op, clipboard persistence handled by OS */ }
    #endregion

    #region FileDrop Methods
    public static void SetFileDropList(StringCollection files) {
        if (files == null) throw new ArgumentNullException(nameof(files));
        if (files.Count == 0) throw new ArgumentException("File list cannot be empty");

        // Allocate memory for DROPFILES struct + paths
        int size = Marshal.SizeOf(typeof(DROPFILES));
        int totalChars = 0;
        foreach (string f in files) totalChars += f.Length + 1; // null-terminated
        totalChars++; // final null

        IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)(size + totalChars * 2));
        IntPtr ptr = GlobalLock(hGlobal);

        try {
            DROPFILES drop = new DROPFILES { pFiles = size, fWide = true };
            Marshal.StructureToPtr(drop, ptr, false);

            IntPtr filePtr = IntPtr.Add(ptr, size);
            foreach (string file in files) {
                char[] chars = (file + "\0").ToCharArray();
                Marshal.Copy(chars, 0, filePtr, chars.Length);
                filePtr = IntPtr.Add(filePtr, chars.Length * 2);
            }

            Marshal.WriteInt16(filePtr, 0); // double null
            GlobalUnlock(hGlobal);

            if (!OpenClipboard(IntPtr.Zero)) throw new ExternalException("Cannot open clipboard");
            EmptyClipboard();
            SetClipboardData(CF_HDROP, hGlobal);
            hGlobal = IntPtr.Zero;
        } finally {
            if (hGlobal != IntPtr.Zero) Marshal.FreeHGlobal(hGlobal);
            CloseClipboard();
        }
    }

    public static StringCollection GetFileDropList() {
        if (!OpenClipboard(IntPtr.Zero)) return null;
        try {
            IntPtr handle = GetClipboardData(CF_HDROP);
            if (handle == IntPtr.Zero) return null;

            // Use shell API to get filenames
            int count = (int)DragQueryFile(handle, 0xFFFFFFFF, null, 0);
            StringCollection result = new();
            for (uint i = 0; i < count; i++) {
                int len = (int)DragQueryFile(handle, i, null, 0);
                StringBuilder sb = new(len + 1);
                DragQueryFile(handle, i, sb, sb.Capacity);
                result.Add(sb.ToString());
            }
            return result;
        } finally { CloseClipboard(); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DROPFILES {
        public int pFiles;
        public int pt_x;
        public int pt_y;
        public bool fNC;
        public bool fWide;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, StringBuilder lpszFile, int cch);
    #endregion

    #region Bitmap Methods
    public static void SetImage(BitmapSource image) {
        if (image == null) throw new ArgumentNullException(nameof(image));

        // Convert BitmapSource to System.Drawing.Bitmap
        Bitmap bmp;
        using (MemoryStream ms = new MemoryStream()) {
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(ms);
            bmp = new Bitmap(ms);
        }

        IntPtr hBitmap = bmp.GetHbitmap();
        try {
            if (!OpenClipboard(IntPtr.Zero)) throw new ExternalException("Cannot open clipboard");
            EmptyClipboard();
            SetClipboardData(CF_BITMAP, hBitmap);
        } finally {
            CloseClipboard();
        }
    }

    public static BitmapSource GetImage() {
        if (!OpenClipboard(IntPtr.Zero)) return null;
        try {
            IntPtr handle = GetClipboardData(CF_BITMAP);
            if (handle == IntPtr.Zero) return null;
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        } finally { CloseClipboard(); }
    }

    public static bool ContainsImage() {
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try { return GetClipboardData(CF_BITMAP) != IntPtr.Zero; } finally { CloseClipboard(); }
    }
    #endregion

    #region Audio Methods (CF_WAVE)
    public static void SetAudio(byte[] audioBytes) {
        if (audioBytes == null) throw new ArgumentNullException(nameof(audioBytes));
        using MemoryStream ms = new MemoryStream(audioBytes);
        SetAudio(ms);
    }

    public static void SetAudio(Stream audioStream) {
        if (audioStream == null) throw new ArgumentNullException(nameof(audioStream));
        byte[] buffer = new byte[audioStream.Length];
        audioStream.Read(buffer, 0, buffer.Length);

        IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)buffer.Length);
        IntPtr ptr = GlobalLock(hGlobal);
        Marshal.Copy(buffer, 0, ptr, buffer.Length);
        GlobalUnlock(hGlobal);

        if (!OpenClipboard(IntPtr.Zero)) throw new ExternalException("Cannot open clipboard");
        try {
            EmptyClipboard();
            SetClipboardData(CF_WAVE, hGlobal);
            hGlobal = IntPtr.Zero;
        } finally {
            CloseClipboard();
            if (hGlobal != IntPtr.Zero) Marshal.FreeHGlobal(hGlobal);
        }
    }

    public static bool ContainsAudio() {
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try { return GetClipboardData(CF_WAVE) != IntPtr.Zero; } finally { CloseClipboard(); }
    }
    #endregion

    #region helpers
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint RegisterClipboardFormat(string lpszFormat);

    public static bool ContainsData(string format) {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        uint formatId = RegisterClipboardFormat(format);
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try {
            return GetClipboardData(formatId) != IntPtr.Zero;
        } finally {
            CloseClipboard();
        }
    }

    public static bool ContainsFileDropList() => GetFileDropList() != null;

    public static bool ContainsText(TextDataFormat format) {
        if (!OpenClipboard(IntPtr.Zero)) return false;
        try {
            switch (format) {
                case TextDataFormat.Text: return GetClipboardData(1) != IntPtr.Zero; // CF_TEXT
                case TextDataFormat.UnicodeText: return GetClipboardData(CF_UNICODETEXT) != IntPtr.Zero;
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
        } finally { CloseClipboard(); }
    }

    public static Stream GetAudioStream() {
        if (!OpenClipboard(IntPtr.Zero)) return null;
        try {
            IntPtr handle = GetClipboardData(CF_WAVE);
            if (handle == IntPtr.Zero) return null;

            IntPtr ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero) return null;

            try {
                int size = GlobalSize(handle);
                byte[] buffer = new byte[size];
                Marshal.Copy(ptr, buffer, 0, size);
                return new MemoryStream(buffer);
            } finally {
                GlobalUnlock(handle);
            }
        } finally { CloseClipboard(); }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int GlobalSize(IntPtr hMem);

    public static object GetData(string format) {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        uint formatId = RegisterClipboardFormat(format);
        if (!OpenClipboard(IntPtr.Zero)) return null;
        try {
            IntPtr handle = GetClipboardData(formatId);
            if (handle == IntPtr.Zero) return null;

            IntPtr ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero) return null;

            try {
                int size = GlobalSize(handle);
                byte[] buffer = new byte[size];
                Marshal.Copy(ptr, buffer, 0, size);
                return buffer;
            } finally { GlobalUnlock(handle); }
        } finally { CloseClipboard(); }
    }

    public static IDataObject GetDataObject() {
        return new DataObjectWrapper();
    }

    private class DataObjectWrapper : IDataObject {
        public object GetData(Type format) => ClipboardManager.GetData(format.FullName);
        public object GetData(string format) => ClipboardManager.GetData(format);
        public object GetData(string format, bool autoConvert) => GetData(format);
        public bool GetDataPresent(Type format) => GetData(format.FullName) != null;
        public bool GetDataPresent(string format) => GetData(format) != null;
        public bool GetDataPresent(string format, bool autoConvert) => GetDataPresent(format);
        public string[] GetFormats() => new string[0];
        public string[] GetFormats(bool autoConvert) => new string[0];
        public void SetData(object data) => ClipboardManager.SetDataObject(data);
        public void SetData(string format, object data) => ClipboardManager.SetData(format, data);
        public void SetData(string format, object data, bool autoConvert) {
            // 'autoConvert' is ignored in this implementation
            SetData(format, data);
        }

        public void SetData(Type format, object data) {
            if (format == null) throw new ArgumentNullException(nameof(format));
            SetData(format.FullName, data);
        }
    }

    public static bool IsCurrent(IDataObject data) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        return data == GetDataObject();
    }

    public static void SetData(string format, object data) {
        if (string.IsNullOrEmpty(format)) throw new ArgumentNullException(nameof(format));
        if (data == null) throw new ArgumentNullException(nameof(data));
        uint formatId = RegisterClipboardFormat(format);

        byte[] buffer = data as byte[];
        if (buffer == null) throw new ArgumentException("Only byte[] supported for SetData");

        IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)buffer.Length);
        IntPtr ptr = GlobalLock(hGlobal);
        Marshal.Copy(buffer, 0, ptr, buffer.Length);
        GlobalUnlock(hGlobal);

        if (!OpenClipboard(IntPtr.Zero)) throw new ExternalException("Cannot open clipboard");
        try {
            EmptyClipboard();
            SetClipboardData(formatId, hGlobal);
            hGlobal = IntPtr.Zero;
        } finally {
            CloseClipboard();
            if (hGlobal != IntPtr.Zero) Marshal.FreeHGlobal(hGlobal);
        }
    }

    public static void SetDataObject(object data) => SetDataObject(data, true);

    public static void SetDataObject(object data, bool copy) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data is IDataObject dobj) { /* could implement full copy logic */ } else SetData("CustomData", data); // fallback
    }
#endregion
}

public enum TextDataFormat { Text, UnicodeText }
