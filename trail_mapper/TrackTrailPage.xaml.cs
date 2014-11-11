using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Devices.Geolocation;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using trail_mapper.ViewModels;
using System.Windows.Media;
using System.Threading;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;

//predictive text box
//http://developer.nokia.com/community/wiki/Enabling_predictive_text_in_a_TextBox_in_Windows_Phone

//POST to a web-service
//http://stackoverflow.com/questions/4166222/httpwebrequest-endgetresponse-throws-a-notsupportedexception-in-windows-phone-7

namespace trail_mapper
{
    public partial class TrackTrailPage : PhoneApplicationPage
    {
        private string _newTrailText = "New trail";
        //private int _autoStopAfterMins = 120;
        private Guid _trailId = Guid.Empty;
        private bool _autoSaved = false;
        private DateTime _recordingStartTime = DateTime.MinValue;
        private DateTime _lastSaveTime = DateTime.MinValue;
        private readonly TimeSpan _timeBetweenAutoSaves = TimeSpan.FromMinutes(5);
        //private Timer _timer; 
        private readonly UploadHelper _uploadHelper = new UploadHelper();
        private readonly SaveHelper _saveHelper = new SaveHelper();

        public TrackTrailPage()
        {
            InitializeComponent();
            _newTrailText = trail_mapper.Resources.AppResources.NewTrailTextboxWatermark;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (App.ViewModel.State == RecordingState.Unknown) CorrectTheAppState();
            Dispatcher.BeginInvoke(() => { UpdateButtons(); UpdateStatistics(); });
            //if (IsolatedStorageSettings.ApplicationSettings.Contains("AutoStopAfterMins"))
            //    _autoStopAfterMins = (int)IsolatedStorageSettings.ApplicationSettings["AutoStopAfterMins"];
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            if (App.Geolocator != null)
            {
                App.Geolocator.PositionChanged -= geolocator_PositionChanged;
                //App.Geolocator = null;
            }
        }

        private void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            if (App.ViewModel.State == RecordingState.RecordingStarted)
            {
                if (args.Position.Coordinate.PositionSource == PositionSource.Satellite)
                {
                    if (App.ViewModel.State == RecordingState.RecordingStarted)
                        App.ViewModel.TrailHistory.Add(new HistoryItem { Time = DateTime.UtcNow, Altitude = args.Position.Coordinate.Altitude.Value, Latitude = args.Position.Coordinate.Latitude, Longitude = args.Position.Coordinate.Longitude, Speed = args.Position.Coordinate.Speed.Value });

                    if (!App.RunningInBackground)
                    {
                        Dispatcher.BeginInvoke(() => { UpdateButtons(); UpdateStatistics(); });
                    }

                    if (DateTime.UtcNow > _lastSaveTime.Add(_timeBetweenAutoSaves))
                    {
                        _autoSaved = true;
                        _lastSaveTime = DateTime.UtcNow;
                        var trailMap = new TrailMap { Name = "AutoSave-" + _recordingStartTime.ToString("yyyyMMdd-hhmmss"), History = App.ViewModel.TrailHistory, Id = _trailId };
                        Dispatcher.BeginInvoke(() => _saveHelper.SaveTrail(trailMap));
                    }
                }

                //if (DateTime.UtcNow > _recordingStartTime.AddMinutes(_autoStopAfterMins))
                //    StopTracking();
            }
        }

        private async void StartTrackingButton_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.TrailHistory = new List<HistoryItem>();
            App.ViewModel.State = RecordingState.RecordingStarted;
            UpdateButtons(); 
            UpdateStatistics();

            _autoSaved = false;
            _trailId = Guid.NewGuid();
            _lastSaveTime = DateTime.UtcNow;
            _recordingStartTime = DateTime.UtcNow;
            //_timer = new Timer(Timer_tick, null, 100, 1000);
            App.Geolocator.PositionChanged += geolocator_PositionChanged;

            Geoposition position = await App.Geolocator.GetGeopositionAsync();
            if (position.Coordinate.PositionSource == PositionSource.Satellite)
            {
                App.ViewModel.TrailHistory.Add(new HistoryItem { Time = DateTime.UtcNow, Altitude = position.Coordinate.Altitude.Value, Latitude = position.Coordinate.Latitude, Longitude = position.Coordinate.Longitude, Speed = position.Coordinate.Speed.Value });
                Dispatcher.BeginInvoke(() => { UpdateButtons(); UpdateStatistics(); });
            }
        }

        private void StopTrackingButton_Click(object sender, RoutedEventArgs e)
        {
            StopTracking();
            Dispatcher.BeginInvoke(() => { UpdateButtons(); UpdateStatistics(); });
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var trailName = string.IsNullOrEmpty(TrailNameTextbox.Text) ? "(no name)" : TrailNameTextbox.Text;
            var trailMap = new TrailMap { Name = trailName, History = App.ViewModel.TrailHistory, Id = _trailId };

            App.ViewModel.AddTrailHistory(trailMap);
            App.ViewModel.State = RecordingState.New;
            App.ViewModel.SelectedTrail = trailMap;

            Dispatcher.BeginInvoke(() => { _saveHelper.SaveTrail(trailMap); _saveHelper.CleanUpAutoSave(_autoSaved, _recordingStartTime); });
            //Dispatcher.BeginInvoke(() => { UpdateButtons(); UpdateStatistics(); });

            App.ViewModel.SelectedTrail = trailMap;
            NavigationService.Navigate(new Uri("/TrailDisplayPage.xaml", UriKind.Relative));
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var trailName = string.IsNullOrEmpty(TrailNameTextbox.Text) ? "(no name)" : TrailNameTextbox.Text;
            var trailMap = new TrailMap { Name = trailName, History = App.ViewModel.TrailHistory, Id = _trailId };
            _uploadHelper.UploadTrailMap(trailMap);
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

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to discard this recording? Nothing will be saved, this cannot be undone.", "Discard recording", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                App.ViewModel.TrailHistory = new List<HistoryItem>();
                App.ViewModel.State = RecordingState.New;
                Dispatcher.BeginInvoke(() => { UpdateButtons(); UpdateStatistics(); });
            }
        }

        //private void Timer_tick(object state)
        //{
        //    if (!App.RunningInBackground)
        //    {
        //        Dispatcher.BeginInvoke(() => { UpdateButtons(); UpdateStatistics(); });
        //    }

        //    if (DateTime.UtcNow > _recordingStartTime.AddMinutes(_autoStopAfterMins))
        //        StopTracking();
        //}

        private void UpdateButtons()
        {
            StartTrackingButton.IsEnabled = App.ViewModel.State == RecordingState.New;
            SaveButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingFinished;
            DiscardButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingFinished;
            TrailNameTextbox.IsEnabled = App.ViewModel.State == RecordingState.RecordingFinished;
            StopTrackingButton.IsEnabled = App.ViewModel.State == RecordingState.RecordingStarted;
            //UploadButton.IsEnabled = App.ViewModel.TrailHistory == null || App.ViewModel.TrailHistory.Count > 0;
            if (App.ViewModel.State == RecordingState.New) TrailNameTextbox.Text = _newTrailText;
        }

        private void CorrectTheAppState()
        {
            if (App.Geolocator != null)
                App.ViewModel.State = RecordingState.RecordingStarted;
            else if (App.ViewModel.TrailHistory != null && App.ViewModel.TrailHistory.Count > 0)
                App.ViewModel.State = RecordingState.RecordingFinished;
            else
                App.ViewModel.State = RecordingState.New;
        }

        private string GetStateDescription(RecordingState recordingState)
        {
            switch (recordingState)
            {
                case RecordingState.New: return "new";
                case RecordingState.RecordingFinished: return "finished";
                case RecordingState.RecordingStarted: return "started";
                case RecordingState.Saved: return "saved";
                default: return "unknown";
            }
        }

        private void UpdateStatistics()
        {
            var map = new TrailMap();
            var now = DateTime.UtcNow;
            map.History = App.ViewModel.TrailHistory;
            TimeTextBlock.Text = map.FormattedTotalTime; //now.Subtract(_recordingStartTime).ToString(@"h\:mm\:ss");
            DistanceTextBlock.Text = map.FormattedTotalDistance;
            if (App.ViewModel.TrailHistory == null)
                txtStatus.Text = String.Format("Status: {0}", GetStateDescription(App.ViewModel.State));
            else
                txtStatus.Text = String.Format("Status: {0}, {1} points recorded", GetStateDescription(App.ViewModel.State), App.ViewModel.TrailHistory.Count);
        }

        private void StopTracking()
        {
            App.Geolocator.PositionChanged -= geolocator_PositionChanged;
            App.Geolocator = null;
            //_timer = null;

            if (App.ViewModel.TrailHistory.Count > 1)
                App.ViewModel.State = RecordingState.RecordingFinished;
            else
                App.ViewModel.State = RecordingState.New;
        }
    }
}