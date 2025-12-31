using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageLogic;
using AnyUi;
using FunctionZero.TreeListItemsSourceZero;
using Microsoft.Maui.Devices;

namespace MauiTestTree
{
    public partial class MainPage : ContentPage
    {
        protected MainViewModel _viewModel = new MainViewModel();

        int count = 0;

        public MainPage()
        {
            // default start
            InitializeComponent();

            // initialize view model
            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                _viewModel.ScreenIdiom = MainViewModel.ScreenIdiomEnum.Phone;
            }
            else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
            {
                _viewModel.ScreenIdiom = MainViewModel.ScreenIdiomEnum.Tablet;
            }
            else if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            {
                _viewModel.ScreenIdiom = MainViewModel.ScreenIdiomEnum.Desktop;
            }

            // display view model
            BindingContext = _viewModel;

            //if (MyTree.SelectedItems is ObservableCollection<object> oc)
            //    oc.CollectionChanged += (s, e) => {
            //        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add
            //            && e.NewItems != null)
            //            foreach (var ni in e.NewItems)
            //                if (ni is TreeNodeContainer<object> tnc && tnc.Data is TreeElemNode tn)
            //                    Trace.WriteLine("Add:" + tn.Caption);
            //        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove
            //            && e.OldItems != null)
            //            foreach (var ni in e.OldItems)
            //                if (ni is TreeNodeContainer<object> tnc && tnc.Data is TreeElemNode tn)
            //                    Trace.WriteLine("Remove:" + tn.Caption);
            //    };

            // TestAnyUI(2);

            Loaded += async (s, e) =>
            {
                await TestAnyUI(81);
            };
        }

        protected async Task TestAnyUI(int mode)
        {
            AnyUiStackPanel stack = new();
            if (mode == 0)
            {
                var g = new AnyUiGrid();
                g.RowDefinitions.Add(new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Auto));
                g.RowDefinitions.Add(new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Auto));
                g.RowDefinitions.Add(new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Auto));
                g.RowDefinitions.Add(new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Auto));
                g.ColumnDefinitions.Add(new AnyUiColumnDefinition(1.0, AnyUiGridUnitType.Star));
                g.ColumnDefinitions.Add(new AnyUiColumnDefinition(1.0, AnyUiGridUnitType.Auto));
                stack.Children.Add(g);

                var l = new AnyUiLabel() { Content = "Hallo", Background = AnyUiBrushes.LightBlue, VerticalContentAlignment = AnyUiVerticalAlignment.Center };
                AnyUiGrid.SetRow(l, 0);
                AnyUiGrid.SetColumn(l, 0);
                g.Add(l);

                var b = new AnyUiButton() { Content = "Push me", Background = AnyUiBrushes.DarkGray, Foreground = AnyUiBrushes.White, Margin = new AnyUiThickness(3) };
                AnyUiGrid.SetRow(b, 0);
                AnyUiGrid.SetColumn(b, 1);
                g.Add(b);
                b.setValueLambda = (o) => {
                    Trace.WriteLine("Button pressed!");
                    return new AnyUiLambdaActionNone();
                };

                var e = new AnyUiTextBox() { Text = "Hallo", Margin = new AnyUiThickness(80, 60, 5, 5), Background = AnyUiBrushes.Yellow };
                AnyUiGrid.SetRow(e, 1);
                AnyUiGrid.SetColumn(e, 0);
                AnyUiGrid.SetColumnSpan(e, 2);
                g.Add(e);

                var cb = new AnyUiComboBox() { Items = (new[] { "A", "Bb", "Ccc", "Dddd" }).Cast<object>().ToList(), Margin = new AnyUiThickness(4), SelectedIndex = 2 };
                AnyUiGrid.SetRow(cb, 2);
                AnyUiGrid.SetColumn(cb, 0);
                g.Add(cb);
                cb.setValueLambda = (o) => {
                    if (o is string s)
                        Trace.WriteLine($"Combobox item {s} selected!");
                    return new AnyUiLambdaActionNone();
                };
            }

            if (mode == 1)
            {
                var deh = new DispEditHelperModules();
                deh.levelColors = new() { 
                    MainSection = new() { Bg = AnyUiBrushes.DarkBlue, Fg = AnyUiBrushes.White},
                    SubSection = new() { Bg = AnyUiBrushes.LightBlue, Fg = AnyUiBrushes.White},
                    SubSubSection = new() { Bg = AnyUiBrushes.DarkGray, Fg = AnyUiBrushes.White},
                    HintSeverityHigh = new() { Bg = AnyUiBrushes.Red, Fg = AnyUiBrushes.Black},
                    HintSeverityNotice = new() { Bg = AnyUiBrushes.Yellow, Fg = AnyUiBrushes.Black}
                };
                deh.repo = new();
                deh.DisplayOrEditEntityReferable(
                    new AasCore.Aas3_1.Environment(), 
                    stack, 
                    parentContainer: new AasCore.Aas3_1.Submodel("http://abc.de/123"),
                    referable: new AasCore.Aas3_1.Property(AasCore.Aas3_1.DataTypeDefXsd.String, idShort: "Test123", category: "ABC"),
                    indexPosition: 0);
            }

            if (mode == 2)
            {
                var deh = new DispEditHelperMultiElement();
                deh.levelColors = new()
                {
                    MainSection = new() { Bg = AnyUiBrushes.DarkBlue, Fg = AnyUiBrushes.White },
                    SubSection = new() { Bg = AnyUiBrushes.LightBlue, Fg = AnyUiBrushes.White },
                    SubSubSection = new() { Bg = AnyUiBrushes.DarkGray, Fg = AnyUiBrushes.White },
                    HintSeverityHigh = new() { Bg = AnyUiBrushes.Red, Fg = AnyUiBrushes.Black },
                    HintSeverityNotice = new() { Bg = AnyUiBrushes.Yellow, Fg = AnyUiBrushes.Black }
                };
                deh.repo = new();
                var sme = new AasCore.Aas3_1.Property(AasCore.Aas3_1.DataTypeDefXsd.String, idShort: "Test123", category: "ABC");
                deh.DisplayOrEditAasEntitySubmodelElement(
                    new AasxPackageLogic.PackageCentral.PackageCentral(),
                    new AasCore.Aas3_1.Environment(),
                    parentContainer: new AasCore.Aas3_1.Submodel("http://abc.de/123"),
                    wrapper: sme,
                    sme: sme,
                    indexPosition: 0,
                    editMode: true,
                    repo: new ModifyRepo(),
                    stack: stack,
                    hintMode: true
                    );
            }

            if (mode < 80)
            {
                var dc = new AnyUiDisplayContextMaui();
                var ve = dc.GetOrCreateMauiElement(stack, null, allowReUse: false);

                var dbg = new VisualTreeDebugger();
                dbg.Dump(ve!, VisualTreeDebugger.Attributes.All);
                ;

                // RightVerticalStack.Children.Add(dbg.Elements[0]);
            }
            else
            if (mode == 81)
            {
                var dc = new AnyUiDisplayContextMaui();

                dc.TryRegisterIconFont("uc", "OpenSansRegular", 16);
                dc.TryRegisterIconFont("awe", "FontAwesome", 16);

                var env = new AasCore.Aas3_1.Environment();
                var parentContainer = new AasCore.Aas3_1.Submodel("http://abc.de/123");
                var sme = new AasCore.Aas3_1.Property(AasCore.Aas3_1.DataTypeDefXsd.String, idShort: "Test123", category: "ABC");

                var entities = new ListOfVisualElementBasic();
                entities.Add(new VisualElementSubmodelElement(
                    parent: null, cache: null, env, parentContainer: parentContainer, wrap: sme, 0));

                await RightDispEditEntity.DisplayOrEditVisualAasxElement(
                    packages: new AasxPackageLogic.PackageCentral.PackageCentral(),
                    displayContext: dc,
                    entities: entities,
                    editMode: true,
                    hintMode: true,
                    flyoutProvider: null
                    );
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ToolbarItems.Clear();

            foreach (var rootHandle in _viewModel.MobileContextRootMenuHandles)
            {
                ToolbarItems.Add(new ToolbarItem
                {
                    Text = $"{rootHandle}",
                    Order = ToolbarItemOrder.Secondary,
                    Command = new Command(() => OnToolbarClicked(rootHandle))
                });
            }

            // adopt some small graphical issues
            if (_viewModel.IsScreenIdiomDesktop)
                TitleViewFixItems.Margin = new Thickness(8, -6, 0, 0);

            // generate main menu
            GenerateMainMenu();
        }

        protected record KeyGestureInfo (KeyboardAcceleratorModifiers Modifiers, string Key);

        protected KeyGestureInfo? ParseWpfInputGesture(string input)
        {
            // access
            if (input == null)
                return null;
            input = input.Trim();
            if (input == "")
                return null;

            // single?
            var parts = input.Split('+');
            if (parts.Length <= 1)
                return new KeyGestureInfo(KeyboardAcceleratorModifiers.None, input);

            // decompose
            KeyboardAcceleratorModifiers mods = KeyboardAcceleratorModifiers.None;
            var key = "";
            foreach (var p in parts)
            {
                if (p == "Ctrl")
                    mods = mods | KeyboardAcceleratorModifiers.Ctrl;
                if (p == "Alt")
                    mods = mods | KeyboardAcceleratorModifiers.Alt;
                if (p == "Shift")
                    mods = mods | KeyboardAcceleratorModifiers.Ctrl;
                if (p == "Cmd")
                    mods = mods | KeyboardAcceleratorModifiers.Cmd;
                else if (p == parts.LastOrDefault())
                    key = p;
            }
            return new KeyGestureInfo(mods, key);
        }


        protected MenuFlyoutItem? GenerateMainMenu_CreateItem(AasxMenuItemBase mib)
        {
            var mic = mib as AasxMenuItem;
            if (mic == null || mic.Hidden || mic.IsCheckable)
                return null;

            // recurse?
            if (mic.Childs != null)
            {
                var res = new MenuFlyoutSubItem()
                {
                    BindingContext = mic,
                    Text = mic.Header.Replace("_", "")
                };

                foreach (var micb in mic.Childs)
                {
                    var mbic = GenerateMainMenu_CreateItem(micb);
                    if (mbic != null)
                        res.Add(mbic);
                }
                return res;
            }
            else
            {
                var res = new MenuFlyoutItem()
                {
                    BindingContext = mic,
                    Text = mic.Header.Replace("_", ""),
                };

                var kgi = ParseWpfInputGesture(mic.InputGesture);
                if (kgi != null)
                    res.KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = kgi.Key, Modifiers = kgi.Modifiers });

                res.Clicked += MainMenuItem_Clicked;

                return res;
            }
        }

        protected async void MainMenuItem_Clicked(object? sender, EventArgs e)
        {
            ;

            if (sender is MenuFlyoutItem mfi)
            {
                if (mfi.Text == "Edit settings")
                {
                    await EditMainMenuCheckableOptionsAsync();
                }
            }
        }

        // <summary>
        /// For mobile devices, shows a dedicated page in order to present
        /// menu choices.
        /// </summary>
        private async Task EditMainMenuCheckableOptionsAsync()
        {
            // generate modal page and start
            var pickerPage = new MenuCheckableOptions(_viewModel.MainMenu);
            await Navigation.PushModalAsync(pickerPage);
        }

        protected void GenerateMainMenu()
        {
            // clear
            var mbis = RootOfPage.MenuBarItems;
            mbis.Clear();

            // do the 1st level manually
            foreach (var rmh in _viewModel.RootMenuHandles)
            {
                // find start of menu hierarchy
                var mi = _viewModel.MainMenu.FindHeader(rmh) as AasxMenuItem;
                if (mi == null)
                    continue;

                // add visual root
                var mbi = new MenuBarItem()
                {
                    BindingContext = mi,
                    Text = mi.Header.Replace("_", "")
                };
                mbis.Add(mbi);

                // add childs?
                if (mi.Childs != null && mi.Childs.Count >= 1)
                    foreach (var c in mi.Childs)
                    {
                        var mbic = GenerateMainMenu_CreateItem(c);
                        if (mbic != null)
                            mbi.Add(mbic);
                    }

                // provide a special case for "Options", which elsewise is empty
                if (mbi.Text == "Option" && mbi.Count < 1)
                {
                    // add a special item for editing IsCheckable's
                    var res = new MenuFlyoutItem()
                    {
                        BindingContext = null,
                        Text = "Edit settings"
                    };
                    
                    res.Clicked += MainMenuItem_Clicked;

                    mbi.Add(res);
                }
            }
        }

        protected async void OnToolbarClicked(string rootHandle)
        {
            // handle action
            await ShowMobilePanelMenuAsync(rootHandle);
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            //count++;

            //if (count == 1)
            //    CounterBtn.Text = $"Clicked {count} time";
            //else
            //    CounterBtn.Text = $"Clicked {count} times";

            //SemanticScreenReader.Announce(CounterBtn.Text);
        }

        double _startLeftWidth;
        double _startRightWidth;

        double _rightOverLeftFactor = 1.0;

        void OnDividerPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (e.StatusType == GestureStatus.Started)
            {
                //_startLeftWidth = MainGrid.ColumnDefinitions[0].Width.Value;
                //_startRightWidth = MainGrid.ColumnDefinitions[2].Width.Value;

                _startLeftWidth = LeftContent.Width;
                _startRightWidth = RightContent.Width;
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                var delta = e.TotalX;

                var newLeft = _startLeftWidth + delta;
                var newRight = _startRightWidth - delta;

                int minLeft = (_viewModel.ScreenIdiom == MainViewModel.ScreenIdiomEnum.Phone) ? 30 : 150;
                int minRight = (_viewModel.ScreenIdiom == MainViewModel.ScreenIdiomEnum.Phone) ? 100 : 150;

                if (newLeft >= minLeft && newRight >= minRight)
                {

                    MainGrid.ColumnDefinitions[0].Width = new GridLength(newLeft, GridUnitType.Absolute);
                    MainGrid.ColumnDefinitions[2].Width = new GridLength(newRight, GridUnitType.Absolute);

                    _rightOverLeftFactor = newRight / newLeft;

                    this.Content?.InvalidateMeasure();
                }
            }
            else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                // switch back to auto size
                MainGrid.ColumnDefinitions[0].Width = new GridLength(1.0, GridUnitType.Star);
                MainGrid.ColumnDefinitions[2].Width = new GridLength(_rightOverLeftFactor, GridUnitType.Star);
            }
        }

        protected void RequestScreenDivide(MainViewModel.ScreenDivideModeEnum newMode)
        {
            if (newMode == MainViewModel.ScreenDivideModeEnum.Left)
            {
                LeftContent.IsVisible = true;
                RightContent.IsVisible = false;
                MainGrid.ColumnDefinitions[0].Width = MainGrid.Width
                    - MainGrid.ColumnDefinitions[1].Width.Value;
            }
            else if (newMode == MainViewModel.ScreenDivideModeEnum.Right)
            {
                LeftContent.IsVisible = false;
                RightContent.IsVisible = true;
                MainGrid.ColumnDefinitions[0].Width = 0;
                MainGrid.ColumnDefinitions[2].Width = MainGrid.Width;
            }
            else
            {
                LeftContent.IsVisible = true;
                RightContent.IsVisible = true;

                double left = Math.Min(300, 0.4 * MainGrid.Width);
                MainGrid.ColumnDefinitions[0].Width = left;
                MainGrid.ColumnDefinitions[2].Width = MainGrid.Width
                    - MainGrid.ColumnDefinitions[1].Width.Value
                    - left;
            }

            // remember
            _viewModel.ScreenDivide = newMode;
        }

        private void OnExpandRoot1(object sender, EventArgs e)
        {
            //foreach (var node in (treeView.ItemsSource as ObservableCollection<TreeNode>)!)
            //{
            //    //if (node.Name == "Root1")
            //    //    node.IsExpanded = true;
            //}
        }

        private void OnCollapseAll(object sender, EventArgs e)
        {
            //void Collapse(TreeNode n)
            //{
            //    //n.IsExpanded = false;
            //    //foreach (var child in n.Children)
            //    //    Collapse(child);
            //}

            //foreach (var node in (treeView.ItemsSource as ObservableCollection<TreeNode>)!)
            //    Collapse(node);
        }

        private async void ToolBarItem_Clicked(object sender, EventArgs e)
        {
            if (sender == LeftPanelToolBarButton)
            {
                if (_viewModel.ScreenDivide == MainViewModel.ScreenDivideModeEnum.Left)
                    // reset to both
                    RequestScreenDivide(MainViewModel.ScreenDivideModeEnum.LeftAndRight);
                else
                    RequestScreenDivide(MainViewModel.ScreenDivideModeEnum.Left);
                return;
            }

            if (sender == RightPanelToolBarButton)
            {
                if (_viewModel.ScreenDivide == MainViewModel.ScreenDivideModeEnum.Right)
                    // reset to both
                    RequestScreenDivide(MainViewModel.ScreenDivideModeEnum.LeftAndRight);
                else
                    RequestScreenDivide(MainViewModel.ScreenDivideModeEnum.Right);
                return;
            }

            string? activateMenu = null;

            if (sender is ToolbarItem tbi && tbi.Text != null)
            {
                if (tbi.Text.ToLower() == "File".ToLower())
                    activateMenu = "File";
                if (tbi.Text.ToLower() == "Workspace".ToLower())
                    activateMenu = "Workspace";
                if (tbi.Text.ToLower() == "Option".ToLower())
                    activateMenu = "Option";
                if (tbi.Text.ToLower() == "Help".ToLower())
                    activateMenu = "Help";
            }

            // activate modal panel for menu pick on mobile devices
            if (activateMenu != null)
                await ShowMobilePanelMenuAsync(activateMenu);
        }

        /// <summary>
        /// For mobile devices, shows a dedicated page in order to present
        /// menu choices.
        /// </summary>
        private async Task ShowMobilePanelMenuAsync(string activateMenu)
        {
            // find menu
            var m = _viewModel.MainMenu.FindHeader(activateMenu);
            if (m == null)
                return;

            // generate view model for this menu
            var mpvm = new MenuPickerViewModel();
            mpvm.DialogHeader = $"Select option for the « {activateMenu} » menu";
            mpvm.AddFrom(m, omitRoot: true);

            // generate picker control and start
            var pickerPage = new MenuPickerPage(mpvm);
            await Navigation.PushModalAsync(pickerPage);

            // Execution continues AFTER PopModalAsync
            var result = pickerPage.SelectedOption;
                        if (result != null)
            {
                // Handle selection
                // TODO
                Trace.WriteLine($"User picked: {result}");
            }
        }

        /// <summary>
        /// Activated in Windows/ macOS Catalyst
        /// </summary>
        private void ClassicMenuBarItem_Clicked(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Actiated on mobile device from top bar / left menu items
        /// </summary>
        private async void OnTitleViewButton_Clicked(object sender, EventArgs e)
        {
            if (!(sender is Button b && b.Text != null))
                return;

            foreach (var mh in _viewModel.RootMenuHandles)
                if (b.Text == $"{mh}")
                    await ShowMobilePanelMenuAsync(mh);
        }
    }
}
