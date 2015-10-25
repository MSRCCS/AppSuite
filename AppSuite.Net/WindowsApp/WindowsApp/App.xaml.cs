using WindowsApp.Common;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Threading;
using Windows.Phone.UI.Input;
using Prajna.Services.Vision.Data;
using Prajna.AppLibrary;
using WindowsApp.Views;



// The Pivot Application template is documented at http://go.microsoft.com/fwlink/?LinkID=391641

namespace WindowsApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;
        internal static String DefaultGateway = "vhub.trafficmanager.net"; // "vhub.trafficmanager.net"; 
        internal static Guid CustomerID = Guid.Empty;
        internal static String CustomerKey = "SecretKeyUsed";

        internal interface IActivatedArgs { }

        internal static GatewayHttpInterface VMHub = new GatewayHttpInterface(App.DefaultGateway, App.CustomerID, App.CustomerKey);
        /// <summary>
        /// Current gateway to be contacted
        /// </summary>
        public String CurrentGateway { get; set; }

        internal String TutorialRun { get; set; }

        /// <summary>
        /// Current provider to be used
        /// </summary>
        public RecogEngine CurrentProvider { get; set; }

        /// <summary>
        /// Current Recognition Domain 
        /// </summary>
        public RecogInstance CurrentDomain { get; set; }
        /// <summary>
        /// A set of gateways to be choosed
        /// </summary>
        public ConcurrentDictionary<String, String> GatewayCollection { get; set; }
        /// <summary>
        /// A memory stream that contains the current image being recognized. 
        /// </summary>
        public MemoryStream CurrentImageRecog { get; set; }
        /// <summary>
        /// Current image recongition result
        /// </summary>
        public String CurrentRecogResult { get; set; }
        /// <summary>
        /// see https://msdn.microsoft.com/en-us/library/windows/apps/xaml/dn614994.aspx
        /// </summary>
        public ContinuationManager ContinuationManager { get; private set; }

#if WINDOWS_PHONE_APP
        //ContinuationManager continuationManager;
#endif
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            //Initializing the component
            this.InitializeComponent();

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape | DisplayOrientations.PortraitFlipped | DisplayOrientations.LandscapeFlipped;

            this.Suspending += this.OnSuspending;

            // JinL: my code
            this.GatewayCollection = new ConcurrentDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            LoadGatewayInfo();
            LoadProviderInfo();
            LoadDomainInfo();
            LoadTutorialInfo();
        }


        private ApplicationDataContainer GetStorageSetting()
        {
            return Windows.Storage.ApplicationData.Current.LocalSettings;
        }

        /// <summary>
        /// Load information of gateway for Local Storage
        /// </summary>
        public void LoadGatewayInfo()
        {
            var localSetting = GetStorageSetting();
            try
            {
                if (localSetting.Values.ContainsKey(GatewayHttpInterface.GatewayCollectionKey))
                {
                    var buf = localSetting.Values[GatewayHttpInterface.GatewayCollectionKey] as byte[];
                    var gatewayCollection = GatewayHttpInterface.DecodeFromBytes<OneServerInfo[]>(buf);
                    foreach (var info in gatewayCollection)
                    {
                        GatewayCollection.GetOrAdd(info.HostName, info.HostInfo);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("LoadGatewayInfo fails with exception {0}", e);
                // Swallow exception if the store can not be used. 
            }

            if (localSetting.Values.ContainsKey(GatewayHttpInterface.GatewayKey))
                CurrentGateway = localSetting.Values[GatewayHttpInterface.GatewayKey].ToString();
            else
                CurrentGateway = App.DefaultGateway;
            GatewayCollection.GetOrAdd(this.CurrentGateway, "Default");
            VMHub.CurrentGateway = this.CurrentGateway;

        }
        /// <summary>
        /// Save information of gateway to local storage
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="gatewayCollection"></param>
        public void SaveGatewayInfo(String gateway, List<OneServerInfo> gatewayCollection)
        {
            var localSetting = GetStorageSetting();
            localSetting.Values[GatewayHttpInterface.GatewayKey] = gateway;
            var arr = gatewayCollection.ToArray();
            localSetting.Values[GatewayHttpInterface.GatewayCollectionKey] = GatewayHttpInterface.EncodeToBytes<OneServerInfo[]>(arr);
            CurrentGateway = gateway;
            VMHub.CurrentGateway = gateway;
            GatewayCollection.Clear();
            foreach (var info in gatewayCollection)
            {
                GatewayCollection.GetOrAdd(info.HostName, info.HostInfo);
            }
            GatewayCollection.GetOrAdd(this.CurrentGateway, "Default");
        }

        /// <summary>
        /// loads the provider information
        /// </summary>
        private void LoadProviderInfo()
        {
            var localSetting = GetStorageSetting();
            CurrentProvider = null;

            try
            {
                if (localSetting.Values.ContainsKey(GatewayHttpInterface.ProviderKey))
                {
                    var buf = localSetting.Values[GatewayHttpInterface.ProviderKey] as byte[];
                    CurrentProvider = GatewayHttpInterface.DecodeFromBytes<RecogEngine>(buf);
                    VMHub.CurrentProvider = CurrentProvider;
                }
                else
                {
                    CurrentProvider = null;
                    VMHub.CurrentProvider = null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("LoadGatewayInfo fails with exception {0}", e);
                // Swallow exception if the store can not be used. 
            }

        }

        /// <summary>
        /// saves the provider information
        /// </summary>
        internal void SaveProviderInfo()
        {
            if (!Object.ReferenceEquals(CurrentProvider, null))
            {
                var localSetting = GetStorageSetting();
                localSetting.Values[GatewayHttpInterface.ProviderKey] = GatewayHttpInterface.EncodeToBytes<RecogEngine>(CurrentProvider);
                VMHub.CurrentProvider = CurrentProvider;
            }
        }

        /// <summary>
        /// loads the domain information
        /// </summary>
        private void LoadDomainInfo()
        {
            var localSetting = GetStorageSetting();
            CurrentDomain = null;
            try
            {
                if (localSetting.Values.ContainsKey(GatewayHttpInterface.DomainKey))
                {
                    var buf = localSetting.Values[GatewayHttpInterface.DomainKey] as byte[];
                    CurrentDomain = GatewayHttpInterface.DecodeFromBytes<RecogInstance>(buf);
                    VMHub.CurrentDomain = CurrentDomain;
                }
                else
                {
                    CurrentDomain = null;
                    VMHub.CurrentDomain = null;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("LoadDomainInfo fails with exception {0}", e);
                // Swallow exception if the store can not be used. 
            }

        }

        /// <summary>
        /// saves the domain information
        /// </summary>
        internal void SaveDomainInfo()
        {
            if (!Object.ReferenceEquals(CurrentDomain, null))
            {
                var localSetting = GetStorageSetting();
                localSetting.Values[GatewayHttpInterface.DomainKey] = GatewayHttpInterface.EncodeToBytes<RecogInstance>(CurrentDomain);
                VMHub.CurrentDomain = CurrentDomain;
            }
        }
        // Save user information whether the tutorial has been run
        internal void saveTutorialInfo()
        {
            
            if (!Object.ReferenceEquals(TutorialRun,null))
            {
                var localSetting = GetStorageSetting();
                localSetting.Values[GatewayHttpInterface.tutorialShown] = "Yes";

            }
        }
        //loads the tutorial information
        internal void LoadTutorialInfo()
        {
            var localSetting = GetStorageSetting();
            
            try
            {
                string tutorial = localSetting.Values[GatewayHttpInterface.tutorialShown] as String;
                TutorialRun = tutorial;

            }
            catch (Exception e)
            {
                Debug.WriteLine("TutorialRun failed with exception",e);
            }
        }
        
        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {

                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
                
                if (TutorialRun == "Yes")
                {
                    if (!rootFrame.Navigate(typeof(OptionsPage), e.Arguments))
                    {

                        throw new Exception("Failed to create initial page");
                    }
                }
                else
                {
                    TutorialRun = "Yes";
                    if (!rootFrame.Navigate(typeof(PrivacyPolicy), e.Arguments))
                    {

                        throw new Exception("Failed to create initial page");
                    }
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

     
        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        private Frame CreateRootFrame()
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }
      
        private async Task RestoreStatusAsync(ApplicationExecutionState previousExecutionState)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (previousExecutionState == ApplicationExecutionState.Terminated)
            {
                // Restore the saved session state only when appropriate
                try
                {
                    await SuspensionManager.RestoreAsync();
                }
                catch (SuspensionManagerException)
                {
                    //Something went wrong restoring state.
                    //Assume there is no state and continue
                }
            }
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Handle OnActivated event to deal with File Open/Save continuation activation kinds
        /// </summary>
        /// <param name="e">Application activated event arguments, it can be casted to proper sub-type based on ActivationKind</param>
        protected async override void OnActivated(IActivatedEventArgs e)
        {            
            if (e.Kind.Equals(ActivationKind.PickFileContinuation))
            {
               
                base.OnActivated(e);

                ContinuationManager = new ContinuationManager();

                Frame rootFrame = CreateRootFrame();
                await RestoreStatusAsync(e.PreviousExecutionState);

                if (rootFrame.Content == null)
                {

                    rootFrame.Navigate(typeof(OptionsPage));
                }

                var continuationEventArgs = e as IContinuationActivatedEventArgs;

                if (continuationEventArgs != null)
                {
                    Frame scenarioFrame = OptionsPage.Current.FindName("ScenarioFrame") as Frame;

                    if (scenarioFrame != null)
                    {
                        // Call ContinuationManager to handle continuation activation
                        // pass the frame that it is the options page
                        ContinuationManager.Continue(continuationEventArgs, scenarioFrame);
                    }

                }
                
                Window.Current.Activate();

            }
            else {
                
            }
           
        }
   
#endif
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

    }
}
