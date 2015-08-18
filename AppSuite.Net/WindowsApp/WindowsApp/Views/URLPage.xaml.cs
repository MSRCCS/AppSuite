using WindowsApp.Common;
using WindowsApp.Data;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Media.Devices;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Imaging;
using WindowsApp.Views;

namespace WindowsApp.Views
{
    public sealed partial class URLPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        public static URLPage Current;

        public URLPage()
        {
            this.InitializeComponent();
            Current = this;
            
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        #region Navigation
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-1");
        }
        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }
        #endregion

        /// <summary>
        /// Displays a preview of the image before the image is sent through processing
        /// </summary>
        private async void showURLImage(object sender, TextControlPasteEventArgs e)
        {

            if (string.IsNullOrEmpty(URL.Text) || URL.Text.Equals("Please Enter a URL"))
            {
                //wait for text to be pasted into the TextBlock definded in xaml
                await Task.Delay(100);
            }
            if (string.IsNullOrEmpty(URL.Text) || URL.Text.Equals("Please Enter a URL"))
            {
                URL.Text = "Please enter a URL";
            }
            try
            {
                String input = URL.Text;

                //checks if the URL entered is a direct reference to a .jpeg, .jpg or .png file
                if (input.Contains(".jpeg") || input.Contains(".jpg") || input.Contains(".png"))
                {
                    Uri uri = new Uri(input);
                    BitmapImage bmpImage = new BitmapImage(uri);
                    errorImage.Visibility = Visibility.Collapsed;
                    errorMessage.Visibility = Visibility.Collapsed;
                    previewImage.Source = bmpImage;
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                URL.Text = "The URL entered was invalid";
                errorImage.Visibility = Visibility.Visible;
                errorMessage.Visibility = Visibility.Visible;
            }

        }
        /// <summary>
        /// Gets the image from the uri and sends the image through processing
        /// </summary>
        private async void getImageFromURL(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(URL.Text) || URL.Text.Equals("Please Enter a URL"))
            {
                URL.Text = "Please Enter a URL";
            }
            else
            {
                try
                {
                    String input = URL.Text;
                    Uri uri = new Uri(input);
                    var currentApp = (App)App.Current;
                    var rass = RandomAccessStreamReference.CreateFromUri(uri);
                    var stream = (await rass.OpenReadAsync()).AsStream();
                    int len = (int)stream.Length;
                    byte[] buff = new byte[len];
                    int pos = 0;
                    int r = 0;
                    while ((r = stream.Read(buff, pos, len - pos)) > 0)
                    {
                        pos += r;
                    }

                    currentApp.CurrentImageRecog = new MemoryStream(buff);
                    currentApp.CurrentRecogResult = await App.VMHub.ProcessRequest(buff);

                    Frame.Navigate(typeof(WindowsApp.Views.RecogResultPage), "url");
                }
                catch
                {
                    URL.Text = "The URL entered was invalid";
                    errorImage.Visibility = Visibility.Visible;
                    errorMessage.Visibility = Visibility.Visible;
                }
            }
        }



    }
}



