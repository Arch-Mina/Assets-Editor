// OTML library
// for reading and writing .otfi

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets_Editor;

public static class OtmlUtil {
    public class BadCast : Exception {
        public BadCast() : base("failed to cast value") { }
    }

    public static bool Cast<TInput, TOutput>(TInput input, out TOutput output) {
        try {
            var str = input?.ToString() ?? "";
            if (typeof(TOutput) == typeof(string)) {
                output = (TOutput)(object)str;
                return true;
            }
            if (typeof(TOutput) == typeof(bool)) {
                if (str == "true") { output = (TOutput)(object)true; return true; }
                if (str == "false") { output = (TOutput)(object)false; return true; }
                output = default!;
                return false;
            }
            if (typeof(TOutput) == typeof(char)) {
                if (str.Length == 1) {
                    output = (TOutput)(object)str[0];
                    return true;
                }
                output = default!;
                return false;
            }
            if (typeof(TOutput) == typeof(int)) {
                if (int.TryParse(str, out var v)) { output = (TOutput)(object)v; return true; }
                output = default!;
                return false;
            }
            if (typeof(TOutput) == typeof(long)) {
                if (long.TryParse(str, out var v)) { output = (TOutput)(object)v; return true; }
                output = default!;
                return false;
            }
            if (typeof(TOutput) == typeof(double)) {
                if (double.TryParse(str, out var v)) { output = (TOutput)(object)v; return true; }
                output = default!;
                return false;
            }
            // fallback for other types
            output = (TOutput)Convert.ChangeType(str, typeof(TOutput));
            return true;
        } catch {
            output = default!;
            return false;
        }
    }

    public static T SafeCast<T>(object input) {
        if (Cast(input, out T result)) return result;
        throw new BadCast();
    }
}

public class OTMLException : Exception {
    public OTMLException(string msg) : base(msg) { }
    public OTMLException(OTMLNode node, string msg) : base(FormatNode(node, msg)) { }
    public OTMLException(OTMLDocument doc, string msg, int line = -1) : base(FormatDoc(doc, msg, line)) { }

    private static string FormatNode(OTMLNode node, string msg) {
        var src = node.Source;
        if (!string.IsNullOrEmpty(src)) return $"OTML error in '{src}': {msg}";
        return $"OTML error: {msg}";
    }
    private static string FormatDoc(OTMLDocument doc, string msg, int line) {
        var src = doc.Source;
        if (!string.IsNullOrEmpty(src)) {
            if (line >= 0) return $"OTML error in '{src}' at line {line}: {msg}";
            return $"OTML error in '{src}': {msg}";
        }
        return $"OTML error: {msg}";
    }
}

public class OTMLNode {
    public string Tag { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Unique { get; set; }
    public bool IsNull { get; set; }
    public string Source { get; set; } = "";

    public OTMLNode? Parent { get; private set; }
    private readonly List<OTMLNode> _children = [];

    public int Size => _children.Count;
    public IReadOnlyList<OTMLNode> Children => [.. _children.Where(c => !c.IsNull)];

    public static OTMLNode Create(string tag = "", bool unique = false) {
        return new OTMLNode { Tag = tag, Unique = unique };
    }

    public static OTMLNode Create(string tag, string value) {
        return new OTMLNode { Tag = tag, Value = value, Unique = true };
    }

    public void AddChild(OTMLNode child) {
        if (!string.IsNullOrEmpty(child.Tag)) {
            var same = _children.Where(c => c.Tag == child.Tag && (c.Unique || child.Unique)).ToList();
            if (same.Count != 0) {
                child.Unique = true;
                var existing = same.First();

                if (existing.Children.Any() && child.Children.Any()) {
                    var tmp = existing.Clone();
                    tmp.Merge(child);
                    child.Copy(tmp);
                }

                ReplaceChild(existing, child);
                _children.RemoveAll(c => c != child && c.Tag == child.Tag);
                child.Parent = this;
                return;
            }
        }
        _children.Add(child);
        child.Parent = this;
    }

    public bool RemoveChild(OTMLNode child) {
        if (_children.Remove(child)) { child.Parent = null; return true; }
        return false;
    }

    public bool ReplaceChild(OTMLNode oldChild, OTMLNode newChild) {
        var i = _children.IndexOf(oldChild);
        if (i >= 0) {
            oldChild.Parent = null;
            newChild.Parent = this;
            _children[i] = newChild;
            return true;
        }
        return false;
    }

    public void Merge(OTMLNode node) {
        foreach (var c in node._children)
            AddChild(c.Clone());
        Tag = node.Tag;
        Source = node.Source;
    }

    public void Copy(OTMLNode node) {
        Tag = node.Tag;
        Value = node.Value;
        Unique = node.Unique;
        IsNull = node.IsNull;
        Source = node.Source;
        Clear();
        foreach (var c in node._children)
            AddChild(c.Clone());
    }

    public void Clear() {
        foreach (var c in _children) c.Parent = null;
        _children.Clear();
    }

    public OTMLNode Clone() {
        var n = new OTMLNode {
            Tag = Tag,
            Value = Value,
            Unique = Unique,
            IsNull = IsNull,
            Source = Source
        };
        foreach (var c in _children) n.AddChild(c.Clone());
        return n;
    }

    public OTMLNode? Get(string tag) {
        return _children.FirstOrDefault(c => c.Tag == tag && !c.IsNull);
    }

    public OTMLNode At(string tag) {
        var n = Get(tag);
        return n ?? throw new OTMLException(this, $"child node with tag '{tag}' not found");
    }

    public OTMLNode? GetIndex(int index) {
        if (index >= 0 && index < _children.Count) return _children[index];
        return null;
    }

    public OTMLNode AtIndex(int index) {
        var n = GetIndex(index);
        return n ?? throw new OTMLException(this, $"child node with index '{index}' not found");
    }

    public T ValueAs<T>() {
        if (typeof(T) == typeof(string)) {
            var v = Value;
            if (v.StartsWith('"') && v.EndsWith('"')) {
                v = v[1..^1];
                v = v.Replace("\\", "\\").Replace("\"", "\"")
                        .Replace("\\t", "\t").Replace("\\n", "\n")
                        .Replace("\\'", "'");
            }
            return (T)(object)v;
        }
        return OtmlUtil.SafeCast<T>(Value);
    }

    public string Emit() => OTMLEmitter.EmitNode(this, 0);
}

public class OTMLDocument : OTMLNode {
    public static OTMLDocument Create() {
        return new OTMLDocument { Tag = "doc" };
    }

    public static OTMLDocument Parse(string filename) {
        if (!File.Exists(filename)) throw new OTMLException($"failed to open file {filename}");
        using var sr = new StreamReader(filename);
        return Parse(sr, filename);
    }

    public static OTMLDocument Parse(TextReader reader, string source) {
        var doc = new OTMLDocument { Source = source };
        var parser = new OTMLParser(doc, reader);
        parser.Parse();
        return doc;
    }

    public bool Save(string filename) {
        Source = filename;
        try {
            File.WriteAllText(filename, Emit() + "\n");
            return true;
        } catch { return false; }
    }
}

public class OTMLParser(OTMLDocument doc, TextReader reader) {
    private readonly OTMLDocument _doc = doc;
    private readonly TextReader _reader = reader;
    private OTMLNode? _currentParent = doc;
    private OTMLNode? _previousNode;
    private int _currentDepth = 0;
    private int _currentLine = 0;

    public void Parse() {
        string? line;
        while ((line = NextLine()) != null)
            ParseLine(line);
    }

    private string? NextLine() {
        _currentLine++;
        return _reader.ReadLine();
    }

    private int GetDepth(string line) {
        int spaces = 0;
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) {
            spaces += (line[i] == ' ') ? 1 : 2;
            i++;
        }
        if (spaces % 2 != 0)
            throw new OTMLException(_doc, "must indent every 2 spaces", _currentLine);
        return spaces / 2;
    }

    private void ParseLine(string line) {
        if (line == null) return;
        var depth = GetDepth(line);
        line = line.Trim();
        if (string.IsNullOrEmpty(line)) return;
        if (line.StartsWith("//")) return;

        if (depth == _currentDepth + 1)
            _currentParent = _previousNode;
        else if (depth < _currentDepth) {
            for (int i = 0; i < _currentDepth - depth; i++)
                _currentParent = _currentParent?.Parent;
        } else if (depth != _currentDepth) {
            throw new OTMLException(_doc, "invalid indentation depth", _currentLine);
        }
        _currentDepth = depth;
        ParseNode(line);
    }

    private void ParseNode(string data) {
        string tag = "";
        string value = "";
        int lineNum = _currentLine;

        if (data.StartsWith('-')) {
            value = data[1..].Trim();
        } else {
            int pos = data.IndexOf(':');
            if (pos >= 0) {
                tag = data[..pos].Trim();
                if (pos + 1 < data.Length) value = data[(pos + 1)..].Trim();
            } else
                tag = data.Trim();
        }

        var node = OTMLNode.Create(tag);
        node.Unique = data.Contains(':');
        node.Source = _doc.Source + ":" + lineNum;

        if (value == "~") node.IsNull = true;
        else node.Value = value;

        _currentParent?.AddChild(node);
        _previousNode = node;
    }
}

public static class OTMLEmitter {
    public static string EmitNode(OTMLNode node, int depth) {
        var sb = new StringBuilder();
        if (depth >= 0) {
            sb.Append(new string('\t', depth));
            if (!string.IsNullOrEmpty(node.Tag)) {
                sb.Append(node.Tag);
                if (!string.IsNullOrEmpty(node.Value) || node.Unique || node.IsNull)
                    sb.Append(':');
            } else sb.Append('-');

            if (node.IsNull) sb.Append(" ~");
            else if (!string.IsNullOrEmpty(node.Value)) sb.Append(' ').Append(node.Value);
        }

        var children = node.Children.ToList();
        for (int i = 0; i < children.Count; i++) {
            sb.Append('\n');
            sb.Append(EmitNode(children[i], depth + 1));
        }

        return sb.ToString();
    }
}
