using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo;

public interface IBase : INeatooObject, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IBaseMetaProperties
{
    IBase? Parent { get; }
    internal void AddChildTask(Task task);
    internal IProperty GetProperty(string propertyName);
    internal IProperty this[string propertyName] { get => GetProperty(propertyName); }
    internal IPropertyManager<IProperty> PropertyManager { get; }
}

[Factory]
public abstract class Base<T> : INeatooObject, IBase, ISetParent, IJsonOnDeserialized
    where T : Base<T>
{
    // Fields
    protected AsyncTasks RunningTasks { get; private set; } = new AsyncTasks();
    // Properties
    protected IPropertyManager<IProperty> PropertyManager { get; set; }
    IPropertyManager<IProperty> IBase.PropertyManager => PropertyManager;
    public IBase? Parent { get; protected set; }
    protected IProperty this[string propertyName] { get => GetProperty(propertyName); }
    public bool IsBusy => RunningTasks.IsRunning || PropertyManager.IsBusy;

    // Constructors
    public Base(IBaseServices<T> services)
    {
        PropertyManager = services.PropertyManager ?? throw new ArgumentNullException("PropertyManager");

        if (PropertyManager is IPropertyManager<IProperty>)
        {
            PropertyManager.NeatooPropertyChanged += _PropertyManager_NeatooPropertyChanged;
            PropertyManager.PropertyChanged += _PropertyManager_PropertyChanged;
        }

        RunningTasks.OnFullSequenceComplete = () =>
        {
            CheckIfMetaPropertiesChanged();
            return Task.CompletedTask;
        };

    }

    // Methods
    protected virtual void SetParent(IBase? parent)
    {
        Parent = parent;
    }

    void ISetParent.SetParent(IBase? parent)
    {
        SetParent(parent);
    }

    protected virtual void CheckIfMetaPropertiesChanged()
    {

    }

    protected virtual void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual Task RaiseNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;
    }

    protected virtual Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(eventArgs.Property!, this, eventArgs.InnerEventArgs));
    }

    private Task _PropertyManager_NeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.Property != null && eventArgs.Property == eventArgs.Source) // One of this object's properties
        {
            if (eventArgs.Property.Value is ISetParent child)
            {
                child.SetParent(this);
            }
            
            // This isn't meant to go to parent Neatoo objects, thru the tree, just immediate outside listeners
            RaisePropertyChanged(eventArgs.FullPropertyName);
        }

        return ChildNeatooPropertyChanged(eventArgs);
    }
    private void _PropertyManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        CheckIfMetaPropertiesChanged();
    }

    protected virtual P? Getter<P>([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        return (P?) PropertyManager[propertyName]?.Value;
    }
    
    protected virtual void Setter<P>(P? value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        var task = PropertyManager[propertyName].SetPrivateValue(value);

        if (!task.IsCompleted)
        {
            if (Parent != null)
            {
                Parent.AddChildTask(task);
            }

            RunningTasks.AddTask(task);
        }

        if(task.Exception != null)
        {
            throw task.Exception;
        }
    }

    public virtual void AddChildTask(Task task)
    {
        // This has the effect of only running one task per object graph
        // BUT if I don't do this I have to loop in WaitForTask on IsBusy...
        // Not sure which is better
        //AsyncTaskSequencer.AddTask((t) => task);
        //Parent?.AddChildTask(task);

        if (Parent != null)
        {
            Parent.AddChildTask(task);
        } 

        RunningTasks.AddTask(task);
    }

    public IProperty GetProperty(string propertyName)
    {
        return PropertyManager[propertyName];
    }

    public virtual void OnDeserialized()
    {
        PropertyManager.NeatooPropertyChanged += _PropertyManager_NeatooPropertyChanged;
        PropertyManager.PropertyChanged += _PropertyManager_PropertyChanged;


        foreach (var property in PropertyManager.GetProperties)
        {
            if (property.Value is ISetParent setParent)
            {
                setParent.SetParent(this);
            }
        }
    }

    public virtual async Task WaitForTasks()
    {
        // I don't like this...
        //while (IsBusy)
        //{
        //    await PropertyManager.WaitForTasks();
        //    await AsyncTaskSequencer.AllDone;
        //}

        //await PropertyManager.WaitForTasks();
        await RunningTasks.AllDone;

        if (Parent == null)
        {
            if (IsBusy)
            {

                var busyProperty = PropertyManager.GetProperties.FirstOrDefault(p => p.IsBusy);

            }

            // Raise Errors
            Debug.Assert(!IsBusy, "Should not be busy after running all rules");
        }
    }

    // Events
    public event PropertyChangedEventHandler? PropertyChanged;
    public event NeatooPropertyChanged? NeatooPropertyChanged;
}