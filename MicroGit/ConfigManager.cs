using System.Collections;
using System.Text.Json;
using LibGit2Sharp;

namespace MicroGit;

public class ConfigManager
{
    private readonly string _configFilePath = "./config.json";
    private readonly string _pf = "microgit-q13";
    public StateModel state;

    public ConfigManager()
    {
        Load();
    }
    
    private void Load()
    {
        if (File.Exists(_configFilePath))
        {
            var configJson = File.ReadAllText(_configFilePath);
            state = JsonSerializer.Deserialize<StateModel>(configJson);
        }
        
        if (state == null)
        {
            state = new StateModel();
        }
    }
    
    public void Save()
    {
        try
        {
            var configJson = JsonSerializer.Serialize(state);
            File.WriteAllText(_configFilePath, configJson);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    public string SetCreds(string username, String personalAccessToken, bool saveToFile = true)
    {
        if (state.Credentials == null)
        {
            state.Credentials = new CredentialsModel();
        }
        state.Credentials.Username = StringCipher.Encrypt(username, _pf);
        state.Credentials.PersonalAccessToken = StringCipher.Encrypt(personalAccessToken, _pf);
        if (saveToFile)
        {
            state.Credentials.SavedToFile = true;
            Save();
        }
        return "Credentials saved";
    }
    
    public (string?, string?) GetCreds()
    {
        if (state.Credentials?.SavedToFile ?? false)
        {
            return (StringCipher.Decrypt(state.Credentials.Username, _pf), 
                StringCipher.Decrypt(state.Credentials.PersonalAccessToken, _pf));
        }
        
        return (state.Credentials?.Username, state.Credentials?.PersonalAccessToken);
    }

    public void SetRemoteType(RemoteTypes type)
    {
        state.Credentials.RemoteHost = type.ToString();
        Save();
    }
    
    public int GetDirCount()
    {
        return state?.Directories?.Count ?? 0;
    }

    public void DeleteSavedCredentials()
    {
        state.Credentials.Email = "";
        state.Credentials.Username = "";
        state.Credentials.Password = "";
        state.Credentials.PersonalAccessToken = "";
        state.Credentials.RemoteHost = "";
    }

    public (string, string, string, string, string) GetSavedCredentials()
    {
        return (
        state.Credentials.Email,
        state.Credentials.Username,
        state.Credentials.Password,
        state.Credentials.PersonalAccessToken,
        state.Credentials.RemoteHost);
    }                                     
    
    public string GetUsername()
    {
        return GetCreds().Item1;
    }
    
    public string GetPersonalAccessToken()
    {
        return GetCreds().Item2;
    }

    public void AddRepo(string repo)
    {
        state.Repos.Add(repo);
    }

    public void SetDirectory(string dir)
    {
        state.Directories = new() { dir };
        Save();
    }

    public void AddDirectory(string dir)
    {
        if (!state.Directories.Contains(dir))
            state.Directories.Add(dir);
        Save();
    }

    public void AddDirectories(List<string> dirs)
    {
        foreach (var dir in dirs)
        {
            if (!state.Directories.Contains(dir))
                state.Directories.Add(dir);
        }
        Save();
    }

    public void ClearDirectories(string dir)
    {
        state.Directories.Clear();
        Save();
    }

    public List<string> GetDirectories()
    {
        return state.Directories;
    }

    public void SetCurrentDirectory(List<string> dirs)
    {
        state.CurrentDirectories = dirs;
        Save();
    }

    public void SetMergeOptions(MergeOptions mergeOptions)
    {
        state.MergeOptions = mergeOptions;
        Save();
    }
    
    public MergeOptions? GetMergeOptions()
    {
        return state.MergeOptions;
    }
}