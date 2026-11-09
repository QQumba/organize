using System.Text;
using Organizer.Cli;

Console.OutputEncoding = Encoding.UTF8;
return await CliApp.RunAsync(args, Console.Out, Console.Error);
