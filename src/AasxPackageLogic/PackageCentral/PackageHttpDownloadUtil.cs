/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase.AdminShellEvents;
using AdminShellNS;
using Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;
using AasxIntegrationBase;
using System.IO;
using AasxOpenIdClient;
using System.Net.Http;
using System.Net;
using System.Threading;
using IdentityModel.Client;
using System.Reflection;
using Newtonsoft.Json;
using static AasxPredefinedConcepts.ConceptModel.ConceptModelZveiTechnicalData;
using System.Text.RegularExpressions;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This class provides an central download service for HTTP.
    /// Can handle security.
    /// Can handle fake request / responses.
    /// </summary>
    public class PackageHttpDownloadUtil
    {
        static PackageHttpDownloadUtil()
        {
            TryLoadFakeRequests(Assembly.GetExecutingAssembly(),
                "AasxPackageLogic.Resources.PackageContainerFakeAnswers.json");
        }

        public class FakeReqResp
        {
            public string Request = "";
            public string Response = "";
        }

        public class FakeAnswers
        {
            public List<FakeReqResp> Http = new List<FakeReqResp>();
        }

        protected static FakeAnswers _fakeAnswers = new FakeAnswers();

        public static void TryLoadFakeRequests(Assembly assy, string resPath)
        {
            // access resource
            using (var stream = assy?.GetManifestResourceStream(resPath))
            using (StreamReader sr = new StreamReader(stream))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = new JsonSerializer();
                _fakeAnswers = serializer.Deserialize<FakeAnswers>(reader);
            }
        }

        private static OpenIdClientInstance.UiLambdaSet GenerateUiLambdaSet(PackCntRuntimeOptions runtimeOptions = null)
        {
            var res = new OpenIdClientInstance.UiLambdaSet();

            if (runtimeOptions?.ShowMesssageBox != null)
                res.MesssageBox = (content, text, title, buttons) =>
                    runtimeOptions.ShowMesssageBox(content, text, title, buttons);

            return res;
        }

        public static async Task DownloadToMemoryStream(
            Uri sourceUri,
            Action<MemoryStream> lambdaDownloadDone,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null,
            bool allowFakeResponses = false)
        {
            // access
            if (sourceUri == null)
                return;

            // check for fake answers?
            if (allowFakeResponses && _fakeAnswers?.Http != null)
                foreach (var fa in _fakeAnswers.Http)
                    if (fa.Request.Equals(sourceUri.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (var memStream = new MemoryStream())
                        using (var writer = new StreamWriter(memStream))
                        {
                            writer.Write(AdminShellUtil.Base64Decode(fa.Response));
                            writer.Flush();
                            memStream.Flush();
                            memStream.Seek(0, SeekOrigin.Begin);
                            lambdaDownloadDone?.Invoke(memStream);
                            return;
                        }
                    }

            // TODO: debug, remove!!
            return;

            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = false;

            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Accept", "application/aas");
            client.BaseAddress = new Uri(sourceUri.GetLeftPart(UriPartial.Authority));
            var requestPath = sourceUri.PathAndQuery;

            // Log
            runtimeOptions?.Log?.Info($"HttpClient() with base-address {client.BaseAddress} " +
                $"and request {requestPath} .. ");

            // Token existing?
            var clhttp = containerList as PackageContainerListHttpRestBase;
            var oidc = clhttp?.OpenIdClient;
            if (oidc == null)
            {
                runtimeOptions?.Log?.Info("  no ContainerList available. No OpecIdClient possible!");
                if (clhttp != null && OpenIDClient.email != "")
                {
                    clhttp.OpenIdClient = new OpenIdClientInstance();
                    clhttp.OpenIdClient.email = OpenIDClient.email;
                    clhttp.OpenIdClient.ssiURL = OpenIDClient.ssiURL;
                    clhttp.OpenIdClient.keycloak = OpenIDClient.keycloak;
                    oidc = clhttp.OpenIdClient;
                }
            }
            if (oidc != null)
            {
                if (oidc.token != "")
                {
                    runtimeOptions?.Log?.Info($"  using existing bearer token.");
                    client.SetBearerToken(oidc.token);
                }
                else
                {
                    if (oidc.email != "")
                    {
                        runtimeOptions?.Log?.Info($"  using existing email token.");
                        client.DefaultRequestHeaders.Add("Email", OpenIDClient.email);
                    }
                }
            }

            bool repeat = true;

            while (repeat)
            {
                // get response?
                var response = await client.GetAsync(requestPath,
                    HttpCompletionOption.ResponseHeadersRead);

                if (clhttp != null
                    && response.StatusCode == System.Net.HttpStatusCode.TemporaryRedirect)
                {
                    string redirectUrl = response.Headers.Location.ToString();
                    // ReSharper disable once RedundantExplicitArrayCreation
                    string[] splitResult = redirectUrl.Split(new string[] { "?" },
                        StringSplitOptions.RemoveEmptyEntries);
                    splitResult[0] = splitResult[0].TrimEnd('/');

                    if (splitResult.Length < 1)
                    {
                        runtimeOptions?.Log?.Error("TemporaryRedirect, but url split to successful");
                        break;
                    }

                    runtimeOptions?.Log?.Info("Redirect to: " + splitResult[0]);

                    if (oidc == null)
                    {
                        runtimeOptions?.Log?.Info("Creating new OpenIdClient..");
                        oidc = new OpenIdClientInstance();
                        clhttp.OpenIdClient = oidc;
                        clhttp.OpenIdClient.email = OpenIDClient.email;
                        clhttp.OpenIdClient.ssiURL = OpenIDClient.ssiURL;
                        clhttp.OpenIdClient.keycloak = OpenIDClient.keycloak;
                    }

                    oidc.authServer = splitResult[0];

                    runtimeOptions?.Log?.Info($".. authentication at auth server {oidc.authServer} needed");

                    var response2 = await oidc.RequestTokenAsync(null,
                        GenerateUiLambdaSet(runtimeOptions));
                    if (oidc.keycloak == "" && response2 != null)
                        oidc.token = response2.AccessToken;
                    if (oidc.token != "" && oidc.token != null)
                        client.SetBearerToken(oidc.token);

                    repeat = true;
                    continue;
                }

                repeat = false;

                if (response.IsSuccessStatusCode)
                {
                    var contentLength = response.Content.Headers.ContentLength;
                    var contentFn = response.Content.Headers.ContentDisposition?.FileName;

                    // log
                    runtimeOptions?.Log?.Info($".. response with header-content-len {contentLength} " +
                        $"and file-name {contentFn} ..");

                    var contentStream = await response?.Content?.ReadAsStreamAsync();
                    if (contentStream == null)
                        throw new PackageContainerException(
                        $"While getting data bytes from {sourceUri.ToString()} via HttpClient " +
                        $"no data-content was responded!");

                    // create temp file and write to it
                    var givenFn = sourceUri.ToString();
                    if (contentFn != null)
                        givenFn = contentFn;
                    runtimeOptions?.Log?.Info($".. downloading and scanning by proxy/firewall {client.BaseAddress} " +
                        $"and request {requestPath} .. ");

                    using (var memStream = new MemoryStream())
                    {
                        // copy with progress
                        var bufferSize = 4024;
                        var deltaSize = 512 * 1024;
                        var buffer = new byte[bufferSize];
                        long totalBytesRead = 0;
                        long lastBytesRead = 0;
                        int bytesRead;

                        runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Starting,
                                contentLength, totalBytesRead);

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length,
                            default(CancellationToken)).ConfigureAwait(false)) != 0)
                        {
                            await memStream.WriteAsync(buffer, 0, bytesRead,
                                default(CancellationToken)).ConfigureAwait(false);

                            totalBytesRead += bytesRead;

                            if (totalBytesRead > lastBytesRead + deltaSize)
                            {
                                runtimeOptions?.Log?.Info($".. downloading to memory stream");
                                runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Ongoing,
                                    contentLength, totalBytesRead);
                                lastBytesRead = totalBytesRead;
                            }
                        }

                        // assume bytes read to be total bytes
                        runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Final,
                            totalBytesRead, totalBytesRead);

                        // log                
                        runtimeOptions?.Log?.Info($".. download done with {totalBytesRead} bytes read!");

                        // execute lambda
                        memStream.Flush();
                        memStream.Seek(0, SeekOrigin.Begin);
                        lambdaDownloadDone?.Invoke(memStream);
                    }
                }
                else
                {
                    Log.Singleton.Error("DownloadFromSource Server gave: Operation not allowed!");
                    throw new PackageContainerException($"Server operation not allowed!");
                }
            }
        }

        public static Uri GetBaseUri(string location)
        {
            // try an explicit search for known parts of ressources
            // (preserves scheme, host and leading pathes)
            var m = Regex.Match(location, @"^(.*?)(/shells|/submodel|/conceptdescription)");
            if (m.Success)
                return new Uri(m.Groups[1].ToString() + "/");

            // just go to the first slash
            var p0 = location.IndexOf("//");
            if (p0 > 0)
            {
                var p = location.IndexOf('/', p0 + 2);
                if (p > 0)
                {
                    return new Uri(location.Substring(0, p) + "/");
                }
            }

            // go to error
            return null;
        }

        //public static string CombineUri (string uri1, string uri2)
        //{
        //    var res = "" + uri1;
        //    if (uri2?.HasContent() == true)
        //    {
        //        if (!res.EndsWith("/"))
        //            res += "/";
        //        res += uri2;
        //    }
        //    return res;
        //}

        public static Uri CombineUri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null || relativeUri?.HasContent() != true)
                return null;

            if (Uri.TryCreate(baseUri, relativeUri, out var res))
                return res;

            return null;
        }

        public static Uri BuildUriForSubmodel(Uri baseUri, Aas.IReference submodelRef)
        {
            // access
            if (baseUri == null || submodelRef?.IsValid() != true 
                || submodelRef.Count() != 1 || submodelRef.Keys[0].Type != KeyTypes.Submodel)
                return null;

            // try combine
            var smidenc = AdminShellUtil.Base64Encode(submodelRef.Keys[0].Value);
            return CombineUri(baseUri, $"submodels/{smidenc}");
        }

    }
}
