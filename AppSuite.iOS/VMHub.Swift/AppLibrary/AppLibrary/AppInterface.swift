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

/// Manage One Http Get Request
public class OneGetRequest: NSURLConnectionDelegate {
    
    var data = NSMutableData()
    var request : NSURLRequest?
    var connection: NSURLConnection?
    
    init( url: NSURL, startImmediately: Bool )
    {
        request = NSURLRequest(URL: url)
        //Sending outbound http request request
        connection = NSURLConnection(request: request!, delegate: self, startImmediately: startImmediately)
    }
    
    @objc public func connection(connection: NSURLConnection, didReceiveResponse response: NSURLResponse)
    {
        //Will be called when
        NSLog("didReceiveResponse")
    }
    
    @objc public func connection(connection: NSURLConnection, didReceiveData _data: NSData)
    {
        NSLog("didReceiveData")
        self.data.appendData(_data)
    }
    
    @objc public func connectionDidFinishLoading(connection: NSURLConnection)
    {
        NSLog("connectionDidFinishLoading")
        
        // let responseStr:NSString = NSString(data:self.data, encoding:NSUTF8StringEncoding)
        //var responseDict: NSDictionary = NSJSONSerialization.JSONObjectWithData(responseData,options: NSJSONReadingOptions.MutableContainers, error:nil) as NSDictionary
        //self.createWebViewLoadHTMLString(responseStr);
    }
    
    @objc public func connection(connection: NSURLConnection, didFailWithError error: NSError)
    {
        NSLog("didFailWithError=%@",error)
    }
    
    @objc public func isEqual(anObject: AnyObject?) -> Bool {
        return self === anObject
    }
}

/// GatewayHttpInterface manages connection to the hub
public class GatewayHttpInterface {
    
    /// Default Gateway Used
    private var DefaultGateway = "imhub-cus.cloudapp.net"
    
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

