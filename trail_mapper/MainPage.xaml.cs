﻿using System;
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
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }

            if (App.ViewModel.State == RecordingState.RecordingStarted)
                NavigationService.Navigate(new Uri("/TrackTrailPage.xaml", UriKind.Relative));

            if (e.NavigationMode == NavigationMode.New)
                if (!IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
                    PromptIfWeCanUseUsersLocation();

            var locationEnabled = (bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"];
            NewTrailButton.IsEnabled = locationEnabled;
            AllowLocationCheckbox.IsChecked = locationEnabled;
        }

        private void NewTrailButton_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.State = RecordingState.New; 
            NavigationService.Navigate(new Uri("/TrackTrailPage.xaml", UriKind.Relative));
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var trailMap = (TrailMap)((StackPanel)sender).Tag;
            //trailMap.CleanUpData();
            App.ViewModel.SelectedTrail = trailMap; 
            NavigationService.Navigate(new Uri("/TrailDisplayPage.xaml", UriKind.Relative));
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
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
            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
            NewTrailButton.IsEnabled = true;
        }

        private void AllowLocationCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
            NewTrailButton.IsEnabled = false;
        }

        private void PromptIfWeCanUseUsersLocation()
        {
            //If they didn't we ask for it
            MessageBoxResult result = MessageBox.Show(trail_mapper.Resources.AppResources.LocationPrivacyPolicyBody, trail_mapper.Resources.AppResources.LocationPrivacyPolicyTitle, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
            }

            IsolatedStorageSettings.ApplicationSettings.Save();
        }
    }
}