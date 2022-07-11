using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearchWeb.Api.Models
{
    public class AlbumDto
    {
        public AlbumDto()
        {
            Albums = new();
        }

        public List<Album> Albums { get; set; }
    }
}
