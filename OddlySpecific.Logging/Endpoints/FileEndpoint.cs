using OddlySpecific.Logging.Utility;
using System.Diagnostics;
using System.Text;

namespace OddlySpecific.Logging
{
    public sealed class FileEndpoint : ILoggerEndpoint
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        private static readonly byte[] NewLine = Encoding.GetBytes(Environment.NewLine);

        public int TabSize { get; set; }

        public bool FlushAfterWrite { get; set; }

        public string LogPath { get; }

        private Stream? stream;
        private Logger? logger;

        private FileEndpoint(string path)
        {
            LogPath = path ?? throw new ArgumentNullException(nameof(path));
        }

        void ILoggerEndpoint.Initialize(Logger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var pathDirectory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(pathDirectory) && !Directory.Exists(pathDirectory))
                Directory.CreateDirectory(pathDirectory);

            stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        }

        public void LogLine(LogLineInfo logLine)
        {
            if (stream == null)
                return;

            var prefix = BuildPrefix(logLine);
            var line = StringUtility.PrefixLines(logLine.Message, prefix);

            var buffer = Encoding.GetBytes(line);
            stream.Write(buffer, 0, buffer.Length);
            stream.Write(NewLine, 0, NewLine.Length);

            if (FlushAfterWrite)
                stream.Flush();
        }

        public void Flush()
        {
            stream?.Flush();
        }

        public void Dispose()
        {
            stream?.Dispose();
            stream = null;
        }

        private string BuildPrefix(LogLineInfo logLine)
        {
            var builder = new StringBuilder(30);

            builder.Append(TimeZoneInfo.ConvertTime(logLine.TimestampUtc, TimeZoneInfo.Local)
                                       .ToString("yyyy-MM-dd HH:mm:ss.ff"));
            builder.Append(' ');
            builder.Append("VDIWEFN"[(int)logLine.Severity]);
            builder.Append(" | ");
            builder.Append(new string(' ', logLine.Depth * TabSize));
            switch (logLine.Severity)
            {
                case Severity.Warning:
                    builder.Append("WARN (");
                    builder.Append(logger.WarningCount);
                    builder.Append("): ");
                    break;

                case Severity.Error:
                    builder.Append("ERROR(");
                    builder.Append(logger.ErrorCount);
                    builder.Append("): ");
                    break;

                case Severity.Fatal:
                    builder.Append("FATAL(");
                    builder.Append(logger.FatalCount);
                    builder.Append("): ");
                    break;
            }

            return builder.ToString();
        }

        [DebuggerStepThrough]
        public static FileEndpoint NewEndpoint(
            string path,
            int tabSize = 4,
            bool flushAfterWrite = true
        )
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return new FileEndpoint(path)
            {
                TabSize = tabSize,
                FlushAfterWrite = flushAfterWrite
            };
        }

        [DebuggerStepThrough]
        public static Logger NewLogger(
            string path,
            int tabSize = 4,
            bool flushAfterWrite = true
        )
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var endpoint = NewEndpoint(path, tabSize, flushAfterWrite);

            var logger = new Logger();
            logger.Endpoints.Add(endpoint);

            return logger;
        }
    }
}
