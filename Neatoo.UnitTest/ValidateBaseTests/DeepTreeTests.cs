using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.ValidateBaseTests
{
    internal class NeatooDeepTreeNode : ValidateBase<NeatooDeepTreeNode>
    {
        public NeatooDeepTreeNode(int depth) : base(new ValidateBaseServices<NeatooDeepTreeNode>()) {
            Depth = depth;

            //using (PauseAllActions())
            {
                if (depth < 10)
                {
                    Left = new NeatooDeepTreeNode(depth + 1);
                    Right = new NeatooDeepTreeNode(depth + 1);
                }
                StringProperty = $"Depth {depth}";
            }
            //RuleManager.AddValidationAsync(async (t) =>
            //{
            //    await Task.Delay(10);

            //    if(StringProperty == "Invalid")
            //    {
            //        return "Invalid";
            //    }

            //    return string.Empty;
            //}, (t) => t.StringProperty);
        }
        public int Depth { get; set; }

        public string? StringProperty { get => Getter<string>(); set => Setter(value); }

        public NeatooDeepTreeNode? Left { get => Getter<NeatooDeepTreeNode>(); set => Setter(value); }
        public NeatooDeepTreeNode? Right { get => Getter<NeatooDeepTreeNode>(); set => Setter(value); }

    }

    [TestClass]
    public class DeepTreeTests
    {

        NeatooDeepTreeNode tree;

        [TestMethod]
        public void DeepTreeTests_Construct()
        {
             tree = new NeatooDeepTreeNode(0);
        }

        [TestMethod]
        public void DeepTreeTests_IsValid()
        {
            tree = new NeatooDeepTreeNode(0);
            Assert.IsTrue(tree.IsValid);
        }
    }
}
