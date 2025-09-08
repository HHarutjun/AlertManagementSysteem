using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace VanDijk.AlertManagement.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly AlertManager _alertManager;

        public AlertsController(
            ILogProcessor logProcessor,
            IAlertCreator alertCreator,
            IAlertSender alertSender,
            ISentAlertsStorage sentAlertsStorage,
            AlertStrategyManager strategyManager,
            IRecipientResolver recipientResolver,
            ITaskCreator taskCreator)
        {
            _alertManager = new AlertManager(
                logProcessor,
                alertCreator,
                alertSender,
                sentAlertsStorage,
                strategyManager,
                recipientResolver,
                taskCreator
            );
        }

        [HttpGet("runApplication")]
        public async Task<IActionResult> RunApplication()
        {
            await _alertManager.ProcessAlertsAsync();
            var html = "<html><body><h2>Alert pipeline uitgevoerd</h2><p>De alerts zijn verwerkt en verstuurd.</p></body></html>";
            return Content(html, "text/html");
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessAlerts()
        {
            await _alertManager.ProcessAlertsAsync();
            return Ok("Alerts processed.");
        }
    }
}
