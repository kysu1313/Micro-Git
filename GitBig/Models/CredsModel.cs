namespace GitBig;

public class ConfigModel
{
    public string Username { get; set; }
    public string PersonalAccessToken { get; set; }
    public string Email { get; set; }
    public StateModel State { get; set; } = new StateModel();
    public bool SavedToFile { get; set; }
    public List<string> Directories { get; set; } = new();
}
