using System.Windows;
using System.Windows.Media;

namespace HorseBarn.WPF.Views;

/// <summary>
/// Interaction logic for HorseBarnView.xaml
/// </summary>
public partial class HorseBarnView : Window
{
    public HorseBarnView()
    {
        InitializeComponent();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        var accentBrush = TryFindResource("AccentColorBrush") as SolidColorBrush;
        if (accentBrush != null) accentBrush.Color.CreateAccentColors();
    }
}
