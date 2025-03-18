using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.ValidateBaseTests;



[TestClass]
public class ValidateListBaseTests
{

    IServiceScope scope;
    IValidateObjectList List;
    IValidateObject Child;

    [TestInitialize]
    public void TestInitailize()
    {
        scope = UnitTestServices.GetLifetimeScope();
        List = scope.GetRequiredService<IValidateObjectList>();
        Child = scope.GetRequiredService<IValidateObject>();
        List.PropertyChanged += Validate_PropertyChanged;
        Child.PropertyChanged += ChildValidate_PropertyChanged;
        List.Add(Child);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Assert.IsFalse(List.IsBusy);
        Assert.IsFalse(List.IsSelfBusy);
        List.PropertyChanged -= Validate_PropertyChanged;
        Child.PropertyChanged -= ChildValidate_PropertyChanged;
    }

    private List<string> propertyChangedCalls = new List<string>();
    private void Validate_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        propertyChangedCalls.Add(e.PropertyName);
    }

    private List<string> childPropertyChangedCalls = new List<string>();
    private void ChildValidate_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        childPropertyChangedCalls.Add(e.PropertyName);
    }

    [TestMethod]
    public void ValidateList_Constructor()
    {

    }


    [TestMethod]
    public void ValidateList_ChildInvalid()
    {
        Child.FirstName = "Error";
        Assert.IsFalse(Child.IsValid);
        Assert.IsFalse(Child.IsSelfValid);
        Assert.IsFalse(List.IsBusy);
        Assert.IsFalse(List.IsValid);
        Assert.IsTrue(List.IsSelfValid);

        Assert.IsTrue(propertyChangedCalls.Contains(nameof(List.IsValid)));
        Assert.IsFalse(propertyChangedCalls.Contains(nameof(List.IsSelfValid)));

        Assert.IsTrue(childPropertyChangedCalls.Contains(nameof(Child.FirstName)));
        Assert.IsTrue(childPropertyChangedCalls.Contains(nameof(Child.IsValid)));
        Assert.IsTrue(childPropertyChangedCalls.Contains(nameof(Child.IsSelfValid)));
        // No async rules - so never busy
        Assert.IsFalse(childPropertyChangedCalls.Contains(nameof(Child.IsBusy)));
        Assert.IsFalse(childPropertyChangedCalls.Contains(nameof(Child.IsSelfBusy)));
    }

}
