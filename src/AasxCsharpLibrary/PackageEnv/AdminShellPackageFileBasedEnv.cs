/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS.DiaryData;
using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Aas = AasCore.Aas3_1;

namespace AdminShellNS
{
    /// <summary>
    /// This class encapsulates an AdminShellEnvironment and supplementary files into an AASX Package.
    /// Specifically has the capability to load, update and store .XML, .JSON and .AASX packages.
    /// </summary>
    public class AdminShellPackageFileBasedEnv : AdminShellPackageEnvBase
    {
        // Note: the first array element [0] should be conforming the actual spec (for saving)
        protected static string[] relTypesOrigin = new[] {
            "http://admin-shell.io/aasx/relationships/aasx-origin",
            "http://www.admin-shell.io/aasx/relationships/aasx-origin"
        };

        protected static string[] relTypesSpec = new[] {
            "http://admin-shell.io/aasx/relationships/aas-spec",
            "http://www.admin-shell.io/aasx/relationships/aas-spec"
        };

        protected static string[] relTypesSuppl = new[] {
            "http://admin-shell.io/aasx/relationships/aas-suppl",
            "http://www.admin-shell.io/aasx/relationships/aas-suppl"
        };

        protected static string[] relTypesThumb = new[] {
            "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail"
        };

        private string _fn = "New Package";

        private string _tempFn = null;

        private Package _openPackage = null;
        private readonly ListOfAasSupplementaryFile _pendingFilesToAdd = new ListOfAasSupplementaryFile();
        private readonly ListOfAasSupplementaryFile _pendingFilesToDelete = new ListOfAasSupplementaryFile();

        public AdminShellPackageFileBasedEnv() : base() { }

        public AdminShellPackageFileBasedEnv(IEnvironment env) : base(env) { }

        public AdminShellPackageFileBasedEnv(string fn, bool indirectLoadSave = false) : base()
        {
            Load(fn, indirectLoadSave);
        }

        public override bool IsOpen
        {
            get
            {
                return _openPackage != null;
            }
        }

        public override void SetFilename(string fileName)
        {
            _fn = fileName;
        }

        public override string Filename
        {
            get
            {
                return _fn;
            }
        }

        private class FindRelTuple
        {
            public bool Deprecated { get; set; }
            public PackageRelationship Rel { get; set; }
        }

        private static IEnumerable<FindRelTuple> FindAllRelationships(
            Package package, string[] relTypes,
            bool? filterForDeprecatedEquals = null)
        {
            for (int i = 0; i < relTypes.Length; i++)
                foreach (var x in package.GetRelationshipsByType(relTypes[i]))
                {
                    var res = new FindRelTuple() { Deprecated = (i > 0), Rel = x };
                    if (filterForDeprecatedEquals.HasValue &&
                        filterForDeprecatedEquals.Value != res.Deprecated)
                        continue;
                    yield return res;
                }
        }

        private static IEnumerable<FindRelTuple> FindAllRelationships(
            PackagePart part, string[] relTypes,
            bool? filterForDeprecatedEquals = null)
        {
            for (int i = 0; i < relTypes.Length; i++)
                foreach (var x in part.GetRelationshipsByType(relTypes[i]))
                {
                    var res = new FindRelTuple() { Deprecated = (i > 0), Rel = x };
                    if (filterForDeprecatedEquals.HasValue &&
                     filterForDeprecatedEquals.Value != res.Deprecated)
                        continue;
                    yield return res;
                }
        }

        private static AasCore.Aas3_1.Environment LoadXml(string fn)
        {
            try
            {
                using (var reader = new StreamReader(fn))
                {
                    var aasEnv = AdminShellSerializationHelper.DeserializeXmlFromStreamWithCompat(
                        reader.BaseStream);

                    if (aasEnv == null)
                        throw new Exception("Type error for XML file");

                    return aasEnv;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"While reading AAS {fn} at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        private static AasCore.Aas3_1.Environment LoadJson(string fn)
        {
            try
            {
                using (var file = System.IO.File.OpenRead(fn))
                {
                    // dead-csharp off
                    //// TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                    //var serializer = new JsonSerializer();
                    //serializer.Converters.Add(
                    //    new AdminShellConverters.JsonAasxConverter(
                    //        "modelType", "name"));

                    //var aasEnv = (AasCore.Aas3_1_RC02.Environment)serializer.Deserialize(
                    //    file, typeof(AasCore.Aas3_1_RC02.Environment));
                    // dead-csharp on
                    var node = System.Text.Json.Nodes.JsonNode.Parse(file);
                    var aasEnv = Jsonization.Deserialize.EnvironmentFrom(node);

                    return aasEnv;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"While reading AAS {fn} at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        /// <remarks><paramref name="fn"/> is unequal <paramref name="fnToLoad"/> if indirectLoadSave is used.</remarks>
        private static (AasCore.Aas3_1.Environment, Package) LoadPackageAasx(string fn, string fnToLoad)
        {
            AasCore.Aas3_1.Environment aasEnv;
            Package openPackage = null;

            Package package;
            try
            {
                package = Package.Open(fnToLoad, FileMode.Open);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    fn == fnToLoad
                        ? $"While opening the package to read AASX {fn} " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}"
                        : $"While opening the package to read AASX {fn} indirectly from {fnToLoad} " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }

            try
            {
                // get the origin from the package
                PackagePart originPart = null;
                foreach (var x in FindAllRelationships(package, relTypesOrigin))
                    if (x.Rel.SourceUri.ToString() == "/")
                    {
                        //originPart = package.GetPart(x.TargetUri);
                        var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                        if (package.PartExists(absoluteURI))
                        {
                            originPart = package.GetPart(absoluteURI);
                        }
                        break;
                    }

                if (originPart == null)
                    throw (new Exception("Unable to find AASX origin. Aborting!"));

                // get the specs from the package
                //first try to find it without www, the "updated" ns
                PackagePart specPart = null;
                foreach (var x in FindAllRelationships(originPart, relTypesSpec))
                {
                    //specPart = package.GetPart(x.TargetUri);
                    var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                    if (package.PartExists(absoluteURI))
                    {
                        specPart = package.GetPart(absoluteURI);
                    }
                    break;
                }

                if (specPart == null)
                    throw (new Exception("Unable to find AASX spec(s). Aborting!"));

                // open spec part to read
                try
                {
                    if (specPart.Uri.ToString().ToLower().Trim().EndsWith("json"))
                    {
                        using (var s = specPart.GetStream(FileMode.Open))
                        {
                            // dead-csharp off
                            //using (var file = new StreamReader(s))
                            //{
                            //JsonSerializer serializer = new JsonSerializer();
                            //serializer.Converters.Add(
                            //    new AdminShellConverters.JsonAasxConverter(
                            //        "modelType", "name"));

                            //aasEnv = (AasCore.Aas3_1_RC02.Environment)serializer.Deserialize(
                            //    file, typeof(AasCore.Aas3_1_RC02.Environment));

                            var node = System.Text.Json.Nodes.JsonNode.Parse(s);
                            aasEnv = Jsonization.Deserialize.EnvironmentFrom(node);
                            //}
                            // dead-csharp on
                        }
                    }
                    else
                    {
                        using (var s = specPart.GetStream(FileMode.Open))
                        {
                            // own catch loop to be more specific
                            aasEnv = AdminShellSerializationHelper.DeserializeXmlFromStreamWithCompat(s);
                            openPackage = package;

                            if (aasEnv == null)
                                throw new Exception("Type error for XML file!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        fn == fnToLoad
                            ? $"While reading spec from the AASX {fn} " +
                              $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}"
                            : $"While reading spec from the {fn} (and indirectly over {fnToLoad}) " +
                              $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    fn == fnToLoad
                        ? $"While reading the AASX {fn} " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}"
                        : $"While reading the {fn} (and indirectly over {fnToLoad}) " +
                          $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
            finally
            {
                if (openPackage == null)
                {
                    package.Close();
                }
            }

            return (aasEnv, openPackage);
        }

        public void Load(string fn, bool indirectLoadSave = false)
        {
            _fn = fn;
            _openPackage?.Close();
            _openPackage = null;

            string extension = Path.GetExtension(fn).ToLower();
            switch (extension)
            {
                case ".xml":
                    {
                        _aasEnv = LoadXml(fn);
                        break;
                    }
                case ".json":
                    {
                        _aasEnv = LoadJson(fn);
                        break;
                    }
                case ".aasx":
                    {
                        var fnToLoad = fn;
                        _tempFn = null;
                        if (indirectLoadSave)
                        {
                            try
                            {
                                _tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".aasx");
                                System.IO.File.Copy(fn, _tempFn);
                                fnToLoad = _tempFn;

                            }
                            catch (Exception ex)
                            {
                                throw new Exception(
                                    $"While copying AASX {fn} for indirect load to {fnToLoad} " +
                                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
                            }
                        }

                        // load package AASX
                        (_aasEnv, _openPackage) = LoadPackageAasx(fn, fnToLoad);
                        break;
                    }
                default:
                    throw new Exception(
                        $"Does not know how to handle the extension {extension} of the file: {fn}");
            }
        }

        public void SetTempFn(string fn)
        {
            try
            {
                _tempFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".aasx");
                System.IO.File.Copy(fn, _tempFn);

            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"While copying AASX {fn}" +
                    $"at {AdminShellUtil.ShortLocation(ex)} gave: {ex.Message}");
            }
        }

        public void LoadFromAasEnvString(string content)
        {
            try
            {
                // dead-csharp off
                //using (var file = new StringReader(content))
                //{
                // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                //JsonSerializer serializer = new JsonSerializer();
                //serializer.Converters.Add(new AdminShellConverters.JsonAasxConverter("modelType", "name"));
                //_aasEnv = (AasCore.Aas3_1_RC02.Environment)serializer.Deserialize(
                //    file, typeof(AasCore.Aas3_1_RC02.Environment));

                var node = System.Text.Json.Nodes.JsonNode.Parse(content);
                _aasEnv = Jsonization.Deserialize.EnvironmentFrom(node);
                //}
                // dead-csharp on
            }
            catch (Exception ex)
            {
                throw (new Exception(
                    string.Format("While reading AASENV string {0} gave: {1}",
                        AdminShellUtil.ShortLocation(ex), ex.Message)));
            }
        }

        public void ClearTaintedFlag(Aas.IIdentifiable idf)
        {
            // access
            if (idf is ITaintedData itd && itd.TaintedData?.Tainted != null)
                itd.TaintedData.Tainted = null;
        }

        public void ClearAllIdentifiableTaintedFlags()
        {
            // access
            if (AasEnv == null)
                return;

            // all
            foreach (var aas in this.AasEnv.AllAssetAdministrationShells())
                ClearTaintedFlag(aas);

            foreach (var sm in this.AasEnv.AllSubmodels())
                ClearTaintedFlag(sm);

            foreach (var cd in this.AasEnv.AllConceptDescriptions())
                ClearTaintedFlag(cd);
        }

        public override bool SaveAs(string fn, bool writeFreshly = false, SerializationFormat prefFmt = SerializationFormat.None,
                MemoryStream useMemoryStream = null, bool saveOnlyCopy = false)
        {
            // silently fix flaws
            _aasEnv?.SilentFix30();

            // ok, which format?
            if (fn.ToLower().EndsWith(".xml"))
            {
                // save only XML
                if (!saveOnlyCopy)
                    _fn = fn;
                try
                {
                    Stream s = (useMemoryStream != null)
                        ? (Stream)useMemoryStream
                        : System.IO.File.Open(fn, FileMode.Create, FileAccess.Write);

                    try
                    {
                        // dead-csharp off
                        // TODO (Michael Hoffmeister, 2020-08-01): use a unified function to create a serializer
                        //var serializer = new XmlSerializer(typeof(AasCore.Aas3_1_RC02.Environment));
                        //var nss = GetXmlDefaultNamespaces();
                        //serializer.Serialize(s, _aasEnv, nss);
                        // dead-csharp on
                        var writer = XmlWriter.Create(s, new XmlWriterSettings()
                        {
                            Indent = true,
                            OmitXmlDeclaration = true
                        });
                        Xmlization.Serialize.To(
                            _aasEnv, writer);
                        writer.Flush();
                        writer.Close();
                        s.Flush();
                        ClearAllIdentifiableTaintedFlags();
                    }
                    finally
                    {
                        // close?
                        if (useMemoryStream == null)
                            s.Close();
                    }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While writing AAS {0} at {1} gave: {2}",
                            fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            if (fn.ToLower().EndsWith(".json"))
            {
                // save only JSON
                // This functionality is an initial test.
                if (!saveOnlyCopy)
                    _fn = fn;
                try
                {
                    Stream s = (useMemoryStream != null) ? (Stream)useMemoryStream
                        : System.IO.File.Open(fn, FileMode.Create, FileAccess.Write);

                    try
                    {
                        var jsonWriterOptions = new System.Text.Json.JsonWriterOptions
                        {
                            Indented = true
                        };
                        using var wr = new System.Text.Json.Utf8JsonWriter(s, jsonWriterOptions);
                        Jsonization.Serialize.ToJsonObject(_aasEnv).WriteTo(wr);
                        wr.Flush();
                        s.Flush();
                        ClearAllIdentifiableTaintedFlags();
                    }
                    finally
                    {
                        // close?
                        if (useMemoryStream == null)
                            s.Close();
                    }
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While writing AAS {0} at {1} gave: {2}",
                            fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            if (fn.ToLower().EndsWith(".aasx"))
            {
                // save package AASX
                try
                {
                    // We want existing contents to be preserved, but do not want to allow the change of the file name.
                    // Therefore: copy the file to a new name, then re-open.
                    // fn could be changed, therefore close "old" package first
                    if (_openPackage != null)
                    {
                        try
                        {
                            _openPackage.Close();
                            if (!writeFreshly)
                            {
                                if (_tempFn != null)
                                    System.IO.File.Copy(_tempFn, fn);
                                else
                                {
                                    /* TODO (MIHO, 2021-01-02): check again.
                                     * Revisiting this code after a while, and after
                                     * the code has undergo some changes by MR, the following copy command needed
                                     * to be amended with a if to protect against self-copy. */
                                    if (_fn != fn)
                                        System.IO.File.Copy(_fn, fn);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogInternally.That.SilentlyIgnoredError(ex);
                        }
                        _openPackage = null;
                    }

                    // approach is to utilize the existing package, if possible. If not, create from scratch
                    Package package = null;
                    if (useMemoryStream != null)
                    {
                        package = Package.Open(
                            useMemoryStream, (writeFreshly) ? FileMode.Create : FileMode.OpenOrCreate);
                    }
                    else
                    {
                        package = Package.Open(
                            (_tempFn != null) ? _tempFn : fn,
                            (writeFreshly) ? FileMode.Create : FileMode.OpenOrCreate);
                    }
                    _fn = fn;

                    // first check, if we have deprecated origin
                    foreach (var x in FindAllRelationships(package, relTypesOrigin,
                            filterForDeprecatedEquals: true))
                        if (x.Rel.SourceUri.ToString() == "/")
                        {
                            var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                            if (package.PartExists(absoluteURI))
                            {
                                var tempPart = package.GetPart(absoluteURI);

                                // delete old type, because its not according to spec or something
                                // then replace with the current type
                                package.DeleteRelationship(x.Rel.Id);
                                package.CreateRelationship(
                                    tempPart.Uri, TargetMode.Internal,
                                    relTypesOrigin.FirstOrDefault());
                                break;
                            }
                        }                    

                    // now check, if the origin could be found with correct relationships
                    PackagePart originPart = null;
                    foreach (var x in FindAllRelationships(package, relTypesOrigin,
                            filterForDeprecatedEquals: false))
                        if (x.Rel.SourceUri.ToString() == "/")
                        {
                            var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                            if (package.PartExists(absoluteURI))
                            {
                                originPart = package.GetPart(absoluteURI);
                            }
                        }

                    // MIHO, 2024-05-29
                    // fix the case, that part exists but is not really associated by a
                    // relationship
                    var uriOrigin = new Uri("/aasx/aasx-origin", UriKind.RelativeOrAbsolute);
                    if (originPart == null && package.PartExists(uriOrigin))
                    {
                        // get the part
                        originPart = package.GetPart(uriOrigin);

                        // make the relationship
                        package.CreateRelationship(
                            originPart.Uri, TargetMode.Internal,
                            relTypesOrigin.FirstOrDefault());
                    }

                    if (originPart == null)
                    {
                        // create, as not existing
                        originPart = package.CreatePart(
                            new Uri("/aasx/aasx-origin", UriKind.RelativeOrAbsolute),
                            System.Net.Mime.MediaTypeNames.Text.Plain, CompressionOption.Maximum);
                        using (var s = originPart.GetStream(FileMode.Create))
                        {
                            var bytes = System.Text.Encoding.ASCII.GetBytes("Intentionally empty");
                            s.Write(bytes, 0, bytes.Length);
                        }
                        package.CreateRelationship(
                            originPart.Uri, TargetMode.Internal,
                            relTypesOrigin.FirstOrDefault());
                    }

                    // get the specs from the package, first again: deprecated
                    foreach (var x in FindAllRelationships(
                        originPart, relTypesSpec,
                        filterForDeprecatedEquals: true))
                    {
                        var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                        if (package.PartExists(absoluteURI))
                        {
                            var tempPart = package.GetPart(absoluteURI);

                            //delete old type, because its not according to spec or something
                            //then replace with the current type
                            originPart.DeleteRelationship(x.Rel.Id);
                            originPart.CreateRelationship(
                                tempPart.Uri, TargetMode.Internal,
                                relTypesSpec.FirstOrDefault());
                            break;
                        }
                    }

                    // now check, if the specs could be found with correct relationships
                    PackagePart specPart = null;
                    PackageRelationship specRel = null;
                    foreach (var x in FindAllRelationships(originPart, relTypesSpec,
                        filterForDeprecatedEquals: false))
                    {
                        specRel = x.Rel;
                        var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                        if (package.PartExists(absoluteURI))
                        {
                            specPart = package.GetPart(absoluteURI);
                        }
                    }

                    // check, if we have to change the spec part
                    if (specPart != null && specRel != null)
                    {
                        var name = System.IO.Path.GetFileNameWithoutExtension(
                            specPart.Uri.ToString()).ToLower().Trim();
                        var ext = System.IO.Path.GetExtension(specPart.Uri.ToString()).ToLower().Trim();
                        if ((ext == ".json" && prefFmt == SerializationFormat.Xml)
                             || (ext == ".xml" && prefFmt == SerializationFormat.Json)
                             || (name.StartsWith("aasenv-with-no-id")))
                        {
                            // try kill specpart
                            try
                            {
                                originPart.DeleteRelationship(specRel.Id);
                                package.DeletePart(specPart.Uri);
                            }
                            catch (Exception ex)
                            {
                                LogInternally.That.SilentlyIgnoredError(ex);
                            }
                            finally { specPart = null; specRel = null; }
                        }
                    }

                    if (specPart == null)
                    {
                        // create, as not existing
                        var frn = "aasenv-with-no-id";
                        if (_aasEnv.AssetAdministrationShellCount() > 0)
                            frn = _aasEnv.AllAssetAdministrationShells().FirstOrDefault()
                                    .GetFriendlyName() ?? frn;
                        var aas_spec_fn = "/aasx/#/#.aas";
                        if (prefFmt == SerializationFormat.Json)
                            aas_spec_fn += ".json";
                        else
                            aas_spec_fn += ".xml";
                        aas_spec_fn = aas_spec_fn.Replace("#", "" + frn);

                        // new: make sure the part is not existing anymore
                        var aas_spec_uri = new Uri(aas_spec_fn, UriKind.RelativeOrAbsolute);
                        if (package.PartExists(aas_spec_uri))
                            package.DeletePart(aas_spec_uri);

                        // now create
                        specPart = package.CreatePart(
                            new Uri(aas_spec_fn, UriKind.RelativeOrAbsolute),
                            System.Net.Mime.MediaTypeNames.Text.Xml, CompressionOption.Maximum);
                        originPart.CreateRelationship(
                            specPart.Uri, TargetMode.Internal,
                            relTypesSpec.FirstOrDefault());
                    }

                    // now, specPart shall be != null!
                    if (specPart.Uri.ToString().ToLower().Trim().EndsWith("json"))
                    {
                        using (var s = specPart.GetStream(FileMode.Create))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.NullValueHandling = NullValueHandling.Ignore;
                            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                            serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                            using (var sw = new StreamWriter(s))
                            {
                                using (JsonWriter writer = new JsonTextWriter(sw))
                                {
                                    serializer.Serialize(writer, _aasEnv);
                                }
                            }
                        }
                    }
                    else
                    {
                        using (var s = specPart.GetStream(FileMode.Create))
                        {

                            var writer = XmlWriter.Create(s, new XmlWriterSettings()
                            {
                                Indent = true,
                                OmitXmlDeclaration = true
                            });
                            Xmlization.Serialize.To(
                                _aasEnv, writer);
                            writer.Flush();
                            writer.Close();
                            s.Flush();
                        }
                    }

                    // Handling of aas_suppl namespace from v2 to v3
                    // Need to check/test in detail, with thumbnails as well
                    if (specPart != null)
                    {                        
                        foreach (var x in FindAllRelationships(
                            specPart, relTypesSuppl, 
                            filterForDeprecatedEquals: true).ToList())
                        {
                            var uri = x.Rel.TargetUri;
                            PackagePart filePart = null;
                            var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                            if (package.PartExists(absoluteURI))
                            {
                                filePart = package.GetPart(absoluteURI);

                                //delete old type, because its not according to spec or something
                                //then replace with the current type
                                specPart.DeleteRelationship(x.Rel.Id);
                                specPart.CreateRelationship(
                                    filePart.Uri, TargetMode.Internal,
                                    relTypesSuppl.FirstOrDefault());
                            }
                        }
                    }

                    // there might be pending files to be deleted (first delete, then add,
                    // in case of identical files in both categories)
                    foreach (var psfDel in _pendingFilesToDelete)
                    {
                        // try find an existing part for that file ..
                        var found = false;

                        // normal files
                        foreach (var x in FindAllRelationships(specPart, relTypesSuppl))
                            if (x.Rel.TargetUri == psfDel.Uri)
                            {
                                // try to delete
                                specPart.DeleteRelationship(x.Rel.Id);
                                package.DeletePart(psfDel.Uri);
                                found = true;
                                break;
                            }

                        // thumbnails
                        foreach (var x in FindAllRelationships(package, relTypesThumb))
                            if (x.Rel.TargetUri == psfDel.Uri)
                            {
                                // try to delete
                                package.DeleteRelationship(x.Rel.Id);
                                package.DeletePart(psfDel.Uri);
                                found = true;
                                break;
                            }

                        if (!found)
                            throw (new Exception(
                                $"Not able to delete pending file {psfDel.Uri} in saving package {fn}"));
                    }

                    // after this, there are no more pending for delete files
                    _pendingFilesToDelete.Clear();

                    // write pending supplementary files
                    foreach (var psfAdd in _pendingFilesToAdd)
                    {
                        // make sure ..
                        if ((psfAdd.SourceLocalPath == null && psfAdd.SourceGetBytesDel == null) ||
                            psfAdd.Location != AdminShellPackageSupplementaryFile.LocationType.AddPending)
                            continue;

                        // normal file?
                        if (psfAdd.SpecialHandling == AdminShellPackageSupplementaryFile.SpecialHandlingType.None ||
                            psfAdd.SpecialHandling ==
                                AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                        {

                            // try find an existing part for that file ..
                            PackagePart filePart = null;
                            if (psfAdd.SpecialHandling == AdminShellPackageSupplementaryFile.SpecialHandlingType.None)
                            {
                                foreach (var x in FindAllRelationships(specPart, relTypesSuppl))
                                    if (x.Rel.TargetUri == psfAdd.Uri)
                                    {
                                        //filePart = package.GetPart(x.TargetUri);
                                        var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                                        if (package.PartExists(absoluteURI))
                                        {
                                            filePart = package.GetPart(absoluteURI);
                                        }
                                        break;
                                    }

                                // try to fix?
                                if (filePart == null && package.PartExists(psfAdd.Uri))
                                {
                                    // brutally delete old one?
                                    package.DeletePart(psfAdd.Uri);
                                }
                            }
                            if (psfAdd.SpecialHandling ==
                                AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                            {
                                foreach (var x in FindAllRelationships(package, relTypesThumb))
                                    if (x.Rel.SourceUri.ToString() == "/" && x.Rel.TargetUri == psfAdd.Uri)
                                    {
                                        //filePart = package.GetPart(x.TargetUri);
                                        var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                                        if (package.PartExists(absoluteURI))
                                        {
                                            filePart = package.GetPart(absoluteURI);
                                        }
                                        break;
                                    }
                            }

                            if (filePart == null)
                            {
                                // determine mimeType
                                var mimeType = psfAdd.UseMimeType;
                                // reconcile mime
                                if (mimeType == null && psfAdd.SourceLocalPath != null)
                                    mimeType = AdminShellPackageFileBasedEnv.GuessMimeType(psfAdd.SourceLocalPath);
                                // still null?
                                if (mimeType == null)
                                    // see: https://stackoverflow.com/questions/6783921/
                                    // which-mime-type-to-use-for-a-binary-file-thats-specific-to-my-program
                                    mimeType = "application/octet-stream";

                                // create new part and link
                                filePart = package.CreatePart(psfAdd.Uri, mimeType, CompressionOption.Maximum);
                                if (psfAdd.SpecialHandling ==
                                    AdminShellPackageSupplementaryFile.SpecialHandlingType.None)
                                    specPart.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        relTypesSuppl.FirstOrDefault());
                                if (psfAdd.SpecialHandling ==
                                    AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail)
                                    package.CreateRelationship(
                                        filePart.Uri, TargetMode.Internal,
                                        relTypesThumb.FirstOrDefault());
                            }

                            // now should be able to write
                            using (var s = filePart.GetStream(FileMode.Create))
                            {
                                if (psfAdd.SourceLocalPath != null)
                                {
                                    var bytes = System.IO.File.ReadAllBytes(psfAdd.SourceLocalPath);
                                    s.Write(bytes, 0, bytes.Length);
                                }

                                if (psfAdd.SourceGetBytesDel != null)
                                {
                                    var bytes = psfAdd.SourceGetBytesDel();
                                    if (bytes != null)
                                        s.Write(bytes, 0, bytes.Length);
                                }
                            }
                        }
                    }

                    // after this, there are no more pending for add files
                    _pendingFilesToAdd.Clear();

                    // flush, but leave open
                    package.Flush();
                    _openPackage = package;

                    // if in temp fn, close the package, copy to original fn, re-open the package
                    if (_tempFn != null)
                        try
                        {
                            package.Close();
                            System.IO.File.Copy(_tempFn, _fn, overwrite: true);
                            _openPackage = Package.Open(_tempFn, FileMode.OpenOrCreate);
                        }
                        catch (Exception ex)
                        {
                            throw (new Exception(
                                string.Format("While write AASX {0} indirectly at {1} gave: {2}",
                                fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                        }

                    // done
                    ClearAllIdentifiableTaintedFlags();
                }
                catch (Exception ex)
                {
                    throw (new Exception(
                        string.Format("While write AASX {0} at {1} gave: {2}",
                        fn, AdminShellUtil.ShortLocation(ex), ex.Message)));
                }
                return true;
            }

            // Don't know to handle
            throw new Exception($"Does not know how to handle the file: {fn}");
        }

        /// <summary>
        /// Temporariyl saves & closes package and executes lambda. Afterwards, the package is re-opened
        /// under the same file name
        /// </summary>
        /// <param name="lambda">Action which is to be executed while the file is CLOSED</param>
        /// <param name="prefFmt">Format for the saved file</param>
        public override void TemporarilySaveCloseAndReOpenPackage(
            Action lambda,
            AdminShellPackageFileBasedEnv.SerializationFormat prefFmt = AdminShellPackageFileBasedEnv.SerializationFormat.None)
        {
            // access 
            if (!this.IsOpen)
                throw (new Exception(
                    string.Format("Could not temporarily close and re-open AASX {0}, because package" +
                    "not open as expected!", Filename)));

            // try-catch for the steps before the lambda
            try
            {
                // save (it will be open, still)
                SaveAs(this.Filename, prefFmt: prefFmt);

                // close
                _openPackage.Flush();
                _openPackage.Close();
            }
            catch (Exception ex)
            {
                throw (new Exception(
                    string.Format("While temporarily close and re-open AASX {0} at {1} gave: {2}",
                    Filename, AdminShellUtil.ShortLocation(ex), ex.Message)));
            }

            // try-catch for the lambda
            try
            {
                // execute lambda
                lambda?.Invoke();
            }
            catch (Exception ex)
            {
                throw (new Exception(
                    string.Format("While temporarily close and re-open AASX {0} at {1} gave: {2}",
                    Filename, AdminShellUtil.ShortLocation(ex), ex.Message)));
            }
            finally
            {
                // even after failing of the lambda, the package shall be re-opened
                if (Filename.ToLower().EndsWith(".aasx"))
                {
                    _openPackage = Package.Open(Filename, FileMode.OpenOrCreate);
                }
            }
        }

        private int BackupIndex = 0;

        public void BackupInDir(string backupDir, int maxFiles)
        {
            // access
            if (backupDir == null || maxFiles < 1)
                return;

            // we do it not caring on any errors
            try
            {
                // get index in form
                if (BackupIndex == 0)
                {
                    // do not always start at 0!!
                    var rnd = new Random();
                    BackupIndex = rnd.Next(maxFiles);
                }
                var ndx = BackupIndex % maxFiles;
                BackupIndex += 1;

                // build a filename
                var bdfn = Path.Combine(backupDir, $"backup{ndx:000}.xml");

                // raw save
                using (var s = new StreamWriter(bdfn))
                {
                    // dead-csharp off
                    //var serializer = new XmlSerializer(typeof(AasCore.Aas3_1_RC02.Environment));
                    //var nss = new XmlSerializerNamespaces();
                    //nss.Add("xsi", System.Xml.Schema.XmlSchema.InstanceNamespace);
                    //nss.Add("aas", "http://www.admin-shell.io/aas/2/0");
                    //nss.Add("IEC61360", "http://www.admin-shell.io/IEC61360/2/0");
                    //serializer.Serialize(s, _aasEnv, nss);
                    // dead-csharp on
                    var writer = XmlWriter.Create(s, new XmlWriterSettings()
                    {
                        Indent = true,
                        OmitXmlDeclaration = true
                    });
                    Xmlization.Serialize.To(
                        _aasEnv, writer);
                    writer.Flush();
                    writer.Close();
                    s.Flush();
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }
        }

        public override bool IsLocalFile(string uriString)
        {
            // look at the uri
            var sap = AdminShellUtil.GetSchemeAndPath(uriString);
            if (sap == null)
                return false;
            if (sap.Scheme != "file")
                return false;

            // look at package / file path
            if (_openPackage == null)
                return false;
            if (sap.Path == null || !sap.Path.StartsWith("/"))
                return false;

            // check further
            var isLocal = _openPackage.PartExists(new Uri(uriString, UriKind.RelativeOrAbsolute));
            return isLocal;
        }

        public override byte[] GetBytesFromPackageOrExternal(
            string uriString,
            string aasId = null,
            string smId = null,
            ISecurityAccessHandler secureAccess = null,
            string acceptHeader = null,
            string idShortPath = null)
        {
            // IMPORTANT! First try to use the base implementation to get an stream to
            // HTTP or ABSOLUTE file
            var absBytes = base.GetBytesFromPackageOrExternal(uriString, secureAccess: secureAccess);
            if (absBytes != null)
                return absBytes;

            // now, split uri string (again) for ourselves
            var sap = AdminShellUtil.GetSchemeAndPath(uriString);
            if (sap == null)
                return null;

            // now, it has to be an package file
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));

            // exist
            var puri = new Uri(sap.Path, UriKind.RelativeOrAbsolute);
            if (!_openPackage.PartExists(puri))
                throw (new Exception(string.Format($"AASX Package has no part {uriString}. Aborting!")));

            // get part
            var part = _openPackage.GetPart(puri);
            if (part == null)
                throw (new Exception(
                    string.Format($"Cannot access part {uriString} in {_fn}. Aborting!")));

            // read bytes
            using (var stream = part.GetStream(FileMode.Open, FileAccess.Read))
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public override async Task<byte[]> GetBytesFromPackageOrExternalAsync(
            string uriString,
            string aasId = null,
            string smId = null,
            ISecurityAccessHandler secureAccess = null,
            string idShortPath = null)
        {
            // IMPORTANT! First try to use the base implementation to get an stream to
            // HTTP or ABSOLUTE file
            var absBytes = await base.GetBytesFromPackageOrExternalAsync(uriString, secureAccess: secureAccess);
            if (absBytes != null)
                return absBytes;

            // now, split uri string (again) for ourselves
            var sap = AdminShellUtil.GetSchemeAndPath(uriString);
            if (sap == null)
                return null;

            // now, it has to be an package file
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));

            // exist
            var puri = new Uri(sap.Path, UriKind.RelativeOrAbsolute);
            if (!_openPackage.PartExists(puri))
                throw (new Exception(string.Format($"AASX Package has no part {sap.Path}. Aborting!")));

            // get part
            var part = _openPackage.GetPart(puri);
            if (part == null)
                throw (new Exception(
                    string.Format($"Cannot access part {sap.Path} in {_fn}. Aborting!")));

            // read bytes
            using (var stream = part.GetStream(FileMode.Open, FileAccess.Read))
            using (MemoryStream ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                return ms.ToArray();
            }
        }

        public override bool PutBytesToPackageOrExternal(
            string uriString,
            byte[] data,
            string aasId = null,
            string smId = null,
            string idShortPath = null)
        {
            // split uri string
            var sap = AdminShellUtil.GetSchemeAndPath(uriString);
            if (sap == null)
                return false;

            // if not a file, refer to base
            // also, if package is not open or is no local file
            if (sap.Scheme != "file"
                || _openPackage == null
                || !IsLocalFile(uriString))
            {
                return base.PutBytesToPackageOrExternal(uriString, data, aasId, smId, idShortPath);
            }

            // now, we're supposed to handle it!

            // exist
            var puri = new Uri(sap.Path, UriKind.RelativeOrAbsolute);
            if (!_openPackage.PartExists(puri))
                throw (new Exception(string.Format($"AASX Package has no part {sap.Path}. Aborting!")));

            // get part
            var part = _openPackage.GetPart(puri);
            if (part == null)
                throw (new Exception(
                    string.Format($"Cannot access part {sap.Path} in {_fn}. Aborting!")));

            // read bytes
            using (var stream = part.GetStream(FileMode.Create, FileAccess.Write))
            using (MemoryStream ms = new MemoryStream(data))
            {
                ms.CopyTo(stream);
            }

            return true;
        }

        public async Task ReplaceSupplementaryFileInPackageAsync(string sourceUri, string targetFile, string targetContentType, Stream fileContent)
        {
            // access
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));

            if (!string.IsNullOrEmpty(sourceUri))
            {
                _openPackage.DeletePart(new Uri(sourceUri, UriKind.RelativeOrAbsolute));

            }
            var targetUri = PackUriHelper.CreatePartUri(new Uri(targetFile, UriKind.RelativeOrAbsolute));
            PackagePart packagePart = _openPackage.CreatePart(targetUri, targetContentType);
            fileContent.Position = 0;
            using (Stream dest = packagePart.GetStream())
            {
                await fileContent.CopyToAsync(dest);
            }
        }

        public long GetStreamSizeFromPackage(string uriString)
        {
            long res = 0;
            try
            {
                if (_openPackage == null)
                    return 0;

                PackagePart part = null;
                var uri = new Uri(uriString, UriKind.RelativeOrAbsolute);
                if (_openPackage.PartExists(uri))
                {
                    part = _openPackage.GetPart(uri);
                }
                if (part != null)
                {
                    using (var s = part.GetStream(FileMode.Open))
                    {
                        res = s.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
                return 0;
            }
            return res;
        }

        public override byte[] GetLocalThumbnailBytes(ref Uri thumbUri)
        {
            // access
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));
            // get the thumbnail over the relationship
            PackagePart thumbPart = null;
            foreach (var x in FindAllRelationships(_openPackage, relTypesThumb))
                if (x.Rel.SourceUri.ToString() == "/")
                {
                    //thumbPart = _openPackage.GetPart(x.TargetUri);
                    var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                    if (_openPackage.PartExists(absoluteURI))
                    {
                        thumbPart = _openPackage.GetPart(absoluteURI);
                    }
                    thumbUri = x.Rel.TargetUri;
                    break;
                }
            if (thumbPart == null)
                throw (new Exception("Unable to find AASX thumbnail. Aborting!"));

            // read bytes
            using (var stream = thumbPart.GetStream(FileMode.Open, FileAccess.Read))
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }        

        public override ListOfAasSupplementaryFile GetListOfSupplementaryFiles()
        {
            // new result
            var result = new ListOfAasSupplementaryFile();

            // access
            if (_openPackage != null)
            {
                // get the thumbnail(s) from the package
                foreach (var x in FindAllRelationships(_openPackage, relTypesThumb))
                    if (x.Rel.SourceUri.ToString() == "/")
                    {
                        result.Add(new AdminShellPackageSupplementaryFile(
                            x.Rel.TargetUri,
                            location: AdminShellPackageSupplementaryFile.LocationType.InPackage,
                            specialHandling: AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail));
                    }

                // get the origin from the package
                PackagePart originPart = null;
                foreach (var x in FindAllRelationships(_openPackage, relTypesOrigin))
                    if (x.Rel.SourceUri.ToString() == "/")
                    {
                        //originPart = _openPackage.GetPart(x.TargetUri);
                        var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                        if (_openPackage.PartExists(absoluteURI))
                        {
                            originPart = _openPackage.GetPart(absoluteURI);
                        }
                        break;
                    }

                if (originPart != null)
                {
                    // get the specs from the origin
                    PackagePart specPart = null;
                    foreach (var x in FindAllRelationships(originPart, relTypesSpec))
                    {
                        //specPart = _openPackage.GetPart(x.TargetUri);
                        var absoluteURI = PackUriHelper.ResolvePartUri(x.Rel.SourceUri, x.Rel.TargetUri);
                        if (_openPackage.PartExists(absoluteURI))
                        {
                            specPart = _openPackage.GetPart(absoluteURI);
                        }
                        break;
                    }

                    if (specPart != null)
                    {
                        // get the supplementaries from the package, derived from spec
                        foreach (var x in FindAllRelationships(specPart, relTypesSuppl))
                        {
                            result.Add(
                                new AdminShellPackageSupplementaryFile(
                                    x.Rel.TargetUri, 
                                    location: AdminShellPackageSupplementaryFile.LocationType.InPackage));
                        }
                    }
                }
            }

            // add or modify the files to delete
            foreach (var psfDel in _pendingFilesToDelete)
            {
                // already in
                var found = result.Find(x => { return x.Uri == psfDel.Uri; });
                if (found != null)
                    found.Location = AdminShellPackageSupplementaryFile.LocationType.DeletePending;
                else
                {
                    psfDel.Location = AdminShellPackageSupplementaryFile.LocationType.DeletePending;
                    result.Add(psfDel);
                }
            }

            // add the files to store as well
            foreach (var psfAdd in _pendingFilesToAdd)
            {
                // already in (should not happen ?!)
                var found = result.Find(x => { return x.Uri == psfAdd.Uri; });
                if (found != null)
                    found.Location = AdminShellPackageSupplementaryFile.LocationType.AddPending;
                else
                {
                    psfAdd.Location = AdminShellPackageSupplementaryFile.LocationType.AddPending;
                    result.Add(psfAdd);
                }
            }

            // done
            return result;
        }

        public static string GuessMimeType(string fn)
        {
            var file_ext = System.IO.Path.GetExtension(fn).ToLower().Trim();
            var content_type = System.Net.Mime.MediaTypeNames.Text.Plain;
            if (file_ext == ".pdf") content_type = System.Net.Mime.MediaTypeNames.Application.Pdf;
            if (file_ext == ".xml") content_type = System.Net.Mime.MediaTypeNames.Text.Xml;
            if (file_ext == ".txt") content_type = System.Net.Mime.MediaTypeNames.Text.Plain;
            if (file_ext == ".igs") content_type = "application/iges";
            if (file_ext == ".iges") content_type = "application/iges";
            if (file_ext == ".stp") content_type = "application/step";
            if (file_ext == ".step") content_type = "application/step";
            if (file_ext == ".jpg") content_type = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            if (file_ext == ".jpeg") content_type = System.Net.Mime.MediaTypeNames.Image.Jpeg;
            if (file_ext == ".png") content_type = "image/png";
            if (file_ext == ".gif") content_type = System.Net.Mime.MediaTypeNames.Image.Gif;
            return content_type;
        }

        public override void PrepareSupplementaryFileParameters(ref string targetDir, ref string targetFn)
        {
            // re-work target dir
            if (targetDir != null)
                targetDir = targetDir.Replace(@"\", "/");

            // rework targetFn
            if (targetFn != null)
                targetFn = Regex.Replace(targetFn, @"[^A-Za-z0-9-.]+", "_");
        }

        /// <summary>
        /// Add a file as supplementary file to package. Operation will be pending, package needs to be saved in order
        /// materialize embedding.
        /// </summary>
        /// <returns>Target path of file in package</returns>
        public override string AddSupplementaryFileToStore(
            string sourcePath, string targetDir, string targetFn, bool embedAsThumb,
            AdminShellPackageSupplementaryFile.SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            // beautify parameters
            if ((sourcePath == null && sourceGetBytesDel == null) || targetDir == null || targetFn == null)
                return null;

            // build target path
            targetDir = targetDir.Trim();
            if (!targetDir.EndsWith("/"))
                targetDir += "/";
            targetFn = targetFn.Trim();
            if (sourcePath == "" || targetDir == "" || targetFn == "")
                throw (new Exception("Trying add supplementary file with empty name or path!"));

            var targetPath = "" + targetDir.Trim() + targetFn.Trim();

            // base function
            AddSupplementaryFileToStore(sourcePath, targetPath, embedAsThumb, sourceGetBytesDel, useMimeType);

            // return target path
            return targetPath;
        }

        public void AddSupplementaryFileToStore(string sourcePath, string targetPath, bool embedAsThumb,
            AdminShellPackageSupplementaryFile.SourceGetByteChunk sourceGetBytesDel = null, string useMimeType = null)
        {
            // beautify parameters
            if ((sourcePath == null && sourceGetBytesDel == null) || targetPath == null)
                return;

            sourcePath = sourcePath?.Trim();
            targetPath = targetPath.Trim();

            // add record
            _pendingFilesToAdd.Add(
                new AdminShellPackageSupplementaryFile(
                    new Uri(targetPath, UriKind.RelativeOrAbsolute),
                    sourcePath,
                    location: AdminShellPackageSupplementaryFile.LocationType.AddPending,
                    specialHandling: (embedAsThumb
                        ? AdminShellPackageSupplementaryFile.SpecialHandlingType.EmbedAsThumbnail
                        : AdminShellPackageSupplementaryFile.SpecialHandlingType.None),
                    sourceGetBytesDel: sourceGetBytesDel,
                    useMimeType: useMimeType)
                );

        }

        public override void DeleteSupplementaryFile(AdminShellPackageSupplementaryFile psf)
        {
            if (psf == null)
                throw (new Exception("No supplementary file given!"));

            if (psf.Location == AdminShellPackageSupplementaryFile.LocationType.AddPending)
            {
                // is still pending in add list -> remove
                _pendingFilesToAdd.RemoveAll((x) => { return x.Uri == psf.Uri; });
            }

            if (psf.Location == AdminShellPackageSupplementaryFile.LocationType.InPackage)
            {
                // add to pending delete list
                _pendingFilesToDelete.Add(psf);
            }
        }

        public override void Close()
        {
            _openPackage?.Close();
            _openPackage = null;
            _fn = "";
            _aasEnv = null;
        }

        public override void Flush()
        {
            if (_openPackage != null)
                _openPackage.Flush();
        }

        public override void Dispose()
        {
            Close();
        }

        public void EmbeddAssetInformationThumbnail(IResource defaultThumbnail, Stream fileContent)
        {
            // access
            if (_openPackage == null)
                throw (new Exception(string.Format($"AASX Package {_fn} not opened. Aborting!")));

            if (!string.IsNullOrEmpty(defaultThumbnail.Path))
            {
                var sourceUri = defaultThumbnail.Path.Replace(Path.DirectorySeparatorChar, '/');
                _openPackage.DeletePart(new Uri(sourceUri, UriKind.RelativeOrAbsolute));

            }
            var targetUri = PackUriHelper.CreatePartUri(new Uri(defaultThumbnail.Path, UriKind.RelativeOrAbsolute));

            PackagePart packagePart = _openPackage.CreatePart(targetUri, defaultThumbnail.ContentType, compressionOption: CompressionOption.Maximum);

            _openPackage.CreateRelationship(packagePart.Uri, TargetMode.Internal,
                                        "http://schemas.openxmlformats.org/package/2006/" +
                                        "relationships/metadata/thumbnail");

            //Write to the part
            fileContent.Position = 0;
            using (Stream dest = packagePart.GetStream())
            {
                fileContent.CopyTo(dest);
            }
        }
    }
}