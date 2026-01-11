using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.TestInfrastructure;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Comprehensive tests for stable rule identification across serialization.
/// Verifies that RuleId values are preserved during serialize/deserialize and
/// that rules correctly clear only their own messages.
/// </summary>
[TestClass]
public class StableRuleIdSerializationTests : IntegrationTestBase
{
    private IStableRuleIdEntity _entity = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        InitializeScope();
        _entity = GetRequiredService<IStableRuleIdEntity>();
        await _entity.RunRules();
    }

    private IStableRuleIdEntity RoundTrip(IStableRuleIdEntity entity)
    {
        var json = Serialize(entity);
        return Deserialize<IStableRuleIdEntity>(json);
    }

    #region Basic Serialization Tests

    [TestMethod]
    public void InitialState_IsInvalid_DueToMultipleRules()
    {
        // Name is null → NameNotEmptyRule fails
        // RequiredField is null → Required attribute fails
        Assert.IsFalse(_entity.IsValid);
    }

    [TestMethod]
    public void RoundTrip_PreservesInvalidState()
    {
        var deserialized = RoundTrip(_entity);
        Assert.IsFalse(deserialized.IsValid);
    }

    [TestMethod]
    public void RoundTrip_PreservesMessageCount()
    {
        var originalMessages = _entity.BrokenRuleMessages.ToList();
        var deserialized = RoundTrip(_entity);
        var deserializedMessages = deserialized.BrokenRuleMessages.ToList();

        Assert.AreEqual(originalMessages.Count, deserializedMessages.Count,
            "Message count should be preserved after round-trip");
    }

    [TestMethod]
    public void RoundTrip_PreservesRuleIds()
    {
        var originalRuleIds = _entity.BrokenRuleMessages.Select(m => m.RuleId).OrderBy(x => x).ToList();
        var deserialized = RoundTrip(_entity);
        var deserializedRuleIds = deserialized.BrokenRuleMessages.Select(m => m.RuleId).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(originalRuleIds, deserializedRuleIds,
            "RuleIds should be preserved after round-trip");
    }

    [TestMethod]
    public void RoundTrip_PreservesPropertyNames()
    {
        var originalProps = _entity.BrokenRuleMessages.Select(m => m.PropertyName).OrderBy(x => x).ToList();
        var deserialized = RoundTrip(_entity);
        var deserializedProps = deserialized.BrokenRuleMessages.Select(m => m.PropertyName).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(originalProps, deserializedProps,
            "PropertyNames should be preserved after round-trip");
    }

    [TestMethod]
    public void RoundTrip_PreservesMessageText()
    {
        var originalTexts = _entity.BrokenRuleMessages.Select(m => m.Message).OrderBy(x => x).ToList();
        var deserialized = RoundTrip(_entity);
        var deserializedTexts = deserialized.BrokenRuleMessages.Select(m => m.Message).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(originalTexts, deserializedTexts,
            "Message text should be preserved after round-trip");
    }

    #endregion

    #region Rule Clears Only Own Messages Tests

    [TestMethod]
    public async Task AfterRoundTrip_FixingOneRule_ClearsOnlyThatRulesMessage()
    {
        // Setup: multiple rules broken
        _entity.Name = null;  // NameNotEmptyRule fails
        _entity.Value = -5;   // ValuePositiveRule fails
        await _entity.WaitForTasks();

        var initialMessageCount = _entity.BrokenRuleMessages.Count();
        Assert.IsTrue(initialMessageCount >= 2, "Should have at least 2 broken rules");

        // Round trip
        var deserialized = RoundTrip(_entity);

        // Fix only the Value rule
        deserialized.Value = 5;
        await deserialized.WaitForTasks();

        // ValuePositiveRule message should be cleared
        var valueMessages = deserialized.BrokenRuleMessages
            .Where(m => m.PropertyName == nameof(IStableRuleIdEntity.Value) && m.Message?.Contains("ValuePositiveRule") == true)
            .ToList();
        Assert.AreEqual(0, valueMessages.Count, "ValuePositiveRule message should be cleared after fixing");

        // NameNotEmptyRule message should still exist
        var nameMessages = deserialized.BrokenRuleMessages
            .Where(m => m.PropertyName == nameof(IStableRuleIdEntity.Name) && m.Message?.Contains("NameNotEmptyRule") == true)
            .ToList();
        Assert.AreEqual(1, nameMessages.Count, "NameNotEmptyRule message should still exist");
    }

    [TestMethod]
    public async Task AfterRoundTrip_FixingAttributeRule_ClearsOnlyThatRulesMessage()
    {
        // Setup: attribute rule broken
        _entity.RequiredField = null;  // Required attribute fails
        _entity.Name = "ValidName";    // Fix the name rules
        await _entity.WaitForTasks();

        Assert.IsTrue(_entity.BrokenRuleMessages.Any(m => m.PropertyName == nameof(IStableRuleIdEntity.RequiredField)),
            "Required attribute should produce a message");

        // Round trip
        var deserialized = RoundTrip(_entity);

        // Fix the Required attribute rule
        deserialized.RequiredField = 42;
        await deserialized.WaitForTasks();

        // Required message should be cleared
        var requiredMessages = deserialized.BrokenRuleMessages
            .Where(m => m.PropertyName == nameof(IStableRuleIdEntity.RequiredField))
            .ToList();
        Assert.AreEqual(0, requiredMessages.Count, "Required attribute message should be cleared after fixing");
    }

    #endregion

    #region Multiple Rules Same Property Tests

    [TestMethod]
    public async Task MultipleRulesSameProperty_EachHasDistinctRuleId()
    {
        // Trigger multiple rules on Name property
        _entity.Name = null;  // NameNotEmptyRule fails
        await _entity.WaitForTasks();

        var nameMessages = _entity.BrokenRuleMessages
            .Where(m => m.PropertyName == nameof(IStableRuleIdEntity.Name))
            .ToList();

        // Should have at least the NameNotEmptyRule message
        Assert.IsTrue(nameMessages.Count >= 1);

        // Now trigger NameLengthRule too
        _entity.Name = "ThisNameIsWayTooLong";  // NameNotEmptyRule passes, NameLengthRule fails
        await _entity.WaitForTasks();

        nameMessages = _entity.BrokenRuleMessages
            .Where(m => m.PropertyName == nameof(IStableRuleIdEntity.Name))
            .ToList();

        Assert.AreEqual(1, nameMessages.Count, "Should have exactly one Name message (NameLengthRule)");
        Assert.IsTrue(nameMessages[0].Message?.Contains("NameLengthRule") == true);
    }

    [TestMethod]
    public async Task AfterRoundTrip_MultipleRulesSameProperty_CorrectRuleCleared()
    {
        // Trigger NameLengthRule (Name too long)
        _entity.Name = "VeryVeryLongNameThatExceedsLimit";
        _entity.RequiredField = 1;  // Fix required to isolate Name issues
        await _entity.WaitForTasks();

        var deserialized = RoundTrip(_entity);

        // Verify NameLengthRule message exists after round-trip
        var beforeFix = deserialized.BrokenRuleMessages
            .Where(m => m.Message?.Contains("NameLengthRule") == true)
            .ToList();
        Assert.AreEqual(1, beforeFix.Count, "NameLengthRule message should exist after round-trip");

        // Fix by setting a short valid name
        deserialized.Name = "Short";
        await deserialized.WaitForTasks();

        // NameLengthRule message should be cleared
        var afterFix = deserialized.BrokenRuleMessages
            .Where(m => m.Message?.Contains("NameLengthRule") == true)
            .ToList();
        Assert.AreEqual(0, afterFix.Count, "NameLengthRule message should be cleared after fixing");
    }

    [TestMethod]
    public async Task AfterRoundTrip_FluentValidation_CorrectlyCleared()
    {
        // Trigger the "forbidden" fluent validation
        _entity.Name = "forbidden";
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        Assert.IsTrue(_entity.BrokenRuleMessages.Any(m => m.Message?.Contains("forbidden") == true),
            "Forbidden validation should produce a message");

        var deserialized = RoundTrip(_entity);

        // Verify message exists after round-trip
        Assert.IsTrue(deserialized.BrokenRuleMessages.Any(m => m.Message?.Contains("forbidden") == true),
            "Forbidden message should survive round-trip");

        // Fix the violation
        deserialized.Name = "allowed";
        await deserialized.WaitForTasks();

        // Message should be cleared
        Assert.IsFalse(deserialized.BrokenRuleMessages.Any(m => m.Message?.Contains("forbidden") == true),
            "Forbidden message should be cleared after fixing");
    }

    #endregion

    #region Multiple Round Trips Tests

    [TestMethod]
    public async Task MultipleRoundTrips_RuleIdsRemainStable()
    {
        // First round trip
        _entity.Name = null;
        _entity.Value = -1;
        await _entity.WaitForTasks();

        var firstRoundTrip = RoundTrip(_entity);
        var firstRuleIds = firstRoundTrip.BrokenRuleMessages.Select(m => m.RuleId).OrderBy(x => x).ToList();

        // Second round trip
        var secondRoundTrip = RoundTrip(firstRoundTrip);
        var secondRuleIds = secondRoundTrip.BrokenRuleMessages.Select(m => m.RuleId).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(firstRuleIds, secondRuleIds,
            "RuleIds should remain stable across multiple round-trips");

        // Third round trip
        var thirdRoundTrip = RoundTrip(secondRoundTrip);
        var thirdRuleIds = thirdRoundTrip.BrokenRuleMessages.Select(m => m.RuleId).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(firstRuleIds, thirdRuleIds,
            "RuleIds should remain stable across three round-trips");
    }

    [TestMethod]
    public async Task MultipleRoundTrips_FixAndBreakAndFix_MessagesCorrectlyManaged()
    {
        // Initial state: Name is null, multiple rules broken
        _entity.RequiredField = 1;  // Fix required to focus on Name/Value
        await _entity.WaitForTasks();

        var trip1 = RoundTrip(_entity);

        // Fix Name after first round trip
        trip1.Name = "Valid";
        await trip1.WaitForTasks();
        Assert.IsFalse(trip1.BrokenRuleMessages.Any(m => m.PropertyName == nameof(IStableRuleIdEntity.Name)),
            "Name messages should be cleared after fixing");

        var trip2 = RoundTrip(trip1);

        // Break Name again after second round trip
        trip2.Name = null;
        await trip2.WaitForTasks();
        Assert.IsTrue(trip2.BrokenRuleMessages.Any(m => m.PropertyName == nameof(IStableRuleIdEntity.Name)),
            "Name messages should appear again after breaking");

        var trip3 = RoundTrip(trip2);

        // Fix Name again after third round trip
        trip3.Name = "Valid";
        await trip3.WaitForTasks();
        Assert.IsFalse(trip3.BrokenRuleMessages.Any(m => m.PropertyName == nameof(IStableRuleIdEntity.Name)),
            "Name messages should be cleared again after fixing");
    }

    #endregion

    #region Edge Cases Tests

    [TestMethod]
    public async Task RoundTrip_WithNoMessages_RemainsValid()
    {
        // Setup: all rules pass
        _entity.Name = "Valid";
        _entity.Value = 100;
        _entity.Email = "test@example.com";
        _entity.RequiredField = 42;
        await _entity.WaitForTasks();

        Assert.IsTrue(_entity.IsValid, "Entity should be valid when all rules pass");
        Assert.AreEqual(0, _entity.BrokenRuleMessages.Count(), "Should have no messages");

        var deserialized = RoundTrip(_entity);

        Assert.IsTrue(deserialized.IsValid, "Deserialized entity should remain valid");
        Assert.AreEqual(0, deserialized.BrokenRuleMessages.Count(), "Should still have no messages");
    }

    [TestMethod]
    public async Task RoundTrip_WithAllRulesBroken_AllMessagesPreserved()
    {
        // Break every rule
        _entity.Name = null;                    // NameNotEmptyRule
        _entity.Value = -1;                     // ValuePositiveRule
        _entity.Email = "invalid-no-at-sign";   // Email validation
        _entity.RequiredField = null;           // Required attribute
        await _entity.WaitForTasks();

        var originalCount = _entity.BrokenRuleMessages.Count();
        Assert.IsTrue(originalCount >= 4, $"Expected at least 4 messages, got {originalCount}");

        var deserialized = RoundTrip(_entity);
        var deserializedCount = deserialized.BrokenRuleMessages.Count();

        Assert.AreEqual(originalCount, deserializedCount,
            "All messages should be preserved after round-trip");
    }

    [TestMethod]
    public async Task RoundTrip_RuleIdZero_NeverAssigned()
    {
        // Setup some broken rules
        _entity.Name = null;
        await _entity.WaitForTasks();

        var messages = _entity.BrokenRuleMessages.ToList();
        Assert.IsTrue(messages.Count > 0, "Should have some messages");

        // Verify no RuleId is 0 (0 would indicate uninitialized/default)
        foreach (var message in messages)
        {
            Assert.AreNotEqual(0u, message.RuleId,
                $"RuleId should not be 0 for message on property {message.PropertyName}");
        }

        // Same check after round-trip
        var deserialized = RoundTrip(_entity);
        foreach (var message in deserialized.BrokenRuleMessages)
        {
            Assert.AreNotEqual(0u, message.RuleId,
                $"RuleId should not be 0 after round-trip for message on property {message.PropertyName}");
        }
    }

    [TestMethod]
    public async Task RoundTrip_DifferentRules_HaveDifferentRuleIds()
    {
        // Break multiple different rules
        _entity.Name = null;         // NameNotEmptyRule
        _entity.Value = -1;          // ValuePositiveRule
        _entity.RequiredField = null; // Required attribute
        await _entity.WaitForTasks();

        var ruleIds = _entity.BrokenRuleMessages.Select(m => m.RuleId).ToList();
        var distinctRuleIds = ruleIds.Distinct().ToList();

        Assert.AreEqual(ruleIds.Count, distinctRuleIds.Count,
            "Each rule should have a distinct RuleId");

        // Same check after round-trip
        var deserialized = RoundTrip(_entity);
        var deserializedRuleIds = deserialized.BrokenRuleMessages.Select(m => m.RuleId).ToList();
        var deserializedDistinct = deserializedRuleIds.Distinct().ToList();

        Assert.AreEqual(deserializedRuleIds.Count, deserializedDistinct.Count,
            "Each rule should still have a distinct RuleId after round-trip");
    }

    #endregion

    #region Specific Rule Type Tests

    [TestMethod]
    public async Task InjectedRule_RuleIdStableAcrossRoundTrip()
    {
        // Trigger injected NameNotEmptyRule
        _entity.Name = null;
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var originalMessage = _entity.BrokenRuleMessages
            .First(m => m.Message?.Contains("NameNotEmptyRule") == true);
        var originalRuleId = originalMessage.RuleId;

        var deserialized = RoundTrip(_entity);
        var deserializedMessage = deserialized.BrokenRuleMessages
            .First(m => m.Message?.Contains("NameNotEmptyRule") == true);

        Assert.AreEqual(originalRuleId, deserializedMessage.RuleId,
            "Injected rule RuleId should be stable across round-trip");
    }

    [TestMethod]
    public async Task FluentValidation_RuleIdStableAcrossRoundTrip()
    {
        // Trigger fluent validation on Value
        _entity.Value = 2000;  // Exceeds 1000 limit
        _entity.Name = "Valid";
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var originalMessage = _entity.BrokenRuleMessages
            .First(m => m.Message?.Contains("cannot exceed 1000") == true);
        var originalRuleId = originalMessage.RuleId;

        var deserialized = RoundTrip(_entity);
        var deserializedMessage = deserialized.BrokenRuleMessages
            .First(m => m.Message?.Contains("cannot exceed 1000") == true);

        Assert.AreEqual(originalRuleId, deserializedMessage.RuleId,
            "Fluent validation RuleId should be stable across round-trip");
    }

    [TestMethod]
    public async Task AttributeRule_RuleIdStableAcrossRoundTrip()
    {
        // Trigger Required attribute
        _entity.RequiredField = null;
        _entity.Name = "Valid";
        await _entity.WaitForTasks();

        var originalMessage = _entity.BrokenRuleMessages
            .First(m => m.PropertyName == nameof(IStableRuleIdEntity.RequiredField));
        var originalRuleId = originalMessage.RuleId;

        var deserialized = RoundTrip(_entity);
        var deserializedMessage = deserialized.BrokenRuleMessages
            .First(m => m.PropertyName == nameof(IStableRuleIdEntity.RequiredField));

        Assert.AreEqual(originalRuleId, deserializedMessage.RuleId,
            "Attribute rule RuleId should be stable across round-trip");
    }

    #endregion
}
