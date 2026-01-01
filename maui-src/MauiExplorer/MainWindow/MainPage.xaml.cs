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

namespace MauiTestTree
{
    public partial class MainPage : ContentPage, IFlyoutProvider, IPushApplicationEvent, IMainWindow
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
        protected AnyUiDisplayContextMaui? DisplayContext = null;

        /// <summary>
        /// This symbol is only a link to the abstract main-windows class.
        /// </summary>
        public PackageCentral? PackageCentral
        {
            get => Logic?.PackageCentral;

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
            if (PackageCentral?.MainAvailable == true)
                t += " - " + PackageCentral.MainItem.ToString();
            if (PackageCentral?.AuxAvailable == true)
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
                PackageCentral, PackageCentral.Selector.Main, MainMenu?.IsChecked("EditMenu") == true,
                lazyLoadingFirst: true);

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
                MainMenu?.SetChecked("EditMenu", nextEditMode.HasValue ? nextEditMode.Value : false);
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
                nextEditMode = MainMenu?.IsChecked("EditMenu") == true;

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
                foreach (var sm in PackageCentral?.Main?.AasEnv?.OverSubmodelsOrEmpty())
                    sm?.SetAllParents();

            // displaying
            try
            {
                await RestartUIafterNewPackage(onlyAuxiliary, nextEditMode);

                if (autoFocusFirstRelevant && PackageCentral?.Main?.AasEnv is Aas.IEnvironment menv)
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
                var lru = PackageCentral?.Repositories?.FindLRU();
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
            AdminShellPackageEnvBase package, ListOfVisualElementBasic entities,
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
        public async Task RedrawElementViewAsync(DispEditHighlight.HighlightFieldInfo hightlightField = null)
        {
            await Task.Yield();

            if (DisplayElements == null)
                return;

            // the AAS will cause some more visual effects
            var tvlaas = DisplayElements.SelectedItem as VisualElementAdminShell;
            if (PackageCentral?.MainAvailable == true 
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
                PackageCentral?.Main,
                DisplayElements.SelectedItems,
                 _viewModel.MainMenu?.IsChecked("EditMenu") == true,
                 _viewModel.MainMenu?.IsChecked("HintsMenu") == true,
                 _viewModel.MainMenu?.IsChecked("ShowIriMenu") == true,
                 _viewModel.MainMenu?.IsChecked("CheckSmtElements") == true,
                 hightlightField: hightlightField);

        }

        #endregion
        #region Callbacks
        //===============

        private async void Window_Loaded(object sender, EventArgs e)
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
            DisplayContext = new AnyUiDisplayContextMaui(this, PackageCentral);
            Logic.DisplayContext = DisplayContext;
            Logic.MainWindow = this;

            // re-load known endpoints
            //1//_securityAccessHandler = new WinGdiSecurityAccessHandler(DisplayContext,
            //1//    knownEndpoints: Options.Curr.KnownEndpoints);

            // making up "empty" picture
            _viewModel.AasId = "<id unknown!>";
            _viewModel.AssetId = "<id unknown!>";

            // logical main menu
            var logicalMainMenu = ExplorerMenuFactory.CreateMainMenu();
            logicalMainMenu.DefaultActionAsync = CommandBinding_GeneralDispatch;

            // top level children have other color
            logicalMainMenu.DefaultForeground = AnyUiColors.Black;
            foreach (var mi in logicalMainMenu)
                if (mi is AasxMenuItem mii)
                    mii.Foreground = AnyUiColors.White;

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

            // Timer for below
            System.Windows.Threading.DispatcherTimer MainTimer = new System.Windows.Threading.DispatcherTimer();
            MainTimer.Tick += async (s, a) =>
            {
                
            };
            MainTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            MainTimer.Start();
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
                    PackageCentral?.Repositories.Add(lru);
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
                        PackageCentral?.Repositories.AddAtTop(fr2);
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
            PackageCentral?.MainItem.New();
            await RedrawAllAasxElementsAsync();

            // pump all pending log messages (from plugins) into the
            // log / status line, before setting the last information
            MainTimer_HandleLogMessages();

            // Try to load?            
            if (Options.Curr.AasxToLoad != null)
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
                        runtimeOptions: PackageCentral?.CentralRuntimeOptions);

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

            if (Options.Curr.AuxToLoad != null)
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
            if (Options.Curr.ShowEvents)
                PanelConcurrentSetVisibleIfRequired(true, targetEvents: true);

            // trigger re-index
            TriggerPendingReIndexElements();

            // script file to launch?
            if (Options.Curr.ScriptFn.HasContent())
            {
                try
                {
                    Log.Singleton.Info("Opening and executing '{0}' for script commands.", Options.Curr.ScriptFn);
                    Logic?.StartScriptFile(Options.Curr.ScriptFn, MainMenu?.Menu, Logic);
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
                    Logic?.StartScriptCommand(Options.Curr.ScriptCmd, MainMenu?.Menu, Logic);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"when executing script file {Options.Curr.ScriptCmd}");
                }
            }

            AasxIntegrationBaseWpf.CountryFlagWpf.LoadImage();
        }

        //
        // Timer + Events to the outside
        //

        protected bool _timerRunning = false;
        protected bool _timerAlreadyBusy = false;

        protected void StartTimer()
        {
            _timerRunning = true;
            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(50), () =>
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

        private void ToolFindReplace_ResultSelected(AasxSearchUtil.SearchResultItem resultItem)
        {
            // have a result?
            if (resultItem == null || resultItem.businessObject == null)
                return;

            // for valid display, app needs to be in edit mode
            if (MainMenu.IsChecked("EditMenu") != true)
            {
                this.MessageBoxFlyoutShow(
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

        private string _lastMessageBlue = "";
        private string _lastMessageError = "";

        private void MainTimer_HandleLogMessages()
        {
            // pop log messages from the plug-ins into the Stored Prints in Log
            Plugins.PumpPluginLogsIntoLog(this.FlyoutLoggingPush);

            // check for Stored Prints in Log
            StoredPrint sp;
            while ((sp = Log.Singleton.PopLastShortTermPrint()) != null)
            {
                // pop
                Message.Content = "" + sp.msg;

                // display
                switch (sp.color)
                {
                    default:
                        throw ExhaustiveMatch.Failed(sp.color);
                    case StoredPrint.Color.Black:
                        {
                            Message.Background = Brushes.White;
                            Message.Foreground = Brushes.Black;
                            Message.FontWeight = FontWeights.Normal;
                            break;
                        }
                    case StoredPrint.Color.Blue:
                        {
                            _lastMessageBlue = "" + sp.msg;
                            Message.Background = Brushes.LightBlue;
                            Message.Foreground = Brushes.Black;
                            Message.FontWeight = FontWeights.Normal;
                            break;
                        }
                    case StoredPrint.Color.Yellow:
                        {
                            _lastMessageBlue = "" + sp.msg;
                            Message.Background = Brushes.Yellow;
                            Message.Foreground = Brushes.Black;
                            Message.FontWeight = FontWeights.Bold;
                            break;
                        }
                    case StoredPrint.Color.Red:
                        {
                            _lastMessageError = "" + sp.msg;
                            Message.Background = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
                            Message.Foreground = Brushes.White;
                            Message.FontWeight = FontWeights.Bold;
                            break;
                        }
                }

                // message window
                _messageReportWindow?.AddStoredPrint(sp);

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
                LabelNumberErrors.Content = "Errors: " + ne;
                LabelNumberErrors.Background = new SolidColorBrush(Color.FromRgb(0xd4, 0x20, 0x44)); // #D42044
            }
            else
            if (nb > 0)
            {
                LabelNumberErrors.Content = "Major: " + nb;
                LabelNumberErrors.Background = Brushes.LightBlue;
            }
            else
            {
                LabelNumberErrors.Content = "No attention";
                LabelNumberErrors.Background = Brushes.White;
            }
        }
    }
}
