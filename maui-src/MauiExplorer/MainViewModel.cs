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
        
        public TreeElemNode RootNode { get; set; } = new();

        public List<Person> ListData { get; } = new List<Person>();

        public AasxMenu MainMenu = AasxPackageExplorer.ExplorerMenuFactory.CreateMainMenu();

        public string LogLine { get; set; } = "Ready.";
        public Color LogFg { get; set; } = Colors.Black;
        public Color LogBg { get; set; } = XamlHelpers.GetDynamicRessource("Gray100", defValue: Colors.LightGray);

        //
        //
        //

        protected object? _selectedItem = null;
        public object? SelectedItem { get => _selectedItem; 
            set 
            {
                _selectedItem = value;
                if (_selectedItem is TreeNodeContainer<object> tnc && tnc.Data is TreeElemNode tn)
                    Trace.WriteLine("" + tn.Caption);
            } 
        }
        public ObservableCollection<object> SelectedItems = new();

        public MainViewModel()
        {
            ListData.Add(new Person() { Name = "Micha", Title = "Nobody" });
            ListData.Add(new Person() { Name = "Paule", Title = "Profi" });

            RootNode.Caption = "Hallo";

            var root1 = new TreeElemNode { Tag = "AAS", Caption = "SMT PCN", Info = "SMT for Product Change Notification" };
            root1.Children.Add(new TreeElemNode { Tag = "SMC", Caption = "Section 1", Info = "Basics" });
            root1.Children.Add(new TreeElemNode { Tag = "SMC", Caption = "Section 2" });

            var root2 = new TreeElemNode { Tag = "Asset", Caption = "Asset", Info = "Asset description" };
            root2.Children.Add(new TreeElemNode { Tag = "SMC", Caption = "Child 3" });

            var root3 = new TreeElemNode { Tag = "SMC", Caption = "Root3" };
            root3.Children.Add(new TreeElemNode { Tag = "SMC", Caption = "Child 4" });

            RootNode.Children.Add(root1);
            RootNode.Children.Add(root2);
            RootNode.Children.Add(root3);

            SelectedItems.CollectionChanged += (s, e) =>
            {
                Trace.WriteLine("" + s);
            };
        }
    }
}
