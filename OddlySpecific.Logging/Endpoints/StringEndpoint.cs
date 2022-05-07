using OddlySpecific.Logging.Utility;
using System.Diagnostics;
using System.Text;

namespace OddlySpecific.Logging
{
    public sealed class StringEndpoint : ILoggerEndpoint
    {
        public int TabSize { get; set; }

        private readonly StringBuilder builder;

        private StringEndpoint(int capacity)
        {
            builder = new StringBuilder(capacity);
        }

        void ILoggerEndpoint.Initialize(Logger logger)
        {
            // NOTE Nothing to do
        }

        public void LogLine(LogLineInfo logLine)
        {
            var prefix = new string(' ', logLine.Depth * TabSize);
            var line = StringUtility.PrefixLines(logLine.Message, prefix);
            builder.AppendLine(line);
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        void IDisposable.Dispose()
        {
            // NOTE Nothing to do
        }

        [DebuggerStepThrough]
        public static StringEndpoint NewEndpoint(
            int tabSize = 4,
            int capacity = 4096
        )
        {
            return new StringEndpoint(capacity)
            {
                TabSize = tabSize
            };
        }

        [DebuggerStepThrough]
        public static Logger NewLogger(
            int tabSize = 4,
            int capacity = 4096
        )
        {
            var logger = new Logger();
            logger.Endpoints.Add(NewEndpoint(tabSize, capacity));
            return logger;
        }
    }
}
