using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocGen;

public static class FoundationPlan {
    public enum LetterType {
        FPR,
        PPR,
        GPR,
    }

    public static string ParseIntoInstructions(string filename) {
        // Key = Raw Placeholder, Value = Gemini Prompt
        var instructionsMap = new Dictionary<string, string>();

        using (var wordDoc = WordprocessingDocument.Open(filename, false)) {
            var body = wordDoc.MainDocumentPart!.Document!.Body!;

            foreach (var paragraph in body.Descendants<Paragraph>()) {
                var currentGroup = new List<Run>();
                var highlightedGroups = new List<string>();

                foreach (var run in paragraph.Descendants<Run>()) {
                    if (run.RunProperties?.Highlight is not null) {
                        currentGroup.Add(run);
                    }
                    else if (currentGroup.Any()) {
                        highlightedGroups.Add(string.Join("", currentGroup.Select(r => r.InnerText)).Trim());
                        currentGroup.Clear();
                    }
                }
                if (currentGroup.Any()) {
                    highlightedGroups.Add(string.Join("", currentGroup.Select(r => r.InnerText)).Trim());
                }

                foreach (var templateInstruction in highlightedGroups) {
                    if (string.IsNullOrWhiteSpace(templateInstruction)) continue;
                    if (instructionsMap.ContainsKey(templateInstruction)) continue;

                    string surroundingContext = paragraph.InnerText;
                    string prompt = $"In the sentence: \"{surroundingContext}\", replace the placeholder \"{templateInstruction}\" using data from the PDF.";

                    instructionsMap.Add(templateInstruction, prompt);
                }
            }

            return System.Text.Json.JsonSerializer.Serialize(
                instructionsMap,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
        }
    }

    private static void EditRun(List<Run> highlightedRuns, Dictionary<string, string> instructionMap) {
        var instructionKey = string.Join("", highlightedRuns.Select(r => r.InnerText)).Trim();

        if (instructionMap.TryGetValue(instructionKey, out var replacementText)) {
            var firstRun = highlightedRuns.First();
            firstRun.RemoveAllChildren<Text>();
            firstRun.AppendChild(new Text(replacementText));

            foreach (var run in highlightedRuns.Skip(1)) {
                run.RemoveAllChildren<Text>();
            }
        }
    }

    public static void EditDoc(string response, string filepath) {
        var resultsMap = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(response)!;

        using (var wordDoc = WordprocessingDocument.Open(filepath, true)) {
            var body = wordDoc.MainDocumentPart!.Document!.Body!;
            foreach (var paragraph in body.Descendants<Paragraph>()) {
                var currentGroup = new List<Run>();
                var runs = paragraph.Descendants<Run>();

                foreach (var run in runs) {
                    if (run.RunProperties?.Highlight is not null) {
                        currentGroup.Add(run);
                    }
                    else if (currentGroup.Any()) {
                        EditRun(currentGroup, resultsMap);
                        currentGroup.Clear();
                    }
                }

                if (currentGroup.Any()) {
                    EditRun(currentGroup, resultsMap);
                }
            }

            wordDoc.MainDocumentPart.Document.Save();
        }
    }
}
