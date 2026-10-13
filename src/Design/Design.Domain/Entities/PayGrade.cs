// -----------------------------------------------------------------------------
// Design.Domain - A Value Object Is a Record
// -----------------------------------------------------------------------------
// THE QUESTION THE HABIT ASKS: "It's a value object, so ValidateBase?"
//
// No. A value object is a record: immutable, compared by value, no identity,
// no change tracking, no rules. ValidateBase is for an input model - something
// a person edits and that validates as they type. If a value needs rules, it
// is not a value object.
//
// The constructor may throw. PayGrade is a closed set the system assigns
// (hiring, promotion); an unknown code reaching this constructor is a
// programming error, which is what an exception is for. A value a person
// types (an email address on a form) is not a value object - it is a property
// on the input model with a rule, so the person sees a message, not a crash.
//
// No [Factory]: construction needs no service and no authorization. A record
// takes [Factory] only when it does.
// -----------------------------------------------------------------------------

namespace Design.Domain.Entities;

#region skill-value-object-record
/// <summary>
/// Value object for an employee's pay grade: one of a closed set of codes,
/// each with a salary band.
/// </summary>
public sealed record PayGrade
{
    private static readonly IReadOnlyDictionary<string, (decimal Min, decimal Max)> Bands =
        new Dictionary<string, (decimal, decimal)>
        {
            ["A"] = (30_000m, 45_000m),
            ["B"] = (45_000m, 65_000m),
            ["C"] = (65_000m, 90_000m),
            ["D"] = (90_000m, 130_000m),
        };

    public PayGrade(string code)
    {
        // Bad input is an application failure, not a validation message:
        // nobody types a grade; code assigns one from the set.
        if (!Bands.ContainsKey(code))
        {
            throw new ArgumentException($"Unknown pay grade '{code}'", nameof(code));
        }

        Code = code;
    }

    public string Code { get; }

    public decimal MinSalary => Bands[Code].Min;
    public decimal MaxSalary => Bands[Code].Max;

    public bool Covers(decimal salary) => salary >= MinSalary && salary <= MaxSalary;

    /// <summary>The grade every new hire starts at.</summary>
    public static PayGrade Entry => new("A");

    /// <summary>Every grade, in order: what a select offers.</summary>
    public static IReadOnlyList<PayGrade> All { get; } = Bands.Keys.Order().Select(code => new PayGrade(code)).ToList();

    public override string ToString() => Code;
}
#endregion
