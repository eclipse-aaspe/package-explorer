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

    }
}