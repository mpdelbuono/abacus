namespace Abacus.FrontEnd.FleetPresenceApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Defines the public API surface for the FleetPresenceApi service.
    /// </summary>
    [Route("api/[controller]")]
    public class FleetMemberTimeController : Controller
    {
        // GET api/<controller>/5
        [HttpGet("{id}")]
        public string Get(int playerId)
        {
            throw new System.NotImplementedException();
        }
    }
}
