using ANPR.Models;
using ANPR.Utitlities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;

namespace ANPR.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        [HttpGet]
        public ActionResult Upload()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        [HttpPost]
        public ActionResult Upload(FormCollection formCollection)
        {
            if (Request != null)
            {
                HttpPostedFileBase file = Request.Files[0];

                if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                {
                    string fileName = file.FileName;
                    string fileContentType = file.ContentType;
                    byte[] paramFileBytes = new byte[file.ContentLength];

                    HttpContent stringContent = new StringContent(fileName);
                    HttpContent fileStreamContent = new StreamContent(file.InputStream);
                    HttpContent bytesContent = new ByteArrayContent(paramFileBytes);

                    using (var client = new HttpClient())
                    {
                        using (var formData = new MultipartFormDataContent())
                        {
                            formData.Add(stringContent, "param1", "param1");
                            formData.Add(fileStreamContent, "file1", "file1");
                            formData.Add(bytesContent, "imageUploaded", "imageUploaded");
                            var response = client.PostAsync("http://132.148.85.241:8000/​", formData).Result;
                            //if (!response.IsSuccessStatusCode)
                            //{
                            //    return null;
                            //}
                            //var result = response.Content.ReadAsStreamAsync().Result;


                        }
                    }

                    ImageResponse imageResponse = JsonConvert.DeserializeObject<ImageResponse>(@"..\Sample\response.json".Load());
                    return View("Result", imageResponse);
                }
            }
            return new EmptyResult();
        }
    }
}
