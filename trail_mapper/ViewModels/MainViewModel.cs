using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
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

        public ObservableCollection<ItemViewModel> Items { get; private set; }
        public ObservableCollection<TrailMap> MapItems { get; private set; }
        public IList<HistoryItem> TrailHistory { get; set; }
        public TrailMap SelectedTrail;

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public void LoadData()
        {
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
                                if (json.Contains("Name"))
                                {
                                    var trailMap = JsonConvert.DeserializeObject<TrailMap>(json);
                                    MapItems.Add(trailMap);
                                }
                            }
                        }                        
                    }
                }
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
            }
        }
    }
}