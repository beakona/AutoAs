using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BeaKona.AutoAsGenerator;

internal sealed class CSharpCodeTextWriter : ICodeTextWriter
{
    public CSharpCodeTextWriter(GeneratorExecutionContext context, Compilation compilation)
    {
        this.Context = context;
        this.Compilation = compilation;
    }

    public GeneratorExecutionContext Context { get; }
    public Compilation Compilation { get; }

    public void WriteTypeReference(SourceBuilder builder, ITypeSymbol type, ScopeInfo scope)
    {
        if (scope.TryGetAlias(type, out string? typeName))
        {
            if (typeName != null)
            {
                builder.Append(typeName);
            }
        }
        else
        {
            bool processed = false;
            if (type.SpecialType != SpecialType.None)
            {
                processed = true;
                switch (type.SpecialType)
                {
                    default: processed = false; break;
                    case SpecialType.System_Object: builder.Append("object"); break;
                    case SpecialType.System_Void: builder.Append("void"); break;
                    case SpecialType.System_Boolean: builder.Append("bool"); break;
                    case SpecialType.System_Char: builder.Append("char"); break;
                    case SpecialType.System_SByte: builder.Append("sbyte"); break;
                    case SpecialType.System_Byte: builder.Append("byte"); break;
                    case SpecialType.System_Int16: builder.Append("short"); break;
                    case SpecialType.System_UInt16: builder.Append("ushort"); break;
                    case SpecialType.System_Int32: builder.Append("int"); break;
                    case SpecialType.System_UInt32: builder.Append("uint"); break;
                    case SpecialType.System_Int64: builder.Append("long"); break;
                    case SpecialType.System_UInt64: builder.Append("ulong"); break;
                    case SpecialType.System_Decimal: builder.Append("decimal"); break;
                    case SpecialType.System_Single: builder.Append("float"); break;
                    case SpecialType.System_Double: builder.Append("double"); break;
                    //case SpecialType.System_Half: builder.Append("half"); break;
                    case SpecialType.System_String: builder.Append("string"); break;
                }
            }

            if (processed == false)
            {
                if (type is IArrayTypeSymbol array)
                {
                    this.WriteTypeReference(builder, array.ElementType, scope);
                    builder.Append('[');
                    for (int i = 1; i < array.Rank; i++)
                    {
                        builder.Append(',');
                    }
                    builder.Append(']');
                }
                else
                {
                    static bool IsTupleWithAliases(INamedTypeSymbol tuple)
                    {
                        return tuple.TupleElements.Any(i => i.CorrespondingTupleField != null && i.Equals(i.CorrespondingTupleField, SymbolEqualityComparer.Default) == false);
                    }

                    if (type.IsTupleType && type is INamedTypeSymbol tupleType && IsTupleWithAliases(tupleType))
                    {
                        builder.Append('(');
                        bool first = true;
                        foreach (IFieldSymbol field in tupleType.TupleElements)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                builder.Append(", ");
                            }
                            this.WriteTypeReference(builder, field.Type, scope);
                            builder.Append(' ');
                            this.WriteIdentifier(builder, field);
                        }
                        builder.Append(')');
                    }
                    else if (type is INamedTypeSymbol nt && SemanticFacts.IsNullableT(this.Compilation, nt))
                    {
                        this.WriteTypeReference(builder, nt.TypeArguments[0], scope);
                    }
                    else
                    {
                        if (type is ITypeParameterSymbol == false)
                        {
                            if (type.Equals(scope.Type, SymbolEqualityComparer.Default) == false)
                            {
                                string? alias = SemanticFacts.ResolveAssemblyAlias(this.Compilation, type.ContainingAssembly);
                                ISymbol[] symbols;
                                if (alias == null)
                                {
                                    symbols = SemanticFacts.GetRelativeSymbols(type, scope.Type);
                                }
                                else
                                {
                                    symbols = SemanticFacts.GetContainingSymbols(type, false);
                                    builder.Append(alias);
                                    builder.Append("::");
                                    builder.RegisterAlias(alias);
                                }

                                foreach (ISymbol symbol in symbols)
                                {
                                    this.WriteIdentifier(builder, symbol);

                                    if (symbol is INamedTypeSymbol snt && snt.IsGenericType)
                                    {
                                        builder.Append('<');
                                        this.WriteTypeArgumentsDefinition(builder, snt.TypeArguments, scope);
                                        builder.Append('>');
                                    }

                                    builder.Append('.');
                                }
                            }
                        }

                        this.WriteIdentifier(builder, type);

                        {
                            if (type is INamedTypeSymbol tnt && tnt.IsGenericType)
                            {
                                builder.Append('<');
                                this.WriteTypeArgumentsDefinition(builder, tnt.TypeArguments, scope);
                                builder.Append('>');
                            }
                        }
                    }
                }
            }
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            builder.Append('?');
        }
    }

    public void WriteTypeArgumentsDefinition(SourceBuilder builder, IEnumerable<ITypeSymbol> typeArguments, ScopeInfo scope)
    {
        bool first = true;
        foreach (ITypeSymbol t in typeArguments)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                builder.Append(", ");
            }
            this.WriteTypeReference(builder, t, scope);
        }
    }

    public void WriteTypeDeclarationBeginning(SourceBuilder builder, INamedTypeSymbol type, ScopeInfo scope)
    {
        builder.Append("partial");
        builder.Append(' ');
        if (type.TypeKind == TypeKind.Class)
        {
            bool isRecord = type.DeclaringSyntaxReferences.Any(i => i.GetSyntax() is RecordDeclarationSyntax);
            builder.Append(isRecord ? "record" : "class");
        }
        else if (type.TypeKind == TypeKind.Struct)
        {
            builder.Append("struct");
        }
        else if (type.TypeKind == TypeKind.Interface)
        {
            builder.Append("interface");
        }
        else
        {
            throw new NotSupportedException(nameof(WriteTypeDeclarationBeginning));
        }
        builder.Append(' ');
        this.WriteTypeReference(builder, type, scope);
    }

    public bool WriteNamespaceBeginning(SourceBuilder builder, INamespaceSymbol @namespace)
    {
        if (@namespace != null)
        {
            INamespaceSymbol[] containingNamespaces = @namespace.GetNamespaceElements();

            if (containingNamespaces.Length > 0)
            {
                builder.AppendIndentation();
                builder.Append("namespace");
                builder.Append(' ');
                builder.AppendLine(string.Join(".", containingNamespaces.Select(i => GetSourceIdentifier(i))));
                builder.AppendIndentation();
                builder.AppendLine('{');
                builder.IncrementIndentation();

                return true;
            }
        }

        return false;
    }

    public void WriteHolderReference(SourceBuilder builder, ISymbol member, ScopeInfo scope)
    {
        if (member.IsStatic)
        {
            this.WriteTypeReference(builder, member.ContainingType, scope);
        }
        else
        {
            builder.Append("this");
        }
    }

    public void WriteIdentifier(SourceBuilder builder, ISymbol symbol)
    {
        builder.Append(this.GetSourceIdentifier(symbol));
    }

    #region helper members

    private string GetSourceIdentifier(ISymbol symbol)
    {
        if (symbol is IPropertySymbol propertySymbol && propertySymbol.IsIndexer)
        {
            return "this";
        }
        else if (symbol is INamespaceSymbol ns)
        {
            return ns.Name;
            //return string.Join("+", ns.ConstituentNamespaces.Select(i => i.Name));
            //return $"<{@namespace.Name};{symbol}>" + this.GetSourceIdentifier(@namespace.Name);
        }
        else if (symbol.DeclaringSyntaxReferences.Length == 0)
        {
            return symbol.Name;
        }
        else
        {
            foreach (SyntaxReference syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                SyntaxNode syntax = syntaxReference.GetSyntax();
                if (syntax is BaseTypeDeclarationSyntax type)
                {
                    return this.GetSourceIdentifier(type.Identifier);
                }
                else if (syntax is MethodDeclarationSyntax method)
                {
                    return this.GetSourceIdentifier(method.Identifier);
                }
                else if (syntax is ParameterSyntax parameter)
                {
                    return this.GetSourceIdentifier(parameter.Identifier);
                }
                else if (syntax is VariableDeclaratorSyntax variableDeclarator)
                {
                    return this.GetSourceIdentifier(variableDeclarator.Identifier);
                }
                else if (syntax is VariableDeclarationSyntax variableDeclaration)
                {
                    if (variableDeclaration.Variables.Any(i => i.Identifier.IsVerbatimIdentifier()))
                    {
                        return "@" + symbol;
                    }
                    else
                    {
                        return symbol.ToString();
                    }
                }
                else if (syntax is BaseFieldDeclarationSyntax field)
                {
                    if (field.Declaration.Variables.Any(i => i.Identifier.IsVerbatimIdentifier()))
                    {
                        return "@" + symbol;
                    }
                    else
                    {
                        return symbol.ToString();
                    }
                }
                else if (syntax is PropertyDeclarationSyntax property)
                {
                    return this.GetSourceIdentifier(property.Identifier);
                }
                else if (syntax is IndexerDeclarationSyntax)
                {
                    throw new InvalidOperationException("trying to resolve indexer name");
                }
                else if (syntax is EventDeclarationSyntax @event)
                {
                    return this.GetSourceIdentifier(@event.Identifier);
                }
                else if (syntax is TypeParameterSyntax typeParameter)
                {
                    return this.GetSourceIdentifier(typeParameter.Identifier);
                }
                else if (syntax is TupleTypeSyntax)
                {
                    return symbol.Name;
                }
                else if (syntax is TupleElementSyntax tupleElement)
                {
                    return this.GetSourceIdentifier(tupleElement.Identifier);
                }
                else if (syntax is NamespaceDeclarationSyntax @namespace)
                {
                    throw new NotSupportedException(syntax.GetType().ToString());
                }
                else
                {
                    throw new NotSupportedException(syntax.GetType().ToString());
                }
            }

            throw new NotSupportedException();
        }
    }

    private string GetSourceIdentifier(SyntaxToken identifier)
    {
        if (identifier.IsVerbatimIdentifier())
        {
            return "@" + identifier.ValueText;
        }
        else
        {
            return identifier.ValueText;
        }
    }

    private string GetSourceIdentifier(NameSyntax name)
    {
        if (name is SimpleNameSyntax simpleName)
        {
            return this.GetSourceIdentifier(simpleName.Identifier);
        }
        else if (name is QualifiedNameSyntax qualifiedName)
        {
            string left = this.GetSourceIdentifier(qualifiedName.Left);
            string right = this.GetSourceIdentifier(qualifiedName.Right);
            if (string.IsNullOrEmpty(left))
            {
                if (string.IsNullOrEmpty(right))
                {
                    throw new NotSupportedException("both are null_or_empty.");
                }
                else
                {
                    return right;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(right))
                {
                    return left;
                }
                else
                {
                    return left + "." + right;
                }
            }
        }
        else
        {
            throw new NotSupportedException(name.GetType().ToString());
        }
    }

    #endregion
}
