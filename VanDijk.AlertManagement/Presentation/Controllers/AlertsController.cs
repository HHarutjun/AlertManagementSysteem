namespace VanDijk.AlertManagement.Presentation.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Controller for testing and running the application from the webapp.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
        {
        private readonly AlertManager alertManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlertsController"/> class.
        /// </summary>
        /// <param name="logProcessor">The log processor.</param>
        /// <param name="alertCreator">The alert creator.</param>
        /// <param name="alertSender">The alert sender.</param>
        /// <param name="sentAlertsStorage">The sent alerts storage.</param>
        /// <param name="strategyManager">The alert strategy manager.</param>
        /// <param name="recipientResolver">The recipient resolver.</param>
        /// <param name="taskCreator">The task creator.</param>
        public AlertsController(
            ILogProcessor logProcessor,
            IAlertCreator alertCreator,
            IAlertSender alertSender,
            ISentAlertsStorage sentAlertsStorage,
            AlertStrategyManager strategyManager,
            IRecipientResolver recipientResolver,
            ITaskCreator taskCreator)
        {
            this.alertManager = new AlertManager(
                logProcessor,
                alertCreator,
                alertSender,
                sentAlertsStorage,
                strategyManager,
                recipientResolver,
                taskCreator);
        }

        /// <summary>
        /// Runs the alert processing pipeline and returns a confirmation HTML page.
        /// </summary>
        /// <returns>An HTML response indicating the alerts have been processed and sent.</returns>
        [HttpGet("runApplication")]
        public async Task<IActionResult> RunApplication()
        {
            await this.alertManager.ProcessAlertsAsync();
            var html = "<html><body><h2>Alert pipeline uitgevoerd</h2><p>De alerts zijn verwerkt en verstuurd.</p></body></html>";
            return this.Content(html, "text/html");
        }

        /// <summary>
        /// Processes alerts asynchronously and returns a confirmation response.
        /// </summary>
        /// <returns>An IActionResult indicating the result of the alert processing.</returns>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessAlerts()
        {
            await this.alertManager.ProcessAlertsAsync();
            return this.Ok("Alerts processed.");
        }
    }
}
