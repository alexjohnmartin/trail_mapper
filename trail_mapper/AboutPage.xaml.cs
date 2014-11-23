using System;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Xml.Linq;
using RateMyApp.Helpers;

namespace trail_mapper
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        public void TwitterButton_Click(object sender, EventArgs e)
        {
            var task = new WebBrowserTask
            {
                Uri = new Uri("https://twitter.com/AlexJohnMartin", UriKind.Absolute)
            };
            task.Show();
        }

        public void StoreButton_Click(object sender, EventArgs e)
        {
            var currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var task = new WebBrowserTask
            {
                Uri = new Uri(string.Format("http://www.windowsphone.com/{0}/store/publishers?publisherId=nocturnal%2Btendencies&appId=63cb6767-4940-4fa1-be8c-a7f58e455c3b", currentCulture.Name), UriKind.Absolute)
            };
            task.Show();
        }

        public void ReviewButton_Click(object sender, EventArgs e)
        {
            FeedbackHelper.Default.Reviewed();
            var marketplace = new MarketplaceReviewTask();
            marketplace.Show();
        }

        public void EmailButton_Click(object sender, EventArgs e)
        {
            var version = XDocument.Load("WMAppManifest.xml").Root.Element("App").Attribute("Version").Value;
            var email = new EmailComposeTask();
            email.Subject = "Feedback for Trail Mapper (WP8)";
            email.Body = string.Format("{0}{0}Trail Mapper version {1}{0}Windows Phone version {2}", 
                                       Environment.NewLine, version, Environment.OSVersion);
            email.Show();
        }
    }
}