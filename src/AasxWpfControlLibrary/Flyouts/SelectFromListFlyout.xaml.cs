/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{

    /// <summary>
    /// Creates a flyout in order to select items from a list
    /// </summary>
    public partial class SelectFromListFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataSelectFromList DiaData = new AnyUiDialogueDataSelectFromList();

        public SelectFromListFlyout()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill caption
            if (DiaData.Caption != null)
                TextBlockCaption.Text = "" + DiaData.Caption;

            // fill listbox
            ListBoxPresets.Items.Clear();
            foreach (var loi in DiaData.ListOfItems)
                ListBoxPresets.Items.Add("" + loi.Text);

            // alternative buttons
            if (DiaData.AlternativeSelectButtons != null)
            {
                this.ButtonsPanel.Children.Clear();
                foreach (var txt in DiaData.AlternativeSelectButtons)
                {
                    var b = new Button();
                    b.Content = "" + txt;
                    b.Foreground = Brushes.White;
                    b.FontSize = 18;
                    b.Padding = new Thickness(4);
                    b.Margin = new Thickness(4);
                    this.ButtonsPanel.Children.Add(b);
                }
            }

            // special case: select files
            if (DiaData.SelectFiles)
            {
                this.ButtonsPanel.Children.Clear();

                // b1 = Plus
                var b1 = new Button()
                {
                    Content = " + ",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(4),
                    Margin = new Thickness(4)
                };
                b1.SetResourceReference(Control.StyleProperty, "TranspRoundCorner");
                DockPanel.SetDock(b1, Dock.Left);
                this.ButtonsPanel.Children.Add(b1);

                // b2 = Minus
                var b2 = new Button()
                {
                    Content = " \u2212 ",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(4),
                    Margin = new Thickness(4)
                };
                b2.SetResourceReference(Control.StyleProperty, "TranspRoundCorner");
                DockPanel.SetDock(b2, Dock.Left);
                this.ButtonsPanel.Children.Add(b2);

                // b3 = OK
                var b3 = new Button()
                {
                    Content = "OK",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    Padding = new Thickness(4),
                    Margin = new Thickness(4)
                };
                b3.SetResourceReference(Control.StyleProperty, "TranspRoundCorner");
                b3.Click += ButtonSelect_Click;
                this.ButtonsPanel.Children.Add(b3);

                // add actions
                b1.Click += (s1, e1) =>
                {
                    var dlg = new Microsoft.Win32.OpenFileDialog();
                    dlg.CheckFileExists = true;
                    dlg.Multiselect = true;
                    if (dlg.ShowDialog() ?? false && dlg.FileNames != null)
                        foreach (var fn in dlg.FileNames)
                        {
                            var fi = new AnyUiDialogueListItem() { Text = fn, Tag = fn };
                            DiaData.ListOfItems.Add(fi);
                            ListBoxPresets.Items.Add("" + fi.Text);
                        }
                };

                b2.Click += (s2, e2) =>
                {
                    var i = ListBoxPresets.SelectedIndex;
                    if (i >= 0 && i < DiaData.ListOfItems.Count)
                    {
                        DiaData.ListOfItems.RemoveAt(i);
                        ListBoxPresets.Items.RemoveAt(i);
                    }
                };

                // allow drop
                ListBoxPresets.AllowDrop = true;
                ListBoxPresets.Drop += (s4, e4) =>
                {
                    if (e4.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e4.Data.GetData(DataFormats.FileDrop);
                        foreach (var fn in files)
                        {
                            var fi = new AnyUiDialogueListItem() { Text = fn, Tag = fn };
                            DiaData.ListOfItems.Add(fi);
                            ListBoxPresets.Items.Add("" + fi.Text);
                        }
                    }

                    e4.Handled = true;
                };
            }
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        //
        // Mechanics
        //


        private bool PrepareResult()
        {
            // special case: file list
            if (DiaData.SelectFiles)
            {
                DiaData.Result = true;
                return true;
            }

            // normal case
            var i = ListBoxPresets.SelectedIndex;
            if (DiaData.ListOfItems != null && i >= 0 && i < DiaData.ListOfItems.Count)
            {
                DiaData.ResultIndex = i;
                DiaData.ResultItem = DiaData.ListOfItems[i];
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            DiaData.ResultIndex = -1;
            DiaData.ResultItem = null;
            ControlClosed?.Invoke();
        }

        private void ListBoxPresets_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }
    }
}
