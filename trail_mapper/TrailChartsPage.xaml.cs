using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using trail_mapper.ViewModels;

namespace trail_mapper
{
    public partial class TrailChartsPage : PhoneApplicationPage
    {
        public TrailChartsPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
            LayoutRoot.DataContext = App.ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            App.Breadcrumb = "loading data";
            
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
    }

    public class model
    {
        public double ProdId { get; set; }
        public string Prodname { get; set; }
        public double Value { get; set; }
    }

    public class viewmodel
    {
        public viewmodel()
        {
            this.Products = new ObservableCollection<model>();

            Products.Add(new model() { ProdId = 1, Prodname = "Rice", Value = 53 });
            Products.Add(new model() { ProdId = 2, Prodname = "Wheat", Value = 76 });
            Products.Add(new model() { ProdId = 3, Prodname = "Oil", Value = 80 });
            Products.Add(new model() { ProdId = 4, Prodname = "Corn", Value = 50 });
            Products.Add(new model() { ProdId = 5, Prodname = "Gram", Value = 68 });
            Products.Add(new model() { ProdId = 6, Prodname = "Milk", Value = 90 });
            Products.Add(new model() { ProdId = 7, Prodname = "Butter", Value = 73 });
        }

        public ObservableCollection<model> Products { get; set; }
    }
}