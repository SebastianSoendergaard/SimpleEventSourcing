using Cart.Cli.Client;

Console.WriteLine("Enter program to execute, valid options:");
Console.WriteLine("  1: client");
Console.WriteLine("  2: monitor");
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

    default:
        Console.WriteLine("Invalid input!");
        break;
}

