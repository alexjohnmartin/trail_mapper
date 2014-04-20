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
using System.Windows.Media;
using System.Threading;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;

//predictive text box
//http://developer.nokia.com/community/wiki/Enabling_predictive_text_in_a_TextBox_in_Windows_Phone

namespace trail_mapper
{
    public partial class TrackTrailPage : PhoneApplicationPage
    {
        private string _newTrailText = "New trail";
        //private bool _hasAnyDataChanged = false;
        //private Timer _timer; 

        public TrackTrailPage()
        {
            InitializeComponent();
            App.Geolocator.PositionChanged += geolocator_PositionChanged;
            _newTrailText = trail_mapper.Resources.AppResources.NewTrailTextboxWatermark; 
        }

        //private void Timer_Tick(object state)
        //{
        //    if (App.RunningInBackground)
        //    {
        //        _timer = null;
        //        return;
        //    }

        //    if (!_hasAnyDataChanged) return;
        //    if (App.ViewModel.TrailHistory.Count < 1) return;

        //    //LatitudeTextBlock.Text = App.ViewModel.TrailHistory.Last().Latitude.ToString();
        //    //LongitudeTextBlock.Text = App.ViewModel.TrailHistory.Last().Longitude.ToString();

        //    var gc = new GeoCoordinateCollection();
        //    foreach (var item in App.ViewModel.TrailHistory)
        //        gc.Add(new GeoCoordinate(item.Latitude, item.Longitude, item.Altitude));

        //    var dynamicPolyline = new MapPolyline();
        //    dynamicPolyline.StrokeDashed = false;
        //    dynamicPolyline.StrokeThickness = 5;
        //    dynamicPolyline.StrokeColor = Colors.Red;
        //    dynamicPolyline.Path = gc;

        //    HereMap.MapElements.Add(dynamicPolyline);

        //    var array = App.ViewModel.TrailHistory.Select(i => new GeoCoordinate(i.Latitude, i.Longitude)).ToArray();
        //    var boundingRectangle = LocationRectangle.CreateBoundingRectangle(array);
        //    HereMap.SetView(boundingRectangle);

        //    _hasAnyDataChanged = false;
        //}

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            //_timer = new Timer(Timer_Tick, null, 100, 5000);
            UpdateButtons(); 
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
            if (args.Position.Coordinate.PositionSource == PositionSource.Satellite)
            {
                if (!App.RunningInBackground)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        //update display
                        LatitudeTextBlock.Text = args.Position.Coordinate.Latitude.ToString();
                        LongitudeTextBlock.Text = args.Position.Coordinate.Longitude.ToString();
                    });
                }

                if (App.ViewModel.State == RecordingState.RecordingStarted)
                    App.ViewModel.TrailHistory.Add(new HistoryItem { Time = DateTime.Now, Altitude = args.Position.Coordinate.Altitude.Value, Latitude = args.Position.Coordinate.Latitude, Longitude = args.Position.Coordinate.Longitude });

                //_hasAnyDataChanged = true;
            }
        }

        private async void StartTrackingButton_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.State = RecordingState.RecordingStarted;
            UpdateButtons();

            App.ViewModel.TrailHistory = new List<HistoryItem>();
            //App.Geolocator.PositionChanged += geolocator_PositionChanged;

            var locator = new Geolocator();
            Geoposition position = await locator.GetGeopositionAsync();
            if (position.Coordinate.PositionSource == PositionSource.Satellite)
            {
                LatitudeTextBlock.Text = position.Coordinate.Latitude.ToString();
                LongitudeTextBlock.Text = position.Coordinate.Longitude.ToString();
                App.ViewModel.TrailHistory.Add(new HistoryItem { Time = DateTime.Now, Altitude = position.Coordinate.Altitude.Value, Latitude = position.Coordinate.Latitude, Longitude = position.Coordinate.Longitude });
                //_hasAnyDataChanged = true;
            }
            //HereMap.Center = new GeoCoordinate(position.Coordinate.Latitude, position.Coordinate.Longitude);
        }

        private void StopTrackingButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.TrailHistory.Count > 0)
                App.ViewModel.State = RecordingState.RecordingFinished;
            else
                App.ViewModel.State = RecordingState.New;

            UpdateButtons();

            //App.Geolocator.PositionChanged -= geolocator_PositionChanged;
            //App.Geolocator = null;
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
            App.ViewModel.SelectedTrail = trailMap;
            NavigationService.Navigate(new Uri("/TrailDisplayPage.xaml", UriKind.Relative));
        }

        private void WatermarkTB_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TrailNameTextbox.Text == _newTrailText)
            {
                TrailNameTextbox.Text = "";
                SolidColorBrush Brush1 = new SolidColorBrush();
                Brush1.Color = Colors.Magenta;
                TrailNameTextbox.Foreground = Brush1;
            }
        }

        private void WatermarkTB_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TrailNameTextbox.Text == String.Empty)
            {
                TrailNameTextbox.Text = _newTrailText;
                SolidColorBrush Brush2 = new SolidColorBrush();
                Brush2.Color = Colors.Blue;
                TrailNameTextbox.Foreground = Brush2;
            }
        }

        private void UpdateButtons()
        {
            StartTrackingButton.IsEnabled = App.ViewModel.State == RecordingState.New;
            SaveButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingFinished;
            DiscardButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingFinished;
            TrailNameTextbox.IsEnabled = App.ViewModel.State == RecordingState.RecordingFinished;
            StopTrackingButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingStarted;
            if (App.ViewModel.State == RecordingState.New) TrailNameTextbox.Text = _newTrailText;
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to discard this recording? Nothing will be saved, this cannot be undone.", "Discard recording", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                App.ViewModel.TrailHistory = new List<HistoryItem>();
                App.ViewModel.State = RecordingState.New;
                UpdateButtons();
            }
        }
    }
}