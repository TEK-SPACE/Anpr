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
            var violationTypes = new List<string> { "BOOT", "TOW", "STOLEN", "WARNING" };
            foreach (var result in imageResponse.Results)
            {
                foreach (var candidate in result.Candidates)
                {
                    //candidate.Plate = candidate.Plate+ stateName
                    var violoationType = Violation(candidate.Plate, id);
                    candidate.Violation = !string.IsNullOrWhiteSpace(violoationType) && violationTypes.Any(x => x.Equals(violoationType, StringComparison.OrdinalIgnoreCase));
                    candidate.Expired = Expired(candidate.Plate, id);
                    candidate.ValidPayments = !string.IsNullOrWhiteSpace(violoationType) || ValidPayments(candidate.Plate, id);
                    candidate.NoMatches = NoMatches(candidate.Plate, id);
                    candidate.AssignedClass = candidate.Violation
                        ? $"Violation {violoationType}"
                        : candidate.ValidPayments
                            ? !string.IsNullOrWhiteSpace(violoationType) 
                            && !violationTypes.All(x => x.Equals(violoationType, StringComparison.OrdinalIgnoreCase)) ? $"Paid {violoationType} {GetRateName(customerId:id, plateNumber: candidate.Plate)}" : $"Paid {GetRateName(customerId:id, plateNumber: candidate.Plate)}"
                            : candidate.Expired
                                ? "Expired"
                                : candidate.NoMatches
                                    ? "NoMatch"
                                    : "Empty";
                }
            }

            return Json(imageResponse, jsonSerializerSettings);
        }

        private string GetRateName(int customerId, string plateNumber)
        {
            if (customerId == 7012)
            {
                using (PemsUsProEntities context = new PemsUsProEntities())
                {
                    return (from txn in context.PayByCellPlateTxns
                        join pv in context.ParkVehicles on txn.VehicleId equals pv.VehicleID
                        where txn.CustomerId == customerId && pv.LPNumber == plateNumber
                        select txn.TxnSeqNum).FirstOrDefault();
                }
            }
            return string.Empty;
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

        private string Violation(string plateNumber, int customerId)
        {
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                return context.ENF_Permits.FirstOrDefault(x =>
                    x.ENFPlateNo.Equals(plateNumber, StringComparison.OrdinalIgnoreCase) &&
                    x.ENFCustomerId.Equals(customerId))?.ENFType;
            }
        }

        private bool NoMatches(string plateNumber, int customerId)
        {
            bool noMatch;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                noMatch = !context.EnfVendorTransactions.Any(
                    x => x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)
                         && x.EnfCustomerId == customerId);
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

        public static DateTime UtcToZoneDateTime(DateTime dateTime, int customerId)
        {
            string clientZone;
            var customerZoneId = 0;
            using (PEMSRBAC_US_PROEntities context = new PEMSRBAC_US_PROEntities())
            {
                customerZoneId = context.CustomerProfiles.FirstOrDefault(x => x.CustomerId == customerId)?.TimeZoneID ??
                                 0;
            }
            if (customerZoneId < 1)
                return DateTime.UtcNow;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                clientZone = context.TimeZones.FirstOrDefault(x => x.TimeZoneID == customerZoneId)?.TimeZoneName;
            }
            if (string.IsNullOrWhiteSpace(clientZone))
                return DateTime.UtcNow;
            try
            {
                TimeZoneInfo clientTimeZone = TimeZoneInfo.FindSystemTimeZoneById(clientZone);
                DateTime converteDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, clientTimeZone);
                return converteDateTime;
            }
            catch (Exception e)
            {
                return DateTime.UtcNow;
            }
        }

        private bool ValidPayments(string plateNumber, int customerId)
        {
            bool hasValidPayments;
           
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                hasValidPayments = context.EnfVendorTransactions.Any(x => x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)
                                                                          && x.EnfCustomerId.HasValue && x.EnfCustomerId.Value.Equals(customerId));
                if (hasValidPayments) return true;
                var currentDateTime = UtcToZoneDateTime(DateTime.Now, customerId);
                var dayBefore = DateTime.Now.AddDays(-1);
                hasValidPayments = (from pcp in context.PayByCellPlateTxns
                    join pv in context.ParkVehicles on pcp.VehicleId equals pv.VehicleID
                    //join vt in context.EnfVendorTransactions on pv.LPNumber equals vt.PlateNumber
                    where
                    pv.LPNumber == plateNumber && pcp.CustomerId == customerId &&
                    (pcp.TransDateTime > dayBefore && pcp.TransDateTime < currentDateTime)
                    && pcp.ExpiryDateTime > currentDateTime
                                    select pv).Any();
            }
            return hasValidPayments;
        }

        private bool Expired(string plateNumber, int customerId)
        {
            bool isExpired;
            using (PemsUsProEntities context = new PemsUsProEntities())
            {
                var currentDateTime = UtcToZoneDateTime(DateTime.Now, customerId);
                isExpired = !context.EnfVendorTransactions.Any(x => 
                x.PlateNumber.Equals(plateNumber, StringComparison.OrdinalIgnoreCase)
                && x.EnfCustomerId.HasValue && x.EnfCustomerId.Value.Equals(customerId)
                && x.ExpiryDate < currentDateTime);
                if (isExpired)
                {
                    isExpired =
                    (from pv in context.ParkVehicles
                        join pcp in context.PayByCellPlateTxns on pv.VehicleID equals pcp.VehicleId
                        where pv.LPNumber == plateNumber && pcp.CustomerId == customerId && pcp.ExpiryDateTime < currentDateTime
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