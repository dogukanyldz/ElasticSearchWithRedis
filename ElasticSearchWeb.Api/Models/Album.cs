using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearchWeb.Api.Models
{
    public class Album
    {
        public string Id { get; set; }
       
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
