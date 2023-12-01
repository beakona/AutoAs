namespace BeaKona.AutoAsGenerator;

internal interface ICodeTextWriter
{
    Compilation Compilation { get; }

    void WriteTypeReference(SourceBuilder builder, ITypeSymbol type, ScopeInfo scope);

    void WriteTypeArgumentsDefinition(SourceBuilder builder, IEnumerable<ITypeSymbol> typeArguments, ScopeInfo scope);
    void WriteTypeDeclarationBeginning(SourceBuilder builder, INamedTypeSymbol type, ScopeInfo scope);

    void WriteNamespaceBeginning(SourceBuilder builder, INamespaceSymbol @namespace);

    void WriteHolderReference(SourceBuilder builder, ISymbol member, ScopeInfo scope);

    void WriteIdentifier(SourceBuilder builder, ISymbol symbol);

}
