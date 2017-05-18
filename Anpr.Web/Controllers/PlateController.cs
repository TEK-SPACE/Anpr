using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using ANPR.Models;
using ANPR.Utitlities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ANPR.Controllers
{
    public class PlateController : BaseApiController
    {
        [HttpPost]
        public async Task<JsonResult<Candidate>> Post(string number)
        {
            Candidate candidate = new Candidate();
            string responseContent;
            using (var httpClient = new HttpClient())
            {
                var httpRequestMessage =
                    new HttpRequestMessage(HttpMethod.Post, BaseUri)
                    {
                        Content = new StringContent($"{{plateNumber:{number}}}")
                    };
                HttpContent httpContent = new HttpMessageContent(httpRequestMessage);
                //httpContent.Headers.Add("Content-Type", "application/json");
                var response = await httpClient.PostAsync(BaseUri, httpContent);
                responseContent = await response.Content.ReadAsStringAsync();
            }

            if (!string.IsNullOrWhiteSpace(responseContent))
            {
                try
                {
                    candidate = JsonConvert.DeserializeObject<Candidate>(responseContent);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            if (candidate == null)
            {
                candidate = JsonConvert.DeserializeObject<Candidate>(@"..\Sample\candidate.json".Load());
            }
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return Json(candidate, jsonSerializerSettings);
        }
    }
}
