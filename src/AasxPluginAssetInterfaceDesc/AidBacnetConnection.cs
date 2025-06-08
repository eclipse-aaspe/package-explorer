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

        override public async Task<bool> Open()
        {
            try
            {
                // Simple initialization
                Client = new BacnetClient();

                // Parse device ID from URI (bacnet://1234)
                //string deviceIdStr = TargetUri.Host;
                //if (!uint.TryParse(deviceIdStr, out DeviceId))
                  //  return false;

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

        override public async Task<int> UpdateItemValueAsync(AidIfxItemStatus item)
         {
            // access
            if (item?.FormData?.Href?.HasContent() != true
                || item.FormData.Modv_function?.HasContent() != true
                || !IsConnected())
                return 0;
            int res = 0;

            // Decode address and value
            var match = Regex.Match(item.FormData.Href, @"^(\d+)(\?value=(\d+))?$");
            if (!match.Success)
                return 0;
            if (!uint.TryParse(match.Groups[1].ToString(), out var address))
                return 0;
            if (match.Groups[3].Success && !uint.TryParse(match.Groups[3].ToString(), out var value))
                return 0;
            // Perform the write operation
            try
            {
                Client.WriteProperty(address, BacnetPropertyIds.PROP_PRESENT_VALUE, value);
                res = 1; // Success
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Bacnet write error: {ex.Message}");
                res = -1; // Error
            }
            await Task.Yield();
            return res;
        }
}
       