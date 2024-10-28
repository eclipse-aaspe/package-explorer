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
using System.Net.Http.Headers;
using System.Text;
using AasCore.Aas3_0;
using System.ServiceModel;

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

        // TODO: Refactor
        public static async Task HttpGetToMemoryStreamOLD(
            Uri sourceUri,
            Action<MemoryStream, string> lambdaDownloadDone,
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
                            lambdaDownloadDone?.Invoke(memStream, "content.json");
                            return;
                        }
                    }

            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = false;

            var client = new HttpClient(handler);

            // TODO/CHECK: Basyx does not like this: 
            // client.DefaultRequestHeaders.Add("Accept", "application/aas");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.BaseAddress = new Uri(sourceUri.GetLeftPart(UriPartial.Authority));
            var requestPath = sourceUri.PathAndQuery;

            // Log
            runtimeOptions?.Log?.Info($"HttpClient GET() with base-address {client.BaseAddress} " +
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
                        lambdaDownloadDone?.Invoke(memStream, contentFn);
                    }
                }
                else
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // not found but also no error: intentionally do nothing
                }
                else
                {
                    Log.Singleton.Error($"DownloadFromSource server gave status code {response.StatusCode}!");
                    throw new PackageContainerException($"Unsuccessfull status code {response.StatusCode}");
                }
            }
        }

        /// <summary>
        /// Try get an HTTP ressource by GET.
        /// </summary>
        /// <param name="lambdaDownloadDoneOrFail">Called also for failed status codes!</param>
        /// <exception cref="Exception">Any exception might occur outside the HTTP status codes.</exception>
        public static async Task HttpGetToMemoryStream(
            Uri sourceUri,
            Action<HttpStatusCode, MemoryStream, string> lambdaDownloadDoneOrFail,
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
                            lambdaDownloadDoneOrFail?.Invoke(HttpStatusCode.OK, memStream, "content.json");
                            return;
                        }
                    }

            // 

            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = false;

            var client = new HttpClient(handler);

            // TODO/CHECK: Basyx does not like this: 
            // client.DefaultRequestHeaders.Add("Accept", "application/aas");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.BaseAddress = new Uri(sourceUri.GetLeftPart(UriPartial.Authority));
            var requestPath = sourceUri.PathAndQuery;

            // Log
            if (runtimeOptions?.ExtendedConnectionDebug == true)
                runtimeOptions.Log?.Info($"HttpClient GET() with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

            // Token existing?
            var clhttp = containerList as PackageContainerListHttpRestBase;
            var oidc = clhttp?.OpenIdClient;
            if (oidc == null)
            {
                if (runtimeOptions?.ExtendedConnectionDebug == true)
                    runtimeOptions.Log?.Info("  no ContainerList available. No OpecIdClient possible!");
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
                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info($"  using existing bearer token.");
                    client.SetBearerToken(oidc.token);
                }
                else
                {
                    if (oidc.email != "")
                    {
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                            runtimeOptions.Log?.Info($"  using existing email token.");
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

                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info("Redirect to: " + splitResult[0]);

                    if (oidc == null)
                    {
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                            runtimeOptions.Log?.Info("Creating new OpenIdClient..");
                        oidc = new OpenIdClientInstance();
                        clhttp.OpenIdClient = oidc;
                        clhttp.OpenIdClient.email = OpenIDClient.email;
                        clhttp.OpenIdClient.ssiURL = OpenIDClient.ssiURL;
                        clhttp.OpenIdClient.keycloak = OpenIDClient.keycloak;
                    }

                    oidc.authServer = splitResult[0];

                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info($".. authentication at auth server {oidc.authServer} needed");

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
                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info($".. response with header-content-len {contentLength} " +
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
                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info($".. downloading and scanning by proxy/firewall {client.BaseAddress} " +
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
                                if (runtimeOptions?.ExtendedConnectionDebug == true)
                                    runtimeOptions.Log?.Info($".. downloading to memory stream");
                                runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Ongoing,
                                    contentLength, totalBytesRead);
                                lastBytesRead = totalBytesRead;
                            }
                        }

                        // assume bytes read to be total bytes
                        runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Final,
                            totalBytesRead, totalBytesRead);

                        // log                
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                            runtimeOptions.Log?.Info($".. download done with {totalBytesRead} bytes read!");

                        // execute lambda
                        memStream.Flush();
                        memStream.Seek(0, SeekOrigin.Begin);
                        lambdaDownloadDoneOrFail?.Invoke(response.StatusCode, memStream, contentFn);
                    }
                }
                else
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    lambdaDownloadDoneOrFail?.Invoke(response.StatusCode, null, null);
                }
                else
                {
                    Log.Singleton.Error($"DownloadFromSource server gave status code {response.StatusCode}!");
                    lambdaDownloadDoneOrFail?.Invoke(response.StatusCode, null, null);
                }
            }
        }

        public static async Task<HttpStatusCode?> HttpPostRequestToMemoryStream(
            Uri sourceUri,
            string requestContentType,
            string requestBody,
            Action<MemoryStream, string> lambdaDownloadDone,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null,
            bool allowFakeResponses = false)
        {
            // access
            if (sourceUri == null)
                return null;

            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = false;

            var client = new HttpClient(handler);

            // client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.BaseAddress = new Uri(sourceUri.GetLeftPart(UriPartial.Authority));
            var requestPath = sourceUri.PathAndQuery;

            // Log
            if (runtimeOptions?.ExtendedConnectionDebug == true)
                runtimeOptions.Log?.Info($"HttpClient GET() with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

            // Token existing?
            var clhttp = containerList as PackageContainerListHttpRestBase;
            var oidc = clhttp?.OpenIdClient;
            if (oidc == null)
            {
                if (runtimeOptions?.ExtendedConnectionDebug == true)
                    runtimeOptions.Log?.Info("  no ContainerList available. No OpecIdClient possible!");
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
                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info($"  using existing bearer token.");
                    client.SetBearerToken(oidc.token);
                }
                else
                {
                    if (oidc.email != "")
                    {
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                            runtimeOptions.Log?.Info($"  using existing email token.");
                        client.DefaultRequestHeaders.Add("Email", OpenIDClient.email);
                    }
                }
            }

            // prepare request
            var requestContent = new StringContent(requestBody, Encoding.UTF8, requestContentType);

            // retrieve response
            bool repeat = true;

            while (repeat)
            {
                // get response?
                var response = await client.PostAsync(requestPath, requestContent);                    

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
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions?.Log?.Error("TemporaryRedirect, but url split to successful");
                        break;
                    }

                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info("Redirect to: " + splitResult[0]);

                    if (oidc == null)
                    {
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                            runtimeOptions.Log?.Info("Creating new OpenIdClient..");
                        oidc = new OpenIdClientInstance();
                        clhttp.OpenIdClient = oidc;
                        clhttp.OpenIdClient.email = OpenIDClient.email;
                        clhttp.OpenIdClient.ssiURL = OpenIDClient.ssiURL;
                        clhttp.OpenIdClient.keycloak = OpenIDClient.keycloak;
                    }

                    oidc.authServer = splitResult[0];

                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info($".. authentication at auth server {oidc.authServer} needed");

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
                    //
                    // this portion of the code is prepared to receive large sets of content data
                    //

                    var contentLength = response.Content.Headers.ContentLength;
                    var contentFn = response.Content.Headers.ContentDisposition?.FileName;

                    // log
                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions  .Log?.Info($".. response with header-content-len {contentLength} " +
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
                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions  .Log?.Info($".. downloading and scanning by proxy/firewall {client.BaseAddress} " +
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
                                if (runtimeOptions?.ExtendedConnectionDebug == true)
                                    runtimeOptions.Log?.Info($".. downloading to memory stream");
                                runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Ongoing,
                                    contentLength, totalBytesRead);
                                lastBytesRead = totalBytesRead;
                            }
                        }

                        // assume bytes read to be total bytes
                        runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.Final,
                            totalBytesRead, totalBytesRead);

                        // log                
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                            runtimeOptions.Log?.Info($".. download done with {totalBytesRead} bytes read!");

                        // execute lambda
                        memStream.Flush();
                        memStream.Seek(0, SeekOrigin.Begin);
                        lambdaDownloadDone?.Invoke(memStream, contentFn);

                        // good
                        return HttpStatusCode.OK;
                    }
                }
                else
                {
                    //
                    // assume smaller conent data
                    //
                    var responseContents = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        // not found but also no error: intentionally do nothing
                        return response.StatusCode;
                    }
                    else
                    {
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                        {
                            Log.Singleton.Error($"HttpPostRequestToMemoryStream server gave status code " +
                                $"{(int)response.StatusCode} {response.StatusCode}!");
                            Log.Singleton.Error("  response content: {0}", responseContents);
                        }
                        return response.StatusCode;
                    }
                }
            }

            return null;
        }

        public static async Task<Tuple<HttpStatusCode, string>> HttpPutPostFromMemoryStream(
            MemoryStream ms,
            Uri destUri,
            bool usePost = false,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null)
        {
            // access
            if (ms == null || destUri == null)
                return null;

            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;

            // new http client
            var client = new HttpClient(handler);
            
            client.BaseAddress = new Uri(destUri.GetLeftPart(UriPartial.Authority));
            var requestPath = destUri.PathAndQuery;

            // Log
            if (runtimeOptions?.ExtendedConnectionDebug == true)
                runtimeOptions.Log?.Info($"HttpClient PUT/POSTZ() with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

            // Token existing?
            var clhttp = containerList as PackageContainerListHttpRestBase;
            var oidc = clhttp?.OpenIdClient;
            if (oidc == null)
            {
                if (runtimeOptions?.ExtendedConnectionDebug == true)
                    runtimeOptions.Log?.Info("  no ContainerList available. No OpecIdClient possible!");
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
                    if (runtimeOptions?.ExtendedConnectionDebug == true)
                        runtimeOptions.Log?.Info($"  using existing bearer token.");
                    client.SetBearerToken(oidc.token);
                }
                else
                {
                    if (oidc.email != "")
                    {
                        if (runtimeOptions?.ExtendedConnectionDebug == true)
                            runtimeOptions.Log?.Info($"  using existing email token.");
                        client.DefaultRequestHeaders.Add("Email", OpenIDClient.email);
                    }
                }
            }

            // BEGIN Workaround behind some proxies
            // Stream is sent twice, if proxy-authorization header is not set
            string proxyFile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/proxy.dat";
            string username = "";
            string password = "";
            if (System.IO.File.Exists(proxyFile))
            {
                using (StreamReader sr = new StreamReader(proxyFile))
                {
                    // ReSharper disable MethodHasAsyncOverload
                    sr.ReadLine();
                    username = sr.ReadLine();
                    password = sr.ReadLine();
                    // ReSharper enable MethodHasAsyncOverload
                }
            }
            if (username != "" && password != "")
            {
                var authToken = Encoding.ASCII.GetBytes(username + ":" + password);
                client.DefaultRequestHeaders.ProxyAuthorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(authToken));
            }
            // END Workaround behind some proxies

            // make base64
            //var ba = ms.ToArray();
            //var base64 = Convert.ToBase64String(ba);

            // customised HttpContent to track progress
            // var data = new ProgressableStreamContent(Encoding.UTF8.GetBytes(base64), runtimeOptions);
            var data = new ProgressableStreamContent(ms.ToArray(), runtimeOptions);

            // get response?
            using (var response = (!usePost) 
                ? await client.PutAsync(requestPath, data)
                : await client.PostAsync(requestPath, data))
            {
                var content = "";
                if (response.IsSuccessStatusCode)
                    // TODO: give back?
                    content = await response.Content.ReadAsStringAsync();

                // ok!
                return new Tuple<HttpStatusCode, string>(response.StatusCode, content);
            }
        }

        public static async Task<Tuple<HttpStatusCode, string>> HttpPutPostIdentifiable(
            Aas.IIdentifiable idf,
            Uri destUri,
            bool usePost = false,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null)
        {
            // access
            if (idf == null || destUri == null)
                return null;

            // serialize to memory stream
            using (var ms = new MemoryStream())
            {
                var jsonWriterOptions = new System.Text.Json.JsonWriterOptions
                {
                    Indented = false
                };

                using (var wr = new System.Text.Json.Utf8JsonWriter(ms, jsonWriterOptions))
                {
                    // serialize
                    Jsonization.Serialize.ToJsonObject(idf).WriteTo(wr);
                    wr.Flush();
                    ms.Flush();

                    // prepare for reading again
                    ms.Seek(0, SeekOrigin.Begin);

                    // write
                    return await PackageHttpDownloadUtil.HttpPutPostFromMemoryStream(
                        ms,
                        destUri: destUri,
                        runtimeOptions: runtimeOptions,
                        containerList: containerList,
                        usePost: usePost);
                }
            }
        }

    }
}
