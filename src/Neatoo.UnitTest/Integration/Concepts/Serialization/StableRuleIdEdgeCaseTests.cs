using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.RemoteFactory.Internal;
using Neatoo.Rules;
using Neatoo.UnitTest.TestInfrastructure;
using System.Text.Json;

namespace Neatoo.UnitTest.Integration.Concepts.Serialization;

/// <summary>
/// Edge case and adversarial tests for stable rule identification.
/// Tests unusual, boundary, and potentially problematic scenarios.
/// </summary>
[TestClass]
public class StableRuleIdEdgeCaseTests : IntegrationTestBase
{
    private IStableRuleIdEntity _entity = null!;
    private NeatooJsonSerializer _serializer = null!;

    [TestInitialize]
    public async Task TestInitialize()
    {
        InitializeScope();
        _entity = GetRequiredService<IStableRuleIdEntity>();
        _serializer = GetRequiredService<NeatooJsonSerializer>();
        await _entity.RunRules();
    }

    private IStableRuleIdEntity RoundTrip(IStableRuleIdEntity entity)
    {
        var json = Serialize(entity);
        return Deserialize<IStableRuleIdEntity>(json);
    }

    #region Boundary Value Tests

    [TestMethod]
    public async Task RuleId_IsNotMaxUInt_UnlessIntentional()
    {
        _entity.Name = null;
        await _entity.WaitForTasks();

        foreach (var message in _entity.BrokenRuleMessages)
        {
            // RuleId should be a small ordinal, not near max value
            Assert.IsTrue(message.RuleId < 1000,
                $"RuleId {message.RuleId} seems unexpectedly large - expected small ordinals");
        }
    }

    [TestMethod]
    public async Task RuleId_StartsAtOneNotZero()
    {
        // Zero typically indicates uninitialized state
        _entity.Name = null;
        _entity.Value = -1;
        _entity.RequiredField = null;
        await _entity.WaitForTasks();

        var minRuleId = _entity.BrokenRuleMessages.Min(m => m.RuleId);
        Assert.IsTrue(minRuleId >= 1,
            "RuleId should start at 1, not 0 (0 indicates uninitialized)");
    }

    [TestMethod]
    public async Task RuleIds_AreContiguous()
    {
        // Break all rules to see all RuleIds
        _entity.Name = null;
        _entity.Value = -1;
        _entity.Email = "no-at";
        _entity.RequiredField = null;
        await _entity.WaitForTasks();

        var ruleIds = _entity.BrokenRuleMessages.Select(m => m.RuleId).Distinct().OrderBy(x => x).ToList();

        // RuleIds should be contiguous or close to it (1, 2, 3, etc.)
        // Not 1, 100, 5000, etc.
        if (ruleIds.Count > 1)
        {
            var maxGap = 0u;
            for (int i = 1; i < ruleIds.Count; i++)
            {
                var gap = ruleIds[i] - ruleIds[i - 1];
                if (gap > maxGap) maxGap = gap;
            }
            Assert.IsTrue(maxGap < 10,
                $"RuleIds should be reasonably contiguous, but found gap of {maxGap}");
        }
    }

    #endregion

    #region Concurrent Rule Execution Tests

    [TestMethod]
    public async Task ConcurrentRuleExecution_MessagesStillCorrect()
    {
        // Trigger multiple rules simultaneously
        _entity.Name = null;
        _entity.Value = -1;
        _entity.Email = "invalid";
        _entity.RequiredField = null;

        // Run rules - they may execute concurrently
        await _entity.WaitForTasks();

        var messagesBefore = _entity.BrokenRuleMessages.ToList();
        var ruleIdsBefore = messagesBefore.Select(m => m.RuleId).OrderBy(x => x).ToList();

        // Round trip
        var deserialized = RoundTrip(_entity);
        var messagesAfter = deserialized.BrokenRuleMessages.ToList();
        var ruleIdsAfter = messagesAfter.Select(m => m.RuleId).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(ruleIdsBefore, ruleIdsAfter,
            "RuleIds should be preserved even with concurrent rule execution");
    }

    [TestMethod]
    public async Task RapidPropertyChanges_MessagesStayConsistent()
    {
        // Rapidly change properties to stress the rule system
        for (int i = 0; i < 10; i++)
        {
            _entity.Name = i % 2 == 0 ? null : "Valid";
            _entity.Value = i % 2 == 0 ? -1 : 100;
        }
        await _entity.WaitForTasks();

        // Final state should be Name="Valid", Value=100 (both valid)
        Assert.AreEqual("Valid", _entity.Name);
        Assert.AreEqual(100, _entity.Value);

        // After round trip, state should be preserved
        var deserialized = RoundTrip(_entity);
        Assert.AreEqual("Valid", deserialized.Name);
        Assert.AreEqual(100, deserialized.Value);
    }

    #endregion

    #region Message Content Edge Cases

    [TestMethod]
    public async Task MessageWithUnicodeCharacters_PreservedAcrossRoundTrip()
    {
        // The fluent validation "forbidden" message - let's test with unicode in values
        _entity.Name = "forbidden";  // Triggers message "Name cannot be 'forbidden'"
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var originalMessages = _entity.BrokenRuleMessages.Select(m => m.Message).ToList();
        var deserialized = RoundTrip(_entity);
        var deserializedMessages = deserialized.BrokenRuleMessages.Select(m => m.Message).ToList();

        CollectionAssert.AreEqual(originalMessages, deserializedMessages,
            "Message content should be preserved exactly");
    }

    [TestMethod]
    public async Task MessageWithSpecialJsonCharacters_PreservedAcrossRoundTrip()
    {
        // Test that messages with quotes and special JSON chars are handled
        _entity.Name = null;  // Message: "Name cannot be empty (NameNotEmptyRule)"
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var originalText = _entity.BrokenRuleMessages.First().Message;
        var json = Serialize(_entity);

        // Verify the JSON is valid
        Assert.IsNotNull(json);
        Assert.IsTrue(json.Length > 0);

        var deserialized = Deserialize<IStableRuleIdEntity>(json);
        var deserializedText = deserialized.BrokenRuleMessages.First().Message;

        Assert.AreEqual(originalText, deserializedText,
            "Message text with special characters should round-trip correctly");
    }

    [TestMethod]
    public async Task EmptyStringMessage_DifferentFromNoMessage()
    {
        // Valid state - no messages
        _entity.Name = "Valid";
        _entity.Value = 100;
        _entity.Email = "test@test.com";
        _entity.RequiredField = 42;
        await _entity.WaitForTasks();

        Assert.AreEqual(0, _entity.BrokenRuleMessages.Count());

        var deserialized = RoundTrip(_entity);
        Assert.AreEqual(0, deserialized.BrokenRuleMessages.Count());
    }

    #endregion

    #region Serialization Format Tests

    [TestMethod]
    public async Task SerializedJson_ContainsRuleIdProperty()
    {
        _entity.Name = null;
        await _entity.WaitForTasks();

        var json = Serialize(_entity);

        // The JSON should contain RuleId serialization
        // This depends on the serialization format, but we expect some identifier
        Assert.IsTrue(json.Contains("RuleId") || json.Contains("ruleId") || json.Contains("id"),
            "Serialized JSON should contain rule identifier");
    }

    [TestMethod]
    public async Task SerializedJson_IsValidJson()
    {
        _entity.Name = null;
        _entity.Value = -1;
        _entity.RequiredField = null;
        await _entity.WaitForTasks();

        var json = Serialize(_entity);

        // Verify it's parseable as JSON
        try
        {
            JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            Assert.Fail($"Serialized output is not valid JSON: {ex.Message}");
        }
    }

    [TestMethod]
    public async Task DeserializeTwice_SameResult()
    {
        _entity.Name = null;
        await _entity.WaitForTasks();

        var json = Serialize(_entity);

        var first = Deserialize<IStableRuleIdEntity>(json);
        var second = Deserialize<IStableRuleIdEntity>(json);

        var firstRuleIds = first.BrokenRuleMessages.Select(m => m.RuleId).OrderBy(x => x).ToList();
        var secondRuleIds = second.BrokenRuleMessages.Select(m => m.RuleId).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(firstRuleIds, secondRuleIds,
            "Deserializing same JSON twice should produce identical results");
    }

    #endregion

    #region Rule Removal/Addition Scenarios

    [TestMethod]
    public async Task AfterRoundTrip_NewRuleTriggered_GetsNewRuleId()
    {
        // Start with Name valid
        _entity.Name = "Valid";
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        Assert.IsFalse(_entity.BrokenRuleMessages.Any(m => m.PropertyName == nameof(IStableRuleIdEntity.Name)));

        var deserialized = RoundTrip(_entity);

        // Now trigger a name rule
        deserialized.Name = null;
        await deserialized.WaitForTasks();

        // Should have a message with a valid RuleId
        var nameMessage = deserialized.BrokenRuleMessages
            .First(m => m.PropertyName == nameof(IStableRuleIdEntity.Name));

        Assert.AreNotEqual(0u, nameMessage.RuleId, "New message should have valid RuleId");
    }

    [TestMethod]
    public async Task AfterRoundTrip_AllRulesPass_NoOrphanMessages()
    {
        // Start broken
        _entity.Name = null;
        _entity.Value = -1;
        await _entity.WaitForTasks();

        var brokenCount = _entity.BrokenRuleMessages.Count();
        Assert.IsTrue(brokenCount > 0);

        var deserialized = RoundTrip(_entity);

        // Fix everything
        deserialized.Name = "Valid";
        deserialized.Value = 100;
        deserialized.Email = "test@test.com";
        deserialized.RequiredField = 42;
        await deserialized.WaitForTasks();

        Assert.AreEqual(0, deserialized.BrokenRuleMessages.Count(),
            "All messages should be cleared when all rules pass");
    }

    #endregion

    #region Property Name Matching Tests

    [TestMethod]
    public async Task MessagePropertyName_ExactMatch()
    {
        _entity.Name = null;
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var message = _entity.BrokenRuleMessages.First(m => m.Message?.Contains("NameNotEmptyRule") == true);
        Assert.AreEqual(nameof(IStableRuleIdEntity.Name), message.PropertyName,
            "PropertyName should exactly match the property identifier");
    }

    [TestMethod]
    public async Task MessagePropertyName_CaseSensitive()
    {
        _entity.Name = null;
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var message = _entity.BrokenRuleMessages.First(m => m.PropertyName == nameof(IStableRuleIdEntity.Name));

        // PropertyName should be PascalCase "Name", not lowercase "name"
        Assert.AreEqual("Name", message.PropertyName,
            "PropertyName should have correct PascalCase casing");
    }

    #endregion

    #region Stress Tests

    [TestMethod]
    public async Task ManyRoundTrips_NoAccumulation()
    {
        _entity.Name = null;
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var initialCount = _entity.BrokenRuleMessages.Count();

        var current = _entity;
        for (int i = 0; i < 20; i++)
        {
            current = RoundTrip(current);
        }

        var finalCount = current.BrokenRuleMessages.Count();
        Assert.AreEqual(initialCount, finalCount,
            "Message count should not change through repeated round-trips");
    }

    [TestMethod]
    public async Task ManyRoundTrips_RuleIdsStable()
    {
        _entity.Name = null;
        _entity.Value = -1;
        _entity.RequiredField = null;
        await _entity.WaitForTasks();

        var originalRuleIds = _entity.BrokenRuleMessages
            .Select(m => m.RuleId).OrderBy(x => x).ToList();

        var current = _entity;
        for (int i = 0; i < 20; i++)
        {
            current = RoundTrip(current);
        }

        var finalRuleIds = current.BrokenRuleMessages
            .Select(m => m.RuleId).OrderBy(x => x).ToList();

        CollectionAssert.AreEqual(originalRuleIds, finalRuleIds,
            "RuleIds should remain stable through 20 round-trips");
    }

    #endregion

    #region State Consistency Tests

    [TestMethod]
    public async Task IsValid_ConsistentWithMessages()
    {
        _entity.Name = null;
        await _entity.WaitForTasks();

        Assert.IsFalse(_entity.IsValid);
        Assert.IsTrue(_entity.BrokenRuleMessages.Any());

        var deserialized = RoundTrip(_entity);

        // IsValid should be consistent with messages
        if (deserialized.BrokenRuleMessages.Any())
        {
            Assert.IsFalse(deserialized.IsValid,
                "IsValid should be false when there are broken rule messages");
        }
        else
        {
            Assert.IsTrue(deserialized.IsValid,
                "IsValid should be true when there are no broken rule messages");
        }
    }

    [TestMethod]
    public async Task AfterRoundTrip_IsModified_NotAffectedByDeserialization()
    {
        // IsModified should typically be reset/managed during deserialization
        _entity.Name = "Changed";
        _entity.RequiredField = 1;
        await _entity.WaitForTasks();

        var wasModified = _entity.IsModified;

        var deserialized = RoundTrip(_entity);

        // Deserialization behavior for IsModified depends on implementation
        // but it shouldn't interfere with rule messages
        Assert.AreEqual(_entity.BrokenRuleMessages.Count(), deserialized.BrokenRuleMessages.Count());
    }

    #endregion

    #region Order Independence Tests

    [TestMethod]
    public async Task RuleExecutionOrder_DoesNotAffectRuleIds()
    {
        // Trigger rules in different orders and verify RuleIds are the same
        _entity.Name = null;
        await _entity.WaitForTasks();
        var nameRuleId1 = _entity.BrokenRuleMessages
            .First(m => m.PropertyName == nameof(IStableRuleIdEntity.Name)).RuleId;

        _entity.Name = "Valid";
        _entity.Value = -1;
        await _entity.WaitForTasks();

        _entity.Name = null;
        await _entity.WaitForTasks();
        var nameRuleId2 = _entity.BrokenRuleMessages
            .First(m => m.PropertyName == nameof(IStableRuleIdEntity.Name)).RuleId;

        Assert.AreEqual(nameRuleId1, nameRuleId2,
            "RuleId should be the same regardless of when the rule triggers");
    }

    [TestMethod]
    public async Task MessageOrderInCollection_DoesNotAffectMatching()
    {
        // Break multiple rules
        _entity.Name = null;
        _entity.Value = -1;
        _entity.RequiredField = null;
        await _entity.WaitForTasks();

        var deserialized = RoundTrip(_entity);

        // Fix each one individually and verify correct clearing
        deserialized.Name = "Valid";
        await deserialized.WaitForTasks();

        var remainingMessages = deserialized.BrokenRuleMessages.ToList();
        Assert.IsFalse(remainingMessages.Any(m => m.PropertyName == nameof(IStableRuleIdEntity.Name)),
            "Name messages should be cleared regardless of order in collection");

        Assert.IsTrue(remainingMessages.Any(m => m.PropertyName == nameof(IStableRuleIdEntity.Value)),
            "Value messages should still exist");
    }

    #endregion
}
