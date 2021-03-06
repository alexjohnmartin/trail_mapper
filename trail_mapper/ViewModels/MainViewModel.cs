﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using trail_mapper.Resources;
using Windows.Devices.Geolocation;

namespace trail_mapper.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.Items = new ObservableCollection<ItemViewModel>();
        }

        private const string ExampleJson = @"{""Id"":""da53bea3-7c24-4f91-a69e-bd2b056a0e54"",""Name"":""Example trail"",""History"":[{""Time"":""2013-07-26T02:12:46.2509629Z"",""Speed"":0.0,""Latitude"":49.23358093947172,""Longitude"":-122.86661613732576,""Altitude"":117.5},{""Time"":""2013-07-26T02:12:47.8182084Z"",""Speed"":4.5,""Latitude"":49.232661947607994,""Longitude"":-122.8579886443913,""Altitude"":13},{""Time"":""2013-07-26T02:12:47.8182084Z"",""Speed"":2.8,""Latitude"":49.232661947607994,""Longitude"":-122.8579886443913,""Altitude"":13},{""Time"":""2013-07-26T02:25:56.8353002Z"",""Speed"":5.6,""Latitude"":49.275783989578485,""Longitude"":-122.79781311750412,""Altitude"":25},{""Time"":""2013-07-26T02:27:11.7287271Z"",""Speed"":4.4,""Latitude"":49.27615849301219,""Longitude"":-122.79763399623334,""Altitude"":82.5},{""Time"":""2013-07-26T02:27:48.8360497Z"",""Speed"":4.6,""Latitude"":49.277490712702274,""Longitude"":-122.80161749571562,""Altitude"":59},{""Time"":""2013-07-26T02:28:26.6515222Z"",""Latitude"":49.27980235777795,""Longitude"":-122.80168689787388,""Altitude"":18.5},{""Time"":""2013-07-26T02:29:04.6109888Z"",""Latitude"":49.2831433005631,""Longitude"":-122.80192737467587,""Altitude"":86.5},{""Time"":""2013-07-26T02:29:42.7283436Z"",""Latitude"":49.28643395192921,""Longitude"":-122.80168857425451,""Altitude"":67},{""Time"":""2013-07-26T02:30:19.7745374Z"",""Latitude"":49.28920065052807,""Longitude"":-122.80174423009157,""Altitude"":154},{""Time"":""2013-07-26T02:30:57.6961027Z"",""Latitude"":49.293378526344895,""Longitude"":-122.80059657990932,""Altitude"":102.5},{""Time"":""2013-07-26T02:31:35.1277652Z"",""Latitude"":49.29450958035886,""Longitude"":-122.80559135600924,""Altitude"":145},{""Time"":""2013-07-26T02:32:12.1185917Z"",""Latitude"":49.29414438083768,""Longitude"":-122.80966135673225,""Altitude"":169},{""Time"":""2013-07-26T02:32:49.1098621Z"",""Latitude"":49.29563292302191,""Longitude"":-122.81145215034485,""Altitude"":201},{""Time"":""2013-07-26T02:33:26.1786653Z"",""Latitude"":49.29753360338509,""Longitude"":-122.81418020837009,""Altitude"":214},{""Time"":""2013-07-26T02:34:06.4890117Z"",""Latitude"":49.298998760059476,""Longitude"":-122.81895018182695,""Altitude"":256.5},{""Time"":""2013-07-26T02:34:54.4901197Z"",""Latitude"":49.30066323839128,""Longitude"":-122.81969072297215,""Altitude"":247}],""Description"":""Distance: 11.03km, time: 0:22:08""}";
        public ObservableCollection<model> Altitudes { get; private set; }
        public ObservableCollection<model> Speeds { get; private set; }

        public ObservableCollection<ItemViewModel> Items { get; private set; }
        public ObservableCollection<TrailMap> MapItems { get; private set; }
        public IList<HistoryItem> TrailHistory { get; set; }
        public TrailMap SelectedTrail { get; set; }
        public RecordingState State { get; set; }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public void LoadData()
        {
            //TODO:trigger a data sync

            TrailMap trailMap;
            Speeds = new ObservableCollection<model>();
            Altitudes = new ObservableCollection<model>();

            MapItems = new ObservableCollection<TrailMap>(); 
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (var filename in iso.GetFileNames(@"*"))
                {
                    if (filename.EndsWith(".json"))
                    {
                        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(filename, FileMode.Open, iso))
                        {
                            using (StreamReader reader = new StreamReader(isoStream))
                            {
                                var json = reader.ReadToEnd();
                                trailMap = JsonConvert.DeserializeObject<TrailMap>(json);
                                MapItems.Add(trailMap);
                            }
                        }                        
                    }
                }

                //example data
                trailMap = JsonConvert.DeserializeObject<TrailMap>(ExampleJson);
                MapItems.Add(trailMap);
            }

            this.IsDataLoaded = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        internal void AddTrailHistory(TrailMap trailMap)
        {
            if (MapItems != null)
            {
                MapItems.Add(trailMap);
                NotifyPropertyChanged("MapItems");

                //TODO:trigger a data sync
            }
        }

        internal void RemoveTrailMap(TrailMap trailMap)
        {
            MapItems.Remove(trailMap);
            NotifyPropertyChanged("MapItems");

            //TODO:trigger a data sync
        }

        internal void UpdateSelectedTrailName(string newName)
        {
            SelectedTrail.Name = newName;
            NotifyPropertyChanged("SelectedTrail");
            var item = MapItems.First(i => i.Id == SelectedTrail.Id);
            item.Name = newName;
            NotifyPropertyChanged("MapItems");

            //TODO:trigger a data sync
        }
    }
}