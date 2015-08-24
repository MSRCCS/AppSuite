//
//  HomeViewController.swift
//  VMHub.Swift
//
//  Created by jinl on 8/22/15.
//  Copyright © 2015 Mighty Dog. All rights reserved.
//

import Foundation
import UIKit

/// The view controller for the home screen
class HomeViewController : UIViewController {
    var imageCache = [String:UIImage]()
    
    @IBOutlet var settingButton: UIButton?
    @IBOutlet var cameraButton : UIButton?
    @IBOutlet var folderButton : UIButton?
    @IBOutlet var searchButton : UIButton?
    
    func loadImages() {
        if ( imageCache["camera"]==nil )
        {
            imageCache["camera"] = UIImage(named: "camera.png")
        }
        if ( imageCache["folder"]==nil )
        {
            imageCache["folder"] = UIImage(named: "folder.png")
        }
        if ( imageCache["search"]==nil )
        {
            imageCache["search"] = UIImage(named: "search.png")
        }
        if ( imageCache["settings"]==nil )
        {
            imageCache["settings"] = UIImage(named: "settings.png")
        }
    }
    
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        let bounds = view.bounds
        let width = bounds.width
        let height = bounds.height
        
        let imSizeX = min( width/2.0, height/4.0 )
        let imSizeX2 = ( width - imSizeX ) / 2.0
        let imSizeY2 = ( height - imSizeX * 3.0 ) / 4.0
        let bounds0 = CGRect(origin:CGPoint(x:0.0, y:0.0),size:CGSize(width: 60.0, height: 60.0))
        let bounds1 = CGRect(origin:CGPoint(x: imSizeX2, y: imSizeY2), size: CGSize(width: imSizeX, height:imSizeX))
        let bounds2 = CGRect(origin:CGPoint(x: imSizeX2, y: imSizeY2*2.0+imSizeX), size: CGSize(width: imSizeX, height:imSizeX))
        let bounds3 = CGRect(origin:CGPoint(x: imSizeX2, y: imSizeY2*3.0+imSizeX*2.0), size: CGSize(width: imSizeX, height:imSizeX))
        
        self.view.backgroundColor = UIColor(red: 0xfd/255, green: 0xe8/255, blue: 0xd7/255, alpha: 1.0)
        
        loadImages()
        settingButton?.setBackgroundImage(imageCache["settings"], forState: UIControlState.Normal)
        settingButton?.setTitle(" ", forState: UIControlState.Normal)
        settingButton?.bounds = bounds0
        cameraButton?.frame = bounds1
        cameraButton?.setBackgroundImage( imageCache["camera"], forState: UIControlState.Normal)
        cameraButton?.setTitle(" ", forState: UIControlState.Normal)
        folderButton?.frame = bounds2
        folderButton?.setBackgroundImage( imageCache["folder"], forState: UIControlState.Normal)
        folderButton?.setTitle(" ", forState: UIControlState.Normal)
        searchButton?.frame = bounds3
        searchButton?.setBackgroundImage( imageCache["search"], forState: UIControlState.Normal)
        searchButton?.setTitle(" ", forState: UIControlState.Normal)
        // Do any additional setup after loading the view, typically from a nib.
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    override func viewWillAppear(animated: Bool) {
        super.viewWillAppear(animated)
    }
    
    @IBAction func settingsTapped(sender : AnyObject) {
        let settingViewController = self.storyboard?.instantiateViewControllerWithIdentifier("SettingViewController")
        if ( settingViewController != nil )
        {
            self.navigationController?.pushViewController(settingViewController!, animated: true)
    
        }
    }
    
    @IBAction func cameraTapped(sender : AnyObject) {
    }
    
    @IBAction func folderTapped(sender : AnyObject) {
    }
    
    @IBAction func searchTapped(sender : AnyObject) {
    }
    
    
}

