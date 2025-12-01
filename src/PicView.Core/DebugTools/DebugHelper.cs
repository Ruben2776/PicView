using System.Diagnostics;
using System.Globalization;

namespace PicView.Core.DebugTools;

/// <summary>
///     Provides helper methods for logging debug information during development.
/// </summary>
public static class DebugHelper
{
    /// <summary>
    /// Logs detailed debug information for an exception, including the class name, method name, message, and stack trace.
    /// This method is intended to be used for development and debugging purposes only.
    /// </summary>
    /// <param name="className">
    /// The name of the class where the exception occurred. Typically passed using <c>nameof(ClassName)</c>.
    /// </param>
    /// <param name="methodName">
    /// The name of the method where the exception occurred. Typically passed using
    /// <c>nameof(MethodName)</c>.
    /// </param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <example>
    /// <code>
    /// catch (Exception ex)
    /// {
    ///     DebugHelper.LogDebug(nameof(MyClass), nameof(MyMethod), ex);
    /// }
    /// </code>
    /// </example>
    [Conditional("DEBUG")]
    public static void LogDebug(string className, string methodName, Exception exception)
    {
        Debug.WriteLine(
            $"\n[{DateTime.Now.ToString("T", CultureInfo.CurrentCulture)}] {className}.{methodName} exception: {exception.Message}");
        Debug.WriteLine(exception.StackTrace + Environment.NewLine);
    }

    /// <inheritdoc cref="LogDebug(string,string,System.Exception)"/>
    [Conditional("DEBUG")]
    public static void LogDebug(string className, string methodName, string exceptionMessage)
    {
        Debug.WriteLine(
            $"\n[{DateTime.Now.ToString("T", CultureInfo.CurrentCulture)}] {className}.{methodName} exception: {exceptionMessage}");
    }
}