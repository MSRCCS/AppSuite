//
//  AppInterface.swift
//  AppLibrary
//
//  Description:
//      App Library for VM Hub
//
//  Created by jinl on 8/23/15.
//  Copyright © 2015 Mighty Dog. All rights reserved.
//

import Foundation

/// Service status of a certain object (gateway, provider, etc)
internal class ServiceState
{
    /// Whether the service is Live at last observation
    private var Live = false
    /// Last time when the service state is updated
    /// Note that with the current implementation, CACurrentMediaTime may only be the up time of the iOS device. 
    /// It may be fine with the ServiceState check though.
    private var Ticks = CACurrentMediaTime()
    /// If this time has passed, rechecked the liveness of the system
    var CheckForLivenessInMilliseconds = 30000
    /// Initialize an instance of the ServiceState. We always initialize the Liveness to false,
    /// and the time of check to be one day old. This will trigger rechecking of the state.
    init( )
    {
        Live = false;
        Ticks = CACurrentMediaTime() - Double(CheckForLivenessInMilliseconds) / 1000.0
    }
    /// Set the state of the liveness of service
    private func SetState( bLive : Bool)
    {
        Live = bLive;
        Ticks = CACurrentMediaTime()
    }
    
    /// State of the service state
    var State : Bool
    {
        get
        {
            return Live;
        }
        set( newState )
        {
            SetState( newState);
        }
    }
    /// whether need to check the state
    internal func NeedsToCheck() -> Bool
    {
        if (Live)
        {
            return false;
        }
        else
        {
            let ticksCur = CACurrentMediaTime()
            return ((ticksCur - Ticks) * 1000.0 >= Double (CheckForLivenessInMilliseconds))
        }
    
    }
}

/// Manage Http Request
public class OneHttpWebRequest {
    public init( urlInput: NSURL?, reqCompletionHandler: NSString? -> Void )
    {
        if let url = urlInput
        {
        let req = NSURLRequest(URL: url )
        NSURLConnection.sendAsynchronousRequest( req,
            queue: NSOperationQueue.mainQueue(),
            completionHandler:
            { ( response: NSURLResponse?, data: NSData?, error:NSError? ) -> Void in
                
                    if error != nil || data == nil {
                        print(error) // failed to complete request
                        reqCompletionHandler(nil)
                        return
                    }
                    
                    do
                    {
                        let go = NSString( data: data!, encoding: NSUTF8StringEncoding )
                        reqCompletionHandler(go)
                    }
        } )
        }
        else
        {
            print("OneHttpWebRequest: \(urlInput) is not a URL")
            reqCompletionHandler(nil)
        }
    }
}

/// GatewayHttpInterface manages connection to the hub
public class GatewayHttpInterface {
    
    /// Page to check when validating internet connectivity
    private static var DefaultCheckSite = "www.bing.com"
    
    /// Default Gateway Used
    private var DefaultGateway = "vm-hub.trafficmanager.net"
    
    /// Default Address to get the status pag
    private var DefaultPath = "/web/info"
    
    /// Customer ID to be used
    private var CustomerIDString = "00000000-0000-0000-0000-000000000000"
    
    /// Customer Key 
    public var CustomerKey = ""
    
    public var ErrorMessage : String?
    public var ExceptionMessage : String?
    
    private var NetworkStatus = AppLibrary.ServiceState()
    private var GatewayStatus = AppLibrary.ServiceState()
    private var ProviderStatus = AppLibrary.ServiceState()
    private var InstanceStatus = AppLibrary.ServiceState()
    
    /// form a service URL from a site and a path
    public func FormURL( site: String, path: String ) -> NSURL?
    {
        let url = NSURL(scheme: "http", host: site, path: path)
        return url
    }
    
    /// form a service URL from an internal URL
    public func FormURL( path: String ) -> NSURL?
    {
        let url = NSURL(scheme: "http", host: DefaultGateway, path: path)
        return url
    }
    
    /// call a web service
    public func GetService( url: NSURL? )
    {
        
    }
    
    /// Construct an AppLibrary instance
    public init()
    {
    }
    
    
    public func Info() -> String {
        return "VM Hub App Library"
    }
    
    
    /// Check if Internet is connected. If true, the Internet connection state will be updated. Otherwise,the Internet connection state will be checked
    /// if it hasn't been connected, and sometime has passed from the last state updated.
    /// Return: whether the Internet is connected.
    public func CheckInternetConnection( bForce: Bool, completionHandler: Bool -> Void )
    {
        if ( bForce || NetworkStatus.NeedsToCheck())
        {
            let url = self.FormURL(GatewayHttpInterface.DefaultCheckSite, path: "/")
            _ = OneHttpWebRequest( urlInput: url, reqCompletionHandler: { ( resp: NSString? ) -> Void in
                if let retResp = resp
                {
                    if retResp.length > 0
                    {
                        completionHandler(true)
                    }
                    else
                    {
                        self.ErrorMessage = String(format: "Access to site \(GatewayHttpInterface.DefaultCheckSite) return 0B")
                        completionHandler(false)
                    }
                }
                else
                {
                    self.ErrorMessage = String(format: "Access to site \(GatewayHttpInterface.DefaultCheckSite) fails to complete")
                    completionHandler(false)
                }
            } )
        }
        else
        {
            completionHandler(NetworkStatus.State)
        }
    }


}

