using System.Diagnostics;
using LibGit2Sharp.Handlers;
using MicroGit.HelperFunctions;
using Spectre.Console;

namespace MicroGit;
using LibGit2Sharp;

public class RepoService
{
    
    public static List<string> GetReposInCurrentDir(string path)
    {
        var dirs = GetDirsInCurrentDir(path);
        var repos = new List<string>();
        foreach (var dir in dirs)
        {
            if (Repository.IsValid(dir))
            {
                repos.Add(dir);
            }
        }
        return repos;
    }
    
    public static void GetRemoteBranches(List<string> _repos, Dictionary<string, List<string>> _RemoteBranchQueue)
    {
        foreach (var repo in _repos)
        {
            using var repository = new Repository(repo);
            var branches = Repository.ListRemoteReferences(repo)
                .Where(elem => elem.IsLocalBranch).ToList();
            var lst = new List<string>();
            foreach (var branch in branches)
            {
                lst.Add(branch.CanonicalName);
                // AnsiConsole.Markup($"[green]{branch.CanonicalName.Replace("refs/heads/", "")}[/] - [blue]Is remote tracking: {branch.IsRemoteTrackingBranch}[/]\n");
            }
            _RemoteBranchQueue.TryAdd(repo, lst);
        }
    }

    public static void PushChanges(Repository repo, string commitMessage, ConfigManager _config)
    {
        try {
            var pushOptions = GetPushOptions(_config);
            
            // var options = new PushOptions() 
            // {CredentialsProvider = (url, user, cred) => 
            //     new DefaultCredentials()}; // This is for Active Directory ???????

            var localBranch = GetLocalBranch(repo);
            var trackedBranch = localBranch.TrackedBranch;

            if (trackedBranch == null)
            {
                repo.Branches.Update(repo.Head, updater =>
                {
                    updater.Remote = repo.Network.Remotes["origin"].Name;
                    updater.UpstreamBranch = repo.Head.CanonicalName;
                });
                localBranch = GetLocalBranch(repo);
            }

            repo.Branches.Update(localBranch, b => 
                b.TrackedBranch = localBranch.TrackedBranch.CanonicalName );

            var pushRefSpec = @"refs/heads/main";
            var authorName = repo.Config.Get<string>("user.name").Value;
            var author = new Signature(authorName, "na", DateTimeOffset.Now);
            repo.Commit(commitMessage, author, author);
            repo.Network.Push(localBranch, pushOptions); // YES IT FINALLY WORKS :D
            // repo.Network.Push(remote, pushRefSpec, pushOptions);
            
        }
        catch (Exception e) {
            if (e.Message.Contains("401")) {
                Console.WriteLine("Exception:RepoActions:PushChanges " + e.Message);
                AnsiConsole.MarkupLine($"[red]401 Unauthorized error, you can set login credentials with -cr / --creds[/]\n");
            }
            else {
                AnsiConsole.MarkupLine($"[red]Awww jeezz, something went horribly wrong :([/]");
                AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
            }
        }
    }

    public static string CreateRemoteBranch(string branchName, string basedOnBranchName, 
        string repoPath, ConfigManager config)
    {
        try
        {
            using var repository = new Repository(repoPath);
            var remote = repository.Network.Remotes["origin"];
            var basedOnRemoteBranch = repository.Branches[$"refs/remotes/origin/{basedOnBranchName}"];
            var localBranch = GetLocalBranch(repository);
            var remoteBranch = repository.Branches[$"refs/remotes/origin/{branchName}"];
            if (remoteBranch == null)
            {
                remoteBranch = repository.CreateBranch($"refs/remotes/origin/{branchName}", localBranch.Tip);
            }
            Commands.Checkout(repository, localBranch);
            repository.Branches.Update(localBranch, b => 
                b.TrackedBranch = remoteBranch.CanonicalName,
                b => b.Remote = basedOnRemoteBranch.RemoteName);
            
            repository.Network.Push(remoteBranch, GetPushOptions(config));
            return localBranch.UpstreamBranchCanonicalName;
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
        return "";
    }
    
    public static string GetRemoteBranchName(string repoName, string remoteBranchName, List<string> options, Repository repo)
    {
        bool validBranchName = false;
        while (!validBranchName)
        {
            remoteBranchName = Utils<string>.CustomPrompt(
                $"Select the remote branch you want to set upstream for {repoName}:",
                "Set upstream", options);
            if (!repo.Branches.Any(x => string.Equals(x.RemoteName, remoteBranchName)))
            {
                validBranchName = true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Branch {remoteBranchName} already exists[/]");
            }
        }

        return remoteBranchName;
    }

    public static Branch? GetRemoteBranch(string repoPath, string branchName)
    {
        try
        {
            using var repository = new Repository(repoPath);
            var basedOnRemoteBranch = repository.Branches[$"refs/remotes/origin/{branchName}"];
            return basedOnRemoteBranch;
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
        return null;
    }

    private static PushOptions? GetPushOptions(ConfigManager config)
    {
        try
        {
            var pushOptions = new PushOptions()
            {
                CredentialsProvider = (url, user, cred) =>
                    new UsernamePasswordCredentials
                    {
                        Username = config.GetUsername(), 
                        Password = config.GetPersonalAccessToken()
                    }
            };
            return pushOptions;
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }

        return null;
    }

    private static FetchOptions? GetFetchOptions(ConfigManager config)
    {
        try
        {
            var fetchOptions = new FetchOptions()
            {
                CredentialsProvider = (url, user, cred) =>
                    new UsernamePasswordCredentials
                    {
                        Username = config.GetUsername(), 
                        Password = config.GetPersonalAccessToken()
                    }
            };
            return fetchOptions;
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }

        return null;
    }

    public static string[] GetDirsInCurrentDir(string path)
    {
        return Directory.GetDirectories(path, String.Empty, SearchOption.TopDirectoryOnly);
    }

    public static string GetLocalBranchName(Repository repo)
    {
        return repo.Branches.First(x => x.IsCurrentRepositoryHead).FriendlyName;
    }

    public static Branch GetLocalBranch(Repository repo)
    {
        return repo.Branches.First(x => x.IsCurrentRepositoryHead);
    }
    
    public static Repository GetRepoDetails(string path)
    {
        var repo = new Repository(path);
        return repo;
    }

    public static bool MakeCommit(string path, string message)
    {
        var repo = new Repository(path);
        // var author = new Signature(repo.Index., "
        // repo.Commit(message, new Signature("MicroGit", "
        return true;
    }

    public static List<Reference>? GetAllRemoteBranches(
        Repository repo, ConfigManager configManager, bool getTrackingOnly = false)
    {
        try
        {
            var refs = Repository.ListRemoteReferences(
                    repo.Network.Remotes.First().Url, 
                    (_, _, _) =>
                        new UsernamePasswordCredentials
                        {
                            Username = configManager.GetUsername(), 
                            Password = configManager.GetPersonalAccessToken()
                        })
                .Where(elem => elem.IsLocalBranch).ToList();
            return getTrackingOnly ? refs.Where(elem => elem.IsRemoteTrackingBranch).ToList() : refs;
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }

        return null;
    }

    public static bool CheckIfRemoteBranchExists(Repository repo, string remoteBranchName)
    {
        try
        {
            var remoteBranch = repo.Branches[remoteBranchName];
            return (remoteBranch != null && remoteBranch.IsTracking);
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }

        return false;
    }
    
    public static Branch? GetTrackedBranch(Repository repo, string branchName)
    {
        try
        {
            var branch = repo.Branches[branchName];
            if (branch != null)
            {
                return branch.TrackedBranch;
            }
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }

        return null;
    }
    
    public static List<Branch>? GetRemoteTrackingBranches(Repository repo)
    {
        try
        {
            return repo.Branches
                .Where(elem => elem.IsTracking && elem.IsRemote).ToList();
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }

        return null;
    }

    public static void FetchLogic(string repoPath, ConfigManager configManager)
    {
        try
        {
            using var repo = new Repository(repoPath);
            Commands.Fetch(repo, "origin", new string[0], GetFetchOptions(configManager), 
                "Fetching updates");
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
    }

    public static void PullLogic(string repoPath, MergeOptions mergeOptions, ConfigManager configManager)
    {
        try
        {
            using var repo = new Repository(repoPath);
            var options = new PullOptions
            {
                MergeOptions = mergeOptions
            };
            var signature = new Signature(new Identity("Your name", "Your email"), 
                DateTimeOffset.Now);
            var result = Commands.Pull(repo, signature, options);
        }
        catch (Exception e)
        {
            Utils<string>.WriteErrorIssue(e);
        }
    }
}

























