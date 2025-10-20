using System.Text.Json;

namespace Cart.Cli.Client;

internal static class InstrumentationMonitor
{
    public static async Task Run()
    {
        Console.Write("Type log file path: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Invalid file path!");
            return;
        }

        var dir = Path.GetDirectoryName(input);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        Console.WriteLine($"Logging all instrumentation actions to: {input}");
        Console.WriteLine("");
        Console.WriteLine("");

        var headline = "StartTime;ProcessingTime (ms);Type;ItemCount";
        File.WriteAllLines(input, [headline]);

        while (!Console.KeyAvailable)
        {
            await LogInstrumentationActions(input ?? "");
            await Task.Delay(1000);
        }
    }

    static async Task LogInstrumentationActions(string filePath)
    {
        Console.Write($"Reading actions... ");
        using var client = new HttpClient();
        var url = $"https://localhost:7165/api/support/get-instumentation-actions/v1";
        var result = await client.GetAsync(url);
        var content = await result.Content.ReadAsStringAsync();
        if (!result.IsSuccessStatusCode)
        {
            Console.WriteLine($"{result.StatusCode} - {content}");
            return;
        }
        var actions = JsonSerializer.Deserialize<List<Action>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var lines = actions?.Select(x => x.ToLogLine()) ?? [];
        File.AppendAllLines(filePath, lines);
        Console.WriteLine($"OK - logged {lines.Count()} actions.");
    }

    private record Action(DateTimeOffset StartTime, string Type, JsonElement? StartDetails, JsonElement? CompleteDetails, TimeSpan ProcessingTime)
    {
        public string ToLogLine()
        {
            return $"{StartTime:O};{ProcessingTime.TotalMilliseconds};{Type};{GetItemCount()}";
        }

        private int GetItemCount()
        {
            var itemCount = 0;

            switch (Type)
            {
                case "append-events":
                    if (CompleteDetails != null)
                    {
                        itemCount = CompleteDetails.Value.GetProperty("eventCount").GetInt32();
                    }
                    break;

                case "load-events":
                    if (CompleteDetails != null)
                    {
                        itemCount = CompleteDetails.Value.GetProperty("eventCount").GetInt32();
                    }
                    break;

                case "notify-subscribers":
                    break;

                case "notify-subscribers-load-events":
                    if (CompleteDetails != null)
                    {
                        itemCount = CompleteDetails.Value.GetProperty("eventCount").GetInt32();
                    }
                    break;

                case "notify-subscribers-apply-events":
                    if (CompleteDetails != null)
                    {
                        itemCount = CompleteDetails.Value.GetProperty("eventCount").GetInt32();
                    }
                    break;

                default:
                    break;
            }

            return itemCount;
        }
    }
}
