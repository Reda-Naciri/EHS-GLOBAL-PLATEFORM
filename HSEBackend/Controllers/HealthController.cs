using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HSEBackend.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Health status of the API</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetHealth()
        {
            try
            {
                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    service = "HSE Backend API"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = "Health check failed"
                });
            }
        }

        /// <summary>
        /// Simple ping endpoint
        /// </summary>
        /// <returns>Ping response</returns>
        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            return Ok(new
            {
                message = "pong",
                timestamp = DateTime.UtcNow
            });
        }
    }
}