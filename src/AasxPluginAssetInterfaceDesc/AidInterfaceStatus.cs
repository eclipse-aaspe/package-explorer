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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using AdminShellNS.DiaryData;
using Extensions;
using AasxIntegrationBase;
using AasxPredefinedConcepts.AssetInterfacesDescription;
using FluentModbus;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;
using AnyUi;
using System.Windows.Media.Animation;
using AasxIntegrationBase.AdminShellEvents;
using System.IO;

namespace AasxPluginAssetInterfaceDescription
{
    /// <summary>
    /// These instances are attached to an <c>AidIfxItemStatus</c> in order to
    /// identify, which AAS elements need to be updated.
    /// </summary>
    public class AidMappingOutputItem
    {
        /// <summary>
        /// The RelationElements of the AID MC, which caused this item.
        /// </summary>
        public AasClassMapperHintedRelation MapRelation;
    }

    public enum AidIfxItemKind { Unknown, Property, Action, Event };

    public class AidIfxItemStatus
    {
        /// <summary>
        /// Which kind of information: Property, Action, Event.
        /// </summary>
        public AidIfxItemKind Kind = AidIfxItemKind.Unknown;

        /// <summary>
        /// Contains the hierarchy information, where the item is stored in hierarchy
        /// of the given interface (end-point).
        /// </summary>
        public string Location = "";

        /// <summary>
        /// Display name of the item. Could be from IdShort, key, title.
        /// </summary>
        public string DisplayName = "";

        /// <summary>
        /// Contains the forms information with all detailed information for the 
        /// technology.
        /// </summary>
        public CD_Forms FormData = null;

        /// <summary>
        /// String data for value incl. unit information.
        /// </summary>
        public string Value = "";

        /// <summary>
        /// Link to entity (property, action, event).
        /// </summary>
        public object Tag = null;

        /// <summary>
        /// Initally <c>null</c>, set to the items which shall be updated, whenever
        /// this item status is updated.
        /// </summary>
        public List<AidMappingOutputItem> MapOutputItems = null;

        /// <summary>
        /// Holds reference to the AnyUI element showing the value.
        /// </summary>
        public AnyUiUIElement RenderedUiElement = null;
    }

    public enum AidInterfaceTechnology { HTTP, Modbus, MQTT, OPCUA }

    public class AidInterfaceStatus
    {
        /// <summary>
        /// Technology being used ..
        /// </summary>
        public AidInterfaceTechnology Technology = AidInterfaceTechnology.HTTP;

        /// <summary>
        /// Display name. Could be from SMC IdShort or title.
        /// Will be printed in bold.
        /// </summary>
        public string DisplayName = "";

        /// <summary>
        /// Further infornation. Printed in light appearence.
        /// </summary>
        public string Info = "";

        /// <summary>
        /// The information items (properties, actions, events)
        /// </summary>
        public MultiValueDictionary<string, AidIfxItemStatus> Items = 
            new MultiValueDictionary<string, AidIfxItemStatus>();

        /// <summary>
        /// Base connect information.
        /// </summary>
        public string EndpointBase = "";

        /// <summary>
        /// Actual summary of the status of the interface.
        /// </summary>
        public string LogLine = "Idle.";

        /// <summary>
        /// Black = idle, Blue = active, Red = error.
        /// </summary>
        public StoredPrint.Color LogColor = StoredPrint.Color.Black;

        /// <summary>
        /// Link to entity (interface).
        /// </summary>
        public object Tag = null;

        /// <summary>
        /// Holds the technology connection currently used.
        /// </summary>
        public AidBaseConnection Connection = null;

        /// <summary>
        /// Will get increment, when a value changed.
        /// </summary>
        public UInt64 ValueChanges = 0;

        /// <summary>
        /// If greater 10, specifies the time rate in milli seconds for polling the 
        /// respective subscriptions.
        /// </summary>
        public double UpdateFreqMs = 0;

        /// <summary>
        /// If greater 10, specifies the desired timeout in milli seconds.
        /// </summary>
        public double TimeOutMs = 0;

        /// <summary>
        /// To be used with <c>UpdateFreqMs</c>.
        /// </summary>
        protected DateTime _lastCyclicUpdate = DateTime.Now;

        public bool CheckIfTimeForCyclicUpdate(DateTime now)
        {
            if (UpdateFreqMs >= 10.0
                && (now - _lastCyclicUpdate).TotalMilliseconds < UpdateFreqMs)
                return false;
            _lastCyclicUpdate = now;
            return true;
        }

        protected string ComputeKey(string key)
        {
            if (key != null)
            {
                if (Technology == AidInterfaceTechnology.MQTT)
                {
                    key = key.Trim().Trim('/').ToLower();
                }
            }
            return key;
        }

        public void SetLogLine (StoredPrint.Color color, string line)
        {
            LogColor = color;
            LogLine = line;
        }

        /// <summary>
        /// Computes a technology specific key and adds item.
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(AidIfxItemStatus item)
        {
            // acceess
            if (item == null)
                return;

            // compute key
            var key = ComputeKey(item?.FormData?.Href);
            
            // now add
            Items.Add(key, item);
        }

        /// <summary>
        /// Computes key based on technology, checks if items can be found
        /// and enumerates these.
        /// </summary>
        public IEnumerable<AidIfxItemStatus> GetItemsFor(string key)
        {
            key = ComputeKey(key);
            if (Items.ContainsKey(key))
                foreach (var item in Items[key])
                    yield return item;
        }
    }

    public class AidBaseConnection
    {
        /// <summary>
        /// As a connection is a very "brittle" technology depending on a lot of external
        /// events, a log might be helpful.
        /// </summary>
        public LogInstance Log = null;

        public Uri TargetUri;

        /// <summary>
        /// For initiating the connection. Right now, not foreseen/ encouraged by the SMT.
        /// </summary>
        public string User = null;

        /// <summary>
        /// For initiating the connection. Right now, not foreseen/ encouraged by the SMT.
        /// </summary>
        public string Password = null;

        /// <summary>
        /// If greater 10, specifies the time rate in milli seconds for polling the 
        /// respective subscriptions.
        /// </summary>
        public double UpdateFreqMs = 0;

        /// <summary>
        /// If greater 10, specifies the desired timeout in milli seconds.
        /// </summary>
        public double TimeOutMs = 0;

        public DateTime LastActive = default(DateTime);

        public Action<string, string> MessageReceived = null;

        public Action<Aas.ISubmodelElement> AnimateSingleValueChange = null;

        virtual public bool Open()
        {
            return false;
        }

        virtual public bool IsConnected()
        {
            return false;
        }

        virtual public void Close()
        {
        }

        /// <summary>
        /// Tries to update the value (by polling).
        /// </summary>
        /// <returns>Number of values changed</returns>
        virtual public int UpdateItemValue(AidIfxItemStatus item)
        {
            return 0;
        }

        /// <summary>
        /// Tries to update the value (by polling).
        /// </summary>
        /// <returns>Number of values changed</returns>
        virtual public async Task<int> UpdateItemValueAsync(AidIfxItemStatus item)
        {
            await Task.Yield();
            return 0;
        }

        // <summary>
        /// Tries to update the value (by polling). Async opion is preferred.
        /// </summary>
        /// <returns>Number of values changed</returns>
        virtual public void PrepareContinousRun(IEnumerable<AidIfxItemStatus> items)
        {

        }

        public void NotifyOutputItems(AidIfxItemStatus item, string strval)
        {
            // access
            if (item == null)
                return;

            // map output items
            if (item.MapOutputItems != null)
                foreach (var moi in item.MapOutputItems)
                {
                    // valid?
                    if (moi?.MapRelation?.Second == null
                        || !(moi.MapRelation.SecondHint is Aas.Property prop))
                        continue;

                    // set here
                    prop.Value = strval;

                    // create
                    var evi = new AasPayloadUpdateValueItem(
                        path: (prop)?.GetModelReference()?.Keys,
                        value: prop.ValueAsText());

                    evi.ValueId = prop.ValueId;

                    evi.FoundReferable = prop;

                    // add to the aas element itself
                    DiaryDataDef.AddAndSetTimestamps(prop, evi, isCreate: false);

                    // give upwards for animation
                    AnimateSingleValueChange?.Invoke(prop);
                }
        }
    }

    public class AidGenericConnections<T> : Dictionary<Uri, T> where T : AidBaseConnection, new()
    {
        public T GetOrCreate(string target, LogInstance log = null)
        {
            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
                return null;
            if (this.ContainsKey(uri))
                return this[uri];

            var conn = new T() { Log = log, TargetUri = uri };
            return conn;
        }
    }

    /// <summary>
    /// Holds track of all current Aid interface status information.
    /// Idea: well be preset and updated by plug-in events.
    /// Will then allow technical connect to asset interfaces.
    /// Will exist **longer** than the invocation of just the plugin UI.
    /// </summary>
    public class AidAllInterfaceStatus
    {
        /// <summary>
        /// Set to logger, if logging is desired.
        /// </summary>
        protected LogInstance _log = null;

        /// <summary>
        /// Holds a "pointer" to the "last current" Submodel defining the AID description.
        /// Note: unlike most other plugins, this plugin is mostly intended to run in
        /// the background.
        /// </summary>
        public Aas.ISubmodel SmAidDescription = null;

        /// <summary>
        /// Holds a "pointer" to the "last current" Submodel defining the 
        /// AID mapping configuration.
        /// Note: unlike most other plugins, this plugin is mostly intended to run in
        /// the background.
        /// </summary>
        public Aas.ISubmodel SmAidMapping = null;

        /// <summary>
        /// Current setting, which technologies shall be used.
        /// </summary>
        public bool[] UseTech = { false, false, false, true };

        /// <summary>
        /// Will hold connections steady and continously update values, either by
        /// timer pased polling or by subscriptions.
        /// </summary>
        public bool ContinousRun = false;

        public List<AidInterfaceStatus> InterfaceStatus = new List<AidInterfaceStatus>();

        public AidGenericConnections<AidHttpConnection> HttpConnections =
            new AidGenericConnections<AidHttpConnection>();

        public AidGenericConnections<AidModbusConnection> ModbusConnections = 
            new AidGenericConnections<AidModbusConnection>();

        public AidGenericConnections<AidMqttConnection> MqttConnections =
            new AidGenericConnections<AidMqttConnection>();

        public AidGenericConnections<AidOpcUaConnection> OpcUaConnections =
            new AidGenericConnections<AidOpcUaConnection>();

        public AidAllInterfaceStatus(LogInstance log = null)
        {
            _log = log;
        }

        public void RememberNothing()
        {
            SmAidDescription = null;
        }

        public bool EnoughMemories()
        {
            return SmAidDescription != null;
        }

        public void RememberAidSubmodel(Aas.ISubmodel sm, AssetInterfaceOptionsRecord optRec,
            bool adoptUseFlags)
        {
            if (sm == null || optRec == null)
                return;

            if (optRec.IsDescription)
                SmAidDescription = sm;

            if (adoptUseFlags)
            {
                UseTech[(int)AidInterfaceTechnology.HTTP] = optRec.UseHttp;
                UseTech[(int)AidInterfaceTechnology.Modbus] = optRec.UseModbus;
                UseTech[(int)AidInterfaceTechnology.MQTT] = optRec.UseMqtt;
                UseTech[(int)AidInterfaceTechnology.OPCUA] = optRec.UseOpcUa;
            }
        }

        public void RememberMappingSubmodel(Aas.ISubmodel sm)
        {
            SmAidMapping = sm;
        }

        protected AidBaseConnection GetOrCreate(
            AidInterfaceStatus ifcStatus,
            string endpointBase,
            LogInstance log = null)
        {
            // access
            if (ifcStatus == null)
                return null;

            // find connection by factory
            AidBaseConnection conn = null;
            switch (ifcStatus.Technology)
            {
                case AidInterfaceTechnology.HTTP:
                    conn = HttpConnections.GetOrCreate(endpointBase, log);
                    break;

                case AidInterfaceTechnology.Modbus:
                    conn = ModbusConnections.GetOrCreate(endpointBase, log);
                    break;

                case AidInterfaceTechnology.MQTT:
                    conn = MqttConnections.GetOrCreate(endpointBase, log);
                    break;

                case AidInterfaceTechnology.OPCUA:
                    conn = OpcUaConnections.GetOrCreate(endpointBase, log);
                    break;
            }

            conn.UpdateFreqMs = ifcStatus.UpdateFreqMs;
            conn.TimeOutMs = ifcStatus.TimeOutMs;

            return conn;
        }

        /// <summary>
        /// Will get increment, when a value changed.
        /// </summary>
        public UInt64 SumValueChanges()
        {
            UInt64 sum = 0;
            foreach (var ifc in InterfaceStatus)
                sum += ifc.ValueChanges;
            return sum;
        }

        /// <summary>
        /// Will connect to each target once, get values and will disconnect again.
        /// </summary>
        public void UpdateValuesSingleShot()
        {
            // access allowed
            if (ContinousRun)
                return;
            SetAllLogIdle();

            // for all
            foreach (var tech in AdminShellUtil.GetEnumValues<AidInterfaceTechnology>())
            {
                // use?
                if (!UseTech[(int)tech])
                    continue;

                // find all interfaces with that technology
                foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == tech))
                {
                    // get a connection
                    if (ifc.EndpointBase?.HasContent() != true)
                        continue;

                    // find connection by factory
                    // single means: log the events
                    AidBaseConnection conn = GetOrCreate(ifc, ifc.EndpointBase, _log);
                    if (conn == null)
                        continue;

                    // open it
                    if (!conn.Open())
                        continue;
                    ifc.Connection = conn;

                    // go thru all items (sync)
                    foreach (var item in ifc.Items.Values)
                        ifc.ValueChanges += (UInt64)conn.UpdateItemValue(item);

                    // go thru all items (async)
                    var task = Task.Run(async () => 
                    {
                        // see: https://www.hanselman.com/blog/parallelforeachasync-in-net-6
                        await Parallel.ForEachAsync(
                            ifc.Items.Values,
                            new ParallelOptions() { MaxDegreeOfParallelism = 10 },
                            async (item, token) =>
                            {
                                ifc.ValueChanges += (UInt64)(await ifc.Connection.UpdateItemValueAsync(item));
                            });
                    });
                    task.Wait();
                }
            }

            // close all connections
            foreach (var ifc in InterfaceStatus)
            {
                if (ifc.Connection?.IsConnected() == true)
                    ifc.Connection.Close();
            }
        }

        protected void SetAllLogIdle()
        {
            foreach (var ifc in InterfaceStatus)
                ifc.SetLogLine(StoredPrint.Color.Black, "Idle.");
        }

        /// <summary>
        /// Will connect to each target, leave the connection open, will enable 
        /// cyclic updates.
        /// </summary>
        public void StartContinousRun()
        {
            // off
            ContinousRun = false;
            SetAllLogIdle();

            // for all
            foreach (var tech in AdminShellUtil.GetEnumValues<AidInterfaceTechnology>())
            {
                // use?
                if (!UseTech[(int)tech])
                    continue;

                // find all interfaces with that technology
                foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == tech))
                {
                    // get a connection
                    if (ifc.EndpointBase?.HasContent() != true)
                    {
                        ifc.SetLogLine(StoredPrint.Color.Red, "Endpoint is not specified.");
                        continue;
                    }

                    // find connection by factory
                    AidBaseConnection conn = GetOrCreate(ifc, ifc.EndpointBase);
                    if (conn == null)
                        continue;

                    // open it
                    if (!conn.Open())
                    {
                        ifc.SetLogLine(StoredPrint.Color.Red, $"Endpoint connot be opened: {ifc.EndpointBase}.");
                        continue;
                    }
                    ifc.Connection = conn;
                    ifc.SetLogLine(StoredPrint.Color.Blue, "Connection established.");

                    // start subscriptions ..
                    conn.MessageReceived = (topic, msg) =>
                    {
                        foreach (var ifc2 in InterfaceStatus)
                            foreach (var item in ifc2.GetItemsFor(topic))
                            {
                                // note value
                                item.Value = msg;

                                // notify
                                conn.NotifyOutputItems(item, msg);

                                // note value change
                                ifc2.ValueChanges++;

                                // remember last use
                                if (ifc2.Connection != null)
                                    ifc2.Connection.LastActive = DateTime.Now;
                            }
                    };
                    conn.AnimateSingleValueChange = OnAnimateSingleValueChange;
                    conn.PrepareContinousRun(ifc.Items.Values);
                    ifc.SetLogLine(StoredPrint.Color.Blue, "Connection established and prepared.");
                }
            }

            // now switch ON!
            ContinousRun = true;
        }

        /// <summary>
        /// Will stop continous run and close all connections.
        /// </summary>
        public void StopContinousRun()
        {
            // off
            ContinousRun = false;
            SetAllLogIdle();

            // close all connections
            foreach (var ifc in InterfaceStatus)
            {
                if (ifc.Connection?.IsConnected() == true)
                    ifc.Connection.Close();
            }
        }

        /// <summary>
        /// In continous run, will fetch values for polling based technologies (HTTP, Modbus, ..).
        /// </summary>
        public async Task UpdateValuesContinousByTickAsyc()
        {
            // access allowed
            if (!ContinousRun)
                return;

            var now = DateTime.Now;

            // for all
            foreach (var tech in AdminShellUtil.GetEnumValues<AidInterfaceTechnology>())
            {
                // use?
                if (!UseTech[(int)tech])
                    continue;

                // find all interfaces with that technology
                foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == tech))
                {
                    // get a connection
                    if (ifc?.Connection?.IsConnected() != true)
                        continue;

                    // recently enough
                    if (!ifc.CheckIfTimeForCyclicUpdate(now))
                        continue;

                    // go thru all items (sync)
                    foreach (var item in ifc.Items.Values)
                        ifc.ValueChanges += (UInt64) ifc.Connection.UpdateItemValue(item);

                    // go thru all items (async)
                    // see: https://www.hanselman.com/blog/parallelforeachasync-in-net-6
                    await Parallel.ForEachAsync(
                        ifc.Items.Values, 
                        new ParallelOptions() { MaxDegreeOfParallelism = 10 }, 
                        async (item, token) =>
                    {
                        ifc.ValueChanges += (UInt64) (await ifc.Connection.UpdateItemValueAsync(item));
                    });
                }
            }
        }

        //
        // further Event collection
        //

        public List<Aas.ISubmodelElement> AnimatedSingleValueChange = new List<Aas.ISubmodelElement>();

        protected void OnAnimateSingleValueChange(Aas.ISubmodelElement sme)
        {
            if (sme == null)
                return;

            lock (AnimatedSingleValueChange)
                AnimatedSingleValueChange.Add(sme);
        }

        //
        // Building, intake from Submodel
        //

        public void PrepareAidInformation(Aas.ISubmodel smAid, Aas.ISubmodel smMapping = null,
            Func<Aas.IReference, Aas.IReferable> lambdaLookupReference = null)
        {
            // access
            InterfaceStatus.Clear();
            if (smAid == null)
                return;

            // get data AID
            var dataAid = new AasxPredefinedConcepts.AssetInterfacesDescription.CD_AssetInterfacesDescription();
            PredefinedConceptsClassMapper.ParseAasElemsToObject(smAid, dataAid, lambdaLookupReference);

            // get data MC
            var dataMc = (smMapping != null) ?
                new AasxPredefinedConcepts.AssetInterfacesMappingConfiguration.
                    CD_AssetInterfacesMappingConfiguration() : null;
            PredefinedConceptsClassMapper.ParseAasElemsToObject(smMapping, dataMc, lambdaLookupReference);

            // prepare
            foreach (var tech in AdminShellUtil.GetEnumValues<AidInterfaceTechnology>())
            {
                var ifxs = dataAid?.InterfaceHTTP;
                if (tech == AidInterfaceTechnology.Modbus) ifxs = dataAid?.InterfaceMODBUS;
                if (tech == AidInterfaceTechnology.MQTT) ifxs = dataAid?.InterfaceMQTT;
                if (tech == AidInterfaceTechnology.OPCUA) ifxs = dataAid?.InterfaceOPCUA;
                if (ifxs == null || ifxs.Count < 1)
                    continue;
                foreach (var ifx in ifxs)
                {
                    // new interface
                    var dn = AdminShellUtil.TakeFirstContent(ifx.Title, ifx.__Info__?.Referable?.IdShort);
                    var aidIfx = new AidInterfaceStatus()
                    {
                        Technology = tech,
                        DisplayName = $"{dn}",
                        Info = $"{ifx.EndpointMetadata?.Base}",
                        EndpointBase = "" + ifx.EndpointMetadata?.Base,
                        Tag = ifx
                    };
                    InterfaceStatus.Add(aidIfx);

                    // Properties .. lambda recursion
                    Action<string, CD_PropertyName> recurseProp = null;
                    recurseProp = (location, propName) =>
                    {
                        // add item
                        var ifcItem = new AidIfxItemStatus()
                        {
                            Kind = AidIfxItemKind.Property,
                            Location = location,
                            DisplayName = AdminShellUtil.TakeFirstContent(
                                propName.Title, propName.Key, propName.__Info__?.Referable?.IdShort),
                            FormData = propName.Forms,
                            Value = "???"
                        };
                        aidIfx.AddItem(ifcItem);

                        // does (some) mapping have a source with this property name?
                        var lst = new List<AidMappingOutputItem>();
                        if (dataMc?.MappingConfigurations?.MappingConfigs != null)
                            foreach (var mc in dataMc.MappingConfigurations.MappingConfigs)
                                if (mc.InterfaceReference?.ValueHint != null
                                    && mc.InterfaceReference?.ValueHint == ifx?.__Info__?.Referable
                                    && mc.MappingSourceSinkRelations?.MapRels != null)
                                    foreach (var mr in mc.MappingSourceSinkRelations.MapRels)
                                        if (mr?.FirstHint != null
                                            && mr.FirstHint == propName?.__Info__?.Referable)
                                            lst.Add(new AidMappingOutputItem()
                                            {
                                                MapRelation = mr
                                            });
                        if (lst.Count > 0)
                            ifcItem.MapOutputItems = lst;

                        // directly recurse?
                        /*
                        if (propName?.Properties?.Property != null)
                            foreach (var child in propName.Properties.Property)
                                recurseProp(location + " . " + ifcItem.DisplayName, child);
                        */
                    };

                    if (ifx.InterfaceMetadata?.Properties?.Property == null)
                        continue;
                    foreach (var propName in ifx.InterfaceMetadata?.Properties?.Property)
                        recurseProp("\u2302", propName);
                }
            }
        }

        protected List<int> SelectValuesToIntList<ITEM>(
            IEnumerable<ITEM> items,
            Func<ITEM, string> selectStringValue) where ITEM : class
        {
            return items
                // select polling time present
                .Select(selectStringValue)
                .Where((pt) => pt != null)
                // convert to int
                .Select(s => Int32.TryParse(s, out int n) ? n : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .ToList();
        }

        protected void SetDoubleOnDefaultOrAvgOfIntList(
            ref double theValue,
            double minimumVal,
            double defaultVal,
            List<int> list)
        {
            if (defaultVal >= minimumVal)
                theValue = defaultVal;
            if (list.Count > 0)
                theValue = Math.Max(minimumVal, list.Average());
        }

        /// <summary>
        /// to be called after <c>PrepareAidInformation</c>
        /// </summary>
        public void SetAidInformationForUpdateAndTimeout(
            double defaultUpdateFreqMs = 0,
            double defaultTimeOutMs = 0)
        {
            // for Http, analyze update frequency and timeout
            foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == AidInterfaceTechnology.HTTP))
            {
                // polltimes
                SetDoubleOnDefaultOrAvgOfIntList(
                    ref ifc.UpdateFreqMs, 10.0, defaultUpdateFreqMs,
                    SelectValuesToIntList(ifc?.Items?.Values, (it) => it.FormData?.Htv_pollingTime));

                // time out
                SetDoubleOnDefaultOrAvgOfIntList(
                    ref ifc.TimeOutMs, 10.0, defaultTimeOutMs,
                    SelectValuesToIntList(ifc?.Items?.Values, (it) => it.FormData?.Htv_timeout));
            }

            // for Modbus, analyze update frequency and timeout
            foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == AidInterfaceTechnology.Modbus))
            {
                // polltimes
                SetDoubleOnDefaultOrAvgOfIntList(
                    ref ifc.UpdateFreqMs, 10.0, defaultUpdateFreqMs,
                    SelectValuesToIntList(ifc?.Items?.Values, (it) => it.FormData?.Modbus_pollingTime));

                // time out
                SetDoubleOnDefaultOrAvgOfIntList(
                    ref ifc.TimeOutMs, 10.0, defaultTimeOutMs,
                    SelectValuesToIntList(ifc?.Items?.Values, (it) => it.FormData?.Modbus_timeout));
            }

            // for OPC UA, analyze update frequency and timeout
            foreach (var ifc in InterfaceStatus.Where((i) => i.Technology == AidInterfaceTechnology.OPCUA))
            {
                // polltimes
                SetDoubleOnDefaultOrAvgOfIntList(
                    ref ifc.UpdateFreqMs, 10.0, defaultUpdateFreqMs,
                    SelectValuesToIntList(ifc?.Items?.Values, (it) => it.FormData?.OpcUa_pollingTime));

                // time out
                SetDoubleOnDefaultOrAvgOfIntList(
                    ref ifc.TimeOutMs, 10.0, defaultTimeOutMs,
                    SelectValuesToIntList(ifc?.Items?.Values, (it) => it.FormData?.OpcUa_timeout));
            }
        }
    }

}
