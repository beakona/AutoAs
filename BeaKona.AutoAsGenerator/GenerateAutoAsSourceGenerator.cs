using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace BeaKona.AutoAsGenerator;

[Generator]
public sealed class GenerateAutoAsSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created for each generation pass
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        Compilation compilation = context.Compilation;
        if (compilation is CSharpCompilation)
        {
            //retrieve the populated receiver
            if (context.SyntaxReceiver is SyntaxReceiver receiver)
            {
                // get newly bound attribute
                if (compilation.GetTypeByMetadataName(typeof(GenerateAutoAsAttribute).FullName) is INamedTypeSymbol generateAutoAsAttributeSymbol)
                {
                    GenerateAutoAsAttribute? GetGenerateAutoAsAttribute(INamedTypeSymbol type)
                    {
                        foreach (AttributeData attribute in type.GetAttributes())
                        {
                            if (attribute.AttributeClass != null && attribute.AttributeClass.Equals(generateAutoAsAttributeSymbol, SymbolEqualityComparer.Default))
                            {
                                var result = new GenerateAutoAsAttribute();

                                foreach (KeyValuePair<string, TypedConstant> arg in attribute.NamedArguments)
                                {
                                    switch (arg.Key)
                                    {
                                        case nameof(GenerateAutoAsAttribute.EntireInterfaceHierarchy):
                                            {
                                                if (arg.Value.Value is bool b)
                                                {
                                                    result.EntireInterfaceHierarchy = b;
                                                }
                                            }
                                            break;
                                        case nameof(GenerateAutoAsAttribute.SkipSystemInterfaces):
                                            {
                                                if (arg.Value.Value is bool b)
                                                {
                                                    result.SkipSystemInterfaces = b;
                                                }
                                            }
                                            break;
                                    }
                                }

                                return result;
                            }
                        }

                        return null;
                    }

                    var types = new List<INamedTypeSymbol>();

                    foreach (TypeDeclarationSyntax candidate in receiver.Candidates)
                    {
                        SemanticModel model = compilation.GetSemanticModel(candidate.SyntaxTree);

                        if (model.GetDeclaredSymbol(candidate) is INamedTypeSymbol type)
                        {
                            if (GetGenerateAutoAsAttribute(type) is GenerateAutoAsAttribute attribute)
                            {
                                if (type.IsPartial() == false)
                                {
                                    Helpers.ReportDiagnostic(context, "BKAA01", nameof(AutoAsResource.AA01_title), nameof(AutoAsResource.AA01_message), nameof(AutoAsResource.AA01_description), DiagnosticSeverity.Error, type,
                                        type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                                    continue;
                                }

                                if (type.IsStatic)
                                {
                                    Helpers.ReportDiagnostic(context, "BKAA02", nameof(AutoAsResource.AA02_title), nameof(AutoAsResource.AA02_message), nameof(AutoAsResource.AA02_description), DiagnosticSeverity.Error, type,
                                        type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                                    continue;
                                }

                                if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
                                {
                                    Helpers.ReportDiagnostic(context, "BKAA03", nameof(AutoAsResource.AA03_title), nameof(AutoAsResource.AA03_message), nameof(AutoAsResource.AA03_description), DiagnosticSeverity.Error, type);
                                    continue;
                                }

                                try
                                {
                                    string? code = GenerateAutoAsSourceGenerator.ProcessClass(context, compilation, type, attribute);
                                    if (code != null)
                                    {
                                        string name = type.Arity > 0 ? $"{type.Name}_{type.Arity}" : type.Name;
#if PEEK_1
                                        GeneratePreview(context, name, code);
#else
                                        context.AddSource($"{name}_GenerateAutoAs.g.cs", SourceText.From(code, Encoding.UTF8));
#endif
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Helpers.ReportDiagnostic(context, "BKAA09", nameof(AutoAsResource.AA04_title), nameof(AutoAsResource.AA04_message), nameof(AutoAsResource.AA04_description), DiagnosticSeverity.Error, type,
                                        ex.ToString().Replace("\r", "").Replace("\n", ""));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

#if PEEK_1
    private static void GeneratePreview(GeneratorExecutionContext context, string name, string code)
    {
        var output = new StringBuilder();
        output.AppendLine("namespace BeaKona.Output {");
        output.AppendLine($"public static class Debug_{name}");
        output.AppendLine("{");
        output.AppendLine($"public static readonly string Info = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(\"{Convert.ToBase64String(Encoding.UTF8.GetBytes(code ?? ""))}\"));");
        output.AppendLine("}");
        output.AppendLine("}");
        context.AddSource($"Output_Debug_{name}.g.cs", SourceText.From(output.ToString(), Encoding.UTF8));
    }
#endif

    private static string? ProcessClass(GeneratorExecutionContext context, Compilation compilation, INamedTypeSymbol type, GenerateAutoAsAttribute attribute)
    {
        var scope = new ScopeInfo(type);

        List<INamedTypeSymbol> interfaces;

        if (attribute.EntireInterfaceHierarchy)
        {
            interfaces = type.AllInterfaces.Where(i => i.CanBeReferencedByName).ToList();
        }
        else
        {
            interfaces = [];

            //interface list is small, we will not use HashSet here
            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var @interface in t.Interfaces)
                {
                    if (@interface.CanBeReferencedByName && interfaces.Contains(@interface) == false)
                    {
                        interfaces.Add(@interface);
                    }
                }
            }
        }

        if (attribute.SkipSystemInterfaces)
        {
            for (int i = 0; i < interfaces.Count; i++)
            {
                var @interface = interfaces[i];

                if (@interface.ContainingNamespace is INamespaceSymbol @namespace)
                {
                    if (@namespace.FirstNonGlobalNamespace() is INamespaceSymbol first)
                    {
                        if (first.Name.Equals("System", StringComparison.InvariantCulture) || first.Name.StartsWith("System.", StringComparison.InvariantCulture))
                        {
                            interfaces.RemoveAt(i--);
                        }
                    }
                }
            }
        }

        var options = SourceBuilderOptions.Load(context, null);
        var builder = new SourceBuilder(options);

        ICodeTextWriter writer = new CSharpCodeTextWriter(context, compilation);
        bool anyReasonToEmitSourceFile = false;
        bool error = false;

        builder.AppendLine("// <auto-generated />");
        //bool isNullable = compilation.Options.NullableContextOptions == NullableContextOptions.Enable;
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        writer.WriteNamespaceBeginning(builder, type.ContainingNamespace);

        List<INamedTypeSymbol> containingTypes = [];
        for (INamedTypeSymbol? ct = type.ContainingType; ct != null; ct = ct.ContainingType)
        {
            containingTypes.Insert(0, ct);
        }

        foreach (INamedTypeSymbol ct in containingTypes)
        {
            builder.AppendIndentation();
            writer.WriteTypeDeclarationBeginning(builder, ct, new ScopeInfo(ct));
            builder.AppendLine();
            builder.AppendIndentation();
            builder.AppendLine('{');
            builder.IncrementIndentation();
        }

        builder.AppendIndentation();
        writer.WriteTypeDeclarationBeginning(builder, type, scope);
        builder.AppendLine();
        builder.AppendIndentation();
        builder.AppendLine('{');
        builder.IncrementIndentation();

        if (interfaces.Any())
        {
            var existingMethods = type.GetMembers().OfType<IMethodSymbol>().Where(i => i.MethodKind != MethodKind.ExplicitInterfaceImplementation).Select(i => i.Name).ToHashSet();

            string GetModifiedMethodName(string methodName, int? index)
            {
                if (methodName.StartsWith("I", StringComparison.InvariantCulture) && methodName.Length > 1 && char.IsUpper(methodName[1]))
                {
                    methodName = methodName.Substring(1);
                }
                if (index.HasValue)
                {
                    methodName = $"{methodName}_{index.Value}";
                }
                return "As" + methodName;
            }

            void WriteMethod(INamedTypeSymbol @interface, int? index)
            {
                var desiredMethodName = GetModifiedMethodName(@interface.Name, index);
                if (existingMethods.Add(desiredMethodName))
                {
                    anyReasonToEmitSourceFile = true;

                    builder.AppendIndentation();
                    builder.Append(@interface.DeclaredAccessibility == Accessibility.Internal ? "internal" : "public");
                    builder.Append(' ');
                    writer.WriteTypeReference(builder, @interface, scope);
                    builder.Append(' ');
                    builder.Append(desiredMethodName);
                    builder.Append("() => ");
                    writer.WriteHolderReference(builder, type, scope);
                    builder.Append(';');
                    builder.AppendLine();
                }
            }

            foreach (var group in interfaces.GroupBy(i => i.Name))
            {
                if (group.Count() == 1)
                {
                    WriteMethod(group.First(), null);
                }
                else
                {
                    int index = 0;

                    foreach (var @interface in group.OrderBy(i => i.TypeArguments.Length).ThenBy(i => i.Name))
                    {
                        WriteMethod(@interface, index++);
                    }
                }
            }
        }

        builder.DecrementIndentation();
        builder.AppendIndentation();
        builder.Append('}');

        for (int i = 0; i < containingTypes.Count; i++)
        {
            builder.AppendLine();
            builder.DecrementIndentation();
            builder.AppendIndentation();
            builder.Append('}');
        }

        if (type.ContainingNamespace != null && type.ContainingNamespace.ConstituentNamespaces.Length > 0)
        {
            builder.AppendLine();
            builder.DecrementIndentation();
            builder.AppendIndentation();
            builder.Append('}');
        }

        if (builder.Options.InsertFinalNewLine)
        {
            builder.AppendLine();
        }

        return error == false && anyReasonToEmitSourceFile ? builder.ToString() : null;
    }

    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    private sealed class SyntaxReceiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> Candidates { get; } = [];

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // any type with at least one attribute is a candidate for source generation
            if (syntaxNode is TypeDeclarationSyntax typeDeclarationSyntax && typeDeclarationSyntax.AttributeLists.Count > 0)
            {
                this.Candidates.Add(typeDeclarationSyntax);
            }
        }
    }
}
