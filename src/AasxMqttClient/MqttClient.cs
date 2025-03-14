﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <opensource@phoenixcontact.com>
Author: Andreas Orzelski

Copyright (c) 2019 Fraunhofer IOSB-INA Lemgo,
    eine rechtlich nicht selbständige Einrichtung der Fraunhofer-Gesellschaft
    zur Förderung der angewandten Forschung e.V. <florian.pethig@iosb-ina.fraunhofer.de>
Author: Florian Pethig

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using AnyUi;
using Extensions;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

namespace AasxMqttClient
{
    public class AnyUiDialogueDataMqttPublisher : AnyUiDialogueDataBase
    {
        [JsonIgnore]
        public static int MqttDefaultPort = 1883;

        [JsonIgnore]
        public static string HelpString =
            "{aas} = AAS.idShort, {aas-id} = AAS.identification" + System.Environment.NewLine +
            "{sm} = Submodel.idShort, {sm-id} = Submodel.identification" + System.Environment.NewLine +
            "{path} = Path of idShorts";

        public string BrokerUrl = "localhost:1883";
        public bool MqttRetain = false;

        public bool EnableFirstPublish = true;
        public string FirstTopicAAS = "AAS";
        public string FirstTopicSubmodel = "Submodel_{sm}";

        public bool EnableEventPublish = false;
        public string EventTopic = "Events";

        public bool SingleValuePublish = false;
        public bool SingleValueFirstTime = false;
        public string SingleValueTopic = "Values";

        public bool LogDebug = false;

        public AnyUiDialogueDataMqttPublisher(
            string caption = "",
            double? maxWidth = null)
            : base(caption, maxWidth)
        {
        }

        public static AnyUiDialogueDataMqttPublisher CreateWithOptions(
            string caption = "",
            double? maxWidth = null,
            Newtonsoft.Json.Linq.JToken jtoken = null)
        {
            // access
            if (jtoken == null)
                return new AnyUiDialogueDataMqttPublisher(caption, maxWidth);

            // ReSharper disable EmptyGeneralCatchClause
            try
            {
                var res = jtoken.ToObject<AnyUiDialogueDataMqttPublisher>();
                if (res != null)
                    // found something
                    return res;
            }
            catch { }
            // ReSharper enable EmptyGeneralCatchClause

            // .. no, default!
            return new AnyUiDialogueDataMqttPublisher(caption, maxWidth);
        }
    }

    public class MqttClient
    {
        private AnyUiDialogueDataMqttPublisher _diaData = null;
        private GrapevineLoggerToStoredPrints _logger = null;
        private IMqttClient _mqttClient = null;

        private int _countElement, _countEvent, _countSingleValue;

        protected void LogStatus(int incElement = 0, int incEvent = 0, int incSingleValue = 0)
        {
            _countElement += incElement;
            _countEvent += incEvent;
            _countSingleValue += incSingleValue;

            if (_logger != null)
                _logger.Append(new StoredPrint(
                    StoredPrint.Color.Black, "",
                    messageType: StoredPrint.MessageTypeEnum.Status,
                    statusItems: new[] {
                        new StoredPrint.StatusItem("# of AAS Element", "# element", "" + _countElement),
                        new StoredPrint.StatusItem("# of event", "# event", "" + _countEvent),
                        new StoredPrint.StatusItem("# of single value", "# value", "" + _countSingleValue)
                    }));
        }

        /// <summary>
        /// Splits into host part and numerical port number. Format e.g. "192.168.0.27:1883" or "localhost:1884".
        /// Note: special function realized, as side effects of <c>Uri()</c> not clear.
        /// </summary>
        private Tuple<string, int> SplitBrokerUrl(string url)
        {
            // TODO (MIHO, 2021-06-30): check use of Url()
            if (url == null)
                return null;

            // trivial
            int p = url.IndexOf(':');
            if (p < 0 || p >= url.Length)
                return new Tuple<string, int>(url, AnyUiDialogueDataMqttPublisher.MqttDefaultPort);

            // split
            var host = url.Substring(0, p);
            var pstr = url.Substring(p + 1);
            if (int.TryParse(pstr, out int pnr))
                return new Tuple<string, int>(host, pnr);

            // default
            return new Tuple<string, int>(host, AnyUiDialogueDataMqttPublisher.MqttDefaultPort);
        }

        private string GenerateTopic(string template,
            string defaultIfNull = null,
            string aasIdShort = null, string aasId = null,
            string smIdShort = null, string smId = null,
            string path = null)
        {
            var res = template;

            if (defaultIfNull != null && res == null)
                res = defaultIfNull;

            if (aasIdShort != null)
                res = res.Replace("{aas}", "" + aasIdShort);

            if (aasId != null)
                res = res.Replace("{aas-id}", "" + System.Net.WebUtility.UrlEncode(aasId));

            if (smIdShort != null)
                res = res.Replace("{sm}", "" + smIdShort);

            if (smId != null)
                res = res.Replace("{sm-id}", "" + System.Net.WebUtility.UrlEncode(smId));

            if (path != null)
                res = res.Replace("{path}", path);

            // make sure that topic are not starting / ending with '/'
            res = res.Trim(' ', '/');

            return res;
        }

        public async Task StartAsync(
            AdminShellPackageEnv package,
            AnyUiDialogueDataMqttPublisher diaData,
            GrapevineLoggerToStoredPrints logger = null)
        {
            // first options
            _diaData = diaData;
            _logger = logger;
            if (_diaData == null)
            {
                logger?.Error("Error: no options available.");
                return;
            }

            // broker?
            var hp = SplitBrokerUrl(_diaData.BrokerUrl);
            if (hp == null)
            {
                _logger?.Error("Error: no broker URL available.");
                return;
            }
            _logger?.Info($"Conneting broker {hp.Item1}:{hp.Item2} ..");

            // Create TCP based options using the builder.

            var options = new MqttClientOptionsBuilder()
                .WithClientId("AASXPackageXplorer MQTT Client")
                .WithTcpServer(hp.Item1, hp.Item2)
                .Build();

            //create MQTT Client and Connect using options above

            _mqttClient = new MqttFactory().CreateMqttClient();
            await _mqttClient.ConnectAsync(options);
            if (_mqttClient.IsConnected)
                _logger?.Info("### CONNECTED WITH SERVER ###");

            //publish AAS to AAS Topic

            if (_diaData.EnableFirstPublish)
            {
                foreach (Aas.AssetAdministrationShell aas in package.AasEnv.AllAssetAdministrationShells())
                {
                    _logger?.Info("Publish first AAS");
                    var message = new MqttApplicationMessageBuilder()
                                   .WithTopic(GenerateTopic(
                                        _diaData.FirstTopicAAS, defaultIfNull: "AAS",
                                        aasIdShort: aas.IdShort, aasId: aas.Id))
                                   .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(aas))
                                   .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                   .WithRetainFlag(_diaData.MqttRetain)
                                   .Build();

                    await _mqttClient.PublishAsync(message);
                    LogStatus(incElement: 1);

                    //publish submodels
                    foreach (var sm in package.AasEnv.AllSubmodels())
                    {
                        // whole structure
                        _logger?.Info("Publish first " + "Submodel_" + sm.IdShort);

                        var message2 = new MqttApplicationMessageBuilder()
                                        .WithTopic(GenerateTopic(
                                            _diaData.FirstTopicSubmodel, defaultIfNull: "Submodel_" + sm.IdShort,
                                            aasIdShort: aas.IdShort, aasId: aas.Id,
                                            smIdShort: sm.IdShort, smId: sm.Id))
                                       .WithPayload(Newtonsoft.Json.JsonConvert.SerializeObject(sm))
                                       .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                       .WithRetainFlag(_diaData.MqttRetain)
                                       .Build();

                        await _mqttClient.PublishAsync(message2);
                        LogStatus(incElement: 1);

                        // single values as well? 
                        if (_diaData.SingleValueFirstTime)
                            PublishSingleValues_FirstTimeSubmodel(aas, sm, sm.GetReference()?.Keys);
                    }
                }
            }

            _logger?.Info("Publish full events: " + _diaData.EnableEventPublish);
            _logger?.Info("Publish single values: " + _diaData.SingleValuePublish);
        }

        protected bool IsLeaf(ISubmodelElement sme)
        {
            if (sme == null)
                return false;
            var childExist = sme.EnumerateChildren().FirstOrDefault();
            return childExist == null;
        }

        private void PublishSingleValues_FirstTimeSubmodel(
            Aas.IAssetAdministrationShell aas,
            Aas.ISubmodel sm,
            List<Aas.IKey> startPath)
        {
            // trivial
            if (aas == null || sm == null)
                return;

            // give this to (recursive) function
            sm.RecurseOnSubmodelElements(null, (o, parents, sme) =>
            {
                // assumption is, the sme is now "leaf" of a SME-hierarchy
                if (!IsLeaf(sme))
                    return true;

                // value of the leaf
                var valStr = sme.ValueAsText();

                // build a complete path of keys
                var path = startPath.Copy();
                path.AddRange(parents.ToKeyList());
                path.Add(sme?.ToKey());
                var pathStr = path.BuildIdShortPath();

                // publish
                if (_diaData.LogDebug)
                    _logger?.Info("Publish single value (first time)");

                var msg = new MqttApplicationMessageBuilder()
                            .WithTopic(GenerateTopic(
                                _diaData.SingleValueTopic, defaultIfNull: "SingleValue",
                                aasIdShort: aas.IdShort, aasId: aas.Id,
                                smIdShort: sm.IdShort, smId: sm.Id,
                                path: pathStr))
                            .WithPayload(valStr)
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                            .WithRetainFlag(_diaData.MqttRetain)
                            .Build();
                _mqttClient.PublishAsync(msg).GetAwaiter().GetResult();
                LogStatus(incSingleValue: 1);

                // recurse
                return true;
            });
        }

        private void PublishSingleValues_ChangeItem(
            AasEventMsgEnvelope ev,
            ExtendEnvironment.ReferableRootInfo ri,
            List<Aas.IKey> startPath,
            AasPayloadStructuralChangeItem ci)
        {
            // trivial
            if (ev == null || ci == null || startPath == null)
                return;

            // only specific reasons
            if (!(ci.Reason == StructuralChangeReason.Create || ci.Reason == StructuralChangeReason.Modify))
                return;

            // need a payload
            if (ci.Path == null || ci.Data == null)
                return;

            var dataRef = ci.GetDataAsReferable();

            // give this to (recursive) function
            var messages = new List<MqttApplicationMessage>();
            if (dataRef is IReferable dataSme)
            {
                dataSme.RecurseOnReferables(null, (o, parents, rf) =>
                {
                    // assumption is, the sme is now "leaf" of a SME-hierarchy
                    var sme = rf as Aas.ISubmodelElement;
                    if (sme == null || !IsLeaf(sme))
                        return true;

                    // value of the leaf
                    var valStr = sme.ValueAsText();

                    // build a complete path of keys
                    var path = startPath.Copy();
                    path.AddRange(ci.Path);
                    path.AddRange(parents.ToKeyList());
                    path.Add(sme?.ToKey());
                    var pathStr = path.BuildIdShortPath();

                    // publish
                    if (_diaData.LogDebug)
                        _logger?.Info("Publish single value (create/ update)");
                    messages.Add(
                        new MqttApplicationMessageBuilder()
                            .WithTopic(GenerateTopic(
                                _diaData.SingleValueTopic, defaultIfNull: "SingleValue",
                                aasIdShort: ri?.AAS?.IdShort, aasId: ri?.AAS?.Id,
                                smIdShort: ri?.Submodel?.IdShort, smId: ri?.Submodel?.Id,
                                path: pathStr))
                            .WithPayload(valStr)
                            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                            .WithRetainFlag(_diaData.MqttRetain)
                            .Build());

                    // recurse
                    return true;
                });
            }

            // publish these
            // convert to synchronous behaviour
            int count = 0;
            foreach (var msg in messages)
            {
                count++;
                _mqttClient.PublishAsync(msg).GetAwaiter().GetResult();
            }
            LogStatus(incSingleValue: count);
        }

        private void PublishSingleValues_UpdateItem(
            AasEventMsgEnvelope ev,
            ExtendEnvironment.ReferableRootInfo ri,
            List<Aas.IKey> startPath,
            AasPayloadUpdateValueItem ui)
        {
            // trivial
            if (ev == null || ui == null || startPath == null || ui.Path == null)
                return;

            // value of the leaf
            var valStr = "" + ui.Value;

            // build a complete path of keys
            var path = startPath.Copy();
            path.AddRange(ui.Path);
            var pathStr = path.BuildIdShortPath();

            // publish
            if (_diaData.LogDebug)
                _logger?.Info("Publish single value (update value)");
            var message = new MqttApplicationMessageBuilder()
                    .WithTopic(GenerateTopic(
                        _diaData.SingleValueTopic, defaultIfNull: "SingleValue",
                        aasIdShort: ri?.AAS?.IdShort, aasId: ri?.AAS?.Id,
                        smIdShort: ri?.Submodel?.IdShort, smId: ri?.Submodel?.Id,
                        path: pathStr))
                    .WithPayload(valStr)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                    .WithRetainFlag(_diaData.MqttRetain)
                    .Build();

            // publish
            _mqttClient.PublishAsync(message).GetAwaiter().GetResult();
            LogStatus(incSingleValue: 1);
        }

        public void PublishEventAsync(AasEventMsgEnvelope ev,
            ExtendEnvironment.ReferableRootInfo ri = null)
        {
            // access
            if (ev == null || _mqttClient == null || !_mqttClient.IsConnected)
                return;

            // serialize the event
            var settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AasEventMsgEnvelope) });
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.Formatting = Formatting.Indented;
            var json = JsonConvert.SerializeObject(ev, settings);

            // aas / sm already available in rootInfo, prepare idShortPath
            var sourcePathStr = "";
            var sourcePath = new List<Aas.IKey>();
            if (ev.Source?.Keys != null && ri != null && ev.Source.Keys.Count > ri.NrOfRootKeys)
            {
                sourcePath = ev.Source.Keys.SubList(ri.NrOfRootKeys);
                sourcePathStr = sourcePath.BuildIdShortPath();
            }

            var observablePath = new List<Aas.IKey>();
            if (ev.ObservableReference?.Keys != null && ri != null
                && ev.ObservableReference.Keys.Count > ri.NrOfRootKeys)
            {
                observablePath = ev.ObservableReference.Keys.SubList(ri.NrOfRootKeys);
            }

            // publish the full event?
            if (_diaData.EnableEventPublish)
            {
                if (_diaData.LogDebug)
                    _logger?.Info("Publish Event");
                var message = new MqttApplicationMessageBuilder()
                               .WithTopic(GenerateTopic(
                                    _diaData.EventTopic, defaultIfNull: "Event",
                                    aasIdShort: ri?.AAS?.IdShort, aasId: ri?.AAS?.Id,
                                    smIdShort: ri?.Submodel?.IdShort, smId: ri?.Submodel?.Id,
                                    path: sourcePathStr))
                               .WithPayload(json)
                               .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                               .WithRetainFlag(_diaData.MqttRetain)
                               .Build();

                // convert to synchronous behaviour
                _mqttClient.PublishAsync(message).GetAwaiter().GetResult();
                LogStatus(incEvent: 1);
            }

            // deconstruct the event into single units?
            if (_diaData.SingleValuePublish)
            {
                if (_diaData.LogDebug)
                    _logger?.Info("Publish single values ..");

                if (ev.PayloadItems != null)
                    foreach (var epl in ev.PayloadItems)
                    {
                        if (epl is AasPayloadStructuralChange apsc && apsc.Changes != null)
                            foreach (var ci in apsc.Changes)
                                PublishSingleValues_ChangeItem(ev, ri, observablePath, ci);

                        if (epl is AasPayloadUpdateValue apuv && apuv.Values != null)
                            foreach (var ui in apuv.Values)
                                PublishSingleValues_UpdateItem(ev, ri, observablePath, ui);
                    }
            }
        }

        public void PublishEvent(AasEventMsgEnvelope ev,
            ExtendEnvironment.ReferableRootInfo ri = null)
        {
            PublishEventAsync(ev, ri);
        }
    }
}
