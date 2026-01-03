using System.Text.Json;
using LocalAIAssistant.Data;
using LocalAIAssistant.Data.Models;
using LocalAIAssistant.Services.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIAssistant.Services;

public class OllamaConfigService
{

    private readonly string _configPath = Path.Combine(FileSystem.AppDataDirectory
                                                     , StringConsts.OllamaConfigJsonFileName);

    private          OllamaConfig    _config;
    private readonly ILoggingService _logger;
    
    public event Action<OllamaConfig>? ConfigChanged;
    
    public OllamaConfig Current
    {
        get { return _config; }
    }

    public OllamaConfigService(ILoggingService logger)
    {
        _logger = logger;
        
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            _config = JsonSerializer.Deserialize<OllamaConfig>(json) ?? new OllamaConfig { Model = "llama3" };
        }
        else
        {
            _config = new OllamaConfig { Model = "llama3" };
            SaveConfig();
        }
    }

    public OllamaConfig GetConfig()
    {
        return _config;
    }

    public void SetModel(string model)
    {
        _config.Model = model;
        SaveConfig();
        
        var handler = ConfigChanged;
        handler?.Invoke(_config);
    }

    private void SaveConfig()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configPath, json);
    }

    public void UpdateConfig(OllamaConfig? newConfig)
    {
        if (newConfig == null)
        {
            _config = new OllamaConfig( );
        }
        else
        {
            _config = new OllamaConfig
                      {
                          Model       = newConfig.Model
                        , NumPredict  = newConfig.NumPredict
                        , Temperature = newConfig.Temperature
                        , Host        = newConfig.Host
                      };        
        }
        
        SaveConfig();
        ConfigChanged?.Invoke(_config);
        
        _logger.LogInformation($"Config changed to {_config}");
    }

}
