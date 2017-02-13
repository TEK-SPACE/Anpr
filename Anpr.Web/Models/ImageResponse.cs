using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ANPR.Models
{
    public class ImageResponse
    {
        [JsonProperty("results")]
        public List<ResponseResult> Results { get; set; }
    }

    public class ResponseResult
    {
        [JsonProperty("candidates")]
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonProperty("plate")]
        public string Plate { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }
}