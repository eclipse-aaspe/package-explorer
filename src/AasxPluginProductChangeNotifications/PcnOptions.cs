/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;

namespace AasxPluginProductChangeNotifications
{
    public class PcnOptionsRecord : AasxPluginOptionsLookupRecordBase
    {
        public enum VersionEnum { V10pre, V10 }

        public VersionEnum Version = VersionEnum.V10;
    }

    public class PcnOptions : AasxPluginLookupOptionsBase
    {
        public List<PcnOptionsRecord> Records = new List<PcnOptionsRecord>();

        /// <summary>
        /// Create a set of minimal options
        /// </summary>
        public static PcnOptions CreateDefault()
        {
            var defs = new DefinitionsMTP.ModuleTypePackage();

            var rec1 = new PcnOptionsRecord();
            rec1.Version = PcnOptionsRecord.VersionEnum.V10pre;
            rec1.AllowSubmodelSemanticId = new[] { 
                new Aas.Key(Aas.KeyTypes.Submodel, "0173-10029#01-XFB001#001") }.ToList();

            var rec2 = new PcnOptionsRecord();
            rec2.Version = PcnOptionsRecord.VersionEnum.V10;
            rec2.AllowSubmodelSemanticId = new[] {
                new Aas.Key(Aas.KeyTypes.Submodel, "0173-1#01-AHE582#003") }.ToList();

            var opt = new PcnOptions();
            opt.Records.Add(rec1);
            opt.Records.Add(rec2);

            return opt;
        }
    }
}
