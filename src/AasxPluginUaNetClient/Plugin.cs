/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>,
Author: Michael Hoffmeister.

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

For OPC Content:

Copyright (c) 1996-2016, OPC Foundation. All rights reserved.
The source code in this file is covered under a dual-license scenario:
 - RCL: for OPC Foundation members in good-standing
 - GPL V2: everybody else
RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
GNU General Public License as published by the Free Software Foundation
version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
This source code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE
*/

#define OPCUA2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

#if OPCUA2
using AasxOpcUa2Client;
using AdminShellNS;
#else
#endif

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    // ReSharper disable once UnusedType.Global
    public class AasxPlugin : AasxPluginBase
    {
        public new void InitPlugin(string[] args)
        {
            PluginName = "AasxPluginUaNetClient";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            var res = ListActionsBasicHelper(
                enableCheckVisualExt: false,
                enableLicenses: true);
#if OPCUA2
            res.Add(new AasxPluginActionDescriptionBase("create-client",
                "Creates a OPC UA client and returns as plain object. Arguments: (string _endpointURL, "
                + "bool _autoAccept, int _stopTimeout, string _userName, string _password).",
                useAsync: true));
            res.Add(new AasxPluginActionDescriptionBase("read-sme-value",
                "Reads a value and returns as plain object. Arguments: (UASampleClient client, string nodeName, "
                + "int index).",
                useAsync: true));
#else
            res.Add(new AasxPluginActionDescriptionBase("create-client",
                "Creates a OPC UA client and returns as plain object. Arguments: (string _endpointURL, "
                + "bool _autoAccept, int _stopTimeout, string _userName, string _password)."));
            res.Add(new AasxPluginActionDescriptionBase("read-sme-value",
                "Reads a value and returns as plain object. Arguments: (UASampleClient client, string nodeName, "
                + "int index)."));
#endif
            return res.ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense =
                    "This application uses the OPC Foundation .NET Standard stack. See: OPC REDISTRIBUTABLES "
                    + "Agreement of Use." + Environment.NewLine +
                    "The OPC UA Example Code of OPC UA Standard is licensed under the MIT license (MIT).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

#if OPCUA2
#else
            if (action == "create-client")
            {
                // OPC Copyright
                MessageBox.Show(
                    "Copyright (c) 2018-2023 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>, " +
                    "author: Andreas Orzelski\n\n" +
                    "Portions copyright (c) by OPC Foundation, Inc. and licensed under the Reciprocal Community " +
                    "License (RCL)\n" + "see https://opcfoundation.org/license/rcl.html",
                    "Plugin Notice"
                    );

                // check for arguments
                if (args == null || args.Length != 5 || !(args[0] is string && args[1] is bool && args[2] is int
                    && args[3] is string && args[4] is string))
                {
                    _log.Info("create-client() call with wrong arguments. Expected: (string _endpointURL, "
                        + "bool _autoAccept, int _stopTimeout, string _userName, string _password)");
                    return null;
                }

                // re-establish arguments
                var _endpointURL = args[0] as string;
                var _autoAccept = (bool)args[1];
                var _stopTimeout = (int)args[2];
                var _userName = args[3] as string;
                var _password = args[4] as string;

                // make client
                var client = new SampleClient.UASampleClient(_endpointURL, _autoAccept, _stopTimeout,
                    _userName, _password);
                client.ConsoleSampleClient().Wait();

                // return as plain object
                var res = new AasxPluginResultBaseObject();
                res.strType = "UASampleClient";
                res.obj = client;
                return res;
            }

            if (action == "read-sme-value")
            {
                // check for arguments
                if (args == null || args.Length != 3 || !(args[0] is SampleClient.UASampleClient
                    && args[1] is string && args[2] is int))
                {
                    _log.Info("read-sme-value() call with wrong arguments. Expected: (UASampleClient client, "
                        + "string nodeName, int index)");
                    return null;
                }

                // re-establish arguments
                var client = args[0] as SampleClient.UASampleClient;
                var nodeName = args[1] as string;
                var Namespace = (int)args[2];

                // make the call
                var value = client?.ReadSubmodelElementValue(nodeName, Namespace);

                // return as plain object
                var res = new AasxPluginResultBaseObject();
                res.strType = "value object";
                res.obj = value;
                return res;
            }

#endif
            return null;
        }

        public new async Task<object> ActivateActionAsync(string action, params object[] args)
        {
#if OPCUA2
            if (action == "create-client")
            {
                // check for arguments
                if (args == null || args.Length != 5 || !(args[0] is string && args[1] is bool && args[2] is int
                    && args[3] is string && args[4] is string))
                {
                    _log.Info("create-client() call with wrong arguments. Expected: (string _endpointURL, "
                        + "bool _autoAccept, int _stopTimeout, string _userName, string _password)");
                    return null;
                }

                // re-establish arguments
                var _endpointURL = args[0] as string;
                var _autoAccept = (bool)args[1];
                var _stopTimeout = (int)args[2];
                var _userName = args[3] as string;
                var _password = args[4] as string;

                try
                {

                    // make client
                    var client = new AasOpcUaClient2(
                        endpointURL: _endpointURL,
                        autoAccept: _autoAccept,
                        // timeOutMs: (uint)_stopTimeout,
                        userName: _userName, password: _password);

                    await client.DirectConnect();

                    // return as plain object
                    var res = new AasxPluginResultBaseObject();
                    res.strType = "UASampleClient";
                    res.obj = client;
                    return res;

                } catch (Exception ex)
                {
                    _log?.Error(ex, "create OPC UA client");
                }

                return null;
            }

            if (action == "read-sme-value")
            {
                // check for arguments
                if (args == null || args.Length != 3 || !(args[0] is AasOpcUaClient2 client
                    && args[1] is string && args[2] is int))
                {
                    _log.Info("read-sme-value() call with wrong arguments. Expected: (UASampleClient client, "
                        + "string nodeName, int index)");
                    return null;
                }

                // re-establish arguments
                var nodeName = args[1] as string;
                var nsIndex = (int)args[2];

                try
                {

                    // make the call
                    var nid = client?.CreateNodeId(nodeName, nsIndex);
                    var value = (await client?.ReadNodeIdAsync(nid))?.Value;

                    // return as plain object
                    var res = new AasxPluginResultBaseObject();
                    res.strType = "value object";
                    res.obj = value;
                    return res;

                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "read node");
                }
            }

#endif
            return null;
        }
    }
}
