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

namespace trail_mapper
{
    public partial class TrailDisplayPage : PhoneApplicationPage
    {
        public TrailDisplayPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel.SelectedTrail;
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
    }
}