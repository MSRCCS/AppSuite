using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Web;
using System.Web.Mvc;
using VMHubClientLibrary;
using VMHub.Data;
using System.Threading.Tasks;
using WebDemo.Models;
using System.Net;
using Newtonsoft.Json;
using Utility;

namespace WebDemo.Controllers
{
    public class VMHubController : Controller
    {
        const string cookieName = "VMHubCookies";
        GatewayHttpInterface vmHub = new GatewayHttpInterface("vm-hub.trafficmanager.net", Guid.Empty, "SecretKeyShouldbeLongerThan10");

        public async Task<GatewaysViewModel> PopulateOptions(string selectedGateway, string selectedClassifier)
        {
            var gatewayList = await vmHub.GetActiveGateways();

            RecogEngine[] providerList = await vmHub.GetActiveProviders();
            List<RecogInstance> classifierList = new List<RecogInstance>();
            foreach (var item in providerList)
            {
                vmHub.CurrentProvider = item;
                var lst = await vmHub.GetWorkingInstances();
                classifierList.AddRange(lst);
            }

            var model = new GatewaysViewModel
            {
                Gateways = new SelectList(gatewayList.Select(x => new { name = x.HostName }), 
                                        "name", "name", selectedGateway), 
                Classifiers = new SelectList(classifierList.Select(x => new { id = x.ServiceID, name = x.Name + " " + x.ServiceID.ToString() }), 
                                        "id", "name", selectedClassifier)
            };

            return model;
        }

        // GET: VMHub
        public async Task<ActionResult> Index(GatewaysViewModel Model)
        {
            string selectedClassifier = Model.SelectedClassifier;
            var cookie = Request.Cookies[cookieName];
            if (cookie != null)
                selectedClassifier = cookie.Value;
            var model = await PopulateOptions(Model.SelectedGateway, selectedClassifier);

            return View(model);
        }

        public static Bitmap DrawRectangle(Image img, RecogResult[] recogResult)
        {
            //Setup the drawing color map;
            Color bkColor = Color.Transparent;
            System.Drawing.Imaging.PixelFormat pf = default(System.Drawing.Imaging.PixelFormat);
            if (bkColor == Color.Transparent)
                pf = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            else
                pf = img.PixelFormat;

            var newImg = new Bitmap(img.Width, img.Height, pf);
            using (var g = Graphics.FromImage(newImg))
            using (Pen pen = new Pen(Color.Green, 3))
            using (Font arialFont = new Font("Arial", 15))
            {
                g.DrawImage(img, 0, 0, img.Width, img.Height);

                int facecnt = 0;
                foreach (var face in recogResult)
                {
                    var r = face.Rect;
                    g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);

                    string text = string.Format("Face{0}", facecnt);
                    g.DrawString(text, arialFont, Brushes.Green, new PointF(r.X, r.Y + r.Height));
                    facecnt++;
                }
            }

            return newImg;
        }

        // GET: VMHub
        [HttpPost]
        public async Task<ActionResult> Index(HttpPostedFileBase file, string imageUrl, GatewaysViewModel Model)
        {
            HttpCookie vmHubCookies = new HttpCookie(cookieName);
            vmHubCookies.Value = Model.SelectedClassifier;
            vmHubCookies.Expires = DateTime.Now.AddMonths(1);
            Response.Cookies.Add(vmHubCookies);

            if (file == null && string.IsNullOrEmpty(imageUrl))
                return RedirectToAction("Index");

            ViewBag.ImageUrl = imageUrl;

            string result = string.Empty;

            var updatedModel = await PopulateOptions(Model.SelectedGateway, Model.SelectedClassifier);

            if (Model.SelectedClassifier == null)
                result = "Classifier not selected!";
            else
            {
                try
                {
                    byte[] imageData = null;

                    if (file != null && file.ContentLength > 0)
                    {
                        using (BinaryReader br = new BinaryReader(file.InputStream))
                            imageData = br.ReadBytes(file.ContentLength);
                        imageData = ImageProcessing.ResizeImageInJpeg(imageData, 1600, 85L);
                        // only set ImageData for locally uploaded image for showing in HTML page
                        // for Web image, the image will be displayed directly from Web Url.
                        ViewBag.ImageData = imageData;
                    }
                    else if (!string.IsNullOrEmpty(imageUrl))
                    {
                        using (System.Net.WebClient webClient = new WebClient())
                            imageData = await webClient.DownloadDataTaskAsync(imageUrl);
                        imageData = ImageProcessing.ResizeImageInJpeg(imageData, 1600, 85L);
                    }

                    result = await vmHub.ProcessAsyncString(Guid.Empty, Guid.Empty, Guid.Parse(Model.SelectedClassifier), Guid.Empty, Guid.Empty, imageData);
                    try
                    {
                        var recogResult = JsonConvert.DeserializeObject<RecogResult[]>(result);
                        // draw rectangles
                        using (var ms = new MemoryStream(imageData))
                        using (var bmp = new Bitmap(ms))
                        using (var newImg = DrawRectangle(bmp, recogResult))
                        { 
                            ViewBag.ImageData = ImageProcessing.SaveImageToByteArray(newImg, 85L);
                        }
                    }
                    catch
                    {

                    }
                }
                catch (Exception e)
                {
                    result = string.Format("Image cannot be uploaded: {0}", e.Message);
                }

            }

            ViewBag.ResultString = result;

            return View(updatedModel);
        }

    }
}