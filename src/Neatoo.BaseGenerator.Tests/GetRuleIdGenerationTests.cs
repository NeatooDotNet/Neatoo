using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.BaseGenerator.Tests;

/// <summary>
/// Tests for GetRuleId override generation.
/// Verifies that the generator correctly extracts rule expressions and generates
/// stable ordinal IDs for serialization round-trips.
/// </summary>
[TestClass]
public class GetRuleIdGenerationTests
{
    #region AddRule Tests

    [TestMethod]
    public void GetRuleId_AddRule_ExtractsRuleExpression()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IMyRule : Neatoo.IRule<ITestEntity> { }
                public class MyRule : IMyRule { }

                public interface ITestEntity : Neatoo.IEntityBase { }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public TestEntity()
                    {
                        RuleManager.AddRule(new MyRule());
                    }

                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated, "Should generate code for TestEntity");
        Assert.IsTrue(generated.Contains("GetRuleId"), "Should generate GetRuleId override");
        Assert.IsTrue(generated.Contains("new MyRule()"), "Should contain the rule expression");
    }

    [TestMethod]
    public void GetRuleId_MultipleAddRules_GeneratesAllExpressions()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IRuleA : Neatoo.IRule<ITestEntity> { }
                public interface IRuleB : Neatoo.IRule<ITestEntity> { }
                public class RuleA : IRuleA { }
                public class RuleB : IRuleB { }

                public interface ITestEntity : Neatoo.IEntityBase { }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public TestEntity()
                    {
                        RuleManager.AddRule(new RuleA());
                        RuleManager.AddRule(new RuleB());
                    }

                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("new RuleA()"), "Should contain RuleA expression");
        Assert.IsTrue(generated.Contains("new RuleB()"), "Should contain RuleB expression");
    }

    #endregion

    #region AddValidation Tests

    [TestMethod]
    public void GetRuleId_AddValidation_ExtractsLambdaExpression()
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
                    public TestEntity()
                    {
                        RuleManager.AddValidation(
                            t => string.IsNullOrEmpty(t.Name) ? "Name required" : "",
                            t => t.Name);
                    }

                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("GetRuleId"), "Should generate GetRuleId override");
        // The lambda expression should be captured
        Assert.IsTrue(generated.Contains("t =>") || generated.Contains("string.IsNullOrEmpty"),
            "Should contain the validation lambda expression");
    }

    #endregion

    #region AddAction Tests

    [TestMethod]
    public void GetRuleId_AddAction_ExtractsLambdaExpression()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? FirstName { get; set; }
                    string? LastName { get; set; }
                    string? FullName { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public TestEntity()
                    {
                        RuleManager.AddAction(
                            t => t.FullName = t.FirstName + " " + t.LastName,
                            t => t.FirstName,
                            t => t.LastName);
                    }

                    public partial string? FirstName { get; set; }
                    public partial string? LastName { get; set; }
                    public partial string? FullName { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("GetRuleId"), "Should generate GetRuleId override");
        // Should capture the action lambda, not the trigger properties
        Assert.IsTrue(generated.Contains("FullName =") || generated.Contains("t.FullName"),
            "Should contain the action lambda expression");
    }

    #endregion

    #region Attribute Rules Tests

    [TestMethod]
    public void GetRuleId_RequiredAttribute_GeneratesAttributeExpression()
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
                    [Required]
                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("GetRuleId"), "Should generate GetRuleId override");
        Assert.IsTrue(generated.Contains("RequiredAttribute_Name"),
            "Should generate attribute rule expression");
    }

    [TestMethod]
    public void GetRuleId_MultipleAttributes_GeneratesAllExpressions()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface ITestEntity : Neatoo.IEntityBase
                {
                    string? Name { get; set; }
                    string? Email { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    [Required]
                    [StringLength(100)]
                    public partial string? Name { get; set; }

                    [Required]
                    public partial string? Email { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("RequiredAttribute_Name"), "Should have Required for Name");
        Assert.IsTrue(generated.Contains("StringLengthAttribute_Name"), "Should have StringLength for Name");
        Assert.IsTrue(generated.Contains("RequiredAttribute_Email"), "Should have Required for Email");
    }

    #endregion

    #region Hash-Based ID Tests

    [TestMethod]
    public void GetRuleId_UsesHashBasedIds()
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
                    [Required]
                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        // Should use hex hash IDs, not sequential ordinals
        Assert.IsTrue(generated.Contains("=> 0x"), "Should use hash-based hex IDs");
        Assert.IsFalse(generated.Contains("=> 1u,"), "Should not use sequential ordinals");
    }

    [TestMethod]
    public void GetRuleId_ExpressionsAreSortedAlphabetically()
    {
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IRuleZ : Neatoo.IRule<ITestEntity> { }
                public interface IRuleA : Neatoo.IRule<ITestEntity> { }
                public class RuleZ : IRuleZ { }
                public class RuleA : IRuleA { }

                public interface ITestEntity : Neatoo.IEntityBase { }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    public TestEntity()
                    {
                        // Register Z before A
                        RuleManager.AddRule(new RuleZ());
                        RuleManager.AddRule(new RuleA());
                    }

                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);

        // RuleA should come before RuleZ alphabetically
        var ruleAIndex = generated.IndexOf("new RuleA()");
        var ruleZIndex = generated.IndexOf("new RuleZ()");

        Assert.IsTrue(ruleAIndex < ruleZIndex,
            "RuleA should appear before RuleZ in generated code (alphabetical order)");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void GetRuleId_NoRules_DoesNotGenerateOverride()
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
        // With no rules, GetRuleId should not be generated
        Assert.IsFalse(generated.Contains("GetRuleId"),
            "Should not generate GetRuleId when there are no rules");
    }

    [TestMethod]
    public void GetRuleId_FallsBackToBaseForUnknown()
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
                    [Required]
                    public partial string? Name { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");

        Assert.IsNotNull(generated);
        Assert.IsTrue(generated.Contains("_ => base.GetRuleId"),
            "Should have fallback to base.GetRuleId for unknown expressions");
    }

    #endregion

    #region Inheritance Tests

    [TestMethod]
    public void GetRuleId_DerivedClassWithRules_HashIdsDoNotCollide()
    {
        // Hash-based IDs are derived from expression content, so different
        // expressions in base and derived classes get different IDs.
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IBaseEntity : Neatoo.IEntityBase
                {
                    string? A { get; set; }
                    string? B { get; set; }
                    string? Foo { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class BaseEntity : Neatoo.EntityBase<BaseEntity>, IBaseEntity
                {
                    public BaseEntity()
                    {
                        RuleManager.AddAction(
                            t => t.Foo = t.A + t.B,
                            t => t.A,
                            t => t.B);
                    }

                    public partial string? A { get; set; }
                    public partial string? B { get; set; }
                    public partial string? Foo { get; set; }
                }

                public interface IDerivedEntity : IBaseEntity
                {
                    string? C { get; set; }
                    string? Bar { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class DerivedEntity : BaseEntity, IDerivedEntity
                {
                    public DerivedEntity()
                    {
                        RuleManager.AddAction(
                            t => t.Bar = t.C,
                            t => t.C);
                    }

                    public partial string? C { get; set; }
                    public partial string? Bar { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var baseGenerated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "BaseEntity");
        var derivedGenerated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "DerivedEntity");

        Assert.IsNotNull(baseGenerated, "Should generate code for BaseEntity");
        Assert.IsNotNull(derivedGenerated, "Should generate code for DerivedEntity");

        Assert.IsTrue(baseGenerated.Contains("GetRuleId"), "BaseEntity should have GetRuleId");
        Assert.IsTrue(derivedGenerated.Contains("GetRuleId"), "DerivedEntity should have GetRuleId");

        // Both use hash-based IDs — different expressions produce different hashes
        Assert.IsTrue(baseGenerated.Contains("=> 0x"), "BaseEntity should use hash-based IDs");
        Assert.IsTrue(derivedGenerated.Contains("=> 0x"), "DerivedEntity should use hash-based IDs");

        // Extract the hash values and verify they don't collide
        var baseHash = ExtractHashValue(baseGenerated, "t => t.Foo = t.A + t.B");
        var derivedHash = ExtractHashValue(derivedGenerated, "t => t.Bar = t.C");

        Assert.IsNotNull(baseHash, "Should find hash for base expression");
        Assert.IsNotNull(derivedHash, "Should find hash for derived expression");
        Assert.AreNotEqual(baseHash, derivedHash,
            "Base and derived expressions must produce different hash IDs");
    }

    [TestMethod]
    public void GetRuleId_ThreeLevelHierarchy_AllHashIdsUnique()
    {
        // Three-level hierarchy: Base → Middle → Leaf, each with rules.
        // Hash-based IDs ensure no collisions across hierarchy levels.
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                public interface IBaseEntity : Neatoo.IEntityBase
                {
                    string? A { get; set; }
                    string? ComputedA { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class BaseEntity : Neatoo.EntityBase<BaseEntity>, IBaseEntity
                {
                    public BaseEntity()
                    {
                        RuleManager.AddAction(
                            t => t.ComputedA = t.A,
                            t => t.A);
                    }

                    public partial string? A { get; set; }
                    public partial string? ComputedA { get; set; }
                }

                public interface IMiddleEntity : IBaseEntity
                {
                    string? B { get; set; }
                    string? ComputedB { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class MiddleEntity : BaseEntity, IMiddleEntity
                {
                    public MiddleEntity()
                    {
                        RuleManager.AddAction(
                            t => t.ComputedB = t.B,
                            t => t.B);
                    }

                    public partial string? B { get; set; }
                    public partial string? ComputedB { get; set; }
                }

                public interface ILeafEntity : IMiddleEntity
                {
                    string? C { get; set; }
                    string? ComputedC { get; set; }
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class LeafEntity : MiddleEntity, ILeafEntity
                {
                    public LeafEntity()
                    {
                        RuleManager.AddAction(
                            t => t.ComputedC = t.C,
                            t => t.C);
                    }

                    public partial string? C { get; set; }
                    public partial string? ComputedC { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        var baseGenerated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "BaseEntity");
        var middleGenerated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "MiddleEntity");
        var leafGenerated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "LeafEntity");

        Assert.IsNotNull(baseGenerated);
        Assert.IsNotNull(middleGenerated);
        Assert.IsNotNull(leafGenerated);

        // Extract hashes and verify all three are unique
        var baseHash = ExtractHashValue(baseGenerated, "t => t.ComputedA = t.A");
        var middleHash = ExtractHashValue(middleGenerated, "t => t.ComputedB = t.B");
        var leafHash = ExtractHashValue(leafGenerated, "t => t.ComputedC = t.C");

        Assert.IsNotNull(baseHash);
        Assert.IsNotNull(middleHash);
        Assert.IsNotNull(leafHash);

        Assert.AreNotEqual(baseHash, middleHash, "Base and Middle must have different hashes");
        Assert.AreNotEqual(middleHash, leafHash, "Middle and Leaf must have different hashes");
        Assert.AreNotEqual(baseHash, leafHash, "Base and Leaf must have different hashes");
    }

    /// <summary>
    /// Extracts the hex hash value from generated GetRuleId code for a given expression.
    /// </summary>
    private static string? ExtractHashValue(string generated, string expression)
    {
        // Look for pattern: @"expression" => 0xHHHHHHHHu
        var escapedExpr = expression.Replace("\"", "\"\"");
        var marker = $"@\"{escapedExpr}\" => ";
        var index = generated.IndexOf(marker);
        if (index < 0) return null;

        var start = index + marker.Length;
        var end = generated.IndexOf('u', start);
        if (end < 0) return null;

        return generated.Substring(start, end - start);
    }

    #endregion
}
