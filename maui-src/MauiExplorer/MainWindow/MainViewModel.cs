using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using FunctionZero.TreeListItemsSourceZero;

namespace MauiTestTree
{
    public class MainViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// For responsive UI design, this app distinguishes different screen device types.
        /// </summary>
        public enum ScreenIdiomEnum { Desktop = 0, Tablet = 1, Phone = 2 }

        /// <summary>
        /// Main switch entity for screen type for responsive UI design.
        /// Set by the main page control.
        /// </summary>
        public ScreenIdiomEnum ScreenIdiom { get; set; } = ScreenIdiomEnum.Desktop;

        /// <summary>
        /// Bool flag from Screen idiom
        /// </summary>
        public bool IsScreenIdiomDesktop { get => ScreenIdiom == ScreenIdiomEnum.Desktop; }

        /// <summary>
        /// For constrained situations, the screen can only focus on left or right panel.
        /// </summary>
        public enum ScreenDivideModeEnum { LeftAndRight, Left, Right }

        /// <summary>
        /// Actual setting of the screen divide between left and right panel
        /// </summary>
        public ScreenDivideModeEnum ScreenDivide { get; set; } = ScreenDivideModeEnum.LeftAndRight;

        /// <summary>
        /// Describes the header names of the menu start points.
        /// </summary>
        public string[] RootMenuHandles = new[] { "File", "Workspace", "Option", "Help" };

        /// <summary>
        /// Decides, which top menu start points are displayed on mobile's top bar.
        /// </summary>
        public IEnumerable<string> MobileTopRootMenuHandles { get => (ScreenIdiom != ScreenIdiomEnum.Phone) ? RootMenuHandles : new string[] { }; }

        /// <summary>
        /// Decides, which top menu start points are displayed on the context menu of the mobile application.
        /// </summary>
        public IEnumerable<string> MobileContextRootMenuHandles { get => (ScreenIdiom == ScreenIdiomEnum.Phone) ? RootMenuHandles : new string[] { }; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string AasId { get; set; } = "www.example.com/ids/aas/4045_9021_2042_4546";
        public string AssetId { get; set; } = "www.example.com/ids/asset/1345_9021_2042_9492";
        
        public AasxMenu MainMenu = AasxPackageExplorer.ExplorerMenuFactory.CreateMainMenu();

        public string LogLine { get; set; } = "Ready.";
        public Color LogFg { get; set; } = Colors.Black;
        public Color LogBg { get; set; } = XamlHelpers.GetDynamicRessource("Gray100", defValue: Colors.LightGray);

        //
        //
        //

        public ObservableCollection<object> SelectedItems = new();

        public MainViewModel()
        {
        }
    }
}
