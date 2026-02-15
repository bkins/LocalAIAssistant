// using System;
// using System.Threading.Tasks;
// using CommunityToolkit.Mvvm.ComponentModel;
// using Microsoft.Maui.Storage;
//
// namespace LocalAIAssistant.Services;
//
// public enum ApiEnvironment
// {
//     QaAndroid
//   , QaLocal
//   , Qa
//   , Dev
// }
//
// public partial class ApiEnvironmentService : ObservableObject
// {
//     const string StorageKey = "ApiEnvironment";
//
//     [ObservableProperty] private ApiEnvironment current;
//
//     public string BaseUrl => ResolveUrl();
//     
//     //TODO: Test 'ApiEnvironment.Qa' and 'ApiEnvironment.Local'.  Are they needed?
//     string ResolveUrl() =>
//             Current switch
//             {
//                     ApiEnvironment.Dev       => "http://192.168.0.33:5273",
//                     ApiEnvironment.Qa        => "http://192.168.0.33:5272",
//                     ApiEnvironment.QaAndroid => "http://192.168.0.33:5272",
//                     ApiEnvironment.QaLocal   => "http://localhost:5272",
//                     _                        => throw new ArgumentOutOfRangeException()
//             };
//     public Task InitializeAsync(ApiEnvironment defaultEnvironment, bool forceDefault =  false)
//     {
//         if (IsInitialized) return Task.CompletedTask;
//
//         if (forceDefault) Preferences.Set(StorageKey, defaultEnvironment.ToString());
//     
//         var saved = Preferences.Get(StorageKey, defaultEnvironment.ToString());
//     
//         if (!Enum.TryParse(saved, out ApiEnvironment env))
//             env = defaultEnvironment;
//     
//         Current = env;
//         IsInitialized = true;
//     
//         return Task.CompletedTask;
//     }
//
//     public bool IsInitialized { get; set; }
//
//
//     public async Task SetAsync (ApiEnvironment env)
//     {
//         if (Current == env) return;
//
//         Current = env;
//         Preferences.Set(StorageKey
//                       , env.ToString());
//     }
// }
