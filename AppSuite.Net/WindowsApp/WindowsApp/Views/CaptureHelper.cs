
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using WindowsApp.Common;
using WindowsApp.Data;


namespace WindowsApp
{
    class CaptureHelper : IDisposable
    {
        MediaCapture mediaCapture;
        ImageEncodingProperties imgEncodingProperties;
        MediaEncodingProfile videoEncodingProperties;
        public VideoDeviceController VideoDeviceController
        {
            get { return mediaCapture.VideoDeviceController; }
        }
        public async Task<MediaCapture> Initialize(CaptureUse primaryUse = CaptureUse.Photo)
        {
            // Create MediaCapture and init
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();
            mediaCapture.VideoDeviceController.PrimaryUse = primaryUse;

            // Create photo encoding properties as JPEG and set the size that should be used for photo capturing
            imgEncodingProperties = ImageEncodingProperties.CreateJpeg();
            imgEncodingProperties.Width = 640;
            imgEncodingProperties.Height = 480;
            // Create video encoding profile as MP4 
            videoEncodingProperties = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Vga);
            // Lots of properties for audio and video could be set here...
            return mediaCapture;
        }
        public async Task<StorageFile> CapturePhoto(string desiredName = "photo.jpg")
        {
            // Create new unique file in the pictures library and capture photo into it
            var photoStorageFile = await KnownFolders.PicturesLibrary.CreateFileAsync(desiredName, CreationCollisionOption.GenerateUniqueName);
            await mediaCapture.CapturePhotoToStorageFileAsync(imgEncodingProperties, photoStorageFile);
            return photoStorageFile;
        }
        public async Task<StorageFile> StartVideoRecording(string desiredName = "video.mp4")
        {
            // Create new unique file in the videos library and record video! 
            var videoStorageFile = await KnownFolders.VideosLibrary.CreateFileAsync(desiredName, CreationCollisionOption.GenerateUniqueName);
            await mediaCapture.StartRecordToStorageFileAsync(videoEncodingProperties, videoStorageFile);
            return videoStorageFile;
        }
        public async Task StopVideoRecording()
        {
            // Stop video recording
            await mediaCapture.StopRecordAsync();
        }
        public async Task StartPreview()
        {
            // Start Preview stream
            await mediaCapture.StartPreviewAsync();


        }
        public async Task StartPreview(IMediaExtension previewSink, double desiredPreviewArea)
        {
            // List of supported video preview formats to be used by the default preview format selector.
            var supportedVideoFormats = new List<string> { "nv12", "rgb32" };
            // Find the supported preview size that's closest to the desired size
            var availableMediaStreamProperties =
            mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview)
            .OfType<VideoEncodingProperties>()
            .Where(p => p != null && !String.IsNullOrEmpty(p.Subtype) && supportedVideoFormats.Contains(p.Subtype.ToLower()))
            .OrderBy(p => Math.Abs(p.Height * p.Width - desiredPreviewArea))
            .ToList();
            var previewFormat = availableMediaStreamProperties.FirstOrDefault();
            // Start Preview stream
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, previewFormat);
            await mediaCapture.StartPreviewToCustomSinkAsync(new MediaEncodingProfile { Video = previewFormat }, previewSink);
        }
        public async Task StopPreview()
        {
            // Stop Preview stream
            await mediaCapture.StopPreviewAsync();
        }
        public void Dispose()
        {
            if (mediaCapture != null)
            {
                mediaCapture.Dispose();
                mediaCapture = null;
            }
        }
    }
}
