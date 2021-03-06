﻿using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Services.FileSystem;
using Newtonsoft.Json.Linq;

namespace Satrabel.OpenContent.Components.TemplateHelpers
{
    public static class ImageHelper
    {
        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="columnWidth">Size of the image. In Bootstrap 12th</param>
        /// <param name="isMobile"></param>
        /// <param name="retina"></param>
        /// <returns></returns>
        public static int CalculateMaxPixels(float columnWidth, bool isMobile, bool retina = true)
        {
            if (columnWidth < 0 || columnWidth > 1) columnWidth = 1;
            if (isMobile && retina)
            {
                return Convert.ToInt32(2 * 768 * columnWidth);
            }
            if (isMobile && !retina)
            {
                return Convert.ToInt32(2 * 480 * columnWidth);
            }
            return Convert.ToInt32(2 * 1200 * columnWidth);
        }



        public static string GetImageUrl(IFileInfo file, Ratio ratio)
        {
            if (file == null) throw new NoNullAllowedException("FileInfo should not be null");

            var url = file.ToUrl();
            if (url.Contains("LinkClick.aspx")) return url;
            if (ModuleDefinitionController.GetModuleDefinitionByFriendlyName("OpenDocument") == null) return url;

            url = url.RemoveQueryParams();

            if (file.ContentItemID > 0)
            {
                var contentItem = Util.GetContentController().GetContentItem(file.ContentItemID);
                if (contentItem != null)
                {
                    JObject content = JObject.Parse(contentItem.Content);
                    var crop = content["crop"];
                    if (crop is JObject)
                    {
                        foreach (var cropperobj in crop["croppers"].Children())
                        {
                            var cropper = cropperobj.Children().First();
                            int x = int.Parse(cropper["x"].ToString());
                            int y = int.Parse(cropper["y"].ToString());
                            int w = int.Parse(cropper["width"].ToString());
                            int h = int.Parse(cropper["height"].ToString());
                            var cropratio = new Ratio(w, h);
                            if (Math.Abs(cropratio.AsFloat - ratio.AsFloat) < 0.02) //allow 2% margin
                            {
                                return url + string.Format("?crop={0},{1},{2},{3}", x, y, w, h);
                            }
                        }
                    }
                }
            }

            return url + string.Format("?width={0}&height={1}&mode=crop", ratio.Width, ratio.Height);
        }


        public static Image Resize(Image image, int scaledWidth, int scaledHeight)
        {
            return new Bitmap(image, scaledWidth, scaledHeight);
        }

        public static Image Crop(Image image, int x, int y, int width, int height)
        {
            var croppedBitmap = new Bitmap(width, height);

            using (var g = Graphics.FromImage(croppedBitmap))
            {
                g.DrawImage(image,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            }

            return croppedBitmap;
        }
        /*
        public static Image Center(Image image, int width, int height)
        {

            Bitmap sourceImage; = image.
            int targetWidth = 200;
            int targetHeight = 200;

            int x = image.Width / 2 - width / 2;
            int y = image.Height / 2 - height / 2;

            Rectangle cropArea = new Rectangle(x, y, targetWidth, targetHeight);

            Bitmap targetImage = image.Clone(cropArea, image.PixelFormat);

            return targetImage;
        }
         */
        public static Image SaveCroppedImage(Image image, int targetWidth, int targetHeight, out int left, out int top, out int srcWidth, out int srcHeight)
        {
            ImageCodecInfo jpgInfo = ImageCodecInfo.GetImageEncoders().Where(codecInfo => codecInfo.MimeType == "image/jpeg").First();
            Image finalImage = image;
            System.Drawing.Bitmap bitmap = null;
            left = 0;
            top = 0;
            srcWidth = 0;
            srcHeight = 0;
            try
            {
                srcWidth = targetWidth;
                srcHeight = targetHeight;
                bitmap = new System.Drawing.Bitmap(targetWidth, targetHeight);
                double croppedHeightToWidth = (double)targetHeight / targetWidth;
                double croppedWidthToHeight = (double)targetWidth / targetHeight;

                if (image.Width > image.Height)
                {
                    srcWidth = (int)(Math.Round(image.Height * croppedWidthToHeight));
                    if (srcWidth < image.Width)
                    {
                        srcHeight = image.Height;
                        left = (image.Width - srcWidth) / 2;
                    }
                    else
                    {
                        srcHeight = (int)Math.Round(image.Height * ((double)image.Width / srcWidth));
                        srcWidth = image.Width;
                        top = (image.Height - srcHeight) / 2;
                    }
                }
                else
                {
                    srcHeight = (int)(Math.Round(image.Width * croppedHeightToWidth));
                    if (srcHeight < image.Height)
                    {
                        srcWidth = image.Width;
                        top = (image.Height - srcHeight) / 2;
                    }
                    else
                    {
                        srcWidth = (int)Math.Round(image.Width * ((double)image.Height / srcHeight));
                        srcHeight = image.Height;
                        left = (image.Width - srcWidth) / 2;
                    }
                }
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height), new Rectangle(left, top, srcWidth, srcHeight), GraphicsUnit.Pixel);
                }
                finalImage = bitmap;
            }
            catch { }

            /*
            try
            {
                using (EncoderParameters encParams = new EncoderParameters(1))
                {
                    encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)100);
                    //quality should be in the range [0..100] .. 100 for max, 0 for min (0 best compression)
                    finalImage.Save(filePath, jpgInfo, encParams);
                    return true;
                }
            }
            catch { }
             */
            if (bitmap != null)
            {
                //bitmap.Dispose();
            }
            //return false;
            return finalImage;
        }


    }
}