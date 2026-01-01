using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AnyUi;

namespace MauiTestTree;

public partial class DiplayVisualAasxElements : ContentView
{
    public DiplayVisualAasxElements()
    {
        InitializeComponent();
    }

    public VisualElementGeneric? SelectedItem
    {
        get
        {
            return null;
        }
    }

    public ListOfVisualElementBasic? SelectedItems
    {
        get
        {
            return null;
        }
    }

    public ListOfVisualElementBasic? GetSelectedItems()
    {
        return SelectedItems;
    }

    public void RebuildAasxElements(
            PackageCentral packages,
            PackageCentral.Selector selector,
            bool editMode = false, string? filterElementName = null,
            bool lazyLoadingFirst = false,
            int expandModePrimary = 1,
            int expandModeAux = 0)
    {
    }

    public void ExpandAllItems()
    {
    }

    public VisualElementGeneric? SearchVisualElementOnMainDataObject(object dataObject,
            bool alsoDereferenceObjects = false,
            ListOfVisualElement.SupplementaryReferenceInformation? sri = null)
    {
        return null;
    }

    public bool TrySelectVisualElement(VisualElementGeneric ve, bool? wishExpanded)
    {
        return false;
    }

    public bool TrySelectMainDataObject(
        object dataObject, bool? wishExpanded,
        bool alsoDereferenceObjects = false)
    {
        return false;
    }

    public bool Contains(VisualElementGeneric ve)
    {
        return false;
    }

    public void Refresh()
    {
    }

    public bool IsAnyTaintedIdentifiable()
    {
        return false;
    }

    /// <summary>
    /// Identifies top visual elements, which are above all content elements
    /// </summary>
    /// <returns></returns>
    public IEnumerable<VisualElementGeneric> FindAllVisualElementTop()
    {
        yield break;
    }

    /// <summary>
    /// Activates the caching of the "expanded" states of the tree, even if the tree is multiple
    /// times rebuilt via <code>RebuildAasxElements</code>.
    /// </summary>
    public void ActivateElementStateCache()
    {
    }

    //
    // Event queuing
    //

    public void PushEvent(AnyUiLambdaActionBase la)
    {
    }

    //
    // Further
    //

    public VisualElementGeneric? TrySynchronizeToInternalTreeState()
    {
        return null;
    }
}