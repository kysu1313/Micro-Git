namespace GitBig;

public class CredsModel
{
    public string Username { get; set; }
    public string PersonalAccessToken { get; set; }
    public string Email { get; set; }
    public bool SavedToFile { get; set; }
    public List<string> Directories { get; set; } = new();
}
