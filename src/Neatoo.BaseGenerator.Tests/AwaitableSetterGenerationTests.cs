using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.BaseGenerator.Tests;

/// <summary>
/// Tests for the generated awaitable setter, Task Set{Name}(T value).
/// </summary>
[TestClass]
public class AwaitableSetterGenerationTests
{
    private static (string Interface, string Class) Generate(string members, string interfaceMembers = "", bool partialInterface = true)
    {
        var interfaceModifier = partialInterface ? "public partial interface" : "public interface";
        var source = $$"""
            {{GeneratorTestHelper.StandardUsings}}
            {{GeneratorTestHelper.NeatooStubs}}

            namespace TestNamespace
            {
                {{interfaceModifier}} ITestEntity : Neatoo.IEntityBase
                {
                    {{interfaceMembers}}
                }

                [Neatoo.RemoteFactory.Factory]
                public partial class TestEntity : Neatoo.EntityBase<TestEntity>, ITestEntity
                {
                    {{members}}
                }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = GeneratorTestHelper.GetGeneratedSourceForClass(result, "TestEntity");
        Assert.IsNotNull(generated, "Should generate code for TestEntity");

        var classStart = generated.IndexOf("partial class TestEntity", StringComparison.Ordinal);
        Assert.IsTrue(classStart >= 0, "Should generate the class");
        return (generated[..classStart], generated[classStart..]);
    }

    [TestMethod]
    public void PublicSet_GeneratesPublicSetterOnClassAndInterface()
    {
        var (iface, cls) = Generate("public partial string? Name { get; set; }");

        Assert.IsTrue(cls.Contains("public global::System.Threading.Tasks.Task SetName(string? value)"), "public SetName on the class");
        Assert.IsTrue(cls.Contains("NameProperty.SetValue(value)"), "writes through SetValue, which honours IsReadOnly");
        Assert.IsTrue(cls.Contains("return task;"), "returns the cascade's Task");
        Assert.IsTrue(iface.Contains("global::System.Threading.Tasks.Task SetName(string? value);"), "SetName on the partial interface");
    }

    [TestMethod]
    public void PrivateSet_GeneratesPrivateSetterOnClassOnly()
    {
        var (iface, cls) = Generate("public partial decimal Total { get; private set; }");

        Assert.IsTrue(cls.Contains("private global::System.Threading.Tasks.Task SetTotal(decimal value)"), "private SetTotal on the class");
        Assert.IsTrue(cls.Contains("TotalProperty.SetPrivateValue(value)"), "writes past IsReadOnly, as the private setter does");
        Assert.IsFalse(iface.Contains("SetTotal"), "a private write never reaches the interface");
    }

    [TestMethod]
    public void ProtectedAndInternalSet_KeepTheirAccessibility_NotOnInterface()
    {
        var (iface, cls) = Generate("""
            public partial string? A { get; protected set; }
            public partial string? B { get; internal set; }
            """);

        Assert.IsTrue(cls.Contains("protected global::System.Threading.Tasks.Task SetA(string? value)"));
        Assert.IsTrue(cls.Contains("internal global::System.Threading.Tasks.Task SetB(string? value)"));
        Assert.IsFalse(iface.Contains("SetA") || iface.Contains("SetB"), "only a public write goes on the interface");
    }

    [TestMethod]
    public void GetOnlyInitAndLazyLoad_GenerateNoSetter()
    {
        var (iface, cls) = Generate("""
            public partial string? ReadOnly { get; }
            public partial string? InitOnly { get; init; }
            public partial Neatoo.EntityLazyLoad<string>? Lazy { get; set; }
            """);

        Assert.IsFalse(cls.Contains("SetReadOnly") || cls.Contains("SetInitOnly") || cls.Contains("SetLazy"));
        Assert.IsFalse(iface.Contains("SetReadOnly") || iface.Contains("SetInitOnly") || iface.Contains("SetLazy"));
    }

    [TestMethod]
    public void HandWrittenSetMember_GeneratesNoSetter()
    {
        var (iface, cls) = Generate("""
            public partial string? Name { get; set; }
            public void SetName(int code) { }
            """);

        Assert.IsFalse(cls.Contains("Task SetName("), "a hand-written SetName is never collided with or hidden");
        Assert.IsFalse(iface.Contains("Task SetName("));
    }

    [TestMethod]
    public void InterfaceAlreadyDeclaresSetter_NotRedeclared()
    {
        var (iface, cls) = Generate(
            "public partial string? Name { get; set; }",
            "System.Threading.Tasks.Task SetName(string? value);");

        Assert.IsTrue(cls.Contains("public global::System.Threading.Tasks.Task SetName(string? value)"), "the class still implements it");
        Assert.IsFalse(iface.Contains("Task SetName("), "the interface's own declaration stands");
    }

    [TestMethod]
    public void InterfaceDeclaresPropertyGetOnly_NoSetterOnInterface()
    {
        var (iface, cls) = Generate(
            "public partial string? Name { get; set; }",
            "string? Name { get; }");

        Assert.IsTrue(cls.Contains("public global::System.Threading.Tasks.Task SetName(string? value)"), "the class keeps its public write");
        Assert.IsFalse(iface.Contains("SetName"), "the interface author left the write out; SetName does not reopen it");
    }

    [TestMethod]
    public void NonPartialInterface_ClassSetterOnly()
    {
        var (iface, cls) = Generate("public partial string? Name { get; set; }", "string? Name { get; set; }", partialInterface: false);

        Assert.IsTrue(cls.Contains("public global::System.Threading.Tasks.Task SetName(string? value)"));
        Assert.IsFalse(iface.Contains("SetName"), "no interface is generated for a non-partial interface");
    }
}
