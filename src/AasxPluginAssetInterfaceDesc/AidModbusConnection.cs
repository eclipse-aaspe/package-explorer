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
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AasxPredefinedConcepts.AssetInterfacesDescription;
using FluentModbus;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AasxPluginAssetInterfaceDescription
{
    public class AidModbusConnection : AidBaseConnection
    {
        public ModbusTcpClient Client;

        override public async Task<bool> Open()
        {
            try
            {
                Client = new ModbusTcpClient();
                if (TimeOutMs >= 10)
                    Client.ConnectTimeout = (int)TimeOutMs;
                Client.Connect(new IPEndPoint(IPAddress.Parse(TargetUri.Host), TargetUri.Port));
                LastActive = DateTime.Now;

                await Task.Yield();

                return true;
            } catch (Exception ex)
            {
                Client = null;
                return false;
            }
        }

        override public bool IsConnected()
        {
            return Client != null && Client.IsConnected;
        }

        override public void Close()
        {
            if (IsConnected())
            {
                Client.Disconnect();
                Client = null;
            }
            else
            {
                Client = null;
            }
        }

        // Note: the async version of ReadHoldingRegisters seems not to work properly?
        override public int UpdateItemValue(AidIfxItemStatus item)
        {
            // access
            if (item?.FormData?.Href?.HasContent() != true
                || item.FormData.Modv_function?.HasContent() != true)
                return 0;
            int res = 0;

            // decode address + quantity
            // (assumption: 1 quantity = 2 bytes)
            var match = Regex.Match(item.FormData.Href, @"^(\d{1,5})(\?quantity=(\d+))?$");
            if (!match.Success)
                return 0;

            if (!int.TryParse(match.Groups[1].ToString(), out var address))
                return 0;
            if (!int.TryParse(match.Groups[3].ToString(), out var quantity))
                quantity = 1;
            quantity = Math.Max(0, Math.Min(0xffff, quantity));

            // perform function (id = in data)
            byte[] id = null;
            if (item.FormData.Modv_function.Trim().ToLower() == "readholdingregisters")
            {
                //Get device unitID
                int.TryParse(TargetUri.LocalPath.Replace("/", ""), out var unitID);

                // readHoldingRegisters
                id = (Client.ReadHoldingRegisters<byte>(unitID, address, 2 * quantity)).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(id);
                // time
                LastActive = DateTime.Now;
            }

            // success with reading?
            if (id == null || id.Length < 1)
                return 0;

            // swapping (od = out data)
            // https://doc.iobroker.net/#de/adapters/adapterref/iobroker.modbus/README.md?wp
            var mbtp = item.FormData.Modv_type?.ToLower();
            var byteSequence = item.FormData.Modv_mostSignificantByte;
            var wordSequence = item.FormData.Modv_mostSignificantWord;
            byte[] od = id.ToArray();
            if (quantity == 2)
            {
                if (byteSequence == "" && wordSequence == "") // //byte sequence defined at the local level

                    //byte sequence defined at the global level
                    if ((mostSignificantByte == "" && mostSignificantWord == "") || 
                        (mostSignificantByte == "true" && mostSignificantWord == "") || 
                        (mostSignificantByte == "" && mostSignificantWord == "true") || 
                        (mostSignificantByte == "true" && mostSignificantWord == "true"))
                    {
                        //use default, byte == true and word  == true (Big endian and no word swapping)
                    }

                    else if ((mostSignificantByte == "" && mostSignificantWord == "false") || 
                        (mostSignificantByte == "true" && mostSignificantWord == "false"))
                    {
                        //big endian wordswap AABBCCDD => CCDDAABB
                        od[3] = id[1]; od[2] = id[0]; od[1] = id[3]; od[0] = id[2];
                    }
                    else if ((mostSignificantByte == "false" && mostSignificantWord == "true") || 
                        (mostSignificantByte == "false" && mostSignificantWord == ""))
                    {
                        // Little Endian AABBCCDD => DDCCBBAA
                        Array.Reverse(od);
                    }

                    //byte sequence defined at the global level
                    else if ((byteSequence == "true" && wordSequence == "") || 
                        (byteSequence == "" && wordSequence == "true") || 
                        (byteSequence == "true" && wordSequence == "true"))
                    {
                        //use default, byte == true and word  == true (Big endian and no word swapping)
                    }

                    //byte sequence defined at the global level
                    else if ((byteSequence == "true" && wordSequence == "false") || 
                        (byteSequence == "" && wordSequence == "false"))
                    {
                        //big endian wordswap AABBCCDD => CCDDAABB
                        od[3] = id[1]; od[2] = id[0]; od[1] = id[3]; od[0] = id[2];
                    }

                    //byte sequence defined at the global level
                    else if ((byteSequence == "false" && wordSequence == "true") || 
                        (byteSequence == "false" && wordSequence == ""))
                    {
                        // Little Endian little endian AABBCCDD => DDCCBBAA
                        Array.Reverse(od);
                    }



            }
            else
            if (quantity == 1)
            {

                if (byteSequence == "" && mostSignificantByte == "false")  //byte sequence defined at the global level
                {
                    // little endian AABB => BBAA
                    od[1] = id[0]; od[0] = id[1];
                }

                else if (byteSequence != "" && byteSequence.ToLower() == "false")  //byte sequence defined at local level, it overrides gloabal level
                {
                    // little endian AABB => BBAA
                    od[1] = id[0]; od[0] = id[1];
                }
                else
                {
                    // big endian AABB => AABB
                }
            }

            // conversion to value
            // idea: (1) convert to binary type, (2) convert to adequate string representation
            var strval = "";
            if (mbtp == "xsd:unsignedint" && quantity >= 2)
            {
                strval = BitConverter.ToUInt32(od).ToString();
            }
            else
            if (mbtp == "xsd:integer" && quantity >= 2)
            {
                strval = BitConverter.ToInt32(od).ToString();
            }
            else
            if (mbtp == "xsd:int" && quantity >= 1)
            {
                strval = BitConverter.ToUInt16(od).ToString();
            }
            else
            if (mbtp == "xsd:int" && quantity >= 1)
            {
                strval = BitConverter.ToInt16(od).ToString();
            }
            else
            if (mbtp == "xsd:unsignedshort" && quantity >= 1)
            {
                strval = Convert.ToByte(od[0]).ToString();
            }
            else
            if (mbtp == "xsd:short" && quantity >= 1)
            {
                strval = Convert.ToSByte(od[0]).ToString();
            }
            else
            if (mbtp == "xsd:float" && quantity >= 2)
            {
                strval = BitConverter.ToSingle(od).ToString("R", CultureInfo.InvariantCulture);
            }
            else
            if (mbtp == "xsd:double" && quantity >= 4)
            {
                strval = BitConverter.ToDouble(od).ToString("R", CultureInfo.InvariantCulture);
            }
            else
            if (mbtp == "xsd:string" && quantity >= 1)
            {
                strval = BitConverter.ToString(od);
            }

            // save in item
            item.Value = strval;

            // notify
            NotifyOutputItems(item, strval);

            // ok
            return 1;
        }
    }
}
