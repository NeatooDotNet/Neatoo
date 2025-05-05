using Neatoo.RemoteFactory;
using Neatoo.Rules;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo
{


    public interface IValidateBase : IBase, IValidateMetaProperties, INotifyPropertyChanged, INotifyNeatooPropertyChanged
    {
        /// <summary>
        /// Pause events, rules and ismodified
        /// Only affects the Setter method
        /// Not SetProperty or LoadProperty
        /// </summary>
        bool IsPaused { get; }

        internal string? ObjectInvalid { get; }

        new IValidateProperty GetProperty(string propertyName);

        new IValidateProperty this[string propertyName] { get => GetProperty(propertyName); }

        bool TryGetProperty(string propertyName, out IValidateProperty validateProperty);
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

        public IReadOnlyCollection<IPropertyMessage> PropertyMessages => PropertyManager.PropertyMessages;

        protected (bool IsValid, bool IsSelfValid, bool IsBusy) MetaState { get; private set; }

        protected override void CheckIfMetaPropertiesChanged()
        {
            base.CheckIfMetaPropertiesChanged();

            if (MetaState.IsValid != IsValid)
            {
                RaisePropertyChanged(nameof(IsValid));
                RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsValid), this));
            }
            if (MetaState.IsSelfValid != IsSelfValid)
            {
                RaisePropertyChanged(nameof(IsSelfValid));
                RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsSelfValid), this));
            }
            if (MetaState.IsBusy != IsBusy)
            {
                RaisePropertyChanged(nameof(IsBusy));
                RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(nameof(IsBusy), this));
            }

            ResetMetaState();
        }

        protected virtual void ResetMetaState()
        {
            MetaState = (IsValid, IsSelfValid, IsBusy);
        }

        protected override async Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
        {
            if (!IsPaused)
            {
                await RunRules(eventArgs.FullPropertyName);

                await base.ChildNeatooPropertyChanged(eventArgs);

                CheckIfMetaPropertiesChanged();
            }
            else
            {
                ResetMetaState();
            }
        }

        protected override void RaisePropertyChanged(string propertyName)
        {
            if (!IsPaused)
            {
                base.RaisePropertyChanged(propertyName);
            }
        }

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

        public bool TryGetProperty(string propertyName, out IValidateProperty validateProperty)
        {
            if (PropertyManager.HasProperty(propertyName))
            {
                validateProperty = PropertyManager[propertyName];
                return true;
            }
            validateProperty = null!;
            return false;
        }

        new public IValidateProperty this[string propertyName] { get => GetProperty(propertyName); }

        public bool IsPaused { get; protected set; }

        private class Paused : IDisposable
        {
            private readonly ValidateBase<T> _validateBase;
            public Paused(ValidateBase<T> validateBase)
            {
                _validateBase = validateBase;
            }
            public void Dispose()
            {
                _validateBase.ResumeAllActions();
            }
        }

        public virtual IDisposable PauseAllActions()
        {
            if (!IsPaused)
            {
                IsPaused = true;
                PropertyManager.PauseAllActions();
            }

            return new Paused(this);
        }

        public virtual void ResumeAllActions()
        {
            if (IsPaused)
            {
                IsPaused = false;
                PropertyManager.ResumeAllActions();
                ResetMetaState();
            }
        }

        public virtual Task PostPortalConstruct()
        {
            return Task.CompletedTask;
        }

        public virtual Task RunRules(string propertyName, CancellationToken? token = null)
        {
            var task = RuleManager.RunRules(propertyName, token);

            CheckIfMetaPropertiesChanged();

            return task;
        }

        public virtual async Task RunRules(RunRulesFlag runRules = Neatoo.RunRulesFlag.All, CancellationToken? token = null)
        {
            if (runRules == Neatoo.RunRulesFlag.All)
            {
                ClearAllMessages();
            }

            if ((runRules | Neatoo.RunRulesFlag.Self) != Neatoo.RunRulesFlag.Self)
            {
                await PropertyManager.RunRules(runRules, token);
            }

            await RuleManager.RunRules(runRules, token);
            await RunningTasks.AllDone;

            //this.AddAsyncMethod((t) => PropertyManager.RunRules(token));
            // TODO - This isn't raising the 'IsValid' property changed event
            //await base.WaitForTasks();
            if (this.Parent == null)
            {
                Debug.Assert(!IsBusy, "Should not be busy after running all rules");
            }
        }

        public virtual void ClearSelfMessages()
        {
            this[nameof(ObjectInvalid)].ClearAllMessages();
            PropertyManager.ClearSelfMessages();
        }

        public virtual void ClearAllMessages()
        {
            this[nameof(ObjectInvalid)].ClearAllMessages();
            PropertyManager.ClearAllMessages();
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