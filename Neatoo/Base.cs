using Neatoo.Core;
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
    protected AsyncTasks AsyncTaskSequencer { get; private set; } = new AsyncTasks();
    // Properties
    protected IPropertyManager<IProperty> PropertyManager { get; set; }
    IPropertyManager<IProperty> IBase.PropertyManager => PropertyManager;
    public IBase? Parent { get; protected set; }
    protected IProperty this[string propertyName] { get => GetProperty(propertyName); }
    public bool IsSelfBusy => !AsyncTaskSequencer.AllDone.IsCompleted || PropertyManager.IsSelfBusy;
    public bool IsBusy => IsSelfBusy || PropertyManager.IsBusy;

    // Constructors
    public Base(IBaseServices<T> services)
    {
        PropertyManager = services.PropertyManager ?? throw new ArgumentNullException("PropertyManager");

        if (PropertyManager is IPropertyManager<IProperty>)
        {
            PropertyManager.NeatooPropertyChanged += _PropertyManager_NeatooPropertyChanged;
            PropertyManager.PropertyChanged += _PropertyManager_PropertyChanged;
        }

        AsyncTaskSequencer.OnFullSequenceComplete = () =>
        {
            CheckIfMetaPropertiesChanged(true);
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

    protected virtual void CheckIfMetaPropertiesChanged(bool raiseBusy = true)
    {

    }

    protected virtual void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual Task RaiseNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        Debug.Assert(PropertyManager.GetProperties.Any(p => p.Name == breadCrumbs.PropertyName), "Property not found");

        // This is for databinding only - not other Neatoo objects
        return NeatooPropertyChanged?.Invoke(new PropertyChangedBreadCrumbs(breadCrumbs.PropertyName, this, breadCrumbs.InnerBreadCrumb)) ?? Task.CompletedTask;
    }

    protected virtual Task ChildNeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {
        return RaiseNeatooPropertyChanged(breadCrumbs);
    }

    private Task _PropertyManager_NeatooPropertyChanged(PropertyChangedBreadCrumbs breadCrumbs)
    {

        if (breadCrumbs.Source is IProperty property) // This can be clearer
        {
            if (this[property.Name].Value is ISetParent child)
            {
                child.SetParent(this);
            }

            // This isn't meant to go to parent Neatoo objects, thru the tree, just immediate outside listeners
            RaisePropertyChanged(breadCrumbs.FullPropertyName);
        }

        return ChildNeatooPropertyChanged(breadCrumbs);
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

            AsyncTaskSequencer.AddTask(task);
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

        AsyncTaskSequencer.AddTask(task);
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
        await AsyncTaskSequencer.AllDone;

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