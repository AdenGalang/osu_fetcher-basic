using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace osu_fetcher.Views
{
    public partial class MainWindow : Window
    {
        private void LoadJsonClick(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON files|*.json|All files|*.*" };
            if (dlg.ShowDialog() == true)
                LoadJson(dlg.FileName);
        }

        private async void FetchClick(object sender, System.Windows.RoutedEventArgs e)
        {
            BtnFetch.IsEnabled = false;
            if (FetchStatusText != null) FetchStatusText.Text = "Authenticating...";

            try
            {
                string clientId = TxtClientId.Text.Trim();
                string clientSecret = TxtClientSecret.Text.Trim();
                string userId = TxtUserId.Text.Trim();
                string mode = (CmbMode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "osu";

                var tokenParams = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "grant_type", "client_credentials" },
            { "scope", "public" }
        };

                using var requestContent = new FormUrlEncodedContent(tokenParams);
                var tokenResponse = await Http.PostAsync("https://osu.ppy.sh/oauth/token", requestContent);
                if (!tokenResponse.IsSuccessStatusCode) throw new Exception("OAuth failed.");

                var tokenJson = JObject.Parse(await tokenResponse.Content.ReadAsStringAsync());
                string accessToken = tokenJson["access_token"]?.ToString() ?? throw new Exception("Token parse failed.");

                if (FetchStatusText != null) FetchStatusText.Text = "Fetching data...";

                var profileJson = await GetOsuDataAsync($"users/{userId}/{mode}", accessToken);
                var bestJson = await GetOsuDataAsync($"users/{userId}/scores/best?limit=100&mode={mode}", accessToken);
                var recentJson = await GetOsuDataAsync($"users/{userId}/scores/recent?limit=100&mode={mode}", accessToken);

                _data = new JObject
                {
                    ["profile"] = profileJson,
                    ["best_scores"] = bestJson,
                    ["recent_scores"] = recentJson
                };

                await PopulateAll();
                if (FetchStatusText != null) FetchStatusText.Text = "Fetch complete!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                BtnFetch.IsEnabled = true;
            }
        }

        // ── Navigation
        public void NavClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var tag = ((Button)sender).Tag?.ToString();

            PageProfile.Visibility = tag == "Profile" ? Visibility.Visible : Visibility.Collapsed;
            PageScores.Visibility = tag == "Scores" ? Visibility.Visible : Visibility.Collapsed;
            PageRecent.Visibility = tag == "Recent" ? Visibility.Visible : Visibility.Collapsed;
            PageAllScores.Visibility = tag == "AllScores" ? Visibility.Visible : Visibility.Collapsed;
            PageGraphs.Visibility = tag == "Graphs" ? Visibility.Visible : Visibility.Collapsed;
            PageSettings.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            if (sender == BtnSettings)
            {
                LoadSettingsUI();
            }
        
        var navButtons = new[] { BtnProfile, BtnScores, BtnRecent, BtnAllScores, BtnGraphs, BtnSettings };
            foreach (var btn in navButtons)
            {
                if (btn != null)
                    btn.Style = (Style)FindResource("BtnGhost");
            }

            ((Button)sender).Style = (Style)FindResource("BtnNav");
        }
        private async Task PopulateAll()
        {
            if (_data == null) return;
            await PopulateProfile();
            PopulateScores();
            PopulateRecent();
            PopulateGraphs();
        }

        // ── Profile
        private async Task PopulateProfile()
        {
            var p = _data!["profile"]!;
            var stat = p["statistics"]!;
            var jsonUserId = p["id"]?.ToString();
            if (!string.IsNullOrEmpty(jsonUserId) && string.IsNullOrEmpty(TxtUserId.Text))
            {
                TxtUserId.Text = jsonUserId;
                UpdateUserIdGhostVisibility();
            }

            string username = p["username"]?.ToString() ?? "Unknown";
            string globalRank = $"#{stat["global_rank"]?.ToObject<int>():N0}";
            string ppValue = $"{stat["pp"]?.ToObject<double>():N0}pp"; 

            UsernameText.Text = username;
            RankText.Text = globalRank;
            StatPP.Text = ppValue;
            StatRank.Text = globalRank;
            StatAcc.Text = $"{stat["hit_accuracy"]?.ToObject<double>():F2}%";
            StatPlays.Text = $"{stat["play_count"]?.ToObject<int>():N0}";
            StatCountryRank.Text = $"#{stat["country_rank"]?.ToObject<int>():N0}";
            StatMaxCombo.Text = $"{stat["maximum_combo"]?.ToObject<int>():N0}x";
            StatLevel.Text = $"{stat["level"]?["current"]} ({stat["level"]?["progress"]}%)";

            if (SidebarUsername != null) SidebarUsername.Text = username;
            if (SidebarRank != null) SidebarRank.Text = globalRank;
            if (SidebarPP != null) SidebarPP.Text = ppValue;

            var secs = stat["play_time"]?.ToObject<long>() ?? 0;
            var ts = TimeSpan.FromSeconds(secs);
            StatPlayTime.Text = $"{(int)ts.TotalHours}h {ts.Minutes}m";

            var gc = stat["grade_counts"]!;
            GradeSSH.Text = gc["ssh"]?.ToString() ?? "0";
            GradeSS.Text = gc["ss"]?.ToString() ?? "0";
            GradeSH.Text = gc["sh"]?.ToString() ?? "0";
            GradeS.Text = gc["s"]?.ToString() ?? "0";
            GradeA.Text = gc["a"]?.ToString() ?? "0";

            Hit300.Text = $"{stat["count_300"]?.ToObject<long>():N0}";
            Hit100.Text = $"{stat["count_100"]?.ToObject<long>():N0}";
            Hit50.Text = $"{stat["count_50"]?.ToObject<long>():N0}";
            HitMiss.Text = $"{stat["count_miss"]?.ToObject<long>():N0}";

            var avatarUrl = p["avatar_url"]?.ToString();
            var coverUrl = p["cover_url"]?.ToString();

            if (avatarUrl != null)
            {
                var avatarBmp = await osu_fetcher.Services.ImageCache.LoadAsync(avatarUrl);
                if (avatarBmp != null)
                {
                    AvatarImage.Source = avatarBmp;
                    if (SidebarAvatar != null) SidebarAvatar.Source = avatarBmp;
                }
            }
            if (coverUrl != null)
            {
                CoverImage.Source = await osu_fetcher.Services.ImageCache.LoadAsync(coverUrl);
            }
            await RenderUserAchievements(p);
        }
        private UIElement BuildScoreRow(JToken sc, int idx, bool recent = false)
        {
            var bm = sc["beatmap"];
            var bms = sc["beatmapset"];
            var stats = sc["statistics"];
            var pp = sc["pp"]?.ToObject<double?>() ?? 0;
            var rank = sc["rank"]?.ToString() ?? "?";
            var acc = sc["accuracy"]?.ToObject<double>() * 100 ?? 0;
            var mods = string.Join("", sc["mods"]?.Select(m => m.ToString()) ?? []);
            var title = bms?["title"]?.ToString() ?? bm?["version"]?.ToString() ?? "Unknown";
            var artist = bms?["artist"]?.ToString() ?? "";
            var diff = bm?["version"]?.ToString() ?? "";
            var sr = bm?["difficulty_rating"]?.ToObject<double>() ?? 0;
            var combo = sc["max_combo"]?.ToObject<int>() ?? 0;
            var miss = stats?["count_miss"]?.ToObject<int>() ?? 0;
            var rankColor = rank switch
            {
                "X" or "XH" or "SSH" => Brushes.Gold,
                "S" or "SH" => new SolidColorBrush(Color.FromRgb(170, 187, 255)),
                "A" => new SolidColorBrush(Color.FromRgb(68, 221, 136)),
                "B" => Brushes.DodgerBlue,
                "C" => Brushes.Orange,
                _ => Brushes.Gray
            };

            var coverUrl = bms?["covers"]?["list"]?.ToString();
            var row = new Border();
            if (this.TryFindResource("ScoreRow") is Style scoreRowStyle)
            {
                row.Style = scoreRowStyle;
            }
            else
            {
                row.Background = new SolidColorBrush(Color.FromRgb(37, 40, 64));
                row.CornerRadius = new CornerRadius(8);
                row.Padding = new Thickness(12, 8, 12, 8);
                row.Margin = new Thickness(0, 0, 0, 8);
                row.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 45, 69));
                row.BorderThickness = new Thickness(1);
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var idxTb = new TextBlock
            {
                Text = $"#{idx}",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 170)),
                FontSize = 12
            };
            Grid.SetColumn(idxTb, 0);
            grid.Children.Add(idxTb);

            var rankBorder = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var rankTb = new TextBlock
            {
                Text = rank,
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI Bold"),
                Foreground = rankColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            rankBorder.Child = rankTb;
            Grid.SetColumn(rankBorder, 1);
            grid.Children.Add(rankBorder);

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var coverImg = new Image
            {
                Width = 44,
                Height = 44,
                Stretch = Stretch.UniformToFill,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            if (coverUrl != null)
                _ = LoadImage(coverUrl).ContinueWith(t => {
                    if (t.Result != null)
                        Dispatcher.Invoke(() => coverImg.Source = t.Result);
                });
            Grid.SetColumn(coverImg, 0);
            titleGrid.Children.Add(coverImg);

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            titleStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 255))
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = $"{artist}  [{diff}]  ★{sr:F1}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 170)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (!string.IsNullOrEmpty(mods))
                titleStack.Children.Add(new TextBlock
                {
                    Text = $"+{mods}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 80))
                });
            Grid.SetColumn(titleStack, 1);
            titleGrid.Children.Add(titleStack);
            Grid.SetColumn(titleGrid, 2);
            grid.Children.Add(titleGrid);

            var accTb = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            accTb.Children.Add(new TextBlock
            {
                Text = $"{acc:F2}%",
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI Semibold"),
                Foreground = new SolidColorBrush(Color.FromRgb(68, 221, 136)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            accTb.Children.Add(new TextBlock
            {
                Text = $"{combo}x  {miss}m",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 170)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            Grid.SetColumn(accTb, 3);
            grid.Children.Add(accTb);

            var ppTb = new TextBlock
            {
                Text = pp > 0 ? $"{pp:F0}pp" : "---",
                FontSize = 15,
                FontFamily = new FontFamily("Segoe UI Bold"),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 107, 138)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(ppTb, 4);
            grid.Children.Add(ppTb);

            var date = sc["created_at"]?.ToObject<DateTime>() ?? DateTime.MinValue;
            var dateTb = new TextBlock
            {
                Text = date == DateTime.MinValue ? "" : date.ToString("MMM dd yyyy"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 170)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(dateTb, 5);
            grid.Children.Add(dateTb);

            row.Child = grid;

            var url = bm?["url"]?.ToString();
            if (url != null)
            {
                row.Cursor = Cursors.Hand;
                row.MouseDown += (s, e) => {
                    if (e.ChangedButton == MouseButton.Left)
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                };
            }

            return row;
        }

        // ──==Graphs
        private void PopulateGraphs()
        {
            var p = _data!["profile"]!;

            // Rank history-
            var rankData = p["rank_history"]?["data"]?.ToObject<int[]>() ?? [];
            RankSeries = [
                new LineSeries<int> {
                    Values = rankData,
                    Stroke = new SolidColorPaint(SKColor.Parse("#e94560")) { StrokeThickness = 2 },
                    Fill   = new LinearGradientPaint(SKColor.Parse("#44e94560"), SKColor.Parse("#00e94560"),
                                                     new SKPoint(0.5f,0), new SKPoint(0.5f,1)),
                    GeometrySize = 0,
                    LineSmoothness = 0.5
                }
            ];
            RankYAxes = [new Axis {
                IsInverted = true,
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#a0a0c0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#22ffffff"))
            }];
            RankXAxes = [new Axis {
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#a0a0c0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#11ffffff"))
            }];

            // playcounts-
            var monthly = p["monthly_playcounts"] as JArray ?? [];
            var playLabels = monthly.Select(m => m["start_date"]?.ToString()?[..7] ?? "").ToArray();
            var playCounts = monthly.Select(m => m["count"]?.ToObject<int>() ?? 0).ToArray();
            PlaySeries = [
                new ColumnSeries<int> {
                    Values = playCounts,
                    Fill   = new SolidColorPaint(SKColor.Parse("#880f3460")),
                    Stroke = new SolidColorPaint(SKColor.Parse("#e94560")) { StrokeThickness = 1 },
                    MaxBarWidth = 20
                }
            ];
            PlayXAxes = [new Axis {
                Labels = playLabels,
                TextSize = 9,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#a0a0c0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#11ffffff")),
                LabelsRotation = -45
            }];
            PlayYAxes = [new Axis {
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#a0a0c0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#22ffffff"))
            }];

            // PP distribution-
            var best = _data!["best_scores"] as JArray ?? [];
            var ppValues = best.Select((sc, i) => sc["pp"]?.ToObject<double>() ?? 0).ToArray();
            var ppLabels = best.Select((sc, i) => $"#{i + 1}").ToArray();
            PPSeries = [
                new ColumnSeries<double> {
                    Values = ppValues,
                    Fill   = new LinearGradientPaint(SKColor.Parse("#88e94560"), SKColor.Parse("#440f3460"),
                                                     new SKPoint(0.5f,0), new SKPoint(0.5f,1)),
                    Stroke = new SolidColorPaint(SKColor.Parse("#e94560")) { StrokeThickness = 1 },
                    MaxBarWidth = 12
                }
            ];
            PPXAxes = [new Axis {
                Labels = ppLabels,
                TextSize = 8,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#a0a0c0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#11ffffff"))
            }];
            PPYAxes = [new Axis {
                TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#a0a0c0")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#22ffffff")),
                Labeler = v => $"{v:F0}pp"
            }];

            RankChart.Series = RankSeries;
            RankChart.XAxes = RankXAxes;
            RankChart.YAxes = RankYAxes;
            PlayChart.Series = PlaySeries;
            PlayChart.XAxes = PlayXAxes;
            PlayChart.YAxes = PlayYAxes;
            PPChart.Series = PPSeries;
            PPChart.XAxes = PPXAxes;
            PPChart.YAxes = PPYAxes;
        }

        private void PopulateScores()
        {
            ScoresList.Children.Clear();
            var scores = _data!["best_scores"] as JArray ?? [];
            int idx = 1;
            foreach (var sc in scores)
                ScoresList.Children.Add(BuildScoreRow(sc, idx++));
        }

        private void PopulateRecent()
        {
            var scores = _data!["recent_scores"] as JArray;

            if (scores == null)
            {
                System.Diagnostics.Debug.WriteLine("Recent scores data is null!");
                return;
            }

            RecentList.Children.Clear();
            int idx = 1;

            foreach (var sc in scores)
            {
                RecentList.Children.Add(BuildScoreRow(sc, idx++, recent: true));
            }

            System.Diagnostics.Debug.WriteLine($"Added {scores.Count} recent scores.");
        }
        private void UpdateUserIdGhostVisibility()
        {
            if (UserIdGhost != null)
            {
                UserIdGhost.Visibility = string.IsNullOrEmpty(TxtUserId.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
    }
}
