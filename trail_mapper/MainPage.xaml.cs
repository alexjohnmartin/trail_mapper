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

//tilt effect on controls
//http://developer.nokia.com/community/wiki/Tilt_Effect_for_Windows_Phone

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

        private bool _feedbackOverlayBound = false;
        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            var NewTrailButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.png", UriKind.Relative));
            NewTrailButton.IsEnabled = IsLocationEnabled();
            NewTrailButton.Text = "new trail"; //trail_mapper.Resources.AppResources.AppBarDeleteButtonText;
            NewTrailButton.Click += NewTrailButton_Click;
            ApplicationBar.Buttons.Add(NewTrailButton);

            var SettingsButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.settings.png", UriKind.Relative));
            SettingsButton.Text = "settings"; //trail_mapper.Resources.AppResources.AppBarDeleteButtonText;
            SettingsButton.Click += Settings_Click;
            ApplicationBar.Buttons.Add(SettingsButton);

            var InfoButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.information.png", UriKind.Relative));
            InfoButton.Text = "about"; //trail_mapper.Resources.AppResources.AppBarDeleteButtonText;
            InfoButton.Click += Info_Click;
            ApplicationBar.Buttons.Add(InfoButton);

            if (!_feedbackOverlayBound)
            {
                FeedbackOverlay.VisibilityChanged += FeedbackOverlay_VisibilityChanged;
                _feedbackOverlayBound = true;
            }
        }

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

            if (e.NavigationMode == NavigationMode.New)
            {
                App.Breadcrumb = "checking location is enabled";
                var geolocator = new Geolocator();
                if (geolocator.LocationStatus.Equals(PositionStatus.Disabled))
                    MessageBox.Show("Location services are disabled on your device, you will not be able to record new trails until you enable this in your device's settings", "Location disabled", MessageBoxButton.OK);
            
                App.Breadcrumb = "checking location consent";
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
                    PromptIfWeCanUseUsersLocation();
            }

            Dispatcher.BeginInvoke(() => BuildApplicationBar());
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void Info_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

        private void NewTrailButton_Click(object sender, EventArgs e)
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

        private readonly UploadHelper _uploadHelper = new UploadHelper();
        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            App.Breadcrumb = "upload selected";
            var menuItem = (MenuItem)sender;
            var trailMap = (TrailMap)menuItem.Tag;
            _uploadHelper.UploadTrailMap(trailMap);
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
        
        private void FeedbackOverlay_VisibilityChanged(object sender, EventArgs e)
        {
            if (ApplicationBar != null)
            {
                ApplicationBar.IsVisible = (FeedbackOverlay.Visibility != Visibility.Visible);
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

            BuildApplicationBar();
        }

        private bool IsLocationEnabled()
        {
            App.Breadcrumb = "updating buttons on main page";
            try
            {
                var geolocator = new Geolocator();
                return IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent") &&
                       bool.Parse(IsolatedStorageSettings.ApplicationSettings["LocationConsent"].ToString()) &&
                       !geolocator.LocationStatus.Equals(PositionStatus.NotAvailable) &&
                       !geolocator.LocationStatus.Equals(PositionStatus.Disabled) &&
                       !App.ViewModel.State.Equals(RecordingState.RecordingStarted);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error getting whether location is enabled", MessageBoxButton.OK);
                return false;
            }
        }

        //private void AutoStopMaxTimeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (!_updating)
        //    {
        //        Deployment.Current.Dispatcher.BeginInvoke(delegate
        //        {
        //            App.Breadcrumb = "auto-stop changed";
        //            var item = (ListPickerItem)((ListPicker)sender).SelectedItem;
        //            if (item == null) return;
        //            IsolatedStorageSettings.ApplicationSettings["AutoStopAfterMins"] = int.Parse(item.Tag.ToString());
        //            IsolatedStorageSettings.ApplicationSettings.Save();
        //        });
        //    }
        //}

        //private void MovementThresholdList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (!_updating)
        //    {
        //        Deployment.Current.Dispatcher.BeginInvoke(delegate
        //        {
        //            App.Breadcrumb = "threshold changed";
        //            var item = (ListPickerItem)((ListPicker)sender).SelectedItem;
        //            if (item == null) return;
        //            var accuracy = double.Parse(item.Tag.ToString());
        //            App.Geolocator.MovementThreshold = accuracy;
        //            IsolatedStorageSettings.ApplicationSettings["MovementThreshold"] = accuracy;
        //            IsolatedStorageSettings.ApplicationSettings.Save();
        //        });
        //    }
        //}

        //private void AccuracyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (!_updating)
        //    {
        //        Deployment.Current.Dispatcher.BeginInvoke(delegate
        //        {
        //            App.Breadcrumb = "accuracy changed";
        //            var item = (ListPickerItem)((ListPicker)sender).SelectedItem;
        //            if (item == null) return;
        //            var accuracy = item.Tag.ToString();
        //            App.Geolocator.DesiredAccuracy = accuracy == "High" ? PositionAccuracy.High : PositionAccuracy.Default;
        //            IsolatedStorageSettings.ApplicationSettings["TrackingAccuracy"] = accuracy;
        //            IsolatedStorageSettings.ApplicationSettings.Save();
        //        });
        //    }
        //}
    }
}