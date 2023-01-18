namespace GitBig;
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

    public static string[] GetDirsInCurrentDir(string path)
    {
        return Directory.GetDirectories(path, String.Empty, SearchOption.TopDirectoryOnly);
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
        // repo.Commit(message, new Signature("GitBig", "
        return true;
    }
}