using ANPR.Models;
using ANPR.Utitlities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

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
        public async Task<ActionResult> Upload(FormCollection formCollection)
        {
            var baseUri = "http://localhost:49977/api/Ocr";
            string boundary = "----CustomBoundary" + DateTime.Now.Ticks.ToString("x");

        HttpPostedFileBase file = Request?.Files[0];

            if (file == null || (file.ContentLength <= 0) || string.IsNullOrEmpty(file.FileName))
                return new EmptyResult();

            string fileName = file.FileName;
            byte[] paramFileBytes = new byte[file.ContentLength];

            HttpContent stringContent = new StringContent(fileName);
            HttpContent fileStreamContent = new StreamContent(file.InputStream);
            HttpContent bytesContent = new ByteArrayContent(paramFileBytes);
            ImageResponse imageResponse = null;

            using (var formDataContent = new MultipartFormDataContent())
            {
                formDataContent.Add(stringContent, "param1", "param1");
                formDataContent.Add(fileStreamContent, "file1", "file1");
                formDataContent.Add(bytesContent, "imageUploaded", "imageUploaded");
                using (var httpClient = new HttpClient())
                {
                    formDataContent.Headers.ContentType =
          MediaTypeHeaderValue.Parse("multipart/form-data; boundary=" + boundary);

                    ByteArrayContent byteContent = new ByteArrayContent(paramFileBytes);
                    byteContent.Headers.ContentType =
          MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.PostAsync(baseUri, byteContent);
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        content = content.Replace("-nan", "0");
                        imageResponse = JsonConvert.DeserializeObject<ImageResponse>(content);
                    }
                }
            }

            if (imageResponse?.Results == null || !imageResponse.Results.Any())
            {
                imageResponse = JsonConvert.DeserializeObject<ImageResponse>(@"..\Sample\response.json".Load());
            }

            return View("Result", imageResponse);
        }

        [HttpPost]
        public async Task<ActionResult> Other(FormCollection formCollection)
        {
            var baseUri = "http://132.148.85.241:8000/";
            HttpPostedFileBase file = Request?.Files[0];

            if (file == null || (file.ContentLength <= 0) || string.IsNullOrEmpty(file.FileName))
                return new EmptyResult();

            string fileName = file.FileName;
            byte[] paramFileBytes = new byte[file.ContentLength];

            HttpContent stringContent = new StringContent(fileName);
            HttpContent fileStreamContent = new StreamContent(file.InputStream);
            HttpContent bytesContent = new ByteArrayContent(paramFileBytes);
            ImageResponse imageResponse = null;

            using (var formDataContent = new MultipartFormDataContent())
            {
                formDataContent.Add(stringContent, "param1", "param1");
                formDataContent.Add(fileStreamContent, "file1", "file1");
                formDataContent.Add(bytesContent, "imageUploaded", "imageUploaded");
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Image-type", "jpeg");

                    var response = await httpClient.PostAsync(baseUri, formDataContent);
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        content = content.Replace("-nan", "0");
                        imageResponse = JsonConvert.DeserializeObject<ImageResponse>(content);
                    }
                }
            }
            if (imageResponse?.Results == null || !imageResponse.Results.Any())
            {
                imageResponse = JsonConvert.DeserializeObject<ImageResponse>(@"..\Sample\response.json".Load());
            }

            return View("Result", imageResponse);
        }
    }
}
