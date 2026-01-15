using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;

namespace MauiTestTree;

public partial class MenuPickerPage : ContentPage
{
    protected MenuPickerViewModel _viewModel { get; set; } = new();

    public string? SelectedHeader { get => _viewModel.SelectedItem?.Header; }
    
    public MenuPickerPage(MenuPickerViewModel? preset = null)
    {
        InitializeComponent();
        if (preset == null)
            _viewModel.FillDemoData();
        else
            _viewModel = preset;
        BindingContext = _viewModel;
        SetSelectEnabled(false);
    }

    private TaskCompletionSource<string?>? _tcs;

    public Task<string?> MauiShowPageAsync(INavigation navigation)
    {
        _tcs = new ();
        navigation.PushModalAsync(this);
        return _tcs.Task;
    }

    private void SetSelectEnabled(bool enable)
    {
        //if (!enable)
        //{
        //    DumpButton.TextColor = Color.FromUint((DumpButton.TextColor.ToUint() & 0x00ffffff) | 0x40000000);
        //    DumpButton.BorderColor = Color.FromUint((DumpButton.BorderColor.ToUint() & 0x00ffffff) | 0x40000000);
        //}
        //else
        //{
        //    DumpButton.TextColor = Color.FromUint((DumpButton.TextColor.ToUint() & 0x00ffffff) | 0xff000000);
        //    DumpButton.BorderColor = Color.FromUint((DumpButton.BorderColor.ToUint() & 0x00ffffff) | 0xff000000);
        //}
    }

    private async void OnOuterButtonClicked(object sender, EventArgs e)
    {
        await Task.Yield();
        if (sender == CancelButton)
        {
            _viewModel.SelectedItem = null;
            _tcs?.TrySetResult(_viewModel.SelectedItem?.Header);
        }

        if (sender == DumpButton && _viewModel.SelectedItem != null)
        {
            _tcs?.TrySetResult(_viewModel.SelectedItem?.Header);
        }
    }

    private void ItemsView_ItemTapped(object sender, TappedEventArgs e)
    {
        if (!(sender is Grid g && g.BindingContext is MenuPickerItem mpi))
            return;

        _viewModel.SelectedItem = mpi;

        foreach (var it in _viewModel.Items)
            it.IsSelected = it == _viewModel.SelectedItem;

        SetSelectEnabled(true);
    }

    private void DumpButton_Clicked(object sender, EventArgs e)
    {
        ;
    }
}

//
// View model
//

public class MenuPickerViewModel
{
    /// <summary>
    /// If set, title for the dialog panel
    /// </summary>
    public string DialogHeader { get; set; } = "Select a menu option";

    /// <summary>
    /// Root item for the tree
    /// </summary>
    // public MenuPickerItem RootItem { get; set; } = new();

    public ObservableCollection<MenuPickerItem> Items { get; set; } = new();

    /// <summary>
    /// Currently selected item
    /// </summary>
    public MenuPickerItem? SelectedItem;

    public void FillDemoData()
    {
        Items.Add(new MenuPickerItem()
        {
            Header = "New",
            HelpText = "Create new AASX package."
        });
        Items.Add(new MenuPickerItem()
        {
            Header = "Open",
            HelpText = "Open (local) existing AASX package.",
            IsSelected = false
        });
        Items.Add(new MenuPickerItem()
        {
            Header = "Query open repositories/ registries …",
            HelpText = "Selects and repository item (AASX) from the open AASX file repositories."
        });
        Items.Add(new MenuPickerItem()
        {
            Header = "Import …",
            HelpText = "Import options",
            IsSubMenu = true
        });
        Items.Add(new MenuPickerItem()
        {
            Header = "Import further AASX file into AASX …",
            HelpText = "Import AASX file(s) with entities to overall AAS environment."
        });
    }

    public void AddFrom(AasxMenuItemBase mib, bool omitRoot = false)
    {
        // access
        if (mib is not AasxMenuItem menu)
            return;

        // treat it as simple item, first
        var mpi = new MenuPickerItem()
        {
            Header = menu.Header,
            HelpText = menu.HelpText,
            IsCheckable = menu.IsCheckable,
            IsChecked = menu.IsChecked
        };
        if (!omitRoot)
            Items.Add(mpi);

        // has childs or not?
        if (menu.Childs != null && menu.Childs.Count() >= 1 )
        {
            mpi.IsSubMenu = true;
            foreach (var mic in menu.Childs)
                AddFrom(mic);
        }
    }
}

//
// Menu items
//

public class MenuPickerItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Displayed header in GUI based applications.
    /// </summary>
    public string Header { get; set; } = "";

    /// <summary>
    /// Can be switched to checked or not
    /// </summary>
    public bool IsCheckable { get; set; } = false;

    /// <summary>
    /// Switch   state to initailize with.
    /// </summary>
    public bool IsChecked { get; set; } = false;

    /// <summary>
    /// Help text or description in command line applications.
    /// </summary>
    public string HelpText { get; set; } = "";

    /// <summary>
    /// Is currently selected
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }
    bool _isSelected = false;

    /// <summary>
    /// True if it is the header of a sub section
    /// </summary>
    public bool IsSubMenu { get; set; } = false;

    /// <summary>
    /// Sub menues
    /// </summary>
    // public ObservableCollection<MenuPickerItem> Children { get; set; } = new ObservableCollection<MenuPickerItem>();

    /// <summary>
    /// Menues with Children are not selectable.
    /// </summary>
    // public bool IsSelectable { get { return Children.Count() < 1; } }

    public bool DisplayCheckOn { get => IsCheckable && IsChecked; }
    public bool DisplayCheckOff { get => IsCheckable && !IsChecked; }
}