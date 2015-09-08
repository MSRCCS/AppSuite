using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Web;
using System.Web.Mvc;
using VMHubClientLibrary;
using vHub.Data;
using System.Threading.Tasks;
using WebDemo.Models;
using System.Net;
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

        public static System.Drawing.Image resizeImage(System.Drawing.Image imgToResize, Size size)
        {
            return (System.Drawing.Image)(new Bitmap(imgToResize, size));
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
                        imageData = ImageProcessing.ResizeImageInJpeg(imageData, 600, 85L);
                        // only set ImageData for locally uploaded image for showing in HTML page
                        // for Web image, the image will be displayed directly from Web Url.
                        ViewBag.ImageData = imageData;
                    }
                    else if (!string.IsNullOrEmpty(imageUrl))
                    {
                        using (System.Net.WebClient webClient = new WebClient())
                            imageData = await webClient.DownloadDataTaskAsync(imageUrl);
                        imageData = ImageProcessing.ResizeImageInJpeg(imageData, 600, 85L);
                    }

                    result = await vmHub.ProcessAsyncString(Guid.Empty, Guid.Empty, Guid.Parse(Model.SelectedClassifier), Guid.Empty, Guid.Empty, imageData);
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