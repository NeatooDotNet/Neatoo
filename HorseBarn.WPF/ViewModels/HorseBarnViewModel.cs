using Caliburn.Micro;
using HorseBarn.lib;
using HorseBarn.lib.Cart;
using HorseBarn.lib.Horse;
using Neatoo;
using System.Collections.Specialized;
using System.Windows;

namespace HorseBarn.WPF.ViewModels;

public class HorseBarnViewModel : Screen, IHandle<IHorseCriteria>
{
    private readonly IWindowManager windowManager;
    private readonly IHorseBarnFactory horseBarnFactory;
    private readonly IEventAggregator eventAggregator;
    private readonly CreateHorseViewModel.Factory createHorseViewModel;
    private readonly CartViewModel.Factory createCartViewModel;
    private readonly HorseViewModel.Factory horseViewModelFactory;
    private readonly IHorseCriteriaFactory horseCriteriaPortal;

    public HorseBarnViewModel(IWindowManager windowManager,
        IHorseBarnFactory horseBarnFactory,
        IEventAggregator eventAggregator,
        CreateHorseViewModel.Factory createHorseViewModel,
        CartViewModel.Factory createCartViewModel,
        HorseViewModel.Factory horseViewModelFactory,
        IHorseCriteriaFactory horseCriteriaPortal)
    {
        this.windowManager = windowManager;
        this.horseBarnFactory = horseBarnFactory;
        this.eventAggregator = eventAggregator;
        this.createHorseViewModel = createHorseViewModel;
        this.createCartViewModel = createCartViewModel;
        this.horseViewModelFactory = horseViewModelFactory;
        this.horseCriteriaPortal = horseCriteriaPortal;
        eventAggregator.SubscribeOnPublishedThread(this);
    }

    public IHorseBarn HorseBarn { get; private set; }
    public IObservableCollection<HorseViewModel> PastureHorses { get; private set; }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken);

        HorseBarn = await horseBarnFactory.Fetch();

        // TODO - Improve fetch when the object is not found
        // how does CSLA handle this?
        if (HorseBarn.Pasture is null)
        {
            HorseBarn = horseBarnFactory.Create();

            var horseCriteria = horseCriteriaPortal.Fetch();
            horseCriteria.Name = "Secretariat";
            horseCriteria.Breed = Breed.Clydesdale;
            horseCriteria.BirthDay = new DateOnly(2010, 1, 1);

            HorseBarn.AddNewHorse(horseCriteria);

            horseCriteria = horseCriteriaPortal.Fetch();
            horseCriteria.Name = "Seattle Slew";
            horseCriteria.Breed = Breed.Clydesdale;
            horseCriteria.BirthDay = new DateOnly(2000, 1, 1);

            HorseBarn.AddNewHorse(horseCriteria);

            horseCriteria = horseCriteriaPortal.Fetch();
            horseCriteria.Name = "Speedy";
            horseCriteria.Breed = Breed.Thoroughbred;
            horseCriteria.BirthDay = new DateOnly(2010, 1, 1);

            HorseBarn.AddNewHorse(horseCriteria);

            horseCriteria = horseCriteriaPortal.Fetch();
            horseCriteria.Name = "Flash";
            horseCriteria.Breed = Breed.Mustang;
            horseCriteria.BirthDay = new DateOnly(2015, 1, 1);

            HorseBarn.AddNewHorse(horseCriteria);


            ICart cart = await HorseBarn.AddRacingChariot();
            cart.NumberOfHorses = 2;
            cart.Name = "Racing Chariot A";

            cart = await HorseBarn.AddWagon();
            cart.NumberOfHorses = 2;
            cart.Name = "Wagon A";

            await HorseBarn.RunAllRules();

            HorseBarn = (IHorseBarn) await HorseBarn.Save();
        }

        LoadHorseBarn();

    }

    private void LoadHorseBarn()
    {
        Horses_CollectionChanged(this, null);
        HorseBarn.Pasture.Horses.CollectionChanged += Horses_CollectionChanged;

        HorseBarn.Carts.CollectionChanged += Carts_CollectionChanged;
        Carts_CollectionChanged(null, null);

        NotifyOfPropertyChange(nameof(HorseBarn));
    }

    private void Carts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Carts = new BindableCollection<CartViewModel>(HorseBarn.Carts.Select(c => createCartViewModel(HorseBarn, c)));
        NotifyOfPropertyChange(nameof(Carts));
    }

    private void Horses_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PastureHorses = new BindableCollection<HorseViewModel>(HorseBarn.Pasture.Horses.Select(h => horseViewModelFactory(h)));
        NotifyOfPropertyChange(nameof(PastureHorses));
    }

    public Task AddHorse()
    {
        return windowManager.ShowDialogAsync(createHorseViewModel(HorseBarn.Horses.Select(h => h.Name).ToList()));
    }

    public Task HandleAsync(IHorseCriteria message, CancellationToken cancellationToken)
    {
        HorseBarn.AddNewHorse(message);
        return Task.CompletedTask;
    }

    public IObservableCollection<CartViewModel> Carts { get; set; } = new BindableCollection<CartViewModel>();

    public async Task AddRacingChariot()
    {
        Carts.Add(createCartViewModel(HorseBarn, await HorseBarn.AddRacingChariot()));
    }

    public async Task AddWagon()
    {
        Carts.Add(createCartViewModel(HorseBarn, await HorseBarn.AddWagon()));
    }

    public void HandleDragDrop(object source, DragEventArgs e)
    {
        var horseViewModel = e.Data.GetData(typeof(HorseViewModel)) as HorseViewModel;
        if (horseViewModel != null && horseViewModel.Horse != null)
        {
            HorseBarn.MoveHorseToPasture(horseViewModel.Horse);
        }
    }

    public async void Save()
    {
        HorseBarn.Pasture.Horses.CollectionChanged -= Horses_CollectionChanged;
        HorseBarn.Carts.CollectionChanged -= Carts_CollectionChanged;
        HorseBarn = (IHorseBarn) await HorseBarn.Save();
        LoadHorseBarn();
    }

    //public void HandleDragOver(object source, DragEventArgs e)
    //{
    //    Debug.WriteLine("HorseBarn HandleDragOver");

    //    var horseViewModel = e.Data.GetData(typeof(HorseViewModel)) as HorseViewModel;
    //    if (horseViewModel != null && horseViewModel.Horse != null)
    //    {
    //        e.Effects = DragDropEffects.Move;
    //    }
    //    else
    //    {
    //        e.Effects = DragDropEffects.None;
    //    }
    //}
}
