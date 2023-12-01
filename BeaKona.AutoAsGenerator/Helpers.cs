namespace BeaKona.AutoAsGenerator;
internal static class Helpers
{
    public static void ReportDiagnostic(GeneratorExecutionContext context, string id, string title, string message, string description, DiagnosticSeverity severity, SyntaxNode? node, params object?[] messageArgs)
    {
        Helpers.ReportDiagnostic(context, id, title, message, description, severity, node?.GetLocation(), messageArgs);
    }

    public static void ReportDiagnostic(GeneratorExecutionContext context, string id, string title, string message, string description, DiagnosticSeverity severity, ISymbol? member, params object?[] messageArgs)
    {
        Helpers.ReportDiagnostic(context, id, title, message, description, severity, member != null && member.Locations.Length > 0 ? member.Locations[0] : null, messageArgs);
    }

    public static void ReportDiagnostic(GeneratorExecutionContext context, string id, string title, string message, string description, DiagnosticSeverity severity, Location? location, params object?[] messageArgs)
    {
        var lTitle = new LocalizableResourceString(title, AutoAsResource.ResourceManager, typeof(AutoAsResource));
        var lMessage = new LocalizableResourceString(message, AutoAsResource.ResourceManager, typeof(AutoAsResource));
        var lDescription = new LocalizableResourceString(description, AutoAsResource.ResourceManager, typeof(AutoAsResource));
        var category = typeof(GenerateAutoAsSourceGenerator).Namespace;
        var link = "https://github.com/beakona/AutoAs";

        var dd = new DiagnosticDescriptor(id, lTitle, lMessage, category, severity, true, lDescription, link, WellKnownDiagnosticTags.NotConfigurable);
        var d = Diagnostic.Create(dd, location, messageArgs);
        context.ReportDiagnostic(d);
    }
}
