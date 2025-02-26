/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;

using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

// Note: this is Experimental: change to new OPC UA client

namespace AasxOpcUa2Client
{
    public class HandledNodeId
    {
        public uint Handle;
        public NodeId Nid;

        public HandledNodeId() { }

        public HandledNodeId(uint handle, NodeId nid)
        {
            Handle = handle;
            Nid = nid;
        }
    }

    public class AasOpcUaClient2
    {
        const int ReconnectPeriod = 10;
        
        /// <summary>
        /// Good condition: starting or running
        /// </summary>
        public AasOpcUaClientStatus ClientStatus;
                
        protected LogInstance _log = null;

        protected string _endpointURL;
        protected static bool _autoAccept = true;
        protected string _userName;
        protected string _password;
        protected uint _timeOutMs = 2000;

        protected ClientSessionChannel _channel = null;

        public AasOpcUaClient2(string endpointURL, bool autoAccept, 
            string userName, string password,
            uint timeOutMs = 2000,
            LogInstance log = null)
        {
            _endpointURL = endpointURL;
            _autoAccept = autoAccept;
            _userName = userName;
            _password = password;
            _timeOutMs = timeOutMs;
            _log = log;
        }

        public async Task DirectConnect()
        {
            await StartClientAsync();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public AasOpcUaClientStatus StatusCode { get => ClientStatus; }

        public async Task StartClientAsync()
        {
            _log?.Info("1 - Starting up.");
            ClientStatus = AasOpcUaClientStatus.ErrorCreateApplication;

            // describe this client application.
            var clientDescription = new ApplicationDescription
            {
                ApplicationName = "AASX Package Explorer",
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:AASX Package Explorer",
                ApplicationType = ApplicationType.Client
            };

            // create a 'ClientSessionChannel', a client-side channel that opens a 'session' with the server.
            _channel = new ClientSessionChannel(
                clientDescription,
                null, // no x509 certificates
                new AnonymousIdentity(), // no user identity
                "" + _endpointURL,
                SecurityPolicyUris.None); // no encryption

            // try opening a session and reading a few nodes.
            await _channel.OpenAsync();

            // ok
            ClientStatus = AasOpcUaClientStatus.Running;

            // final
            _log?.Info("9 - Connection established.");
        }

        public NodeId CreateNodeId(uint value, int nsIndex)
        {
            return new NodeId(value, (ushort)nsIndex);
        }

        public NodeId CreateNodeId(string nodeName, int index)
        {
            return new NodeId(nodeName, (ushort)index);
        }

        private Dictionary<string, ushort> nsDict = null;

        public NodeId CreateNodeId(string nodeName, string ns)
        {
            // access
            if (_channel == null || nodeName == null || ns == null)
                return null;

            // find namespace
            int nsi = -1;
            int i = 0;
            foreach (var nsuri in _channel.NamespaceUris)
            {
                if (nsuri.Equals(ns, StringComparison.InvariantCultureIgnoreCase))
                {
                    nsi = i;
                    break;
                }
                i++;
            }
            if (nsi < 0)
                return null;

            // ok, directly refer to
            return new NodeId(nodeName, (ushort)nsi);
        }

        public NodeId ParseAndCreateNodeId(string input)
        {
            return NodeId.Parse(input);
        }

        public async Task<string> ReadSubmodelElementValueAsStringAsync(string nodeName)
        {
            if (_channel == null)
                return "";

            // build a ReadRequest. See 'OPC UA Spec Part 4' paragraph 5.10.2
            var readRequest = new ReadRequest
            {
                // set the NodesToRead to an array of ReadValueIds.
                NodesToRead = new[] {
                    // construct a ReadValueId from a NodeId and AttributeId.
                    new ReadValueId {
                        // you can parse the nodeId from a string.
                        // e.g. NodeId.Parse("ns=2;s=Demo.Static.Scalar.Double")
                        NodeId = NodeId.Parse(nodeName),
                        // variable class nodes have a Value attribute.
                        AttributeId = AttributeIds.Value
                    }
                }
            };
            // send the ReadRequest to the server.
            var readResult = await _channel.ReadAsync(readRequest);

            // the 'Results' array returns DataValues, one for every ReadValueId.
            var res = readResult.Results[0].GetValueOrDefault<string>();

            return res;
        }

        public async Task<DataValue> ReadNodeIdAsync(NodeId nid)
        {
            if (_channel == null)
                return null;

            // build a ReadRequest. See 'OPC UA Spec Part 4' paragraph 5.10.2
            var readRequest = new ReadRequest
            {
                // set the NodesToRead to an array of ReadValueIds.
                NodesToRead = new[] {
                    // construct a ReadValueId from a NodeId and AttributeId.
                    new ReadValueId {
                        // you can parse the nodeId from a string.
                        // e.g. NodeId.Parse("ns=2;s=Demo.Static.Scalar.Double")
                        NodeId = nid,
                        // variable class nodes have a Value attribute.
                        AttributeId = AttributeIds.Value
                    }
                }
            };
            // send the ReadRequest to the server.
            var readResult = await _channel.ReadAsync(readRequest);

            // the 'Results' array returns DataValues, one for every ReadValueId.
            var res = readResult.Results[0];

            return res;
        }

        public async Task<IDisposable> SubscribeNodeIdsAsync(
            HandledNodeId[] nids, 
            Action<uint, DataValue> handler,
            int publishingInterval = 1000)
        {
            if (_channel == null || nids == null || _channel.State != CommunicationState.Opened  || handler == null)
                return null;

            //Create a subscription
            var subscriptionRequest = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 100,
                RequestedMaxKeepAliveCount = 10,
                RequestedLifetimeCount = 30,
                PublishingEnabled = true
            };
            
            var subscriptionResponse = await _channel.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);
            var id = subscriptionResponse.SubscriptionId;
            //Add items to the subscription
            var itemsToCreate = new List<MonitoredItemCreateRequest>();
            foreach (var nid in nids)
                itemsToCreate.Add(new MonitoredItemCreateRequest
                {
                    ItemToMonitor = new ReadValueId
                    {
                        NodeId = nid.Nid,
                        AttributeId = AttributeIds.Value
                    },
                    MonitoringMode = MonitoringMode.Reporting,
                    RequestedParameters = new MonitoringParameters
                    {
                        ClientHandle = nid.Handle,
                        SamplingInterval = -1,
                        QueueSize = 0,
                        DiscardOldest = true
                    }
                });
            
            var itemsRequest = new CreateMonitoredItemsRequest
            {
                SubscriptionId = id,
                ItemsToCreate = itemsToCreate.ToArray(),
            };
            
            var itemsResponse = await _channel.CreateMonitoredItemsAsync(itemsRequest).ConfigureAwait(false);
            
            //Publish the subscription
            var publishRequest = new PublishRequest
            {
                SubscriptionAcknowledgements = new SubscriptionAcknowledgement[0]
            };

            // Publish the subscription
            var token = _channel.Where(pr => pr.SubscriptionId == id).Subscribe(pr =>
            {
                // loop thru all the data change notifications
                if (pr?.NotificationMessage?.NotificationData == null)
                    return;
                var dcns = pr.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                foreach (var dcn in dcns)
                {
                    foreach (var min in dcn.MonitoredItems)
                    {
                        if (min.Value == null)
                            continue;
                        handler?.Invoke(min.ClientHandle, min.Value);
                        Console.WriteLine($"sub: {pr.SubscriptionId}; handle: {min.ClientHandle}; value: {min.Value}");
                    }
                }
            },
            ex =>
            {
                Console.WriteLine($"IObserver handled exception '{ex.GetType()}'. {ex.Message}");
            });

            return token;
        }        
    }
}
