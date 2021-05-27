using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReportAPI.Models
{
    public class ConvertResult
    {
        [JsonProperty("fileUrl")]
        public string FileUrl { get; set; }
    }
}