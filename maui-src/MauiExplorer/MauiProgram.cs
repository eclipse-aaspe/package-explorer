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

            return builder.Build();
        }


    }
}
