using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Neatoo.BaseGenerator;
using System.Collections.Immutable;
using System.Reflection;

namespace Neatoo.BaseGenerator.Tests;

/// <summary>
/// Helper class for running source generator tests.
/// </summary>
public static class GeneratorTestHelper
{
    /// <summary>
    /// Runs the PartialBaseGenerator against the provided source code.
    /// </summary>
    /// <param name="source">The C# source code to compile and run the generator against.</param>
    /// <returns>The generator run result containing generated trees and diagnostics.</returns>
    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new PartialBaseGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        return driver.GetRunResult();
    }

    /// <summary>
    /// Runs the generator and returns both the result and output compilation.
    /// </summary>
    public static (GeneratorDriverRunResult Result, Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics)
        RunGeneratorWithCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = GetReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new PartialBaseGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        return (driver.GetRunResult(), outputCompilation, diagnostics);
    }

    /// <summary>
    /// Gets the first generated source file content, or null if none generated.
    /// </summary>
    public static string? GetGeneratedSource(GeneratorDriverRunResult result)
    {
        var generatedTree = result.GeneratedTrees.FirstOrDefault();
        return generatedTree?.GetText().ToString();
    }

    /// <summary>
    /// Gets generated source for a specific class name.
    /// </summary>
    public static string? GetGeneratedSourceForClass(GeneratorDriverRunResult result, string className)
    {
        var generatedTree = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains(className));
        return generatedTree?.GetText().ToString();
    }

    private static List<MetadataReference> GetReferences()
    {
        var references = new List<MetadataReference>();

        // Core runtime references
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        // Essential runtime assemblies
        var runtimeAssemblies = new[]
        {
            "System.Runtime.dll",
            "System.Collections.dll",
            "System.Linq.dll",
            "System.Linq.Expressions.dll",
            "System.Threading.Tasks.dll",
            "System.ComponentModel.dll",
            "System.ComponentModel.Primitives.dll",
            "System.ObjectModel.dll",
            "netstandard.dll",
        };

        foreach (var assembly in runtimeAssemblies)
        {
            var path = Path.Combine(runtimePath, assembly);
            if (File.Exists(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // Add mscorlib/System.Private.CoreLib
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        // Add System.ComponentModel.DataAnnotations for [Required] etc.
        var annotationsAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.ComponentModel.Annotations");
        if (annotationsAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(annotationsAssembly.Location));
        }

        return references;
    }

    /// <summary>
    /// Standard using statements for test source code that simulates Neatoo entities.
    /// </summary>
    public const string StandardUsings = """
        using System;
        using System.Collections.Generic;
        using System.ComponentModel.DataAnnotations;
        using System.Linq.Expressions;
        using System.Threading.Tasks;
        """;

    /// <summary>
    /// Minimal stub for Neatoo types needed for generator testing.
    /// The generator only needs to see the attribute and base class structure.
    /// </summary>
    public const string NeatooStubs = """
        namespace Neatoo.RemoteFactory
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class FactoryAttribute : Attribute { }
        }

        namespace Neatoo
        {
            public interface IValidateBase { }
            public interface IEntityBase : IValidateBase { }

            public class ValidateBase<T> where T : ValidateBase<T>
            {
                protected TValue Getter<TValue>() => default!;
                protected void Setter<TValue>(TValue value) { }
                protected virtual uint GetRuleId(string sourceExpression) => 0;
                protected IRuleManager RuleManager => null!;
            }

            public class EntityBase<T> : ValidateBase<T> where T : EntityBase<T>
            {
            }

            public interface IRuleManager
            {
                void AddRule<TTarget>(IRule<TTarget> rule) where TTarget : IValidateBase;
                void AddValidation<TTarget>(Func<TTarget, string> func, Expression<Func<TTarget, object?>> trigger);
                void AddAction<TTarget>(Action<TTarget> func, Expression<Func<TTarget, object?>> trigger1);
                void AddAction<TTarget>(Action<TTarget> func, Expression<Func<TTarget, object?>> trigger1, Expression<Func<TTarget, object?>> trigger2);
            }

            public interface IRule<T> where T : IValidateBase { }
        }
        """;
}
