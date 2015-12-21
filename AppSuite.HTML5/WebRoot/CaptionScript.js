(function()
{
    $('#findSimilar').hide();
    var maxWidth = 1200;
    var maxHeight = 1200;

    var urlParams = getUrlParams(window.location);
    var gateway = urlParams.gateway ? urlParams.gateway : window.location.hostname;
    var prajnaClient = new PrajnaClient(gateway, EmptyGUID, "SecretKeyShouldbeLongerThan10");
    var serviceGUID = urlParams.serviceguid ? urlParams.serviceguid : "c03f7af8-b8ed-3c0e-bb6b-d38cc72f7260";

    function getUrlParams(url)
    {
        var params = {};
        
        if (location.search) {
            var parts = url.search.substring(1).split('&');
        
            for (var i = 0; i < parts.length; i++) {
                var nv = parts[i].split('=');
                if (!nv[0]) continue;
                params[nv[0].toLowerCase()] = nv[1] || true;
            }
        }
        
        return params;
    }

    function getScalingRatio(width, height)
    {
        var ratio = width / maxWidth;
        if (ratio < height / maxHeight)
            ratio = height / maxHeight;
        return ratio;
    }
    
    function rotateImageToRightDirection(imgDataUrl, callBack)
    {
        //For IOS
        //http://stackoverflow.com/questions/16336158/wrong-orientation-when-image-captured-by-html5-file-api-on-ios-6-0
        //http://chariotsolutions.com/blog/post/take-and-manipulate-photo-with-web-page/
        
        var byteString;
        if (imgDataUrl.split(',')[0].indexOf('base64') >= 0) {
            byteString = atob(imgDataUrl.split(',')[1]);
        }
        else {
            byteString = unescape(imgDataUrl.split(',')[1]);
        }
        
        var exif, transform = "none";
        exif = EXIF.readFromBinaryFile(new BinaryFile(byteString));
        if (exif.Orientation === 8)
            transform = "left";
        else if (exif.Orientation === 6)
            transform = "right";
        else if (exif.Orientation === 3)
            transform = "flip";
        
        var img = new Image();
        img.onload = function() {
            var srcWidth = img.width;
            var srcHeight = img.height;
            var c=document.getElementById("cropCanvas");
            var ctx=c.getContext("2d");
            if(transform === 'right') {
                // should rotate, so the x, y will be reversed
                ratio = getScalingRatio(srcHeight, srcWidth);
                c.width = srcHeight/ratio;
                c.height = srcWidth/ratio;
                ctx.rotate(90*Math.PI/180);
                ctx.translate(0, -srcHeight/ratio);
            } else if(transform === 'left') {
                // should rotate, so the x, y will be reversed
                ratio = getScalingRatio(srcHeight, srcWidth);
                c.width = srcHeight/ratio;
                c.height = srcWidth/ratio;
                ctx.rotate(-90*Math.PI/180);
                ctx.translate(-srcWidth/ratio, 0);
            } else if(transform === 'flip') {
                ratio = getScalingRatio(srcWidth, srcHeight);
                c.width = srcWidth/ratio;
                c.height = srcHeight/ratio;
                ctx.rotate(Math.PI);
                ctx.translate(-srcWidth/ratio, -srcHeight/ratio);
            } else {
                ratio = getScalingRatio(srcWidth, srcHeight);
                c.width = srcWidth/ratio;
                c.height = srcHeight/ratio;
            }
            ctx.drawImage(img, 0, 0, srcWidth, srcHeight, 0, 0, srcWidth/ratio, srcHeight/ratio);
            callBack(c.toDataURL('image/jpeg', 0.9));
        };
        img.src = imgDataUrl;
    }
    
    function recognizeImage(imageDataUrl)
    {
        var c=document.getElementById("resultCanvas");
        var ctx=c.getContext("2d");
        var img = new Image();
        img.onload = function() {
            ratio = getScalingRatio(img.width, img.height);
            c.width = img.width/ratio;
            c.height = img.height/ratio;
            ctx.drawImage(img, 0, 0, img.width, img.height, 0, 0, c.width, c.height)
            $("#spinner").show();
        };
        img.src = imageDataUrl;
        
        var reqString = prajnaClient.FormReqServiceString(EmptyGUID, EmptyGUID, serviceGUID, EmptyGUID, EmptyGUID);
        // "d6297090-d72c-9507-2bf4-d2dfcfc67b61", EmptyGUID, EmptyGUID)
        var reqUrl = prajnaClient.FormServiceURL(reqString);
        $.ajax({
            type: "POST",
            url: reqUrl,
            contentType: false,
            processData: false,
            data: dataURLToBlob2(imageDataUrl)
        })
        .done(function (responseData) {
            console.log(JSON.stringify(responseData));
            getRequestCallBack(responseData);
        })
        .fail(function (jqXHR, textStatus) {
            $("#result").html("Request failed. Please check the status of vm-hub server.");
        });
    }
                
    function displayImage(input) {
        $("#result").hide();
        if (input.files && input.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                // For IOS
                rotateImageToRightDirection(e.target.result, recognizeImage);
            }
            reader.readAsDataURL(input.files[0]);
                        
        }
        $("#cropCanvas").hide();
    }
    
    
	function dataURLToBlob2(dataURL) {
		var BASE64_MARKER = ';base64,';
		if (dataURL.indexOf(BASE64_MARKER) == -1) {
		  var parts = dataURL.split(',');
		  var contentType = parts[0].split(':')[1];
		  var raw = decodeURIComponent(parts[1]);

		  return new Blob([raw], {type: contentType});
		}

		var parts = dataURL.split(BASE64_MARKER);
		var contentType = parts[0].split(':')[1];
		var raw = window.atob(parts[1]);
		var rawLength = raw.length;

		var uInt8Array = new Uint8Array(rawLength);

		for (var i = 0; i < rawLength; ++i) {
		  uInt8Array[i] = raw.charCodeAt(i);
		}
		return new Blob([uInt8Array], {type: "multipart/form-data"});
	}
    
    function getRequestCallBack(responseData)
    {
        $("#spinner").hide();
        $("#result").show();

        var results = responseData.Description;
        $("#result").html(results);
    }
    
    $("#spinner").hide();
    window.displayImage = displayImage;
}
)();