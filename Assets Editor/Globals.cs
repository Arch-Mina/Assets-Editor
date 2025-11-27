using System;

namespace Assets_Editor; 

public class Globals {
    // directory pickers
    // to generate more use tools -> create GUID in visual studio
    public static readonly Guid GUID_MainWindowAssetsPicker = new("43A0E8FA-B129-4DB4-AD2D-0C44C23CE222");

    // to do
    // public static readonly Guid GUID_MainWindowServerPicker = new("C01637F9-8C7B-4610-BBA2-530487BC57A2");

    public static readonly Guid GUID_DatEditor1 = new("820617D2-0ECA-4632-B62D-42F740BD731A"); // export as images path
    public static readonly Guid GUID_DatEditor2 = new("4FC8F7A5-4840-4840-A68B-26DFD955D224"); // export sprite as bitmap
    public static readonly Guid GUID_DatEditor3 = new("1A5860A3-5722-4FFC-B6F2-FCE4E9FE255F"); // import aec/obd
    public static readonly Guid GUID_DatEditor4 = new("5788DE66-9141-4995-9550-118FCFE88609"); // export as aec

    public static readonly Guid GUID_LegacyDatEditor1 = new("AB8D0A9E-F28F-4C63-8F76-EE41552BE4BB"); // export sprite as bitmap
    public static readonly Guid GUID_LegacyDatEditor2 = new("9BA45B21-456E-469C-9F15-1045455F348F"); // import sprite from bitmap
    public static readonly Guid GUID_LegacyDatEditor3 = new("268BE18A-5FCC-4D34-A8F0-B8643F9D3E77"); // replace sprite to selected bitmap

    public static readonly Guid GUID_ImportManager1 = new("72ED3998-51EC-4F28-8E39-846885C4FA7C"); // import from spr/dat
    public static readonly Guid GUID_ImportManager2 = new("0570A6C2-A004-4704-A627-E6F5A199494A"); // import from obd

    public static readonly Guid GUID_LuaWindowExportCSV = new("EABE038F-85C5-486E-8E76-AEAA0EB939F7"); // export lua output to CSV

    public static readonly Guid GUID_ObdDecoder_export = new("45F3020C-EF52-4CA0-8918-2E2C1CAAA96E"); // export to obd

    public static readonly Guid GUID_OtbEditor_load = new("C6314359-B3E6-43B0-B481-B60D4461BF9D"); // load otb
    public static readonly Guid GUID_OtbEditor_save = new("BFDCF4F6-ADE0-477D-AF45-472216ACBF47"); // save otb

    public static readonly Guid GUID_SprEditor_save = new("9AA10262-1929-47A2-85CE-A83183D8E620"); // save as bitmap
}
