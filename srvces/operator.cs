using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu_fetcher.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static System.Net.Http.HttpClient;
using static System.Net.WebRequestMethods;

namespace osu_fetcher.Views; 

    public partial class MainWindow : Window
    {

    private void LoadSettingsUI()
    {
        var cfg = Services.ConfigService.Load();
        if (ChkAutoFetch != null)
        {
            ChkAutoFetch.IsChecked = !cfg.DisableAutoFetch;
        }
    }


    private async void SaveSettingsClick(object sender, System.Windows.RoutedEventArgs e)
    {
        TxtSettingsLog.Clear();
        void AppendLog(string message)
        {
            TxtSettingsLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            TxtSettingsLog.ScrollToEnd();
        }

        try
        {
            AppendLog("Initializing credentials verification workflow...");

            string clientId = TxtClientId.Text.Trim();
            string clientSecret = TxtClientSecret.Text.Trim();
            string userId = TxtUserId.Text.Trim();
            string mode = (CmbMode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "osu";

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) || string.IsNullOrWhiteSpace(userId))
            {
                AppendLog("ERROR: Form processing halted. Client ID, Client Secret, and User ID fields cannot be blank.");
                SettingsStatus.Text = "Validation failed: Missing information fields.";
                return;
            }

            var cfg = new osu_fetcher.Services.AppConfig
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                UserId = userId,
                Mode = mode
            };

            AppendLog("Writing active credentials to default configuration file 'config.json'...");
            osu_fetcher.Services.ConfigService.Save(cfg);
            AppendLog("Local application config written and updated successfully.");

            AppendLog("Sending token payload sequence to identity server (POST https://osu.ppy.sh/oauth/token)...");

            var tokenRequestParams = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "grant_type", "client_credentials" },
                    { "scope", "public" }
                };

            using var requestContent = new FormUrlEncodedContent(tokenRequestParams);

            if (!Http.DefaultRequestHeaders.Contains("User-Agent"))
            {
                Http.DefaultRequestHeaders.Add("User-Agent", "osu-fetcher-desktop-app");
            }

            var response = await Http.PostAsync("https://osu.ppy.sh/oauth/token", requestContent);
            AppendLog($"HTTP Response: {(int)response.StatusCode} {response.ReasonPhrase}");

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                AppendLog($"ERROR: Access token request denied. Server returned structural details:\n{responseBody}");
                SettingsStatus.Text = "OAuth Validation Broken";
                return;
            }

            AppendLog("OAuth verification accepted. Decoding secure access token wrapper...");
            var tokenJson = JObject.Parse(responseBody);
            string accessToken = tokenJson["access_token"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(accessToken))
            {
                AppendLog("ERROR: Unexpected parsing issue. Access payload key is missing or corrupted.");
                SettingsStatus.Text = "Parsing failure";
                return;
            }

            AppendLog("Bearer validation key extracted. Authenticating user profile endpoint...");

            string userApiUrl = $"https://osu.ppy.sh/api/v2/users/{userId}/{mode}";
            AppendLog($"Querying endpoint path (GET {userApiUrl})...");

            using var apiRequest = new HttpRequestMessage(HttpMethod.Get, userApiUrl);
            apiRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            apiRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var apiResponse = await Http.SendAsync(apiRequest);
            AppendLog($"HTTP API Response: {(int)apiResponse.StatusCode} {apiResponse.ReasonPhrase}");

            string apiResponseBody = await apiResponse.Content.ReadAsStringAsync();

            if (!apiResponse.IsSuccessStatusCode)
            {
                AppendLog($"ERROR: Cannot connect to profile target information. API returned detailed context:\n{apiResponseBody}");
                SettingsStatus.Text = "User resolution mismatch";
                return;
            }

            var userJson = JObject.Parse(apiResponseBody);
            string verifiedUsername = userJson["username"]?.ToString() ?? "Unknown";

            AppendLog($"SUCCESS: Target user resolved! Authenticated safely as player: '{verifiedUsername}'");
            SettingsStatus.Text = "Settings verified & configured!";
        }
        catch (Exception ex)
        {
            AppendLog($"CRITICAL CRASH: Process interrupted prematurely by exception module:\n{ex.Message}");
            if (ex.InnerException != null)
            {
                AppendLog($"Inner Exception reference: {ex.InnerException.Message}");
            }
            SettingsStatus.Text = "Connection process failed.";
        }
    }

    private async Task<JToken> GetOsuDataAsync(string endpoint, string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"https://osu.ppy.sh/api/v2/{endpoint}");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var res = await Http.SendAsync(req);
        if (!res.IsSuccessStatusCode) throw new Exception($"API error: {res.ReasonPhrase}");

        return JToken.Parse(await res.Content.ReadAsStringAsync());
    }

    private static async Task<BitmapImage?> LoadImage(string url)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(url);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = new MemoryStream(bytes);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    



}