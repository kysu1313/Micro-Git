namespace GitBig;

public class StateModel
{
    public List<string> repos { get; set; } = new();
    public List<string> commitQueue { get; set; } = new();
    public Dictionary<string, List<string>> remoteBranchQueue { get; set; } = new();
    public Dictionary<string, string> branchQueue { get; set; } = new();
}