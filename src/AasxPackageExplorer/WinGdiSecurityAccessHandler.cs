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
using Aas = AasCore.Aas3_0;

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

        protected override async Task<string> AskForAuthServerUri(string baseAddress, string authServerUri)
        {
            var ucJob = new AnyUiDialogueDataModalPanel("Specify authentification server endpoint ..");
            ucJob.ActivateRenderPanel(null,
                disableScrollArea: false,
                dialogButtons: AnyUiMessageBoxButton.OK,
                renderPanel: (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 2, new[] { "200:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5),
                                margin: new AnyUiThickness(10, 0, 30, 0));

                    panel.Add(g);

                    // dynamic rows
                    int row = 0;

                    // Info
                    helper.Set(
                        helper.AddSmallLabelTo(g, row, 0, content:
                            "For use of advanced authentification methods, the endpoint address of an authentification " +
                            "server is required! This serves the role of an identification provider and is usually provided " +
                            "by the data space.",
                            wrapping: AnyUiTextWrapping.Wrap),
                        colSpan: 2);
                    row++;

                    // separation
                    helper.AddSmallBorderTo(g, row, 0,
                        borderThickness: new AnyUiThickness(0.5), borderBrush: AnyUiBrushes.White,
                        colSpan: 2,
                        margin: new AnyUiThickness(0, 0, 0, 20));
                    row++;

                    // URI

                    helper.AddSmallLabelTo(g, row, 0, content: "Endpoint URI:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row, 1,
                                    text: $"{authServerUri}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { authServerUri = s; });

                    row++;

                    // give back
                    return g;
                });

            if (_displayContext != null && await _displayContext.StartFlyoverModalAsync(ucJob))
                return authServerUri;
            return null;
        }
    }
}
