using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AnyUi;

namespace MauiTestTree.Flyouts
{
    public static class MauiFlyoutTestCases
    {
        public static async Task ExecuteMauiFlyoutTestCase(IFlyoutProvider ifo, IMainWindow main, int ndx)
        {
            var dcMaui = ifo?.GetDisplayContext() as AnyUiDisplayContextMaui;
            if (dcMaui == null || main == null)
                return;

            if (ndx == 0)
            {
                var uc = new AnyUiDialogueDataEmpty($"Hallo hallo");

                if (await dcMaui.StartFlyoverModalAsync(uc))
                {
                    await main.RedrawElementViewAsync();
                }
            }

            if (ndx == 1)
            {
                var uc = new AnyUiDialogueDataTextEditorWithContextMenu(
                                            caption: $"Hallo hallo",
                                            mimeType: "application/json",
                                            text: "{ }");

                uc.Presets = new();
                uc.Presets.Add(new AnyUiDialogueDataTextEditor.Preset() { Name = "Aaaaaa", Lines = new[] { "A1", "A2", "A3" } });
                uc.Presets.Add(new AnyUiDialogueDataTextEditor.Preset() { Name = "Bbbbbb", Lines = new[] { "B1", "B2", "B3" } });

                // context menu
                uc.ContextMenuCreate = () =>
                {
                    return new AasxMenu()
                            .AddAction("Clip", "Copy JSON to clipboard", "\U0001F4CB");
                };

                uc.ContextMenuAction = (cmd, mi, ticket) =>
                {
                    if (cmd == "clip")
                    {
                        Trace.WriteLine("clip!");
                    }
                };

                if (await dcMaui.StartFlyoverModalAsync(uc))
                {
                    await main.RedrawElementViewAsync();
                }
            }

            if (ndx == 2)
            {
                var uc = new AnyUiDialogueDataTextBox($"Enter some meaningful text:");

                uc.Symbol = AnyUiMessageBoxImage.Error;
                uc.Text = "123";

                if (await dcMaui.StartFlyoverModalAsync(uc))
                {
                    await main.RedrawElementViewAsync();
                }
            }

            if (ndx == 3)
            {
                var res = await dcMaui.MessageBoxFlyoutShowAsync(
                    "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed " +
                    "diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam",
                    "Very meaningful question",
                    AnyUiMessageBoxButton.YesNoCancel,
                    AnyUiMessageBoxImage.Question);
                Trace.WriteLine($"Answer: {res.ToString()}");
            }

            if (ndx == 4)
            {
                var uc = new AnyUiDialogueDataSelectFromRepository($"Enter Asset id:");

                uc.Items = new List<PackageContainerRepoItem>();
                uc.Items.Add(new PackageContainerRepoItem("https://example.com/assetIds/001", tag: "A", fn: null));
                uc.Items.Add(new PackageContainerRepoItem("https://example.com/assetIds/002", tag: "BBB bbb", fn: null));
                uc.Items.Add(new PackageContainerRepoItem("https://example.com/assetIds/003", tag: "CC C CCC CC", fn: null));

                if (await dcMaui.StartFlyoverModalAsync(uc))
                {
                    await main.RedrawElementViewAsync();
                }
            }

            if (ndx == 5)
            {
                var innerDiaData = new AnyUiDialogueDataChangeElementAttributes()
                {

                };
                
                var uc = new AnyUiDialogueDataModalPanel(innerDiaData.Caption);
                uc.ActivateRenderPanel(innerDiaData,
                    (uci) =>
                    {
                        // create panel
                        var panel = new AnyUiStackPanel();
                        var helper = new AnyUiSmallWidgetToolkit();

                        var g = helper.AddSmallGrid(5, 4, new[] { "220:", "3*", "100:", "1*" },
                                    padding: new AnyUiThickness(0, 5, 0, 5));
                        g.RowDefinitions[1].MinHeight = 16.0;
                        g.RowDefinitions[3].MinHeight = 16.0;
                        panel.Add(g);

                        // Row 0 : Attribute and language
                        helper.AddSmallLabelTo(g, 0, 0, content: "Attribute:", verticalCenter: true);
                        AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g, 0, 1,
                                    items: AnyUiDialogueDataChangeElementAttributes.AttributeNames,
                                    selectedIndex: (int)innerDiaData.AttributeToChange),
                                minWidth: 400),
                            (i) => { innerDiaData.AttributeToChange = (AnyUiDialogueDataChangeElementAttributes.AttributeEnum)i; });

                        helper.AddSmallLabelTo(g, 0, 2, content: "Language:", verticalCenter: true);
                        AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g, 0, 3,
                                    text: innerDiaData.AttributeLang,
                                    items: AasxLanguageHelper.Languages.GetAllLanguages().ToArray(),
                                    isEditable: true),
                                minWidth: 200),
                            (s) => { innerDiaData.AttributeLang = s; });

                        // Row 2 : Change pattern
                        helper.AddSmallLabelTo(g, 2, 0, content: "Change pattern:", verticalCenter: true);
                        AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g, 2, 1,
                                    text: innerDiaData.Pattern,
                                    items: AnyUiDialogueDataChangeElementAttributes.PatternPresets,
                                    isEditable: true),
                                minWidth: 600,
                                colSpan: 3),
                            (s) => { innerDiaData.Pattern = s; });

                        // Row 4 : Help
                        helper.AddSmallLabelTo(g, 4, 0, content: "Help:");
                        helper.Set(
                            helper.AddSmallLabelTo(g, 4, 1,
                                margin: new AnyUiThickness(0, 2, 2, 2),
                                content: string.Join(System.Environment.NewLine, AnyUiDialogueDataChangeElementAttributes.HelpLines),
                                verticalAlignment: AnyUiVerticalAlignment.Top,
                                verticalContentAlignment: AnyUiVerticalAlignment.Top,
                                fontSize: 0.7,
                                wrapping: AnyUiTextWrapping.Wrap),
                            colSpan: 3,
                            minHeight: 100,
                            horizontalAlignment: AnyUiHorizontalAlignment.Stretch);

                        // give back
                        return panel;
                    });

                if (await dcMaui.StartFlyoverModalAsync(uc))
                {
                    Trace.WriteLine("Modal: " + uc.ResultButton.ToString());
                    await main.RedrawElementViewAsync();
                }
            }
        }
    }
}
