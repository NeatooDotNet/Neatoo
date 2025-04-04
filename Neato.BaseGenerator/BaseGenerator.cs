using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Text;

namespace Neato.BaseGenerator
{


    [Generator(LanguageNames.CSharp)]
    public class PartialBaseGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) =>
            // Register the source output
            context.RegisterSourceOutput(context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null),
                static (ctx, source) => Execute(ctx, source!.Value.classDeclaration, source.Value.semanticModel));

        public static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax classDeclarationSyntax;

        public static (ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            try
            {
                var classDeclaration = (ClassDeclarationSyntax)context.Node;

                //var classNamedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

                //if (classNamedTypeSymbol == null)
                //{
                //    return null;
                //}

                if (classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))// && ClassOrBaseClassIsNeatooBaseClass(classNamedTypeSymbol))
                {
                    return (classDeclaration, context.SemanticModel);
                }

                return null;
            }
            catch (Exception)
            {

            }

            return null;
        }

        private static bool ClassOrBaseClassIsNeatooBaseClass(INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.Name == "Base" && namedTypeSymbol.ContainingNamespace.Name == "Neatoo")
            {
                return true;
            }
            if (namedTypeSymbol.BaseType != null)
            {
                return ClassOrBaseClassIsNeatooBaseClass(namedTypeSymbol.BaseType);
            }
            return false;
        }

        internal class PartialBaseText
        {

            public ClassDeclarationSyntax ClassDeclarationSyntax { get; set; } = null!;
            public INamedTypeSymbol ClassNamedSymbol { get; set; } = null!;
            public SemanticModel SemanticModel { get; set; } = null!;
            public InterfaceDeclarationSyntax? InterfaceDeclarationSyntax { get; set; }
            public string AccessModifier { get; set; } = "public";
            public StringBuilder PropertyDeclarations { get; set; } = new();
            public StringBuilder? InterfacePropertyDeclarations { get; set; }
            public StringBuilder MapperMethods { get; set; } = new();
        }

        private static void Execute(SourceProductionContext context, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
        {
            var messages = new List<string>();
            string source;

            try
            {
                var classNamedSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax) ?? throw new Exception($"Cannot get named symbol for {classDeclarationSyntax}");

                var usingDirectives = new List<string>() { "using Neatoo;", "using Microsoft.Extensions.DependencyInjection;" };

                var partialText = new PartialBaseText()
                {
                    ClassDeclarationSyntax = classDeclarationSyntax,
                    ClassNamedSymbol = classNamedSymbol,
                    SemanticModel = semanticModel
                };

                var targetClassName = classNamedSymbol.Name;
                // Generate the source code for the found method
                var namespaceName = FindNamespace(classDeclarationSyntax) ?? "MissingNamespace";

                try
                {
                    UsingStatements(usingDirectives, partialText, namespaceName, messages);

                    var classDeclaration = classDeclarationSyntax.ToFullString().Substring(classDeclarationSyntax.Modifiers.FullSpan.Start - classDeclarationSyntax.FullSpan.Start, classDeclarationSyntax.Identifier.FullSpan.End - classDeclarationSyntax.Modifiers.FullSpan.Start);

                    if (classDeclarationSyntax.TypeParameterList != null)
                    {
                        classDeclaration = classDeclarationSyntax.ToFullString().Substring(classDeclarationSyntax.Modifiers.FullSpan.Start - classDeclarationSyntax.FullSpan.Start, classDeclarationSyntax.TypeParameterList.FullSpan.End - classDeclarationSyntax.Modifiers.FullSpan.Start);
                    }

                    AddPartialProperties(partialText);
                    AddMapModifiedToMethod(partialText, messages);


                    var interfaceSource = "";

                    if (partialText.InterfaceDeclarationSyntax != null)
                    {
                        interfaceSource = $@"
                        public partial interface I{targetClassName} {{
                            {partialText.InterfacePropertyDeclarations}
                        }}";
                    }

                    source = $@"
                    #nullable enable

                    {WithStringBuilder(usingDirectives)}
                    /*
                    Debugging Messages:
                    {WithStringBuilder(messages)}
                    */
                    
                    namespace {namespaceName}
                    {{
                        {interfaceSource}
                        {classDeclaration} {{
                            {partialText.PropertyDeclarations}
{partialText.MapperMethods}
                        }}

                    }}
                    
                    ";
                    source = source.Replace("[, ", "[");
                    source = source.Replace("(, ", "(");
                    source = source.Replace(", )", ")");
                    source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
                }
                catch (Exception ex)
                {
                    source = @$"/* Error: {ex.GetType().FullName} {ex.Message} */";
                }

                context.AddSource($"{namespaceName}.{targetClassName}.g.cs", source);
            }
            catch (Exception ex)
            {
                source = $"// Error: {ex.Message}";
                context.AddSource($"Error.{classDeclarationSyntax.Identifier.Text}.g.cs", source);

            }
        }

        internal static void AddPartialProperties(PartialBaseText partialBaseText)
        {

            var interfaceSyntax = partialBaseText.ClassNamedSymbol.Interfaces.FirstOrDefault(i => i.Name == $"I{partialBaseText.ClassNamedSymbol.Name}");
            List<string> interfaceProperties = [];

            if (interfaceSyntax != null)
            {
                var interfaceDeclarationSyntax = partialBaseText.ClassNamedSymbol.Interfaces.First(i => i.Name == $"I{partialBaseText.ClassNamedSymbol.Name}").DeclaringSyntaxReferences.First().GetSyntax() as InterfaceDeclarationSyntax;

                if (interfaceDeclarationSyntax != null && interfaceDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    partialBaseText.InterfaceDeclarationSyntax = interfaceDeclarationSyntax;
                    partialBaseText.InterfacePropertyDeclarations = new StringBuilder();
                    interfaceProperties = interfaceDeclarationSyntax.Members.OfType<PropertyDeclarationSyntax>().Select(p => p.Identifier.Text).ToList();

                }
            }

            var properties = partialBaseText.ClassDeclarationSyntax.Members.OfType<PropertyDeclarationSyntax>()
                                .Where(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                                .ToDictionary(p => p.Identifier.Text, p => p);

            foreach (var propertyKVP in properties)
            {
                var property = propertyKVP.Value;
                var accessibility = property.Modifiers.First().ToString();
                var propertyType = property.Type.ToString();
                var propertyName = property.Identifier.Text;

                partialBaseText.PropertyDeclarations.AppendLine($"{accessibility} partial {propertyType} {propertyName} {{ get => Getter<{propertyType}>();  set=>Setter(value); }}");

                if (partialBaseText.InterfacePropertyDeclarations != null &&
                        !interfaceProperties.Contains(propertyName))
                {
                    partialBaseText.InterfacePropertyDeclarations.AppendLine($"{propertyType} {propertyName} {{ get; set; }}");
                }
            }
        }

        internal static void AddMapModifiedToMethod(PartialBaseText partialBaseText, List<string> messages)
        {
            var classProperties = GetPropertiesRecursive(partialBaseText.ClassNamedSymbol);
            var classMethods = partialBaseText.ClassNamedSymbol.GetMembers().OfType<IMethodSymbol>().ToList() ?? [];

            foreach (var classMethod in classMethods)
            {
                var methodBuilder = new StringBuilder();
                messages.Add($"Method {classMethod.Name}");

                if (classMethod.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is MethodDeclarationSyntax classSyntax)
                {
                    messages.Add($"MethodDeclarationSyntax {classMethod.Name}");

                    var mapTo = classSyntax.Identifier.Text == "MapModifiedTo";
                    if (mapTo && classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        messages.Add($"Method {classMethod.Name} is a Match");

                        methodBuilder.AppendLine($"{classSyntax.ToFullString().Trim().TrimEnd(';')}");
                        methodBuilder.AppendLine("{");

                        var parameterSymbol = classMethod.Parameters.SingleOrDefault();
                        if (parameterSymbol == null)
                        {
                            messages.Add($"Single parameter not found for {classMethod.Name}");
                            break;
                        }
                        else
                        {
                            messages.Add($"Parameter {parameterSymbol.Name} {parameterSymbol.Type} found for {classMethod.Name}");
                        }

                        var parameterSyntax = classSyntax.ParameterList.Parameters.First();
                        var parameterIdentifier = parameterSyntax.Identifier.Text;

                        var parameterProperties = GetPropertiesRecursive(parameterSymbol?.Type as INamedTypeSymbol);
                        parameterProperties.ForEach(p =>
                        {
                            messages.Add($"Parameter Property {p.Name} {p.Type} found");
                        });

                        var propertiesMatched = false;

                        foreach (var parameterProperty in parameterProperties)
                        {
                            var classProperty = classProperties.FirstOrDefault(p => p.Name == parameterProperty.Name);
                            if (classProperty != null)
                            {
                                propertiesMatched = true;
                                var nullException = "";

                                if (classProperty.NullableAnnotation == NullableAnnotation.Annotated
                                    && parameterProperty.NullableAnnotation != NullableAnnotation.Annotated)
                                {
                                    nullException = $"?? throw new NullReferenceException(\"{partialBaseText.ClassNamedSymbol?.ToDisplayString()}.{classProperty.Name}\")";
                                }

                                methodBuilder.AppendLine($"if (this[nameof({classProperty.Name})].IsModified){{");
                                methodBuilder.AppendLine($"{parameterIdentifier}.{parameterProperty.Name} = this.{classProperty.Name}{nullException};");
                                methodBuilder.AppendLine("}");
                            }
                        }

                        methodBuilder.AppendLine("}");

                        if (propertiesMatched)
                        {
                            partialBaseText.MapperMethods.Append(methodBuilder);
                        }
                    }
                }
            }
        }
        public static string? FindNamespace(SyntaxNode syntaxNode)
        {
            if (syntaxNode.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
            {
                return namespaceDeclarationSyntax.Name.ToString();
            }
            else if (syntaxNode.Parent is FileScopedNamespaceDeclarationSyntax parentClassDeclarationSyntax)
            {
                return parentClassDeclarationSyntax.Name.ToString();
            }
            else if (syntaxNode.Parent != null)
            {
                return FindNamespace(syntaxNode.Parent);
            }
            else
            {
                return null;
            }
        }

        public static string WithStringBuilder(IEnumerable<string> strings)
        {
            var sb = new StringBuilder();
            foreach (var s in strings)
            {
                sb.AppendLine(s);
            }
            return sb.ToString();
        }

        public static List<IPropertySymbol> GetPropertiesRecursive(INamedTypeSymbol? classNamedSymbol)
        {
            var properties = classNamedSymbol?.GetMembers().OfType<IPropertySymbol>().ToList() ?? [];
            if (classNamedSymbol?.BaseType != null)
            {
                properties.AddRange(GetPropertiesRecursive(classNamedSymbol.BaseType));
            }
            return properties;
        }

        internal static void UsingStatements(List<string> usingDirectives, PartialBaseText partialBaseText, string namespaceName, List<string> messages)
        {
            var parentClassDeclaration = partialBaseText.ClassDeclarationSyntax.Parent as ClassDeclarationSyntax;
            var parentClassUsingText = "";

            while (parentClassDeclaration != null)
            {
                messages.Add("Parent class: " + parentClassDeclaration.Identifier.Text);
                parentClassUsingText = $"{parentClassDeclaration.Identifier.Text}.{parentClassUsingText}";
                parentClassDeclaration = parentClassDeclaration.Parent as ClassDeclarationSyntax;
            }

            if (!string.IsNullOrEmpty(parentClassUsingText))
            {
                usingDirectives.Add($"using static {namespaceName}.{parentClassUsingText.TrimEnd('.')};");
            }

            var recurseClassDeclaration = partialBaseText.ClassDeclarationSyntax;

            while (recurseClassDeclaration != null)
            {
                var compilationUnitSyntax = recurseClassDeclaration.SyntaxTree.GetCompilationUnitRoot();
                foreach (var using_ in compilationUnitSyntax.Usings)
                {
                    if (!usingDirectives.Contains(using_.ToString()))
                    {
                        usingDirectives.Add(using_.ToString());
                    }
                }
                recurseClassDeclaration = GetBaseClassDeclarationSyntax(partialBaseText.SemanticModel, recurseClassDeclaration, messages);
            }
        }

        private static ClassDeclarationSyntax? GetBaseClassDeclarationSyntax(SemanticModel semanticModel, ClassDeclarationSyntax classDeclaration, List<string> messages)
        {
            try
            {
                var correctSemanticModel = semanticModel.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);

                var classSymbol = correctSemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

                if (classSymbol?.BaseType == null)
                {
                    return null;
                }

                var baseTypeSymbol = classSymbol.BaseType;
                var baseTypeSyntaxReference = baseTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault();

                if (baseTypeSyntaxReference == null)
                {
                    return null;
                }

                var baseTypeSyntaxNode = baseTypeSyntaxReference.GetSyntax() as ClassDeclarationSyntax;

                return baseTypeSyntaxNode;
            }
            catch (Exception ex)
            {
                messages.Add(ex.Message);
                return null;
            }
        }
    }
}