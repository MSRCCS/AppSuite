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
    
    @IBOutlet var cameraButton : UIImageView?
    @IBOutlet var folderButton : UIImageView?
    @IBOutlet var searchButton : UIImageView?
    
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        let bounds = view.bounds
        let width = bounds.width
        
        let imSizeX2 = width / 6.0
        let imSizeX = width / 2.0
        let bounds1 = CGRect(x: imSizeX2, y: imSizeX2, width: imSizeX, height: imSizeX)
        let bounds2 = CGRect(x: imSizeX2, y: imSizeX*2.0, width: imSizeX, height: imSizeX)
        let bounds3 = CGRect(x: imSizeX2, y: imSizeX*2.0, width: imSizeX, height: imSizeX)
        
        self.view.backgroundColor = UIColor(red: 0xfd/255, green: 0xe8/255, blue: 0xd7/255, alpha: 1.0)
        
        imageCache["camera"] = UIImage(named: "camera.png")
        imageCache["folder"] = UIImage(named: "folder.png")
        imageCache["search"] = UIImage(named: "search.png")
        cameraButton?.image = imageCache["camera"]
        cameraButton?.bounds = bounds1
        folderButton?.image = imageCache["folder"]
        folderButton?.bounds = bounds2
        searchButton?.image = imageCache["search"]
        searchButton?.bounds = bounds3
        // Do any additional setup after loading the view, typically from a nib.
    }
    
    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }
    
    override func viewWillAppear(animated: Bool) {
        super.viewWillAppear(animated)
    }
    
}

