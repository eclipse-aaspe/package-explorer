/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// In order to use this define, a reference to System.Drawing.Common in required
#define UseMagickNet

#if UseMagickNet

using AdminShellNS;
using AnyUi;
using Extensions;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_1;

namespace AasxIntegrationBaseGdi
{
    /// <summary>
    /// This class exists multiple time in source code, controlled by #defines.
    /// Only one #define shall be given.
    /// This class understands the term GDI to be "graphics dependent inteface" ;-)
    /// </summary>
    public static class AnyUiGdiHelper
    {
        public static AnyUiBitmapInfo CreateAnyUiBitmapInfo(MagickImage source, bool doFreeze = true)
        {
            var res = new AnyUiBitmapInfo();

            if (source != null)
            {
                // take over direct data
                res.ImageSource = source;
                res.PixelWidth = source.Width;
                res.PixelHeight = source.Height;

                // provide PNG as well
                using (var cloneImage = source.Clone())
                {
                    cloneImage.Format = MagickFormat.Png;
                    res.PngData = cloneImage.ToByteArray();
                }
            }

            return res;
        }

        public static AnyUiBitmapInfo CreateAnyUiBitmapInfo(string path)
        {
            var bi = new MagickImage(path);
            return CreateAnyUiBitmapInfo(bi);
        }

        public static AnyUiBitmapInfo CreateAnyUiBitmapFromResource(string path,
            Assembly assembly = null)
        {
            try
            {
                if (assembly == null)
                    assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(path))
                {
                    if (stream == null)
                        return null;
                    var bi = new MagickImage(stream);
                    return CreateAnyUiBitmapInfo(bi);
                }
            }
            catch (Exception ex)
            {
                LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        public static AnyUiBitmapInfo LoadBitmapInfoFromBytes(byte[] ba)
        {
            if (ba == null || ba.Length < 1)
                return null;

            try
            {
                using (var ms = new MemoryStream(ba))
                {
                    // load image
                    var bi = new MagickImage(ms);
                    var binfo = CreateAnyUiBitmapInfo(bi);

                    // give this back
                    return binfo;
                }
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        public static AnyUiBitmapInfo LoadBitmapInfoFromPackage(
            AdminShellPackageEnvBase package, string path,
            ISecurityAccessHandler secureAccess = null,
            bool transparentBackground = false)
        {
            if (package == null || path == null)
                return null;

            try
            {
                var inputBytes = package.GetBytesFromPackageOrExternal(path, 
                    acceptHeader: "image/png, image/jpeg, image/gif",
                    secureAccess: secureAccess);
                if (inputBytes == null)
                    return null;

                // load image
                MagickImage bi = null;
                if (transparentBackground)
                {

                    var magicReadSettings = new MagickReadSettings
                    {
                        ColorSpace = ColorSpace.Transparent,
                        BackgroundColor = MagickColors.Transparent,
                    };

                    bi = new MagickImage(inputBytes, magicReadSettings);
                }
                else
                {
                    bi = new MagickImage(inputBytes);
                }
                
                var binfo = CreateAnyUiBitmapInfo(bi);

                // give this back
                return binfo;
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            return null;
        }

        // DEPRECATED
        //public static AnyUiBitmapInfo LoadBitmapInfoFromStream(Stream stream)
        //{
        //    if (stream == null)
        //        return null;

        //    try
        //    {
        //        // load image
        //        var bi = new MagickImage(stream);
        //        var binfo = CreateAnyUiBitmapInfo(bi);

        //        // give this back
        //        return binfo;
        //    }
        //    catch (Exception ex)
        //    {
        //        AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
        //    }

        //    return null;
        //}

        // TODO (MIHO, 2023-02-23): make the whole thing async!!

        public static async Task<AnyUiBitmapInfo> MakePreviewFromPackageOrUrlAsync(
            AdminShellPackageEnvBase package, string path,
            string aasId, string smId, string idShortPath,
            double dpi = 75)
        {
            if (path == null)
                return null;

            AnyUiBitmapInfo res = null;

            try
            {
                byte[] thumbBytes = null;
                if (true /*= package?.IsLocalFile(path)*/)
                    thumbBytes = await package.GetBytesFromPackageOrExternalAsync(path, aasId, smId, 
                        idShortPath: idShortPath);
                else
                {
                    // try download
#if __old
                    var wc = new WebClient();                    
                    thumbStream = wc.OpenRead(path);
#else
                    // dead-csharp off
                    /*
                     * 
                    // upgrade to HttpClient and follow re-directs
                    var hc = new HttpClient();
                    var response = hc.GetAsync(path).GetAwaiter().GetResult();

                    // if you call response.EnsureSuccessStatusCode here it will throw an exception
                    if (response.StatusCode == HttpStatusCode.Moved
                        || response.StatusCode == HttpStatusCode.Found)
                    {
                        var location = response.Headers.Location;
                        response = hc.GetAsync(location).GetAwaiter().GetResult();
                    }

                    response.EnsureSuccessStatusCode();
                    thumbStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    */
                    // dead-csharp on
#endif
                }

                if (thumbBytes == null)
                    return null;

                using (var images = new MagickImageCollection())
                {
                    var settings = new MagickReadSettings();
                    settings.Density = new Density(dpi);
                    settings.FrameIndex = 0; // First page
                    settings.FrameCount = 1; // Number of pages

                    // Read only the first page of the pdf file
                    images.Read(thumbBytes, settings);

                    if (images.Count > 0 && images[0] is MagickImage img)
                    {
                        res = CreateAnyUiBitmapInfo(img);
                    }
                }
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            return res;
        }

        //
        // Support for loading multiple files one after each other
        // 

        public class DelayedFileContentLoadBase
        {
            public Action<DelayedFileContentLoadBase, AnyUiBitmapInfo> LambdaLoaded;
        }

        public class DelayedFileContentForFileElement : DelayedFileContentLoadBase
        {
            public AdminShellPackageEnvBase Package;
            public string FileUri;
            
            public string AasId;
            public string SmId;
            public string IdShortPath;
        }

        public class DelayedFileContentLoader
        {
            protected List<DelayedFileContentLoadBase> _jobs = new List<DelayedFileContentLoadBase>();

            public void Add(DelayedFileContentLoadBase job)
            {
                lock (_jobs)
                {
                    _jobs.Add(job);
                }
            }

            /// <summary>
            /// Make sure, that <c>Submodel.SetParents()</c> has been executed!
            /// </summary>
            /// <param name="package"></param>
            /// <param name="aasId"></param>
            /// <param name=""></param>
            public void Add(             
                AdminShellPackageEnvBase package, 
                Aas.IAssetAdministrationShell aas, 
                Aas.ISubmodel submodel,
                Aas.IFile fileElem,
                Action<DelayedFileContentLoadBase, AnyUiBitmapInfo> lambdaLoaded)
            {
                // access
                if (package == null || aas == null || submodel == null || fileElem == null)
                    return;

                var idShortPath = "" + fileElem.CollectIdShortPathByParent(
                        separatorChar: '.', excludeIdentifiable: true);


                Add(new DelayedFileContentForFileElement()
                {
                    LambdaLoaded = lambdaLoaded,
                    Package = package,
                    AasId = "" + aas.Id,
                    SmId = "" + submodel.Id,
                    IdShortPath = idShortPath,
                    FileUri = fileElem.Value
                });
            }

            public async Task<bool> TickToLoad(
                ISecurityAccessHandler secureAccess)
            {
                if (_jobs.Count < 1)
                    return false;

                // pick one and start
                DelayedFileContentLoadBase job = null;
                lock (_jobs)
                {
                    job = _jobs.First();
                    _jobs.RemoveAt(0);
                }
                if (job == null)
                    return false;

                // now try loading
                if (job is DelayedFileContentForFileElement jobfc)
                {
                    var inputBytes = await jobfc.Package?.GetBytesFromPackageOrExternalAsync(
                        uriString: jobfc.FileUri,
                        aasId: "" + jobfc.AasId,
                        smId: "" + jobfc.SmId,
                        secureAccess: secureAccess, 
                        idShortPath: jobfc.IdShortPath);

                    var bi = AnyUiGdiHelper.LoadBitmapInfoFromBytes(inputBytes);

                    if (bi != null)
                    {
                        jobfc.LambdaLoaded?.Invoke(jobfc, bi);
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

#endif
