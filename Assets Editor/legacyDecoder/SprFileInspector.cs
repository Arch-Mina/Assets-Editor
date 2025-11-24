using System;
using System.IO;

namespace Assets_Editor;

public struct SprInfo {
    public string Signature;     // SPR signature as "0x...."
    public bool IsExtended;       // True if SPR uses 4-byte offsets
    public bool HasTransparency;   // True if SPR uses chunk-based transparency
    public int SpriteCount;        // Total number of sprites
}

public static class SprFileInspector {

}
