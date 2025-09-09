using System;
using System.IO;
using System.Linq;

/// <summary>
/// Provides functionality to render alert templates into HTML bodies for email or display purposes.
/// </summary>
public static class AlertTemplateRenderer
{
    /// <summary>
    /// Renders the HTML body for an alert message using the specified component.
    /// </summary>
    /// <param name="message">The alert message to render.</param>
    /// <param name="component">The component name to use if not found in the message.</param>
    /// <returns>The rendered HTML body as a string.</returns>
    public static string RenderHtmlBody(string message, string component)
    {
        var componentBlocks = message.Split(new[] { "-----------------------------" }, StringSplitOptions.RemoveEmptyEntries);
        string template = File.ReadAllText(GetTemplatePath());

        string htmlBody = string.Empty;
        foreach (var block in componentBlocks)
        {
            var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var firstLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;

            string severity = ExtractField(firstLine, "Severity:");
            string timestamp = ExtractField(firstLine, "Timestamp:");
            string comp = ExtractField(firstLine, "Component:") ?? ExtractField(firstLine, "Endpoint:") ?? component;
            string taakReferentie = string.Empty;

            foreach (var line in lines)
            {
                if (line.StartsWith("Taak referentie:", StringComparison.OrdinalIgnoreCase))
                {
                    taakReferentie = line.Substring("Taak referentie:".Length).Trim();
                    break;
                }
            }

            string niveauLabel = severity switch
            {
                "Fatal" or "3" => "Fataal",
                "Warning" or "2" => "Waarschuwing",
                "Info" or "1" => "Info",
                _ => severity
            };
            string buttonColor = severity switch
            {
                "Fatal" or "3" => "#E30613",
                "Warning" or "2" => "#ff9800",
                "Info" or "1" => "#2196f3",
                _ => "#888"
            };

            var logLines = lines
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("Taak referentie:", StringComparison.OrdinalIgnoreCase))
                .ToList();
            string beschrijving = string.Join("<br/>", logLines.Select(l => System.Net.WebUtility.HtmlEncode(l)));

            htmlBody += template
                .Replace("{{Severity}}", severity)
                .Replace("{{Timestamp}}", timestamp)
                .Replace("{{Component}}", comp)
                .Replace("{{Beschrijving}}", beschrijving)
                .Replace("{{TaakReferentie}}", taakReferentie)
                .Replace("{{NiveauLabel}}", niveauLabel)
                .Replace("{{ButtonColor}}", buttonColor)
                + "<br/><br/>";
        }

        return htmlBody;
    }

    private static string GetTemplatePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Presentation", "Templates")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new FileNotFoundException("Kan Presentation\\Templates\\AlertTemplate.html niet vinden vanaf base directory: " + baseDir);
        }

        var templatePath = Path.Combine(dir.FullName, "Presentation", "Templates", "AlertTemplate.html");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("AlertTemplate.html niet gevonden op: " + templatePath);
        }

        return templatePath;
    }

    private static string ExtractField(string message, string field)
    {
        var idx = message.IndexOf(field, StringComparison.OrdinalIgnoreCase);
        if (idx == -1)
        {
            return string.Empty;
        }

        idx += field.Length;
        var end = message.IndexOfAny(new[] { '|', '\n' }, idx);
        if (end == -1)
        {
            end = message.Length;
        }

        return message.Substring(idx, end - idx).Trim();
    }
}
