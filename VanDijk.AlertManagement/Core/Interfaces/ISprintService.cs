using System.Threading.Tasks;

namespace VanDijk.AlertManagement.Core.Interfaces
{
    public interface ISprintService
    {
        Task<string> GetCurrentSprintAsync(string teamName);
    }
}
