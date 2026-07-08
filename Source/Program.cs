// TODO LIST
// Make it edit the report date
// Add comment about structural calculations
// Needs to be more free to add info, not just do text replacement

// GI reports
// Select template based on type of soil
// Descision tree
// Soil expansion: low, liquifaction, high, determined by PI value

internal class Program
{
    static IEnumerable<string> GetUploadFiles()
    {
        return System.IO.Directory.GetFiles(".")
            .Where(fn => fn.EndsWith(".pdf"));
    }

    static async Task Main()
    {
        Console.WriteLine("Current files in directory: ");
        foreach (var file in System.IO.Directory.GetFiles(".")) 
        {
            Console.WriteLine(file);
        }
        Console.WriteLine();

        Console.Write("Enter plan type (FPR, GPR, or PPR): ");
        string letterType = Console.ReadLine()!.Trim();
        while (letterType != "FPR" && letterType != "PPR" && letterType != "GPR")
        {
            Console.Write("Illegal plan type, please type FPR, GPR, or PPR: ");
            letterType = Console.ReadLine()!.Trim();
        }

        string templateDocDocx = "template.docx";
        string letterTitle = letterType switch
        {
            "FPR" => "FOUNDATION PLAN REVIEW",
            "PPR" => "PROJECT PLAN REVIEW",
            "GPR" => "GRADING & DRAINING PLAN REVIEW",
            _ => throw new ArgumentException($"Report type must be either FPR, PPR, or GPR, illegal chosen option is {letterType}"),
        };
        string outputDocPath = "final.docx";

        Console.Write("Type any other additional instructions you would like to give the AI (Press enter to submit or skip): ");
        // TODO: should this be nullable and drillable?
        string additionInstructions = Console.ReadLine()!.Trim();

        System.IO.File.Copy(templateDocDocx, outputDocPath, true);

        Console.WriteLine("Uploading pdfs to Gemini");
        var tasks = GetUploadFiles().Select(fs => AiQuery.UploadFile(fs));
        var responses = await Task.WhenAll(tasks);

        var instructions = DocGen.FoundationPlan.ParseIntoInstructions(templateDocDocx, letterType);
        var prompt = $@"You are an expert Geotechnical Engineering Assistant. 
                    Your job is to accurately extract project metadata from Geotechnical Investigation (GI) reports to draft legal Foundation Plan Review letters. 
                    Precision is critical.

                    Analyze the two attached PDF reports, which is a geotechnical investigation and a site plan. 
                    I will provide you a JSON array of keys representing data fields I need. 
                    For each key in the array, find the corresponding factual answer in the PDFs.
                    Each piece of data that needs to be replaced will be format like {{Data}}
                    Return a flat JSON object where the keys match my list exactly, and the values are your extractions.
                    Make sure that not only the data is correct, but small things like capitalization and grammar are correct.
                    If you fail to find context for the field to be replaced, it is best to not do any replacement.

                    If you find additional information to add that might be important, such as other structural plan, do your best effort to add that information.

                    The title of the letter is {letterTitle}.
                    The type of the letter is a {letterType}.
                    The current date is {DateTime.Now}.
                    Additional instructions given by user: {additionInstructions}.

                    FIELDS TO EXTRACT:
                    {instructions}";

        Console.WriteLine("Uploading instructions to Gemini");
        var response = await AiQuery.Query([prompt], responses);

        DocGen.FoundationPlan.EditDoc(response, outputDocPath);

        Console.WriteLine($"Doc generated at {outputDocPath}");

        Console.WriteLine("\nDone. Press any key to exit");
        _ = Console.ReadKey();
    }
}
