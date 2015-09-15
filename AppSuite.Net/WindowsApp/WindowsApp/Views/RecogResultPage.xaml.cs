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
using Windows.UI;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.ApplicationModel.Activation;
using Windows.Storage.Streams;


// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace WindowsApp.Views
{
    /// <summary>
    /// Creates the page
    /// </summary>
    public sealed partial class RecogResultPage : Page
    {
        List<String> resultsList = new List<String>();
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private int count;
        private DispatcherTimer timer = new DispatcherTimer();

        internal static RecogResultPage Current;
        /// <summary>
        /// Constructor
        /// </summary>
        public RecogResultPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            Application.Current.Resuming += new EventHandler<object>(App_Resuming);
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            Current = this;
        }
        private void App_Resuming(Object sender, Object e)
        {
            //TODO: Refresh network data
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

         

         void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
           Frame.Navigate(typeof(OptionsPage));
           e.Handled = true;
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
       public async Task<String> UpdateResult()
        {
            var currentApp = (App)App.Current;

            var ms = currentApp.CurrentImageRecog;
            if (!Object.ReferenceEquals(ms, null))
            {
                
                // Get photo as a BitmapImage 
                BitmapImage bmpImage = new BitmapImage();

                ms.Seek(0L, SeekOrigin.Begin);
                await bmpImage.SetSourceAsync(ms.AsRandomAccessStream());

                // imagePreivew is a <Image> object defined in XAML 
                imageRecog.Source = bmpImage;
                
                
            }

            var resultString = currentApp.CurrentRecogResult;

                if (resultString.Contains(':') && resultString.Contains(';'))
                {
                    spliceResult(resultString);
                }
                else if (resultString.Contains("Reason:Request Entity Too Large"))
                {
                    String reason = "Sorry the request entity was too large";
                    String r2 = "Please try your request again";
                    resultsList.Add(reason);
                    resultsList.Add(r2);
                }

                else
                {
                    resultsList.Add(resultString);
                }

            ViewOfResults.ItemsSource = this.resultsList;
            return " ";
        }
       private void spliceResult(String resultString)
       {
           int lengthOfWholeString = resultString.Length;

           if (resultString.Length == 0)
           {
               return;

           }

           //call splice results
           //else return string
           int semicolon = 0;
           
           int endOfName = findColon(resultString);
           if (endOfName <= 0)
           {
               String error = "Sorry, there was an unexpected error";
               resultsList.Add(error);
               return;
           }
           String name = resultString.Substring(semicolon, endOfName);
           String num = resultString.Substring(endOfName + 1, 4);
           double result = 0;
           if (double.TryParse(num, out result))
           {
               result = result * 100;
           }
           String final = name + " " + result + "%";
           resultsList.Add(name + howManyDots(final)+ result + "%");
           int endOfResult = findEnd(resultString);
           if (endOfResult == -1)
           {
               return;
           }
           resultString = resultString.Substring(endOfResult + 1, resultString.Length - (endOfResult + 1));
           semicolon = findEnd(resultString);
    
           spliceResult(resultString);
       }


       //finds the length of the border in terms of periods
       private String dotCreater()
       {
           int totalDots = (int)(borderReferenceForWidth.ActualWidth / dotReference.ActualWidth) - 5 * (int)(dotReference.ActualWidth);
           String finishedDots = "";

           for (int i = 0; i < totalDots; i++)
           {
               finishedDots = finishedDots + ".";

           }
           return finishedDots;
       }
       private String howManyDots(string resultString)
       {
           
           String numbDotsInPic = dotCreater();
           int lengthOfDots = numbDotsInPic.Length;
           int length = resultString.Length;
           if(length >= lengthOfDots)
           {
               return " ";
           }
           int numSpaces = howManySpaces(resultString);
           int numUC = numSpaces + 1;
           int numLC = length - numUC - numSpaces;
           lengthOfDots = lengthOfDots - (numLC * 2 + numUC * 3 - (numSpaces + 1));
           String dots = " ";
           while (lengthOfDots > 0)
           {
               dots += ".";
               lengthOfDots--;
           }
           return dots;
       }
       private int howManySpaces(string resultString)
       {
           int count = 0;
           foreach (char letter in resultString)
           {
               if (Char.IsWhiteSpace(letter))
               {
                   count++;
               }
           }
           return count;
       }
       private int findColon(string resultString)
       {
           String temp = resultString;
           char colon = ':';
           return temp.IndexOf(colon);
       }

       private int findEnd(string resultString)
       {
           String temp = resultString;
           char semicolon = ';';
           int final = temp.IndexOf(semicolon);

           return final;
       } 
     
        #region NavigationHelper registration
/// <summary>
/// called when the page is navigated to
/// </summary>
/// <param name="e"></param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            this.navigationHelper.OnNavigatedTo(e);
            await UpdateResult();
        }

        private void rotateImage()
        { 
            RotateTransform rotate = new RotateTransform();
            rotate.Angle = 90;
            rotate.CenterX = 225;
            rotate.CenterY = 225;
            imageRecog.RenderTransform = rotate;
            
        }
        /// <summary>
        /// helps with navigation
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            var currentApp = (App)App.Current;
            currentApp.CurrentImageRecog = null;
            currentApp.CurrentRecogResult = null;
            LayoutRoot.Children.Clear();
      

            this.navigationHelper.OnNavigatedFrom(e);
           
        }

        #endregion

        private async void returnHome_Click(object sender, RoutedEventArgs e)
        {
            //ADD DISPATCH TO MAIN THREAD TYPE THING
            //make class and then call frame . navigate on dispatch ui
          var currentApp = (App)App.Current;
          currentApp.CurrentRecogResult = null;
          currentApp.CurrentImageRecog = null;
          Frame.Navigate(typeof(OptionsPage));
            Windows.UI.Core.CoreDispatcher dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                new Windows.UI.Core.DispatchedHandler(
                    () => Frame.Navigate(typeof(WindowsApp.Views.OptionsPage))));   
        }

        private void switchColor(object sender, RoutedEventArgs e)
        {
            Button b1 = (Button)sender;
            if(count==0)
            {
                b1.Background = new SolidColorBrush(Color.FromArgb(255, 153, 1, 0));
                count=1;
            }
            else if (count == 1)
            {
                b1.Background = new SolidColorBrush(Color.FromArgb(255, 107, 33, 68));
                count = 2;
            }
            else
            {
                b1.Background = new SolidColorBrush(Color.FromArgb(255, 73, 5, 64));
                count = 0;
            }

        }
    }
}
