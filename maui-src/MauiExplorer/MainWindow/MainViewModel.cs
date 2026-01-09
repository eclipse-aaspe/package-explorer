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
using AasxPackageLogic;
using FunctionZero.TreeListItemsSourceZero;
using Aas = AasCore.Aas3_1;

namespace MauiTestTree
{
    /// <summary>
    /// As the MAUI uses tree component needs a root element with Members to be
    /// displayed as first, crete this artificial root elmennt
    /// </summary>
    public class RootOfListOfVisualElement : INotifyPropertyChanged
    {
        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // members (name is important <-> XAML !)
        public ListOfVisualElement Members { get => _members; set { _members = value; OnPropertyChanged(); } }
        protected ListOfVisualElement _members = new();

        // is expanded is always true to allow tree display
        public bool IsExpanded { get => true; set {; } }
    }

    /// <summary>
    /// This view model is shared by the main window and its most important 
    /// components, such as the tree and the element edit panel.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

        public string AasId { get; set; } = "www.example.com/ids/aas/4045_9021_2042_4546";
        public string AssetId { get; set; } = "www.example.com/ids/asset/1345_9021_2042_9492";
        
        public AasxMenu MainMenu = AasxPackageExplorer.ExplorerMenuFactory.CreateMainMenu();

        // the log line is the line at the very bottom left, quite long

        public string LogLine { 
            get => _logline; 
            set { if (_logline == value) return; _logline = value; OnPropertyChanged(); }
        } 
        protected string _logline = "Ready.";

        public Color LogFg
        {
            get => _logFg;
            set { if (_logFg == value) return; _logFg = value; OnPropertyChanged(); }
        }
        protected Color _logFg = Colors.Black;
        
        public Color LogBg
        {
            get => _logBg;
            set { if (_logBg == value) return; _logBg = value; OnPropertyChanged(); }
        }
        protected Color _logBg = XamlHelpers.GetDynamicRessource("Gray100", defValue: Colors.LightGray);
        
        public FontWeight LogFontWeight { 
            get => _logFontWeight;
            set { if (_logFontWeight == value) return; _logFontWeight = value; OnPropertyChanged(); }
        }
        protected FontWeight _logFontWeight = FontWeight.Regular;

        // the attention indicator sums up important warnings/ errors

        public string AttentionText
        {
            get => _attentionText;
            set { if (_attentionText == value) return; _attentionText = value; OnPropertyChanged(); }
        }
        protected string _attentionText = "";

        public Color AttentionBg
        {
            get => _attentionBg;
            set { if (_attentionBg == value) return; _attentionBg = value; OnPropertyChanged(); }
        }
        protected Color _attentionBg = Colors.Transparent;

        //
        // To be modified
        //

        // public TreeElemNode RootNode { get; set; } = new();

        /// <summary>
        /// Note: One Root, then all the members to be displayed as list.
        /// Note: Therefore one level more than with AASPE-WPF!!
        /// </summary>
        // public VisualElementEnvironmentItem RootNode { get; set; } = new VisualElementEnvironmentItem(null, null, null, null, VisualElementEnvironmentItem.ItemType.Env);
        public RootOfListOfVisualElement RootNode { get; set; } = new();

        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value)
                    return;
                _selectedItem = value;
                // Trace.WriteLine($"Setter VM hash: {GetHashCode()}");
                OnPropertyChanged();
            }
        }
        protected object? _selectedItem = null;
        
        public ObservableCollection<object> SelectedItems { get; set; } = new();

        public bool MultiSelectOn
        {
            get => _multiSelectOn;
            set
            {
                if (_multiSelectOn == value)
                    return;
                _multiSelectOn = value;
                OnPropertyChanged();
                OnPropertyChanged(name: nameof(MultiSelectMode));
            }
        }
        protected bool _multiSelectOn = false;

        public SelectionMode MultiSelectMode { get => _multiSelectOn ? SelectionMode.Multiple : SelectionMode.Single; }

        public MainViewModel()
        {
#if old
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
#else
            //RootNode.IsExpanded = true;
            //RootNode.Members.Add(new VisualElementSubmodel(null, null, null, null, new Aas.Submodel("12344333", idShort: "sxsaxssa")));
            //RootNode.Members.Add(new VisualElementSubmodel(null, null, null, null, new Aas.Submodel("43554354", idShort: "dewdweddew")));


            RootNode.Members.Add(new VisualElementSubmodel(null, null, null, null, new Aas.Submodel("12344333", idShort: "sxsaxssa")));
            RootNode.Members.Add(new VisualElementSubmodel(null, null, null, null, new Aas.Submodel("43554354", idShort: "dewdweddew")));
#endif
        }
    }
}
