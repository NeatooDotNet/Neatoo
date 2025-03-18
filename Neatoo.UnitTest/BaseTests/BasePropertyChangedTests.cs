using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Core;
using Neatoo.UnitTest.BaseTests.Objects;
using System.ComponentModel;

namespace Neatoo.UnitTest.BaseTests;

[TestClass]
public class BasePropertyChangedTests
{
    private IServiceScope scope;
    private IBaseObject grandChild;
    private IBaseObject child;
    private IBaseObjectList list;
    private IBaseObject parent;
    private List<string> parentPropertyNames = new List<string>();
    private List<PropertyChangedBreadCrumbs> parentBreadCrumbs = new List<PropertyChangedBreadCrumbs>();
    private List<string> childPropertyNames = new List<string>();
    private List<PropertyChangedBreadCrumbs> childBreadCrumbs = new List<PropertyChangedBreadCrumbs>();
    private List<string> listPropertyNames = new List<string>();
    private List<PropertyChangedBreadCrumbs> listBreadCrumbs = new List<PropertyChangedBreadCrumbs>();

    [TestInitialize]
    public void TestInitialize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        grandChild = scope.GetRequiredService<IBaseObject>();
        grandChild.StringProperty = "GrandChild";
        Assert.IsFalse(grandChild.IsBusy);

        child = scope.GetRequiredService<IBaseObject>();
        child.StringProperty = "Child";
        child.Child = grandChild;
        Assert.IsFalse(grandChild.IsBusy);

        list = scope.GetRequiredService<IBaseObjectList>();
        list.Add(child);
        Assert.IsFalse(list.IsBusy);

        parent = scope.GetRequiredService<IBaseObject>();
        parent.StringProperty = "Parent";
        parent.ChildList = list;
        Assert.IsFalse(parent.IsBusy);

        parent.PropertyChanged += Parent_PropertyChanged;
        parent.NeatooPropertyChanged += Parent_NeatooPropertyChanged;

        child.PropertyChanged += Child_PropertyChanged;
        child.NeatooPropertyChanged += Child_NeatooPropertyChanged;

        list.PropertyChanged += List_PropertyChanged;
        list.NeatooPropertyChanged += List_NeatooPropertyChanged;
    }

    private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        parentPropertyNames.Add(e.PropertyName);
    }

    private Task Parent_NeatooPropertyChanged(PropertyChangedBreadCrumbs propertyNameBreadCrumbs)
    {
        parentBreadCrumbs.Add(propertyNameBreadCrumbs);
        return Task.CompletedTask;
    }

    private void Child_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        childPropertyNames.Add(e.PropertyName);
    }

    private Task Child_NeatooPropertyChanged(PropertyChangedBreadCrumbs propertyNameBreadCrumbs)
    {
        childBreadCrumbs.Add(propertyNameBreadCrumbs);
        return Task.CompletedTask;
    }

    private void List_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        listPropertyNames.Add(e.PropertyName);
    }

    private Task List_NeatooPropertyChanged(PropertyChangedBreadCrumbs propertyNameBreadCrumbs)
    {
        listBreadCrumbs.Add(propertyNameBreadCrumbs);
        return Task.CompletedTask;
    }

    [TestMethod]
    public void BasePropertyChangedTests_Construct()
    {
        Assert.AreSame(grandChild.Parent, child);
        Assert.AreSame(child.Parent, parent);
        Assert.AreSame(list.Parent, parent);
    }

    [TestMethod]
    public void BasePropertyChangedTests_GrandChild_PropertyChanged()
    {
        grandChild.StringProperty = "test";

        Assert.IsFalse(grandChild.IsBusy);
        Assert.AreEqual(1, parentBreadCrumbs.Count);
        Assert.AreEqual("ChildList.Child.StringProperty", parentBreadCrumbs[0].FullPropertyName);
    }

}
