using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxPackageExplorer;
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

        }
    }
}
