using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace TokenProvider.Functions
{
    public class GenerateToken
    {
        private readonly ILogger<GenerateToken> _logger;

        public GenerateToken(ILogger<GenerateToken> logger)
        {
            _logger = logger;
        }

        [Function("GenerateToken")]
        public async Task <IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route ="token/generate")] HttpRequest req, [FromBody] TokenRequest tokenRequest)
        {
            if (tokenRequest == null || tokenRequest.Email == null)
            {
                return new BadRequestObjectResult(new { Error = "please provided a valid userId and a valid user email address" });
            }
            return new OkObjectResult("");
        }
    }
}
