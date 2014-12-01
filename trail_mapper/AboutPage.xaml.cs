using System;
using System.Linq;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Xml.Linq;
using RateMyApp.Helpers;
using Microsoft.Live;
using Microsoft.Live.Controls;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Phone.Shell;

namespace trail_mapper
{
    public partial class AboutPage : PhoneApplicationPage
    {
        private LiveConnectClient client;

        public AboutPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
        }

        private void loginButton_SessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {

            if (e != null && e.Status == LiveConnectSessionStatus.Connected)
            {
                //the session status is connected so we need to set this session status to client
                this.client = new LiveConnectClient(e.Session);
                Upload.IsEnabled = true;
            }
            else
            {
                this.client = null;
                Upload.IsEnabled = false;
            }

        }

        private async void Connect_Click(object sender, EventArgs e)
        {
            bool connected = false;
            try
            {
                var authClient = new LiveAuthClient("00000000441169AA");
                LiveLoginResult result = await authClient.LoginAsync(new string[] { "wl.signin", "wl.skydrive", "wl.skydrive_update" });

                if (result.Status == LiveConnectSessionStatus.Connected)
                {
                    connected = true;
                    client = new LiveConnectClient(result.Session);
                    //var meResult = await connectClient.GetAsync("me");
                    //dynamic meData = meResult.Result;
                    //updateUI(meData);
                }
            }
            catch (LiveAuthException ex)
            {
                // Display an error message.
            }
            catch (LiveConnectException ex)
            {
                // Display an error message.
            }

            // Turn off the display of the connection button in the UI.
            Upload.IsEnabled = connected;
            ConnectButton.Visibility = connected ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void Upload_Click(object sender, EventArgs e)
        {
            App.ViewModel.IsBusy = true;

            Dispatcher.BeginInvoke(() =>
            {
                UploadTrailMaps();
            });
        }

        private async void UploadTrailMaps()
        {
            if (client != null)
            {
                try
                {
                    using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        var filenames = iso.GetFileNames(@"*");
                        if (filenames.Any())
                        {
                            string skyDriveFolder = await CreateDirectoryAsync(client, "TrailMaps", "me/skydrive");
                            foreach (var filename in filenames)
                            {
                                if (filename.EndsWith(".json"))
                                {
                                    var isfs = iso.OpenFile(filename, FileMode.Open, FileAccess.Read);
                                    var res = await client.UploadAsync(skyDriveFolder, filename, isfs, OverwriteOption.Overwrite);
                                }
                            }
                            MessageBox.Show(string.Format("{0} trail maps uploaded", filenames.Count()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please sign in with your Microsoft Account.");
            }
            Dispatcher.BeginInvoke(() => App.ViewModel.IsBusy = false);
        }

        protected async Task<string> CreateDirectoryAsync(LiveConnectClient client, string folderName, string parentFolder)
        {
            string folderId = null;

            // Retrieves all the directories.
            var queryFolder = parentFolder + "/files?filter=folders,albums";
            var opResult = await client.GetAsync(queryFolder);
            dynamic result = opResult.Result;

            foreach (dynamic folder in result.data)
            {
                // Checks if current folder has the passed name.
                if (folder.name.ToLowerInvariant() == folderName.ToLowerInvariant())
                {
                    folderId = folder.id;
                    break;
                }
            }

            if (folderId == null)
            {
                // Directory hasn't been found, so creates it using the PostAsync method.
                var folderData = new Dictionary<string, object>();
                folderData.Add("name", folderName);
                opResult = await client.PostAsync(parentFolder, folderData);
                result = opResult.Result;

                // Retrieves the id of the created folder.
                folderId = result.id;
            }

            return folderId;
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