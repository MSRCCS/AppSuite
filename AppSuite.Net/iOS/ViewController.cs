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
		MonitorServiceData.fs
  
	Description: 
		Class that defines the monitored status of a gateway server 

	Author:																	
 		Jin Li, Partner Research Manager
 		Microsoft Research, One Microsoft Way
 		Email: jinl@microsoft.com, Tel. (425) 703-8451
    Date:
        Aug. 2015
 *******************************************************************/
using System;
using UIKit;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ClientSuite.iOS
{
	public partial class ViewController : UIViewController
	{
		int count = 1;

		public ViewController (IntPtr handle) : base (handle)
		{		
		}


		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			// Code to start the Xamarin Test Cloud Agent
			#if ENABLE_TEST_CLOUD
			Xamarin.Calabash.Start ();
			#endif

			View.BackgroundColor =  UIColor.FromRGB( 0xfd, 0xe8, 0xd7); 

			// Perform any additional setup after loading the view, typically from a nib.
			//Button.AccessibilityIdentifier = "settingButton";
			//Button.TouchUpInside += delegate {
			//	var title = string.Format ("{0} clicks!", count++);
			//	Button.SetTitle (title, UIControlState.Normal);
			//};
		}

		public override void DidReceiveMemoryWarning ()
		{		
			base.DidReceiveMemoryWarning ();		
			// Release any cached data, images, etc that aren't in use.		
		}
	}
}
