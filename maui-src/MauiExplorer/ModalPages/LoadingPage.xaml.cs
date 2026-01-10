using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AasxPackageLogic;

namespace MauiTestTree;

/// <summary>
/// The loading page shows first some logo / information about autors and
/// licenses, which are necessary anyway for legalr reasons.
/// After a short period of time, it starts the rest of the application up
/// (that lengthy class loading of PackageLogic and DataStore can occur when
/// visuals are already present).
/// It then substitutes itself with the 'real' main page.
/// </summary>
public partial class LoadingPage : ContentPage
{
    protected LoadingPageViewModel _viewModel = new();

    //
    // Start of component
    //

	public LoadingPage(LoadingPageViewModel? preset = null)
	{
		InitializeComponent();
        if (preset != null)
            _viewModel = preset;
        BindingContext = _viewModel;
        StartTimer();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), async () =>
        {
            // load
            await AppStartup();

            // then: reach necessary state to close
            _timerState = 1;
        });
    }

    protected void DismissPage()
    {
        StopTimer();
        Application.Current.MainPage = new AppShell();
    }

    private void CancelButton_Clicked(object sender, EventArgs e)
    {
        if (_timerState >= 1)
            DismissPage();
    }

    //
    // Timer + Events to the outside
    //

    protected DateTime _timerStartTime;
    protected bool _timerRunning = false;
    protected int _timerState = 0;

    protected void StartTimer()
    {
        _timerStartTime = DateTime.UtcNow;
        _timerRunning = true;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(100), () =>
        {
            // dismiss
            if (_timerState == 1 
                && (DateTime.UtcNow - _timerStartTime).TotalMilliseconds >= 1000.0 * _viewModel.SplashTimeSecs)
                DismissPage();

            // else, keep running
            return _timerRunning; // true = keep running, false = stop
        });
    }

    protected void StopTimer()
    {
        _timerRunning = false;
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

    /// <summary>
    /// Infers application options based on the command-line arguments.
    /// </summary>
    /// <param name="exePath">path to AasxPackageExplorer.exe</param>
    /// <param name="args">command-line arguments</param>
    /// <returns>inferred options</returns>
    protected async Task<OptionsInformation> InferOptions(string exePath, string[] args)
    {
        var optionsInformation = new OptionsInformation();

        // Load the default command-line options from a file with a conventional file name
        // in MAUI: from the APP data directory
        var pathToDefaultOptions = System.IO.Path.Combine(
            FileSystem.AppDataDirectory,
            System.IO.Path.GetFileNameWithoutExtension(exePath) + ".options.json");

        // create because not existing?
        if (!File.Exists(pathToDefaultOptions))
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("options.default.json");
            using var reader = new StreamReader(stream);
            File.WriteAllText(pathToDefaultOptions, reader.ReadToEnd());
        }

        Log.Singleton.Info(
            "The default options are expected in the JSON file: {0}", pathToDefaultOptions);
        if (File.Exists(pathToDefaultOptions))
        {
            Log.Singleton.Info(
                "Loading the default options from: {0}", pathToDefaultOptions);
            OptionsInformation.ReadJson(pathToDefaultOptions, optionsInformation);
        }
        else
        {
            Log.Singleton.Info(
                "The JSON file with the default options does not exist;" +
                "no default options were loaded: {0}", pathToDefaultOptions);
        }

        // Cover the special case for having a single positional command-line option

        if (args.Length == 1 && !args[0].StartsWith("-"))
        {
            string directAasx = args[0];
            Log.Singleton.Info("Direct request to load AASX {0} ..", directAasx);
            optionsInformation.AasxToLoad = directAasx;
        }

        // Parse options from the command-line and execute the directives on the fly (such as parsing and
        // overruling given in the additional option files, *e.g.*, through "-read-json" and "-options")

        Log.Singleton.Info($"Parsing {args.Length} command-line option(s)...");

        for (var i = 0; i < args.Length; i++)
            Log.Singleton.Info($"Command-line option: {i}: {args[i]}");

        OptionsInformation.ParseArgs(args, optionsInformation);

        return optionsInformation;
    }

    //
    // Start up
    //

    protected async Task AppStartup()
    {
        // dead-csharp off
        // MIHO: This does not work
        // WinPInvokeHelpers.SetProcessDPIAware(WinPInvokeHelpers.PROCESS_DPI_AWARENESS.Process_DPI_Unaware);
        // dead-csharp on
        // allow long term logging (for report box)
        Log.Singleton.EnableLongTermStore();

        // catch unhandled exceptions
        SetupExceptionHandling();

        // Build up of options
        Log.Singleton.Info("Application startup.");
        var exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
        Options.ReplaceCurr(await InferOptions(exePath!, new string[] { }));

#if cdscds
            // commit some options to other global locations
            AdminShellUtil.DefaultLngIso639 = AasxLanguageHelper.GetFirstLangCode(Options.Curr.DefaultLangs) ?? "en?";

            // search for plugins?
            if (Options.Curr.PluginDir != null)
            {
                var searchDir = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(exePath),
                    Options.Curr.PluginDir);

                Log.Singleton.Info(
                    "Searching for the plugins in the plugin directory: {0}", searchDir);

                var pluginDllInfos = Plugins.TrySearchPlugins(searchDir);

                Log.Singleton.Info(
                    $"Found {pluginDllInfos.Count} plugin(s) in the plugin directory: {searchDir}");

                Options.Curr.PluginDll.AddRange(pluginDllInfos);
            }

            Log.Singleton.Info(
                $"Loading and activating {Options.Curr.PluginDll.Count} plugin(s)...");

            Plugins.LoadedPlugins = LoadAndActivatePlugins(Options.Curr.PluginDll);

            // at end, write all default options to JSON?
            if (Options.Curr.WriteDefaultOptionsFN != null)
            {
                // info
                var fullFilename = System.IO.Path.GetFullPath(Options.Curr.WriteDefaultOptionsFN);
                Log.Singleton.Info($"Writing resulting options to a JSON file: {fullFilename}");

                // retrieve
                Plugins.TryGetDefaultOptionsForPlugins(Options.Curr.PluginDll, Plugins.LoadedPlugins);
                OptionsInformation.WriteJson(Options.Curr, fullFilename, withComments: true);
            }

            // colors
            if (true)
            {
                var resNames = new[] {
                    "LightAccentColor", "DarkAccentColor", "DarkestAccentColor", "FocusErrorBrush" };
                for (int i = 0; i < resNames.Length; i++)
                {
                    var x = this.FindResource(resNames[i]);
                    if (x != null
                        && x is System.Windows.Media.SolidColorBrush
                        && Options.Curr.AccentColors.ContainsKey((OptionsInformation.ColorNames)i))
                        this.Resources[resNames[i]] = AnyUiDisplayContextWpf.GetWpfBrush(
                            Options.Curr.GetColor((OptionsInformation.ColorNames)i));
                }
                resNames = new[] { "FocusErrorColor" };
                for (int i = 0; i < resNames.Length; i++)
                {
                    var x = this.FindResource(resNames[i]);
                    if (x != null
                        && x is System.Windows.Media.Color
                        && Options.Curr.AccentColors.ContainsKey((OptionsInformation.ColorNames)(3 + i)))
                        this.Resources[resNames[i]] = AnyUiDisplayContextWpf.GetWpfColor(
                            Options.Curr.GetColor((OptionsInformation.ColorNames)(3 + i)));
                }
            }

            // languages
            if (Options.Curr.OfferedLangs?.HasContent() == true)
                AasxIntegrationBase.AasxLanguageHelper.Languages.InitByCustomString(Options.Curr.OfferedLangs);

            // preferences
            Pref pref = Pref.Read();

            // show splash (required for licenses of open source)
            if (Options.Curr.SplashTime != 0)
            {
                var splash = new CustomSplashScreenNew(pref);
                splash.Show();
            }

            // show main window
            MainWindow wnd = new MainWindow(pref);
            wnd.Show();
#endif
    }

}

//
// View model
//

public class LoadingPageViewModel : INotifyPropertyChanged
{
    //
    // INotifyPropertyChanged
    // 

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // 
    // Members
    //

    public string Authors { get; set; } = "";
    public string Version { get; set; } = "";
    public string BuildDate { get; set; } = "";
    public string Licenses { get; set; } = "";

    /// <summary>
    /// Splash time in seconds
    /// </summary>
    public double SplashTimeSecs = 10.0;

    //
    // Constructor
    //

    public LoadingPageViewModel()
    {
        var pref = AasxSoftwareInfo.SoftwareInfoBase.Read();

        Authors = pref.Authors;
        Version = pref.GitDescribe;
        BuildDate = pref.BuildDate;
        Licenses = "[AASX Package Explorer]" + System.Environment.NewLine +
                   pref.LicenseShort + System.Environment.NewLine +
                   pref.LicenseLong;
    }
}
