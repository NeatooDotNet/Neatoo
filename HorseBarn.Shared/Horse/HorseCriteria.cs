using Neatoo;
using Neatoo.AuthorizationRules;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel.DataAnnotations;

namespace HorseBarn.lib.Horse;

public interface IHorseCriteria : IValidateBase
{
    string? Name { get; set; }
    DateOnly? BirthDay { get; set; }
    Breed Breed { get; set; }
}

[Factory]
internal class HorseCriteria : ValidateBase<HorseCriteria>, IHorseCriteria
{
    public HorseCriteria(IValidateBaseServices<HorseCriteria> services,
                        IHorseNameUniqueRule horseNameUniqueRule,
                        IAllRequiredRulesExecuted.Factory allRequiredRulesExecutedFactory) : base(services)
    {
        RuleManager.AddRule(horseNameUniqueRule);
        RuleManager.AddValidation(static (t) =>
        {
            if (t.HorseNames.Contains(t.Name))
            {
                return $"Name must be unique. HorseBarn already contains {t.Name}";
            }
            return string.Empty;
        }, t => t.Name);
        var allRequiredRulesExecuted = allRequiredRulesExecutedFactory(RuleManager.Rules.OfType<IRequiredRule>());
        RuleManager.AddRule(allRequiredRulesExecuted);
        allRequiredRulesExecuted.RunRule(this).Wait();
    }

    [Required]
    public string? Name { get => Getter<string>(); set => Setter(value); }

    [Required]
    public Breed Breed { get => Getter<Breed>(); set => Setter(value); }

    [Required]
    public DateOnly? BirthDay { get => Getter<DateOnly?>(); set => Setter(value); }

    private IReadOnlyCollection<string> HorseNames { get; set; } = [];

    [Fetch]
    public void Fetch()
    {
    }

    [Fetch]
    public void Fetch(IEnumerable<string> horseNames)
    {
        HorseNames = horseNames.ToList();
    }

}
