using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ExampleApi.Swagger
{
    public class AddXInternalIdsHeaderParameter : IOperationFilter
    {
        public const string HeaderName = "X-Internal-Ids";


        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            (operation.Parameters ??= new List<OpenApiParameter>())
            .Add(new OpenApiParameter
                {

                    Name = HeaderName,
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "array", Items = new OpenApiSchema{ Type = "string"}}
                });
        }
    }
}