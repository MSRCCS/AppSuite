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
		RecogResultPage.xaml.cs
  
	Description: 
		A page showing recognition result

	Author:																	
 		Jin Li, Partner Research Manager
 		Microsoft Research, One Microsoft Way
    Date:
        June. 2015
 *******************************************************************/
using WindowsApp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using System.Text;

using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.ApplicationModel.Activation;
using Windows.Storage.Streams;


// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace WindowsApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PhotoLibraryResults : Page, IFileOpenPickerContinuable
    {
        private static DependencyProperty FrameSessionStateKeyProperty =
           DependencyProperty.RegisterAttached("_FrameSessionStateKey", typeof(String), typeof(SuspensionManager), null);
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private DispatcherTimer timer = new DispatcherTimer();

        public static PhotoLibraryResults Current;
        public PhotoLibraryResults()
        {
            this.InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += ContinuousUpdate;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            Current = this;
        }

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

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }
        /// <summary>
        /// Force to update recognition result
        /// </summary>
        /// 
        /* public void UpdateResult()
         {
             var currentApp = (App)App.Current;

             var ms = currentApp.CurrentImageRecog;
            
             if (!Object.ReferenceEquals(ms, null))
             {
                 // Get photo as a BitmapImage 
                 BitmapImage bmpImage = new BitmapImage();
                 // bmpImage.UriSource = new Uri(file.Path);
                 ms.Seek(0L, SeekOrigin.Begin);
                 bmpImage.SetSource(ms.AsRandomAccessStream());
                
                 // imagePreivew is a <Image> object defined in XAML 
                 imageRecog.Source = bmpImage;
    ;
             }

             var resultString = currentApp.CurrentRecogResult;
             if (String.IsNullOrEmpty(resultString))
             {
                 recogResult.Text = "is not returned";
             }
             else
             {
                 recogResult.Text = "is " + resultString;
             }
         }
         */

        private void ContinuousUpdate(Object sender, Object e)
        {
            //var currentApp = (App)App.Current;
            //var bResultArrived = currentApp.TryWaitForResult();
            //UpdateResult();
            //if (bResultArrived )
            //{
            //    timer.Stop();
            //}
            //else
            //{ 

            //}


        }
        private void selectPhoto_click()
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            // Sets Frame to OptionsProxy PAge
            if (!ScenarioFrame.Navigate(typeof(WindowsApp.Views.OptionsProxy)))
            {
                throw new Exception("Failed to create scenario list");
            }

            openPicker.PickSingleFileAndContinue();

        }
        public void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {
            



            //var fileBuf = await file.OpenReadAsync();

            //Byte[] fileBytes = null;
            //Windows.Storage.Streams.IBuffer fileBytes = null;
            /*var fileBuf = await file.OpenReadAsync();
            uint filesize = Convert.ToUInt32(fileBuf.Size);
 
            var ms = fileBuf.ReadAsync(fileBytes, filesize, Windows.Storage.Streams.InputStreamOptions.None);*/
            //var ms = new MemoryStream(orgImageBuf, 0, orgImageBuf.Length, false);
            //currentApp.CurrentImageRecog = ms;
            //currentApp.CurrentRecogResult = String.Format("Image is of {0}B", orgImageBuf.Length);
            //Frame.Navigate(typeof(WindowsApp.Views.RecogResultPage));
            //Byte[] buf = ms.ToArray();
            //currentApp.CurrentRecogResult = await App.VMHub.ProcessRequest(buf);
            /* var currentApp = (App)App.Current;
            
            
             IReadOnlyList<StorageFile> files = args.Files;
            
             if (files.Count > 0)
             {
                 //using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
                 /*{
                     using (IInputStream outputStream = fileStream.GetInputStreamAt(0))
                     {
                         using (DataReader dataReader = new DataReader(outputStream))
                         {
                             Byte[] imageBytes = null;
                             dataReader.ReadBytes(imageBytes);
                             dataReader.DetachStream();
                            // currentApp.CurrentRecogResult = await App.VMHub.ProcessRequest(imageBytes);
                         }
                     }
                 }
             }
             else
             {
                 return ;
             }*/

        }
        public async void ContinueFileSavePicker(FileSavePickerContinuationEventArgs args)
        {

            StorageFile file = args.File;
            if (file != null)
            {

                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                await FileIO.WriteTextAsync(file, file.Name);
                // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
            }
        }


        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            var check = e.Parameter as String;
            if (check != null && check.Equals("CapturedPhoto"))
            {
 

            }
            else
            {
                SuspensionManager.RegisterFrame(ScenarioFrame, "ScenarioFrame");
                selectPhoto_click();
            }
            // UpdateResult();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {


            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void returnHome_Click(object sender, RoutedEventArgs e)
        {

            Frame.Navigate(typeof(WindowsApp.Views.OptionsPage));


        }

        private void goToOptionsProxy(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WindowsApp.Views.OptionsProxy));

        }
    }
}
