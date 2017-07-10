using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using ANPR.Models;
using ANPR.Utitlities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ANPR.Controllers
{
    public class OcrController : BaseApiController
    {
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

        // POST api/<controller>
        [HttpPost]
        public async Task<JsonResult<ImageResponse>> Post(int id)
        {
            string content = null;
            ImageResponse imageResponse = null;
            var httpRequest = HttpContext.Current.Request;
            Dictionary<string, object> dict = new Dictionary<string, object>();

            var fileBytesRequest = Request.Content.ReadAsByteArrayAsync().Result;

            //foreach (string file in httpRequest.Files)
            //{
            //var postedFile = httpRequest.Files[file];
            //if (postedFile != null && postedFile.ContentLength > 0)
            //{
            //int MaxContentLength = 1024 * 1024 * 1; //Size = 1 MB  
            //IList<string> allowedFileExtensions = new List<string> {".jpg", ".gif", ".png"};
            //var ext = postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.'));
            //var extension = ext.ToLower();
            //var filePath =
            //    HttpContext.Current.Server.MapPath("~/images/" + postedFile.FileName + extension);
            //postedFile.SaveAs(filePath);

            //byte[] fileBytes = File.ReadAllBytes(filePath);
            ByteArrayContent byteArrayContent = new ByteArrayContent(fileBytesRequest);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("image/jpeg")); //ACCEPT header
            httpClient.DefaultRequestHeaders.Add("image-type", "jpeg");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type",
                "image/jpeg; charset=utf-8");
            try
            {
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(BaseUri, byteArrayContent);
                content = httpResponseMessage.Content.ReadAsStringAsync().Result;
            }
            catch (Exception e)
            {
                throw new Exception(BaseUri + e.ToString());
            }

            //}
            //}

            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    content = content.Replace("-nan", "0");
                    imageResponse = JsonConvert.DeserializeObject<ImageResponse>(content);
                }
                catch (Exception e)
                {
                    throw new Exception(BaseUri + content);
                }
            }

            //if (imageResponse?.Results == null || !imageResponse.Results.Any())
            //{
            //    imageResponse = JsonConvert.DeserializeObject<ImageResponse>(@"..\Sample\response.json".Load());
            //}
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
                    candidate.Violation = Violation(candidate.Plate, id);
                    candidate.Expired = Expired(candidate.Plate, id);
                    candidate.ValidPayments = ValidPayments(candidate.Plate, id);
                    candidate.NoMatches = NoMatches(candidate.Plate, id);
                    candidate.AssignedClass = candidate.Violation
                        ? "violation"
                        : candidate.Expired
                            ? "expired"
                            : candidate.ValidPayments
                                ? "validPayment"
                                : candidate.NoMatches
                                    ? "nomatch"
                                    : "empty";
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

        private bool Violation(string plateNumber, int customerId)
        {
            bool hasViolations;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                hasViolations = context.ENF_Permits.Any(x => 
                x.ENFPlateNo.Equals(plateNumber, StringComparison.OrdinalIgnoreCase) && x.ENFCustomerId.Equals(customerId));
            }

            return hasViolations;
        }

        private bool NoMatches(string plateNumber, int customerId)
        {
            bool noMatch;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                noMatch = !context.EnfVendorTransactions.Any(x => x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase));
                if (noMatch)
                {
                    noMatch =
                        !context.ENF_Permits.Any(
                            x => x.ENFPlateNo.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)
                                 && x.ENFCustomerId.Equals(customerId));
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

        private bool ValidPayments(string plateNumber, int customerId)
        {
            bool hasValidPayments;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                hasValidPayments = context.EnfVendorTransactions.Any(x => x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)
                                                                          && x.EnfCustomerId.HasValue && x.EnfCustomerId.Value.Equals(customerId));
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

        private bool Expired(string plateNumber, int customerId)
        {
            bool isExpired;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                isExpired = !context.EnfVendorTransactions.Any(x => 
                x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)
                && x.EnfCustomerId.HasValue && x.EnfCustomerId.Value.Equals(customerId)
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