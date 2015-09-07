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
    
    /// Default Gateway Used
    private var DefaultGateway = "vm-hub.trafficmanager.net"
    
    /// Default Address to get the status pag
    private var DefaultPath = "/web/info"
    
    /// Customer ID to be used
    private var CustomerIDString = "00000000-0000-0000-0000-000000000000"
    
    /// Customer Key 
    public var CustomerKey = ""
    
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
    

}

