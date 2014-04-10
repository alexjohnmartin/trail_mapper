using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using System.IO;
using trail_mapper.ViewModels;

namespace trail_mapper
{
    public partial class TrackTrailPage : PhoneApplicationPage
    {
        public TrackTrailPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (App.Geolocator == null)
            {
                App.Geolocator = new Geolocator();
                App.Geolocator.DesiredAccuracy = PositionAccuracy.High;
                App.Geolocator.MovementThreshold = 10; // The units are meters.
            }
            UpdateButtons(); 
        }

        private void UpdateButtons()
        {
            StartTrackingButton.IsEnabled = App.ViewModel.State == RecordingState.New;
            StopTrackingButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingStarted;
            SaveButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingFinished;
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            if (App.Geolocator != null)
            {
                App.Geolocator.PositionChanged -= geolocator_PositionChanged;
                App.Geolocator = null;
            }
        }

        private void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (!App.RunningInBackground)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    LatitudeTextBlock.Text = args.Position.Coordinate.Latitude.ToString();
                    LongitudeTextBlock.Text = args.Position.Coordinate.Longitude.ToString();
                });
            }
            else
            {
                //Microsoft.Phone.Shell.ShellToast toast = new Microsoft.Phone.Shell.ShellToast();
                //toast.Content = args.Position.Coordinate.Latitude.ToString("0.00");
                //toast.Title = "Location: ";
                //toast.NavigationUri = new Uri("/Page2.xaml", UriKind.Relative);
                //toast.Show();
            }

            App.ViewModel.TrailHistory.Add(new HistoryItem { Time = DateTime.Now, Altitude = args.Position.Coordinate.Altitude.Value, Latitude = args.Position.Coordinate.Latitude, Longitude = args.Position.Coordinate.Longitude }); 
        }

        private async void StartTrackingButton_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.State = RecordingState.RecordingStarted;
            UpdateButtons();

            App.ViewModel.TrailHistory = new List<HistoryItem>();
            App.Geolocator.PositionChanged += geolocator_PositionChanged;

            var locator = new Geolocator();
            Geoposition position = await locator.GetGeopositionAsync();
            LatitudeTextBlock.Text = position.Coordinate.Latitude.ToString();
            LongitudeTextBlock.Text = position.Coordinate.Longitude.ToString();
            App.ViewModel.TrailHistory.Add(new HistoryItem { Time = DateTime.Now, Altitude = position.Coordinate.Altitude.Value, Latitude = position.Coordinate.Latitude, Longitude = position.Coordinate.Longitude }); 
        }

        private void StopTrackingButton_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.State = RecordingState.RecordingFinished;
            UpdateButtons();

            App.Geolocator.PositionChanged -= geolocator_PositionChanged;
            App.Geolocator = null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var trailName = string.IsNullOrEmpty(TrailNameTextbox.Text) ? "(no name)" : TrailNameTextbox.Text;
            var trailMap = new TrailMap { Name = trailName, History = App.ViewModel.TrailHistory };
            var json = JsonConvert.SerializeObject(trailMap);

            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = iso.CreateFile(trailMap.Id.ToString() + ".json"))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine(json);
                        writer.Flush();
                        writer.Close();
                    }
                }
            }

            App.ViewModel.AddTrailHistory(trailMap);
            App.ViewModel.State = RecordingState.Saved;
            NavigationService.GoBack();
        }
    }
}