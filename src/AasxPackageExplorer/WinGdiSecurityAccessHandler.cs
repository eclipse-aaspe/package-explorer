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
using static AasxPackageLogic.PackageCentral.PackageContainerHttpRepoSubset;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    public class WinGdiSecurityAccessHandler : ISecurityAccessHandler
    {
        protected class MaintainedEndpoint
        {
            /// <summary>
            /// Copy of the Options information
            /// </summary>
            public KnownEndpointDescription Endpoint;

            /// <summary>
            /// Last time it was renewed, e.g. for hourly access token. 
            /// <c>null</c> means it has never been established.
            /// </summary>
            public DateTime? LastRenewed = null;

            /// <summary>
            /// If not null, lastly generated HTTP header item (e.g. with a token)
            /// </summary>
            public HttpHeaderDataItem LastHeaderItem = null;
        }

        /// <summary>
        /// Holds the actual maintained data
        /// </summary>
        protected List<MaintainedEndpoint> _endpoints = new List<MaintainedEndpoint>();

        // enforce provision of display context and endpoints
        protected AnyUiContextBase _displayContext = null;
        
        public WinGdiSecurityAccessHandler(AnyUiContextBase dc,
            IEnumerable<KnownEndpointDescription> knownEndpoints) { 
            _displayContext = dc;
            _endpoints = knownEndpoints.Select((o) => new MaintainedEndpoint { Endpoint = o }).ToList();
        }

        /// <summary>
        /// This will be called by the application in order to get an HTTP header data item helping
        /// to access restricted information from an AAS server.
        /// </summary>
        public async Task<HttpHeaderDataItem> DetermineAuthenticateHeader(string baseAddress)
        {
            // can find a match in baseAddress?
            var me = _endpoints?.Find((p) => 
                        true == p?.Endpoint?.BaseAddress?.Trim().Equals(baseAddress?.Trim(), StringComparison.InvariantCultureIgnoreCase));

            // no match?
            if (me == null)
            {
                // make a new entry
                me = new MaintainedEndpoint()
                {
                    Endpoint = new KnownEndpointDescription()
                    {
                        BaseAddress = baseAddress,
                        AccessInfo = new SecurityAccessUserInfo() { Method = SecurityAccessMethod.Ask },
                    }
                };
                _endpoints.Add(me);
            }

            // can re-use existing data?
            if (me.LastRenewed != null
                && me.Endpoint?.AccessInfo != null
                && (DateTime.UtcNow - me.LastRenewed.Value).TotalMinutes < me.Endpoint.AccessInfo.RenewPeriodMins
                && me.LastHeaderItem != null)
            {
                Log.Singleton.Info("Secure access credential for {0} are still valid from: {1}. Will be re-used!",
                    baseAddress, me.LastRenewed.ToString());
                return me.LastHeaderItem;
            }

            // may ask for a method
            var methodToUse = SecurityAccessMethod.Ask;
            if (me?.Endpoint?.AccessInfo != null)
                methodToUse = me.Endpoint.AccessInfo.Method;

            // if still ask?
            if (methodToUse == SecurityAccessMethod.Ask)
            {
                var res = await AskForAccessMethod(baseAddress);
                if (res != null)
                    methodToUse = res.Value;
                else
                {
                    Log.Singleton.Error("For accessing {0}, no authentification method could be provided. Aborting!", baseAddress);
                    return null;
                }
            }

            // pass on (with E-Stop)
            return await DetermineAuthenticateHeaderForEndpoint(baseAddress, me, methodToUse);
        }

        protected async Task<SecurityAccessMethod?> AskForAccessMethod(string baseAddress)
        {
            // define dialogue and map presets into dialogue items
            var uc = new AnyUiDialogueDataSelectFromList(
                            "Select which access method shall be used ..");
            uc.ListOfItems = new AnyUiDialogueListItemList(new[] {
                                new AnyUiDialogueListItem("No security", SecurityAccessMethod.None),
                                new AnyUiDialogueListItem("Basic (Username, PW)", SecurityAccessMethod.Basic),
                                new AnyUiDialogueListItem("Certificate Store", SecurityAccessMethod.CertificateStore),
                                new AnyUiDialogueListItem("Certificate File", SecurityAccessMethod.File),
                                new AnyUiDialogueListItem("Interactive by browser", SecurityAccessMethod.InteractiveEntry)
                            });

            // perform dialogue
            if (_displayContext != null 
                && await _displayContext.StartFlyoverModalAsync(uc)
                && uc.Result && uc.ResultItem?.Tag is SecurityAccessMethod sam) 
                return sam;
            return null;
        }

        protected async Task<Tuple<string, string>> AskForUsernamePassword(string baseAddress, string username, string password)
        {
            var ucJob = new AnyUiDialogueDataModalPanel("Complete Basic authentification data ..");
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
                        helper.AddSmallLabelTo(g, row, 0, content: $"Intended base address: {baseAddress}"),
                        colSpan: 2);
                    row++;

                    // separation
                    helper.AddSmallBorderTo(g, row, 0,
                        borderThickness: new AnyUiThickness(0.5), borderBrush: AnyUiBrushes.White,
                        colSpan: 2,
                        margin: new AnyUiThickness(0, 0, 0, 20));
                    row++;

                    // Username

                    helper.AddSmallLabelTo(g, row, 0, content: "Username:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row, 1,
                                    text: $"{username}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { username = s; });

                    row++;

                    // Password

                    helper.AddSmallLabelTo(g, row, 0, content: "Password:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g, row, 1,
                                    text: $"{password}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                            (s) => { password = s; });

                    row++;

                    // give back
                    return g;
                });

            if (_displayContext != null && await _displayContext.StartFlyoverModalAsync(ucJob))
                return new Tuple<string, string>(username, password);
            return null;
        }

        protected async Task<string> AskForAuthServerUri(string baseAddress, string authServerUri)
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

        protected async Task<X509Certificate2Collection> AskForSelectFromCertCollection(
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

        protected async Task<HttpHeaderDataItem> DetermineAuthenticateHeaderForEndpoint(
            string baseAddress,
            MaintainedEndpoint endpoint,
            SecurityAccessMethod preferredMethod)
        {
            // need to have some data accessible!
            if (endpoint?.Endpoint?.AccessInfo == null)
                return null;

            // some of the methods are handled without connecting to auth-server
            switch (preferredMethod)
            {
                case SecurityAccessMethod.None:
                    // null shall be perfectly valid
                    return null;

                case SecurityAccessMethod.Basic:
                    // get the infos shorter
                    var userName = endpoint.Endpoint.AccessInfo.Username;
                    var PW = endpoint.Endpoint.AccessInfo.Password;

                    // to be completed
                    if (userName?.HasContent() != true
                        || PW?.HasContent() != true)
                    {
                        var res = await AskForUsernamePassword(baseAddress, userName, PW);
                        if (res != null)
                        {
                            userName = res.Item1;
                            PW = res.Item2;
                        }
                    }

                    // bring together
                    var pseudoToken = AdminShellUtil.Base64UrlEncode($"{userName}:{PW}");

                    // build correct header key, remember, return
                    endpoint.LastRenewed = DateTime.UtcNow;
                    endpoint.LastHeaderItem = new HttpHeaderDataItem("Authentification", $"Basic {pseudoToken.ToString()}");
                    return endpoint.LastHeaderItem;
            }

            // prepare some variables
            string email = "";
            var entraid = "";

            X509Certificate2 certificate = null;
            string[] x5c = null;

            // connect to auth-server
            // check if the auth-server could be asked for
            // var authConfigUrl = "https://www.admin-shell-io.com/50001/.well-known/openid-configuration";
            var authConfigUrl = "" + endpoint.Endpoint.AccessInfo.AuthServer;
            if (authConfigUrl?.HasContent() != true)
            {
                var res = await AskForAuthServerUri(baseAddress, authConfigUrl);
                if (res != null)
                {
                    authConfigUrl = res;
                }
            }
            
            // still not?
            if (authConfigUrl?.HasContent() != true)
            {
                Log.Singleton.Info(StoredPrint.Color.Blue, 
                    "For accessing {0}, no authentification server URI could be provided. Using no security!", baseAddress);
                return null;
            }

            // first request to auth-server: token endpoint
            var handler = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
            var client = new HttpClient(handler);
            var configJson = "";
            try
            {
                configJson = await client.GetStringAsync(authConfigUrl);
            }
            catch (Exception ex)
            {
                Log.Singleton.Error(ex, $"when querying authentification server {authConfigUrl}");
                return null;
            }

            var config = System.Text.Json.JsonDocument.Parse(configJson);
            var tokenEndpoint = config.RootElement.GetProperty("token_endpoint").GetString();

            // other methods are dependent on auth-server
            switch (preferredMethod)
            {
                case SecurityAccessMethod.CertificateStore:

                    // load root certs from auth-server
                    List<string> rootCertSubjects = new List<string>();
                    if (config.RootElement.TryGetProperty("rootCertSubjects", out System.Text.Json.JsonElement rootCerts))
                    {
                        foreach (var subject in rootCerts.EnumerateArray())
                        {
                            var s = subject.GetString();
                            if (s != null)
                            {
                                rootCertSubjects.Add(s);
                            }
                        }
                    }

                    // access X509 store (user certificates)
                    X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                    X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                    X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(
                        X509FindType.FindByTimeValid, DateTime.Now, false);

                    // get user certificate chain
                    Boolean rootCertFound = false;
                    X509Certificate2Collection fcollection2 = new X509Certificate2Collection();
                    foreach (X509Certificate2 fc in fcollection)
                    {
                        X509Chain fch = new X509Chain();
                        fch.Build(fc);
                        foreach (X509ChainElement element in fch.ChainElements)
                        {
                            if (rootCertSubjects.Contains(element.Certificate.Subject))
                            {
                                rootCertFound = true;
                                fcollection2.Add(fc);
                            }
                        }
                    }
                    if (rootCertFound)
                        fcollection = fcollection2;

                    // let user choose certificate 
                    var scollection = await AskForSelectFromCertCollection(baseAddress, fcollection);
                    if (scollection == null)
                    {
                        Log.Singleton.Info(StoredPrint.Color.Blue,
                            "For accessing {0}, no valid certificate could be provided. Using no security!", baseAddress);
                        return null;
                    }

                    // build BASE64 certificate chain
                    if (scollection.Count != 0)
                    {
                        certificate = scollection[0];
                        X509Chain ch = new X509Chain();
                        ch.Build(certificate);

                        string[] X509Base64 = new string[ch.ChainElements.Count];

                        int j = 0;
                        foreach (X509ChainElement element in ch.ChainElements)
                        {
                            X509Base64[j++] = Convert.ToBase64String(element.Certificate.GetRawCertData());
                        }

                        x5c = X509Base64;
                    }
                    break;

                case SecurityAccessMethod.File:
                    // TODO: make certificate path configurable
                    certificate = new X509Certificate2("../../../Andreas_Orzelski_Chain.pfx", "i40");

                    // Zertifikatskette vorbereiten
                    var chain = new X509Certificate2Collection();
                    chain.Import("../../../Andreas_Orzelski_Chain.pfx", "i40");
                    x5c = chain.Cast<X509Certificate2>().Reverse().Select(c => Convert.ToBase64String(c.RawData)).ToArray();
                    break;

                case SecurityAccessMethod.InteractiveEntry:

                    // test tenant for ENTRA id
                    // TODO: configure clientId
                    var tenant = "common"; // Damit auch externe Konten wie @live.de funktionieren
                    var clientId = "865f6ac0-cdbc-44c6-98cc-3e35c39ecb6e"; // aus der App-Registrierung
                    var scopes = new[] { "openid", "profile", "email" }; // für ID Token im JWT-Format

                    // let Windows start a browser to select 
                    var app = PublicClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority(AzureCloudInstance.AzurePublic, tenant)
                        .WithDefaultRedirectUri()             // entspricht http://localhost
                        .Build();

                    var result = await app
                        .AcquireTokenInteractive(scopes)
                        .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                        .ExecuteAsync();

                    entraid = result.IdToken;
                    break;
            }

            if (entraid == "")
            {
                if (certificate == null || x5c == null)
                    return null;

                // E-Mail as user extract
                email = certificate.GetNameInfo(X509NameType.EmailName, false);
                if (string.IsNullOrEmpty(email))
                {
                    var subject = certificate.Subject;
                    var match = subject.Split(',').FirstOrDefault(s => s.Trim().StartsWith("E="));
                    email = match?.Split('=')[1];
                }
            }

            // JWT create
            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, "client.jwt"),
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
            };
            JwtSecurityToken token = null;

            // add X5C to token header or ENTRA id to token
            if (entraid == "")
            {
                claims.Add(new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, email!));

                var credentials = new X509SigningCredentials(certificate);
                token = new JwtSecurityToken(
                    issuer: "client.jwt",
                    audience: tokenEndpoint,
                    claims: claims,
                    notBefore: now,
                    expires: now.AddMinutes(1),
                    signingCredentials: credentials
                );
                token.Header["x5c"] = x5c;
            }
            else
            {
                claims.Add(new("entraid", entraid));

                // var secret = "test-with-entra-id-34zu8934h89ehhghbgeg54tgfbufrbbssdbsbibu4trui45tr";
                var secret = entraid;
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                token = new JwtSecurityToken(
                    issuer: "client.jwt",
                    audience: tokenEndpoint,
                    claims: claims,
                    notBefore: now,
                    expires: now.AddMinutes(1),
                    signingCredentials: credentials
                );
            }

            // build JWT token
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            // token requets to auth-server
            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "scope", "resource1.scope1" },
                    { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                    { "client_assertion", jwt }
                })
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            // second request to auth-server: get token
            var response = await client.SendAsync(request);
            var contentStr = await response.Content.ReadAsStringAsync();

            // de-compose JSON object
            var contentJson = JObject.Parse(contentStr);
            if (!AdminShellUtil.DynamicHasProperty(contentJson, "access_token"))
                return null;
            var accessToken = contentJson["access_token"];

            // build correct header key, remember, return
            endpoint.LastRenewed = DateTime.UtcNow;
            endpoint.LastHeaderItem = new HttpHeaderDataItem("Authorization", $"Bearer {accessToken.ToString()}");
            return endpoint.LastHeaderItem;
        }
    }
}
