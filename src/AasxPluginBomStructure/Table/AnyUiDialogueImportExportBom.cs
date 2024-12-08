/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

namespace AasxPluginBomStructure.Table
{
    /// <summary>
    /// This class allows exporting a Submodel to various UML formats.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// domain is quite the same and maybe special file format dependencies will 
    /// be re equired in the future.
    /// </summary>
    public static class AnyUiDialogueImportExportBom
    {
        public static async Task<bool> ImportExportBomDialogBased(
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext,
            BomStructureOptions pluginOptions,
            bool doImport)
        {
            // access
            if (ticket == null || displayContext == null)
                return false;

            // check preconditions
            if (ticket.Env == null || ticket.Submodel == null || ticket.SubmodelElement != null)
            {
                log?.Error("Import table: A Submodel has to be selected!");
                return false;
            }

            // ask for parameter record?
            var record = ticket["Record"] as ImportExportBomRecord;
            if (record == null)
                record = new ImportExportBomRecord();

            // maybe given a preset name?
            if (ticket["Preset"] is string pname && pluginOptions.Presets != null)
                for (int i = 0; i < pluginOptions.Presets.Count; i++)
                    if (pluginOptions.Presets[i].Name.ToLower()
                            .Contains(pname.ToLower()))
                    {
                        record = pluginOptions.Presets[i];
                    }

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // maybe given a format name?
            if (ticket["Format"] is string fmt)
                ;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel(
                (!doImport)
                    ? "Export BOM items as Table …"
                    : "Import BOM items from Table …");
            uc.ActivateRenderPanel(record,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 3, new[] { "190:", "150:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5));

                    // TODO (???, 0000-00-00): Put this into above function!
                    g.ColumnDefinitions[0].MinWidth = 190;
                    g.ColumnDefinitions[0].MaxWidth = 190;

                    panel.Add(g);

                    int currRow = 0;

                    // Format
                    helper.AddSmallLabelTo(g, currRow, 0, content: "Format:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallComboBoxTo(g, 0, 1,
                                items: ImportExportBomRecord.FormatNames,
                                selectedIndex: (int)record.Format),
                            minWidth: 200, maxWidth: 200, colSpan: 2),
                            (i) => { record.Format = i; });

                    currRow++;

                    // Presets
                    {
                        helper.AddSmallLabelTo(g, currRow, 0, content: "Presets:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        var g2 = helper.Set(
                                    helper.AddSmallGridTo(g, currRow, 1, 1, 4, new[] { "#", "#", "#", "#" },
                                        padding: new AnyUiThickness(0, 0, 4, 0)),
                                    colSpan: 2);

                        if (displayContext.HasCapability(AnyUiContextCapability.DialogWithoutFlyover))
                        {

                            AnyUiUIElement.RegisterControl(
                                helper.AddSmallButtonTo(
                                    g2, 0, 0, content: "Load ..",
                                    padding: new AnyUiThickness(4, 0, 4, 0)),
                                setValueAsync: async (o) =>
                                {
                                    // ask for filename
                                    var ofData = await displayContext.MenuSelectOpenFilenameAsync(
                                        ticket: null, argName: null,
                                        caption: "Select preset JSON file to load ..",
                                        proposeFn: "",
                                        filter: "Preset JSON file (*.json)|*.json|All files (*.*)|*.*",
                                        msg: "Not found",
                                        requireNoFlyout: true);
                                    if (ofData?.Result != true)
                                        return new AnyUiLambdaActionNone();

                                    // load new data
                                    try
                                    {
                                        log?.Info("Loading new preset data {0} ..", ofData.TargetFileName);
                                        var newRec = ImportExportBomRecord.LoadFromFile(ofData.TargetFileName);
                                        record = newRec;
                                        uc.Data = newRec;
                                        return new AnyUiLambdaActionModalPanelReRender(uc);
                                    }
                                    catch (Exception ex)
                                    {
                                        log?.Error(ex, "when loading plugin preset data");
                                    }
                                    return new AnyUiLambdaActionNone();
                                });

                            AnyUiUIElement.RegisterControl(
                                helper.AddSmallButtonTo(
                                    g2, 0, 1, content: "Save ..",
                                    padding: new AnyUiThickness(4, 0, 4, 0)),
                                setValueAsync: async (o) =>
                                {
                                    // ask for filename
                                    var sfData = await displayContext.MenuSelectSaveFilenameAsync(
                                        ticket: null, argName: null,
                                        caption: "Select preset JSON file to save ..",
                                        proposeFn: "new.json",
                                        filter: "Preset JSON file (*.json)|*.json|All files (*.*)|*.*",
                                        msg: "Not found");
                                    if (sfData?.Result != true)
                                        return new AnyUiLambdaActionNone();

                                    // save new data
                                    try
                                    {
                                        record.SaveToFile(sfData.TargetFileName);
                                        log?.Info("Saved preset data to {0}.", sfData.TargetFileName);
                                    }
                                    catch (Exception ex)
                                    {
                                        log?.Error(ex, "when saving plugin preset data");
                                    }
                                    return new AnyUiLambdaActionNone();
                                });
                        }

                        if (pluginOptions?.Presets != null)
                        {
                            helper.AddSmallLabelTo(g2, 0, 2, content: "From options:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                            AnyUiComboBox cbPreset = null;
                            cbPreset = AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallComboBoxTo(g2, 0, 4,
                                        items: pluginOptions.Presets?.Select((pr) => "" + pr.Name).ToArray(),
                                        text: "Please select preset to load .."),
                                    minWidth: 350, maxWidth: 400),
                                    (o) =>
                                    {
                                        if (!cbPreset.SelectedIndex.HasValue)
                                            return new AnyUiLambdaActionNone();
                                        var ndx = cbPreset.SelectedIndex.Value;
                                        if (ndx < 0 || ndx >= pluginOptions.Presets.Count)
                                            return new AnyUiLambdaActionNone();
                                        var newRec = pluginOptions.Presets[ndx];
                                        record = newRec;
                                        uc.Data = newRec;
                                        return new AnyUiLambdaActionModalPanelReRender(uc);
                                    });

                        }
                    }

                    currRow++;

                    // Row Start
                    helper.AddSmallLabelTo(g, currRow, 0, content: "Row of start of data:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallTextBoxTo(g, currRow, 1,
                                margin: new AnyUiThickness(0, 2, 2, 2),
                                text: $"{record.RowStart:D}",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                minWidth: 70, maxWidth: 70,
                                horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                (i) => { record.RowStart = i; });

                    currRow++;

                    // Col Hierarchy
                    helper.AddSmallLabelTo(g, currRow, 0, content: "Column(s) Hierarchy:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.RegisterControl(
                        helper.Set(
                            helper.AddSmallTextBoxTo(g, currRow, 1,
                                margin: new AnyUiThickness(0, 2, 2, 2),
                                text: "" + string.Join(",", record.ColHierarchy?.Select((n) => n.ToString()) ?? new List<string>()),
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                                setValue: (o) => { 
                                    if (o is string st)
                                    {
                                        var l = st.Split(',', options: StringSplitOptions.RemoveEmptyEntries
                                                                       | StringSplitOptions.TrimEntries);
                                        record.ColHierarchy = new List<int>();
                                        foreach (var le in l)
                                            if (int.TryParse(le, out var i) && i >= 1)
                                                record.ColHierarchy.Add(i);
                                    }
                                    return new AnyUiLambdaActionNone();
                                });

                    helper.AddSmallLabelTo(g, currRow, 2, content: "(optional, comma separated list of numbers)",
                            padding: new AnyUiThickness(10, 0, 0, 0),
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    currRow++;

                    // lambda for adding column fields
                    Action<string, string, int, Action<int>> lambdaAddColumnField = (title, comment, val, setVal) =>
                    {

                        helper.AddSmallLabelTo(g, currRow, 0, content: title,
                                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                                    verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, currRow, 1,
                                    margin: new AnyUiThickness(0, 2, 2, 2),
                                    text: $"{val:D}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: 70, maxWidth: 70,
                                    horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                    (i) => { setVal?.Invoke(i); });

                        helper.AddSmallLabelTo(g, currRow, 2, content: comment,
                                padding: new AnyUiThickness(10, 0, 0, 0),
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        currRow++;
                    };

                    // engange lambda
                    lambdaAddColumnField("Column IdShort", "(mandatory, will be filtered)", 
                        record.ColIdShort, (i) => { record.ColIdShort = i; });

                    lambdaAddColumnField("Column DisplayName", "(optional, -1 will disable)",
                        record.ColDispName, (i) => { record.ColDispName = i; });

                    lambdaAddColumnField("Column bulk count", "(optional, -1 will disable)",
                        record.ColBulkCount, (i) => { record.ColBulkCount = i; });

                    lambdaAddColumnField("Column ref.desig.", "(optional, -1 will disable)",
                        record.ColRefDesignation, (i) => { record.ColRefDesignation = i; });

                    lambdaAddColumnField("Column manufacturer", "(optional, -1 will disable)",
                        record.ColManufacturer, (i) => { record.ColManufacturer = i; });

                    lambdaAddColumnField("Column part name", "(optional, -1 will disable)",
                        record.ColPartName, (i) => { record.ColPartName = i; });

                    lambdaAddColumnField("Column order code", "(optional, -1 will disable)",
                        record.ColPartOrderCode, (i) => { record.ColPartOrderCode = i; });

                    lambdaAddColumnField("Column part url", "(optional, -1 will disable)",
                        record.ColPartUrl, (i) => { record.ColPartUrl = i; });

                    lambdaAddColumnField("# worksheets", "(optional, -1 means all worksheets)",
                        record.NumOfWorksheets, (i) => { record.NumOfWorksheets = i; });

                    // Flags

                    helper.AddSmallLabelTo(g, currRow, 0, content: "Flags:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g, currRow, 1,
                                    content: "Relation below Entity",
                                    isChecked: record.RelInEntity,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                                (b) => { record.RelInEntity = b; });

                    currRow++;

                    // give back
                    return panel;
                });

            if (!ticket.ScriptMode)
            {
                // do the dialogue
                if (!(await displayContext.StartFlyoverModalAsync(uc)))
                    return false;

                // stop
                await Task.Delay(1000);
            }

            // dome open/ save dialog base data
            var dlgFileName = "";
            var dlgFilter = "";

            if (record.Format == (int)ImportExportBomRecord.FormatEnum.Excel)
            {
                dlgFileName = "new.xlsx";
                dlgFilter = "Microsoft Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            }

            // ask for filename?
            if (!doImport)
            {
                // Export
                if (!(await displayContext.MenuSelectSaveFilenameToTicketAsync(
                            ticket, "File",
                            "Select file to be exported",
                            dlgFileName,
                            dlgFilter,
                            "Export table: No valid filename.",
                            argLocation: "Location",
                            reworkSpecialFn: true)))
                    return false;
            }
            else
            {
                if (!(await displayContext.MenuSelectOpenFilenameToTicketAsync(
                            ticket, "File",
                            "Select file to be imported ..",
                            dlgFileName,
                            dlgFilter,
                            "Import table: No valid filename.")))
                    return false;
            }

            var fn = ticket["File"] as string;

            // the Submodel elements need to have parents
            var sm = ticket.Submodel;
            sm.SetAllParents();
            var res = false;
            if (!doImport)
            {
                // Export
                // Export(options, record, fn, sm, ticket?.Env, ticket, log);

                // persist
                // await displayContext.CheckIfDownloadAndStart(log, ticket["Location"], fn);

                // done
                log.Info($"Exporting table data to table {fn} finished.");
            }
            else
            {
                // Import
                res = Import(pluginOptions, record, fn, sm, ticket?.Env, ticket, log);
                log.Info($"Importing table data from table {fn} finished.");
            }

            return res;
        }

        private static bool Import(
            BomStructureOptions options,
            ImportExportBomRecord record,
            string fn,
            Aas.ISubmodel sm, Aas.IEnvironment env,
            AasxMenuActionTicket ticket = null,
            LogInstance log = null)
        {
            // get the import file
            if (fn == null || record == null)
                return false;
            var success = false;

            // try import
            try
            {
                log.Info("Importing BOM table: {0}", fn);
                try
                {
                    if (record.Format == (int)ImportExportBomRecord.FormatEnum.Excel)
                    {
                        success = true;
                        var pop = new ImportBomPopulateByTable(log, record, sm, env, options);
                        int nws = 0;
                        foreach (var tp in ImportTableExcelProvider.CreateProviders(fn))
                        {
                            // count number of work sheets
                            nws++;
                            if (record.NumOfWorksheets >= 1 && nws > record.NumOfWorksheets)
                                break;

                            // populate
                            pop.PopulateBy(tp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(ex, "importing BOM table");
                    success = false;
                }

                if (!success && ticket?.ScriptMode != true)
                    log?.Error(
                        "Table import: Some error occured while importing the table. " +
                        "Please refer to the log messages.");
            }
            catch (Exception ex)
            {
                log?.Error(ex, "When exporting table, an error occurred");
            }

            return success;
        }

    }
}
