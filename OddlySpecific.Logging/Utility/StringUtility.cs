using System.Diagnostics;
using System.Text;

namespace OddlySpecific.Logging.Utility
{
    internal static class StringUtility
    {
        [DebuggerStepThrough]
        public static string PrefixLines(string value, string prefix)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrEmpty(prefix))
                return value;

            var builder = new StringBuilder(value, value.Length + 8 * prefix.Length);
            int i = 0;
            do
            {
                builder.Insert(i, prefix);
                i = builder.IndexOf('\n', i + 1) + 1;
            } while (i != 0);

            return builder.ToString();
        }

        public static int IndexOf(this StringBuilder builder, char ch, int offset)
        {
            var len = builder.Length;
            while (offset < len)
            {
                if (builder[offset] == ch)
                    return offset;

                ++offset;
            }

            return -1;
        }

        [DebuggerStepThrough]
        public static string QuotedOrDefault(this object? value, string defaultValue = "null")
        {
            if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));

            return value == null
                ? defaultValue
                : "\"" + value + '"';
        }

        [DebuggerStepThrough]
        public static string NormalizeLF(this string value, string? lineFeed = null)
        {
            if (string.IsNullOrEmpty(value) || (!value.Contains('\r') && !value.Contains('\n')))
                return value;

            lineFeed ??= Environment.NewLine;

            value = value.Replace("\r\n", "\n").Replace('\r', '\n');

            if (lineFeed != "\n")
                value = value.Replace("\n", lineFeed);

            return value;
        }

        // From https://stackoverflow.com/a/24769702
        [DebuggerStepThrough]
        public static StringBuilder TrimEnd(this StringBuilder sb)
        {
            if (sb.Length == 0) return sb;

            int i = sb.Length - 1;

            for (; i >= 0; i--)
                if (!char.IsWhiteSpace(sb[i]))
                    break;

            if (i < sb.Length - 1)
                sb.Length = i + 1;

            return sb;
        }
    }
}
