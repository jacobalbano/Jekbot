using OddlySpecific.Logging.Utility;
using System.Collections;

namespace OddlySpecific.Logging
{
    public partial class Logger : IDisposable
    {
        public Severity SeverityThreshold { get; set; }

        public EndpointCollection Endpoints { get; }

        public int WarningCount { get; private set; }

        public int ErrorCount { get; private set; }

        public int FatalCount { get; private set; }

        private readonly List<LoggerContext> contextStack;

        public Logger()
        {
            SeverityThreshold = Severity.Info;

            Endpoints = new EndpointCollection(this);
            contextStack = new List<LoggerContext>(4);
        }

        public LoggerContext Context(Severity severity, string format, params Func<object>[] args)
        {
            var message = SafelyFormat(format, args);
            return Context(severity, message);
        }

        public LoggerContext Context(Severity severity, string message)
        {
            Log(severity, message);
            return new LoggerContext(severity, this);
        }

        public void Log(Severity severity, string format, params Func<object>[] args)
        {
            var message = SafelyFormat(format, args);
            Log(severity, message);
        }

        public void Log(Severity severity, string message)
        {
            if (!IsSeverityVisible(severity))
                return;

            if (severity == Severity.Error)
                ++ErrorCount;
            else if (severity == Severity.Warning)
                ++WarningCount;
            else if (severity == Severity.Fatal)
                ++FatalCount;

            message = message.NormalizeLF();
            var logLine = new LogLineInfo(severity, message, contextStack.Count);
            DispatchLogLine(logLine);
        }

        private void DispatchLogLine(LogLineInfo logLine)
        {
            try
            {
                for (int i = 0; i < Endpoints.Count; ++i)
                    Endpoints[i].LogLine(logLine);
            }
            catch { /* Squash? */ }
        }

        private bool IsSeverityVisible(Severity severity)
        {
            if (severity < SeverityThreshold)
                return false;

            for (var i = contextStack.Count - 1; i >= 0; --i)
            {
                var item = contextStack[i];
                if (item.Severity < SeverityThreshold)
                    return false;
            }

            return true;
        }

        private void PushContext(LoggerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            contextStack.Add(context);
        }

        private void PopContext()
        {
            contextStack.RemoveAt(contextStack.Count - 1);
        }

        public void Dispose()
        {
            Endpoints.Dispose();
        }

        private static string SafelyFormat(string format, IReadOnlyList<Func<object>> args)
        {
            try
            {
                if (args.Count == 0)
                    return format;

                var result = new object[args.Count];
                for (int i = 0; i < args.Count; ++i)
                {
                    try
                    {
                        result[i] = args[i]();

                        if (result[i] is Exception pex)
                            result[i] = ExceptionUtility.GenerateTextBlock(pex);
                    }
                    catch (Exception ex)
                    {
                        result[i] = $"(! Exception: {ex.Message} !)";
                    }
                }

                return string.Format(format, result);
            }
            catch
            {
                return $"(! MALFORMED: {format.QuotedOrDefault()} !)";
            }
        }

        public sealed class LoggerContext : IDisposable
        {
            public Severity Severity { get; }

            private string? footerMessage;

            private readonly Logger logger;

            public LoggerContext(Severity severity, Logger logger)
            {
                Severity = severity;

                this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

                logger.PushContext(this);
            }

            public LoggerContext WithFooter(string message)
            {
                if (footerMessage != null)
                    throw new InvalidOperationException("Cannot specify a footer because one has already been set");

                footerMessage = message ?? throw new ArgumentNullException(nameof(message));

                return this;
            }

            public LoggerContext WithFooter(string format, params Func<object>[] args)
            {
                if (format == null) throw new ArgumentNullException(nameof(format));
                if (args == null) throw new ArgumentNullException(nameof(args));

                var message = SafelyFormat(format, args);
                return WithFooter(message);
            }

            public void Dispose()
            {
                if (footerMessage != null)
                    logger.Log(Severity, footerMessage);

                logger.PopContext();
            }
        }

        public sealed class EndpointCollection : IList<ILoggerEndpoint>, IDisposable
        {
            public int Count => endpoints.Count;

            public bool IsReadOnly => false;

            private readonly List<ILoggerEndpoint> endpoints;
            private readonly Logger logger;

            public ILoggerEndpoint this[int index]
            {
                get => endpoints[index];
                set => throw new NotSupportedException();
            }

            internal EndpointCollection(Logger logger)
            {
                this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
                endpoints = new List<ILoggerEndpoint>(2);
            }

            public T? Get<T>()
                where T : ILoggerEndpoint
            {
                return (T?)endpoints.SingleOrDefault(x => x is T);
            }

            public IEnumerable<T> GetAll<T>()
                where T : ILoggerEndpoint
            {
                return endpoints.OfType<T>();
            }

            public void Remove<T>()
                where T : ILoggerEndpoint
            {
                var toRemove = GetAll<T>().ToList();
                foreach (var x in toRemove)
                    Remove(x);
            }

            public void Add(ILoggerEndpoint item)
            {
                item.Initialize(logger);
                endpoints.Add(item);
            }

            public bool Remove(ILoggerEndpoint item)
            {
                var result = endpoints.Remove(item);
                if (result)
                    item.Dispose();

                return result;
            }

            public void Clear()
            {
                foreach (var endpoint in endpoints)
                    endpoint.Dispose();

                endpoints.Clear();
            }

            public bool Contains(ILoggerEndpoint item)
            {
                return endpoints.Contains(item);
            }

            public void CopyTo(ILoggerEndpoint[] array, int arrayIndex)
            {
                throw new NotSupportedException();
            }

            public int IndexOf(ILoggerEndpoint item)
            {
                return endpoints.IndexOf(item);
            }

            public void Insert(int index, ILoggerEndpoint item)
            {
                endpoints.Insert(index, item);
            }

            public void RemoveAt(int index)
            {
                endpoints[index].Dispose();
                endpoints.RemoveAt(index);
            }

            public void Dispose()
            {
                foreach (var endpoint in endpoints)
                    endpoint.Dispose();
            }

            public IEnumerator<ILoggerEndpoint> GetEnumerator()
            {
                return endpoints.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }

    public interface ILoggerEndpoint : IDisposable
    {
        void Initialize(Logger logger);

        void LogLine(LogLineInfo logLine);
    }

    // TODO Consider getting rid of this to reduce memory pressure
    public record struct LogLineInfo
    {
        public DateTime TimestampUtc { get; }

        public Severity Severity { get; }

        public string Message { get; }

        public int Depth { get; }

        internal LogLineInfo(Severity severity, string message, int depth)
        {
            TimestampUtc = DateTime.UtcNow;
            Severity = severity;
            Message = message;
            Depth = depth;
        }
    }

    public enum Severity
    {
        /// <summary>
        /// Information that is only useful for developers. Useful for production debugging,
        /// and it's ok if the amount of logging here would impact performance.
        /// Examples: Loop indices, line-by-line debugging, etc
        /// </summary>
        Verbose = 0,

        /// <summary>
        /// Information that is only useful for investigating an issue in production.
        /// Examples: Path of files being operated on, configuration dump, etc
        /// </summary>
        Diag = 1,

        /// <summary>
        /// Information that relates to normal operation
        /// Examples: Status messages, etc
        /// </summary>
        Info = 2,

        /// <summary>
        /// Information that relates to an issue that relates to a potential issue.
        /// Examples: Misconfiguration, lack of credentials, etc
        /// </summary>
        Warning = 3,

        /// <summary>
        /// Information that relates to an non-fatal error that occurred. Meaning that
        /// the software is able to recover and continue operation.
        /// Example: I/O Errors, lost connection, etc.
        /// </summary>
        Error = 4,

        /// <summary>
        /// Information that relates to a fatal error that occurred. The software can
        /// no longer continue to operate and must terminate.
        /// Examples: Missing configuration, unhandled exceptions, etc.
        /// </summary>
        Fatal = 5,

        /// <summary>
        /// Logging is disabled
        /// </summary>
        None = 6
    }
}