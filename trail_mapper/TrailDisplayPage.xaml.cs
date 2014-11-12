using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Maps.Controls;
using System.Windows.Media;
using System.Device.Location;
using Microsoft.Phone.Tasks;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using trail_mapper.ViewModels;

namespace trail_mapper
{
    public partial class TrailDisplayPage : PhoneApplicationPage
    {
        public TrailDisplayPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel.SelectedTrail;
            AltitudeStackPanel.DataContext = App.ViewModel;
            SpeedStackPanel.DataContext = App.ViewModel;
            BuildApplicationBar();
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            //var shareDataButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.map.treasure.png", UriKind.Relative));
            //shareDataButton.Text = trail_mapper.Resources.AppResources.AppBarShareDataButtonText;
            //shareDataButton.Click += ShareData_Click;
            //ApplicationBar.Buttons.Add(shareDataButton);

            var DeleteButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.delete.png", UriKind.Relative));
            DeleteButton.Text = trail_mapper.Resources.AppResources.AppBarDeleteButtonText;
            DeleteButton.Click += Delete_Click;
            ApplicationBar.Buttons.Add(DeleteButton);

            var UploadButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.upload.png", UriKind.Relative));
            UploadButton.Text = "upload"; //trail_mapper.Resources.AppResources.AppBarDeleteButtonText;
            UploadButton.Click += Upload_Click;
            ApplicationBar.Buttons.Add(UploadButton);
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            var trailMap = App.ViewModel.SelectedTrail;
            if (MessageBox.Show("Are you sure you want to delete this map?", "Delete " + trailMap.Name, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                //delete from isolated storage
                using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                    iso.DeleteFile(trailMap.Id.ToString() + ".json");
                //remove from view model
                App.ViewModel.RemoveTrailMap(trailMap);
                //go back to main page
                NavigationService.GoBack();
            }
        }

        private readonly UploadHelper _uploadHelper = new UploadHelper();
        private void Upload_Click(object sender, EventArgs e)
        {
            var trailMap = App.ViewModel.SelectedTrail;
            _uploadHelper.UploadTrailMap(trailMap);
        }

        //private void ShareData_Click(object sender, EventArgs e)
        //{
        //    var filePath = SaveMapImageToMediaLibrary();
        //    if (!string.IsNullOrEmpty(filePath))
        //    {
        //        ShareMediaTask shareTask = new ShareMe2diaTask();
        //        shareTask.FilePath = filePath;
        //        shareTask.Show();
        //    }
        //}

        private string SaveMapImageToMediaLibrary()
        {
            var bitmap = new WriteableBitmap(LayoutRoot, null);
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.SaveJpeg(stream, bitmap.PixelWidth, bitmap.PixelHeight, 0, 100);
                stream.Seek(0, SeekOrigin.Begin);

                foreach (MediaSource source in MediaSource.GetAvailableMediaSources())
                {
                    if (source.MediaSourceType == MediaSourceType.LocalDevice)
                    {
                        var mediaLibrary = new MediaLibrary(source);
                        var filename = "trailmap-" + App.ViewModel.SelectedTrail.Name.Replace(" ", "-");
                        var picture = mediaLibrary.SavePicture(filename, stream);
                        return picture.GetPath();
                    }
                }
            }
            return string.Empty;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var lastStackItem = NavigationService.BackStack.FirstOrDefault();
            if (lastStackItem.Source.OriginalString.Contains("TrackTrailPage.xaml"))
                NavigationService.RemoveBackEntry();

            var gc = new GeoCoordinateCollection();
            foreach (var item in App.ViewModel.SelectedTrail.History)
                gc.Add(new GeoCoordinate(item.Latitude, item.Longitude, item.Altitude));

            var dynamicPolyline = new MapPolyline();
            dynamicPolyline.StrokeDashed = false;
            dynamicPolyline.StrokeThickness = 5;
            dynamicPolyline.StrokeColor = Colors.Red;
            dynamicPolyline.Path = gc;

            HereMap.MapElements.Add(dynamicPolyline);
            HereMap.Center = App.ViewModel.SelectedTrail.Centre;

            if (e.NavigationMode == NavigationMode.New)
            {
                LoadTrailData(App.ViewModel.SelectedTrail);
            }
        }

        private void LoadTrailData(TrailMap trailMap)
        {
            int index = 0;
            App.ViewModel.Speeds.Clear();
            App.ViewModel.Altitudes.Clear();
            if (trailMap.History == null || trailMap.History.Count() == 0) return;

            var startTime = trailMap.History.Min(p => p.Time);
            var endTime = trailMap.History.Max(p => p.Time);
            var lengthInSeconds = endTime.Subtract(startTime).TotalSeconds;
            var numberOfPointsOnGraph = 20;
            var timeBetweenPointsInSeconds = lengthInSeconds / numberOfPointsOnGraph;

            var axis = (Syncfusion.UI.Xaml.Charts.NumericalAxis)AltitudeAreaChart.SecondaryAxis;
            axis.Minimum = trailMap.History.Min(p => p.Altitude);
            axis.Maximum = trailMap.History.Max(p => p.Altitude);
            axis = (Syncfusion.UI.Xaml.Charts.NumericalAxis)SpeedAreaChart.SecondaryAxis;
            axis.Minimum = trailMap.History.Min(p => p.Speed) * 3.6; //metres-per-second to km/h
            axis.Maximum = trailMap.History.Max(p => p.Speed) * 3.6;
            foreach (var point in trailMap.History)
            {
                var pointTime = point.Time.Subtract(startTime);
                if (pointTime.TotalSeconds >= index * timeBetweenPointsInSeconds)
                {
                    index++;
                    App.ViewModel.Altitudes.Add(new model
                    {
                        ProdId = index,
                        Prodname = index % 3 == 1 ? pointTime.ToString(@"h\:mm\:ss") : string.Empty,
                        Value = point.Altitude
                    });
                    App.ViewModel.Speeds.Add(new model
                    {
                        ProdId = index,
                        Prodname = index % 3 == 1 ? pointTime.ToString(@"h\:mm\:ss") : string.Empty,
                        Value = point.Speed * 3.6
                    });
                }
            }
        }

        private void HereMap_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.SelectedTrail.History.Count > 0)
            {
                var array = App.ViewModel.SelectedTrail.History.Select(i => new GeoCoordinate(i.Latitude, i.Longitude)).ToArray();
                var boundingRectangle = LocationRectangle.CreateBoundingRectangle(array);
                HereMap.SetView(boundingRectangle);
            }
        }
    }
}