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
		SelectProvider.xaml.cs
  
	Description: 
		Interface to select the provider

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
using vHub.Data;
using VMHubClientLibrary;
using System.Threading.Tasks;
using WindowsApp.Common;

namespace WindowsApp.Views
{
    /// <summary>
    /// Create the page
    /// </summary>
    public sealed partial class SelectProvider : Page
    {
        private NavigationHelper navigationHelper;
        private RecogEngine OldProvider { get; set; }
        private Int32 MinItemShowed { get; set; }
        private Int32 NumberItemShowed { get; set; }
        private Int32 SelectedIndexValue { get; set; }
        private ConcurrentDictionary<Guid, RecogEngine> ProviderAdded { get; set; }
        internal Boolean hasSaved = false;
        /// <summary>
        /// Provider list that is returned by Provider
        /// </summary>
        internal ObservableCollection<RecogEngine> ProviderList { get; set; }
        internal RecogEngine LastProvider { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>
        public SelectProvider()
        {
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            this.InitializeComponent();
        }

        #region Navigation
        internal NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ProviderList = new ObservableCollection<RecogEngine>();
            ProviderAdded = new ConcurrentDictionary<Guid, RecogEngine>();

            ViewOfProvider.ItemsSource = this.ProviderList;
            LastProvider = null;
            SelectedIndexValue = 0;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var ScreenHeight = Window.Current.Bounds.Height;
            var DeviceHeight = ScreenHeight * scaleFactor;
            NumberItemShowed = (int)Math.Floor((ScreenHeight - 220.0) / 140.0 + 0.5);
            MinItemShowed = 0;
            var currentApp = (App)App.Current;
            OldProvider = currentApp.CurrentProvider;
            var result = await UpdateProviderInfo();

            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        #region Provider Operations
        private void AddProvider(RecogEngine entry)
        {
            var id = entry.RecogEngineID;
            RecogEngine oldServer;
            if (ProviderAdded.TryRemove(id, out oldServer))
            {
                this.ProviderList.Remove(oldServer);
            }
            var priorInfo = this.ProviderAdded.GetOrAdd(id, entry);
            if (Object.ReferenceEquals(priorInfo, entry))
            {
                this.ProviderList.Add(entry);
            }
        }

        private async Task<int> UpdateProviderInfo()
        {
            string errorMessage = "";
            try
            {
                var currentApp = (App)App.Current;

                this.CurrentProvider.Text = "";
                App.VMHub.CurrentProvider = currentApp.CurrentProvider;
                if (!Object.ReferenceEquals(currentApp.CurrentProvider, null))
                {
                    this.CurrentProvider.Text = currentApp.CurrentProvider.RecogEngineName;
                    AddProvider(currentApp.CurrentProvider);
                    this.LastProvider = currentApp.CurrentProvider;
                }

                {
                    var lst = await App.VMHub.GetActiveProviders();
                    foreach (var item in lst)
                    {
                        if (lst == null)
                        {
                            return 0;
                        }
                        AddProvider(item);
                    }
                }
                var cnt = (double)(this.ProviderList.Count);
                if (String.IsNullOrEmpty(CurrentProvider.Text) && cnt > 0)
                {
                    LastProvider = ProviderList[0];
                    CurrentProvider.Text = LastProvider.RecogEngineName;
                }
                this.ViewOfProvider.Height = cnt * 140.0;
                return 0;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            NetworkError.SetError(errorMessage);
            Frame.Navigate(typeof(NetworkError));
            return 0;
        }

        private void SelectedProvider_Event(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as RecogEngine;
                this.CurrentProvider.Text = item.RecogEngineName;
                var idx = ProviderList.IndexOf(item);
                var cnt = ProviderList.Count;
                if (idx >= 0)
                {

                    var minShow = MinItemShowed;
                    var maxShow = MinItemShowed + NumberItemShowed - 1;
                    var midShow = (minShow + maxShow) / 2;
                    var diff = idx - midShow;
                    if (diff != 0)
                    {
                        var newMinShow = MinItemShowed;
                        RecogEngine scrollItem = null;
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
                                scrollItem = ProviderList[newMinShow];
                                scrollidx = newMinShow;
                            }
                            else
                            {
                                scrollItem = ProviderList[maxShow];
                                scrollidx = maxShow;
                            }
                            MinItemShowed = newMinShow;
                            this.ViewOfProvider.ScrollIntoView(scrollItem);

                        }
                    }
                }
            }
        }

        private void findSelectedProvider()
        {
            RecogEngine selected = null;
            foreach (var item in ProviderAdded)
            {
                if (String.Compare(this.CurrentProvider.Text, item.Value.RecogEngineName, StringComparison.OrdinalIgnoreCase) == 0)
                    selected = item.Value;
            }

            if (!Object.ReferenceEquals(selected, null))
            {
                var currentApp = (App)App.Current;
                currentApp.CurrentProvider = selected;
            }
        }
        #endregion

        #region Buttons

        /// <summary>
        /// Button click to save Provider selection
        /// </summary>
        private void SaveProviderSelection_Click(object sender, RoutedEventArgs e)
        {
            hasSaved = true;
            findSelectedProvider();
            var currentApp = (App)App.Current;
            currentApp.SaveProviderInfo();
        }

        /// <summary>
        /// Button click to save Provider selection
        /// </summary>
        private async void UpdateProviderSelection_Click(object sender, RoutedEventArgs e)
        {
            findSelectedProvider();
            await UpdateProviderInfo();
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

