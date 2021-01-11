
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SteveTalksCode.StrongTypedHeaders;

namespace ExampleApi
{
    public class ExampleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IHttpRequestHeadersAccessor _headers;

        public ExampleMiddleware(RequestDelegate next, ILogger<ExampleMiddleware> logger,
            IHttpRequestHeadersAccessor headers)
        {
            _headers = headers;
            _next = next;
            _logger = logger;
        }

        // ReSharper disable once UnusedMember.Global - used implicitly
        public async Task InvokeAsync(HttpContext context)
        {
            var extHeader = _headers.GetHeader<ExternalCorrelation>();

            if (extHeader is not null && extHeader.HasValue)
            {
                _logger.LogInformation($"The external correlation id received was {extHeader.Value}");
            }

            var intHeader = _headers.GetHeader<InternalCorrelation>();

            if (intHeader is not null && intHeader.HasValue)
            {
                foreach (var intValue in intHeader.Value)
                {
                    _logger.LogInformation($"The internal correlation id received was {intValue}");
                }
            }
            await _next(context);
        }
    }
}
