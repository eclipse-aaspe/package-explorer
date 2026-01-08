namespace MauiTestTree
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // return new Window(new AppShell());

            // make a loading page first ..
            return new Window(new LoadingPage());
        }
    }
}