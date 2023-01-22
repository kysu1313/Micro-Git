using Spectre.Console;

namespace MicroGit.HelperFunctions;

public class Utils<T>
{
    private static string _errorIssuePrompt = $"[red]If you think this is a bug, please create an issue on GitHub:[/]";
    private static string _errorIssueLink = $"[blue]https://github.com/kysu1313/Micro-Git/issues[/]";
    
    public static string GetInput(string prompt)
    {
        AnsiConsole.WriteLine(prompt);
        AnsiConsole.Write(">> ");
        return Console.ReadLine() ?? String.Empty;
    }

    public static void WriteErrorIssue(Exception? e = null)
    {
        if (e != null)
        {
            AnsiConsole.MarkupLine($"[red]Awww you broke me :([/]");
            AnsiConsole.MarkupLine($"[red]Error: {e.Message}[/]");
        }
        AnsiConsole.MarkupLine(_errorIssuePrompt);
        AnsiConsole.MarkupLine(_errorIssueLink);
    }
    
    
    public static List<string> RepoSelectPrompt(string instructions, ConfigManager _configManager)
    {
        AnsiConsole.Markup($"[green]{instructions}[/]\n");
        var table = new Table();
        table.AddColumn("Repo #");
        table.AddColumn("Repo Name");

        var repoPrompts = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select your [green]repos[/]")
                .NotRequired()
                .PageSize(10)
                .Mode(SelectionMode.Leaf)
                .MoreChoicesText("[grey](Move up and down to reveal more repos)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a repo, " +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoiceGroup("Repos", _configManager.state.Repos));

        // Write the selected repos to the terminal
        foreach (string repo in repoPrompts)
        {
            AnsiConsole.WriteLine(repo);
        }

        return repoPrompts;
    }
    
    public static T CustomPrompt<T>(string instructions, string title, List<T> options)
    {
        AnsiConsole.Markup($"\n[green]{instructions}[/]\n");

        var prompt = AnsiConsole.Prompt(
            new SelectionPrompt<T>()
                .Title(title)
                .PageSize(10)
                .Mode(SelectionMode.Leaf)
                .AddChoices(options));

        return prompt;
    }
    
    public static string CustomPrompt(string instructions, string title, List<string> options)
    {
        AnsiConsole.Markup($"\n[green]{instructions}[/]\n");

        var prompt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(10)
                .Mode(SelectionMode.Leaf)
                .AddChoices(options));

        return prompt;
    }
    
    public static string YesNoSelectPrompt(string instructions)
    {
        AnsiConsole.Markup($"\n[green]{instructions}[/]\n");
        var table = new Table();
        table.AddColumn("Repo #");
        table.AddColumn("Repo Name");

        var repoPrompt = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select [green]Yes[/] or [red]No[/]?")
                .PageSize(10)
                .Mode(SelectionMode.Leaf)
                .AddChoices(new[]{"Yes", "No"}));

        return repoPrompt;
    }

    public static string GetPassword()
    {
        var pass = string.Empty;
        ConsoleKey key;
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && pass.Length > 0)
            {
                Console.Write("\b \b");
                pass = pass[0..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("");
                pass += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);
        
        return pass;
    }
    
}