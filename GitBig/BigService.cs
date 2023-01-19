using LibGit2Sharp;
using Spectre.Console;
using Tree = Spectre.Console.Tree;

namespace GitBig;

public class BigService
{

    private Dictionary<string, string> _commands = new () {
        { "[green]-f, --find[/]", "[green]Find all GIT repos in current folder.[/]" },
        // { "[blue]-a <#>, --add <#>[/]", "[blue]Add the repo # to commit queue.[/]" },
        { "[lightskyblue3_1]-d, --details[/]", "[lightskyblue3_1]Show details of the repo #.[/]" },
        { "[deeppink4_2]-dv, --details-verbose[/]", "[deeppink4_2]Show all details of the repo #.[/]" },
        { "[yellow4_1]-sr, --show-repo-queue[/]", "[yellow4_1]Show found repos #.[/]" },
        { "[sandybrown]-sc, --show-commit-queue[/]", "[sandybrown]Show current commit queue #.[/]" },
        { "[deeppink1_1]-sb, --show-branch-queue[/]", "[deeppink1_1]Show current branch queue #.[/]" },
        { "[mistyrose1]-sd, --show-diff[/]", "[mistyrose1]Show diff against <branch name> or remote if <branch name> not specified #.[/]" },
        { "[gold3]-se, --select[/]", "[gold3]Select repos to commit #.[/]" },
        { "[violet]-c <message>, --commit <message>[/]", "[violet]Commit the selected repos (stages changes) with <message> commit message #.[/]" },
        { "[deeppink3]-b, --branch[/]", "[deeppink3]Create new branch for selected repos #.[/]" },
        { "[lightsalmon3_1]-bc, --branch-checkout[/]", "[lightsalmon3_1]Create new branch and check it out for selected repos #.[/]" },
        { "[purple]-dr, --dir <path>[/]", "[purple]Set the directory to search from #.[/]" },
        { "[yellow]-h, --help[/]", "[yellow]Show command table #.[/]" },
        { "[red]-e, --exit <#>[/]", "[red]Exit[/]" },
    };
    private StateModel _stateModel;
    private string _dir;

    public BigService()
    {
        AnsiConsole.Markup("[underline red]MICRO-GIT[/]\n");
        _stateModel = new StateModel();
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
            case "--select":
            case "-se":
                SelectState();
                break;
            case "--commit":
            case "-c":
                CommitChanges(args);
                break;
            case "--show-commit-queue":
            case "-sc":
                ShowCommit();
                break;
            case "--show-repo-queue":
            case "-sr":
                ShowRepos();
                break;
            case "--show-branch-queue":
            case "-sb":
                ShowBranches();
                break;
            case "--show-diff":
            case "-sd":
                ShowDiff();
                break;
            case "--branch":
            case "-b":
                BranchState();
                break;
            case "--branch-checkout":
            case "-bc":
                BranchState(true);
                break;
            case "--help":
            case "-h":
                DrawTable();
                break;
            case "--exit":
                return;
            default:
                AnsiConsole.Markup("[red]I didn't understand that.[/]\n");
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

    private void CommitChanges(IReadOnlyList<string> args)
    {
        if (args.Count == 1)
        {
            AnsiConsole.Markup("[red]No commit message provided[/]");
            return;
        }
        
        if (_stateModel.commitQueue.Count == 0)
        {
            AnsiConsole.Markup("[red]No repos selected[/]\n");
            return;
        }
        
        var commitMessage = string.Join(" ", args.Skip(1));
        
        if (string.IsNullOrEmpty(commitMessage))
        {
            AnsiConsole.Markup("[red]No commit message provided[/]\n");
            return;
        }

        foreach (var repo in _stateModel.commitQueue)
        {
            using var repository = new Repository(repo);
            var status = repository.RetrieveStatus();
            var files = status.Where(x => x.State is FileStatus.ModifiedInWorkdir or FileStatus.NewInWorkdir).ToList();
            if (files.Count == 0)
            {
                AnsiConsole.Markup($"[red]{repo}[/] [yellow]has no changes to commit[/]\n");
                continue;
            }
            
            if (status.IsDirty)
            {
                Commands.Stage(repository, "*");
                var authorName = repository.Config.Get<string>("user.name").Value;
                var author = new Signature(authorName, "na", DateTimeOffset.Now);
                repository.Commit(commitMessage, author, author);
            }
        }
    }

    private void ShowDiff()
    {
        // Show diff of selected repos compared to remote

        RepoService.GetRemoteBranches(_stateModel.repos, _stateModel.remoteBranchQueue);
        RepoService.FetchRemoteBranches(_stateModel.remoteBranchQueue);
        
        
        var selectedRepos = RepoSelectPrompt("Select repos to show diff for");

        var root = new Tree("Diffs");
        foreach (var repo in selectedRepos)
        {
            var repoName = repo.Split('\\').Last();
            var repoNode = root.AddNode($"[yellow]{repoName}[/]");
            using var repository = new Repository(repo);
            
            var toCommit = repository.Head.Tip.Tree;
            var fromCommit = repository.Head.Tip.Parents.First().Tree;
            // var toCommit = repository.Head.TrackedBranch.Tip.Tree;
            
            var patch = repository.Diff.Compare<Patch>(fromCommit, toCommit);

            var innerTable = new Table()
                .RoundedBorder()
                .AddColumn("Files");

            foreach (var pec in patch)
            {
                innerTable.AddRow($"[red]{pec.Path} = {pec.LinesAdded + pec.LinesDeleted} ({pec.LinesAdded}+ and {pec.LinesDeleted}-)[/]");
            }
            
            repoNode.AddNode(innerTable);
            
        }
        
        AnsiConsole.Write(root);
    }

    private void BranchState(bool shouldCheckout = false)
    {
        try
        {
            SelectReposToBranch();
            if (_stateModel.branchQueue.Count == 0)
            {
                AnsiConsole.Markup("[red]No repos selected[/]\n");
                return;
            }
            AnsiConsole.Markup("[green]Enter branch name[/]\n");
            var branchName = GetInput()[0];
            if (string.IsNullOrEmpty(branchName))
            {
                AnsiConsole.Markup("[red]No branch name provided[/]\n");
                return;
            }
            
            foreach (var (repo, branch) in _stateModel.branchQueue)
            {
                using var repository = new Repository(repo);
                Branch? newBranch = null;
                
                if (repository.Branches[branchName] == null)
                {
                    newBranch = repository.CreateBranch(branchName);
                    _stateModel.branchQueue[repo] = newBranch.CanonicalName;
                }
                else
                {
                    AnsiConsole.Markup($"[red]Branch {branchName} already exists in {repo}[/]\n");
                }
                
                
                if (shouldCheckout && newBranch != null)
                {
                    Commands.Checkout(repository, newBranch);
                }
            }

            if (!shouldCheckout)
            {
                var shouldCheckoutBranch = YesNoSelectPrompt($"Do you want to checkout the branch {branchName} for all repos?");
                if (string.Equals(shouldCheckoutBranch.First(), "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var (repo, branch) in _stateModel.branchQueue)
                    {
                        using var repository = new Repository(repo);
                        var branchToCheckout = repository.Branches[branch];
                        Commands.Checkout(repository, branchToCheckout);
                    }
                    AnsiConsole.Markup($"[green]Branch {branchName} created and checked out successfully for the following repos:[/]\n");
                    _stateModel.branchQueue.ToList().ForEach(x => AnsiConsole.Markup($"[green]{x.Key}[/]\n"));
                }
            }
            
            if (shouldCheckout)
            {
                AnsiConsole.Markup($"[green]Branch {branchName} created and checked out successfully for the following repos:[/]\n");
                _stateModel.branchQueue.ToList().ForEach(x => AnsiConsole.Markup($"[green]{x.Key}[/]\n"));
            }
        }
        catch (Exception e)
        {
            AnsiConsole.Markup($"[red]{e.Message}[/]");
        }
    }

    private void FindState(string dir)
    {
        AnsiConsole.Status()
            .Start("Thinking...", ctx => 
            {
                _stateModel.repos = RepoService.GetReposInCurrentDir(dir);
                RepoService.GetRemoteBranches(_stateModel.repos, _stateModel.remoteBranchQueue);
                ctx.Status("Thinking some more");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
            });
        _stateModel.repos = RepoService.GetReposInCurrentDir(dir);
        
        if (_stateModel.repos.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos found![/]\n");
        }
        AnsiConsole.Markup($"[green]Found {_stateModel.repos.Count} repos in current directory[/]\n");
    }

    private void FindDetailState(bool verbose = false)
    {
        if (_stateModel.repos.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos found![/]");
            AnsiConsole.MarkupLine("[red]Try passing the -a flag to search All sub directories.[/]\n");
            return;
        }
        
        // Create the tree
        var root = new Tree("Repository Details");
        
        var colors = new [] { "red", "green", "yellow", "blue", "magenta", "cyan", "white" };
        var random = new Random();

        foreach (var repo in _stateModel.repos)
        {
            var details = RepoService.GetRepoDetails(repo);
            // get random color
            var color = colors[random.Next(0, colors.Length)];
            var repoNode = root.AddNode($"[{color}]{details.Info.Path}[/]");
            var branches = repoNode.AddNode($"[{color}]Branches: {details.Branches.Count()}[/]");
            var localBranch = repoNode.AddNode($"[{color}]Local Branch: {details.Branches.First().FriendlyName}[/]");
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

    private void ShowRepos()
    {
        if (_stateModel.repos.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Found the following repos:[/]");
            _stateModel.repos.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]"));
        }
        else
        {
            AnsiConsole.MarkupLine("[red]No repos found! You can specify the directory path with -dr <path>[/]\n");
        }
    }

    private void ShowCommit()
    {
        if (_stateModel.commitQueue.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Found the following commits:[/]");
            _stateModel.commitQueue.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]\n"));
        }
        else
        {
            AnsiConsole.MarkupLine("[red]No commits currently being tracked![/]\n");
        }
    }

    private void ShowBranches()
    {
        if (_stateModel.branchQueue.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Found the following branches:[/]");
            _stateModel.branchQueue.ToList().ForEach(x =>
            {
                AnsiConsole.MarkupLine($"[blue]{x.Key}[/]\n");
                AnsiConsole.MarkupLine($"[blue]{x.Value}[/]\n");
            });
        }
        else
        {
            AnsiConsole.MarkupLine("[red]No branches currently being tracked! You can create a new branch with -b[/]\n");
        }
    }

    private void SelectReposToBranch()
    {
        
        var repoPrompts = RepoSelectPrompt("[green]Select repos to create a new branch for.[/]\n");

        if (repoPrompts.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos selected![/]\n");
            return;
        }
        
        AnsiConsole.MarkupLine("[green]Selected Repos:[/]");
        repoPrompts.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]"));
        
        // Add selected repos to commit queue
        foreach (var repoPrompt in repoPrompts)
        {
            _stateModel.branchQueue.Add(repoPrompt, "");
        }
        
    }

    private void SelectState()
    {
        var repoPrompts = RepoSelectPrompt("[green]Select a repo to add to commit queue.[/]\n");
        
        if (repoPrompts.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos selected![/]\n");
            return;
        }
        
        AnsiConsole.MarkupLine("[green]Selected Repos:[/]");
        repoPrompts.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]"));
        
        // Add selected repos to commit queue
        _stateModel.commitQueue.AddRange(repoPrompts);
    }

    private List<string> RepoSelectPrompt(string instructions)
    {
        AnsiConsole.Markup($"[green]{instructions}[/]\n");
        var table = new Table();
        table.AddColumn("Repo #");
        table.AddColumn("Repo Name");

        var repoPrompts = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select your [green]repos[/]?")
                .NotRequired()
                .PageSize(10)
                .Mode(SelectionMode.Leaf)
                .MoreChoicesText("[grey](Move up and down to reveal more repos)[/]")
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a repo, " +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoiceGroup("Repos", _stateModel.repos));

        // Write the selected repos to the terminal
        foreach (string repo in repoPrompts)
        {
            AnsiConsole.WriteLine(repo);
        }

        return repoPrompts;
    }
    
    private List<string> YesNoSelectPrompt(string instructions)
    {
        AnsiConsole.Markup($"[green]{instructions}[/]\n");
        var table = new Table();
        table.AddColumn("Repo #");
        table.AddColumn("Repo Name");

        var repoPrompts = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [green]Yes[/] or [red]No[/]?")
                .NotRequired()
                .PageSize(10)
                .Mode(SelectionMode.Leaf)
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to select, " +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoices(new[]{"Yes", "No"}));

        // Write the selected repos to the terminal
        foreach (string repo in repoPrompts)
        {
            AnsiConsole.WriteLine(repo);
        }

        return repoPrompts;
    }
}