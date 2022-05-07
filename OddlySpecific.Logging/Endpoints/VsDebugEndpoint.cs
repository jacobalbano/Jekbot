using OddlySpecific.Logging.Utility;
using System.Diagnostics;

namespace OddlySpecific.Logging
{
    public sealed class VsDebugEndpoint : ILoggerEndpoint
    {
        public int TabSize { get; set; }

        private VsDebugEndpoint()
        {
            // NOTE Private ctor to enforce factory pattern
        }

        void ILoggerEndpoint.Initialize(Logger logger)
        {
            // NOTE Nothing to do
        }

        public void LogLine(LogLineInfo logLine)
        {
            // NOTE Keeping the branches inside of the Debug.WriteLine call will mean that in a
            //      Release build this method will be empty as these calls are optimized out
            Debug.WriteLine(
                StringUtility.PrefixLines(
                    logLine.Message,
                    new string(' ', logLine.Depth * TabSize)
                )
            );
        }

        public void Dispose()
        {
            // NOTE Nothing to do
        }

        [DebuggerStepThrough]
        public static VsDebugEndpoint NewEndpoint(
            int tabSize = 4
        )
        {
            return new VsDebugEndpoint
            {
                TabSize = tabSize
            };
        }

        [DebuggerStepThrough]
        public static Logger NewLogger(
            int tabSize = 4
        )
        {
            var logger = new Logger();
            logger.Endpoints.Add(NewEndpoint(tabSize));

            return logger;
        }
    }
}
