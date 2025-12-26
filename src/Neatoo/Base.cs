using Neatoo.Internal;
using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Neatoo;

/// <summary>
/// Defines the base interface for all Neatoo domain objects.
/// </summary>
/// <remarks>
/// <see cref="IBase"/> provides the foundation for property management, parent-child relationships,
/// property change notifications, and asynchronous task tracking. All Neatoo business objects
/// implement this interface either directly or through derived interfaces.
/// </remarks>
public interface IBase : INeatooObject, INotifyPropertyChanged, INotifyNeatooPropertyChanged, IBaseMetaProperties
{
    /// <summary>
    /// Gets the parent object in the object graph hierarchy.
    /// </summary>
    /// <value>
    /// The parent <see cref="IBase"/> object, or <c>null</c> if this is a root object.
    /// </value>
    IBase? Parent { get; }

    /// <summary>
    /// Adds a child task to be tracked for completion.
    /// </summary>
    /// <param name="task">The task to track.</param>
    internal void AddChildTask(Task task);

    /// <summary>
    /// Gets the property with the specified name.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IProperty"/> instance for the specified property.</returns>
    internal IProperty GetProperty(string propertyName);

    /// <summary>
    /// Gets the property with the specified name using indexer syntax.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IProperty"/> instance for the specified property.</returns>
    internal IProperty this[string propertyName] => this.GetProperty(propertyName);

    /// <summary>
    /// Gets the property manager responsible for managing all properties on this object.
    /// </summary>
    internal IPropertyManager<IProperty> PropertyManager { get; }
}

/// <summary>
/// The foundational abstract base class for all Neatoo domain objects.
/// </summary>
/// <typeparam name="T">The concrete type deriving from this base class, used for the curiously recurring template pattern (CRTP).</typeparam>
/// <remarks>
/// <para>
/// <see cref="Base{T}"/> provides the core infrastructure for Neatoo domain objects including:
/// </para>
/// <list type="bullet">
/// <item><description>Property management through <see cref="IPropertyManager{IProperty}"/></description></item>
/// <item><description>Parent-child relationship tracking in object graphs</description></item>
/// <item><description>Property change notification via <see cref="INotifyPropertyChanged"/></description></item>
/// <item><description>Asynchronous task tracking for property setters and rules</description></item>
/// </list>
/// <para>
/// This class uses the Factory pattern for instantiation. Direct construction is not recommended;
/// use the generated factory methods instead.
/// </para>
/// </remarks>
[Factory]
public abstract class Base<T> : INeatooObject, IBase, ISetParent, IJsonOnDeserialized
    where T : Base<T>
{
    /// <summary>
    /// Gets the task tracker for managing asynchronous operations on this object.
    /// </summary>
    /// <remarks>
    /// Used internally to track property setter tasks and ensure all async operations complete
    /// before marking the object as not busy.
    /// </remarks>
    protected AsyncTasks RunningTasks { get; private set; } = new AsyncTasks();

    /// <summary>
    /// Gets or sets the property manager responsible for managing all properties on this object.
    /// </summary>
    /// <remarks>
    /// The property manager handles property value storage, change notifications, and busy state tracking.
    /// </remarks>
    protected IPropertyManager<IProperty> PropertyManager { get; set; }

    /// <summary>
    /// Gets the property manager for interface implementation.
    /// </summary>
    IPropertyManager<IProperty> IBase.PropertyManager => this.PropertyManager;

    /// <summary>
    /// Gets the parent object in the object graph hierarchy.
    /// </summary>
    /// <value>The parent <see cref="IBase"/> object, or <c>null</c> if this is a root object.</value>
    public IBase? Parent { get; protected set; }

    /// <summary>
    /// Gets the property with the specified name.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IProperty"/> instance for the specified property.</returns>
    protected IProperty this[string propertyName] { get => this.GetProperty(propertyName); }

    /// <summary>
    /// Gets a value indicating whether the object has asynchronous operations in progress.
    /// </summary>
    /// <value><c>true</c> if async tasks are running or any property is busy; otherwise, <c>false</c>.</value>
    public bool IsBusy => this.RunningTasks.IsRunning || this.PropertyManager.IsBusy;

    /// <summary>
    /// Initializes a new instance of the <see cref="Base{T}"/> class.
    /// </summary>
    /// <param name="services">The base services containing the property manager and other dependencies.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or its PropertyManager is null.</exception>
    public Base(IBaseServices<T> services)
    {
        this.PropertyManager = services.PropertyManager ?? throw new ArgumentNullException("PropertyManager");

        if (this.PropertyManager is IPropertyManager<IProperty>)
        {
            this.PropertyManager.NeatooPropertyChanged += this._PropertyManager_NeatooPropertyChanged;
            this.PropertyManager.PropertyChanged += this._PropertyManager_PropertyChanged;
        }

        this.RunningTasks.OnFullSequenceComplete = () =>
        {
            this.CheckIfMetaPropertiesChanged();
            return Task.CompletedTask;
        };

    }

    /// <summary>
    /// Sets the parent object for this instance in the object graph hierarchy.
    /// </summary>
    /// <param name="parent">The parent object, or <c>null</c> to make this a root object.</param>
    /// <remarks>
    /// This method is called automatically when an object is assigned to a property of another Neatoo object.
    /// Override this method to add custom parent assignment logic.
    /// </remarks>
    protected virtual void SetParent(IBase? parent)
    {
        this.Parent = parent;
    }

    /// <summary>
    /// Explicit interface implementation for setting the parent object.
    /// </summary>
    /// <param name="parent">The parent object, or <c>null</c> to make this a root object.</param>
    void ISetParent.SetParent(IBase? parent)
    {
        this.SetParent(parent);
    }

    /// <summary>
    /// Checks if any meta properties (such as IsBusy) have changed and raises appropriate notifications.
    /// </summary>
    /// <remarks>
    /// Override this method in derived classes to add additional meta property change detection.
    /// Always call the base implementation when overriding.
    /// </remarks>
    protected virtual void CheckIfMetaPropertiesChanged()
    {

    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void RaisePropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the <see cref="NeatooPropertyChanged"/> event with the specified event arguments.
    /// </summary>
    /// <param name="eventArgs">The event arguments containing property change details.</param>
    /// <returns>A task representing the asynchronous event handling operation.</returns>
    protected virtual Task RaiseNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return this.NeatooPropertyChanged?.Invoke(eventArgs) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Handles property change notifications from child objects in the object graph.
    /// </summary>
    /// <param name="eventArgs">The event arguments containing the child property change details.</param>
    /// <returns>A task representing the asynchronous event handling operation.</returns>
    /// <remarks>
    /// This method wraps the child event in a new event args that includes this object as the source,
    /// allowing the event to bubble up through the object graph hierarchy.
    /// </remarks>
    protected virtual Task ChildNeatooPropertyChanged(NeatooPropertyChangedEventArgs eventArgs)
    {
        return this.RaiseNeatooPropertyChanged(new NeatooPropertyChangedEventArgs(eventArgs.Property!, this, eventArgs.InnerEventArgs));
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
            this.RaisePropertyChanged(eventArgs.FullPropertyName);
        }

        return this.ChildNeatooPropertyChanged(eventArgs);
    }
    private void _PropertyManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.CheckIfMetaPropertiesChanged();
    }

    /// <summary>
    /// Gets the value of the specified property.
    /// </summary>
    /// <typeparam name="P">The type of the property value.</typeparam>
    /// <param name="propertyName">The name of the property. Automatically populated by the compiler when called from a property getter.</param>
    /// <returns>The property value, or <c>default</c> if the property is not set.</returns>
    /// <remarks>
    /// Use this method in property getters to retrieve values from the property manager.
    /// The property name is automatically captured from the calling member.
    /// </remarks>
    /// <example>
    /// <code>
    /// public string Name { get => Getter&lt;string&gt;(); set => Setter(value); }
    /// </code>
    /// </example>
    protected virtual P? Getter<P>([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        return (P?)this.PropertyManager[propertyName]?.Value;
    }

    /// <summary>
    /// Sets the value of the specified property.
    /// </summary>
    /// <typeparam name="P">The type of the property value.</typeparam>
    /// <param name="value">The new value for the property.</param>
    /// <param name="propertyName">The name of the property. Automatically populated by the compiler when called from a property setter.</param>
    /// <remarks>
    /// <para>
    /// Use this method in property setters to store values through the property manager.
    /// The property name is automatically captured from the calling member.
    /// </para>
    /// <para>
    /// Setting a property value may trigger asynchronous operations such as validation rules.
    /// These tasks are automatically tracked and can be awaited via <see cref="WaitForTasks"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="AggregateException">Thrown if the property setter task fails.</exception>
    /// <example>
    /// <code>
    /// public string Name { get => Getter&lt;string&gt;(); set => Setter(value); }
    /// </code>
    /// </example>
    protected virtual void Setter<P>(P? value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        var task = this.PropertyManager[propertyName].SetPrivateValue(value);

        if (!task.IsCompleted)
        {
            if (this.Parent != null)
            {
                this.Parent.AddChildTask(task);
            }

            this.RunningTasks.AddTask(task);
        }

        if (task.Exception != null)
        {
            throw task.Exception;
        }
    }

    /// <summary>
    /// Adds a child task to be tracked for completion.
    /// </summary>
    /// <param name="task">The task to track.</param>
    /// <remarks>
    /// Tasks added through this method are propagated up the object graph hierarchy
    /// to ensure the root object can await all pending operations.
    /// </remarks>
    public virtual void AddChildTask(Task task)
    {
        // This has the effect of only running one task per object graph
        // BUT if I don't do this I have to loop in WaitForTask on IsBusy...
        // Not sure which is better
        //AsyncTaskSequencer.AddTask((t) => task);
        //Parent?.AddChildTask(task);

        if (this.Parent != null)
        {
            this.Parent.AddChildTask(task);
        }

        this.RunningTasks.AddTask(task);
    }

    /// <summary>
    /// Gets the property with the specified name.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The <see cref="IProperty"/> instance for the specified property.</returns>
    public IProperty GetProperty(string propertyName)
    {
        return this.PropertyManager[propertyName];
    }

    /// <summary>
    /// Called after JSON deserialization to restore event subscriptions and parent-child relationships.
    /// </summary>
    /// <remarks>
    /// This method reattaches event handlers to the property manager and re-establishes
    /// parent references for all child objects. Override to add custom deserialization logic.
    /// </remarks>
    public virtual void OnDeserialized()
    {
        this.PropertyManager.NeatooPropertyChanged += this._PropertyManager_NeatooPropertyChanged;
        this.PropertyManager.PropertyChanged += this._PropertyManager_PropertyChanged;


        foreach (var property in this.PropertyManager.GetProperties)
        {
            if (property.Value is ISetParent setParent)
            {
                setParent.SetParent(this);
            }
        }
    }

    /// <summary>
    /// Waits for all asynchronous tasks on this object to complete.
    /// </summary>
    /// <returns>A task that completes when all pending operations have finished.</returns>
    /// <remarks>
    /// <para>
    /// This method should be called after setting properties to ensure all asynchronous
    /// operations (such as validation rules) have completed before proceeding.
    /// </para>
    /// <para>
    /// When called on a root object (Parent is null), this method asserts that the object
    /// is no longer busy after all tasks complete.
    /// </para>
    /// </remarks>
    public virtual async Task WaitForTasks()
    {
        // I don't like this...
        //while (IsBusy)
        //{
        //    await PropertyManager.WaitForTasks();
        //    await AsyncTaskSequencer.AllDone;
        //}

        //await PropertyManager.WaitForTasks();
        await this.RunningTasks.AllDone;

        if (this.Parent == null)
        {
            if (this.IsBusy)
            {

                var busyProperty = this.PropertyManager.GetProperties.FirstOrDefault(p => p.IsBusy);

            }

            // Raise Errors
            Debug.Assert(!this.IsBusy, "Should not be busy after running all rules");
        }
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    /// <remarks>
    /// This event follows the standard <see cref="INotifyPropertyChanged"/> pattern
    /// for data binding scenarios.
    /// </remarks>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Occurs when a Neatoo property changes, providing extended event information.
    /// </summary>
    /// <remarks>
    /// This event provides richer property change information than <see cref="PropertyChanged"/>,
    /// including support for async event handlers and nested property change tracking.
    /// </remarks>
    public event NeatooPropertyChanged? NeatooPropertyChanged;
}
