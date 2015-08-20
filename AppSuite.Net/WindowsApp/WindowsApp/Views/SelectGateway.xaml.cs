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
		SelectGateway.xaml.cs
  
	Description: 
		Interface to select gateway server

	Author:																	
 		Jin Li, Partner Research Manager
 		Microsoft Research, One Microsoft Way
    Date:
        June. 2015
 *******************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
using Windows.Graphics.Display;
using Prajna.Service.CoreServices.Data;
using VMHubClientLibrary;
using System.Threading.Tasks;
using WindowsApp.Common;
using WindowsApp.Data;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Media.Devices;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Imaging;
using WindowsApp.Views;

namespace WindowsApp.Views
{
    /// <summary>
    /// Create the page
    /// </summary>
    public sealed partial class SelectGateway : Page
    {
        private String OldGateway { get; set; }
        private Int32 MinItemShowed { get; set; }
        private Int32 NumberItemShowed { get; set; }
        private Int32 SelectedIndexValue { get; set; }
        private ConcurrentDictionary<String, OneServerInfo> GatewayAdded { get; set; }
        internal ObservableCollection<OneServerInfo> GatewayList { get; set; }
        internal String LastGateway { get; set; }
        internal Boolean hasSaved = false;
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        internal static SelectGateway Current;
        /// <summary>
        /// Constructor
        /// </summary>
        public SelectGateway()
        {
            this.InitializeComponent();
            Current = this;

            this.NavigationCacheMode = NavigationCacheMode.Required;           
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        #region Navigation
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
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-1");
           // this.DefaultViewModel[FirstGroupName] = sampleDataGroup;
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
            GatewayList = new ObservableCollection<OneServerInfo>();
            GatewayAdded = new ConcurrentDictionary<String, OneServerInfo>(StringComparer.OrdinalIgnoreCase);
            ViewOfGateway.ItemsSource = this.GatewayList;
            LastGateway = null;
            SelectedIndexValue = 0;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var ScreenHeight = Window.Current.Bounds.Height;
            var DeviceHeight = ScreenHeight * scaleFactor;
            NumberItemShowed = (int)Math.Floor((ScreenHeight - 220.0) / 60.0 + 0.5);
            MinItemShowed = 0;
            var currentApp = (App)App.Current;
            OldGateway = currentApp.CurrentGateway;
            UpdateGatewayInfo();
            this.navigationHelper.OnNavigatedTo(e);
        }


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Gateway Operations

        private void AddGateway(String hostname, String info)
        {
            var serverInfo = new OneServerInfo();
            serverInfo.HostName = hostname;
            serverInfo.HostInfo = info;
            OneServerInfo oldServer;
            if (GatewayAdded.TryRemove(hostname, out oldServer))
            {
                this.GatewayList.Remove(oldServer);
            }
            var priorInfo = this.GatewayAdded.GetOrAdd(hostname, serverInfo);
            if (Object.ReferenceEquals(priorInfo, serverInfo))
            {
                this.GatewayList.Add(serverInfo);
            }
        }

        private async void UpdateGatewayInfo()
        {
            var currentApp = (App)App.Current;
            this.CurrentGateway.Text = currentApp.CurrentGateway;
            App.VMHub.CurrentGateway = currentApp.CurrentGateway;
            foreach (var kv in currentApp.GatewayCollection)
            {
                AddGateway(kv.Key, kv.Value);
            }

            if (String.IsNullOrEmpty(this.LastGateway))
            {
                this.LastGateway = currentApp.CurrentGateway;
            }

            
                var lst = await App.VMHub.GetActiveGateways();

                foreach (var item in lst)
                {
                    AddGateway(item.HostName, item.HostInfo);
                    currentApp.GatewayCollection.GetOrAdd(item.HostName, item.HostInfo);
                }
           
            var cnt = (double)(this.GatewayList.Count);
            this.ViewOfGateway.Height = cnt * 60.0;

        }

        private void SelectedGateway_Event(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as OneServerInfo;
                this.CurrentGateway.Text = item.HostName;
                var idx = GatewayList.IndexOf(item);
                var cnt = GatewayList.Count;
                if (idx >= 0)
                {

                    var minShow = MinItemShowed;
                    var maxShow = MinItemShowed + NumberItemShowed - 1;
                    var midShow = (minShow + maxShow) / 2;
                    var diff = idx - midShow;
                    if (diff != 0)
                    {
                        var newMinShow = MinItemShowed;
                        OneServerInfo scrollItem = null;
                        if (diff < 0)
                            newMinShow = Math.Max(0, MinItemShowed + diff);
                        else
                        {
                            maxShow = Math.Min(cnt - 1, MinItemShowed + diff + NumberItemShowed - 1);
                            newMinShow = Math.Max(0, maxShow - NumberItemShowed + 1);
                        }
                        if (newMinShow != MinItemShowed)
                        {

                            int scrollidx = 0;
                            if (diff < 0)
                            {
                                scrollItem = GatewayList[newMinShow];
                                scrollidx = newMinShow;
                            }
                            else
                            {
                                scrollItem = GatewayList[maxShow];
                                scrollidx = maxShow;
                            }
                            MinItemShowed = newMinShow;
                            this.ViewOfGateway.ScrollIntoView(scrollItem);

                        }
                    }
                }
            }
        }

        #endregion

        #region Buttons
        /// <summary>
        /// Button click to update gateway selection
        /// </summary>
        private void UpdateGatewaySelection_Click(object sender, RoutedEventArgs e)
        {
            var currentApp = (App)App.Current;
            currentApp.CurrentGateway = this.CurrentGateway.Text;
            UpdateGatewayInfo();

        }

        /// <summary>
        /// Button click to save gateway selection
        /// </summary>
        private void SaveGatewaySelection_Click(object sender, RoutedEventArgs e)
        {
            hasSaved = true;
            var currentApp = (App)App.Current;
            currentApp.SaveGatewayInfo(this.CurrentGateway.Text, GatewayList.ToList());
        }

        /// <summary>
        ///  Navigate back to OptionsPage
        /// </summary>
        private async void goToHomePage(object sender, RoutedEventArgs e)
        {
            //Check if the user has saved the domain selection
            if (hasSaved)
            {
                hasSaved = false;
                Frame.Navigate(typeof(OptionsPage));
            }
            else
            {
                //If the selection has not been saved, make a speechbuble visible 
                //that says "Dont Forget to Save"
                SpeechBubble.Visibility = Visibility.Visible;
                await Task.Delay(1500);
                SpeechBubble.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

    }
}
