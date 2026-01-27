using System.Diagnostics;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Extensions;
using Aas = AasCore.Aas3_1;

namespace MauiTestTree;

public partial class DiplayVisualAasxElements : ContentView, IDisplayElements
{

    public DiplayVisualAasxElements()
    {
        InitializeComponent();

        // wait till Loaded(), until the BindingContext is ready to provide the correct view model!
        Loaded += (_,_) => {
            // as working with binding, subscribing to the view model
            // Trace.WriteLine($"VM hash: {_viewModel.GetHashCode()}");
            // _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            _viewModel.SelectedItems.CollectionChanged += (s,e) =>
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(10), () =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    // HandleSelection(_viewModel.SelectedItems.ToList());

                    _stabilizedSelectedItems = new();
                    if (_viewModel.SelectedItems != null)
                        foreach (var si in _viewModel.SelectedItems)
                            if (si is FunctionZero.TreeListItemsSourceZero.TreeNodeContainer<object> tnc
                                && tnc.Data is VisualElementGeneric veg)
                                _stabilizedSelectedItems.Add(veg);

                    FireSelectedItem();
                });
            };
        };
    }

    //
    // External members
    //

    public event EventHandler? SelectedItemChanged = null;

    //
    // Internal members
    //

    private bool _lastEditMode = false;

    protected MainViewModel _viewModel { get => BindingContext as MainViewModel ?? new MainViewModel();  }

    protected CancellationTokenSource? _cts;
    protected ListOfVisualElementBasic _stabilizedSelectedItems = new();

    /// <summary>
    /// Peeks through the view model and the (artificial) root elements directly to the members representing
    /// visual element. Named same to the WPF version.
    /// </summary>
    protected ListOfVisualElement displayedTreeViewLines { get => _viewModel.RootNode?.Members ?? new ListOfVisualElement(); }

    /// <summary>
    /// Holds the states of the "IsExpanded" over multiple generations of the tree structure.
    /// </summary>
    protected TreeViewLineCache? treeViewLineCache = null;

    /// <summary>
    /// (Only or first) selected item in the tree.
    /// Refers directly to the view model.
    /// </summary>
    public VisualElementGeneric? SelectedItem { get => _viewModel.SelectedItem as VisualElementGeneric; }

    /// <summary>
    /// All selected items in the tree.
    /// Generates on the fly the required types from the view model.
    /// </summary>
    public ListOfVisualElementBasic? SelectedItems
    {
        get => _stabilizedSelectedItems;
    }

    /// <summary>
    /// All selected items in the tree.
    /// Generates on the fly the required types from the view model.
    /// </summary>
    //public ListOfVisualElementBasic? SelectedItems {
    //    get {
    //        // first look at the list of all selected items
    //        ListOfVisualElementBasic res = new();
    //        if (_viewModel.SelectedItems != null)
    //            foreach (var si in _viewModel.SelectedItems)
    //                if (si is FunctionZero.TreeListItemsSourceZero.TreeNodeContainer<object> tnc
    //                    && tnc.Data is VisualElementGeneric veg)
    //                    res.Add(veg);

    //        // the currently selected items may or may not add to it
    //        // or (if selection mode = single), may be the only item
    //        if (!res.Contains(SelectedItem))
    //            res.Add(SelectedItem);
    //        return res;
    //    }
    //}

    /// <summary>
    /// All selected items in the tree.
    /// Generates on the fly the required types from the view model.
    /// </summary>
    public ListOfVisualElementBasic? GetSelectedItems() => SelectedItems;

    //
    // Selection management
    //

    private bool preventSelectedItemChanged = false;

    public void DisableSelectedItemChanged()
    {
        preventSelectedItemChanged = true;
    }

    public void EnableSelectedItemChanged()
    {
        preventSelectedItemChanged = false;
    }

    private void FireSelectedItem()
    {
        if (!preventSelectedItemChanged && SelectedItemChanged != null)
            SelectedItemChanged(this, new EventArgs());
    }

    private void SuppressSelectionChangeNotification(Action lambda)
    {
        // WPF version needed it
        lambda.Invoke();
    }

    //private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName == nameof(MainViewModel.SelectedItem))
    //        FireSelectedItem();
    //}


    //
    // Expansion management
    //

    private void TreeViewInner_Expanded(VisualElementGeneric ve)
    {
        // access and check
        if (ve == null)
            return;

        // select (but no callback!)
        SelectSingleVisualElement(ve, preventFireItem: true);

        // need lazy loading?
        if (!ve.NeedsLazyLoading)
            return;

        // try execute, may take some time
        // TODO (MIHO, 2ß26-01-08): Check change the mouse cursor here
        try
        {
            displayedTreeViewLines?.ExecuteLazyLoading(ve, forceExpanded: true);
        }
        finally
        {
            // potentially change mouse cursor back
        }
    }

    /// <summary>
    /// Tries to expand all items, which aren't currently yet, e.g. because of lazy loading.
    /// Is found to be a valid pre-requisite in case of lazy loading for 
    /// <c>SearchVisualElementOnMainDataObject</c>.
    /// Potentially a expensive operation.
    /// </summary>
    public void ExpandAllItems()
    {
        if (displayedTreeViewLines == null)
            return;

        // try execute, may take some time
        // TODO (MIHO, 2ß26-01-08): Check change the mouse cursor here
        try
        {
            // search (materialized)
            var candidates = FindAllVisualElement((ve) => ve.NeedsLazyLoading).ToList();

            // susequently approach
            foreach (var ve in candidates)
                displayedTreeViewLines.ExecuteLazyLoading(ve);
        }
        catch (Exception ex)
        {
            Log.Singleton.Error(ex, "when expanding all visual AASX elements");
        }
        finally
        {
            // potentially change mouse cursor back
        }
    }

    //
    // Element View Drawing
    //

    public void Clear()
    {
        //1// treeViewInner.ItemsSource = null;
        //1// treeViewInner.UpdateLayout();

        // Note: The MAUI tree component (TreeViewZero) has a flaw:
        //       A "Reset" of the ObservableCollection crashed, therefore
        //       items shall be deleted item by item or a new() shall be done.
        //       Also, resetting the collection lead to loosing the connection.
        // Already tried:
        //       _viewModel.RootNode.Members = new ListOfVisualElement();
        //       _viewModel.RootNode = null;
        //       _viewModel.RootNode = new RootOfListOfVisualElement();
        //       MyTree.BindingContext = _viewModel.RootNode;
        // Last resort (kinda OK, because only small number of top-most nodes)
        while (_viewModel.RootNode.Members.Count > 0)
            _viewModel.RootNode.Members.RemoveAt(_viewModel.RootNode.Members.Count - 1);
    }

    protected VisualElementGeneric? _lastExpandedChangedElem = null;
    protected DateTime _lastExpandedChangedTime = default(DateTime);

    protected void MonitorAllExpandableNodes()
    {
        if (displayedTreeViewLines != null)
            foreach (var ve in displayedTreeViewLines.FindAllVisualElementTop())
                ve.ForAllExpandableNotes((veg) => {
                    veg.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(VisualElementGeneric.IsExpanded))
                        {
                            var item = (VisualElementGeneric)s!;

                            Dispatcher.Dispatch(() =>
                            {
                                var node = item;

                                if (node == _lastExpandedChangedElem 
                                    && (DateTime.UtcNow - _lastExpandedChangedTime).TotalMilliseconds < 50)
                                {
                                    return;
                                }

                                // ok, register new
                                _lastExpandedChangedElem = node;
                                _lastExpandedChangedTime = DateTime.UtcNow;

                                // 'emulate' evend
                                if (ve.IsExpanded)
                                    TreeViewInner_Expanded(node);

                                // Trace.WriteLine("NOW!");
                                // OnNodeExpandedChanged(node, node.IsExpanded);
                            });

                            // OnNodeExpandedChanged(item, item.IsExpanded);
                        }
                    };
                });
    }

    public void RebuildAasxElements(
            PackageCentral packages,
            PackageCentral.Selector selector,
            bool editMode = false, string? filterElementName = null,
            bool lazyLoadingFirst = false,
            int expandModePrimary = 1,
            int expandModeAux = 0)
    {
        // clear tree
        Clear();
        _lastEditMode = editMode;

        // valid?
        if (packages.MainAvailable)
        {
            // generate lines, add
            displayedTreeViewLines.AddVisualElementsFromShellEnv(
                treeViewLineCache, packages.Main?.AasEnv, packages.Main,
                packages.MainItem?.Filename, editMode, expandMode: expandModePrimary, lazyLoadingFirst: lazyLoadingFirst);

            // more?
            if (packages.AuxAvailable &&
                (selector == PackageCentral.Selector.MainAux
                    || selector == PackageCentral.Selector.MainAuxFileRepo))
            {
                displayedTreeViewLines.AddVisualElementsFromShellEnv(
                    treeViewLineCache, packages.Aux?.AasEnv, packages.Aux,
                    packages.AuxItem?.Filename, editMode, expandMode: expandModeAux, lazyLoadingFirst: lazyLoadingFirst);
            }

            // more?
            if (packages.Repositories != null && selector == PackageCentral.Selector.MainAuxFileRepo)
            {
                var pkg = new AdminShellPackageFileBasedEnv();
                foreach (var fr in packages.Repositories)
                    fr.PopulateFakePackage(pkg);

                displayedTreeViewLines.AddVisualElementsFromShellEnv(
                    treeViewLineCache, pkg?.AasEnv, pkg,
                    null, editMode, expandMode: expandModeAux, lazyLoadingFirst: lazyLoadingFirst);
            }

            // may be filter
            if (filterElementName != null)
                foreach (var dtl in displayedTreeViewLines)
                    // it is not likely, that we have to delete on this level, therefore don't care
                    FilterLeavesOfVisualElements(dtl, filterElementName);

            // any of these lines?
            if (displayedTreeViewLines.Count < 1)
            {
                // emergency
                displayedTreeViewLines.Add(
                    new VisualElementEnvironmentItem(
                        null /* no parent */, treeViewLineCache, packages.Main, packages.Main?.AasEnv,
                        VisualElementEnvironmentItem.ItemType.EmptySet));
            }

        }

        // redraw
        //1// treeViewInner.ItemsSource = displayedTreeViewLines;
        //1// treeViewInner.UpdateLayout();

        // select 1st
        if (displayedTreeViewLines.Count > 0)
        {
            displayedTreeViewLines[0].IsSelected = true;
            displayedTreeViewLines[0].IsExpanded = true;
            _viewModel.SelectedItem = displayedTreeViewLines[0];

            // SelectSingleVisualElement(displayedTreeViewLines[0]);
        }

        // start monitoring expansions
        MonitorAllExpandableNodes();
    }

    protected ListOfVisualElementBasic TranslateMainDataObjectsToVisualElements(IEnumerable<object> mainObjects)
    {
        var ves = new ListOfVisualElementBasic();
        if (mainObjects != null)
            foreach (var mo in mainObjects)
            {
                var ve = SearchVisualElementOnMainDataObject(mo);
                if (ve != null)
                    ves.Add(ve);
            }
        return ves;
    }

    /// <summary>
    /// This function cares, that all PARENT ABOVE the visual elements are expanded!!
    /// </summary>
    public void TryExpandMainDataObjects(IEnumerable<object> mainObjects, bool preventFireItem = false)
    {
        // gather objects
        var ves = TranslateMainDataObjectsToVisualElements(mainObjects);

        // select
        TryExpandVisualElements(ves);

        // fire event
        FireSelectedItem();
    }

    public VisualElementGeneric? GetDefaultVisualElement()
    {
        if (displayedTreeViewLines.Count < 1)
            return null;

        return displayedTreeViewLines[0];
    }

    public VisualElementGeneric? SearchVisualElementOnMainDataObject(object? dataObject,
            bool alsoDereferenceObjects = false,
            ListOfVisualElement.SupplementaryReferenceInformation? sri = null)
    {
        return displayedTreeViewLines.FindFirstVisualElementOnMainDataObject(
            dataObject, alsoDereferenceObjects, sri);
    }

    public void SelectSingleVisualElement(VisualElementGeneric ve, bool preventFireItem = false)
    {
        if (ve == null)
            return;
        ve.IsSelected = true;
        _viewModel.SelectedItem = ve;
        _viewModel.SelectedItems.Clear();
        _viewModel.SelectedItems.Add(ve);
        if (!preventFireItem)
            FireSelectedItem();
    }

    public bool TrySelectVisualElement(VisualElementGeneric ve, bool? wishExpanded)
    {
        // access?
        if (ve == null)
            return false;

        if (wishExpanded == true)
        {
            // go upward the tree in order to expand, as well
            var sii = ve;
            while (sii != null)
            {
                sii.IsExpanded = true;
                sii = sii.Parent;
            }
        }
        if (wishExpanded == false)
            ve.IsExpanded = false;

        // select (but no callback!)
        SelectSingleVisualElement(ve, preventFireItem: true);
        Refresh();

        // OK
        return true;
    }

    public bool TrySelectMainDataObject(
        object dataObject, bool? wishExpanded,
        bool alsoDereferenceObjects = false)
    {
        // access?
        var ve = SearchVisualElementOnMainDataObject(dataObject,
            alsoDereferenceObjects: alsoDereferenceObjects);
        if (ve == null)
            return false;

        // select
        return TrySelectVisualElement(ve, wishExpanded);
    }

    public void TrySelectMainDataObjects(IEnumerable<object> mainObjects, bool preventFireItem = false)
    {
        // gather objects
        var ves = TranslateMainDataObjectsToVisualElements(mainObjects);

        // select
        TrySelectVisualElements(ves, preventFireItem);

        // fire event
        FireSelectedItem();
    }

    /// <summary>
    /// This function cares, that all PARENT ABOVE the visual elements are expanded!!
    /// </summary>
    public bool TryExpandVisualElements(ListOfVisualElementBasic? ves)
    {
        // access?
        if (ves == null)
            return false;

        // suppressed
        SuppressSelectionChangeNotification(() =>
        {

            // step 2 : expand PARENTS
            foreach (var x in ves)
            {
                var sii = x?.Parent;
                while (sii != null)
                {
                    sii.IsExpanded = true;
                    sii = sii.Parent;
                }
            }
        });

        //1// treeViewInner.UpdateLayout();

        // OK
        return true;
    }

    public bool TrySelectVisualElements(ListOfVisualElementBasic ves, bool preventFireItem = false)
    {
        // access?
        if (ves == null)
            return false;

        // suppressed
        SuppressSelectionChangeNotification(() =>
        {

            // step 1 : deselect all
            foreach (var si in SelectedItems.ForEachSafe())
                si.IsSelected = false;
            _viewModel.SelectedItems.Clear();

            // step 2 : expand PARENTS
            foreach (var x in ves)
            {
                var sii = x?.Parent;
                while (sii != null)
                {
                    sii.IsExpanded = true;
                    sii = sii.Parent;
                }
            }

            // step 3 : select
            foreach (var ve in ves)
            {
                if (ve == null)
                    continue;
                ve.IsSelected = true;
                _viewModel.SelectedItems.Add(ve);
            }
        });

        // fire
        if (!preventFireItem)
            FireSelectedItem();

        //1// treeViewInner.UpdateLayout();

        // OK
        return true;
    }

    public bool Contains(VisualElementGeneric ve)
    {
        return displayedTreeViewLines.ContainsDeep(ve);
    }

    public void Dispose()
    {
        MyTree.ClearLogicalChildren();
    }

    public void Refresh()
    {
    }

    public int RefreshAllChildsFromMainData(VisualElementGeneric? root)
    {
        /* TODO (MIHO, 2021-01-04): check to replace all occurences of RefreshFromMainData() by
         * making the tree-items ObservableCollection and INotifyPropertyChanged */

        // access
        if (root == null)
            return 0;

        // self
        var sum = 1;
        root.RefreshFromMainData();

        // children?
        if (root.Members != null)
            foreach (var child in root.Members)
                sum += RefreshAllChildsFromMainData(child);

        // ok
        return sum;
    }

    /// <summary>
    /// Return true, if <code>mem</code> has to be deleted, because not in filter.
    /// </summary>
    /// <param name="mem"></param>
    /// <param name="fullFilterElementName"></param>
    /// <returns></returns>
    public bool FilterLeavesOfVisualElements(VisualElementGeneric mem, string? fullFilterElementName)
    {
        if (fullFilterElementName == null)
            return (false);
        fullFilterElementName = fullFilterElementName.Trim().ToLower();
        if (fullFilterElementName == "")
            return (false);

        // has Members -> is not leaf!
        if (mem.Members != null && mem.Members.Count > 0)
        {
            // go into non-leafs mode -> simply go over list
            var todel = new List<VisualElementGeneric>();
            foreach (var x in mem.Members)
                if (FilterLeavesOfVisualElements(x, fullFilterElementName))
                    todel.Add(x);
            // delete items on list
            foreach (var td in todel)
                mem.Members.Remove(td);
        }
        else
        {
            // consider lazy loading
            if (mem is VisualElementEnvironmentItem memei
                && memei.theItemType == VisualElementEnvironmentItem.ItemType.DummyNode)
                return false;

            // this member is a leaf!!
            var isIn = false;
            var mdo = mem.GetMainDataObject();
            if (mdo is Aas.IReferable mdorf)
            {
                var mdoen = mdorf.GetSelfDescription().AasElementName.Trim().ToLower();
                isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
            }
            else
            if (mdo is Aas.IClass mdoic)
            {
                // this special case was intruduced because of AssetInformation
                var mdoen = mdoic.GetType().Name.ToLower();
                isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
            }
            else
            if (mdo is Aas.Reference)
            {
                // very special case because of importance
                var mdoen = (mdo as Aas.Reference).GetSelfDescription().AasElementName.Trim().ToLower();
                isIn = fullFilterElementName.IndexOf(mdoen, StringComparison.Ordinal) >= 0;
            }
            return !isIn;
        }
        return false;
    }

    public bool IsAnyTaintedIdentifiable()
    {
        return displayedTreeViewLines?.IsAnyTaintedIdentifiable() == true;
    }

    //
    // Element management
    //

#if not_need
    public IEnumerable<VisualElementGeneric> FindAllVisualElement()
    {
        if (displayedTreeViewLines != null)
            foreach (var ve in displayedTreeViewLines.FindAllVisualElement())
                yield return ve;
    }
#endif

    public IEnumerable<VisualElementGeneric> FindAllVisualElement(Predicate<VisualElementGeneric> p)
    {
        if (displayedTreeViewLines != null)
            foreach (var ve in displayedTreeViewLines.FindAllVisualElement(p))
                yield return ve;
    }

    /// <summary>
    /// Identifies top visual elements, which are above all content elements
    /// </summary>
    /// <returns></returns>
    public IEnumerable<VisualElementGeneric> FindAllVisualElementTop()
    {
        if (displayedTreeViewLines != null)
            foreach (var ve in displayedTreeViewLines.FindAllVisualElementTop())
                yield return ve;
    }

    /// <summary>
    /// Identifies visual elements, which are *directly* superordinate to Identifiable.
    /// Note: Could contain duplicates.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<VisualElementGeneric> FindAllVisualElementTopToIdentifiable()
    {
        if (displayedTreeViewLines != null)
            foreach (var ve in displayedTreeViewLines.FindAllVisualElementTopToIdentifiable())
                yield return ve;
    }

    /// <summary>
    /// Activates the caching of the "expanded" states of the tree, even if the tree is multiple
    /// times rebuilt via <code>RebuildAasxElements</code>.
    /// </summary>
    public void ActivateElementStateCache()
    {
        treeViewLineCache = new TreeViewLineCache();
    }

    //
    // Event queuing
    //

    private List<AnyUiLambdaActionBase> _eventQueue = new List<AnyUiLambdaActionBase>();

    public void PushEvent(AnyUiLambdaActionBase la)
    {
        lock (_eventQueue)
        {
            _eventQueue.Add(la);
        }
    }

    public void UpdateFromQueuedEvents()
    {
        if (displayedTreeViewLines == null)
            return;

        lock (_eventQueue)
        {
            foreach (var lab in _eventQueue)
            {
                if (lab is AnyUiLambdaActionPackCntChange lapcc && lapcc.Change != null)
                {
                    // shortcut
                    var e = lapcc.Change;

                    // for speed reasons?
                    if (e.DisableSelectedTreeItemChange)
                        DisableSelectedItemChanged();

                    displayedTreeViewLines.UpdateByEvent(e, treeViewLineCache);

                    if (e.DisableSelectedTreeItemChange)
                        EnableSelectedItemChanged();
                }

                if (lab is AnyUiLambdaActionSelectMainObjects labsmo)
                {
                    this.TrySelectMainDataObjects(labsmo.MainObjects);
                }
            }

            _eventQueue.Clear();
        }
    }

    //
    // Further
    //

    public VisualElementGeneric? TrySynchronizeToInternalTreeState()
    {
        // assume not required, as _viewModel.SelectedItem DIRECTLY managed by tree
        return SelectedItem;

        //1// var x = this.SelectedItem;
        //1// if (x == null && treeViewInner.SelectedItem != null)
        //1// {
        //1//     x = treeViewInner.SelectedItem as VisualElementGeneric;
        //1// 
        //1//     SuppressSelectionChangeNotification(() =>
        //1//     {
        //1//         SetSelectedState(x, true);
        //1//     });
        //1// }
        //1// return x;
    }

    //
    // Interface management
    //

    VisualElementGeneric? IDisplayElements.GetSelectedItem()
    {
        return SelectedItem;
    }

    void IDisplayElements.ClearSelection()
    {
        _viewModel.SelectedItems.Clear();
        _viewModel.SelectedItem = null;
    }

 }