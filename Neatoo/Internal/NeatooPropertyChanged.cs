namespace Neatoo.Core;

public interface INotifyNeatooPropertyChanged
{
    event NeatooPropertyChanged NeatooPropertyChanged;
}

public delegate Task NeatooPropertyChanged(PropertyChangedBreadCrumbs propertyNameBreadCrumbs);

