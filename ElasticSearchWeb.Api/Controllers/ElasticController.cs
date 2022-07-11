using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ElasticSearchWeb.Api.Models;
using Nest;
using StackExchange.Redis;

namespace ElasticSearchWeb.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ElasticController : ControllerBase
    {
        private readonly ILogger<ElasticController> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IElasticClient _elasticClient;
        private readonly IConnectionMultiplexer _redis;
        private static readonly string userId = "dogukan_yildiz";
        public ElasticController(ILogger<ElasticController> logger, IHttpClientFactory httpClient, IElasticClient elasticClient, IConnectionMultiplexer connection)
        {
            _logger = logger;
            _httpClient = httpClient;
            _elasticClient = elasticClient;
            _redis = connection;
        }

        [HttpGet("SetData")]
        public async Task<IActionResult> SetData()
        {
            var client = _httpClient.CreateClient("client");
            var result = await client.GetStringAsync("");
            var albums = JsonConvert.DeserializeObject<List<Album>>(result);  /// get data from jsonplaceholder with httpclient name factory
           
            var bulkdescriptor = new BulkDescriptor();

            foreach (var item in albums)
            {
                bulkdescriptor.Index<Album>(i => i.Document(item).Id(item.Id));
            }
            var bulk= _elasticClient.Bulk(bulkdescriptor);

            if (bulk.Errors)
                return BadRequest();
          
            await _redis.GetDatabase().StringSetAsync(userId, JsonConvert.SerializeObject(albums));
            return Ok(albums);

        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("test");
            //var albums = await _elasticClient.SearchAsync<Album>(s => s.Query(q=> q.Bool(b=> b.Must(m=> m.Exists(e=> e.Field(f=> f.Id))))));

            var request = await _elasticClient.SearchAsync<Album>(s => s.From(0).Size(1000).MatchAll());

            if (request.Documents is null)
                return NotFound("Album not found ");

            var response = await _redis.GetDatabase().StringGetAsync(userId);

            var data = JsonConvert.DeserializeObject<List<Album>>(response);


            return Ok(data);

        }

        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(string id)
        {
            //var albums = await _elasticClient.SearchAsync<Album>(s => s.Query(q=> q.Bool(b=> b.Must(m=> m.Exists(e=> e.Field(f=> f.Id))))));

            var album = await _elasticClient.SearchAsync<Album>(a => a.Query(q => q.Term(t => t.Id, id)));             // 1. method

            var album_2 = await _elasticClient.GetAsync(new DocumentPath<Album>(id));                                  // 2.method
               
            if (album_2.Source is null)
                return NotFound("Album not found ");


            return Ok(album_2.Source);

        }

        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] Album album)
        {

           var result = await _elasticClient.IndexAsync(album,x=> x.Id(album.Id));

            if (!result.IsValid)
                return BadRequest();

            var response = await _elasticClient.SearchAsync<Album>(a => a.From(0).Size(1000).MatchAll());

            await _redis.GetDatabase().StringSetAsync(userId, JsonConvert.SerializeObject(response.Documents.ToList()));

            return Ok(true);

        }
        [HttpPost("AddBulk")]
        public async Task<IActionResult> AddBulk([FromBody] AlbumDto album)
        {

            var bulkdescriptor = new BulkDescriptor();

            foreach (var item in album.Albums)
            {
                bulkdescriptor.Index<Album>(x => x.Document(item).Id(item.Id));
            }

           var response = await _elasticClient.BulkAsync(bulkdescriptor);

            if (!response.IsValid)
                return BadRequest();

            var all_data = await _elasticClient.SearchAsync<Album>(a => a.From(0).Size(1000).MatchAll());

            await _redis.GetDatabase().StringSetAsync(userId, JsonConvert.SerializeObject(all_data.Documents.ToList()));


            return Ok(true);

        }

        [HttpDelete("DeleteById")]
        public async Task<IActionResult> DeleteById(string id)
        {
            var album = await _elasticClient.GetAsync(new DocumentPath<Album>(id));
            if (album.Source == null)
                return NotFound();

            var request = await _elasticClient.DeleteByQueryAsync<Album>(x => x.Query(q => q.Term(t => t.Id, id)));

            if (!request.IsValid)
                return BadRequest();
            var all_data = await _elasticClient.SearchAsync<Album>(a => a.From(0).Size(1000).MatchAll());

            await _redis.GetDatabase().StringSetAsync(userId, JsonConvert.SerializeObject(all_data.Documents.ToList()));

            return Ok(true);

        }


        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] Album album)
        {
            var album_ = await _elasticClient.GetAsync(new DocumentPath<Album>(album.Id));
            if (album_.Source == null)
                return NotFound();
            
            var request = await _elasticClient.UpdateAsync<Album>(album.Id, x => x.Index("albums").Doc(new Album { Title = album.Title }));

            if (!request.IsValid)
                return BadRequest();
            var all_data = await _elasticClient.SearchAsync<Album>(a => a.From(0).Size(1000).MatchAll());

            await _redis.GetDatabase().StringSetAsync(userId, JsonConvert.SerializeObject(all_data.Documents.ToList()));
            return Ok(true);

        }

        [HttpGet("getTitleByFilter")]
        public async Task<IActionResult> getTitleByFilter(string text)
        {
        
                 
         var response = await _elasticClient.SearchAsync<Album>(s => s
         .Query(q => q
           .Wildcard(c => c
            .Field(f => f.Title)
             .Value($"{text}*"))));

            //        var response = await _elasticClient.SearchAsync<Album>(s => s.Query(q => q      2.method
            //.QueryString(d => d.Query("title:test*"))).From(0).Size(100));
            if (!response.Documents.Any())
                return NotFound();
            
            
            return Ok(response.Documents);

        }
  

    }
}
