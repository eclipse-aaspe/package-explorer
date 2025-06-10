using AasxPluginAssetInterfaceDescription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxPredefinedConcepts;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AasxPredefinedConcepts.AssetInterfacesDescription;
using FluentModbus;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net.Http;
using AdminShellNS.DiaryData;
using AasxIntegrationBase.AdminShellEvents;
using System.Drawing;
using MQTTnet;
using MQTTnet.Client;
using System.Web.Services.Description;
using AasxOpcUa2Client;
using System.IO.BACnet;
using System.Threading;
using System.Diagnostics;

namespace AasxPluginAssetInterfaceDescription
{
    public class AidBacnetConnection : AidBaseConnection
    {
        public BacnetClient Client;
        static List<BacNode> DevicesList = new List<BacNode>();

        static void handler_OnIam(BacnetClient sender, BacnetAddress adr, uint device_id, uint max_apdu, BacnetSegmentations segmentation, ushort vendor_id)
        {
            lock (DevicesList)
            {
                // Device already registered?
                foreach (BacNode bn in DevicesList)
                    if (bn.getAdd(device_id) != null) return; // Yes

                // Not already in the list
                DevicesList.Add(new BacNode(adr, device_id)); // Add it
            } // Closing brace added here
        } // Closing brace added here
        override public async Task<bool> Open()
        {
            try
            {
                // Simple initialization
                Client = new BacnetClient();

                // Parse device ID from URI (bacnet://1234)
                //string deviceIdStr = TargetUri.Host;
                //if (!uint.TryParse(deviceIdStr, out DeviceId))
                //return false;

                // Set timeout if specified
                if (TimeOutMs >= 10)
                    Client.Timeout = (int)TimeOutMs;

                // Start the client
                Client.Start();

                Client.OnIam += new BacnetClient.IamHandler(handler_OnIam);

                Client.WhoIs();
                Thread.Sleep(1000); // Allow time for responses

                Console.WriteLine("Devices discovered:");
                lock (DevicesList)
                {
                    foreach (var device in DevicesList)
                    {
                        Console.WriteLine($"Device ID: {device.device_id}, Address: {device.adr}");
                    }
                }

                await Task.Yield();
                return true;
            }
            catch
            {
                Client = null;
                return false;
            }
        }

        override public bool IsConnected()
        {
            // nothing to do, this simple http connection is stateless
            return Client != null;
        }

        override public void Close()
        {
            // nothing to do, this simple http connection is stateless
        }

        override public int UpdateItemValue(AidIfxItemStatus item)
        {
            try
            {
                // Example logic to update item value
                if (item != null && !string.IsNullOrEmpty(item.Location))
                {
                    // Simulate updating the value
                    item.Value = "UpdatedValue";
                    return 0; // Return success code
                }
                else
                {
                    return -1; // Return error code for invalid item
                }
            }
            catch (Exception ex)
            {
                // Log the exception if necessary
                Console.WriteLine($"Error updating item value: {ex.Message}");
                return -2; // Return error code for exception 
            }
        }

        public class BacNode
        {
            public BacnetAddress adr;
            public uint device_id;

            public BacNode(BacnetAddress adr, uint device_id)
            {
                this.adr = adr;
                this.device_id = device_id;
            }

            public BacnetAddress getAdd(uint device_id)
            {
                if (this.device_id == device_id)
                    return adr;
                else
                    return null;
            }
        }
    }
}