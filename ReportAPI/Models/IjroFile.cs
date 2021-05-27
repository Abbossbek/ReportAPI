using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReportAPI.Models
{
    public class IjroFile
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}