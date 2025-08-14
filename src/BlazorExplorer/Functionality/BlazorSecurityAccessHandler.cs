/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019-2021 PHOENIX CONTACT GmbH & Co. KG <opensource@phoenixcontact.com>,
author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// resharper disable all

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_1;
using AdminShellNS;
using Extensions;
using AasxIntegrationBase;
using AasxPackageLogic;
using BlazorExplorer;
using AasxPackageExplorer;
using AasxPackageLogic.PackageCentral;
using AnyUi;

namespace BlazorUI.Functionality
{
    /// <summary>
    /// Handler driven by the main application to handle decisions, configurable options
    /// and user input to select appropriate credentials for the secure access of AAS servers,
    /// Registries and Repositories.
    /// Here: Blazor specific stuff.
    /// </summary>
    public class BlazorSecurityAccessHandler : SecurityAccessHandlerLogicBase
    {
        public BlazorSecurityAccessHandler(AnyUiContextBase dc,
            IEnumerable<KnownEndpointDescription> knownEndpoints)
            : base(dc, knownEndpoints)
        {
        }

        // Remark: all functionality given by UI-abstract base class!
    }
}
