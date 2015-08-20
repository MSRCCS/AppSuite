/********************************************************************
Copyright 2015 Microsoft
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. 
File: 
MainPage.xaml.cs
Description: 
Main page of the windows phone app
Author: 
Jin Li, Partner Research Manager
Microsoft Research, One Microsoft Way
Date:
June. 2015
*******************************************************************/
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


namespace WindowsApp
{
    /// <summary>
    /// Creating the page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        private MediaCapture captureManager;
        internal static MainPage Current;
        internal VideoRotation currentRotation;
        internal Boolean takePicture = false;
        internal Boolean inProcessing = false;

        /// <summary>
        /// Initializing the Component
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;

            this.NavigationCacheMode = NavigationCacheMode.Required;
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

        }

        # region Navigation
        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }
        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data

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
        /// <summary>
        /// Adds an item to the list when the app bar button is clicked.
        /// </summary>




        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            inProcessing = false;
            acceptButton.Visibility = Visibility.Collapsed;
            rejectButton.Visibility = Visibility.Collapsed;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape | DisplayOrientations.PortraitFlipped | DisplayOrientations.LandscapeFlipped;
            await initCamera();
            this.navigationHelper.OnNavigatedTo(e);
        }



        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (captureManager != null)
            {
                captureManager.Dispose();
                captureManager = null;
                capturePreview.Source = null;
                imagePreview.Source = null;
            }
            takePicture = false;
            this.navigationHelper.OnNavigatedFrom(e);
            Loading.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Orientation

        ///<summary>
        ///fixes the video preview orientation of the camera
        ///</summary>
        private void fixOrientation(DisplayInformation displayInfo, object args)
        {
            if (captureManager != null && takePicture == false)
            {
                currentRotation = VideoRotationLookup(displayInfo.CurrentOrientation, false);
                captureManager.SetPreviewRotation(currentRotation);
                captureManager.SetRecordRotation(currentRotation);

                //takePictureButton is a xaml button that fills the entire screen
                takePictureButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// finds the rotation of which to rotate the image
        /// </summary>
        /// <param name="displayOrientation"></param>
        /// the current phone orientation
        /// <param name="counterclockwise"></param>
        /// checks if rotation should be done clockwise or counterclockwise
        /// <returns></returns>
        private VideoRotation VideoRotationLookup(DisplayOrientations displayOrientation, bool counterclockwise)
        {
            switch (displayOrientation)
            {
                case DisplayOrientations.Landscape:
                    return VideoRotation.None;
                case DisplayOrientations.Portrait:
                    return (counterclockwise) ? VideoRotation.Clockwise270Degrees : VideoRotation.Clockwise90Degrees;
                case DisplayOrientations.LandscapeFlipped:
                    return VideoRotation.Clockwise180Degrees;
                case DisplayOrientations.PortraitFlipped:
                    return (counterclockwise) ? VideoRotation.Clockwise90Degrees :
                    VideoRotation.Clockwise270Degrees;
                default:
                    return VideoRotation.None;
            }
        }

        /// <summary>
        /// rotates the Image object defined in xaml. 
        /// visual shortcut to avoid calling Rotate(MemoryStream stream), a time costly method
        /// </summary>
        public void rotatePreviewImage()
        {

            var ScreenHeight = Window.Current.Bounds.Height;
            var ScreenWidth = Window.Current.Bounds.Width;

            RotateTransform rotate = new RotateTransform();
            double rotation = 0;
            if (currentRotation.Equals(VideoRotation.Clockwise90Degrees))
            {
                rotation = 90;
            }
            else if (currentRotation.Equals(VideoRotation.Clockwise180Degrees))
            {
                rotation = 180;
            }
            else if (currentRotation.Equals(VideoRotation.Clockwise270Degrees))
            {
                rotation = 270;
            }
            else
            {
                rotation = 0;
            }
            rotate.Angle = rotation;

            imagePreview.RenderTransformOrigin = new Point(.5, .5);
            imagePreview.RenderTransform = rotate;

        }
        async private Task<byte[]> RotateImage(InMemoryRandomAccessStream mrs)
        {
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(mrs).AsTask().ConfigureAwait(false);
            uint w = decoder.PixelWidth; uint h = decoder.PixelHeight;
            if (currentRotation.Equals(VideoRotation.Clockwise90Degrees) || currentRotation.Equals(VideoRotation.Clockwise270Degrees))
            {
                w = decoder.PixelHeight;
                h = decoder.PixelWidth;
            }


            BitmapTransform transform = new BitmapTransform() { Rotation = (BitmapRotation)currentRotation };

            PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.DoNotColorManage);
            byte[] pixels = pixelData.DetachPixelData();
            //InMemoryRandomAccessStream encoded = new InMemoryRandomAccessStream();
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, mrs);



            encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, w, h, 96, 96, pixels);
            await encoder.FlushAsync().AsTask().ConfigureAwait(false);
            mrs.Seek(0);
            byte[] outBytes = new byte[mrs.Size];
            await mrs.AsStream().ReadAsync(outBytes, 0, outBytes.Length);
            MemoryStream ms = new MemoryStream(outBytes);
            var currentApp = (App)App.Current;
            currentApp.CurrentImageRecog = ms;
            return outBytes;
        }

        #endregion

        #region Take Picture Functions
        /// <summary>
        /// initializes the camera
        /// </summary>
        /// <returns></returns>
        public async Task initCamera()
        {
            captureManager = new MediaCapture();

            var cameraID = await GetCameraID(Windows.Devices.Enumeration.Panel.Back);
            await captureManager.InitializeAsync(new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                PhotoCaptureSource = PhotoCaptureSource.Photo,
                AudioDeviceId = string.Empty,
                VideoDeviceId = cameraID.Id
            });

            //await captureManager.InitializeAsync();

            //Sets the camera resolution to the minimum available. Allows the image to be sent through processing without resizing
            var minResolution = captureManager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).Aggregate((i1, i2) => (i1 as VideoEncodingProperties).Width < (i2 as VideoEncodingProperties).Width ? i1 : i2);
            await captureManager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, minResolution);

            capturePreview.Source = captureManager;
            await captureManager.StartPreviewAsync();
            DisplayInformation displayInfo = DisplayInformation.GetForCurrentView();

            //calls fixOrientation() everytime the phone orientation is changed
            displayInfo.OrientationChanged += fixOrientation;
            fixOrientation(displayInfo, null);

        }

/// <summary>
/// called when user taps the take picture button
/// </summary>
/// <param name="sender"></param>
/// <param name="e"></param>
        private async void takePicture_click(object sender, RoutedEventArgs e)
        {
            await doTakePicture(sender, e);

        }
        async private Task<int> doTakePicture(object sender, RoutedEventArgs e)
        {
            //bool used to see if a picture has been taken
            takePicture = true;

            ImageEncodingProperties imgFormat = ImageEncodingProperties.CreateJpeg();
            MemoryStream ms = new MemoryStream();

            if (captureManager != null)
            {
                //captures a photo and stores it in the MemoryStrem ms
                await captureManager.CapturePhotoToStreamAsync(imgFormat, ms.AsRandomAccessStream());

            }

            var currentApp = (App)App.Current;
            currentApp.CurrentImageRecog = ms;


            //displays the image taken and allows the user to accept or retake the picture
            await captureManager.StopPreviewAsync();
            showImageView();
            buttonAdjust();
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            return 0;
        }
        #endregion

        #region Prieview Image before Processing
        /// <summary>
        /// Gets the photo as a BitmapImage, rotates the image, and displays the image
        /// </summary>
        private void showImageView()
        {
            var currentApp = (App)App.Current;
            var ms = currentApp.CurrentImageRecog;

            if (!Object.ReferenceEquals(ms, null))
            {
                BitmapImage bmpImage = new BitmapImage();
                ms.Seek(0L, SeekOrigin.Begin);
                bmpImage.SetSource(ms.AsRandomAccessStream());
                rotatePreviewImage();
                // imagePreivew is a <Image> object defined in XAML 
                imagePreview.Source = bmpImage;
            }
        }

        private void buttonAdjust()
        {
            takePictureButton.Visibility = Visibility.Collapsed;
            acceptButton.Visibility = Visibility.Visible;
            rejectButton.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// called when the user accepts the picture
        /// rotates the image and sends it through VMHub processing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void acceptPicture_click(object sender, RoutedEventArgs e)
        {
            await acceptPicture();
        }

        private async Task<int> acceptPicture()
        {
            if (!inProcessing)
            {
                inProcessing = true;

                var currentApp = (App)App.Current;
                var ms = currentApp.CurrentImageRecog;
                if (Object.ReferenceEquals(ms, null))
                {
                    currentApp.CurrentRecogResult = "no image taken. ";
                    Frame.Navigate(typeof(WindowsApp.Views.RecogResultPage), "Took Picture");
                }
                else
                {
                    //doesn't allow user to try another request by removing the button
                    acceptButton.Visibility = Visibility.Collapsed;
                    rejectButton.Visibility = Visibility.Collapsed;
                    Loading.Visibility = Visibility.Visible;

                    InMemoryRandomAccessStream mrs = await ConvertTo(ms.ToArray());
                    //rotates the image and sends it through VMHub processing
                    Byte[] buf = await RotateImage(mrs);
                    await this.processRequestAfterRotate(buf);

                }

            }
            return 0;
        }

        /// <summary>
        /// sends the image through VMHub processing
        /// </summary>
        /// <param name="rotatedImage"></param>
        /// <returns></returns>
        private async Task<int> processRequestAfterRotate(byte[] rotatedImage)
        {
            var currentApp = (App)App.Current;
            currentApp.CurrentRecogResult = await App.VMHub.ProcessRequest(rotatedImage);
            Frame.Navigate(typeof(WindowsApp.Views.RecogResultPage), "Took Picture");
            return 0;
        }

        /// <summary>
        /// Converts a byte[] to an InMemoryRandomAccessStream
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public async Task<InMemoryRandomAccessStream> ConvertTo(byte[] bytes)
        {
            InMemoryRandomAccessStream random = new InMemoryRandomAccessStream();
            await random.WriteAsync(bytes.AsBuffer());
            random.Seek(0);
            return random;
         }

        /// <summary>
        /// gets the camera id. Allows the app to set the prefered camera to the back camera
        /// </summary>
        /// <param name="desiredCamera"></param>
        /// <returns></returns>
        private static async Task<DeviceInformation> GetCameraID(Windows.Devices.Enumeration.Panel desiredCamera)
        {
            // get available devices for capturing pictures
            DeviceInformation deviceID = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture))
                .FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredCamera);

            if (deviceID != null) return deviceID;
            else throw new Exception(string.Format("Camera of type {0} doesn't exist.", desiredCamera));
        }

        /// <summary>
        /// allows the user to retake the picture
        /// clears and resets the camera/page to its original state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void retakePicture_click(object sender, RoutedEventArgs e)
        {
            rejectButton.Visibility = Visibility.Collapsed;
            acceptButton.Visibility = Visibility.Collapsed;
            takePictureButton.Visibility = Visibility.Visible;

            if (captureManager != null)
            {
                captureManager.Dispose();
                captureManager = null;
                capturePreview.Source = null;
                imagePreview.Source = null;

            }
            takePicture = false;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped | DisplayOrientations.PortraitFlipped;
            await initCamera();
        }

        #endregion

    }

}


