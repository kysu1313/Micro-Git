namespace MicroGit;

public class CredentialsModel
{
    public string Username { get; set; }
    public string PersonalAccessToken { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public string RemoteHost { get; set; }
    public bool SavedToFile { get; set; }
}

public enum RemoteTypes
{
    GitHub,
    GitLab,
    TFS
}
