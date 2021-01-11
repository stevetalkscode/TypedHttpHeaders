using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Linq;
using ExampleApi.Swagger;
using Microsoft.Extensions.Logging;
using SteveTalksCode.StrongTypedHeaders;

namespace ExampleApi
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

            services.AddLogging( l => l.AddConsole());
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestHeaderMapping", Version = "v1" });
                c.SwaggerGeneratorOptions.OperationFilters.Add(new AddXExternalIdsHeaderParameter());
                c.SwaggerGeneratorOptions.OperationFilters.Add(new AddXInternalIdsHeaderParameter());
            });
            services.AddRequestHeaderMappings(builder =>
            {
                builder
                    .AddMapping(AddXExternalIdsHeaderParameter.HeaderName, enumerable =>
                    {
                        var s = enumerable?.LastOrDefault();
                        return s is null ? new ExternalCorrelation() : new ExternalCorrelation(s);
                    })
                    
                    .AddMapping(AddXInternalIdsHeaderParameter.HeaderName,
                        values => 
                            values is null ? new InternalCorrelation(): new InternalCorrelation(values.ToArray()
                            ))
                    
                    .AddMapping<IAllCorrelation>(
                        new[] {AddXExternalIdsHeaderParameter.HeaderName, AddXInternalIdsHeaderParameter.HeaderName},
                        b =>
                        {
                            var allValues = b.Values.SelectMany(s => s).ToArray();
                            return allValues.Any() ? new AllCorrelation(allValues) : new AllCorrelation();
                        });
            });
            services.AddScoped<HeadersReceived>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ExampleApi v1"));
            }

            app.UseRouting();

            app.UseMiddleware<ExampleMiddleware>();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
