using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using Elasticsearch.Net;
using StackExchange.Redis;

namespace ElasticSearchWeb.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ElasticSearchWeb.Api", Version = "v1" });
            });
            services.AddHttpClient("client", x =>
             {
                 x.BaseAddress = new Uri(Configuration.GetValue<string>("ApiUrl"));

             });
            services.AddSingleton<IConnectionMultiplexer>(x =>
            {
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
                return redis;

            });

            services.AddSingleton<IElasticClient>(x =>
            {
                var pool = new SingleNodeConnectionPool(new Uri(Configuration.GetValue<string>("ElastiSearch")));
                var settings = new ConnectionSettings(pool)
                    .DefaultIndex("albums");
                var client = new ElasticClient(settings);
                return client;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ElasticSearchWeb.Api v1"));
            }
            


            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
