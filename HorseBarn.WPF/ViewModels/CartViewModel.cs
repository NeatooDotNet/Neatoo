using Caliburn.Micro;
using HorseBarn.lib;
using HorseBarn.lib.Cart;
using System.Collections.Specialized;
using System.Windows;

namespace HorseBarn.WPF.ViewModels;




public class CartViewModel : PropertyChangedBase
{

    public delegate CartViewModel Factory(IHorseBarn horseBarn, ICart cart);

    public CartViewModel(IHorseBarn horseBarn, ICart cart, HorseViewModel.Factory horseViewModelFactory)
    {
        this.horseBarn = horseBarn;
        Cart = cart;
        this.horseViewModelFactory = horseViewModelFactory;
        cart.PropertyChanged += Cart_PropertyChanged;
        cart.CollectionChanged += Horses_CollectionChanged;
        RebuildHorseViewModels();
    }

    private void Horses_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildHorseViewModels();
    }

    private void Cart_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICart.NumberOfHorses))
        {
            RebuildHorseViewModels();
        }
    }
    private void RebuildHorseViewModels()
    {
        var index = Cart.NumberOfHorses;
        var horses = Cart.Horses.GetEnumerator();
        HorseViewModels.Clear();

        for (int i = 0; i < index; i++)
        {
            var horse = horses.MoveNext();
             if (horse)
            {
                HorseViewModels.Add(horseViewModelFactory(horses.Current));
            }
            else
            {
                HorseViewModels.Add(horseViewModelFactory(null));
            }
        }
    }

    public ICart Cart { get; }

    public IObservableCollection<HorseViewModel> HorseViewModels { get; } = new BindableCollection<HorseViewModel>();
    private readonly IHorseBarn horseBarn;
    private readonly HorseViewModel.Factory horseViewModelFactory;

    public void Plus()
    {
        Cart.NumberOfHorses++;
    }

    public void Minus()
    {
        Cart.NumberOfHorses--;
    }

    public void HandleDragDrop(object source, DragEventArgs e)
    {
        var horseViewModel = e.Data.GetData(typeof(HorseViewModel)) as HorseViewModel;
        if (horseViewModel != null && horseViewModel.Horse != null && Cart.CanAddHorse(horseViewModel.Horse))
        {
            horseBarn.MoveHorseToCart(horseViewModel.Horse, Cart);
        }
    }

    public void HandleDragOver(object source, DragEventArgs e)
    {
        var horseViewModel = e.Data.GetData(typeof(HorseViewModel)) as HorseViewModel;
        if (horseViewModel != null && horseViewModel.Horse != null)
        {
            var canAdd = Cart.CanAddHorse(horseViewModel.Horse);

            if (!canAdd)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }
    }

    //public void HandleDragLeave(object source, DragEventArgs e)
    //{
    //    Debug.WriteLine("Cart Drag Leave");
    //    e.Effects = DragDropEffects.Move;
    //}
}
