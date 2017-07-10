using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ANPR.Controllers
{
    public class BaseApiController : ApiController
    {
        protected readonly string BaseUri = ConfigurationManager.AppSettings["LicensePlateRecognationServer"];

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
    }
}
