using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using trail_mapper.ViewModels;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;
using Windows.Devices.Geolocation;

//running location tracking apps in the background
//http://msdn.microsoft.com/en-us/library/windowsphone/develop/jj662935%28v=vs.105%29.aspx

namespace trail_mapper
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.Breadcrumb = "loading data";
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
                App.ViewModel.State = RecordingState.New;
            }

            App.Breadcrumb = "seeing if we need to navigate to tracking page";
            if (App.ViewModel.State == RecordingState.RecordingStarted)
                NavigationService.Navigate(new Uri("/TrackTrailPage.xaml", UriKind.Relative));

            Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    UpdateButtons();
                });

            if (e.NavigationMode == NavigationMode.New)
            {
                App.Breadcrumb = "checking location is enabled";
                if (App.Geolocator.LocationStatus.Equals(PositionStatus.Disabled))
                    MessageBox.Show("Location services are disabled on your device, you will not be able to record new trails until you enable this in your device's settings", "Location disabled", MessageBoxButton.OK);
                //App.Geolocator = null;

                App.Breadcrumb = "checking location consent";
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
                    PromptIfWeCanUseUsersLocation();
            }
        }

        private void NewTrailButton_Click(object sender, RoutedEventArgs e)
        {
            App.Breadcrumb = "new trail button click";
            App.ViewModel.State = RecordingState.New; 
            NavigationService.Navigate(new Uri("/TrackTrailPage.xaml", UriKind.Relative));
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.Breadcrumb = "trail tapped";
            var trailMap = (TrailMap)((StackPanel)sender).Tag;
            //trailMap.CleanUpData();
            App.ViewModel.SelectedTrail = trailMap; 
            NavigationService.Navigate(new Uri("/TrailDisplayPage.xaml", UriKind.Relative));
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            App.Breadcrumb = "delete selected";
            var menuItem = (MenuItem)sender;
            var trailMap = (TrailMap)menuItem.Tag;
            if (MessageBox.Show("Are you sure you want to delete this map?", "Delete " + trailMap.Name, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            { 
                //delete from isolated storage
                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                    iso.DeleteFile(trailMap.Id.ToString() + ".json");
                //remove from view model
                App.ViewModel.RemoveTrailMap(trailMap); 
            }
        }

        private void ShareData_Click(object sender, EventArgs e)
        {
            App.Breadcrumb = "share data selected";
            var item = (MenuItem)sender;
            var map = (TrailMap)item.Tag;
            var emailTask = new EmailComposeTask();
            emailTask.Body = JsonConvert.SerializeObject(map);
            emailTask.Show();
        }

        public void TwitterButton_Click(object sender, EventArgs e)
        {
            var task = new WebBrowserTask
            {
                Uri = new Uri("https://twitter.com/AlexJohnMartin", UriKind.Absolute)
            };
            task.Show();
        }

        public void StoreButton_Click(object sender, EventArgs e)
        {
            var currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var task = new WebBrowserTask
            {
                Uri = new Uri(string.Format("http://www.windowsphone.com/{0}/store/publishers?publisherId=nocturnal%2Btendencies&appId=63cb6767-4940-4fa1-be8c-a7f58e455c3b", currentCulture.Name), UriKind.Absolute)
            };
            task.Show();
        }

        public void ReviewButton_Click(object sender, EventArgs e)
        {
            //FeedbackHelper.Default.Reviewed();
            var marketplace = new MarketplaceReviewTask();
            marketplace.Show();
        }

        public void EmailButton_Click(object sender, EventArgs e)
        {
            var email = new EmailComposeTask();
            email.Subject = "Feedback for the Calendar Tile application";
            email.Show();
        }

        private void AllowLocationCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_updating)
            {
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    App.Breadcrumb = "allow location changed";
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
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
                    UpdateButtons();
                });
            }
        }

        private void PromptIfWeCanUseUsersLocation()
        {
            App.Breadcrumb = "prompting if we can use user's location";
            //If they didn't we ask for it
            MessageBoxResult result = MessageBox.Show(trail_mapper.Resources.AppResources.LocationPrivacyPolicyBody, trail_mapper.Resources.AppResources.LocationPrivacyPolicyTitle, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
                IsolatedStorageSettings.ApplicationSettings.Add("LocationConsent", true);
            else
                IsolatedStorageSettings.ApplicationSettings.Add("LocationConsent", false);

            IsolatedStorageSettings.ApplicationSettings.Save();

            UpdateButtons();
        }

        private bool _updating = true;
        private void UpdateButtons()
        {
            App.Breadcrumb = "updating buttons on main page";
            _updating = true;
            var button = "new trail button";
            try
            {
                var locationEnabled = IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent") &&
                        bool.Parse(IsolatedStorageSettings.ApplicationSettings["LocationConsent"].ToString()) &&
                        !App.Geolocator.LocationStatus.Equals(PositionStatus.NotAvailable) &&
                        !App.Geolocator.LocationStatus.Equals(PositionStatus.Disabled) &&
                        !App.ViewModel.State.Equals(RecordingState.RecordingStarted);

                NewTrailButton.IsEnabled = locationEnabled;
                AllowLocationCheckbox.IsChecked = IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent") && (bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"];

                button = "movement threshold list";
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("MovementThreshold")) IsolatedStorageSettings.ApplicationSettings.Add("MovementThreshold", App.DefaultMovementThreshold);
                var threshold = int.Parse(IsolatedStorageSettings.ApplicationSettings["MovementThreshold"].ToString());
                var item = MovementThresholdList.Items.FirstOrDefault(i => int.Parse(((ListPickerItem)i).Tag.ToString()) == threshold);
                var index = MovementThresholdList.Items.IndexOf(item);
                MovementThresholdList.SelectedIndex = index;

                button = "auto-stop list";
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("AutoStopAfterMins")) IsolatedStorageSettings.ApplicationSettings.Add("AutoStopAfterMins", App.DefaultAutoStopInMins);
                var autoStopMins = int.Parse(IsolatedStorageSettings.ApplicationSettings["AutoStopAfterMins"].ToString());
                item = AutoStopMaxTimeList.Items.FirstOrDefault(i => int.Parse(((ListPickerItem)i).Tag.ToString()) == autoStopMins);
                index = AutoStopMaxTimeList.Items.IndexOf(item);
                AutoStopMaxTimeList.SelectedIndex = index;

                button = "accuracy list";
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("TrackingAccuracy")) IsolatedStorageSettings.ApplicationSettings.Add("TrackingAccuracy", App.DefaultAccuracy);
                var accuracy = IsolatedStorageSettings.ApplicationSettings["TrackingAccuracy"].ToString();
                item = AccuracyList.Items.FirstOrDefault(i => ((ListPickerItem)i).Tag.ToString() == accuracy);
                index = AccuracyList.Items.IndexOf(item);
                AccuracyList.SelectedIndex = index;

                IsolatedStorageSettings.ApplicationSettings.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error updating " + button, MessageBoxButton.OK);
            }
            //App.Geolocator = null;            
            _updating = false;
        }

        private void AutoStopMaxTimeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updating)
            {
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    App.Breadcrumb = "auto-stop changed";
                    var item = (ListPickerItem)((ListPicker)sender).SelectedItem;
                    if (item == null) return;
                    IsolatedStorageSettings.ApplicationSettings["AutoStopAfterMins"] = int.Parse(item.Tag.ToString());
                    IsolatedStorageSettings.ApplicationSettings.Save();
                });
            }
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
                    App.Geolocator.MovementThreshold = accuracy;
                    IsolatedStorageSettings.ApplicationSettings["MovementThreshold"] = accuracy;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                });
            }
        }

        private void AccuracyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_updating)
            {
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    App.Breadcrumb = "accuracy changed";
                    var item = (ListPickerItem)((ListPicker)sender).SelectedItem;
                    if (item == null) return;
                    var accuracy = item.Tag.ToString();
                    App.Geolocator.DesiredAccuracy = accuracy == "High" ? PositionAccuracy.High : PositionAccuracy.Default;
                    IsolatedStorageSettings.ApplicationSettings["TrackingAccuracy"] = accuracy;
                    IsolatedStorageSettings.ApplicationSettings.Save();
                });
            }
        }
    }
}