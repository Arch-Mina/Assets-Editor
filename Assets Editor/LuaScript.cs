using Assets_Editor;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tibia.Protobuf.Appearances;

namespace Assets_editor;

public class LuaThings : IEnumerable<KeyValuePair<string, object>> {
    // interface implementation - pairs loop, class indexing
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
        yield return new KeyValuePair<string, object>("outfits", GetOutfits());
        yield return new KeyValuePair<string, object>("objects", GetObjects());
        yield return new KeyValuePair<string, object>("effects", GetEffects());
        yield return new KeyValuePair<string, object>("missiles", GetMissiles());
    }
    // end interface implementation

    private static List<Appearance> GetOutfits() {
        return MainWindow.appearances?.Outfit?.ToList() ?? [];
    }

    private static List<Appearance> GetObjects() {
        return MainWindow.appearances?.Object?.ToList() ?? [];
    }

    private static List<Appearance> GetEffects() {
        return MainWindow.appearances?.Effect?.ToList() ?? [];
    }

    private static List<Appearance> GetMissiles() {
        return MainWindow.appearances?.Missile?.ToList() ?? [];
    }

    private static DynValue Lua_getDynCollection(ScriptExecutionContext context, string type) {
        List<Appearance>? results = type switch {
            "outfits" => GetOutfits(),
            "objects" => GetObjects(),
            "effects" => GetEffects(),
            "missiles" => GetMissiles(),
            _ => [],
        };

        // Create a Lua table
        Table luaTable = new(context.GetScript());

        // Fill the table
        for (int i = 0; i < results.Count; i++) {
            luaTable[i + 1] = UserData.Create(results[i]); // Lua arrays are 1-based
        }

        return DynValue.NewTable(luaTable);
    }

    public static DynValue Lua_getOutfits(ScriptExecutionContext context, CallbackArguments _) {
        return Lua_getDynCollection(context, "outfits");
    }

    public static DynValue Lua_getObjects(ScriptExecutionContext context, CallbackArguments _) {
        return Lua_getDynCollection(context, "objects");
    }

    public static DynValue Lua_getEffects(ScriptExecutionContext context, CallbackArguments _) {
        return Lua_getDynCollection(context, "effects");
    }

    public static DynValue Lua_getMissiles(ScriptExecutionContext context, CallbackArguments _) {
        return Lua_getDynCollection(context, "missiles");
    }

    public static Appearance? FindById(string type, uint id) {
        if (string.IsNullOrEmpty(type)) return null;
        return type.ToLowerInvariant() switch {
            "outfit" => GetOutfits().FirstOrDefault(x => x.Id == id),
            "object" => GetObjects().FirstOrDefault(x => x.Id == id),
            "effect" => GetEffects().FirstOrDefault(x => x.Id == id),
            "missile" => GetMissiles().FirstOrDefault(x => x.Id == id),
            _ => null,
        };
    }
}

// 
class CancelDebugger : IDebugger {
    public CancellationToken CancellationToken { get; set; }

    public DebuggerCaps GetDebuggerCaps() => DebuggerCaps.CanDebugSourceCode;

    public void SetDebugService(DebugService debugService) { }
    public void SetSourceCode(SourceCode sourceCode) { }
    public void SetByteCode(string[] byteCode) { }
    public void SignalExecutionEnded() { }
    public List<DynamicExpression> GetWatchItems() => [];
    public void RefreshBreakpoints(IEnumerable<SourceRef> refs) { }
    public void Update(WatchType watchType, IEnumerable<WatchItem> items) { }

    public bool IsPauseRequested() => CancellationToken.IsCancellationRequested;

    public bool SignalRuntimeException(ScriptRuntimeException ex) => false;

    public DebuggerAction GetAction(int ip, SourceRef sourceref) {
        return new DebuggerAction {
            Action = DebuggerAction.ActionType.Run // just continue running
        };
    }
}
