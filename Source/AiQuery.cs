using Google.GenAI;
using Google.GenAI.Types;

public static class AiQuery {
    private static readonly string ApiKey = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY")
        ?? throw new InvalidOperationException(
            "Environment variable GEMINI_API_KEY is missing. Make sure to configure with export GEMINI_API_KEY=<your key>"
        );
    public static readonly GenerateContentConfig Config = new GenerateContentConfig { ResponseMimeType = "application/json", };
    public static readonly string Model = "gemini-3.5-flash";
    private static readonly Client Client = new Client(
        apiKey: ApiKey,
        httpOptions: new HttpOptions {
            Timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds,
        }
    );

    public static Task<Google.GenAI.Types.File> UploadFile(string filepath) {
        return Client.Files.UploadAsync(filepath);
    }

    public static async Task<string> Query(IEnumerable<string> prompts, IEnumerable<Google.GenAI.Types.File> files) {
        var promptParts = prompts.Select(p => new Part { Text = p });
        var fileParts = files.Select(f => new Part {
            FileData = new FileData {
                FileUri = f.Uri,
                MimeType = f.MimeType,
            }
        });

        var content = new List<Content> {
            new Content {
                Parts =  promptParts.Concat(fileParts).ToList(),
            }
        };

        var response = await Client.Models.GenerateContentAsync(Model, content, Config);

        return response.Text!;
    }
}
