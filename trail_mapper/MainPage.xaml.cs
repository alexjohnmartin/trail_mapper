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
    }
}