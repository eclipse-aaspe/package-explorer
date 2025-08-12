/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using AnyUi;
using System.Reflection;
using DocumentFormat.OpenXml.Spreadsheet;
using ClosedXML.Excel;
using AasxPluginExportTable.Table;

namespace AasxPluginExportTable.BulkChange
{
    /// <summary>
    /// This class allows bulk changing attributes of an Environment.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// functionality uses table input.
    /// </summary>
    public static class AnyUiDialogueBulkChangeSemanticId
    {
        public static async Task<bool> BulkChangeSemanticIdDialogBased(
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext)
        {
            // access
            if (ticket == null || displayContext == null)
                return false;

            // check preconditions
            if (ticket.Env == null)
            {
                log?.Error("Bulk change: A environment needs to be given!");
                return false;
            }

            // ask for parameter record?
            var record = ticket["Record"] as BulkChangeSemanticIdRecord;
            if (record == null)
                record = new BulkChangeSemanticIdRecord();

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // maybe given a format name?
            if (ticket["Format"] is string fmt)
                for (int i = 0; i < BulkChangeSemanticIdRecord.FormatNames.Length; i++)
                    if (BulkChangeSemanticIdRecord.FormatNames[i].ToLower()
                            .Contains(fmt.ToLower()))
                        record.Format = (BulkChangeSemanticIdRecord.FormatEnum)i;

            //
            // Screen 1
            // 

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel("Bulk change semanticId ..");
            uc.ActivateRenderPanel(record,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 3, new[] { "220:", "#", "2:" },
                                padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(g);

                    // Row 0 : Format
                    helper.AddSmallLabelTo(g, 0, 0, content: "Format:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallComboBoxTo(g, 0, 1,
                                items: BulkChangeSemanticIdRecord.FormatNames,
                                selectedIndex: (int)record.Format),
                            minWidth: 200, maxWidth: 200),
                            (i) => { record.Format = (BulkChangeSemanticIdRecord.FormatEnum)i; });

                    // Row 1..n : automatic generation
                    displayContext.AutoGenerateUiFieldsFor(record, helper, g, startRow: 2);

                    // give back
                    return panel;
                });
            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return false;

            // stop
            await Task.Delay(2000);

            // ask for filename?
            if (!(await displayContext.MenuSelectOpenFilenameToTicketAsync(
                        ticket, "File",
                        "Select file for change pairs (2 columns) ..",
                        "",
                        "Excel file (*.xlsx)|*.xlsx|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*",
                        "Bulk change: No valid filename.")))
                return false;

            var fn = ticket["File"] as string;

            //
            // Load excel file into pairs
            //
            var pairs = new List<BulkChangeSemanticIdPair>();

            try
            {
                // open provider for choosen format
                IImportTableProvider provider = null;

                if (record.Format == BulkChangeSemanticIdRecord.FormatEnum.Excel)
                {
                    provider = ImportTableExcelProvider.CreateProviders(fn).FirstOrDefault();
                    if (provider == null || provider.MaxRows < 1)
                        provider = null;
                }

                if (provider == null)
                {
                    log?.Error($"Bulk change semanticIds: no valid exchange pairs in table {fn}. Aborting!");
                    return false;
                }

                // try read
                for (int row = record.RowPairs; row < provider.MaxRows; row++)
                {
                    var oid = provider.Cell(row, 0);
                    var nid = provider.Cell(row, 1);
                    if (oid?.HasContent() == true && nid?.HasContent() == true)
                    {
                        pairs.Add(new BulkChangeSemanticIdPair()
                        {
                            OldId = oid,
                            IntermediateId = $"--BULKID--{row:D5}",
                            NewId = nid,
                        });
                    }
                }

                if (pairs.Count < 1)
                {
                    log?.Error($"Bulk change semanticIds: no valid exchange pairs in table {fn}. Aborting!");
                    return false;
                }
            }
            catch (Exception ex) {
                log?.Error(ex, "when reading excel for bulk change of semanticIds");
            }

            //
            // Screen 2: show list of elements
            //
            var ucSelect = new AnyUiDialogueDataSelectFromDataGrid(
                        "Preview change pairs ..",
                        maxWidth: 9999);

            ucSelect.ColumnDefs = AnyUiListOfGridLength.Parse(new[] { "2*", "1*", "2*" });
            ucSelect.ColumnHeaders = new[] { "Old semanticId", "Intermediate id", "New semanticId" };
            ucSelect.Rows = pairs.Select((pair) => {
                return new AnyUiDialogueDataGridRow(tag: pair, pair.OldId, pair.IntermediateId, pair.NewId);
            }).ToList();
            ucSelect.EmptySelectOk = true;

            if (ticket?.ScriptMode != true)
            {
                // if not in script mode, perform dialogue
                if (!(await displayContext.StartFlyoverModalAsync(ucSelect)))
                    return false;
            }

            // translate result items
            var rowsToUpload = ucSelect.ResultItems;
            if (rowsToUpload == null || rowsToUpload.Count() < 1)
                // nothings means: everything!
                rowsToUpload = ucSelect.Rows;

            if (rowsToUpload.Count < 1)
            {
                log?.Error($"Bulk change semanticIds: no valid exchange pairs in table {fn}. Aborting!");
                return false;
            }

            //
            // Screen 3 : ask to proceed?
            //

            if (AnyUiMessageBoxResult.Yes != displayContext.MessageBoxFlyoutShow(
                $"Proceed with changing {rowsToUpload.Count} semanticIds?",
                $"Bulk change semanticIds",
                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
            {
                log?.Info("Aborted.");
                return false;
            }

            //
            // Step 4: call intended change function
            //

            var num = BulkChangeCore.BulkChangeSemanticId(ticket.Env, pairs, 
                        record.ChangeValueId);
            log?.Info($"Bulk change semanticIds: {num} ids were changed.");

            //
            // finalize
            //
            log?.Info($"Bulk change semanticIds from table {fn} finished.");
            return true;
        }
    }
}
