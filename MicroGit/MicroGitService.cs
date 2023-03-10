using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LibGit2Sharp;
using MicroGit.HelperFunctions;
using Spectre.Console;
using Tree = Spectre.Console.Tree;

namespace MicroGit;

/*
 * LibGit2Sharp Test Cases:
 * https://github.com/libgit2/libgit2sharp/blob/a4c6c4552b24590995c221304e7d7c75c181ea82/LibGit2Sharp.Tests/BranchFixture.cs#L574-L603
 */

public class MicroGitService
{
    private Dictionary<string, string> _commonCommands = new()
    {
        { "", "" },
        { "Frequently Used Commands", "-------------------------------------------------------------------" },
        { "[red3_1]-cr, --creds[/]", "[red3]Set your GIT login credentials! (Required to push)[/]" },
        { "[red3]-c --commit[/]", "[red3]Commit the selected repos (stages ALL changes) with <message> commit message #.[/]" },
        { "[darkorange3_1]-fe, --fetch[/]", "[darkorange3_1]Fetch remote changes for selected branches #.[/]" },
        { "[darkorange3]-pu, --pull[/]", "[darkorange3]Pull remote changes for selected branches #.[/]" },
        { "[lightsalmon3_1]-d, --details (-v for verbose)[/]", "[lightsalmon3_1]Show details of the repo #.[/]" },
        { "[orange3]-bc, --branch-checkout[/]", "[orange3]Create new branch and check it out for selected repos #.[/]" },
        { "[gold3_1]-dr, --dir <path>[/]", "[gold3_1]Set the directory to search from #.[/]" },
        { "[yellow3_1]-da, --dir-add <path>[/]", "[yellow3_1]Add additional directories to be searched #.[/]" },
        { "[chartreuse1]-e, --exit <#>[/]", "[chartreuse1]Exit[/]" },
    };
    private Dictionary<string, string> _commands = new ()
    {
        { "", "" },
        { "Additional Commands", "-------------------------------------------------------------------" },
        { "[yellow2]-b, --branch[/]", "[yellow2]Create new branch for selected repos #.[/]" },
        { "[chartreuse3]-f, --find[/]", "[chartreuse3]Find all GIT repos in current folder.[/]" },
        { "[chartreuse3_1]-dl, --dir-list <path>[/]", "[chartreuse3_1]List saved directories #.[/]" },
        { "[seagreen2]-sv, --saved[/]", "[seagreen2]Show saved info #.[/]" },
        { "[seagreen1_1]-sd, --show-diff (-v for verbose)[/]", "[seagreen1_1]Show local changes in current branch #.[/]" },
        { "[aquamarine1]-sr, --show-repo-queue[/]", "[aquamarine1]Show found repos #.[/]" },
        { "[skyblue1]-sc, --show-commit-queue[/]", "[skyblue1]Show current commit queue #.[/]" },
        { "[deepskyblue2]-sb, --show-branch-queue[/]", "[deepskyblue2]Show current branch queue #.[/]" },
        { "[violet]-se, --select[/]", "[violet]Select repos to commit #.[/]" },
        { "[mediumpurple2]-su, --set-upstream[/]", "[mediumpurple2]Set the upstream branch for selected repos#.[/]" },
        { "[hotpink2]-dc, --delete-credentials[/]", "[hotpink2]Delete saved credentials #.[/]" },
        { "[deeppink3_1]-h, --help[/]", "[deeppink3_1]Show command table #.[/]" },
        { "[deeppink4_2]-e, --exit[/]", "[deeppink4_2]Exit (Also ctrl + c)[/]" },
    };
    private List<string> _dir;
    private ConfigManager _configManager;

    public MicroGitService()
    {
        Console.CancelKeyPress += delegate {
            Environment.Exit(0);
        };
        
        AnsiConsole.Write(
            new FigletText("MICRO-GIT")
                .Color(Color.Red));
        AnsiConsole.Markup("[red]The Ultimate GIT CLI Tool[/]\n");
        AnsiConsole.Markup("[bold yellow]NOTE: In order to fetch / push to private GitHub repos you need to supply a Personal Access Token with the \"repo\" scope![/]\n");
        _configManager = new ConfigManager();
        DrawTable();
    }

    public void DrawTable()
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        var cmdCol = table.AddColumn("Command");
        var descCol = table.AddColumn("Description");
        cmdCol.Alignment = Justify.Left;
        descCol.Alignment = Justify.Left;

        _commonCommands.ToList().ForEach(x => table.AddRow(new Markup(x.Key), new Markup(x.Value)));
        _commands.ToList().ForEach(x => table.AddRow(new Markup(x.Key), new Markup(x.Value)));
        AnsiConsole.Write(table);
    }

    public void MainLoop()
    {
        //TODO: Remove this path
        _dir = new() { Directory.GetCurrentDirectory() };
        FindState(_dir);
        
        HistoryQueue.DetectArrowKeyPress();
        HistoryQueue._keyListenThread.Start();
        var choice = GetInput();
        
        while (!string.Equals(choice[0], "--exit"))
        {
            ParseInput(choice);
            HistoryQueue._stopThread = false;
            choice = GetInput();
        }
    }
    
    private string[] GetInput()
    {
        Console.Write(">> ");
        var input = Console.ReadLine();
        HistoryQueue._stopThread = true;
        return input?.Split(' ').Select(i => i = i.Trim()).ToArray() ?? Array.Empty<string>();
    }
    
    private void ParseInput(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            AnsiConsole.Markup("[red]No arguments provided[/]");
            return;
        }
        
        HistoryQueue.PushCommand(args);
        var verbose = MicroGitHelpers.IsVerbose(args);
        
        switch (args[0])
        {
            case "--creds":
            case "-cr":
                GetCredentials();
                break;
            case "--find":
            case "-f":
                FindState(_configManager.GetDirectories());
                break;
            case "--fetch":
            case "-fe":
                FetchState();
                break;
            case "--pull":
            case "-pu":
                PullState();
                break;
            case "--details":
            case "-d":
                FindDetailState(verbose);
                break;
            case "--dir":
            case "-dr":
                SetDirectory(args);
                break;
            case "--dir-add":
            case "-da":
                AddDirectory(args);
                break;
            case "--dir-list":
            case "-dl":
                ListDirectories();
                break;
            case "--select":
            case "-se":
                SelectState();
                break;
            case "--commit":
            case "-c":
                CommitChanges(args);
                break;
            case "--saved":
            case "-sv":
                ShowSavedData();
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
                ShowDiff(verbose);
                break;
            case "--set-upstream":
            case "-su":
                SetUpstreamBranches();
                break;
            case "--branch":
            case "-b":
                BranchState();
                break;
            case "--branch-checkout":
            case "-bc":
                BranchState(true);
                break;
            case "--branch-checkout-stage-push":
            case "-bcsp":
                BranchState(true, true, true);
                break;
            case "--help":
            case "-h":
                DrawTable();
                break;
            case "--delete-credentials":
            case "-dc":
                DeleteCredentials();
                break;
            case "--exit":
            case "-e":
                return;
            default:
                AnsiConsole.Markup("[red]I didn't understand that.[/]\n");
                break;
        }
    }

    private void FetchState()
    {
        try
        {
            var selectedRepos = Utils<string>.RepoSelectPrompt("Select repos you want to fetch:", _configManager);
            foreach (var repoPath in selectedRepos)
            {
                RepoService.FetchLogic(repoPath, _configManager);
                AnsiConsole.MarkupLine($"[green]{Utils<string>.NameFromPath(repoPath)} Done![/]\n");
            }
            
            AnsiConsole.MarkupLine($"[green]Done![/]\n");
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
    }

    private void PullState()
    {
        try
        {
            var mergeOptions = _configManager.GetMergeOptions();
            var selectedRepos = Utils<string>.RepoSelectPrompt("Select repos you want to fetch:", _configManager);
            var useSavedMergeOptions = false;

            if (mergeOptions != null)
            {
                // TODO: print merge options
                // AnsiConsole.Write(JsonSerializer.Serialize(mergeOptions).EscapeMarkup());
                useSavedMergeOptions = Utils<string>.CustomPrompt<bool>("Do you want to use these saved merge options?", "Save options", new List<bool> { true, false });
            }

            if (mergeOptions == null || !useSavedMergeOptions)
            {
                var fastForwardStrategy = Utils<string>.CustomPrompt<string>("Select fast forward strategy for all repos:", "Select strategy",
                    new List<string> { "Fast forward if possible, otherwise, don't fast forward (Default)", "Only fast forward", "Do not fast forward" });
                var mergeFavor = Utils<string>.CustomPrompt<string>("Select merge favor for all repos:", "Select favor",
                    new List<string> { "Create merge file (Default)", "Current branch", "Their branch" });
                var conflictStrategy = Utils<string>.CustomPrompt<string>("Select conflict strategy for all repos:", "Select strategy",
                    new List<string> { "Create merge files for conflicts (Default)", "Keep theirs", "Keep mine" });
                var saveOptions = Utils<string>.CustomPrompt<bool>("Do you want to save these options?", "Save options", new List<bool> { true, false });
                mergeOptions = Utils<string>.ParseMergeOptions(fastForwardStrategy, mergeFavor, conflictStrategy);
            
                if (saveOptions)
                {
                    _configManager.SetMergeOptions(mergeOptions);
                }
            }
            
            foreach (var repoPath in selectedRepos)
            {
                RepoService.PullLogic(repoPath, mergeOptions, _configManager);
                AnsiConsole.MarkupLine($"[green]{Utils<string>.NameFromPath(repoPath)} Done![/]\n");
            }
            
            AnsiConsole.MarkupLine($"[green]Done![/]\n");
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
    }

    private void SetUpstreamBranches()
    {
        try
        {
            var createNewRemote = "Create new remote";
            var usePreviousBranchName = false;
            var createdBranchName = string.Empty;
            var selectedRepos = Utils<string>.RepoSelectPrompt("Select repos you want to set upstream branches for:", _configManager);

            foreach (var repoPath in selectedRepos)
            {
                var repoName = Utils<string>.NameFromPath(repoPath);
                createdBranchName = MicroGitHelpers.SetUpstreamLogic(repoPath, repoName, createdBranchName, createNewRemote, 
                    _configManager, ref usePreviousBranchName);
            }
            
            AnsiConsole.MarkupLine($"[green]Done![/]\n");
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
    }

    private void DeleteCredentials()
    {
        _configManager.DeleteSavedCredentials();
    }

    private void GetCredentials()
    {
        var remoteHost = Utils<RemoteTypes>.CustomPrompt<RemoteTypes>("What remote host are you using?", "Remote Host", new()
        {
            RemoteTypes.GitHub, RemoteTypes.GitLab, RemoteTypes.TFS
        });

        _configManager.SetRemoteType(remoteHost);
        if (remoteHost == RemoteTypes.TFS)
        {
            AnsiConsole.Markup("[green]I'll use your system credentials to authenticate![/]\n");
            AnsiConsole.Markup("[green]Done![/]\n");
            return;
        }

        var saveCreds = Utils<string>.YesNoSelectPrompt("Do you want to save your credentials? (Recommended) They will be encrypted.");
        bool shouldSave = string.Equals(saveCreds, "Yes", StringComparison.OrdinalIgnoreCase);
        AnsiConsole.Markup("[green]Enter your GIT username[/]");
        AnsiConsole.Markup("[green]Username: [/]");
        var username = Console.ReadLine();
        AnsiConsole.Markup("[green]Enter your GIT Personal Access Token[/]\n");
        AnsiConsole.Markup("[red]Your token must have the \"repo\" scope in order to push to private repos.[/]\n");
        AnsiConsole.Markup("[red]---If you're not sure how to get this, \n---check out the instructions at the link below: \nhttps://docs.github.com/en/enterprise-server@3.4/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token[/]");
        AnsiConsole.Markup("\n[green]Token: [/]");
        var password = Utils<string>.GetPassword();
        if (string.IsNullOrEmpty(password))
        {
            AnsiConsole.Markup("[green]No password provided. :([/]\n");
        }
        Console.WriteLine();
        _configManager.SetCreds(username ?? "", password ?? "", shouldSave);
        AnsiConsole.Markup("[green]Credentials set successfully![/]\n");
    }

    private void ShowSavedData()
    {
        var state = _configManager.state;
        foreach (var stateRepo in state.Repos)
        {
            AnsiConsole.Markup($"[green]Repo: {stateRepo}[/]\n");
        }
        
        foreach (var stateCommit in state.CommitQueue)
        {
            AnsiConsole.Markup($"[green]Commit: {stateCommit}[/]\n");
        }
        
        foreach (var stateBranch in state.BranchQueue)
        {
            AnsiConsole.Markup($"[green]Branch: {stateBranch.Key}[/]\n");
        }
        
        var (email, un, pwd, pat, host) = _configManager.GetSavedCredentials();
        AnsiConsole.Markup($"[green]Email: [/][yellow]{email}[/]\n");
        AnsiConsole.Markup($"[green]Username: [/][yellow]{un}[/]\n");
        AnsiConsole.Markup($"[green]Password (encrypted): [/][yellow]{pwd}[/]\n");
        AnsiConsole.Markup($"[green]PersonalAccessToken (encrypted): [/][yellow]{pat}[/]\n");
        AnsiConsole.Markup($"[green]RemoteHost: [/][yellow]{host}[/]\n");
    } 

    private void SetDirectory(IReadOnlyList<string> args)
    {
        if (args.Count == 1)
        {
            AnsiConsole.Markup("[red]No directory provided[/]\n");
            return;
        }

        // skip first arg and join the rest
        var dr = string.Join(" ", args.Skip(1));
        _configManager.AddDirectory(dr);
        _configManager.SetCurrentDirectory(new () {dr});
        AnsiConsole.Markup($"[greenyellow]Directory set to {_dir.First()}[/]\n"); //TODO: make this work with multiple directories
    }

    private void AddDirectory(IReadOnlyList<string> args)
    {
        if (args.Count == 1)
        {
            AnsiConsole.Markup("[red]No directory provided[/]\n");
            return;
        }

        // skip first arg and join the rest
        var dr = string.Join(" ", args.Skip(1));
        _configManager.AddDirectory(dr);
        AnsiConsole.Markup($"[greenyellow]Tracked directories now include: [/]\n");
        ListDirectories();
    }
    
    private void ListDirectories()
    {
        var dirs = _configManager.GetDirectories();
        if (dirs.Count == 0)
        {
            AnsiConsole.Markup("[red]No saved directories, use -dr to set or -da to add a directory to saved state.[/]\n");
            return;
        }
        
        AnsiConsole.Markup("[green]Directories:[/]\n");
        foreach (var dir in dirs)
        {
            AnsiConsole.Markup($"[green]{dir}[/]\n");
        }
    }

    private void CommitChanges(IReadOnlyList<string> args)
    {
        try
        {
            if ((string.IsNullOrEmpty(_configManager.GetUsername()) ||
                 string.IsNullOrEmpty(_configManager.GetPersonalAccessToken())) &&
                _configManager.GetRemoteType() != RemoteTypes.TFS)
            {
                AnsiConsole.Markup("[red]No credentials set. Use --creds or -cr to set credentials[/]\n");
                return;
            }
        
            // if (_configManager.state.CommitQueue.Count == 0)
            // {
                SelectState();
            // }
        
            var commitMessage = string.Join(" ", args.Skip(1));
        
            if (string.IsNullOrEmpty(commitMessage))
            {
                AnsiConsole.Markup("[red]Please enter a commit message[/]: ");
                commitMessage = Console.ReadLine() ?? "New commit";
            }

            foreach (var repo in _configManager.state.CommitQueue)
            {
                using var repository = new Repository(repo);
                var status = repository.RetrieveStatus();
                var files = status.Modified.Select(mods => mods.FilePath).ToList();

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
                    RepoService.PushChanges(repository, commitMessage, _configManager);
                    AnsiConsole.Markup($"[green]{repo}[/] [yellow]committed successfully![/]\n");
                }
            }
        }
        catch (Exception e)
        {
            AnsiConsole.Markup($"[red]Error: {e.Message}[/]\n");
        }
        
    }

    private void ShowDiff(bool verbose = false)
    {
        // Show diff of selected repos compared to remote

        try
        {
            var selectedRepos = Utils<string>.RepoSelectPrompt("Select repos to show diff for", _configManager);
            var root = new Tree("Diffs");
            AnsiConsole.Status()
                .Start("Thinking...", ctx => 
            {
                foreach (var repoPath in selectedRepos)
                {
                    var repoName = Utils<string>.NameFromPath(repoPath);
                    var repoNode = root.AddNode($"[yellow]{repoName}[/]");

                    using var repo = new Repository(repoPath);
                    var innerTable = new Table()
                        .RoundedBorder();
                    innerTable.AddColumn("Details");
                    var status = repo.RetrieveStatus();

                    var fileRow = innerTable.AddRow($"[green]File Changes[/]");
                    
                    var totalChanged = status.Modified.Count() + status.Added.Count() + status.Removed.Count();
                    fileRow.AddRow($"[green]Total changed files: [/][darkorange]{totalChanged}[/]");
                    
                    foreach (var item in status)
                    {
                        fileRow.AddRow($"[green]{item.FilePath}[/] [darkorange]{item.State}[/]");

                        var sb = new StringBuilder();
                        if (item.State == FileStatus.ModifiedInWorkdir || item.State == FileStatus.ModifiedInIndex) 
                        {
                            var patch = repo.Diff.Compare<Patch> (new List<string>() { item.FilePath });
                            
                            foreach (var pec in patch)
                            {
                                innerTable.AddRow($"[red]{pec.Path} = {pec.LinesAdded + pec.LinesDeleted} ({pec.LinesAdded}+ and {pec.LinesDeleted}-)[/]");
                            }

                            if (!verbose) continue;
                            sb.AppendLine("[red]~~~~ Patch file ~~~~[/]");
                            var lines = patch.Content.Split("\n").ToList();
                            foreach (var line in lines)
                            {
                                if (line.StartsWith("+"))
                                {
                                    sb.AppendLine($"[green]{line.EscapeMarkup()}[/]");
                                }
                                else if (line.StartsWith("-"))
                                {
                                    sb.AppendLine($"[deeppink3]{line.EscapeMarkup()}[/]");
                    
                                }
                                else
                                {
                                    sb.AppendLine(line.EscapeMarkup());
                                }
                            }

                            if (sb.Length > 0)
                            {
                                fileRow.AddRow(new Panel(sb.ToString()));
                            }
                            else
                            {
                                fileRow.AddRow(new Panel("[yellow]No changes[/]"));
                            }
                        }
                    }
                    
                    repoNode.AddNode(innerTable);
                }
                ctx.Status("Thinking some more");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("red"));
            });
            
            AnsiConsole.Write(root);
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
    }

    private void BranchState(bool shouldCheckout = false, bool shouldStageAll = false, bool shouldPush = false)
    {
        try
        {
            SelectReposToBranch();
            if (_configManager.state.BranchQueue.Count == 0)
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
            
            bool didCheckout = false;
            foreach (var (repo, branch) in _configManager.state.BranchQueue)
            {
                using var repository = new Repository(repo);
                Branch? newBranch = null;
                
                if (repository.Branches[branchName] == null)
                {
                    newBranch = repository.CreateBranch(branchName);
                    _configManager.state.BranchQueue[repo] = newBranch.CanonicalName;
                }
                else
                {
                    AnsiConsole.Markup($"[red]Branch {branchName} already exists in {repo}[/]\n");
                }
                
                
                if (shouldCheckout && newBranch != null)
                {
                    didCheckout = true;
                    Commands.Checkout(repository, newBranch);
                }
            }

            if (!shouldCheckout)
            {
                var shouldCheckoutBranch = Utils<string>.YesNoSelectPrompt($"Do you want to checkout the branch {branchName} for all repos?");
                if (string.Equals(shouldCheckoutBranch, "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    didCheckout = true;
                    foreach (var (repo, branch) in _configManager.state.BranchQueue)
                    {
                        using var repository = new Repository(repo);
                        var branchToCheckout = repository.Branches[branch];
                        Commands.Checkout(repository, branchToCheckout);
                    }
                    AnsiConsole.Markup($"[green]Branch {branchName} created and checked out successfully for the following repos:[/]\n");
                    _configManager.state.BranchQueue.ToList().ForEach(x => AnsiConsole.Markup($"[green]{x.Key}[/]\n"));
                }
            }
            
            if (shouldCheckout)
            {
                AnsiConsole.Markup($"[green]Branch {branchName} created and checked out successfully for the following repos:[/]\n");
                _configManager.state.BranchQueue.ToList().ForEach(x => AnsiConsole.Markup($"[green]{x.Key}[/]\n"));
            }
            
            if (shouldStageAll)
            {
                foreach (var (repo, branch) in _configManager.state.BranchQueue)
                {
                    using var repository = new Repository(repo);
                    var status = repository.RetrieveStatus();
                    if (status.IsDirty)
                    {
                        Commands.Stage(repository, "*");
                    }
                }
            }
            
            if (shouldPush)
            {
                foreach (var (repo, branch) in _configManager.state.BranchQueue)
                {
                    using var repository = new Repository(repo);
                    var status = repository.RetrieveStatus();
                    if (status.IsDirty)
                    {
                        Commands.Stage(repository, "*");
                    }
                    var authorName = repository.Config.Get<string>("user.name").Value;
                    var author = new Signature(authorName, "na", DateTimeOffset.Now);
                    repository.Commit($"Auto commit for branch {branchName}", author, author);
                    repository.Network.Push(repository.Head, new PushOptions
                    {
                        CredentialsProvider = (url, usernameFromUrl, types) => new UsernamePasswordCredentials
                        {
                            Username = _configManager.GetUsername(),
                            Password = _configManager.GetPersonalAccessToken()
                        }
                    });
                }
            }

        }
        catch (Exception e)
        {
            AnsiConsole.Markup($"[red]{e.Message}[/]");
        }
    }

    private void FindState(List<string> dirs)
    {
        AnsiConsole.Status()
            .Start("Thinking...", ctx => 
            {
                foreach (var dir in dirs)
                {
                    try
                    {
                        var repos = RepoService.GetReposInCurrentDir(dir);
                        foreach (var repo in repos)
                        {
                            using var repository = new Repository(repo);
                            if (!_configManager.state.Repos.Contains(repo))
                            {
                                _configManager.state.Repos.Add(repo);
                            }
                        }
                        _configManager.AddDirectory(dir);
                    }
                    catch (Exception e)
                    {
                        AnsiConsole.Markup($"[red]{e.Message}[/]");
                    }
                    
                }
                
                RepoService.GetRemoteBranches(_configManager.state.Repos, _configManager.state.RemoteBranchQueue);
                ctx.Status("Thinking some more");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
            });
        
        if (_configManager.state.Repos.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos found![/]\n");
        }
        AnsiConsole.Markup($"[green]Found {_configManager.state.Repos.Count} repos in current directory[/]\n");
    }

    private void FindDetailState(bool verbose = false)
    {
        if (_configManager.state.Repos.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos found![/]");
            AnsiConsole.MarkupLine("[red]Try passing the -a flag to search All sub directories.[/]\n");
            return;
        }
        var selectedRepos = Utils<string>.RepoSelectPrompt("Select repos to show details for", _configManager);
        if (selectedRepos.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos selected![/]\n");
            return;
        }
        // Create the tree
        var root = new Tree("Repository Details");
        AnsiConsole.Status()
            .Start("Thinking...", ctx => 
        {
            foreach (var repo in selectedRepos)
            {
                using var repository = new Repository(repo);
                var status = repository.RetrieveStatus();
                var files = status.Modified.Select(mods => mods.FilePath).ToList();
                
                var details = RepoService.GetRepoDetails(repo);
                var repoNode = root.AddNode($"[gold3_1]{details.Info.Path}[/]");
                var filesChanged = repoNode.AddNode($"[red3_1]Files Changed: {files.Count}[/]");
                var conflicts = repoNode.AddNode($"[deeppink3]Conflicts: {details.Index.Conflicts.Count()}[/]");
                var localBranch = repoNode.AddNode($"[deeppink3_1]Local Branch: {RepoService.GetLocalBranchName(repository)}[/]");
                var trackedBranch = repoNode.AddNode($"[deeppink3_1]Tracking Branch: {RepoService.GetTrackedBranch(repository, RepoService.GetLocalBranchName(repository))}[/]");
                var branches = repoNode.AddNode($"[magenta3_1]Branches: {details.Branches.Count()}[/]");
                var stashes = repoNode.AddNode($"[magenta2]Stashes: {details.Stashes.Count()}[/]");
                var head = repoNode.AddNode($"[hotpink2]Head: {details.Head.RemoteName}[/]");

                if (verbose)
                {
                    head.AddNode($"[hotpink2]Canonica lName: {details.Head.CanonicalName}[/]");
                    head.AddNode($"[hotpink2]Friendly Name: {details.Head.FriendlyName}[/]");
                    head.AddNode($"[hotpink2]Is Remote: {details.Head.IsRemote}[/]");
                    
                    foreach (var detailsBranch in details.Branches)
                    {
                        branches.AddNode($"[grey78]Name: {detailsBranch.FriendlyName}[/]");
                        branches.AddNode($"[grey82]Is Remote: {detailsBranch.IsRemote}[/]");
                        branches.AddNode($"[grey85]Is CurrentRepository Head: {detailsBranch.IsCurrentRepositoryHead}[/]");
                        branches.AddNode($"[grey89]Tip: {detailsBranch.Tip}[/]");
                    }
                    
                    foreach (var detailsStash in details.Stashes)
                    {
                        stashes.AddNode($"[yellow1]Message: {detailsStash.Message}[/]");
                        stashes.AddNode($"[lightgoldenrod1]Stasher: {detailsStash.Index.Author.Name}[/]");
                        stashes.AddNode($"[khaki1]Stashed When: {detailsStash.WorkTree.Author.When}[/]");
                        var notesNode = stashes.AddNode($"[wheat1]Notes:[/]");
                        foreach (var note in  detailsStash.Index.Notes)
                        {
                            notesNode.AddNode($"[cornsilk1]Note: {note.Message}[/]");
                        }
                    }
                    
                    foreach (var detailsConflict in details.Index.Conflicts)
                    {
                        conflicts.AddNode($"[deeppink3]Path: {detailsConflict.Ours.Path}[/]");
                        conflicts.AddNode($"[deeppink3]Ancestor Id: {detailsConflict.Ancestor.Id}[/]");
                        conflicts.AddNode($"[deeppink3]Our Id: {detailsConflict.Ours.Id}[/]");
                        conflicts.AddNode($"[deeppink3]Their Id: {detailsConflict.Theirs.Id}[/]");
                        conflicts.AddNode($"[deeppink3]Our Stage Number: {detailsConflict.Ours.StageLevel}[/]");
                    }
                    
                    foreach (var file in files)
                    {
                        filesChanged.AddNode($"[red3_1]File: {file}[/]");
                    }
                }
            }
            RepoService.GetRemoteBranches(_configManager.state.Repos, _configManager.state.RemoteBranchQueue);
            ctx.Status("Thinking some more");
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("green"));
        });

        // Render the tree
        AnsiConsole.Write(root);
    }

    private void ShowRepos()
    {
        if (_configManager.state.Repos.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Found the following repos:[/]");
            _configManager.state.Repos.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]"));
        }
        else
        {
            AnsiConsole.MarkupLine("[red]No repos found! You can specify the directory path with -dr <path>[/]\n");
        }
    }

    private void ShowCommit()
    {
        if (_configManager.state.CommitQueue.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Found the following commits:[/]");
            _configManager.state.CommitQueue.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]\n"));
        }
        else
        {
            AnsiConsole.MarkupLine("[red]No commits currently being tracked![/]\n");
        }
    }

    private void ShowBranches()
    {
        if (_configManager.state.BranchQueue.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Found the following branches:[/]");
            _configManager.state.BranchQueue.ToList().ForEach(x =>
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
        
        var repoPrompts = 
            Utils<string>.RepoSelectPrompt("[green]Select repos to create a new branch for.[/]\n", _configManager);

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
            _configManager.state.BranchQueue.TryAdd(repoPrompt, "");
        }
        
    }

    private void SelectState()
    {
        var repoPrompts = 
            Utils<string>.RepoSelectPrompt("[green]Select a repo to add to commit queue.[/]\n", _configManager);
        
        if (repoPrompts.Count <= 0)
        {
            AnsiConsole.MarkupLine("[red]No repos selected![/]\n");
            return;
        }
        
        AnsiConsole.MarkupLine("[green]Selected Repos:[/]");
        repoPrompts.ForEach(x => AnsiConsole.MarkupLine($"[blue]{x}[/]"));
        
        // Add selected repos to commit queue
        _configManager.state.CommitQueue.AddRange(repoPrompts);
    }

}