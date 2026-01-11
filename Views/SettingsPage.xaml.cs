using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalAIAssistant.Services;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class SettingsPage : ContentPage
{

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    readonly ApiEnvironmentService _env;
    
    async void OnEnvChanged(object sender, EventArgs e)
    {
        var selected = (string)EnvPicker.SelectedItem;
        var env      = Enum.Parse<ApiEnvironment>(selected);
        
        await _env.SetAsync(env);

        await DisplayAlert("Environment Switched",
                           $"Now using: {env}\n\nRestart app to apply.",
                           "OK");
    }

}