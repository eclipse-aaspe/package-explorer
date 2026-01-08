using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using UraniumUI;
using CommunityToolkit.Maui;
using AasxPackageLogic;

namespace MauiTestTree
{
    public static class MauiProgram
    {
        public static void Test()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var resources = assembly.GetManifestResourceNames();
                foreach (var res in resources)
                {
                    Trace.WriteLine("RES:" + res);
                    if (res.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Found font resource: {res} in {assembly.FullName}");
                    }
                }
            }

        }

        public static MauiApp CreateMauiApp()
        {
            Test();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUraniumUI()
                .UseUraniumUIMaterial() // important!
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSansRegular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSansSemibold.ttf", "OpenSansSemibold");
                    fonts.AddFontAwesomeIconFonts();
                    fonts.AddMaterialSymbolsFonts();
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // catch global exceptions
            SetupExceptionHandling();

            return builder.Build();
        }

        //
        // utility code
        //

        // see: https://stackoverflow.com/questions/793100/globally-catch-exceptions-in-a-wpf-application
        // modified with ChatGPT. In a nutshell: will not catch all, e.g. UI excpetions. Need to be handled
        // more locally.

        private static void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    LogUnhandledException(ex, "AppDomain");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler");
                e.SetObserved();
            };

#if WINDOWS
            Microsoft.UI.Xaml.Application.Current.UnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "WinUI");
                e.Handled = true;
            };
#endif
        }

        private static void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName =
                    System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                AasxPackageLogic.Log.Singleton.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                Log.Singleton.Error(exception, message);
            }
        }
    }
}
