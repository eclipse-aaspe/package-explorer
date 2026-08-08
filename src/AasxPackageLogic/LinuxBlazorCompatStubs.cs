// Linux Blazor port compatibility layer. These stubs keep Windows-only
// workflows out of the Linux web build while preserving the local AASX viewer.
using System;
using System.Threading.Tasks;
using AnyUi;

namespace System.Windows.Forms
{
    public enum DialogResult { None = 0, OK = 1, Cancel = 2, Abort = 3, Retry = 4, Ignore = 5, Yes = 6, No = 7 }
    public enum MessageBoxButtons { OK = 0, OKCancel = 1, AbortRetryIgnore = 2, YesNoCancel = 3, YesNo = 4, RetryCancel = 5 }

    public static class MessageBox
    {
        public static DialogResult Show(string text, string caption = null, MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            return buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel
                ? DialogResult.No
                : DialogResult.OK;
        }
    }

    public class AxHost { public class State { } }

    namespace VisualStyles
    {
        public static class VisualStyleElement
        {
            public static class Window { }
            public static class StartPanel { }
        }
    }
}

namespace System.Windows.Annotations
{
    public class Annotation { }
}


namespace System.Windows
{
    public readonly struct FontWeight
    {
        public FontWeight(string name) { Name = name; }
        public string Name { get; }
        public override string ToString() => Name ?? string.Empty;
    }

    public class Application
    {
        public static Application Current { get; } = null;
        public Dispatcher Dispatcher { get; } = new Dispatcher();
    }

    public class Dispatcher
    {
        public T Invoke<T>(Func<T> callback) => callback();
    }

    public static class FontWeights
    {
        public static FontWeight Normal { get; } = new FontWeight("Normal");
        public static FontWeight Bold { get; } = new FontWeight("Bold");
    }
}

namespace Microsoft.VisualBasic.ApplicationServices
{
    public class ApplicationBase { }
}

namespace System.Security.RightsManagement
{
    public class UseLicense { }
}

namespace System.Windows.Media
{
    public readonly struct FontWeight
    {
        public FontWeight(string name) { Name = name; }
        public string Name { get; }
        public override string ToString() => Name ?? string.Empty;
    }

    public static class FontWeights
    {
        public static FontWeight Normal { get; } = new FontWeight("Normal");
        public static FontWeight Bold { get; } = new FontWeight("Bold");
    }
}

namespace System.Security.Cryptography.X509Certificates
{
    public enum X509SelectionFlag { SingleSelection = 0 }

    public static class X509Certificate2UI
    {
        public static X509Certificate2Collection SelectFromCollection(
            X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag)
        {
            throw new PlatformNotSupportedException("Interactive certificate selection is not available in the Linux Blazor port.");
        }
    }
}

namespace AasxOpenIdClient
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    public class OpenIdClientInstance
    {
        public class UiLambdaSet
        {
            public Func<string, string, string, AnyUiMessageBoxButton, AnyUiMessageBoxResult> MesssageBox;
        }

        public string email = string.Empty;
        public string ssiURL = string.Empty;
        public string keycloak = string.Empty;
        public string token = string.Empty;
        public string authServer = string.Empty;

        public Task<TokenResponse> RequestTokenAsync(object certificate, UiLambdaSet uiLambda = null)
        {
            throw new PlatformNotSupportedException("OpenID remote repository authentication is not available in the Linux Blazor port.");
        }
    }

    public static class OpenIDClient
    {
        public static bool auth = false;
        public static string ssiURL = string.Empty;
        public static string keycloak = string.Empty;
        public static string email = string.Empty;
        public static string token = string.Empty;
        public static string authServer = string.Empty;

        public static Task<TokenResponse> RequestTokenAsync(object certificate, OpenIdClientInstance.UiLambdaSet uiLambda = null)
        {
            throw new PlatformNotSupportedException("OpenID remote repository authentication is not available in the Linux Blazor port.");
        }
    }
}

namespace AasxSignature
{
    public static class PackageHelper
    {
        public static Task<bool> SignAll(string packagePath, string certFn, string storeName = "My", AnyUiMinimalInvokeMessageDelegate invokeMessage = null)
        {
            invokeMessage?.Invoke(true, "AASX signing is not available in the Linux Blazor port.");
            return Task.FromResult(false);
        }

        public static bool Validate(string packagePath, AnyUiMinimalInvokeMessageDelegate invokeMessage = null)
        {
            invokeMessage?.Invoke(false, "AASX signature validation is not available in the Linux Blazor port.");
            return true;
        }
    }
}
