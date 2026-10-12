// -----------------------------------------------------------------------------
// Design.Tests - A Value Object Is a Record
// -----------------------------------------------------------------------------
// Pins the record shape: value equality, a constructor that throws on bad
// input, and an employee carrying one across the client/server pair - the
// risk for a record with a validating constructor is JSON constructor binding
// on the wire, so the round trip is the test that matters.
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using Design.Tests.CompositionTests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Design.Tests.EntityTests;

[TestClass]
public class PayGradeTests
{
    #region skill-value-object-record-test
    [TestMethod]
    public void Record_ComparesByValue_AndRejectsAnUnknownCode()
    {
        Assert.AreEqual(new PayGrade("B"), new PayGrade("B"));
        Assert.AreNotEqual(new PayGrade("B"), new PayGrade("C"));
        Assert.IsTrue(new PayGrade("B").Covers(50_000m));

        // Bad input is a programming error, not a validation message
        Assert.ThrowsExactly<ArgumentException>(() => new PayGrade("Z"));
    }

    [TestMethod]
    public async Task Employee_CarriesTheRecord_AcrossTheClientServerPair()
    {
        var (server, client, link) = DesignClientServer.Scopes();
        using (server)
        using (client)
        {
            var factory = client.ServiceProvider.GetRequiredService<IEmployeeFactory>();

            var employee = factory.Create();
            employee.FirstName = "Ada";
            employee.LastName = "Lovelace";
            employee.Email = "ada@example.com";
            Assert.AreEqual(PayGrade.Entry, employee.Grade, "Create assigned the entry grade");

            employee.Grade = new PayGrade("C");   // a promotion: code assigns a value from the set
            await employee.WaitForTasks();

            var saved = (IEmployee)await employee.Save();
            var fetched = await factory.Fetch(saved.Id);

            Assert.AreEqual(2, link.Calls, "save and fetch both crossed");
            Assert.AreEqual(new PayGrade("C"), fetched!.Grade, "the record survived serialization both ways");
            Assert.AreEqual(90_000m, fetched.Grade!.MaxSalary);
        }
    }
    #endregion
}
