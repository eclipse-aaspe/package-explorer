/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace AdminShellNS
{
    /// <summary>
    /// This class lets an outer functionality keep track on the supplementary files, which are in or
    /// are pending to be added or deleted to an Package.
    /// </summary>
    public class AdminShellPackageSupplementaryFile /*: IReferable*/
    {
        public delegate byte[] SourceGetByteChunk();

        public enum LocationType { InPackage, AddPending, DeletePending }

        public enum SpecialHandlingType { None, EmbedAsThumbnail }

        public readonly Uri Uri = null;

        public readonly string UseMimeType = null;

        public readonly string SourceLocalPath = null;
        public readonly SourceGetByteChunk SourceGetBytesDel = null;

        public LocationType Location;
        public readonly SpecialHandlingType SpecialHandling;

        public AdminShellPackageSupplementaryFile(
            Uri uri, string sourceLocalPath = null, LocationType location = LocationType.InPackage,
            SpecialHandlingType specialHandling = SpecialHandlingType.None,
            SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            Uri = uri;
            UseMimeType = useMimeType;
            SourceLocalPath = sourceLocalPath;
            SourceGetBytesDel = sourceGetBytesDel;
            Location = location;
            SpecialHandling = specialHandling;
        }

        // class derives from Referable in order to provide GetElementName
        public string GetElementName()
        {
            return "File";
        }

    }

    public class ListOfAasSupplementaryFile : List<AdminShellPackageSupplementaryFile>
    {
        public AdminShellPackageSupplementaryFile FindByUri(string path)
        {
            if (path == null)
                return null;

            return this.FirstOrDefault(
                x => x?.Uri?.ToString().Trim() == path.Trim());
        }
    }

    /// <summary>
    /// Provides (static?) helpers for serializing AAS..
    /// </summary>
    public static class AdminShellSerializationHelper
    {

        public static string TryReadXmlFirstElementNamespaceURI(Stream s)
        {
            string res = null;
            try
            {
                var xr = System.Xml.XmlReader.Create(s);
                int i = 0;
                while (xr.Read())
                {
                    // limit amount of read
                    i++;
                    if (i > 99)
                        // obviously not found
                        break;

                    // find element
                    if (xr.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        res = xr.NamespaceURI;
                        break;
                    }
                }
                xr.Close();
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            // return to zero pos
            s.Seek(0, SeekOrigin.Begin);

            // give back
            return res;
        }

        /// <summary>
        /// Skips first few tokens of an XML content until first "real" element is encountered
        /// </summary>
        /// <param name="xmlReader"></param>
        public static void XmlSkipHeader(XmlReader xmlReader)
        {
            while (xmlReader.NodeType == XmlNodeType.XmlDeclaration ||
                   xmlReader.NodeType == XmlNodeType.Whitespace ||
                   xmlReader.NodeType == XmlNodeType.Comment ||
                   xmlReader.NodeType == XmlNodeType.None)
                xmlReader.Read();
        }

        /// <summary>
        /// De-serialize an open stream into Environment. Does version/ compatibility management.
        /// </summary>
        /// <param name="s">Open for read stream</param>
        /// <returns></returns>
        public static AasCore.Aas3_1.Environment DeserializeXmlFromStreamWithCompat(Stream s)
        {
            // not sure
            AasCore.Aas3_1.Environment res = null;

            // try get first element
            var nsuri = TryReadXmlFirstElementNamespaceURI(s);

            // read V1.0?
            if (nsuri != null && nsuri.Trim() == "http://www.admin-shell.io/aas/1/0")
            {
#if !DoNotUseAasxCompatibilityModels
                XmlSerializer serializer = new XmlSerializer(
                    typeof(AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv),
                    "http://www.admin-shell.io/aas/1/0");
                var v10 = serializer.Deserialize(s) as AasxCompatibilityModels.AdminShellV10.AdministrationShellEnv;
                res = new AasCore.Aas3_1.Environment(new List<IAssetAdministrationShell>(), new List<ISubmodel>(), new List<IConceptDescription>());
                res.ConvertFromV10(v10);
                return res;
#else
                throw (new Exception("Cannot handle AAS file format http://www.admin-shell.io/aas/1/0 !"));
#endif
            }

            // read V2.0?
            if (nsuri != null && nsuri.Trim() == "http://www.admin-shell.io/aas/2/0")
            {
#if !DoNotUseAasxCompatibilityModels
                XmlSerializer serializer = new XmlSerializer(
                    typeof(AasxCompatibilityModels.AdminShellV20.AdministrationShellEnv),
                    "http://www.admin-shell.io/aas/2/0");
                var v20 = serializer.Deserialize(s) as AasxCompatibilityModels.AdminShellV20.AdministrationShellEnv;
                res = new AasCore.Aas3_1.Environment(new List<IAssetAdministrationShell>(), new List<ISubmodel>(), new List<IConceptDescription>());
                res.ConvertFromV20(v20);
                return res;
#else
                throw (new Exception("Cannot handle AAS file format http://www.admin-shell.io/aas/1/0 !"));
#endif
            }

            //read 3.0
            if (nsuri != null && nsuri.Trim() == "https://admin-shell.io/aas/3/0")
            {
                using (var xmlReader = XmlReader.Create(s))
                {
                    // TODO (MIHO, 2022-12-26): check if could be feature of AAS core
                    XmlSkipHeader(xmlReader);
                    res = new AasCore.Aas3_1.Environment(new List<IAssetAdministrationShell>(), new List<ISubmodel>(), new List<IConceptDescription>());
                    var v30 = AasCore.Aas3_0.Xmlization.Deserialize.EnvironmentFrom(xmlReader);
                    var output = res.ConvertFromV30(v30) as AasCore.Aas3_1.Environment;
                    return output;
                }
            }

            // read V3.0?
            if (nsuri != null && nsuri.Trim() == Xmlization.NS)
            {
                using (var xmlReader = XmlReader.Create(s))
                {
                    // TODO (MIHO, 2022-12-26): check if could be feature of AAS core
                    XmlSkipHeader(xmlReader);
                    res = Xmlization.Deserialize.EnvironmentFrom(xmlReader);
                    return res;
                }
            }

            // nope!
            return null;
        }

        public static T DeserializeFromJSON<T>(string data) where T : IReferable
        {
            //using (var tr = new StringReader(data))
            //{
            //var serializer = BuildDefaultAasxJsonSerializer();
            //var rf = (T)serializer.Deserialize(tr, typeof(T));

            var node = System.Text.Json.Nodes.JsonNode.Parse(data);
            var rf = Jsonization.Deserialize.IReferableFrom(node);

            return (T)rf;
            //}
        }

        /// <summary>
        /// Use this (new!) to deserialize flexible JSON "coming from the outside"
        /// </summary>
        public static T DeserializeAdaptiveFromJSON<T>(string jsonInput) where T : IClass
        {
            try
            {
                using (JsonTextReader reader = new JsonTextReader(new StringReader(jsonInput)))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new AdminShellConverters.AdaptiveAasIClassConverter(
                    AdminShellConverters.AdaptiveAasIClassConverter.ConversionMode.AasCore));
                    var res = serializer.Deserialize<T>(reader);
                    return res;
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.CompletelyIgnoredError(ex);
            }
            return default(T);
        }
    }

    public abstract class AdminShellPackageEnvBase : IDisposable
    {
        public enum SerializationFormat { None, Xml, Json };

        protected IEnvironment _aasEnv = new AasCore.Aas3_1.Environment();

        public AdminShellPackageEnvBase() { }

        public AdminShellPackageEnvBase(AasCore.Aas3_1.IEnvironment env)
        {
            if (env != null)
                _aasEnv = env;
        }

        public IEnvironment AasEnv
        {
            get
            {
                return _aasEnv;
            }
        }

        public void SetEnvironment(IEnvironment environment)
        {
            _aasEnv = environment;
        }

        // TODO: remove, is not for base class!!
        public virtual void SetFilename(string fileName)
        {
        }

        // TODO: remove, is not for base class!!
        public virtual string Filename
        {
            get
            {
                return "";
            }
        }

        protected static WebProxy _manualProxy = null;

        protected WebProxy GetPossibleManualProxy()
        {
            // already there?
            if (_manualProxy != null)
                return _manualProxy;

            // no, check (once, static!)
            string proxyAddress = "";
            string username = "";
            string password = "";

            string proxyFile = "proxy.txt";
            if (System.IO.File.Exists(proxyFile))
            {
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(proxyFile))
                    {
                        proxyFile = sr.ReadLine();
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("proxy.txt could not be read:");
                    Console.WriteLine(e.Message);
                }
            }

            try
            {
                using (StreamReader sr = new StreamReader(proxyFile))
                {
                    proxyAddress = sr.ReadLine();
                    username = sr.ReadLine();
                    password = sr.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(proxyFile + " not found!");
            }

            if (proxyAddress != "")
            {
                _manualProxy = new WebProxy();
                Uri newUri = new Uri(proxyAddress);
                _manualProxy.Address = newUri;
                _manualProxy.Credentials = new NetworkCredential(username, password);

                return _manualProxy;
            }

            return null;
        }

        public virtual HttpClient CreateDefaultHttpClient()
        {
            // read via HttpClient (uses standard proxies)
            var handler = new HttpClientHandler();
            if (GetPossibleManualProxy() != null)
                handler.Proxy = GetPossibleManualProxy();
            else
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            handler.AllowAutoRedirect = true;

            // new http client
            var client = new HttpClient(handler);

            // ok
            return client;
        }

        public virtual HttpRequestMessage CreateHttpRequest(
            HttpMethod method, string requestUri,
            ISecurityAccessHandler secureAccess = null,
            string acceptHeader = null)
        {
            // start
            var request = new HttpRequestMessage(method, requestUri);

            // seems to be required by Phoenix
            request.Headers.Add("User-Agent", "aasx-package-explorer/1.0.0");

            // would be good to always have an accept header, if in doubt: "*/*"
            if (acceptHeader?.HasContent() == true)
                request.Headers.Add("Accept", acceptHeader);

            // add access headers?
            var headerItem = secureAccess?.LookupAuthenticateHeader(requestUri);
            headerItem?.Enrich(request);

            // ok
            return request;
        }

        /// <summary>
        /// Checks for a file within the package or external, e.g. on a file space.
        /// In derived classes, could use the AAS/ SM / IdShortPath attributes to
        /// refer to files in registry/ repository.
        /// </summary>
        /// <param name="uriString">Local (within package) or external file. Should have a scheme. 
        /// Local file start with a leading slash.</param>
        /// <param name="aasId">For registry/ repository access.</param>
        /// <param name="smId">For registry/ repository access.</param>
        /// <param name="idShortPath">For registry/ repository access.</param>
        /// <returns>Bytes or <c>null</c></returns>
        public virtual byte[] GetBytesFromPackageOrExternal(
            string uriString, 
            string aasId = null,
            string smId = null,
            ISecurityAccessHandler secureAccess = null,
            string acceptHeader = null,
            string idShortPath = null)
        {
            //
            // this part of the functionality works on HTTP and absolute files and is
            // indepedent from a package storage.
            //

            // split scheme and part
            var sap = AdminShellUtil.GetSchemeAndPath(uriString);
            if (sap == null)
                return null;

            // Check, if remote
            if (sap.Scheme.StartsWith("http"))
            {
                // get httpClient
                var client = CreateDefaultHttpClient();

                // make request, may add headers
                HttpResponseMessage response = null;
                using (var request = CreateHttpRequest(HttpMethod.Get, uriString, 
                    acceptHeader: acceptHeader,
                    secureAccess: secureAccess))
                {
                    // send request
                    response = client.SendAsync(request,
                        HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                }

                // MIHO, 2025-07-21: old, working code:
                // var response = client.GetAsync(uriString).GetAwaiter().GetResult();

                // re-direct?
                if (response.StatusCode == HttpStatusCode.Moved
                    || response.StatusCode == HttpStatusCode.Found)
                {
                    var location = response.Headers.Location;
                    response = client.GetAsync(location).GetAwaiter().GetResult();
                }

                // successfull?
                if (!response.IsSuccessStatusCode)
                    return null;

                // get bytes
                var resBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

                // TODO (MIHO, 2024-11-09): looks like a hack from Andreas to
                // detect indirection
                if (resBytes.Length < 500) // indirect load?
                {
                    string json = System.Text.Encoding.UTF8.GetString(resBytes);
                    var parsed = JObject.Parse(json);
                    try
                    {
                        string url = parsed.SelectToken("url").Value<string>();
                        response = client.GetAsync(url).GetAwaiter().GetResult();

                        if (!response.IsSuccessStatusCode)
                            return null;

                        resBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }

                return resBytes;
            }

            // now, has to be file
            if (sap.Scheme != "file")
                return null;

            // if not starting with "/", if has to be an absolute file
            if (!sap.Path.StartsWith('/'))
            {
                try
                {
                    var resBytes = System.IO.File.ReadAllBytes(sap.Path);
                    return resBytes;
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);                    
                }
            }

            // no, nothing more to find here (in base implementation!)
            return null;
        }

        /// <summary>
        /// Checks for a file within the package or external, e.g. on a file space.
        /// In derived classes, could use the AAS/ SM / IdShortPath attributes to
        /// refer to files in registry/ repository.
        /// </summary>
        /// <param name="uriString">Local (within package) or external file. Should have a scheme. 
        /// Local file start with a leading slash.</param>
        /// <param name="aasId">For registry/ repository access.</param>
        /// <param name="smId">For registry/ repository access.</param>
        /// <param name="idShortPath">For registry/ repository access.</param>
        /// <returns>Bytes or <c>null</c></returns>
        public virtual async Task<byte[]> GetBytesFromPackageOrExternalAsync(
            string uriString,
            string aasId = null,
            string smId = null,
            ISecurityAccessHandler secureAccess = null,
            string idShortPath = null)
        {
            await Task.Yield();
            return GetBytesFromPackageOrExternal(uriString, aasId, smId, secureAccess, idShortPath);
        }

        /// <summary>
        /// Writes to a file within the package or to external registry/ repository.
        /// Does not write to external files e.g. on a filespace!
        /// In derived classes, could use the AAS/ SM / IdShortPath attributes to
        /// refer to files in registry/ repository.
        /// </summary>
        /// <param name="uriString">Local (within package) or external file. Should have a scheme. 
        /// Local file start with a leading slash.</param>
        /// <param name="data">Data given by the bytes</param>
        /// <param name="aasId">For registry/ repository access.</param>
        /// <param name="smId">For registry/ repository access.</param>
        /// <param name="idShortPath">For registry/ repository access.</param>
        /// <returns><c>True</c>, if the file was successfully stored.</returns>
        public virtual bool PutBytesToPackageOrExternal(
            string uriString,
            byte[] data,
            string aasId = null,
            string smId = null,
            string idShortPath = null)
        {
            // here, nothing to do, as external file storage is not allowed.
            return false;
        }

        /// <summary>
        /// Writes to a file within the package or to external registry/ repository.
        /// Does not write to external files e.g. on a filespace!
        /// In derived classes, could use the AAS/ SM / IdShortPath attributes to
        /// refer to files in registry/ repository.
        /// </summary>
        /// <param name="uriString">Local (within package) or external file. Should have a scheme. 
        /// Local file start with a leading slash.</param>
        /// <param name="data">Data given by the bytes</param>
        /// <param name="aasId">For registry/ repository access.</param>
        /// <param name="smId">For registry/ repository access.</param>
        /// <param name="idShortPath">For registry/ repository access.</param>
        /// <returns><c>True</c>, if the file was successfully stored.</returns>
        public virtual async Task<bool> PutBytesToPackageOrExternalAsync(
            string uriString,
            byte[] data,
            string aasId = null,
            string smId = null,
            string idShortPath = null)
        {
            // here, nothing to do, as external file storage is not allowed.
            await Task.Yield();
            return PutBytesToPackageOrExternal(uriString, data, aasId, smId, idShortPath);
        }

        public virtual void PrepareSupplementaryFileParameters(ref string targetDir, ref string targetFn)
        {
        }

        public virtual string AddSupplementaryFileToStore(
            string sourcePath, string targetDir, string targetFn, bool embedAsThumb,
            AdminShellPackageSupplementaryFile.SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            return null;
        }

        /// <summary>
        /// Gets the thumbnail data of the local package (and only the local package!)
        /// </summary>
        public virtual byte[] GetLocalThumbnailBytes(ref Uri thumbUri)
        {
            return null;
        }

        /// <summary>
        /// Gets the thumbnail data of the local package or (1st prio) the AAS with the
        /// given id.
        /// Note: does not access remote content from registry/ repository!
        /// </summary>
        public virtual byte[] GetThumbnailBytesFromAasOrPackage(string aasId)
        {
            // find aas?
            var aas = AasEnv?.FindAasById(aasId);
            if (aas?.AssetInformation?.DefaultThumbnail?.Path?.HasContent() == true)
            {
                try
                {
                    // Note: could also use http://...
                    var bytes = GetBytesFromPackageOrExternal(uriString: aas.AssetInformation.DefaultThumbnail.Path);
                    if (bytes != null)
                        return bytes;
                }
                catch (Exception ex)
                {
                    LogInternally.That.CompletelyIgnoredError(ex);
                }
            }

            // or local package?
            try
            {
                Uri dummy = null;
                var bytes = GetLocalThumbnailBytes(ref dummy);
                if (bytes != null)
                    return bytes;
            }
            catch (Exception ex)
            {
                LogInternally.That.CompletelyIgnoredError(ex);
            }

            // no
            return null;
        }

        public virtual ListOfAasSupplementaryFile GetListOfSupplementaryFiles()
        {
            return null;
        }

        public virtual void DeleteSupplementaryFile(AdminShellPackageSupplementaryFile psf)
        {
        }

        /// <summary>
        /// Copies/ download contents and will return filename of temp file.
        /// </summary>
        /// <returns></returns>
        public virtual string MakePackageFileAvailableAsTempFile(string packageUri, bool keepFilename = false)
        {
            // this uses the virtual implementation and should therefore work ok in the base class

            // access
            if (packageUri == null)
                return null;

            // get input stream
            var inputBytes = GetBytesFromPackageOrExternal(packageUri);
            if (inputBytes == null)
                return null;

            // generate tempfile name
            string tempext = System.IO.Path.GetExtension(packageUri);
            string temppath = System.IO.Path.GetTempFileName().Replace(".tmp", tempext);

            // maybe modify tempfile name?
            if (keepFilename)
            {
                var masterFn = System.IO.Path.GetFileNameWithoutExtension(packageUri);
                var tmpDir = System.IO.Path.GetDirectoryName(temppath);
                var tmpFnExt = System.IO.Path.GetFileName(temppath);

                temppath = System.IO.Path.Combine(tmpDir, "" + masterFn + "_" + tmpFnExt);
            }

            // copy to temp file
            System.IO.File.WriteAllBytes(temppath, inputBytes);
            return temppath;
        }

        /// <summary>
        /// Copies/ download contents and will return filename of temp file.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<string> MakePackageFileAvailableAsTempFileAsync(
            string packageUri,
            string aasId = null,
            string smId = null,
            string idShortPath = null,
            bool keepFilename = false,
            ISecurityAccessHandler secureAccess = null)
        {
            // this uses the virtual implementation and should therefore work ok in the base class

            // access
            if (packageUri == null)
                return null;

            // get input stream
            var inputBytes = await GetBytesFromPackageOrExternalAsync(packageUri, aasId, smId, 
                    idShortPath: idShortPath, secureAccess: secureAccess);
            if (inputBytes == null)
                return null;

            // generate tempfile name
            string tempext = System.IO.Path.GetExtension(packageUri);
            string temppath = System.IO.Path.GetTempFileName().Replace(".tmp", tempext);

            // maybe modify tempfile name?
            if (keepFilename)
            {
                var masterFn = System.IO.Path.GetFileNameWithoutExtension(packageUri);
                var tmpDir = System.IO.Path.GetDirectoryName(temppath);
                var tmpFnExt = System.IO.Path.GetFileName(temppath);

                temppath = System.IO.Path.Combine(tmpDir, "" + masterFn + "_" + tmpFnExt);
            }

            // copy to temp file
            await System.IO.File.WriteAllBytesAsync(temppath, inputBytes);
            return temppath;
        }

        public virtual bool SaveAs(
            string fn, bool writeFreshly = false, 
            SerializationFormat prefFmt = SerializationFormat.None,
            MemoryStream useMemoryStream = null, bool saveOnlyCopy = false)
        {
            return false;
        }

        /// <summary>
        /// Temporariyl saves & closes package and executes lambda. Afterwards, the package is re-opened
        /// under the same file name
        /// </summary>
        /// <param name="lambda">Action which is to be executed while the file is CLOSED</param>
        /// <param name="prefFmt">Format for the saved file</param>
        public virtual void TemporarilySaveCloseAndReOpenPackage(
            Action lambda,
            AdminShellPackageFileBasedEnv.SerializationFormat prefFmt = AdminShellPackageFileBasedEnv.SerializationFormat.None)
        {
        }

        public virtual bool IsOpen
        {
            get
            {
                // negative default behaviour
                return false;
            }
        }

        public virtual bool IsLocalFile(string uriString)
        {
            return false;
        }

        public virtual void Close()
        {
        }

        public virtual void Flush()
        {
        }

        public virtual void Dispose()
        {
        }

        //
        // Binary file read + write
        //

        public async Task<bool> PutByteArrayToExternalUri(string uri, byte[] ba)
        {
            // split uri and access
            var sap = AdminShellUtil.GetSchemeAndPath(uri);
            if (ba == null || sap == null)
                return false;

            // directly a HTTP resource?
            if (sap.Scheme.StartsWith("http"))
            {
                try
                {
                    using (var client = new HttpClient())
                    using (var content = new ByteArrayContent(ba))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("*/*");
                        var response = await client.PostAsync(uri, content);
                        return response.IsSuccessStatusCode;
                    }
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                    return false;
                }
            }

            // no other schemes now
            if (sap.Scheme != "file")
                return false;

            // just write
            try
            {
                System.IO.File.WriteAllBytes(sap.Path, ba);
                return true;
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
            
            return false;
        }
    }

}