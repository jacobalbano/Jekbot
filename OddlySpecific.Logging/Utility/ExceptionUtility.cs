using System.Text;

namespace OddlySpecific.Logging.Utility
{
    internal static class ExceptionUtility
    {
        public static string GenerateTextBlock(Exception? ex, bool includeStackTrace = true)
        {
            var builder = new StringBuilder(1024);

            if (ex == null)
                builder.AppendLine("(null exception)");
            else
            {
                var first = true;
                var prefix = "Exception";

                while (ex != null)
                {
                    if (!first)
                    {
                        prefix = "Detail";
                        builder.AppendLine();
                    }

                    if (ex is AggregateException aggEx)
                    {
                        WriteException(builder, ex, prefix, includeStackTrace);

                        for (int i = 0; i < aggEx.InnerExceptions.Count; ++i)
                        {
                            var iAggEx = aggEx.InnerExceptions[i];
                            while (iAggEx != null)
                            {
                                WriteException(builder, iAggEx, "Ex[" + i + "]", includeStackTrace);
                                iAggEx = iAggEx.InnerException;
                            }
                        }

                        break;
                    }
                    else if (ex is ThreadAbortException taEx)
                    {
                        if (taEx.ExceptionState is Exception esEx)
                            WriteException(builder, esEx, "ThreadAbort.State", includeStackTrace);
                        else if (taEx.ExceptionState != null)
                            builder.AppendLine($"ThreadAbort.State: {taEx.ExceptionState}");
                    }

                    WriteException(builder, ex, prefix, includeStackTrace);

                    ex = ex.InnerException;
                    first = false;
                }
            }

            return builder.TrimEnd().ToString();
        }

        private static void WriteException(StringBuilder builder, Exception ex, string prefix, bool includeStackTrace)
        {
            builder.AppendLine($"{prefix}: {ex.Message} (Type: {ex.GetType()})");

            if (includeStackTrace)
            {
                var stackTrace = ex.StackTrace;
                if (!string.IsNullOrEmpty(stackTrace))
                    stackTrace = stackTrace.TrimEnd();
                if (!string.IsNullOrEmpty(stackTrace))
                    builder.AppendLine($"Stack Trace:\n{stackTrace}");
                else
                    builder.AppendLine("\t(No stack trace available)");
            }
        }
    }
}
