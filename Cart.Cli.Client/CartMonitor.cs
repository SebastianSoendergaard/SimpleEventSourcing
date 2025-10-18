using System.Net.Http.Json;

namespace Cart.Cli.Client;

internal static class CartMonitor
{
    public static async Task Run()
    {
        while (!Console.KeyAvailable)
        {
            Console.Clear();
            Console.WriteLine("Cart Monitor - Press any key to quit");
            Console.WriteLine("");
            Console.WriteLine("");
            await ShowProjectorStates();
            Console.WriteLine("");
            await ShowReactorStates();
            Thread.Sleep(2000);
        }
    }

    static async Task ShowProjectorStates()
    {
        Console.WriteLine($" PROJECTOR STATES");
        Console.WriteLine($"-------------------------------------------------------------------------------------------------");
        using var client = new HttpClient();
        var url = $"https://localhost:7165/api/support/get-projector-states/v1";
        var result = await client.GetAsync(url);
        var projectors = await result.Content.ReadFromJsonAsync<List<Projector>>();
        foreach (var p in projectors ?? [])
        {
            var stateText = p.ProcessingState.ProcessingError == null
                ? "OK"
                : $"Attempts: {p.ProcessingState.ProcessingError.ProcessingAttempts,4} - {p.ProcessingState.ProcessingError.ErrorMessage}";
            var text = $"{p.Name,-50} Seq no: {p.ProjectorHeadSequenceNumber,4} / {p.EventStoreHeadSequenceNumber,4}, Confimed: {p.ProcessingState.ConfirmedSequenceNumber,4}, State: {stateText}";
            Console.WriteLine(text);
        }
    }

    static async Task ShowReactorStates()
    {
        Console.WriteLine($" REACTOR STATES");
        Console.WriteLine($"-------------------------------------------------------------------------------------------------");
        using var client = new HttpClient();
        var url = $"https://localhost:7165/api/support/get-reactor-states/v1";
        var result = await client.GetAsync(url);
        var reactors = await result.Content.ReadFromJsonAsync<List<Reactor>>();
        foreach (var r in reactors ?? [])
        {
            var stateText = r.ProcessingState.ProcessingError == null
                ? "OK"
                : $"Attempts: {r.ProcessingState.ProcessingError.ProcessingAttempts,4} - {r.ProcessingState.ProcessingError.ErrorMessage}";
            var text = $"{r.Name,-50} Seq no: {r.ReactorHeadSequenceNumber,4} / {r.EventStoreHeadSequenceNumber,4}, Confimed: {r.ProcessingState.ConfirmedSequenceNumber,4}, State: {stateText}";
            Console.WriteLine(text);
        }
    }

    private record Projector(string Name, int EventStoreHeadSequenceNumber, int ProjectorHeadSequenceNumber, State ProcessingState);
    private record Reactor(string Name, int EventStoreHeadSequenceNumber, int ReactorHeadSequenceNumber, State ProcessingState);

    public record State(
        DateTimeOffset LatestSuccessfulProcessingTime,
        long ConfirmedSequenceNumber,
        Error? ProcessingError = null
    );

    public record Error(
        string ErrorMessage,
        string Stacktrace,
        int ProcessingAttempts,
        DateTimeOffset LatestRetryTime
    );
}
