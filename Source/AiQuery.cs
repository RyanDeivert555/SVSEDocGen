using Google.GenAI;
using Google.GenAI.Types;

public static class AiQuery {
    public static readonly GenerateContentConfig Config = new GenerateContentConfig { ResponseMimeType = "application/json", };
    public static readonly string Model = "gemini-3.5-flash";
    private static Client _client = null!;

    private static string? GetApiKey() => System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");

    public static void MakeClient() {
        var timeoutOptions = new HttpOptions{
            Timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds,
        };

        if (GetApiKey() is string key) {
            _client = new Client(
                apiKey: key,
                httpOptions: timeoutOptions
            );
        } 
        else {
            Console.Write("API key not defined in environment, enter Gemini API key: ");
            string k = Console.ReadLine()!.Trim();

            _client = new Client(
                apiKey: k,
                httpOptions: timeoutOptions
            );
        }
    }

    public static Task<Google.GenAI.Types.File> UploadFile(string filepath) {
        return _client.Files.UploadAsync(filepath);
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

        var response = await _client.Models.GenerateContentAsync(Model, content, Config);

        return response.Text!;
    }
}
