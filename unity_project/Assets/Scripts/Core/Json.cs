using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IOSVN.TuTien.Core
{
    /// <summary>
    /// Small dependency-free JSON reader/writer for the dynamic game views (bag, crafting,
    /// sect, social...). Objects become Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;,
    /// numbers double, plus string/bool/null.
    /// </summary>
    public static class Json
    {
        public static object Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var reader = new Reader(text);
            var value = reader.ReadValue();
            return value;
        }

        public static string Serialize(object value)
        {
            var builder = new StringBuilder(256);
            Write(builder, value);
            return builder.ToString();
        }

        private static void Write(StringBuilder b, object value)
        {
            switch (value)
            {
                case null: b.Append("null"); return;
                case string s: WriteString(b, s); return;
                case bool flag: b.Append(flag ? "true" : "false"); return;
                case J node: Write(b, node.Raw); return;
                case IDictionary<string, object> map:
                {
                    b.Append('{');
                    var first = true;
                    foreach (var pair in map)
                    {
                        if (!first) b.Append(',');
                        first = false;
                        WriteString(b, pair.Key);
                        b.Append(':');
                        Write(b, pair.Value);
                    }
                    b.Append('}');
                    return;
                }
                case IEnumerable list:
                {
                    b.Append('[');
                    var first = true;
                    foreach (var item in list)
                    {
                        if (!first) b.Append(',');
                        first = false;
                        Write(b, item);
                    }
                    b.Append(']');
                    return;
                }
                case float f: b.Append(f.ToString("R", CultureInfo.InvariantCulture)); return;
                case double d: b.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case decimal m: b.Append(m.ToString(CultureInfo.InvariantCulture)); return;
                case IFormattable number when value is int || value is long || value is short || value is byte || value is uint || value is ulong:
                    b.Append(number.ToString(null, CultureInfo.InvariantCulture)); return;
                default: WriteString(b, value.ToString()); return;
            }
        }

        private static void WriteString(StringBuilder b, string s)
        {
            b.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (c < 0x20) b.Append("\\u").Append(((int)c).ToString("x4"));
                        else b.Append(c);
                        break;
                }
            }
            b.Append('"');
        }

        private sealed class Reader
        {
            private readonly string s;
            private int i;

            public Reader(string text) { s = text; }

            public object ReadValue()
            {
                SkipWhite();
                if (i >= s.Length) return null;
                var c = s[i];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': i += 4; return true;
                    case 'f': i += 5; return false;
                    case 'n': i += 4; return null;
                    default: return ReadNumber();
                }
            }

            private Dictionary<string, object> ReadObject()
            {
                var map = new Dictionary<string, object>();
                i++;
                while (true)
                {
                    SkipWhite();
                    if (i >= s.Length) return map;
                    if (s[i] == '}') { i++; return map; }
                    if (s[i] == ',') { i++; continue; }
                    var key = ReadString();
                    SkipWhite();
                    if (i < s.Length && s[i] == ':') i++;
                    map[key] = ReadValue();
                }
            }

            private List<object> ReadArray()
            {
                var list = new List<object>();
                i++;
                while (true)
                {
                    SkipWhite();
                    if (i >= s.Length) return list;
                    if (s[i] == ']') { i++; return list; }
                    if (s[i] == ',') { i++; continue; }
                    list.Add(ReadValue());
                }
            }

            private string ReadString()
            {
                var b = new StringBuilder();
                i++;
                while (i < s.Length)
                {
                    var c = s[i++];
                    if (c == '"') break;
                    if (c != '\\') { b.Append(c); continue; }
                    if (i >= s.Length) break;
                    var e = s[i++];
                    switch (e)
                    {
                        case 'n': b.Append('\n'); break;
                        case 'r': b.Append('\r'); break;
                        case 't': b.Append('\t'); break;
                        case 'b': b.Append('\b'); break;
                        case 'f': b.Append('\f'); break;
                        case 'u':
                            if (i + 4 <= s.Length && int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                                b.Append((char)code);
                            i += 4;
                            break;
                        default: b.Append(e); break;
                    }
                }
                return b.ToString();
            }

            private object ReadNumber()
            {
                var start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var number);
                if (i == start) i++; // skip an unexpected character instead of looping forever
                return number;
            }

            private void SkipWhite()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }
        }
    }

    /// <summary>Null-safe view over a parsed JSON value.</summary>
    public readonly struct J
    {
        public static readonly J Null = new J(null);
        public readonly object Raw;

        public J(object raw) { Raw = raw; }

        public static J Parse(string text) => new J(Json.Parse(text));

        public bool IsNull => Raw == null;
        public bool IsObject => Raw is Dictionary<string, object>;
        public bool IsArray => Raw is List<object>;

        public J this[string key] => Raw is Dictionary<string, object> map && key != null && map.TryGetValue(key, out var v) ? new J(v) : Null;
        public J this[int index] => Raw is List<object> list && index >= 0 && index < list.Count ? new J(list[index]) : Null;

        public bool Has(string key) => Raw is Dictionary<string, object> map && map.ContainsKey(key) && map[key] != null;
        public int Count => Raw is List<object> list ? list.Count : Raw is Dictionary<string, object> map ? map.Count : 0;

        public IEnumerable<J> Items
        {
            get
            {
                if (Raw is List<object> list) foreach (var item in list) yield return new J(item);
            }
        }

        public IEnumerable<KeyValuePair<string, J>> Pairs
        {
            get
            {
                if (Raw is Dictionary<string, object> map) foreach (var pair in map) yield return new KeyValuePair<string, J>(pair.Key, new J(pair.Value));
            }
        }

        public string Str(string fallback = "")
        {
            switch (Raw)
            {
                case null: return fallback;
                case string s: return s;
                case bool b: return b ? "true" : "false";
                case double d: return Math.Abs(d % 1) < 1e-9 && Math.Abs(d) < 1e15 ? ((long)d).ToString(CultureInfo.InvariantCulture) : d.ToString(CultureInfo.InvariantCulture);
                default: return Raw.ToString();
            }
        }

        public double Num(double fallback = 0)
        {
            switch (Raw)
            {
                case double d: return d;
                case bool b: return b ? 1 : 0;
                case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed): return parsed;
                default: return fallback;
            }
        }

        public long Long(long fallback = 0) => Raw == null ? fallback : (long)Math.Round(Num(fallback));
        public int Int(int fallback = 0) => Raw == null ? fallback : (int)Math.Round(Num(fallback));

        public bool Bool(bool fallback = false)
        {
            switch (Raw)
            {
                case bool b: return b;
                case double d: return Math.Abs(d) > double.Epsilon;
                case string s: return s.Length > 0 && s != "false" && s != "0";
                case null: return fallback;
                default: return true;
            }
        }

        /// <summary>First non-empty string among the given keys.</summary>
        public string Any(params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = this[key].Str();
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return string.Empty;
        }

        public override string ToString() => Str();
    }
}
