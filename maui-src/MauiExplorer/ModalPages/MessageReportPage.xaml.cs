using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;

namespace MauiTestTree;

public partial class MessageReportPage : ContentPage
{
    protected MessageReportViewModel _viewModel = new();

    public MessageReportPage(ObservableCollection<StoredPrint>? linkToPrints = null)
    {
        InitializeComponent();
        _viewModel.Prints = linkToPrints;
        BindingContext = _viewModel;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}

//
// View model
//

public class MessageReportViewModel : INotifyPropertyChanged
{
    //
    // INotifyPropertyChanged
    // 

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // 
    // Members
    //

    // idea: directly link to LogInstance without deferral
    public ObservableCollection<StoredPrint>? Prints { get; set; }

    public string Caption { get; set; } = "Log messages";

    // Bindable
    public bool WrapLinesOn
    {
        get => _wrapLines;
        set { if (_wrapLines == value) return; _wrapLines = value; 
              OnPropertyChanged(); OnPropertyChanged(nameof(WrapLinesMode)); }
    }
    protected bool _wrapLines = true;

    public LineBreakMode WrapLinesMode { get => _wrapLines ? LineBreakMode.WordWrap : LineBreakMode.NoWrap; }
}

//
// Converter
//

public class LogColorToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!(value is StoredPrint.Color col))
            return Colors.Gray;
        if (col == StoredPrint.Color.Black)
            return Colors.Black;
        if (col == StoredPrint.Color.Red)
            return Colors.Red;
        if (col == StoredPrint.Color.Yellow)
            return Colors.Orange;
        if (col == StoredPrint.Color.Blue)
            return Colors.Blue;
        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
