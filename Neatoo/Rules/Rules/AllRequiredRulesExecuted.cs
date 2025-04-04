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
            RuleOrder = int.MaxValue;
        }

        public override void OnRuleAdded(IRuleManager ruleManager, uint uniqueIndex)
        {
            base.OnRuleAdded(ruleManager, uniqueIndex);
            requiredRules = ruleManager.Rules.OfType<IRequiredRule>();

            foreach (var rule in requiredRules)
            {
                TriggerProperties.AddRange(rule.TriggerProperties);
            }

            ruleManager.RunRule(this);
        }

        protected override IRuleMessages Execute(IValidateBase target)
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
                return (nameof(IValidateBase.ObjectInvalid), "Required properties not set: " + string.Join(", ", propertyNames)).AsRuleMessages();
            }

            return RuleMessages.None;
        }
    }
}
