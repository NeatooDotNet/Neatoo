using Caliburn.Micro;
using HorseBarn.lib;
using HorseBarn.lib.Horse;
using Neatoo;
using System.Windows;

namespace HorseBarn.WPF.ViewModels;

public class CreateHorseViewModel : Caliburn.Micro.Screen
{
    public delegate CreateHorseViewModel Factory(IReadOnlyCollection<string> horseNames);

    public CreateHorseViewModel(IHorseCriteriaFactory horseCriteriaPortal,
        IEventAggregator eventAggregator,
        IReadOnlyCollection<string> horseNames)
    {
        HorseCriteriaPortal = horseCriteriaPortal;
        this.eventAggregator = eventAggregator;
        this.HorseNames = horseNames;
    }

    public IHorseCriteriaFactory HorseCriteriaPortal { get; }

    private readonly IEventAggregator eventAggregator;
    private readonly IReadOnlyCollection<string> HorseNames;

    public IHorseCriteria HorseCriteria { get; private set; }

    public List<Breed> Breeds => Enum.GetValues<Breed>().ToList();

    protected override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        HorseCriteria = HorseCriteriaPortal.Fetch(HorseNames);
        HorseCriteria.Breed = Breed.Thoroughbred;
        NotifyOfPropertyChange(nameof(HorseCriteria));
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task create()
    {
        await HorseCriteria.RunAllRules();

        if(!HorseCriteria.IsValid)
        {
            MessageBox.Show("Invalid Horse Criteria");
            return;
        }
        await eventAggregator.PublishOnUIThreadAsync(HorseCriteria);
        await this.TryCloseAsync();
    }

    public async Task cancel()
    {
        this.HorseCriteria = null;
        await this.TryCloseAsync();
    }
}
