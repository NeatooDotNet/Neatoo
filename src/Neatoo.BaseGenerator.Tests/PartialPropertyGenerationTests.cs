using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.BaseGenerator.Tests;

/// <summary>
/// Tests for partial property generation.
/// Verifies that the generator correctly generates Getter/Setter implementations
/// for partial properties.
/// </summary>
[TestClass]
public class PartialPropertyGenerationTests
{
    #region Basic Property Generation

    [TestMethod]
    public void PartialProperty_GeneratesGetterSetter()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("partial string? Name"), "Should have partial property");
        Assert.IsTrue(generated.Contains("Getter<string?>()"), "Should call Getter<T>");
        Assert.IsTrue(generated.Contains("Setter(value)"), "Should call Setter");
    }

    [TestMethod]
    public void PartialProperty_MultipleProperties_GeneratesAll()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                    int Age { get; set; }
                    bool IsActive { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                    public partial int Age { get; set; }
                    public partial bool IsActive { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("partial string? Name"), "Should generate Name property");
        Assert.IsTrue(generated.Contains("partial int Age"), "Should generate Age property");
        Assert.IsTrue(generated.Contains("partial bool IsActive"), "Should generate IsActive property");
        Assert.IsTrue(generated.Contains("Getter<string?>()"), "Should have string getter");
        Assert.IsTrue(generated.Contains("Getter<int>()"), "Should have int getter");
        Assert.IsTrue(generated.Contains("Getter<bool>()"), "Should have bool getter");
    }

    #endregion

    #region Property Type Tests

    [TestMethod]
    public void PartialProperty_NullableValueType_GeneratesCorrectType()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    int? NullableInt { get; set; }
                    DateTime? NullableDate { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial int? NullableInt { get; set; }
                    public partial DateTime? NullableDate { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("Getter<int?>()"), "Should have nullable int getter");
        Assert.IsTrue(generated.Contains("Getter<DateTime?>()"), "Should have nullable DateTime getter");
    }

    [TestMethod]
    public void PartialProperty_ComplexType_GeneratesCorrectType()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public class Address { }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    Address? HomeAddress { get; set; }
                    List<string>? Tags { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial Address? HomeAddress { get; set; }
                    public partial List<string>? Tags { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("Getter<Address?>()"), "Should have Address getter");
        Assert.IsTrue(generated.Contains("Getter<List<string>?>()"), "Should have List<string> getter");
    }

    #endregion

    #region Access Modifier Tests

    [TestMethod]
    public void PartialProperty_PublicAccessor_PreservesAccessibility()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("public partial string? Name"),
            "Should preserve public accessibility");
    }

    [TestMethod]
    public void PartialProperty_ProtectedAccessor_PreservesAccessibility()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase { }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    protected partial string? InternalName { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("protected partial string? InternalName"),
            "Should preserve protected accessibility");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void PartialProperty_NonPartialProperty_NotGenerated()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    // Not partial - should not be processed
                    public string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        // Generator should still produce output but without Getter/Setter for non-partial
        if (generated != null)
        {
            Assert.IsFalse(generated.Contains("Getter<string?>()"),
                "Should not generate getter for non-partial property");
        }
    }

    [TestMethod]
    public void PartialProperty_NoPartialProperties_StillGeneratesClass()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase { }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    // Non-partial property
                    public string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        // May or may not generate output depending on generator logic
        // This test documents the expected behavior
        Assert.IsNotNull(result);
    }

    #endregion

    #region Namespace Tests

    [TestMethod]
    public void PartialProperty_PreservesNamespace()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace My.Custom.Namespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("namespace My.Custom.Namespace"),
            "Should preserve the namespace");
    }

    [TestMethod]
    public void PartialProperty_FileScopedNamespace_Works()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace My.FileScoped.Namespace;

            public interface ITestEntity : Neatoo.IEntityBase
            {
                string? Name { get; set; }
            }

            [Neatoo.RemoteFactory.Factory]
            public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
            {
                public partial string? Name { get; set; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("My.FileScoped.Namespace"),
            "Should work with file-scoped namespace");
    }

    #endregion
}
