using OddlySpecific.Logging.Utility;
using System.Diagnostics;
using System.Text;

namespace OddlySpecific.Logging
{
    public sealed class ConsoleEndpoint : ILoggerEndpoint
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        private static readonly byte[] NewLine = Encoding.GetBytes(Environment.NewLine);

        public bool EnableColorCoding { get; set; }

        public int TabSize { get; set; }

        public bool FlushAfterWrite { get; set; }

        private ConsoleColor foreColor;

        private readonly BufferedStream stream;

        private ConsoleEndpoint()
        {
            Console.OutputEncoding = Encoding;
            foreColor = Console.ForegroundColor;

            var stdout = Console.OpenStandardOutput();
            stream = new BufferedStream(stdout, 0x15000);
        }

        void ILoggerEndpoint.Initialize(Logger logger)
        {
            // NOTE Nothing to do
        }

        public void LogLine(LogLineInfo logLine)
        {
            // TODO Currently doesn't work with the buffer logging...
            if (EnableColorCoding)
            {
                var targetColor = GetColorForSeverity(logLine.Severity);
                if (targetColor != foreColor)
                {
                    Console.ForegroundColor = targetColor;
                    foreColor = targetColor;
                }
            }

            // TODO Might be able to be even faster with Spans
            var prefix = new string(' ', logLine.Depth * TabSize);
            var line = StringUtility.PrefixLines(logLine.Message, prefix);

            var buffer = Encoding.GetBytes(line);
            stream.Write(buffer, 0, buffer.Length);
            stream.Write(NewLine, 0, NewLine.Length);

            if (FlushAfterWrite)
                stream.Flush();
        }

        public void Flush()
        {
            stream.Flush();
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        private static ConsoleColor GetColorForSeverity(Severity severity)
        {
            switch (severity)
            {
                case Severity.Verbose:
                case Severity.Diag:
                case Severity.Info:
                    return ConsoleColor.Gray;

                case Severity.Warning:
                    return ConsoleColor.Yellow;

                case Severity.Error:
                case Severity.Fatal:
                    return ConsoleColor.Red;

                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        [DebuggerStepThrough]
        public static ConsoleEndpoint NewEndpoint(
            int tabSize = 4,
            bool enableColorCoding = true
        )
        {
            return new ConsoleEndpoint
            {
                TabSize = tabSize,
                EnableColorCoding = enableColorCoding
            };
        }

        [DebuggerStepThrough]
        public static Logger NewLogger(
            int tabSize = 4,
            bool enableColorCoding = true
        )
        {
            var logger = new Logger();
            logger.Endpoints.Add(NewEndpoint(tabSize, enableColorCoding));

            return logger;
        }
    }
}
