/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxMqttClient;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AdminShellNS;
using AnyUi;
using Extensions;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Aas = AasCore.Aas3_1;

namespace AasxPackageExplorer
{
    /// <summary>
    /// Handler driven by the main application to handle decisions, configurable options
    /// and user input to select appropriate credentials for the secure access of AAS servers,
    /// Registries and Repositories.
    /// Here: Windows specific stuff.
    /// </summary>
    public class WinGdiSecurityAccessHandler : SecurityAccessHandlerLogicBase
    {
        
        public WinGdiSecurityAccessHandler(AnyUiContextBase dc,
            IEnumerable<KnownEndpointDescription> knownEndpoints) 
            : base(dc, knownEndpoints)
        { 
        }

        // Remark: basically all functionality given by UI-abstract base class!

        protected override async Task<X509Certificate2Collection> AskForSelectFromCertCollection(
            string baseAddress,
            X509Certificate2Collection fcollection)
        {
            //
            // The reasonable way: let Windows do its thing!
            // Rationale: The correct way is that the user will provide the certificate.
            // Consequently, the certificate shall be on the users' computer.
            // Running on Blazor, the application would see the certificates of the server
            // and NOT of the user. Therefore, this is a feasible scenario for some use-cases
            // where the server acts as a gateway, but security-wise, this scenario is very
            // questionable.
            //

            // access
            await Task.Yield();
            if (fcollection == null)
                return null;

            if (false)
            {
                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection,
                    "Certificate Select",
                    "Select a certificate for authentification on AAS server",
                    X509SelectionFlag.SingleSelection);

                return scollection;
            }
            else
            {
                // define dialogue and map presets into dialogue items
                var uc = new AnyUiDialogueDataSelectFromList(
                            "Select a certificate for authentification on AAS server");
                uc.ListOfItems = new AnyUiDialogueListItemList(fcollection.Select((o) => new AnyUiDialogueListItem(
                    $"Issuer: {o.Issuer} Not before: {o.NotBefore.ToShortDateString()} Not after: {o.NotAfter.ToShortDateString()}",
                    o)));

                // perform dialogue
                if (_displayContext != null
                    && await _displayContext.StartFlyoverModalAsync(uc)
                    && uc.Result && uc.ResultItem?.Tag is X509Certificate2 cert)
                    return new X509Certificate2Collection(cert);
                return null;
            }
        }
    }
}
