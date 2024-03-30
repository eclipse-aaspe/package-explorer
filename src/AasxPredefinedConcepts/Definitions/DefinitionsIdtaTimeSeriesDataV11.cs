/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using Aas = AasCore.Aas3_0;

// ReSharper disable UnassignedField.Global
// (working by reflection)

namespace AasxPredefinedConcepts
{
    /// <summary>
    /// Definitions of Submodel Basic model for the modeling of time series data (ZVEI) v1.0
    /// </summary>
    public class IdtaTimeSeriesDataV11 : AasxDefinitionBase
    {
        public static IdtaTimeSeriesDataV11 Static = new IdtaTimeSeriesDataV11();

        public Aas.Submodel
            SM_TimeSeriesData;

        public Aas.ConceptDescription
            CD_Metadata,
            CD_Name,
            CD_Description,
            CD_Record,
            CD_RelativeTimePoint,
            CD_Segments,
            CD_ExternalSegment,
            CD_RecordCount,
            CD_StartTime,
            CD_EndTime,
            CD_Duration,
            CD_SamplingInterval,
            CD_SamplingRate,
            CD_State,
            CD_LastUpdate,
            CD_File,
            CD_Blob,
            CD_LinkedSegment,
            CD_Endpoint,
            CD_Query,
            CD_InternalSegment,
            CD_Records;

        public IdtaTimeSeriesDataV11()
        {
            // info
            this.DomainInfo = "Time series data (IDTA) V1.1";

            // IReferable
            this.ReadLibrary(
                Assembly.GetExecutingAssembly(), "AasxPredefinedConcepts.Resources." + "IdtaTimeSeriesDataV11.json");
            this.RetrieveEntriesFromLibraryByReflection(typeof(IdtaTimeSeriesDataV11), useFieldNames: true);
        }
    }
}
