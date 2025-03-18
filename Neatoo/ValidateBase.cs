using Neatoo.Core;
using Neatoo.Internal;
using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo
{

    public interface IValidateBase : IBase, IValidateMetaProperties, INotifyPropertyChanged, INotifyNeatooPropertyChanged
    {
        IReadOnlyList<string> BrokenRuleMessages { get; }

        /// <summary>
        /// Pause events, rules and ismodified
        /// Only affects the Setter method
        /// Not SetProperty or LoadProperty
        /// </summary>
        bool IsPaused { get; }

        internal string? ObjectInvalid { get; }

        new IValidateProperty GetProperty(string propertyName);

        new IValidateProperty this[string propertyName] { get => GetProperty(propertyName); }
    }

    [Factory]
    public abstract class ValidateBase<T> : Base<T>, IValidateBase, INotifyPropertyChanged, IJsonOnDeserializing, IJsonOnDeserialized, IFactoryOnStart, IFactoryOnComplete
        where T : ValidateBase<T>
    {
        protected new IValidatePropertyManager<IValidateProperty> PropertyManager => (IValidatePropertyManager<IValidateProperty>)base.PropertyManager;

        protected IRuleManager<T> RuleManager { get; }

        public ValidateBase(IValidateBaseServices<T> services) : base(services)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            this.RuleManager = services.CreateRuleManager((T)(IValidateBase)this);

            this.RuleManager.AddValidation(static (t) =>
            {
                if (!string.IsNullOrEmpty(t.ObjectInvalid))
                {
                    return t.ObjectInvalid;
                }
                return string.Empty;
            }, (t) => t.ObjectInvalid);

            ResetMetaState();
        }

        public bool IsValid => PropertyManager.IsValid;

        public bool IsSelfValid => PropertyManager.IsSelfValid;

        protected (bool IsValid, bool IsSelfValid, bool IsBusy, bool IsSelfBusy) MetaState { get; private set; }

        protected override void CheckIfMetaPropertiesChanged(bool raiseBusy = false)
        {
            base.CheckIfMetaPropertiesChanged();

            if (MetaState.IsValid != IsValid)
            {
                RaisePropertyChanged(nameof(IsValid));
            }
            if (MetaState.IsSelfValid != IsSelfValid)
            {
                RaisePropertyChanged(nameof(IsSelfValid));
            }
            if (raiseBusy && IsSelfBusy || MetaState.IsSelfBusy != IsSelfBusy)
            {
                RaisePropertyChanged(nameof(IsSelfBusy));
            }
            if (raiseBusy && IsBusy || MetaState.IsBusy != IsBusy)
            {
                RaisePropertyChanged(nameof(IsBusy));
            }

            ResetMetaState();
        }

        protected virtual void ResetMetaState()
        {
            MetaState = (IsValid, IsSelfValid, IsBusy, IsSelfBusy);
        }

        protected override async Task ChildNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
        {
            await base.ChildNeatooPropertyChanged(breadCrumbs);

            if (!IsPaused)
            {
                await CheckRules(breadCrumbs.FullPropertyName);
            }

            CheckIfMetaPropertiesChanged();
        }

        public IReadOnlyList<string> BrokenRuleMessages => PropertyManager.ErrorMessages;

        /// <summary>
        /// Permanently mark invalid
        /// Running all rules will reset this
        /// </summary>
        /// <param name="message"></param>
        protected virtual void MarkInvalid(string message)
        {
            ObjectInvalid = message;
            CheckIfMetaPropertiesChanged();
        }

        public string? ObjectInvalid { get => Getter<string>(); protected set => Setter(value); }

        new public IValidateProperty GetProperty(string propertyName)
        {
            return PropertyManager[propertyName];
        }

        new public IValidateProperty this[string propertyName] { get => GetProperty(propertyName); }

        public bool IsPaused { get; protected set; }

        public virtual void PauseAllActions()
        {
            if (!IsPaused)
            {
                IsPaused = true;
            }
        }

        public virtual void ResumeAllActions()
        {
            if (IsPaused)
            {
                IsPaused = false;
                ResetMetaState();
            }
        }

        public virtual Task PostPortalConstruct()
        {
            return Task.CompletedTask;
        }

        public virtual Task CheckRules(string propertyName)
        {
            var task = RuleManager.CheckRulesForProperty(propertyName);

            CheckIfMetaPropertiesChanged();

            return task;
        }

        public virtual async Task RunSelfRules(CancellationToken? token = null)
        {
            this[nameof(ObjectInvalid)].ClearAllErrors();

            await RuleManager.CheckAllRules(token);
            await AsyncTaskSequencer.AllDone;
        }

        public virtual async Task RunAllRules(CancellationToken? token = null)
        {
            ClearAllErrors();

            await PropertyManager.RunAllRules(token);
            await RuleManager.CheckAllRules(token);
            await AsyncTaskSequencer.AllDone;

            //this.AddAsyncMethod((t) => PropertyManager.RunAllRules(token));
            // TODO - This isn't raising the 'IsValid' property changed event
            //await base.WaitForTasks();
            if (this.Parent == null)
            {
                Debug.Assert(!IsBusy, "Should not be busy after running all rules");
            }
        }

        public virtual void ClearSelfErrors()
        {
            this[nameof(ObjectInvalid)].ClearAllErrors();
            PropertyManager.ClearSelfErrors();
        }

        public virtual void ClearAllErrors()
        {
            this[nameof(ObjectInvalid)].ClearAllErrors();
            PropertyManager.ClearAllErrors();
        }

        IValidateProperty IValidateBase.GetProperty(string propertyName)
        {
            return GetProperty(propertyName);
        }

        public void OnDeserializing()
        {
            PauseAllActions();
        }

        override public void OnDeserialized()
        {
            base.OnDeserialized();
            ResumeAllActions();
        }

        public virtual void FactoryStart(FactoryOperation factoryOperation)
        {
            PauseAllActions();
        }

        public virtual void FactoryComplete(FactoryOperation factoryOperation)
        {
            ResumeAllActions();
        }
    }
}


[Serializable]
public class AddRulesNotDefinedException<T> : Exception
{
    public AddRulesNotDefinedException() : base($"AddRules not defined for {typeof(T).Name}") { }
    public AddRulesNotDefinedException(string message) : base(message) { }
    public AddRulesNotDefinedException(string message, Exception inner) : base(message, inner) { }
}