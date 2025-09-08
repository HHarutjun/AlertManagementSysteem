// <copyright file="AlertManager.cs" company="The Learning Network">
// Copyright (c) The Learning Network. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Manages the processing of alerts, including grouping, notification, and task creation.
/// </summary>
public class AlertManager
{
    private readonly ISentAlertsStorage sentAlertsStorage;
    private readonly ISet<string> sentAlerts;
    private readonly AlertStrategyManager strategyManager;
    private readonly IRecipientResolver recipientResolver;
    private readonly ITaskCreator taskCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertManager"/> class.
    /// </summary>
    /// <param name="logProcessor">The log processor.</param>
    /// <param name="alertCreator">The alert creator.</param>
    /// <param name="alertSender">The alert sender.</param>
    /// <param name="sentAlertsStorage">The sent alerts storage.</param>
    /// <param name="strategyManager">The alert strategy manager.</param>
    /// <param name="recipientResolver">The recipient resolver.</param>
    /// <param name="taskCreator">The task creator.</param>
    public AlertManager(
        ILogProcessor logProcessor,
        IAlertCreator alertCreator,
        IAlertSender alertSender,
        ISentAlertsStorage sentAlertsStorage,
        AlertStrategyManager strategyManager,
        IRecipientResolver recipientResolver,
        ITaskCreator taskCreator)
    {
        this.LogProcessor = logProcessor;
        this.AlertCreator = alertCreator;
        this.AlertSender = alertSender;
        this.sentAlertsStorage = sentAlertsStorage;
        this.sentAlerts = this.sentAlertsStorage.LoadSentAlerts();
        this.strategyManager = strategyManager;
        this.recipientResolver = recipientResolver;
        this.taskCreator = taskCreator;
    }

    private ILogProcessor LogProcessor { get; }

    private IAlertCreator AlertCreator { get; }

    private IAlertSender AlertSender { get; }

    /// <summary>
    /// Processes alerts asynchronously based on logs.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessAlertsAsync()
    {
        // Refresh recipients from storage before processing
        if (this.recipientResolver is RecipientResolver resolver)
        {
            resolver.RefreshRecipients();
        }

        var logs = await this.LogProcessor.FetchLogsAsync();

        Console.WriteLine("[INFO] Opgehaalde logs:");
        foreach (var log in logs)
        {
            Console.WriteLine(log);
        }

        // Gather all unique recipients
        var allRecipients = new List<Recipient>();
        if (this.recipientResolver is IRecipientListProvider listProvider)
        {
            allRecipients = listProvider.GetAllRecipients().ToList();
        }

        var sentThisRun = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Voorbeeld: gebruik KnownProblemsFilterStrategy voor alle recipients
        foreach (var recipient in allRecipients)
        {
            // Alleen één strategie per recipient
            var strategyType = recipient.GroupingStrategy;
            IAlertGroupingStrategy strategy;
            if (strategyType == GroupingStrategyType.KnownProblemsFilter)
            {
                strategy = new KnownProblemsFilterStrategy(component =>
                {
                    // Haal bekende problemId's uit de bestaande taak-description
                    // (implementatie: lees description van de taak voor deze component)
                    // Voorbeeld: return ParseKnownProblemsFromDescription(...);
                    return new HashSet<string>(); // TODO: implementatie
                });
            }
            else
            {
                strategy = AlertGroupingStrategyFactory.CreateStrategy(strategyType);
            }

            var relevantLogs = logs
                .Where(log => recipient.ResponsibleComponents.Any(c => string.Equals(log.ExtractComponent(), c, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            Console.WriteLine($"[Debug] Recipient '{recipient.Name}' - GroupingStrategy: {strategyType} - relevantLogs.Count: {relevantLogs.Count}");

            if (relevantLogs.Count == 0)
            {
                Console.WriteLine($"[Debug] Geen relevante logs voor recipient '{recipient.Name}' met strategy '{strategyType}'.");
                continue;
            }

            var groupedLogs = strategy.GroupLogs(relevantLogs);

            foreach (var group in groupedLogs)
            {
                var groupKey = group.Key;
                var groupLogs = group.Value.Distinct().ToList();

                // Verzamel alle unieke componenten voor deze recipient (moet altijd zijn eigen ResponsibleComponents zijn)
                var components = groupLogs
                    .Select(log => log.ExtractComponent())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .ToList();

                // Alleen de eigen recipient gebruiken, niet alle recipients met deze component!
                var taskReferencePerComponent = new Dictionary<string, string>();
                var processedComponents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (strategyType == GroupingStrategyType.KnownProblemsFilter)
                {
                    foreach (var component in components)
                    {
                        var workItemTitle = $"{component} - KnownProblems";
                        var logsForComponent = groupLogs.Where(l => l.ExtractComponent() == component).ToList();
                        var newProblemIds = logsForComponent.Select(l => l.ExtractProblemId()).Distinct().ToList();

                        if (await this.taskCreator.TaskExistsAsync(recipient.Board, workItemTitle))
                        {
                            var description = await this.taskCreator.GetWorkItemDescriptionAsync(recipient.Board, workItemTitle) ?? string.Empty;
                            var knownProblemIds = new HashSet<string>();
                            foreach (var line in description.Split('\n'))
                            {
                                if (line.Contains("ProblemId:"))
                                {
                                    var pid = line.Split("ProblemId:").Last().Trim();
                                    if (!string.IsNullOrWhiteSpace(pid))
                                    {
                                        knownProblemIds.Add(pid);
                                    }
                                }
                            }

                            var toAdd = newProblemIds.Where(pid => !knownProblemIds.Contains(pid)).ToList();
                            if (toAdd.Any())
                            {
                                var updatedDescription = description + "\n" + string.Join("\n", toAdd.Select(pid => $"ProblemId: {pid}"));
                                await this.taskCreator.UpdateWorkItemDescriptionAsync(recipient.Board, workItemTitle, updatedDescription);
                                foreach (var email in recipient.Emails)
                                {
                                    await this.AlertSender.SendAlertAsync(
                                        string.Join("\n", logsForComponent),
                                        email,
                                        groupKey);
                                }
                            }
                        }
                        else
                        {
                            var description = string.Join("\n", newProblemIds.Select(pid => $"ProblemId: {pid}"));
                            await this.taskCreator.CreateWorkItemAsync(recipient.Board, workItemTitle, description, WorkItemType.Bug);
                            foreach (var email in recipient.Emails)
                            {
                                await this.AlertSender.SendAlertAsync(
                                    string.Join("\n", logsForComponent),
                                    email,
                                    groupKey);
                            }
                        }
                    }

                    continue;
                }

                foreach (var component in components)
                {
                    if (processedComponents.Contains(component))
                    {
                        continue;
                    }

                    processedComponents.Add(component);

                    var logsForComponent = groupLogs.Where(l => l.ExtractComponent() == component).ToList();
                    var firstLog = logsForComponent.FirstOrDefault();
                    var problemId = firstLog?.ExtractProblemId() ?? "UnknownProblem";
                    var exceptionType = firstLog?.ExtractExceptionType();

                    var workItemTitle = !string.IsNullOrEmpty(exceptionType)
                        ? $"{component} - {exceptionType}"
                        : $"{component} - {problemId}";

                    var taskReference = workItemTitle;
                    if (!await this.taskCreator.TaskExistsAsync(recipient.Board, workItemTitle))
                    {
                        try
                        {
                            Console.WriteLine($"[INFO] Taak aanmaken: '{workItemTitle}' op board '{recipient.Board}'");
                            var workItemId = await this.taskCreator.CreateWorkItemAsync(
                                recipient.Board,
                                workItemTitle,
                                string.Join("\n", logsForComponent),
                                WorkItemType.Bug);
                            taskReference = !string.IsNullOrWhiteSpace(workItemId)
                                ? $"#{workItemId} ({workItemTitle})"
                                : workItemTitle;
                            Console.WriteLine($"[INFO] Taak aangemaakt: '{taskReference}'");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Error] Failed to create work item for {component}: {ex.Message}");
                        }
                    }
                    else
                    {
                        if (this.taskCreator is TaskCreator tc)
                        {
                            try
                            {
                                var workItemId = await tc.GetWorkItemIdAsync(recipient.Board, workItemTitle);
                                if (!string.IsNullOrWhiteSpace(workItemId))
                                {
                                    taskReference = $"#{workItemId} ({workItemTitle})";
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Error] Failed to get work item ID for '{workItemTitle}': {ex.Message}");
                            }
                        }
                    }

                    taskReferencePerComponent[component] = taskReference;
                }

                // Alleen de eigen recipient!
                var recipientToBlocks = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var component in components)
                {
                    // Check of deze recipient verantwoordelijk is voor deze component
                    if (!recipient.ResponsibleComponents.Any(c => string.Equals(c, component, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    var logsForComponent = groupLogs.Where(l => l.ExtractComponent() == component).ToList();
                    if (!logsForComponent.Any())
                    {
                        continue;
                    }

                    var taakRef = taskReferencePerComponent.TryGetValue(component, out var t) ? t : string.Empty;
                    var block = string.Join("\n", logsForComponent) +
                                (!string.IsNullOrWhiteSpace(taakRef) ? $"\nTaak referentie: {taakRef}" : string.Empty);

                    foreach (var email in recipient.Emails.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (!recipientToBlocks.TryGetValue(email, out var blocks))
                        {
                            blocks = new HashSet<string>();
                            recipientToBlocks[email] = blocks;
                        }

                        if (!blocks.Any(b => b == block))
                        {
                            blocks.Add(block);
                        }
                    }
                }

                foreach (var kvp in recipientToBlocks)
                {
                    var recipientEmail = kvp.Key;
                    var blocks = kvp.Value.ToList();
                    if (blocks.Count == 0)
                    {
                        continue;
                    }

                    var uniqueGroupKey = $"{groupKey}|{recipientEmail}";
                    if (sentThisRun.Contains(uniqueGroupKey))
                    {
                        continue;
                    }

                    string messageWithBlocks = string.Join(
                        "\n-----------------------------\n",
                        blocks);
                    try
                    {
                        Console.WriteLine($"[INFO] Alert versturen naar {recipientEmail} voor groupKey: {uniqueGroupKey}");
                        await this.AlertSender.SendAlertAsync(messageWithBlocks, recipientEmail, groupKey);
                        Console.WriteLine($"[INFO] Alert verstuurd naar {recipientEmail}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] Failed to send alert to {recipientEmail} for groupKey {uniqueGroupKey}: {ex.Message}");
                    }

                    sentThisRun.Add(uniqueGroupKey);
                }
            }
        }
    }

    private Recipient? DetermineRecipient(Alert alert)
    {
        if (alert == null || string.IsNullOrEmpty(alert.Component))
        {
            return null;
        }

        return this.recipientResolver.ResolveRecipient(alert);
    }
}