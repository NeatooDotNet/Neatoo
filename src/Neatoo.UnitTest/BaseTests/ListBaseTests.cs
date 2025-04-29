using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.BaseTests.Objects;

namespace Neatoo.UnitTest.BaseTests;

[TestClass]
public class ListBaseTests
{
    private IBaseObjectList list;

    [TestInitialize]
    public void TestInitialize()
    {
        list = new BaseObjectList();
    }

}