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
        public static AasCore.Aas3_0.Environment DeserializeXmlFromStreamWithCompat(Stream s)
        {
            // not sure
            AasCore.Aas3_0.Environment res = null;

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
                res = new AasCore.Aas3_0.Environment(new List<IAssetAdministrationShell>(), new List<ISubmodel>(), new List<IConceptDescription>());
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
                res = new AasCore.Aas3_0.Environment(new List<IAssetAdministrationShell>(), new List<ISubmodel>(), new List<IConceptDescription>());
                res.ConvertFromV20(v20);
                return res;
#else
                throw (new Exception("Cannot handle AAS file format http://www.admin-shell.io/aas/1/0 !"));
#endif
            }

            // read V3.0?
            if (nsuri != null && nsuri.Trim() == Xmlization.NS)
            {
                // dead-csharp off
                //XmlSerializer serializer = new XmlSerializer(
                //    typeof(AasCore.Aas3_0_RC02.Environment), "http://www.admin-shell.io/aas/3/0");
                //res = serializer.Deserialize(s) as AasCore.Aas3_0_RC02.Environment;
                // dead-csharp on
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
        // dead-csharp off
        //public static JsonSerializer BuildDefaultAasxJsonSerializer()
        //{
        //    var serializer = new JsonSerializer();
        //    serializer.Converters.Add(
        //        new AdminShellConverters.JsonAasxConverter(
        //            "modelType", "name"));
        //    return serializer;
        //}
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

        //public static T DeserializeFromJSON<T>(JToken obj) where T : IReferable
        //{
        //    if (obj == null)
        //        return default(T);
        //    var serializer = BuildDefaultAasxJsonSerializer();
        //    var rf = obj.ToObject<T>(serializer);
        //    return rf;
        //}

        ///// <summary>
        ///// Use this, if <c>DeserializeFromJSON</c> is too tight.
        ///// </summary>
        //public static T DeserializePureObjectFromJSON<T>(string data)
        //{
        //    using (var tr = new StringReader(data))
        //    {
        //        //var serializer = BuildDefaultAasxJsonSerializer();
        //        //var rf = (T)serializer.Deserialize(tr, typeof(T));
        //        return null;
        //    }
        //}
        // dead-csharp on
    }

    public abstract class AdminShellPackageEnvBase : IDisposable
    {
        public enum SerializationFormat { None, Xml, Json };

        protected IEnvironment _aasEnv = new AasCore.Aas3_0.Environment();

        public AdminShellPackageEnvBase() { }

        public AdminShellPackageEnvBase(AasCore.Aas3_0.Environment env)
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

        protected static WebProxy proxy = null;

        public virtual Stream GetLocalStreamFromPackage(
            string uriString, 
            string aasId = null,
            string smId = null,
            string idShortPath = null,
            FileMode mode = FileMode.Open, 
            FileAccess access = FileAccess.Read)
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
                if (proxy == null)
                {
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
                        proxy = new WebProxy();
                        Uri newUri = new Uri(proxyAddress);
                        proxy.Address = newUri;
                        proxy.Credentials = new NetworkCredential(username, password);
                        Console.WriteLine("Using proxy: " + proxyAddress);
                    }
                }

                var handler = new HttpClientHandler();

                if (proxy != null)
                    handler.Proxy = proxy;
                else
                    handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
                var hc = new HttpClient(handler);

                var response = hc.GetAsync(uriString).GetAwaiter().GetResult();

                // if you call response.EnsureSuccessStatusCode here it will throw an exception
                if (response.StatusCode == HttpStatusCode.Moved
                    || response.StatusCode == HttpStatusCode.Found)
                {
                    var location = response.Headers.Location;
                    response = hc.GetAsync(location).GetAwaiter().GetResult();
                }

                response.EnsureSuccessStatusCode();
                var s = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                if (s.Length < 500) // indirect load?
                {
                    StreamReader reader = new StreamReader(s);
                    string json = reader.ReadToEnd();
                    var parsed = JObject.Parse(json);
                    try
                    {
                        string url = parsed.SelectToken("url").Value<string>();
                        response = hc.GetAsync(url).GetAwaiter().GetResult();
                        response.EnsureSuccessStatusCode();
                        s = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        LogInternally.That.SilentlyIgnoredError(ex);
                    }
                }

                return s;
            }

            // now, has to be file
            if (sap.Scheme != "file")
                return null;

            // if not starting with "/", if has to be an absolute file
            if (!sap.Path.StartsWith('/'))
            {
                try
                {
                    var stream = System.IO.File.Open(sap.Path, mode, access);
                    return stream;
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);                    
                }
            }

            return null;
        }

        public virtual async Task<Stream> GetLocalStreamFromPackageAsync(
            string uriString,
            string aasId = null,
            string smId = null,
            string idShortPath = null,
            FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.Read)
        {
            await Task.Yield();
            return GetLocalStreamFromPackage(uriString, aasId, smId, idShortPath, mode, access);
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

        public virtual byte[] GetByteArrayFromUriOrLocalPackage(string uriString)
        {
            return null;
        }

        /// <remarks>
        /// Ensures:
        /// <ul><li><c>result == null || result.CanRead</c></li></ul>
        /// </remarks>
        public Stream GetLocalThumbnailStream()
        {
            Uri dummy = null;
            var result = GetLocalThumbnailStream(ref dummy);

            // Post-condition
            if (!(result == null || result.CanRead))
            {
                throw new InvalidOperationException("Unexpected unreadable result stream");
            }

            return result;
        }

        /// <remarks>
        /// Ensures:
        /// <ul><li><c>result == null || result.CanRead</c></li></ul>
        /// </remarks>
        public virtual Stream GetLocalThumbnailStream(ref Uri thumbUri)
        {
            return null;
        }

        public virtual Stream GetStreamFromUriOrLocalPackage(string uriString,
            FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.Read)
        {
            return null;
        }

        /// <summary>
        /// This is intended to be the "new" one
        /// </summary>
        public virtual Stream GetThumbnailStreamFromAasOrPackage(string aasId)
        {
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
            using (var input = GetLocalStreamFromPackage(packageUri))
            {
                // any
                if (input == null)
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
                using (var temp = System.IO.File.OpenWrite(temppath))
                {
                    input.CopyTo(temp);
                    return temppath;
                }
            }
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
            bool keepFilename = false)
        {
            // this uses the virtual implementation and should therefore work ok in the base class

            // access
            if (packageUri == null)
                return null;

            // get input stream
            using (var input = await GetLocalStreamFromPackageAsync(packageUri, aasId, smId, idShortPath))
            {
                // ok?
                if (input == null)
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
                using (var temp = System.IO.File.OpenWrite(temppath))
                {
                    input.CopyTo(temp);
                    return temppath;
                }
            }
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

        public async Task<byte[]> GetByteArrayFromExternalInternalUri(string uri)
        {
            // split uri and access
            var sap = AdminShellUtil.GetSchemeAndPath(uri);
            if (sap == null)
                return null;

            // directly a HTTP resource?
            if (sap.Scheme.StartsWith("http"))
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var ba = await client.GetByteArrayAsync(uri);
                        return ba;
                    }
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                    return null;
                }
            }

            // no other schemes now
            if (sap.Scheme != "file")
                return null;

            // check if package local file
            if (IsLocalFile(sap.Path))
            {
                return GetByteArrayFromUriOrLocalPackage(sap.Path);
            }

            // OK, assume a file accessible to this computer
            try
            {
                var ba = await System.IO.File.ReadAllBytesAsync(sap.Path);
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            // nope
            return null;
        }

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