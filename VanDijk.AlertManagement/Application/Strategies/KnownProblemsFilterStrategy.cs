using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Strategie: groepeer op component, binnen component op unieke problemId.
/// Alleen nieuwe problemId's (nog niet bekend in de taak-description) triggeren een alert.
/// </summary>
public class KnownProblemsFilterStrategy : IAlertGroupingStrategy
{
    private readonly Func<string, HashSet<string>> getKnownProblemsForComponent;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnownProblemsFilterStrategy"/> class.
    /// </summary>
    /// <param name="getKnownProblemsForComponent">
    /// Functie die voor een component de bekende problemId's teruggeeft (bijv. uit de taak-description).
    /// </param>
    public KnownProblemsFilterStrategy(Func<string, HashSet<string>> getKnownProblemsForComponent)
    {
        this.getKnownProblemsForComponent = getKnownProblemsForComponent ?? (_ => new HashSet<string>());
    }

    /// <summary>
    /// Groepeer logs per component, filter alleen nieuwe problemId's (nog niet bekend).
    /// </summary>
    /// <param name="logs">De lijst met logregels om te groeperen en te filteren.</param>
    /// <returns>
    /// Een dictionary waarbij de sleutel de componentnaam is en de waarde een lijst van logregels met nieuwe problemId's voor die component.
    /// </returns>
    public IDictionary<string, IList<string>> GroupLogs(IList<string> logs)
    {
        var grouped = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

        // 1. Groepeer per component
        var logsByComponent = logs.GroupBy(log => log.ExtractComponent());

        foreach (var compGroup in logsByComponent)
        {
            var component = compGroup.Key;
            var knownProblems = this.getKnownProblemsForComponent(component);

            // 2. Verzamel unieke problemId's in de logs voor deze component
            var logsByProblemId = compGroup
                .GroupBy(log => log.ExtractProblemId())
                .ToList();

            // 3. Alleen nieuwe problemId's (nog niet bekend) toevoegen
            var newLogs = new List<string>();
            foreach (var problemGroup in logsByProblemId)
            {
                var problemId = problemGroup.Key;
                if (!knownProblems.Contains(problemId))
                {
                    // Voeg alle logs met deze nieuwe problemId toe
                    newLogs.AddRange(problemGroup);
                }
            }

            if (newLogs.Count > 0)
            {
                grouped[component] = newLogs;
            }
        }

        return grouped;
    }
}
