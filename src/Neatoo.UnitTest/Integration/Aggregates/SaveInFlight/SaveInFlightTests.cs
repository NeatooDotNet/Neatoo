using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neatoo.UnitTest.TestInfrastructure;
using System.ComponentModel;

namespace Neatoo.UnitTest.Integration.Aggregates.SaveInFlight;

// A save in flight is its own state (rulings D39 to D41 in docs/VisionAlignment.md, #103):
// - while Save() runs, IsSavable is false and says so; IsBusy and WaitForTasks() do not change;
// - a second Save() on the same object is refused, so one click sends one operation;
// - after a remote save the instance that was sent is left as it was.

internal static class SaveInFlightTestSupport
{
    public static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    /// <summary>Holds every insert open until the returned gate is completed.</summary>
    public static TaskCompletionSource HoldInserts()
    {
        var gate = new TaskCompletionSource();
        SaveInFlightStore.Gate = () => gate.Task;
        return gate;
    }

    /// <summary>Records the names the object raises PropertyChanged for, with IsSavable's value at that moment.</summary>
    public static List<(string? Name, bool IsSavable)> Record(IEntityRoot target)
    {
        var raised = new List<(string? Name, bool IsSavable)>();
        ((INotifyPropertyChanged)target).PropertyChanged += (_, e) => raised.Add((e.PropertyName, target.IsSavable));
        return raised;
    }

    /// <summary>The values IsSavable had each time the object announced it, in order: "false, true".</summary>
    public static string SavableAnnouncements(this List<(string? Name, bool IsSavable)> raised)
    {
        return string.Join(", ", raised.Where(r => r.Name == nameof(IEntityRoot.IsSavable)).Select(r => r.IsSavable ? "true" : "false"));
    }
}

[TestClass]
public class SaveInFlightRemoteTests : ClientServerTestBase
{
    private static readonly TimeSpan Limit = SaveInFlightTestSupport.Limit;

    private readonly CancellationTokenSource neverCancelled = new();

    private IRemoteDraft draft = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        SaveInFlightStore.Reset();
        InitializeScopes();
        draft = GetClientService<IRemoteDraftFactory>().Create();
        draft.Name = "Ada";

        Assert.IsTrue(draft.IsNew, "Precondition: a created draft is new");
        Assert.IsTrue(draft.IsSavable, "Precondition: the draft is savable before the save starts");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        SaveInFlightStore.Reset();
        neverCancelled.Dispose();
    }

    [TestMethod]
    public Task ASecondSave_WhileTheFirstIsInFlight_IsRefused_AndOneInsertIsSent() => OneThreadAtATime.Run(async () =>
    {
        // The double-click of #103: two Save() calls on the same new aggregate, the second
        // started before the first has returned.
        var gate = SaveInFlightTestSupport.HoldInserts();

        var first = draft.Save();
        Assert.IsFalse(first.IsCompleted, "Precondition: the first save is in flight");
        Assert.AreEqual(1, RemoteCallCount, "Precondition: the first save crossed to the server");

        var refused = await Assert.ThrowsExactlyAsync<SaveOperationException>(() => draft.Save().WaitAsync(Limit));
        Assert.AreEqual(SaveFailureReason.SaveInProgress, refused.Reason);

        gate.SetResult();
        await first.WaitAsync(Limit);

        Assert.AreEqual(1, SaveInFlightStore.InsertsStarted, "one click, one insert");
        Assert.AreEqual(1, RemoteCallCount, "the refused save never crossed to the server");
    });

    [TestMethod]
    public Task ASecondSaveWithAToken_WhileTheFirstIsInFlight_IsRefused() => OneThreadAtATime.Run(async () =>
    {
        var gate = SaveInFlightTestSupport.HoldInserts();

        var first = draft.Save(neverCancelled.Token);
        Assert.IsFalse(first.IsCompleted, "Precondition: the first save is in flight");

        var refused = await Assert.ThrowsExactlyAsync<SaveOperationException>(() => draft.Save(neverCancelled.Token).WaitAsync(Limit));
        Assert.AreEqual(SaveFailureReason.SaveInProgress, refused.Reason);

        gate.SetResult();
        await first.WaitAsync(Limit);

        Assert.AreEqual(1, SaveInFlightStore.InsertsStarted);
    });

    [TestMethod]
    public Task WhileASaveIsInFlight_IsSavableIsFalse_AndTheObjectSaysSo() => OneThreadAtATime.Run(async () =>
    {
        // What a Save button bound to IsSavable needs: the value, and the notification to re-read it.
        var gate = SaveInFlightTestSupport.HoldInserts();
        var raised = SaveInFlightTestSupport.Record(draft);

        var saving = draft.Save();

        Assert.IsFalse(draft.IsSavable, "a save in flight is not savable again");
        Assert.AreEqual("false", raised.SavableAnnouncements(), "IsSavable is announced once, as false, when the save starts");

        gate.SetResult();
        await saving.WaitAsync(Limit);
    });

    [TestMethod]
    public Task WhileASaveIsInFlight_TheObjectIsNotBusy_AndAWaitCompletes() => OneThreadAtATime.Run(async () =>
    {
        // Saving is its own state. Busy stays "the running tasks are running", and a wait is not
        // a wait for the save: an operation body may await the object it is saving.
        var gate = SaveInFlightTestSupport.HoldInserts();

        var saving = draft.Save();
        Assert.IsFalse(saving.IsCompleted, "Precondition: the save is in flight");

        Assert.IsFalse(draft.IsBusy);
        Assert.IsTrue(draft.WaitForTasks().IsCompletedSuccessfully, "nothing to wait for");
        Assert.IsTrue(draft.WaitForTasks(neverCancelled.Token).IsCompletedSuccessfully, "nothing to wait for");

        gate.SetResult();
        await saving.WaitAsync(Limit);
    });

    [TestMethod]
    public Task AfterARemoteSave_TheInstanceThatWasSent_IsLeftAsItWas() => OneThreadAtATime.Run(async () =>
    {
        // The caller gets a different instance back and reassigns it. The one that was sent is
        // stale and unchanged: the framework does not mark it spent.
        var gate = SaveInFlightTestSupport.HoldInserts();
        var raised = SaveInFlightTestSupport.Record(draft);

        var saving = draft.Save();
        gate.SetResult();
        var saved = (IRemoteDraft)await saving.WaitAsync(Limit);

        Assert.AreNotSame(draft, saved, "Precondition: a remote save returns a different instance");
        Assert.IsFalse(saved.IsNew);
        Assert.IsFalse(saved.IsSavable, "the returned instance has nothing to save");

        Assert.IsTrue(draft.IsNew, "the sent instance is still new");
        Assert.IsTrue(draft.IsSavable, "the sent instance is savable again once its save has returned");
        Assert.AreEqual("false, true", raised.SavableAnnouncements(), "not savable while saving, then savable again");
    });

    [TestMethod]
    public Task AfterASaveThatFailed_TheObjectIsSavableAgain_AndASecondAttemptSucceeds() => OneThreadAtATime.Run(async () =>
    {
        var gate = SaveInFlightTestSupport.HoldInserts();
        SaveInFlightStore.FailWith = new InvalidOperationException("the insert failed");
        var raised = SaveInFlightTestSupport.Record(draft);

        var failing = draft.Save();
        Assert.IsFalse(draft.IsSavable, "Precondition: not savable while the save is in flight");
        gate.SetResult();
        await Assert.ThrowsAsync<Exception>(() => failing.WaitAsync(Limit));

        Assert.IsTrue(draft.IsSavable, "the user can fix the cause and save again");
        Assert.AreEqual("false, true", raised.SavableAnnouncements());

        SaveInFlightStore.FailWith = null;
        var saved = (IRemoteDraft)await draft.Save().WaitAsync(Limit);

        Assert.IsFalse(saved.IsNew);
        Assert.AreEqual(2, SaveInFlightStore.InsertsStarted, "one insert per attempt");
    });
}

[TestClass]
public class SaveInFlightLocalTests : IntegrationTestBase
{
    private static readonly TimeSpan Limit = SaveInFlightTestSupport.Limit;

    private ILocalDraft draft = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        SaveInFlightStore.Reset();
        InitializeScope();
        draft = GetRequiredService<ILocalDraftFactory>().Create();
        draft.Name = "Ada";

        Assert.IsTrue(draft.IsNew, "Precondition: a created draft is new");
        Assert.IsTrue(draft.IsSavable, "Precondition: the draft is savable before the save starts");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        SaveInFlightStore.Reset();
    }

    [TestMethod]
    public Task ASecondSave_WhileALocalSaveIsInFlight_IsRefused_AndOneInsertRuns() => OneThreadAtATime.Run(async () =>
    {
        var gate = SaveInFlightTestSupport.HoldInserts();

        var first = draft.Save();
        Assert.IsFalse(first.IsCompleted, "Precondition: the first save is in flight");

        var refused = await Assert.ThrowsExactlyAsync<SaveOperationException>(() => draft.Save().WaitAsync(Limit));
        Assert.AreEqual(SaveFailureReason.SaveInProgress, refused.Reason);

        gate.SetResult();
        await first.WaitAsync(Limit);

        Assert.AreEqual(1, SaveInFlightStore.InsertsStarted, "one click, one insert");
    });

    [TestMethod]
    public Task WhenALocalSaveStarts_IsSavableIsFalse_AndTheObjectSaysSo() => OneThreadAtATime.Run(async () =>
    {
        // A local operation pauses the object it runs on, and a paused object raises nothing.
        // The object has to say it is not savable before the operation starts, or a bound Save
        // button in a single-tier application never hears it.
        var gate = SaveInFlightTestSupport.HoldInserts();
        var raised = SaveInFlightTestSupport.Record(draft);

        var saving = draft.Save();
        Assert.IsFalse(saving.IsCompleted, "Precondition: the save is in flight");

        Assert.IsFalse(draft.IsSavable);
        Assert.AreEqual("false", raised.SavableAnnouncements(), "IsSavable is announced once, as false, when the save starts");

        gate.SetResult();
        await saving.WaitAsync(Limit);
    });

    [TestMethod]
    public Task ALocalSaveWithAnAsyncInsert_Completes_OnTheInstanceThatWasSaved() => OneThreadAtATime.Run(async () =>
    {
        // The factory completes a local save on the instance that was saved, while Save() is
        // still running. Completing marks the object unmodified, which is refused while the
        // object is busy: so a save in flight must not be "busy".
        var gate = SaveInFlightTestSupport.HoldInserts();
        var raised = SaveInFlightTestSupport.Record(draft);

        var saving = draft.Save();
        Assert.IsFalse(draft.IsBusy, "a save in flight is not busy");
        gate.SetResult();
        var saved = await saving.WaitAsync(Limit);

        Assert.AreSame(draft, saved, "Precondition: a local save returns the instance it was given");
        Assert.IsFalse(draft.IsNew);
        Assert.IsFalse(draft.IsModified);
        Assert.IsFalse(draft.IsSavable, "saved: nothing left to save");
        Assert.AreEqual("false", raised.SavableAnnouncements(), "savable does not flicker back to true at the end of the save");
    });

    [TestMethod]
    public Task ASynchronousLocalSave_AnnouncesIsSavableOnce() => OneThreadAtATime.Run(async () =>
    {
        // No gate: the insert finishes before Save() returns its task.
        var raised = SaveInFlightTestSupport.Record(draft);

        var saved = await draft.Save().WaitAsync(Limit);

        Assert.AreSame(draft, saved);
        Assert.IsFalse(draft.IsSavable);
        Assert.AreEqual("false", raised.SavableAnnouncements(), "one announcement for one change of value");
        Assert.AreEqual(1, SaveInFlightStore.InsertsStarted);
    });

    [TestMethod]
    public Task AfterALocalSaveThatFailed_TheObjectIsSavableAgain_AndASecondAttemptSucceeds() => OneThreadAtATime.Run(async () =>
    {
        var gate = SaveInFlightTestSupport.HoldInserts();
        SaveInFlightStore.FailWith = new InvalidOperationException("the insert failed");

        var failing = draft.Save();
        Assert.IsFalse(draft.IsSavable, "Precondition: not savable while the save is in flight");
        gate.SetResult();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => failing.WaitAsync(Limit));

        Assert.IsTrue(draft.IsSavable, "the user can fix the cause and save again");

        SaveInFlightStore.FailWith = null;
        await draft.Save().WaitAsync(Limit);

        Assert.IsFalse(draft.IsNew);
        Assert.AreEqual(2, SaveInFlightStore.InsertsStarted, "one insert per attempt");
    });
}
