using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BeaKona.AutoAsGenerator;

internal static class ITypeSymbolExtensions
{
    public static bool IsPartial(this ITypeSymbol @this)
    {
        foreach (SyntaxReference syntax in @this.DeclaringSyntaxReferences)
        {
            if (syntax.GetSyntax() is MemberDeclarationSyntax declaration)
            {
                if (declaration.Modifiers.Any(i => i.IsKind(SyntaxKind.PartialKeyword)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static INamedTypeSymbol[] GetContainingTypes(this ITypeSymbol @this)
    {
        List<INamedTypeSymbol> containingTypes = [];

        for (INamedTypeSymbol? ct = @this.ContainingType; ct != null; ct = ct.ContainingType)
        {
            containingTypes.Insert(0, ct);
        }

        return [.. containingTypes];
    }
}
