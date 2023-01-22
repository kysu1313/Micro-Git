﻿using System.Diagnostics;
using LibGit2Sharp.Handlers;
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
            var remote = repo.Network.Remotes["origin"];
            var uname = _config.GetUsername();
            var pass = _config.GetPersonalAccessToken();
            var pushOptions = new PushOptions()
            {
                CredentialsProvider = (url, user, cred) =>
                    new UsernamePasswordCredentials { Username = uname, Password = pass }
            };
            
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
    
    public void SetUpstreamBranch(){}

    private void PushViaCmd()
    {
        Process cmd = new Process();
        cmd.StartInfo.FileName = "cmd.exe";
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();

        cmd.StandardInput.WriteLine("echo Oscar");
        cmd.StandardInput.Flush();
        cmd.StandardInput.Close();
        cmd.WaitForExit();
        Console.WriteLine(cmd.StandardOutput.ReadToEnd());
    }
    
    public static void FetchRemoteBranches(Dictionary<string, List<string>> _RemoteBranchQueue)
    {
        foreach (var repo in _RemoteBranchQueue)
        {
            using var repository = new Repository(repo.Key);
            foreach (var branch in repo.Value)
            {
                var remoteBranch = repository.Branches[branch];
                var localBranch = repository.Branches[branch.Replace("refs/remotes/origin/", "")];
                if (localBranch == null)
                {
                    localBranch = repository.CreateBranch(branch.Replace("refs/remotes/origin/", ""), remoteBranch.Tip);
                }
                Commands.Checkout(repository, localBranch);
                repository.Reset(ResetMode.Hard, remoteBranch.Tip);
            }
        }
    }
    
    public static void PushRemoteBranches(Dictionary<string, List<string>> _RemoteBranchQueue)
    {
        foreach (var repo in _RemoteBranchQueue)
        {
            using var repository = new Repository(repo.Key);
            var details = GetRepoDetails(repo.Key);
            Remote remote = repository.Network.Remotes["origin"];

            // The local branch "b1" will track a branch also named "b1"
            // in the repository pointed at by "origin"

            repository.Branches.Update(repository.Head,
                b => b.Remote = remote.Name,
                b => b.UpstreamBranch = repository.Head.CanonicalName);
            
            
            // Thus Push will know where to push this branch (eg. the remote)
            // and which branch it should target in the target repository

            repository.Network.Push(repository.Head, new PushOptions
            {
                // CredentialsProvider = (url, usernameFromUrl, types) =>
                //     new UsernamePasswordCredentials
                //     {
                //         Username = details.Username,
                //         Password = details.Password
                //     }
            });

            // Do some stuff
            // ....

            // One can call Push() again without having to configure the branch
            // as everything has already been persisted in the repository config file
            // repository.Network.Push(localBranch, pushOptions);
        }
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
}