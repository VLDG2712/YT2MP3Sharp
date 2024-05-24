/*
 * 
 * Copyright (c) 2024, VLDG2712
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. 
 * 
 */


using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Playlists;

namespace YouTubeMp3Downloader
{
    public class MainForm : MaterialForm
    {
        private MaterialTextBox txtUrl;
        private MaterialTextBox txtOutputPath;
        private MaterialButton btnDownload;
        private MaterialButton btnSettings;
        private TextBox txtStatus;
        private MaterialProgressBar progressBar;
        private Button btnClose;
        private Button btnMinimize;
        private string configFilePath = "config.txt";
        private string defaultSavePath = "";
        private string apiKey = "";

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
            this.txtUrl = new MaterialTextBox() { Hint = "YouTube URL or Playlist URL", Width = 280, Top = 80, Left = 20, MaxLength = 500 };
            this.txtOutputPath = new MaterialTextBox() { Hint = "Output Path", Width = 280, Top = 140, Left = 20, ReadOnly = true };
            this.btnDownload = new MaterialButton() { Text = "Download", Width = 140, Top = 200, Left = 20 };
            this.btnSettings = new MaterialButton() { Text = "Settings", Width = 140, Top = 200, Left = 160 };
            this.txtStatus = new TextBox() { Width = 280, Height = 150, Top = 260, Left = 20, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };
            this.progressBar = new MaterialProgressBar() { Width = 280, Top = 420, Left = 20 };

            this.btnDownload.Click += new EventHandler(this.btnDownload_Click);
            this.btnSettings.Click += new EventHandler(this.btnSettings_Click);

            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.txtOutputPath);
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
                if (settings.Length >= 3)
                {
                    var selectedTheme = settings[0];
                    defaultSavePath = settings[1];
                    apiKey = settings[2];

                    var materialSkinManager = MaterialSkinManager.Instance;
                    if (selectedTheme == "Light")
                    {
                        materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
                    }
                    else
                    {
                        materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
                    }

                    txtOutputPath.Text = defaultSavePath;
                }
            }
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text;
            string outputPath = string.IsNullOrEmpty(txtOutputPath.Text) ? defaultSavePath : txtOutputPath.Text;

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(outputPath))
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

            try
            {
                if (url.Contains("playlist"))
                {
                    await Task.Run(() => DownloadPlaylistAsync(url, outputPath));
                }
                else
                {
                    await Task.Run(() => DownloadVideoAsync(url, outputPath));
                }
            }
            catch (Exception ex)
            {
                txtStatus.AppendText("Error: " + ex.Message + Environment.NewLine);
            }
        }

        private async Task DownloadVideoAsync(string url, string outputPath)
        {
            try
            {
                var youtube = new YoutubeClient();

                // Get video information
                var video = await youtube.Videos.GetAsync(url);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

                // Select the best audio stream
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (streamInfo != null)
                {
                    var sanitizedTitle = SanitizeFileName(video.Title);
                    var tempFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp4");
                    var finalFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp3");

                    txtStatus.AppendText($"Downloading: {video.Title}" + Environment.NewLine);

                    // Download the video as MP4
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, tempFilePath);

                    txtStatus.AppendText($"Converting: {video.Title}" + Environment.NewLine);

                    // Convert MP4 to MP3
                    await Task.Run(() => ConvertToMp3(tempFilePath, finalFilePath));

                    txtStatus.AppendText($"Download complete: {finalFilePath}" + Environment.NewLine);
                }
                else
                {
                    txtStatus.AppendText("No suitable stream found." + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                txtStatus.AppendText("Error: " + ex.Message + Environment.NewLine);
            }
        }

        private async Task DownloadPlaylistAsync(string playlistUrl, string outputPath)
        {
            try
            {
                var youtube = new YoutubeClient();
                var playlist = await youtube.Playlists.GetAsync(playlistUrl);
                var videos = youtube.Playlists.GetVideosAsync(playlist.Id);

                // Count the videos
                int videoCount = 0;
                await foreach (var _ in videos)
                {
                    videoCount++;
                }

                progressBar.Maximum = videoCount;

                // Download videos
                await foreach (var video in youtube.Playlists.GetVideosAsync(playlist.Id))
                {
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                    if (streamInfo != null)
                    {
                        var sanitizedTitle = SanitizeFileName(video.Title);
                        var tempFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp4");
                        var finalFilePath = Path.Combine(outputPath, $"{sanitizedTitle}.mp3");

                        txtStatus.AppendText($"Downloading: {video.Title}" + Environment.NewLine);

                        // Download the video as MP4
                        await youtube.Videos.Streams.DownloadAsync(streamInfo, tempFilePath);

                        txtStatus.AppendText($"Converting: {video.Title}" + Environment.NewLine);

                        // Convert MP4 to MP3
                        await Task.Run(() => ConvertToMp3(tempFilePath, finalFilePath));

                        txtStatus.AppendText($"Download complete: {finalFilePath}" + Environment.NewLine);
                    }
                    else
                    {
                        txtStatus.AppendText($"No suitable stream found for: {video.Title}" + Environment.NewLine);
                    }

                    progressBar.Value += 1;
                }
            }
            catch (Exception ex)
            {
                txtStatus.AppendText("Error: " + ex.Message + Environment.NewLine);
            }
        }

        private void ConvertToMp3(string inputFilePath, string outputFilePath)
        {
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "ffmpeg.exe");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputFilePath}\" -vn -ar 44100 -ac 2 -b:a 320k \"{outputFilePath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
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
