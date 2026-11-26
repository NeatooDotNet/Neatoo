using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.BaseGenerator.Tests;

/// <summary>
/// The generated code compiles clean in an application with nullable enabled. The property's storage,
/// <c>IValidateProperty&lt;T&gt;.Value</c>, is <c>T?</c>; the generated getter of a non-nullable
/// reference partial returns it as <c>T</c>, and an application cannot fix a warning in generated code.
/// </summary>
[TestClass]
public class NullableGetterGenerationTests
{
    /// <summary>
    /// The members of Neatoo the generated code binds to, with the nullability the real ones declare.
    /// </summary>
    private const string CompilableStubs = """
        namespace Microsoft.Extensions.DependencyInjection { }

        namespace Neatoo.RemoteFactory
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class FactoryAttribute : Attribute { }
        }

        namespace Neatoo
        {
            public interface IValidateBase { }
            public interface IEntityBase : IValidateBase { }

            public interface IValidateProperty
            {
                Task SetValue(object? newValue);
                Task SetPrivateValue(object? newValue, bool quietly = false);
                void LoadValue(object? value);
            }

            public interface IValidateProperty<T> : IValidateProperty
            {
                T? Value { get; set; }
            }

            public interface IValidatePropertyManager
            {
                IValidateProperty? this[string propertyName] { get; }
                void Register(IValidateProperty property);
            }

            public interface IPropertyFactory<TOwner>
            {
                IValidateProperty<P> Create<P>(TOwner owner, string propertyName);
            }

            public class ValidateBase<T> : IValidateBase where T : ValidateBase<T>
            {
                protected IValidatePropertyManager PropertyManager => null!;
                protected virtual uint GetRuleId(string sourceExpression) => 0;
                protected virtual void InitializePropertyBackingFields(IPropertyFactory<T> factory) { }
            }

            public class EntityBase<T> : ValidateBase<T>, IEntityBase where T : EntityBase<T>
            {
            }
        }
        """;

    private static List<Diagnostic> CompileGenerated(string members)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            {{CompilableStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    {{members}}
                }
            }
            """;

        var (result, output, _) = GeneratorTestHelper.RunGeneratorWithCompilation(source);
        Assert.IsNotNull(GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity"), "Should generate code for TestEntity");

        // An application's settings: nullable enabled, and every warning reported.
        var options = (CSharpCompilationOptions)output.Options;
        var compilation = output.WithOptions(options.WithNullableContextOptions(NullableContextOptions.Enable));

        return compilation.GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToList();
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics)
        => string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));

    [TestMethod]
    public void NonNullableReferencePartials_CompileWithNoWarnings()
    {
        var diagnostics = CompileGenerated("""
            public partial string Name { get; set; }
            public partial IReadOnlyList<Guid> BoxIds { get; private set; }
            public partial List<int> Scores { get; protected set; }
            public partial string Code { get; }
            """);

        Assert.AreEqual(0, diagnostics.Count, Describe(diagnostics));
    }

    [TestMethod]
    public void NullableReferenceAndValueTypePartials_CompileWithNoWarnings()
    {
        var diagnostics = CompileGenerated("""
            public partial string? Note { get; set; }
            public partial int Count { get; set; }
            public partial Guid Id { get; private set; }
            public partial DateTime? ClosedOn { get; set; }
            public partial long? Size { get; }
            """);

        Assert.AreEqual(0, diagnostics.Count, Describe(diagnostics));
    }
}
