using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;

namespace trail_mapper
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        private bool _updating = true;

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
                Dispatcher.BeginInvoke(() => UpdateButtons());
        }

        private void MovementThresholdList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updating)
            {
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    App.Breadcrumb = "threshold changed";
                    var item = (ListPickerItem)((ListPicker)sender).SelectedItem;
                    if (item == null) return;
                    var accuracy = double.Parse(item.Tag.ToString());
                    //App.Geolocator.MovementThreshold = accuracy;
                    IsolatedStorageSettings.ApplicationSettings["MovementThreshold"] = accuracy;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                });
            }
        }

        private void AllowLocationCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_updating)
            {
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    App.Breadcrumb = "allow location changed";
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                    UpdateButtons();
                });
            }
        }

        private void AllowLocationCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_updating)
            {
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    App.Breadcrumb = "allow location changed";
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                    UpdateButtons();
                });
            }
        }

        private void UpdateButtons()
        {
            _updating = true;

            try
            {
                AllowLocationToggle.IsChecked = IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent") && (bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"];

                if (!IsolatedStorageSettings.ApplicationSettings.Contains("MovementThreshold")) IsolatedStorageSettings.ApplicationSettings.Add("MovementThreshold", App.DefaultMovementThreshold);
                var threshold = int.Parse(IsolatedStorageSettings.ApplicationSettings["MovementThreshold"].ToString());
                var item = MovementThresholdList.Items.FirstOrDefault(i => int.Parse(((ListPickerItem)i).Tag.ToString()) == threshold);
                var index = MovementThresholdList.Items.IndexOf(item);
                MovementThresholdList.SelectedIndex = index;

                IsolatedStorageSettings.ApplicationSettings.Save();
            }
            catch (Exception)
            {
                //TODO:log error
            }

            _updating = false;
        }
    }
}