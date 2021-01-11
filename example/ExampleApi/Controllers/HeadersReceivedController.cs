using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ExampleApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HeadersReceivedController : ControllerBase
    {
        
        [HttpGet]
        public string[] Get([FromServices] HeadersReceived headers) => GetStringsFromHeaders(headers).ToArray();
        

        private static IEnumerable<string> GetStringsFromHeaders(HeadersReceived headers)
        {
            if (headers.External.HasValue)
            {
                yield return $"The external id received from the header is {headers.External.Value}";
            }

            if (headers.Internal.HasValue)
            {
                foreach (var intValue in headers.Internal.Value)
                {
                    yield return $"The internal id values received from the header include {intValue}";
                }
            }

            if (!headers.All.HasValue) yield break;
            
            foreach (var intValue in headers.All.Value)
            {
                yield return $"The composite 'AllHeaders' include {intValue}";
            }
            
        }
    }
}
