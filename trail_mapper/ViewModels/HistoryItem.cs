using System;
using Windows.Devices.Geolocation;

namespace trail_mapper.ViewModels
{
    public class HistoryItem
    {
        public DateTime Time { get; set; }
        public Double Latitude { get; set; }
        public Double Longitude { get; set; }
        public Double Altitude { get; set; }
        public Double Speed { get; set; }
    }
}
