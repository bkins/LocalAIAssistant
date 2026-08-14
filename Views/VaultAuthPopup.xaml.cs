using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using LocalAIAssistant.CognitivePlatform.CpClients.CognitivePlatform;
using LocalAIAssistant.Services;
using Microsoft.Maui.Storage;

namespace LocalAIAssistant.Views;

public partial class VaultAuthPopup : Popup<bool>
{
    private readonly bool                _isSetup;
    private readonly IBiometricService   _biometricService;
    private readonly CognitivePlatformClientBase _apiClient;

    private string _currentPin = string.Empty;
    private string _tempPin    = string.Empty;
    private bool   _confirming = false;

    public VaultAuthPopup(bool isSetupRequired)
    {
        InitializeComponent();

        // Resolve dependencies manually from App Services
        _biometricService = IPlatformApplication.Current?.Services.GetService<IBiometricService>() 
            ?? new DummyBiometricService();
        
        var clientFactory = IPlatformApplication.Current?.Services.GetService<ICognitivePlatformClientFactory>();
        _apiClient = clientFactory?.Create() ?? throw new InvalidOperationException("API client factory not registered.");

        _isSetup = isSetupRequired;

        InitializeUI();
    }

    private void InitializeUI()
    {
        if (_isSetup)
        {
            TitleLabel.Text        = "Setup Secrets Vault";
            DescriptionLabel.Text  = "Choose a secure 4-digit PIN.";
            BioOrSubmitButton.Text = "➔";
        }
        else
        {
            TitleLabel.Text        = "Secrets Vault Locked";
            DescriptionLabel.Text  = "Enter your secure PIN to unlock.";
            BioOrSubmitButton.Text = "🔑";
            
            // Check biometric availability
            CheckBiometricsOnLoad();
        }
        UpdatePinDisplay();
    }

    private async void CheckBiometricsOnLoad()
    {
        var hasSavedPin = !string.IsNullOrEmpty(await SecureStorage.Default.GetAsync("vault_pin"));
        var bioAvailable = await _biometricService.IsAvailableAsync();

        if (bioAvailable && hasSavedPin)
        {
            // Trigger biometrics automatically for seamless user experience
            await TriggerBiometricAuth();
        }
    }

    private void UpdatePinDisplay()
    {
        PinDisplayLabel.Text = new string('●', _currentPin.Length).PadRight(4, '○');
    }

    private async void OnKeypadClicked(object sender, EventArgs e)
    {
        if (sender is Button button && _currentPin.Length < 4)
        {
            _currentPin += button.Text;
            UpdatePinDisplay();
            ErrorLabel.IsVisible = false;

            if (_currentPin.Length == 4)
            {
                if (!_isSetup)
                {
                    // Auto-submit in unlock mode
                    await ProcessUnlockAsync();
                }
            }
        }
    }

    private void OnBackspaceClicked(object sender, EventArgs e)
    {
        if (_currentPin.Length > 0)
        {
            _currentPin = _currentPin[..^1];
            UpdatePinDisplay();
            ErrorLabel.IsVisible = false;
        }
    }

    private async void OnBioOrSubmitClicked(object sender, EventArgs e)
    {
        if (_isSetup)
        {
            if (_currentPin.Length < 4)
            {
                ShowError("PIN must be 4 digits.");
                return;
            }

            if (!_confirming)
            {
                _tempPin = _currentPin;
                _currentPin = string.Empty;
                _confirming = true;
                TitleLabel.Text = "Confirm Vault PIN";
                DescriptionLabel.Text = "Please re-enter your PIN to confirm.";
                UpdatePinDisplay();
            }
            else
            {
                if (_currentPin == _tempPin)
                {
                    await ProcessSetupAsync(_currentPin);
                }
                else
                {
                    ShowError("PINs did not match. Restarting setup.");
                    _currentPin = string.Empty;
                    _tempPin = string.Empty;
                    _confirming = false;
                    InitializeUI();
                }
            }
        }
        else
        {
            // Unlock mode - trigger biometric auth
            await TriggerBiometricAuth();
        }
    }

    private async Task TriggerBiometricAuth()
    {
        var bioAvailable = await _biometricService.IsAvailableAsync();
        if (!bioAvailable)
        {
            ShowError("Biometric login is not available on this device.");
            return;
        }

        var savedPin = await SecureStorage.Default.GetAsync("vault_pin");
        if (string.IsNullOrEmpty(savedPin))
        {
            ShowError("Please unlock with PIN manually first to enable biometrics.");
            return;
        }

        var success = await _biometricService.AuthenticateAsync("Access Secrets Vault");
        if (success)
        {
            var unlockSuccess = await _apiClient.UnlockVaultAsync(savedPin);
            if (unlockSuccess)
            {
                await this.CloseAsync(true);
            }
            else
            {
                ShowError("Biometric unlock failed. Please use PIN.");
            }
        }
    }

    private async Task ProcessUnlockAsync()
    {
        var success = await _apiClient.UnlockVaultAsync(_currentPin);
        if (success)
        {
            // Save PIN to enable biometric unlock next time if biometrics are available
            if (await _biometricService.IsAvailableAsync())
            {
                await SecureStorage.Default.SetAsync("vault_pin", _currentPin);
            }
            await this.CloseAsync(true);
        }
        else
        {
            ShowError("Incorrect PIN.");
            _currentPin = string.Empty;
            UpdatePinDisplay();
        }
    }

    private async Task ProcessSetupAsync(string pin)
    {
        var success = await _apiClient.SetupVaultAsync(pin);
        if (success)
        {
            // Save PIN to enable biometric unlock next time if biometrics are available
            if (await _biometricService.IsAvailableAsync())
            {
                await SecureStorage.Default.SetAsync("vault_pin", pin);
            }
            await this.CloseAsync(true);
        }
        else
        {
            ShowError("Setup failed on server.");
            _currentPin = string.Empty;
            _tempPin = string.Empty;
            _confirming = false;
            InitializeUI();
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await this.CloseAsync(false);
    }
}
