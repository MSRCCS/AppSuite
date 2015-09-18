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


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace WindowsApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PrivacyPolicy : Page
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public PrivacyPolicy()
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
            
        }
        /// <summary>
        /// run the tutorial if the user accepts the privacy policy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runTutorial(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(tutorialSkip));
        }
        /// <summary>
        /// exits the application if the user does not accept the privacy policy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }
    }
}
