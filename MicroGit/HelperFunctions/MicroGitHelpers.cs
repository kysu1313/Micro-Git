using LibGit2Sharp;
using Spectre.Console;

namespace MicroGit.HelperFunctions;

public static class MicroGitHelpers
{

    public static bool IsVerbose(IReadOnlyList<string> args)
    {
        foreach (var arg in args)
        {
            if (arg.Trim().Contains("-v") || arg.Trim().Contains("-verbose"))
                return true;
        }
        return false;
    }
    
    public static string SetUpstreamLogic(string repoPath, string repoName, string createdBranchName, string createNewRemote,
        ConfigManager _configManager, ref bool usePreviousBranchName)
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

                    if (string.Equals(remoteBranchName, createNewRemote, StringComparison.OrdinalIgnoreCase))
                    {
                        remoteBranchName = CreateRemoteBranchBasedOn(repoPath, remotes, _configManager);
                    }

                    if (string.IsNullOrEmpty(createdBranchName))
                        createdBranchName = remoteBranchName;
                }
                else
                {
                    remoteBranchName = createdBranchName;
                    
                    // Check if the remote branch exists
                    var remoteBranchExists = RepoService.CheckIfRemoteBranchExists(repo, remoteBranchName);
                    CreateBranchIfNotExists(repoPath, remoteBranchExists, remoteBranchName, repo, _configManager);
                }
                
                

                // Get the upstream branch
                var localBranch = RepoService.GetLocalBranch(repo);
                var trackedBranch = localBranch.TrackedBranch;

                if (trackedBranch == null)
                {
                    var upstreamBranchName = repo.Head.CanonicalName;
                    if (upstreamBranchName == null)
                    {
                        upstreamBranchName = CreateRemoteBranchBasedOn(repoPath, remotes, _configManager);
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
    
    
    private static void CreateBranchIfNotExists(string repoPath, bool remoteBranchExists, string remoteBranchName, 
        Repository repo, ConfigManager _configManager)
    {
        if (!remoteBranchExists)
        {
            var createRemoteBranch = Utils<string>.CustomPrompt(
                $"The remote branch {remoteBranchName} does not exist. Do you want to create it now?",
                "Set upstream", new() { "Yes", "No" });

            if (string.Equals(createRemoteBranch, "Yes", StringComparison.OrdinalIgnoreCase))
            {
                var remoteTrackingBranches = RepoService.GetRemoteTrackingBranches(repo);
                var createFromBranch = Utils<string>.CustomPrompt(
                    $"Please select the branch you want to create {remoteBranchName} from:",
                    "Select branch", remoteTrackingBranches.Select(x => x.RemoteName).ToList());
                RepoService.CreateRemoteBranch(remoteBranchName, createFromBranch, repoPath, _configManager);
                AnsiConsole.MarkupLine($"[green]Created remote branch {remoteBranchName}[/]");
            }
        }
    }

    private static string CreateRemoteBranchBasedOn(string repoPath, List<string> remotes, ConfigManager _configManager)
    {
        string remoteBranchName = String.Empty;
        string basedOnBranch = String.Empty;
        bool isValidRemoteBranch = false;
        remoteBranchName = Utils<string>.GetInput("Enter the name of the new remote branch:");
        basedOnBranch = Utils<string>.CustomPrompt("Select the branch you want to base the new remote branch on:",
            "Select remote", remotes.ToList());
        var remoteBranch = RepoService.GetRemoteBranch(basedOnBranch, repoPath);
        if (remoteBranch.IsTracking)
        {
            // isValidRemoteBranch
        }
        
        
        remoteBranchName =
            RepoService.CreateRemoteBranch(remoteBranchName, basedOnBranch, repoPath, _configManager);
        return remoteBranchName;
    }
    
}