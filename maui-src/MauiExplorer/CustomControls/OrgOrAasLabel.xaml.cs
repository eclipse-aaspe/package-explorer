using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;

namespace MauiTestTree
{
    public partial class OrgOrAasLabel : ContentView
    {
        //
        // Constructor
        //

        public OrgOrAasLabel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Text to be displayed
        /// </summary>
        public string AasId
        {
            get => (string)GetValue(AasIdProperty);
            set => SetValue(AasIdProperty, value);
        }

        public static readonly BindableProperty AasIdProperty =
            BindableProperty.Create(
                nameof(AasId),
                typeof(string),
                typeof(AutoFitLabel),
                string.Empty,
                propertyChanged: OnAasIdChanged);

        private static void OnAasIdChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (OrgOrAasLabel)bindable;
            self.AasIdLabel.Text = ForceCharWrap((string)newValue);
            self.SetVisibilities();
        }

        /// <summary>
        /// Text to be displayed
        /// </summary>
        public string AssetId
        {
            get => (string)GetValue(AssetIdProperty);
            set => SetValue(AssetIdProperty, value);
        }

        public static readonly BindableProperty AssetIdProperty =
            BindableProperty.Create(
                nameof(AssetId),
                typeof(string),
                typeof(AutoFitLabel),
                string.Empty,
                propertyChanged: OnAssetIdChanged);

        private static void OnAssetIdChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var self = (OrgOrAasLabel)bindable;
            self.AssetIdLabel.Text = ForceCharWrap((string)newValue);
            self.SetVisibilities();
        }

        //
        // Other
        //

        protected static string ForceCharWrap(string input)
        {
            return string.Join("\u200B", input.ToCharArray());
        }

        protected void SetVisibilities()
        {
            if (AasId.Length > 0 || AssetId.Length > 0)
            {
                AasGrid.IsVisible = true;
                OrgGrid.IsVisible = false;
            }
            else
            {
                AasGrid.IsVisible = false;
                OrgGrid.IsVisible = true;
            }
        }

        //
        // Drop handling
        //

        // Forwarded drop event (containing only file pathes)
        public event EventHandler<IReadOnlyList<string>>? FileDropReceived;

        protected List<string> _draggedFilePathes = new();

        private async void OnDragOver(object sender, DragEventArgs e)
        {
            // see: https://www.youtube.com/watch?v=x6ku0V44GFc
            // https://github.com/YBTopaz8/DragAndDropMAUISample

            _draggedFilePathes = new();

#if WINDOWS
            var WindowsDragEventArgs = e.PlatformArgs?.DragEventArgs;

            if (WindowsDragEventArgs == null)
                return;

            var DraggedOverItems = await WindowsDragEventArgs.DataView.GetStorageItemsAsync();
            e.AcceptedOperation = DataPackageOperation.None;

            if (DraggedOverItems.Count > 0)
            {
                foreach (var item in DraggedOverItems)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        _draggedFilePathes.Add(file.Path);
                    }
                }
            }
#endif

            // Accept only files: not possible in .NET MAUI 9
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void OnDrop(object sender, DropEventArgs e)
        {
            await Task.Yield();

            FileDropReceived?.Invoke(this, _draggedFilePathes);
        }
    }
}