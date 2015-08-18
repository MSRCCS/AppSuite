(*---------------------------------------------------------------------------
	Copyright 2013, Microsoft.	All rights reserved                                                      

	File: 
		ReconitionExample.fs
  
	Description: 
		Recognize an single image: this command line tool will 
        (1) resize the image to 512 (long edge) 
        (2) compress to jpg (%85 quality) 
        (3) send the image to vHub frontend through async call 
        (4) collect and display recognition result, including tags, and performance data 

	Author:																	
 		Yuxiao Hu, RSDE
 		Microsoft Research, One Microsoft Way
 		Email: yuxhu@microsoft.com, Tel. (425) 707-0123
    Date:
        March 2015
	
 ---------------------------------------------------------------------------*)
open System
open System.Collections.Generic
open System.Threading
open System.Diagnostics
open System.IO
open System.Net
open System.Runtime.Serialization
open System.Runtime.Serialization.Json
open System.Threading.Tasks
open System.Drawing
open System.Drawing.Imaging

open vHub.Data
open Prajna.Tools

open Prajna.Tools.FSharp
open Prajna.Tools.StringTools

open VMHubClientLibrary

/// <summary>Ping vHub with two testing strings: /Test/{s}/{t} </summary>
/// <param name = "serviceURL">base URL string to the vHub gateway machine</param>
/// <param name = "intput1">test string 1</param>
/// <param name = "intput2">test string 2</param>
/// <return>the response html string will be returned, including the information about inputs of the test</return>
let testVHubAsync (serviceURL:string, input1:string, input2:string) =
    // ping web site asynchronously
    let testURL = "http://" + serviceURL + "/Test/" + input1 + "/" + input2
    try
        let req = WebRequest.Create(testURL)
        let resp = req.GetResponse()
        let stream = resp.GetResponseStream()
        let reader = new StreamReader( stream, System.Text.Encoding.UTF8, false, 1024000, false ) 
        let html = reader.ReadToEnd()
        Logger.LogF( LogLevel.MildVerbose, ( fun _ -> sprintf "vHub OK! returned %d bytes" html.Length ))
        html
    with
        | e -> 
            Logger.LogF( LogLevel.Error, (fun _ -> sprintf "Exception %A when pinging vHub gateway with request: %s" e.Message testURL ))
            ""

/// helpler function to get image encoding format 
let getEncoder (format:ImageFormat) =
    let mutable returncodec = null
    let codecs = ImageCodecInfo.GetImageDecoders()
    for codec in codecs do
        if codec.FormatID = format.Guid then
            returncodec <- codec
    returncodec

/// Read a byte[] from files
let readBytesFromFile (filename ) = 
    use fileStream= new FileStream( filename, FileMode.Open, FileAccess.Read )
    let len = fileStream.Seek( 0L, SeekOrigin.End )
    fileStream.Seek( 0L, SeekOrigin.Begin ) |> ignore
    if len > int64 Int32.MaxValue then 
        failwith "ReadBytesFromFile isn't capable to deal with file larger than 2GB"
    let bytes = Array.zeroCreate<byte> (int len)
    let readLen = fileStream.Read( bytes, 0, int len )
    if readLen < int len then 
        failwith (sprintf "ReadBytesFromFile failed to read to the end of file (%dB), read in %dB" len readLen )
    bytes

/// <summary> Resize an image (as a byte array and return a resized byte array)</summary>
/// <param name = "imageData">Image data as a byte array (image bits in JPEG or other formats)</param>
/// <param name = "maxWH">Max(width, height) of the output/resized image</param>
/// <param name = "quality">JPEG quality(0 - 100). Typically we can set it to 85)</param>
/// <return> Resized image (as a byte array representing the JPEG image bits). If failed, emtpty array will be returned.</return>
let resizeImg (imageData:byte[], maxWH:int, quality:int64)  =
    try
        use img = System.Drawing.Image.FromStream(new MemoryStream(imageData))
        if Math.Max(img.Width, img.Height) <= maxWH && img.RawFormat = ImageFormat.Jpeg && img.PixelFormat = PixelFormat.Format24bppRgb then
            (img.Width, img.Height, imageData)
        else
            let mutable width = 0
            let mutable height = 0

            if img.Width > img.Height then
                width <- maxWH
                height <- img.Height * maxWH / img.Width
            else
                height <- maxWH
                width <- img.Width * maxWH / img.Height
            use thumb = new Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        
            use g = Graphics.FromImage(thumb)
            g.CompositingQuality <- Drawing2D.CompositingQuality.HighQuality
            g.InterpolationMode <- Drawing2D.InterpolationMode.HighQualityBicubic
            g.DrawImage(img, new Rectangle(0, 0, width, height))
        
            let jpgEncoder = getEncoder(ImageFormat.Jpeg)
            let myEncoder = Imaging.Encoder.Quality
            let myEncoderParas = new EncoderParameters(1)
            let myEncoderPara = new EncoderParameter(myEncoder, quality)
            myEncoderParas.Param.[0] <- myEncoderPara
                
            let imgResized = new MemoryStream();
            thumb.Save(imgResized, jpgEncoder, myEncoderParas)
            (width, height, imgResized.ToArray())
        with
            | _ as ex -> Logger.LogF( LogLevel.Error, fun _ -> sprintf "Exception %A when resizing input image. Emtpy array returned." ex  )
                         (0, 0, [||])

    
/// <summary>resize and compress the input image</summary>
/// <param name = "imgFilePath">image file path, which will be processed</param>
/// <return>image path/name, (byte array of the processed image, original image buffer length )</return>
let prepareImage imgFilePath imgSize=
    // load the image and preprocess it: resize and convert to jpeg 85%
    if (File.Exists(imgFilePath)) then
        Logger.LogF( LogLevel.WildVerbose, ( fun _ -> sprintf "opening image input file %s" imgFilePath ))
        let imgBuf = readBytesFromFile imgFilePath
        Logger.LogF( LogLevel.WildVerbose, ( fun _ -> sprintf "resizing %s..." imgFilePath))
        let ( width, height, testImgBuf ) = resizeImg (imgBuf, imgSize, 85L)
        Logger.LogF( LogLevel.WildVerbose, ( fun _ -> sprintf "resized image %s to %d X %d" imgFilePath width height ))
        imgFilePath, ( testImgBuf, imgBuf.Length )
    else    
        imgFilePath, ( [||], 0 )

/// <param name = "imgFilePath">image file path, which will be processed</param>
/// <return>byte array of the processed image</return>
let prepareImageCluster (name:string, imgBuf: byte[], imgSize:int) =
    // load the image and preprocess it: resize and convert to jpeg 85%
    let ext = Path.GetExtension( name )
    if String.Compare( ext, ".jpg", StringComparison.OrdinalIgnoreCase )=0 then 
            let testImgBuf = resizeImg (imgBuf, imgSize, 85L)
            let width, height, resizedBuf = testImgBuf
            if width > 0 && height > 0 then 
                Logger.LogF( LogLevel.MildVerbose, ( fun _ -> sprintf "Resize image %s to %dx%d" name width height ))
                Some ( name, (resizedBuf, imgBuf.Length)  ) 
            else
                Logger.LogF( LogLevel.MildVerbose, ( fun _ -> sprintf "Failed to resize image %s (become size %d %d)" name width height ))
                None
    else 
        Logger.LogF( LogLevel.MildVerbose, ( fun _ -> sprintf "File %s is not an image" name ))
        None

/// <summary>Ping vHub to get cached blob by its GUID </summary>
/// <param name = "serviceURL">base URL string to the vHub gateway machine</param>
/// <param name = "blobGuid">GUID of the blob which will be read</param>
/// <return>the string will be returned in string</return>
let getAuxData( serviceURL:string, blobGuid:string ) =
    //TODO::
    "Done!"

// helper function to tell whether a file is known image format
let isImgFile filePath = 
    let ImgExt = [| ".jpg", ".txt", ".asp", ".css", ".cs", ".xml" |]
    Array.exists ( fun t -> System.String.Equals( Path.GetExtension( filePath) ,t , StringComparison.CurrentCultureIgnoreCase ))

// helper function to load server list from a file
let loadServerList ( listFilePath:string, listLineSplit:string, serverURLIndex:int) =
    let t1 = (DateTime.UtcNow)
    let total = ref 0L
    let lines = ref 0 
    use reader = new StreamReader( listFilePath, Text.ASCIIEncoding.UTF8, false, 102400)
    let toSave = 
        seq { 
            while not reader.EndOfStream do 
                let line = reader.ReadLine().ToLower()
                if not (line.StartsWith ("#")) then
                    let strArray = line.Split( listLineSplit.ToCharArray(), StringSplitOptions.RemoveEmptyEntries )
                    if strArray.Length> serverURLIndex then 
                        yield ( strArray.[serverURLIndex], strArray )
                        Logger.LogF( LogLevel.ExtremeVerbose, ( fun _ -> sprintf "read %s" strArray.[serverURLIndex] ))
                        lines := !lines + 1
                        total := !total + ( int64 ( strArray.[serverURLIndex].Length + line.Length ))
        }
    //let svrList = toSave |> Seq.map (fun (a, _) -> a) |> Seq.distinct |> Seq.toArray
    let svrList = toSave |> Seq.map fst |> Seq.toArray
    let t2 = (DateTime.UtcNow)
    let elapse = t2.Subtract(t1)
    //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "Processed %d Lines with total %dB in %f sec, throughput = %f MB/s" !lines !total elapse.TotalSeconds ((float !total)/elapse.TotalSeconds/1000000.)  )
    svrList

// helper function: check whether the recognition result is in the right format: tag:score;[tag:score]
let parseRecogResult (strResult:string) =
    try
        let Preds = strResult.Split ([|';'|], System.StringSplitOptions.None)
        let Tuples = Preds |> Array.map (fun pair -> let SingleResult = pair.Split ([|':'|], System.StringSplitOptions.None)
                                                     (SingleResult.[0], System.Double.Parse(SingleResult.[1])))
        Tuples
    with
    | e -> 
        Logger.LogF( LogLevel.Error, (fun _ -> sprintf "Exception %A when parsing recogResult: %s" e.Message strResult ))
        [||]


let tsvReader tsvFilePath = 
    seq {
        let fstream = new System.IO.StreamReader(tsvFilePath:string)
        let totalLines = ref 0L
        while not fstream.EndOfStream do 
            let line = fstream.ReadLine()
            let items = line.Split([|'\t'|])
            //if items.Length = 3 then 
            //    let byt = Convert.FromBase64String( items.[defs.pos("imagedata")])
            //    totalLines := !totalLines + 1L
            //yield !totalLines, items.[defs.pos("groupkey")], items.[defs.pos("itemkey")], byt
            yield items
        }

// helper function: load image list in tsv files 
let loadImagesFromTSV (tsvFilePath:string) =
    // load ground truth from TSV file (already randomlize)
    let GT =  tsvReader tsvFilePath
              |> Seq.map (fun x -> (x.[0].Trim(), x.[1].Trim().ToLower(), System.Convert.FromBase64String(x.[2]) ))
    
    let imageBuf = GT |> Seq.map ( fun x -> let (imgFilePath, label, imgBuf ) = x
                                            Logger.LogF( LogLevel.WildVerbose, ( fun _ -> sprintf "resizing %s..." imgFilePath))
                                            let ( width, height, testImgBuf ) = resizeImg (imgBuf, 256, 85L)
                                            Logger.LogF( LogLevel.WildVerbose, ( fun _ -> sprintf "resized image %s to %d X %d" imgFilePath width height ))
                                            imgFilePath, ( testImgBuf, imgBuf.Length )
                                  )
    imageBuf

// helpler function: append a record to retry File
let appendToRetryLog (retryFilePath:string, imgFile:string) =
    use sw = new StreamWriter( retryFilePath, true , Text.Encoding.UTF8)
    sw.WriteLine(imgFile)
    sw.Close()

(*
let Usage = "
    Usage: Recognize an single image. \n\
    Command line arguments:\n\
    -cmd        List      list all the active(available) recognizers
                Recog     recognize single image
                Ping      ping recognition server
    -file       File_Name       file name or relative path of the input image which need to be recognized/tagged \n\
    -rootDir    Root_Directory  this directories holds all images \n\
    -vMHub      name of the service            traffic manager name of the service \n\
    -svrListFile    File_Name   file path of the vHub gateway server list file, each line is a server \n\
    -svrKey        #serverKey field \n\
    -svrListSplit   Characters used to split the server list fields\n\
    -serviceGuid    GUID        GUID for recognizer \n\
    -distGUID       GUID        GUID for load balancer \n\
    -aggrGUID       GUID        GUID for result aggregation \n\
//    -in         Copy into Prajna \n\
//    -out        Copy outof Prajna \n\
//    -local      Local directory. All files in the directories will be copy to (or from) remote \n\
//    -remote     Name of the distributed Prajna folder\n\
//    -ver V      Select a particular DKV with Version string: in format yyMMdd_HHmmss.fff \n\
//    -rep REP    Number of Replication \n\
//    -slimit S   # of record to serialize \n\
//    -balancer B Type of load balancer \n\
//    -nump N     Number of partitions \n\
//    -flag FLAG  DKVFlag \n\
//    -speed S    Limiting speed of each peer to S bps\n\
//    -upload     Upload a text file as DKV to the cluster. Each line of is split to a string[], the key is the #uploadKey field\n\
//    -uploadKey  #uploadKey field\n\
//    -uploadSplit Characters used to split the upload\n\
//    -maxwait    Maximum time in millisecond to wait for a web request for activity, before consider it failed \n\
//    -download   LIMIT  Download files to the cluster. Using key as URL. LIMIT indicates # of parallel download allowed \n\
//    -exe        Execute function in a separate exe\n\
//    -task       Execute function in parallel tasks\n\
//    -downloadChunk Chunk of Bytes to download\n\
//    -countTag   Count how many images has a certain tag\n\
"
*)

let Usage = @"
    Usage: Verification tool for IRC 
    Command line arguments:
        -cmd            Recog     recognize a single image
                        Ping      ping recognition gateway (vm-hub.trafficManager.net) 
                                    to see whether it responses OK
                        Check     check a classifer (GUID) to see whether it is available
                        List      list the Guids of all active classifiers
                        ListProviders list the Guids of all active providers
                        Batch     reognize all the jpg files 
        -vHub           URL       http access point of vHub service, 
                                    required parameter for Recog/Ping/Check command
        -serviceGuid    GUID      the GUID of the classifer (your team's classiferID)
                                    required parameter for Recog/Check command
        -providerGuid   GUID      the GUID of the provider (your team's private GUID)
                                    required parameter for Recog command
        -file           File_Path           file name or relative path of the input image which need to be recognized/tagged 
                                                required parameter for Recog command
        -rootDir        Root_Directory      this directories holds all images 
        -TSV            File_Name           this TSV file hold all images
        -svrListFile    File_Name           file path of the vHub gateway server list file, each line is a server
        -svrKey         #serverKey          field index of the server URL
        -svrListSplit   Characters          used to split the server list fields
        -resizeImage    NumOfPixelOfLongEdge resize the long edge of the image to NumOfPixelOfLongEdge, default 256
        -log            File_Path           log file path
    Example:
        CommandLineRecognition.exe -cmd Ping -vHub vm-Hub.trafficManager.net
        CommandLineRecognition.exe -cmd Check -vHub vm-Hub.trafficManager.net -serviceGuid ca99a8b9-0de2-188b-6c14-747619a2ada8
        CommandLineRecognition.exe -cmd Recog -vHub vm-Hub.trafficManager.net -serviceGuid ca99a8b9-0de2-188b-6c14-747619a2ada8 -providerGuid 456B46AB-EE4F-407E-8348-3E62DC879FD9 -file c:\dog.jpg
        CommandLineRecognition.exe -cmd List -vHub vm-hub.trafficmanager.net 
        CommandLineRecognition.exe -cmd Batch -Vhub vm-Hub.trafficManager.net -rootDir \\yuxiao-z840\prajna\data\images\office-bigset  -serviceGUID a0d6d212-58f5-cf2c-abbb-0713d32505bf  //office
        CommandLineRecognition.exe -cmd Batch -Vhub vm-Hub.trafficManager.net -rootDir \\yuxiao-z840\prajna\data\images\office-bigset  -serviceGUID f3dbd360-5e58-5765-1776-35a97ae29663  //VIA
    "

type WebInit(gateway, customerID, customerKey ) = 
    member val Hub = VMHubClientLibrary.GatewayHttpInterface( gateway, customerID, customerKey ) with get
    static member val Initialized = 
        System.Net.ServicePointManager.DefaultConnectionLimit  <- 100
        true with get

    /// <summary>ping vHub gateway to get the active classifiers information through ActiveClassifiers.html</summary>
    /// <param name = "imgFilePath">image file path, which will be processed</param>
    /// <return>html response including the active classifiers</return>
    //TODO:: parse the returned html to extract the classifier information: name, GUID
    member x.GetActiveClassifiers serviceURL =
        let requestURL = "http://" + serviceURL + "/ActiveClassifiers.html"
        try 
            let req = WebRequest.Create(requestURL)
            let resp = req.GetResponse()
            let stream = resp.GetResponseStream()
            let reader = new StreamReader( stream, System.Text.Encoding.UTF8, false, 1024000 )
            let response = reader.ReadToEnd() 
            Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Received response of length %A bytes" response.Length ))
        //    let results = HtmlDocument.Load requestURL
            "Done!"
         with
            | e -> 
                Logger.LogF( LogLevel.Error, (fun _ -> sprintf "Exception %A when pinging vHub gateway with request: %s" e.Message requestURL ))
                ""

    
    /// <summary>ping vHub gateway to get the active classifiers information through ActiveClassifiers.html</summary>
    /// <param name = "imgFilePath">image file path, which will be processed</param>
    /// <return>html response including the active classifiers</return>
    //TODO:: parse the returned html to extract the classifier information: name, GUID
    member x.getAllServiceGUIDs serviceURL =
        let t1 = (DateTime.UtcNow.Ticks)
        let requestURL = "http://"  + serviceURL + "/VHub/GetAllServiceGuids/" + t1.ToString() + "/2000" 
        try
            let req = System.Net.WebRequest.Create( requestURL )
            req.Method <- "GET"
            //let stream = req.GetRequestStream()
            //let sendStream = new MemStream 10
            //sendStream.CopyTo( stream ) 
            let response = req.GetResponse()
            let responseStream = response.GetResponseStream()
            let json = DataContractJsonSerializer(typeof<Guid[]>)
            let ob = json.ReadObject( responseStream )
            match ob with 
            | :? (Guid []) as result -> 
                result |> Array.map ( fun t-> t.ToString ()) 
            | _ -> 
                sprintf "Expect reply as Guid[], but recieved %A" ob |> ignore
                [||]
        with
        | e -> 
            Logger.LogF( LogLevel.Error, (fun _ -> sprintf "Exception %A when pinging vHub gateway with request: %s" e.Message requestURL ))
            [||]
    
    /// <summary>contact vHub gateway to recognize a image, blocked call, through /VHub/Classify/{idString}/{distribution}/{aggregation}/{ticks}/{rtt}</summary>
    /// <param name = "imgFilePath">image file path, which will be processed</param>
    /// <param name = "serviceURL">base URL string to the vHub gateway machine</param>
    /// <param name = "serviceGuid">GUID of the recognizer which will be used</param>
    /// <param name = "distGuid">GUID of the load balancer which will be used</param>
    /// <param name = "aggrGuid">GUID of the result aggregator which will be used</param>
    /// <return>the response html string will be returned, including the information about inputs of the test</return>
    //let recognizeSync ( imgFilePath:string, serviceURL:string, serviceGuid:string, distGuid:string, aggrGuid:string ) =
    //    let t1 = DateTime.UtcNow
    //    let requestURL = serviceURL + "/VHub/Classify/" + serviceGuid + "/" + distGuid + "/" + aggrGuid + "/" + t1.ToLongTimeString() + "/2000" 
    //    let ( w, h, imgBuf ) = prepareImage imgFilePath
    //    if w > 0 && h > 0 then
    //        let req = System.Net.WebRequest.Create( requestURL )
    //        req.Headers <- HttpContentTypes.Json
    //        req
    //        let response = Http.RequestString ( requestURL, 
    //                                            headers = [ ContentType HttpContentTypes.Json ],
    //                                            body = BinaryUpload imgBuf,
    //                                            silentHttpErrors = true,
    //                                            httpMethod = "POST" )
    //        let elapse = DateTime.UtcNow.Subtract(t1).TotalMilliseconds
    //        printfn "%s" response
    //        printfn "got respones in %f MSec" elapse 

    /// <summary>contact vHub gateway to recognize a image, blocked call, through /VHub/Classify/{idString}/{distribution}/{aggregation}/{ticks}/{rtt}</summary>
    /// <param name = "imgFilePath">image file path, which will be processed</param>
    /// <param name = "serviceURL">base URL string to the vHub gateway machine</param>
    /// <param name = "serviceGuid">GUID of the recognizer which will be used</param>
    /// <param name = "distGuid">GUID of the load balancer which will be used</param>
    /// <param name = "aggrGuid">GUID of the result aggregator which will be used</param>
    /// <return>the response html string will be returned, including the information about inputs of the test</return>
    member x.Recognize ( imgFilePath:string, imgSize:int, providerID, schemaID, serviceID, distID, aggrID ) =
        WebInit.Initialized |> ignore
        let ( _, (imgBuf, oriSize)) = prepareImage imgFilePath imgSize
        if imgBuf.Length <= 0 then
            new Task<_> (fun _ -> sprintf "Image %s doesn't exist, size is 0" imgFilePath )
        else
            x.Hub.ProcessAsyncString( providerID, schemaID, serviceID, distID, aggrID, imgBuf )

    member x.RecognizeBuffer ( imgBuf, providerID, schemaID, serviceID, distID, aggrID ) =
        x.Hub.ProcessAsyncString( providerID, schemaID, serviceID, distID, aggrID, imgBuf )
       

// helper function: submit images under a folder to DKV


// main function 
[<EntryPoint>]
let main argv = 
    let logFile = sprintf @"c:\Log\vHub\RecognitionExample_%s.log" (VersionToString( (DateTime.UtcNow) ))
    let newarg = Array.append [| "-log"; logFile; "-con" |] argv
    let parse = ArgumentParser(newarg)
    let cmd = parse.ParseString( "-cmd", "" )
    let serviceURL = parse.ParseString( "-vHub", "vm-hub.trafficmanager.net" )
    //check whether serviceURL file is availabe
    let serverListFile = parse.ParseString( "-svrListFile", null )
    let serverListKey = parse.ParseInt( "-svrKey", 0 )
    let serverListSplit = parse.ParseString( "-svrListSplit", "\t" )
    let serviceGuid = parse.ParseString( "-serviceGuid", "ca99a8b9-0de2-188b-6c14-747619a2ada8" )
    let providerGuid = parse.ParseString( "-providerGuid", (Guid.Empty.ToString()) )
    let distGuid = parse.ParseString( "-distGuid", (Guid.Empty.ToString()) )
    let aggrGuid = parse.ParseString( "-aggrGuid", (Guid.Empty.ToString()) )
    let imgFileName = parse.ParseString( "-file", "" )
    let tsvFileName = parse.ParseString( "-TSV", "" ) // @".\combined.tsv"
    let retryFileName = parse.ParseString( "-retryFile", "" )
    let nRepeat = parse.ParseInt( "-repeat", 1 )
    let imgRootDir = parse.ParseString( "-rootdir", @"." )
    let remoteDKVname = parse.ParseString( "-remote", @"jinl\1012_pick" )
    let imgFilePath = if StringTools.IsNullOrEmpty( imgRootDir) then imgFileName else Path.Combine( imgRootDir, imgFileName )
    let tsvFilePath =  tsvFileName 
    let retryFilePath = logFile + ".retry"
    let resizeImage = parse.ParseInt( "-resizeImage", 256)
    
    let serverList = 
        if StringTools.IsNullOrEmpty serviceURL && not (StringTools.IsNullOrEmpty serverListFile) then
            let serverListFilePath = if StringTools.IsNullOrEmpty( imgRootDir) then imgFileName else Path.Combine( imgRootDir, serverListFile ) 
            loadServerList(serverListFilePath, serverListSplit, serverListKey)
        else
            [|serviceURL|]
    let gateway = serverList.[0] 
    let customerID = Guid.Empty
    let secret = "SecretKeyShouldbeLongerThan10"
    //TODO:: remove http:// if necessary
    let recog = WebInit( gateway, customerID, secret )
    let providerID = Guid.Parse( providerGuid )
    let schemaID = Guid.Empty
    let serviceID = Guid.Parse( serviceGuid )
    let distID = Guid.Empty
    let aggrID = Guid.Empty

    if parse.RemainingArgs.Length > 0 || argv.Length = 0 then
        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "%s " Usage))

    //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "%A" serverList  )
        
    System.Net.ServicePointManager.DefaultConnectionLimit <- 1000
    System.Threading.ThreadPool.SetMinThreads( 200, 30 ) |> ignore 
    //System.Threading.ThreadPool.SetMaxThreads( 10, 10) |> ignore
    

    match cmd with
    | "Check" ->
        // show active classifiers
         //getActiveClassifiers serviceURL
         //let serverListFiltered = serverList |> Seq.filter (fun t -> true) |> Seq.toArray
         //serverList |> Array.map ( recog.GetActiveClassifiers) |>ignore
         
         //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "pinged %d vHub servers" serverList.Length   )
         serverList |> Array.map (fun t ->  
                                        let Guids = recog.getAllServiceGUIDs t
                                        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "contacting %s..." t ))
                                        let result = Guids |> Array.tryFind ( fun x -> x.Equals(serviceGuid, StringComparison.OrdinalIgnoreCase ) )
                                        match result with
                                        | Some  x -> Logger.LogF(LogLevel.Info, ( fun _ -> sprintf "pinged %s and verified %s is available, you can now send test request to it" serviceURL x ) )
                                        | None -> Logger.LogF(LogLevel.Info, ( fun _ -> sprintf "pinged %s, but didn't see %s available, please double check:  \n\t(1) VM-Hub gateway (%s) is reachable \n\t(2) your classifier is still running on your own machines\n\t(3) the serviceGUID/providerGUID you used (%s/%s) is correct.\n if you still see problems, contact IRC organizer." serviceURL  serviceGuid serviceURL serviceGuid providerGuid) )
                                                                          
                                        //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "\t%d Classifiers:" Guids.Length  )
                                        ) |> ignore
        
    | "Ping" ->
        // ping vHub service to see whether it is available
        let testInputs = [|"input1", "input2"|]
        let testParams = serverList |> Array.map (fun  t -> (testInputs |> Array.map (fun (a, b) -> t,a,b))) |> Array.collect ( fun t -> t )
        //let testParams = serverList |> Array.map (fun t -> (t, "input1", "input2" ))
        for param in testParams do 
            let results = testVHubAsync param
            if results.Contains("<body>Called with 'input1' and 'input2'</body></html>") then
                Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Checked %s and received expected response!" serviceURL ) )
            else
                Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Checked %s but recevied unexpected response, please contact IRC organizer." serviceURL ) )
                
    | "Recog" ->    
        // formulate the http request
        for i = 0 to nRepeat - 1 do 
            let result = recog.Recognize(imgFilePath, resizeImage, providerID, schemaID, serviceID, distID, aggrID ).Result
            Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Recognition result is \n%s" result ))
    
    
    | "List" ->
        // show active classifiers by serviceGuids
         //getActiveClassifiers serviceURL
         serverList |> Array.map ( recog.GetActiveClassifiers) |>ignore
         Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "pinged %d vHub servers" serverList.Length  ))
         serverList |> Array.map (fun t ->  
                                        let Guids = recog.getAllServiceGUIDs t
                                        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "contacting %s..." t ))
                                        Guids |> Array.map ( fun x -> Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "\t\t%s" x ) )) |>ignore
                                        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "\t%d Classifiers:" Guids.Length ))
                                        ) |> ignore
    | "Batch" ->
        
        // load the images under imgRootDir
        let imgBufferBeforeFilter = if (File.Exists (tsvFilePath)) then 
                                        // load the images from TSV file
                                        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Loading images from %s ..." tsvFilePath))
                                        loadImagesFromTSV tsvFilePath
                                    else
                                        // load the images under imgRootDir
                                        let imgFiles =  Directory.GetFiles(imgRootDir, "*.jpg", SearchOption.AllDirectories) |> Array.ofSeq
                                        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "found %d jpeg images from %s, now resizing them..." imgFiles.Length  imgRootDir))
                                        let resizedImageBuffers = imgFiles |> Seq.map (fun x -> prepareImage x resizeImage)
                                        resizedImageBuffers

        let imgBuffer = imgBufferBeforeFilter |> Seq.choose ( fun tuple ->  let fname, tuple1 = tuple
                                                                            let imgBuf, _ = tuple1
                                                                            if imgBuf.Length <=0  then//||  not (fname.Equals(@"Basenji\f455864a-8bc2-4659-b0b8-cf66e9ce1727.jpg")) then
                                                                                None
                                                                            else
                                                                                Some( fname, imgBuf ) )
        
        //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "Processed Images: %d" imgBuffer.Length  )
        
        let serviceGuids = recog.getAllServiceGUIDs serviceURL
        let t1 = DateTime.UtcNow 
        let numSentFiles = ref 0
        let numSucceedFiles = ref 0
        let numFailedFiles = ref 0
        let total = ref 0L
        let totalRecogMS = ref 0L
        let rnd = Random()
        for i = 0 to nRepeat - 1 do 
            for tuple in imgBuffer do
                let Guid = if serviceGuid.Equals("Random") then serviceGuids.[rnd.Next(serviceGuids.Length)] else serviceGuid    
                let name, imgBuf = tuple
                numSentFiles := !numSentFiles + 1 
                let timeStart = DateTime.UtcNow.Ticks
                let recogResult = recog.RecognizeBuffer(imgBuf, providerID, schemaID, serviceID, distID, aggrID ).Result
                let recogSize = imgBuf.Length
                let timeEnd = DateTime.UtcNow.Ticks
                let recogMs = ( timeEnd - timeStart ) / TimeSpan.TicksPerMillisecond
                let resultPairs = parseRecogResult(recogResult)
                //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "parsed results: %A" resultPairs  )
                if recogResult.Contains "Failed" || recogResult.Contains "Exception"  || recogResult.Contains "return 0B" || resultPairs.Length < 1 then 
                     appendToRetryLog(retryFilePath, name)
                     numFailedFiles := !numFailedFiles + 1
                else
                     numSucceedFiles := !numSucceedFiles + 1
                total :=  !total + int64 imgBuf.Length
                totalRecogMS:= !totalRecogMS + int64 recogMs
                Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "[Sent:%d Succ:%d Failed:%d / Total:%d] Image\t%s\tis recognized by \t%s\t as\t%s\t(resized to %dB, used %d ms)" 
                                                               !numSentFiles !numSucceedFiles !numFailedFiles !numSentFiles name Guid recogResult recogSize recogMs))
                    
        let elapse = DateTime.UtcNow.Subtract(t1)
        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Processed %d Files with total %d MB in %f sec, throughput = %f MB/s, avg recogtime = %d ms, %d files failed (%f\%%)" 
                                                       (!numSucceedFiles + !numFailedFiles) (!total) elapse.TotalSeconds ((float !total)/elapse.TotalSeconds/1000000.) (if (!numSucceedFiles + !numFailedFiles) >0 then (!totalRecogMS/(int64 (!numSucceedFiles + !numFailedFiles))) else -1L)
                                                       !numFailedFiles (if (!numSucceedFiles + !numFailedFiles)>0 then (float !numFailedFiles)/(float (!numSucceedFiles + !numFailedFiles))*100.0 else -1.0)))

    | "BatchAsync" ->       //batch upload and recognition 

        let monitor = new Object()
        let imgBufferBeforeFilter = if (File.Exists (tsvFilePath)) then 
                                        // load the images from TSV file
                                        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Loading images from %s ..." tsvFilePath))
                                        loadImagesFromTSV tsvFilePath
                                    else
                                        // load the images under imgRootDir
                                        let imgFiles =  Directory.GetFiles(imgRootDir, "*.jpg", SearchOption.AllDirectories) |> Array.ofSeq
                                        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "found %d jpeg images from %s, now resizing them..." imgFiles.Length  imgRootDir))
                                        let resizedImageBuffers = imgFiles |> Seq.map (fun x -> prepareImage x resizeImage)
                                        resizedImageBuffers

        let imgBuffer = imgBufferBeforeFilter |> Seq.choose ( fun tuple ->  let fname, tuple1 = tuple
                                                                            let imgBuf, _ = tuple1
                                                                            if imgBuf.Length <=0 then
                                                                                None
                                                                            else
                                                                                Some( fname, imgBuf ) )
        //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "Processed Images: %d" imgBuffer.Length  )
   
        let t1 = (DateTime.UtcNow)
        let numSucceedFiles = ref 0
        let numFailedFiles = ref 0
        let numSentFiles = ref 0
        let total = ref 0L
        let totalRecogMS = ref 0L
        let serviceGuids = recog.getAllServiceGUIDs serviceURL
        let rnd = Random()
        for i = 0 to nRepeat - 1 do 
            let recogImageAndShow tuple = 
                async { 
                    let Guid = if serviceGuid.Equals("Random") then serviceGuids.[rnd.Next(serviceGuids.Length)] else serviceGuid 
                    let name, imgBuf = tuple
                    
                    Interlocked.Increment( numSentFiles) |> ignore
                    //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "!!!!!!!!!!!!!!!!!!! Sent File %d!" !numSentFiles )
                    let timeStart = DateTime.UtcNow.Ticks
                    let ta = recog.RecognizeBuffer(imgBuf, providerID, schemaID, serviceID, distID, aggrID )
                    let recogSize = imgBuf.Length
                    let! recogResult = Async.AwaitTask ta
                    let timeEnd = DateTime.UtcNow.Ticks
                    //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "!!!!!!!!!!!!!!!!!!!Processed one image!" )
                    let recogMs = ( timeEnd - timeStart ) / TimeSpan.TicksPerMillisecond
                    let resultPairs = parseRecogResult(recogResult)
                    //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "parsed results: %A" resultPairs  )
                    if recogResult.Contains "Failed" || recogResult.Contains "Exception"  || recogResult.Contains "return 0B" || resultPairs.Length < 1 then 
                        Interlocked.Increment( numFailedFiles) |> ignore
                    else  
                        Interlocked.Increment( numSucceedFiles) |> ignore
                    Interlocked.Add( total,  int64 imgBuf.Length ) |> ignore
                    Interlocked.Add( totalRecogMS, int64 recogMs ) |> ignore

                    Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "[Sent:%d Succ:%d Failed:%d / Total:%d] Image\t%s\tis recognized by \t%s\t as\t%s\t(resized to %dB, used %d ms)" 
                                                                    !numSentFiles !numSucceedFiles !numFailedFiles !numSentFiles name Guid recogResult recogSize recogMs))
                    }
            //let recogResult = Async.RunSynchronously ( Async.Parallel [for tuple in imgBuffer -> recogImageAndShow tuple ] )
            let concurrencySemaphore = new SemaphoreSlim(10)
            let RecogTasks = 
                [|  for  tuple in imgBuffer ->
                        concurrencySemaphore.Wait()
                        let t1 = Async.StartAsTask(recogImageAndShow(tuple))
                        t1.ContinueWith( fun ( _:Task) -> concurrencySemaphore.Release() |> ignore)
                |]
            Task.WaitAll(RecogTasks)
            concurrencySemaphore.Dispose()

            ()
        let t2 = (DateTime.UtcNow)
        let elapse = t2.Subtract(t1)
        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Processed %d Files with total %d MB in %f sec, throughput = %f MB/s, avg recogtime = %d ms, %d files failed (%f\%%)" 
                                                       (!numSucceedFiles + !numFailedFiles) (!total) elapse.TotalSeconds ((float !total)/elapse.TotalSeconds/1000000.) (if (!numSucceedFiles + !numFailedFiles) >0 then (!totalRecogMS/(int64 (!numSucceedFiles + !numFailedFiles))) else -1L)
                                                       !numFailedFiles (if (!numSucceedFiles + !numFailedFiles)>0 then (float !numFailedFiles)/(float (!numSucceedFiles + !numFailedFiles))*100.0 else -1.0)))
(*
    | "BatchAsyncA" ->       //batch upload and recognition 
        // load the images under imgRootDir
        let imgFiles =  Directory.GetFiles(imgRootDir, "*.jpg", SearchOption.AllDirectories) |> Array.ofSeq
        //Logger.LogF( LogLevel.Info,  fun _ -> sprintf "Source Images: %A" imgFiles  )
        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "found %d jpeg images" imgFiles.Length ))
        let t1 = (DateTime.UtcNow)
        let numFiles = ref 0
        let numFailedFiles = ref 0
        let total = ref 0L
        let totalRecogMS = ref 0L
        let serviceGuids = getAllServiceGUIDs serviceURL
        let rnd = Random()
        let recogImageAndShow imgFile = 
            async { 
                let Guid = if serviceGuid.Equals("Random") then serviceGuids.[rnd.Next(serviceGuids.Length)] else serviceGuid 
                let! recog = recog.recognizeAsync ( imgFile, serviceURL, Guid, distGuid, aggrGuid )
                let name, tuple = recog
                let recogResult, orgSize, recogSize, recogTicks = tuple 
                let recogMs = recogTicks / TimeSpan.TicksPerMillisecond
                Interlocked.Add( numFiles, 1 ) |> ignore
                if recogResult.Contains "Failed" || recogResult.Contains "Exception" then numFailedFiles := !numFailedFiles + 1
                Interlocked.Add( total,  int64 orgSize) |> ignore
                Interlocked.Add( totalRecogMS, int64 recogMs ) |> ignore
                Logger.LogF( LogLevel.WildVerbose, ( fun _ -> sprintf "Image %s is recognized as %s (org %dB, resized to %dB, use %d ms) " name recogResult orgSize recogSize recogMs))
                }

        let recogResult = Async.RunSynchronously ( Async.Parallel [for imgFile in imgFiles -> recogImageAndShow imgFile ] )
        let t2 = (DateTime.UtcNow)
        let elapse = t2.Subtract(t1)
        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "Processed %d Files with total %d MB in %f sec, throughput = %f MB/s, avg recogtime = %d ms, %d files failed(%f\%%)" 
                                                       !numFiles (!total>>>20) elapse.TotalSeconds ((float !total)/elapse.TotalSeconds/1000000.) (if !numFiles>0 then (!totalRecogMS/(int64 !numFiles)) else -1L)
                                                       !numFailedFiles (if !numFiles>0 then (float !numFailedFiles)/(float !numFiles)*100.0 else -1.0)))

                                                        *)
    | _ ->
        Logger.LogF( LogLevel.Info, ( fun _ -> sprintf "unknown command %s\n%s" cmd Usage))

    0
