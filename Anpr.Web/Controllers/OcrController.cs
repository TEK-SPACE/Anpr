using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using ANPR.Models;
using ANPR.Utitlities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Json = System.Web.Helpers.Json;

namespace ANPR.Controllers
{
    public class OcrController : ApiController
    {
        private readonly string _baseUri = "http://132.148.85.241:8000/";
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        [System.Web.Http.HttpPost]
        public async Task<JsonResult<ImageResponse>> Post()
        {
            var httpContent = Request.Content;
            //get file name from content disposition
            var fileName = httpContent.Headers.ContentDisposition?.FileName ?? "image.jpg";
            //Get file stream from the request content
            var fileStream = await httpContent.ReadAsStreamAsync();

            ImageResponse imageResponse = null;

            string content;
            using (var httpClient = new HttpClient())
            {
                var requestContent = new MultipartFormDataContent();
                var imageContent = new StreamContent(fileStream);
                imageContent.Headers.ContentType =
                    MediaTypeHeaderValue.Parse("image/jpeg");
                requestContent.Add(imageContent, "image", fileName);
                requestContent.Headers.Add("Image-type", "jpeg");
                var response = await httpClient.PostAsync(_baseUri, requestContent);
                content = await response.Content.ReadAsStringAsync();
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                content = content.Replace("-nan", "0");
                imageResponse = JsonConvert.DeserializeObject<ImageResponse>(content);
            }

            if (imageResponse?.Results == null || !imageResponse.Results.Any())
            {
                imageResponse = JsonConvert.DeserializeObject<ImageResponse>(@"..\Sample\response.json".Load());
            }
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return Json(imageResponse, jsonSerializerSettings);
        }

        public async Task<string> UploadImage(string url, byte[] imageData)
        {
            var requestContent = new MultipartFormDataContent();
            //    here you can specify boundary if you need---^
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType =
                MediaTypeHeaderValue.Parse("image/jpeg");

            requestContent.Add(imageContent, "image", "image.jpg");
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, requestContent);
                return await response.Content.ReadAsStringAsync();
            }
        }
        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}