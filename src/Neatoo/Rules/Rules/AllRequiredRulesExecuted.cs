namespace Neatoo.Rules.Rules
{
    public interface IAllRequiredRulesExecuted : IRule<IValidateBase>
    {
    }

    public class AllRequiredRulesExecuted : RuleBase<IValidateBase>, IAllRequiredRulesExecuted
    {
        private IEnumerable<IRequiredRule> requiredRules;

        public AllRequiredRulesExecuted()
        {
            this.RuleOrder = int.MaxValue;
        }

        public override void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex)
        {
            base.OnRuleAdded(ruleManager, uniqueIndex);
            this.requiredRules = ruleManager.Rules.OfType<IRequiredRule>();

            foreach (var rule in this.requiredRules)
            {
                this.TriggerProperties.AddRange(rule.TriggerProperties);
            }

            ruleManager.RunRule(this);
        }

        protected override IRuleMessages Execute(IValidateBase target)
        {
            var propertyNames = new List<string>();
            var triggerProperties = new List<ITriggerProperty>();
            foreach (var rule in this.requiredRules)
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
                return (nameof(IValidateBaseInternal.ObjectInvalid), "Required properties not set: " + string.Join(", ", propertyNames)).AsRuleMessages();
            }

            return nameof(IValidateBaseInternal.ObjectInvalid).ClearRuleMessageForProperty();
        }
    }
}
