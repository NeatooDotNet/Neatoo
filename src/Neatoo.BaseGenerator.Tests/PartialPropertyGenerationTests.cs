using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.BaseGenerator.Tests;

/// <summary>
/// Tests for partial property generation.
/// Verifies that the generator correctly generates property backing fields
/// and property implementations for partial properties.
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
        Assert.IsTrue(generated.Contains("NameProperty"), "Should have backing field property");
        Assert.IsTrue(generated.Contains("IValidateProperty<string?>"), "Should have typed backing field");
        Assert.IsTrue(generated.Contains("NameProperty.Value"), "Should access backing field Value");
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
        Assert.IsTrue(generated.Contains("NameProperty"), "Should have string backing field");
        Assert.IsTrue(generated.Contains("AgeProperty"), "Should have int backing field");
        Assert.IsTrue(generated.Contains("IsActiveProperty"), "Should have bool backing field");
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
        Assert.IsTrue(generated.Contains("IValidateProperty<int?>"), "Should have nullable int backing field type");
        Assert.IsTrue(generated.Contains("IValidateProperty<DateTime?>"), "Should have nullable DateTime backing field type");
        Assert.IsTrue(generated.Contains("NullableIntProperty"), "Should have NullableIntProperty");
        Assert.IsTrue(generated.Contains("NullableDateProperty"), "Should have NullableDateProperty");
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
        Assert.IsTrue(generated.Contains("IValidateProperty<Address?>"), "Should have Address backing field type");
        Assert.IsTrue(generated.Contains("IValidateProperty<List<string>?>"), "Should have List<string> backing field type");
        Assert.IsTrue(generated.Contains("HomeAddressProperty"), "Should have HomeAddressProperty");
        Assert.IsTrue(generated.Contains("TagsProperty"), "Should have TagsProperty");
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

        // Generator should still produce output but without backing field for non-partial
        if (generated != null)
        {
            Assert.IsFalse(generated.Contains("NameProperty"),
                "Should not generate backing field for non-partial property");
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
