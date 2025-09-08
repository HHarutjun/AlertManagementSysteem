using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VanDijk.AlertManagement.Core.Interfaces;

/// <summary>
/// Provides services for retrieving sprint information from Azure DevOps.
/// </summary>
public class SprintService : ISprintService
{
    private readonly string organizationUrl;
    private readonly string projectName;
    private readonly string personalAccessToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="SprintService"/> class.
    /// </summary>
    /// <param name="organizationUrl">The Azure DevOps organization URL.</param>
    /// <param name="projectName">The name of the Azure DevOps project.</param>
    /// <param name="personalAccessToken">The personal access token for authentication.</param>
    /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
    public SprintService(string organizationUrl, string projectName, string personalAccessToken)
    {
        this.organizationUrl = organizationUrl ?? throw new ArgumentNullException(nameof(organizationUrl));
        this.projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        this.personalAccessToken = personalAccessToken ?? throw new ArgumentNullException(nameof(personalAccessToken));
    }

    /// <summary>
    /// Retrieves the current sprint path for the specified team from Azure DevOps.
    /// </summary>
    /// <param name="teamName">The name of the team to get the current sprint for.</param>
    /// <returns>The path of the current sprint as a string.</returns>
    public async Task<string> GetCurrentSprintAsync(string teamName)
    {
        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{this.personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var url = $"{this.organizationUrl}/{this.projectName}/{teamName}/_apis/work/teamsettings/iterations?$timeframe=current&api-version=7.0";
        var response = await httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("[Debug] Azure DevOps iterations response: " + responseContent); // Voeg deze regel toe
            var iterationList = JsonSerializer.Deserialize<IterationListResponse>(responseContent);

            if (iterationList?.Value != null && iterationList.Value.Count > 0)
            {
                var currentIteration = iterationList.Value[0];
                if (!string.IsNullOrEmpty(currentIteration.Path))
                {
                    return currentIteration.Path;
                }

                throw new Exception("Iteration path is null.");
            }

            throw new Exception("No current sprint found for the team.");
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch current sprint: {response.StatusCode} - {error}");
        }
    }
}
