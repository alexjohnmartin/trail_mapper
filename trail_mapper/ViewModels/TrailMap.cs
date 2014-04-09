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
                if (History.Count == 0)
                    return new GeoCoordinate();

                if (History.Count == 1)
                    return new GeoCoordinate(History.First().Latitude, History.First().Longitude, History.First().Altitude); 

                //calculate MEAN (average)
                //double totalAlt = 0; 
                //double totalLat = 0;
                //double totalLong = 0;
                //foreach (var item in History)
                //{
                //    totalAlt += item.Altitude; 
                //    totalLat += item.Latitude;
                //    totalLong += item.Longitude; 
                //}

                //calculate MEDIAN (middle value)
                var alts = History.Select(i => i.Altitude).ToList();
                alts.Sort(); 
                var lats = History.Select(i => i.Latitude).ToList();
                lats.Sort(); 
                var longs = History.Select(i => i.Longitude).ToList();
                longs.Sort();
                int index = (History.Count / 2) - 1; 

                return new GeoCoordinate(lats[index], longs[index], alts[index]);
            }
        }

        //detecting outliers
        //http://mathforum.org/library/drmath/view/52794.html
        public void CleanUpData()
        { 
            //mean
            var mean = Centre;

            ////calculate variance
            //double varianceTotal = 0;
            var distancesFromCentre = new List<double>(); 
            foreach (var item in History)
            {
                var distance = new GeoCoordinate(item.Latitude, item.Longitude, item.Altitude).GetDistanceTo(mean);
                distancesFromCentre.Add(distance); 
                //varianceTotal += (distance * distance);
            }
            distancesFromCentre.Sort();
            var lq = distancesFromCentre[(distancesFromCentre.Count / 4)];
            var uq = distancesFromCentre[(distancesFromCentre.Count / 4) * 3];
            var iqr = uq - lq;
            var lower = lq - (1.5 * iqr);
            var upper = uq + (1.5 * iqr); 

            //var variance = varianceTotal / History.Count;
            //var standardDeviation = Math.Sqrt(variance); 

            ////TODO:forget it if the start and end are outside the standard deviation
            var startDistanceFromMean = new GeoCoordinate(History.First().Latitude, History.First().Longitude, History.First().Altitude).GetDistanceTo(mean);
            var endDistanceFromMean = new GeoCoordinate(History.Last().Latitude, History.Last().Longitude, History.Last().Altitude).GetDistanceTo(mean);
            //if (startDistanceFromMean > lower && startDistanceFromMean < upper &&
            //    endDistanceFromMean > lower && endDistanceFromMean < upper)
            //{
                var index = 0;
                while (index < History.Count)
                {
                    var distanceFromMean = new GeoCoordinate(History[index].Latitude, History[index].Longitude, History[index].Altitude).GetDistanceTo(mean);
                    if (distanceFromMean < lower || distanceFromMean > upper)
                        History.RemoveAt(index);
                    else
                        index++;
                }
            //}
        }
    }
}
