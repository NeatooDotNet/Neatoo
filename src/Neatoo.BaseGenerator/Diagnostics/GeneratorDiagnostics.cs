using Microsoft.CodeAnalysis;

namespace Neatoo.BaseGenerator.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the Neatoo source generator.
/// All diagnostics are warnings to ensure builds succeed while making issues visible.
/// </summary>
internal static class GeneratorDiagnostics
{
    private const string Category = "Neatoo.SourceGenerator";

    /// <summary>
    /// NEATOO001: Reported when an exception occurs during source generation.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratorException = new(
        id: "NEATOO001",
        title: "Source Generator Error",
        messageFormat: "Neatoo source generator error in {0}: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An exception occurred during source generation. Check the exception message for details.");

    /// <summary>
    /// NEATOO002: Reported with stack trace information in debug builds only.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratorStackTrace = new(
        id: "NEATOO002",
        title: "Source Generator Stack Trace",
        messageFormat: "Stack trace for NEATOO001: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Stack trace information for the source generator error. Only reported in debug builds.");

    /// <summary>
    /// NEATOO003: Reported when the semantic target generation fails.
    /// </summary>
    public static readonly DiagnosticDescriptor SemanticTargetException = new(
        id: "NEATOO003",
        title: "Semantic Target Generation Error",
        messageFormat: "Failed to get semantic target for class '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An exception occurred while analyzing a class for source generation.");

    /// <summary>
    /// NEATOO004: Reported when getting base class declaration fails.
    /// </summary>
    public static readonly DiagnosticDescriptor BaseClassResolutionError = new(
        id: "NEATOO004",
        title: "Base Class Resolution Error",
        messageFormat: "Failed to resolve base class for '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An exception occurred while resolving the base class declaration syntax.");

    /// <summary>
    /// Helper method to report an exception with optional stack trace in debug builds.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="descriptor">The diagnostic descriptor to use.</param>
    /// <param name="location">The location in source code, or Location.None.</param>
    /// <param name="messageArgs">Arguments for the message format.</param>
    public static void ReportException(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
    }

    /// <summary>
    /// Helper method to report an exception with stack trace in debug builds.
    /// </summary>
    /// <param name="context">The source production context.</param>
    /// <param name="descriptor">The primary diagnostic descriptor.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="location">The location in source code, or Location.None.</param>
    /// <param name="contextName">The name of the context where the error occurred (e.g., class name).</param>
    public static void ReportExceptionWithStackTrace(
        SourceProductionContext context,
        DiagnosticDescriptor descriptor,
        Exception exception,
        Location location,
        string contextName)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            descriptor,
            location,
            contextName,
            exception.Message));

#if DEBUG
        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                GeneratorStackTrace,
                location,
                exception.StackTrace));
        }
#endif
    }
}
