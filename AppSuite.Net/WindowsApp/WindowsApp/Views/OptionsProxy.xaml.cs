
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
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Imaging;
using System.Runtime;
using System.Runtime.Serialization;
//using System.Runtime.Serialization.Formatters;
//using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using WindowsApp.Views;
//I want to use this
//using Windows.Phone.Media.Capture;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace WindowsApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// public static OptionsProxy Current;

    public sealed partial class OptionsProxy : Page, IFileOpenPickerContinuable
    {
        internal static OptionsProxy Current;
        OptionsPage rootPage = OptionsPage.Current;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        /// <summary>
        /// Initializing the Component
        /// </summary>
        public OptionsProxy()
        {
            this.InitializeComponent();
            Current = this;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        #region Navigation
        /// <summary>
        /// call to navigation helper class
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
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }
        /// <summary>
        /// helps with page navigation
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }
        /// <summary>
        /// helps with page navigation
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }
        #endregion

        #region Choose Library Photo
        /// <summary>
        /// resumes the application after choosing a photo from the photo library
        /// </summary>
        /// <param name="args"></param>
        public async void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            if (args.Files.Count > 0)
            {
                MemoryStream stream;
                var currentApp = (App)App.Current;

                IReadOnlyList<StorageFile> files = args.Files;

                var ms = await ReadFile(args.Files[0]);

                if (Object.ReferenceEquals(ms, null))
                {
                    currentApp.CurrentRecogResult = "no image selected. ";
                }
                else
                {
                    stream = new MemoryStream(ms);
                    currentApp.CurrentImageRecog = stream;
                    Byte[] buf = await ResizeImage(stream, 256);
                    try
                    {
                        await this.processRequestAfterRotate(buf);
                    }
                    catch (Exception e)
                    {
                        string error = e.Message.ToString();
                        Frame.Navigate(typeof(NetworkError), error);
                        return;
                    }
                }
            }
            else
                Frame.Navigate(typeof(OptionsPage));
        }

        private async Task<int> processRequestAfterRotate(byte[] buf)
        {
            var currentApp = (App)App.Current;
            currentApp.CurrentRecogResult = await App.VMHub.ProcessRequest(buf);
            Frame.Navigate(typeof(WindowsApp.Views.RecogResultPage), "Upload Picture");
            return 0;
        }

        //adjusts the storage file into a byte array for processing
        internal async Task<byte[]> ReadFile(StorageFile file)
        {
            byte[] fileBytes = null;
            using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];
                using (DataReader reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }
            return fileBytes;
        }

           async private Task<byte[]> ResizeImage(MemoryStream ms, uint maxImageSize)
        {
            MemoryStream temp = ms;
            IRandomAccessStream ras = temp.AsRandomAccessStream();
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(ras).AsTask().ConfigureAwait(false);
                uint w = decoder.PixelWidth, h = decoder.PixelHeight;
                if (w > maxImageSize || h > maxImageSize)
                {
                    if (w > h)
                    {
                        w = maxImageSize;
                        h = decoder.PixelHeight * maxImageSize / decoder.PixelWidth;
                    }
                    else
                    {
                        w = decoder.PixelWidth * maxImageSize / decoder.PixelHeight;
                        h = maxImageSize;
                    }
                }
                BitmapTransform transform = new BitmapTransform() { ScaledHeight = h, ScaledWidth = w };
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);
                byte [] pixels = pixelData.DetachPixelData();
                InMemoryRandomAccessStream encoded = new InMemoryRandomAccessStream();
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, encoded);
                encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, w, h, 96, 96, pixels);
                await encoder.FlushAsync().AsTask().ConfigureAwait(false);
                encoded.Seek(0);
                byte[] outBytes = new byte[encoded.Size];
                await encoded.AsStream().ReadAsync(outBytes, 0, outBytes.Length);
                return outBytes;         
        }
        #endregion
    }
}
