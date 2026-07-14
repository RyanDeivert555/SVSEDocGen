// TODO LIST
// Make it edit the report date
// Add comment about structural calculations
// Needs to be more free to add info, not just do text replacement

// GI reports
// Select template based on type of soil
// Descision tree
// Soil expansion: low, liquifaction, high, determined by PI value

internal class Program {
    internal static async Task Main() {
        Console.WriteLine("Current files in directory: ");
        string[] fsFiles = System.IO.Directory.GetFiles(".");
        foreach (var file in fsFiles) {
            Console.WriteLine(file);
        }
        Console.WriteLine();

        Console.Write("Enter plan type (FPR, GPR, or PPR): ");
        string letterType = Console.ReadLine()!.Trim();
        while (letterType != "FPR" && letterType != "PPR" && letterType != "GPR") {
            Console.Write("Illegal plan type, please type FPR, GPR, or PPR: ");
            letterType = Console.ReadLine()!.Trim();
        }
        var letterEnum = Enum.Parse<DocGen.FoundationPlan.LetterType>(letterType);

        string templateDocDocx = "template.docx";
        string letterTitle = letterEnum switch {
            DocGen.FoundationPlan.LetterType.FPR => "FOUNDATION PLAN REVIEW",
            DocGen.FoundationPlan.LetterType.PPR => "PROJECT PLAN REVIEW",
            DocGen.FoundationPlan.LetterType.GPR => "GRADING & DRAINING PLAN REVIEW",
            _ => throw new ArgumentException($"Report type must be either FPR, PPR, or GPR, illegal chosen option is {letterType}"),
        };
        var outputDocPath = "final.docx";

        Console.Write("Type any other additional instructions you would like to give the AI (Press enter to submit or skip): ");
        // TODO: should this be nullable and drillable?
        string? additionInstructions = Console.ReadLine()?.Trim();

        System.IO.File.Copy(templateDocDocx, outputDocPath, true);

        Console.WriteLine("Uploading pdfs to Gemini");
        var tasks = fsFiles
            .Where(s => s.EndsWith(".pdf"))
            .Select(fs => AiQuery.UploadFile(fs));
        var responses = await Task.WhenAll(tasks);

        var instructions = DocGen.FoundationPlan.ParseIntoInstructions(templateDocDocx);
        var prompt = $@"You are an expert Geotechnical Engineering Assistant. 
                    Your job is to accurately extract project metadata from Geotechnical Investigation (GI) reports to draft legal Foundation Plan Review letters. 
                    Precision is critical, as this is a legal document.

                    TASK:
                    For each key in the FIELDS TO EXTRACT list, find the factual value in the PDFs. Return a flat JSON object whose keys match the list exactly. Return ONLY the JSON object, no other text.

                    EXTRACTION RULES:
                    - Extract only what is explicitly stated in the documents. Do not infer, add, or embellish.
                    - If a value cannot be found, return an empty string for that key. Never guess.
                    - Dates must be formatted as 'Month D, YYYY' (e.g., 'January 8, 2025').
                    - Job numbers, sheet numbers, and file numbers must be copied character-for-character.

                    TERMINOLOGY RULES (override whatever wording the source documents use):
                    - Always refer to the reviewed plans as the 'Foundation Plan', never 'Structural Plans' or 'Structural Drawings', even if the drawings are titled that way.
                    - The GI report is referred to as the 'Geotechnical Investigation report'.
                    - Do not use the public project number, use the company's job number

                    CAPITALIZATION RULES:
                    - Document titles are capitalized: 'Foundation Plan', 'Geotechnical Investigation report', 'Foundation Plan Review'.
                    - Ordinary nouns in running text are lowercase: 'the subject site', 'the proposed residence' is capitalized only when used as the project title ('Proposed Residence').
                    - Names of firms and people must match the source exactly.
                    - Never output all-caps values unless the source field is an acronym or the template requires it.

                    CONTEXT:
                    - Letter title: {letterTitle}
                    - Letter type: the acronym of the letter title
                    - Current date: {DateTime.Now:MMMM d, yyyy}
                    - Additional user instructions: {additionInstructions}

                    FIELDS TO EXTRACT:
                    {instructions}";

        Console.WriteLine("Uploading instructions to Gemini");
        var response = await AiQuery.Query([prompt], responses);

        DocGen.FoundationPlan.EditDoc(response, outputDocPath);

        Console.WriteLine($"Doc generated at {outputDocPath}");

        Console.WriteLine("\nDone. Please make sure to review the completed document. Press any key to exit");
        _ = Console.ReadKey();
    }
}
