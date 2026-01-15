namespace MauiTestTree
{
    public partial class App : Application
    {
        protected Window? _window = null;

        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // return new Window(new AppShell());

            // make a loading page first ..
            _window = new Window(new LoadingPage());


#if WINDOWS
            _window.Destroying += Window_Destroying;
#else
#endif

            return _window;
        }

        private async void Window_Destroying(object? sender, EventArgs e)
        {
            // on other platforms, at least we could save/ close the file?
            if (App.Current?.Windows[0]?.Page is AppShell aps
                                && aps.CurrentPage is MainPage mp)
            {
                _ = await mp.IsWindowClosingAllowed(askForCloseCancel: false);
            }
        }

#if WINDOWS
        private async void OnNativeWindowClosed(object sender, Microsoft.UI.Xaml.WindowEventArgs e)
        {
            await Task.Yield();
        }
#endif
    }
}