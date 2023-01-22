namespace MicroGit;

public class StateModel
{
    public ConfigModel Config { get; set; }
    public CredentialsModel Credentials { get; set; } = new();
    public List<string> Repos { get; set; } = new();
    public List<string> CommitQueue { get; set; } = new();
    public Dictionary<string, List<string>> RemoteBranchQueue { get; set; } = new();
    public Dictionary<string, string> BranchQueue { get; set; } = new();
    public List<string> Directories { get; set; } = new();
    public List<string> CurrentDirectories { get; set; } = new();
}