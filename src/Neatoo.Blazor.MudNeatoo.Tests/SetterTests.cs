using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;
using MudBlazor.Services;
using Neatoo.Blazor.MudNeatoo.Components;

namespace Neatoo.Blazor.MudNeatoo.Tests;

/// <summary>
/// Every MudNeatoo input writes through its <c>Setter</c> when one is given, and through
/// <c>EntityProperty.SetValue</c> otherwise. Each test renders the component and raises the
/// wrapped MudBlazor component's own change callback, as a user's change does.
/// </summary>
[TestClass]
public sealed class SetterTests : IAsyncDisposable
{
    private BunitContext _context = null!;
    private SetterTarget _target = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _context = new BunitContext();
        _context.Services.AddMudServices();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        // The picker, select and autocomplete inputs open a popover, which needs its provider.
        _context.Render<MudPopoverProvider>();
        _target = new SetterTarget();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private IEntityProperty Property(string name) => _target[name];

    private static Task<IEnumerable<string>> Search(string? text, CancellationToken token) =>
        Task.FromResult<IEnumerable<string>>(new[] { "Warm-up" });

    /// <summary>Renders the input, then raises the wrapped component's change callback with the value.</summary>
    private async Task Change<TComponent, TInner, TValue>(
        Action<ComponentParameterCollectionBuilder<TComponent>> parameters,
        Func<TInner, EventCallback<TValue>> changed,
        TValue value)
        where TComponent : class, IComponent
        where TInner : IComponent
    {
        var rendered = _context.Render<TComponent>(parameters);
        var inner = rendered.FindComponent<TInner>().Instance;
        await rendered.InvokeAsync(() => changed(inner).InvokeAsync(value));
    }

    // ---- With a Setter: the setter receives the value; the property is not written ----

    [TestMethod]
    public async Task Slider_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        int? received = null;
        await Change<MudNeatooSlider<int>, MudSlider<int>, int>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Minutes)))
                  .Add(c => c.Min, 0).Add(c => c.Max, 100)
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, 30);

        Assert.AreEqual(30, received);
        Assert.AreEqual(0, _target.Minutes, "the setter owns the write");
    }

    [TestMethod]
    public async Task Slider_AwaitsItsSetter()
    {
        var release = new TaskCompletionSource();
        var rendered = _context.Render<MudNeatooSlider<int>>(p => p
            .Add(c => c.EntityProperty, Property(nameof(SetterTarget.Minutes)))
            .Add(c => c.Min, 0).Add(c => c.Max, 100)
            .Add(c => c.Setter, async v => { await release.Task; await _target.SetMinutes(v); }));
        var slider = rendered.FindComponent<MudSlider<int>>().Instance;

        var change = rendered.InvokeAsync(() => slider.ValueChanged.InvokeAsync(30));
        Assert.IsFalse(change.IsCompleted, "the change waits for the setter");

        release.SetResult();
        await change;
        Assert.AreEqual(30, _target.Minutes);
    }

    [TestMethod]
    public async Task NumericField_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        int? received = null;
        await Change<MudNeatooNumericField<int>, MudNumericField<int>, int>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Minutes)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, 30);

        Assert.AreEqual(30, received);
        Assert.AreEqual(0, _target.Minutes);
    }

    [TestMethod]
    public async Task TextField_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        string? received = null;
        await Change<MudNeatooTextField<string>, MudTextField<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, "Warm-up");

        Assert.AreEqual("Warm-up", received);
        Assert.IsNull(_target.Name);
    }

    [TestMethod]
    public async Task Autocomplete_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        string? received = null;
        await Change<MudNeatooAutocomplete<string>, MudAutocomplete<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name)))
                  .Add(c => c.SearchFunc, Search)
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, "Warm-up");

        Assert.AreEqual("Warm-up", received);
        Assert.IsNull(_target.Name);
    }

    [TestMethod]
    public async Task Select_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        string? received = null;
        await Change<MudNeatooSelect<string>, MudSelect<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, "Warm-up");

        Assert.AreEqual("Warm-up", received);
        Assert.IsNull(_target.Name);
    }

    [TestMethod]
    public async Task RadioGroup_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        string? received = null;
        await Change<MudNeatooRadioGroup<string>, MudRadioGroup<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, "Warm-up");

        Assert.AreEqual("Warm-up", received);
        Assert.IsNull(_target.Name);
    }

    [TestMethod]
    public async Task CheckBox_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        bool? received = null;
        await Change<MudNeatooCheckBox<bool>, MudCheckBox<bool>, bool>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Locked)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, true);

        Assert.AreEqual(true, received);
        Assert.IsFalse(_target.Locked);
    }

    [TestMethod]
    public async Task Switch_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        bool? received = null;
        await Change<MudNeatooSwitch<bool>, MudSwitch<bool>, bool>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Locked)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.ValueChanged, true);

        Assert.AreEqual(true, received);
        Assert.IsFalse(_target.Locked);
    }

    [TestMethod]
    public async Task DatePicker_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        DateTime? received = null;
        var date = new DateTime(2026, 10, 16);
        await Change<MudNeatooDatePicker, MudDatePicker, DateTime?>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.When)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.DateChanged, date);

        Assert.AreEqual(date, received);
        Assert.IsNull(_target.When);
    }

    [TestMethod]
    public async Task TimePicker_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        TimeSpan? received = null;
        var time = new TimeSpan(9, 30, 0);
        await Change<MudNeatooTimePicker, MudTimePicker, TimeSpan?>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.At)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.TimeChanged, time);

        Assert.AreEqual(time, received);
        Assert.IsNull(_target.At);
    }

    [TestMethod]
    public async Task DateRangePicker_WithASetter_CallsIt_AndLeavesTheWriteToIt()
    {
        DateRange? received = null;
        var range = new DateRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 16));
        await Change<MudNeatooDateRangePicker, MudDateRangePicker, DateRange?>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Range)))
                  .Add(c => c.Setter, v => { received = v; return Task.CompletedTask; }),
            inner => inner.DateRangeChanged, range);

        Assert.AreSame(range, received);
        Assert.IsNull(_target.Range);
    }

    // ---- With a Setter: the change waits for the setter to finish ----

    /// <summary>Renders the input with a setter that waits on a gate, and asserts the change waits too.</summary>
    private async Task AssertTheChangeAwaitsTheSetter<TComponent, TInner, TValue>(
        Action<ComponentParameterCollectionBuilder<TComponent>, Func<TValue, Task>> parameters,
        Func<TInner, EventCallback<TValue>> changed,
        TValue value)
        where TComponent : class, IComponent
        where TInner : IComponent
    {
        var release = new TaskCompletionSource();
        var finished = false;
        Func<TValue, Task> setter = async _ => { await release.Task; finished = true; };

        var rendered = _context.Render<TComponent>(p => parameters(p, setter));
        var inner = rendered.FindComponent<TInner>().Instance;
        var change = rendered.InvokeAsync(() => changed(inner).InvokeAsync(value));
        Assert.IsFalse(change.IsCompleted, "the change waits for the setter");

        release.SetResult();
        await change;
        Assert.IsTrue(finished);
    }

    [TestMethod]
    public Task NumericField_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooNumericField<int>, MudNumericField<int>, int>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Minutes))).Add(c => c.Setter, s),
            inner => inner.ValueChanged, 30);

    [TestMethod]
    public Task TextField_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooTextField<string>, MudTextField<string>, string>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name))).Add(c => c.Setter, s),
            inner => inner.ValueChanged, "Warm-up");

    [TestMethod]
    public Task Autocomplete_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooAutocomplete<string>, MudAutocomplete<string>, string>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name))).Add(c => c.SearchFunc, Search).Add(c => c.Setter, s),
            inner => inner.ValueChanged, "Warm-up");

    [TestMethod]
    public Task Select_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooSelect<string>, MudSelect<string>, string>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name))).Add(c => c.Setter, s),
            inner => inner.ValueChanged, "Warm-up");

    [TestMethod]
    public Task RadioGroup_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooRadioGroup<string>, MudRadioGroup<string>, string>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name))).Add(c => c.Setter, s),
            inner => inner.ValueChanged, "Warm-up");

    [TestMethod]
    public Task CheckBox_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooCheckBox<bool>, MudCheckBox<bool>, bool>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Locked))).Add(c => c.Setter, s),
            inner => inner.ValueChanged, true);

    [TestMethod]
    public Task Switch_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooSwitch<bool>, MudSwitch<bool>, bool>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Locked))).Add(c => c.Setter, s),
            inner => inner.ValueChanged, true);

    [TestMethod]
    public Task DatePicker_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooDatePicker, MudDatePicker, DateTime?>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.When))).Add(c => c.Setter, s),
            inner => inner.DateChanged, new DateTime(2026, 10, 16));

    [TestMethod]
    public Task TimePicker_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooTimePicker, MudTimePicker, TimeSpan?>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.At))).Add(c => c.Setter, s),
            inner => inner.TimeChanged, new TimeSpan(9, 30, 0));

    [TestMethod]
    public Task DateRangePicker_AwaitsItsSetter() =>
        AssertTheChangeAwaitsTheSetter<MudNeatooDateRangePicker, MudDateRangePicker, DateRange?>(
            (p, s) => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Range))).Add(c => c.Setter, s),
            inner => inner.DateRangeChanged, new DateRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 16)));

    // ---- Without a Setter: the property is written, as before ----

    [TestMethod]
    public async Task Slider_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooSlider<int>, MudSlider<int>, int>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Minutes))).Add(c => c.Min, 0).Add(c => c.Max, 100),
            inner => inner.ValueChanged, 30);
        Assert.AreEqual(30, _target.Minutes);
    }

    [TestMethod]
    public async Task NumericField_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooNumericField<int>, MudNumericField<int>, int>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Minutes))),
            inner => inner.ValueChanged, 30);
        Assert.AreEqual(30, _target.Minutes);
    }

    [TestMethod]
    public async Task TextField_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooTextField<string>, MudTextField<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name))),
            inner => inner.ValueChanged, "Warm-up");
        Assert.AreEqual("Warm-up", _target.Name);
    }

    [TestMethod]
    public async Task Autocomplete_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooAutocomplete<string>, MudAutocomplete<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name)))
                  .Add(c => c.SearchFunc, Search),
            inner => inner.ValueChanged, "Warm-up");
        Assert.AreEqual("Warm-up", _target.Name);
    }

    [TestMethod]
    public async Task Select_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooSelect<string>, MudSelect<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name))),
            inner => inner.ValueChanged, "Warm-up");
        Assert.AreEqual("Warm-up", _target.Name);
    }

    [TestMethod]
    public async Task RadioGroup_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooRadioGroup<string>, MudRadioGroup<string>, string>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Name))),
            inner => inner.ValueChanged, "Warm-up");
        Assert.AreEqual("Warm-up", _target.Name);
    }

    [TestMethod]
    public async Task CheckBox_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooCheckBox<bool>, MudCheckBox<bool>, bool>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Locked))),
            inner => inner.ValueChanged, true);
        Assert.IsTrue(_target.Locked);
    }

    [TestMethod]
    public async Task Switch_WithoutASetter_WritesTheProperty()
    {
        await Change<MudNeatooSwitch<bool>, MudSwitch<bool>, bool>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Locked))),
            inner => inner.ValueChanged, true);
        Assert.IsTrue(_target.Locked);
    }

    [TestMethod]
    public async Task DatePicker_WithoutASetter_WritesTheProperty()
    {
        var date = new DateTime(2026, 10, 16);
        await Change<MudNeatooDatePicker, MudDatePicker, DateTime?>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.When))),
            inner => inner.DateChanged, date);
        Assert.AreEqual(date, _target.When);
    }

    [TestMethod]
    public async Task TimePicker_WithoutASetter_WritesTheProperty()
    {
        var time = new TimeSpan(9, 30, 0);
        await Change<MudNeatooTimePicker, MudTimePicker, TimeSpan?>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.At))),
            inner => inner.TimeChanged, time);
        Assert.AreEqual(time, _target.At);
    }

    [TestMethod]
    public async Task DateRangePicker_WithoutASetter_WritesTheProperty()
    {
        var range = new DateRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 16));
        await Change<MudNeatooDateRangePicker, MudDateRangePicker, DateRange?>(
            p => p.Add(c => c.EntityProperty, Property(nameof(SetterTarget.Range))),
            inner => inner.DateRangeChanged, range);
        Assert.AreSame(range, _target.Range);
    }
}
