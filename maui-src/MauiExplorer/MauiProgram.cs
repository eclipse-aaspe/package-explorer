using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using UraniumUI;
using CommunityToolkit.Maui;
using AasxPackageLogic;
using Microsoft.Maui.Handlers;

#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
#endif

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
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(true);
                })
                .UseUraniumUI()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSansRegular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSansSemibold.ttf", "OpenSansSemibold");
                    // for using glyph names in MAUI
                    fonts.AddMaterialSymbolsFonts(); 
                    // for using glyphs from WinUI, font should be extra contained in Resources/Fonts!
                    // fonts.AddFont("material-symbols-outlined-latin-400-normal.ttf", "MaterialOutlined");
                });

#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(wndLifeCycleBuilder =>
                {
                    wndLifeCycleBuilder.OnWindowCreated(window =>
                    {
                        // 1. Get the native window handle
                        var nativeWindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        var winuiAppWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(win32WindowsId);
                        // 2. Subscribe to the Closing event
                        // NOTE: This fires when the user clicks the "X" button
                        winuiAppWindow.Closing += async (s, args) =>
                        {
                            // cancel normal behaviour, first
                            args.Cancel = true;

                            if (App.Current?.Windows[0]?.Page is AppShell aps
                                && aps.CurrentPage is MainPage mp)
                            {
                                var doClose = await mp.IsWindowClosingAllowed(askForCloseCancel: true);

                                if (doClose)
                                    App.Current.Quit();
                            }
                        };
                    });
                });
            });

            ScrollViewHandler.Mapper.AppendToMapping("DisableFocusScrolling", (handler, view) =>
            {
                if (handler.PlatformView is ScrollViewer sv)
                {
                    // Prevent focus-triggered scrolling
                    sv.BringIntoViewOnFocusChange = false;

                    // Prevent automatic anchor repositioning when layout changes
                    sv.VerticalAnchorRatio = 0.0;
                }
            });
#endif

            // no underline for Entry field. Global, as it is everywhere meaningful
            EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                if (handler.PlatformView != null)
                {
                    // Remove underline completely
                    handler.PlatformView.Background = null;

                    // Optional: remove default padding added by Android
                    handler.PlatformView.SetPadding(0, 0, 0, 0);
                }
#endif
            });

            // no underline for Picker field. Global, as it is everywhere meaningful
            PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
            {
#if ANDROID
                if (handler.PlatformView != null)
                {
                    // Remove underline completely
                    handler.PlatformView.Background = null;

                    // Optional: remove default padding added by Android
                    handler.PlatformView.SetPadding(0, 0, 0, 0);
                }
#endif
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }


    }
}
