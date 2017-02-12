using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace ANPR.Controllers
{
    public class OcrController : ApiController
    {
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
        public async Task Post([FromBody]string value)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("","");
                client.BaseAddress = new Uri("http://132.148.85.241:8000/​");
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("", "login")
                });
                var result = await client.PostAsync("/", content);
                string resultContent = await result.Content.ReadAsStringAsync();

                //If this doesnt return any result use sample response and parse

                Console.WriteLine(resultContent);
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