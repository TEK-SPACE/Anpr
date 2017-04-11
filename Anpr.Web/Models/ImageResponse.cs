using Newtonsoft.Json;
using System.Collections.Generic;

namespace ANPR.Models
{
    public class ImageResponse
    {

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("data_type")]
        public string DataType { get; set; }
        [JsonProperty("epoch_time")]
        public long EpochTime { get; set; }

        [JsonProperty("img_width")]
        public int ImgWidth { get; set; }

        [JsonProperty("img_height")]
        public int ImgHeight { get; set; }

        [JsonProperty("processing_time_ms")]
        public double ProcessingTimeMs { get; set; }

        [JsonProperty("regions_of_interest")]
        public List<Interest> RegionsOfInterest { get; set; }

        [JsonProperty("results")]
        public List<Result> Results { get; set; }
    }


    public class Result
    {
        [JsonProperty("plate")]
        public string Plate { get; set; }
        [JsonProperty("confidence")]
        public decimal Confidence { get; set; }
        [JsonProperty("coordinates")]
        public List<Coordinate> Coordinates { get; set; }
        [JsonProperty("candidates")]
        public List<Candidate> Candidates { get; set; }
    }

    public class Candidate
    {
        [JsonProperty("plate")]
        public string Plate { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("matches_template")]
        public int MatchesTemplate { get; set; }

        public bool Violation { get; set; }
        public bool Expired { get; set; }
        public bool ValidPayments { get; set; }
        public bool NoMatches { get; set; }

        public string AssignedClass { get; set; }

    }

    public class Coordinate
    {
        [JsonProperty("x")]
        public int X { get; set; }
        [JsonProperty("y")]
        public int Y { get; set; }
    }
    public class Interest
    {
        [JsonProperty("x")]
        public int X { get; set; }
        [JsonProperty("y")]
        public int Y { get; set; }
        [JsonProperty("width")]
        public int Width { get; set; }
        [JsonProperty("height")]
        public int Height { get; set; }
    }
}