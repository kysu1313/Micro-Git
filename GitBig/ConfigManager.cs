using System.Text.Json;

namespace GitBig;

public class ConfigManager
{
    private readonly string _configFilePath = "./config.json";
    private readonly string _credsFilePath = "./creds.json";
    private readonly string _pf = "microgit-q13";
    private CredsModel _creds;

    public ConfigManager()
    {
        _creds = new CredsModel();
        Load();
    }
    
    private void Load()
    {
        if (File.Exists(_configFilePath))
        {
            var configJson = File.ReadAllText(_configFilePath);
            _creds = JsonSerializer.Deserialize<CredsModel>(configJson);
        }
    }
    
    public string SetCreds(string username, String personalAccessToken, bool saveToFile = true)
    {
        _creds.Username = StringCipher.Encrypt(username, _pf);
        _creds.PersonalAccessToken = StringCipher.Encrypt(personalAccessToken, _pf);
        if (saveToFile)
        {
            _creds.SavedToFile = true;
            Save();
        }
        return "Credentials saved";
    }
    
    public (string, string) GetCreds()
    {
        if (_creds.SavedToFile)
        {
            return (StringCipher.Decrypt(_creds.Username, _pf), 
                StringCipher.Decrypt(_creds.PersonalAccessToken, _pf));
        }
        else
        {
            return (_creds.Username, _creds.PersonalAccessToken);
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
            var configJson = JsonSerializer.Serialize(_creds);
            File.WriteAllText(_configFilePath, configJson);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}