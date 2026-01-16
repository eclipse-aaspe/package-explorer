using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;

namespace MauiTestTree
{
    /// <summary>
    /// Single menu picker item, to be used in MAUI menu substitute for mobilde devices
    /// </summary>
    public class MenuPickerItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Name to look up the item in the menu structure
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Displayed header in GUI based applications.
        /// </summary>
        public string Header { get; set; } = "";

        /// <summary>
        /// Header without underscores for accelerators
        /// </summary>
        public string DisplayHeader { get => Header.Replace("_", ""); }

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

    /// <summary>
    /// The overall view model for mobile menu substitutes
    /// </summary>
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

        /// <summary>
        /// Add from a AasxMenu
        /// </summary>
        /// <returns>Number of added children</returns>
        public int AddFrom(AasxMenuItemBase mib, 
            bool omitRoot = false, 
            bool showDeprecated = true,
            AasxMenuFilter filterMask = AasxMenuFilter.All)
        {
            // access
            if (mib is not AasxMenuItem menu)
                return 0;
            int res = 0;

            // treat it as simple item, first
            var mpi = new MenuPickerItem()
            {
                Name = menu.Name,
                Header = menu.Header,
                HelpText = menu.HelpText,
                IsCheckable = menu.IsCheckable,
                IsChecked = menu.IsChecked
            };

            if (mib.Name == "FileRepoNew")
                Trace.WriteLine("FRN");

            if (menu.Childs != null && menu.Childs.Count() >= 1)
                mpi.IsSubMenu = true;

            var toAdd = !omitRoot;
            if (!showDeprecated && mib is AasxMenuItem mi && mi.Deprecated)
                toAdd = false;
            if (!mpi.IsSubMenu && ((int) mib.Filter & (int) filterMask) < 1)
                toAdd = false;
            if (toAdd)
            {
                Items.Add(mpi);
                res++;
            }

            // has childs or not?
            if (mpi.IsSubMenu)
            {
                mpi.IsSubMenu = true;
                foreach (var mic in menu.Childs!)
                    res += AddFrom(mic, showDeprecated: showDeprecated, filterMask: filterMask);

                // Menu without children?
                if (res == 1)
                {
                    Items.Remove(mpi);
                }
            }

            return res;
        }
    }


}
