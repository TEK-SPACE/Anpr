using System;
using ANPR.Models;
using ANPR.Utitlities;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
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
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        [HttpPost]
        public async Task<ActionResult> Upload(FormCollection formCollection)
        {
            Debug.Assert(Request.Url != null, "Request.Url != null");
            var baseUri = $"{Request.Url.Scheme}://{Request.Url.Host}:{Request.Url.Port}/{ConfigurationManager.AppSettings["VirtualPath"]}api/Ocr?id={formCollection["CustomerId"]}";

            HttpPostedFileBase file = Request?.Files[0];

            if (file == null || (file.ContentLength <= 0) || string.IsNullOrEmpty(file.FileName))
                return new EmptyResult();
            var directoryPath = Path.Combine(AssemblyDirectory.Replace("bin", ""), "images");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            var filePath = Path.Combine(directoryPath, file.FileName);
            file.SaveAs(filePath);

            string fileName = file.FileName;

            ImageResponse imageResponse = new ImageResponse();

            using (var formDataContent = new MultipartFormDataContent())
            {
                var stringContent = new StringContent(fileName);
                formDataContent.Add(stringContent, fileName);

                var streamContent = new StreamContent(file.InputStream);
                streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                streamContent.Headers.ContentLength = file.ContentLength;
                streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = fileName,
                    FileName = file.FileName
                };
                formDataContent.Add(streamContent);

                using (var httpClient = new HttpClient())
                {
                    HttpResponseMessage response;
                    string content;
                    try
                    {
                        response = await httpClient.PostAsync(baseUri, formDataContent);
                        content = await response.Content.ReadAsStringAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        content = content.Replace("-nan", "0");
                        imageResponse = JsonConvert.DeserializeObject<ImageResponse>(content);
                    }
                }
            }
            ViewBag.FilePath = fileName;
            return View("Result", imageResponse);
        }

        [HttpPost]
        public async Task<ActionResult> Other(FormCollection formCollection)
        {
            var licensePlateRecognationServerUri = ConfigurationManager.AppSettings["LicensePlateRecognationServer"];
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

                    var response = await httpClient.PostAsync(licensePlateRecognationServerUri, formDataContent);
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
