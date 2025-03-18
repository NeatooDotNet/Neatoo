using Caliburn.Micro;
using HorseBarn.lib.Horse;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;

namespace HorseBarn.WPF.ViewModels;

public class HorseViewModel : PropertyChangedBase
{
    public delegate HorseViewModel Factory(IHorse? horse);

    public HorseViewModel()
    {
    }

    public HorseViewModel(IHorse? horse)
    {
        Horse = horse;
    }

    private IHorse? _horse;

    public IHorse? Horse
    {
        get => _horse;
        set
        {
            _horse = value;
            NotifyOfPropertyChange();
        }
    }

    public void HandleMouseMove(object dataContext, object source, MouseEventArgs e)
    {
        HorseViewModel? horse = dataContext as HorseViewModel;
        if (horse?.Horse != null && e.LeftButton == MouseButtonState.Pressed)
        {
            Debug.WriteLine("Horse DoDragDrop");
            DragDrop.DoDragDrop((FrameworkElement)source, horse, DragDropEffects.Move | DragDropEffects.None);
        }
    }

}
