using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Neatoo.UnitTest.Unit.Rules;

/// <summary>
/// Unit tests for the RunRulesFlag enum.
/// Tests flag values, bitwise operations, and combinations.
/// </summary>
[TestClass]
public class RunRulesFlagTests
{
    #region Individual Flag Value Tests

    [TestMethod]
    public void None_Value_EqualsZero()
    {
        // Arrange & Act
        var value = (int)RunRulesFlag.None;

        // Assert
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void NoMessages_Value_EqualsOne()
    {
        // Arrange & Act
        var value = (int)RunRulesFlag.NoMessages;

        // Assert
        Assert.AreEqual(1, value);
    }

    [TestMethod]
    public void Messages_Value_EqualsTwo()
    {
        // Arrange & Act
        var value = (int)RunRulesFlag.Messages;

        // Assert
        Assert.AreEqual(2, value);
    }

    [TestMethod]
    public void NotExecuted_Value_EqualsFour()
    {
        // Arrange & Act
        var value = (int)RunRulesFlag.NotExecuted;

        // Assert
        Assert.AreEqual(4, value);
    }

    [TestMethod]
    public void Executed_Value_EqualsEight()
    {
        // Arrange & Act
        var value = (int)RunRulesFlag.Executed;

        // Assert
        Assert.AreEqual(8, value);
    }

    [TestMethod]
    public void Self_Value_EqualsSixteen()
    {
        // Arrange & Act
        var value = (int)RunRulesFlag.Self;

        // Assert
        Assert.AreEqual(16, value);
    }

    #endregion

    #region Power of Two Validation Tests

    [TestMethod]
    public void NoMessages_IsPowerOfTwo_ReturnsTrue()
    {
        // Arrange
        var value = (int)RunRulesFlag.NoMessages;

        // Act
        var isPowerOfTwo = value != 0 && (value & (value - 1)) == 0;

        // Assert
        Assert.IsTrue(isPowerOfTwo);
    }

    [TestMethod]
    public void Messages_IsPowerOfTwo_ReturnsTrue()
    {
        // Arrange
        var value = (int)RunRulesFlag.Messages;

        // Act
        var isPowerOfTwo = value != 0 && (value & (value - 1)) == 0;

        // Assert
        Assert.IsTrue(isPowerOfTwo);
    }

    [TestMethod]
    public void NotExecuted_IsPowerOfTwo_ReturnsTrue()
    {
        // Arrange
        var value = (int)RunRulesFlag.NotExecuted;

        // Act
        var isPowerOfTwo = value != 0 && (value & (value - 1)) == 0;

        // Assert
        Assert.IsTrue(isPowerOfTwo);
    }

    [TestMethod]
    public void Executed_IsPowerOfTwo_ReturnsTrue()
    {
        // Arrange
        var value = (int)RunRulesFlag.Executed;

        // Act
        var isPowerOfTwo = value != 0 && (value & (value - 1)) == 0;

        // Assert
        Assert.IsTrue(isPowerOfTwo);
    }

    [TestMethod]
    public void Self_IsPowerOfTwo_ReturnsTrue()
    {
        // Arrange
        var value = (int)RunRulesFlag.Self;

        // Act
        var isPowerOfTwo = value != 0 && (value & (value - 1)) == 0;

        // Assert
        Assert.IsTrue(isPowerOfTwo);
    }

    [TestMethod]
    public void AllIndividualFlags_AreDistinctPowersOfTwo()
    {
        // Arrange
        var flags = new[]
        {
            RunRulesFlag.NoMessages,
            RunRulesFlag.Messages,
            RunRulesFlag.NotExecuted,
            RunRulesFlag.Executed,
            RunRulesFlag.Self
        };

        // Act
        var values = flags.Select(f => (int)f).ToArray();

        // Assert
        Assert.AreEqual(flags.Length, values.Distinct().Count(), "All flag values should be unique");
        foreach (var value in values)
        {
            var isPowerOfTwo = value != 0 && (value & (value - 1)) == 0;
            Assert.IsTrue(isPowerOfTwo, $"Value {value} should be a power of two");
        }
    }

    #endregion

    #region All Flag Tests

    [TestMethod]
    public void All_Value_EqualsBitwiseOrOfAllOtherFlags()
    {
        // Arrange
        var expectedValue = RunRulesFlag.NoMessages | RunRulesFlag.Messages |
                           RunRulesFlag.NotExecuted | RunRulesFlag.Executed |
                           RunRulesFlag.Self;

        // Act
        var allValue = RunRulesFlag.All;

        // Assert
        Assert.AreEqual(expectedValue, allValue);
    }

    [TestMethod]
    public void All_IntValue_EqualsThirtyOne()
    {
        // Arrange
        var expected = 1 + 2 + 4 + 8 + 16; // 31

        // Act
        var value = (int)RunRulesFlag.All;

        // Assert
        Assert.AreEqual(expected, value);
        Assert.AreEqual(31, value);
    }

    [TestMethod]
    public void All_HasFlag_ContainsNoMessages()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(RunRulesFlag.All.HasFlag(RunRulesFlag.NoMessages));
    }

    [TestMethod]
    public void All_HasFlag_ContainsMessages()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(RunRulesFlag.All.HasFlag(RunRulesFlag.Messages));
    }

    [TestMethod]
    public void All_HasFlag_ContainsNotExecuted()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(RunRulesFlag.All.HasFlag(RunRulesFlag.NotExecuted));
    }

    [TestMethod]
    public void All_HasFlag_ContainsExecuted()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(RunRulesFlag.All.HasFlag(RunRulesFlag.Executed));
    }

    [TestMethod]
    public void All_HasFlag_ContainsSelf()
    {
        // Arrange & Act & Assert
        Assert.IsTrue(RunRulesFlag.All.HasFlag(RunRulesFlag.Self));
    }

    #endregion

    #region HasFlag Tests

    [TestMethod]
    public void HasFlag_None_AlwaysReturnsTrue()
    {
        // Arrange
        var flags = new[]
        {
            RunRulesFlag.None,
            RunRulesFlag.NoMessages,
            RunRulesFlag.Messages,
            RunRulesFlag.NotExecuted,
            RunRulesFlag.Executed,
            RunRulesFlag.Self,
            RunRulesFlag.All
        };

        // Act & Assert
        foreach (var flag in flags)
        {
            Assert.IsTrue(flag.HasFlag(RunRulesFlag.None),
                $"{flag} should have flag None");
        }
    }

    [TestMethod]
    public void HasFlag_SingleFlag_DoesNotContainOtherFlags()
    {
        // Arrange
        var flag = RunRulesFlag.NoMessages;

        // Act & Assert
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.Messages));
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.NotExecuted));
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.Executed));
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.Self));
    }

    [TestMethod]
    public void HasFlag_CombinedFlags_ContainsBothFlags()
    {
        // Arrange
        var combined = RunRulesFlag.NoMessages | RunRulesFlag.Messages;

        // Act & Assert
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.Messages));
        Assert.IsFalse(combined.HasFlag(RunRulesFlag.NotExecuted));
    }

    [TestMethod]
    public void HasFlag_Self_OnlySelfReturnsTrue()
    {
        // Arrange
        var flag = RunRulesFlag.Self;

        // Act & Assert
        Assert.IsTrue(flag.HasFlag(RunRulesFlag.Self));
        Assert.IsTrue(flag.HasFlag(RunRulesFlag.None));
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.Messages));
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.NotExecuted));
        Assert.IsFalse(flag.HasFlag(RunRulesFlag.Executed));
    }

    #endregion

    #region Bitwise OR Tests

    [TestMethod]
    public void BitwiseOr_TwoFlags_CreatesCombinedValue()
    {
        // Arrange
        var flag1 = RunRulesFlag.NoMessages;
        var flag2 = RunRulesFlag.Messages;

        // Act
        var combined = flag1 | flag2;

        // Assert
        Assert.AreEqual(3, (int)combined);
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.Messages));
    }

    [TestMethod]
    public void BitwiseOr_NoneWithFlag_ReturnsOriginalFlag()
    {
        // Arrange
        var flag = RunRulesFlag.Executed;

        // Act
        var result = RunRulesFlag.None | flag;

        // Assert
        Assert.AreEqual(flag, result);
    }

    [TestMethod]
    public void BitwiseOr_AllWithAnyFlag_ReturnsAll()
    {
        // Arrange
        var flags = new[]
        {
            RunRulesFlag.None,
            RunRulesFlag.NoMessages,
            RunRulesFlag.Messages,
            RunRulesFlag.NotExecuted,
            RunRulesFlag.Executed,
            RunRulesFlag.Self
        };

        // Act & Assert
        foreach (var flag in flags)
        {
            var result = RunRulesFlag.All | flag;
            Assert.AreEqual(RunRulesFlag.All, result,
                $"All | {flag} should equal All");
        }
    }

    [TestMethod]
    public void BitwiseOr_MultipleFlags_CreatesCorrectComposite()
    {
        // Arrange
        var expected = 1 + 4 + 16; // NoMessages + NotExecuted + Self = 21

        // Act
        var combined = RunRulesFlag.NoMessages | RunRulesFlag.NotExecuted | RunRulesFlag.Self;

        // Assert
        Assert.AreEqual(expected, (int)combined);
    }

    #endregion

    #region Bitwise AND Tests

    [TestMethod]
    public void BitwiseAnd_SameFlag_ReturnsSameFlag()
    {
        // Arrange
        var flag = RunRulesFlag.Messages;

        // Act
        var result = flag & flag;

        // Assert
        Assert.AreEqual(flag, result);
    }

    [TestMethod]
    public void BitwiseAnd_DifferentFlags_ReturnsNone()
    {
        // Arrange
        var flag1 = RunRulesFlag.NoMessages;
        var flag2 = RunRulesFlag.Messages;

        // Act
        var result = flag1 & flag2;

        // Assert
        Assert.AreEqual(RunRulesFlag.None, result);
    }

    [TestMethod]
    public void BitwiseAnd_AllWithSingleFlag_ReturnsSingleFlag()
    {
        // Arrange
        var flag = RunRulesFlag.Executed;

        // Act
        var result = RunRulesFlag.All & flag;

        // Assert
        Assert.AreEqual(flag, result);
    }

    [TestMethod]
    public void BitwiseAnd_CombinedWithSingleFlag_ExtractsFlag()
    {
        // Arrange
        var combined = RunRulesFlag.NoMessages | RunRulesFlag.Messages | RunRulesFlag.Executed;
        var target = RunRulesFlag.Messages;

        // Act
        var result = combined & target;

        // Assert
        Assert.AreEqual(target, result);
    }

    [TestMethod]
    public void BitwiseAnd_NoneWithAnyFlag_ReturnsNone()
    {
        // Arrange
        var flags = new[]
        {
            RunRulesFlag.NoMessages,
            RunRulesFlag.Messages,
            RunRulesFlag.NotExecuted,
            RunRulesFlag.Executed,
            RunRulesFlag.Self,
            RunRulesFlag.All
        };

        // Act & Assert
        foreach (var flag in flags)
        {
            var result = RunRulesFlag.None & flag;
            Assert.AreEqual(RunRulesFlag.None, result,
                $"None & {flag} should equal None");
        }
    }

    #endregion

    #region Bitwise XOR Tests

    [TestMethod]
    public void BitwiseXor_SameFlag_ReturnsNone()
    {
        // Arrange
        var flag = RunRulesFlag.NotExecuted;

        // Act
        var result = flag ^ flag;

        // Assert
        Assert.AreEqual(RunRulesFlag.None, result);
    }

    [TestMethod]
    public void BitwiseXor_DifferentFlags_ReturnsCombined()
    {
        // Arrange
        var flag1 = RunRulesFlag.NoMessages;
        var flag2 = RunRulesFlag.Messages;

        // Act
        var result = flag1 ^ flag2;

        // Assert
        Assert.AreEqual(flag1 | flag2, result);
    }

    [TestMethod]
    public void BitwiseXor_ToggleFlagOn_AddsFlag()
    {
        // Arrange
        var current = RunRulesFlag.NoMessages;
        var toToggle = RunRulesFlag.Messages;

        // Act
        var result = current ^ toToggle;

        // Assert
        Assert.IsTrue(result.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(result.HasFlag(RunRulesFlag.Messages));
    }

    [TestMethod]
    public void BitwiseXor_ToggleFlagOff_RemovesFlag()
    {
        // Arrange
        var current = RunRulesFlag.NoMessages | RunRulesFlag.Messages;
        var toToggle = RunRulesFlag.Messages;

        // Act
        var result = current ^ toToggle;

        // Assert
        Assert.IsTrue(result.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsFalse(result.HasFlag(RunRulesFlag.Messages));
        Assert.AreEqual(RunRulesFlag.NoMessages, result);
    }

    [TestMethod]
    public void BitwiseXor_AllWithAll_ReturnsNone()
    {
        // Arrange & Act
        var result = RunRulesFlag.All ^ RunRulesFlag.All;

        // Assert
        Assert.AreEqual(RunRulesFlag.None, result);
    }

    #endregion

    #region Bitwise NOT Tests

    [TestMethod]
    public void BitwiseNot_None_ReturnsAllBitsSet()
    {
        // Arrange & Act
        var result = ~RunRulesFlag.None;

        // Assert
        Assert.IsTrue(result.HasFlag(RunRulesFlag.All));
    }

    [TestMethod]
    public void BitwiseNot_SingleFlag_RemovesThatFlag()
    {
        // Arrange
        var flag = RunRulesFlag.Messages;

        // Act
        var result = ~flag & RunRulesFlag.All;

        // Assert
        Assert.IsFalse(result.HasFlag(RunRulesFlag.Messages));
        Assert.IsTrue(result.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(result.HasFlag(RunRulesFlag.NotExecuted));
        Assert.IsTrue(result.HasFlag(RunRulesFlag.Executed));
        Assert.IsTrue(result.HasFlag(RunRulesFlag.Self));
    }

    #endregion

    #region Flag Independence Tests

    [TestMethod]
    public void IndependentFlags_SettingOneDoesNotAffectOthers()
    {
        // Arrange
        var combined = RunRulesFlag.None;

        // Act - Add flags one at a time
        combined |= RunRulesFlag.NoMessages;
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsFalse(combined.HasFlag(RunRulesFlag.Messages));

        combined |= RunRulesFlag.Messages;
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.Messages));
        Assert.IsFalse(combined.HasFlag(RunRulesFlag.NotExecuted));

        combined |= RunRulesFlag.NotExecuted;
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.Messages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NotExecuted));
        Assert.IsFalse(combined.HasFlag(RunRulesFlag.Executed));
    }

    [TestMethod]
    public void IndependentFlags_RemovingOneDoesNotAffectOthers()
    {
        // Arrange
        var combined = RunRulesFlag.All;

        // Act - Remove flags one at a time
        combined &= ~RunRulesFlag.Self;
        Assert.IsFalse(combined.HasFlag(RunRulesFlag.Self));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.Messages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NotExecuted));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.Executed));

        combined &= ~RunRulesFlag.Executed;
        Assert.IsFalse(combined.HasFlag(RunRulesFlag.Self));
        Assert.IsFalse(combined.HasFlag(RunRulesFlag.Executed));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.Messages));
        Assert.IsTrue(combined.HasFlag(RunRulesFlag.NotExecuted));
    }

    #endregion

    #region Flags Attribute Validation Tests

    [TestMethod]
    public void RunRulesFlag_HasFlagsAttribute()
    {
        // Arrange
        var type = typeof(RunRulesFlag);

        // Act
        var hasFlagsAttribute = type.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;

        // Assert
        Assert.IsTrue(hasFlagsAttribute, "RunRulesFlag should have the [Flags] attribute");
    }

    [TestMethod]
    public void RunRulesFlag_IsEnum()
    {
        // Arrange & Act
        var isEnum = typeof(RunRulesFlag).IsEnum;

        // Assert
        Assert.IsTrue(isEnum);
    }

    #endregion

    #region Composite Flag Value Tests

    [TestMethod]
    public void CombinedFlag_MessagesAndExecuted_EqualsExpectedValue()
    {
        // Arrange
        var expected = 2 + 8; // Messages (2) + Executed (8) = 10

        // Act
        var combined = RunRulesFlag.Messages | RunRulesFlag.Executed;

        // Assert
        Assert.AreEqual(expected, (int)combined);
    }

    [TestMethod]
    public void CombinedFlag_NoMessagesAndNotExecuted_EqualsExpectedValue()
    {
        // Arrange
        var expected = 1 + 4; // NoMessages (1) + NotExecuted (4) = 5

        // Act
        var combined = RunRulesFlag.NoMessages | RunRulesFlag.NotExecuted;

        // Assert
        Assert.AreEqual(expected, (int)combined);
    }

    [TestMethod]
    public void CombinedFlag_ExecutedAndSelf_EqualsExpectedValue()
    {
        // Arrange
        var expected = 8 + 16; // Executed (8) + Self (16) = 24

        // Act
        var combined = RunRulesFlag.Executed | RunRulesFlag.Self;

        // Assert
        Assert.AreEqual(expected, (int)combined);
    }

    #endregion

    #region Edge Case Tests

    [TestMethod]
    public void None_CastToInt_ReturnsZero()
    {
        // Arrange & Act
        var value = (int)RunRulesFlag.None;

        // Assert
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void IntZero_CastToFlag_ReturnsNone()
    {
        // Arrange & Act
        var flag = (RunRulesFlag)0;

        // Assert
        Assert.AreEqual(RunRulesFlag.None, flag);
    }

    [TestMethod]
    public void IntValue_CastToFlag_ReturnsCorrectFlag()
    {
        // Arrange & Act & Assert
        Assert.AreEqual(RunRulesFlag.NoMessages, (RunRulesFlag)1);
        Assert.AreEqual(RunRulesFlag.Messages, (RunRulesFlag)2);
        Assert.AreEqual(RunRulesFlag.NotExecuted, (RunRulesFlag)4);
        Assert.AreEqual(RunRulesFlag.Executed, (RunRulesFlag)8);
        Assert.AreEqual(RunRulesFlag.Self, (RunRulesFlag)16);
        Assert.AreEqual(RunRulesFlag.All, (RunRulesFlag)31);
    }

    [TestMethod]
    public void InvalidIntValue_CastToFlag_ReturnsCombinedRepresentation()
    {
        // Arrange - 3 is not a defined value but is valid as NoMessages | Messages
        var intValue = 3;

        // Act
        var flag = (RunRulesFlag)intValue;

        // Assert
        Assert.IsTrue(flag.HasFlag(RunRulesFlag.NoMessages));
        Assert.IsTrue(flag.HasFlag(RunRulesFlag.Messages));
        Assert.AreEqual(RunRulesFlag.NoMessages | RunRulesFlag.Messages, flag);
    }

    [TestMethod]
    public void FlagEquality_SameValue_AreEqual()
    {
        // Arrange
        var flag1 = RunRulesFlag.NoMessages | RunRulesFlag.Messages;
        var flag2 = RunRulesFlag.NoMessages | RunRulesFlag.Messages;

        // Act & Assert
        Assert.AreEqual(flag1, flag2);
        Assert.IsTrue(flag1 == flag2);
    }

    [TestMethod]
    public void FlagInequality_DifferentValue_AreNotEqual()
    {
        // Arrange
        var flag1 = RunRulesFlag.NoMessages | RunRulesFlag.Messages;
        var flag2 = RunRulesFlag.NoMessages | RunRulesFlag.Executed;

        // Act & Assert
        Assert.AreNotEqual(flag1, flag2);
        Assert.IsTrue(flag1 != flag2);
    }

    #endregion

    #region Flag Checking Helper Pattern Tests

    [TestMethod]
    public void CheckingFlagWithBitwiseAnd_EquivalentToHasFlag()
    {
        // Arrange
        var combined = RunRulesFlag.NoMessages | RunRulesFlag.Messages | RunRulesFlag.Executed;

        // Act
        var hasFlagResult = combined.HasFlag(RunRulesFlag.Messages);
        var bitwiseCheckResult = (combined & RunRulesFlag.Messages) == RunRulesFlag.Messages;

        // Assert
        Assert.AreEqual(hasFlagResult, bitwiseCheckResult);
    }

    [TestMethod]
    public void CheckingFlagWithBitwiseAnd_NonZeroCheck_EquivalentToHasFlag()
    {
        // Arrange
        var combined = RunRulesFlag.NoMessages | RunRulesFlag.Messages | RunRulesFlag.Executed;

        // Act
        var hasFlagResult = combined.HasFlag(RunRulesFlag.Messages);
        var bitwiseCheckResult = (combined & RunRulesFlag.Messages) != RunRulesFlag.None;

        // Assert
        Assert.AreEqual(hasFlagResult, bitwiseCheckResult);
    }

    #endregion
}
