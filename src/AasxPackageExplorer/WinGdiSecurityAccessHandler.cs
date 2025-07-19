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

        /// <summary>
        /// Re-initializes the internal data structures with <c>Options</c> information.
        /// </summary>
        /// <param name="knownEndpoints"></param>
        public void ReLoad(IEnumerable<KnownEndpointDescription> knownEndpoints)
        {
            _endpoints = knownEndpoints.Select((o) => new MaintainedEndpoint { Endpoint = o }).ToList();
        }

        public async Task<HttpHeaderDataItem> DetermineAuthenticateHeader(
            string baseAddress, 
            SecurityAccessMethod? preferredInvocationtype)
        {
            // access
            if (!preferredInvocationtype.HasValue)
                return null;

            // prepare some variables
            string email = "";
            var entraid = "";

            X509Certificate2 certificate = null;
            string[] x5c = null;

            // connect to auth-server
            // TODO: make auth server URI configurable
            var handler = new HttpClientHandler { DefaultProxyCredentials = CredentialCache.DefaultCredentials };
            var client = new HttpClient(handler);

            // first request to auth-server: token endpoint
            var configUrl = "https://www.admin-shell-io.com/50001/.well-known/openid-configuration";
            var configJson = await client.GetStringAsync(configUrl);
            var config = System.Text.Json.JsonDocument.Parse(configJson);
            var tokenEndpoint = config.RootElement.GetProperty("token_endpoint").GetString();

            switch (preferredInvocationtype.Value)
            {
                case SecurityAccessMethod.Basic:
                    // TODO: get the infos
                    var userName = "Bernd";
                    var PW = "Brot";

                    // bring together
                    var pseudoToken = AdminShellUtil.Base64UrlEncode($"{userName}:{PW}");

                    // build correct header key
                    return new HttpHeaderDataItem("Authentification", $"Basic {pseudoToken.ToString()}");

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
                    // TODO: Think about to detach from/ to Windows specific UI
                    X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection,
                        "Test Certificate Select",
                        "Select a certificate from the following list to get information on that certificate",
                        X509SelectionFlag.SingleSelection);

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

            // build correct header key
            return new HttpHeaderDataItem("Authorization", $"Bearer {accessToken.ToString()}");

        }
    }
}
