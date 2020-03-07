namespace Abacus.FrontEnd.FleetPresenceApi.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// API surface for health checks by LB probes and monitoring.
    /// </summary>
    [Route("api/[controller]")]
    public class IsHealthyController : Controller
    {
        /// <summary>
        /// Performs the health check and either returns "True" as a 200 or an error message as a 500-class error.
        /// </summary>
        /// <returns>"True" with HTTP status 200 if the service is healthy, or an error message with
        /// a 500-class HTTP status if the service is unhealthy.</returns>
        [HttpGet]
        public async Task<ActionResult<string>> Get() => throw new NotImplementedException();
    }
}
