/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.ConceptModel;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using ScottPlot;
using static AasxPluginPlotting.PlottingViewControl;
using System.IO.Packaging;
using System.Windows.Media.Media3D;
using System.Runtime.Intrinsics.Arm;

namespace AasxPluginPlotting
{
    /// <summary>
    /// This class reads Time Series V1.0 data into plot items.
    /// </summary>
    public class TimeSeriesReaderV11 : TimeSeriesReaderBase
    {
        protected IdtaTimeSeriesDataV11 pcts = IdtaTimeSeriesDataV11.Static;        

        protected Tuple<TimeSeriesTimeAxis, Aas.IProperty> DetectTimeSpecifier(
            Aas.IProperty prop,
            MatchMode mm)
        {
            // access
            // needs to have a semanticId 
            if (prop?.SemanticId?.IsValid() != true || pcts == null)
                return null;

            // needs to have an appropriate name
            if (prop.IdShort?.HasContent() != true || !prop.IdShort.ToLower().StartsWith("time"))
                return null;
            
            // detect
            if (prop.SemanticId.Matches(pcts.CD_UtcTimeIdta, mm)
                || prop.SemanticId.Matches(pcts.CD_UtcTimeIecCdd, mm))
                return new Tuple<TimeSeriesTimeAxis, Aas.IProperty>(TimeSeriesTimeAxis.Utc, prop);

            if (prop.SemanticId.Matches(pcts.CD_TaiTimeIdta, mm)
                || prop.SemanticId.Matches(pcts.CD_TaiTimeIecCdd, mm))
                return new Tuple<TimeSeriesTimeAxis, Aas.IProperty>(TimeSeriesTimeAxis.Tai, prop);

            if (prop.SemanticId.Matches(pcts.CD_RelativePointInTime, mm)
                || prop.SemanticId.Matches(pcts.CD_RelativeTimePoint, mm))
                return new Tuple<TimeSeriesTimeAxis, Aas.IProperty>(TimeSeriesTimeAxis.Plain, prop);

            if (prop.SemanticId.Matches(pcts.CD_RelativeTimeDurationIdta, mm)
                || prop.SemanticId.Matches(pcts.CD_RelativeTimeDurationIecCdd, mm))
                return new Tuple<TimeSeriesTimeAxis, Aas.IProperty>(TimeSeriesTimeAxis.Duration, prop);

            // no
            return null;
        }

        /// <summary>
        /// This functions add new data from a segment.
        /// In case of no data set existing, this function will add new datasets, as well.
        /// It is able to understand time series record and variable modes.
        /// </summary>
        public override void TimeSeriesAddSegmentData(
            Aas.IEnvironment env,
            MatchMode mm,
            TimeSeriesData tsd,
            Aas.ISubmodelElementCollection smcseg)
        {
            // access
            if (pcts == null || smcseg?.SemanticId?.IsValid() != true)
                return;

            // add after
            int dataIndex = tsd.DataSet.GetMaxIndex();

            // find records for internal segment?
            if (smcseg.SemanticId.Matches(pcts.CD_InternalSegment, mm))
            {
                // find records list
                var recsEnum = smcseg.Value.FindFirstSemanticIdAsEnumerable(pcts.CD_Records, mm);
                if (recsEnum?.Value == null)
                    return;

                foreach (var smcrec in recsEnum.Value.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                    pcts.CD_Record, mm))
                {
                    // if the very first record, may add tsd meta data
                    if (dataIndex == 0 && (tsd.DataSet == null || tsd.DataSet.Count < 1))
                    {
                        TimeSeriesStartColumns(env, tsd, smcrec);
                    }

                    // make it bullet proof!
                    if (tsd.DataSet == null || tsd.DataSet.Count < 1)
                    {
                        return;
                    }

                    // simply find all elements of the record
                    foreach (var smevar in smcrec.Value)
                    {
                        // is time element?
                        TimeSeriesDataSet ds = null;
                        var timeSpec = DetectTimeSpecifier(smevar as Aas.IProperty, MatchMode.Relaxed);
                        if (timeSpec != null)
                        {
                            // find the data set to put in
                            ds = tsd.DataSet.FindAllTimeDatSet(timeSpec.Item1).FirstOrDefault();
                            if (ds == null)
                                continue;

                            // convert?
                            var tm = SpecifiedTimeToDouble(timeSpec.Item1, timeSpec.Item2.Value);
                            if (!tm.HasValue)
                                continue;

                            // add
                            ds.DataAdd(dataIndex, tm.Value);

                            // ok
                            continue;
                        }

                        // normal element
                        if (!(smevar is Aas.IProperty propVar))
                            continue;
                        
                        // now access the value of the data point as float value
                        if (!double.TryParse(propVar.Value, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out var dataValue))
                            continue;

                        // add
                        ds.DataAdd(dataIndex, dataValue);
                    }

                    // add to next sample
                    dataIndex++;
                }
            }

            // find records for external segment?
            if (smcseg.SemanticId.Matches(pcts.CD_ExternalSegment, mm))
            {
            }
        }

        protected void TimeSeriesStartColumns(
            Aas.IEnvironment env,
            TimeSeriesData tsd, 
            Aas.ISubmodelElementCollection smcrec)
        {
            // access
            if (tsd == null || smcrec?.Value == null)
                return;
            
            // the record shall only have one "primary" data set 
            // (otherwise than stated in the spec)
            TimeSeriesDataSet primaryTimeDS = null;

            // simply find all elements of the record
            foreach (var smevar in smcrec.Value)
            {
                // the dataset id is?
                var dsid = "" + smevar.IdShort;
                if (!dsid.HasContent())
                    continue;

                // query avilable information on the time
                var timeSpec = DetectTimeSpecifier(smevar as Aas.IProperty, MatchMode.Relaxed);

                // already have a dataset with that id .. ignore
                var ds = tsd.FindDataSetById(dsid);
                if (ds != null)
                    continue;

                // add
                ds = new TimeSeriesDataSet() { DataSetId = dsid };
                tsd.DataSet.Add(ds);

                // find a DataPoint description? .. store it!
                if (ds.DataPoint == null)
                {
                    ds.DataPoint = smevar;
                    ds.DataPointCD = env?.FindConceptDescriptionByReference(smevar.SemanticId);
                }

                // plot arguments for datapoint?
                ds.Args = PlotArguments.Parse(smevar.HasExtensionOfName("TimeSeries.Args")?.Value);

                // time
                if (timeSpec != null)
                {
                    // remember primary time ds
                    primaryTimeDS = primaryTimeDS ?? ds;

                    // make time axis
                    ds.TimeAxis = timeSpec.Item1;

                    // now fix (one time!) the time data set for this data set
                    if (!tsd.TimeDsLookup.ContainsKey(timeSpec.Item1))
                        tsd.TimeDsLookup[timeSpec.Item1] = ds;

                    continue;
                }

                // no time
                ;
            }

            // assign all non-time data sets to the primary data set
            foreach (var ds in tsd.DataSet)
                ds.AssignedTimeDS = primaryTimeDS;
        }

        public override ListOfTimeSeriesData TimeSeriesStartFromSubmodel(
            Aas.IEnvironment env, Aas.Submodel sm)
        {
            // access
            var res = new ListOfTimeSeriesData();
            if (sm?.SubmodelElements == null)
                return res;
            var mm = MatchMode.Relaxed;

            // make initial data for time series
            var tsd = new TimeSeriesData() { SourceTimeSeries = sm };
            res.Add(tsd);

            // find args attached to the Submodel itself
            tsd.Args = PlotArguments.Parse(sm.HasExtensionOfName("TimeSeries.Args")?.Value);

            // find SMC for Metadata and (optional) record for the metadata
            var smcmd = sm.SubmodelElements.FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(
                    pcts.CD_Metadata, mm);
            if (smcmd != null)
            {
                // 2nd chance for arguments in metadata
                tsd.Args = tsd.Args ?? PlotArguments.Parse(smcmd.HasExtensionOfName("TimeSeries.Args")?.Value);

                // check for a record
                var smcr0 = smcmd.Value.FindFirstSemanticIdAs<Aas.ISubmodelElementCollection>(
                    pcts.CD_Record, mm);

                // set columns
                if (smcr0?.Value != null)
                    TimeSeriesStartColumns(env, tsd, smcr0);
            }

            // find SMC/SML for segments
            foreach (var segs in sm.SubmodelElements.FindAllSemanticIdAsEnumerable(pcts.CD_Segments, mm))
            {
                // access
                if (segs?.Sme == null || segs.Value == null)
                    continue;

                // find ALL kind of segements
                var segEnum = segs.Value.FindAllSemanticIdAs<Aas.ISubmodelElementCollection>(
                        pcts.CD_InternalSegment, mm).Concat(
                    segs.Value.FindAllSemanticIdAs<Aas.ISubmodelElementCollection>(
                        pcts.CD_ExternalSegment, mm)).Concat(
                    segs.Value.FindAllSemanticIdAs<Aas.ISubmodelElementCollection>(
                        pcts.CD_LinkedSegment, mm));

                foreach (var seg in segEnum)
                {
                    TimeSeriesAddSegmentData(env, mm, tsd, seg);
                }
            }

            return res;
        }
    }
}
