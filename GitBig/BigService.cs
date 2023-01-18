using LibGit2Sharp;
using Spectre.Console;
using Tree = Spectre.Console.Tree;

namespace GitBig;

public class BigService
{

    private Dictionary<string, string> _commands = new () {
        { "[green]-f, --find[/]", "[green]Find all GIT repos in current folder.[/]" },
        { "[blue]-a, --add <#>[/]", "[blue]Add the repo # to commit queue.[/]" },
        { "[lightskyblue3_1]-d, --details[/]", "[lightskyblue3_1]Show details of the repo #.[/]" },
        { "[lightskyblue3_1]-dv, --details-verbose[/]", "[lightskyblue3_1]Show all details of the repo #.[/]" },
        { "[yellow4_1]-s, --show[/]", "[yellow4_1]Show found repos #.[/]" },
        { "[purple]-dr, --dir <path>[/]", "[purple]Set the directory to search from #.[/]" },
        { "[yellow]-h, --help[/]", "[yellow]Show command table #.[/]" },
        { "[red]-e, --exit <#>[/]", "[red]Add the repo # to commit queue.[/]" },
    };
    private List<string> _repos = new();
    private List<string> _commitQueue = new();
    private string _dir;

    public BigService()
    {
        AnsiConsole.Markup("[underline red]GIT BIG[/]\n");
        DrawTable();
    }

    public void DrawTable()
    {
        var table = new Table();
        table.AddColumn("Command");
        table.AddColumn("Description");
        _commands.ToList().ForEach(x => table.AddRow(new Markup(x.Key), new Markup(x.Value)));
        AnsiConsole.Write(table);
    }

    public void MainLoop()
    {
        _dir = "C:\\Users\\ksups\\PROGRAMS\\JS";//Directory.GetCurrentDirectory();
        FindState(_dir);
        
        var choice = GetInput();
        
        while (!string.Equals(choice[0], "--exit"))
        {
            ParseInput(choice);
            choice = GetInput();
        }
    }
    
    private static string[] GetInput()
    {
        Console.Write(">> ");
        var input = Console.ReadLine();
        
        return input?.Split(' ').Select(i => i = i.Trim()).ToArray() ?? Array.Empty<string>();
    }
    
    private void ParseInput(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            AnsiConsole.Markup("[red]No arguments provided[/]");
            return;
        }
        
        switch (args[0])
        {
            case "--find":
            case "-f":
                FindState(_dir);
                break;
            case "--details":
            case "-d":
                FindDetailState();
                break;
            case "--details-verbose":
            case "-dv":
                FindDetailState(true);
                break;
            case "--dir":
            case "-dr":
                SetDirectory(args);
                break;
            case "--show":
            case "-s":
                ShowState();
                break;
            case "--help":
            case "-h":
                DrawTable();
                break;
            case "--exit":
                return;
            default:
                AnsiConsole.Markup("[red]I didn't understand that.[/]");
                break;
        }
    }

    private void SetDirectory(IReadOnlyList<string> args)
    {
        if (args.Count == 1)
        {
            AnsiConsole.Markup("[red]No directory provided[/]");
            return;
        }
        
        _dir = args[1];
        AnsiConsole.Markup($"[green]Directory set to {_dir}[/]\n");
    }

    private void FindState(string dir)
    {
        AnsiConsole.Status()
            .Start("Thinking...", ctx => 
            {
                _repos = RepoService.GetReposInCurrentDir(dir);
                ctx.Status("Thinking some more");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
            });
        _repos = RepoService.GetReposInCurrentDir(dir);
        
        if (_repos.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos found![/]");
        }
        AnsiConsole.Markup($"[green]Found {_repos.Count} repos in current directory[/]\n");
    }

    private void FindDetailState(bool verbose = false)
    {
        if (_repos.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos found![/]");
            AnsiConsole.MarkupLine("[red]Try passing the -a flag to search All sub directories.[/]");
            return;
        }
        
        // Create the tree
        var root = new Tree("Repository Details");
        
        var colors = new [] { "red", "green", "yellow", "blue", "magenta", "cyan", "white" };
        var random = new Random();

        foreach (var repo in _repos)
        {
            var details = RepoService.GetRepoDetails(repo);
            // get random color
            var color = colors[random.Next(0, colors.Length)];
            var repoNode = root.AddNode($"[{color}]{details.Info.Path}[/]");
            var branches = repoNode.AddNode($"[{color}]Branches: {details.Branches.Count()}[/]");
            var head = repoNode.AddNode($"[{color}]Head: {details.Head.RemoteName}[/]");
            var stashes = repoNode.AddNode($"[{color}]Stashes: {details.Stashes.Count()}[/]");
            var conflicts = repoNode.AddNode($"[{color}]Conflicts: {details.Index.Conflicts.Count()}[/]");

            if (verbose)
            {
                head.AddNode($"[{color}]Canonica lName: {details.Head.CanonicalName}[/]");
                head.AddNode($"[{color}]Friendly Name: {details.Head.FriendlyName}[/]");
                head.AddNode($"[{color}]Is Remote: {details.Head.IsRemote}[/]");
                
                foreach (var detailsBranch in details.Branches)
                {
                    branches.AddNode($"[{color}]Name: {detailsBranch.FriendlyName}[/]");
                    branches.AddNode($"[{color}]Is Remote: {detailsBranch.IsRemote}[/]");
                    branches.AddNode($"[{color}]Is CurrentRepository Head: {detailsBranch.IsCurrentRepositoryHead}[/]");
                    branches.AddNode($"[{color}]Tip: {detailsBranch.Tip}[/]");
                }
                
                foreach (var detailsStash in details.Stashes)
                {
                    stashes.AddNode($"[{color}]Message: {detailsStash.Message}[/]");
                    stashes.AddNode($"[{color}]Stasher: {detailsStash.Index.Author.Name}[/]");
                    stashes.AddNode($"[{color}]Stashed When: {detailsStash.WorkTree.Author.When}[/]");
                    var notesNode = stashes.AddNode($"[{color}]Notes:[/]");
                    foreach (var note in  detailsStash.Index.Notes)
                    {
                        notesNode.AddNode($"[{color}]Note: {note.Message}[/]");
                    }
                }
                
                foreach (var detailsConflict in details.Index.Conflicts)
                {
                    conflicts.AddNode($"[{color}]Path: {detailsConflict.Ours.Path}[/]");
                    conflicts.AddNode($"[{color}]Ancestor Id: {detailsConflict.Ancestor.Id}[/]");
                    conflicts.AddNode($"[{color}]Our Id: {detailsConflict.Ours.Id}[/]");
                    conflicts.AddNode($"[{color}]Their Id: {detailsConflict.Theirs.Id}[/]");
                    conflicts.AddNode($"[{color}]Our Stage Number: {detailsConflict.Ours.StageLevel}[/]");
                }
            }
        }
        // Add some nodes
        // var foo = root.AddNode("[yellow]Foo[/]");
        // var table = foo.AddNode(new Table()
        //     .RoundedBorder()
        //     .AddColumn("First")
        //     .AddColumn("Second")
        //     .AddRow("1", "2")
        //     .AddRow("3", "4")
        //     .AddRow("5", "6"));
        //
        // table.AddNode("[blue]Baz[/]");
        // foo.AddNode("Qux");

        // var bar = root.AddNode("[yellow]Bar[/]");
        // bar.AddNode(new Calendar(2020, 12)
        //     .AddCalendarEvent(2020, 12, 12)
        //     .HideHeader());

        // Render the tree
        AnsiConsole.Write(root);
    }

    private void ShowState()
    {
        if (_repos.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Found the following repos:[/]");
            _repos.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]"));
        }
        else
        {
            AnsiConsole.MarkupLine("[red]No repos found![/]");
        }
    }
    
    private void SelectState()
    {
        
        AnsiConsole.Markup("[green]Select a repo to add to commit queue.[/]\n");
        var table = new Table();
        table.AddColumn("Repo #");
        table.AddColumn("Repo Name");
        
        var repoPrompts = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("What are your [green]favorite repos[/]?")
                .NotRequired()
                .PageSize(10)
                .Mode(SelectionMode.Leaf)
                .MoreChoicesText("[grey](Move up and down to reveal more repos)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a repo, " + 
                    "[green]<enter>[/] to accept)[/]")
                .AddChoices(_repos));

        // Write the selected repos to the terminal
        foreach (string repo in repoPrompts) 
        {
            AnsiConsole.WriteLine(repo);
        }
        
        if (repoPrompts.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos selected![/]");
            return;
        }
        
        AnsiConsole.MarkupLine("[green]Selected Repos:[/]");
        repoPrompts.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]"));
        
        // Add selected repos to commit queue
        _commitQueue.AddRange(repoPrompts);
        
    }
}