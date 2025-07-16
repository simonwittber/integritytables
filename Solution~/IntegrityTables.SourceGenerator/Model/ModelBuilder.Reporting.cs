using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntegrityTables.SourceGeneration.Model;

public static partial class ModelBuilder
{
#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor BrokenConvention = new(
        id: "ITG003",
        title: "Broken Convention",
        messageFormat: "{0}: {1}",
        category: "IntegrityTables.SourceGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
#pragma warning restore RS2008
    
    private static void ReportConventionError(SourceProductionContext context, Location location, ITypeSymbol typeArgument, string msg)
    {
        ReportDiagnostic(context, location, typeArgument.Name, msg);
    }

    private static void ReportDiagnostic(SourceProductionContext context, Location location, string arg, string msg)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            BrokenConvention,
            location,
            arg,
            msg
        ));
    }

    private static void ReportConventionError(SourceProductionContext context, IParameterSymbol symbol, string msg)
    {
        ReportDiagnostic(context, symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(), symbol.Name, msg);
    }

    private static void ReportConventionError(SourceProductionContext context, IMethodSymbol method, string msg)
    {
        ReportDiagnostic(context,
            method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
            method.Name,
            msg
        );
    }

    private static void ReportConventionError(SourceProductionContext context, IFieldSymbol fieldSymbol, string msg)
    {
        ReportDiagnostic(context,
            fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation(),
            fieldSymbol.Name,
            msg
        );
    }
}