/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxOpenIdClient;
using AdminShellNS;
using Namotion.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_1;

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

        public static HttpClient CreateHttpClient(
            Uri baseUri,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null)
        {
            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = true;

            // new http client
            var client = new HttpClient(handler);

            // TODO/CHECK: Basyx does not like this: 
            // client.DefaultRequestHeaders.Add("Accept", "application/aas");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.BaseAddress = new Uri(baseUri.GetLeftPart(UriPartial.Authority));

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

            return client;
        }

        /// <summary>
        /// Try get an HTTP ressource by GET.
        /// </summary>
        /// <param name="lambdaDownloadDoneOrFail">Called also for failed status codes!</param>
        /// <exception cref="Exception">Any exception might occur outside the HTTP status codes.</exception>
        public static async Task HttpGetToMemoryStream(
            HttpClient reUseClient,
            Uri sourceUri,
            Action<HttpStatusCode, MemoryStream, string> lambdaDownloadDoneOrFail,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null,
            bool allowFakeResponses = false,
            bool doNotLogExceptions = false)
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

            // client
            var client = reUseClient ?? CreateHttpClient(sourceUri, runtimeOptions, containerList);
            var requestPath = sourceUri.PathAndQuery;

            // Log
            if (runtimeOptions?.ExtendedConnectionDebug == true)
                runtimeOptions.Log?.Info($"HttpClient GET() with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

            // make a request
            HttpResponseMessage response = null;
            using (var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, requestPath))
            {
                // assume headers to be for authorization
                if (runtimeOptions?.HttpHeaderData?.Headers != null)
                    foreach (var header in runtimeOptions.HttpHeaderData?.Headers)
                    {
                        requestMessage.Headers.Add(header.Key, header.Value);
                    }

                response = await client.SendAsync(requestMessage,
                    HttpCompletionOption.ResponseHeadersRead);
            }

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

                    runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.StartDownload,
                            contentLength, totalBytesRead);

                    // MIHO, 25-06-11: not sure if this timeout works
                    using var cts = new CancellationTokenSource(5000);

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length,
                        cts.Token).ConfigureAwait(false)) != 0)
                    {
                        await memStream.WriteAsync(buffer, 0, bytesRead,
                            default(CancellationToken)).ConfigureAwait(false);

                        totalBytesRead += bytesRead;

                        if (totalBytesRead > lastBytesRead + deltaSize)
                        {
                            if (runtimeOptions?.ExtendedConnectionDebug == true)
                                runtimeOptions.Log?.Info($".. downloading to memory stream");
                            runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.PerformDownload,
                                contentLength, totalBytesRead);
                            lastBytesRead = totalBytesRead;
                        }
                    }

                    // assume bytes read to be total bytes
                    runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.EndDownload,
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
                if (!doNotLogExceptions)
                    Log.Singleton.Error($"DownloadFromSource server gave status code {response.StatusCode} when fetching {sourceUri}!");
                lambdaDownloadDoneOrFail?.Invoke(response.StatusCode, null, null);
            }
        }

        public static async Task<HttpStatusCode?> HttpPostRequestToMemoryStream(
            HttpClient reUseClient,
            Uri sourceUri,
            string requestContentType,
            string requestBody,
            Action<MemoryStream, string> lambdaDownloadDone,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null)
        {
            // access
            if (sourceUri == null)
                return null;

            // client
            var client = reUseClient ?? CreateHttpClient(sourceUri, runtimeOptions, containerList);
            var requestPath = sourceUri.PathAndQuery;

            // Log
            if (runtimeOptions?.ExtendedConnectionDebug == true)
                runtimeOptions.Log?.Info($"HttpClient POST() request with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

            // prepare request
            var requestContent = new StringContent(requestBody, Encoding.UTF8, requestContentType);

            // retrieve response
            var response = await client.PostAsync(requestPath, requestContent);

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

                    runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.StartDownload,
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
                            runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.PerformDownload,
                                contentLength, totalBytesRead);
                            lastBytesRead = totalBytesRead;
                        }
                    }

                    // assume bytes read to be total bytes
                    runtimeOptions?.ProgressChanged?.Invoke(PackCntRuntimeOptions.Progress.EndDownload,
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

        /// <summary>
        /// Does es PUT/POST
        /// </summary>
        /// <param name="reUseClient"></param>
        /// <param name="ms"></param>
        /// <param name="destUri"></param>
        /// <param name="usePost"></param>
        /// <param name="runtimeOptions"></param>
        /// <param name="containerList"></param>
        /// <param name="mpParamName">If set with a name, name of the parameter in the multi part body to be transferred as a file</param>
        /// <returns></returns>
        public static async Task<Tuple<HttpStatusCode, string>> HttpPutPostFromMemoryStream(
            HttpClient reUseClient,
            MemoryStream ms,
            Uri destUri,
            bool usePost = false,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null,
            bool useMultiPart = false,
            string mpParamName = null,
            string mpFileName = null,
            string mpContentType = null)            
        {
            // access
            if (ms == null || destUri == null)
                return null;

            // client
            var client = reUseClient ?? CreateHttpClient(destUri, runtimeOptions, containerList);
            var requestPath = destUri.PathAndQuery;

            // Log
            if (runtimeOptions?.ExtendedConnectionDebug == true)
                runtimeOptions.Log?.Info($"HttpClient PUT/POST() with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

            // make overall content
            HttpContent overallContent = null;
            if (useMultiPart 
                && mpParamName?.HasContent() == true 
                && mpFileName?.HasContent() == true 
                && mpContentType?.HasContent() == true)
            {
#if __code_worked_for_aasxserver
                // Note: may re-define reUseClient's headers!!
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));

                overallContent = new MultipartFormDataContent();

                var fileContent = new StreamContent(ms);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mpContentType);

                // Adding the file as form-data
                (overallContent as MultipartFormDataContent).Add(fileContent, mpParamName, mpFileName);
#else
                // code worked for BaSyx
                var multipart = new MultipartFormDataContent();

                ms.Position = 0;
                var fileContent = new StreamContent(ms);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(mpContentType);

                // Add the file (e.g., field name = "file")
                multipart.Add(fileContent, mpParamName, mpFileName);

                // Add required string parameter: fileName (this was the key enabler!)
                multipart.Add(new StringContent(mpFileName), "fileName");

                overallContent = multipart;
#endif
            }
            else
            {
                // var data = new ProgressableStreamContent(ms.ToArray(), runtimeOptions);
                overallContent = new ByteArrayContent(ms.ToArray());
                overallContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                // var data = new StringContent("1.2345", Encoding.UTF8, "application/json");
            }

            // get response?
            using (var response = (!usePost) 
                ? await client.PutAsync(requestPath, overallContent)
                : await client.PostAsync(requestPath, overallContent))
            {
                // always get back content to allow debug
                var content = await response.Content.ReadAsStringAsync();

                // ok!
                return new Tuple<HttpStatusCode, string>(response.StatusCode, content);
            }
        }

        /// <summary>
        /// Send a DELETE to the client.
        /// </summary>
        /// <param name="availClient">If not <c>null</c>, re-use client</param>
        public static async Task<Tuple<HttpStatusCode, string>> HttpDeleteUri(
            HttpClient reUseClient,
            Uri delUri,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null)
        {
            // access
            if (delUri == null)
                return null;

            // client
            var client = reUseClient ?? CreateHttpClient(delUri, runtimeOptions, containerList);
            var requestPath = delUri.PathAndQuery;

            // Log
            if (runtimeOptions?.ExtendedConnectionDebug == true)
                runtimeOptions.Log?.Info($"HttpClient DELETE() with base-address {client.BaseAddress} " +
                    $"and request {requestPath} .. ");

            // get response?
            using (var response = await client.DeleteAsync(requestPath))
            {
                // try read in any case, despite:
                // https://stackoverflow.com/questions/17640666/httpclient-deleteasync-and-content-readadstringasync-always-return-null
                var content = "";
                if (response.StatusCode != HttpStatusCode.NoContent)
                    await response.Content.ReadAsStringAsync();

                // ok!
                return new Tuple<HttpStatusCode, string>(response.StatusCode, content);
            }
        }

        public static async Task<Tuple<HttpStatusCode, string>> HttpPutPostIdentifiable(
            HttpClient reUseClient,
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
                        reUseClient,
                        ms,
                        destUri: destUri,
                        runtimeOptions: runtimeOptions,
                        containerList: containerList,
                        usePost: usePost);
                }
            }
        }

        public static async Task<Tuple<HttpStatusCode, string>> HttpPutPostBytes(
            HttpClient reUseClient,
            byte[] ba,
            Uri destUri,
            bool usePost = false,
            PackCntRuntimeOptions runtimeOptions = null,
            PackageContainerListBase containerList = null)
        {
            // access
            if (ba == null || destUri == null)
                return null;

            // serialize to memory stream
            using (var ms = new MemoryStream(ba))
            {
                // write
                return await PackageHttpDownloadUtil.HttpPutPostFromMemoryStream(
                    reUseClient,
                    ms,
                    destUri: destUri,
                    runtimeOptions: runtimeOptions,
                    containerList: containerList,
                    usePost: usePost);
            }
        }

        /// <summary>
        /// This utility is able to parallel download Identifiables and will call lambda upon.
        /// Insted of a list of location, it is taking a list of objects (entities) and a lambda
        /// to extract the location from.
        /// </summary>
        /// <typeparam name="T">Type of Identifiable</typeparam>
        /// <typeparam name="E">Type of entity element</typeparam>
        public static async Task<int> DownloadListOfIdentifiables<T, E>(
            HttpClient reUseClient,
            IEnumerable<E> entities,
            Func<E, Uri> lambdaGetLocation,
            Action<HttpStatusCode, T, string, E> lambdaDownloadDoneOrFail,
            PackCntRuntimeOptions runtimeOptions = null,
            bool allowFakeResponses = false,
            bool useParallel = false,
            Func<E, Type> lambdaGetTypeToSerialize = null) where T : Aas.IIdentifiable
        {
            // access
            if (entities == null)
                return 0;

            // result
            int numRes = 0;

            // lambda for deserialize
            Func<JsonNode, E, T> lambdaDeserialize = (node, ent) =>
            {
                if (typeof(T).IsInterface && lambdaGetTypeToSerialize != null)
                {
                    var t = lambdaGetTypeToSerialize(ent);
                    if (t.IsAssignableTo(typeof(Aas.IAssetAdministrationShell)))
                        return (T)((Aas.IIdentifiable)Jsonization.Deserialize.AssetAdministrationShellFrom(node));
                    if (t.IsAssignableTo(typeof(Aas.ISubmodel)))
                        return (T)((Aas.IIdentifiable)Jsonization.Deserialize.SubmodelFrom(node));
                    if (t.IsAssignableTo(typeof(Aas.IConceptDescription)))
                        return (T)((Aas.IIdentifiable)Jsonization.Deserialize.ConceptDescriptionFrom(node));
                }

                if (typeof(T).IsAssignableFrom(typeof(Aas.IAssetAdministrationShell)))
                    return (T)((Aas.IIdentifiable)Jsonization.Deserialize.AssetAdministrationShellFrom(node));
                if (typeof(T).IsAssignableFrom(typeof(Aas.ISubmodel)))
                    return (T)((Aas.IIdentifiable)Jsonization.Deserialize.SubmodelFrom(node));
                if (typeof(T).IsAssignableFrom(typeof(Aas.IConceptDescription)))
                    return (T)((Aas.IIdentifiable)Jsonization.Deserialize.ConceptDescriptionFrom(node));
                return default(T);
            };

            // over all locations
            if (!useParallel)
            {
                foreach (var ent in entities)
                {
                    await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                        reUseClient,
                        sourceUri: lambdaGetLocation?.Invoke(ent),
                        allowFakeResponses: allowFakeResponses,
                        runtimeOptions: runtimeOptions,
                        lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                        {
                            // not OK?
                            if (code != HttpStatusCode.OK)
                            {
                                lambdaDownloadDoneOrFail?.Invoke(code, default(T), null, ent);
                                return;
                            }

                            // go on
                            try
                            {
                                var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                T idf = lambdaDeserialize(node, ent);
                                lambdaDownloadDoneOrFail?.Invoke(code, idf, contentFn, ent);
                                if (code == HttpStatusCode.OK)
                                    numRes++;
                            }
                            catch (Exception ex)
                            {
                                runtimeOptions?.Log?.Error(ex, $"Parsing downloaded {typeof(T).GetDisplayName()}");
                                lambdaDownloadDoneOrFail?.Invoke(HttpStatusCode.UnprocessableEntity, default(T), null, ent);
                            }
                        });
                }
            }
            else
            {
                await Parallel.ForEachAsync(entities,
                    new ParallelOptions() { 
                        MaxDegreeOfParallelism = Options.Curr.MaxParallelReadOps,
                        CancellationToken = runtimeOptions?.CancellationTokenSource?.Token ?? CancellationToken.None
                    },
                    async (ent, token) =>
                    {
                        var thisEnt = ent;
                        await PackageHttpDownloadUtil.HttpGetToMemoryStream(
                            reUseClient,
                            sourceUri: lambdaGetLocation?.Invoke(ent),
                            allowFakeResponses: allowFakeResponses,
                            runtimeOptions: runtimeOptions,
                            lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                            {
                                // not OK?
                                if (code != HttpStatusCode.OK)
                                {
                                    lambdaDownloadDoneOrFail?.Invoke(code, default(T), null, ent);
                                    return;
                                }

                                // go on
                                try
                                {
                                    var node = System.Text.Json.Nodes.JsonNode.Parse(ms);
                                    T idf = lambdaDeserialize(node, ent);
                                    lambdaDownloadDoneOrFail?.Invoke(code, idf, contentFn, thisEnt);
                                    if (code == HttpStatusCode.OK)
                                        numRes++;
                                }
                                catch (Exception ex)
                                {
                                    runtimeOptions?.Log?.Error(ex, $"Parsing downloaded {typeof(T).GetDisplayName()}");
                                    lambdaDownloadDoneOrFail?.Invoke(HttpStatusCode.UnprocessableEntity, default(T), null, thisEnt);
                                }
                            });
                    });

            }

            // ok
            return numRes;
        }

        public static async Task<T> DownloadIdentifiableToOK<T>(
            Uri location,
            PackCntRuntimeOptions runtimeOptions = null,
            bool allowFakeResponses = false) where T : Aas.IIdentifiable
        {
            // access
            if (location == null)
                return default(T);

            T res = default(T);

            await DownloadListOfIdentifiables<T, Uri>(
                null,
                new[] { location },
                lambdaGetLocation: (loc) => loc,
                runtimeOptions: runtimeOptions,
                allowFakeResponses: allowFakeResponses,
                lambdaDownloadDoneOrFail: (code, idf, contentFn, ent) =>
                {
                    if (code == HttpStatusCode.OK)
                        res = idf;
                });

            return res;
        }

        /// <summary>
        /// Can download arbitrary dynamic entity.
        /// </summary>
        /// <returns>Either dynamic object or <c>null</c></returns>
        public static async Task<dynamic> DownloadEntityToDynamicObject(
            Uri uri,
            PackCntRuntimeOptions runtimeOptions = null,
            bool allowFakeResponses = false,
            bool doNotLogExceptions = false)
        {
            // prepare receing the descriptors
            dynamic resObj = null;

            // GET
            await HttpGetToMemoryStream(
                null,
                sourceUri: uri,
                allowFakeResponses: allowFakeResponses,
                doNotLogExceptions: doNotLogExceptions,
                runtimeOptions: runtimeOptions,
                lambdaDownloadDoneOrFail: (code, ms, contentFn) =>
                {
                    if (code != HttpStatusCode.OK)
                        return;

                    try
                    {
                        // try working with dynamic objects
                        using (StreamReader reader = new StreamReader(ms, System.Text.Encoding.UTF8, true))
                        using (var jsonTextReader = new JsonTextReader(reader))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            resObj = serializer.Deserialize(jsonTextReader);
                        }
                    }
                    catch (Exception ex)
                    {
                        runtimeOptions?.Log?.Error(ex, "Parsing initially downloaded AAS");
                    }
                });

            return resObj;
        }

        public static async Task<int> DeleteListOfEntities<E>(
            HttpClient reUseClient,
            IEnumerable<E> entities,
            Func<E, Uri> lambdaGetLocation,
            Action<HttpStatusCode, string, E> lambdaDeleteDoneOrFail,
            PackCntRuntimeOptions runtimeOptions = null,
            bool allowFakeResponses = false,
            bool useParallel = false) 
        {
            // access
            if (entities == null)
                return 0;

            // result
            int numRes = 0;

            // over all locations
            if (!useParallel)
            {
                foreach (var ent in entities)
                {
                    // delete
                    var res = await HttpDeleteUri(
                        reUseClient,
                        delUri: lambdaGetLocation?.Invoke(ent),
                        runtimeOptions: runtimeOptions);

                    // tell
                    lambdaDeleteDoneOrFail?.Invoke(res.Item1, res.Item2, ent);
                }
            }
            else
            {
                await Parallel.ForEachAsync(entities,
                    new ParallelOptions() { 
                        MaxDegreeOfParallelism = Options.Curr.MaxParallelWriteOps , 
                        CancellationToken = runtimeOptions?.CancellationTokenSource?.Token ?? CancellationToken.None 
                    },
                    async (ent, token) =>
                    {
                        // delete
                        var res = await HttpDeleteUri(
                            reUseClient,
                            delUri: lambdaGetLocation?.Invoke(ent),
                            runtimeOptions: runtimeOptions);

                        // tell
                        lambdaDeleteDoneOrFail?.Invoke(res.Item1, res.Item2, ent);
                    });

            }

            // ok
            return numRes;
        }
    }
}
