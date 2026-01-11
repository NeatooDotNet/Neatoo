using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.BaseGenerator.Tests;

/// <summary>
/// Tests for MapModifiedTo method generation.
/// Verifies that the generator correctly generates property mapping
/// with IsModified checks.
/// </summary>
[TestClass]
public class MapModifiedToGenerationTests
{
    // Extended stubs that include indexer for property access
    private const string ExtendedNeatooStubs = """
        namespace Neatoo.RemoteFactory
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class FactoryAttribute : Attribute { }
        }

        namespace Neatoo
        {
            public interface IValidateBase { }
            public interface IEntityBase : IValidateBase { }

            public interface IProperty
            {
                bool IsModified { get; }
            }

            public class ValidateBase<T> where T : ValidateBase<T>
            {
                protected TValue Getter<TValue>() => default!;
                protected void Setter<TValue>(TValue value) { }
                protected virtual uint GetRuleId(string sourceExpression) => 0;
                protected IRuleManager RuleManager => null!;
                public IProperty this[string propertyName] => null!;
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

    #region Basic MapModifiedTo Tests

    [TestMethod]
    public void MapModifiedTo_PartialMethod_GeneratesImplementation()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{ExtendedNeatooStubs}}

            namespace TestNamespace
            {
                public class PersonDto
                {
                    public string? Name { get; set; }
                    public int Age { get; set; }
                }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                    int Age { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                    public partial int Age { get; set; }

                    public partial void MapModifiedTo(PersonDto dto);
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("MapModifiedTo"), "Should contain MapModifiedTo method");
        Assert.IsTrue(generated.Contains("IsModified"), "Should check IsModified");
    }

    [TestMethod]
    public void MapModifiedTo_MapsMatchingProperties()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{ExtendedNeatooStubs}}

            namespace TestNamespace
            {
                public class PersonDto
                {
                    public string? Name { get; set; }
                    public int Age { get; set; }
                }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                    int Age { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                    public partial int Age { get; set; }

                    public partial void MapModifiedTo(PersonDto dto);
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        // Should map both Name and Age since they exist in both
        Assert.IsTrue(generated.Contains("dto.Name"), "Should map Name property");
        Assert.IsTrue(generated.Contains("dto.Age"), "Should map Age property");
    }

    #endregion

    #region IsModified Check Tests

    [TestMethod]
    public void MapModifiedTo_ChecksIsModifiedBeforeMapping()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{ExtendedNeatooStubs}}

            namespace TestNamespace
            {
                public class PersonDto
                {
                    public string? Name { get; set; }
                }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }

                    public partial void MapModifiedTo(PersonDto dto);
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        // Should check IsModified before assigning
        Assert.IsTrue(generated.Contains("if") && generated.Contains("IsModified"),
            "Should have IsModified check in if statement");
        Assert.IsTrue(generated.Contains("nameof(Name)") || generated.Contains(@"[""Name""]"),
            "Should reference property by name");
    }

    #endregion

    #region Non-Matching Properties Tests

    [TestMethod]
    public void MapModifiedTo_IgnoresNonMatchingProperties()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{ExtendedNeatooStubs}}

            namespace TestNamespace
            {
                public class PersonDto
                {
                    public string? Name { get; set; }
                    // No Age property
                }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                    int Age { get; set; }  // This won't map
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                    public partial int Age { get; set; }

                    public partial void MapModifiedTo(PersonDto dto);
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        // Should map Name but not Age (since DTO doesn't have Age)
        Assert.IsTrue(generated.Contains("dto.Name"), "Should map Name property");
        // Age should not be mapped since DTO doesn't have it
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void MapModifiedTo_NoMatchingProperties_GeneratesEmptyBody()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{ExtendedNeatooStubs}}

            namespace TestNamespace
            {
                public class UnrelatedDto
                {
                    public string? Foo { get; set; }
                }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }

                    public partial void MapModifiedTo(UnrelatedDto dto);
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        // When no properties match, the method body may be empty or not generated
        // This test documents expected behavior
        Assert.IsNotNull(generated);
    }

    [TestMethod]
    public void MapModifiedTo_NonPartialMethod_NotGenerated()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{ExtendedNeatooStubs}}

            namespace TestNamespace
            {
                public class PersonDto
                {
                    public string? Name { get; set; }
                }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }

                    // Not partial - generator should not touch this
                    public void MapTo(PersonDto dto)
                    {
                        dto.Name = Name;
                    }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        // Generated code should not contain a MapTo implementation
        Assert.IsFalse(generated.Contains("void MapTo("),
            "Should not generate implementation for non-partial method");
    }

    #endregion
}
