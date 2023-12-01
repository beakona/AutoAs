namespace BeaKona.AutoAsGenerator;

internal static class SemanticFacts
{
    public static string? ResolveAssemblyAlias(Compilation compilation, IAssemblySymbol assembly)
    {
        //MetadataReferenceProperties.GlobalAlias
        if (compilation.GetMetadataReference(assembly) is MetadataReference mr)
        {
            foreach (string alias in mr.Properties.Aliases)
            {
                if (string.IsNullOrEmpty(alias) == false)
                {
                    return alias;
                }
            }
        }

        return null;
    }

    public static ISymbol[] GetRelativeSymbols(ITypeSymbol type, ITypeSymbol scope)
    {
        ISymbol[] typeSymbols = GetContainingSymbols(type, false);
        ISymbol[] scopeSymbols = GetContainingSymbols(scope, true);

        int count = Math.Min(typeSymbols.Length, scopeSymbols.Length);
        for (int i = 0; i < count; i++)
        {
            if (typeSymbols[i].Equals(scopeSymbols[i], SymbolEqualityComparer.Default) == false)
            {
                int remaining = typeSymbols.Length - i;
                if (remaining > 0)
                {
                    ISymbol[] result = new ISymbol[remaining];
                    Array.Copy(typeSymbols, i, result, 0, result.Length);
                    return result;
                }
                else
                {
                    break;
                }
            }
        }

        return [];
    }

    public static ISymbol[] GetContainingSymbols(ITypeSymbol type, bool includeSelf)
    {
        List<ISymbol> symbols = [];

        for (ISymbol t = includeSelf ? type : type.ContainingSymbol; t != null; t = t.ContainingSymbol)
        {
            if (t is IModuleSymbol || t is IAssemblySymbol)
            {
                break;
            }
            if (t is INamespaceSymbol tn && tn.IsGlobalNamespace)
            {
                break;
            }
            symbols.Insert(0, t);
        }

        return [.. symbols];
    }

    public static bool IsNullableT(Compilation compilation, INamedTypeSymbol type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.IsValueType)
        {
            if (type.IsGenericType && type.IsUnboundGenericType == false)
            {
                INamedTypeSymbol symbolNullableT = compilation.GetSpecialType(SpecialType.System_Nullable_T);

                return symbolNullableT.ConstructUnboundGenericType().Equals(type.ConstructUnboundGenericType(), SymbolEqualityComparer.Default);
            }
        }

        return false;
    }
}
