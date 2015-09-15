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
		SelectDomain.xaml.cs
  
	Description: 
		Interface to select the Domain

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
using WindowsApp.Common;
using WindowsApp.Data;
using vHub.Data;
using VMHubClientLibrary;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using WindowsApp;
using WindowsApp.Views;

namespace WindowsApp.Views
{
    /// <summary>
    /// create the page
    /// </summary>
    public sealed partial class SelectDomain : Page
    {
        private RecogInstance OldDomain { get; set; }
        private Int32 MinItemShowed { get; set; }
        private Int32 NumberItemShowed { get; set; }
        private Int32 SelectedIndexValue { get; set; }
        private ConcurrentDictionary<Guid, RecogInstance> DomainAdded { get; set; }
        private ObservableCollection<RecogInstance> DomainList { get; set; }
        private Guid LastDomainID { get; set; }
        private RecogInstance LastDomain { get; set; }
        private Boolean hasSaved = false;
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary defaultViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        public static SelectDomain Current;
        /// <summary>
        /// Constructor
        /// </summary>
        public SelectDomain()
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
            DomainList = new ObservableCollection<RecogInstance>();
            DomainAdded = new ConcurrentDictionary<Guid, RecogInstance>();
            ViewOfDomain.ItemsSource = this.DomainList;
            LastDomain = null;
            SelectedIndexValue = 0;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var ScreenHeight = Window.Current.Bounds.Height;
            var DeviceHeight = ScreenHeight * scaleFactor;
            NumberItemShowed = (int)Math.Floor((ScreenHeight - 220.0) / 90.0 + 0.5);
            MinItemShowed = 0;
            var currentApp = (App)App.Current;
            OldDomain = currentApp.CurrentDomain;
            UpdateDomainInfo();
            this.navigationHelper.OnNavigatedTo(e);
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        # region Domain Operations
        private async void UpdateDomainInfo()
        {
            try
            {


                var currentApp = (App)App.Current;
                App.VMHub.CurrentDomain = currentApp.CurrentDomain;

                this.CurrentDomain.Text = "";
                if (!Object.ReferenceEquals(currentApp.CurrentDomain, null))
                {
                    this.CurrentDomain.Text = currentApp.CurrentDomain.Name;
                    AddDomain(currentApp.CurrentDomain);
                    this.LastDomain = currentApp.CurrentDomain;
                }

                {
                    var lst = await App.VMHub.GetWorkingInstances();
                    foreach (var item in lst)
                    {
                        AddDomain(item);
                    }
                }
                var cnt = (double)(this.DomainList.Count);
                if (String.IsNullOrEmpty(CurrentDomain.Text) && cnt > 0)
                {
                    LastDomain = DomainList[0];
                    LastDomainID = LastDomain.ServiceID;
                    CurrentDomain.Text = LastDomain.EngineName;
                    CurrentDomain.Text = LastDomain.Version.ToString();
                }
                this.ViewOfDomain.Height = cnt * 90.0;
            }
            catch (Exception e)
            {
                string error = e.Message.ToString();
                Frame.Navigate(typeof(NetworkError), error);
            }
        }

        private void AddDomain(RecogInstance entry)
        {
            var id = entry.ServiceID;
            RecogInstance oldServer;
            if (DomainAdded.TryRemove(id, out oldServer))
            {
                this.DomainList.Remove(oldServer);
            }
            var priorInfo = this.DomainAdded.GetOrAdd(id, entry);
            if (Object.ReferenceEquals(priorInfo, entry))
            {
                this.DomainList.Add(entry);
            }
        }

        private void SelectedDomain_Event(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as RecogInstance;
                this.CurrentDomain.Text = item.Name;
                var idx = DomainList.IndexOf(item);
                var cnt = DomainList.Count;
                if (idx >= 0)
                {

                    var minShow = MinItemShowed;
                    var maxShow = MinItemShowed + NumberItemShowed - 1;
                    var midShow = (minShow + maxShow) / 2;
                    var diff = idx - midShow;
                    if (diff != 0)
                    {
                        var newMinShow = MinItemShowed;
                        RecogInstance scrollItem = null;
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
                                scrollItem = DomainList[newMinShow];
                                scrollidx = newMinShow;
                            }
                            else
                            {
                                scrollItem = DomainList[maxShow];
                                scrollidx = maxShow;
                            }
                            MinItemShowed = newMinShow;
                            this.ViewOfDomain.ScrollIntoView(scrollItem);

                        }
                    }
                }
            }
        }

        private void findSelectedDomain()
        {
            RecogInstance selected = null;
            foreach (var item in DomainAdded)
            {
                if (String.Compare(this.CurrentDomain.Text, item.Value.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    selected = item.Value;
            }

            if (!Object.ReferenceEquals(selected, null))
            {
                var currentApp = (App)App.Current;
                currentApp.CurrentDomain = selected;
            }
        }
        #endregion

        #region Buttons
        /// <summary>
        /// Button click to update the domain list
        /// </summary>
        private void UpdateDomainSelection_Click(object sender, RoutedEventArgs e)
        {
            findSelectedDomain();
            UpdateDomainInfo();
        }

        /// <summary>
        /// Button click to save Domain selection
        /// </summary>
        private void SaveDomainSelection_Click(object sender, RoutedEventArgs e)
        {
            hasSaved = true;
            findSelectedDomain();
            var currentApp = (App)App.Current;
            currentApp.SaveDomainInfo();
        }
        /// <summary>
        /// Navigate back to OptionsPage
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
              
                SpeechBubble.Visibility = Visibility.Visible;
                await Task.Delay(1500);
                SpeechBubble.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

    }

}

