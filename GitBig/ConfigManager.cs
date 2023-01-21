using System.Collections;
using System.Text.Json;

namespace GitBig;

public class ConfigManager
{
    private readonly string _configFilePath = "./config.json";
    private readonly string configsFilePath = "./creds.json";
    private readonly string _pf = "microgit-q13";
    public ConfigModel configs;

    public ConfigManager()
    {
        configs = new ConfigModel();
        Load();
    }
    
    private void Load()
    {
        if (File.Exists(_configFilePath))
        {
            var configJson = File.ReadAllText(_configFilePath);
            configs = JsonSerializer.Deserialize<ConfigModel>(configJson);
        }
    }
    
    public string SetCreds(string username, String personalAccessToken, bool saveToFile = true)
    {
        configs.Username = StringCipher.Encrypt(username, _pf);
        configs.PersonalAccessToken = StringCipher.Encrypt(personalAccessToken, _pf);
        if (saveToFile)
        {
            configs.SavedToFile = true;
            Save();
        }
        return "Credentials saved";
    }
    
    public (string, string) GetCreds()
    {
        if (configs.SavedToFile)
        {
            return (StringCipher.Decrypt(configs.Username, _pf), 
                StringCipher.Decrypt(configs.PersonalAccessToken, _pf));
        }
        else
        {
            return (configs.Username, configs.PersonalAccessToken);
        }
    }
    
    public string GetUsername()
    {
        return GetCreds().Item1;
    }
    
    public string GetPersonalAccessToken()
    {
        return GetCreds().Item2;
    }
    
    public void Save()
    {
        try
        {
            var configJson = JsonSerializer.Serialize(configs);
            File.WriteAllText(_configFilePath, configJson);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void SetDirectory(string dir)
    {
        configs.Directories = new() { dir };
        Save();
    }

    public void AddDirectory(string dir)
    {
        configs.Directories.Add(dir);
        Save();
    }

    public void AddDirectories(List<string> dirs)
    {
        configs.Directories.AddRange(dirs);
        Save();
    }

    public void ClearDirectories(string dir)
    {
        configs.Directories.Clear();
        Save();
    }

    public List<string> GetDirectories()
    {
        return configs.Directories;
    }
}