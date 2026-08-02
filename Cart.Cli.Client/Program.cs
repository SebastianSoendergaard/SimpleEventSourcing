using Cart.Cli.Client;

Console.WriteLine("Enter program to execute, valid options:");
Console.WriteLine("  1: client");
Console.WriteLine("  2: monitor");
Console.WriteLine("  3: instrumentation to console");
Console.WriteLine("  4: instrumentation to file");
var input = Console.ReadKey();
Console.WriteLine("");
Console.WriteLine("");

switch (input.KeyChar)
{
    case '1':
        CartClient.Run().GetAwaiter().GetResult();
        break;

    case '2':
        CartMonitor.Run().GetAwaiter().GetResult();
        break;

    case '3':
        InstrumentationConsoleMonitor.Run().GetAwaiter().GetResult();
        break;

    case '4':
        InstrumentationFileMonitor.Run().GetAwaiter().GetResult();
        break;

    default:
        Console.WriteLine("Invalid input!");
        break;
}

