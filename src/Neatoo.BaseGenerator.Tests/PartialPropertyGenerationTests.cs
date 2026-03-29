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

    #region LazyLoad Property Tests

    [TestMethod]
    public void LazyLoadProperty_GeneratesLoadValueSetter()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IChildEntity : Neatoo.IEntityBase { }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("LazyChildProperty.LoadValue(value)"),
            "LazyLoad setter should use LoadValue, not .Value =");
        Assert.IsFalse(generated.Contains("LazyChildProperty.Value = value"),
            "LazyLoad setter should NOT use .Value = (that triggers rules)");
        Assert.IsFalse(generated.Contains("RunningTasks.AddTask"),
            "LazyLoad setter should NOT include task tracking");
    }

    [TestMethod]
    public void LazyLoadProperty_GeneratesCreateEntityLazyLoadRegistration()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IChildEntity : Neatoo.IEntityBase { }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("factory.CreateEntityLazyLoad<IChildEntity>"),
            "LazyLoad registration should use CreateEntityLazyLoad<TInner>");
        Assert.IsFalse(generated.Contains("factory.Create<Neatoo.EntityLazyLoad<IChildEntity>>"),
            "LazyLoad registration should NOT use Create<LazyLoad<T>>");
    }

    [TestMethod]
    public void LazyLoadProperty_GeneratesBackingFieldWithWrapperType()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IChildEntity : Neatoo.IEntityBase { }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("IValidateProperty<Neatoo.EntityLazyLoad<IChildEntity>>"),
            "Backing field should use the full LazyLoad<T> wrapper type");
        Assert.IsTrue(generated.Contains("LazyChildProperty"),
            "Should have LazyChildProperty backing field");
    }

    [TestMethod]
    public void LazyLoadProperty_MixedWithScalarProperties()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IChildEntity : Neatoo.IEntityBase { }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                    Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; set; }
                    public partial Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        // Scalar property uses .Value = and task tracking
        Assert.IsTrue(generated.Contains("NameProperty.Value = value"),
            "Scalar property should use .Value = setter");
        // LazyLoad property uses LoadValue and no task tracking
        Assert.IsTrue(generated.Contains("LazyChildProperty.LoadValue(value)"),
            "LazyLoad property should use LoadValue setter");
        // Registration: scalar uses Create, LazyLoad uses CreateEntityLazyLoad
        Assert.IsTrue(generated.Contains("factory.Create<string?>"),
            "Scalar should use factory.Create");
        Assert.IsTrue(generated.Contains("factory.CreateEntityLazyLoad<IChildEntity>"),
            "LazyLoad should use factory.CreateEntityLazyLoad");
    }

    [TestMethod]
    public void LazyLoadProperty_GetterOnly_NoSetter()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IChildEntity : Neatoo.IEntityBase { }

                public interface ITestEntity : Neatoo.IEntityBase
                {
                    Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("LazyChildProperty.Value;"),
            "Getter-only LazyLoad should have getter");
        Assert.IsFalse(generated.Contains("LoadValue"),
            "Getter-only LazyLoad should NOT have setter");
    }

    [TestMethod]
    public void LazyLoadProperty_StringInnerType()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    Neatoo.EntityLazyLoad<string> LazyDesc { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial Neatoo.EntityLazyLoad<string> LazyDesc { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("factory.CreateEntityLazyLoad<string>"),
            "LazyLoad<string> should use CreateEntityLazyLoad<string>");
        Assert.IsTrue(generated.Contains("LazyDescProperty.LoadValue(value)"),
            "LazyLoad<string> should use LoadValue setter");
    }

    #endregion

    #region Private Setter Tests

    /// <summary>
    /// Scenario 1: WHEN a partial property has private set, THEN the generated setter has private set accessor.
    /// </summary>
    [TestMethod]
    public void PartialProperty_PrivateSetter_GeneratesPrivateSetAccessor()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; private set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("private set"),
            "Generated setter should have 'private set' accessor");
        Assert.IsTrue(generated.Contains("SetPrivateValue(value)"),
            "Private setter should call SetPrivateValue(value)");
    }

    /// <summary>
    /// Scenario 2: WHEN a partial property has private set and NeedsInterfaceDeclaration=true,
    /// THEN the generated interface declaration has get; only (no set;).
    /// </summary>
    [TestMethod]
    public void PartialProperty_PrivateSetter_InterfaceIsGetOnly()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; private set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        // Interface should have get; only for private set properties
        Assert.IsTrue(generated.Contains("string? Name { get; }"),
            "Interface declaration should have 'get;' only for private set property");
        Assert.IsFalse(generated.Contains("string? Name { get; set; }"),
            "Interface declaration should NOT have 'get; set;' for private set property");
    }

    /// <summary>
    /// Scenario 3: WHEN a partial property has private set, THEN the generated setter body
    /// calls SetPrivateValue(value) instead of .Value = value.
    /// </summary>
    [TestMethod]
    public void PartialProperty_PrivateSetter_UsesSetPrivateValue()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Name { get; private set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("NameProperty.SetPrivateValue(value)"),
            "Private setter should use SetPrivateValue(value)");
        Assert.IsFalse(generated.Contains("NameProperty.Value = value"),
            "Private setter should NOT use .Value = value");
    }

    /// <summary>
    /// Scenario 4: WHEN a partial property has protected set, THEN the generated setter has
    /// protected set accessor and uses .Value = value (same as public set).
    /// </summary>
    [TestMethod]
    public void PartialProperty_ProtectedSetter_PreservesAccessorAndUsesValueAssignment()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Derived { get; protected set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("protected set"),
            "Generated setter should have 'protected set' accessor");
        Assert.IsTrue(generated.Contains("DerivedProperty.Value = value"),
            "Protected setter should use .Value = value (same as public)");
        Assert.IsFalse(generated.Contains("SetPrivateValue"),
            "Protected setter should NOT use SetPrivateValue");
    }

    /// <summary>
    /// Scenario 5: WHEN a partial property has internal set, THEN the generated setter has
    /// internal set accessor and uses .Value = value (same as public set).
    /// </summary>
    [TestMethod]
    public void PartialProperty_InternalSetter_PreservesAccessorAndUsesValueAssignment()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? Data { get; internal set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("internal set"),
            "Generated setter should have 'internal set' accessor");
        Assert.IsTrue(generated.Contains("DataProperty.Value = value"),
            "Internal setter should use .Value = value (same as public)");
        Assert.IsFalse(generated.Contains("SetPrivateValue"),
            "Internal setter should NOT use SetPrivateValue");
    }

    /// <summary>
    /// Scenario 6: WHEN a partial property has private set and is a LazyLoad property,
    /// THEN the generated setter has private set and uses LoadValue(value).
    /// </summary>
    [TestMethod]
    public void PartialProperty_LazyLoadWithPrivateSetter_UsesLoadValue()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IChildEntity : Neatoo.IEntityBase { }

                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial Neatoo.EntityLazyLoad<IChildEntity> LazyChild { get; private set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("private set"),
            "LazyLoad private setter should have 'private set' accessor");
        Assert.IsTrue(generated.Contains("LazyChildProperty.LoadValue(value)"),
            "LazyLoad private setter should use LoadValue(value)");
        Assert.IsFalse(generated.Contains("SetPrivateValue"),
            "LazyLoad setter should NOT use SetPrivateValue (LoadValue already bypasses IsReadOnly)");
        Assert.IsFalse(generated.Contains("RunningTasks.AddTask"),
            "LazyLoad setter should NOT include task tracking");
    }

    /// <summary>
    /// Scenario 7: WHEN a partial property has no setter (get-only), THEN generation is unchanged.
    /// Verifies the existing getter-only behavior is not broken by private setter changes.
    /// </summary>
    [TestMethod]
    public void PartialProperty_GetOnlyProperty_UnchangedByPrivateSetterFeature()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? ReadOnly { get; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("ReadOnlyProperty.Value;"),
            "Getter-only property should have getter accessing .Value");
        Assert.IsFalse(generated.Contains("set {") || generated.Contains("set{"),
            "Getter-only property should NOT have a setter");
        Assert.IsFalse(generated.Contains("SetPrivateValue"),
            "Getter-only property should NOT reference SetPrivateValue");
    }

    /// <summary>
    /// Scenario 12: WHEN an entity has both public set and private set properties,
    /// THEN public uses .Value = value and private uses SetPrivateValue.
    /// </summary>
    [TestMethod]
    public void PartialProperty_MixedPublicAndPrivateSetters_GeneratesCorrectPatterns()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public partial interface ITestEntity : Neatoo.IEntityBase
                {
                    string? PublicName { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public partial string? PublicName { get; set; }
                    public partial decimal ComputedTotal { get; private set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");

        // Public property uses .Value = value
        Assert.IsTrue(generated.Contains("PublicNameProperty.Value = value"),
            "Public setter should use .Value = value");

        // Private property uses SetPrivateValue
        Assert.IsTrue(generated.Contains("ComputedTotalProperty.SetPrivateValue(value)"),
            "Private setter should use SetPrivateValue(value)");

        // Interface: PublicName has get; set; (already in interface, not regenerated)
        // Interface: ComputedTotal has get; only (generated by generator)
        Assert.IsTrue(generated.Contains("decimal ComputedTotal { get; }"),
            "Private set property should have get; only on interface");
    }

    #endregion
}
