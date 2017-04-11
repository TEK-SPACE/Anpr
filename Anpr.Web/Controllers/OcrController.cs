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
                try
                {
                    content = content.Replace("-nan", "0");
                    imageResponse = JsonConvert.DeserializeObject<ImageResponse>(content);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
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

            foreach (var result in imageResponse.Results)
            {
                foreach (var candidate in result.Candidates)
                {
                    candidate.Violation = Violation(candidate.Plate);
                    candidate.Expired = Expired(candidate.Plate);
                    candidate.ValidPayments = ValidPayments(candidate.Plate);
                    candidate.NoMatches = NoMatches(candidate.Plate);
                    candidate.AssignedClass = candidate.Violation
                        ? "violation"
                        : candidate.Expired
                            ? "expired"
                                : candidate.ValidPayments
                                    ? "validPayment" : candidate.NoMatches ? "nomatch" : "empty";
                }
            }

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

        private bool Violation(string plateNumber)
        {
            bool hasViolations;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                hasViolations = context.ENF_Permits.Any(x => x.ENFPlateNo.Equals(plateNumber, StringComparison.OrdinalIgnoreCase));
            }

            return hasViolations;
        }

        private bool NoMatches(string plateNumber)
        {
            bool noMatch;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                noMatch = !context.EnfVendorTransactions.Any(x => x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase));
                if (noMatch)
                {
                    noMatch =
                        !context.ENF_Permits.Any(
                            x => x.ENFPlateNo.Equals(plateNumber, StringComparison.OrdinalIgnoreCase));
                }
                if (noMatch)
                {
                    noMatch = !(from pcp in context.PayByCellPlateTxns
                        join pv in context.ParkVehicles on pcp.VehicleId equals pv.VehicleID
                        where pv.LPNumber == plateNumber
                        select pv).Any();
                }
            }
            return noMatch;
        }

        private bool ValidPayments(string plateNumber)
        {
            bool hasValidPayments;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                hasValidPayments = context.EnfVendorTransactions.Any(x => x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase));
                if (hasValidPayments) return true;
                var dayBefore = DateTime.Now.AddDays(-1);
                hasValidPayments = (from pcp in context.PayByCellPlateTxns
                    join pv in context.ParkVehicles on pcp.VehicleId equals pv.VehicleID
                    join vt in context.EnfVendorTransactions on pv.LPNumber equals vt.PlateNumber
                    where
                    pv.LPNumber == plateNumber &&
                    (pcp.TransDateTime > dayBefore && pcp.TransDateTime < DateTime.Now)
                    && vt.ExpiryDate > DateTime.Now
                    select pv).Any();
            }
            return hasValidPayments;
        }

        private bool Expired(string plateNumber)
        {
            bool isExpired;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                isExpired = !context.EnfVendorTransactions.Any(x => 
                x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)
                && x.ExpiryDate < DateTime.Now);
                if (isExpired)
                {
                    isExpired =
                    (from pv in context.ParkVehicles
                        join pcp in context.PayByCellPlateTxns on pv.VehicleID equals pcp.VehicleId
                        where pv.LPNumber == plateNumber && pcp.ExpiryDateTime < DateTime.Now
                        select pv).Any();
                }
            }

            return isExpired;
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