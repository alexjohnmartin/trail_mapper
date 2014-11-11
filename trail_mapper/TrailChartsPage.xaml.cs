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
            App.ViewModel.Products.Clear();

            var startTime = trailMap.History.Min(p => p.Time);
            var endTime = trailMap.History.Max(p => p.Time);
            var lengthInSeconds = endTime.Subtract(startTime).TotalSeconds;
            var numberOfPointsOnGraph = 20;
            var timeBetweenPointsInSeconds = lengthInSeconds / numberOfPointsOnGraph;

            var axis = (Syncfusion.UI.Xaml.Charts.NumericalAxis)AreaChart.SecondaryAxis;
            axis.Minimum = trailMap.History.Min(p => p.Altitude);
            axis.Maximum = trailMap.History.Max(p => p.Altitude);
            foreach (var point in trailMap.History)
            {
                if (point.Time.Subtract(startTime).TotalSeconds >= index * timeBetweenPointsInSeconds)
                {
                    App.ViewModel.Products.Add(new model
                    {
                        ProdId = index,
                        Prodname = "",
                        Stock = point.Altitude
                    });
                    index++;
                }
            }
        }
    }

    public class model
    {
        public double ProdId { get; set; }
        public string Prodname { get; set; }
        public double Stock { get; set; }
    }

    public class viewmodel
    {
        public viewmodel()
        {
            this.Products = new ObservableCollection<model>();

            Products.Add(new model() { ProdId = 1, Prodname = "Rice", Stock = 53 });
            Products.Add(new model() { ProdId = 2, Prodname = "Wheat", Stock = 76 });
            Products.Add(new model() { ProdId = 3, Prodname = "Oil", Stock = 80 });
            Products.Add(new model() { ProdId = 4, Prodname = "Corn", Stock = 50 });
            Products.Add(new model() { ProdId = 5, Prodname = "Gram", Stock = 68 });
            Products.Add(new model() { ProdId = 6, Prodname = "Milk", Stock = 90 });
            Products.Add(new model() { ProdId = 7, Prodname = "Butter", Stock = 73 });
        }

        public ObservableCollection<model> Products { get; set; }
    }
}