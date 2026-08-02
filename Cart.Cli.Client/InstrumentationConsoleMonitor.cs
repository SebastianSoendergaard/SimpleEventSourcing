using System.Text.Json;

namespace Cart.Cli.Client;

internal static class InstrumentationConsoleMonitor
{
    public static async Task Run()
    {
        Console.WriteLine($"Logging all instrumentation actions");
        Console.WriteLine("");
        Console.WriteLine("");

        while (!Console.KeyAvailable)
        {
            await LogInstrumentationActions();
            await Task.Delay(1000);
        }
    }

    static async Task LogInstrumentationActions()
    {
        Console.WriteLine($"Reading actions... ");
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
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine($"OK - logged {lines.Count()} actions.");
    }

    private record Action(DateTimeOffset StartTime, string Type, JsonElement? StartDetails, JsonElement? CompleteDetails, TimeSpan ProcessingTime)
    {
        public string ToLogLine()
        {
            var startTime = $"StartTime: {StartTime:O}".PadRight(45);
            var processingTime = $"ProcessingTime(ms): { ProcessingTime.TotalMilliseconds}".PadRight(30);
            var type = $"Type: {Type}".PadRight(40);
            var itemCount = $"ItemCount: {GetItemCount()}".PadRight(15);

            return $"{startTime} {processingTime} {type} {itemCount}";
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
