using System.ComponentModel;

namespace Neatoo;

public interface IPropertyManager<out P> : INotifyNeatooPropertyChanged, INotifyPropertyChanged
    where P : IProperty
{
    bool IsBusy { get; }
    Task WaitForTasks();
    bool HasProperty(string propertyName);
    P GetProperty(string propertyName);
    public P? this[string propertyName] { get => GetProperty(propertyName); }
    internal IPropertyInfoList PropertyInfoList { get; }
    internal IEnumerable<P> GetProperties { get; }
    void SetProperties(IEnumerable<IProperty> properties);
}




