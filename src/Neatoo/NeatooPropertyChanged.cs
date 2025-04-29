namespace Neatoo;

public interface INotifyNeatooPropertyChanged
{
    event NeatooPropertyChanged NeatooPropertyChanged;
}

public delegate Task NeatooPropertyChanged(NeatooPropertyChangedEventArgs propertyNameBreadCrumbs);

