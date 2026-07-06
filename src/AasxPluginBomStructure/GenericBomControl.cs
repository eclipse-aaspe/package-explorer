/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// #define TESTMODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using AasxIntegrationBase;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using System.Windows;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;


namespace AasxPluginBomStructure
{

    /// <summary>
    /// This set of static functions lay-out a graph according to the package information.
    /// Right now, no domain-specific lay-out.
    /// </summary>
    public class GenericBomControl
    {
        private AdminShellPackageEnvBase _package;
        private Aas.Submodel _submodel;
        private bool _createOnPackage = false;

        private Microsoft.Msagl.Drawing.Graph theGraph = null;
        private Microsoft.Msagl.WpfGraphControl.GraphViewer theViewer = null;
        private Aas.IReferable theReferable = null;
        private DockPanel _insideDockPanel = null;

        private PluginEventStack eventStack = null;

        private BomStructureOptionsRecordList _bomRecords = new BomStructureOptionsRecordList();

        private GenericBomCreatorOptions _creatorOptions = new GenericBomCreatorOptions();

        private Dictionary<Aas.IReferable, GenericBomCreatorOptions> preferredPreset =
            new Dictionary<Aas.IReferable, GenericBomCreatorOptions>();

        private BomStructureOptions _bomOptions = new BomStructureOptions();

        private GenericBomCreator _bomCreator = null;

        private Microsoft.Msagl.Core.Geometry.Point _rightClickCoordinates = 
            new Microsoft.Msagl.Core.Geometry.Point();

        private Microsoft.Msagl.Drawing.IViewerObject _objectUnderCursor = null;

        private TabControl _tabControlBottom = null;
        private TabItem _tabItemEdit = null;

        private bool _needsFinalize = false;
        private Button _buttonFinalize = null;

        public void SetEventStack(PluginEventStack es)
        {
            this.eventStack = es;
        }

        protected WrapPanel CreateTopPanel()
        {
            // create TOP controls
            var wpTop = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(0xf0, 0xf0, 0xf0)),
            };

            // style

            wpTop.Children.Add(new Label() { 
                Content = "Layout style: ",                
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var cbli = new ComboBox()
            {
                Margin = new Thickness(0, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            foreach (var psn in this.PresetSettingNames)
                cbli.Items.Add(psn);
            cbli.SelectedIndex = _creatorOptions.LayoutIndex;
            cbli.SelectionChanged += (s3, e3) =>
            {
                _creatorOptions.LayoutIndex = cbli.SelectedIndex;
                RememberSettings();
                RedrawGraph();
            };
            wpTop.Children.Add(cbli);

            // spacing

            wpTop.Children.Add(new Label() { 
                Content = "Spacing: ",
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var sli = new Slider()
            {
                Orientation = Orientation.Horizontal,
                Width = 100,
                Minimum = 1,
                Maximum = 100,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                Value = _creatorOptions.LayoutSpacing,
                Margin = new Thickness(10, 2, 10, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            sli.ValueChanged += (s, e) =>
            {
                _creatorOptions.LayoutSpacing = e.NewValue;
                RememberSettings();
                RedrawGraph();
            };
            wpTop.Children.Add(sli);

            // Compact labels

            var cbcomp = new CheckBox()
            {
                Content = "Compact labels",
                Margin = new Thickness(10, 2, 10, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                IsChecked = _creatorOptions.CompactLabels,
            };
            RoutedEventHandler cbcomb_changed = (s2, e2) =>
            {
                _creatorOptions.CompactLabels = cbcomp.IsChecked == true;
                RememberSettings();
                RedrawGraph();
            };
            cbcomp.Checked += cbcomb_changed;
            cbcomp.Unchecked += cbcomb_changed;
            wpTop.Children.Add(cbcomp);

            // show asset ids

            var cbaid = new CheckBox()
            {
                Content = "Show Asset ids",
                Margin = new Thickness(10, 2, 10, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsChecked = _creatorOptions.CompactLabels,
            };
            RoutedEventHandler cbaid_changed = (s2, e2) =>
            {
                _creatorOptions.ShowAssetIds = cbaid.IsChecked == true;
                RememberSettings();
                RedrawGraph();
            };
            cbaid.Checked += cbaid_changed;
            cbaid.Unchecked += cbaid_changed;
            wpTop.Children.Add(cbaid);

            // finalize button
            _buttonFinalize = new Button()
            {
                Content = "Finalize design",
                ToolTip = "Will reload all contents including redisplay of AAS tree of elements",
                IsEnabled = _needsFinalize,
                Padding = new Thickness(2, -2, 2, -1),
                Margin = new Thickness(2, 1, 2, 1),
                MinHeight = 24
            };
            _buttonFinalize.Click += (s3, e3) =>
            {
                // acknowledge
                SetNeedsFinalize(false);

                // send event to main application
                var evt = new AasxPluginResultEventRedrawAllElements()
                {
                };
                this.eventStack.PushEvent(evt);
            };
            wpTop.Children.Add(_buttonFinalize);

#if __old__

            // "select" button

            var btnSelect = new Button()
            {
                Content = "Selection \U0001f846 tree",
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(4, 0, 4, 0)
            };
            btnSelect.Click += (s3, e3) =>
            {
                // check for marked entities
                var markedRf = GetSelectedViewerReferables().ToList();
                if (markedRf.Count < 1)
                    return;

                // send event to main application
                var evt = new AasxPluginResultEventVisualSelectEntities()
                {
                    Referables = markedRf
                };
                this.eventStack.PushEvent(evt);
            };
            wpTop.Children.Add(btnSelect);
#endif

            // return

            return wpTop;
        }

        protected void SetNeedsFinalize(bool state)
        {
            _needsFinalize = state;
            if (_buttonFinalize != null)
                _buttonFinalize.IsEnabled = state;
        }

        /// <summary>
        /// This is the "normal" view of the BOM plugin
        /// </summary>
        public object FillWithWpfControls(
            BomStructureOptions bomOptions,
            object opackage, object osm, object masterDockPanel)
        {
            // access
            _package = opackage as AdminShellPackageEnvBase;
            _submodel = osm as Aas.Submodel;
            _createOnPackage = false;
            _bomOptions = bomOptions;
            var master = masterDockPanel as DockPanel;
            if (_bomOptions == null || _package == null || _submodel == null || master == null)
                return null;

            // set of records helping layouting
            _bomRecords = new BomStructureOptionsRecordList(
                _bomOptions.LookupAllIndexKey<BomStructureOptionsRecord>(
                    _submodel.SemanticId?.GetAsExactlyOneKey()));

            // clear some other members (GenericBomControl is not allways created new)
            _creatorOptions = new GenericBomCreatorOptions();

            // apply some global options?
            foreach (var br in _bomRecords)
            {
                if (br.Layout >= 1 && br.Layout <= PresetSettingNames.Length)
                    _creatorOptions.LayoutIndex = br.Layout - 1;
                if (br.Compact.HasValue)
                    _creatorOptions.CompactLabels = br.Compact.Value;
            }

            // already user defined?
            if (preferredPreset != null && preferredPreset.ContainsKey(_submodel))
                _creatorOptions = preferredPreset[_submodel].Copy();

            // the Submodel elements need to have parents
            _submodel.SetAllParents();

            // create TOP controls
            var spTop = CreateTopPanel();
            DockPanel.SetDock(spTop, Dock.Top);
            master.Children.Add(spTop);

            // create BOTTOM controls
            var legend = GenericBomCreator.GenerateWrapLegend();

            _tabControlBottom = new TabControl() { MinHeight = 100 };
            _tabControlBottom.Items.Add(new TabItem() { 
                Header = "", Name = "tabItemLegend", 
                Visibility = Visibility.Collapsed,
                Content = legend });

            _tabItemEdit = new TabItem() { 
                Header = "", Name = "tabItemEdit", 
                Visibility = Visibility.Collapsed,
                Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None }) };
            _tabControlBottom.Items.Add(_tabItemEdit);

            DockPanel.SetDock(_tabControlBottom, Dock.Bottom);
            master.Children.Add(_tabControlBottom);

            // set default for very small edge label size
            Microsoft.Msagl.Drawing.Label.DefaultFontSize = 6;

            // make a Dock panel
            var dp = new DockPanel();
            dp.ClipToBounds = true;
            dp.MinWidth = 10;
            dp.MinHeight = 10;

            // very important: add first the panel, then add graph
            master.Children.Add(dp);

            // graph
            var graph = CreateGraph(_package, _submodel, _creatorOptions);

            // very important: first bind it, then add graph
            var viewer = new Microsoft.Msagl.WpfGraphControl.GraphViewer();
            viewer.BindToPanel(dp);
            viewer.MouseDown += Viewer_MouseDown;
            viewer.MouseMove += Viewer_MouseMove;
            viewer.MouseUp += Viewer_MouseUp;
            viewer.ObjectUnderMouseCursorChanged += Viewer_ObjectUnderMouseCursorChanged;
            viewer.ViewChangeEvent += Viewer_ViewChangeEvent;
            viewer.Graph = graph;

            // test
            dp.ContextMenu = new ContextMenu();
            dp.ContextMenu.Items.Add(new MenuItem() { Header = "Jump to selected ..", Tag = "JUMP" });
            dp.ContextMenu.Items.Add(new Separator());
            dp.ContextMenu.Items.Add(new MenuItem() { Header = "Edit Node / Edge ..", Tag = "EDIT" });
            dp.ContextMenu.Items.Add(new MenuItem() { Header = "Create Node (to selected) ..", Tag = "CREATE" });
            dp.ContextMenu.Items.Add(new MenuItem() { Header = "Delete (selected) Node(s) ..", Tag = "DELETE" });

#if _not_now
            dp.ContextMenu.Items.Add(new Separator());
            dp.ContextMenu.Items.Add(new MenuItem() { Header = "Export as SVG ..", Tag = "EXP-SVG" });
#endif

            foreach (var x in dp.ContextMenu.Items)
                if (x is MenuItem mi)
                    mi.Click += ContextMenu_Click;

            // make it re-callable
            theGraph = graph;
            theViewer = viewer;
            theReferable = _submodel;
            _insideDockPanel = dp;

            // return viewer for advanced manipulation
            return viewer;
        }

        private Microsoft.Msagl.Core.Geometry.Curves.PlaneTransformation _savedTransform = null;

        private void Viewer_ViewChangeEvent(object sender, EventArgs e)
        {
            _savedTransform = theViewer.Transform;
        }

        protected bool IsViewerNode(Microsoft.Msagl.Drawing.IViewerNode vn)
        {
            foreach (var x in theViewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode xvn && xvn == vn)
                    return true;
            return false;
        }

        protected Microsoft.Msagl.Drawing.IViewerNode FindViewerNode(Microsoft.Msagl.Drawing.Node node)
        {
            if (theViewer == null || node == null)
                return null;
            foreach (var x in theViewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode vn
                    && vn.Node == node)
                    return vn;
            return null;
        }

        protected IEnumerable<Microsoft.Msagl.Drawing.IViewerNode> GetSelectedViewerNodes()
        {
            if (theViewer == null)
                yield break;

            foreach (var x in theViewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode vn)
                    if (vn.MarkedForDragging)
                        yield return vn;
        }

        protected IEnumerable<Aas.IReferable> GetSelectedViewerReferables()
        {
            if (theViewer == null)
                yield break;

            foreach (var x in theViewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode vn)
                    if (vn.MarkedForDragging && vn.Node?.UserData is Aas.IReferable rf)
                        yield return rf;
        }

        protected Tuple<Aas.Entity, Aas.RelationshipElement> CreateNodeAndRelationInBom(
            string nodeIdShort,
            string nodeSemId,
            string nodeSuppSemId,
            Aas.IReferable parent,
            string relSemId,
            string relSuppSemId)
        {
            // access
            if (_submodel == null)
                return null;

            // create
            var ent = new Aas.Entity(Aas.EntityType.CoManagedEntity, idShort: nodeIdShort);
            ent.Parent = parent;
            if (nodeSemId?.HasContent() == true)
                ent.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSemId) }).ToList());
            if (nodeSuppSemId?.HasContent() == true)
                ent.SupplementalSemanticIds = (new Aas.IReference[] {
                        new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                            (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSuppSemId) }).ToList())
                    }).ToList();

            // where to add?
            var contToAdd = ((parent as Aas.IEntity) as Aas.IReferable) ?? _submodel;
            contToAdd.Add(ent);

            // try build a relationship
            Aas.RelationshipElement rel = null;
            if (parent != null)
            {
                var klFirst = _submodel.BuildKeysToTop(parent as Aas.ISubmodelElement);
                if (klFirst.Count == 0)
                    klFirst.Add(new Aas.Key(Aas.KeyTypes.Submodel, _submodel.Id));
                var klSecond = _submodel.BuildKeysToTop(ent);

                if (klFirst.Count >= 1 && klSecond.Count >= 1)
                {
                    rel = new Aas.RelationshipElement(
                        idShort: "HasPart_" + nodeIdShort,
                        first: new Aas.Reference(Aas.ReferenceTypes.ModelReference, klFirst),
                        second: new Aas.Reference(Aas.ReferenceTypes.ModelReference, klSecond));
                    if (relSemId?.HasContent() == true)
                        rel.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                            (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, relSemId) }).ToList());
                    if (relSuppSemId?.HasContent() == true)
                        rel.SupplementalSemanticIds = (new Aas.IReference[] {
                        new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                            (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, relSuppSemId) }).ToList())
                    }).ToList();
                    contToAdd.Add(rel);
                }
            }

            // ok
            return new Tuple<Aas.Entity, Aas.RelationshipElement>(ent, rel);
        }

        protected void AdjustNodeInBom(
            Aas.ISubmodelElement nodeSme,
            string nodeIdShort,
            string nodeSemId,
            string nodeSuppSemId)
        {
            // access
            if (_submodel == null || nodeSme == null)
                return;

            // we need to exchange node in References!
            var kl = _submodel?.BuildKeysToTop(nodeSme);
            var changeRels = kl.Count >= 2;
            var oldRefToNode = new Aas.Reference(Aas.ReferenceTypes.ModelReference, kl);

            // write back new values
            nodeSme.IdShort = nodeIdShort;
            if (nodeSemId?.HasContent() == true)
                nodeSme.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSemId) }).ToList());
            else
                nodeSme.SemanticId = null;

            if (nodeSuppSemId?.HasContent() == true)
                nodeSme.SupplementalSemanticIds = (new Aas.IReference[] {
                    new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                        (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSuppSemId) }).ToList())
                    }).ToList();
            else
                nodeSme.SupplementalSemanticIds = null;

            // use the same logic to make a replacement reference (needs no further check)
            var newRefToNode = new Aas.Reference(Aas.ReferenceTypes.ModelReference, _submodel?.BuildKeysToTop(nodeSme));

            // now search recursively for all RefElems and RelElems referring to it
            _submodel?.RecurseOnSubmodelElements(null, (o, parents, sme) => {

                // figure out the last parent = container of SME
                Aas.IReferable cont = (parents.Count < 1) ? _submodel : parents.LastOrDefault();

                // to change?
                if (sme is Aas.IRelationshipElement relEl)
                {
                    relEl.First?.ReplacePartialHead(oldRefToNode, newRefToNode);
                    relEl.Second?.ReplacePartialHead(oldRefToNode, newRefToNode);
                }
                if (sme is Aas.IReferenceElement refEl)
                {
                    refEl.Value?.ReplacePartialHead(oldRefToNode, newRefToNode);
                }

                // always search further
                return true;
            });
        }

        protected void AdjustEdgeInBom(
            Aas.ISubmodelElement edgeSme,
            string edgeIdShort,
            string edgeSemId,
            string edgeSuppSemId)
        {
            // access
            if (_submodel == null || edgeSme == null)
                return;

            // write back new values
            edgeSme.IdShort = edgeIdShort;
            if (edgeSemId?.HasContent() == true)
                edgeSme.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, edgeSemId) }).ToList());
            else
                edgeSme.SemanticId = null;

            if (edgeSuppSemId?.HasContent() == true)
                edgeSme.SupplementalSemanticIds = (new Aas.IReference[] {
                    new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                        (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, edgeSuppSemId) }).ToList())
                    }).ToList();
            else
                edgeSme.SupplementalSemanticIds = null;
        }

#if cdscsd
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="source"></param>
        ///// <param name="target"></param>
        ///// <param name="registerForUndo"></param>
        ///// <returns></returns>
        public Drawing.Edge AddEdge(
            Microsoft.Msagl.Drawing.Graph Graph,
            Microsoft.Msagl.Drawing.Node source, Microsoft.Msagl.Drawing.Node target, bool registerForUndo)
        {
            Debug.Assert( Graph.FindNode(source.Id) == source);
            Debug.Assert(Graph.FindNode(target.Id) == target);

            Microsoft.Msagl.Drawing.Edge drawingEdge = Graph.AddEdge(source.Id, target.Id);
            drawingEdge.Label = new Microsoft.Msagl.Drawing.Label();
            var geometryEdge = drawingEdge.GeometryEdge = new Microsoft.Msagl.Core.Layout.Edge();
            geometryEdge.GeometryParent = Graph.GeometryGraph;

            var a = source.GeometryNode.Center;
            var b = target.GeometryNode.Center;
            if (source == target)
            {
                Microsoft.Msagl.Core.Geometry.Geometry.CornerSite start = new CornerSite(a);
                CornerSite end = new CornerSite(b);
                var mid1 = source.GeometryNode.Center;
                mid1.X += (source.GeometryNode.BoundingBox.Width / 3 * 2);
                var mid2 = mid1;
                mid1.Y -= source.GeometryNode.BoundingBox.Height / 2;
                mid2.Y += source.GeometryNode.BoundingBox.Height / 2;
                CornerSite mid1s = new CornerSite(mid1);
                CornerSite mid2s = new CornerSite(mid2);
                start.Next = mid1s;
                mid1s.Previous = start;
                mid1s.Next = mid2s;
                mid2s.Previous = mid1s;
                mid2s.Next = end;
                end.Previous = mid2s;
                geometryEdge.UnderlyingPolyline = new SmoothedPolyline(start);
                geometryEdge.Curve = geometryEdge.UnderlyingPolyline.CreateCurve();
            }
            else
            {
                CornerSite start = new CornerSite(a);
                CornerSite end = new CornerSite(b);
                CornerSite mids = new CornerSite(a * 0.5 + b * 0.5);
                start.Next = mids;
                mids.Previous = start;
                mids.Next = end;
                end.Previous = mids;
                geometryEdge.UnderlyingPolyline = new SmoothedPolyline(start);
                geometryEdge.Curve = geometryEdge.UnderlyingPolyline.CreateCurve();
            }

            geometryEdge.Source = drawingEdge.SourceNode.GeometryNode;
            geometryEdge.Target = drawingEdge.TargetNode.GeometryNode;
            geometryEdge.EdgeGeometry.TargetArrowhead = new Arrowhead() { Length = drawingEdge.Attr.ArrowheadLength };
            Arrowheads.TrimSplineAndCalculateArrowheads(geometryEdge, geometryEdge.Curve, true, true);


            IViewerEdge ve;
            AddEdge(ve = CreateEdgeWithGivenGeometry(drawingEdge), registerForUndo);
            layoutEditor.AttachLayoutChangeEvent(ve);
            return drawingEdge;

        }
#endif

        static Microsoft.Msagl.Core.Layout.Node GeometryNode(Microsoft.Msagl.Drawing.IViewerNode node)
        {
            Microsoft.Msagl.Core.Layout.Node geomNode = ((Microsoft.Msagl.Drawing.Node)node.DrawingObject).GeometryNode;
            return geomNode;
        }

        protected void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi 
                && mi.Tag is string miTag
                && miTag?.HasContent() == true)
            {
                if (miTag == "JUMP")
                {
                    if (_objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode)
                        NavigateTo(_objectUnderCursor?.DrawingObject);
                }

                if (miTag == "EDIT")
                {
                    if (_objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode node
                     && node.Node?.UserData is Aas.ISubmodelElement nodeSme)
                    {
                        // create job
                        var stat = new DialogueStatus() { Type = DialogueType.EditNode, AasElem = nodeSme };

                        // set the action
                        stat.Action = (st, action) =>
                        {
                            // correct?
                            if (action != "OK" || !(st.AasElem is Aas.ISubmodelElement nodeSme))
                                return;

                            // modify
                            AdjustNodeInBom(
                                nodeSme,
                                nodeIdShort: st.TextBoxIdShort.Text,
                                nodeSemId: st.ComboBoxNodeSemId.Text,
                                nodeSuppSemId: st.ComboBoxNodeSupplSemId.Text);

                            // refresh
                            SetNeedsFinalize(true);
                            RedrawGraph();
                        };

                        // in any case, create a node
                        StartShowDialogue(stat);
                    }

                    if (_objectUnderCursor is Microsoft.Msagl.Drawing.IViewerEdge edge
                     && edge.Edge?.UserData is Aas.ISubmodelElement edgeSme)
                    {
                        // create job
                        var stat = new DialogueStatus() { 
                            Type = DialogueType.EditEdge, AasElem = edgeSme 
                        };

                        // set the action
                        stat.Action = (st, action) =>
                        {
                            // correct?
                            if (action != "OK" || !(st.AasElem is Aas.ISubmodelElement esme))
                                return;

                            // modify
                            AdjustEdgeInBom(
                                esme,
                                edgeIdShort: st.TextBoxIdShort.Text,
                                edgeSemId: st.ComboBoxRelSemId.Text,
                                edgeSuppSemId: st.ComboBoxRelSupplSemId.Text);

                            // refresh
                            SetNeedsFinalize(true);
                            RedrawGraph();
                        };

                        // in any case, create a node
                        StartShowDialogue(stat);
                    }
                }

                if (miTag == "CREATE")
                {
                    // create job
                    var stat = new DialogueStatus() { Type = DialogueType.Create };

                    stat.ParentNode = GetSelectedViewerNodes().FirstOrDefault();

                    if (stat.ParentNode == null
                        && _objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode n2)
                        stat.ParentNode = n2;

                    stat.ParentReferable = stat.ParentNode?.Node?.UserData as Aas.IReferable;

                    // figure out if first node
                    stat.IsEntryNode = null == _submodel?.SubmodelElements?.FindFirstSemanticIdAs<Aas.IEntity>(
                        AasxPredefinedConcepts.HierarchStructV10.Static.CD_EntryNode?.GetSingleKey(),
                        matchMode: MatchMode.Relaxed);

                    // figure out reverse direction
                    stat.ReverseDir = _submodel?.SubmodelElements?.FindFirstSemanticIdAs<Aas.IProperty>(
                        AasxPredefinedConcepts.HierarchStructV10.Static.CD_ArcheType?.GetSingleKey(),
                        matchMode: MatchMode.Relaxed)?
                            .Value?.ToUpper().Trim() == "ONEUP";

                    // set the action
                    stat.Action = (st, action) =>
                    {
                        // correct?
                        if (action != "OK" || _bomCreator == null || theViewer == null)
                            return;

                        // check number of nodes BEFORE operation
                        int noOfNodes = theViewer?.Graph?.NodeCount ?? 0;

                        // create entity
                        var ents = CreateNodeAndRelationInBom(
                            nodeIdShort: st.TextBoxIdShort.Text,
                            nodeSuppSemId: st.ComboBoxNodeSupplSemId.Text,
                            nodeSemId: st.ComboBoxNodeSemId.Text,
                            parent: st.ParentReferable,
                            relSemId: st.ComboBoxRelSemId.Text,
                            relSuppSemId: st.ComboBoxRelSupplSemId.Text);
                                               
                        if (ents == null || ents.Item1 == null)
                            return;

#if shitty_no_works
                        // create a node
                        var node = _bomCreator.GenerateEntityNode(ents.Item1, allowSkip: false);
                        theViewer.CreateIViewerNode(node, _rightClickCoordinates, null);

                        // even a link
                        if (ents.Item2 != null && st.ParentNode?.Node != null)
                        {
                            var edge = _bomCreator.CreateRelationLink(
                                theViewer.Graph,
                                st.ParentNode.Node,
                                node,
                                ents.Item2);

                            theViewer.CreateEdgeWithGivenGeometry(edge);
                        }
#else

                        // refresh (if it was empty before, reset viewport)
                        SetNeedsFinalize(true);

                        if (noOfNodes < 1)
                        {
                            theViewer?.SetInitialTransform();
                            _savedTransform = null;
                        }
                        RedrawGraph();

#endif
                    };

                    // in any case, create a node
                    StartShowDialogue(stat);

                    // best approach to set the focus!
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _dialogueStatus.TextBoxIdShort.Focus();
                    }), System.Windows.Threading.DispatcherPriority.Render);
                }

                if (miTag == "DELETE")
                {
                    // create job
                    var stat = new DialogueStatus() { Type = DialogueType.Delete };

                    var test = (_objectUnderCursor as Microsoft.Msagl.Drawing.IViewerNode)?
                                    .Node?.UserData as Aas.IReferable;
                    if (test != null)
                        stat.Nodes.Add(test);
                    stat.Nodes.AddRange(GetSelectedViewerReferables());

                    // set the action
                    stat.Action = (st, action) =>
                    {
                        // all above nodes
                        foreach (var node in st.Nodes)
                        {
                            // only SME
                            if (!(node is Aas.ISubmodelElement nodeSmeToDel))
                                continue;

                            // find containing Referable and KeyList to it
                            // (the key list must not only contain the Submodel!)
                            var contToDelIn = _submodel?.FindContainingReferable(nodeSmeToDel);
                            var kl = _submodel?.BuildKeysToTop(nodeSmeToDel);
                            if (nodeSmeToDel == null || contToDelIn == null || kl.Count < 2)
                                continue;

                            // build reference to it
                            var refToNode = new Aas.Reference(Aas.ReferenceTypes.ModelReference, kl);

                            // now search recursively for:
                            // the node, all RefElems and RelElems referring to it
                            var toDel = new List<Tuple<Aas.IReferable, Aas.ISubmodelElement>>();
                            _submodel?.RecurseOnSubmodelElements(null, (o, parents, sme) => {

                                // figure out the last parent = container of SME
                                Aas.IReferable cont = (parents.Count < 1) ? _submodel : parents.LastOrDefault();

                                // note: trust, that corresponding Remove() will check first for presence ..
                                if ((sme == nodeSmeToDel)
                                    || (sme is Aas.ReferenceElement refEl
                                        && refEl.Value?.Matches(refToNode) == true)
                                    || (sme is Aas.RelationshipElement relEl
                                        && (relEl.First?.Matches(refToNode) == true
                                            || relEl.Second?.Matches(refToNode) == true)))
                                {
                                    toDel.Add(new Tuple<Aas.IReferable, Aas.ISubmodelElement>(cont, sme));
                                }

                                // always search further
                                return true;
                            });

                            // now del
                            foreach (var td in toDel)
                                td.Item1?.Remove(td.Item2);

                        }
                        
                        // refresh
                        SetNeedsFinalize(true);
                        RedrawGraph();
                    };

                    // in any case, create a node
                    StartShowDialogue(stat);

#if shitty_no_works

                    if (_objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode node)
                    {
                        var addNodesToDel = new List<Microsoft.Msagl.Drawing.IViewerNode>();

                        // try to detect additional edges to asset boxes here?
                        if (node?.Node != null)
                            foreach (var x in theViewer.Entities)
                                if (x is Microsoft.Msagl.Drawing.IViewerEdge ve)
                                {
                                    if (ve.Edge.SourceNode == node.Node)
                                        if (ve.Edge?.TargetNode?.UserData is GenericBomCreator.UserDataAsset)
                                            addNodesToDel.Add(FindViewerNode(ve.Edge.TargetNode));
                                    if (ve.Edge.TargetNode == node.Node)
                                        if (ve.Edge?.SourceNode?.UserData is GenericBomCreator.UserDataAsset)
                                            addNodesToDel.Add(FindViewerNode(ve.Edge.SourceNode));
                                }

                        // now delete
                        if (node != null)
                            theViewer.RemoveNode(node, true);

                        // delete additional nodes
                        foreach (var antd in addNodesToDel)
                            if (IsViewerNode(antd))
                                theViewer.RemoveNode(antd, true);

                        // delete node and relations in BOM

                    // which SME does this node relate to
                    var nodeSmeToDel = node?.Node?.UserData as Aas.ISubmodelElement;

                        // find containing Referable and KeyList to it
                        // (the key list must not only contain the Submodel!)
                        var contToDelIn = _submodel?.FindContainingReferable(nodeSmeToDel);
                        var kl = _submodel?.BuildKeysToTop(nodeSmeToDel);
                        if (nodeSmeToDel == null || contToDelIn == null || kl.Count < 2)
                            return;

                        // build reference to it
                        var refToNode = new Aas.Reference(Aas.ReferenceTypes.ModelReference, kl);

                        // now search recursively for:
                        // the node, all RefElems and RelElems referring to it
                        var toDel = new List<Tuple<Aas.IReferable, Aas.ISubmodelElement>>();
                        _submodel?.RecurseOnSubmodelElements(null, (o, parents, sme) => {

                            // figure out the last parent = container of SME
                            Aas.IReferable cont = (parents.Count < 1) ? _submodel : parents.LastOrDefault();

                            // note: trust, that corresponding Remove() will check first for presence ..
                            if (   (sme == nodeSmeToDel)
                                || (sme is Aas.ReferenceElement refEl
                                    && refEl.Value?.Matches(refToNode) == true)
                                || (sme is Aas.RelationshipElement relEl
                                    && (relEl.First?.Matches(refToNode) == true
                                        || relEl.Second?.Matches(refToNode) == true)))
                            {
                                toDel.Add(new Tuple<Aas.IReferable, Aas.ISubmodelElement>(cont, sme));
                            }

                            // always search further
                            return true;
                        });

                        // now del
                        foreach (var td in toDel)
                            td.Item1?.Remove(td.Item2);

                        // refresh
                        RedrawGraph();
                    }
#endif
                }

            // https://github.com/microsoft/automatic-graph-layout/issues/372
#if __not_now

                if (miTag == "EXP-SVG" && theViewer.Graph != null)
                {
                    // ask for file name
                    var dlg = new Microsoft.Win32.SaveFileDialog()
                    {
                        FileName = "new",
                        DefaultExt = ".svg",
                        Filter = "Scalable Vector Graphics (.svg)|*.svg|All files|*.*"
                    };
                    if (dlg.ShowDialog() != true)
                        return;

                    // theViewer.Graph.CreateGeometryGraph();
                    LayoutHelpers.CalculateLayout(theViewer.Graph.GeometryGraph, new SugiyamaLayoutSettings(), null);

                    foreach (var n in theViewer.Graph.Nodes)
                        if (n.Label != null)
                        {
                            n.Label.Width = 100;
                            n.Label.Height = 20;
                        }

                    // take care on resources
                    try
                    {
                        // SvgGraphWriter.Write(theViewer.Graph, dlg.FileName, null, null, 4);
                        using (var stream = File.Create(dlg.FileName))
                        {
                            var svgWriter = new Microsoft.Msagl.Drawing.SvgGraphWriter(stream, theViewer.Graph);
                            svgWriter.Write();
                        }
                    }
                    catch (Exception ex)
                    {
                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    }

                    // toggle redisplay -> graph is renewed for display
                    RedrawGraph();
                }
#endif

            }
        }

        /// <summary>
        /// This is used by the menu option to create BOM overview on full package
        /// </summary>
        public object CreateViewPackageReleations(
            BomStructureOptions bomOptions,
            object opackage,
            DockPanel master)
        {
            // access
            _package = opackage as AdminShellPackageEnvBase;
            _submodel = null;
            _createOnPackage = true;
            _bomOptions = bomOptions;
            if (_bomOptions == null || _package?.AasEnv == null)
                return null;

            // new master panel
            // dead-csharp off
            // var master = new DockPanel();
            // dead-csharp on

            // clear some other members (GenericBomControl is not allways created new)
            _creatorOptions = new GenericBomCreatorOptions();

            // index all submodels
            foreach (var sm in _package.AasEnv.OverSubmodelsOrEmpty())
                sm.SetAllParents();

            // create controls
            var spTop = CreateTopPanel();
            DockPanel.SetDock(spTop, Dock.Top);
            master.Children.Add(spTop);

            // create BOTTOM controls
            var legend = GenericBomCreator.GenerateWrapLegend();
            DockPanel.SetDock(legend, Dock.Bottom);
            master.Children.Add(legend);

            // set default for very small edge label size
            Microsoft.Msagl.Drawing.Label.DefaultFontSize = 6;

            // make a Dock panel (within)
            var dp = new DockPanel();
            dp.ClipToBounds = true;
            dp.MinWidth = 10;
            dp.MinHeight = 10;

            // very important: add first the panel, then add graph
            master.Children.Add(dp);

            // graph
            var graph = CreateGraph(_package, null, _creatorOptions, createOnPackage: _createOnPackage);

            // very important: first bind it, then add graph
            var viewer = new Microsoft.Msagl.WpfGraphControl.GraphViewer();
            viewer.BindToPanel(dp);
            viewer.MouseDown += Viewer_MouseDown;
            viewer.MouseMove += Viewer_MouseMove;
            viewer.MouseUp += Viewer_MouseUp;
            viewer.ObjectUnderMouseCursorChanged += Viewer_ObjectUnderMouseCursorChanged;
            viewer.Graph = graph;

            // make it re-callable
            theGraph = graph;
            theViewer = viewer;
            theReferable = _submodel;

            // return viewer for advanced manipulation
            // dead-csharp off
            // return viewer;
            // dead-csharp on

            // return master
            return master;
        }

        private Microsoft.Msagl.Drawing.Graph CreateGraph(
            AdminShellPackageEnvBase env,
            Aas.Submodel sm,
            GenericBomCreatorOptions options,
            bool createOnPackage = false)
        {
            // access   
            if (env?.AasEnv == null || (sm == null && !createOnPackage) || options == null)
                return null;

            //create a graph object
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("BOM-graph");

#if TESTMODE
            //create the graph content
            graph.AddEdge("A", "B");
            var e1 = graph.AddEdge("B", "C");
            e1.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.None;
            e1.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.None;
            e1.Attr.Color = Microsoft.Msagl.Drawing.Color.Magenta;
            e1.GeometryEdge = new Microsoft.Msagl.Core.Layout.Edge();
            // e1.LabelText = "Dumpf!";
            e1.LabelText = "hbhbjhbjhb";
            // e1.Label = new Microsoft.Msagl.Drawing.Label("Dumpf!!");
            graph.AddEdge("A", "C").Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
            graph.FindNode("A").Attr.FillColor = Microsoft.Msagl.Drawing.Color.Magenta;
            graph.FindNode("B").Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;
            Microsoft.Msagl.Drawing.Node c = graph.FindNode("C");
            graph.FindNode("B").LabelText = "HalliHallo";
            c.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PaleGreen;
            c.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Diamond;
            c.Label.FontSize = 28;

#else

            _bomCreator = new GenericBomCreator(
                env?.AasEnv,
                _bomRecords,
                options);

            // Turn on logging if required
            //// using (var tw = new StreamWriter("bomgraph.log"))
            {
                if (!createOnPackage)
                {
                    // just one Submodel
                    _bomCreator.RecurseOnLayout(1, graph, null, sm.SubmodelElements, 1, null);
                    _bomCreator.RecurseOnLayout(2, graph, null, sm.SubmodelElements, 1, null);
                    _bomCreator.RecurseOnLayout(3, graph, null, sm.SubmodelElements, 1, null);
                }
                else
                {
                    for (int pass = 1; pass <= 3; pass++)
                        foreach (var sm2 in env.AasEnv.OverSubmodelsOrEmpty())
                        {
                            // create AAS and SM
                            if (pass == 1)
                                _bomCreator.CreateAasAndSubmodelNodes(graph, sm2);

                            // modify creator's bomRecords on the fly
                            var recs = new BomStructureOptionsRecordList(
                                _bomOptions.LookupAllIndexKey<BomStructureOptionsRecord>(
                                    sm2.SemanticId?.GetAsExactlyOneKey()));
                            _bomCreator.SetRecods(recs);

                            // graph itself
                            _bomCreator.RecurseOnLayout(pass, graph, null, sm2.SubmodelElements, 1, null,
                                entityParentRef: sm2);
                        }
                }
            }

            // make default or (already) preferred settings
            var settings = GivePresetSettings(options, graph.NodeCount);
            if (this.preferredPreset != null && sm != null
                && this.preferredPreset.ContainsKey(sm))
                settings = GivePresetSettings(this.preferredPreset[sm], graph.NodeCount);
            if (settings != null)
                graph.LayoutAlgorithmSettings = settings;

            // switching between LR and TB makes a lot of issues, therefore:
            // LR is the most useful one!
            graph.Attr.LayerDirection = Microsoft.Msagl.Drawing.LayerDirection.LR;

#endif
            return graph;
        }

        private void Viewer_ObjectUnderMouseCursorChanged(
            object sender, Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs e)
        {
        }

        private void Viewer_MouseUp(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
        }

        private void Viewer_MouseMove(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
        }

        protected void NavigateTo(Microsoft.Msagl.Drawing.DrawingObject obj)
        {
            if (obj != null && obj.UserData != null)
            {
                var us = obj.UserData;
                if (us is Aas.IReferable)
                {
                    // make event
                    var refs = new List<Aas.IKey>();
                    (us as Aas.IReferable).CollectReferencesByParent(refs);

                    // ok?
                    if (refs.Count > 0)
                    {
                        var evt = new AasxPluginResultEventNavigateToReference();
                        evt.targetReference = ExtendReference.CreateNew(refs);
                        this.eventStack.PushEvent(evt);
                    }
                }

                if (us is Aas.Reference)
                {
                    var evt = new AasxPluginResultEventNavigateToReference();
                    evt.targetReference = (us as Aas.Reference);
                    this.eventStack.PushEvent(evt);
                }
            }
        }

        private void Viewer_MouseDown(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
            if (e != null && e.Clicks > 1 && e.LeftButtonIsPressed && theViewer != null && this.eventStack != null)
            {
                // double-click detected, can access the viewer?
                try
                {
                    var x = theViewer.ObjectUnderMouseCursor;
                    NavigateTo(x?.DrawingObject);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            if (e != null && e.Clicks == 1 && e.RightButtonIsPressed 
                && _insideDockPanel.ContextMenu != null
                && theViewer != null)
            {
                _objectUnderCursor = theViewer.ObjectUnderMouseCursor;
                _rightClickCoordinates = theViewer.ScreenToSource(e);
              
                _insideDockPanel.ContextMenu.IsOpen = true;
            }
        }

        private string[] PresetSettingNames =
        {
            "1 | Tree style layout",
            "2 | Round layout (variable)",
        };

        private Microsoft.Msagl.Core.Layout.LayoutAlgorithmSettings GivePresetSettings(
            GenericBomCreatorOptions opt, int nodeCount)
        {
            if (opt == null || opt.LayoutIndex == 0)
            {
                // Tree
                var settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings();
                return settings;
            }
            else
            {
                // Round
                var settings = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
                settings.RepulsiveForceConstant = 8.0 / (1.0 + nodeCount) * (5.0 + opt.LayoutSpacing);
                return settings;
            }
        }

        protected void RememberSettings()
        {
            // try to remember preferred setting
            if (this.theReferable != null && preferredPreset != null && _creatorOptions != null)
                this.preferredPreset[this.theReferable] = _creatorOptions.Copy();
        }

        protected void RedrawGraph()
        {
            try
            {
                // re-draw (brutally)
                theGraph = CreateGraph(_package, _submodel, _creatorOptions, createOnPackage: _createOnPackage);

                theViewer.Graph = null;
                theViewer.Graph = theGraph;

                // may take over last view
                if (_savedTransform != null)
                    theViewer.Transform = _savedTransform;
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
        }

        //
        // Dialog pages
        //

        protected void StartShowDialogue(DialogueStatus stat)
        {
            _tabItemEdit.Content = CreateDialoguePanel(stat);
            _tabControlBottom.SelectedItem = _tabItemEdit;
        }

        protected void HideDialoguePage()
        {
            _tabControlBottom.SelectedItem = _tabControlBottom.Items[0];
            _dialogueStatus = null;
        }

        protected void AddToGrid(
            Grid grid,
            int row, int col,
            int rowSpan = 0, int colSpan = 0,
            FrameworkElement fe = null)
        {
            if (grid == null || fe == null)
                return;

            Grid.SetRow(fe, row);
            Grid.SetColumn(fe, col);
            if (rowSpan > 0)
                Grid.SetRowSpan(fe, rowSpan);
            if (colSpan > 0)
                Grid.SetColumnSpan(fe, colSpan);
            grid.Children.Add(fe);
        }

        protected enum DialogueType { None = 0, EditNode, EditEdge, Create, Delete }

        protected class DialogueStatus
        {
            public DialogueType Type = DialogueType.None;

            public Microsoft.Msagl.Drawing.IViewerNode ParentNode = null;
            public Aas.IReferable ParentReferable = null;

            public Aas.IReferable AasElem = null;
            public List<Aas.IReferable> Nodes = new List<Aas.IReferable>();

            public TextBox TextBoxIdShort = null;

            public ComboBox 
                ComboBoxNodeSemId = null, 
                ComboBoxNodeSupplSemId = null, 
                ComboBoxRelSemId = null,
                ComboBoxRelSupplSemId = null;

            public Action<DialogueStatus, string> Action = null;

            public bool IsEntryNode = false;
            public bool ReverseDir = false;
        }

        protected DialogueStatus _dialogueStatus = null;

        protected Panel CreateDialoguePanel(DialogueStatus stat)
        {
            // remember
            _dialogueStatus = stat;

            if (stat.Type == DialogueType.None)
            {
                // empty panel
                var grid = new Grid();
                return grid;
            }

            if (stat.Type == DialogueType.EditNode || stat.Type == DialogueType.EditEdge 
                || stat.Type == DialogueType.Create)
            {
                // add node
                var grid = new Grid();
                var prefHS = AasxPredefinedConcepts.HierarchStructV10.Static;
                var create = stat.Type == DialogueType.Create;
                var editNode = stat.Type == DialogueType.EditNode;
                var editEdge = stat.Type == DialogueType.EditEdge;

                // 4 rows (IdShort, Node.semId, Node.suppSemId, Rel.semId, Rel.suppSemId, expand, buttons)
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });

                // 4 cols (auto, expand, small, expand)
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(1.0, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(20.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(1.0, GridUnitType.Star) });

                // idShort
                AddToGrid(grid, 0, 0, fe: new Label() { 
                    Content = editEdge ? "Rel.idShort:" : "Node.idShort:" 
                });
                AddToGrid(grid, 0, 1, colSpan:1, fe: stat.TextBoxIdShort = new TextBox() { 
                    Text = (editNode || editEdge) ? stat.AasElem?.IdShort : "", 
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(0, -1, 0, -1),
                    Margin = new Thickness(0, 2, 0, 2),
                });
                AddToGrid(grid, 0, 3, fe: new Label() { Content = "(Enter confirms add)", Foreground = Brushes.DarkGray });

                // Node.semId
                if (create || editNode)
                {
                    AddToGrid(grid, 1, 0, fe: new Label() { Content = "Node.semanticId:" });
                    stat.ComboBoxNodeSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    stat.ComboBoxNodeSemId.Items.Add(prefHS.CD_EntryNode?.GetSingleKey()?.Value);
                    stat.ComboBoxNodeSemId.Items.Add(prefHS.CD_Node?.GetSingleKey()?.Value);

                    if (!editNode)
                    {
                        if (stat.IsEntryNode)
                            stat.ComboBoxNodeSemId.Text = stat.ComboBoxNodeSemId.Items[0].ToString();
                        else
                            stat.ComboBoxNodeSemId.Text = stat.ComboBoxNodeSemId.Items[1].ToString();
                    }
                    else
                    {
                        stat.ComboBoxNodeSemId.Text = "" + (stat.AasElem as Aas.IHasSemantics).
                            SemanticId.Keys?.FirstOrDefault()?.Value;
                    }                

                    AddToGrid(grid, 1, 1, colSpan: 3, fe: stat.ComboBoxNodeSemId);

                    // Node.supplSemId
                    AddToGrid(grid, 2, 0, fe: new Label() { Content = "Node.supplSemId:" });
                    stat.ComboBoxNodeSupplSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    if (_bomRecords != null)
                        foreach (var br in _bomRecords)
                            if (br.NodeSupplSemIds != null)
                                foreach (var nss in br.NodeSupplSemIds)
                                    if (!stat.ComboBoxNodeSupplSemId.Items.Contains(nss))
                                        stat.ComboBoxNodeSupplSemId.Items.Add(nss);

                    if (!editNode)
                    {
                        stat.ComboBoxNodeSupplSemId.Text = "";
                    }
                    else
                    {
                        stat.ComboBoxNodeSupplSemId.Text = 
                            "" + (stat.AasElem as Aas.IHasSemantics)?.SupplementalSemanticIds?
                                .FirstOrDefault()?.Keys?.FirstOrDefault()?.Value;
                    }

                    AddToGrid(grid, 2, 1, colSpan: 3, fe: stat.ComboBoxNodeSupplSemId);

                }

                if (create || editEdge)
                {
                    // Rel.semId
                    AddToGrid(grid, 3, 0, fe: new Label() { Content = "Relation.semanticId:" });
                    stat.ComboBoxRelSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    stat.ComboBoxRelSemId.Items.Add(prefHS.CD_HasPart?.GetSingleKey()?.Value);
                    stat.ComboBoxRelSemId.Items.Add(prefHS.CD_IsPartOf?.GetSingleKey()?.Value);

                    if (!editEdge)
                    {
                        if (stat.ReverseDir)
                            stat.ComboBoxRelSemId.Text = stat.ComboBoxRelSemId.Items[1].ToString();
                        else
                            stat.ComboBoxRelSemId.Text = stat.ComboBoxRelSemId.Items[0].ToString();
                    } else
                    {
                        stat.ComboBoxRelSemId.Text = "" + (stat.AasElem as Aas.IHasSemantics)
                            .SemanticId.Keys?.FirstOrDefault()?.Value;
                    }

                    AddToGrid(grid, 3, 1, colSpan: 3, fe: stat.ComboBoxRelSemId);

                    // Node.supplSemId
                    AddToGrid(grid, 4, 0, fe: new Label() { Content = "Rel.supplSemId:" });
                    stat.ComboBoxRelSupplSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    if (_bomRecords != null)
                        foreach (var br in _bomRecords)
                            if (br.EdgeSupplSemIds != null)
                                foreach (var nss in br.EdgeSupplSemIds)
                                    if (!stat.ComboBoxRelSupplSemId.Items.Contains(nss))
                                        stat.ComboBoxRelSupplSemId.Items.Add(nss);

                    if (!editEdge)
                    {
                        stat.ComboBoxRelSupplSemId.Text = "";
                    }
                    else
                    {
                        stat.ComboBoxRelSupplSemId.Text =
                            "" + (stat.AasElem as Aas.IHasSemantics)?.SupplementalSemanticIds?
                                .FirstOrDefault()?.Keys?.FirstOrDefault()?.Value;
                    }

                    AddToGrid(grid, 4, 1, colSpan: 3, fe: stat.ComboBoxRelSupplSemId);
                }

                // Add button
                var btnCancel = new Button() { Content = "Cancel", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnCancel.Click += (s, e) => {
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePage();
                };
                AddToGrid(grid, grid.RowDefinitions.Count - 1, 1, fe: btnCancel);

                // Add action

                Action lambdaAdd = () => {
                    // access
                    if (stat != null)
                    {
                        var idShort = "" + stat.TextBoxIdShort?.Text;
                        stat.Action?.Invoke(stat, "OK");
                    }

                    // done
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePage();
                };

                stat.TextBoxIdShort.KeyDown += (s2, e2) =>
                {
                    if (e2.Key == System.Windows.Input.Key.Return)
                        lambdaAdd.Invoke();
                };

                var btnAdd = new Button() {  Content = "OK", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnAdd.Click += (s, e) => { lambdaAdd.Invoke(); } ;
                AddToGrid(grid, grid.RowDefinitions.Count - 1, 3, fe: btnAdd);                

                return grid;
            }

            if (stat.Type == DialogueType.Delete)
            {
                // confirmation (delete)
                var grid = new Grid();
                
                // 5 rows (spacer, text, small gap, buttons, space)
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10.0, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });

                // 5 cols (small, expand, small, expand, small)
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });

                // text
                AddToGrid(grid, 1, 1, colSpan: 3, fe: new TextBox() { 
                    Text = "Proceed with deleting selected nodes?", 
                    FontSize = 14.0, TextWrapping = TextWrapping.Wrap,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    IsReadOnly = true
                });

                // Add button
                var btnCancel = new Button() { Content = "Cancel", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnCancel.Click += (s, e) => {
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePage();
                };
                AddToGrid(grid, 3, 1, fe: btnCancel);

                // Add button
                var btnAdd = new Button() { Content = "OK", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnAdd.Click += (s, e) => {
                    // access
                    stat.Action?.Invoke(stat, "OK");

                    // done
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePage();
                };
                AddToGrid(grid, 3, 3, fe: btnAdd);

                return grid;
            }

            // uuh?
            return new Grid();
        }

    }
}
