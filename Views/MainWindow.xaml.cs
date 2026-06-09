using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace osu_fetcher.Views
{
    public partial class MainWindow : Window
    {
        private JObject? _data;
        private static readonly HttpClient Http = new();
        private Dictionary<int, string> _medalIdToSlugMap = new();
        private Dictionary<int, string> _medalOverrideMap = new();
        public ISeries[] RankSeries { get; set; } = [];
        public ISeries[] PlaySeries { get; set; } = [];
        public ISeries[] PPSeries { get; set; } = [];
        public Axis[] RankXAxes { get; set; } = [];
        public Axis[] RankYAxes { get; set; } = [];
        public Axis[] PlayXAxes { get; set; } = [];
        public Axis[] PlayYAxes { get; set; } = [];
        public Axis[] PPXAxes { get; set; } = [];
        public Axis[] PPYAxes { get; set; } = [];

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadConfig();
            LoadDefinitiveMedalMap();
            this.Loaded += MainWindow_Loaded;
            LoadSettingsUI();

            var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "osu_data.json");
            if (File.Exists(defaultPath))
                LoadJson(defaultPath);

            bool isConfigured = !string.IsNullOrEmpty(TxtClientId.Text) && !string.IsNullOrEmpty(TxtUserId.Text);

            if (isConfigured)
            {
                NavClick(BtnProfile, new RoutedEventArgs());
            }
            else
            {
                NavClick(BtnSettings, new RoutedEventArgs());
                MessageBox.Show("Please fill out your API credentials in Settings.", "Setup Required");
            }
        }
       
        private void LoadDefinitiveMedalMap()
        {
            _medalIdToSlugMap.Clear();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mapping", "medal_map.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        if (int.TryParse(kvp.Key, out int id))
                            _medalIdToSlugMap[id] = kvp.Value;
                    }
                }
            }
        }

        private async Task RenderUserAchievements(JToken p)
        {
            if (this.FindName("AchievementsContent") is Panel achievementsContainer)
            {
                achievementsContainer.Children.Clear();
                var userMedals = p["user_achievements"] as JArray;

                if (userMedals == null) return;

                foreach (var medal in userMedals)
                {
                    if (!int.TryParse(medal["achievement_id"]?.ToString(), out var achievementId))
                        continue;

                    var medalBorder = new Border
                    {
                        Width = 50,
                        Height = 50,
                        Margin = new Thickness(4),
                        CornerRadius = new CornerRadius(6),
                        Background = (Brush)FindResource("BgCard"),
                        BorderBrush = (Brush)FindResource("Border"),
                        BorderThickness = new Thickness(1)
                    };

                    var medalImg = new Image { Stretch = Stretch.Uniform, Margin = new Thickness(4) };
                    medalBorder.Child = medalImg;
                    achievementsContainer.Children.Add(medalBorder);
                    _ = Task.Run(async () =>
                    {
                        string? finalSlug = null;

                        if (_medalOverrideMap.TryGetValue(achievementId, out var ovr)) finalSlug = ovr;
                        else if (_medalIdToSlugMap.TryGetValue(achievementId, out var map)) finalSlug = map;
                        if (finalSlug != null)
                        {
                            var url = finalSlug.Contains(".png")
                                ? $"https://assets.ppy.sh/medals/web/{finalSlug}"
                                : $"https://assets.ppy.sh/medals/web/{finalSlug}@2x.png";

                            var b = await osu_fetcher.Services.ImageCache.LoadAsync(url);

                            if (b != null)
                            {
                                Dispatcher.Invoke(() => medalImg.Source = b);
                                return;
                            }
                        }
                        Dispatcher.Invoke(() => {
                            
                    var placeholder = new System.Windows.Shapes.Rectangle { Width = 42, Height = 42, Fill = (Brush)FindResource("Border") };
                            var vb = new VisualBrush(placeholder);
                            var bmp = new RenderTargetBitmap(42, 42, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                            var dv = new DrawingVisual();
                            using (var dc = dv.RenderOpen()) dc.DrawRectangle(vb, null, new Rect(0, 0, 42, 42));
                            bmp.Render(dv);
                            medalImg.Source = BitmapFrame.Create(bmp);
                        });
                    });
                }
        }   }

        private async void LoadJson(string path)
        {
            try
            {
                var raw = await File.ReadAllTextAsync(path);
                _data = JObject.Parse(raw);
                await PopulateAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load JSON:\n{ex.Message}");
            }
        }
        public void LoadConfig()
        {
            try
            {
                var cfg = Services.ConfigService.Load();
                TxtClientId.Text = cfg.ClientId;
                TxtClientSecret.Text = cfg.ClientSecret;
                TxtUserId.Text = cfg.UserId;

                if (CmbMode != null && !string.IsNullOrEmpty(cfg.Mode))
                {
                    foreach (ComboBoxItem item in CmbMode.Items)
                    {
                        if (item.Content?.ToString() == cfg.Mode)
                        {
                            CmbMode.SelectedItem = item;
                            break;
                        }
                    }
                }

                UpdateUserIdGhostVisibility();
            }
            catch { } }

        // ── Window chrome ──
        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
        private void CloseClick(object s, RoutedEventArgs e) => Close();
        private void MinimizeClick(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private ScrollViewer GetPageProfile()
        {
            return PageProfile;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var cfg = Services.ConfigService.Load();
            bool isConfigured = Services.ConfigService.HasCredentials(cfg);

            if (isConfigured)
            {
                NavClick(BtnProfile, new RoutedEventArgs());

                if (!cfg.DisableAutoFetch)
                {
                    FetchClick(BtnFetch, new RoutedEventArgs());
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Auto-fetch is disabled via config.");
                }
            }
            else
            {
                NavClick(BtnSettings, new RoutedEventArgs());
                MessageBox.Show("Welcome! Please verify your API credentials in the Settings tab to begin.", "First Time Setup");
            }
        }
        private void AutoFetch_Changed(object sender, RoutedEventArgs e)
        {
            var cfg = Services.ConfigService.Load();
            cfg.DisableAutoFetch = !(ChkAutoFetch.IsChecked ?? true);
            Services.ConfigService.Save(cfg);
        }

        private void TxtUserId_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateUserIdGhostVisibility();
        }
        private void UserId_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // placeholder 
        }
        private void FetchAllScoresClick(object sender, System.Windows.RoutedEventArgs e)
        {
            // placeholder 
        }
    }
}
