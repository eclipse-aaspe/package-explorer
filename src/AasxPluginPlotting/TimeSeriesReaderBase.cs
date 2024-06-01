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

namespace AasxPluginPlotting
{
    /// <summary>
    /// Base class and factory (!) for reading Time Series Submodel data.
    /// </summary>
    public class TimeSeriesReaderBase
    {
        public double? SpecifiedTimeToDouble(
            TimeSeriesTimeAxis timeAxis, string bufValue)
        {
            if (timeAxis == TimeSeriesTimeAxis.Utc || timeAxis == TimeSeriesTimeAxis.Tai)
            {
                // strict time string
                if (DateTime.TryParse(bufValue, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal, out DateTime dt))
                {
                    return dt.ToOADate();
                }
            }

            // plain time or plain value
            if (double.TryParse(bufValue, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double fValue))
                return fValue;

            // no?
            return null;
        }

        public virtual ListOfTimeSeriesData TimeSeriesStartFromSubmodel(
            Aas.IEnvironment env, Aas.Submodel sm)
        {
            return new ListOfTimeSeriesData();
        }

        public virtual void TimeSeriesAddSegmentData(
            Aas.IEnvironment env,
            MatchMode mm,
            TimeSeriesData tsd,
            Aas.ISubmodelElementCollection smcseg)
        {
        }

        public static TimeSeriesReaderBase GetReader(Aas.Submodel sm)
        {
            if (sm?.SemanticId?.IsValid() != true)
                return null;

            if (sm.SemanticId.MatchesExactlyOneKey(
                ZveiTimeSeriesDataV10.Static.SM_TimeSeriesData.GetSemanticKey(),
                matchMode: MatchMode.Relaxed))
                return new TimeSeriesReaderV10();

            if (sm.SemanticId.MatchesExactlyOneKey(
                IdtaTimeSeriesDataV11.Static.SM_TimeSeries.GetSemanticKey(),
                matchMode: MatchMode.Relaxed))
                return new TimeSeriesReaderV11();

            return null;
        }
    }
}
