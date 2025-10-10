/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPackageExplorer;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AasxPackageLogic.PackageCentral.PackageContainerHttpRepoSubset;
using Aas = AasCore.Aas3_1;

namespace AasxPackageLogic
{
    public class DispEditHelperEntities : DispEditHelperSammModules
    {
        static string PackageSourcePath = "";
        static string PackageTargetFn = "";
        static string PackageTargetDir = "/aasx";
        static bool PackageEmbedAsThumbnail = false;

        public DispEditHelperCopyPaste.CopyPasteBuffer theCopyPaste = new DispEditHelperCopyPaste.CopyPasteBuffer();


        //
        //
        // --- AssetInformation
        //
        //

        public void DisplayOrEditAasEntityAssetInformation(
            PackageCentral.PackageCentral packages, Aas.IEnvironment env,
            Aas.IAssetAdministrationShell aas, Aas.IAssetInformation asset,
            object preferredNextFocus,
            bool editMode, ModifyRepo repo, AnyUiStackPanel stack, bool embedded = false,
            bool hintMode = false,
            AasxMenu superMenu = null)
        {
            // Kind

            this.DisplayOrEditEntityAssetKind(stack, asset.AssetKind,
                (k) => { asset.AssetKind = k; }, relatedReferable: aas);

            // Global Asset ID

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () => string.IsNullOrEmpty(asset.GlobalAssetId) == true,
                    "It is strongly encouraged to have the AAS associated with a global asset id from the " +
                    "very beginning. If the AAS describes a product, the individual asset id should be " +
                    "found on its name plate. " +
                    "This  attribute  is  required  as  soon  as  the  AAS  is exchanged via partners in " +
                    "the life cycle of the asset.",
                    severityLevel: HintCheck.Severity.High),
                new HintCheck(
                    () =>
                    {
                        int count = 0;
                        foreach(var aas in env.AllAssetAdministrationShells())
                        {
                            if(aas.AssetInformation.GlobalAssetId == asset.GlobalAssetId)
                                count++;
                        }
                        return (count>=2?true:false);
                    },
                    "It is not allowed to have duplicate GlobalAssetIds in the same file. This will break functionality and we strongly encoure to make the Id unique!",
                    severityLevel: HintCheck.Severity.High)
            });

            // Global Asset ID

            this.AddGroup(stack, "globalAssetId:", this.levelColors.SubSection);

            if (this.SafeguardAccess(
                    stack, repo, asset.GlobalAssetId, "globalAssetId:", "Create data element!",
                    v =>
                    {
                        asset.GlobalAssetId = "";
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                //TODO (jtikekar, 0000-00-00): check with Micha
                this.AddKeyValueExRef(stack, "globalAssetId", asset, asset.GlobalAssetId, null, repo,
                    setValue: v =>
                    {
                        asset.GlobalAssetId = v as string;
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: new[] { "Generate", "Input", "Rename", "Add existing", "Delete" },
                    auxButtonToolTips: new[] {
                        "Generate an id based on the customizable template option for asset ids.",
                        "Input the id, may be by the aid of barcode scanner",
                        "Rename the id and all occurences of the id in the AAS",
                        "Add id from existing element in main/ aux packages",
                        "Delete this entity"
                    },
                    auxButtonLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            asset.GlobalAssetId = "" + AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdAsset);
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: preferredNextFocus);
                        }

                        if (i == 1)
                        {
                            var uc = new AnyUiDialogueDataTextBox(
                                "Global Asset ID:",
                                maxWidth: 1400,
                                symbol: AnyUiMessageBoxImage.Question,
                                options: AnyUiDialogueDataTextBox.DialogueOptions.FilterAllControlKeys,
                                text: "" + asset.GlobalAssetId);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                asset.GlobalAssetId = "" + uc.Text;
                                this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: asset);
                            }
                        }

                        if (i == 2 && env != null)
                        {
                            var uc = new AnyUiDialogueDataTextBox(
                                "New Global Asset ID:",
                                symbol: AnyUiMessageBoxImage.Question,
                                maxWidth: 1400,
                                text: "" + asset.GlobalAssetId);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                var res = false;

                                try
                                {
                                    // rename
                                    var lrf = env.RenameIdentifiable<Aas.AssetInformation>(
                                        asset.GlobalAssetId,
                                        uc.Text);

                                    // use this information to emit events
                                    if (lrf != null)
                                    {
                                        res = true;
                                        foreach (var rf in lrf)
                                        {
                                            var rfi = rf.FindParentFirstIdentifiable();
                                            if (rfi != null)
                                                this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                }

                                if (!res)
                                    this.context.MessageBoxFlyoutShow(
                                        "The renaming of the Submodel or some referring elements " +
                                        "has not performed successfully! Please review your inputs and " +
                                        "the AAS structure for any inconsistencies.",
                                        "Warning",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);

                                return new AnyUiLambdaActionRedrawAllElements(asset);
                            }
                        }

                        if (i == 3)
                        {
                            var k2 = SmartSelectAasEntityKeys(packages,
                                PackageCentral.PackageCentral.Selector.MainAuxFileRepo, "All");

                            if (k2 != null && k2.Count >= 1)
                            {
                                asset.GlobalAssetId = "" + k2[0].Value;
                                this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                            }
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: asset);
                        }

                        if (i == 4)
                        {
                            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                               "Delete globalAssetId?",
                               "AssetInformation",
                               AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            {
                                asset.GlobalAssetId = null;
                                this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                            }
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: asset);
                        }

                        return new AnyUiLambdaActionNone();
                    });
                // dead-csharp off
                //this.AddKeyReference(
                //    stack, "globalAssetId", asset.GlobalAssetId, repo,
                //    packages, PackageCentral.PackageCentral.Selector.MainAux,
                //    showRefSemId: false,
                //    auxButtonTitles: new[] { "Generate", "Input", "Rename" },
                //    auxButtonToolTips: new[] {
                //        "Generate an id based on the customizable template option for asset ids.",
                //        "Input the id, may be by the aid of barcode scanner",
                //        "Rename the id and all occurences of the id in the AAS"
                //    },
                //    auxButtonLambda: (i) =>
                //    {
                //        if (i == 0)
                //        {
                //            asset.GlobalAssetId = "" + AdminShellUtil.GenerateIdAccordingTemplate(
                //                Options.Curr.TemplateIdAsset);
                //            this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                //            return new AnyUiLambdaActionRedrawAllElements(nextFocus: preferredNextFocus);
                //        }

                //        if (i == 1)
                //        {
                //            var uc = new AnyUiDialogueDataTextBox(
                //                "Global Asset ID:",
                //                maxWidth: 1400,
                //                symbol: AnyUiMessageBoxImage.Question,
                //                options: AnyUiDialogueDataTextBox.DialogueOptions.FilterAllControlKeys,
                //                text: "" + asset.GlobalAssetId);
                //            if (this.context.StartFlyoverModal(uc))
                //            {
                //                asset.GlobalAssetId = "" + uc.Text;
                //                this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                //                return new AnyUiLambdaActionRedrawAllElements(nextFocus: asset);
                //            }
                //        }

                //        if (i == 2 && env != null)
                //        {
                //            var uc = new AnyUiDialogueDataTextBox(
                //                "New Global Asset ID:",
                //                symbol: AnyUiMessageBoxImage.Question,
                //                maxWidth: 1400,
                //                text: "" + asset.GlobalAssetId);
                //            if (this.context.StartFlyoverModal(uc))
                //            {
                //                var res = false;

                //                try
                //                {
                //                    // rename
                //                    var lrf = env.RenameIdentifiable<Aas.AssetInformation>(
                //                        asset.GlobalAssetId,
                //                        uc.Text);

                //                    // use this information to emit events
                //                    if (lrf != null)
                //                    {
                //                        res = true;
                //                        foreach (var rf in lrf)
                //                        {
                //                            var rfi = rf.FindParentFirstIdentifiable();
                //                            if (rfi != null)
                //                                this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                //                        }
                //                    }
                //                }
                //                catch (Exception ex)
                //                {
                //                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                //                }

                //                if (!res)
                //                    this.context.MessageBoxFlyoutShow(
                //                        "The renaming of the Submodel or some referring elements " +
                //                        "has not performed successfully! Please review your inputs and " +
                //                        "the AAS structure for any inconsistencies.",
                //                        "Warning",
                //                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);

                //                return new AnyUiLambdaActionRedrawAllElements(asset);
                //            }
                //        }
                //        return new AnyUiLambdaActionNone();
                //    });
                // dead-csharp on
                // print code sheet
                AddActionPanel(stack, "Actions:",
                repo: repo,
                superMenu: superMenu,
                ticketMenu: new AasxMenu()
                    .AddAction("print-code-sheet", "Print asset code sheet ..",
                        "Prints an sheet with 2D codes for the asset id."),
                ticketAction: (buttonNdx, ticket) =>
                {
                    if (buttonNdx == 0)
                    {
                        if (context is AnyUiContextPlusDialogs cpd
                            && cpd.HasCapability(AnyUiContextCapability.WPF))
                        {
                            var uc = new AnyUiDialogueDataEmpty();
                            this.context?.StartFlyover(uc);
                            try
                            {
                                if (string.IsNullOrEmpty(asset.GlobalAssetId) != true)
                                    this.context?.PrintSingleAssetCodeSheet(
                                        asset.GlobalAssetId, aas?.IdShort);
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, "When printing, an error occurred");
                            }
                            this.context?.CloseFlyover();
                        }
                        else
                        {
                            Log.Singleton.Error("Printing is only supported in the WPF version.");
                        }
                    }
                    return new AnyUiLambdaActionNone();
                });
            }

            // Asset Type

            this.AddGroup(stack, "assetType:", this.levelColors.SubSection);

            if (this.SafeguardAccess(
                    stack, repo, asset.AssetType, "assetType:", "Create data element!",
                    v =>
                    {
                        asset.AssetType = "";
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }))
            {
                //TODO (jtikekar, 0000-00-00): check with Micha
                this.AddKeyValueExRef(stack, "assetType", asset, asset.AssetType, null, repo,
                    setValue: v =>
                    {
                        asset.AssetType = v as string;
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    });
            }

            // Specific Asset IDs
            // list of multiple key value pairs
            this.DisplayOrEditEntityListOfSpecificAssetIds(stack, asset.SpecificAssetIds,
                (ico) => { asset.SpecificAssetIds = ico; },
                key: "specificAssetId",
                relatedReferable: aas);

            // Thumbnail: File [0..1]

            this.AddGroup(stack, "DefaultThumbnail: Resource element", this.levelColors.SubSection,
                requestAuxButton: repo != null,
                auxButtonTitle: (asset.DefaultThumbnail == null) ? null : "Delete",
                auxButtonLambda: (o) =>
                {
                    if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                               "Delete Resource element for thumbnail? This operation can not be reverted!",
                               "AssetInformation",
                               AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                    {
                        asset.DefaultThumbnail = null;
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }

                    return new AnyUiLambdaActionNone();
                });

            if (this.SafeguardAccess(
                stack, repo, asset.DefaultThumbnail, $"defaultThumbnail:", $"Create empty Resource element!",
                v =>
                {
                    asset.DefaultThumbnail = new Aas.Resource(""); //File replaced by resource in V3
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                var substack = AddSubStackPanel(stack, "  "); // just a bit spacing to the left
                // dead-csharp off
                // Note: parentContainer = null effectively seems to disable "unwanted" functionality
                // DisplayOrEditAasEntitySubmodelElement(
                //    packages: packages, env: env, parentContainer: null, wrapper: null,
                //    sme: (Aas.ISubmodelElement)asset.DefaultThumbnail,
                //    editMode: editMode, repo: repo, stack: substack, hintMode: hintMode);
                // dead-csharp on
                DisplayOrEditEntityFileResource(
                    substack, 
                    packages.Main,
                    aas, repo, superMenu,
                    asset.DefaultThumbnail.Path, asset.DefaultThumbnail.ContentType,
                    (fn, ct) =>
                    {
                        asset.DefaultThumbnail.Path = fn;
                        asset.DefaultThumbnail.ContentType = ct;
                    },
                    relatedReferable: aas);
            }

        }

        //
        //
        // --- AAS Env
        //
        //

        public void DisplayOrEditAasEntityAasEnv(
            PackageCentral.PackageCentral packages, Aas.IEnvironment env,
            VisualElementEnvironmentItem ve, bool editMode, AnyUiStackPanel stack,
            bool hintMode = false,
            AasxMenu superMenu = null,
            IMainWindow mainWindow = null)
        {
            this.AddGroup(stack, "Environment of AssetInformation Administration Shells", this.levelColors.MainSection);
            if (env == null)
                return;

            if (editMode &&
                (ve.theItemType == VisualElementEnvironmentItem.ItemType.Env
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.Shells
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllConceptDescriptions))
            {
                // some hints
                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return env.AssetAdministrationShellCount() < 1; },
                            "There are no Administration Shells in this AAS environment. " +
                            (env.AssetAdministrationShells == null ? "List is null! " : "List is empty! ") +
                            "You should consider adding an Administration Shell by clicking 'Add AAS' " +
                            "on the edit panel below.",
                        breakIfTrue: true),
                    new HintCheck(
                        () => { return env.SubmodelCount() < 1; },
                            "There are no Submodels in this AAS environment. " +
                            (env.Submodels == null ? "List is null! " : "List is empty! ") +
                            "In this application, Submodels are " +
                            "created by adding them to associated to Administration Shells. " +
                            "Therefore, an Adminstration Shell shall exist before and shall be selected. " +
                            "You could then add Submodels by clicking " +
                            "'Create new Submodel of kind Type/Instance' on the edit panel. " +
                            "This step is typically done after creating asset and Administration Shell."),
                    new HintCheck(
                        () => { return env.ConceptDescriptionCount() < 1; },
                            "There are no ConceptDescriptions in this AAS environment. " +
                            (env.ConceptDescriptions == null ? "List is null! " : "List is empty! ") +
                            "Even if SubmodelElements can reference external concept descriptions, " +
                            "it is best practice to include (duplicates of the) concept descriptions " +
                            "inside the AAS environment. You should consider adding a ConceptDescription " +
                            "by clicking 'Add ConceptDescription' on the panel below or " +
                            "adding a SubmodelElement to a Submodel. This step is typically done after " +
                            "creating assets and Administration Shell and when creating SubmodelElements."),
                });

                // let the user control the number of entities
                AddActionPanel(
                    stack, "Entities:",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("add-aas", "Add AAS",
                            "Adds an AAS with blank information.")
                        .AddAction("add-cd", "Add ConceptDescription",
                            "Adds an ConceptDescription with blank information.")
                        .AddAction("add-sm-inst", "Add Submodel instance",
                            "Adds an Submodel instance without direct reference in AAS.",
                            conditional: ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels)
                        .AddAction("add-sm-temp", "Add Submodel template",
                            "Adds an Submodel template without direct reference in AAS.",
                            conditional: ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // create TOGETHER with AssetInformation!!, as serialization might fail!
                            var aas = new Aas.AssetAdministrationShell("",
                                new Aas.AssetInformation(Aas.AssetKind.NotApplicable));
                            aas.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdAas);
                            env.Add(aas);
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange(
                                StructuralChangeReason.Create));
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: aas);
                        }

                        if (buttonNdx == 1)
                        {
                            var cd = new Aas.ConceptDescription("");
                            cd.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdConceptDescription);
                            env.Add(cd);
                            this.AddDiaryEntry(cd, new DiaryEntryStructChange(
                                StructuralChangeReason.Create));
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: cd);
                        }

                        if (buttonNdx == 2 || buttonNdx == 3)
                        {
                            var sm = new Aas.Submodel("");
                            sm.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                (buttonNdx == 2) ? Options.Curr.TemplateIdSubmodelInstance
                                    : Options.Curr.TemplateIdSubmodelTemplate);
                            if(buttonNdx == 2)
                            {
                                sm.Kind = ModellingKind.Instance;
                            }
                            else
                            {
                                sm.Kind = ModellingKind.Template;
                            }
                            env.Add(sm);
                            this.AddDiaryEntry(sm, new DiaryEntryStructChange(
                                StructuralChangeReason.Create));
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sm);
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // Copy AAS
                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.Shells)
                {
                    this.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                    this.AddActionPanel(
                        stack, "Copy existing AAS:",
                        repo: repo,
                        superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("copy-single", "Copy single",
                                "Copy single selected entity from another AAS, caring for ConceptDescriptions.")
                            .AddAction("copy-recurse", "Copy recursively",
                                "Copy selected entity and children from another AAS, caring for ConceptDescriptions.")
                            .AddAction("copy-with-files", "Copy rec. w/ suppl. files",
                                "Copy selected entity and children from another AAS, caring for ConceptDescriptions " +
                                "and supplemental files."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0 || buttonNdx == 1 || buttonNdx == 2)
                            {
                                var rve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                                    Aas.Stringification.ToString(Aas.KeyTypes.AssetAdministrationShell)) as VisualElementAdminShell;

                                if (rve != null)
                                {
                                    var copyRecursively = buttonNdx == 1 || buttonNdx == 2;
                                    var createNewIds = env == rve.theEnv;
                                    var copySupplFiles = buttonNdx == 2;

                                    var potentialSupplFilesToCopy = new Dictionary<string, string>();
                                    Aas.AssetAdministrationShell destAAS = null;

                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is Aas.AssetAdministrationShell sourceAAS)
                                    {
                                        //
                                        // copy AAS
                                        //
                                        try
                                        {
                                            // make a copy of the AAS itself
                                            destAAS = (mdo as Aas.AssetAdministrationShell).Copy();
                                            if (createNewIds)
                                            {
                                                destAAS.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                                        Options.Curr.TemplateIdAas);

                                                if (destAAS.AssetInformation != null)
                                                {
                                                    destAAS.AssetInformation.GlobalAssetId = AdminShellUtil.GenerateIdAccordingTemplate(
                                                                Options.Curr.TemplateIdAsset);
                                                }
                                            }

                                            env.Add(destAAS);
                                            this.AddDiaryEntry(destAAS, new DiaryEntryStructChange(
                                                StructuralChangeReason.Create));

                                            // clear, copy Submodels?
                                            if (copyRecursively)
                                            {
                                                foreach (var smr in sourceAAS.AllSubmodels())
                                                {
                                                    // need access to source submodel
                                                    var srcSub = rve.theEnv.FindSubmodel(smr);
                                                    if (srcSub == null)
                                                        continue;

                                                    // get hold of suppl file infos?
                                                    if (srcSub.SubmodelElements != null)
                                                        foreach (var f in
                                                                srcSub.SubmodelElements.FindDeep<Aas.File>())
                                                        {
                                                            if (f != null && f.Value != null &&
                                                                    f.Value.StartsWith("/") &&
                                                                    !potentialSupplFilesToCopy
                                                                    .ContainsKey(f.Value.ToLower().Trim()))
                                                                potentialSupplFilesToCopy[
                                                                    f.Value.ToLower().Trim()] =
                                                                        f.Value.ToLower().Trim();
                                                        }

                                                    // complicated new ids?
                                                    if (!createNewIds)
                                                    {
                                                        // straightforward between environments
                                                        var destSMR = env.CopySubmodelRefAndCD(
                                                            rve.theEnv, smr, copySubmodel: true, copyCD: true,
                                                            shallowCopy: false);
                                                        if (destSMR != null)
                                                        {
                                                            destAAS.Add(destSMR);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // in the same environment?
                                                        // means: we have to generate a new submodel ref 
                                                        // by using template mechanism
                                                        var tid = Options.Curr.TemplateIdSubmodelInstance;
                                                        if (srcSub.Kind != null && srcSub.Kind == Aas.ModellingKind.Template)
                                                            tid = Options.Curr.TemplateIdSubmodelTemplate;

                                                        // create Submodel as deep copy 
                                                        // with new id from scratch
                                                        var dstSub = srcSub.Copy();
                                                        dstSub.Id = AdminShellUtil.GenerateIdAccordingTemplate(tid);

                                                        // make a new ref
                                                        var dstRef = dstSub.GetModelReference().Copy();

                                                        // formally add this to active environment and AAS
                                                        env.Add(dstSub);
                                                        destAAS.Add(dstRef);

                                                        this.AddDiaryEntry(dstSub, new DiaryEntryStructChange(
                                                            StructuralChangeReason.Create));
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.Singleton.Error(ex, $"copying AAS");
                                        }

                                        //
                                        // Copy suppl files
                                        //
                                        if (copySupplFiles && rve.thePackage != null && packages.Main != rve.thePackage)
                                        {
                                            // copy conditions met
                                            foreach (var fn in potentialSupplFilesToCopy.Values)
                                            {
                                                try
                                                {
                                                    // copy ONLY if not existing in destination
                                                    // rationale: do not potential harm the source content, 
                                                    // even when voiding destination integrity
                                                    if (rve.thePackage.IsLocalFile(fn)
                                                        && !packages.Main.IsLocalFile(fn))
                                                    {
                                                        var tmpFile =
                                                            rve.thePackage.MakePackageFileAvailableAsTempFile(fn);
                                                        var targetDir = System.IO.Path.GetDirectoryName(fn);

                                                        // target dir must not contain backslashes (!)
                                                        var targetFn = System.IO.Path.GetFileName(fn);
                                                        targetDir = targetDir.Replace("\\", "/");

                                                        // add
                                                        packages.Main.AddSupplementaryFileToStore(
                                                            tmpFile, targetDir, targetFn, false);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log.Singleton.Error(
                                                        ex, $"copying supplemental file {fn}");
                                                }
                                            }
                                        }

                                        //
                                        // Done
                                        //
                                        return new AnyUiLambdaActionRedrawAllElements(
                                            nextFocus: destAAS, isExpanded: true);
                                    }
                                }
                            }

                            return new AnyUiLambdaActionNone();
                        });
                }

                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.Shells
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllSubmodels
                    || ve.theItemType == VisualElementEnvironmentItem.ItemType.AllConceptDescriptions)
                {
                    // Cut, copy, paste within list of Assets
                    this.DispPlainListOfIdentifiablePasteHelper<Aas.IIdentifiable>(
                        stack, repo, this.theCopyPaste,
                        label: "Buffer:",
                        lambdaPasteInto: (cpi, del) =>
                        {
                            // access
                            if (cpi is CopyPasteItemIdentifiable cpiid)
                            {
                                // some pre-conditions not met?
                                if (cpiid?.entity == null || (del && cpiid?.parentContainer == null))
                                    return null;

                                // divert
                                object res = null;
                                if (cpiid.entity is Aas.AssetAdministrationShell itaas)
                                {
                                    // new 
                                    var aas = itaas.Copy();
                                    env.Add(aas);
                                    this.AddDiaryEntry(aas, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = aas;

                                    // delete
                                    if (del && cpiid.parentContainer is List<Aas.AssetAdministrationShell> aasold
                                        && aasold.Contains(itaas))
                                    {
                                        aasold.Remove(itaas);
                                        this.AddDiaryEntry(itaas,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }
                                else
                                if (cpiid.entity is Aas.ConceptDescription itcd)
                                {
                                    // new 
                                    var cd = itcd.Copy();
                                    env.ConceptDescriptions ??= new List<IConceptDescription>();
                                    env.ConceptDescriptions.Add(cd);
                                    this.AddDiaryEntry(cd, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = cd;

                                    // delete
                                    if (del && cpiid.parentContainer is List<Aas.ConceptDescription> cdold
                                        && cdold.Contains(itcd))
                                    {
                                        cdold.Remove(itcd);
                                        this.AddDiaryEntry(itcd,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }

                                // ok
                                return res;
                            }

                            if (cpi is CopyPasteItemSubmodel cpism)
                            {
                                // some pre-conditions not met?
                                if (cpism?.sm == null || (del && cpism?.parentContainer == null))
                                    return null;

                                // divert
                                object res = null;
                                if (cpism.sm is Aas.Submodel itsm)
                                {
                                    // new 
                                    var asset = itsm.Copy();
                                    env.Submodels ??= new List<ISubmodel>();
                                    env.Submodels.Add(itsm);
                                    this.AddDiaryEntry(itsm, new DiaryEntryStructChange(
                                        StructuralChangeReason.Create));
                                    res = asset;

                                    // delete
                                    if (del && cpism.parentContainer is List<Aas.Submodel> smold
                                        && smold.Contains(itsm))
                                    {
                                        smold.Remove(itsm);
                                        this.AddDiaryEntry(itsm,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }

                                // ok
                                return res;
                            }

                            // nok
                            return null;
                        });
                }

                //
                // Concept Descriptions
                //

                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.AllConceptDescriptions)
                {
                    //
                    // Copy / import
                    //

                    this.AddGroup(stack, "Import of ConceptDescriptions", this.levelColors.MainSection);

                    // Copy
                    this.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                    this.AddActionPanel(
                        stack, "Copy from existing ConceptDescription:",
                        repo: repo,
                        superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("copy-single", "Copy single",
                                "Copy single selected entity from another AAS."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var rve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                                    "ConceptDescription") as VisualElementConceptDescription;
                                if (rve != null)
                                {
                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is Aas.ConceptDescription)
                                    {
                                        var clone = (mdo as Aas.ConceptDescription).Copy();
                                        this.MakeNewIdentifiableUnique(clone);
                                        env.Add(clone);
                                        this.AddDiaryEntry(clone,
                                            new DiaryEntryStructChange(StructuralChangeReason.Create));
                                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: clone);
                                    }
                                }
                            }

                            return new AnyUiLambdaActionNone();
                        });

                    //
                    // Dynamic rendering
                    //

                    this.AddGroup(stack, "Dynamic rendering of ConceptDescriptions", this.levelColors.MainSection);

                    var g1 = this.AddSubGrid(stack, "Dynamic order:", 1, 2, new[] { "#", "#" },
                        paddingCaption: new AnyUiThickness(5, 0, 0, 0),
                        minWidthFirstCol: GetWidth(FirstColumnWidth.Standard));
                    AnyUiComboBox cb1 = null;
                    cb1 = AnyUiUIElement.RegisterControl(
                        this.AddSmallComboBoxTo(g1, 0, 0,
                            margin: new AnyUiThickness(2, 2, 2, 2), padding: new AnyUiThickness(5, 0, 5, 0),
                            minWidth: 250,
                            items: new[] {
                            "List index", "idShort", "Identification", "By AasSubmodel",
                            "By SubmodelElements", "Structured"
                        }),
                        (o) =>
                        {
                            // resharper disable AccessToModifiedClosure
                            if (cb1?.SelectedIndex.HasValue == true)
                            {
                                ve.CdSortOrder = (VisualElementEnvironmentItem.ConceptDescSortOrder)
                                    cb1.SelectedIndex.Value;
                            }
                            else
                            {
                                Log.Singleton.Error("ComboxBox Dynamic rendering of entities has no value");
                            }
                            // resharper enable AccessToModifiedClosure
                            return new AnyUiLambdaActionNone();
                        },
                        takeOverLambda: new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: env?.ConceptDescriptions));

                    // set currently selected value
                    if (cb1 != null)
                        cb1.SelectedIndex = (int)ve.CdSortOrder;

                    //
                    // Static order 
                    //

                    this.AddGroup(stack, "Static order of ConceptDescriptions", this.levelColors.MainSection);

                    this.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return true;  },
                            "The sort operation permanently changes the order of ConceptDescriptions in the " +
                            "environment. It cannot be reverted!",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                    var g2 = this.AddSubGrid(stack, "Entities:", 1, 1, new[] { "#" },
                        paddingCaption: new AnyUiThickness(5, 0, 0, 0),
                        minWidthFirstCol: GetWidth(FirstColumnWidth.Standard));
                    AnyUiUIElement.RegisterControl(
                        this.AddSmallButtonTo(g2, 0, 0, content: "Sort according above order",
                            margin: new AnyUiThickness(2, 2, 2, 2), padding: new AnyUiThickness(5, 0, 5, 0)),
                        (o) =>
                        {
                            if (env.ConceptDescriptionCount() < 1)
                            {
                                Log.Singleton.Error("No ConceptDescriptions found for sorting. Aborting!");
                                return new AnyUiLambdaActionNone();
                            }

                            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                               "Perform sort operation? This operation can not be reverted!",
                               "ConceptDescriptions",
                               AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            {
                                var success = false;
                                if (ve.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.IdShort)
                                {
                                    var x = env.ConceptDescriptions.ToList();
                                    x.Sort(new ComparerIdShort());
                                    env.ConceptDescriptions = x;
                                    success = true;
                                }
                                if (ve.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.Id)
                                {
                                    var x = env.ConceptDescriptions.ToList();
                                    x.Sort(new ComparerIdentification());
                                    env.ConceptDescriptions = x;
                                    success = true;
                                }
                                if (ve.CdSortOrder == VisualElementEnvironmentItem.ConceptDescSortOrder.BySubmodel)
                                {
                                    var cmp = env.CreateIndexedComparerCdsForSmUsage();
                                    var x = env.ConceptDescriptions.ToList();
                                    x.Sort(cmp);
                                    env.ConceptDescriptions = x;
                                    success = true;
                                }

                                if (success)
                                {
                                    ve.CdSortOrder = VisualElementEnvironmentItem.ConceptDescSortOrder.None;
                                    return new AnyUiLambdaActionRedrawAllElements(nextFocus: env?.ConceptDescriptions);
                                }
                                else
                                    this.context.MessageBoxFlyoutShow(
                                       "Cannot apply selected sort order!",
                                       "ConceptDescriptions",
                                       AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                            }

                            return new AnyUiLambdaActionNone();
                        });

                    //
                    // various "repairs" of CDs
                    //

                    this.AddGroup(stack, "Maintenance of ConceptDescriptions (CDs)", this.levelColors.MainSection);

                    this.AddActionPanel(
                        stack, "Fix:",
                        repo: repo,
                        superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("fix-data-specs", "Fix data specs wrt. content",
                                "Auto-detect content of data specification and set References accordingly."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                            {
                                if (AnyUi.AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "Fix data specification References according known types of " +
                                    "data specification content? " +
                                    "This operation cannot be reverted!",
                                    "ConceptDescriptions",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                    return new AnyUiLambdaActionNone();

                                // use the same order as displayed
                                Log.Singleton.Info("ConceptDescriptions: Fix data specification References " +
                                    "according known types.");

                                if (env?.ConceptDescriptions == null)
                                {
                                    Log.Singleton.Info(".. no ConceptDescription!");
                                    return new AnyUiLambdaActionNone();
                                }

                                foreach (var cd in env.AllConceptDescriptions())
                                {
                                    var change = false;
                                    if (cd.EmbeddedDataSpecifications != null)
                                        foreach (var esd in cd.EmbeddedDataSpecifications)
                                            change = change || ExtendEmbeddedDataSpecification
                                                .FixReferenceWrtContent(esd);

                                    if (change)
                                        Log.Singleton.Info(".. change: " + cd.ToCaptionInfo()?.Item1);
                                }

                                return new AnyUiLambdaActionRedrawAllElements(
                                    nextFocus: env?.ConceptDescriptions);
                            }

                            return new AnyUiLambdaActionNone();
                        });
                }
            }
            else if (ve.theItemType == VisualElementEnvironmentItem.ItemType.SupplFiles && packages.MainStorable)
            {
                // Files

                this.AddGroup(stack, "Supplemental file to add:", this.levelColors.SubSection);

                var g = this.AddSmallGrid(5, 3, new[] { "#", "*", "#" });
                this.AddSmallLabelTo(g, 0, 0, padding: new AnyUiThickness(2, 0, 0, 0), content: "Source path: ");
                AnyUiUIElement.RegisterControl(
                    this.AddSmallTextBoxTo(g, 0, 1, margin: new AnyUiThickness(2, 2, 2, 2), text: PackageSourcePath),
                    (o) =>
                    {
                        if (o is string)
                            PackageSourcePath = o as string;
                        return new AnyUiLambdaActionNone();
                    });
                AnyUiUIElement.RegisterControl(
                    this.AddSmallButtonTo(
                        g, 0, 2, margin: new AnyUiThickness(2, 2, 2, 2), padding: new AnyUiThickness(5, 0, 5, 0),
                        content: "Select"),
                        (o) =>
                        {
                            var uc = new AnyUiDialogueDataOpenFile(
                                caption: "Open supplemental file",
                                message: "Select a supplementary file to add..");
                            this.context?.StartFlyoverModal(uc);
                            if (uc.Result && uc.TargetFileName != null)
                            {
                                PackageSourcePath = uc.TargetFileName;
                                PackageTargetFn = System.IO.Path.GetFileName(uc.TargetFileName);
                                PackageTargetFn = PackageTargetFn.Replace(" ", "_");
                            }
                            return new AnyUiLambdaActionRedrawEntity();
                        });
                this.AddSmallLabelTo(g, 1, 0, padding: new AnyUiThickness(2, 0, 0, 0), content: "Target filename: ");
                AnyUiUIElement.RegisterControl(
                    this.AddSmallTextBoxTo(g, 1, 1, margin: new AnyUiThickness(2, 2, 2, 2), text: PackageTargetFn),
                    (o) =>
                    {
                        if (o is string)
                            PackageTargetFn = o as string;
                        return new AnyUiLambdaActionNone();
                    });
                this.AddSmallLabelTo(g, 2, 0, padding: new AnyUiThickness(2, 0, 0, 0), content: "Target path: ");
                AnyUiUIElement.RegisterControl(
                    this.AddSmallTextBoxTo(g, 2, 1, margin: new AnyUiThickness(2, 2, 2, 2), text: PackageTargetDir),
                    (o) =>
                    {
                        if (o is string)
                            PackageTargetDir = o as string;
                        return new AnyUiLambdaActionNone();
                    });
                AnyUiUIElement.RegisterControl(
                    this.AddSmallCheckBoxTo(g, 3, 1, margin: new AnyUiThickness(2, 2, 2, 2),
                    content: "Embed as thumbnail (only one file per package!)", isChecked: PackageEmbedAsThumbnail),
                    (o) =>
                    {
                        if (o is bool)
                            PackageEmbedAsThumbnail = (bool)o;
                        return new AnyUiLambdaActionNone();
                    });
                AnyUiUIElement.RegisterControl(
                    this.AddSmallButtonTo(g, 4, 1, margin: new AnyUiThickness(2, 2, 2, 2),
                    padding: new AnyUiThickness(5, 0, 5, 0), content: "Add file to package"),
                    (o) =>
                    {
                        try
                        {
                            var ptd = PackageTargetDir;
                            if (PackageEmbedAsThumbnail)
                                ptd = "/";
                            packages.Main.AddSupplementaryFileToStore(
                                PackageSourcePath, ptd, PackageTargetFn, PackageEmbedAsThumbnail);
                            Log.Singleton.Info(StoredPrint.Color.Blue,
                                "Added {0} to pending package items. A save-operation is required.",
                                PackageSourcePath);
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(ex, "Adding file to package");
                        }
                        PackageSourcePath = "";
                        PackageTargetFn = "";
                        return new AnyUiLambdaActionRedrawAllElements(
                            nextFocus: VisualElementEnvironmentItem.GiveAliasDataObject(
                                VisualElementEnvironmentItem.ItemType.Package));
                    });
                stack.Children.Add(g);
            }
            else if (ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext
                || ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev)
            {
                // check all pre-requisites
                if (!(context is AnyUiContextPlusDialogs plusDialogs
                     && ve.thePackage is AdminShellPackageDynamicFetchEnv dynPack
                     && dynPack.GetContext() is PackageContainerHttpRepoSubsetFetchContext fetchContext
                     && fetchContext.Record != null
                     && mainWindow != null))
                {
                    AddHintBubble(stack, hintMode, new HintCheck(
                        () => true,
                            "Not enough data to provide dynamic fetch operations.",
                            severityLevel: HintCheck.Severity.High));
                    return;
                }

                // at the beginning already
                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev
                    && fetchContext.Record.PageOffset == 0)
                {
                    AddHintBubble(stack, hintMode, new HintCheck(
                        () => true,
                            "No further fetch operation available " +
                            "(at the beginning of the selected subset of elements?).",
                            severityLevel: HintCheck.Severity.Notice));
                    return;
                }

                // at the end?
                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext
                    && fetchContext.Cursor?.HasContent() != true)
                {
                    AddHintBubble(stack, hintMode, new HintCheck(
                        () => true,
                            "No further fetch operation available " +
                            "(at the end of the selected subset of elements?).",
                            severityLevel: HintCheck.Severity.Notice));
                    return;
                }

                // go ahead
                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev)
                    AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => true,
                                "The entities in this structure were fetched dynamically from " +
                                "endpoints such as registries and repositories. This fetch could " +
                                "be modified to load elements prior to the displayed set of elements. ",
                                severityLevel: HintCheck.Severity.Notice),
                        new HintCheck(
                            () => true,                                
                                "Note: This operation causes many elements to be reloaded and skipped, " +
                                "therefore might be a long-lasting operation.",
                                severityLevel: HintCheck.Severity.Notice) 
                    });

                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext)
                    AddHintBubble(stack, hintMode, new HintCheck(
                            () => true,
                                "The entities in this structure were fetched dynamically from " +
                                "endpoints such as registries and repositories. This fetch could " +
                                "be advanced to the next set of elements.",
                                severityLevel: HintCheck.Severity.Notice));

                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchPrev)
                    AddActionPanel(stack, "Actions:",
                        repo: repo,
                        superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("fetch-prev", "Fetch prev",
                                "Fetch the previous set of elements."),
                        ticketActionAsync: async (buttonNdx, ticket) =>
                        {
                            var res = await ExecuteUiForFetchOfElements(
                                packages, context, ticket, mainWindow, fetchContext,
                                preserveEditMode: true,
                                doEditNewRecord: false,
                                doCheckTainted: true,
                                doFetchGoPrev: true,
                                doFetchExec: true);

                            // success will trigger redraw independently, therefore always return none
                            return new AnyUiLambdaActionNone();
                        });


                if (ve.theItemType == VisualElementEnvironmentItem.ItemType.FetchNext)
                    AddActionPanel(stack, "Actions:",
                        repo: repo,
                        superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("fetch-next", "Fetch next",
                                "Fetch the next set of elements."),
                        ticketActionAsync: async (buttonNdx, ticket) =>
                        {
                            var res = await ExecuteUiForFetchOfElements(
                                packages, context, ticket, mainWindow, fetchContext,
                                preserveEditMode: true,
                                doEditNewRecord: false,
                                doCheckTainted: true,
                                doFetchGoNext: true,
                                doFetchExec: true);

                            // success will trigger redraw independently, therefore always return none
                            return new AnyUiLambdaActionNone();
                        });
            }
            else
            {
                // Default
                this.AddHintBubble(
                    stack,
                    hintMode,
                    new[] {
                        new HintCheck(
                            () => { return env.AssetAdministrationShellCount() < 1; },
                                "There are no AssetAdministrationShell entities in the environment. " +
                                (env.AssetAdministrationShells == null ? "List is null! " : "List is empty! ") +
                                "Select the 'Administration Shells' item on the middle panel and " +
                                "select 'Add AAS' to add a new entity."),
                        new HintCheck(
                            () => { return env.ConceptDescriptionCount() < 1; },
                                "There are no embedded ConceptDescriptions in the environment. " +
                                (env.ConceptDescriptions == null ? "List is null! " : "List is empty! ") +
                                "It is a good practice to have those. Select or add an AssetAdministrationShell, " +
                                "Submodel and SubmodelElement and add a ConceptDescription.",
                            severityLevel: HintCheck.Severity.Notice),
                    });

                // overview information

                var g = this.AddSmallGrid(
                            6, 1, new[] { "*" }, margin: new AnyUiThickness(5, 5, 0, 0));
                this.AddSmallLabelTo(
                    g, 0, 0, content: "This structure holds the main entities of Administration shells.");
                this.AddSmallLabelTo(
                    g, 1, 0, content: String.Format("#AssetAdministrationShells: {0}.", env.AssetAdministrationShellCount()),
                    margin: new AnyUiThickness(0, 5, 0, 0));
                this.AddSmallLabelTo(g, 3, 0, content: String.Format("#Submodels: {0}.", env.SubmodelCount()));
                this.AddSmallLabelTo(
                    g, 4, 0, content: String.Format("#ConceptDescriptions: {0}.", env.ConceptDescriptionCount()));
                stack.Children.Add(g);

                // dynamic fetched
                if (ve.thePackage is AdminShellPackageDynamicFetchEnv dynPack
                    && mainWindow != null)
                {
                    AddHintBubble(stack, hintMode, new HintCheck(
                        () => true,
                            "The entities in this structure were fetched dynamically from " +
                            "endpoints such as registries and repositories.", 
                            severityLevel: HintCheck.Severity.Notice));

                    this.AddGroup(stack, "Dynamic fetch environment", this.levelColors.SubSection);

                    // more infos?
                    if (dynPack.GetContext() is PackageContainerHttpRepoSubsetFetchContext fetchContext
                        && fetchContext.Record != null)
                    {
                        var record = fetchContext.Record;

                        AddKeyValue(stack, key: "BaseType", repo: null,
                            value: record.GetBaseTypStr().ToUpper());

                        AddKeyValue(stack, key: "BaseAddress", repo: null,
                            value: record.BaseAddress);

                        AddKeyValue(stack, key: "Operation", repo: null,
                            value: record.GetFetchOperationStr());

                        if (record.GetAllAas || record.GetAllSubmodel || record.GetAllCD)
                        {
                            AddKeyValue(stack, key: "Page limit", repo: null,
                                value: "" + record.PageLimit);

                            AddKeyValue(stack, key: "Page skip", repo: null,
                                value: "" + record.PageSkip);

                            AddKeyValue(stack, key: "Page offset", repo: null,
                                value: "" + record.PageOffset + " (counted)");
                        }
                    }

                    AddActionPanel(stack, "Actions:",
                        repo: repo,
                        superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("refine-fetch", "Refine fetch ..",
                                "Refine the fetch parameters which led to this dynamic set of elements."),
                        ticketActionAsync: async (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0
                                && context is AnyUiContextPlusDialogs plusDialogs)
                            {
                                // default, but better: used record
                                var record = (((ve.thePackage as AdminShellPackageDynamicFetchEnv)?.GetContext()
                                              as PackageContainerHttpRepoSubsetFetchContext)?.Record
                                              as ConnectExtendedRecord)
                                             ?? new PackageContainerHttpRepoSubset.ConnectExtendedRecord();

                                // ok, prepare new fetch context (no continuiation)
                                var fetchContext = new PackageContainerHttpRepoSubsetFetchContext()
                                {
                                    Record = record
                                };

                                // refer to (static) function
                                var res = await ExecuteUiForFetchOfElements(
                                    packages, context, ticket, mainWindow, fetchContext,
                                    preserveEditMode: true,
                                    doEditNewRecord: true,
                                    doCheckTainted: true,
                                    doFetchGoNext: false,
                                    doFetchExec: true);

                                // success will trigger redraw independently, therefore always return none
                                return new AnyUiLambdaActionNone();

                                //var location = PackageContainerHttpRepoSubset.BuildLocationFrom(record);
                                //if (location == null)
                                //{
                                //    MainWindowLogic.LogErrorToTicketStatic(ticket, 
                                //        new InvalidDataException(),
                                //        "Error building location from query selection. Aborting.");
                                //    return new AnyUiLambdaActionNone();
                                //}

                                //// more details into container options
                                //var containerOptions = new PackageContainerHttpRepoSubset.
                                //    PackageContainerHttpRepoSubsetOptions(PackageContainerOptionsBase.CreateDefault(Options.Curr),
                                //    record);

                                //// load
                                //Log.Singleton.Info($"For refining extended connect, loading " +
                                //    $"from {location} into container");

                                //var container = await PackageContainerFactory.GuessAndCreateForAsync(
                                //    packages,
                                //    location,
                                //    location,
                                //    overrideLoadResident: true,
                                //    containerOptions: containerOptions,
                                //    runtimeOptions: packages.CentralRuntimeOptions);

                                //if (container == null)
                                //    Log.Singleton.Error($"Failed to load from {location}");
                                //else
                                //    mainWindow.UiLoadPackageWithNew(packages.MainItem,
                                //        takeOverContainer: container, onlyAuxiliary: false, indexItems: true,
                                //        storeFnToLRU: location,
                                //        nextEditMode: editMode);

                                //Log.Singleton.Info($"Successfully loaded {location}");
                            }
                            return new AnyUiLambdaActionNone();
                        });
                }
            }
        }


        //
        //
        // --- Supplementary file
        //
        //

        public void DisplayOrEditAasEntitySupplementaryFile(
            PackageCentral.PackageCentral packages,
            VisualElementSupplementalFile entity,
            AdminShellPackageSupplementaryFile psf, bool editMode,
            AnyUiStackPanel stack,
            AasxMenu superMenu = null)
        {
            //
            // Package
            //
            this.AddGroup(stack, "Supplemental file for package of AASX", this.levelColors.MainSection);

            if (editMode && packages.MainStorable && psf != null)
            {
                AddActionPanel(stack, "Action",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("file-delete", "Delete",
                            "Deletes the supplemental file from the respective AAS environment."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                    "Delete selected entity? This operation can not be reverted!", "AAS-ENV",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            {
                                // try remember where we are
                                var sibling = entity.FindSibling()?.GetDereferencedMainDataObject();

                                // delete
                                try
                                {
                                    packages.Main.DeleteSupplementaryFile(psf);
                                    Log.Singleton.Info(
                                    "Added {0} to pending package items to be deleted. " +
                                        "A save-operation might be required.", PackageSourcePath);
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(ex, "Deleting file in package");
                                }

                                // try to re-focus to a sibling
                                if (sibling != null)
                                {
                                    // stay around
                                    return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: sibling);
                                }
                                else
                                {
                                    // jump to root
                                    return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: VisualElementEnvironmentItem.GiveAliasDataObject(
                                            VisualElementEnvironmentItem.ItemType.Package));
                                }
                            }

                        return new AnyUiLambdaActionNone();
                    });
            }
        }

        //
        //
        // --- Dynamic fetch of elements
        //
        //

        /// <summary>
        /// Fetch helper. Different <c>do...</c> flags are to be set!
        /// </summary>
        /// <returns><c>True</c>, when a new fetch has been executed successfully</returns>
        public static async Task<bool> ExecuteUiForFetchOfElements(
            PackageCentral.PackageCentral packages,
            AnyUiContextBase displayContext,
            AasxMenuActionTicket ticket,
            IMainWindow mainWindow,
            PackageContainerHttpRepoSubsetFetchContext fetchContext,
            HttpHeaderData additionalHeaderData = null,
            bool preserveEditMode = true,
            bool doCheckTainted = false,
            bool doEditNewRecord = false,
            bool doFetchGoPrev = false,
            bool doFetchGoNext = false,
            bool doFakeGoNext = false,
            bool doFetchExec = false)
        {
            await Task.Yield();

            // fetchContext is required!!
            if (fetchContext == null)
                return false;

            if (doCheckTainted)
            {
                // check if something is tainted
                if (mainWindow?.CheckIsAnyTaintedIdentifiableInMain() == true)
                {
                    if (AnyUiMessageBoxResult.Yes != displayContext.MessageBoxFlyoutShow(
                        "There are unsafed data changes in Identifiables. A fetch of elements " +
                        "might result in data loss.",
                        "Proceed with fetch?",
                        AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                        return false;
                }
            }

            if (doEditNewRecord)
            {
                var record = fetchContext.Record ?? new PackageContainerHttpRepoSubset.ConnectExtendedRecord();

                var uiRes = await PackageContainerHttpRepoSubset.PerformConnectExtendedDialogue(
                    ticket, displayContext,
                    "Connect AAS repositories and registries",
                    record);

                if (!uiRes)
                    return false;

                // modify fetch context to be "fresh"
                fetchContext.Cursor = null;
                fetchContext.Record = record;
            }

            if (doFetchGoPrev)
            {
                // provide no cursor, therefore fetch from very beginning, skip elements
                fetchContext.Cursor = null;
                fetchContext.Record.PageSkip = Math.Max(0, fetchContext.Record.PageOffset - fetchContext.Record.PageLimit);
                fetchContext.Record.PageOffset -= fetchContext.Record.PageLimit;
                fetchContext.Record.PageOffset = Math.Max(0, fetchContext.Record.PageOffset);
            }

            if (doFetchGoNext)
            {
                // modify (!) record data to do no skip anymore, using cursor data
                fetchContext.Record.PageOffset += (fetchContext.Record.PageLimit + fetchContext.Record.PageSkip);
                fetchContext.Record.PageOffset = Math.Max(0, fetchContext.Record.PageOffset);
                fetchContext.Record.PageSkip = 0;
            }

            if (doFakeGoNext)
            {
                // provide no cursor, therefore fetch from very beginning, skip elements
                fetchContext.Cursor = null;
                fetchContext.Record.PageSkip = Math.Max(0, fetchContext.Record.PageOffset + fetchContext.Record.PageLimit);
                fetchContext.Record.PageOffset += fetchContext.Record.PageLimit;
                fetchContext.Record.PageOffset = Math.Max(0, fetchContext.Record.PageOffset);
            }

            if (doFetchExec)
            {
                // build location
                var location = PackageContainerHttpRepoSubset.BuildLocationFrom(
                    fetchContext.Record, fetchContext.Cursor);

                if (location == null)
                {
                    MainWindowLogic.LogErrorToTicketStatic(ticket,
                        new InvalidDataException(),
                        "Error building location from fetch selection. Aborting.");
                    return false;
                }

                // more details into container options
                var containerOptions = new PackageContainerHttpRepoSubset.
                    PackageContainerHttpRepoSubsetOptions(PackageContainerOptionsBase.CreateDefault(Options.Curr),
                    fetchContext.Record);

                containerOptions.BaseUris = location.BaseUris;

                // load
                Log.Singleton.Info($"For refining extended connect, loading " +
                    $"from {location} into container");

                packages.CentralRuntimeOptions.CancellationTokenSource = new System.Threading.CancellationTokenSource();

                var runtimeOptions = packages.CentralRuntimeOptions;
                runtimeOptions.HttpHeaderData = fetchContext.Record.HeaderData;
                if (additionalHeaderData != null)
                    runtimeOptions.HttpHeaderData = HttpHeaderData.Merge(runtimeOptions.HttpHeaderData, additionalHeaderData);

                var container = await PackageContainerFactory.GuessAndCreateForAsync(
                    packages,
                    location.Location.ToString(),
                    location.Location.ToString(),
                    overrideLoadResident: true,
                    autoAuthenticate: fetchContext.Record?.AutoAuthenticate == true,
                    containerOptions: containerOptions,
                    runtimeOptions: runtimeOptions);

                if (container == null)
                {
                    Log.Singleton.Error($"Failed to load from {location.Location}.");
                    return false;
                }

                // display
                mainWindow.UiLoadPackageWithNew(packages.MainItem,
                    takeOverContainer: container, onlyAuxiliary: false, indexItems: true,
                    storeFnToLRU: location.Location.ToString(),
                    preserveEditMode: preserveEditMode,
                    autoFocusFirstRelevant: true);

                Log.Singleton.Info($"Successfully processed retrieval attempt of {location.Location}");

                // okay
                return true;
            }
            
            return false;
        }

        //
        //
        // --- AAS
        //
        //

        public void DisplayOrEditAasEntityAas(
            PackageCentral.PackageCentral packages, 
            AdminShellPackageEnvBase package,
            Aas.IEnvironment env,
            Aas.IAssetAdministrationShell aas,
            bool editMode, AnyUiStackPanel stack, bool hintMode = false,
            AasxMenu superMenu = null)
        {
            this.AddGroup(stack, "AssetAdministrationShell (according IEC63278)", this.levelColors.MainSection);
            if (aas == null)
                return;

            // check, if Submodel is sitting in Repo
            var sideInfo = OnDemandListIdentifiable<Aas.IAssetAdministrationShell>
                    .FindSideInfoInListOfIdentifiables(
                        env.AssetAdministrationShells, aas.GetReference());

            // Entities
            if (editMode)
            {
                //
                // New (MIHO, 2024-06-10): allow even if aas.Submodels is null
                //

                // main group
                this.AddGroup(stack, "Editing of entities", this.levelColors.SubSection);

                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = aas,
                    ParentElem = env
                };

                // Up/ down/ del
                this.EntityListUpDownDeleteHelper<Aas.IAssetAdministrationShell>(
                    stack, repo, 
                    env.AssetAdministrationShells, (lst) => { env.AssetAdministrationShells = lst; },
                    aas, env, "AAS:",
                    superMenu: superMenu,
                    extraMenu: new AasxMenu()
                        .AddAction("finalize-aas", "Finalize AAS",
                            "Check and auto-correct AAS to be uploaded into Repository."),
                    moveDoesNotModify: true,
                    sendUpdateEvent: evTemplate,
                    postActionHookAsync: async (actionName, ticket) =>
                    {
                        await Task.Yield();

                        if (actionName == "aas-elem-delete")
                        {
                            // be a bit informy
                            Log.Singleton.Info($"Deleted AAS {aas.IdShort} from local Environment.");
                            
                            // simply prepare some Keys!
                            var idfKeys = (new Aas.IKey[] {
                                new Aas.Key(KeyTypes.AssetAdministrationShell, "" + aas.Id) }).ToList();
                            foreach (var smr in aas.Submodels.ForEachSafe())
                                if (smr?.IsValid() == true)
                                    idfKeys.Add(new Aas.Key(KeyTypes.Submodel, smr.Keys.First().Value));

                            // check if to delete Submodels in local Environment?
                            var delInLocal = AnyUiMessageBoxResult.Yes == await this.context.MessageBoxFlyoutShowAsync(
                                    "Delete Submodels in local Environment as well? " +
                                    "This operation can not be reverted!",
                                    "Delete Submodels in local Environment?",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning);
                            if (delInLocal)
                            {
                                foreach (var ik in idfKeys)
                                    if (ik.Type == KeyTypes.Submodel)
                                    {
                                        var sm = env?.FindSubmodelById(ik.Value);
                                        if (sm != null)
                                        {
                                            env.Remove(sm);
                                            Log.Singleton.Info($"Deleted Submodel {sm.IdShort} from local Environment.");
                                        }
                                    }
                            }

                            // delete in repo as well?
                            var delInRepo = sideInfo?.Id?.HasContent() == true
                                    && sideInfo.StubLevel >= AasIdentifiableSideInfoLevel.IdOnly;

                            if (delInRepo)
                            {
                                if (AnyUiMessageBoxResult.Yes != await this.context.MessageBoxFlyoutShowAsync(
                                        "Delete AAS in Repository as well? " +
                                        "This operation can not be reverted!",
                                        "Delete in Repository?",
                                        AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                    delInRepo = false;
                            }

                            // delete in repo?
                            if (delInRepo)
                            {
                                // call function
                                // (only the side info in the _specific_ endpoint gives information, in which
                                //  repo the CDs could be deleted)
                                await PackageContainerHttpRepoSubset.AssistantDeleteIdfsInRepo(
                                    null, context,
                                    "Delete AAS and Submodels in Repository/ Registry",
                                    "AAS and Submodel",
                                    idfKeys,
                                    runtimeOptions: packages.CentralRuntimeOptions,
                                    presetRecord: new PackageContainerHttpRepoSubset.DeleteAssistantJobRecord()
                                    {
                                        // assume Repo ?!
                                        BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,

                                        // extract base address
                                        BaseAddress = "" + PackageContainerHttpRepoSubset.GetBaseUri(
                                            sideInfo?.DesignatedEndpoint?.AbsoluteUri)?.AbsoluteUri
                                    });
                            }

                            // manually redraw
                            this.appEventsProvider?.PushApplicationEvent(new AasxPluginResultEventRedrawAllElements());
                        }
                    },
                    lambdaExtraMenuAsync: async (buttonNdx) =>
                    {
                        await Task.Yield();
                        if (buttonNdx == 0)
                        {
                            // get a list
                            var idfs = env?.FindAllReferencedIdentifiablesForAas(aas);
                            if (idfs == null || idfs.Count() < 1)
                            {
                                Log.Singleton.Error("When finalizing AAS, no Identifiables could be found! Aborting!");
                            }
                            Log.Singleton.Info($"Finalize AAS {aas.IdShort}: Processing {idfs.Count()} Identifiables.");

                            if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                "This operation reworks the contents of the dependent Identifiables to be " +
                                "compliant to the AAS specification. Some data might get lost! " +
                                "Do you want to proceed?",
                                "Finalize Identifiables",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            // safe
                            try
                            {
                                var visitor = new AasxFixListVisitor();
                                int naas = 0, nsm = 0, ncd = 0;

                                foreach (var idf in idfs)
                                {
                                    // Not able to do a generic lambda, therefore multiply here ..
                                    if (idf is Aas.IAssetAdministrationShell aas)
                                    {
                                        var eiaas = env.AssetAdministrationShells as OnDemandListIdentifiable<Aas.IAssetAdministrationShell>;
                                        var ndx = eiaas?.TestIndexOf(aas);
                                        if (eiaas == null || ndx.Value < 0)
                                            continue;
                                        var newaas = (Aas.AssetAdministrationShell)visitor.Transform(aas);
                                        eiaas.Update(ndx.Value, newaas);
                                        naas++;
                                        idf.SetTainted(true);
                                    }

                                    if (idf is Aas.ISubmodel sm)
                                    {
                                        var eism = env.Submodels as OnDemandListIdentifiable<Aas.ISubmodel>;
                                        var ndx = eism?.TestIndexOf(sm);
                                        if (eism == null || ndx.Value < 0)
                                            continue;
                                        var newsm = (Aas.Submodel)visitor.Transform(sm);
                                        eism.Update(ndx.Value, newsm);
                                        nsm++;
                                        idf.SetTainted(true);
                                    }

                                    if (idf is Aas.IConceptDescription cd)
                                    {
                                        var eicd = env.ConceptDescriptions as OnDemandListIdentifiable<Aas.IConceptDescription>;
                                        var ndx = eicd?.TestIndexOf(cd);
                                        if (eicd == null || ndx.Value < 0)
                                            continue;
                                        var newcd = (Aas.ConceptDescription)visitor.Transform(cd);
                                        eicd.Update(ndx.Value, newcd);
                                        ncd++;
                                        idf.SetTainted(true);
                                    }
                                }

                                Log.Singleton.Info($"Finalized: {naas} AAS, {nsm} Submodels, {ncd} CDs.");
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(ex, $"when finalizing AAS {aas.IdShort}!");
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // Cut, copy, paste within list of AASes
                this.DispPlainIdentifiableCutCopyPasteHelper<Aas.IAssetAdministrationShell>(
                    stack, repo, this.theCopyPaste,
                    env.AssetAdministrationShells, aas, 
                    (o) => { return (o as Aas.AssetAdministrationShell).Copy(); },
                    label: "Buffer:",
                    checkPasteInfo: (cpb) => cpb?.Items?.AllOfElementType<CopyPasteItemSubmodel>() == true,
                    doPasteInto: (cpi, del) =>
                    {
                        // access
                        var item = cpi as CopyPasteItemSubmodel;
                        if (item?.smref == null)
                            return null;

                        // duplicate
                        foreach (var x in aas.AllSubmodels())
                            if (x?.Matches(item.smref, MatchMode.Identification) == true)
                                return null;

                        // add 
                        var newsmr = item.smref.Copy();
                        aas.Add(newsmr);

                        // special case: Submodel does not exist, as pasting was from external
                        if (item.sm != null)
                        {
                            var smtest = env.FindSubmodel(newsmr);
                            if (smtest == null)
                            {
                                env.Add(item.sm);
                                this.AddDiaryEntry(item.sm,
                                    new DiaryEntryStructChange(StructuralChangeReason.Create));
                            }
                        }

                        // delete
                        if (del && item.parentContainer is Aas.IAssetAdministrationShell aasold)
                            aasold.Remove(item.smref);

                        // ok
                        return newsmr;
                    });

                // If AAS.Submodels is null, give clear indication
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return aas.Submodels == null; },
                                "The list of Submodel references is set to null. Creation will be done " +
                                "using respective functionalities below.",
                            severityLevel: HintCheck.Severity.High,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return aas.SubmodelCount() < 1;  },
                            "You have no Submodels referenced by this Administration Shell. " +
                                "This is rather unusual, as the Submodels are the actual carriers of information. " +
                                "Most likely, you want to click 'Create new Submodel of kind Instance'. " +
                                "You might also consider to load another AASX as auxiliary AASX " +
                                "(see 'File' menu) to copy structures from.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                // now let create
                this.AddActionPanel(
                    stack, "SubmodelRef:",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("ref-existing", "Reference to existing Submodel",
                            "Links the SubmodelReference to an existing Submodel.")
                        .AddAction("create-template", "Create new Submodel of kind Template",
                            "Creates a new Submodel of kind Template and link to this SubmodelReference.")
                        .AddAction("create-instance", "Create new Submodel of kind Instance",
                            "Creates a new Submodel of kind Instance and link to this SubmodelReference."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            if (AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation creates a reference to an existing Submodel. " +
                                        "By this, two AAS will share exactly the same data records. " +
                                        "Changing one will cause the other AAS's information to change as well. " +
                                        "This operation is rather special. Do you want to proceed?",
                                    "Submodel sharing",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            // select existing Submodel
                            var ks = this.SmartSelectAasEntityKeys(packages,
                                        PackageCentral.PackageCentral.Selector.Main,
                                        "Submodel");
                            if (ks != null)
                            {
                                // create ref
                                var smr = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(ks));
                                aas.Submodels ??= new List<IReference>();
                                aas.Submodels.Add(smr);

                                // event for AAS
                                this.AddDiaryEntry(aas, new DiaryEntryStructChange());

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(
                                    nextFocus: smr, isExpanded: true);
                            }
                        }

                        if (buttonNdx == 1 || buttonNdx == 2)
                        {
                            // create new submodel
                            var submodel = new Aas.Submodel("");

                            // directly create identification, as we need it!
                            if (buttonNdx == 1)
                            {
                                submodel.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelTemplate);
                                submodel.Kind = Aas.ModellingKind.Template;
                            }
                            else
                            {
                                submodel.Id = AdminShellUtil.GenerateIdAccordingTemplate(
                                    Options.Curr.TemplateIdSubmodelInstance);
                                submodel.Kind = ModellingKind.Instance;
                            }
                                

                            // add
                            this.AddDiaryEntry(submodel,
                                    new DiaryEntryStructChange(StructuralChangeReason.Create));
                            env.Add(submodel);

                            // create ref
                            var smr = new Aas.Reference(Aas.ReferenceTypes.ModelReference, 
                                new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.Submodel, submodel.Id) });
                            aas.Add(smr);

                            // event for AAS
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange());

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smr, isExpanded: true);
                        }

                        return new AnyUiLambdaActionNone();
                    });

                    this.AddHintBubble(stack, hintMode, new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                    
                this.AddActionPanel(
                    stack, "Copy from existing Submodel:",
                    firstColumnWidth: FirstColumnWidth.Large,
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("copy-single", "Copy single",
                            "Copy selected Submodel without children from another AAS, " +
                            "caring for ConceptDescriptions.")
                        .AddAction("copy-recurse", "Copy recursively",
                            "Copy selected Submodel and children from another AAS, " +
                            "caring for ConceptDescriptions."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "SubmodelRef") as VisualElementSubmodelRef;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is Aas.Reference)
                                {
                                    // we have 2 different use cases: 
                                    // (1) copy between AAS ENVs, 
                                    // (2) copy in one AAS ENV!
                                    if (env != rve.theEnv)
                                    {
                                        // use case (1) copy between AAS ENVs
                                        var clone = env.CopySubmodelRefAndCD(
                                            rve.theEnv, mdo as Aas.Reference, copySubmodel: true,
                                            copyCD: true, shallowCopy: buttonNdx == 0);
                                        if (clone == null)
                                            return new AnyUiLambdaActionNone();
                                        aas.Add(clone);
                                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                                        return new AnyUiLambdaActionRedrawAllElements(
                                            nextFocus: clone, isExpanded: true);
                                    }
                                    else
                                    {
                                        // use case (2) copy in one AAS ENV!

                                        // need access to source submodel
                                        var srcSub = rve.theEnv.FindSubmodel(mdo as Aas.Reference);
                                        if (srcSub == null)
                                            return new AnyUiLambdaActionNone();

                                        // means: we have to generate a new submodel ref
                                        // by using template mechanism
                                        var tid = Options.Curr.TemplateIdSubmodelInstance;
                                        if (srcSub.Kind != null && srcSub.Kind == Aas.ModellingKind.Template)
                                            tid = Options.Curr.TemplateIdSubmodelTemplate;

                                        // create Submodel as deep copy 
                                        // with new id from scratch
                                        var dstSub = srcSub.Copy();
                                        dstSub.Id = AdminShellUtil.GenerateIdAccordingTemplate(tid);

                                        // make a new ref
                                        var dstRef = dstSub.GetModelReference().Copy();

                                        // formally add this to active environment 
                                        env.Add(dstSub);
                                        this.AddDiaryEntry(dstSub,
                                            new DiaryEntryStructChange(StructuralChangeReason.Create));

                                        // .. and AAS
                                        aas.Add(dstRef);
                                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                                        return new AnyUiLambdaActionRedrawAllElements(
                                            nextFocus: dstRef, isExpanded: true);
                                    }
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });   
                
                // on demand loading?
                if (package is AdminShellPackageDynamicFetchEnv dynPack)
                {
                    this.AddActionPanel(
                    stack, "Load missing stubs:",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("stub-load-submodels", "Submodels",
                            "Load missing Submodels only for this AAS.")
                        .AddAction("stub-load-concepts", "ConceptDescriptions",
                            "Load missing ConceptDescriptions only for this AAS.")
                        .AddAction("stub-load-thumbnail", "Thumbnail",
                            "Load missing Thumbnail only for this AAS."),
                    ticketActionAsync: async (buttonNdx, ticket) =>
                    {
                        if (buttonNdx >= 0 && buttonNdx <= 1)
                        {

                            List<LocatedReference> lrs = null;

                            if (buttonNdx == 0)
                                lrs = aas?.FindAllSubmodelReferences().ToList();

                            if (buttonNdx == 1)
                                lrs = env.FindAllSemanticIdsForAas(aas).ToList();

                            if (lrs != null)
                            {
                                var ids = lrs.Select((lr) => (lr?.Reference?.IsValid() == true) ? lr.Reference.Keys[0].Value : null).ToList();
                                var fetched = await dynPack.TryFetchSpecificIds(ids,
                                        useParallel: Options.Curr.MaxParallelReadOps > 1);
                                if (fetched)
                                    return new AnyUiLambdaActionRedrawAllElements(nextFocus: aas);
                            }
                        }

                        if (buttonNdx == 2)
                        {
                            var fetched = await dynPack.TryFetchThumbnail(aas);
                            if (fetched)
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: aas);
                        }

                        return new AnyUiLambdaActionNone();
                    });
                }
            }

            // info about sideInfo
            DisplayOrEditEntitySideInfo(env, stack, aas, sideInfo, "AAS", superMenu);

            // Referable
            this.DisplayOrEditEntityReferable(
                env, stack,
                parentContainer: null, referable: aas, indexPosition: 0,
                superMenu: superMenu);

            // Identifiable
            this.DisplayOrEditEntityIdentifiable(
                stack, package, env, aas,
                Options.Curr.TemplateIdAas,
                injectToId: new DispEditHelperModules.DispEditInjectAction(
                    new[] { "Rename" },
                    auxLambda: null,
                    auxLambdaAsync: async (i) =>
                    {
                        if (i == 0 && env != null)
                        {
                            var uc = new AnyUiDialogueDataTextBox(
                                "New ID:",
                                symbol: AnyUiMessageBoxImage.Question,
                                maxWidth: 1400,
                                text: aas.Id);
                            if (await this.context.StartFlyoverModalAsync(uc)
                                && uc.Text?.HasContent() == true)
                            {
                                var oldId = aas.Id;
                                var newId = uc.Text.Trim();
                                var res = false;

                                try
                                {
                                    // check, if Submodel is sitting in Repo
                                    var sideInfo = OnDemandListIdentifiable<Aas.IAssetAdministrationShell>
                                            .FindSideInfoInListOfIdentifiables(
                                                env.AssetAdministrationShells, aas.GetReference());
                                    if (sideInfo != null)
                                    {
                                        // in any case, update Id
                                        sideInfo.Id = newId;

                                        // ask user for repo operation
                                        if (AnyUiMessageBoxResult.Yes == await this.context.MessageBoxFlyoutShowAsync(
                                                "Rename AAS in Repository as well? This operation can not be reverted!",
                                                "Rename Identifiable",
                                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                        {

                                            // rename in repo
                                            // (only the side info in the _specific_ endpoint gives information, in
                                            // which repo the Indentifiables could be deleted)
                                            var newEndpoint = await PackageContainerHttpRepoSubset
                                                .AssistantRenameIdfsInRepo<Aas.IAssetAdministrationShell>(
                                                baseUri: PackageContainerHttpRepoSubset.GetBaseUri(
                                                    sideInfo.DesignatedEndpoint?.AbsoluteUri),
                                                oldId: oldId,
                                                newId: newId,
                                                runtimeOptions: packages.CentralRuntimeOptions,
                                                moreLog: true);

                                            Log.Singleton.Info("Rename in repo performed successfully.");

                                            // adopt in side info
                                            sideInfo.QueriedEndpoint = newEndpoint;
                                            sideInfo.DesignatedEndpoint = newEndpoint;
                                        }
                                    }

                                    // rename
                                    var lrf = env.RenameIdentifiable<Aas.AssetAdministrationShell>(
                                        oldId, newId);

                                    Log.Singleton.Info("Rename in ram-based environment performed successfully.");

                                    // rename in environment helper structures?
                                    if (package is AdminShellPackageDynamicFetchEnv dynPack)
                                    {
                                        // rename in dynamic fetch environment
                                        dynPack.RenameThumbnailData(oldId, newId);
                                        Log.Singleton.Info("Rename of thumbnail in dynamic fetch environment performed successfully.");
                                    }

                                    // use this information to emit events
                                    if (lrf != null)
                                    {
                                        res = true;
                                        foreach (var rf in lrf)
                                        {
                                            var rfi = rf.FindParentFirstIdentifiable();
                                            if (rfi != null)
                                                this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                }

                                if (!res)
                                    this.context.MessageBoxFlyoutShow(
                                        "The renaming of the AAS or some referring elements has not " +
                                            "performed successfully! Please review your inputs and the AAS " +
                                            "structure for any inconsistencies.",
                                            "Warning",
                                            AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                                return new AnyUiLambdaActionRedrawAllElements(aas);
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    }));

            // hasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
            this.DisplayOrEditEntityHasDataSpecificationReferences(stack, aas.EmbeddedDataSpecifications,
                (ds) => { aas.EmbeddedDataSpecifications = ds; }, relatedReferable: aas, superMenu: superMenu);

            // use some asset reference
            var asset = aas.AssetInformation;

            // derivedFrom
            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(
                    () =>
                    {
                        return asset != null && asset.AssetKind == Aas.AssetKind.Instance &&
                            ( aas.DerivedFrom == null || aas.DerivedFrom.Keys.Count < 1);
                    },
                    "You have decided to create an AAS for kind = 'Instance'. " +
                        "You might derive this from another AAS of kind = 'Instance' or " +
                        "from another AAS of kind = 'Type'. It is perfectly fair to create " +
                        "an AssetAdministrationShell with no 'derivedFrom' relation! " +
                        "However, for example, if you're an supplier of products which stem from a series-type, " +
                        "you might want to maintain a relation of the AAS's of the individual prouct instances " +
                        "to the AAS of the series type.",
                    severityLevel: HintCheck.Severity.Notice)
            });
            if (this.SafeguardAccess(
                stack, repo, aas.DerivedFrom, "derivedFrom:", "Create data element!",
                v =>
                {
                    aas.DerivedFrom = new Aas.Reference(Aas.ReferenceTypes.ModelReference,
                        new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.AssetAdministrationShell, "") });
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                this.AddGroup(stack, "Derived From", this.levelColors.SubSection);

                Func<List<Aas.IKey>, AnyUiLambdaActionBase> lambda = (kl) =>
                {
                    return new AnyUiLambdaActionNavigateTo(
                        new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)), translateAssetToAAS: true);
                };

                this.AddKeyReference(
                    stack, "derivedFrom", 
                    aas.DerivedFrom, () => aas.DerivedFrom = null,
                    repo,
                    packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo, "AssetAdministrationShell",
                    showRefSemId: false,
                    jumpLambda: lambda, noEditJumpLambda: lambda, relatedReferable: aas,
                    auxContextHeader: new[] { "\u2573", "Delete derivedFrom" },
                    auxContextLambda: (i) =>
                    {
                        if (i == 0)
                        {
                            aas.DerivedFrom = null;
                            this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }
                        return new AnyUiLambdaActionNone();
                    });
            }

            //
            // Asset linked with AAS
            //

            this.AddGroup(stack, "AssetInformation", this.levelColors.MainSection,
                requestAuxButton: repo != null,
                auxButtonTitle: (aas.AssetInformation == null) ? null : "Delete",
                auxButtonLambda: (o) =>
                {
                    if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                               "Delete AssetInformation in general? This operation can not be reverted!",
                               "AssetInformation",
                               AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                    {
                        aas.AssetInformation = null;
                        this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionRedrawEntity();
                    }

                    return new AnyUiLambdaActionNone();
                });

            this.AddHintBubble(stack, hintMode, new[] {
                new HintCheck(() => aas.AssetInformation == null,
                    "No AssetInformation created. According to the specification, there is a 1:1 relation " +
                    "of AAS and AssetInformation. Please urgently create the AssetInformation, as elsewise the " +
                    "serialization of the AAS might fail!!.")
            });
            if (this.SafeguardAccess(
                stack, repo, aas.AssetInformation, "AssetInformation:", "Create data element!",
                v =>
                {
                    aas.AssetInformation = new Aas.AssetInformation(Aas.AssetKind.Type);
                    this.AddDiaryEntry(aas, new DiaryEntryStructChange());
                    return new AnyUiLambdaActionRedrawEntity();
                }))
            {
                DisplayOrEditAasEntityAssetInformation(
                    packages, env, aas, aas.AssetInformation,
                    preferredNextFocus: aas,
                    editMode: editMode, repo: repo, stack: stack, hintMode: hintMode,
                    superMenu: superMenu);
            }
        }

        //
        //
        // --- Submodel Ref
        //
        //

        public void DisplayOrEditAasEntitySubmodelOrRef(
            PackageCentral.PackageCentral packages,
            AdminShellPackageEnvBase packEnv,
            Aas.IEnvironment env,
            Aas.IAssetAdministrationShell aas,
            Aas.IReference smref, 
            Action setSmRefNull,
            Aas.ISubmodel submodel,
            bool editMode,
            AnyUiStackPanel stack, bool hintMode = false, bool checkSmt = false,
			AasxMenu superMenu = null)
        {
            // This panel renders first the SubmodelReference and then the Submodel, below
            if (smref != null)
            {
                this.AddGroup(stack, "SubmodelReference of AAS", this.levelColors.MainSection);

                Func<List<Aas.IKey>, AnyUiLambdaActionBase> lambda = (kl) =>
                 {
                     return new AnyUiLambdaActionNavigateTo(
                         new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)), alsoDereferenceObjects: false);
                 };

                this.AddKeyListKeys(
                    stack, "submodelRef", 
                    smref.Keys, setSmRefNull,
                    repo,
                    packages, PackageCentral.PackageCentral.Selector.Main, "Reference Submodel ",
                    takeOverLambdaAction: new AnyUiLambdaActionRedrawAllElements(smref),
                    jumpLambda: lambda, relatedReferable: aas);
            }

            // check, if Submodel is sitting in Repo
            var sideInfo = OnDemandListIdentifiable<Aas.ISubmodel>
                    .FindSideInfoInListOfIdentifiables(
                        env.Submodels, submodel?.GetReference());

            // entities when under AAS (smref)
            if (editMode && smref != null)
            {
                this.AddGroup(stack, "Editing of entities (within specific AAS)", this.levelColors.SubSection);

                //
                // Up/Down Helper for SM References!
                //

                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = smref,
                    ParentElem = aas
                };

                this.EntityListUpDownDeleteHelper<Aas.IReference>(
                    stack, repo, 
                    aas.Submodels, (lst) => { aas.Submodels = lst; },
                    smref, aas, "Reference:", sendUpdateEvent: evTemplate,
                    explicitParent: aas,
                    postActionHookAsync: async (actionName, ticket) => {
                        await Task.Yield();
                        if (actionName == "aas-elem-delete")
                        {
                            // ask for complete deletion
                            if (ticket?.ScriptMode != true 
                                && AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                "Delete selected Submodel for all AAS in the Environment? " +
                                "This operation can not be reverted!", "AAS-ENV",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return;

                            // do
                            var smExist = env.FindSubmodel(smref);
                            if (smExist != null)
                                env.Remove(smExist);

                            // delete in repo as well?
                            var delInRepo = sideInfo?.Id?.HasContent() == true
                                    && sideInfo.StubLevel >= AasIdentifiableSideInfoLevel.IdOnly;

                            if (delInRepo)
                            {
                                if (AnyUiMessageBoxResult.Yes != await this.context.MessageBoxFlyoutShowAsync(
                                        "Delete Submodel in Repository as well? " +
                                        "This operation can not be reverted!",
                                        "Delete Identifiable",
                                        AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                    delInRepo = false;
                            }

                            // delete in repo?
                            if (delInRepo)
                            {
                                // simply prepare one Key!
                                var smKey = new Aas.IKey[] { new Aas.Key(KeyTypes.Submodel, "" + submodel.Id) };

                                // call function
                                // (only the side info in the _specific_ endpoint gives information, in which
                                //  repo the CDs could be deleted)
                                await PackageContainerHttpRepoSubset.AssistantDeleteIdfsInRepo(
                                    null, context,
                                    "Delete Submodel in Repository/ Registry",
                                    "Submodel",
                                    smKey,
                                    runtimeOptions: packages.CentralRuntimeOptions,
                                    presetRecord: new PackageContainerHttpRepoSubset.DeleteAssistantJobRecord()
                                    {
                                        // assume Repo ?!
                                        BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,

                                        // extract base address
                                        BaseAddress = "" + PackageContainerHttpRepoSubset.GetBaseUri(
                                            sideInfo?.DesignatedEndpoint?.AbsoluteUri)?.AbsoluteUri
                                    });
                            }

                            // manually redraw
                            this.appEventsProvider?.PushApplicationEvent(new AasxPluginResultEventRedrawAllElements());
                        }
                    });
            }

            // entities other
            if (editMode && smref == null && submodel != null)
            {
                this.AddGroup(
                    stack, "Editing of entities (within environment)",
                    this.levelColors.MainSection);

                //
                // Up/Down Helper for Submodels themself!
                //

                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = submodel,
                    ParentElem = env
                };

                this.EntityListUpDownDeleteHelper<Aas.ISubmodel>(
                    stack, repo,
                    env.Submodels, (lst) => { env.Submodels = lst; },
                    submodel, alternativeFocus: env, 
                    "Submodel:", sendUpdateEvent: evTemplate,
                    explicitParent: aas,
                    moveDoesNotModify: true,
                    postActionHookAsync: async (actionName, ticket) => {
                        await Task.Yield();
                        if (actionName == "aas-elem-delete")
                        {
                            // delete in repo as well?
                            var delInRepo = sideInfo?.Id?.HasContent() == true
                                    && sideInfo.StubLevel >= AasIdentifiableSideInfoLevel.IdOnly;

                            if (delInRepo)
                            {
                                if (AnyUiMessageBoxResult.Yes != await this.context.MessageBoxFlyoutShowAsync(
                                        "Delete Submodel in Repository as well? " +
                                        "This operation can not be reverted!",
                                        "Delete Identifiable",
                                        AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                    delInRepo = false;
                            }

                            // delete in repo?
                            if (delInRepo)
                            {
                                // simply prepare one Key!
                                var smKey = new Aas.IKey[] { new Aas.Key(KeyTypes.Submodel, "" + submodel.Id) };

                                // call function
                                // (only the side info in the _specific_ endpoint gives information, in which
                                //  repo the CDs could be deleted)
                                await PackageContainerHttpRepoSubset.AssistantDeleteIdfsInRepo(
                                    null, context,
                                    "Delete Submodel in Repository/ Registry",
                                    "Submodel",
                                    smKey,
                                    runtimeOptions: packages.CentralRuntimeOptions,
                                    presetRecord: new PackageContainerHttpRepoSubset.DeleteAssistantJobRecord()
                                    {
                                        // assume Repo ?!
                                        BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,

                                        // extract base address
                                        BaseAddress = "" + PackageContainerHttpRepoSubset.GetBaseUri(
                                            sideInfo?.DesignatedEndpoint?.AbsoluteUri)?.AbsoluteUri
                                    });
                            }

                            // manually redraw
                            this.appEventsProvider?.PushApplicationEvent(new AasxPluginResultEventRedrawAllElements());
                        }
                    });

#if __old_not_required_anymore
                AddActionPanel(stack, "Submodel:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("aas-elem-del", "Delete \U0001f847 here",
                            "Deletes the currently selected Submodel in the local environment.",
                            inputGesture: "Ctrl+Shift+Delete")
                        .AddAction("delete-sm-in-repo", "Delete SM \u274c in Repo",
                            "Delete Submodel by Id in a given Repository or Registry."),
                    ticketActionAsync: async (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                            if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                     "Delete selected Submodel? This operation can not be reverted!", "AAS-ENV",
                                     AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            {
                                // ask if to delete all references
                                if (ticket?.ScriptMode != true
                                    && AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                    "Remove References to this Submodel from all AAS in the environment?",
                                    "Remove Submodel",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                {
                                    env.RemoveReferences(
                                        rf: submodel.GetModelReference(),
                                        inAas: true);
                                }

                                // delete the Submodel itself
                                env.Remove(submodel);
                                this.AddDiaryEntry(submodel, new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: null, isExpanded: null);
                            }

                        if (buttonNdx == 1)
                        {
                            // check, if Submodel is sitting in Repo
                            var sideInfo = OnDemandListIdentifiable<Aas.ISubmodel>
                                    .FindSideInfoInListOfIdentifiables(
                                        env.Submodels, submodel.GetReference());

                            // enough info
                            if (sideInfo.StubLevel < AasIdentifiableSideInfoLevel.IdOnly
                                || sideInfo.Id?.HasContent() != true)
                            {
                                Log.Singleton.Error("No Id information available for deleting Identifiable in " +
                                    "Repository or Registry.");
                                return new AnyUiLambdaActionNone();
                            }

                            // simply prepare one Key!
                            var smKey = new Aas.IKey[] { new Aas.Key(KeyTypes.Submodel, "" + submodel.Id) };
                            
                            // call function
                            // (only the side info in the _specific_ endpoint gives information, in which
                            //  repo the CDs could be deleted)
                            await PackageContainerHttpRepoSubset.AssistantDeleteIdfsInRepo(
                                ticket, context,
                                "Delete Submodel in Repository/ Registry",
                                "Submodel",
                                smKey,
                                runtimeOptions: packages.CentralRuntimeOptions,
                                presetRecord: new PackageContainerHttpRepoSubset.DeleteAssistantJobRecord()
                                {
                                    // assume Repo ?!
                                    BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,

                                    // extract base address
                                    BaseAddress = "" + PackageContainerHttpRepoSubset.GetBaseUri(
                                        sideInfo?.DesignatedEndpoint?.AbsoluteUri)?.AbsoluteUri
                                });

                            // ok
                            return new AnyUiLambdaActionNone();
                        }

                        return new AnyUiLambdaActionNone();
                    });
#endif
            }

            // Cut, copy, paste within an aas
            // Resharper disable once ConditionIsAlwaysTrueOrFalse
            if (editMode && smref != null && submodel != null && aas != null)
            {
                // cut/ copy / paste
                this.DispSubmodelCutCopyPasteHelper<Aas.IReference>(stack, repo, this.theCopyPaste,
                    aas.Submodels, smref, (sr) => { return new Aas.Reference(sr.Type, new List<Aas.IKey>(sr.Keys)); },
                    smref, submodel, superMenu: superMenu,
                    label: "Buffer:",
                    checkEquality: (r1, r2) =>
                    {
                        if (r1 != null && r2 != null)
                            return (r1.Matches(r2, MatchMode.Identification));
                        return false;
                    },
                    extraAction: (cpi) =>
                    {
                        if (cpi is CopyPasteItemSubmodel item)
                        {
                            // special case: Submodel does not exist, as pasting was from external
                            if (env?.Submodels != null && item.smref != null && item.sm != null)
                            {
                                var smtest = env.FindSubmodel(item.smref);
                                if (smtest == null)
                                {
                                    env.Add(item.sm);
                                    this.AddDiaryEntry(item.sm,
                                        new DiaryEntryStructChange(StructuralChangeReason.Create));
                                }
                            }
                        }
                    });
            }
            else
            // Cut, copy, paste within the Submodels
            if (editMode && smref == null && submodel != null && env != null)
            {
                // cut/ copy / paste
                this.DispSubmodelCutCopyPasteHelper<Aas.ISubmodel>(stack, repo, this.theCopyPaste,
                    env.Submodels, submodel, (sm) => { return sm.Copy(); },
                    null, submodel, superMenu: superMenu,
                    label: "Buffer:",
                    checkEquality: (s1, s2) =>
                    {
                        if (s1?.Id != null && s2?.Id != null)
                            return (s1.Id.Equals(s2.Id, StringComparison.InvariantCultureIgnoreCase));
                        return false;
                    },
                    modifyAfterClone: (cloneSm, duplicate) =>
                    {
                        if (cloneSm != null && duplicate)
                            MakeNewIdentifiableUnique(cloneSm);
                    });
            }

            // normal edit of the submodel
            if (editMode && submodel != null)
            {
                DispSmeListAddNewHelper<ISubmodelElement>(env, stack, repo,
                    key: "SubmodelElement:",
                    submodel.SubmodelElements,
                    setOutput: (sml) => submodel.SubmodelElements = sml,
                    superMenu: superMenu,
                    basedOnSemanticId: submodel.SemanticId);

                this.AddHintBubble(stack, hintMode, new[] {
                    new HintCheck(
                        () => { return this.packages.AuxAvailable;  },
                        "You have opened an auxiliary AASX package. You can copy elements from it!",
                        severityLevel: HintCheck.Severity.Notice)
                });
                this.AddActionPanel(
                    stack, "Copy existing SMEs:",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("copy-single", "Copy single",
                            "Copy selected Submodel without children from another AAS, " +
                            "caring for ConceptDescriptions.")
                        .AddAction("copy-recurse", "Copy recursively",
                            "Copy selected Submodel and children from another AAS, " +
                            "caring for ConceptDescriptions."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "SubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null && env != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is Aas.ISubmodelElement)
                                {
                                    var clone = env.CopySubmodelElementAndCD(
                                        rve.theEnv, mdo as Aas.ISubmodelElement,
                                        copyCD: true, shallowCopy: buttonNdx == 0);

                                    if (submodel.SubmodelElements == null)
                                        submodel.SubmodelElements =
                                            new List<Aas.ISubmodelElement>();

                                    // make unqiue?
                                    if (!submodel.SubmodelElements.CheckIdShortIsUnique(clone.IdShort))
                                        this.MakeNewReferableUnique(clone);

                                    // ReSharper disable once PossibleNullReferenceException -- ignore a false positive
                                    submodel.SubmodelElements.Add(clone);

                                    // emit events
                                    // TODO (MIHO, 2021-08-17): create events for CDs are not emitted!
                                    this.AddDiaryEntry(clone,
                                        new DiaryEntryStructChange(StructuralChangeReason.Create));

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        submodel, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // create ConceptDescriptions for ECLASS
                var targets = new List<Aas.ISubmodelElement>();
                if (submodel.SubmodelElements != null)
                    this.IdentifyTargetsForEclassImportOfCDs(
                        env, new List<Aas.ISubmodelElement>(submodel.SubmodelElements),
                        ref targets);
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return submodel.SubmodelElements != null && submodel.SubmodelElements.Count > 0  &&
                                    targets.Count > 0;
                            },
                            "Consider creating ConceptDescriptions from ECLASS or from existing SubmodelElements.",
                            severityLevel: HintCheck.Severity.Notice)
                });

                this.AddActionPanel(
                    stack, "ConceptDescriptions:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("create-eclass", "Create \U0001f844 ECLASS",
                            "Create missing CDs searching from ECLASS.")
                        .AddAction("create-this", "Create \U0001f844 this",
                            "Creates an ConceptDescription from this element and " +
                            "assigns the SubmodelElement to it.")
                        .AddAction("create-smes", "Create \U0001f844 SMEs (all)",
                            "Create missing CDs from semanticId of used SMEs.")
                        .AddAction("delete-cd-in-repo", "Delete CD \u274c in Repo",
                            "Delete ConceptDescriptions which are referenced by semanticId of SubmodelElements " +
                            "in a given Repository or Registry."),
                    ticketActionAsync: async (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // from ECLASS
                            // ReSharper disable RedundantCast
                            this.ImportEclassCDsForTargets(
                                env, (smref != null) ? (object)smref : (object)submodel, targets);
                            // ReSharper enable RedundantCast
                        }

                        if (buttonNdx == 1)
                        {
                            var res = this.ImportCDsFromSmSme(env, submodel, recurseChilds: false, repairSemIds: true);

                            if (res.Item1 > 0)
                            {
                                Log.Singleton.Error("Cannot create CD because no valid semanticId is present " +
                                    "in SME.");
                                return new AnyUiLambdaActionNone();
                            }
                            if (res.Item2 > 0)
                            {
                                Log.Singleton.Error("Cannot create CD because CD with semanticId is already " +
                                    "present in AAS environment.");
                                return new AnyUiLambdaActionNone();
                            }
                            Log.Singleton.Info(StoredPrint.Color.Blue, $"Added {res.Item3} CDs to the environment.");

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: null);
                        }

                        if (buttonNdx == 2)
                        {
                            // from all SMEs

                            var adaptive61360 = 
                                this.context?.MessageBoxFlyoutShow(
                                    "Create IEC61360 data specifications and adaptively fill preferredName " +
                                    "and definition by idShort and description attributes?",
                                    "Create CDs from all SMEs",
                                    AnyUiMessageBoxButton.YesNoCancel, AnyUiMessageBoxImage.Information);

                            if (adaptive61360 == AnyUiMessageBoxResult.Cancel)
                                return new AnyUiLambdaActionNone();

                            var res = this.ImportCDsFromSmSme(env, submodel, recurseChilds: true, repairSemIds: true,
                                adaptive61360: adaptive61360 == AnyUiMessageBoxResult.Yes);

                            Log.Singleton.Info(StoredPrint.Color.Blue, $"Added {res.Item3} CDs to the environment, " +
                                $"while {res.Item1} invalid semanticIds were present and " +
                                $"{res.Item2} CDs were already existing.");

                            return new AnyUiLambdaActionRedrawAllElements(
                                        submodel, isExpanded: true);
                        }

                        if (buttonNdx == 3)
                        {
                            // check, if Submodel is sitting in Repo
                            var sideInfo = OnDemandListIdentifiable<Aas.ISubmodel>
                                    .FindSideInfoInListOfIdentifiables(
                                        env.Submodels, submodel.GetReference());

                            if (sideInfo == null)
                            {
                                Log.Singleton.Error("Not enough information to delete AAS in Repository/ Registry!");
                                return new AnyUiLambdaActionNone();
                            }

                            // collect Ids of SubmodelElements.semanticId
                            var lrs = env?.FindAllSemanticIdsForSubmodel(submodel);
                            if (lrs == null)
                                return new AnyUiLambdaActionNone();

                            var cdIds = lrs.Select((lr) => lr?.Reference?.GetAsExactlyOneKey()?.Value);
                            var cdKeys = cdIds.Select((cdid) => new Aas.Key(KeyTypes.ConceptDescription, cdid)).Cast<Aas.IKey>();

                            // call function
                            // (only the side info in the _specific_ endpoint gives information, in which
                            //  repo the CDs could be deleted)
                            await PackageContainerHttpRepoSubset.AssistantDeleteIdfsInRepo(
                                ticket, context,
                                "Delete CDs in Repository/ Registry",
                                "ConceptDescription",
                                cdKeys,
                                runtimeOptions: packages.CentralRuntimeOptions,
                                presetRecord: new PackageContainerHttpRepoSubset.DeleteAssistantJobRecord()
                                {
                                    // assume Repo ?!
                                    BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,

                                    // extract base address
                                    BaseAddress = "" + PackageContainerHttpRepoSubset.GetBaseUri(
                                        sideInfo?.DesignatedEndpoint?.AbsoluteUri)?.AbsoluteUri
                                });

                            // ok
                            return new AnyUiLambdaActionNone();
                        }

                        return new AnyUiLambdaActionNone();
                    });

                this.AddActionPanel(
                    stack, "Submodel&-elems:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("upgrade-qualifiers", "Upgrade qualifiers",
                            "Upgrades particular qualifiers from V2.0 to V3.0 for selected element.")
#if __old_approach
                        .AddAction("remove-qualifiers", "Remove qualifiers",
                            "Removes all qualifiers for selected element.")
                        .AddAction("remove-extensions", "Remove extensions",
                            "Removes all extensions for selected element.")
#else
                        .AddAction("remove-attributes", "Remove attributes",
                            "Removes specific attrributes for each selected element.")
#endif
                        .AddAction("fix-references", "Fix References",
                            "Fix, if References first key to Identifiables use idShort instead of id."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // confirm
                            if (ticket?.ScriptMode != true
                                && AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will affect all Qualifers of " +
                                    "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                    "Upgrade qualifiers",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            // action
                            var pfn = Options.Curr.QualifiersFile;
                            try
                            {
                                // read
                                var presets = ReadQualiferPresets(pfn);
                                if (presets == null)
                                {
                                    Log.Singleton.Error(
                                        $"JSON file for Quialifer presets not defined nor existing ({pfn}).");
                                    return new AnyUiLambdaActionNone();
                                }

                                QualiferUpgradeReferable(presets, submodel);

                                submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                                {
                                    // upgrade
                                    QualiferUpgradeReferable(presets, sme);

                                    // recurse
                                    return true;
                                });

                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"While upgrade Qualifiers by accessing ({pfn})");
                            }

                            // emit event for Submodel and children
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                        }

#if __moved_to_menu

                        if (buttonNdx == 1)
						{
                            // ask
							if (ticket?.ScriptMode != true
								&& AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
									"This operation will move data in particular Qualifiers to Extensions of " +
									"the Submodel and all of its SubmodelElements. Do you want to proceed?",
									"Convert SMT qualifiers to SMT extension",
									AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
								return new AnyUiLambdaActionNone();

                            // do
                            int anyChanges = 0;
                            Action<Aas.IReferable> lambdaConvert = (o) => {
                                if (AasSmtQualifiers.ConvertSmtQualifiersToExtension(o))
                                    anyChanges++;
                            };

                            lambdaConvert(submodel);
							submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
							{
								// do
								lambdaConvert(sme);
								// recurse
								return true;
							});

                            // report
                            Log.Singleton.Info($"Convert SMT qualifiers to SMT extension: {anyChanges} changes done.");

							// emit event for Submodel and children
							this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

							return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
						}

						if (buttonNdx == 2)
						{
							// ask 1
							if (ticket?.ScriptMode != true
								&& AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
									"This operation analyzes the element relatioships in the Submodel " +
                                    "and will take over these as organize references into SMT attribute " +
                                    "records of associated ConceptDescriptions. Do you want to proceed?",
									"Take over SM element relationships to CDs",
									AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
								return new AnyUiLambdaActionNone();

							// ask 2
							var eachElemDetails = true;
							if (ticket?.ScriptMode != true)
								eachElemDetails = AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
									"Create detailed SMT attributes for each relevant ConceptDescription, " +
                                    "include SubmodelElement type list?",
									"Take over SM element relationships to CDs",
									AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning);

#if __not_useful
                            // ask 2
                            var resetOrganize = true;
                            if (ticket?.ScriptMode != true)
                                resetOrganize = AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                    "Reset existing organize references in CDs?",
                                    "Take over SM element relationships to CDs",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning);
#endif

							// do
							int anyChanges = 0;
							Action<Aas.IReferable> lambdaConvert = (o) => {
								if (SmtAttributeRecord.TakeoverSmOrganizeToCds(env, o, 
                                        eachElemDetails: eachElemDetails))
									anyChanges++;
							};

							lambdaConvert(submodel);
							submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
							{
								// do
								lambdaConvert(sme);
								// recurse
								return true;
							});

							// report
							Log.Singleton.Info($"Take over SM element relationships to CDs: {anyChanges} changes done.");

							// emit event for Submodel and children
							this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

							return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
						}
#endif

#if __old_approach
						if (buttonNdx == 1)
                        {
                            if (ticket?.ScriptMode != true
                                && AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will affect all Qualifers of " +
                                    "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                    "Remove qualifiers",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            if (submodel.Qualifiers != null)
                                submodel.Qualifiers.Clear();

                            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                            {
                                // clear
                                if (sme.Qualifiers != null)
                                    sme.Qualifiers.Clear();
                                // recurse
                                return true;
                            });

                            // emit event for Submodel and children
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                        }

                        if (buttonNdx == 2)
                        {
                            if (ticket?.ScriptMode != true
                                && AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will affect all Extensions of " +
                                    "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                    "Remove extensions",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            if (submodel.Extensions != null)
                                submodel.Extensions.Clear();

                            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                            {
                                // clear
                                if (sme.Extensions != null)
                                    sme.Extensions.Clear();
                                // recurse
                                return true;
                            });

                            // emit event for Submodel and children
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                        }
#else
                        if (buttonNdx == 1)
                        {
                            // define dialogue and map presets into dialogue items
                            var uc = new AnyUiDialogueDataSelectFromList(
                                "Select which attributes to be removed from all SubmodelElements ...");
                            uc.ListOfItems = new AnyUiDialogueListItemList(true,
                                "All Qualifiers", "QUAL",
                                "All Extensions", "EXT",
                                "Add Descriptions", "DESC");

                            // perform dialogue
                            this.context.StartFlyoverModal(uc);
                            if (!(uc.Result && uc.ResultItem?.Tag is string selectedTag))
                                return new AnyUiLambdaActionNone();

                            // be absolute sure!
                            if (ticket?.ScriptMode != true
                                && AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will affect the selected attributes of " +
                                    "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                    "Remove attributes",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            // do it on Submodel level
                            
                            if (selectedTag == "QUAL")
                                submodel.Qualifiers = null;
                            if (selectedTag == "EXT")
                                submodel.Extensions = null;
                            if (selectedTag == "DESC")
                                submodel.Description = null;

                            // do it on SubmodelElements
                            submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                            {
                                // clear
                                if (selectedTag == "QUAL")
                                    sme.Qualifiers = null;
                                if (selectedTag == "EXT")
                                    sme.Extensions = null;
                                if (selectedTag == "DESC")
                                    sme.Description = null;

                                // recurse
                                return true;
                            });

                            // emit event for Submodel and children
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                        }
#endif

#if __old_approach
                        if (buttonNdx == 3)
#else
                        if (buttonNdx == 2)
#endif
                        {
                            // confirm
                            if (ticket?.ScriptMode != true
                                && AnyUiMessageBoxResult.Yes != this.context.MessageBoxFlyoutShow(
                                    "This operation will affect all References within " +
                                    "the Submodel and all of its SubmodelElements. Do you want to proceed?",
                                    "Fix References",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                return new AnyUiLambdaActionNone();

                            // action
                            try
                            {
                                ExtendIReferable.FixReferences(submodel, env?.AllSubmodels());
                                submodel.RecurseOnSubmodelElements(null, (o, parents, sme) =>
                                {
                                    // upgrade
                                    ExtendIReferable.FixReferences(sme, env?.AllSubmodels());

                                    // recurse
                                    return true;
                                });

                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"While fixing References.");
                            }

                            // emit event for Submodel and children
                            this.AddDiaryEntry(submodel, new DiaryEntryStructChange(), allChildrenAffected: true);

                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: smref, isExpanded: true);
                        }
                        return new AnyUiLambdaActionNone();
                    });

				// Check for cardinality
				if (checkSmt)
					DisplayOrEditEntityCheckValue(env, stack, _checkValueHandle, submodel);

			}

            // info about sideInfo
            if (submodel != null)
            {
                DisplayOrEditEntitySideInfo(env, stack, submodel, sideInfo, "Submodel", superMenu);
            }
            else
            {
                if (packEnv is AdminShellPackageDynamicFetchEnv dynPack)
                {
                    DisplayOrEditEntityMissingSideInfo(stack, "Submodel");
                }
            }

            // Submodel attributes
            if (submodel != null)
            {

                // Submodel
                this.AddGroup(stack, "Submodel", this.levelColors.MainSection);

                // IReferable (part 1)
                this.DisplayOrEditEntityReferable(
                    env, stack,
                    parentContainer: null, referable: submodel, indexPosition: 0,
                    hideExtensions: true,
                    superMenu: superMenu);

                // Identifiable
                this.DisplayOrEditEntityIdentifiable(
                    stack, packEnv, env, submodel,
                    (submodel.Kind == Aas.ModellingKind.Template)
                        ? Options.Curr.TemplateIdSubmodelTemplate
                        : Options.Curr.TemplateIdSubmodelInstance,
                    new DispEditHelperModules.DispEditInjectAction(
                        new[] { "Rename" },
                        auxLambda: null,
                        auxLambdaAsync: async (i) =>
                        {
                            if (i == 0 && env != null)
                            {
                                var uc = new AnyUiDialogueDataTextBox(
                                    "New ID:",
                                    symbol: AnyUiMessageBoxImage.Question,
                                    maxWidth: 1400,
                                    text: submodel.Id);
                                if (this.context.StartFlyoverModal(uc))
                                {
                                    var oldId = submodel.Id;
                                    var newId = uc.Text.Trim();
                                    var res = false;

                                    try
                                    {
                                        // check, if Submodel is sitting in Repo
                                        var sideInfo = OnDemandListIdentifiable<Aas.ISubmodel>
                                                .FindSideInfoInListOfIdentifiables(
                                                    env.Submodels, submodel.GetReference());
                                        if (sideInfo != null)
                                        {
                                            // in any case, update Id
                                            sideInfo.Id = newId;

                                            // ask user for repo operation
                                            if (AnyUiMessageBoxResult.Yes == await this.context.MessageBoxFlyoutShowAsync(
                                                    "Rename Submodel in Repository as well? This operation can not be reverted!",
                                                    "Rename Identifiable",
                                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                            {
                                                // rename in repo
                                                // (only the side info in the _specific_ endpoint gives information, in
                                                // which repo the Indentifiables could be deleted)
                                                var newEndpoint = await PackageContainerHttpRepoSubset
                                                    .AssistantRenameIdfsInRepo<Aas.ISubmodel>(
                                                    baseUri: PackageContainerHttpRepoSubset.GetBaseUri(
                                                        sideInfo.DesignatedEndpoint?.AbsoluteUri),
                                                    oldId: oldId,
                                                    newId: newId,
                                                    runtimeOptions: packages.CentralRuntimeOptions,
                                                    moreLog: true);

                                                Log.Singleton.Info("Rename in repo performed successfully.");

                                                // adopt in side info
                                                sideInfo.QueriedEndpoint = newEndpoint;
                                                sideInfo.DesignatedEndpoint = newEndpoint;
                                            }
                                        }

                                        // rename
                                        var lrf = env.RenameIdentifiable<Aas.Submodel>(
                                            submodel.Id, uc.Text);

                                        // use this information to emit events
                                        if (lrf != null)
                                        {
                                            res = true;
                                            foreach (var rf in lrf)
                                            {
                                                var rfi = rf.FindParentFirstIdentifiable();
                                                if (rfi != null)
                                                    this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                    }

                                    if (!res)
                                        this.context.MessageBoxFlyoutShow(
                                            "The renaming of the Submodel or some referring elements " +
                                            "has not performed successfully! Please review your inputs and " +
                                            "the AAS structure for any inconsistencies.",
                                            "Warning",
                                            AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                                    return new AnyUiLambdaActionRedrawAllElements(smref);
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        }));

                // HasKind
                this.DisplayOrEditEntityModelingKind(
                    stack, submodel.Kind,
                    (k) => { submodel.Kind = k; },
                    instanceExceptionStatement:
                        "Exception: if you want to declare a Submodel, which is been standardised " +
                        "by you or a standardisation body.",
                    relatedReferable: submodel);

                // HasSemanticId
                this.DisplayOrEditEntitySemanticId(stack, submodel,
                    "The semanticId may be either a reference to a submodel " +
                    "with kind=Type (within the same or another Administration Shell) or " +
                    "it can be an external reference to an external standard " +
                    "defining the semantics of the submodel (for example an PDF if a standard).",
                    addExistingEntities: Aas.KeyTypes.Referable + " " + Aas.KeyTypes.Submodel + " " +
                        Aas.KeyTypes.ConceptDescription,
                    relatedReferable: submodel);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                this.DisplayOrEditEntityQualifierCollection(
                    stack, submodel.Qualifiers,
                    (q) => { submodel.Qualifiers = q; },
                    relatedReferable: submodel,
                    superMenu: superMenu);

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                this.DisplayOrEditEntityHasDataSpecificationReferences(stack, submodel.EmbeddedDataSpecifications,
                    (ds) => { submodel.EmbeddedDataSpecifications = ds; },
                    relatedReferable: submodel,
                    superMenu: superMenu);

				// IReferable (part 2)
				this.DisplayOrEditEntityReferableContinue(
					env, stack,
					parentContainer: null, referable: submodel, indexPosition: 0,
					hideExtensions: true, superMenu: superMenu);

			}

            //
            // ConceptDescription <- via semantic ID ?!
            //

            if (submodel?.SemanticId?.Keys != null && submodel.SemanticId.Keys.Count > 0)
            {
                // cd is easy
                var cd = env.FindConceptDescriptionByReference(submodel.SemanticId);

                // available?
                if (cd == null)
                {
                    this.AddGroup(
                        stack, "ConceptDescription cannot be looked up within the AAS environment!",
                        this.levelColors.MainSection);
                }
                else
                {
                    DisplayOrEditAasEntityConceptDescription(
                        packages, env, submodel, cd, editMode, repo, stack,
                        embedded: true,
                        hintMode: hintMode);
                }
            }

            //
            // Submodel Value
            //

            if (submodel != null)
            {
                this.AddGroup(stack, "Submodel elements", this.levelColors.MainSection);
                if (submodel.SubmodelElements != null)
                    this.AddKeyValue(stack, "# of elements", "" + submodel.SubmodelElements.Count);
                else
                    this.AddKeyValue(stack, "Elements", "Please add elements via editing of sub-ordinate entities");
            }
        }

        //
        //
        // --- Submodel Stub
        //
        //

        public void DisplayOrEditAasEntitySubmodelStub(
            PackageCentral.PackageCentral packages, 
            AdminShellPackageEnvBase packEnv,
            Aas.IAssetAdministrationShell aas,
            Aas.IReference smref,
            Action setSmRefNull,
            AasIdentifiableSideInfo sideInfo,
            bool editMode,
            AnyUiStackPanel stack, bool hintMode = false, bool checkSmt = false,
            AasxMenu superMenu = null)
        {
            // access the on demand classes
            var packOD = packEnv as AdminShellPackageDynamicFetchEnv;

            // header
            this.AddGroup(stack, "Submodel stub data (Identifiable not already loaded)", this.levelColors.MainSection);

            // error?
            if (packOD == null)
            {
                AddHintBubble(stack, true,
                    new HintCheck(
                        () => true, "Package environment does not provide on demand functionality",
                        severityLevel: HintCheck.Severity.High));
            }
            else
            {
                // infos
                DisplayOrEditEntitySideInfo(packEnv?.AasEnv, stack, aas, sideInfo, "Submodel", superMenu);

                // actions
                AddActionPanel(stack, "Action:",
                    repo: repo,
                    superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("stub-load", "Load",
                            "Loads the Identifiable data from available data source.")
                        .AddAction("stub-load-all", "Load all",
                            "Loads data from available data source for all " +
                            "Identifiable stubs in environment.")
                        .AddAction("delete-sm-in-repo", "Delete Submodel \u274c in Repo",
                            "Delete Submodel by Id in a given Repository or Registry."),
                    ticketActionAsync: async (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var fetchedSm = await packOD.FindOrFetchIdentifiable(sideInfo?.Id);
                            if (fetchedSm != null)
                            {
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: fetchedSm);
                            }
                        }

                        if (buttonNdx == 1)
                        {
                            var res = await packOD.TryFetchAllMissingIdentifiables();
                            if (res)
                            {
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: null);
                            }
                        }

                        if (buttonNdx == 2)
                        {
                            // enough info
                            if (sideInfo.StubLevel < AasIdentifiableSideInfoLevel.IdOnly
                                || sideInfo.Id?.HasContent() != true)
                            {
                                Log.Singleton.Error("No Id information available for deleting Identifiable in " +
                                    "Repository or Registry.");
                                return new AnyUiLambdaActionNone();
                            }

                            // simply prepare one Key!
                            var smKey = new Aas.IKey[] { new Aas.Key(KeyTypes.Submodel, "" + sideInfo.Id) };

                            // call function
                            // (only the side info in the _specific_ endpoint gives information, in which
                            //  repo the CDs could be deleted)
                            await PackageContainerHttpRepoSubset.AssistantDeleteIdfsInRepo(
                                ticket, context,
                                "Delete Submodels in Repository/ Registry",
                                "Submodel",
                                smKey,
                                runtimeOptions: packages.CentralRuntimeOptions,
                                presetRecord: new PackageContainerHttpRepoSubset.DeleteAssistantJobRecord()
                                {
                                    // assume Repo ?!
                                    BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,

                                    // extract base address
                                    BaseAddress = "" + PackageContainerHttpRepoSubset.GetBaseUri(
                                        sideInfo?.DesignatedEndpoint?.AbsoluteUri)?.AbsoluteUri
                                });

                            // ok
                            return new AnyUiLambdaActionNone();
                        }

                        return new AnyUiLambdaActionNone();
                    });
            }
        }

        //
        //
        // --- Concept Description
        //
        //

        public void DisplayOrEditAasEntityConceptDescription(
            PackageCentral.PackageCentral packages, Aas.IEnvironment env,
            Aas.IReferable parentContainer, 
            Aas.IConceptDescription cd, 
            bool editMode, ModifyRepo repo,
            AnyUiStackPanel stack, bool embedded = false, bool hintMode = false, bool preventMove = false,
            AasxMenu superMenu = null)
        {
            this.AddGroup(stack, "ConceptDescription", this.levelColors.MainSection);

            // info about sideInfo
            var sideInfo = OnDemandListIdentifiable<Aas.IConceptDescription>
                .FindSideInfoInListOfIdentifiables(
                    env.ConceptDescriptions, cd.GetCdReference());

            // Up/ down/ del
            if (editMode && !embedded)
            {
                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = cd,
                    // TODO (MIHO, 2022-12-19): parent changed from env.ConceptDescriptions to env.
                    // Check, if rest of code will still run.
                    ParentElem = env
                };

                this.EntityListUpDownDeleteHelper<Aas.IConceptDescription>(
                    stack, repo, 
                    env.ConceptDescriptions, (lst) => { env.ConceptDescriptions = lst; },
                    cd, env, "CD:", sendUpdateEvent: evTemplate,
                    preventMove: preventMove,
                    superMenu: superMenu,
                    moveDoesNotModify: true,
                    postActionHookAsync: async (actionName, ticket) =>
                    {
                        await Task.Yield();
                        
                        // Note: sideinfo needs to be looked up before the helper, as the helper might
                        // delete it!
                        if (sideInfo?.Id?.HasContent() != true || sideInfo.StubLevel < AasIdentifiableSideInfoLevel.IdOnly)
                            return;

                        // ask?
                        if (AnyUiMessageBoxResult.Yes != await this.context.MessageBoxFlyoutShowAsync(
                                "Delete ConceptDescription in Repository as well? " +
                                "This operation can not be reverted!",
                                "Delete Identifiable",
                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                            return;

                        // simply prepare one Key!
                        var cdKey = new Aas.IKey[] { new Aas.Key(KeyTypes.ConceptDescription, "" + cd.Id) };

                        // call function
                        // (only the side info in the _specific_ endpoint gives information, in which
                        //  repo the CDs could be deleted)
                        await PackageContainerHttpRepoSubset.AssistantDeleteIdfsInRepo(
                            null, context,
                            "Delete ConceptDescription in Repository/ Registry",
                            "ConceptDescription",
                            cdKey,
                            runtimeOptions: packages.CentralRuntimeOptions,
                            presetRecord: new PackageContainerHttpRepoSubset.DeleteAssistantJobRecord()
                            {
                                // assume Repo ?!
                                BaseType = ConnectExtendedRecord.BaseTypeEnum.Repository,

                                // extract base address
                                BaseAddress = "" + PackageContainerHttpRepoSubset.GetBaseUri(
                                    sideInfo?.DesignatedEndpoint?.AbsoluteUri)?.AbsoluteUri
                            });
                    });
            }

            // Cut, copy, paste within list of CDs
            if (editMode && env != null)
            {
                // cut/ copy / paste
                this.DispPlainIdentifiableCutCopyPasteHelper<Aas.IConceptDescription>(
                    stack, repo, this.theCopyPaste,
                    env.ConceptDescriptions, cd, (o) => { return (o as Aas.ConceptDescription).Copy(); },
                    label: "Buffer:", superMenu: superMenu);
            }

            DisplayOrEditEntitySideInfo(env, stack, cd, sideInfo, "ConceptDescription", superMenu);

            // IReferable
            Action<bool> lambdaRf = (hideExtensions) =>
            {
                this.DisplayOrEditEntityReferable(
                    env, stack, parentContainer: parentContainer, referable: cd,
                    indexPosition: 0,
                    hideExtensions: hideExtensions,
                    superMenu: superMenu,
                    injectToIdShort: new DispEditHelperModules.DispEditInjectAction(
                        new[] { "Sync" },
                        new[] { "Copy (if target is empty) idShort to preferredName and SubmodelElement idShort." },
                        (v) =>
                        {
                            AnyUiLambdaActionBase la = new AnyUiLambdaActionNone();
                            if ((int)v != 0)
                                return la;

                            var ds = cd.GetIEC61360();
                            if (ds != null && (ds.PreferredName == null || ds.PreferredName.Count < 1
                                // the following absurd case happens in reality ..
                                || (ds.PreferredName.Count == 1 && ds.PreferredName[0].Text?.HasContent() != true)))
                            {
                                ds.PreferredName = new List<Aas.ILangStringPreferredNameTypeIec61360>
                                {
                                new Aas.LangStringPreferredNameTypeIec61360(
                                    AdminShellUtil.GetDefaultLngIso639(), cd.IdShort)
                                };
                                this.AddDiaryEntry(cd, new DiaryEntryStructChange());
                                la = new AnyUiLambdaActionRedrawEntity();
                            }

                            if (parentContainer != null & parentContainer is Aas.ISubmodelElement)
                            {
                                var sme = parentContainer as Aas.ISubmodelElement;
                                if (sme.IdShort == null || sme.IdShort.Trim() == "")
                                {
                                    sme.IdShort = cd.IdShort;
                                    this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                                    la = new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return la;
                        }));
            };

            // Identifiable

            Action lambdaIdf = () =>
            {
                this.DisplayOrEditEntityIdentifiable(
                    stack, packages?.Main, env, cd,
                    Options.Curr.TemplateIdConceptDescription,
                    new DispEditHelperModules.DispEditInjectAction(
                    new[] { "Rename" },
                    auxLambda: null,
                    auxLambdaAsync: async (i) =>
                    {
                        if (i == 0 && env != null)
                        {
                            var uc = new AnyUiDialogueDataTextBox(
                                "New ID:",
                                symbol: AnyUiMessageBoxImage.Question,
                                maxWidth: 1400,
                                text: cd.Id);
                            if (this.context.StartFlyoverModal(uc))
                            {
                                var oldId = cd.Id;
                                var newId = uc.Text.Trim();
                                var res = false;

                                try
                                {
                                    // check, if Submodel is sitting in Repo
                                    var sideInfo = OnDemandListIdentifiable<Aas.IConceptDescription>
                                            .FindSideInfoInListOfIdentifiables(
                                                env.ConceptDescriptions, cd.GetReference());
                                    if (sideInfo != null)
                                    {
                                        // in any case, update Id
                                        sideInfo.Id = newId;

                                        // ask user for repo operation
                                        if (AnyUiMessageBoxResult.Yes == await this.context.MessageBoxFlyoutShowAsync(
                                                "Rename ConceptDescription in Repository as well? " +
                                                "This operation can not be reverted!",
                                                "Rename Identifiable",
                                                AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                        {

                                            // rename in repo
                                            // (only the side info in the _specific_ endpoint gives information, in
                                            // which repo the Indentifiables could be deleted)
                                            var newEndpoint = await PackageContainerHttpRepoSubset
                                                .AssistantRenameIdfsInRepo<Aas.IConceptDescription>(
                                                baseUri: PackageContainerHttpRepoSubset.GetBaseUri(
                                                    sideInfo.DesignatedEndpoint?.AbsoluteUri),
                                                oldId: oldId,
                                                newId: newId,
                                                runtimeOptions: packages.CentralRuntimeOptions,
                                                moreLog: true);

                                            Log.Singleton.Info("Rename in repo performed successfully.");

                                            // adopt in side info
                                            sideInfo.QueriedEndpoint = newEndpoint;
                                            sideInfo.DesignatedEndpoint = newEndpoint;
                                        }
                                    }

                                    // rename
                                    var lrf = env.RenameIdentifiable<Aas.ConceptDescription>(
                                        cd.Id, uc.Text);

                                    // use this information to emit events
                                    if (lrf != null)
                                    {
                                        res = true;
                                        foreach (var rf in lrf)
                                        {
                                            var rfi = rf.FindParentFirstIdentifiable();
                                            if (rfi != null)
                                                this.AddDiaryEntry(rfi, new DiaryEntryStructChange());
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                                }

                                if (!res)
                                    this.context.MessageBoxFlyoutShow(
                                        "The renaming of the ConceptDescription or some referring elements has not " +
                                            "performed successfully! Please review your inputs and the AAS " +
                                            "structure for any inconsistencies.",
                                            "Warning",
                                            AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Warning);
                                return new AnyUiLambdaActionRedrawAllElements(cd);
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    }));
            };

            // isCaseOf are MULTIPLE references. That is: multiple x multiple keys!
            Action lambdaIsCaseOf = () =>
            {
                this.DisplayOrEditEntityListOfReferences(stack, cd.IsCaseOf,
                    (ico) => { cd.IsCaseOf = ico; },
                    "isCaseOf", relatedReferable: cd, superMenu: superMenu);
            };

#if OLD
            // joint header for data spec ref and content
            this.AddGroup(stack, "HasDataSpecification:", this.levelColors.SubSection);

            // check, if there is a IEC61360 content amd, subsequently, also a according data specification
            var esc = cd.EmbeddedDataSpecifications?.FindFirstIEC61360Spec();
            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return esc != null && (esc.DataSpecification == null
                            || !esc.DataSpecification.MatchesExactlyOneKey(
                                ExtendListOfEmbeddedDataSpecification.GetKeyForIec61360())); },
                        "IEC61360 content present, but data specification missing. Please add according reference.",
                        breakIfTrue: true),
                });

            //TODO (jtikekar, 0000-00-00): cd.dataspecifications vs embeddedDS
            // use the normal module to edit ALL data specifications
            this.DisplayOrEditEntityHasDataSpecificationReferences(stack, cd.EmbeddedDataSpecifications,
                (ds) => { cd.EmbeddedDataSpecifications = ds; },
                addPresetNames: new[] { "IEC61360" },
                addPresetKeyLists: new[] {
                    new List<Aas.Key>(){ 
                        ExtendListOfEmbeddedDataSpecification.GetKeyForIec61360() } },
                dataSpecRefsAreUsual: true, relatedReferable: cd);

            // the IEC61360 Content

            // TODO (MIHO, 2020-09-01): extend the lines below to cover also data spec. for units

            this.AddHintBubble(
                stack, hintMode,
                new[] {
                    new HintCheck(
                        () => { return cd.EmbeddedDataSpecifications?.GetIEC61360Content() == null; },
                        "Providing an embeddedDataSpecification, e.g. IEC61360 data specification content, " +
                            "is mandatory. This holds the descriptive information " +
                            "of an concept and allows for an off-line understanding of the meaning " +
                            "of an concept/ ISubmodelElement. Please create this data element.",
                        breakIfTrue: true),
                });
            var Iec61360spec = cd.EmbeddedDataSpecifications.FindFirstIEC61360Spec();
            if (this.SafeguardAccess(
                    stack, repo, Iec61360spec.DataSpecificationContent, "embeddedDataSpecification:",
                    "Create IEC61360 data specification content",
                    c))
            {
                this.DisplayOrEditEntityDataSpecificationIEC61360(
                    stack, Iec61360spec.DataSpecificationContent as Aas.DataSpecificationIec61360, 
                    relatedReferable: cd);
            }

#else

            // new apprpoach: model distinct sections with [Reference + Content]
            Action<bool> lambdaEDS = (suppressWarning) =>
            {
                DisplayOrEditEntityHasEmbeddedSpecification(
                    env, stack, cd.EmbeddedDataSpecifications,
                    (v) => { cd.EmbeddedDataSpecifications = v; },
                    addPresetNames: new[] { "IEC61360" /* , "Physical Unit" */ },
                    addPresetKeyLists: new[] {
                    new List<Aas.IKey>(){ ExtendIDataSpecificationContent.GetKeyForIec61360() /* ,
                    new List<Aas.IKey>(){ ExtendIDataSpecificationContent.GetKeyForPhysicalUnit() */ }
                    },
                    relatedReferable: cd, superMenu: superMenu,
                    suppressNoEdsWarning: suppressWarning);
            };
#endif

            // experimental: SAMM elements

            Action lambdaSammExt = () =>
            {
                DisplayOrEditEntitySammExtensions(
                    env, stack, cd.Extensions,
                    (v) => { cd.Extensions = v; },
                    addPresetNames: new[] { "IEC61360" /* , "Physical Unit" */ },
                    addPresetKeyLists: new[] {
                    new List<Aas.IKey>(){ ExtendIDataSpecificationContent.GetKeyForIec61360() /* ,
                    new List<Aas.IKey>(){ ExtendIDataSpecificationContent.GetKeyForPhysicalUnit() */ }
                    },
                    relatedReferable: cd, superMenu: superMenu);
            };

			// experimental: SMT elements

			Action lambdaExtRecs = () =>
			{
				DisplayOrEditEntityExtensionRecords(
					env, stack, cd.Extensions,
					(v) => { cd.Extensions = v; },
					relatedReferable: cd, superMenu: superMenu);
			};

			// check if to display special order for SAMM, SMT
			var specialOrderSAMM_SMT = 
                DispEditHelperSammModules.CheckReferableForSammExtensionType(cd) != null;

			if (specialOrderSAMM_SMT)
            {
				lambdaIdf();
				lambdaRf(true);
				lambdaSammExt();
				lambdaExtRecs();

				this.AddGroup(stack, "Continue Referable:", levelColors.MainSection);
				lambdaIsCaseOf();

				DisplayOrEditEntityListOfExtension(
	                stack: stack, extensions: cd.Extensions,
	                setOutput: (v) => { cd.Extensions = v; },
	                relatedReferable: cd, superMenu: superMenu);

				lambdaEDS(true);
			}
			else
            {
                lambdaRf(true);
                lambdaIdf();
                lambdaIsCaseOf();

				DisplayOrEditEntityListOfExtension(
					stack: stack, extensions: cd.Extensions,
					setOutput: (v) => { cd.Extensions = v; },
					relatedReferable: cd, superMenu: superMenu);

				lambdaEDS(false);
                lambdaSammExt();
				lambdaExtRecs();
			}
		}

		public void DisplayOrEditAasEntityValueReferencePair(
            PackageCentral.PackageCentral packages, Aas.IEnvironment env,
            Aas.IReferable parentContainer, Aas.IConceptDescription cd, Aas.IValueReferencePair vlp, bool editMode,
            ModifyRepo repo,
            AnyUiStackPanel stack, bool embedded = false, bool hintMode = false)
        {
            this.AddGroup(stack, "ConceptDescription / ValueList item", this.levelColors.MainSection);

            AddActionPanel(stack, "Action:",
                new[] { "Jump to CD" }, repo,
                action: (buttonNdx) =>
                {
                    if (buttonNdx == 0)
                    {
                        return new AnyUiLambdaActionNavigateTo(vlp?.ValueId);
                    }
                    return new AnyUiLambdaActionNone();
                });
        }

        //
        //
        // --- Operation Variable
        //
        //

        public void DisplayOrEditAasEntityOperationVariable(
            PackageCentral.PackageCentral packages, Aas.IEnvironment env,
            Aas.IReferable parentContainer, 
            Aas.IOperationVariable ov, 
            bool editMode,
            AnyUiStackPanel stack, bool hintMode = false,
            AasxMenu superMenu = null)
        {
            //
            // Submodel Element GENERAL
            //

            // OperationVariable is a must!
            if (ov == null)
                return;

            if (editMode)
            {
                this.AddGroup(stack, "Editing of entities", this.levelColors.MainSection);
                if (parentContainer != null && parentContainer is Aas.Operation operation)
                {
                    // have 3 lists to be individually managed
                    if (operation.InputVariables?.Contains(ov) == true)
                    {
                        this.EntityListUpDownDeleteHelper<Aas.IOperationVariable>(
                                stack, repo,
                                operation.InputVariables,
                                (lst) => { operation.InputVariables = lst; },
                                ov, 
                                env, "OperationVariable:");
                    }
                    else if (operation.OutputVariables?.Contains(ov) == true)
                    {
                        this.EntityListUpDownDeleteHelper<Aas.IOperationVariable>(
                                stack, repo,
                                operation.OutputVariables,
                                (lst) => { operation.OutputVariables = lst; },
                                ov, env, "OperationVariable:");
                    }
                    else if (operation.InoutputVariables?.Contains(ov) == true)
                    {
                        this.EntityListUpDownDeleteHelper<Aas.IOperationVariable>(
                                stack, repo,
                                operation.InoutputVariables,
                                (lst) => { operation.InoutputVariables = lst; },
                                ov, env, "OperationVariable:");
                    }
                }

            }

            // always an OperationVariable
            if (true)
            {
                this.AddGroup(stack, "OperationVariable", this.levelColors.MainSection);

                if (ov.Value == null)
                {
                    this.AddGroup(
                        stack, "OperationVariable value is not set!", this.levelColors.SubSection);

                    if (editMode)
                    {
                        this.AddActionPanel(
                            stack, "value:",
                            repo: repo, superMenu: superMenu,
                            ticketMenu: new AasxMenu()
                                .AddAction("add-prop", "Add Property",
                                    "Adds a new Property to the containing collection.")
                                .AddAction("add-mlp", "Add MultiLang.Prop.",
                                    "Adds a new MultiLanguageProperty to the containing collection.")
                                .AddAction("add-smc", "Add Collection",
                                    "Adds a new SubmodelElementCollection to the containing collection.")
                                .AddAction("add-named", "Add other ..",
                                    "Adds a selected kind of SubmodelElement to the containing collection.",
                                    args: new AasxMenuListOfArgDefs()
                                        .Add("Kind", "Name (not abbreviated) of kind of SubmodelElement.")),
                            ticketActionAsync: async (buttonNdx, ticket) =>
                            {
                                if (buttonNdx >= 0 && buttonNdx <= 3)
                                {
                                    // which adequate type?
                                    var en = Aas.AasSubmodelElements.SubmodelElement;
                                    if (buttonNdx == 0)
                                        en = Aas.AasSubmodelElements.Property;
                                    if (buttonNdx == 1)
                                        en = Aas.AasSubmodelElements.MultiLanguageProperty;
                                    if (buttonNdx == 2)
                                        en = Aas.AasSubmodelElements.SubmodelElementCollection;
                                    if (buttonNdx == 3)
                                        en = await this.SelectAdequateEnum(
                                            "Select SubmodelElement to create ..",
                                            excludeValues: new[] {
                                                Aas.AasSubmodelElements.DataElement,
                                                Aas.AasSubmodelElements.EventElement,
                                                Aas.AasSubmodelElements.Operation,
                                                Aas.AasSubmodelElements.ContainerElement
                                            });

                                    // ok?
                                    if (en != Aas.AasSubmodelElements.SubmodelElement)
                                    {
                                        // create
                                        Aas.ISubmodelElement sme2 =
                                            AdminShellUtil.CreateSubmodelElementFromEnum(en,
                                                defaultHelper: Options.Curr.GetCreateDefaultHelper());

                                        // add
                                        var smw = sme2;
                                        ov.Value = smw;

                                        // emit event (for parent container, e.g. Operation)
                                        this.AddDiaryEntry(parentContainer,
                                            new DiaryEntryStructChange(StructuralChangeReason.Create));

                                        // redraw
                                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: ov);
                                    }
                                }
                                return new AnyUiLambdaActionNone();
                            });

                    }
                }
                else
                {
                    // value is already set
                    // operations on it

                    if (editMode)
                    {
#if __old_violates_spec
                        this.AddActionPanel(stack, "value:",
                            repo: repo, superMenu: superMenu,
                            ticketMenu: new AasxMenu()
                                .AddAction("remove-value", "Remove existing",
                                    "Remove existing value from the OperationVariable."),
                            ticketAction: (buttonNdx, ticket) =>
                            {
                                if (buttonNdx == 0)
                                    if (AnyUiMessageBoxResult.Yes == this.context.MessageBoxFlyoutShow(
                                             "Delete value, which is the dataset of a SubmodelElement? " +
                                                 "This cannot be reverted!",
                                             "AAS-ENV", AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                    {
                                        ov.Value = null;

                                        // emit event (for parent container, e.g. Operation)
                                        this.AddDiaryEntry(parentContainer, new DiaryEntryStructChange());

                                        return new AnyUiLambdaActionRedrawAllElements(nextFocus: ov);
                                    }
                                return new AnyUiLambdaActionNone();
                            });
#endif

                        this.AddHintBubble(stack, hintMode, new[] {
                            new HintCheck(
                                () => { return this.packages.AuxAvailable;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                        this.AddActionPanel(
                            stack, "Copy existing SMEs:",
                            repo: repo, superMenu: superMenu,
                            ticketMenu: new AasxMenu()
                                .AddAction("copy-single", "Copy single",
                                    "Copy single selected entity from another AAS, caring for ConceptDescriptions.")
                                .AddAction("copy-recurse", "Copy recursively",
                                    "Copy selected entity and children from another AAS, caring for ConceptDescriptions."),
                            ticketAction: (buttonNdx, ticket) =>
                            {
                                if (buttonNdx == 0 || buttonNdx == 1)
                                {
                                    var rve = this.SmartSelectAasEntityVisualElement(
                                        packages, PackageCentral.PackageCentral.Selector.MainAux,
                                        "SubmodelElement") as VisualElementSubmodelElement;

                                    if (rve != null)
                                    {
                                        var mdo = rve.GetMainDataObject();
                                        if (mdo != null && mdo is Aas.ISubmodelElement)
                                        {
                                            var clone = env.CopySubmodelElementAndCD(
                                                rve.theEnv, mdo as Aas.ISubmodelElement,
                                                copyCD: true,
                                                shallowCopy: buttonNdx == 0);

                                            // emit event (for parent container, e.g. Operation)
                                            this.AddDiaryEntry(parentContainer, new DiaryEntryStructChange());

                                            ov.Value = clone;
                                            return new AnyUiLambdaActionRedrawEntity();
                                        }
                                    }
                                }

                                return new AnyUiLambdaActionNone();
                            });
                    }

                    // value == ISubmodelElement is displayed
                    this.AddGroup(
                        stack, "OperationVariable value (is a SubmodelElement)", this.levelColors.SubSection);
                    var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                    // huh, recursion in a lambda based GUI feedback function??!!
                    if (ov.Value != null && ov.Value != null) // avoid at least direct recursions!
                        DisplayOrEditAasEntitySubmodelElement(
                            packages, env, parentContainer, ov.Value, null, 0, editMode, repo,
                            substack, hintMode);
                }
            }
        }


        //
        //
        // --- Submodel Element
        //
        //

        public void DisplayOrEditAasEntitySubmodelElement(
            PackageCentral.PackageCentral packages, Aas.IEnvironment env,
            Aas.IReferable parentContainer, Aas.ISubmodelElement wrapper,
            Aas.ISubmodelElement sme, int indexPosition, bool editMode, ModifyRepo repo, AnyUiStackPanel stack,
            bool hintMode = false, bool checkSmt = false, bool nestedCds = false,
            AasxMenu superMenu = null)
        {
            //
            // Submodel Element GENERAL
            //

            // if wrapper present, must point to the sme
            if (wrapper != null)
            {
                if (sme != null && sme != wrapper)
                    return;
                sme = wrapper;
            }

            // submodelElement is a must!
            if (sme == null)
                return;

            // edit SubmodelElements's attributes
            if (editMode)
            {
                this.AddGroup(stack, "Editing of entities", this.levelColors.MainSection);

                // for sake of space efficiency, smuggle "Refactor" into this
                var horizStack = new AnyUiWrapPanel();
                horizStack.Orientation = AnyUiOrientation.Horizontal;
                stack.Children.Add(horizStack);

                // the event template will help speed up visual updates of the tree
                var evTemplate = new PackCntChangeEventData()
                {
                    Container = packages?.GetAllContainer((cnr) => cnr?.Env?.AasEnv == env).FirstOrDefault(),
                    ThisElem = sme,
                    ParentElem = parentContainer
                };

                // entities helper
                if (parentContainer != null && parentContainer is Aas.Submodel && wrapper != null)
                    this.EntityListUpDownDeleteHelper<Aas.ISubmodelElement>(
                        horizStack, repo, 
                        (parentContainer as Aas.Submodel).SubmodelElements,
                        (lst) => { (parentContainer as Aas.Submodel).SubmodelElements = lst; },
                        wrapper, alternativeFocus: parentContainer,
                        label: "SubmodelElement:", nextFocus: wrapper, sendUpdateEvent: evTemplate,
                        superMenu: superMenu);

                if (parentContainer != null && parentContainer is Aas.SubmodelElementCollection &&
                        wrapper != null)
                    this.EntityListUpDownDeleteHelper<Aas.ISubmodelElement>(
                        horizStack, repo, 
                        (parentContainer as Aas.SubmodelElementCollection).Value,
                        (lst) => { (parentContainer as Aas.SubmodelElementCollection).Value = lst; },
                        wrapper, alternativeFocus: parentContainer, label: "SubmodelElement:",
                        nextFocus: wrapper, sendUpdateEvent: evTemplate,
                        superMenu: superMenu);

                // although SML is the ideal case for acceleration, the calculation/ display of
                // the index position prevents us from this
                // TODO (MIHO, 2023-01-17): optimize also this important special case
                if (parentContainer != null && parentContainer is Aas.SubmodelElementList &&
                        wrapper != null)
                    this.EntityListUpDownDeleteHelper<Aas.ISubmodelElement>(
                        horizStack, repo, 
                        (parentContainer as Aas.SubmodelElementList).Value,
                        (lst) => { (parentContainer as Aas.SubmodelElementList).Value = lst; },
                        wrapper, alternativeFocus: parentContainer, label: "SubmodelElement:",
                        nextFocus: wrapper, sendUpdateEvent: null,
                        superMenu: superMenu);

                if (parentContainer != null && parentContainer is Aas.Entity && wrapper != null)
                    this.EntityListUpDownDeleteHelper<Aas.ISubmodelElement>(
                        horizStack, repo, 
                        (parentContainer as Aas.Entity).Statements,
                        (lst) => { (parentContainer as Aas.Entity).Statements = lst; },
                        wrapper, env, "SubmodelElement:",
                        nextFocus: wrapper, sendUpdateEvent: evTemplate,
                        superMenu: superMenu);

                // refactor?
                if (parentContainer != null && parentContainer is Aas.IReferable)
                    this.AddActionPanel(
                        horizStack, "Refactoring:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("refactor", "Refactor",
                                "Takes the selected AAS element and converts it to a new kind, keeping most of the attributes."),
                        ticketActionAsync: async (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                            {
                                // which?
                                var refactorSme = await this.SmartRefactorSme(packages.Main, sme);
                                var parMgr = (parentContainer as Aas.IReferable);

                                // ok?
                                if (refactorSme != null && parMgr != null)
                                {
                                    // open heart surgery: change in parent container accepted
                                    parMgr.Replace(sme, refactorSme);

                                    // notify event
                                    this.AddDiaryEntry(sme,
                                        new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    this.AddDiaryEntry(refactorSme,
                                        new DiaryEntryStructChange(StructuralChangeReason.Create));

                                    // redraw
                                    return new AnyUiLambdaActionRedrawAllElements(nextFocus: refactorSme);
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        });

                // cut/ copy / paste
                if (parentContainer != null)
                {
                    this.DispSmeCutCopyPasteHelper(stack, repo, env, parentContainer, this.theCopyPaste, wrapper, sme,
                        label: "Buffer:", superMenu: superMenu);
                }
            }


            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (editMode)
            // ReSharper enable ConditionIsAlwaysTrueOrFalse
            {
                // guess kind or instances
                Aas.ModellingKind? parentKind = Aas.ModellingKind.Template;
                if (parentContainer != null && parentContainer is Aas.Submodel)
                    parentKind = (parentContainer as Aas.Submodel).Kind;

                // relating to CDs
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return sme.SemanticId == null || sme.SemanticId.IsEmpty(); },
                            "The semanticId (see below) is empty. " +
                                "This SubmodelElement ist currently not assigned to any ConceptDescription. " +
                                "However, it is recommended to do such assignemt. " +
                                "With the 'Assign ..' buttons below you might create and/or assign " +
                                "the SubmodelElement to an ConceptDescription.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                // CDs, dynamic menu items
                var cdmenu = new AasxMenu()
                        .AddAction("assign-existing", "Use existing",
                            "Assign the SubmodelElement to an semanticId of an existing ConceptDescription.")
                        .AddAction("create-empty", "Create empty",
                            "Creates an empty ConceptDescription and assigns the SubmodelElement to it.")
                        .AddAction("create-eclass", "Create \U0001f844 ECLASS",
                            "Selects an concept from ECLASS, creates an ConceptDescription and " +
                            "assigns the SubmodelElement to it.")
                        .AddAction("create-this", "Create \U0001f844 this",
                            "Creates an ConceptDescription from this element and " +
                            "assigns the SubmodelElement to it.");

                if (sme.EnumeratesChildren())
                {
                    cdmenu.AddAction("create-all", "Create \U0001f844 all",
                            "Creates ConceptDescription(s) for this element and all subsequent children " +
                            "and assigns the SubmodelElement(s) to it.");
                }

                this.AddActionPanel(
                    stack, "Concept Description:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: cdmenu,
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            // select existing CD
                            var ks = this.SmartSelectAasEntityKeys(
                                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo);
                            if (ks != null)
                            {
                                // set the semantic id
                                //Using ModelReference for "Use existing" as this cd is being fetched from model/env
                                sme.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(ks));

                                // if empty take over shortName
                                var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                                if ((sme.IdShort == null || sme.IdShort.Trim() == "") && cd != null)
                                {
                                    sme.IdShort = "" + cd.IdShort;
                                    if (sme.IdShort == "")
                                        // NEW (2024-07-03): use preferred name instead of default name
                                        sme.IdShort = AdminShellUtil.CapitalizeFirstLetter(cd.GetDefaultPreferredName());
                                }

                                // emit event
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                            }
                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 1)
                        {
                            // create empty CD
                            var cd = new Aas.ConceptDescription(AdminShellUtil.GenerateIdAccordingTemplate(
                                Options.Curr.TemplateIdConceptDescription));

                            // store in AAS enviroment
                            env.Add(cd);

                            // go over to ISubmodelElement
                            // set the semantic id
                            sme.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.ConceptDescription, cd.Id) });

                            // emit event
                            this.AddDiaryEntry(sme, new DiaryEntryStructChange());

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 2)
                        {
                            // ECLASS
                            // feature available
                            if (Options.Curr.EclassDir == null)
                            {
                                // eclass dir?
                                this.context?.MessageBoxFlyoutShow(
                                        "The AASX Package Explore can take over ECLASS definition. " +
                                        "In order to do so, the commandine parameter -eclass has" +
                                        "to refer to a folder withe ECLASS XML files.", "Information",
                                        AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Information);
                                return new AnyUiLambdaActionNone();
                            }

                            // select
                            string resIRDI = null;
                            Aas.ConceptDescription resCD = null;
                            if (this.SmartSelectEclassEntity(
                                AnyUiDialogueDataSelectEclassEntity.SelectMode.ConceptDescription,
                                ref resIRDI, ref resCD))
                            {
                                // create the concept description itself, if available,
                                // if not exactly the same is present
                                if (resCD != null)
                                {
                                    var newcd = resCD;
                                    if (null == env.FindConceptDescriptionByReference(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.ConceptDescription, newcd.Id) })))
                                    {
                                        env.ConceptDescriptions ??= new List<IConceptDescription>();
                                        env.ConceptDescriptions.Add(newcd);
                                    }
                                }

                                // set the semantic key
                                sme.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>() { new Aas.Key(Aas.KeyTypes.ConceptDescription, resIRDI) });

                                // if empty take over shortName
                                var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                                if ((sme.IdShort == null || sme.IdShort.Trim() == "") && cd != null)
                                    // NEW (2024-07-03): use preferred name instead of default name
                                    sme.IdShort = AdminShellUtil.CapitalizeFirstLetter(cd.GetDefaultPreferredName());


                                // emit event
                                this.AddDiaryEntry(sme, new DiaryEntryStructChange());
                            }

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 3)
                        {
                            var res = this.ImportCDsFromSmSme(env, sme, recurseChilds: false, repairSemIds: true);

                            if (res.Item1 > 0)
                            {
                                Log.Singleton.Error("Cannot create CD because no valid semanticId is present " +
                                    "in SME.");
                                return new AnyUiLambdaActionNone();
                            }
                            if (res.Item2 > 0)
                            {
                                Log.Singleton.Error("Cannot create CD because CD with semanticId is already " +
                                    "present in AAS environment.");
                                return new AnyUiLambdaActionNone();
                            }
                            Log.Singleton.Info(StoredPrint.Color.Blue, $"Added {res.Item3} CDs to the environment.");

                            // redraw
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: sme);
                        }

                        if (buttonNdx == 4)
                        {
                            var adaptive61360 = AnyUiMessageBoxResult.Yes ==
                                this.context?.MessageBoxFlyoutShow(
                                    "Create IEC61360 data specification and adaptively fill preferredName " +
                                    "and definition by idShort and description attributes.",
                                    "Create CDs from SMEs",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Information);

                            var res = this.ImportCDsFromSmSme(env, sme, recurseChilds: true, repairSemIds: true,
                                adaptive61360: adaptive61360);

                            Log.Singleton.Info(StoredPrint.Color.Blue, $"Added {res.Item3} CDs to the environment, " +
                                $"while {res.Item1} invalid semanticIds were present and " +
                                $"{res.Item2} CDs were already existing.");
                        }

                        return new AnyUiLambdaActionNone();
                    });

                // create ConceptDescriptions for ECLASS
                var targets = new List<Aas.ISubmodelElement>();
                this.IdentifyTargetsForEclassImportOfCDs(
                    env, new List<Aas.ISubmodelElement>(new[] { sme }), ref targets);
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck( () => { return targets.Count > 0;  },
                        "Consider importing a ConceptDescription from ECLASS for the existing SubmodelElement.",
                        severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddActionPanel(
                    stack, "ConceptDescriptions from ECLASS:",
                    firstColumnWidth: FirstColumnWidth.Large,
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("import-missing", "Import missing",
                        "Checks for the element and its children, if semanticIds link to missing CD " +
                        "and imports those from ECLASS."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            this.ImportEclassCDsForTargets(env, sme, targets);
                        }

                        return new AnyUiLambdaActionNone();
                    });

            }

            if (editMode && (sme is Aas.SubmodelElementCollection
                || sme is Aas.SubmodelElementList || sme is Aas.Entity))
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                List<Aas.ISubmodelElement> listOfSME = null;
                if (sme is Aas.SubmodelElementCollection)
                    listOfSME = (sme as Aas.SubmodelElementCollection).Value;
                if (sme is Aas.SubmodelElementList)
                    listOfSME = (sme as Aas.SubmodelElementList).Value;
                if (sme is Aas.Entity)
                    listOfSME = (sme as Aas.Entity).Statements;

                // adding of SME

                DispSmeListAddNewHelper(env, stack, repo,
                    key: "SubmodelElement:",
                    listOfSME,
                    setOutput: (sml) =>
                    {
                        if (sme is Aas.SubmodelElementCollection)
                            (sme as Aas.SubmodelElementCollection).Value = sml;
                        if (sme is Aas.SubmodelElementList)
                            (sme as Aas.SubmodelElementList).Value = sml;
                        if (sme is Aas.Entity)
                            (sme as Aas.Entity).Statements = sml;
                    },
                    superMenu: superMenu,
                    basedOnSemanticId: sme.SemanticId);

                // Copy

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                });
                this.AddActionPanel(
                    stack, "Copy existing SMEs:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("copy-single", "Copy single",
                            "Copy single selected entity from another AAS, caring for ConceptDescriptions.")
                        .AddAction("copy-recurse", "Copy recursively",
                            "Copy selected entity and children from another AAS, caring for ConceptDescriptions."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "SubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is Aas.ISubmodelElement)
                                {
                                    var clone = env.CopySubmodelElementAndCD(
                                        rve.theEnv, mdo as Aas.ISubmodelElement, copyCD: true,
                                        shallowCopy: buttonNdx == 0);

                                    // make unqiue and add
                                    if (sme.GetChildsAsList()?.CheckIdShortIsUnique(clone.IdShort) == false)
                                        this.MakeNewReferableUnique(clone);
                                    sme.AddChild(clone);

                                    // emit event
                                    this.AddDiaryEntry(sme, new DiaryEntryStructChange());

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: sme, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

				// Check for cardinality
				if (checkSmt)
					DisplayOrEditEntityCheckValue(env, stack, _checkValueHandle, sme);
			}

            Aas.IConceptDescription jumpToCD = null;
            if (sme?.SemanticId != null && sme.SemanticId.Keys.Count > 0)
                jumpToCD = env?.FindConceptDescriptionByReference(sme.SemanticId);

            if (jumpToCD != null && editMode)
            {
                this.AddGroup(stack, "Navigation of entities", this.levelColors.MainSection);

                AddActionPanel(stack, "Navigate to:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("navigate-cd", "ConceptDescription",
                            "Finds the associated ConceptDescription by semanticId and visually selects it."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            return new AnyUiLambdaActionRedrawAllElements(nextFocus: jumpToCD, isExpanded: true);
                        }
                        return new AnyUiLambdaActionNone();
                    });
            }

            if (editMode && sme is Aas.Operation smo)
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                foreach (var dir in AdminShellUtil.GetEnumValues<OperationVariableDirection>())
                {
                    // dispatch
                    var names = (new[] { "In", "Out", "InOut" })[(int)dir];
                    var ovl = smo.GetVars(dir);

                    // group
                    this.AddGroup(substack, "OperationVariables " + names, this.levelColors.SubSection);

                    // add, paste
                    this.AddHintBubble(
                        substack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return ovl == null || ovl.Count < 1; },
                                    "This list of OperationVariables currently has no elements, yet. " +
                                    (ovl == null ? "List is null! " : "List is empty! ") +
                                    "Please check, which in- and out-variables are required.",
                                severityLevel: HintCheck.Severity.Notice)
                        });

                    // add
                    this.AddActionPanel(
                        substack, "OperationVariable:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("add-prop", "Add Property",
                                "Adds a new Property to the containing collection.")
                            .AddAction("add-mlp", "Add MultiLang.Prop.",
                                "Adds a new MultiLanguageProperty to the containing collection.")
                            .AddAction("add-smc", "Add Collection",
                                "Adds a new SubmodelElementCollection to the containing collection.")
                            .AddAction("add-named", "Add other ..",
                                "Adds a selected kind of SubmodelElement to the containing collection.",
                                args: new AasxMenuListOfArgDefs()
                                    .Add("Kind", "Name (not abbreviated) of kind of SubmodelElement.")),
                        ticketActionAsync: async (buttonNdx, ticket) =>
                        {
                            if (buttonNdx >= 0 && buttonNdx <= 3)
                            {
                                // which adequate type?
                                var en = Aas.AasSubmodelElements.SubmodelElement;
                                if (buttonNdx == 0)
                                    en = Aas.AasSubmodelElements.Property;
                                if (buttonNdx == 1)
                                    en = Aas.AasSubmodelElements.MultiLanguageProperty;
                                if (buttonNdx == 2)
                                    en = Aas.AasSubmodelElements.SubmodelElementCollection;
                                if (buttonNdx == 3)
                                    en = await this.SelectAdequateEnum(
                                        "Select SubmodelElement to create ..",
                                        excludeValues: new[] {
                                            Aas.AasSubmodelElements.DataElement,
                                            Aas.AasSubmodelElements.EventElement,
                                            Aas.AasSubmodelElements.Operation,
                                            Aas.AasSubmodelElements.ContainerElement
                                        });

                                // ok?
                                if (en != Aas.AasSubmodelElements.SubmodelElement)
                                {
                                    // create SME
                                    var sme2 =
                                        AdminShellUtil.CreateSubmodelElementFromEnum(en,
                                            defaultHelper: Options.Curr.GetCreateDefaultHelper());

                                    // prepare
                                    ovl ??= new List<Aas.IOperationVariable>();
                                    var ov = new Aas.OperationVariable(sme2);
                                    ovl.Add(ov);
                                    smo.SetVars(dir, ovl);

                                    // emit event
                                    this.AddDiaryEntry(smo, new DiaryEntryStructChange());

                                    // redraw
                                    return new AnyUiLambdaActionRedrawAllElements(nextFocus: ov);
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        });

                    // Buffer
                    AddActionPanel(
                        substack, "Buffer:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("operation-paste", "Paste into",
                                "Pastes an SubmodelElement from the paste buffer into the operation variable(s)."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0
                                && this.theCopyPaste?.Valid == true
                                && this.theCopyPaste.Items != null
                                && this.theCopyPaste.Items.AllOfElementType<CopyPasteItemSME>())
                            {
                                object businessObj = null;
                                foreach (var it in this.theCopyPaste.Items)
                                {
                                    // access
                                    var item = it as CopyPasteItemSME;
                                    if (item?.Sme == null)
                                    {
                                        Log.Singleton.Error("When pasting SME, an element was invalid.");
                                        continue;
                                    }

                                    var smw2 = item.Sme.Copy();

                                    businessObj = smo.AddChild(smw2,
                                        new EnumerationPlacmentOperationVariable() { Direction = dir });

                                    // may delete original
                                    if (!this.theCopyPaste.Duplicate)
                                    {
                                        this.DispDeleteCopyPasteItem(item);

                                        // emit event
                                        this.AddDiaryEntry(item.Sme,
                                            new DiaryEntryStructChange(StructuralChangeReason.Delete));
                                    }
                                }

                                // emit event
                                this.AddDiaryEntry(smo, new DiaryEntryStructChange());

                                // redraw
                                return new AnyUiLambdaActionRedrawAllElements(nextFocus: businessObj);
                            }

                            return new AnyUiLambdaActionNone();
                        });

                    this.AddHintBubble(
                        substack, hintMode,
                        new[] {
                            new HintCheck(
                                () => { return this.packages.AuxAvailable;  },
                                "You have opened an auxiliary AASX package. You can copy elements from it!",
                                severityLevel: HintCheck.Severity.Notice)
                        });
                    AddActionPanel(
                        substack, "Copy from existing OperationVariable:",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("copy-single", "Copy single",
                                "Copy single selected entity from another AAS, caring for ConceptDescriptions.")
                            .AddAction("copy-recurse", "Copy recursively",
                                "Copy selected entity and children from another AAS, caring for ConceptDescriptions."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0 || buttonNdx == 1)
                            {
                                var rve = this.SmartSelectAasEntityVisualElement(
                                    packages, PackageCentral.PackageCentral.Selector.MainAux,
                                    "OperationVariable") as VisualElementOperationVariable;

                                if (rve != null)
                                {
                                    var mdo = rve.GetMainDataObject();
                                    if (mdo != null && mdo is Aas.OperationVariable mdov && mdov.Value != null)
                                    {
                                        smo.AddChild(mdov.Value.Copy(),
                                            new EnumerationPlacmentOperationVariable() { Direction = dir });

                                        // emit event
                                        this.AddDiaryEntry(smo, new DiaryEntryStructChange());

                                        return new AnyUiLambdaActionRedrawAllElements(
                                            nextFocus: smo, isExpanded: true);
                                    }
                                }
                            }

                            return new AnyUiLambdaActionNone();
                        });

                }

            }

            if (editMode && sme is Aas.AnnotatedRelationshipElement are)
            {
                this.AddGroup(stack, "Editing of sub-ordinate entities", this.levelColors.MainSection);

                var substack = this.AddSubStackPanel(stack, "  "); // just a bit spacing to the left

                DispSmeListAddNewHelper<IDataElement>(env, stack, repo,
                    key: "annotation:",
                    are.Annotations,
                    setOutput: (sml) => are.Annotations = sml,
                    superMenu: superMenu,
                    basedOnSemanticId: are.SemanticId);

                this.AddHintBubble(
                    substack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return this.packages.AuxAvailable;  },
                            "You have opened an auxiliary AASX package. You can copy elements from it!",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                this.AddActionPanel(
                    substack, "Copy from existing DataElement:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("copy-single", "Copy single",
                            "Copy single selected entity from another AAS, caring for ConceptDescriptions."),
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var rve = this.SmartSelectAasEntityVisualElement(
                                packages, PackageCentral.PackageCentral.Selector.MainAux,
                                "SubmodelElement") as VisualElementSubmodelElement;

                            if (rve != null)
                            {
                                var mdo = rve.GetMainDataObject();
                                if (mdo != null && mdo is Aas.IDataElement)
                                {
                                    var clonesmw = (mdo as Aas.IDataElement).Copy();

                                    if (are.Annotations == null)
                                        are.Annotations = new List<Aas.IDataElement>();

                                    // ReSharper disable once PossibleNullReferenceException  -- ignore a false positive
                                    are.Annotations.Add(clonesmw);

                                    // emit event
                                    this.AddDiaryEntry(are, new DiaryEntryStructChange(), allChildrenAffected: true);

                                    return new AnyUiLambdaActionRedrawAllElements(
                                        nextFocus: clonesmw, isExpanded: true);
                                }
                            }
                        }

                        return new AnyUiLambdaActionNone();
                    });

            }

            {
                this.AddGroup(
                    stack,
                    $"Submodel Element ({"" + sme?.GetSelfDescription().AasElementName})",
                    this.levelColors.MainSection);

                // IReferable (part 1)
                this.DisplayOrEditEntityReferable(
                    env, stack,
                    parentContainer: parentContainer, referable: sme, indexPosition: indexPosition,
                    hideExtensions: true,
                    superMenu: superMenu,
                    injectToIdShort: new DispEditHelperModules.DispEditInjectAction(
                        auxTitles: new[] { "Sync" },
                        auxToolTips: new[] { "Copy (if target is empty) idShort " +
                        "to concept desctiption idShort and shortName." },
                        auxActions: (buttonNdx) =>
                        {
                            if (sme.SemanticId != null && sme.SemanticId.Keys.Count > 0)
                            {
                                var cd = env.FindConceptDescriptionByReference(sme.SemanticId);
                                if (cd != null)
                                {
                                    if (cd.IdShort == null || cd.IdShort.Trim() == "")
                                        cd.IdShort = sme.IdShort;

                                    var ds = cd.EmbeddedDataSpecifications?.GetIEC61360Content();
                                    if (ds != null && (ds.ShortName == null || ds.ShortName.Count < 1))
                                    {
                                        ds.ShortName = new List<Aas.ILangStringShortNameTypeIec61360>
                                        {
                                            new Aas.LangStringShortNameTypeIec61360(
                                                AdminShellUtil.GetDefaultLngIso639(), sme.IdShort)
                                        };
                                    }

                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        }));


                // HasSemanticId
                this.DisplayOrEditEntitySemanticId(stack, sme,
                    "The use of semanticId for SubmodelElements is mandatory! " +
                    "Only by this means, an automatic system can identify and " +
                    "understand the meaning of the SubmodelElements and, for example, " +
                    "its unit or logical datatype. " +
                    "The semanticId shall reference to a ConceptDescription within the AAS environment " +
                    "or an external repository, such as IEC CDD or ECLASS or " +
                    "a company / consortia repository.",
                    checkForCD: true,
                    addExistingEntities: Aas.KeyTypes.ConceptDescription.ToString(),
                    cpb: theCopyPaste, relatedReferable: sme);

                // Qualifiable: qualifiers are MULTIPLE structures with possible references. 
                // That is: multiple x multiple keys!
                this.DisplayOrEditEntityQualifierCollection(
                    stack, sme.Qualifiers,
                    (q) => { sme.Qualifiers = q; }, relatedReferable: sme,
                    superMenu: superMenu);

                // HasDataSpecification are MULTIPLE references. That is: multiple x multiple keys!
                this.DisplayOrEditEntityHasDataSpecificationReferences(stack, sme.EmbeddedDataSpecifications,
                (ds) => { sme.EmbeddedDataSpecifications = ds; }, relatedReferable: sme, superMenu: superMenu);

				// IReferable (part 2)
				this.DisplayOrEditEntityReferableContinue(
					env, stack,
					parentContainer: null, referable: sme, indexPosition: 0,
					hideExtensions: true, superMenu: superMenu);

				//
				// ConceptDescription <- via semantic ID ?!
				//

				if (sme.SemanticId != null && sme.SemanticId.Keys.Count > 0 && !nestedCds)
                {
                    // CD
                    var cd = env.FindConceptDescriptionByReference(sme.SemanticId);

                    // available
                    if (cd == null)
                    {
                        this.AddGroup(
                            stack, "ConceptDescription cannot be looked up within the AAS environment!",
                            this.levelColors.MainSection);
                    }
                    else
                    {
                        DisplayOrEditAasEntityConceptDescription(
                            packages, env, sme, cd, editMode, repo, stack,
                            embedded: true,
                            hintMode: hintMode);
                    }
                }

            }

            //
            // Submodel Element VALUES
            //
            if (sme is Aas.Property)
            {
                var p = sme as Aas.Property;
                this.AddGroup(stack, "Property", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p?.ValueType == null || Aas.Stringification.ToString(p.ValueType).Trim().Length < 1; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                AddKeyValueExRef(
                    stack, "valueType", p, Aas.Stringification.ToString(p.ValueType), null, repo,
                    v =>
                    {
                        var vt = Aas.Stringification.DataTypeDefXsdFromString((string)v);
                        if (vt.HasValue)
                            p.ValueType = vt.Value;
                        this.AddDiaryEntry(p, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxMinWidth: 190,
                    comboBoxIsEditable: editMode,
                    comboBoxItems: ExtendStringification.DataTypeXsdToStringArray().ToArray() // Enum.GetNames(typeof(DataTypeDefXsd))
                    );

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return p.Value == null || p.Value.Trim().Length < 1; },
                            "The value of the Property. " +
                                "Please provide a string representation " +
                                "(in case of numbers: without quotes, '.' as decimal separator, " +
                                "in XML number representation).",
                            severityLevel: HintCheck.Severity.Notice,
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return true == p.Value?.Contains('\r') || true == p.Value?.Contains('\n'); },
                            "It is strongly not recommended to have multi-line properties. " +
                            "However, the technological possibility is given.",
                            severityLevel: HintCheck.Severity.Notice)

                    });

				// now: Value
				AddKeyValueExRef(
                    stack, "value", p, p.Value, null, repo,
                    v =>
                    {
                        // primary update
                        p.Value = v as string;
                        this.AddDiaryEntry(p, new DiaryEntryUpdateValue());
                        
                        // kick off value check?
                        if (_checkValueHandle != null && checkSmt)
                        {
                            DisplayOrEditEntityCheckValue(env, stack, _checkValueHandle, sme, update: true);
                            return new AnyUiLambdaActionEntityPanelReRender(
                                mode: AnyUiRenderMode.StatusToUi,
                                updateElemsOnly: new Dictionary<AnyUiUIElement, bool>() {
                                    { _checkValueHandle.Border, true },
									{ _checkValueHandle.TextBlock, true }
								});
						}
                        
                        // normal
                        return new AnyUiLambdaActionNone();
                    },
                    auxButtonTitles: new[] { "\u2261" },
                    auxButtonToolTips: new[] { "Edit in multiline editor" },
                    auxButtonLambda: (buttonNdx) =>
                    {
                        if (buttonNdx == 0)
                        {
                            var uc = new AnyUiDialogueDataTextEditor(
                                caption: $"Edit Property '{"" + p.IdShort}'",
                                mimeType: "text/markdown",
                                text: p.Value);

#if test
                            // test wise
                            uc.Presets = new List<AnyUiDialogueDataTextEditor.Preset>
							{
								new AnyUiDialogueDataTextEditor.Preset() { Name = "AAA", Lines = new[] { "Aaa", "AAA" } },
								new AnyUiDialogueDataTextEditor.Preset() { Name = "BBB", Lines = new[] { "Bbb", "bbb", "bbb bbb" } }
							};
#endif

                            if (this.context.StartFlyoverModal(uc))
                            {
                                p.Value = uc.Text;
                                this.AddDiaryEntry(p, new DiaryEntryUpdateValue());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                        }
                        return new AnyUiLambdaActionNone();
                    });

                if (checkSmt)
				    DisplayOrEditEntityCheckValue(env, stack, _checkValueHandle, sme);

				this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () =>
                            {
                                return (p.Value == null || p.Value.Trim().Length < 1) &&
                                    (p.ValueId == null || p.ValueId.IsEmpty());
                            },
                            "Yon can express the value also be referring to a (enumumerated) value " +
                                "in a (the respective) repository. " +
                                "Below, you can create a reference to the value in the external repository.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                if (this.SafeguardAccess(
                        stack, repo, p.ValueId, "valueId:", "Create data element!",
                        v =>
                        {
                            p.ValueId = Options.Curr.GetDefaultEmptyReference();
                            this.AddDiaryEntry(p, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddGroup(stack, "ValueId:", this.levelColors.SubSection);

                    this.AddKeyReference(
                        stack, "valueId", 
                        p.ValueId, () => p.ValueId = null,
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All", // no restriction
                        relatedReferable: p,
                        showRefSemId: false, // not necessary, I think
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); },
                        auxContextHeader: new[] { "\u2573", "Delete valueId" },
                        auxContextLambda: (i) =>
                        {
                            if (i == 0)
                            {
                                p.ValueId = null;
                                this.AddDiaryEntry(p, new DiaryEntryStructChange());
                                return new AnyUiLambdaActionRedrawEntity();
                            }
                            return new AnyUiLambdaActionNone();
                        });
                }
            }
            else if (sme is Aas.MultiLanguageProperty)
            {
                var mlp = sme as Aas.MultiLanguageProperty;
                this.AddGroup(stack, "MultiLanguageProperty", this.levelColors.MainSection);

                // Value
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return mlp.Value == null || mlp.Value.Count < 1; },
                            "Please add a string value, defined in multiple languages.",
                            breakIfTrue: true),
                        new HintCheck(
                            () => { return mlp.Value.Count <2; },
                            "Please add multiple languanges.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, mlp.Value, "value:", "Create data element!",
                        v =>
                        {
							mlp.Value = ExtendILangStringTextType.CreateFrom(
                                lang: AdminShellUtil.GetDefaultLngIso639(), 
                                text: Options.Curr.DefaultEmptyLangText);
                            this.AddDiaryEntry(mlp, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    // edit
					this.AddKeyListLangStr<Aas.ILangStringTextType>(
                        stack, "value", mlp.Value, repo,
                        relatedReferable: mlp,
                        setNullList: () => mlp.Value = null,
                        emitCustomEvent: (rf) => {
                            // primary
							this.AddDiaryEntry(rf, new DiaryEntryUpdateValue());
                           
							// kick off value check?
							if (_checkValueHandle != null && checkSmt)
							{
								DisplayOrEditEntityCheckValue(env, stack, _checkValueHandle, sme, update: true);
								return new AnyUiLambdaActionEntityPanelReRender(
									mode: AnyUiRenderMode.StatusToUi,
									updateElemsOnly: new Dictionary<AnyUiUIElement, bool>() {
									    { _checkValueHandle.Border, true },
									    { _checkValueHandle.TextBlock, true }
									});
							}

                            // normal
                            return new AnyUiLambdaActionNone();
                        });

					// provide check
                    if (checkSmt)
					    DisplayOrEditEntityCheckValue(env, stack, _checkValueHandle, sme);
				}

                // ValueId

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                    new HintCheck(
                        () => mlp.ValueId != null && mlp.ValueId.IsValid() != true,
                        "According to the specification, an existing list of elements shall contain " +
                        "at least one element and for each element all mandatory fields shall be " +
                        "not empty.")
                });

                if (this.SafeguardAccess(
                        stack, repo, mlp.ValueId, "valueId:", "Create data element!",
                        v =>
                        {
                            mlp.ValueId = Options.Curr.GetDefaultEmptyReference(); 
                            this.AddDiaryEntry(mlp, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddGroup(stack, "ValueID", this.levelColors.SubSection);
                    this.AddKeyListKeys(
                        stack, "valueId", 
                        mlp.ValueId.Keys, () => mlp.ValueId = null,
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        Aas.Stringification.ToString(Aas.KeyTypes.GlobalReference),
                        relatedReferable: mlp,
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else if (sme is Aas.Range)
            {
                var rng = sme as Aas.Range;
                this.AddGroup(stack, "Range", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rng?.ValueType == null; },
                            "Please check, if you can provide a value type for the concept. " +
                                "Value types are provided by built-in types of XML Schema Definition 1.1.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                AddKeyValueExRef(
                    stack, "valueType", rng, Aas.Stringification.ToString(rng.ValueType), null, repo,
                    v =>
                    {
                        rng.ValueType = (Aas.DataTypeDefXsd)Aas.Stringification.DataTypeDefXsdFromString((string)v);
                        this.AddDiaryEntry(rng, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true, comboBoxMinWidth: 190,
                    comboBoxItems: ExtendStringification.DataTypeXsdToStringArray().ToArray());

                var mine = rng.Min == null || rng.Min.Trim().Length < 1;
                var maxe = rng.Max == null || rng.Max.Trim().Length < 1;

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return mine && maxe; },
                            "Please provide either min or max.",
                            severityLevel: HintCheck.Severity.High),
                        new HintCheck(
                            () => { return mine; },
                            "The value of the minimum of the Range. " +
                                "Please provide a string representation (without quotes, '.' as decimal separator, " +
                                "in XML number representation). " +
                                "If the min value is missing then the value is assumed to be negative infinite.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                AddKeyValueExRef(
                    stack, "min", rng, rng.Min, null, repo,
                    v =>
                    {
                        rng.Min = v as string;
                        this.AddDiaryEntry(rng, new DiaryEntryUpdateValue());
                        return new AnyUiLambdaActionNone();
                    });

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return maxe; },
                            "The value of the maximum of the Range. " +
                                "Please provide a string representation (without quotes, '.' as decimal separator, " +
                                "in XML number representation). " +
                                "If the min value is missing then the value is assumed to be positive infinite.",
                            severityLevel: HintCheck.Severity.Notice)
                    });

                AddKeyValueExRef(
                    stack, "max", rng, rng.Max, null, repo,
                    v =>
                    {
                        rng.Max = v as string;
                        this.AddDiaryEntry(rng, new DiaryEntryUpdateValue());
                        return new AnyUiLambdaActionNone();
                    });
            }
            else if (sme is Aas.File fl)
            {
                this.AddGroup(stack, "File", this.levelColors.MainSection);

                // refer to mini-module
                DisplayOrEditEntityFileResource(
                    stack, packages.Main,
                    fl, repo, superMenu,
                    fl.Value, fl.ContentType,
                    (fn, ct) =>
                    {
                        fl.Value = fn;
                        fl.ContentType = ct;
                    }, fl);
            }
            else if (sme is Aas.Blob blb)
            {
                this.AddGroup(stack, "Blob", this.levelColors.MainSection);

                // check, if this is binary

                var isBinary = !AdminShellUtil.CheckForTextContentType(blb.ContentType);
                if (AdminShellUtil.CheckIfAsciiOnly(blb.Value, bytesToCheck: 2048))
                    isBinary = false;

                // Value (depending on binary)

                if (!isBinary)
                {
                    // show text and let directly edit
                    AddKeyValue(
                        stack, "value", (blb.Value == null) ? "" : Encoding.Default.GetString(blb.Value),
                        nullValue: null, repo: repo,
                        setValue: v =>
                        {
                            blb.Value = Encoding.Default.GetBytes((string)v);
                            this.AddDiaryEntry(blb, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionNone();
                        },
                        valueHash: (blb.Value == null) ? 0 : blb.Value.GetHashCode(),
                        containingObject: blb,
                        limitToOneRowForNoEdit: true,
                        auxButtonTitles: new[] { "\u2261" },
                        auxButtonToolTips: new[] { "Edit in multiline editor" },
                        auxButtonLambda: (buttonNdx) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var uc = new AnyUiDialogueDataTextEditor(
                                                    caption: $"Edit Blob '{"" + blb.IdShort}'",
                                                    mimeType: blb.ContentType,
                                                    text: Encoding.Default.GetString(blb.Value ?? new byte[0]));
                                if (this.context.StartFlyoverModal(uc))
                                {
                                    blb.Value = Encoding.Default.GetBytes(uc.Text);
                                    this.AddDiaryEntry(blb, new DiaryEntryUpdateValue());
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        });

                    // Offer BASE64 to binary
                    if (AdminShellUtil.CheckIfAsciiOnly(blb.Value))
                    {
                        this.AddActionPanel(
                            stack, "Action",
                            repo: repo, superMenu: superMenu,
                            ticketMenu: new AasxMenu()
                                .AddAction("base64-to-binary", "BASE64 \u2192 binary",
                                    "Take value as BASE64 and convert to binary."),
                            ticketActionAsync: async (buttonNdx, ticket) =>
                            {
                                if (buttonNdx == 0)
                                {
                                    // ask
                                    if (AnyUiMessageBoxResult.Yes != await
                                            this.context.MessageBoxFlyoutShowAsync(
                                            "Convert? This operation cannot be reverted!",
                                            "BASE64 \u2192 binary",
                                            AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                        return new AnyUiLambdaActionNone();

                                    // do
                                    try
                                    {
                                        var strRep = Encoding.Default.GetString(blb.Value);
                                        var byteRep = System.Convert.FromBase64String(strRep);
                                        blb.Value = byteRep;
                                    } 
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex, "when converting BASE64 to binary.");
                                    }

                                    // show
                                    return new AnyUiLambdaActionRedrawEntity();
                                }

                                return new AnyUiLambdaActionNone();
                            });
                    }
                }
                else
                {
                    // show warnings!
                    var g = AddSmallGrid(1, 3, new[] { "#", "*", "#" });
                    stack.Add(g);
                    g.ColumnDefinitions[0].MinWidth = GetWidth(FirstColumnWidth.Standard);

                    AddSmallLabelTo(g, 0, 0, content: "value:",
                        margin: new AnyUiThickness(5, 0, 0, 0));
                    AddSmallLabelTo(g, 0, 1, content: "(This value seems to contain binary content of " +
                        $"{"" + blb.Value?.Length.ToString()} bytes.)",
                        margin: new AnyUiThickness(5, 0, 0, 0));

                    if (editMode)
                        AnyUiUIElement.RegisterControl(
                            AddSmallButtonTo(
                                g, 0, 2, content: "\u2261",
                                toolTip: "Edit in multiline editor",
                                margin: new AnyUiThickness(2),
                                padding: new AnyUiThickness(5, 0, 5, 0)),
                            setValueAsync: async (o) =>
                            {
                                if (AnyUiMessageBoxResult.Yes == await
                                        this.context.MessageBoxFlyoutShowAsync(
                                    "Edit value? Value seems to be binary data.",
                                    "Multiline editor",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                {
                                    var uc = new AnyUiDialogueDataTextEditor(
                                        caption: $"Edit Blob '{"" + blb.IdShort}'",
                                        mimeType: blb.ContentType,
                                        text: Encoding.Default.GetString(blb.Value ?? new byte[0]));
                                    if (await context.StartFlyoverModalAsync(uc))
                                    {
                                        blb.Value = Encoding.Default.GetBytes(uc.Text);
                                        this.AddDiaryEntry(blb, new DiaryEntryUpdateValue());
                                        return new AnyUiLambdaActionRedrawEntity();
                                    }
                                }
                                return new AnyUiLambdaActionNone();
                            });

                    // Offer binary to BASE64 
                    if (true)
                    {
                        this.AddActionPanel(
                            stack, "Action",
                            repo: repo, superMenu: superMenu,
                            ticketMenu: new AasxMenu()
                                .AddAction("binary-to-base64", "Binary \u2192 BASE64",
                                    "Take value as binary bytes and convert to BASE64."),
                            ticketActionAsync: async (buttonNdx, ticket) =>
                            {
                                if (buttonNdx == 0)
                                {
                                    // ask
                                    if (AnyUiMessageBoxResult.Yes != await
                                            this.context.MessageBoxFlyoutShowAsync(
                                            "Convert? This operation cannot be reverted!",
                                            "Binary \u2192 BASE64",
                                            AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                        return new AnyUiLambdaActionNone();

                                    // do
                                    try
                                    { 
                                        var strRep = System.Convert.ToBase64String(blb.Value);
                                        blb.Value = Encoding.Default.GetBytes(strRep);
                                    } 
                                    catch (Exception ex)
                                    {
                                        Log.Singleton.Error(ex, "when converting binary to BASE64.");
                                    }

                                    // show
                                    return new AnyUiLambdaActionRedrawEntity();
                                }

                                return new AnyUiLambdaActionNone();
                            });
                    }
                }

                // ContentType

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return blb.ContentType == null || blb.ContentType.Trim().Length < 1 ||
                                    blb.ContentType.IndexOf('/') < 1 || blb.ContentType.EndsWith("/");
                            },
                            "The content-type of the file. Also known as MIME type. " +
                            "See RFC2046.", severityLevel: HintCheck.Severity.Notice)
                    });

                AddKeyValueExRef(
                    stack, "contentType", blb, blb.ContentType, null, repo,
                    v =>
                    {
                        blb.ContentType = v as string;
                        this.AddDiaryEntry(blb, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: true,
                    comboBoxItems: AdminShellUtil.GetPopularMimeTypes());

                // Further file assistance

                if (editMode && uploadAssistance != null && packages.Main != null)
                {

                    this.AddGroup(stack, "File to blob assistance", this.levelColors.SubSection);

                    this.AddKeyDropTarget(
                        stack, "Source file to add",
                        !(this.uploadAssistance.SourcePath.HasContent())
                            ? "(Please drop a file to set source file to add)"
                            : this.uploadAssistance.SourcePath,
                        null, repo,
                        v =>
                        {
                            this.uploadAssistance.SourcePath = v as string;
                            return new AnyUiLambdaActionRedrawEntity();
                        }, minHeight: 40);

                    this.AddActionPanel(
                        stack, "Action",
                        repo: repo, superMenu: superMenu,
                        ticketMenu: new AasxMenu()
                            .AddAction("select-source", "Select source file",
                                "Select a filename to be added later.")
                            .AddAction("add-to-blob", "Add to Blob",
                                "Add or update blob value from given file.")
                            .AddAction("clear-blob", "Clear Blob",
                                "Clear blob value."),
                        ticketAction: (buttonNdx, ticket) =>
                        {
                            if (buttonNdx == 0)
                            {
                                var uc = new AnyUiDialogueDataOpenFile(
                                message: "Select a file to add..");
                                this.context?.StartFlyoverModal(uc);
                                if (uc.Result && uc.TargetFileName != null)
                                {
                                    this.uploadAssistance.SourcePath = uc.TargetFileName;
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }

                            if (buttonNdx == 1)
                            {
                                try
                                {
                                    // read file
                                    Log.Singleton.Info("Add to blob: reading {0} .. ", uploadAssistance.SourcePath);
                                    var data = System.IO.File.ReadAllBytes(uploadAssistance.SourcePath);

                                    // put it into blob (binary wise)
                                    blb.Value = data;
                                }
                                catch (Exception ex)
                                {
                                    Log.Singleton.Error(
                                        ex, $"Adding file {uploadAssistance.SourcePath} to blob");
                                }

                                // refresh dialogue
                                uploadAssistance.SourcePath = "";
                                return new AnyUiLambdaActionRedrawEntity();
                            }

                            if (buttonNdx == 2)
                            {
                                if (AnyUiMessageBoxResult.Yes == context.MessageBoxFlyoutShow(
                                    "Clear value? This operation cannot be reverted.",
                                    "Blob",
                                    AnyUiMessageBoxButton.YesNo, AnyUiMessageBoxImage.Warning))
                                {
                                    blb.Value = new byte[] { };

                                    // refresh dialogue
                                    uploadAssistance.SourcePath = "";
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }

                            return new AnyUiLambdaActionNone();
                        });
                }
            }
            else if (sme is Aas.ReferenceElement rfe)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "ReferenceElement", this.levelColors.MainSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rfe.Value == null || rfe.Value.IsEmpty(); },
                            "Please choose the target of the reference. " +
                                "You refer to any IReferable, if local within the AAS environment or outside. " +
                                "The semantics of your reference shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rfe.Value, "Target reference:", "Create data element!",
                        v =>
                        {
                            rfe.Value = new Aas.Reference(Aas.ReferenceTypes.ExternalReference, new List<Aas.IKey>());
                            this.AddDiaryEntry(rfe, new DiaryEntryUpdateValue());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    Func<List<Aas.IKey>, AnyUiLambdaActionBase> lambda = (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(
                            new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)), translateAssetToAAS: true);
                    };
                    this.AddKeyReference(stack, "value", 
                        rfe.Value, () => rfe.Value = null,
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All", // no restriction
                        addFromKnown: true,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rfe,
                        showRefSemId: true, // in this case, show also the referenced semId!!
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }
            }
            else
            if (sme is Aas.IRelationshipElement rele)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "" + sme.GetSelfDescription().AasElementName, this.levelColors.MainSection);

                // re-use lambda
                Func<List<Aas.IKey>, AnyUiLambdaActionBase> lambda = (kl) =>
                {
                    return new AnyUiLambdaActionNavigateTo(
                        new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)), translateAssetToAAS: true);
                };

                // members

                // First

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rele.First == null || rele.First.IsEmpty(); },
                            "Please choose the first element of the relationship. " +
                                "In terms of a semantic triple, it would be the subject. " +
                                "The semantics of your reference (the predicate) shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rele.First, "First relation:", "Create w/ default!",
                        v =>
                        {
                            rele.First = Options.Curr.GetDefaultEmptyReference();
                            this.AddDiaryEntry(rele, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyReference(
                        stack, "first", 
                        //rele.First, () => rele.First = Options.Curr.GetDefaultEmptyReference(),                        
                        rele.First, () => rele.First = null,                        
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All", // no restriction
                        addFromKnown: true,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rele,
                        showRefSemId: true, // in this case, show also the referenced semId!!
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }

                // small space in between
                AddVerticalSpace(stack, height: 12);

                // Second

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return rele.Second == null || rele.Second.IsEmpty(); },
                            "Please choose the second element of the relationship. " +
                                "In terms of a semantic triple, it would be the object. " +
                                "The semantics of your reference (the predicate) shall be described " +
                                "by the concept referred by semanticId.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, rele.Second, "Second relation:", "Create w/ default!",
                        v =>
                        {
                            rele.Second = Options.Curr.GetDefaultEmptyReference();
                            this.AddDiaryEntry(rele, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    this.AddKeyReference(
                        stack, "second", 
                        //rele.Second, () => rele.Second = Options.Curr.GetDefaultEmptyReference(),
                        rele.Second, () => rele.Second = null,
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAuxFileRepo,
                        addExistingEntities: "All", // no restriction
                        addFromKnown: true,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: lambda, noEditJumpLambda: lambda,
                        relatedReferable: rele,
                        showRefSemId: true, // in this case, show also the referenced semId!!
                        emitCustomEvent: (rf) => { this.AddDiaryEntry(rf, new DiaryEntryUpdateValue()); });
                }

                // specifically for annotated relationship?
                if (sme is Aas.AnnotatedRelationshipElement /* arele */)
                {
                }
            }
            else if (sme is Aas.Capability)
            {
                this.AddGroup(stack, "Capability", this.levelColors.MainSection);
                this.AddKeyValue(stack, "Value", "Right now, Capability does not have further value elements.");
            }
            else if (sme is Aas.SubmodelElementCollection smc)
            {
                this.AddGroup(stack, "SubmodelElementCollection", this.levelColors.MainSection);
                if (smc.Value != null)
                    this.AddKeyValue(stack, "# of values", "" + smc.Value.Count);
                else
                    this.AddKeyValue(stack, "Values", "Please add elements via editing of sub-ordinate entities");
            }
            else if (sme is Aas.SubmodelElementList sml)
            {
                this.AddGroup(stack, "SubmodelElementList", this.levelColors.MainSection);
                if (sml.Value != null)
                    this.AddKeyValue(stack, "# of values", "" + sml.Value.Count);
                else
                    this.AddKeyValue(stack, "Values", "Please add elements via editing of sub-ordinate entities");

                this.AddSmallCheckBox(
                   stack, "orderRelevant:", sml.OrderRelevant ?? false, 
                   additionalInfo: " (true if order in list is relevant)",
                   setValue: (b) => { sml.OrderRelevant = b; return new AnyUiLambdaActionNone(); });

                // stats
                var stats = sml.EvalConstraintStat();

                // type of the items of the list

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => stats?.AllChildSmeTypeMatch == false,
                            "Constraint AASd-108 violated: All first level child elements in a " +
                            "SubmodelElementList shall have the same submodel element type as specified " +
                            "in SubmodelElementList/typeValueListElement.")
                    });
                this.AddKeyValueExRef(
                    stack, "typeValueListElement", sml, Aas.Stringification.ToString(sml.TypeValueListElement),
                    null, repo,
                    v =>
                    {
                        var tvle = Aas.Stringification.AasSubmodelElementsFromString(v as string);
                        if (tvle.HasValue)
                            sml.TypeValueListElement = tvle.Value;
                        this.AddDiaryEntry(sml, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: editMode, comboBoxMinWidth: 190,
                    comboBoxItems: Enum.GetNames(typeof(Aas.AasSubmodelElements)));

                // ValueType for the list

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => stats?.AllChildValueTypeMatch == false,
                            "Constraint AASd-109 violated: If SubmodelElementList/typeValueListElement " +
                            "equal to Aas.Property or Aas.Range, SubmodelElementList/valueTypeListElement shall " +
                            "be set and all first level child elements in the SubmodelElementList shall " +
                            "have the the value type as specified in SubmodelElementList/valueTypeListElement")
                    });
                this.AddKeyValueExRef(
                    stack, "valueTypeListElement", sml, Aas.Stringification.ToString(sml.ValueTypeListElement),
                    null, repo,
                    v =>
                    {
                        sml.ValueTypeListElement = Aas.Stringification.DataTypeDefXsdFromString((string)v);
                        this.AddDiaryEntry(sml, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxIsEditable: editMode, comboBoxMinWidth: 190,
                    comboBoxItems: ExtendStringification.DataTypeXsdToStringArray().ToArray());

                // SemanticId for the list

                this.AddHintBubble(
                   stack, hintMode,
                   new[] {
                        new HintCheck(
                            () => stats?.AllChildSemIdMatch == false,
                            "Constraint AASd-107 violated: If a first level child element in a " +
                            "SubmodelElementList has a semanticId it shall be identical to " +
                            "SubmodelElementList/semanticIdListElement.")
                   });

                // do not use the DisplayOrEditEntitySemanticId(), but use native functions

                // add from Copy Buffer
                var bufferKeys = CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // add the keys
                if (this.SafeguardAccess(
                        stack, repo, sml.SemanticIdListElement, "semanticIdListElement:", "Create w/ default!",
                        v =>
                        {
                            sml.SemanticIdListElement = Options.Curr.GetDefaultEmptyReference();
                            this.AddDiaryEntry(sml, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                    AddKeyReference(
                        stack, "semanticIdListElement", 
                        sml.SemanticIdListElement, () => sml.SemanticIdListElement = null,
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.MainAux,
                        showRefSemId: false,
                        addExistingEntities: "Submodel SubmodelElement ConceptDescription ", addFromKnown: true,
                        modifyAddExistingKey: (inRefs) =>
                        {
                            var outRefs = inRefs.Copy();

                            // allow also to select SMEs to get the semantic key
                            var rf = packages.FindAllReferablesWith(inRefs)?.FirstOrDefault();
                            if (rf != null && !(rf is Aas.IConceptDescription)
                                && rf is Aas.IHasSemantics ihs
                                && ihs.GetConceptDescriptionId() is string ihsid)
                            {
                                // modify keys
                                outRefs = new Aas.Reference(ReferenceTypes.ExternalReference,
                                    keys: new Aas.IKey[] {
                                    new Aas.Key(KeyTypes.GlobalReference, ihsid)
                                }.ToList());
                            }

                            return outRefs;
                        },
                        addEclassIrdi: true,
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: (kl) =>
                        {
                            return new AnyUiLambdaActionNavigateTo(sml.SemanticIdListElement);
                        },
                        relatedReferable: sml,
                        auxContextHeader: new[] { "\U0001F796", "Auto-detect" },
                        auxContextLambda: (i) =>
                        {
                            if (i == 0)
                            {
                                // check for underlying SMCs
                                if (sml.Value != null && sml.Value.Count > 0
                                    && sml.Value.First() is Aas.ISubmodelElementCollection subsmc
                                    && subsmc.SemanticId?.IsValid() == true)
                                {
                                    sml.SemanticIdListElement = subsmc.SemanticId.Copy();

                                    this.AddDiaryEntry(sml, new DiaryEntryStructChange());
                                    return new AnyUiLambdaActionRedrawEntity();
                                }
                            }
                            return new AnyUiLambdaActionNone();
                        });

            }
            else if (sme is Aas.Operation)
            {
                var p = sme as Aas.Operation;
                this.AddGroup(stack, "Operation", this.levelColors.MainSection);
                if (p.InputVariables != null)
                    this.AddKeyValue(stack, "# of input vars.", "" + p.InputVariables.Count);
                if (p.OutputVariables != null)
                    this.AddKeyValue(stack, "# of output vars.", "" + p.OutputVariables.Count);
                if (p.InoutputVariables != null)
                    this.AddKeyValue(stack, "# of in/out vars.", "" + p.InoutputVariables.Count);
            }
            else if (sme is Aas.Entity)
            {
                var ent = sme as Aas.Entity;
                this.AddGroup(stack, "Entity", this.levelColors.MainSection);

                if (ent.Statements != null)
                    this.AddKeyValue(stack, "# of statements", "" + ent.Statements.Count);
                else
                    this.AddKeyValue(
                        stack, "Statements", "Please add statements via editing of sub-ordinate entities");

                // EntityType
                // dead-csharp off
                //is not nullable!
                //this.AddHintBubble(
                //    stack, hintMode,
                //    new[] {
                //        new HintCheck(
                //            () => {
                //                return ent?.EntityType == null;

                //            },
                //            "EntityType needs to be either CoManagedEntity (no assigned Aas.AssetInformation reference) " +
                //                "or SelfManagedEntity (with assigned Aas.AssetInformation reference)",
                //            severityLevel: HintCheck.Severity.High)
                //    });
                // dead-csharp on
                AddKeyValueExRef(
                    stack, "entityType", ent, Aas.Stringification.ToString(ent.EntityType), null, repo,
                    v =>
                    {
                        ent.EntityType = (Aas.EntityType)Aas.Stringification.EntityTypeFromString((string)v);
                        this.AddDiaryEntry(ent, new DiaryEntryStructChange());
                        return new AnyUiLambdaActionNone();
                    },
                    comboBoxItems: Enum.GetNames(typeof(Aas.EntityType)),
                    comboBoxIsEditable: true);

                // GlobalAssetId

                AddGroup(stack, "GlobalAssetId (in case of self managed asset, preferred)", levelColors.SubSection);

                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => {
                                return ent?.EntityType != null &&
                                    ent.EntityType == Aas.EntityType.SelfManagedEntity &&
                                    (string.IsNullOrEmpty(ent.GlobalAssetId));
                            },
                            "Please choose the global identifier for the SelfManagedEntity.",
                            severityLevel: HintCheck.Severity.Notice)
                    });
                if (this.SafeguardAccess(
                        stack, repo, ent.GlobalAssetId, "globalAssetId:", "Create data element!",
                        v =>
                        {
                            ent.GlobalAssetId = "";
                            this.AddDiaryEntry(ent, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    Func<List<Aas.IKey>, AnyUiLambdaActionBase> lambda = (kl) =>
                    {
                        return new AnyUiLambdaActionNavigateTo(
                            new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)), translateAssetToAAS: true);
                    };
                    //TODO (jtikekar, 0000-00-00): check with Micha
                    // dead-csharp off
                    this.AddKeyValueExRef(stack, "globalAssetId", ent, ent.GlobalAssetId, null, repo,
                        v =>
                        {
                            ent.GlobalAssetId = v as string;
                            this.AddDiaryEntry(ent, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionNone();
                        });
                }

                this.DisplayOrEditEntityListOfSpecificAssetIds(stack, ent.SpecificAssetIds,
                                (ico) => { ent.SpecificAssetIds = ico; },
                                key: "specificAssetId",
                                relatedReferable: ent);
            }
            else if (sme is Aas.BasicEventElement bev)
            {
                // buffer Key for later
                var bufferKeys = DispEditHelperCopyPaste.CopyPasteBuffer.PreparePresetsForListKeys(theCopyPaste);

                // group
                this.AddGroup(stack, "BasicEvent", this.levelColors.MainSection);

                // attributed
                this.AddHintBubble(
                    stack, hintMode,
                    new[] {
                        new HintCheck(
                            () => { return bev.Observed == null || bev.Observed.IsEmpty(); },
                                "Please choose the Referabe, e.g. Aas.Submodel, SubmodelElementCollection or " +
                                "DataElement which is being observed. " + System.Environment.NewLine +
                                "You could refer to any IReferable, however it recommended restrict the scope " +
                                "to the local AAS or even within a Submodel.",
                            severityLevel: HintCheck.Severity.Notice)
                });
                if (this.SafeguardAccess(
                        stack, repo, bev.Observed, "observed:", "Create data element!",
                        v =>
                        {
                            bev.Observed = new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>());
                            this.AddDiaryEntry(bev, new DiaryEntryStructChange());
                            return new AnyUiLambdaActionRedrawEntity();
                        }))
                {
                    // hint
                    AddHintBubble(
                        stack, hintMode,
                        new HintCheck(
                            () => bev.Observed.Keys != null && bev.Observed.IsValid() != true,
                            "According to the specification, an existing list of elements shall contain " +
                            "at least one element and for each element all mandatory fields shall be " +
                            "not empty."));

                    // keys
                    this.AddKeyListKeys(stack, "observed",
                        bev.Observed.Keys, () => bev.Observed = Options.Curr.GetDefaultEmptyModelReference(),
                        repo,
                        packages, PackageCentral.PackageCentral.Selector.Main, 
                        addExistingEntities: "All",
                        addPresetNames: bufferKeys.Item1,
                        addPresetKeyLists: bufferKeys.Item2,
                        jumpLambda: (kl) =>
                        {
                            return new AnyUiLambdaActionNavigateTo(new Aas.Reference(Aas.ReferenceTypes.ModelReference, new List<Aas.IKey>(kl)));
                        },
                        relatedReferable: bev);
                }

                // group
                this.AddGroup(stack, "Invocation of Events", this.levelColors.SubSection);

                this.AddActionPanel(
                    stack, "Emit Event:",
                    repo: repo, superMenu: superMenu,
                    ticketMenu: new AasxMenu()
                        .AddAction("emit-direct", "Emit directly",
                            "Emits selected event without user-defined payload.")
                        .AddAction("emit-json", "Emit with JSON payload",
                            "Emits selected event after editing user-defined payload."),
                    addWoEdit: new[] { true, true },
                    ticketAction: (buttonNdx, ticket) =>
                    {
                        string PayloadsRaw = null;

                        if (buttonNdx == 1)
                        {
                            var uc = new AnyUiDialogueDataTextEditor(
                                                caption: $"Edit raw Payload for '{"" + bev.IdShort}'",
                                                mimeType: "application/json",
                                                text: "[]");
                            if (this.context.StartFlyoverModal(uc))
                            {
                                PayloadsRaw = uc.Text;
                            }
                        }

                        if (buttonNdx == 0 || buttonNdx == 1)
                        {
                            // find the observable)
                            var observable = env.FindReferableByReference(bev.Observed);

                            // send event
                            var ev = new AasEventMsgEnvelope(
                                DateTime.UtcNow,
                                source: bev.GetModelReference(),
                                sourceSemanticId: bev.SemanticId,
                                observableReference: bev.Observed,
                                observableSemanticId: (observable as Aas.IHasSemantics)?.SemanticId);

                            // specific payload?
                            if (PayloadsRaw != null)
                            {
                                ev.PayloadItems = null;
                                ev.PayloadsRaw = PayloadsRaw;
                            }

                            // emit it to PackageCentral
                            packages?.PushEvent(ev);

                            return new AnyUiLambdaActionNone();
                        }

                        return new AnyUiLambdaActionNone();
                    });
            }
            else
                this.AddGroup(stack, "SubmodelElement is unknown!", this.levelColors.MainSection);
        }


        /// <summary>
        /// Super function to basically edit all known visual elements.
        /// Note: With hesitation, the <c>mainWindow</c> is passed into this function and shall only 
        ///       be used in exceptional cases.
        /// Note: Because of Blazor principles for display of components, this function or its subordinates 
        ///       MUST NOT be async!
        /// </summary>
        public bool DisplayOrEditCommonEntity(
            PackageCentral.PackageCentral packages,
            AnyUiStackPanel stack,
            AasxMenu superMenu,
            bool editMode, bool hintMode, bool checkSmt,
			VisualElementEnvironmentItem.ConceptDescSortOrder? cdSortOrder,
            VisualElementGeneric entity,
            IMainWindow mainWindow)
        {
            if (entity is VisualElementEnvironmentItem veei)
            {
                DisplayOrEditAasEntityAasEnv(
                    packages, veei.theEnv, veei, editMode, stack, hintMode: hintMode,
                    superMenu: superMenu, mainWindow: mainWindow);
            }
            else if (entity is VisualElementAdminShell veaas)
            {
                DisplayOrEditAasEntityAas(
                    packages, veaas.thePackage, veaas.theEnv, 
                    veaas.theAas, 
                    editMode, stack, hintMode: hintMode,
                    superMenu: superMenu);
            }
            else if (entity is VisualElementAsset veas)
            {
                DisplayOrEditAasEntityAssetInformation(
                    packages, veas.theEnv, veas.theAas, veas.theAsset, veas.theAsset,
                    editMode, repo, stack, hintMode: hintMode,
                    superMenu: superMenu);
            }
            else if (entity is VisualElementSubmodelRef vesmref)
            {
                // data
                Aas.IAssetAdministrationShell aas = null;
                if (vesmref.Parent is VisualElementAdminShell xpaas)
                    aas = xpaas.theAas;

                // edit
                DisplayOrEditAasEntitySubmodelOrRef(
                    packages, vesmref.thePackage, vesmref.theEnv, aas, 
                    vesmref.theSubmodelRef, 
                    () =>
                    {
                        vesmref.theAas.Remove(vesmref.theSubmodelRef);
                    },
                    vesmref.theSubmodel,
                    editMode, stack,
                    hintMode: hintMode, checkSmt: checkSmt,
					superMenu: superMenu);
            }
            else if (entity is VisualElementSubmodel vesm && vesm.theSubmodel != null)
            {
                DisplayOrEditAasEntitySubmodelOrRef(
                    packages, vesm.thePackage, vesm.theEnv, 
                    aas: null, smref: null, setSmRefNull: null, 
                    submodel: vesm.theSubmodel, 
                    editMode: editMode, stack: stack,
                    hintMode: hintMode, checkSmt: checkSmt,
					superMenu: superMenu);
            }
            else if (entity is VisualElementSubmodelStub vesms && vesms.theSideInfo != null)
            {
                DisplayOrEditAasEntitySubmodelStub(
                    packages, vesms.thePackEnv,
                    aas: null, smref: null, setSmRefNull: null,
                    sideInfo: vesms.theSideInfo, editMode: editMode, stack: stack,
                    hintMode: hintMode, checkSmt: checkSmt,
                    superMenu: superMenu);
            }
            else if (entity is VisualElementSubmodelElement vesme)
            {
                DisplayOrEditAasEntitySubmodelElement(
                    packages, vesme.theEnv, vesme.theContainer, vesme.theWrapper, vesme.theWrapper,
                    vesme.IndexPosition, editMode,
                    repo, stack, hintMode: hintMode, checkSmt: checkSmt, superMenu: superMenu,
                    nestedCds: cdSortOrder.HasValue &&
                        cdSortOrder.Value == VisualElementEnvironmentItem.ConceptDescSortOrder.BySme);
            }
            else if (entity is VisualElementOperationVariable vepv)
            {
                DisplayOrEditAasEntityOperationVariable(
                    packages, vepv.theEnv, vepv.theContainer, vepv.theOpVar, editMode,
                    stack, hintMode: hintMode,
                    superMenu: superMenu);
            }
            else if (entity is VisualElementConceptDescription vecd)
            {
                DisplayOrEditAasEntityConceptDescription(
                    packages, vecd.theEnv, null, 
                    vecd.theCD, 
                    editMode, repo, stack, hintMode: hintMode,
                    superMenu: superMenu,
                    preventMove: cdSortOrder.HasValue &&
                        cdSortOrder.Value != VisualElementEnvironmentItem.ConceptDescSortOrder.None);
            }
            else if (entity is VisualElementValueRefPair vevlp)
            {
                DisplayOrEditAasEntityValueReferencePair(
                    packages, vevlp.theEnv, null, vevlp.theCD, vevlp.theVLP,
                    editMode, repo, stack, hintMode: hintMode);
            }
            else
            if (entity is VisualElementSupplementalFile vesf)
            {
                DisplayOrEditAasEntitySupplementaryFile(packages, vesf, vesf.theFile, editMode, stack,
                    superMenu: superMenu);
            }
            else
            {
                // not found!
                return false;
            }

            // add common footer
            // Background: when editing near the footer of the scroll panel, the vertical
            // scroll tends to sit a little above the last element of interest and the user
            // is to required always scroll a littple bit down
            if (stack?.Children != null && stack.Children.Count > 0)
            {
                stack.Add(new AnyUiLabel() { Content = "" });
                stack.Add(new AnyUiLabel() { Content = "" });
                stack.Add(new AnyUiLabel() { Content = "" });
            }

            // one of the upper cases
            return true;
        }
    }
}
