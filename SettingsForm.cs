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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace YouTubeMp3Downloader
{
    public class SettingsForm : MaterialForm
    {
        private MaterialRadioButton rbLightTheme;
        private MaterialRadioButton rbDarkTheme;
        private MaterialTextBox txtDefaultPath;
        private MaterialTextBox txtApiKey;
        private MaterialButton btnSave;
        private Button btnClose;
        private Button btnMinimize;
        private string configFilePath = "config.txt";
        private MaterialSkinManager materialSkinManager;

        public SettingsForm()
        {
            InitializeComponent();
            InitializeMaterialSkin();
            LoadSettings();
        }

        private void InitializeMaterialSkin()
        {
            materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.BlueGrey800, Primary.BlueGrey900,
                Primary.BlueGrey500, Accent.LightBlue200,
                TextShade.WHITE);
        }

        private void InitializeComponent()
        {
            this.rbLightTheme = new MaterialRadioButton() { Text = "Light Theme", Width = 280, Top = 80, Left = 20 };
            this.rbDarkTheme = new MaterialRadioButton() { Text = "Dark Theme", Width = 280, Top = 110, Left = 20, Checked = true };
            this.txtDefaultPath = new MaterialTextBox() { Hint = "Default Save Path", Width = 280, Top = 140, Left = 20 };
            this.txtApiKey = new MaterialTextBox() { Hint = "API Key", Width = 280, Top = 170, Left = 20 };
            this.btnSave = new MaterialButton() { Text = "Save Settings", Width = 280, Top = 210, Left = 20 };

            this.rbLightTheme.CheckedChanged += new EventHandler(this.ThemeChanged);
            this.rbDarkTheme.CheckedChanged += new EventHandler(this.ThemeChanged);
            this.btnSave.Click += new EventHandler(this.btnSave_Click);

            this.Controls.Add(this.rbLightTheme);
            this.Controls.Add(this.rbDarkTheme);
            this.Controls.Add(this.txtDefaultPath);
            this.Controls.Add(this.txtApiKey);
            this.Controls.Add(this.btnSave);

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

            this.Text = "Settings";
            this.ClientSize = new System.Drawing.Size(320, 300);
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

        private void ThemeChanged(object sender, EventArgs e)
        {
            if (rbLightTheme.Checked)
            {
                materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            }
            else if (rbDarkTheme.Checked)
            {
                materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            var selectedTheme = rbLightTheme.Checked ? "Light" : "Dark";
            var defaultPath = txtDefaultPath.Text;
            var apiKey = txtApiKey.Text;

            File.WriteAllLines(configFilePath, new[] { selectedTheme, defaultPath, apiKey });
            MessageBox.Show("Settings saved.");
            UpdateMainFormTheme();
        }

        private void LoadSettings()
        {
            if (File.Exists(configFilePath))
            {
                var settings = File.ReadAllLines(configFilePath);
                if (settings.Length >= 3)
                {
                    var selectedTheme = settings[0];
                    var defaultPath = settings[1];
                    var apiKey = settings[2];

                    if (selectedTheme == "Light")
                    {
                        rbLightTheme.Checked = true;
                        rbDarkTheme.Checked = false;
                    }
                    else
                    {
                        rbLightTheme.Checked = false;
                        rbDarkTheme.Checked = true;
                    }

                    txtDefaultPath.Text = defaultPath;
                    txtApiKey.Text = apiKey;
                }
            }
        }

        private void UpdateMainFormTheme()
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm != null)
            {
                mainForm.LoadSettings();
            }
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