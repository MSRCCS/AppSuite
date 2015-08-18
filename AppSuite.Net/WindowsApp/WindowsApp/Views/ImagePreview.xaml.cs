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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace WindowsApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ImagePreview : Page
    {
        public ImagePreview()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

           // DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            showImage();
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
           // DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape | DisplayOrientations.Portrait | DisplayOrientations.LandscapeFlipped | DisplayOrientations.PortraitFlipped;
        }
        private async void showImage()
        {


            var currentApp = (App)App.Current;
            MemoryStream ms = currentApp.CurrentImageRecog;
       //my attempt at rotating the image. In order for this to work need to call enc.SetPixelData() then enc.FlushAsync().  
       //The problem is SetPixelData requires 7 arguments ( BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, uint width, uint height, double dpiX, double dpiY, byte[]pixels)
            //I dont know how to get these arguments. 


           // BitmapDecoder dec = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());

            //PixelDataProvider p= await dec.GetPixelDataAsync();
           
            
            //ms.AsRandomAccessStream().Size = 0;
            //BitmapEncoder enc = await BitmapEncoder.CreateForTranscodingAsync(ms.AsRandomAccessStream(), dec);
            //enc.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees;
            //enc.SetPixelData(BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, uint width, uint height, double dpiX, double dpiY, byte[]pixels);
            
           // await enc.FlushAsync();
            System.Diagnostics.Debug.WriteLine("I got out of FlushAsync");
            //ISSUE HERE
            //ImageStream im = await dec.GetPreviewAsync();
            //await enc.FlushAsync();

  
            
      
            
            
            // Get photo as a BitmapImage 
            //if (!Object.ReferenceEquals(im, null))
            //{
              //  BitmapImage bmpImage = new BitmapImage();
                
                // bmpImage.UriSource = new Uri(file.Path);
               // im.Seek(0L);
                //bmpImage.SetSource(im);

                // imagePreivew is a <Image> object defined in XAML 
                //imagePreview2.Source = bmpImage;
            //}

        }
        private void retake(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WindowsApp.MainPage));
        }

        private async void accept(object sender, RoutedEventArgs e)
        {
            var currentApp = (App)App.Current;
            var ms = currentApp.CurrentImageRecog;
            if (Object.ReferenceEquals(ms, null))
            {
                currentApp.CurrentRecogResult = "no image taken. ";
            }
            else
            {
                Byte[] buf = ms.ToArray();
                currentApp.CurrentRecogResult = await App.VMHub.ProcessRequest(buf);
            } 
            Frame.Navigate(typeof(WindowsApp.Views.SelectDomain), "CapturedPhoto");
        }
    }
}
