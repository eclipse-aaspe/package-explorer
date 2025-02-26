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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AdminShellNS;

namespace AasxOpcUa2Client
{
    public enum AasOpcUaClientStatus
    {
        Starting = 0,
        ErrorCreateApplication = 0x11,
        ErrorDiscoverEndpoints = 0x12,
        ErrorCreateSession = 0x13,
        ErrorBrowseNamespace = 0x14,
        ErrorCreateSubscription = 0x15,
        ErrorMonitoredItem = 0x16,
        ErrorAddSubscription = 0x17,
        ErrorRunning = 0x18,
        ErrorReadConfigFile = 0x19,
        ErrorNoKeepAlive = 0x30,
        ErrorInvalidCommandLine = 0x100,
        Running = 0x1000,
        Quitting = 0x8000,
        Quitted = 0x8001
    };
}
