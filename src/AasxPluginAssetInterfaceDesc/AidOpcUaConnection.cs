/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

#define OPCUA2

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
using AasxPredefinedConcepts.AssetInterfacesDescription;
using FluentModbus;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;
using MQTTnet;
using MQTTnet.Client;
using System.Web.Services.Description;
using AasxOpcUa2Client;
using System.IO;



#if OPCUA2
using Workstation.ServiceModel.Ua;
#else
using Opc.Ua;
#endif

namespace AasxPluginAssetInterfaceDescription
{
    public class AidOpcUaConnection : AidBaseConnection
    {

#if OPCUA2
        public AasOpcUaClient2 Client;
#else
        public AasOpcUaClient Client;
#endif

        public class SubscribedItem
        {
            public string NodePath;
#if OPCUA2
            public IDisposable Subscription;
            public uint Handle;
#else
            public Opc.Ua.Client.Subscription Subscription;
#endif
            public AidIfxItemStatus Item;
        }

#if OPCUA2
        protected Dictionary<uint, SubscribedItem> _subscriptions
            = new Dictionary<uint, SubscribedItem>();
#else
        protected Dictionary<NodeId, SubscribedItem> _subscriptions
            = new Dictionary<NodeId, SubscribedItem>();
#endif

        override public async Task<bool> Open()
        {
            try
            {
                // make client
                // use the full target uri as endpoint (first)
#if OPCUA2
                
                Client = new AasOpcUaClient2(
                    TargetUri.ToString(),
                    autoAccept: true,
                    userName: this.User,
                    password: this.Password,
                    timeOutMs: (TimeOutMs >= 10) ? (uint)TimeOutMs : 2000,
                    log: Log,
                    securityMode: (MessageSecurityMode?)this.SecurityMode,
                    securityPolicy: this.SecurityPolicy,
                    autoConnect: this.OPCAutoConnection);
                
#else
                Client = new AasOpcUaClient(
                    TargetUri.ToString(), 
                    autoAccept: true, 
                    userName: this.User,
                    password: this.Password,
                    timeOutMs: (TimeOutMs >= 10) ? (uint) TimeOutMs : 2000,
                    log: Log);
                // Client.Run();
#endif

                await Client.DirectConnect();

                // ok
                return IsConnected();
            }
            catch (Exception ex)
            {
                Client = null;
                // _subscribedTopics.Clear();
                return false;
            }
        }

        override public bool IsConnected()
        {
            // simple
            return Client != null && Client.StatusCode == AasOpcUaClientStatus.Running;
        }

        override public void Close()
        {
            if (IsConnected())
            {
                try
                {
                    // Client.Cancel();
                    Client.Close();
                } catch (Exception ex)
                {
                    ;
                }
                // _subscribedTopics.Clear();
            }
        }

#if OPCUA2
        override public async Task<int> UpdateItemValueAsync(AidIfxItemStatus item)
        {
            // access
            if (!IsConnected())
                return 0;

            // careful
            try
            {
                var nodePath = "" + item.FormData?.Href;
                nodePath = nodePath.Replace("/?id =", "").Trim();
                // get an node id?
                var nid = Client.ParseAndCreateNodeId(nodePath);

                // direct read possible?
                var dv = await Client.ReadNodeIdAsync(nid);
                item.Value = AdminShellUtil.ToStringInvariant(dv?.Value);

                // notify
                NotifyOutputItems(item, item.Value);
                LastActive = DateTime.Now;

                // success 
                return 1;
            }
            catch (Exception ex)
            {
                ;
            }

            return 0;
        }
#else
        override public int UpdateItemValue(AidIfxItemStatus item)
        {
            // access
            if (!IsConnected())
                return 0;

            // careful
            try
            {
                // get an node id?
                var nid = Client.ParseAndCreateNodeId(item?.FormData?.Href);

                // direct read possible?
                var dv = Client.ReadNodeId(nid);
                item.Value = AdminShellUtil.ToStringInvariant(dv?.Value);
                LastActive = DateTime.Now;
            }
            catch (Exception ex)
            {
                ;
            }

            return 0;
        }
#endif

        protected uint _singletonHandleId = 1;

        override public async Task PrepareContinousRunAsync(IEnumerable<AidIfxItemStatus> items)
        {
            // access
            if (!IsConnected() || items == null)
                return;

#if OPCUA2

            // put all items into a single subscription ..
            var nids = new List<HandledNodeId>();
            foreach (var item in items)
            {
                // valid href?

                var nodePath = "" + item.FormData?.Href;
                nodePath = nodePath.Replace("/?id =", "").Trim();
                if (!nodePath.HasContent())
                    continue;

                

                // get an node id?
                var nid = Client.ParseAndCreateNodeId(nodePath);
                if (nid == null)
                    continue;

                // generate handle
                var handle = _singletonHandleId++;

                // add
                nids.Add(new HandledNodeId(handle, nid));

                _subscriptions.Add(handle,
                    new SubscribedItem()
                    {
                        NodePath = nodePath,
                        Item = item,
                        Handle = handle
                    });
            }

            // subscribe all together
            var sub = await Client.SubscribeNodeIdsAsync(
                nids.ToArray(),
                handler: SubscriptionHandler,
                publishingInterval: (UpdateFreqMs >= 10) ? (int)UpdateFreqMs : 500);

#else


            // over the items
            // go the easy way: put each item into one subscription
            foreach (var item in items)
            {
                // valid href?
                var nodePath = "" + item.FormData?.Href;
                nodePath = nodePath.Trim();
                if (!nodePath.HasContent())
                    continue;

                // get an node id?
                var nid = Client.ParseAndCreateNodeId(nodePath);
                if (nid == null)
                    continue;

                // is topic already subscribed?
                if (_subscriptions.ContainsKey(nodePath))
                    continue;

                // ok, make subscription               
                _subscriptions.Add(nodePath,
                    new SubscribedItem()
                    {
                        NodePath = nodePath,
                        Subscription = sub,
                        Item = item,
                    });
                var sub = Client.SubscribeNodeIds(
                    new[] { nid },
                    handler: SubscriptionHandler,
                    publishingInterval: (UpdateFreqMs >= 10) ? (int) UpdateFreqMs : 500);
                _subscriptions.Add(nodePath,
                    new SubscribedItem() {
                        NodePath = nodePath,
                        Subscription = sub,
                        Item = item,
                    });
        }

#endif

            }

#if OPCUA2
        protected void SubscriptionHandler(uint handle, DataValue dataValue)
        {
            if (_subscriptions?.ContainsKey(handle) != true)
                return;

            // okay
            var subi = _subscriptions[handle];
            if (subi?.Item != null && subi.NodePath?.HasContent() == true)
            {
                // take over most actual value
                var valueObj = dataValue?.Value;
                if (valueObj != null)
                    MessageReceived?.Invoke(subi.NodePath, AdminShellUtil.ToStringInvariant(valueObj));
            }
        }
#else
        protected void SubscriptionHandler(
            Opc.Ua.Client.MonitoredItem monitoredItem,
            Opc.Ua.Client.MonitoredItemNotificationEventArgs e)
        {
            // key is the "start node"
            if (_subscriptions == null || monitoredItem?.StartNodeId == null
                || !_subscriptions.ContainsKey(monitoredItem.StartNodeId))
                return;

            // okay
            var subi = _subscriptions[monitoredItem.StartNodeId];
            if (subi?.Item != null && subi.NodePath?.HasContent() == true)
            {
                // take over most actual value
                var valueObj = monitoredItem.DequeueValues().LastOrDefault();
                if (valueObj != null)
                    MessageReceived?.Invoke(subi.NodePath, AdminShellUtil.ToStringInvariant(valueObj.Value));
            }
        }
#endif

        }
    }
