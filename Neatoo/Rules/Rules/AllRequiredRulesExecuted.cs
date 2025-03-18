namespace Neatoo.Rules.Rules
{
    public interface IAllRequiredRulesExecuted : IRule<IValidateBase>
    {
        delegate IAllRequiredRulesExecuted Factory(IEnumerable<IRequiredRule> requiredRules);
    }

    internal class AllRequiredRulesExecuted : RuleBase<IValidateBase>, IAllRequiredRulesExecuted
    {
        private readonly IEnumerable<IRequiredRule> requiredRules;

        public AllRequiredRulesExecuted(IEnumerable<IRequiredRule> requiredRules) : base()
        {
            requiredRules.ToList().ForEach(r => TriggerProperties.AddRange(r.TriggerProperties));
            this.requiredRules = requiredRules;
        }

        public override PropertyErrors Execute(IValidateBase target)
        {
            List<string> propertyNames = new List<string>();
            List<ITriggerProperty> triggerProperties = new List<ITriggerProperty>();
            foreach (var rule in requiredRules)
            {
                if (rule is IRequiredRule requiredRule)
                {
                    if (!requiredRule.Executed)
                    {
                        propertyNames.AddRange(requiredRule.TriggerProperties.Select(tp => tp.PropertyName));
                        triggerProperties.AddRange(requiredRule.TriggerProperties);
                    }
                }
            }

            if(propertyNames.Count > 0)
            {
                return new PropertyError(nameof(IValidateBase.ObjectInvalid), "Required properties not set: " + string.Join(", ", propertyNames));
            }

            return PropertyErrors.None;
        }
    }
}
