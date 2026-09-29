using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pollinations
{
    /// <summary>
    /// A small JSON reader and writer. Unity's JsonUtility cannot describe the
    /// model catalogue, which is a list of objects with free-form fields, and
    /// Newtonsoft is an extra dependency a game should not have to add, so the
    /// package carries its own parser.
    /// </summary>
    public static class PollinationsJson
    {
        /// <summary>Parses text into Dictionary&lt;string, object&gt;, List&lt;object&gt;, string, double, bool or null. Returns false when the text is not valid JSON.</summary>
        public static bool TryParse(string text, out object value)
        {
            value = null;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int index = 0;
            try
            {
                value = ParseValue(text, ref index);
            }
            catch (FormatException)
            {
                value = null;
                return false;
            }

            SkipWhitespace(text, ref index);
            return index == text.Length;
        }

        public static object Parse(string text)
        {
            object value;
            if (!TryParse(text, out value))
            {
                throw new FormatException("Not valid JSON.");
            }

            return value;
        }

        /// <summary>Parses text as a JSON object; returns null when it is not one.</summary>
        public static Dictionary<string, object> ParseObject(string text)
        {
            return Parse(text) as Dictionary<string, object>;
        }

        public static Dictionary<string, object> AsObject(object value)
        {
            return value as Dictionary<string, object>;
        }

        public static List<object> AsArray(object value)
        {
            return value as List<object>;
        }

        public static string AsString(object value)
        {
            if (value == null)
            {
                return null;
            }

            var text = value as string;
            if (text != null)
            {
                return text;
            }

            if (value is double)
            {
                return ((double)value).ToString("0.################", CultureInfo.InvariantCulture);
            }

            if (value is bool)
            {
                return ((bool)value) ? "true" : "false";
            }

            return null;
        }

        public static double AsNumber(object value, double fallback = 0.0)
        {
            if (value is double)
            {
                return (double)value;
            }

            var text = value as string;
            double parsed;
            if (text != null && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        public static string GetString(Dictionary<string, object> source, string key, string fallback = "")
        {
            if (source == null || !source.ContainsKey(key))
            {
                return fallback;
            }

            var text = AsString(source[key]);
            return text ?? fallback;
        }

        public static double GetNumber(Dictionary<string, object> source, string key, double fallback)
        {
            if (source == null || !source.ContainsKey(key))
            {
                return fallback;
            }

            return AsNumber(source[key], fallback);
        }

        public static string Write(object value)
        {
            var builder = new StringBuilder();
            WriteValue(builder, value);
            return builder.ToString();
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            var text = value as string;
            if (text != null)
            {
                WriteString(builder, text);
                return;
            }

            if (value is bool)
            {
                builder.Append(((bool)value) ? "true" : "false");
                return;
            }

            if (value is double || value is float || value is int || value is long)
            {
                builder.Append(Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("0.################", CultureInfo.InvariantCulture));
                return;
            }

            var dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                builder.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> pair in dictionary)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    WriteString(builder, pair.Key);
                    builder.Append(':');
                    WriteValue(builder, pair.Value);
                }

                builder.Append('}');
                return;
            }

            var list = value as IEnumerable<object>;
            if (list != null)
            {
                builder.Append('[');
                bool first = true;
                foreach (object item in list)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    WriteValue(builder, item);
                }

                builder.Append(']');
                return;
            }

            WriteString(builder, value.ToString());
        }

        private static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');
            foreach (char character in text)
            {
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < ' ')
                        {
                            builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        private static object ParseValue(string text, ref int index)
        {
            SkipWhitespace(text, ref index);
            if (index >= text.Length)
            {
                throw new FormatException("Unexpected end of JSON.");
            }

            char character = text[index];
            switch (character)
            {
                case '{':
                    return ParseObjectBody(text, ref index);
                case '[':
                    return ParseArrayBody(text, ref index);
                case '"':
                    return ParseString(text, ref index);
                case 't':
                    Expect(text, ref index, "true");
                    return true;
                case 'f':
                    Expect(text, ref index, "false");
                    return false;
                case 'n':
                    Expect(text, ref index, "null");
                    return null;
                default:
                    return ParseNumber(text, ref index);
            }
        }

        private static Dictionary<string, object> ParseObjectBody(string text, ref int index)
        {
            var result = new Dictionary<string, object>();
            index++;
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == '}')
            {
                index++;
                return result;
            }

            while (true)
            {
                SkipWhitespace(text, ref index);
                if (index >= text.Length || text[index] != '"')
                {
                    throw new FormatException("Expected a property name.");
                }

                string key = ParseString(text, ref index);
                SkipWhitespace(text, ref index);
                if (index >= text.Length || text[index] != ':')
                {
                    throw new FormatException("Expected ':'.");
                }

                index++;
                result[key] = ParseValue(text, ref index);
                SkipWhitespace(text, ref index);
                if (index >= text.Length)
                {
                    throw new FormatException("Unterminated object.");
                }

                if (text[index] == ',')
                {
                    index++;
                    continue;
                }

                if (text[index] == '}')
                {
                    index++;
                    return result;
                }

                throw new FormatException("Expected ',' or '}'.");
            }
        }

        private static List<object> ParseArrayBody(string text, ref int index)
        {
            var result = new List<object>();
            index++;
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == ']')
            {
                index++;
                return result;
            }

            while (true)
            {
                result.Add(ParseValue(text, ref index));
                SkipWhitespace(text, ref index);
                if (index >= text.Length)
                {
                    throw new FormatException("Unterminated array.");
                }

                if (text[index] == ',')
                {
                    index++;
                    continue;
                }

                if (text[index] == ']')
                {
                    index++;
                    return result;
                }

                throw new FormatException("Expected ',' or ']'.");
            }
        }

        private static string ParseString(string text, ref int index)
        {
            index++;
            var builder = new StringBuilder();
            while (true)
            {
                if (index >= text.Length)
                {
                    throw new FormatException("Unterminated string.");
                }

                char character = text[index++];
                if (character == '"')
                {
                    return builder.ToString();
                }

                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }

                if (index >= text.Length)
                {
                    throw new FormatException("Unterminated escape.");
                }

                char escape = text[index++];
                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (index + 4 > text.Length)
                        {
                            throw new FormatException("Truncated \\u escape.");
                        }

                        ushort code;
                        if (!ushort.TryParse(text.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                        {
                            throw new FormatException("Bad \\u escape.");
                        }

                        builder.Append((char)code);
                        index += 4;
                        break;
                    default:
                        throw new FormatException("Unknown escape.");
                }
            }
        }

        private static object ParseNumber(string text, ref int index)
        {
            int start = index;
            while (index < text.Length)
            {
                char character = text[index];
                if ((character >= '0' && character <= '9') || character == '-' || character == '+' || character == '.' || character == 'e' || character == 'E')
                {
                    index++;
                    continue;
                }

                break;
            }

            string slice = text.Substring(start, index - start);
            double number;
            if (slice.Length == 0 || !double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                throw new FormatException("Not a number.");
            }

            return number;
        }

        private static void Expect(string text, ref int index, string literal)
        {
            if (index + literal.Length > text.Length || string.CompareOrdinal(text, index, literal, 0, literal.Length) != 0)
            {
                throw new FormatException("Expected " + literal + ".");
            }

            index += literal.Length;
        }

        private static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length)
            {
                char character = text[index];
                if (character == ' ' || character == '\t' || character == '\n' || character == '\r')
                {
                    index++;
                    continue;
                }

                break;
            }
        }
    }
}
