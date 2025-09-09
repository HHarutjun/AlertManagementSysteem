namespace VanDijk.AlertManagement.Core.Interfaces
{
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods for sprint-related operations.
    /// </summary>
    public interface ISprintService
    {
        /// <summary>
        /// Gets the current sprint for the specified team asynchronously.
        /// </summary>
        /// <param name="teamName">The name of the team.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current sprint as a string.</returns>
        Task<string> GetCurrentSprintAsync(string teamName);
    }
}
