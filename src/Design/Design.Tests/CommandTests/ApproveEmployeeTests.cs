// -----------------------------------------------------------------------------
// Design.Tests - Command Tests
// -----------------------------------------------------------------------------
// Pins the command pattern: a [Remote, Execute] private static method reached
// through its generated delegate. It returns data on success and throws when
// reached with an employee that cannot be approved - no failure result.
// -----------------------------------------------------------------------------

using Design.Domain.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.CommandTests;

[TestClass]
public class ApproveEmployeeTests
{
    private IServiceScope _scope = null!;
    private ApproveEmployee.Approve _approve = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _scope = DesignTestServices.GetScope();
        _approve = _scope.GetRequiredService<ApproveEmployee.Approve>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _scope.Dispose();
    }

    [TestMethod]
    public async Task Approve_ApprovableEmployee_ReturnsReceiptAndApproves()
    {
        var receipt = await _approve(1, "Manager");

        Assert.AreEqual("Ada Lovelace", receipt.FullName);
        Assert.AreEqual("Manager", receipt.ApproverName);

        var repository = (MockApproveEmployeeRepository)_scope.GetRequiredService<IApproveEmployeeRepository>();
        Assert.IsTrue(repository.Employees[1].IsApproved);
    }

    [TestMethod]
    public async Task Approve_AlreadyApproved_ThrowsInsteadOfReturningFailure()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _approve(2, "Manager"));
    }

    [TestMethod]
    public async Task Approve_UnknownEmployee_Throws()
    {
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _approve(99, "Manager"));
    }
}
