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

namespace AasxPluginPlotting
{
    /// <summary>
    /// This class reads Time Series V1.0 data into plot items.
    /// </summary>
    public class TimeSeriesReaderV10 : TimeSeriesReaderBase
    {
        protected ZveiTimeSeriesDataV10 pcts = ZveiTimeSeriesDataV10.Static;        

        protected Tuple<TimeSeriesTimeAxis, Aas.Property> DetectTimeSpecifier(
            MatchMode mm,
            Aas.SubmodelElementCollection smc)
        {
            // access
            if (smc?.Value == null || pcts == null)
                return null;

            // detect
            Aas.Property prop = null;
            prop = smc.Value.FindFirstSemanticIdAs<Aas.Property>(pcts.CD_UtcTime.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, Aas.Property>(TimeSeriesTimeAxis.Utc, prop);

            prop = smc.Value.FindFirstSemanticIdAs<Aas.Property>(pcts.CD_TaiTime.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, Aas.Property>(TimeSeriesTimeAxis.Tai, prop);

            prop = smc.Value.FindFirstSemanticIdAs<Aas.Property>(pcts.CD_Time.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, Aas.Property>(TimeSeriesTimeAxis.Plain, prop);

            prop = smc.Value.FindFirstSemanticIdAs<Aas.Property>(pcts.CD_TimeDuration.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, Aas.Property>(TimeSeriesTimeAxis.Plain, prop);

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
            if (pcts == null || smcseg == null)
                return;

            // challenge is to select SMes, which are NOT from a known semantic id!
            var tsvAllowed = new[]
            {
                pcts.CD_RecordId.GetSingleKey(),
                pcts.CD_UtcTime.GetSingleKey(),
                pcts.CD_TaiTime.GetSingleKey(),
                pcts.CD_Time.GetSingleKey(),
                pcts.CD_TimeDuration.GetSingleKey(),
                pcts.CD_ValueArray.GetSingleKey(),
                pcts.CD_ExternalDataFile.GetSingleKey()
            };

            var tsrAllowed = new[]
            {
                pcts.CD_RecordId.GetSingleKey(),
                pcts.CD_UtcTime.GetSingleKey(),
                pcts.CD_TaiTime.GetSingleKey(),
                pcts.CD_Time.GetSingleKey(),
                pcts.CD_TimeDuration.GetSingleKey(),
                pcts.CD_ValueArray.GetSingleKey()
            };

            // find variables?
            foreach (var smcvar in smcseg.Value.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                pcts.CD_TimeSeriesVariable.GetReference(), mm))
            {
                // makes only sense with record id
                var recid = "" + smcvar.Value.FindFirstSemanticIdAs<Aas.Property>(
                    pcts.CD_RecordId.GetReference(), mm)?.Value?.Trim();
                if (recid.Length < 1)
                    continue;

                // add need a value array as well!
                var valarr = "" + smcvar.Value.FindFirstSemanticIdAs<Aas.Blob>(
                    pcts.CD_ValueArray.GetReference(), mm)?.Value;
                if (valarr.Length < 1)
                    continue;

                // already have a dataset with that id .. or make new?
                var ds = tsd.FindDataSetById(recid);
                if (ds == null)
                {
                    // add
                    ds = new TimeSeriesDataSet() { DataSetId = recid };
                    tsd.DataSet.Add(ds);

                    // at this very moment, check if this is a time series
                    var timeSpec = DetectTimeSpecifier(mm, smcvar);
                    if (timeSpec != null)
                        ds.TimeAxis = timeSpec.Item1;

                    // find a DataPoint description?
                    var pdp = smcvar.Value.FindFirstAnySemanticId<Aas.Property>(tsvAllowed, mm,
                        invertAllowed: true);
                    if (pdp != null && ds.DataPoint == null)
                    {
                        ds.DataPoint = pdp;
                        ds.DataPointCD = env?.FindConceptDescriptionByReference(pdp.SemanticId);
                    }

                    // plot arguments for record?
                    ds.Args = PlotArguments.Parse(smcvar.HasExtensionOfName("TimeSeries.Args")?.Value);
                }

                // now try add the value array
                ds.DataAdd(valarr, fillTimeGaps: ds.TimeAxis != TimeSeriesTimeAxis.None);
            }

            // find records?
            foreach (var smcrec in smcseg.Value.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                pcts.CD_TimeSeriesRecord.GetReference(), mm))
            {
                // makes only sense with a numerical record id
                var recid = "" + smcrec.Value.FindFirstSemanticIdAs<Aas.Property>(
                    pcts.CD_RecordId.GetReference(), mm)?.Value?.Trim();
                if (recid.Length < 1)
                    continue;
                if (!int.TryParse(recid, out var dataIndex))
                    continue;

                // to prevent attacks, restrict index
                if (dataIndex < 0 || dataIndex > 16 * 1024 * 1024)
                    continue;

                // but, in this case, the dataset id's and data comes from individual
                // data points
                foreach (var pdp in smcrec.Value.FindAllSemanticId<Aas.Property>(tsrAllowed,
                        invertedAllowed: true))
                {
                    // the dataset id is?
                    var dsid = "" + pdp.IdShort;
                    if (!dsid.HasContent())
                        continue;

                    // query avilable information on the time
                    var timeSpec = DetectTimeSpecifier(mm, smcrec);
                    if (timeSpec == null)
                        continue;

                    // already have a dataset with that id .. or make new?
                    var ds = tsd.FindDataSetById(dsid);
                    if (ds == null)
                    {
                        // add
                        ds = new TimeSeriesDataSet() { DataSetId = dsid };
                        tsd.DataSet.Add(ds);

                        // find a DataPoint description? .. store it!
                        if (ds.DataPoint == null)
                        {
                            ds.DataPoint = pdp;
                            ds.DataPointCD = env?.FindConceptDescriptionByReference(pdp.SemanticId);
                        }

                        // now fix (one time!) the time data set for this data set
                        if (tsd.TimeDsLookup.ContainsKey(timeSpec.Item1))
                            ds.AssignedTimeDS = tsd.TimeDsLookup[timeSpec.Item1];
                        else
                        {
                            // create this
                            ds.AssignedTimeDS = new TimeSeriesDataSet()
                            {
                                DataSetId = "Time_" + timeSpec.Item1.ToString()
                            };
                            tsd.TimeDsLookup[timeSpec.Item1] = ds.AssignedTimeDS;
                        }

                        // plot arguments for datapoint?
                        ds.Args = PlotArguments.Parse(pdp.HasExtensionOfName("TimeSeries.Args")?.Value);
                    }

                    // now access the value of the data point as float value
                    if (!double.TryParse(pdp.Value, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var dataValue))
                        continue;

                    // TimeDS and time is required
                    if (ds.AssignedTimeDS == null)
                        continue;

                    var tm = SpecifiedTimeToDouble(timeSpec.Item1, timeSpec.Item2.Value);
                    if (!tm.HasValue)
                        continue;

                    // ok, push the data into the dataset
                    ds.AssignedTimeDS.DataAdd(dataIndex, tm.Value);
                    ds.DataAdd(dataIndex, dataValue);
                }
            }
        }

        public override ListOfTimeSeriesData TimeSeriesStartFromSubmodel(
            Aas.IEnvironment env, Aas.Submodel sm)
        {
            // access
            var res = new ListOfTimeSeriesData();
            if (sm?.SubmodelElements == null)
                return res;
            var pcts = AasxPredefinedConcepts.ZveiTimeSeriesDataV10.Static;
            var mm = MatchMode.Relaxed;

            // find SMC for TimeSeries itself -> this will result in a plot
            foreach (var smcts in sm.SubmodelElements.FindAllSemanticIdAs<Aas.SubmodelElementCollection>(
                pcts.CD_TimeSeries.GetReference(), mm))
            {
                // make initial data for time series
                var tsd = new TimeSeriesData() { SourceTimeSeries = smcts };
                res.Add(tsd);

                // plot arguments for time series
                tsd.Args = PlotArguments.Parse(smcts.HasExtensionOfName("TimeSeries.Args")?.Value);

                var tssReference = pcts.CD_TimeSeriesSegment.GetReference();
                var smcAllValues = smcts.Value.
                    FindAllSemanticIdAs<Aas.SubmodelElementCollection>(tssReference, mm);

                // If we have a SubmodelCollection where the TimeSeries data is at the properties level
                // this loop iterates through it and adds the data to the time series plot. Otherwise, if no
                // SubmodelCollections were found (count = 0), it will look one level deeper and check if elements
                // from type SubmodelElementCollection are found there, adding them to the plot afterwards too.
                // resharper disable once PossibleMultipleEnumeration
                if (smcAllValues.Count() != 0)
                {
                    // find segments
                    // resharper disable once PossibleMultipleEnumeration
                    foreach (var smcseg in smcAllValues)
                    {
                        TimeSeriesAddSegmentData(env, mm, tsd, smcseg);
                    }
                }
                else
                {
                    foreach (var v in smcts.Value)
                    {
                        if (v is Aas.SubmodelElementCollection sme)
                        {
                            smcAllValues = sme.Value.
                                FindAllSemanticIdAs<Aas.SubmodelElementCollection>(tssReference, mm);
                            // find segements
                            foreach (var smcseg in smcAllValues)
                            {
                                TimeSeriesAddSegmentData(env, mm, tsd, smcseg);
                            }
                        }
                    }
                }
            }

            return res;
        }
    }
}
