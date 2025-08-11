using AasxIntegrationBase;
using AasxIntegrationBase.AdminShellEvents;
using AasxPluginAssetInterfaceDescription;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.AssetInterfacesDescription;
using AdminShellNS;
using AdminShellNS.DiaryData;
using Extensions;
using FluentModbus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Description;
using Aas = AasCore.Aas3_0;

namespace AasxPluginAssetInterfaceDescription
{
    public class AidBacnetConnection : AidBaseConnection
    {
        public BacnetClient Client;
        private Dictionary<uint, BacnetAddress> DeviceAddresses = new Dictionary<uint, BacnetAddress>();
        override public async Task<bool> Open()
        {
            try
            {
                Client = new BacnetClient();
                Client.OnIam += OnIamHandler;

                if (TimeOutMs >= 10)
                { 
                    Client.Timeout = (int)TimeOutMs;
                } 

                Client.Start();
                LastActive = DateTime.Now;
                
                await Task.Yield();
                return true;
            }
            catch (Exception ex)
            {
                Client = null;
                return false;
            }
        }

        private void OnIamHandler(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxAPDU, BacnetSegmentations segmentation, ushort vendorId)
        {
            // Store the device address from I-Am response
            DeviceAddresses[deviceId] = adr;
        }

        override public bool IsConnected()
        {
            // nothing to do, this simple bacnet connection is stateless
            return Client != null;
        }

        override public void Close()
        {
            // Dispose client
            if (Client != null)
            {
                Client.Dispose();
                Client = null;
            }
        }

        override public async Task<int> UpdateItemValueAsync(AidIfxItemStatus item)
        {
            // access
            if (item?.FormData?.Href?.HasContent() != true
                || item.FormData.Bacv_useService?.HasContent() != true
                || !IsConnected())
                return 0;
            int res = 0;

            // GET?
            if (item.FormData.Bacv_useService.Trim().ToLower() == "readproperty")
            {
                try
                {
                    // Extract device ID
                    uint deviceId = uint.Parse(TargetUri.Host);

                    // Find device address
                    BacnetAddress deviceAddress;
                    if (!DeviceAddresses.ContainsKey(deviceId))
                    {
                        // Perform WhoIs request
                        Client.WhoIs((int)deviceId, (int)deviceId);
                        await Task.Delay(1000); // Wait for response
                    }

                    if (!DeviceAddresses.TryGetValue(deviceId, out deviceAddress))
                    {
                        throw new Exception($"Device {deviceId} not found.");
                    }

                    
                    // get object type,instance, and property
                    var href = item.FormData.Href.TrimStart('/');
                    string[] mainParts = href.Split('/');
                    string[] objectParts = mainParts[0].Split(',');
                    
                    // Create objectId
                    var objectType = (BacnetObjectTypes)int.Parse(objectParts[0]); 
                    uint instance = uint.Parse(objectParts[1]);
                    BacnetObjectId objectId = new BacnetObjectId(objectType, instance);
                    
                    // Get property from href
                    var propertyId = (BacnetPropertyIds)int.Parse(mainParts[1]);

                    // Read Property
                    IList<BacnetValue> values;
                    bool result = Client.ReadPropertyRequest(
                        deviceAddress,
                        objectId,
                        propertyId,                   
                        out values
                    );

                    if (result)
                    {
                        var value = values[0].Value.ToString();
                        item.Value = value;
                        res = 1;
                        NotifyOutputItems(item, value);
                    }
                }
                catch (Exception ex)
                {
                    // set breakpoint in order to get failed connections!
                }
            }

            return res;
        }


    }
}