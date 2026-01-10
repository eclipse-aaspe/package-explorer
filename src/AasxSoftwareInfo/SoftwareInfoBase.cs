/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System.Reflection;
using System.Text.RegularExpressions;

namespace AasxSoftwareInfo
{
    /// <summary>
    /// Information about the application.
    /// Refactored. Idea: no dependencies of PackageLogic or Plugins, in order to be loaded
    /// early/ fast.
    /// </summary>
    public class SoftwareInfoBase
    {
        public readonly string Authors;

        /// <summary>
        /// The current (used) licenses of the application.
        /// </summary>
        public readonly string LicenseShort;

        /// <summary>
        /// The last build date of the application.
        /// </summary>
        public readonly string BuildDate;

        /// <summary>
        /// Git description.
        /// Hopefully: [current tag]-[commits since tags]-[abbreviated commit hash]
        /// </summary>
        public readonly string GitDescribe;

        /// <summary>
        /// The full license texts of the application.
        /// Note: In the base class, this is not populated!
        /// </summary>
        public readonly string LicenseLong;

        /// <summary>
        /// The current version string of the application.
        /// Note: in the past, there was a semantic version such as "1.9.8.3", but
        /// this was not maintained properly. Now, a version is derived from the
        /// build data with the intention, that the according tag in Github-Releases
        /// will be identical.
        /// </summary>
        public readonly string Version;

        public SoftwareInfoBase(string authors, string licenseShort, string buildDate, string licenseLong, string version,
            string gitDescribe)
        {
            Authors = authors;
            LicenseShort = licenseShort;
            BuildDate = buildDate;
            LicenseLong = licenseLong;
            Version = version;
            GitDescribe = gitDescribe;
        }

        /// <summary>
        /// Reads the necessary resources from the system and produces the author, license, build and version
        /// information about the application.
        /// </summary>
        /// <returns>relevant information about the application</returns>
        public static SoftwareInfoBase Read()
        {
            string authors = "Michael Hoffmeister, Andreas Orzelski, Erich Barnstedt, Juilee Tikekar et al.";

            string licenseShort =
                "This software is licensed under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The Newtonsoft.JSON serialization is licensed under the MIT License (MIT)." + Environment.NewLine +
                "The QR code generation is licensed under the MIT license (MIT)." + Environment.NewLine +
                "The Zxing.Net Dot Matrix Code (DMC) generation is licensed " +
                "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The Grapevine REST server framework is licensed " +
                "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The AutomationML.Engine is licensed under the MIT license (MIT)." +
                "The MQTT server and client is licensed " +
                "under the MIT license (MIT)." + Environment.NewLine +
                "The IdentityModel OpenID client is licensed " +
                "under the Apache License 2.0 (Apache-2.0)." + Environment.NewLine +
                "The jose-jwt object signing and encryption is licensed " +
                "under the MIT license (MIT).";

            string buildDate = "";
            using (var stream =
                Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("AasxSoftwareInfo.Resources.BuildDate.txt"))
            {
                if (stream != null)
                {
                    TextReader tr = new StreamReader(stream);
                    string fileContents = tr.ReadToEnd();
                    if (fileContents.Length > 20)
                        fileContents = fileContents.Substring(0, 20) + "..";
                    buildDate = fileContents.Trim();
                }
            }

            string gitDesc = "";
            using (var stream =
                Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("AasxSoftwareInfo.Resources.GitDescribe.txt"))
            {
                if (stream != null)
                {
                    TextReader tr = new StreamReader(stream);
                    string fileContents = tr.ReadToEnd();
                    if (fileContents.Length > 50)
                        fileContents = fileContents.Substring(0, 50) + "..";
                    gitDesc = fileContents.Trim();
                }
            }

            string version = "(not available)";
            {
                // %date% in European format (e.g. during development)
                var m = Regex.Match(buildDate, @"(\d+)\.(\d+)\.(\d+)");
                if (m.Success && m.Groups.Count >= 4)
                {
                    version = "v" + ((m.Groups[3].Value.Length == 2) ? "20" : "")
                                  + m.Groups[3].Value + "-"
                                  + m.Groups[2].Value + "-"
                                  + m.Groups[1].Value;
                }
                else
                {
                    // %date% in US local (e.g. from continuous integration from Github)
                    m = Regex.Match(buildDate, @"(\d+)\/(\d+)\/(\d+)");
                    if (m.Success && m.Groups.Count >= 4)
                        version = "v" + ((m.Groups[3].Value.Length == 2) ? "20" : "")
                                      + m.Groups[3].Value + "-"
                                      + m.Groups[1].Value + "-"
                                      + m.Groups[2].Value;
                }
            }

            return new SoftwareInfoBase(authors, licenseShort, buildDate, "", version, gitDesc);
        }
    }
}
