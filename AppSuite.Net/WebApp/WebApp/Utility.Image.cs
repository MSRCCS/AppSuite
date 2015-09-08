using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Utility
{
    public class ImageProcessing
    {
        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo returncodec = null;
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    returncodec = codec;
            }
            return returncodec;
        }

        static public Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var g = Graphics.FromImage(destImage))
            {
                //g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                //g.SmoothingMode = SmoothingMode.HighQuality;
                //g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                //using (var wrapMode = new ImageAttributes())
                //{
                //    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                //    g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                //}
                g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            }

            return destImage;
        }

        static public byte[] ResizeImageInJpeg(byte[] imgData, int maxImageSize = 0, Int64 quality = 85L)
        {
            using (Stream mr = new MemoryStream(imgData))
            using (Bitmap streamImg = (Bitmap)Bitmap.FromStream(mr))
            {
                Bitmap img = streamImg;
                if (maxImageSize > 0)
                {
                    int w = img.Width, h = img.Height;
                    if (w > maxImageSize || h > maxImageSize)
                    {
                        if (w > h)
                        {
                            w = maxImageSize;
                            h = img.Height * maxImageSize / img.Width;
                        }
                        else
                        {
                            w = img.Width * maxImageSize / img.Height;
                            h = maxImageSize;
                        }
                        img = ResizeImage(streamImg, w, h);
                    }
                }

                if (img.PixelFormat != PixelFormat.Format24bppRgb)
                    img = img.Clone(new Rectangle(0, 0, img.Width, img.Height), PixelFormat.Format24bppRgb);

                // if img not changed (neither resized nor cloned), just return the original imgData;
                if (img == streamImg)
                    return imgData;

                // save image to jpg format
                var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                var myEncoder = System.Drawing.Imaging.Encoder.Quality;
                var myEncoderParas = new EncoderParameters(1);
                var myEncoderPara = new EncoderParameter(myEncoder, quality);
                myEncoderParas.Param[0] = myEncoderPara;

                var mw = new MemoryStream();
                img.Save(mw, jpgEncoder, myEncoderParas);
                return mw.ToArray();
            }
        }
    }
}