using Microsoft.Xna.Framework.Media;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using System.Windows.Threading;
using trail_mapper.ViewModels;

namespace trail_mapper
{
    class MapDownloader
    {
        const int MaxPoints = 80;
        const string Width = "1000";
        const string Height = "1000";
        const string LineWeight = "6";
        const string LineColor = "FFFF0000";
        const string ShadowColor = "FFFF0000";
        const string FormatString = "0.00000";
        const string HereMapsAppId = "PC3CUQZkDFZ46i8ifPIL";
        const string HereMapsAppCode = "u_JokeYoH5JkfpvqL2CuFA";
        const string DownloadUrl = "http://image.maps.cit.api.here.com/mia/1.6/route?app_id={0}&app_code={1}&r={2}&m={3}&lc={4}&sc={5}&lw={6}&h={7}&w={8}";

        IsolatedStorageFile MyStore = IsolatedStorageFile.GetUserStoreForApplication();
        private string _filename = string.Empty;

        private Dispatcher _dispatcher;
        public MapDownloader(Dispatcher dispatcher) { _dispatcher = dispatcher; }

        public async Task<bool> DownloadMapImageAsync(TrailMap trailMap)
        {
            if (trailMap.History == null && trailMap.History.Count < 1) return false;

            var downloadUrl = string.Empty;
            await Task.Run(() => { 
                _filename = trailMap.FileName + ".jpg";
                downloadUrl = string.Format(DownloadUrl, HereMapsAppId, HereMapsAppCode, GetMapPoints(trailMap), GetMarkers(trailMap), LineColor, ShadowColor, LineWeight, Height, Width);
            });

            try
            {
                var client = new WebClient();
                client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted_wide);
                client.OpenReadAsync(new Uri(downloadUrl), client);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        void client_OpenReadCompleted_wide(object sender, OpenReadCompletedEventArgs e)
        {
            var resInfo = new StreamResourceInfo(e.Result, null);
            
            resInfo.Stream.Seek(0, SeekOrigin.Begin);
            foreach (MediaSource source in MediaSource.GetAvailableMediaSources())
            {
                if (source.MediaSourceType == MediaSourceType.LocalDevice)
                {
                    var mediaLibrary = new MediaLibrary(source);
                    var picture = mediaLibrary.SavePicture(_filename, resInfo.Stream);
                }
            }
            resInfo.Stream.Close();
            _dispatcher.BeginInvoke(() => MessageBox.Show("map downloaded to pictures"));
        }

        private string GetMapPoints(TrailMap trailMap)
        {
            var mod = 1;
            if (trailMap.History.Count > MaxPoints)
                mod = trailMap.History.Count / MaxPoints;

            var sb = new StringBuilder();
            var first = trailMap.History.First();
            var last = trailMap.History.Last();
            sb.Append(first.Latitude);
            sb.Append(',');
            sb.Append(first.Longitude);
            sb.Append(',');

            var index = 0;
            foreach (var point in trailMap.History)
            {
                index++;
                if (index % mod == 0)
                {
                    sb.Append(point.Latitude.ToString(FormatString));
                    sb.Append(',');
                    sb.Append(point.Longitude.ToString(FormatString));
                    sb.Append(',');
                }
            }

            sb.Append(last.Latitude);
            sb.Append(',');
            sb.Append(last.Longitude);
            return sb.ToString();
        }

        private string GetMarkers(TrailMap trailMap)
        {
            var sb = new StringBuilder();
            var first = trailMap.History.First();
            var last = trailMap.History.Last();
            sb.Append(first.Latitude);
            sb.Append(',');
            sb.Append(first.Longitude);
            sb.Append(',');
            sb.Append(last.Latitude);
            sb.Append(',');
            sb.Append(last.Longitude);
            return sb.ToString();
        }
    }
}
/*
https://developer.here.com/rest-apis/documentation/enterprise-map-image/topics/resource-route.html

http://image.maps.cit.api.here.com/mia/1.6/route
         ?app_id=DemoAppId01082013GAL
         &app_code=AJKnXv84fjrb0KIHawS0Tg
         &r=52.5338,13.2966,52.538361,13.325329
         &m=52.5338,13.2966,52.538361,13.325329
         &lc=ff0000ff
         &sc=ff0000ff
         &lw=6
         &h=800&w=1000
*/