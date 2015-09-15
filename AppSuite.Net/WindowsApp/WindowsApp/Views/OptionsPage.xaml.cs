﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.ApplicationModel.Activation;
using Windows.Storage.Streams;
using WindowsApp.Common;
using Windows.Graphics.Display;
using System.Threading.Tasks;
using Windows.UI;
using VMHubClientLibrary;
using System.Net.Http;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
namespace WindowsApp.Views
{
    /// <summary>
    /// Create the page
    /// </summary>
    public sealed partial class OptionsPage : Page, IFileOpenPickerContinuable
    {
        internal static OptionsPage Current;
        private readonly NavigationHelper navigationHelper;
        private static DependencyProperty FrameSessionStateKeyProperty =
          DependencyProperty.RegisterAttached("_FrameSessionStateKey", typeof(String), typeof(SuspensionManager), null);

        /// <summary>
        /// Initializing the Component
        /// </summary>
        public OptionsPage()
        {
            InitializeComponent();
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
/// called when navigate to the page
/// </summary>
/// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            this.navigationHelper.OnNavigatedTo(e);
        }
        protected  override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Options
        /// <summary>
        /// Navigates to different pages based on which circular button, defined in xaml, is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void goToCamera(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
       public void UploadPhoto(object sender, RoutedEventArgs e)
        {
            SuspensionManager.RegisterFrame(ScenarioFrame, "ScenarioFrame");

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            // Sets Frame to OptionsProxy Page
            if (!ScenarioFrame.Navigate(typeof(WindowsApp.Views.OptionsProxy)))
            {
                throw new Exception("Failed to create scenario list");
            }

            openPicker.PickSingleFileAndContinue();
            ScenarioFrame.Visibility = Visibility.Visible;

        }
        /// <summary>
        /// space holder
        /// </summary>
        /// <param name="args"></param>
        public void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args)
        {

        }
        private void PasteURL(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WindowsApp.Views.URLPage));
        }
        #endregion

        # region Settings
        /// <summary>
        /// Checks if the selected Gateway, Provider, and Domain are active
        /// The xaml buttons will turn green if the selection is active and red if the selection is inactive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void checkGatewayProviderDomainStatus(object sender, RoutedEventArgs e)
        {
           //theoretical working version
           try
            {
                await checkGateway();
            }
           catch (HttpRequestException ex)
            {
                string error = ex.Message;
                Frame.Navigate(typeof(NetworkError), error);
                return;
            }
        //working sample
            /*
            var checkNetwork = false;
            if(!checkNetwork)
            {
                string exception = "Network error";
                Frame.Navigate(typeof(NetworkError), exception);
                return;
            }
             * */
            await renderButtonColor();
             
        }
        public async Task renderButtonColor()
        {
            if (await checkGateway())
            {
                //green
                gatewayIdentifier.Background = new SolidColorBrush(Color.FromArgb(255, 55, 188, 97));
            }
            else
            {
                //red
                gatewayIdentifier.Background = new SolidColorBrush(Color.FromArgb(255, 204, 50, 50));
            }

            if (await checkProvider())
            {
                //green
                providerIdentifier.Background = new SolidColorBrush(Color.FromArgb(255, 55, 188, 97));
                if (await checkDomain())
                {
                    //green
                    domainIdentifier.Background = new SolidColorBrush(Color.FromArgb(255, 55, 188, 97));
                }
                else
                {
                    //red
                    domainIdentifier.Background = new SolidColorBrush(Color.FromArgb(255, 204, 50, 50));
                }
            }
            else
            {
                //red
                providerIdentifier.Background = new SolidColorBrush(Color.FromArgb(255, 204, 50, 50));
                domainIdentifier.Background = new SolidColorBrush(Color.FromArgb(255, 204, 50, 50));
            }


        }
        /// <summary>
        /// Gets a List of the active gateways
        /// checks if the currently selected gateway is in that List
        /// </summary>
        /// <returns>
        /// True if the selected gateway is in the List of active gateways
        /// False otherwise
        /// </returns>
        /// checkProvider() and checkDomain() perform in a similar way
        internal async Task<Boolean> checkGateway()
        {
                var activeGateways = await App.VMHub.GetActiveGateways();
                var currentGateway = App.VMHub.CurrentGateway;

                if (activeGateways.Equals(null) || currentGateway.Equals(null))
                    return false;

                foreach (var item in activeGateways)
                {
                    if ((item.HostName).Equals(App.VMHub.CurrentGateway))
                    {
                        return true;
                    }
                }
                return false;

            
        }

        internal async Task<Boolean> checkProvider()
        {
            try
            {
                var activeProviders = await App.VMHub.GetActiveProviders();
                var currentProvider = App.VMHub.CurrentProvider;
                if (activeProviders == null || currentProvider == null)
                    return false;
                foreach (var provider in activeProviders)
                {
                    if ((provider.RecogEngineName).Equals(currentProvider.RecogEngineName))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        internal async Task<Boolean> checkDomain()
        {
            try
            {
                var activeDomains = await App.VMHub.GetWorkingInstances();
                var currentDomain = App.VMHub.CurrentDomain;
                if (activeDomains == null || currentDomain == null)
                    return false;
                foreach (var domain in activeDomains)
                {
                    if (domain.Name.Equals(currentDomain.Name))
                    {
                        return true;
                    }

                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Navigate to the Select Gateway/Provider/Domain pages when the cooresponding option is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectGateway_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WindowsApp.Views.SelectGateway));
        }
        private void SelectProvider_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WindowsApp.Views.SelectProvider));
        }
        private void SelectDomain_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(WindowsApp.Views.SelectDomain));
        }
        #endregion

    }
}
