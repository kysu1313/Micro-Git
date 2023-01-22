using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using MicroGit.HelperFunctions;
using Spectre.Console;
using Tree = Spectre.Console.Tree;

namespace MicroGit;

/*
 * LibGit2Sharp Test Cases:
 * https://github.com/libgit2/libgit2sharp/blob/a4c6c4552b24590995c221304e7d7c75c181ea82/LibGit2Sharp.Tests/BranchFixture.cs#L574-L603
 */

public class BigService
{
    private Dictionary<string, string> _commonCommands = new()
    {
        { "", "" },
        { "Frequently Used Commands", "-------------------------------------------------------------------" },
        { "[palegreen1_1]-cr, --creds[/]", "[palegreen1_1]Set your GIT login credentials! (Required to push)[/]" },
        { "[deeppink3]-c <message>, --commit <message>[/]", "[deeppink3]Commit the selected repos (stages ALL changes) with <message> commit message #.[/]" },
        { "[paleturquoise1]-d, --details[/]", "[paleturquoise1]Show details of the repo #.[/]" },
        { "[lightsalmon3_1]-bc, --branch-checkout[/]", "[lightsalmon3_1]Create new branch and check it out for selected repos #.[/]" },
        { "[magenta2]-th, --token-help[/]", "[magenta2]Show instructions on how to create a Personal Access Token for GitHub #.[/]" },
        { "[purple]-dr, --dir <path>[/]", "[purple]Set the directory to search from #.[/]" },
        { "[deeppink3]-da, --dir-add <path>[/]", "[deeppink3]Add additional directories to be searched #.[/]" },
        { "[red]-e, --exit <#>[/]", "[red]Exit[/]" },
    };
    private Dictionary<string, string> _commands = new () 
    {
        { "", "" },
        { "Additional Commands", "-------------------------------------------------------------------" },
        { "[deeppink3]-b, --branch[/]", "[deeppink3]Create new branch for selected repos #.[/]" },
        { "[green]-f, --find[/]", "[green]Find all GIT repos in current folder.[/]" },
        { "[deeppink1_1]-dl, --dir-list <path>[/]", "[deeppink1_1]List saved directories #.[/]" },
        { "[gold3]-sv, --saved[/]", "[gold3]Show saved info #.[/]" },
        { "[mistyrose1]-sd, --show-diff[/]", "[mistyrose1]Show diff against <branch name> or remote if <branch name> not specified #.[/]" },
        { "[deeppink4_2]-dv, --details-verbose[/]", "[deeppink4_2]Show verbose details of the repo #.[/]" },
        { "[yellow4_1]-sr, --show-repo-queue[/]", "[yellow4_1]Show found repos #.[/]" },
        { "[sandybrown]-sc, --show-commit-queue[/]", "[sandybrown]Show current commit queue #.[/]" },
        { "[deeppink1_1]-sb, --show-branch-queue[/]", "[deeppink1_1]Show current branch queue #.[/]" },
        { "[gold3]-se, --select[/]", "[gold3]Select repos to commit #.[/]" },
        { "[lightsalmon3_1]-su, --set-upstream[/]", "[lightsalmon3_1]Set the upstream branch for selected repos#.[/]" },
        { "[deeppink3]-bcsp, --branch-checkout-stage-push[/]", "[deeppink3]Create new branch for selected repos, checkout branch, stage all and push #.[/]" },
        { "[deeppink4_2]-dc, --delete-credentials[/]", "[deeppink4_2]Delete saved credentials #.[/]" },
        { "[yellow]-h, --help[/]", "[yellow]Show command table #.[/]" },
        { "[red]-e, --exit[/]", "[red]Exit (Also ctrl + c)[/]" },
    };
    // private StateModel _configManager.state;
    private List<string> _dir;
    private ConfigManager _configManager;

    public BigService()
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
        _dir = new() { "C:\\Users\\ksups\\PROGRAMS\\JS" };//Directory.GetCurrentDirectory() };
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
            case "--creds":
            case "-cr":
                GetCredentials();
                break;
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
                ShowDiff();
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

    private void SetUpstreamBranches()
    {
        try
        {
            var createNewRemote = "Create new remote";
            var usePreviousBranchName = false;
            var createdBranchName = string.Empty;
            var selectedRepos = Utils<string>.RepoSelectPrompt("Select repos you want to set upstream branches for:", _configManager);

            var root = new Tree("Diffs");
            foreach (var repoPath in selectedRepos)
            {
                var repoName = repoPath.Split('\\').Last();
                var repoNode = root.AddNode($"[yellow]{repoName}[/]");

                createdBranchName = SetUpstreamLogic(repoPath, repoName, createdBranchName, createNewRemote, 
                    ref usePreviousBranchName);
            }
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
    }

    private string SetUpstreamLogic(string repoPath, string repoName, string createdBranchName, string createNewRemote,
        ref bool usePreviousBranchName)
    {
        var remoteBranchName = string.Empty;
        using (var repo = new Repository(repoPath))
        {
            // var remotes = repo.Network.Remotes;
            var allRemoteBranches = RepoService.GetAllRemoteBranches(repo, _configManager);
            var remotes = allRemoteBranches.Select(elem => elem.CanonicalName
                    .Replace("refs/heads/", "")).ToList();
            if (!remotes.Any())
            {
                AnsiConsole.MarkupLine($"[red]No remotes for {repoName}.[/]");
                var shouldFetch = Utils<string>.CustomPrompt("Would you like to try to set upstream to origin/main?",
                    "Set upstream", new() { "Yes", "No" });
                if (string.Equals(shouldFetch, "Yes"))
                {
                    AnsiConsole.MarkupLine("What is the remote url?");
                    AnsiConsole.Markup("url >> ");
                    var remoteUrl = Console.ReadLine();
                    var remote = repo.Network.Remotes.Add("origin", remoteUrl ?? "");
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(createdBranchName))
                {
                    var selction = Utils<string>.CustomPrompt($"Do you want to re-use the branch name {createdBranchName}?:",
                        "Set upstream", new() { "Yes", "No" });
                    usePreviousBranchName = string.Equals(selction, "Yes", StringComparison.OrdinalIgnoreCase);
                }

                if (!usePreviousBranchName)
                {
                    // Get the remote branch name (or create a new one)
                    var options = remotes.ToList();
                    options.Add(createNewRemote);

                    remoteBranchName = RepoService.GetRemoteBranchName(repoName, remoteBranchName, options, repo);

                    // if (string.Equals(remoteBranchName, createNewRemote, StringComparison.OrdinalIgnoreCase))
                    // {
                        remoteBranchName = CreateRemoteBranchBasedOn(repoPath, remotes);
                    // }

                    if (string.IsNullOrEmpty(createdBranchName))
                        createdBranchName = remoteBranchName;
                }
                else
                {
                    remoteBranchName = createdBranchName;
                }

                // Get the upstream branch
                var localBranch = RepoService.GetLocalBranch(repo);
                var trackedBranch = localBranch.TrackedBranch;

                if (trackedBranch == null)
                {
                    var upstreamBranchName = repo.Head.CanonicalName;
                    if (upstreamBranchName == null)
                    {
                        upstreamBranchName = CreateRemoteBranchBasedOn(repoPath, remotes);
                    }
                    repo.Branches.Update(repo.Head, updater =>
                    {
                        updater.Remote = repo.Network.Remotes[remoteBranchName].Name;
                        updater.UpstreamBranch = upstreamBranchName;
                    });
                    localBranch = RepoService.GetLocalBranch(repo);
                }

                repo.Branches.Update(localBranch, b =>
                    b.TrackedBranch = localBranch.TrackedBranch.CanonicalName);
            }
        }

        return createdBranchName;
    }

    private string CreateRemoteBranchBasedOn(string repoPath, List<string> remotes)
    {
        string selectNewBranch = "Select a new branch";
        var setTrackingBranch = "Set a tracking branch";
        string remoteBranchName = String.Empty;
        string basedOnBranch = String.Empty;
        bool isValidRemoteBranch = false;
        while (!isValidRemoteBranch)
        {
            
            
            /*
             * TODO:
             * 1. Get the remote branch name
             * 2. Get the branch to base the new remote branch on
             * 3. Create the new remote branch
             * 4. Set the upstream branch
             * 5. Set the tracking branch
             * 6. Push the new remote branch
             */
            
            
            remoteBranchName = Utils<string>.GetInput("Enter the name of the new remote branch:");
            basedOnBranch = Utils<string>.CustomPrompt("Select the branch you want to base the new remote branch on:",
                "Select remote", remotes.ToList());
            var remoteBranch = RepoService.GetRemoteBranch(basedOnBranch, repoPath);
            if (remoteBranch != null && remoteBranch.IsTracking)
            {
                break;
            }
            AnsiConsole.MarkupLine($"[red]The branch {basedOnBranch} is not tracking a branch.[/]");
            var selection = Utils<string>.CustomPrompt("Would you like to:",
                "Select remote", new() { selectNewBranch, setTrackingBranch });

            // switch (selection)
            // {
            //     case "Select a new branch":
            //         break;
            //     case "Set a tracking branch":
            //         var trackingBranch = Utils<string>.CustomPrompt("Select the branch you want to track:",
            //             "Select remote", remotes.ToList());
            //         RepoService.SetTrackingBranch(trackingBranch, repoPath);
            //         break;
            // }
        }
        
        remoteBranchName =
            RepoService.CreateRemoteBranch(remoteBranchName, basedOnBranch, repoPath, _configManager);
        return remoteBranchName;
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

        if (string.Equals(remoteHost, RemoteTypes.TFS.ToString()))
        {
            AnsiConsole.Markup("[green]I'll use your system credentials to authenticate![/]\n");
            _configManager.SetRemoteType(RemoteTypes.TFS);
            return;
        }

        _configManager.SetRemoteType(remoteHost);
        
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
        AnsiConsole.Markup($"[greenyellow]Directory set to {_dir}[/]\n");
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
            if (string.IsNullOrEmpty(_configManager.GetUsername()) || 
                string.IsNullOrEmpty(_configManager.GetPersonalAccessToken()))
            {
                AnsiConsole.Markup("[red]No credentials set. Use --creds or -cr to set credentials[/]\n");
                return;
            }
        
            if (_configManager.state.CommitQueue.Count == 0)
            {
                SelectState();
            }
        
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

    private void ShowDiff()
    {
        // Show diff of selected repos compared to remote

        var selectedRepos = Utils<string>.RepoSelectPrompt("Select repos to show diff for", _configManager);

        var root = new Tree("Diffs");
        foreach (var repoPath in selectedRepos)
        {
            var repoName = repoPath.Split('\\').Last();
            var repoNode = root.AddNode($"[yellow]{repoName}[/]");
            
            using (var repo = new Repository(repoPath))
            {
                var innerTable = new Table()
                    .RoundedBorder();
                innerTable.AddColumn("Details");
                 
                // Get the current branch
                Branch currentBranch = repo.Head;

                // Get the upstream branch
                var upstream = currentBranch.TrackedBranch;

                if (upstream == null)
                {
                    innerTable.AddRow("[red]No upstream branch[/]");
                    repoNode.AddNode(innerTable);
                    continue;
                }

                // Get the diff between the current branch and the upstream branch
                var diff = repo.Diff.Compare<Patch>(currentBranch.Tip.Tree, upstream.Tip.Tree);
                
                if (diff.Count() == 0)
                {
                    innerTable.AddRow($"[red]{repoPath}[/] [yellow]has no changes to commit[/]\n");
                    repoNode.AddNode(innerTable);
                    continue;
                }
                
                // Iterate through the diffs and print the file names
                foreach (var pec in diff)
                {
                    innerTable.AddRow($"[red]{pec.Path} = {pec.LinesAdded + pec.LinesDeleted} ({pec.LinesAdded}+ and {pec.LinesDeleted}-)[/]");
                }
                
                repoNode.AddNode(innerTable);
            }

            // var innerTable = new Table()
            //     .RoundedBorder()
            //     .AddColumn("Files");
            //
            // foreach (var pec in patch)
            // {
            //     innerTable.AddRow($"[red]{pec.Path} = {pec.LinesAdded + pec.LinesDeleted} ({pec.LinesAdded}+ and {pec.LinesDeleted}-)[/]");
            // }
            //
            // repoNode.AddNode(innerTable);
            
        }
        
        AnsiConsole.Write(root);
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
                // _configManager.state.Repos = RepoService.GetReposInCurrentDir(dir);
                RepoService.GetRemoteBranches(_configManager.state.Repos, _configManager.state.RemoteBranchQueue);
                ctx.Status("Thinking some more");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
            });
        // _configManager.state.Repos = RepoService.GetReposInCurrentDir(dir);
        
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
        
        // Create the tree
        var root = new Tree("Repository Details");
        
        var colors = new [] { "red", "palegreen1_1", "darkorange3_1", "gold3_1", "deeppink1_1", "lightsalmon1", "paleturquoise1" };
        var random = new Random();

        foreach (var repo in _configManager.state.Repos)
        {
            
            using var repository = new Repository(repo);
            var status = repository.RetrieveStatus();
            var files = status.Modified.Select(mods => mods.FilePath).ToList();
            
            var details = RepoService.GetRepoDetails(repo);
            // get random color
            var color = colors[random.Next(0, colors.Length)];
            var repoNode = root.AddNode($"[{color}]{details.Info.Path}[/]");
            var filesChanged = repoNode.AddNode($"[red3_1]Files Changed: {files.Count}[/]");
            var conflicts = repoNode.AddNode($"[deeppink3]Conflicts: {details.Index.Conflicts.Count()}[/]");
            var localBranch = repoNode.AddNode($"[deeppink3_1]Local Branch: {RepoService.GetLocalBranchName(repository)}[/]");
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