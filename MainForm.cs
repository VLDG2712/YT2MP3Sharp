/*
 * 
Copyright (c) 2024, VLDG2712
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. 
 * 
 */

using MaterialSkin;
using MaterialSkin.Controls;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace YouTubeMp3Downloader
{
    public class MainForm : MaterialForm
    {
        private MaterialTextBox txtUrl;
        private ComboBox formatComboBox;
        private MaterialButton btnDownload;
        private MaterialButton btnSettings;
        private TextBox txtStatus;
        private MaterialProgressBar progressBar;
        private Button btnClose;
        private Button btnMinimize;
        private string configFilePath = "config.txt";
        private string defaultSavePath = "";
        private string apiKey = "";
        private int concurrentDownloads = 5;

        public MainForm()
        {
            InitializeComponent();
            InitializeMaterialSkin();
            LoadSettings();
            this.Icon = new Icon("Resources/icon.ico"); // Set the form icon
        }

        private void InitializeMaterialSkin()
        {
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.BlueGrey800, Primary.BlueGrey900,
                Primary.BlueGrey500, Accent.LightBlue200,
                TextShade.WHITE);
        }

        private void InitializeComponent()
        {
            this.txtUrl = new MaterialTextBox() { Hint = "YouTube URL or Playlist URL", Width = 280, Top = 58, Left = 20, MaxLength = 500 };
            this.formatComboBox = new ComboBox() { Width = 280, Top = 110, Left = 20, DropDownStyle = ComboBoxStyle.DropDownList };
            this.formatComboBox.Items.AddRange(new string[] { "MP4", "MP3" });
            this.formatComboBox.SelectedIndex = 0; // Default to MP4
            this.btnDownload = new MaterialButton() { Text = "Download", Width = 140, Top = 150, Left = 20 };
            this.btnSettings = new MaterialButton() { Text = "Settings", Width = 140, Top = 150, Left = 160 };
            this.txtStatus = new TextBox() { Width = 280, Height = 240, Top = 200, Left = 20, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            this.progressBar = new MaterialProgressBar() { Width = 280, Top = 460, Left = 20 };

            this.btnDownload.Click += new EventHandler(this.btnDownload_Click);
            this.btnSettings.Click += new EventHandler(this.btnSettings_Click);

            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.formatComboBox);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.progressBar);

            // Custom title bar buttons with icons
            this.btnClose = new Button()
            {
                Width = 20,
                Height = 20,
                Top = 8,
                Left = this.ClientSize.Width - 50,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackgroundImage = Image.FromFile("Resources/close_icon.png"),
                BackgroundImageLayout = ImageLayout.Stretch,
                FlatStyle = FlatStyle.Flat
            };
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.Click += (sender, e) => this.Close();

            this.btnMinimize = new Button()
            {
                Width = 20,
                Height = 20,
                Top = 8,
                Left = this.ClientSize.Width - 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackgroundImage = Image.FromFile("Resources/minimize_icon.png"),
                BackgroundImageLayout = ImageLayout.Stretch,
                FlatStyle = FlatStyle.Flat
            };
            this.btnMinimize.FlatAppearance.BorderSize = 0;
            this.btnMinimize.Click += (sender, e) => this.WindowState = FormWindowState.Minimized;

            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnMinimize);

            this.Text = "YouTube MP3 Downloader";
            this.ClientSize = new System.Drawing.Size(320, 480);
            this.FormBorderStyle = FormBorderStyle.None; // Remove the default title bar
            this.MaximizeBox = false; // Disable the maximize box

            // Enable dragging of the form
            this.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            // Hide default control box
            this.ControlBox = false;
        }

        public void LoadSettings()
        {
            if (File.Exists(configFilePath))
            {
                var settings = File.ReadAllLines(configFilePath);
                if (settings.Length >= 4)
                {
                    var selectedTheme = settings[0];
                    defaultSavePath = settings[1];
                    apiKey = settings[2];
                    concurrentDownloads = int.Parse(settings[3]);

                    var materialSkinManager = MaterialSkinManager.Instance;
                    if (selectedTheme == "Light")
                    {
                        materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
                    }
                    else
                    {
                        materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
                    }
                }
            }
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text;

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(defaultSavePath))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please set the API Key in settings.");
                return;
            }

            txtStatus.Clear();
            progressBar.Value = 0;

            string format = formatComboBox.SelectedItem.ToString();

            try
            {
                txtStatus.AppendText("Starting download process...\n");

                if (url.Contains("playlist"))
                {
                    await Task.Run(() => DownloadPlaylistAsync(url, defaultSavePath, format));
                }
                else
                {
                    await Task.Run(() => DownloadVideoAsync(url, defaultSavePath, format));
                }
            }
            catch (Exception ex)
            {
                txtStatus.Invoke((Action)(() => txtStatus.AppendText("Error: " + ex.Message + Environment.NewLine)));
            }
        }

        private async Task DownloadVideoAsync(string url, string outputPath, string format)
        {
            try
            {
                txtStatus.Invoke((Action)(() => txtStatus.AppendText("Fetching video information...\n")));
                var youtube = new YoutubeClient();

                // Get video information
                var video = await youtube.Videos.GetAsync(url);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                if (format == "MP4")
                {
                    var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                    if (streamInfo != null)
                    {
                        var sanitizedTitle = SanitizeFileName(video.Title);
                        var finalFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp4");

                        txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Downloading: {video.Title}" + Environment.NewLine)));

                        await youtube.Videos.Streams.DownloadAsync(streamInfo, finalFilePath);

                        txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Download complete: {finalFilePath}" + Environment.NewLine)));
                    }
                    else
                    {
                        txtStatus.Invoke((Action)(() => txtStatus.AppendText("No suitable stream found." + Environment.NewLine)));
                    }
                }
                else if (format == "MP3")
                {
                    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                    if (streamInfo != null)
                    {
                        var sanitizedTitle = SanitizeFileName(video.Title);
                        var tempFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp4");
                        var finalFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp3");

                        txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Downloading: {video.Title}" + Environment.NewLine)));

                        await youtube.Videos.Streams.DownloadAsync(streamInfo, tempFilePath);

                        txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Converting: {video.Title}" + Environment.NewLine)));

                        await Task.Run(() => ConvertToMp3(tempFilePath, finalFilePath));

                        txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Download complete: {finalFilePath}" + Environment.NewLine)));
                    }
                    else
                    {
                        txtStatus.Invoke((Action)(() => txtStatus.AppendText("No suitable stream found." + Environment.NewLine)));
                    }
                }
            }
            catch (Exception ex)
            {
                txtStatus.Invoke((Action)(() => txtStatus.AppendText("Error: " + ex.Message + Environment.NewLine)));
            }
        }

        private async Task DownloadPlaylistAsync(string playlistUrl, string outputPath, string format)
        {
            try
            {
                txtStatus.Invoke((Action)(() => txtStatus.AppendText("Fetching playlist information...\n")));
                var youtube = new YoutubeClient();
                var playlist = await youtube.Playlists.GetAsync(playlistUrl);
                var videos = youtube.Playlists.GetVideosAsync(playlist.Id);

                // Manually count the videos
                int videoCount = 0;
                await foreach (var video in videos)
                {
                    videoCount++;
                }

                progressBar.Invoke((Action)(() => progressBar.Maximum = videoCount));
                txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Found {videoCount} videos. Starting download...\n")));

                var tasks = new ConcurrentBag<Task>();
                var throttler = new SemaphoreSlim(concurrentDownloads); // Use the concurrent downloads from settings

                await foreach (var video in youtube.Playlists.GetVideosAsync(playlist.Id))
                {
                    await throttler.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                            if (format == "MP4")
                            {
                                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                                if (streamInfo != null)
                                {
                                    var sanitizedTitle = SanitizeFileName(video.Title);
                                    var finalFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp4");

                                    txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Downloading: {video.Title}" + Environment.NewLine)));

                                    await youtube.Videos.Streams.DownloadAsync(streamInfo, finalFilePath);

                                    txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Download complete: {finalFilePath}" + Environment.NewLine)));
                                }
                                else
                                {
                                    txtStatus.Invoke((Action)(() => txtStatus.AppendText($"No suitable stream found for: {video.Title}" + Environment.NewLine)));
                                }
                            }
                            else if (format == "MP3")
                            {
                                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                                if (streamInfo != null)
                                {
                                    var sanitizedTitle = SanitizeFileName(video.Title);
                                    var tempFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp4");
                                    var finalFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp3");

                                    txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Downloading: {video.Title}" + Environment.NewLine)));

                                    await youtube.Videos.Streams.DownloadAsync(streamInfo, tempFilePath);

                                    txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Converting: {video.Title}" + Environment.NewLine)));

                                    await Task.Run(() => ConvertToMp3(tempFilePath, finalFilePath));

                                    txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Download complete: {finalFilePath}" + Environment.NewLine)));
                                }
                                else
                                {
                                    txtStatus.Invoke((Action)(() => txtStatus.AppendText($"No suitable stream found for: {video.Title}" + Environment.NewLine)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            txtStatus.Invoke((Action)(() => txtStatus.AppendText($"Error downloading {video.Title}: {ex.Message}" + Environment.NewLine)));
                        }
                        finally
                        {
                            throttler.Release();
                        }

                        progressBar.Invoke((Action)(() => progressBar.Value += 1));
                    }));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                txtStatus.Invoke((Action)(() => txtStatus.AppendText("Error: " + ex.Message + Environment.NewLine)));
            }
        }

        private void ConvertToMp3(string inputFilePath, string outputFilePath)
        {
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ffmpeg", "bin", "ffmpeg.exe");
            int coreCount = Environment.ProcessorCount;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputFilePath}\" -threads {coreCount} -vn -ar 44100 -ac 2 -b:a 320k \"{outputFilePath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) txtStatus.Invoke((Action)(() => txtStatus.AppendText(e.Data + Environment.NewLine))); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) txtStatus.Invoke((Action)(() => txtStatus.AppendText(e.Data + Environment.NewLine))); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }

            File.Delete(inputFilePath); // Delete the temporary MP4 file after conversion
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitizedTitle = new string(fileName.Where(ch => !invalidChars.Contains(ch)).ToArray());
            return sanitizedTitle;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
            LoadSettings(); // Reload settings after the settings form is closed 
        }

        // Import necessary WinAPI functions for dragging the form
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
    }
}
