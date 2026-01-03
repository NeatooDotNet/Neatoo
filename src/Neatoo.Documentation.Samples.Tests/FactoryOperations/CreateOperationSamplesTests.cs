using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.Documentation.Samples.FactoryOperations;

namespace Neatoo.Documentation.Samples.Tests.FactoryOperations;

/// <summary>
/// Tests for CreateOperationSamples.cs code snippets.
/// </summary>
[TestClass]
[TestCategory("Documentation")]
[TestCategory("FactoryOperations")]
public class CreateOperationSamplesTests : SamplesTestBase
{
    [TestInitialize]
    public void TestInitialize()
    {
        InitializeScope();
    }

    #region SimpleProduct Tests

    [TestMethod]
    public void SimpleProduct_Create_InitializesIdAndDate()
    {
        // Arrange
        var factory = GetRequiredService<ISimpleProductFactory>();

        // Act
        var product = factory.Create();

        // Assert
        Assert.AreNotEqual(Guid.Empty, product.Id);
        Assert.IsTrue(product.CreatedDate > DateTime.MinValue);
        Assert.IsTrue(product.IsNew);
    }

    [TestMethod]
    public void SimpleProduct_Create_PropertiesSetInCreate_MarksModified()
    {
        // Arrange
        var factory = GetRequiredService<ISimpleProductFactory>();

        // Act
        var product = factory.Create();

        // Assert - Create() sets Id and CreatedDate, so entity is modified
        Assert.IsTrue(product.IsModified);
        Assert.IsTrue(product.IsNew);
    }

    [TestMethod]
    public void SimpleProduct_SetName_BecomesModified()
    {
        // Arrange
        var factory = GetRequiredService<ISimpleProductFactory>();
        var product = factory.Create();

        // Act
        product.Name = "Widget";

        // Assert
        Assert.IsTrue(product.IsModified);
    }

    #endregion

    #region ProjectWithTasks Tests

    [TestMethod]
    public void ProjectWithTasks_Create_InitializesChildList()
    {
        // Arrange
        var factory = GetRequiredService<IProjectWithTasksFactory>();

        // Act
        var project = factory.Create();

        // Assert
        Assert.IsNotNull(project.Tasks);
        Assert.AreEqual(0, project.Tasks.Count);
    }

    [TestMethod]
    public void ProjectWithTasks_Create_CanAddTasks()
    {
        // Arrange
        var projectFactory = GetRequiredService<IProjectWithTasksFactory>();
        var taskFactory = GetRequiredService<IProjectTaskFactory>();

        var project = projectFactory.Create();

        // Act
        var task1 = taskFactory.Create();
        task1.Title = "First Task";
        project.Tasks.Add(task1);

        var task2 = taskFactory.Create();
        task2.Title = "Second Task";
        project.Tasks.Add(task2);

        // Assert
        Assert.AreEqual(2, project.Tasks.Count);
        Assert.IsTrue(project.IsModified);
    }

    #endregion
}
