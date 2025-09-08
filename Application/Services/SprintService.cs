using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public interface ISprintService
{
    Task<string> GetCurrentSprintAsync(string teamName);
}

public class SprintService : ISprintService
{
    private readonly string _organizationUrl;
    private readonly string _projectName;
    private readonly string _personalAccessToken;

    public SprintService(string organizationUrl, string projectName, string personalAccessToken)
    {
        _organizationUrl = organizationUrl ?? throw new ArgumentNullException(nameof(organizationUrl));
        _projectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        _personalAccessToken = personalAccessToken ?? throw new ArgumentNullException(nameof(personalAccessToken));
    }

    public async Task<string> GetCurrentSprintAsync(string teamName)
    {
        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_personalAccessToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var url = $"{_organizationUrl}/{_projectName}/{teamName}/_apis/work/teamsettings/iterations?$timeframe=current&api-version=7.0";
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
