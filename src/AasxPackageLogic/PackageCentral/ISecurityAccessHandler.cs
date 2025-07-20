/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static AasxPackageLogic.PackageCentral.PackageContainerHttpRepoSubset;

namespace AasxPackageLogic.PackageCentral
{
    /// <summary>
    /// This enum distincts between different methods an security authentification could be 
    /// determined.
    /// </summary>
    public enum SecurityAccessMethod { 
        /// <summary>
        /// Without authorization/ athentification
        /// </summary>
        None, 
        /// <summary>
        /// Ask before every attempt to connect for a certain grant time
        /// </summary>
        Ask, 
        /// <summary>
        /// Basic = username / password
        /// </summary>
        Basic, 
        /// <summary>
        /// Having a certificate in e.g. the Windows cert store
        /// </summary>
        CertificateStore, 
        /// <summary>
        /// External file, e.g. PKI file
        /// </summary>
        File, 
        /// <summary>
        /// Interactive entry, e.g. ENTRA id
        /// </summary>
        InteractiveEntry 
    }

    /// <summary>
    /// This class comprises the data the user has to provide/ choose in order to authenticate or
    /// authorize himself against a AAS server, Repository or Registry.
    /// </summary>
    public class SecurityAccessUserInfo
    {
        /// <summary>
        /// Preferred method to authenticate or authorize
        /// </summary>
        public SecurityAccessMethod Method;

        /// <summary>
        /// If empty, the user will be prompted.
        /// </summary>
        public string Username = "";

        /// <summary>
        /// If empty, the user will be prompted.
        /// </summary>
        public string Password = "";

        /// <summary>
        /// Endpoint of the authentification server
        /// </summary>
        public string AuthServer = "";

        /// <summary>
        /// (Absolute) filename of a certificate file to use.
        /// </summary>
        public string CertFile = "";

        /// <summary>
        /// Password to open the provided certificate file.
        /// </summary>
        public string CertPassword = "";

        /// <summary>
        /// Renew the security credential after a certain period in time in minutes.
        /// Note: Default is rather short because of secure by design
        /// </summary>
        public int RenewPeriodMins = 5;
    }

    public class KnownEndpointDescription
    {
        /// <summary>
        /// The part of the URI, e.g. HTTP address, which is variable before the API spec
        /// will add.
        /// </summary>
        public string BaseAddress;

        /// <summary>
        /// Infos how to access the endpoint security-wise
        /// </summary>
        public SecurityAccessUserInfo AccessInfo;
    }

    /// <summary>
    /// This interface allows accessing a handler to get some access information (e.g. HTTP headers)
    /// suitable to access some AAS registries or repositories.
    /// </summary>
    public interface ISecurityAccessHandler
    {
        Task<HttpHeaderDataItem> DetermineAuthenticateHeader(string baseAddress);
        void ClearAllCredentials();
    }
}
