using OddlySpecific.Logging.Utility;
using System.Runtime.CompilerServices;

namespace OddlySpecific.Logging
{
    public sealed partial class Logger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext VerboseContext(string message)
        {
            return Context(Severity.Verbose, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext DiagContext(string message)
        {
            return Context(Severity.Diag, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext InfoContext(string message)
        {
            return Context(Severity.Info, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext WarningContext(string message)
        {
            return Context(Severity.Warning, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext ErrorContext(string message)
        {
            return Context(Severity.Error, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext FatalContext(string message)
        {
            return Context(Severity.Fatal, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext VerboseContext(string format, params Func<object>[] args)
        {
            return Context(Severity.Verbose, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext DiagContext(string format, params Func<object>[] args)
        {
            return Context(Severity.Diag, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext InfoContext(string format, params Func<object>[] args)
        {
            return Context(Severity.Info, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext WarningContext(string format, params Func<object>[] args)
        {
            return Context(Severity.Warning, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext ErrorContext(string format, params Func<object>[] args)
        {
            return Context(Severity.Error, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LoggerContext FatalContext(string format, params Func<object>[] args)
        {
            return Context(Severity.Fatal, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogVerbose(string message)
        {
            Log(Severity.Verbose, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDiag(string message)
        {
            Log(Severity.Diag, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string message)
        {
            Log(Severity.Info, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message)
        {
            Log(Severity.Warning, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message)
        {
            Log(Severity.Error, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFatal(string message)
        {
            Log(Severity.Fatal, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogVerbose(string format, params Func<object>[] args)
        {
            Log(Severity.Verbose, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDiag(string format, params Func<object>[] args)
        {
            Log(Severity.Diag, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(string format, params Func<object>[] args)
        {
            Log(Severity.Info, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string format, params Func<object>[] args)
        {
            Log(Severity.Warning, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string format, params Func<object>[] args)
        {
            Log(Severity.Error, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFatal(string format, params Func<object>[] args)
        {
            Log(Severity.Fatal, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogVerbose(Exception ex)
        {
            Log(Severity.Verbose, ExceptionUtility.GenerateTextBlock(ex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDiag(Exception ex)
        {
            Log(Severity.Diag, ExceptionUtility.GenerateTextBlock(ex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo(Exception ex)
        {
            Log(Severity.Info, ExceptionUtility.GenerateTextBlock(ex));
        }

        public void LogWarning(Exception ex)
        {
            Log(Severity.Warning, ExceptionUtility.GenerateTextBlock(ex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(Exception ex)
        {
            Log(Severity.Error, ExceptionUtility.GenerateTextBlock(ex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFatal(Exception ex)
        {
            Log(Severity.Fatal, ExceptionUtility.GenerateTextBlock(ex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogVerbose()
        {
            Log(Severity.Verbose, "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDiag()
        {
            Log(Severity.Diag, "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInfo()
        {
            Log(Severity.Info, "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning()
        {
            Log(Severity.Warning, "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError()
        {
            Log(Severity.Error, "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFatal()
        {
            Log(Severity.Fatal, "");
        }
    }
}
