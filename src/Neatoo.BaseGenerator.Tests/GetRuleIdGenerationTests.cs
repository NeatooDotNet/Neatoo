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

    #region Ordinal Assignment Tests

    [TestMethod]
    public void GetRuleId_OrdinalsStartAtOne()
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
        Assert.IsTrue(generated.Contains("=> 1u"), "First ordinal should be 1, not 0");
        Assert.IsFalse(generated.Contains("=> 0u,"), "Should not have ordinal 0 in switch arms");
    }

    [TestMethod]
    public void GetRuleId_OrdinalsAreSortedAlphabetically()
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

        // RuleA should come before RuleZ alphabetically, so RuleA gets ordinal 1
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
}
