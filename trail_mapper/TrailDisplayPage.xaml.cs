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

namespace trail_mapper
{
    public partial class TrailDisplayPage : PhoneApplicationPage
    {
        public TrailDisplayPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel.SelectedTrail;
            BuildApplicationBar();
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            var DeleteButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.delete.png", UriKind.Relative));
            DeleteButton.Text = trail_mapper.Resources.AppResources.AppBarDeleteButtonText;
            DeleteButton.Click += Delete_Click;
            ApplicationBar.Buttons.Add(DeleteButton);

            //var shareDataButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.database.png", UriKind.Relative));
            //shareDataButton.Text = trail_mapper.Resources.AppResources.AppBarShareDataButtonText;
            //shareDataButton.Click += ShareData_Click;
            //ApplicationBar.Buttons.Add(shareDataButton);
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

        private void ShareData_Click(object sender, EventArgs e)
        {
            var map = App.ViewModel.SelectedTrail;
            var emailTask = new EmailComposeTask();
            emailTask.Body = JsonConvert.SerializeObject(map);
            emailTask.Show();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

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