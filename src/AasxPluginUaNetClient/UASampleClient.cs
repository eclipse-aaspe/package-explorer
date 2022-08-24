/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>,
Author: Michael Hoffmeister.
Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

For OPC Content:

This code is based on:
https://github.com/OPCFoundation/UA-.NETStandard-Samples/tree/master/Samples/NetCoreConsoleClient
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using AdminShellNS;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace SampleClient
{
    public class UASampleClient
    {
        const int ReconnectPeriod = 10;
        Session session;
        SessionReconnectHandler reconnectHandler;
        string endpointURL;
        int clientRunTime = Timeout.Infinite;
        static bool autoAccept = true;
        static ExitCode exitCode;
        string userName;
        string password;

        public UASampleClient(string _endpointURL, bool _autoAccept, int _stopTimeout,
            string _userName, string _password)
        {
            endpointURL = _endpointURL;
            autoAccept = _autoAccept;
            clientRunTime = _stopTimeout <= 0 ? Timeout.Infinite : _stopTimeout * 1000;
            userName = _userName;
            password = _password;
        }

        public void Run()
        {
            try
            {
                ConsoleSampleClient().Wait();
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                return;
            }

            ManualResetEvent quitEvent = new ManualResetEvent(false);
            try
            {
                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    quitEvent.Set();
                    eArgs.Cancel = true;
                };
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }

            // wait for timeout or Ctrl-C
            quitEvent.WaitOne(clientRunTime);

            // return error conditions
            if (session.KeepAliveStopped)
            {
                exitCode = ExitCode.ErrorNoKeepAlive;
                return;
            }

            exitCode = ExitCode.Ok;
        }

        public static ExitCode ExitCode { get => exitCode; }

        public async Task ConsoleSampleClient()
        {
            Console.WriteLine("1 - Create an Application Configuration.");
            exitCode = ExitCode.ErrorCreateApplication;

            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = "UA Core Sample Client",
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = Utils.IsRunningOnMono() ? "Opc.Ua.MonoSampleClient" : "Opc.Ua.SampleClient"
            };

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            config.ApplicationUri = Utils.GetApplicationUriFromCertificate(
                config.SecurityConfiguration.ApplicationCertificate.Certificate);
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                autoAccept = true;
            }
            config.CertificateValidator.CertificateValidation +=
                // ReSharper disable once RedundantDelegateCreation
                new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            Console.WriteLine("2 - Discover endpoints of {0}.", endpointURL);
            exitCode = ExitCode.ErrorDiscoverEndpoints;
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointURL, true, 15000);
            Console.WriteLine("    Selected endpoint uses: {0}",
                selectedEndpoint.SecurityPolicyUri.Substring(selectedEndpoint.SecurityPolicyUri.LastIndexOf('#') + 1));

            Console.WriteLine("3 - Create a session with OPC UA server.");
            exitCode = ExitCode.ErrorCreateSession;
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

            session = await Session.Create(config, endpoint, false, "OPC UA Console Client", 60000,
                new UserIdentity(userName, password), null);

            // register keep alive handler
            session.KeepAlive += Client_KeepAlive;
        }

        private void Client_KeepAlive(Session sender, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                Console.WriteLine("{0} {1}/{2}", e.Status, sender.OutstandingRequestCount, sender.DefunctRequestCount);

                if (reconnectHandler == null)
                {
                    Console.WriteLine("--- RECONNECTING ---");
                    reconnectHandler = new SessionReconnectHandler();
                    reconnectHandler.BeginReconnect(sender, ReconnectPeriod * 1000, Client_ReconnectComplete);
                }
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, reconnectHandler))
            {
                return;
            }

            if (reconnectHandler != null)
            {
                session = reconnectHandler.Session;
                reconnectHandler.Dispose();
            }

            reconnectHandler = null;

            Console.WriteLine("--- RECONNECTED ---");
        }

        private static void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, value.Value, value.SourceTimestamp,
                    value.StatusCode);
            }
        }

        private static void CertificateValidator_CertificateValidation(CertificateValidator validator,
            CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = autoAccept;
                if (autoAccept)
                {
                    Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
                }
                else
                {
                    Console.WriteLine("Rejected Certificate: {0}", e.Certificate.Subject);
                }
            }
        }

        public string ReadSubmodelElementValue(string nodeName, int index)

        {
            NodeId node = new NodeId(nodeName, (ushort)index);
            return (session.ReadValue(node).ToString());
        }

    }
}
