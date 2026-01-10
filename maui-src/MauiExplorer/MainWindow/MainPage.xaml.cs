using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using FunctionZero.TreeListItemsSourceZero;
using Microsoft.Maui.Devices;
using Extensions;
using Aas = AasCore.Aas3_1;
using System.Text;
using static AasxPackageLogic.PackageCentral.PackageContainerHttpRepoSubset;
using AdminShellNS.DiaryData;
using ExhaustiveMatch = ExhaustiveMatching.ExhaustiveMatch;
using System.Web;

namespace MauiTestTree
{
    public partial class MainPage : ContentPage, IFlyoutProvider, IPushApplicationEvent, IMainWindow, IExecuteMainCommand
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

            // many things when loaded
            Loaded += Window_Loaded;

            // TestAnyUI(2);

            //Loaded += async (s, e) =>
            //{
            //    await TestAnyUI(81);
            //};
        }

        #region MAUI stuff
        // ---------------

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
                var dc = new AnyUiDisplayContextMaui(this, this, new PackageCentral());
                var ve = dc.GetOrCreateMauiElement(stack, null, allowReUse: false);

                var dbg = new VisualTreeDebugger();
                dbg.Dump(ve!, VisualTreeDebugger.Attributes.All);
                ;

                // RightVerticalStack.Children.Add(dbg.Elements[0]);
            }
            else
            if (mode == 81)
            {
                var dc = new AnyUiDisplayContextMaui(this, this, new PackageCentral());

                dc.TryRegisterIconFont("uc", "OpenSansRegular", 16);
                dc.TryRegisterIconFont("awe", "FontAwesome", 16);

                var env = new AasCore.Aas3_1.Environment();
                var parentContainer = new AasCore.Aas3_1.Submodel("http://abc.de/123");
                var sme = new AasCore.Aas3_1.Property(AasCore.Aas3_1.DataTypeDefXsd.String, idShort: "Test123", category: "ABC");

                var entities = new ListOfVisualElementBasic();
                entities.Add(new VisualElementSubmodelElement(
                    parent: null, cache: null, env, parentContainer: parentContainer, wrap: sme, 0));

                await DispEditEntityPanel.DisplayOrEditVisualAasxElement(
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

                // regular way
                // Note: Different than WPF / Blazor approaches
                if (mfi.BindingContext is AasxMenuItemBase mib)
                {
                    var ticket = new AasxMenuActionTicket() { MenuItem = mib };
                    await _viewModel.MainMenu.ActivateAction(mib, ticket);
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

        #endregion

        //
        //
        // ported from AASP for WPF
        //
        //

        #region Members
        // ============

        /// <summary>
        /// Abstracted menu functions to be wrapped by functions triggering
        /// more UI feedback.
        /// Remark: "Scripting" is only the highest of the functionality levels
        /// of the "stacked classes"
        /// </summary>
        protected MainWindowScripting Logic = new MainWindowScripting();

        /// <summary>
        /// The top-most display data required for WPF to render elements.
        /// </summary>
        protected AnyUiDisplayContextMaui DisplayContext;

        /// <summary>
        /// This symbol is only a link to the abstract main-windows class.
        /// </summary>
        public PackageCentral PackageCentral
        {
            get => Logic.PackageCentral;
        }

        //1// public AasxMenuWpf MainMenu = new AasxMenuWpf();

        private VisualElementGeneric? _showContentElement = null;
        private VisualElementGeneric? currentEntityForUpdate = null;
        private IFlyoutControl? currentFlyoutControl = null;

        // private BrowserContainer theContentBrowser = new BrowserContainer();

        private AasxIntegrationBase.IAasxOnlineConnection? theOnlineConnection = null;

        /// <summary>
        /// Helper class to "compress events" (group AAS event payloads together).
        /// No relation to UI stuff.
        /// </summary>
        private AasEventCompressor _eventCompressor = new AasEventCompressor();


        //1//
        public AasxMenuWpf DynamicMenu = new AasxMenuWpf();

        /// <summary>
        /// Allows creating tokens.. based on user configured information or UI.
        /// </summary>
        //1// public WinGdiSecurityAccessHandler _securityAccessHandler = null;

        #endregion

        #region Utility functions
        //=======================

        //1//
        //public static string WpfStringAddWrapChars(string str)
        //{
        //    var res = "";
        //    foreach (var c in str)
        //        res += c + "\u200b";
        //    return res;
        //}

        /// <summary>
        /// Directly browse and show an url page
        /// </summary>
        public void ShowContentBrowser(string url, bool silent = false)
        {
            //1//
            //theContentBrowser.GoToContentBrowserAddress(url);
            //if (!silent)
            //    Dispatcher.BeginInvoke((Action)(() => ElementTabControl.SelectedIndex = 1));
        }

        /// <summary>
        /// Directly browse and show help page
        /// </summary>
        public void ShowHelp(bool silent = false)
        {
            if (!silent)
                BrowserDisplayLocalFile(
                    @"https://github.com/admin-shell/aasx-package-explorer/blob/master/help/index.md");
        }

        /// <summary>
        /// Calls the browser. Note: does NOT catch exceptions!
        /// </summary>
        private void BrowserDisplayLocalFile(string url, string? mimeType = null, bool preferInternal = false)
        {
            //1//
            //if (theContentBrowser.CanHandleFileNameExtension(url, mimeType) || preferInternal)
            //{
            //    // try view in browser
            //    Log.Singleton.Info($"Displaying {url} with mimeType {"" + mimeType} locally in embedded browser ..");
            //    ShowContentBrowser(url);
            //}
            //else
            //{
                // open externally
                Log.Singleton.Info($"Displaying {url} with mimeType {"" + mimeType} " +
                    $"remotely in external viewer ..");

                Process proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = url;
                proc.Start();
            // }
        }

        public void ClearAllViews()
        {
            // left side
            _viewModel.AasId = "<id missing!>";
            //1// this.AssetPic.Source = null;
            _viewModel.AssetId = "<id missing!>";

            // middle side
            DisplayElements.Clear();

            // right side
            //1// theContentBrowser.GoToContentBrowserAddress(Options.Curr.ContentHome);
        }

        /// <summary>
        /// Redraw window title, AAS info?, entity view (right), element tree (middle)
        /// </summary>
        /// <param name="keepFocus">Try remember which element was focussed and focus it after redrawing.</param>
        /// <param name="nextFocusMdo">Focus a new main data object attached to an tree element.</param>
        /// <param name="wishExpanded">If focussing, expand this item.</param>
        public async Task RedrawAllAasxElementsAsync(bool keepFocus = false,
            object? nextFocusMdo = null,
            bool wishExpanded = true)
        {
            await Task.Yield();

            // focus info
            var focusMdo = DisplayElements.SelectedItem?.GetDereferencedMainDataObject();

            var t = "AASX Package Explorer V3.1";
            //TODO (jtikekar, 0000-00-00): remove V3RC02
            if (PackageCentral.MainAvailable == true)
                t += " - " + PackageCentral.MainItem.ToString();
            if (PackageCentral.AuxAvailable == true)
                t += " (auxiliary AASX: " + PackageCentral.AuxItem.ToString() + ")";
            this.Title = t;

#if _log_times
            Log.Singleton.Info("Time 10 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif

            // clear the right section, first (might be rebuild by callback from below)
            DispEditEntityPanel.ClearDisplayDefaultStack();
            TakeOverContentEnable(false);

            // rebuild middle section
            DisplayElements.RebuildAasxElements(
                //                                            TODO!!
                PackageCentral, PackageCentral.Selector.Main, true || _viewModel.MainMenu?.IsChecked("EditMenu") == true,
                lazyLoadingFirst: false /* TODO */);

            // ok .. try re-focus!!
            if (keepFocus)
            {
                // make sure that Submodel is expanded
                this.DisplayElements.ExpandAllItems();

                // still proceed?
                var veFound = this.DisplayElements.SearchVisualElementOnMainDataObject(focusMdo,
                        alsoDereferenceObjects: true);

                if (veFound != null)
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
            }

            // display again
            DisplayElements.Refresh();

#if _log_times
            Log.Singleton.Info("Time 90 is: " + DateTime.Now.ToString("hh:mm:ss.fff"));
#endif
        }

        /// <summary>
        /// Checks, if any identifiable is tainted (modified). Helps asking the user if to save
        /// data before losing it.
        /// </summary>
        public bool CheckIsAnyTaintedIdentifiableInMain()
        {
            return DisplayElements.IsAnyTaintedIdentifiable();
        }

        /// <summary>
        /// Large extend. Basially redraws everything after new package has been loaded.
        /// </summary>
        /// <param name="onlyAuxiliary">Only tghe AUX package has been altered.</param>
        public async Task RestartUIafterNewPackage(bool onlyAuxiliary = false, bool? nextEditMode = null)
        {
            if (onlyAuxiliary)
            {
                // reduced, in the background
                await RedrawAllAasxElementsAsync();
            }
            else
            {
                // visually a new content
                // switch off edit mode -> will will cause the browser to show the AAS as selected element
                // and -> this will update the left side of the screen correctly!
                _viewModel.MainMenu?.SetChecked("EditMenu", nextEditMode.HasValue ? nextEditMode.Value : false);
                ClearAllViews();
                await RedrawAllAasxElementsAsync();
                await RedrawElementViewAsync();
                ShowContentBrowser(Options.Curr.ContentHome, silent: true);
                _eventHandling.Reset();
            }
        }

        private AdminShellPackageFileBasedEnv LoadPackageFromFile(string fn)
        {
            if (fn.Trim().ToLower().EndsWith(".aml"))
            {
                var res = new AdminShellPackageFileBasedEnv();
                AasxAmlImExport.AmlImport.ImportInto(res, fn);
                return res;
            }
            else
                return new AdminShellPackageFileBasedEnv(fn, Options.Curr.IndirectLoadSave);
        }

        public void TakeOverContentEnable(bool enabled)
        {
            //1// ContentTakeOver.IsEnabled = enabled;
        }

        public void DisplayExternalEntity(object control)
        {
            if (control is Microsoft.Maui.Controls.View fe)
                DispEditEntityPanel.SetDisplayExternalControl(fe);
        }

        public object? GetEntityMasterPanel()
        {
            return DispEditEntityPanel?.GetMasterPanel();
        }

        /// <summary>
        /// Triggers update of display
        /// </summary>
        public void UpdateDisplay()
        {
            //1// this.UpdateLayout();
        }

        private PackCntRuntimeOptions UiBuildRuntimeOptionsForMainAppLoad()
        {
            var ro = new PackCntRuntimeOptions()
            {
                Log = Log.Singleton,
                //1//
                //ProgressChanged = (state, tfs, tbd, msg) =>
                //{
                //    if (state == PackCntRuntimeOptions.Progress.StartOverall)
                //    {
                //        SetProgressOverall(true, msg);
                //    }

                //    if (state == PackCntRuntimeOptions.Progress.OverallMessage
                //        && _progressOverallActive)
                //    {
                //        SetProgressOverall(true, msg);
                //    }

                //    if (state == PackCntRuntimeOptions.Progress.EndOverall)
                //    {
                //        SetProgressOverall(false, msg);
                //    }

                //    if ((state == PackCntRuntimeOptions.Progress.StartDownload
                //        || state == PackCntRuntimeOptions.Progress.PerformDownload)
                //        && tfs.HasValue && tbd.HasValue)
                //        SetProgressDownload(
                //            Math.Min(100.0, 100.0 * tbd.Value / (tfs.HasValue ? tfs.Value : 5 * 1024 * 1024)),
                //            AdminShellUtil.ByteSizeHumanReadable(tbd.Value));

                //    if (state == PackCntRuntimeOptions.Progress.EndDownload)
                //    {
                //        // clear
                //        SetProgressDownload();

                //        // close message boxes
                //        if (currentFlyoutControl is IntegratedConnectFlyout)
                //            CloseFlyover(threadSafe: true);
                //    }
                //},
                //ShowMesssageBox = (content, text, title, buttons) =>
                //{
                //    // not verbose
                //    if (MainMenu?.IsChecked("VerboseConnect") == false)
                //    {
                //        // give specific default answers
                //        if (title?.ToLower().Trim() == "Select certificate chain".ToLower())
                //            return AnyUiMessageBoxResult.Yes;

                //        // default answer
                //        return AnyUiMessageBoxResult.OK;
                //    }

                //    // make sure the correct flyout is loaded
                //    if (currentFlyoutControl != null && !(currentFlyoutControl is IntegratedConnectFlyout))
                //        return AnyUiMessageBoxResult.Cancel;
                //    if (currentFlyoutControl == null)
                //        StartFlyover(new IntegratedConnectFlyout(PackageCentral, "Connecting .."));

                //    // ok -- perform dialogue in dedicated function / frame
                //    var ucic = currentFlyoutControl as IntegratedConnectFlyout;
                //    if (ucic == null)
                //        return AnyUiMessageBoxResult.Cancel;
                //    else
                //        return ucic.MessageBoxShow(content, text, title, buttons);
                //},
                //AllowFakeResponses = Options.Curr.AllowFakeResponses,
                //ExtendedConnectionDebug = Options.Curr.ExtendedConnectionDebug,
                //SecurityAccessHandler = _securityAccessHandler,
                //GetBaseUriForNewIdentifiablesHandler = async (defBaseUri, idf) =>
                //{
                //    // allready remembered
                //    string baseUriStr = null;
                //    if (Logic.RememberNewIdentifiableBaseUri
                //        && Logic.RememberedNewIdentifiableBaseUriStr?.HasContent() == true)
                //    {
                //        baseUriStr = Logic.RememberedNewIdentifiableBaseUriStr;
                //    }
                //    else
                //    {
                //        // ask user

                //        var rec = new PackageContainerHttpRepoSubset.GetBaseAddressUploadRecord()
                //        {
                //            DisplayIdf = idf,
                //            BaseAddress = defBaseUri.AbsoluteUri,
                //            BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,
                //            Remember = false,
                //        };

                //        var res = await PackageContainerHttpRepoSubset.PerformGetBaseAddressUploadDialogue(
                //            ticket: null,
                //            displayContext: DisplayContext,
                //            caption: "Get Base Address for New Identifiable",
                //            record: rec);

                //        if (res)
                //        {
                //            baseUriStr = rec.BaseAddress;
                //            if (rec.Remember)
                //            {
                //                Logic.RememberNewIdentifiableBaseUri = true;
                //                Logic.RememberedNewIdentifiableBaseUriStr = baseUriStr;
                //            }
                //        }
                //    }

                //    // try convert to URI, again
                //    if (baseUriStr == null)
                //        return null;
                //    try
                //    {
                //        var uris = new BaseUriDict(baseUriStr);
                //        if (idf is Aas.IAssetAdministrationShell)
                //            return uris.GetBaseUriForAasRepo();
                //        if (idf is Aas.ISubmodel)
                //            return uris.GetBaseUriForSmRepo();
                //        if (idf is Aas.IConceptDescription)
                //            return uris.GetBaseUriForCdRepo();
                //        return new Uri(baseUriStr);
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Singleton.Error(ex, $"when building URi for new Identifiables from base address: {baseUriStr}");
                //        return null;
                //    }
                //}
            };
            return ro;
        }

        /// <summary>
        /// This function serve as a kind of unified contact point for all kind
        /// of business functions to trigger loading an item to PackageExplorer data 
        /// represented by an item of PackageCentral. This function triggers UI procedures.
        /// </summary>
        /// <param name="packItem">PackageCentral item to load to</param>
        /// <param name="takeOverEnv">Already loaded environment to take over (alternative 1)</param>
        /// <param name="loadLocalFilename">Local filename to read (alternative 2)</param>
        /// <param name="info">Human information what is loaded</param>
        /// <param name="onlyAuxiliary">Treat as auxiliary load, not main item load</param>
        /// <param name="doNotNavigateAfterLoaded">Disable automatic navigate to behaviour</param>
        /// <param name="takeOverContainer">Already loaded container to take over (alternative 3)</param>
        /// <param name="storeFnToLRU">Store this filename into last recently used list</param>
        /// <param name="indexItems">Index loaded contents, e.g. for animate of event sending</param>
        /// <param name="nextEditMode">Set the edit mode AFTER loading</param>
        public async Task UiLoadPackageWithNew(
            PackageCentralItem packItem,
            AdminShellPackageEnvBase? takeOverEnv = null,
            string? loadLocalFilename = null,
            string? info = null,
            bool onlyAuxiliary = false,
            bool doNotNavigateAfterLoaded = false,
            PackageContainerBase? takeOverContainer = null,
            string? storeFnToLRU = null,
            bool indexItems = false,
            bool preserveEditMode = false,
            bool? nextEditMode = null,
            bool autoFocusFirstRelevant = false)
        {
            // access
            if (packItem == null)
                return;

            // do a bit logic for easy calling via IMainWindow
            if (preserveEditMode)
                nextEditMode = _viewModel.MainMenu?.IsChecked("EditMenu") == true;

            // start loading new stuff
            if (loadLocalFilename != null)
            {
                if (info == null)
                    info = loadLocalFilename;
                Log.Singleton.Info("Loading new AASX from: {0} as auxiliary {1} ..", info, onlyAuxiliary);

                if (!packItem.Load(PackageCentral, loadLocalFilename, loadLocalFilename,
                    overrideLoadResident: true,
                    PackageContainerOptionsBase.CreateDefault(Options.Curr)))
                {
                    Log.Singleton.Error($"Loading local-file {info} as auxiliary {onlyAuxiliary} did not " +
                        $"return any result!");
                    return;
                }
            }
            else
            if (takeOverEnv != null)
            {
                Log.Singleton.Info("Loading new AASX from: {0} as auxiliary {1} ..", info, onlyAuxiliary);
                packItem.TakeOver(takeOverEnv);
            }
            else
            if (takeOverContainer != null)
            {
                Log.Singleton.Info("Loading new AASX from container: {0} as auxiliary {1} ..",
                    "" + takeOverContainer.ToString(), onlyAuxiliary);
                packItem.TakeOver(takeOverContainer);
            }
            else
            {
                Log.Singleton.Error("UiLoadPackageWithNew(): no information what to load!");
                return;
            }

            // establish parents (only for main)
            if (!onlyAuxiliary)
                foreach (var sm in PackageCentral.Main?.AasEnv?.OverSubmodelsOrEmpty())
                    sm?.SetAllParents();

            // displaying
            try
            {
                await RestartUIafterNewPackage(onlyAuxiliary, nextEditMode);

                if (autoFocusFirstRelevant && PackageCentral.Main?.AasEnv is Aas.IEnvironment menv)
                {
                    VisualElementEnvironmentItem.ItemType? eit = null;
                    var nextLevelExpand = false;

                    if (menv.AssetAdministrationShellCount() < 1 && menv.SubmodelCount() >= 1)
                    {
                        // focus on All Submodels
                        eit = VisualElementEnvironmentItem.ItemType.AllSubmodels;
                    }
                    else
                    if (menv.AssetAdministrationShellCount() < 1 && menv.SubmodelCount() < 1
                        && menv.ConceptDescriptionCount() >= 1)
                    {
                        // focus on All CD
                        eit = VisualElementEnvironmentItem.ItemType.AllConceptDescriptions;
                        nextLevelExpand = true;
                    }

                    // now?
                    if (eit.HasValue)
                    {
                        DisplayElements.ExpandAllItems();
                        var ve = DisplayElements.FindAllVisualElementTop()
                            .Where((ve) => ve is VisualElementEnvironmentItem veei && veei.theItemType == eit.Value)
                            .FirstOrDefault();

                        // one level deeper expanded
                        if (nextLevelExpand && ve?.Members != null)
                            foreach (var child in ve.Members)
                                if (child != null)
                                    child.IsExpanded = true;

                        // show this
                        DisplayElements.TrySelectVisualElement(ve, wishExpanded: true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When displaying element tree of {info}, an error occurred");
                return;
            }

            // further actions
            try
            {
                // TODO (MIHO, 2020-12-31): check for ANYUI MIHO
                if (!doNotNavigateAfterLoaded)
                    await Logic?.UiCheckIfActivateLoadedNavTo();

                TriggerPendingReIndexElements();

                if (indexItems && packItem?.Container?.Env?.AasEnv != null)
                    packItem.Container.SignificantElements
                        = new IndexOfSignificantAasElements(packItem.Container.Env.AasEnv);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When performing actions after load of {info}, an error occurred");
                return;
            }

            // record in LRU?
            try
            {
                var lru = PackageCentral.Repositories?.FindLRU();
                if (lru != null && storeFnToLRU.HasContent())
                    lru.Push(packItem?.Container as PackageContainerRepoItem, storeFnToLRU);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"When managing LRU files");
                return;
            }


            // done
            Log.Singleton.Info("AASX {0} loaded.", info);
        }

        public void UiShowRepositories(bool visible)
        {
            //1//
            //// ALWAYS assert an accessible repo (even if invisble)
            //if (PackageCentral.Repositories == null)
            //{
            //if (PackageCentral == null)
            //    return;
            //    PackageCentral.Repositories = new PackageContainerListOfList();
            //    RepoListControl.RepoList = PackageCentral.Repositories;
            //}

            //if (!visible)
            //{
            //    // disable completely
            //    RowDefinitonForRepoList.Height = new GridLength(0.0);
            //}
            //else
            //{
            //    // enable, what has been stored
            //    RowDefinitonForRepoList.Height =
            //            new GridLength(this.ColumnAasRepoGrid.ActualHeight / 2);
            //}
        }

        public async Task PrepareDispEditEntity(
            AdminShellPackageEnvBase package, ListOfVisualElementBasic? entities,
            bool editMode, bool hintMode, bool showIriMode, bool checkSmt,
            DispEditHighlight.HighlightFieldInfo? hightlightField = null)
        {
            // access
            if (PackageCentral == null || DisplayContext == null)
                return;

            // determine some flags
            var tiCds = DisplayElements.SearchVisualElementOnMainDataObject(package?.AasEnv?.ConceptDescriptions) as
                VisualElementEnvironmentItem;

            // update element view?
            DynamicMenu.Menu.Clear();
            var renderHints = await DispEditEntityPanel.DisplayOrEditVisualAasxElement(
                    PackageCentral, DisplayContext,
                    entities, editMode, hintMode, showIriMode, checkSmt, tiCds?.CdSortOrder,
                    flyoutProvider: this,
                    mainWindow: this,
                    appEventProvider: this,
                    hightlightField: hightlightField,
                    superMenu: DynamicMenu.Menu);

            // especially in MAUI: keep the display panel clean, if no rendering was done
            if (renderHints == null || renderHints.showDataPanel == false)
            {
                DispEditEntityPanel.ClearDisplayDefaultStack();
            }

            // panels
            var panelHeight = 48;
            if (renderHints != null && renderHints.showDataPanel == false)
            {
                //1//
                //ContentPanelNoEdit.Visibility = Visibility.Collapsed;
                //ContentPanelEdit.Visibility = Visibility.Collapsed;
                panelHeight = 0;
            }
            else
            {
                if (!editMode)
                {
                    //1//
                    //ContentPanelNoEdit.Visibility = Visibility.Visible;
                    //ContentPanelEdit.Visibility = Visibility.Hidden;
                }
                else
                {
                    //1//
                    //ContentPanelNoEdit.Visibility = Visibility.Hidden;
                    //ContentPanelEdit.Visibility = Visibility.Visible;
                }
            }
            // Reload elements, Drag, Show elements visible or not
            //1// RowContentPanels.Height = new GridLength(panelHeight);

            // scroll or not
            if (renderHints != null && renderHints.scrollingPanel == false)
            {
                //1// ScrollViewerElement.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }
            else
            {
                //1// ScrollViewerElement.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            }

            // further
            //1// ShowContent.IsEnabled = false;
            //1// DragSource.Foreground = Brushes.DarkGray;
            //1// UpdateContent.IsEnabled = false;
            _showContentElement = null;

            // show it
            //1// if (ElementTabControl.SelectedIndex != 0)
            //1//     await Dispatcher.BeginInvoke((Action)(() => ElementTabControl.SelectedIndex = 0));

            // some entities require special handling
            if (entities?.ExactlyOne == true && entities.First() is VisualElementSubmodelElement sme)
            {
                if (sme?.theWrapper is Aas.IFile || sme?.theWrapper is Aas.IBlob)
                {
                    //1// ShowContent.IsEnabled = true;
                    //1// DragSource.Foreground = Brushes.Black;
                    _showContentElement = entities.First();
                }
            }

            if (entities?.ExactlyOne == true
                && this.theOnlineConnection != null && this.theOnlineConnection.IsValid() &&
                this.theOnlineConnection.IsConnected())
            {
                //1// UpdateContent.IsEnabled = true;
                currentEntityForUpdate = entities.First();
            }
        }

        /// <summary>
        /// Based on save information, will redraw the AAS entity (element) view (right).
        /// </summary>
        /// <param name="hightlightField">Highlight field (for find/ replace)</param>
        public async Task RedrawElementViewAsync(DispEditHighlight.HighlightFieldInfo? hightlightField = null)
        {
            await Task.Yield();

            if (DisplayElements == null)
                return;

            // the AAS will cause some more visual effects
            var tvlaas = DisplayElements.SelectedItem as VisualElementAdminShell;
            if (PackageCentral.MainAvailable == true 
                && tvlaas != null && tvlaas.theAas != null && tvlaas.theEnv != null)
            {
                // AAS
                // update graphic left

                // what is AAS specific?
                _viewModel.AasId = AdminShellUtil.EvalToNonNullString("{0}", tvlaas.theAas.Id, "<id missing!>");

                // what is asset specific?
                //1//this.AssetPic.Source = null;
                _viewModel.AssetId = "<id missing!>";
                var asset = tvlaas.theAas.AssetInformation;
                if (asset != null)
                {
                    // text id
                    if (asset.GlobalAssetId != null)
                        _viewModel.AssetId = AdminShellUtil.EvalToNonNullString("{0}", asset.GlobalAssetId);

                    // asset thumbnail
                    try
                    {
                        // identify which stream to use..
                        var picFound = false;

                        // new approach
                        if (PackageCentral.MainAvailable)
                            try
                            {
                                var bytes = PackageCentral.Main.GetThumbnailBytesFromAasOrPackage(tvlaas.theAas.Id);
                                if (bytes != null)
                                    using (var ms = new MemoryStream(bytes))
                                    {
                                        //1//
                                        //// load image
                                        //var bi = new BitmapImage();
                                        //bi.BeginInit();

                                        //// See https://stackoverflow.com/a/5346766/1600678
                                        //bi.CacheOption = BitmapCacheOption.OnLoad;

                                        //bi.StreamSource = ms;
                                        //bi.EndInit();

                                        //this.AssetPic.Source = bi;
                                        //picFound = true;
                                    }
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }

                        // no, ask online server?
                        if (!picFound && this.theOnlineConnection != null
                            && this.theOnlineConnection.IsValid()
                            && this.theOnlineConnection.IsConnected())
                            try
                            {
                                using (var thumbStream = this.theOnlineConnection.GetThumbnailStream())
                                {
                                    if (thumbStream != null)
                                    {
                                        using (var ms = new MemoryStream())
                                        {
                                            //1//
                                            //thumbStream.CopyTo(ms);
                                            //ms.Flush();
                                            //var bitmapdata = ms.ToArray();

                                            //var bi = (BitmapSource)new ImageSourceConverter().ConvertFrom(bitmapdata);
                                            //this.AssetPic.Source = bi;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                            }

                    }
                    catch (Exception ex)
                    {
                        // no error, intended behaviour, as thumbnail might not exist / be faulty in some way
                        // (not violating the spec)
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }
            }

            // for all, prepare the display
            await PrepareDispEditEntity(
                PackageCentral.Main,
                DisplayElements?.SelectedItems,
                 _viewModel.MainMenu?.IsChecked("EditMenu") == true,
                 _viewModel.MainMenu?.IsChecked("HintsMenu") == true,
                 _viewModel.MainMenu?.IsChecked("ShowIriMenu") == true,
                 _viewModel.MainMenu?.IsChecked("CheckSmtElements") == true,
                 hightlightField: hightlightField);

        }

        #endregion
        #region Callbacks
        //===============

        private async void Window_Loaded(object? sender, EventArgs e)
        {
            // create a log file?
            if (Options.Curr.LogFile?.HasContent() == true)
                try
                {
                    _logWriter = new StreamWriter(Options.Curr.LogFile);
                    Log.Singleton.Info("Starting writing log information to {0} ..", Options.Curr.LogFile);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "creating log file: " + Options.Curr.LogFile);
                }

            // basic AnyUI handling
            DisplayContext = new AnyUiDisplayContextMaui(this, this, PackageCentral);
            Logic.DisplayContext = DisplayContext;
            Logic.MainWindow = this;

            // more straight approach in MAUI to handle actions from the enity panel
            DispEditEntityPanel.OutsideAction += HandleEntityPanelOustideAction ;

            // re-load known endpoints
            //1//_securityAccessHandler = new WinGdiSecurityAccessHandler(DisplayContext,
            //1//    knownEndpoints: Options.Curr.KnownEndpoints);

            // making up "empty" picture
            _viewModel.AasId = "<id unknown!>";
            _viewModel.AssetId = "<id unknown!>";

            // logical main menu
            // 1 // var logicalMainMenu = ExplorerMenuFactory.CreateMainMenu();
            _viewModel.MainMenu.DefaultActionAsync = CommandBinding_GeneralDispatch;

            // top level children have other color
            // 1 // logicalMainMenu.DefaultForeground = AnyUiColors.Black;
            // 1 // foreach (var mi in logicalMainMenu)
            // 1 //     if (mi is AasxMenuItem mii)
            // 1 //         mii.Foreground = AnyUiColors.White;

            // WPF main menu
            //1// MainMenu = new AasxMenuWpf();
            //1// MainMenu.LoadAndRender(logicalMainMenu, MenuMain, this.CommandBindings, this.InputBindings);

            // editor modes?
            _viewModel.MainMenu?.SetChecked("EditMenu", Options.Curr.EditMode);
            _viewModel.MainMenu?.SetChecked("HintsMenu", !Options.Curr.NoHints);

            // display elements has a cache
            DisplayElements.ActivateElementStateCache();
            VisualElementEnvironmentItem.SetCdSortOrderByString(Options.Curr.CdSortOrder);

            // show Logo?
            if (Options.Curr.LogoFile != null)
                try
                {
                    //1// var fullfn = System.IO.Path.GetFullPath(Options.Curr.LogoFile);
                    //1// var bi = new BitmapImage(new Uri(fullfn, UriKind.RelativeOrAbsolute));
                    //1// this.LogoImage.Source = bi;
                    //1// this.LogoImage.UpdateLayout();
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }

            // adding the CEF Browser conditionally
            //1// theContentBrowser.Start(Options.Curr.ContentHome, Options.Curr.InternalBrowser);
            //1// CefContainer.Child = theContentBrowser.BrowserControl;

            // window size?
            //1// only Windows via interop, drop it?
            //1// if (Options.Curr.WindowLeft > 0) this.Left = Options.Curr.WindowLeft;
            //1// if (Options.Curr.WindowTop > 0) this.Top = Options.Curr.WindowTop;
            //1// if (Options.Curr.WindowWidth > 0) this.Width = Options.Curr.WindowWidth;
            //1// if (Options.Curr.WindowHeight > 0) this.Height = Options.Curr.WindowHeight;
            //1// if (Options.Curr.WindowMaximized)
            //1//     this.WindowState = WindowState.Maximized;

            var win = App.Current?.Windows.FirstOrDefault();
            if (win != null)
            {
                if (Options.Curr.WindowLeft > 0) win.X = Options.Curr.WindowLeft;
                if (Options.Curr.WindowTop > 0)win.Y = Options.Curr.WindowTop;
                if (Options.Curr.WindowWidth > 0) win.Width = Options.Curr.WindowWidth;
                if (Options.Curr.WindowHeight > 0) win.Height = Options.Curr.WindowHeight;
            }

            // Timer
            StartTimer();

            // attach result search
            //1// ToolFindReplace.Flyout = this;
            //1// ToolFindReplace.ResultSelected += ToolFindReplace_ResultSelected;
            //1// ToolFindReplace.SetProgressBar += SetProgressDownload;

            // Package Central starting ..
            PackageCentral.CentralRuntimeOptions = UiBuildRuntimeOptionsForMainAppLoad();
            PackageCentral.ExecuteMainCommand = this;

            // start with empty repository and load, if given by options
            //1// RepoListControl.PackageCentral = PackageCentral;
            //1// RepoListControl.FlyoutProvider = this;
            //1// RepoListControl.ManageVisuElems = DisplayElements;
            //1// RepoListControl.ExecuteMainCommand = this;
            this.UiShowRepositories(visible: false);

            // event viewer
            //1// UserContrlEventCollection.FlyoutProvider = this;

            // LRU repository?
            var lruFn = PackageContainerListLastRecentlyUsed.BuildDefaultFilename();
            try
            {
                if (System.IO.File.Exists(lruFn))
                {
                    var lru = PackageContainerListLastRecentlyUsed.Load<PackageContainerListLastRecentlyUsed>(lruFn);
                    PackageCentral.Repositories.Add(lru);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while loading last recently used file {lruFn}");
            }

            // Repository pointed by the Options
            if (Options.Curr.AasxRepositoryFns != null && Options.Curr.AasxRepositoryFns.Count > 0)
            {
                foreach (var arf in Options.Curr.AasxRepositoryFns)
                {
                    if (arf?.HasContent() != true)
                        continue;

                    var fr2 = await Logic.UiLoadFileRepositoryAsync(arf, tryLoadResident: true);
                    if (fr2 != null)
                    {
                        this.UiShowRepositories(visible: true);
                        PackageCentral.Repositories.AddAtTop(fr2);
                        Log.Singleton.Info("Added file repository {0} from: {1}.",
                            fr2.Header, arf);
                    }
                    else
                    {
                        Log.Singleton.Error("Error loading file repository from: {0}", arf);
                    }
                }
            }

            // what happens on a repo file click
            //1// RepoListControl.FileDoubleClick += async (senderList, repo, fi) =>
            //1// {
            //1//     //
            //1//     // special case: registry / repo
            //1//     //
            //1// 
            //1//     if (repo is PackageContainerListHttpRestRepository restRepo)
            //1//     {
            //1//         // find a specific location
            //1//         if (PackageContainerHttpRepoSubset.IsValidUriAnyMatch(fi?.Location))
            //1//         {
            //1//             // try load that specific location
            //1//             // check if load fresh or aggregate
            //1//             if (PackageCentral.Main is AdminShellPackageDynamicFetchEnv)
            //1//             {
            //1//                 // load aggregate
            //1//                 Log.Singleton.Info("Aggregating location {0} ..", fi.Location);
            //1//                 var res = await UiSearchRepoAndExtendEnvironmentAsync(
            //1//                     PackageCentral.Main,
            //1//                     fullItemLocation: fi.Location,
            //1//                     trySelect: true);
            //1// 
            //1//                 // error?
            //1//                 if (res == null)
            //1//                     Log.Singleton.Error("Not able to access location {0}", fi.Location);
            //1// 
            //1//                 // in any case, stop here
            //1//                 return;
            //1//             }
            //1//             else
            //1//             {
            //1//                 // load
            //1//                 Log.Singleton.Info("Switching to location {0} ..", fi.Location);
            //1//                 await UiLoadPackageWithNew(PackageCentral.MainItem, null,
            //1//                     fi.Location, onlyAuxiliary: false, preserveEditMode: true);
            //1// 
            //1//                 // in any case, stop here
            //1//                 return;
            //1//             }
            //1//         }
            //1// 
            //1//         // if not a specific location is available, display general dialogue
            //1//         if (true)
            //1//         {
            //1//             var fetchContext = new PackageContainerHttpRepoSubsetFetchContext()
            //1//             {
            //1//                 Record = new ConnectExtendedRecord()
            //1//                 {
            //1//                     BaseType = ConnectExtendedRecord.EvalBaseType(restRepo.PreferredInterface,
            //1//                         ConnectExtendedRecord.BaseTypeEnum.Repository),
            //1//                     BaseAddress = restRepo.Endpoint?.ToString()
            //1//                 }
            //1//             };
            //1// 
            //1//             // refer to (static) function
            //1//             try
            //1//             {
            //1//                 var res = await DispEditHelperEntities.ExecuteUiForFetchOfElements(
            //1//                     PackageCentral, DisplayContext,
            //1//                     ticket: null,
            //1//                     mainWindow: this,
            //1//                     fetchContext: fetchContext,
            //1//                     // merge some HTTP headers?
            //1//                     additionalHeaderData: restRepo.HttpHeaderData,
            //1//                     preserveEditMode: true,
            //1//                     doEditNewRecord: true,
            //1//                     doCheckTainted: true,
            //1//                     doFetchGoNext: false,
            //1//                     doFetchExec: true);
            //1//             }
            //1//             catch (OperationCanceledException)
            //1//             {
            //1//                 Log.Singleton.Info("User cancellation: Repository/ Registry fetch.");
            //1//             }
            //1//             catch (Exception ex)
            //1//             {
            //1//                 Log.Singleton.Error(ex, "when performing Repository/ Registry fetch");
            //1//             }
            //1//         }
            //1//     }
            //1// 
            //1//     //
            //1//     // "normal" file item
            //1//     //
            //1//     if (repo == null || fi == null)
            //1//         return;
            //1// 
            //1//     // safety?
            //1//     if (MainMenu?.IsChecked("FileRepoLoadWoPrompt") != true)
            //1//     {
            //1//         // ask double question
            //1//         if (AnyUiMessageBoxResult.OK != MessageBoxFlyoutShow(
            //1//                 "Load file from AASX file repository?",
            //1//                 "AASX File Repository",
            //1//                 AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
            //1//             return;
            //1//     }
            //1// 
            //1//     // start animation
            //1//     repo.StartAnimation(fi, PackageContainerRepoItem.VisualStateEnum.ReadFrom);
            //1// 
            //1//     // container options
            //1//     var copts = PackageContainerOptionsBase.CreateDefault(Options.Curr);
            //1//     if (fi.ContainerOptions != null)
            //1//         copts = fi.ContainerOptions;
            //1// 
            //1//     // try load ..
            //1//     if (repo is PackageContainerAasxFileRepository restRepository)
            //1//     {
            //1//         if (restRepository.IsAspNetConnection)
            //1//         {
            //1//             var container = await restRepository.LoadAasxFileFromServer(fi.PackageId, PackageCentral.CentralRuntimeOptions);
            //1//             if (container != null)
            //1//             {
            //1//                 await UiLoadPackageWithNew(PackageCentral.MainItem,
            //1//                     takeOverContainer: container, onlyAuxiliary: false,
            //1//                     storeFnToLRU: fi.PackageId);
            //1//             }
            //1// 
            //1//             Log.Singleton.Info($"Successfully loaded AASX Package with PackageId {fi.PackageId}");
            //1// 
            //1//             if (senderList is PackageContainerListControl pclc)
            //1//                 pclc.RedrawStatus();
            //1//         }
            //1//     }
            //1//     else
            //1//     {
            //1//         var location = repo.GetFullItemLocation(fi.Location);
            //1//         if (location == null)
            //1//             return;
            //1//         Log.Singleton.Info($"Auto-load file from repository {location} into container");
            //1// 
            //1//         try
            //1//         {
            //1//             var container = await PackageContainerFactory.GuessAndCreateForAsync(
            //1//                 PackageCentral,
            //1//                 location,
            //1//                 location,
            //1//                 overrideLoadResident: true,
            //1//                 autoAuthenticate: Options.Curr.AutoAuthenticateUris,
            //1//                 takeOver: fi,
            //1//                 fi.ContainerList,
            //1//                 containerOptions: copts,
            //1//                 runtimeOptions: PackageCentral.CentralRuntimeOptions);
            //1// 
            //1//             if (container == null)
            //1//                 Log.Singleton.Error($"Failed to load AASX from {location}");
            //1//             else
            //1//                 await UiLoadPackageWithNew(PackageCentral.MainItem,
            //1//                     takeOverContainer: container, onlyAuxiliary: false,
            //1//                     storeFnToLRU: location);
            //1// 
            //1//             Log.Singleton.Info($"Successfully loaded AASX {location}");
            //1// 
            //1//             if (senderList is PackageContainerListControl pclc)
            //1//                 pclc.RedrawStatus();
            //1//         }
            //1//         catch (Exception ex)
            //1//         {
            //1//             Log.Singleton.Error(ex, $"When auto-loading {location}");
            //1//         }
            //1//     }
            //1// 
            //1// 
            //1// };

            // what happens on a file drop -> dispatch
            //1// RepoListControl.FileDrop += async (senderList, fr, files) =>
            //1// {
            //1//     // access
            //1//     if (files == null || files.Length < 1)
            //1//         return;
            //1// 
            //1//     // hand over the full list for potential bulk adding
            //1//     if (fr != null)
            //1//         await fr.AddByListOfAasxFn(PackageCentral, files);
            //1// 
            //1//     // more than one?
            //1//     foreach (var fn in files)
            //1//     {
            //1//         // repo?
            //1//         var ext = Path.GetExtension(fn).ToLower();
            //1//         if (ext == ".json")
            //1//         {
            //1//             // try handle as repository
            //1//             var newRepo = Logic.UiLoadFileRepository(fn);
            //1//             if (newRepo != null)
            //1//             {
            //1//                 PackageCentral.Repositories.AddAtTop(newRepo);
            //1//             }
            //1//             // no more files ..
            //1//             return;
            //1//         }
            //1// 
            //1//         // aasx?
            //1//         if (fr != null && ext == ".aasx")
            //1//         {
            //1//             // add?
            //1//             fr.AddByAasxFn(PackageCentral, fn);
            //1//         }
            //1//     }
            //1// };

            // initialize menu
            _viewModel.MainMenu?.SetChecked("FileRepoLoadWoPrompt", Options.Curr.LoadWithoutPrompt);
            _viewModel.MainMenu?.SetChecked("ShowIriMenu", Options.Curr.ShowIdAsIri);
            _viewModel.MainMenu?.SetChecked("VerboseConnect", Options.Curr.VerboseConnect);
            _viewModel.MainMenu?.SetChecked("AnimateElements", Options.Curr.AnimateElements);
            _viewModel.MainMenu?.SetChecked("ObserveEvents", Options.Curr.ObserveEvents);
            _viewModel.MainMenu?.SetChecked("CompressEvents", Options.Curr.CompressEvents);
            _viewModel.MainMenu?.SetChecked("CheckSmtElements", Options.Curr.CheckSmtElements);

            // the UI application might receive events from items in the package central
            if (PackageCentral != null)
            {
                PackageCentral.ChangeEventHandler = (data) =>
                {
                    if (data.Reason == PackCntChangeEventReason.Exception)
                        Log.Singleton.Info("PackageCentral events: " + data.Info);
                    DisplayElements.PushEvent(new AnyUiLambdaActionPackCntChange() { Change = data });
                    return false;
                };
            }

            // wire history bheaviour
            if (Logic?.LocationHistory != null)
            {
                Logic.LocationHistory.VisualElementRequested += async (s4, historyItem) =>
                {
                    //1// await ButtonHistory_ObjectRequested(s4, historyItem);
                };

                Logic.LocationHistory.HistoryActive += (s5, active) =>
                {
                    //1// ButtonHistoryBack.IsEnabled = active;
                };
            }

            // nearly last task here ..
            Log.Singleton.Info("Application started ..");

            // start with a new file
            PackageCentral.MainItem.New();
            await RedrawAllAasxElementsAsync();

            // pump all pending log messages (from plugins) into the
            // log / status line, before setting the last information
            MainTimer_HandleLogMessages();

            // Try to load?            
            if (Options.Curr.AasxToLoad?.HasContent() == true)
            {
                var location = Options.Curr.AasxToLoad;
                try
                {
                    Log.Singleton.Info($"Auto-load main package at application start " +
                        $"from {location} into container");

                    var container = await PackageContainerFactory.GuessAndCreateForAsync(
                        PackageCentral,
                        location,
                        location,
                        overrideLoadResident: true,
                        autoAuthenticate: Options.Curr.AutoAuthenticateUris,
                        containerOptions: PackageContainerOptionsBase.CreateDefault(Options.Curr),
                        runtimeOptions: PackageCentral.CentralRuntimeOptions);

                    if (container == null)
                        Log.Singleton.Error($"Failed to auto-load AASX from {location}");
                    else if (container.Env?.AasEnv != null && container.Env.AasEnv.AllIdentifiables().Count() < 1)
                    {
                        Log.Singleton.Info(StoredPrint.Color.Blue,
                            $"Auto-load request seem to result in empty data! Auto-load location: {location}");
                    }
                    else
                        await UiLoadPackageWithNew(PackageCentral.MainItem,
                            takeOverContainer: container, onlyAuxiliary: false, indexItems: true,
                            nextEditMode: Options.Curr.EditMode);

                    Log.Singleton.Info($"Successfully auto-loaded AASX {location}");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When auto-loading main from: {location}");
                }
            }

            if (Options.Curr.AuxToLoad?.HasContent() == true)
            {
                var location = Options.Curr.AuxToLoad;
                try
                {
                    Log.Singleton.Info($"Auto-load auxiliary package at application start " +
                        $"from {location} into container");

                    var container = await PackageContainerFactory.GuessAndCreateForAsync(
                        PackageCentral,
                        location,
                        location,
                        overrideLoadResident: true,
                        autoAuthenticate: Options.Curr.AutoAuthenticateUris,
                        containerOptions: PackageContainerOptionsBase.CreateDefault(Options.Curr),
                        runtimeOptions: PackageCentral.CentralRuntimeOptions);

                    if (container == null)
                        Log.Singleton.Error($"Failed to auto-load AASX from {location}");
                    else
                        await UiLoadPackageWithNew(PackageCentral.AuxItem,
                            takeOverContainer: container, onlyAuxiliary: true, indexItems: false);

                    Log.Singleton.Info($"Successfully auto-loaded AASX {location}");
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When auto-loading aux from: {location}");
                }
            }

            // open last UI elements
            //1// if (Options.Curr.ShowEvents)
            //1//     PanelConcurrentSetVisibleIfRequired(true, targetEvents: true);

            // trigger re-index
            TriggerPendingReIndexElements();

            // script file to launch?
            if (Options.Curr.ScriptFn.HasContent())
            {
                try
                {
                    Log.Singleton.Info("Opening and executing '{0}' for script commands.", Options.Curr.ScriptFn);
                    Logic?.StartScriptFile(Options.Curr.ScriptFn, _viewModel.MainMenu, Logic);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"when executing script file {Options.Curr.ScriptFn}");
                }
            }

            // script file to launch?
            if (Options.Curr.ScriptCmd.HasContent())
            {
                try
                {
                    Log.Singleton.Info("Executing '{0}' as direct script commands.", Options.Curr.ScriptCmd);
                    Logic?.StartScriptCommand(Options.Curr.ScriptCmd, _viewModel.MainMenu, Logic);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"when executing script file {Options.Curr.ScriptCmd}");
                }
            }

            //1// AasxIntegrationBaseWpf.CountryFlagWpf.LoadImage();

            // TEST
            if (true)
            {
                //var page = new TextEditorFlyoutPage(DisplayContext);
                //bool result = await page.ShowAsync(Navigation);

                //if (result)
                //{
                //    ;
                //}


                await MauiTestTree.Flyouts.MauiFlyoutTestCases.ExecuteMauiFlyoutTestCase(this, this, 1);
            }
        }

        //
        // Timer + Events to the outside
        //

        protected bool _timerRunning = false;
        protected bool _timerAlreadyBusy = false;

        protected void StartTimer()
        {
            _timerRunning = true;
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
            {
                // Update UI here
                //  MyLabel.Text = DateTime.Now.ToString("HH:mm:ss");

                if (_timerAlreadyBusy)
                    return true;

                // guarded!
                _timerAlreadyBusy = true;
                _ = MainTimer_Tick(null, null); 
                _timerAlreadyBusy = false;

                return _timerRunning; // true = keep running, false = stop
            });
        }

        protected void StopTimer()
        {
            _timerRunning = false;
        }

        private async void ToolFindReplace_ResultSelected(AasxSearchUtil.SearchResultItem resultItem)
        {
            // have a result?
            if (resultItem == null || resultItem.businessObject == null)
                return;

            // for valid display, app needs to be in edit mode
            if (_viewModel.MainMenu.IsChecked("EditMenu") != true)
            {
                await MessageBoxFlyoutShowAsync(
                    "The application needs to be in edit mode to show found entities correctly. Aborting.",
                    "Find and Replace",
                    AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Hand);
                return;
            }

            // add to "normal" event quoue
            DispEditEntityPanel.AddWishForOutsideAction(
                new AnyUiLambdaActionRedrawAllElements(
                    nextFocus: resultItem.businessObject,
                    isExpanded: true,
                    highlightField: new DispEditHighlight.HighlightFieldInfo(
                        resultItem.containingObject, resultItem.foundObject, resultItem.foundHash),
                    onlyReFocus: true));
        }

        public static string? GetLogLineClearText(string? input)
        {
            return input?.Replace("\r", "").Replace("\n", "");
        }

        public static Color GetLogLineColor(bool isBackground, StoredPrint.Color color)
        {
            if (Application.Current?.RequestedTheme == AppTheme.Light)
            {
                // Light
                switch (color)
                {
                    default:
                        throw ExhaustiveMatch.Failed(color);
                    case StoredPrint.Color.Black:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("White", Colors.White)
                            : Colors.Black;
                    case StoredPrint.Color.Blue:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("PrimaryLight", Colors.LightBlue)
                            : Colors.Black;
                    case StoredPrint.Color.Yellow:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("WarningLight", Colors.Orange)
                            : Colors.Black;
                    case StoredPrint.Color.Red:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("ErrorDark", Color.FromRgb(0xd4, 0x20, 0x44))
                            : Colors.White;
                }
            }
            else
            {
                // Dark
                switch (color)
                {
                    default:
                        throw ExhaustiveMatch.Failed(color);
                    case StoredPrint.Color.Black:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("Gray900", Colors.DarkGray)
                            : Colors.White;
                    case StoredPrint.Color.Blue:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("Primary", Colors.DarkBlue)
                            : Colors.White;
                    case StoredPrint.Color.Yellow:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("WarningDark", Colors.DarkOrange)
                            : Colors.White;
                    case StoredPrint.Color.Red:
                        return isBackground
                            ? XamlHelpers.GetDynamicRessource("ErrorLight", Colors.OrangeRed)
                            : Colors.Black;
                }
            }
        }

        private string _lastMessageBlue = "";
        private string _lastMessageError = "";

        private void MainTimer_HandleLogMessages()
        {
            // pop log messages from the plug-ins into the Stored Prints in Log
            Plugins.PumpPluginLogsIntoLog(FlyoutLoggingPush);

            // check for Stored Prints in Log
            StoredPrint sp;
            while ((sp = Log.Singleton.PopLastShortTermPrint()) != null)
            {
                // pop
                _viewModel.LogLine = "" + GetLogLineClearText(sp.msg);
                _viewModel.LogBg = GetLogLineColor(isBackground: true, sp.color);
                _viewModel.LogFg = GetLogLineColor(isBackground: false, sp.color);

                // last messages?
                switch (sp.color)
                {
                    case StoredPrint.Color.Blue:
                        _lastMessageBlue = _viewModel.LogLine;
                        break;
                    case StoredPrint.Color.Yellow:
                        _lastMessageBlue = _viewModel.LogLine;
                        break;
                    case StoredPrint.Color.Red:
                        _lastMessageError = _viewModel.LogLine;
                        break;
                }

                // message window
                //1// _messageReportWindow?.AddStoredPrint(sp);

                // log?
                if (_logWriter != null)
                    try
                    {
                        _logWriter.WriteLine(sp.ToString());
                        _logWriter.Flush();
                    }
                    catch (Exception ex)
                    {
                        LogInternally.That.SilentlyIgnoredError(ex);
                    }
            }

            // always tell the errors
            var ne = Log.Singleton.NumberErrors;
            var nb = Log.Singleton.NumberBlues;
            if (ne > 0)
            {
                _viewModel.AttentionText = "Errors: " + ne;
                _viewModel.AttentionBg = GetLogLineColor(isBackground: true, StoredPrint.Color.Red);
                _viewModel.AttentionFg = GetLogLineColor(isBackground: false, StoredPrint.Color.Red);
            }
            else
            if (nb > 0)
            {
                _viewModel.AttentionText = "Major: " + nb;
                _viewModel.AttentionBg = GetLogLineColor(isBackground: true, StoredPrint.Color.Yellow);
                _viewModel.AttentionFg = GetLogLineColor(isBackground: false, StoredPrint.Color.Yellow);
            }
            else
            {
                _viewModel.AttentionText = "-";
                _viewModel.AttentionBg = GetLogLineColor(isBackground: true, StoredPrint.Color.Black);
                _viewModel.AttentionFg = GetLogLineColor(isBackground: false, StoredPrint.Color.Black);
            }
        }

        public void TriggerPendingReIndexElements()
        {
            _mainTimer_LastCheckForReIndexElements = DateTime.Now;
            _mainTimer_PendingReIndexElements = true;
        }

        private async Task MainTimer_HandleLambdaAction(AnyUiLambdaActionBase lab)
        {
            // nothing
            if (lab == null)
                return;

            // recurse??
            if (lab is AnyUiLambdaActionList list && list.Actions != null)
                foreach (var ac in list.Actions)
                    await MainTimer_HandleLambdaAction(ac);

            // what to do?
            if (lab is AnyUiLambdaActionRedrawAllElementsBase wish)
            {
                // 2022-02-28: Try to kee focus
                if (wish.RedrawCurrentEntity && wish.NextFocus == null)
                {
                    // figure out the current business object
                    if (DisplayElements != null && DisplayElements.SelectedItem != null &&
                        DisplayElements.SelectedItem != null)
                        wish.NextFocus = DisplayElements.SelectedItem.GetMainDataObject();
                }

                // edit mode affects the total element view
                if (!wish.OnlyReFocus)
                    await RedrawAllAasxElementsAsync();

                // the selection will be shifted ..
                if (wish.NextFocus != null && DisplayElements != null)
                {
                    // for later search in visual elements, expand them all in order to be absolutely 
                    // sure to find business object
                    DisplayElements.ExpandAllItems();

                    // now: search
                    // MIHO 24-06-09: add dereferenced object to find operation vars, submodelrefs?
                    DisplayElements.TrySelectMainDataObject(
                        wish.NextFocus, wish.IsExpanded,
                        alsoDereferenceObjects: true);
                }

                // fake selection
                DispEditHighlight.HighlightFieldInfo? hfi = null;
                if (lab is AnyUiLambdaActionRedrawAllElements wishhl)
                    hfi = wishhl.HighlightField;
                await RedrawElementViewAsync(hightlightField: hfi);

                // ok
                DisplayElements.Refresh();
                TakeOverContentEnable(false);
            }

            if (lab is AnyUiLambdaActionContentsChanged)
            {
                // enable button
                TakeOverContentEnable(true);
            }

            if (lab is AnyUiLambdaActionContentsTakeOver)
            {
                // rework list
                ContentTakeOver_Click("null", new EventArgs());
            }

            if (lab is AnyUiLambdaActionNavigateTo tempNavTo)
            {
                // do some more adoptions
                // MIHO: I think this "adopitons" were made for resident AAS environments directly
                // staying in the RAM of the PackageCentral
                // This is not the usual case of activation of "file repository"
                var rf = tempNavTo.targetReference.Copy();

                if (tempNavTo.translateAssetToAAS
                    && rf?.IsValid() == true
                    && rf.Keys.Count == 1
                    && rf.Keys.First().Type == Aas.KeyTypes.GlobalReference)
                //TODO (jtikekar, 0000-00-00): KeyType.AssetInformation
                {
                    // try to find possible environments containing the asset and try making
                    // replacement
                    foreach (var pe in PackageCentral.GetAllPackageEnv())
                    {
                        if (pe?.AasEnv?.AssetAdministrationShells == null)
                            continue;

                        foreach (var aas in pe.AasEnv.AllAssetAdministrationShells())
                            if (rf.GetAsExactlyOneKey().Value.Equals(aas.AssetInformation.GlobalAssetId))
                            {
                                rf = aas.GetReference();
                                break;
                            }
                    }
                }

                // handle it by UI (may include repo lookup)
                await UiHandleNavigateTo(rf, alsoDereferenceObjects: tempNavTo.alsoDereferenceObjects);
            }

            if (lab is AnyUiLambdaActionDisplayContentFile tempDispCont)
            {
                try
                {
                    BrowserDisplayLocalFile(tempDispCont.fn, tempDispCont.mimeType,
                        preferInternal: tempDispCont.preferInternalDisplay);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(
                        ex, $"While displaying content file {tempDispCont.fn} requested by lambda");
                }
            }

            if (lab is AnyUiLambdaActionPackCntChange
                || lab is AnyUiLambdaActionSelectMainObjects)
            {
                DisplayElements.PushEvent(lab);
            }

            if (lab is AnyUiLambdaActionPluginUpdateAnyUi update)
            {
                // A plugin asks to re-render an exisiting panel.
                // Can get this information?
                var renderedInfo = DispEditEntityPanel.GetLastRenderedRoot();

                if (renderedInfo != null
                    && renderedInfo.Item2 is AnyUiPanel renderedPanel
                    && renderedPanel.Children != null
                    && renderedPanel.Children.Count > 0)
                {
                    // first step: invoke plugin?
                    var plugin = Plugins.FindPluginInstance(update.PluginName);
                    if (plugin != null && plugin.HasAction("update-anyui-visual-extension"))
                    {
                        try
                        {
                            plugin.InvokeAction(
                                "update-anyui-visual-extension", renderedPanel, renderedInfo.Item1,
                                AnyUiDisplayContextMaui.SessionSingletonMaui);
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex,
                                $"update AnyUI based visual extension for plugin {update.PluginName}");
                        }
                    }

                    // 2nd step: redisplay                                                          
                    DispEditEntityPanel.RedisplayRenderedRoot(
                        renderedPanel,
                        update.UpdateMode,
                        useInnerGrid: update.UseInnerGrid);
                }
                else
                {
                    // hard re-display
                    throw new NotImplementedException();
                }
            }

            if (lab is AnyUiLambdaActionModalPanelReRender lamprr)
            {
                if (currentFlyoutControl != null)
                    currentFlyoutControl.LambdaActionAvailable(lamprr);
            }

            if (lab is AnyUiLambdaActionEntityPanelReRender larrep)
            {
                UiHandleReRenderAnyUiInEntityPanel("", larrep.Mode, larrep.UseInnerGrid,
                    updateElemsOnly: larrep.UpdateElemsOnly);
            }

            if (lab is AnyUiLambdaActionReIndexIdentifiables lareii)
            {
                TriggerPendingReIndexElements();
            }
        }

#if NOT_ANYMORE
        private async Task MainTimer_HandleEntityPanel()
        {
            // check if Display/ Edit Control has some work to do ..
            try
            {
                if (DispEditEntityPanel != null && DispEditEntityPanel.WishForOutsideAction != null)
                {
                    while (DispEditEntityPanel.WishForOutsideAction.Count > 0)
                    {
                        var temp = DispEditEntityPanel.WishForOutsideAction[0];
                        DispEditEntityPanel.WishForOutsideAction.RemoveAt(0);

                        await MainTimer_HandleLambdaAction(temp);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While responding to a user interaction");
            }
        }
#else

        protected async Task HandleEntityPanelOustideAction(AnyUiLambdaActionBase action)
        {
            // check if Display/ Edit Control has some work to do ..
            try
            {
                await MainTimer_HandleLambdaAction(action);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While responding to a action requested by the entity panel");
            }
        }

#endif

        #region Load from Repository
        // -------------------------

        protected class LoadFromFileRepositoryInfo
        {
            public Aas.IReferable? Referable;
            public object? BusinessObject;
        }

        private async Task<LoadFromFileRepositoryInfo?> LoadFromFileRepository(PackageContainerRepoItem? fi,
            Aas.IReference? requireReferable = null)
        {
            // access single file repo
            var fileRepo = PackageCentral.Repositories.FindRepository(fi);
            if (fileRepo == null)
                return null;

            // which file?
            var location = fileRepo.GetFullItemLocation(fi?.Location);
            if (location == null)
                return null;

            // try load (in the background/ RAM) first..
            PackageContainerBase? container = null;
            try
            {
                Log.Singleton.Info($"Auto-load file from repository {location} into container");
                container = await PackageContainerFactory.GuessAndCreateForAsync(
                    PackageCentral,
                    location,
                    location,
                    overrideLoadResident: true,
                    autoAuthenticate: Options.Curr.AutoAuthenticateUris,
                    null, null,
                    PackageContainerOptionsBase.CreateDefault(Options.Curr),
                    runtimeOptions: PackageCentral.CentralRuntimeOptions);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"When auto-loading {location}");
            }

            // if successfull ..
            if (container != null)
            {
                // .. try find business object!
                LoadFromFileRepositoryInfo res = new LoadFromFileRepositoryInfo();
                if (requireReferable != null)
                {
                    var rri = new ExtendEnvironment.ReferableRootInfo();
                    res.Referable = container.Env?.AasEnv.FindReferableByReference(requireReferable, rootInfo: rri);
                    res.BusinessObject = res.Referable;

                    // do some special decoding because auf Meta Model V3
                    if (rri.Asset != null)
                        res.BusinessObject = rri.Asset;
                }

                // only proceed, if business object was found .. else: close directly
                if (requireReferable != null && res.Referable == null)
                    container.Close();
                else
                {
                    // make sure the user wants to change
                    if (_viewModel.MainMenu?.IsChecked("FileRepoLoadWoPrompt") != true)
                    {
                        // ask double question
                        if (AnyUiMessageBoxResult.OK != await MessageBoxFlyoutShowAsync(
                                "Load file from AASX file repository?",
                                "AASX File Repository",
                                AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                            return null;
                    }

                    // start animation
                    fileRepo.StartAnimation(fi, PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                    // activate
                    await UiLoadPackageWithNew(PackageCentral.MainItem,
                        takeOverContainer: container, onlyAuxiliary: false);

                    Log.Singleton.Info($"Successfully loaded AASX {location}");
                }

                // return bo to focus
                return res;
            }

            return null;
        }

        #endregion

        #region Rendering of elements
        // --------------------------

        private void UiHandleReRenderAnyUiInEntityPanel(
            string pluginName, AnyUiRenderMode mode, bool useInnerGrid = false,
            Dictionary<AnyUiUIElement, bool>? updateElemsOnly = null)
        {
            // A plugin asks to re-render an existing panel.
            // Can get this information?
            var renderedInfo = DispEditEntityPanel.GetLastRenderedRoot();

            if (renderedInfo != null
                && renderedInfo.Item2 is AnyUiPanel renderedPanel
                && renderedPanel.Children != null
                && renderedPanel.Children.Count > 0)
            {
                // first step: invoke plugin?
                // Note: is OK to have plugin name to null in order to disable calling plugin
                var plugin = Plugins.FindPluginInstance(pluginName);
                if (plugin != null && plugin.HasAction("update-anyui-visual-extension"))
                {
                    try
                    {
                        plugin.InvokeAction(
                            "update-anyui-visual-extension", renderedPanel, DisplayContext,
                            AnyUiDisplayContextMaui.SessionSingletonMaui);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex,
                            $"update AnyUI based visual extension for plugin {pluginName}");
                    }
                }

                // 2nd step: redisplay
                DispEditEntityPanel.RedisplayRenderedRoot(
                    renderedPanel,
                    mode: mode,
                    useInnerGrid: useInnerGrid,
                    updateElemsOnly: updateElemsOnly);
            }
            else
            {
                // hard re-display
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Loading from Repos ..
        // --------------------------

        public async Task<Aas.IIdentifiable?> UiSearchRepoAndExtendEnvironmentAsync(
            AdminShellPackageEnvBase packEnv,
            Aas.IReference? workRef = null,
            string? fullItemLocation = null,
            bool trySelect = false)
        {
            await Task.Yield();

            // access
            if (packEnv == null || (workRef?.IsValid() != true && fullItemLocation?.HasContent() != true))
                return null;

            // check if env is dynamic fetch
            if (packEnv is not AdminShellPackageDynamicFetchEnv dynPack)
                return null;

            // try get a copy of the fetch record
            var context = (dynPack.GetContext() as PackageContainerHttpRepoSubsetFetchContext);
            var record = (context?.Record as ConnectExtendedRecord)?.Copy();
            if (record == null)
                return null;

            // make sure there is a base
            if (record.BaseAddress?.HasContent() != true)
                return null;

            // search for AAS?
            BaseUriDict? baseUris = null;
            var searches = new List<Tuple<ConnectExtendedRecord, BaseUriDict, string>>();
            if (workRef?.IsValid() == true)
            {
                // search for AAS?
                if (workRef.Count() >= 1 && workRef.Keys[0].Type == Aas.KeyTypes.AssetAdministrationShell)
                {
                    // want to search for an AAS
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleAas);
                    record.AasId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(
                        new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }

                // search for Asset?
                if (workRef.Count() >= 1 && workRef.Keys[0].Type == Aas.KeyTypes.GlobalReference)
                {
                    // want to search for an Asset?
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.AasByAssetLink);
                    record.AssetId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(
                        new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }

                // search for Submodel?
                if (workRef.Count() >= 1 && (workRef.Keys[0].Type == Aas.KeyTypes.GlobalReference
                                          || workRef.Keys[0].Type == Aas.KeyTypes.Submodel))
                {
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleSM);
                    record.SmId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(
                        new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }

                // search for CD?
                if (workRef.Count() >= 1 && (workRef.Keys[0].Type == Aas.KeyTypes.GlobalReference
                                          || workRef.Keys[0].Type == Aas.KeyTypes.ConceptDescription))
                {
                    // want to search for an CD?
                    record.SetQueryChoices(ConnectExtendedRecord.QueryChoice.SingleCD);
                    record.CdId = workRef.Keys[0].Value;
                    var basedLoc = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                    baseUris = basedLoc.BaseUris;
                    fullItemLocation = basedLoc.Location.ToString();
                    searches.Add(
                        new Tuple<ConnectExtendedRecord, BaseUriDict, string>(record, baseUris, fullItemLocation));
                }
            }

            // any searches?
            if (searches.Count < 1)
                return null;

            // try to load in sequence, until new Identifiable is found
            // TODO (MIHO, 2024-01-01): take over those options from existing container
            var foundIdfs = new List<Aas.IIdentifiable>();
            foreach (var search in searches)
            {
                var containerOptions = new PackageContainerHttpRepoSubset.
                    PackageContainerHttpRepoSubsetOptions(PackageContainerOptionsBase.CreateDefault(Options.Curr),
                    search.Item1);
                containerOptions.BaseUris = search.Item2;

                var newIdfs = new List<Aas.IIdentifiable>();
                var loadedIdfs = new List<Aas.IIdentifiable>();

                var loadRes = await PackageContainerHttpRepoSubset.LoadFromSourceToTargetAsync(
                    fullItemLocation: search.Item3,
                    targetEnv: packEnv,
                    loadNew: false,
                    trackNewIdentifiables: newIdfs,
                    trackLoadedIdentifiables: loadedIdfs,
                    containerOptions: containerOptions,
                    runtimeOptions: PackageCentral.CentralRuntimeOptions);

                if (loadRes != null && newIdfs.Count >= 1)
                {
                    foundIdfs.AddRange(newIdfs);
                    // may be in the future, we want also NOT to break here?
                    break;
                }
            }

            if (foundIdfs.Count < 1)
                return null;

            // rebuild display elements
            DisplayElements.RebuildAasxElements(
                PackageCentral, PackageCentral.Selector.Main, _viewModel.MainMenu?.IsChecked("EditMenu") == true,
                lazyLoadingFirst: false /* TODO */);

            var newIdf = foundIdfs.FirstOrDefault();

            // display
            if (trySelect)
            {
                var veFound = DisplayElements.SearchVisualElementOnMainDataObject(newIdf, alsoDereferenceObjects: true);
                if (veFound != null)
                {
                    // show ve
                    DisplayElements.ExpandAllItems();
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
                    // remember in history
                    Logic?.LocationHistory?.Push(veFound);
                    // fake selection
                    await RedrawElementViewAsync();
                    DisplayElements.Refresh();
                    TakeOverContentEnable(false);
                }
            }

            // the end
            return newIdf;
        }

        private async Task UiHandleNavigateTo(
            Aas.IReference targetReference,
            bool alsoDereferenceObjects = true)
        {
            // access
            if (targetReference == null || targetReference.Keys.Count < 1)
                return;

            // make a copy of the Reference for searching
            VisualElementGeneric? veFound = null;
            var work = targetReference.Copy();

            try
            {
                // remember some further supplementary search information
                var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(work);
                work = sri.CleanReference;

                // for later search in visual elements, expand them all in order to be absolutely 
                // sure to find business object
                this.DisplayElements.ExpandAllItems();

                // incrementally make it unprecise
                bool firstTime = true;
                while (work.Keys.Count > 0)
                {
                    // try to find a business object in the package
                    object? bo = null;
                    if (PackageCentral.MainAvailable && PackageCentral.Main.AasEnv != null)
                        bo = PackageCentral.Main.AasEnv.FindReferableByReference(work);

                    // Prio 2: check for connected repositories
                    // (An actual "up-to-date" hit in repo in more valuable than a stored container on file space)
                    if (firstTime && bo == null)
                    {
                        bo = await UiSearchRepoAndExtendEnvironmentAsync(PackageCentral.Main, work);
                        firstTime = false;
                    }

                    // if not, may be in aux package (not sure, if this works)
                    if (bo == null && PackageCentral.Aux != null && PackageCentral.Aux.AasEnv != null)
                        bo = PackageCentral.Aux.AasEnv.FindReferableByReference(work);

                    // if not, may look into the AASX file repo
                    if (bo == null && PackageCentral.Repositories != null)
                    {
                        // find?
                        PackageContainerRepoItem? fi = null;
                        if (work.Keys[0].Type == Aas.KeyTypes.GlobalReference)
                            //TODO (jtikekar, 0000-00-00): KeyTypes.AssetInformation
                            fi = await PackageCentral.Repositories.FindByAssetId(work.Keys[0].Value.Trim());
                        if (work.Keys[0].Type == Aas.KeyTypes.AssetAdministrationShell)
                            fi = await PackageCentral.Repositories.FindByAasId(work.Keys[0].Value.Trim());

                        var boInfo = await LoadFromFileRepository(fi, work);
                        bo = boInfo?.BusinessObject;
                    }

                    // anything found?
                    if (bo != null)
                    {
                        // try to look up in visual elements
                        if (this.DisplayElements != null)
                        {
                            DisplayElements.ExpandAllItems();
                            var ve = this.DisplayElements.SearchVisualElementOnMainDataObject(bo,
                                alsoDereferenceObjects: alsoDereferenceObjects, sri: sri);
                            if (ve != null)
                            {
                                veFound = ve;
                                break;
                            }
                        }
                    }

                    // make it more unprecice
                    work.Keys.RemoveAt(work.Keys.Count - 1);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While retrieving element requested for navigate to");
            }

            // if successful, try to display it
            try
            {
                if (veFound != null)
                {
                    // show ve
                    DisplayElements.TrySelectVisualElement(veFound, wishExpanded: true);
                    // remember in history
                    Logic?.LocationHistory?.Push(veFound);
                    // fake selection
                    await RedrawElementViewAsync();
                    DisplayElements.Refresh();
                    TakeOverContentEnable(false);
                }
                else
                {
                    // everything is in default state, push adequate button history
                    var veTop = DisplayElements.GetDefaultVisualElement();
                    Logic?.LocationHistory?.Push(veTop);
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While displaying element requested for navigate to");
            }
        }

        #endregion

        #region Handle application events
        // ------------------------------

        private async Task HandleApplicationEvent(
            AasxIntegrationBase.AasxPluginResultEventBase evt,
            Plugins.PluginInstance? pluginInstance)
        {
            try
            {
                // Navigate To
                //============

                if (evt is AasxIntegrationBase.AasxPluginResultEventNavigateToReference evtNavTo
                    && evtNavTo.targetReference != null && evtNavTo.targetReference.Keys.Count > 0)
                {
                    Log.Singleton.Info("Plugin requested to naviagte to: " + evtNavTo.targetReference.ToStringExtended(1));

                    await UiHandleNavigateTo(evtNavTo.targetReference);
                }

                // Visually select referables in the tree
                //=======================================

                if (evt is AasxIntegrationBase.AasxPluginResultEventVisualSelectEntities evtVisSel
                    && evtVisSel.Referables != null && evtVisSel.Referables.Count > 0)
                {
                    // quite EXPERIMENTAL!
                    // info
                    Log.Singleton.Info($"Plugin request to select {evtVisSel.Referables.Count} Referables.");

                    // set all parents required?
                    foreach (var sm in PackageCentral.Main?.AasEnv?.OverSubmodelsOrEmpty().ForEachSafe())
                        sm?.SetAllParents();

                    // ugly 3 step approach

                    // step 1 : expand all nodes in order to search them ..
                    DisplayElements.ExpandAllItems();
                    await Task.Delay(1000);

                    // step 2 : wait for expand events to be internally digested by treeview
                    DisplayElements.TryExpandMainDataObjects(evtVisSel.Referables);
                    await Task.Delay(1000);

                    // step 3 : now select
                    DisplayElements.TrySelectMainDataObjects(evtVisSel.Referables);
                }

                // Display Content Url
                //====================

                if (evt is AasxIntegrationBase.AasxPluginResultEventDisplayContentFile evtDispCont
                    && evtDispCont.fn != null)
                    try
                    {
                        if (evtDispCont.SaveInsteadDisplay)
                        {
                            var proposeFn = System.IO.Path.GetFileName(evtDispCont.fn);
                            if (evtDispCont.ProposeFn?.HasContent() == true)
                                proposeFn = System.IO.Path.GetFileName(evtDispCont.ProposeFn);

                            var uc = await DisplayContext.MenuSelectSaveFilenameAsync(
                                null, "File", "Save file ..", proposeFn, "All files (*.*)|*.*", "No access", requireNoFlyout: true);

                            if (uc?.Result == true)
                            {
                                System.IO.File.Copy(evtDispCont.fn, uc.TargetFileName);
                                Log.Singleton.Info("Provided file copied to: " + uc.TargetFileName);
                            }
                        }
                        else
                        {
                            BrowserDisplayLocalFile(evtDispCont.fn, evtDispCont.mimeType,
                                preferInternal: evtDispCont.preferInternalDisplay);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"While displaying/ saving content file {evtDispCont.fn} requested by plug-in");
                    }

                // Redraw All
                //===========

                if (evt is AasxIntegrationBase.AasxPluginResultEventRedrawAllElements)
                {
                    if (DispEditEntityPanel != null)
                    {
                        // figure out the current business object
                        object? nextFocus = null;
                        if (DisplayElements != null && DisplayElements.SelectedItem != null &&
                            DisplayElements.SelectedItem != null)
                            nextFocus = DisplayElements.SelectedItem.GetMainDataObject();

                        // add to "normal" event quoue
                        DispEditEntityPanel.AddWishForOutsideAction(
                            new AnyUiLambdaActionRedrawAllElements(nextFocus));
                    }
                }

                // Select AAS entity
                //=======================

                var evSelectEntity = evt as AasxIntegrationBase.AasxPluginResultEventSelectAasEntity;
                if (evSelectEntity != null)
                {
                    //1// var uc = new SelectAasEntityFlyout(
                    //1//     PackageCentral, PackageCentral.Selector.MainAuxFileRepo,
                    //1//     evSelectEntity.filterEntities);
                    //1// this.StartFlyoverModal(uc);
                    //1// if (uc.DiaData.ResultKeys != null)
                    //1// {
                    //1//     // formulate return event
                    //1//     var retev = new AasxIntegrationBase.AasxPluginEventReturnSelectAasEntity();
                    //1//     retev.sourceEvent = evt;
                    //1//     retev.resultKeys = uc.DiaData.ResultKeys;
                    //1// 
                    //1//     // fire back
                    //1//     pluginInstance?.InvokeAction("event-return", retev,
                    //1//         AnyUiDisplayContextMaui.SessionSingletonMaui);
                    //1// }
                }

                // Select File
                //============

                if (evt is AasxIntegrationBase.AasxPluginResultEventSelectFile fileSel)
                {
                    // ask
                    if (!fileSel.SaveDialogue)
                    {
                        //
                        // Open
                        //

                        var options = new PickOptions
                        {
                            PickerTitle = fileSel.Title,
                            FileTypes = AnyUiDisplayContextMaui.GetMauiFilePickerFileTypeFromWpfFilter(fileSel.Filter)
                        };

                        if (!fileSel.MultiSelect)
                        {
                            var result = await FilePicker.Default.PickAsync(options);

                            if (result != null)
                            {
                                // formulate return event
                                var retev = new AasxIntegrationBase.AasxPluginEventReturnSelectFile();
                                retev.sourceEvent = evt;
                                retev.FileNames = new[] { result.FullPath };
                                
                                // fire back
                                pluginInstance?.InvokeAction("event-return", retev,
                                    AnyUiDisplayContextMaui.SessionSingletonMaui);
                            }
                        }
                        else
                        {
                            var result = await FilePicker.Default.PickMultipleAsync(options);

                            if (result != null && result.Count() > 0)
                            {
                                // formulate return event
                                var retev = new AasxIntegrationBase.AasxPluginEventReturnSelectFile();
                                retev.sourceEvent = evt;
                                retev.FileNames = result.Select((r) => r.FullPath).ToArray();

                                // fire back
                                pluginInstance?.InvokeAction("event-return", retev,
                                    AnyUiDisplayContextMaui.SessionSingletonMaui);
                            }
                        }
                    }


                    // 2 The MAUI equivalent: FilePicker
                    // ✅ This is the correct replacement

                    //1// if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                    //1// FileDialog dlg = null;
                    //1// if (fileSel.SaveDialogue)
                    //1//     dlg = new Microsoft.Win32.SaveFileDialog();
                    //1// else
                    //1//     dlg = new Microsoft.Win32.OpenFileDialog();
                    //1// dlg.InitialDirectory = DetermineInitialDirectory(PackageCentral.MainItem.Filename);
                    //1// if (fileSel.Title != null)
                    //1//     dlg.Title = fileSel.Title;
                    //1// if (fileSel.FileName != null)
                    //1//     dlg.FileName = fileSel.FileName;
                    //1// if (fileSel.DefaultExt != null)
                    //1//     dlg.DefaultExt = fileSel.DefaultExt;
                    //1// if (fileSel.Filter != null)
                    //1//     dlg.Filter = fileSel.Filter;
                    //1// if (dlg is Microsoft.Win32.OpenFileDialog ofd)
                    //1//     ofd.Multiselect = fileSel.MultiSelect;
                    //1// var res = dlg.ShowDialog();
                    //1// if (Options.Curr.UseFlyovers) this.CloseFlyover();
                    //1// 
                    //1// // act
                    //1// if (res == true)
                    //1// {
                    //1//     // formulate return event
                    //1//     var retev = new AasxIntegrationBase.AasxPluginEventReturnSelectFile();
                    //1//     retev.sourceEvent = evt;
                    //1//     retev.FileNames = dlg.FileNames;
                    //1// 
                    //1//     // fire back
                    //1//     pluginInstance?.InvokeAction("event-return", retev,
                    //1//         AnyUiDisplayContextWpf.SessionSingletonWpf);
                    //1// }
                }

                // Message Box
                //============

                if (evt is AasxIntegrationBase.AasxPluginResultEventMessageBox evMsgBox)
                {
                    //1// // modal
                    //1// var uc = new MessageBoxFlyout(evMsgBox.Message, evMsgBox.Caption,
                    //1//                 evMsgBox.Buttons, evMsgBox.Image);
                    //1// this.StartFlyoverModal(uc);
                    //1// 
                    //1// // fire back
                    //1// pluginInstance?.InvokeAction("event-return",
                    //1//     new AasxIntegrationBase.AasxPluginEventReturnMessageBox()
                    //1//     {
                    //1//         sourceEvent = evt,
                    //1//         Result = uc.Result
                    //1//     },
                    //1//     AnyUiDisplayContextWpf.SessionSingletonWpf);
                }

                // Invoke other plugin
                //====================

                if (evt is AasxIntegrationBase.AasxPluginResultEventInvokeOtherPlugin evInvOth)
                {
                    // result
                    object? res = null;

                    // find plugin
                    var pi = Plugins.FindPluginInstance(evInvOth.PluginName);

                    // invoke?
                    if (pi != null && pi.HasAction(evInvOth.Action, evInvOth.UseAsync))
                    {
                        if (evInvOth.UseAsync)
                            res = await pi.InvokeActionAsync(evInvOth.Action, evInvOth.Args);
                        else
                            res = pi.InvokeAction(evInvOth.Action, evInvOth.Args);
                    }

                    // fire back
                    pluginInstance?.InvokeAction("event-return",
                        new AasxIntegrationBase.AasxPluginEventReturnInvokeOther()
                        {
                            sourceEvent = evt,
                            ResultData = res
                        },
                        AnyUiDisplayContextMaui.SessionSingletonMaui);
                }

                // Re-render Any UI Panels
                //========================

                if (evt is AasxIntegrationBase.AasxPluginEventReturnUpdateAnyUi update)
                {
                    UiHandleReRenderAnyUiInEntityPanel(update.PluginName, update.Mode, useInnerGrid: true);
                }

                // Push AAS events coming from the plugins into the package central
                //=================================================================

                if (evt is AasxIntegrationBase.AasxPluginResultEventPushSomeEvents someEvt)
                {
                    if (someEvt.AasEvents != null)
                        foreach (var aevt in someEvt.AasEvents)
                        {
                            PackageCentral?.PushEvent(aevt);
                        }

                    var animated = false;
                    if (someEvt.AnimateSingleEvents != null)
                        foreach (var ase in someEvt.AnimateSingleEvents)
                        {
                            DisplayElements.PushEvent(new AnyUiLambdaActionPackCntChange()
                            {
                                Change = new PackCntChangeEventData()
                                {
                                    Container = PackageCentral.MainItem.Container,
                                    Reason = PackCntChangeEventReason.ValueUpdateSingle,
                                    ThisElem = ase,
                                    ParentElem = ase?.Parent,
                                    Info = "Plugin value update"
                                }
                            });
                            animated = true;
                        }

                    if (animated)
                        CheckIfToFlushEvents();
                }

                #endregion
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(
                    ex, $"While responding to a event; may be from plug-in {"" + pluginInstance?.name}");
            }
        }

        private List<AasxIntegrationBase.AasxPluginResultEventBase> _applicationEvents
            = new List<AasxPluginResultEventBase>();

        public void PushApplicationEvent(AasxIntegrationBase.AasxPluginResultEventBase evt)
        {
            if (evt == null)
                return;
            _applicationEvents.Add(evt);
        }

        private async Task MainTimer_HandleApplicationEvents()
        {
            // check if a plug-in has some work to do ..
            foreach (var lpi in Plugins.LoadedPlugins.Values)
            {
                var evt = lpi.InvokeAction("get-events") as AasxIntegrationBase.AasxPluginResultEventBase;
                if (evt != null)
                    await HandleApplicationEvent(evt, lpi);
            }

            // check for application events from main app
            while (_applicationEvents.Count > 0)
            {
                var evt = _applicationEvents[0];
                _applicationEvents.RemoveAt(0);
                await HandleApplicationEvent(evt, null);
            }
        }

        public class EventHandlingStatus
        {
            public DateTime LastQueuedEventRequest = DateTime.Now;
            public DateTime LastReceivedEventTimeStamp = DateTime.MinValue;
            public bool UpdateValuePending = false;
            public bool GetEventsPending = false;
            public int UserErrorsIndicated = 0;
            public bool UserErrorsSuppress = false;

            public void Reset()
            {
                LastQueuedEventRequest = DateTime.Now;
                UpdateValuePending = false;
                GetEventsPending = false;
                UserErrorsIndicated = 0;
                UserErrorsSuppress = false;
            }
        }

        private AnimateDemoValues _mainTimer_AnimateDemoValues = new AnimateDemoValues();

        private void MainTimer_CheckAnimationElements(
            double deltaSecs,
            Aas.IEnvironment? env,
            IndexOfSignificantAasElements significantElems)
        {
            // trivial
            if (env == null || significantElems == null || _viewModel.MainMenu?.IsChecked("AnimateElements") != true)
                return;

            var animated = false;

            // find elements?
            foreach (var rec in significantElems.Retrieve(env, SignificantAasElement.ValueAnimation))
            {
                // valid?
                if (rec?.Reference == null || rec.Reference.Keys.Count < 1 || rec.LiveObject == null)
                    continue;

                // which SME?
                if (rec.LiveObject is Aas.Property prop)
                {
                    _mainTimer_AnimateDemoValues.Animate(prop,
                        emitEvent: (prop2, evi2) =>
                        {
                            // Animate the event visually; create a change event for this.
                            // Note: this might by not ideal, final state
                            /* TODO (MIHO, 2021-10-28): Check, if a better solution exists 
                             * to instrument event updates in a way that they're automatically
                             * visualized */
                            DisplayElements.PushEvent(new AnyUiLambdaActionPackCntChange()
                            {
                                Change = new PackCntChangeEventData()
                                {
                                    Container = PackageCentral.MainItem.Container,
                                    Reason = PackCntChangeEventReason.ValueUpdateSingle,
                                    ThisElem = prop2,
                                    ParentElem = prop2?.Parent,
                                    Info = "Animated value update"
                                }
                            });
                            animated = true;
                        });
                }
            }

            // if any ..
            if (animated)
                CheckIfToFlushEvents();
        }

        private void MainTimer_CheckDiaryDateToEmitEvents(
            DateTime lastTime,
            Aas.IEnvironment? env,
            IndexOfSignificantAasElements significantElems,
            bool emitCompressed)
        {
            // trivial
            if (env == null || significantElems == null || _viewModel.MainMenu?.IsChecked("ObserveEvents") != true)
                return;

            // do this twice
            for (int i = 0; i < 2; i++)
            {
                // divider
                var see = (new[] {
                    SignificantAasElement.EventStructureChangeOutwards,
                    SignificantAasElement.EventUpdateValueOutwards})[i];

                // update events?
                foreach (var rec in significantElems.Retrieve(env, see))
                {
                    // valid?
                    if (rec?.Reference == null || rec.Reference.Keys.Count < 1 || rec.LiveObject == null)
                        continue;
                    var refEv = rec.LiveObject as Aas.BasicEventElement;
                    if (refEv == null)
                        continue;

                    // now, find the observable (with timestamping!)
                    var observable = (IDiaryData)env.FindReferableByReference(refEv.Observed);

                    // some special cases
                    if (true == refEv.Observed?.Matches(
                            Aas.KeyTypes.GlobalReference, "AASENV",
                            MatchMode.Relaxed))
                    {
                        observable = env;
                    }

                    // diary data available
                    if (observable?.DiaryData == null)
                        continue;

                    // get the flags
                    var newCreate = observable.DiaryData
                        .TimeStamp[(int)DiaryDataDef.TimeStampKind.Create] >= lastTime;

                    var newUpdate = observable.DiaryData
                        .TimeStamp[(int)DiaryDataDef.TimeStampKind.Update] >= lastTime;

                    // first check
                    if (!newCreate && !newUpdate)
                        continue;

                    // prepare event payloads
                    var plStruct = new AasPayloadStructuralChange();
                    var plUpdate = new AasPayloadUpdateValue();

                    // for the overall change check, we rely on the timestamping
                    if ((i == 0) || ((i == 1) && newUpdate))
                    {
                        // closure logic
                        var storedI = i;

                        if (observable is Aas.IReferable referable)
                            referable.RecurseOnReferables(null,
                                includeThis: true,
                                lambda: (o, parents, rf) =>
                                {
                                    // further interest?
                                    if (rf == null || rf.DiaryData == null ||
                                    ((rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Create]
                                       < lastTime)
                                      &&
                                      (rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Update]
                                       < lastTime)))
                                        return false;

                                    // yes, inspect further and also go deeper
                                    if (rf.DiaryData.Entries != null)
                                    {
                                        var todel = new List<IAasDiaryEntry>();
                                        foreach (var de in rf.DiaryData.Entries)
                                        {
                                            if (storedI == 0 && de is AasPayloadStructuralChangeItem sci)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plStruct.Changes.Add(sci);

                                                // delete
                                                todel.Add(de);
                                            }

                                            if (storedI == 1 && de is AasPayloadUpdateValueItem uvi)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plUpdate.Values.Add(uvi);

                                                // delete
                                                todel.Add(de);
                                            }
                                        }
                                        foreach (var de in todel)
                                            rf.DiaryData.Entries.Remove(de);
                                    }

                                    // deeper
                                    return true;
                                });
                        if (observable is Aas.Environment environment)
                            environment.RecurseOnReferables(null,
                                includeThis: true,
                                lambda: (o, parents, rf) =>
                                {
                                    // further interest?
                                    if (rf == null || rf.DiaryData == null ||
                                    ((rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Create]
                                       < lastTime)
                                      &&
                                      (rf.DiaryData.TimeStamp[(int)DiaryDataDef.TimeStampKind.Update]
                                       < lastTime)))
                                        return false;

                                    // yes, inspect further and also go deeper
                                    if (rf.DiaryData.Entries != null)
                                    {
                                        var todel = new List<IAasDiaryEntry>();
                                        foreach (var de in rf.DiaryData.Entries)
                                        {
                                            if (storedI == 0 && de is AasPayloadStructuralChangeItem sci)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plStruct.Changes.Add(sci);

                                                // delete
                                                todel.Add(de);
                                            }

                                            if (storedI == 1 && de is AasPayloadUpdateValueItem uvi)
                                            {
                                                // TODO (MIHO, 2021-10-09): prepare path to be relative

                                                // queue event
                                                plUpdate.Values.Add(uvi);

                                                // delete
                                                todel.Add(de);
                                            }
                                        }
                                        foreach (var de in todel)
                                            rf.DiaryData.Entries.Remove(de);
                                    }

                                    // deeper
                                    return true;
                                });
                    }

                    // send event?
                    if (plStruct.Changes.Count < 1 && plUpdate.Values.Count < 1)
                        continue;

                    // send event
                    var ev = new AasEventMsgEnvelope(
                        DateTime.UtcNow,
                        source: refEv.GetReference(),
                        sourceSemanticId: refEv.SemanticId,
                        observableReference: refEv.Observed,
                        // dead-csharp off
                        //observableSemanticId: (observable as IGetSemanticId)?.GetSemanticId());
                        // dead-csharp on
                        // TODO (jtikekar, 0000-00-00): IDiaryData support
                        observableSemanticId: (observable as Aas.IHasSemantics)?.SemanticId);

                    if (plStruct.Changes.Count >= 1)
                        ev.PayloadItems.Add(plStruct);

                    if (plUpdate.Values.Count >= 1)
                        ev.PayloadItems.Add(plUpdate);

                    // emit it to PackageCentral or to buffer?
                    if (emitCompressed)
                        _eventCompressor?.Push(ev);
                    else
                        PackageCentral?.PushEvent(ev);
                }
            }
        }

        private void MainTimer_CheckTaintedIdentifiables(
            DateTime lastTime,
            Aas.IEnvironment? env)
        {
            // trivial
            if (env == null || DisplayElements == null)
                return;

            // find visual elements for Identifiables and check for tainted state
            foreach (var ve in DisplayElements.FindAllVisualElementTopToIdentifiable())
                if (ve is ITaintableIdentifiable ttidf)
                {
                    var tt = ttidf.GetTaintedTime();
                    if (tt == null || tt < lastTime)
                        continue;

                    // kick off redisplay
                    ve.RefreshFromMainData();
                }
        }

        protected EventHandlingStatus _eventHandling = new EventHandlingStatus();

        private void MainTimer_PeriodicalTaskForSelectedEntity()
        {
            // some container options are required
            var copts = PackageCentral?.MainItem?.Container?.ContainerOptions;

            //
            // Investigate on Update Value Events
            // Note: for the time being, Events will be only valid, if Event and observed entity are 
            // within the SAME Submodel
            //
            var veSelected = DisplayElements.SelectedItem;

            if (veSelected != null
                && true == copts?.StayConnected
                && Options.Curr.StayConnectOptions.HasContent()
                && Options.Curr.StayConnectOptions.ToUpper().Contains("SIM")
                && !_eventHandling.UpdateValuePending
                && (DateTime.Now - _eventHandling.LastQueuedEventRequest).TotalMilliseconds > copts.UpdatePeriod)
            {
                _eventHandling.LastQueuedEventRequest = DateTime.Now;

                try
                {
                    // for update values, do not concern about plugins, but use superior Submodel,
                    // as they will relate to this
                    var veSubject = veSelected;
                    if (veSelected is VisualElementPluginExtension)
                        veSubject = veSelected.Parent;

                    // now, filter for know applications
                    if (!(veSubject is VisualElementSubmodelRef || veSubject is VisualElementSubmodelElement))
                        return;

                    // will always require a root Submodel
                    var smrSel = veSelected.FindFirstParent((ve) => (ve is VisualElementSubmodelRef), includeThis: true)
                        as VisualElementSubmodelRef;
                    if (smrSel != null && smrSel.theSubmodel != null)
                    {
                        // parents need to be set
                        var rootSm = smrSel.theSubmodel;
                        rootSm.SetAllParents();

                        // check, if the Submodel has interesting events
                        foreach (var ev in smrSel.theSubmodel.SubmodelElements.FindDeep<Aas.BasicEventElement>((x) =>
                            (true == x?.SemanticId?.Matches(
                                AasxPredefinedConcepts.AasEvents.Static.CD_UpdateValueOutwards.Id
                                ))))
                        {
                            // Submodel defines an events for outgoing value updates -> does the observed scope
                            // lie in the selection?
                            var klObserved = ev.Observed?.Keys;
                            var klSelected = veSubject.BuildKeyListToTop(includeAas: false);
                            // no, klSelected shall lie in klObserved
                            if (klObserved != null && klSelected != null &&
                                klSelected.StartsWith(klObserved,
                                emptyIsTrue: false, matchMode: MatchMode.Relaxed))
                            {
                                // take a shortcut
                                if (PackageCentral?.MainItem?.Container is PackageContainerNetworkHttpFile cntHttp
                                    && cntHttp.ConnectorPrimary is PackageConnectorHttpRest connRest)
                                {
                                    Task.Run(async () =>
                                    {
                                        try
                                        {
                                            var evSnd = await
                                                connRest.SimulateUpdateValuesEventByGetAsync(
                                                    smrSel.theSubmodel,
                                                    ev,
                                                    veSubject.GetDereferencedMainDataObject() as Aas.IReferable,
                                                    timestamp: DateTime.Now,
                                                    topic: "MY-TOPIC",
                                                    subject: "ANY-SUBJECT");
                                            if (evSnd)
                                            {
                                                _eventHandling.UpdateValuePending = true;
                                                _eventHandling.UserErrorsSuppress = false;
                                                _eventHandling.UserErrorsIndicated = 0;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            if (!_eventHandling.UserErrorsSuppress)
                                            {
                                                Log.Singleton.Error(ex,
                                                    "periodically triggering event for simulated update (time-out)");
                                                _eventHandling.UserErrorsIndicated++;

                                                if (_eventHandling.UserErrorsIndicated > 3)
                                                {
                                                    Log.Singleton.Info("Too many repetitive time-outs. Disabling!");
                                                    _eventHandling.UserErrorsSuppress = true;
                                                }
                                            }
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "periodically checking for triggering events");
                }
            }

            // Kick off all event updates?
            if (copts != null
                && Options.Curr.StayConnectOptions.HasContent()
                && Options.Curr.StayConnectOptions.ToUpper().Contains("REST-QUEUE")
                && !_eventHandling.GetEventsPending
                && PackageCentral?.MainItem?.Container is PackageContainerNetworkHttpFile cntHttp2
                && cntHttp2.ConnectorPrimary is PackageConnectorHttpRest connRest2
                && (DateTime.Now - _eventHandling.LastQueuedEventRequest).TotalMilliseconds > copts.UpdatePeriod)
            {
                // mutex!
                _eventHandling.GetEventsPending = true;
                _eventHandling.LastQueuedEventRequest = DateTime.Now;

                // async
                Task.Run(async () =>
                {
                    try
                    {
                        // prepare query
                        var qst = "/geteventmessages";
                        if (_eventHandling.LastReceivedEventTimeStamp != DateTime.MinValue)
                            qst += "/time/"
                                   + AasEventMsgEnvelope.TimeToString(_eventHandling.LastReceivedEventTimeStamp);

                        // execute and digest results
                        var lastTS = await
                            connRest2.PullEvents(qst);

                        // remember for next time
                        if (lastTS != DateTime.MinValue)
                            _eventHandling.LastReceivedEventTimeStamp = lastTS;
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex,
                            "pulling events from REST (time-out?)");
                    }
                    finally
                    {
                        // unlock mutex
                        _eventHandling.GetEventsPending = false;
                    }
                });
            }
        }

        public async Task MainTaimer_HandleIncomingAasEvents()
        {
            int nEvent = 0;
            while (true)
            {
                // try handle a reasonable number of events ..
                nEvent += 1;
                if (nEvent > 100)
                    break;

                // access
                var ev = PackageCentral?.EventBufferEditor?.PopEvent();
                if (ev == null)
                    break;

                // log viewer
                //1// UserContrlEventCollection.PushEvent(ev);

                //1// // inform current Flyover?
                //1// if (currentFlyoutControl is IFlyoutAgent fosc)
                //1//     fosc.GetAgent()?.PushEvent(ev);
                //1// // dead-csharp off
                //1// // inform agents?
                //1// foreach (var fa in UserControlAgentsView.Children)
                //1//     fa.GetAgent()?.PushEvent(ev);

                // push into plugins
                Plugins.PushEventIntoPlugins(ev);

                // to be applicable, the event message Observable has to relate into Main's environment
                var foundObservable = PackageCentral?.Main?.AasEnv?.FindReferableByReference(ev?.ObservableReference);
                if (foundObservable == null)
                    return;

                //
                // Update values?
                //
                var changedSomething = false;
                if (foundObservable is Aas.Submodel || foundObservable is Aas.ISubmodelElement)
                    foreach (var pluv in ev!.GetPayloads<AasPayloadUpdateValue>())
                    {
                        changedSomething = changedSomething || (pluv.Values != null && pluv.Values.Count > 0);

                        // update value received
                        _eventHandling.UpdateValuePending = false;
                    }

                // stupid
                if (Options.Curr.StayConnectOptions.ToUpper().Contains("SIM")
                    && changedSomething)
                {
                    // just for test
                    DisplayElements.RefreshAllChildsFromMainData(DisplayElements.SelectedItem);
                    DisplayElements.Refresh();

                    // apply white list for automatic redisplay
                    // Note: do not re-display plugins!!
                    var ves = DisplayElements.SelectedItem;
                    if (ves != null && (ves is VisualElementSubmodelRef || ves is VisualElementSubmodelElement))
                        await RedrawElementViewAsync();
                }
            }
        }

        private DateTime _mainTimer_LastCheckForDiaryEvents;
        private DateTime _mainTimer_LastCheckForTaintedIdentifiables;
        private DateTime _mainTimer_LastCheckForAnimationElements = DateTime.Now;

        private bool _mainTimer_PendingReIndexElements = false;
        private DateTime _mainTimer_LastCheckForReIndexElements = DateTime.Now;

        private async Task MainTimer_Tick(object sender, EventArgs e)
        {
            // different functions
            MainTimer_HandleLogMessages();
#if NOT_ANYMORE
            await MainTimer_HandleEntityPanel();
#endif
            await MainTimer_HandleApplicationEvents();

            // diary dates -> events, animation
            if (PackageCentral?.MainItem?.Container?.SignificantElements != null)
            {
                MainTimer_CheckDiaryDateToEmitEvents(
                    _mainTimer_LastCheckForDiaryEvents,
                    PackageCentral.MainItem.Container.Env?.AasEnv,
                    PackageCentral.MainItem.Container.SignificantElements,
                    emitCompressed: _viewModel.MainMenu?.IsChecked("CompressEvents") == true);
                _mainTimer_LastCheckForDiaryEvents = DateTime.UtcNow;

                // do animation?
                var deltaSecs = (DateTime.Now - _mainTimer_LastCheckForAnimationElements).TotalSeconds;

                if (deltaSecs >= 0.1)
                {
                    MainTimer_CheckAnimationElements(
                        deltaSecs,
                        PackageCentral.MainItem.Container.Env?.AasEnv,
                        PackageCentral.MainItem.Container.SignificantElements);
                    _mainTimer_LastCheckForAnimationElements = DateTime.Now;
                }
            }

            // flags for tainted Identifiables
            MainTimer_CheckTaintedIdentifiables(
                _mainTimer_LastCheckForTaintedIdentifiables,
                PackageCentral.MainItem.Container?.Env?.AasEnv);
            _mainTimer_LastCheckForTaintedIdentifiables = DateTime.UtcNow;

            // pending re-index?
            var deltaSecs2 = (DateTime.Now - _mainTimer_LastCheckForReIndexElements).TotalSeconds;
            if (deltaSecs2 >= 1.0 && _mainTimer_PendingReIndexElements)
            {
                // dis-engage
                _mainTimer_PendingReIndexElements = false;

                // be modest for the time being
                PackageCentral.ReIndexIdentifiables();

                // Info
                Log.Singleton.Info("Re-indexing Identifiables for faster access.");
            }

            // normal stuff
            MainTimer_PeriodicalTaskForSelectedEntity();
            await MainTaimer_HandleIncomingAasEvents();
            DisplayElements.UpdateFromQueuedEvents();
        }

#endregion

        #region Progress
        // --------------

        private void SetProgressOverallIsEnabled(bool active)
        {
            //1// BorderDisplayElements.IsEnabled = active;
            //1// BorderEditElements.IsEnabled = active;
            //1// BorderContainerList.IsEnabled = active;
            //1// MenuMain.IsEnabled = active;
        }

        protected bool _progressOverallActive = false;

        private void SetProgressOverall(bool active, string message)
        {
            _progressOverallActive = active;

            //1// ProgressBarDownload.Dispatcher.BeginInvoke(
            //1//     System.Windows.Threading.DispatcherPriority.Background,
            //1//     new Action(() => {
            //1//         TextBlockProgressOverall.Background = (active) ? Brushes.DarkGreen : Brushes.White;
            //1//         TextBlockProgressOverall.Text = "" + message;
            //1//         ButtonProgressOverallClear.Visibility = (active) ? Visibility.Visible : Visibility.Collapsed;
            //1//         // an active == true will set all controls to isEnabled == false!! and vice versa
            //1//         SetProgressOverallIsEnabled(!active);
            //1//     }));
        }

        private void ButtonProgressOverallClear_Click(object sender, EventArgs e)
        {
            SetProgressOverall(false, "");
            if (PackageCentral.CentralRuntimeOptions?.CancellationTokenSource != null)
                PackageCentral.CentralRuntimeOptions.CancellationTokenSource.Cancel();
        }

        private void SetProgressDownload()
        {
            SetProgressDownload(0.0, "");
        }

        private void SetProgressDownload(double? percent, string message = null)
        {
            //1// if (percent.HasValue && percent.Value.IsFinite())
            //1//     ProgressBarDownload.Dispatcher.BeginInvoke(
            //1//                 System.Windows.Threading.DispatcherPriority.Background,
            //1//                 new Action(() => ProgressBarDownload.Value = percent.Value));
            //1// 
            //1// if (message != null)
            //1//     LabelProgressBarDownload.Dispatcher.BeginInvoke(
            //1//         System.Windows.Threading.DispatcherPriority.Background,
            //1//         new Action(() => LabelProgressBarDownload.Content = message));
        }

        #endregion

        #region ButtonHistory
        // ------------------

        private async Task ButtonHistory_ObjectRequested(object sender, VisualElementHistoryItem hi)
        {
            // be careful
            try
            {
                // try access visual element directly?
                var ve = hi?.VisualElement;
                if (ve != null && DisplayElements.Contains(ve))
                {
                    // is directly contain in actual tree
                    // show it
                    if (DisplayElements.TrySelectVisualElement(ve, wishExpanded: true))
                    {
                        // fake selection
                        await RedrawElementViewAsync();
                        DisplayElements.Refresh();
                        TakeOverContentEnable(false);

                        // done
                        return;
                    }
                }

                // no? Try to find the business object
                var bo = hi?.VisualElement?.GetMainDataObject();
                if (bo != null)
                {
                    if (DisplayElements.TrySelectMainDataObject(bo, wishExpanded: true, alsoDereferenceObjects: true))
                    {
                        // fake selection
                        await RedrawElementViewAsync();
                        DisplayElements.Refresh();
                        TakeOverContentEnable(false);

                        // done
                        return;
                    }
                }

                // no? .. is there a way to another file?
                if (PackageCentral.Repositories != null && hi?.ReferableAasId != null
                    && hi.ReferableReference != null)
                {
                    // try lookup file in file repository
                    var fi = await PackageCentral.Repositories.FindByAasId(hi.ReferableAasId.Trim());
                    if (fi == null)
                    {
                        Log.Singleton.Info(
                            $"History: Cannot lookup aas id {hi.ReferableAasId} in file repository.");
                        return;
                    }

                    // remember some further supplementary search information
                    var sri = ListOfVisualElement.StripSupplementaryReferenceInformation(hi.ReferableReference);

                    // load it (safe)
                    bo = null;
                    try
                    {
                        var boInfo = await LoadFromFileRepository(fi, sri.CleanReference);
                        bo = boInfo?.BusinessObject;
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"While retrieving file for {hi.ReferableAasId} from file repository");
                    }

                    // still proceed?
                    VisualElementGeneric? veFocus = null;
                    if (bo != null && this.DisplayElements != null)
                    {
                        veFocus = this.DisplayElements.SearchVisualElementOnMainDataObject(bo,
                            alsoDereferenceObjects: true, sri: sri);
                        if (veFocus == null)
                        {
                            Log.Singleton.Error(
                                $"Cannot lookup requested element within loaded file from repository.");
                            return;
                        }
                    }

                    // if successful, try to display it
                    try
                    {
                        // show ve
                        DisplayElements?.TrySelectVisualElement(veFocus!, wishExpanded: true);
                        // remember in history
                        //TODO (MIHO, 0000-00-00): this was a bug??
                        // ButtonHistory.Push(veFocus);
                        // fake selection
                        await RedrawElementViewAsync();
                        DisplayElements!.Refresh();
                        TakeOverContentEnable(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, "While displaying element requested by back button.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "While displaying element requested by plug-in");
            }
        }

        #endregion

        #region Status line
        // ----------------

        /// <summary>
        /// Clears the status line and pending errors.
        /// </summary>
        public void StatusLineClear()
        {
            _lastMessageBlue = "";
            _lastMessageError = "";
            Log.Singleton.ClearNumberErrors();
            _viewModel.LogLine = "";
            _viewModel.LogBg = GetLogLineColor(isBackground: true, StoredPrint.Color.Black);
            _viewModel.LogFg = GetLogLineColor(isBackground: false, StoredPrint.Color.Black);
            _viewModel.LogFontWeight = FontWeight.Regular;
            SetProgressDownload();
        }

        public void ShowLastMessage(StoredPrint.Color showColor)
        {
            // text
            switch (showColor)
            {
                case StoredPrint.Color.Blue:
                    _viewModel.LogLine = "" + _lastMessageBlue;
                    break;

                case StoredPrint.Color.Red:
                    _viewModel.LogLine = "" + _lastMessageError;
                    break;
            }

            // colors
            _viewModel.LogBg = GetLogLineColor(isBackground: true, showColor);
            _viewModel.LogFg = GetLogLineColor(isBackground: false, showColor);
            _viewModel.LogFontWeight = FontWeight.Regular;
        }

        /// <summary>
        /// Show log in a window / list perceivable for the user.
        /// </summary>
        public void LogShow()
        {
            //1// // show only if not present
            //1// if (_messageReportWindow != null)
            //1// {
            //1//     // this is ridiculous, but this seems to make the trick
            //1//     // https://stackoverflow.com/questions/257587/bring-a-window-to-the-front-in-wpf
            //1//     _messageReportWindow.Activate();
            //1//     _messageReportWindow.Topmost = true;  // important
            //1//     _messageReportWindow.Topmost = false; // important
            //1//     _messageReportWindow.Focus();         // important
            //1//     return;
            //1// }
            //1// 
            //1// // Collect all the stored log prints
            //1// IEnumerable<StoredPrint> Prints()
            //1// {
            //1//     var prints = Log.Singleton.GetStoredLongTermPrints();
            //1//     if (prints != null)
            //1//     {
            //1//         foreach (var sp in prints)
            //1//         {
            //1//             yield return sp;
            //1//             if (sp.stackTrace != null)
            //1//                 yield return new StoredPrint("    Stacktrace: " + sp.stackTrace);
            //1//         }
            //1//     }
            //1// }
            //1// 
            //1// // show (non modal)
            //1// _messageReportWindow = new MessageReportWindow(Prints());
            //1// _messageReportWindow.Closed += (s2, e2) =>
            //1// {
            //1//     _messageReportWindow = null;
            //1// };
            //1// _messageReportWindow.Show();
        }

        protected TextWriter? _logWriter = null;

        //1// protected MessageReportWindow _messageReportWindow = null;

        private void ReportControls_Tapped(object sender, TappedEventArgs e)
        {
            if (sender == AttentionLabel)
            {
                // something important
                if (Log.Singleton.NumberErrors > 0 && _lastMessageError?.HasContent() == true)
                    ShowLastMessage(StoredPrint.Color.Red);
                else
                    if (Log.Singleton.NumberBlues > 0 && _lastMessageBlue?.HasContent() == true)
                    ShowLastMessage(StoredPrint.Color.Blue);
            }
        }

        private async void ReportControls_Click(object sender, EventArgs e)
        {
            if (sender == ReportButton)
            {
                // start the page
                var page = new MessageReportPage(Log.Singleton.GetDirectLongTermPrints());
                await Navigation.PushModalAsync(page);
            }

            if (sender == StatusLineClearButton)
            {
                // let look all fresh
                StatusLineClear();
            }

            //1// if (sender == ButtonClear)
            //1// {
            //1//     StatusLineClear();
            //1// }
            //1// 
            //1// if (sender == ButtonLogShow)
            //1// {
            //1//     LogShow();
            //1// }
        }

        /// <summary>
        /// Take a screenshot and save to file
        /// </summary>
        public void SaveScreenshot(string filename = "noname")
        {
            //1// important for scripting use cases!
        }

        #endregion

        #region Window handling
        // --------------------

        private async void DisplayElements_SelectedItemChanged(object sender, EventArgs e)
        {
            // access
            //var x = DisplayElements.SelectedItems?.ToList();
            //return;

            if (DisplayElements == null || sender != DisplayElements)
                return;

            // try identify the business object
            if (DisplayElements.SelectedItem != null)
            {
                Logic?.LocationHistory?.Push(DisplayElements.SelectedItem);
            }

            // may be flush events
            CheckIfToFlushEvents();

            // redraw view
            await RedrawElementViewAsync();
        }

        private async void DisplayElements_MouseDoubleClick(object sender, EventArgs e)
        {
            // we're assuming, that SelectedItem point to the right business object
            var si = DisplayElements.SelectedItem;
            if (si == null)
                return;

            // act depending on selectedItem
            if (si is VisualElementEnvironmentItem siei
                && (siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev
                    || siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext))
            {
                // want to refetch elements
                // check all pre-requisites
                if (!(siei.thePackage is AdminShellPackageDynamicFetchEnv dynPack
                     && dynPack.GetContext() is PackageContainerHttpRepoSubsetFetchContext fetchContext
                     && fetchContext.Record != null))
                {
                    Log.Singleton.Error("Fetch next within dynamic environment: " +
                        "Not enough data to provide dynamic fetch operations.");
                    return;
                }

                // at the start or end?
                var goPrev = siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev;
                var goNext = siei.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext;
                var goNextFake = false;
                if (goNext && fetchContext.Cursor?.HasContent() != true)
                {
                    Log.Singleton.Info(StoredPrint.Color.Blue, "No cursor for fetch operation available " +
                            "(at the end of the selected subset of elements or no server support).");
                    goNext = false;
                    goNextFake = true;
                }

                // refer to (static) function
                var res = await DispEditHelperEntities.ExecuteUiForFetchOfElements(
                    PackageCentral, DisplayContext, new AasxMenuActionTicket(), this /* MainWindow */, fetchContext,
                    preserveEditMode: true,
                    doEditNewRecord: false,
                    doCheckTainted: true,
                    doFetchGoPrev: goPrev,
                    doFetchGoNext: goNext,
                    doFakeGoNext: goNextFake,
                    doFetchExec: true);

                // success will trigger redraw independently, therefore always do nothing
            }
            else
            if (si is VisualElementSubmodelElement)
            {
                // redraw view
                await RedrawElementViewAsync();

                // "simulate" click on "ShowContents"
                //1// ShowContent_Click(this.ShowContent, null);
            }
        }

        private void Window_Closing(object sender, EventArgs e)
        {
            //1// if (this.IsInFlyout())
            //1// {
            //1//     e.Cancel = true;
            //1//     return;
            //1// }
            //1// 
            //1// var positiveQuestion = ScriptModeShutdown ||
            //1//     (Options.Curr.UseFlyovers &&
            //1//     AnyUiMessageBoxResult.Yes == MessageBoxFlyoutShow(
            //1//         "Do you want to proceed closing the application? Make sure, that you have saved your data before.",
            //1//         "Exit application?",
            //1//         AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Question));
            //1// 
            //1// if (!positiveQuestion)
            //1// {
            //1//     e.Cancel = true;
            //1//     return;
            //1// }

            // ok
            Log.Singleton.Info("Application closing ..");

            // package
            Log.Singleton.Info("Closing main and aux package ..");
            try
            {
                PackageCentral?.MainItem?.Close();
                PackageCentral?.AuxItem?.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            // LRU
            try
            {
                // save LRU
                var lru = PackageCentral?.Repositories?.FindLRU();
                if (lru != null)
                {
                    Log.Singleton.Info("Saving LRU ..");
                    var lruFn = PackageContainerListLastRecentlyUsed.BuildDefaultFilename();
                    lru.SaveAsLocalFile(lruFn);
                }

                // also close log silently
                //1// if (_messageReportWindow != null)
                //1//     _messageReportWindow.Close();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            // Log
            if (_logWriter != null)
                try
                {
                    _logWriter.Close();
                    _logWriter = null;
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }

            // done
            //1// e.Cancel = false;
        }

        private void Window_SizeChanged(object sender, EventArgs e)
        {
            //1// if (this.ActualWidth > 1)
            //1// {
            //1//     if (MainSpaceGrid != null && MainSpaceGrid.ColumnDefinitions.Count >= 3)
            //1//     {
            //1//         var w0 = 0.2;
            //1//         if (Options.Curr.PercentageLeftColumn >= 0 && Options.Curr.PercentageLeftColumn <= 100.0)
            //1//             w0 = Options.Curr.PercentageLeftColumn / 100.0;
            //1// 
            //1//         var w4 = 0.4;
            //1//         if (Options.Curr.PercentageRightColumn >= 0 && Options.Curr.PercentageRightColumn <= 100.0)
            //1//             w4 = Options.Curr.PercentageRightColumn / 100.0;
            //1// 
            //1//         MainSpaceGrid.ColumnDefinitions[0].Width = new GridLength(this.ActualWidth * w0);
            //1//         MainSpaceGrid.ColumnDefinitions[4].Width = new GridLength(this.ActualWidth * w4);
            //1//     }
            //1// }
        }

        private async void ShowContent_Click(object sender, EventArgs e)
        {
            await Task.Yield();

            if (sender as string == "1234"  && _showContentElement != null && PackageCentral.MainAvailable
                && _showContentElement is VisualElementSubmodelElement veSme)
            {
                //
                // Text edit of BLOB?
                //

                if (veSme?.theWrapper is Aas.IBlob blb
                    && _viewModel.MainMenu?.IsChecked("EditMenu") == true
                    && AdminShellUtil.CheckForTextContentType(blb.ContentType))
                {
                    Log.Singleton.Info("Trying edit multiline content from {0} ..", blb.IdShort);
                    try
                    {
                        var uc = new AnyUiDialogueDataTextEditor(
                                        caption: $"Edit Blob '{"" + blb.IdShort}'",
                                        mimeType: blb.ContentType,
                                        text: Encoding.Default.GetString(blb.Value ?? new byte[0]));
                        if (await DisplayContext.StartFlyoverModalAsync(uc))
                        {
                            blb.Value = Encoding.Default.GetBytes(uc.Text);
                            DispEditEntityPanel.AddDiaryStructuralChange(blb);
                            await RedrawElementViewAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"When editing content from {blb.IdShort}, an error occurred");
                        return;
                    }
                    Log.Singleton.Info("Content from {0} edited.", blb.IdShort);
                    return;
                }

                if (veSme?.theWrapper is Aas.IFile file
                    && _viewModel.MainMenu?.IsChecked("EditMenu") == true
                    && AdminShellUtil.CheckForTextContentType(file.ContentType))
                {
                    Log.Singleton.Info("Trying edit multiline content from {0} ..", file.IdShort);

                    DispEditHelperModules.DisplayOrEditEntityFileResource_EditTextFileAsync(
                        DisplayContext, PackageCentral.Main,
                        file.ContentType,
                        file.Value);

                    Log.Singleton.Info("Content from {0} edited.", file.IdShort);
                    return;
                }

                //
                // Display?
                //

                Tuple<object?, string?> contentFound = null;
                if (veSme?.theWrapper is Aas.IFile scFile)
                    contentFound = new Tuple<object?, string?>(scFile.Value, scFile.ContentType);
                if (veSme?.theWrapper is Aas.IBlob scBlob && _viewModel.MainMenu?.IsChecked("EditMenu") == false)
                    contentFound = new Tuple<object?, string?>(scBlob.Value, scBlob.ContentType);

                if (contentFound != null)
                {
                    Log.Singleton.Info("Trying display content {0} ..", contentFound.Item1);
                    try
                    {
                        if (contentFound.Item1 is string contentUri)
                        {
                            // if local in the package, then make a tempfile
                            if (!contentUri.ToLower().Trim().StartsWith("http://")
                                && !contentUri.ToLower().Trim().StartsWith("https://"))
                            {
                                // make it a file?
                                // more info for Registry/ Repo available?
                                var x = veSme!.FindAasSubmodelIdShortPath();
                                contentUri = await PackageCentral.Main.MakePackageFileAvailableAsTempFileAsync(contentUri,
                                    aasId: x?.Item1?.Id,
                                    smId: x?.Item2?.Id,
                                    idShortPath: x?.Item3,
                                    secureAccess: null /* //1// _securityAccessHandler */);
                            }

                            BrowserDisplayLocalFile(contentUri, contentFound.Item2);
                        }
                        else
                        if (contentFound.Item1 is byte[] ba)
                        {
                            try
                            {
                                // generate tempfile name
                                string tempext = AdminShellUtil.GuessExtension(
                                    contentType: contentFound.Item2,
                                    contents: ba);
                                string temppath = System.IO.Path.GetTempFileName().Replace(".tmp", tempext);

                                // write it
                                System.IO.File.WriteAllBytes(temppath, ba);

                                // display
                                BrowserDisplayLocalFile(temppath, contentFound.Item2);
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "when preparing BLOB contents to be displayed as file.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(
                            ex, $"When displaying content {contentFound.Item1}, an error occurred");
                        return;
                    }
                    Log.Singleton.Info("Content {0} displayed.", contentFound.Item1);
                }
            }
        }

        private void UpdateContent_Click(object sender, EventArgs e)
        {
            // have a online connection?
            if (this.theOnlineConnection != null && this.theOnlineConnection.IsValid() &&
                this.theOnlineConnection.IsConnected())
            {
                // current entity is a property
                if (this.currentEntityForUpdate != null && this.currentEntityForUpdate is VisualElementSubmodelElement)
                {
                    var viselem = this.currentEntityForUpdate as VisualElementSubmodelElement;
                    if (viselem != null && viselem.theEnv != null &&
                        viselem.theContainer != null && viselem.theContainer is Aas.Submodel &&
                        viselem.theWrapper != null && viselem.theWrapper != null &&
                        viselem.theWrapper is Aas.Property)
                    {
                        // access a valid property
                        var p = viselem.theWrapper as Aas.Property;
                        if (p != null)
                        {
                            // use online connection
                            var x = this.theOnlineConnection.UpdatePropertyValue(
                                viselem.theEnv, viselem.theContainer as Aas.Submodel, p);
                            p.Value = x;

                            // refresh
                            var y = DisplayElements.SelectedItem;
                            y?.RefreshFromMainData();
                            DisplayElements.Refresh();
                        }
                    }
                }
            }
        }

        private void ContentUndo_Click(object sender, EventArgs e)
        {
            DispEditEntityPanel.CallUndo();
        }

        /// <summary>
        /// Check for menu switch and flush events, if required.
        /// </summary>
        public void CheckIfToFlushEvents()
        {
            if (_viewModel.MainMenu?.IsChecked("CompressEvents") == true)
            {
                var evs = _eventCompressor?.Flush();
                if (evs != null)
                    foreach (var ev in evs)
                        PackageCentral?.PushEvent(ev);
            }
        }

        private async void ContentTakeOver_Click(object sender, EventArgs e)
        {
            // some more "OK, good to go" 
            CheckIfToFlushEvents();

            // refresh display of the tree
            var x = DisplayElements.SelectedItem;
            if (x == null)
            {
                // TODO (MIHO, 2021-06-08): find the root cause instead of doing a quick-fix
                // some copy/paste operation seems to leave the DisplayElements-sate in the wrong state
                x = DisplayElements.TrySynchronizeToInternalTreeState();
            }
            x?.RefreshFromMainData();
            DisplayElements.Refresh();

            // new (MIHO, 2024-05-23): testwise redisplay also element panel
            // (MIHO, 2024-07-02): redisplay (full re-render) only, if not currently
            // a plugin is display, which might have internal state!
            if (!(DisplayElements?.SelectedItem is VisualElementPluginExtension))
                await RedrawElementViewAsync();

            // re-enable
            TakeOverContentEnable(false);
        }

        // left out: raw key handler!

        #endregion

        #region Modal Flyovers
        //====================

        //1// private List<StoredPrint> flyoutLogMessages = null;

        public void FlyoutLoggingStart()
        {
            //1// if (flyoutLogMessages == null)
            //1// {
            //1//     flyoutLogMessages = new List<StoredPrint>();
            //1//     return;
            //1// }
            //1// 
            //1// lock (flyoutLogMessages)
            //1// {
            //1//     flyoutLogMessages = new List<StoredPrint>();
            //1// }
        }

        public void FlyoutLoggingStop()
        {
            //1// if (flyoutLogMessages == null)
            //1//     return;
            //1// 
            //1// lock (flyoutLogMessages)
            //1// {
            //1//     flyoutLogMessages = null;
            //1// }
        }

        public void FlyoutLoggingPush(StoredPrint msg)
        {
            //1// if (flyoutLogMessages == null)
            //1//     return;
            //1// 
            //1// lock (flyoutLogMessages)
            //1// {
            //1//     flyoutLogMessages.Add(msg);
            //1// }
        }

        public StoredPrint? FlyoutLoggingPop()
        {
            //1// if (flyoutLogMessages != null)
            //1//     lock (flyoutLogMessages)
            //1//     {
            //1//         if (flyoutLogMessages.Count > 0)
            //1//         {
            //1//             var msg = flyoutLogMessages[0];
            //1//             flyoutLogMessages.RemoveAt(0);
            //1//             return msg;
            //1//         }
            //1//     }
            return null;
        }

        public bool IsInFlyout()
        {
            //1// if (this.GridFlyover.Children.Count > 0)
            //1//     return true;
            return false;
        }

        public async Task StartFlyoverAsync(VisualElement uc)
        {
            // uc needs to implement IFlyoverControl (to be sure that it is valid)
            var ucfoc = uc as IFlyoutControl;
            if (ucfoc == null || !(uc is Page page))
                return;

            // start the page
            await Navigation.PushAsync(page);

            //1// // uc needs to implement IFlyoverControl
            //1// var ucfoc = uc as IFlyoutControl;
            //1// if (ucfoc == null)
            //1//     return;
            //1// 
            //1// // blur the normal grid
            //1// this.InnerGrid.IsEnabled = false;
            //1// var blur = new BlurEffect();
            //1// blur.Radius = 5;
            //1// this.InnerGrid.Opacity = 0.5;
            //1// this.InnerGrid.Effect = blur;
            //1// 
            //1// // populate the flyover grid
            //1// this.GridFlyover.Visibility = Visibility.Visible;
            //1// this.GridFlyover.Children.Clear();
            //1// this.GridFlyover.Children.Add(uc);
            //1// 
            //1// // register the event
            //1// ucfoc.ControlClosed += Ucfoc_ControlClosed;
            //1// currentFlyoutControl = ucfoc;
            //1// 
            //1// // start (focus)
            //1// ucfoc.ControlStart();
        }

        private void Ucfoc_ControlClosed()
        {
            //1// CloseFlyover();
        }

        public async Task CloseFlyoverAsync(bool threadSafe = false)
        {
            await Navigation.PopAsync();

            //1// Action lambda = () =>
            //1// {
            //1//     // blur the normal grid
            //1//     this.InnerGrid.Opacity = 1.0;
            //1//     this.InnerGrid.Effect = null;
            //1//     this.InnerGrid.IsEnabled = true;
            //1// 
            //1//     // un-populate the flyover grid
            //1//     this.GridFlyover.Children.Clear();
            //1//     this.GridFlyover.Visibility = Visibility.Hidden;
            //1// 
            //1//     // unregister
            //1//     currentFlyoutControl = null;
            //1// };
            //1// 
            //1// if (!threadSafe)
            //1//     lambda.Invoke();
            //1// else
            //1//     Dispatcher.BeginInvoke(lambda);
        }

        //
        // SYNCRONOUS
        //

        public void StartFlyoverModal(VisualElement uc, Action? closingAction = null)
        {
            //1// // uc needs to implement IFlyoverControl
            //1// var ucfoc = uc as IFlyoutControl;
            //1// if (ucfoc == null)
            //1//     return;
            //1// 
            //1// // blur the normal grid
            //1// this.InnerGrid.IsEnabled = false;
            //1// var blur = new BlurEffect();
            //1// blur.Radius = 5;
            //1// this.InnerGrid.Opacity = 0.5;
            //1// this.InnerGrid.Effect = blur;
            //1// 
            //1// // populate the flyover grid
            //1// this.GridFlyover.Visibility = Visibility.Visible;
            //1// this.GridFlyover.Children.Clear();
            //1// this.GridFlyover.Children.Add(uc);
            //1// 
            //1// // register the frame
            //1// var frame = new DispatcherFrame();
            //1// ucfoc.ControlClosed += () =>
            //1// {
            //1//     frame.Continue = false; // stops the frame
            //1// };
            //1// 
            //1// // main application needs to know
            //1// currentFlyoutControl = ucfoc;
            //1// 
            //1// // agent behaviour
            //1// var preventClosingAction = false;
            //1// 
            //1// if (uc is IFlyoutAgent ucag)
            //1// {
            //1//     // register for minimize
            //1//     ucag.ControlMinimize += () =>
            //1//     {
            //1//         // only execute if preconditions are well
            //1//         if (ucag.GetAgent() != null && ucag.GetAgent().GenerateFlyoutMini != null)
            //1//         {
            //1//             // do not execute directly
            //1//             preventClosingAction = true;
            //1// 
            //1//             // make a mini
            //1//             var mini = ucag.GetAgent().GenerateFlyoutMini.Invoke();
            //1// 
            //1//             // be careful
            //1//             if (mini is UserControl miniUc)
            //1//             {
            //1//                 // push the agent
            //1//                 UserControlAgentsView.Add(miniUc);
            //1// 
            //1//                 // wrap provided closing action in own closing action
            //1//                 if (ucag.GetAgent() != null)
            //1//                     ucag.GetAgent().ClosingAction = () =>
            //1//                     {
            //1//                         // 1st delete agent
            //1//                         UserControlAgentsView.Remove(miniUc);
            //1// 
            //1//                         // finally, call user provided closing action
            //1//                         closingAction?.Invoke();
            //1//                     };
            //1// 
            //1//                 // show the panel
            //1//                 PanelConcurrentSetVisibleIfRequired(true, targetAgents: true);
            //1// 
            //1//                 // remove the flyover
            //1//                 frame.Continue = false; // stops the frame
            //1//             }
            //1//         }
            //1//     };
            //1// }
            //1// 
            //1// // start (focus)
            //1// ucfoc.ControlStart();
            //1// 
            //1// // This will "block" execution of the current dispatcher frame
            //1// // and run our frame until the dialog is closed.
            //1// Dispatcher.PushFrame(frame);
            //1// 
            //1// // call the closing action (before releasing!)
            //1// // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            //1// if (closingAction != null && !preventClosingAction)
            //1//     closingAction();
            //1// 
            //1// // blur the normal grid
            //1// this.InnerGrid.Opacity = 1.0;
            //1// this.InnerGrid.Effect = null;
            //1// this.InnerGrid.IsEnabled = true;
            //1// 
            //1// // un-populate the flyover grid
            //1// this.GridFlyover.Children.Clear();
            //1// this.GridFlyover.Visibility = Visibility.Hidden;
            //1// 
            //1// // unregister
            //1// currentFlyoutControl = null;
        }

#if TO_DELETE
        public AnyUiMessageBoxResult MessageBoxFlyoutShow(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            //1// if (!Options.Curr.UseFlyovers)
            //1// {
            //1//     return AnyUiMessageBoxResult.Cancel;
            //1// }
            //1// 
            //1// var uc = new MessageBoxFlyout(message, caption, buttons, image);
            //1// StartFlyoverModal(uc);
            //1// return uc.Result;

            return AnyUiMessageBoxResult.None;
        }
#endif

        //
        // ASYNC (are async versions of the WPF modals required)
        //


        public async Task StartFlyoverModalAsync(VisualElement uc, Action? closingAction = null)
        {
            // uc needs to implement IFlyoverControl
            var ucfoc = uc as IFlyoutControl;
            if (ucfoc == null || !(uc is Page page))
                return;

            // before starting, care for closing the dialog
            ucfoc.ControlClosed += async () =>
            {
                await Navigation.PopModalAsync();
            };

            // start the page
            // await Navigation.PushModalAsync(page);
            bool result = await ucfoc.MauiShowPageAsync(Navigation);

            //1// // uc needs to implement IFlyoverControl
            //1// var ucfoc = uc as IFlyoutControl;
            //1// if (ucfoc == null)
            //1//     return;
            //1// 
            //1// // blur the normal grid
            //1// this.InnerGrid.IsEnabled = false;
            //1// var blur = new BlurEffect();
            //1// blur.Radius = 5;
            //1// this.InnerGrid.Opacity = 0.5;
            //1// this.InnerGrid.Effect = blur;
            //1// 
            //1// // populate the flyover grid
            //1// this.GridFlyover.Visibility = Visibility.Visible;
            //1// this.GridFlyover.Children.Clear();
            //1// this.GridFlyover.Children.Add(uc);
            //1// 
            //1// // register the frame
            //1// var frame = new DispatcherFrame();
            //1// ucfoc.ControlClosed += () =>
            //1// {
            //1//     frame.Continue = false; // stops the frame
            //1// };
            //1// 
            //1// // main application needs to know
            //1// currentFlyoutControl = ucfoc;
            //1// 
            //1// // agent behaviour
            //1// var preventClosingAction = false;
            //1// ucfoc.ControlStart();
            //1// 
            //1// // This will "block" execution of the current dispatcher frame
            //1// // and run our frame until the dialog is closed.
            //1// Dispatcher.PushFrame(frame);
            //1// 
            //1// // call the closing action (before releasing!)
            //1// // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            //1// if (closingAction != null && !preventClosingAction)
            //1//     closingAction();
            //1// 
            //1// // blur the normal grid
            //1// this.InnerGrid.Opacity = 1.0;
            //1// this.InnerGrid.Effect = null;
            //1// this.InnerGrid.IsEnabled = true;
            //1// 
            //1// // un-populate the flyover grid
            //1// this.GridFlyover.Children.Clear();
            //1// this.GridFlyover.Visibility = Visibility.Hidden;
            //1// 
            //1// // unregister
            //1// currentFlyoutControl = null;
            //1// 
            //1// // relieve task
            //1// await Task.Yield();
            await Task.Yield();
            return;
        }

        public async Task<AnyUiMessageBoxResult> MessageBoxFlyoutShowAsync(
            string message, string caption, AnyUiMessageBoxButton buttons, AnyUiMessageBoxImage image)
        {
            var uc = new MessageBoxFlyoutPage(message, caption, buttons, image);
            await StartFlyoverModalAsync(uc);
            return uc.Result;
        }

        public Window GetWin32Window()
        {
            return Application.Current!.Windows[0];
        }

        public AnyUiContextBase GetDisplayContext()
        {
            return DisplayContext;
        }

#endregion

        #region Drag&Drop
        //===============

        // see Cross-platform MAUI way (recommended)
        // Use DropGestureRecognizer

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            //1// if (!e.Data.GetDataPresent("myFormat") || sender == e.Source)
            //1// {
            //1//     e.Effects = DragDropEffects.None;
            //1// }
        }

        protected async void Window_Drop(object sender, IReadOnlyList<string> files)
        {
            await Task.Yield();

            // access
            if (files == null || files.Count < 1)
                return;

            // simply try load
            string fn = files[0];
            try
            {
                await UiLoadPackageWithNew(
                    PackageCentral.MainItem, null, loadLocalFilename: fn, onlyAuxiliary: false,
                    nextEditMode: Options.Curr.EditMode);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"while receiving file drop to window");
            }                       
        }

        private bool isDragging = false;
        private Point dragStartPoint = new Point(0, 0);

        private void DragSource_PreviewMouseMove(object sender, EventArgs e)
        {
            //1// // MIHO 2020-09-14: removed this from the check below
            //1// //// && (Math.Abs(dragStartPoint.X) < 0.001 && Math.Abs(dragStartPoint.Y) < 0.001)
            //1// if (e.LeftButton == MouseButtonState.Pressed && !isDragging
            //1//     && PackageCentral.MainAvailable
            //1//     && this._showContentElement is Aas.IFile scFile)
            //1// {
            //1//     Point position = e.GetPosition(null);
            //1//     if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            //1//         Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            //1//     {
            //1//         // check if it an address in the package only
            //1//         if (!scFile.Value.Trim().StartsWith("/"))
            //1//             return;
            //1// 
            //1//         // lock
            //1//         isDragging = true;
            //1// 
            //1//         // fail safe
            //1//         try
            //1//         {
            //1//             // hastily prepare temp file ..
            //1//             var tempfile = PackageCentral.Main.MakePackageFileAvailableAsTempFile(
            //1//                 scFile.Value, keepFilename: true);
            //1// 
            //1//             // Package the data.
            //1//             DataObject data = new DataObject();
            //1//             data.SetFileDropList(new System.Collections.Specialized.StringCollection() { tempfile });
            //1// 
            //1//             // Inititate the drag-and-drop operation.
            //1//             DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
            //1//         }
            //1//         catch (Exception ex)
            //1//         {
            //1//             Log.Singleton.Error(
            //1//                 ex, $"When dragging content {scFile.Value}, an error occurred");
            //1//             return;
            //1//         }
            //1// 
            //1//         // unlock
            //1//         isDragging = false;
            //1//     }
            //1// }
        }

        private void DragSource_PreviewMouseLeftButtonDown(object sender, EventArgs e)
        {
            //1// dragStartPoint = e.GetPosition(null);
        }

        #endregion

        #region Find & Replace
        // -------------------

        /// <summary>
        /// Tools are find & replace
        /// </summary>
        private void ButtonTools_Click(object sender, EventArgs e)
        {
            //1// if (sender == ButtonToolsClose)
            //1// {
            //1//     ToolsGrid.Visibility = Visibility.Collapsed;
            //1//     if (DispEditEntityPanel != null)
            //1//         DispEditEntityPanel.ClearHighlight();
            //1// }
        }

        #endregion

        #region Keyboard shortcut HTML
        // ---------------------------

        // TODO (MIHO, 2026-01-01): Refactor between AASPE apps

        public string CreateTempFileForKeyboardShortcuts()
        {
            try
            {
                //
                // HTML statr
                //

                // create a temp HTML file
                var tmpfn = System.IO.Path.GetTempFileName();

                // rename to html file
                var htmlfn = tmpfn.Replace(".tmp", ".html");
                System.IO.File.Move(tmpfn, htmlfn);

                // create html content as string
                var htmlHeader = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<!doctype html>
                        <html lang=en>
                        <head>
                        <style>
                        body {
                          background-color: #FFFFE0;
                          font-size: small;
                          font-family: Arial, Helvetica, sans-serif;
                        }
                        table {
                          font-family: arial, sans-serif;
                          border-collapse: collapse;
                          width: 100%;
                        }
                        td, th {
                          border: 1px solid #dddddd;
                          text-align: left;
                          padding: 8px;
                        }
                        </style>
                        <meta charset=utf-8>
                        <title>blah</title>
                        </head>
                        <body>");

                var htmlFooter = AdminShellUtil.CleanHereStringWithNewlines(
                    @"</body>
                        </html>");

                var html = new StringBuilder();

                html.Append(htmlHeader);

                var color = false;

                //
                // Keyboard shortcuts
                //

                html.AppendLine("<h3>Keyboard shortcuts</h3>");

                html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                    @"<table style=""width:100%"">
                        <tr>
                        <th>Modifiers & Keys</th>
                        <th>Function</th>
                        <th>Description</th>
                        </tr>"));

                var rowfmt = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<tr style=""background-color: {0}"">
                        <td>{1}</th>
                        <td>{2}</th>
                        <td>{3}</th>
                        </tr>");

                foreach (var sc in DispEditEntityPanel.EnumerateShortcuts())
                {
                    // Function
                    var fnct = "";
                    if (sc.Element is AnyUiButton btn)
                        fnct = "" + btn.Content;

                    // fill
                    html.Append(String.Format(rowfmt,
                        (color) ? "#ffffe0" : "#fffff0",
                        "" + sc.GestureToString(fmt: 0),
                        "" + fnct,
                        "" + sc.Info));

                    // color change
                    color = !color;
                }

                html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                    @"</table>"));

                //
                // Menu command
                //

                // ReSharper disable AccessToModifiedClosure

                Action<AasxMenu> lambdaMenu = (menu) =>
                {

                    html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                        @"<table style=""width:100%"">
                        <tr>
                        <th>Keyboard</th>
                        <th>Menu header</th>
                        <th>ToolCmd / <br><i>Argument</i></th>
                        <th>Description</th>
                        </tr>"));

                    var rowfmtTC = AdminShellUtil.CleanHereStringWithNewlines(
                        @"<tr style=""background-color: {0}"">
                        <td>{1}</td>
                        <td>{2}</td>
                        <td>{3}</td>
                        <td>{4}</td>
                        </tr>");

                    var rowfmtTCAD = AdminShellUtil.CleanHereStringWithNewlines(
                        @"<tr style=""background-color: {0}"">
                        <td colspan=""2"" 
                         style=""border-top:none;border-bottom:none;border-left:none;background-color:#FFFFE0"">
                        </td>
                        <td><i>{1}</i></td>
                        <td><i>{2}</i></td>
                        </tr>");

                    foreach (var mib in menu.FindAll((x) => x is AasxMenuItem))
                    {
                        // access
                        if (!(mib is AasxMenuItem mi) || mi.Name?.HasContent() != true)
                            continue;

                        // filter header
                        var header = mi.Header.Replace("_", "");

                        // fill
                        html.Append(String.Format(rowfmtTC,
                            (color) ? "#ffffe0" : "#fffff0",
                            "" + mi.InputGesture,
                            "" + header,
                            "" + mi.Name,
                            "" + mi.HelpText));

                        // arguments
                        if (mi.ArgDefs != null)
                            foreach (var ad in mi.ArgDefs)
                            {
                                if (ad.Hidden)
                                    continue;
                                html.Append(String.Format(rowfmtTCAD,
                                    (color) ? "#ffffe0" : "#fffff0",
                                    "" + ad.Name,
                                    "" + ad.Help));
                            }

                        // color change
                        color = !color;
                    }

                    html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                        @"</table>"));
                };

                // ReSharper enable AccessToModifiedClosure

                html.AppendLine("<h3>Menu and script commands</h3>");
                lambdaMenu(_viewModel.MainMenu);

                html.AppendLine("<h3>Displayed entity and script commands</h3>");
                lambdaMenu(DynamicMenu.Menu);

                //
                // Script command
                //

                var script = new AasxScript();
                script.PrepareHelp();

                html.AppendLine("<h3>Script built-in commands</h3>");

                html.Append(AdminShellUtil.CleanHereStringWithNewlines(
                    @"<table style=""width:100%"">
                        <tr>
                        <th>Keyword</th>
                        <th>Argument</th>
                        <th>Description</th>
                        </tr>"));

                var rowfmtSC = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<tr style=""background-color: {0}"">
                        <td>{1}</td>
                        <td colspan=""2"">{2}</td>
                        </tr>");

                var rowfmtSCAD = AdminShellUtil.CleanHereStringWithNewlines(
                    @"<tr style=""background-color: {0}"">
                        <td  
                         style=""border-top:none;border-bottom:none;border-left:none;background-color:#FFFFE0"">
                        </td>
                        <td><i>{1}</i></td>
                        <td><i>{2}</i></td>
                        </tr>");

                foreach (var hr in script.ListOfHelp)
                {
                    // fill
                    html.Append(String.Format(rowfmtSC,
                        (color) ? "#ffffe0" : "#fffff0",
                        "" + hr.Keyword,
                        "" + hr.Description));

                    // arguments
                    if (hr.ArgDefs != null)
                        foreach (var ad in hr.ArgDefs)
                        {
                            if (ad.Hidden)
                                continue;
                            html.Append(String.Format(rowfmtSCAD,
                                (color) ? "#ffffe0" : "#fffff0",
                                "" + HttpUtility.HtmlEncode(ad.Name),
                                "" + ad.Help));
                        }

                    // color change
                    color = !color;
                }

                //
                // HTMLend
                //

                html.Append(htmlFooter);

                // write
                System.IO.File.WriteAllText(htmlfn, html.ToString());
                return htmlfn;
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, "Creating HTML file for keyboard shortcuts");
            }
            return null;
        }

        #endregion

        #region Furhter interface methods
        // ------------------------------

        // REFACTOR: for later refactoring
        public void RedrawRepositories()
        {
            // nothing here required
            ;
        }

        // REFACTOR: for later refactoring
        public void RedrawAllElementsAndFocus(object? nextFocus = null, bool isExpanded = true)
        {
            // WPF: inject
            DispEditEntityPanel.AddWishForOutsideAction(
                new AnyUiLambdaActionRedrawAllElements(nextFocus: nextFocus, isExpanded: isExpanded));
        }

        /// <summary>
        /// Gets the interface to the components which manages the AAS tree elements (middle section)
        /// </summary>
        public IDisplayElements GetDisplayElements() => DisplayElements;

        /// <summary>
        /// Returns the <c>AasxMenu</c> of the main menu of the application.
        /// Purpose: script automation
        /// </summary>
        public AasxMenu GetMainMenu()
        {
            return _viewModel.MainMenu;
        }

        /// <summary>
        /// Returns the <c>AasxMenu</c> of the dynmaically built menu of the application.
        /// Purpose: script automation
        /// </summary>
        public AasxMenu GetDynamicMenu()
        {
            return DynamicMenu.Menu;
        }

        /// <summary>
        /// Returns the quite concise script interface of the application
        /// to allow script automation.
        /// </summary>
        public IAasxScriptRemoteInterface GetRemoteInterface()
        {
            return Logic;
        }

        /// <summary>
        /// Allows an other class to inject a lambda action.
        /// This will be perceived by the main window, most likely.
        /// </summary>
        public void AddWishForToplevelAction(AnyUiLambdaActionBase action)
        {
            DispEditEntityPanel.AddWishForOutsideAction(action);
        }


        private async void Button_Click(object sender, EventArgs e)
        {
            await Task.Yield();

            //1// if (sender == ButtonKeyboard)
            //1// {
            //1//     var htmlfn = CreateTempFileForKeyboardShortcuts();
            //1//     BrowserDisplayLocalFile(htmlfn, System.Net.Mime.MediaTypeNames.Text.Html,
            //1//                             preferInternal: true);
            //1// }
            //1// 
            //1// if (sender == ButtonHomeLocation)
            //1// {
            //1//     await CommandBinding_GeneralDispatch("navigatehome", null, new AasxMenuActionTicket());
            //1// }
            //1// 
            //1// if (sender == ButtonHistoryBack)
            //1// {
            //1//     Logic?.LocationHistory?.Pop();
            //1// }
        }

        #endregion

#region General commands
// ---------------------

#if _not_anymore_needed
        /// <summary>
        /// Redraw tree elements (middle), AAS entitty (right side)
        /// </summary>
        public async Task CommandExecution_RedrawAllAsync()
        {
            // redraw everything
            await RedrawAllAasxElementsAsync();
            await RedrawElementViewAsync();
        }

        private async Task CommandBinding_GeneralDispatch(
            string cmd,
            AasxMenuItemBase menuItem,
            AasxMenuActionTicket ticket)
        {
            await Task.Yield();
        }

        public Task<int> ExecuteMainMenuCommand(string menuItemName, bool scriptMode, params object[] args)
        {
            throw new NotImplementedException();
        }
#endif
#endregion
    }
}
