using System.Windows;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using static AasxPackageLogic.DispEditHelperBasics;
using Aas = AasCore.Aas3_1;

namespace MauiTestTree;

public partial class DispEditAasxEntityMaui : ContentView
{
    //
    // Members
    // 

    protected AnyUiDisplayContextMaui.RenderDefaults _renderDefaults;

    //
    // Component
    //

	public DispEditAasxEntityMaui()
	{
		InitializeComponent();

        // built up render defaults
        _renderDefaults = new AnyUiDisplayContextMaui.RenderDefaults()
        {
            FontSizeNormal = 12,
        };

        // Outer life cycle for correct timer
        Loaded += (s1, e1) => { 
            StartTimer(); 
        };
        Unloaded += (s2, e2) =>
        {
            StopTimer();
        };
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
            _ = dispatcherTimer_Tick(null, null);
            _timerAlreadyBusy = false;

            return _timerRunning; // true = keep running, false = stop
        });
    }

    public List<AnyUiLambdaActionBase> WishForOutsideAction = new List<AnyUiLambdaActionBase>();

    public event Func<AnyUiLambdaActionBase, Task> OutsideAction;

    protected void StopTimer()
    {
        _timerRunning = false;
    }

    private async Task dispatcherTimer_Tick(object? sender, EventArgs? e)
    {
        // check for wishes from the modify repo

        if (_helper?.context is AnyUiDisplayContextMaui dcmaui && dcmaui.WishForOutsideAction != null)
        {
            while (dcmaui!.WishForOutsideAction.Count > 0)
            {
                var temp = dcmaui.WishForOutsideAction[0];
                dcmaui.WishForOutsideAction.RemoveAt(0);

                // trivial?
                if (temp is AnyUiLambdaActionNone)
                    continue;

                // what?
                if (temp is AnyUiLambdaActionRedrawEntity)
                {
                    // redraw ourselves?
                    if (_packages != null && _theEntities != null && _helper != null)
                        await DisplayOrEditVisualAasxElement(
                            _packages, dcmaui, _theEntities, _helper.editMode, _helper.hintMode,
                            flyoutProvider: dcmaui?.FlyoutProvider,
                            appEventProvider: _helper?.appEventsProvider);

                    return;
                }

                // all other elements refer to superior functionality
                if (OutsideAction != null)
                    await OutsideAction.Invoke(temp);
            }
        }
    }

    public void AddWishForOutsideAction(AnyUiLambdaActionBase action)
    {
        //1// if (action != null && WishForOutsideAction != null)
        //1//     WishForOutsideAction.Add(action);
    }

    //
    // Members
    //

    private PackageCentral? _packages = null;
    private ListOfVisualElementBasic? _theEntities = null;
    private DispEditHelperMultiElement? _helper = new DispEditHelperMultiElement();
    private AnyUiUIElement? _lastRenderedRootElement = null;

    //
    // Undo
    //

    private void ContentUndo_Click(object sender, EventArgs e)
    {
        CallUndo();
    }

    public void CallUndo()
    {
        try
        {
            var changes = true == _helper?.context?.CallUndoChanges(_lastRenderedRootElement);
            if (changes)
                _helper?.context.EmitOutsideAction(new AnyUiLambdaActionContentsTakeOver());

        }
        catch (Exception ex)
        {
            Log.Singleton.Error(ex, "undoing last changes");
        }
    }

    //
    // Management of loaded plugin
    //

    protected VisualElementGeneric? LoadedPluginNode = null;
    protected Plugins.PluginInstance? LoadedPluginInstance = null;
    protected object? LoadedPluginSessionId = null;
    protected int LoadedPluginApproach = 0;

    /// <summary>
    /// Sends a dispose signal to the loaded plugin in order to properly
    /// release its resources before session might be disposed or plugin might
    /// be changed.
    /// </summary>
    public void DisposeLoadedPlugin()
    {
        // access
        if (LoadedPluginInstance == null || LoadedPluginSessionId == null || LoadedPluginApproach < 1)
        {
            LoadedPluginNode = null;
            LoadedPluginInstance = null;
            LoadedPluginSessionId = null;
            return;
        }

        // try release
        try
        {
            if (LoadedPluginApproach == 1)
                LoadedPluginInstance.InvokeAction("clear-panel-visual-extension",
                    LoadedPluginSessionId);

            if (LoadedPluginApproach == 2)
                LoadedPluginInstance.InvokeAction("dispose-anyui-visual-extension",
                    LoadedPluginSessionId);

            LoadedPluginNode = null;
            LoadedPluginApproach = 0;
            LoadedPluginInstance = null;
            LoadedPluginSessionId = null;
        }
        catch (Exception ex)
        {
            LogInternally.That.CompletelyIgnoredError(ex);
        }
    }

    // 
    // Main function: Maintain an place to attach editing controls
    //

    public IEnumerable<AnyUiDisplayContextMaui.KeyShortcutRecord> EnumerateShortcuts()
    {
        // access
        if (!(_helper?.context is AnyUiDisplayContextMaui dcmaui))
            yield break;

        if (dcmaui.KeyShortcuts != null)
            foreach (var sc in dcmaui.KeyShortcuts)
                yield return sc;
    }

    public StackBase ClearDisplayDefaultStack()
    {
        MasterPanel.Content = null;

        var sp = new VerticalStackLayout();
        sp.Add(new Label());
        MasterPanel.Content = sp;
        _lastRenderedRootElement = null;
        return sp;
    }

    public ContentView GetMasterPanel()
    {
        return MasterPanel;
    }    

    public void SetDisplayExternalControl(View fe)
    {
        MasterPanel.Content = null;
        if (fe != null)
        {
            MasterPanel.Content = fe;
            MasterPanel.Content.InvalidateMeasure();
        }
        _lastRenderedRootElement = null;
    }

    public void ClearHighlight()
    {
        if (_helper != null)
            _helper.ClearHighlights();
    }

    public void ClearPasteBuffer()
    {
        if (_helper?.theCopyPaste != null)
            _helper.theCopyPaste.Clear();
    }

    public void AddDiaryStructuralChange(Aas.IReferable rf)
    {
        // access
        if (rf == null || _helper == null)
            return;

        _helper.AddDiaryEntry(rf, new DiaryEntryStructChange(), new DiaryReference(rf));
    }

    public class DisplayRenderHints
    {
        public bool scrollingPanel = true;
        public bool showDataPanel = true;
        public bool useInnerGrid = false;
    }

    public static AnyUiDisplayContextMaui.IconSourceResolveResult? ResolveImageSourceFont(
        AnyUiDisplayContextMaui dc, 
        AnyUiImageSourceFont isf)
    {
        var res = new AnyUiDisplayContextMaui.IconSourceResolveResult();
        res.FontAlias = dc.FindIconFontByShort(isf.FontId)?.FontAlias ?? "";

        var col = Colors.Transparent;
        if (Application.Current?.RequestedTheme == AppTheme.Light)
        {
            if (isf.Color == AnyUiIconColor.Normal)
                col = XamlHelpers.GetDynamicRessource("Gray900", Color.FromRgb(0x40, 0x40, 0x40));
            if (isf.Color == AnyUiIconColor.Intense)
                col = XamlHelpers.GetDynamicRessource("Primary", Color.FromRgb(0x70, 0x70, 0x70));
            if (isf.Color == AnyUiIconColor.Delete)
                col = XamlHelpers.GetDynamicRessource("ErrorDark", Color.FromRgb(0xd6, 0x2b, 0x00));
        }
        else
        {
            if (isf.Color == AnyUiIconColor.Normal)
                col = XamlHelpers.GetDynamicRessource("Gray100", Color.FromRgb(0xe0, 0xe0, 0xe0));
            if (isf.Color == AnyUiIconColor.Intense)
                col = XamlHelpers.GetDynamicRessource("PrimaryLight", Color.FromRgb(0x70, 0x70, 0x70));
            if (isf.Color == AnyUiIconColor.Delete)
                col = XamlHelpers.GetDynamicRessource("ErrorLight", Color.FromRgb(0xd6, 0x2b, 0x00));
        }
        res.IconColor = AnyUiDisplayContextMaui.GetAnyUiColor(col);

        return res;
    }

    public async Task<DisplayRenderHints> DisplayOrEditVisualAasxElement(
        PackageCentral packages,
        AnyUiDisplayContextMaui displayContext,
        ListOfVisualElementBasic? entities,
        bool editMode, bool hintMode = false, bool showIriMode = false, bool checkSmt = false,
        VisualElementEnvironmentItem.ConceptDescSortOrder? cdSortOrder = null,
        IFlyoutProvider? flyoutProvider = null,
        IMainWindow? mainWindow = null,
        IPushApplicationEvent? appEventProvider = null,
        DispEditHighlight.HighlightFieldInfo? hightlightField = null,
        AasxMenu? superMenu = null)
    {
        //
        // Start
        //

        await Task.Yield();

        // hint mode disable, when not edit
        hintMode = hintMode && editMode;

        // remember objects for UI thread / redrawing
        _packages = packages;
        _theEntities = entities;
        if (_helper != null)
        {
            _helper.packages = packages;
            _helper.highlightField = hightlightField;
            _helper.appEventsProvider = appEventProvider;
        }

        // primary access
        var renderHints = new DisplayRenderHints();
        if (MasterPanel == null || entities == null || entities.Count < 1)
        {
            renderHints.showDataPanel = false;
            return renderHints;
        }

        var stack = new AnyUiStackPanel();

        // create display context for MAUI
        _helper!.levelColors = DispLevelColors.GetLevelColorsFromOptions(Options.Curr);

        // layout for MAUI
        _helper.LayoutHints.PlacementAdd = UILayoutHints.PosOfControl.Bottom;
        _helper.LayoutHints.ExplicitMultiLineEdit = false;

        _helper.LayoutHints.ButtonPrefMediumClear = AnyUiButtonPreference.Image;
        _helper.LayoutHints.ButtonPrefLowClear = AnyUiButtonPreference.Both;

        // Button Standard
        _helper.LayoutHints.StyleButtonStandard.Style = new()
        {
            Background = new AnyUiBrush(0xfff8f8f8),
            BorderColor = AnyUiBrushes.Transparent /* AnyUiDisplayContextMaui.GetAnyUiBrush(
                XamlHelpers.GetDynamicRessource("ThinCntlBorder", Colors.Blue)) */,
            BorderWidth = 0.0,
            BorderRadius = 8,
            Foreground = AnyUiBrushes.Black,
            MinHeight = _renderDefaults.ControlSizeBordered,
            MinWidth = _renderDefaults.ControlSizeBordered,
            HorizontalAlignment = AnyUiHorizontalAlignment.Center
        };

        // Button Action
        _helper.LayoutHints.StyleButtonAction.Style = new()
        {
            Background = AnyUiBrushes.White,
            BorderColor = AnyUiDisplayContextMaui.GetAnyUiBrush(XamlHelpers.GetDynamicRessource("Primary", Colors.Blue)),
            BorderWidth = 1.0,
            BorderRadius = 8,
            Foreground = AnyUiBrushes.Black,
            MinHeight = _renderDefaults.ControlSizeBordered,
            MinWidth = _renderDefaults.ControlSizeBordered,
            HorizontalAlignment = AnyUiHorizontalAlignment.Center
        };

        // Button Hero
        _helper.LayoutHints.StyleButtonHero.Style = new()
        {
            Background = AnyUiDisplayContextMaui.GetAnyUiBrush(XamlHelpers.GetDynamicRessource("Primary", Colors.Blue)),
            BorderColor = AnyUiBrushes.Transparent,
            BorderWidth = 0.0,
            BorderRadius = 8,
            Foreground = AnyUiBrushes.White,
            MinHeight = _renderDefaults.ControlSizeBordered,
            MinWidth = _renderDefaults.ControlSizeBordered,
            HorizontalAlignment = AnyUiHorizontalAlignment.Center
        };

        // Text Boxes
        _helper.LayoutHints.StyleEntry = new()
        {
            // enable outer Border
            BorderRadius = 8,
            BorderPadding = new AnyUiThickness(0,-4,0,0),
            BorderColor = AnyUiBrushes.LightGray
        };
        _helper.LayoutHints.StyleEntryPlateLabel = new()
        {
            // enable outer Border
            BorderRadius = 8,
            BorderPadding = new AnyUiThickness(0,-4,0,0),
            BorderColor = AnyUiBrushes.LightGray,
            PlateLabel = new()
            {
                Margin = new AnyUiThickness(10, -5, 0, 0),
                Padding = new AnyUiThickness(3,0,3,0),
                Background = AnyUiBrushes.White,
                Foreground = AnyUiBrushes.DarkGray
            }
        };


        // for icon resolution
        displayContext.LambdaResolveImageSourceFont = ResolveImageSourceFont;

        IconPool.Delete
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Delete_forever);
        IconPool.Add
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Add);
        IconPool.ClearAll
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Clear_all);
        IconPool.AddExisting
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Fact_check);
        IconPool.AddPreset
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Ballot);
        IconPool.AddBlank
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Add_box);
        IconPool.Jump
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Jump_to_element);

        IconPool.CopyToClipboard
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Content_copy);

        IconPool.MoreVert
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.More_vert);

        IconPool.MoveUp
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Arrow_upward_alt);
        IconPool.MoveDown
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Arrow_downward_alt);
        IconPool.MoveTop
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Arrow_warm_up);
        IconPool.MoveBottom
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Arrow_cool_down);

        IconPool.Cut
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Content_cut);
        IconPool.Copy
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Content_copy);
        IconPool.Paste
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Content_paste);
        IconPool.PasteAbove
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Text_select_move_up);
        IconPool.PasteBelow
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Text_select_move_down);
        IconPool.PasteInto
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Subdirectory_arrow_right);

        IconPool.ContextMenuDropDown
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Arrow_drop_down_circle);

        IconPool.FixText
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Rule);

        IconPool.Generate
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Handyman);
        IconPool.Migrate
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Moving);
        IconPool.Rename
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Edit);

        IconPool.Refresh
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Refresh);

        IconPool.IecOrg
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Power);
        IconPool.EclassOrg
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Key_visualizer);
        IconPool.AutoDetect
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Radar);
        IconPool.CreateNew
                .Modify("mat-out", UraniumUI.Icons.MaterialSymbols.MaterialOutlined.Local_activity);

        // modify repository
        ModifyRepo? repo = null;
        if (editMode)
        {
            // some functionality still uses repo != null to detect editMode!!
            repo = new ModifyRepo();
        }
        _helper.editMode = editMode;
        _helper.hintMode = hintMode;
        _helper.repo = repo;
        _helper.showIriMode = showIriMode;
        _helper.context = displayContext;

        // inform plug that their potential panel might not shown anymore
        Plugins.AllPluginsInvoke("clear-panel-visual-extension");

        var inhibitRenderStackToPanel = false;

        if (entities.ExactlyOne)
        {
            //
            // Dispatch: ONE item
            //
            var entity = entities.First();

            // maintain parent. If in doubt, set null
            ListOfVisualElement.SetParentsBasedOnChildHierarchy(entity);

            //
            // Dispatch
            //               

            // try to delegate to common routine
            var common = _helper.DisplayOrEditCommonEntity(
                packages, stack, superMenu, editMode, hintMode, checkSmt, cdSortOrder, entity,
                mainWindow: mainWindow);

            if (common)
            {
                // can reset plugin
                DisposeLoadedPlugin();
            }
            else
            {
                // some special cases
                if (entity is VisualElementPluginExtension vepe)
                {
                    // for the plugin, prepare secure access
                    ISecurityAccessHandler? secureAccess = null;
                    if (vepe.thePackage is AdminShellPackageDynamicFetchEnv aspdfe)
                    {
                        secureAccess = aspdfe.RuntimeOptions?.SecurityAccessHandler;
                    }

                    // may dispose old (other plugin)
                    if (LoadedPluginInstance == null
                        || LoadedPluginNode != entity
                        || LoadedPluginInstance != vepe.thePlugin)
                    {
                        // invalidate, fill new
                        DisposeLoadedPlugin();
                    }

                    // Use AnyUI as plug-in rendering approach
                    if (true)
                    {
                        //
                        // Render panel via ANY UI !!
                        //

                        try
                        {
                            var opContext = new PluginOperationContextBase()
                            {
                                DisplayMode = (editMode)
                                            ? PluginOperationDisplayMode.MayAddEdit
                                            : PluginOperationDisplayMode.JustDisplay
                            };

                            vepe.thePlugin?.InvokeAction(
                                "fill-anyui-visual-extension", vepe.thePackage, vepe.theReferable,
                                stack, _helper.context, AnyUiDisplayContextMaui.SessionSingletonMaui,
                                opContext, secureAccess);

                            // remember
                            LoadedPluginNode = entity;
                            LoadedPluginApproach = 2;
                            LoadedPluginInstance = vepe.thePlugin;
                            LoadedPluginSessionId = AnyUiDisplayContextMaui.SessionSingletonMaui;
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex,
                                $"render AnyUI based visual extension for plugin {vepe.thePlugin.name}");
                        }

                        // show no panel nor scroll
                        renderHints.scrollingPanel = false;
                        renderHints.showDataPanel = false;
                        renderHints.useInnerGrid = true;
                    }

                }
                else
                    _helper.AddGroup(stack, "Entity is unknown!", _helper.levelColors.MainSection);
            }
        }
        else
        if (entities.Count > 1)
        {
            //
            // Dispatch: MULTIPLE items
            //
            _helper.DisplayOrEditAasEntityMultipleElements(packages, entities, editMode, stack, cdSortOrder,
                superMenu: superMenu);
        }

        // now render master stack
        // render Any UI to MAUI
        if (!inhibitRenderStackToPanel)
        {
            // rendering
            MasterPanel.Content = null;
            VisualElement? spwpf = null;
            if (renderHints.useInnerGrid
                && stack?.Children != null
                && stack.Children.Count == 1
                && stack.Children[0] is AnyUiGrid grid)
            {
                // accessing the already rendered version of display context!
                spwpf = displayContext.GetOrCreateMauiElement(grid, renderDefaults: _renderDefaults);
            }
            else
            {
                spwpf = displayContext.GetOrCreateMauiElement(stack, renderDefaults: _renderDefaults);
            }
            _helper.ShowLastHighlights();

            MasterPanel.Content = spwpf as View;
            MasterPanel.Content?.InvalidateMeasure();
        }

        // keep the stack
        _lastRenderedRootElement = stack;

        // return render hints
        return renderHints;
    }

    public Tuple<AnyUiDisplayContextMaui, AnyUiUIElement?>? GetLastRenderedRoot()
    {
        if (!(_helper?.context is AnyUiDisplayContextMaui dcmaui))
            return null;

        return new Tuple<AnyUiDisplayContextMaui, AnyUiUIElement?>(
            dcmaui, _lastRenderedRootElement);
    }

    public void RedisplayRenderedRoot(
            AnyUiUIElement root,
            AnyUiRenderMode mode,
            bool useInnerGrid = false,
            Dictionary<AnyUiUIElement, bool>? updateElemsOnly = null)
    {
        // safe
        _lastRenderedRootElement = root;
        if (!(_helper?.context is AnyUiDisplayContextMaui dcmaui))
            return;

        // no plugin
        //// DisposeLoadedPlugin();

        // redisplay
        MasterPanel.Content = null;
        VisualElement? spmaui = null;

        var allowReUse = mode == AnyUiRenderMode.StatusToUi;

        if (useInnerGrid
            && root is AnyUiStackPanel stack
            && stack?.Children != null
            && stack.Children.Count == 1
            && stack.Children[0] is AnyUiGrid grid)
        {
            spmaui = dcmaui.GetOrCreateMauiElement(grid, allowReUse: allowReUse, mode: mode,
                updateElemsOnly: updateElemsOnly, renderDefaults: _renderDefaults);
        }
        else
        {
            spmaui = dcmaui.GetOrCreateMauiElement(root, allowReUse: allowReUse, mode: mode,
                updateElemsOnly: updateElemsOnly, renderDefaults: _renderDefaults);
        }

        _helper.ShowLastHighlights();
        MasterPanel.Content = spmaui as View;
    }

}