using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxIntegrationBase;

namespace MauiTestTree;

public partial class MenuPickerPage : ContentPage
{
    public string? SelectedOption { get; private set; }

    public MenuPickerViewModel ViewModel { get; set; } = new();

    public MenuPickerPage(MenuPickerViewModel? preset = null)
    {
        InitializeComponent();
        if (preset == null)
            ViewModel.FillDemoData();
        else
            ViewModel = preset;
        BindingContext = ViewModel;
        SetSelectEnabled(false);
    }

    private void SetSelectEnabled(bool enable)
    {
        if (!enable)
        {
            SelectButton.TextColor = Color.FromUint((SelectButton.TextColor.ToUint() & 0x00ffffff) | 0x40000000);
            SelectButton.BorderColor = Color.FromUint((SelectButton.BorderColor.ToUint() & 0x00ffffff) | 0x40000000);
        }
        else
        {
            SelectButton.TextColor = Color.FromUint((SelectButton.TextColor.ToUint() & 0x00ffffff) | 0xff000000);
            SelectButton.BorderColor = Color.FromUint((SelectButton.BorderColor.ToUint() & 0x00ffffff) | 0xff000000);
        }
    }

    private async void OnOptionClicked(object sender, EventArgs e)
    {
        SelectedOption = ((Button)sender).Text;
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        SelectedOption = null;
        await Navigation.PopModalAsync();
    }

    private void ItemsView_ItemTapped(object sender, TappedEventArgs e)
    {
        if (!(sender is Grid g && g.BindingContext is MenuPickerItem mpi))
            return;

        ViewModel.SelectedItem = mpi;

        foreach (var it in ViewModel.Items)
            it.IsSelected = it == ViewModel.SelectedItem;

        SetSelectEnabled(true);
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
    public object? SelectedItem;

    public void FillDemoData()
    {
        //RootItem.Children.Add(new MenuPickerItem() { 
        //    Header = "New", 
        //    HelpText = "Create new AASX package."
        //});
        //RootItem.Children.Add(new MenuPickerItem()
        //{
        //    Header = "Open",
        //    HelpText = "Open (local) existing AASX package."
        //});
        //RootItem.Children.Add(new MenuPickerItem()
        //{
        //    Header = "Query open repositories/ registries …",
        //    HelpText = "Selects and repository item (AASX) from the open AASX file repositories."
        //});
        //RootItem.Children.Add(new MenuPickerItem()
        //{
        //    Header = "Import …",
        //    HelpText = "Import options",
        //    Children = new ObservableCollection<MenuPickerItem>((new[] { 
        //        new MenuPickerItem()
        //        {
        //            Header = "Import further AASX file into AASX …",
        //            HelpText = "Import AASX file(s) with entities to overall AAS environment."
        //        }
        //    }).AsEnumerable())
        //});

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