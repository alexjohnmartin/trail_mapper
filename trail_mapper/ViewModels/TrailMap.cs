using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq; 

namespace trail_mapper.ViewModels
{
    public class TrailMap
    {
        public string Name { get; set; }
        public IList<HistoryItem> History { get; set; }

        public TimeSpan TotalTime
        {
            get
            {
                if (History.Count < 2)
                    return new TimeSpan();

                return History.Last().Time.Subtract(History.First().Time); 
            }
        }

        public double TotalDistance
        {
            get
            {
                double total = 0;
                if (History.Count < 2)
                    return total;

                for (int i = 1; i < History.Count; i++)
                {
                    total += new GeoCoordinate(History[i].Latitude, History[i].Longitude, History[i].Altitude).GetDistanceTo(new GeoCoordinate(History[i-1].Latitude, History[i-1].Longitude, History[i-1].Altitude)); 
                }
                return total;
            }
        }

        public GeoCoordinate Centre
        {
            get
            {
                double totalAlt = 0; 
                double totalLat = 0;
                double totalLong = 0;
                foreach (var item in History)
                {
                    totalAlt += item.Altitude; 
                    totalLat += item.Latitude;
                    totalLong += item.Longitude; 
                }

                return new GeoCoordinate(totalLat / History.Count, totalLong / History.Count, totalAlt / History.Count);
            }
        }
    }
}
