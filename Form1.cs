using Microsoft.Web.WebView2.Core;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NovaBrowser;

public partial class Form1 : Form
{
    private bool isDarkMode = false;
    private Color accentColor = Color.FromArgb(0, 122, 204);
    private readonly string bookmarksFilePath;
    private readonly List<Bookmark> bookmarks = new();
    private readonly List<BrowserTab> tabs = new();
    private readonly Dictionary<string, Image> faviconCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> faviconLoading = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Microsoft.Web.WebView2.WinForms.WebView2> initializedWebViews = new();
    private readonly object faviconLock = new();
    private static readonly Image defaultFavicon = CreateDefaultFavicon();
    private static readonly HttpClient httpClient = new();
    private static CoreWebView2Environment? _sharedWebViewEnvironment;
    public static string AppVersion => "1.0.0";
    private readonly List<string> addressHistory = new();
    private BrowserTab? activeTab;
    private Button? moreButton;
    private ContextMenuStrip? overflowMenu;
    private System.Windows.Forms.Timer? audioPollTimer;
    private System.Windows.Forms.Timer? audioBlinkTimer;
    private readonly Panel settingsPanel = new();
    private ContextMenuStrip? urlContextMenu;
    private Panel? defaultBrowserContentPanel;
    private Panel? homePageContentPanel;
    private bool showHomeButton = true;
    private bool homePageIsNewTab = true;
    private string customHomeUrl = "https://www.bing.com";

    private sealed record Bookmark(string Title, string Url);

    private static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        var trimmed = url.Trim();
        try
        {
            var uri = new Uri(trimmed);
            var host = uri.Host.ToLowerInvariant();
            if (host.StartsWith("www."))
                host = host.Substring(4);

            var path = uri.AbsolutePath;
            if (path.EndsWith("/") && path.Length > 1)
                path = path.TrimEnd('/');

            var builder = new UriBuilder(uri)
            {
                Host = host,
                Path = path,
                Port = uri.IsDefaultPort ? -1 : uri.Port,
                Fragment = string.Empty,
                Query = uri.Query
            };

            var normalized = builder.Uri.AbsoluteUri;
            return normalized.EndsWith("/") ? normalized.TrimEnd('/') : normalized;
        }
        catch
        {
            return trimmed.TrimEnd('/');
        }
    }

    private sealed class BrowserTab
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime NavigationStartedAt { get; set; }
        public TimeSpan LastLoadDuration { get; set; }
        public bool HasLoadDuration { get; set; }
        public bool IsPlayingAudio { get; set; }
        public bool AudioBlinkOn { get; set; }
        public bool IsSettingsTab { get; set; }
        public bool NavigationSucceeded { get; set; }
        public Microsoft.Web.WebView2.WinForms.WebView2 View { get; }
        public Panel HeaderPanel { get; }
        public Button TabButton { get; }

        public BrowserTab(string title, string url, Microsoft.Web.WebView2.WinForms.WebView2 view, Panel headerPanel, Button tabButton)
        {
            Title = title;
            Url = url;
            NavigationStartedAt = DateTime.UtcNow;
            LastLoadDuration = TimeSpan.Zero;
            HasLoadDuration = false;
            View = view;
            HeaderPanel = headerPanel;
            TabButton = tabButton;
        }
    }

    public Form1()
    {
        InitializeComponent();
        _ = this.Handle;
        EnableTransparentTitleBar();
        bookmarksFilePath = Path.Combine(Application.StartupPath, "bookmarks.json");
        LoadBookmarks();
        RefreshBookmarkButtons();
        // Recompute bookmark layout when the panel resizes
        bookmarksPanel.SizeChanged += (s, e) => RefreshBookmarkButtons();
        tabsPanel.SizeChanged += (s, e) => UpdateTabHeaderSizes();
        this.Resize += (s, e) => RefreshBookmarkButtons();
        ApplyTheme();
        InitializeBrowser();
        InitializeSettingsPanel();

        urlContextMenu = new ContextMenuStrip();
        var copyItem = new ToolStripMenuItem("Copy");
        copyItem.Click += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(txtAddress.Text))
            {
                Clipboard.SetText(txtAddress.Text);
            }
        };
        var pasteItem = new ToolStripMenuItem("Paste");
        pasteItem.Click += (sender, e) =>
        {
            if (Clipboard.ContainsText())
            {
                txtAddress.Text = Clipboard.GetText();
                txtAddress.Focus();
            }
        };
        var pasteAndGoItem = new ToolStripMenuItem("Paste and Go");
        pasteAndGoItem.Click += (sender, e) =>
        {
            if (Clipboard.ContainsText())
            {
                var url = Clipboard.GetText().Trim();
                txtAddress.Text = url;
                AddToAddressHistory(url);
                Navigate(url);
            }
        };
        urlContextMenu.Items.Add(copyItem);
        urlContextMenu.Items.Add(pasteItem);
        urlContextMenu.Items.Add(pasteAndGoItem);
        txtAddress.ContextMenuStrip = urlContextMenu;

        audioPollTimer = new System.Windows.Forms.Timer { Interval = 500 };
        audioPollTimer.Tick += AudioPollTimer_Tick;
        audioPollTimer.Start();

        audioBlinkTimer = new System.Windows.Forms.Timer { Interval = 700 };
        audioBlinkTimer.Tick += (sender, e) =>
        {
            foreach (var tab in tabs)
            {
                if (tab.IsPlayingAudio)
                {
                    tab.AudioBlinkOn = !tab.AudioBlinkOn;
                    tab.TabButton.Invalidate();
                }
                else if (tab.AudioBlinkOn)
                {
                    tab.AudioBlinkOn = false;
                    tab.TabButton.Invalidate();
                }
            }
        };
        audioBlinkTimer.Start();

        this.FormClosing += (s, e) =>
        {
            audioPollTimer?.Stop();
            audioBlinkTimer?.Stop();
        };
    }

    private string GetNewTabUrl() => homePageIsNewTab ? "about:blank" : customHomeUrl;

    private void InitializeBrowser()
    {
        AddTab(GetNewTabUrl());
    }

    private void InitializeSettingsPanel()
    {
        settingsPanel.Dock = DockStyle.Fill;
        settingsPanel.Visible = false;

        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = isDarkMode ? Color.FromArgb(28, 28, 28) : Color.FromArgb(230, 230, 230),
            Padding = new Padding(0, 20, 0, 0)
        };

        var categoryList = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = sidebar.BackColor,
            ForeColor = isDarkMode ? Color.White : Color.Black,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            ItemHeight = 36,
            DrawMode = DrawMode.OwnerDrawFixed,
            IntegralHeight = false
        };
        categoryList.Items.Add("Appearance");
        categoryList.Items.Add("Default Browser");
        categoryList.Items.Add("Home Page");
        categoryList.SelectedIndex = 0;
        categoryList.DrawItem += (sender, e) =>
        {
            if (e.Index < 0) return;
            var back = e.Index == categoryList.SelectedIndex
                ? (isDarkMode ? Color.FromArgb(48, 48, 48) : Color.FromArgb(210, 210, 210))
                : sidebar.BackColor;
            e.Graphics.FillRectangle(new SolidBrush(back), e.Bounds);
            var textBounds = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, categoryList.Items[e.Index]?.ToString(), e.Font!, textBounds, categoryList.ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            e.DrawFocusRectangle();
        };
        sidebar.Controls.Add(categoryList);

        var contentArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = isDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245),
            Padding = new Padding(40, 30, 40, 30),
            AutoScroll = true
        };

        var appearancePanel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = contentArea.BackColor
        };

        int y = 0;

        var sectionTitle = new Label
        {
            Text = "Appearance",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(0, y)
        };
        appearancePanel.Controls.Add(sectionTitle);
        y += 50;

        var modeLabel = new Label
        {
            Text = "Dark Mode",
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(0, y + 4)
        };
        appearancePanel.Controls.Add(modeLabel);

        var toggle = new ToggleSwitch
        {
            Name = "darkModeToggle",
            Location = new Point(120, y),
            Checked = isDarkMode,
            BackColor = contentArea.BackColor
        };
        appearancePanel.Controls.Add(toggle);
        y += 55;

        var colorSectionLabel = new Label
        {
            Text = "Theme Color",
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(0, y)
        };
        appearancePanel.Controls.Add(colorSectionLabel);
        y += 35;

        var colorPanel = new FlowLayoutPanel
        {
            Location = new Point(0, y),
            Size = new Size(400, 50),
            BackColor = contentArea.BackColor,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };

        var colors = new[]
        {
            Color.FromArgb(0, 122, 204),
            Color.FromArgb(255, 87, 87),
            Color.FromArgb(76, 175, 80),
            Color.FromArgb(156, 39, 176),
            Color.FromArgb(255, 152, 0),
            Color.FromArgb(0, 188, 212),
            Color.FromArgb(233, 30, 99),
            Color.FromArgb(121, 85, 72)
        };

        foreach (var color in colors)
        {
            var colorBtn = new Button
            {
                Size = new Size(32, 32),
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0),
                Cursor = Cursors.Hand,
                Tag = color
            };
            colorBtn.FlatAppearance.BorderSize = 2;
            colorBtn.FlatAppearance.BorderColor = color == accentColor
                ? (isDarkMode ? Color.White : Color.Black)
                : (isDarkMode ? Color.FromArgb(80, 80, 80) : Color.FromArgb(190, 190, 190));
            colorBtn.Click += (sender, e) =>
            {
                accentColor = color;
                ApplyTheme();
                foreach (Control ctrl in colorPanel.Controls)
                {
                    if (ctrl is Button btn && btn.Tag is Color c)
                    {
                        btn.FlatAppearance.BorderColor = c == accentColor
                            ? (isDarkMode ? Color.White : Color.Black)
                            : (isDarkMode ? Color.FromArgb(80, 80, 80) : Color.FromArgb(190, 190, 190));
                    }
                }
            };
            colorPanel.Controls.Add(colorBtn);
        }

        appearancePanel.Controls.Add(colorPanel);

        // --- Default Browser Panel ---
        defaultBrowserContentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = contentArea.BackColor,
            Visible = false
        };

        int dbY = 0;
        var dbTitle = new Label
        {
            Text = "Default Browser",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(0, dbY)
        };
        defaultBrowserContentPanel.Controls.Add(dbTitle);
        dbY += 50;

        var dbStatusLabel = new Label
        {
            Text = "Default browser status",
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(0, dbY)
        };
        defaultBrowserContentPanel.Controls.Add(dbStatusLabel);
        dbY += 40;

        var dbCard = new Panel
        {
            Location = new Point(0, dbY),
            Size = new Size(560, 60),
            BackColor = isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(235, 235, 235),
            BorderStyle = BorderStyle.None,
            Padding = new Padding(15)
        };

        var dbCardText = new Label
        {
            Text = IsDefaultBrowser() ? "Nova Browser is your default browser" : "Nova Browser is not your default browser",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(15, 18)
        };
        dbCard.Controls.Add(dbCardText);

        var dbButton = new Button
        {
            Text = "Make default",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 30),
            Location = new Point(440, 15),
            BackColor = accentColor,
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        dbButton.FlatAppearance.BorderSize = 0;
        dbButton.Click += (sender, e) =>
        {
            try
            {
                RegisterAsDefaultBrowser();
                Process.Start(new ProcessStartInfo("ms-settings:default-apps") { UseShellExecute = true });
                MessageBox.Show(
                    "Nova Browser has been registered with Windows.\n\nPlease find 'Nova Browser' in Windows Settings and set it as your default browser for web links.",
                    "Default Browser",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                dbCardText.Text = IsDefaultBrowser() ? "Nova Browser is your default browser" : "Nova Browser is not your default browser";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to register: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        dbCard.Controls.Add(dbButton);
        defaultBrowserContentPanel.Controls.Add(dbCard);

        // --- Home Page Panel ---
        homePageContentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = contentArea.BackColor,
            Visible = false
        };

        int hpY = 0;
        var hpTitle = new Label
        {
            Text = "Home Page",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(0, hpY)
        };
        homePageContentPanel.Controls.Add(hpTitle);
        hpY += 50;

        var hpCard = new Panel
        {
            Location = new Point(0, hpY),
            Size = new Size(560, 220),
            BackColor = isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(235, 235, 235),
            BorderStyle = BorderStyle.None,
            Padding = new Padding(15)
        };

        var hpToggleLabel = new Label
        {
            Text = "Show home button on the toolbar",
            Font = new Font("Segoe UI", 11f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(15, 15)
        };
        hpCard.Controls.Add(hpToggleLabel);

        var homeToggle = new ToggleSwitch
        {
            Name = "homeToggle",
            Location = new Point(460, 12),
            Checked = showHomeButton,
            BackColor = hpCard.BackColor
        };
        homeToggle.CheckedChanged += (sender, e) =>
        {
            showHomeButton = homeToggle.Checked;
            btnHome.Visible = showHomeButton;
        };
        hpCard.Controls.Add(homeToggle);

        var hpSubLabel = new Label
        {
            Text = "Set what the home button opens below:",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.FromArgb(180, 180, 180) : Color.FromArgb(100, 100, 100),
            AutoSize = true,
            Location = new Point(15, 45)
        };
        hpCard.Controls.Add(hpSubLabel);

        var rbNewTab = new RadioButton
        {
            Text = "New tab page",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            BackColor = hpCard.BackColor,
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Location = new Point(15, 75),
            Checked = homePageIsNewTab
        };
        hpCard.Controls.Add(rbNewTab);

        var rbCustom = new RadioButton
        {
            Text = "Set custom site",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            BackColor = hpCard.BackColor,
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Location = new Point(15, 105),
            Checked = !homePageIsNewTab
        };
        hpCard.Controls.Add(rbCustom);

        var hpUrlBox = new TextBox
        {
            Text = customHomeUrl,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            BackColor = isDarkMode ? Color.FromArgb(48, 48, 48) : Color.FromArgb(250, 250, 250),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(35, 135),
            Size = new Size(480, 25)
        };
        hpUrlBox.TextChanged += (sender, e) =>
        {
            customHomeUrl = hpUrlBox.Text.Trim();
        };
        hpCard.Controls.Add(hpUrlBox);

        rbNewTab.CheckedChanged += (sender, e) =>
        {
            if (rbNewTab.Checked)
            {
                homePageIsNewTab = true;
                hpUrlBox.Enabled = false;
            }
        };
        rbCustom.CheckedChanged += (sender, e) =>
        {
            if (rbCustom.Checked)
            {
                homePageIsNewTab = false;
                hpUrlBox.Enabled = true;
            }
        };
        hpUrlBox.Enabled = !homePageIsNewTab;

        homePageContentPanel.Controls.Add(hpCard);

        toggle.CheckedChanged += (sender, e) =>
        {
            isDarkMode = toggle.Checked;
            ApplyTheme();
            sidebar.BackColor = isDarkMode ? Color.FromArgb(28, 28, 28) : Color.FromArgb(230, 230, 230);
            categoryList.BackColor = sidebar.BackColor;
            categoryList.ForeColor = isDarkMode ? Color.White : Color.Black;
            contentArea.BackColor = isDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);
            appearancePanel.BackColor = contentArea.BackColor;
            foreach (Control ctrl in appearancePanel.Controls)
            {
                if (ctrl is Label lbl) lbl.ForeColor = isDarkMode ? Color.White : Color.Black;
                else if (ctrl is ToggleSwitch ts) ts.BackColor = contentArea.BackColor;
            }
            foreach (Control ctrl in colorPanel.Controls)
            {
                if (ctrl is Button btn && btn.Tag is Color)
                {
                    btn.FlatAppearance.BorderColor = isDarkMode ? Color.FromArgb(80, 80, 80) : Color.FromArgb(190, 190, 190);
                }
            }
            if (defaultBrowserContentPanel != null)
            {
                defaultBrowserContentPanel.BackColor = contentArea.BackColor;
                foreach (Control ctrl in defaultBrowserContentPanel.Controls)
                {
                    if (ctrl is Label lbl) lbl.ForeColor = isDarkMode ? Color.White : Color.Black;
                    else if (ctrl is Panel p)
                    {
                        p.BackColor = isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(235, 235, 235);
                        foreach (Control child in p.Controls)
                        {
                            if (child is Label lbl2) lbl2.ForeColor = isDarkMode ? Color.White : Color.Black;
                        }
                    }
                }
            }
            if (homePageContentPanel != null)
            {
                homePageContentPanel.BackColor = contentArea.BackColor;
                foreach (Control ctrl in homePageContentPanel.Controls)
                {
                    if (ctrl is Label lbl) lbl.ForeColor = isDarkMode ? Color.White : Color.Black;
                    else if (ctrl is Panel p)
                    {
                        p.BackColor = isDarkMode ? Color.FromArgb(40, 40, 40) : Color.FromArgb(235, 235, 235);
                        foreach (Control child in p.Controls)
                        {
                            if (child is Label lbl2) lbl2.ForeColor = isDarkMode ? Color.White : Color.Black;
                            else if (child is RadioButton rb)
                            {
                                rb.ForeColor = isDarkMode ? Color.White : Color.Black;
                                rb.BackColor = p.BackColor;
                            }
                            else if (child is TextBox tb)
                            {
                                tb.BackColor = isDarkMode ? Color.FromArgb(48, 48, 48) : Color.FromArgb(250, 250, 250);
                                tb.ForeColor = isDarkMode ? Color.White : Color.Black;
                            }
                            else if (child is ToggleSwitch ts) ts.BackColor = p.BackColor;
                        }
                    }
                }
            }
        };

        categoryList.SelectedIndexChanged += (sender, e) =>
        {
            appearancePanel.Visible = categoryList.SelectedIndex == 0;
            if (defaultBrowserContentPanel != null)
                defaultBrowserContentPanel.Visible = categoryList.SelectedIndex == 1;
            if (homePageContentPanel != null)
                homePageContentPanel.Visible = categoryList.SelectedIndex == 2;
        };

        contentArea.Controls.Add(appearancePanel);
        if (defaultBrowserContentPanel != null)
            contentArea.Controls.Add(defaultBrowserContentPanel);
        if (homePageContentPanel != null)
            contentArea.Controls.Add(homePageContentPanel);

        settingsPanel.Controls.Add(contentArea);
        settingsPanel.Controls.Add(sidebar);
        contentPanel.Controls.Add(settingsPanel);
    }

    private bool IsDefaultBrowser()
    {
        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(@"http\shell\open\command");
            string? value = key?.GetValue("")?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                string exePath = Application.ExecutablePath;
                return value.Contains(exePath, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { }
        return false;
    }

    private void RegisterAsDefaultBrowser()
    {
        string exePath = Application.ExecutablePath;

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications"))
        {
            key.SetValue("NovaBrowser", @"Software\NovaBrowser\Capabilities");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\NovaBrowser\Capabilities"))
        {
            key.SetValue("ApplicationDescription", "Nova Browser");
            key.SetValue("ApplicationName", "NovaBrowser");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\NovaBrowser\Capabilities\URLAssociations"))
        {
            key.SetValue("http", "NovaBrowserHTML");
            key.SetValue("https", "NovaBrowserHTML");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\NovaBrowser\Capabilities\FileAssociations"))
        {
            key.SetValue(".html", "NovaBrowserHTML");
            key.SetValue(".htm", "NovaBrowserHTML");
            key.SetValue(".shtml", "NovaBrowserHTML");
            key.SetValue(".xht", "NovaBrowserHTML");
            key.SetValue(".xhtml", "NovaBrowserHTML");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\NovaBrowserHTML"))
        {
            key.SetValue("", "NovaBrowser HTML Document");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\NovaBrowserHTML\DefaultIcon"))
        {
            key.SetValue("", $"\"{exePath}\",0");
        }

        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\NovaBrowserHTML\shell\open\command"))
        {
            key.SetValue("", $"\"{exePath}\" \"%1\"");
        }

        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_TRANSIENTWINDOW = 3;

    private void EnableTransparentTitleBar()
    {
        try
        {
            int dark = isDarkMode ? 1 : 0;
            DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            int backdrop = DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(this.Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
        }
        catch { }
    }

    private class TransparentStatusStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            // Skip drawing background so parent shows through
        }
    }

    private async void AddTab(string url)
    {
        Microsoft.Web.WebView2.WinForms.WebView2 webView;

        if (tabs.Count == 0)
        {
            webView = webView21;
            webView.Visible = false;
            webView.DefaultBackgroundColor = isDarkMode ? Color.FromArgb(18, 18, 18) : Color.FromArgb(250, 250, 250);
        }
        else
        {
            webView = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                CreationProperties = null,
                DefaultBackgroundColor = isDarkMode ? Color.FromArgb(18, 18, 18) : Color.FromArgb(250, 250, 250),
                Dock = DockStyle.Fill,
                Visible = false
            };
            contentPanel.Controls.Add(webView);
        }

        var tabHeader = new Panel
        {
            AutoSize = false,
            BackColor = isDarkMode ? Color.FromArgb(28, 28, 28) : Color.FromArgb(230, 230, 230),
            Margin = new Padding(0, 0, 4, 0),
            Padding = new Padding(4, 4, 4, 4),
            Height = 54,
            Width = 220
        };

        var tabButton = new Button
        {
            Text = "New Tab",
            AutoSize = false,
            BackColor = isDarkMode ? Color.FromArgb(36, 36, 36) : Color.FromArgb(235, 235, 235),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(12, 10, 32, 10),
            Margin = new Padding(0),
            Height = 46,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Image = defaultFavicon,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            AutoEllipsis = true,
            Font = new Font(Font.FontFamily, 9.0f, FontStyle.Regular)
        };
        tabButton.FlatAppearance.BorderSize = 1;
        tabButton.FlatAppearance.BorderColor = Color.Gray;
        tabButton.SizeChanged += (s, e) => ApplyRoundedTopCorners(tabButton);

        tabHeader.Controls.Add(tabButton);
        tabHeader.SizeChanged += (s, e) => ApplyRoundedTabHeader(tabHeader);

        if (tabsPanel.Controls.Contains(btnNewTab))
        {
            var insertIndex = tabsPanel.Controls.GetChildIndex(btnNewTab);
            tabsPanel.Controls.Add(tabHeader);
            tabsPanel.Controls.SetChildIndex(tabHeader, insertIndex);
        }
        else
        {
            tabsPanel.Controls.Add(tabHeader);
        }

        var tab = new BrowserTab("New Tab", url, webView, tabHeader, tabButton);
        tabs.Add(tab);

        tabButton.MouseClick += (sender, e) =>
        {
            if (sender is not Button btn)
                return;
            var closeRect = new Rectangle(btn.ClientSize.Width - 28, 0, 28, btn.ClientSize.Height);
            if (closeRect.Contains(e.Location) && e.Button == MouseButtons.Left)
            {
                CloseTab(tab);
            }
            else
            {
                SetActiveTab(tab);
            }
        };
        tabButton.Paint += (sender, e) =>
        {
            if (sender is not Button btn)
                return;
            DrawTabButtonExtras(btn, e, tab);
        };

        UpdateTabHeaderSizes();
        SetActiveTab(tab);
        await InitializeWebViewAsync(tab);
    }

    private async Task InitializeWebViewAsync(BrowserTab tab)
    {
        try
        {
            if (_sharedWebViewEnvironment == null)
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "NovaBrowser", "WebView2Data");
                Directory.CreateDirectory(userDataFolder);
                _sharedWebViewEnvironment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder);
            }
            await tab.View.EnsureCoreWebView2Async(_sharedWebViewEnvironment);
        }
        catch (COMException ex) when ((uint)ex.HResult == 0x800700AA)
        {
            // User data folder is locked by another instance; use a per-process folder
            var fallbackFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NovaBrowser", "WebView2Data",
                $"instance-{Process.GetCurrentProcess().Id}");
            Directory.CreateDirectory(fallbackFolder);
            _sharedWebViewEnvironment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: fallbackFolder);
            await tab.View.EnsureCoreWebView2Async(_sharedWebViewEnvironment);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize the browser engine.\n\n{ex.Message}\n\n" +
                "Please ensure Microsoft Edge WebView2 Runtime is installed.",
                "Nova Browser - Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!initializedWebViews.Add(tab.View))
        {
            tab.NavigationStartedAt = DateTime.UtcNow;
            tab.View.Source = new Uri(tab.Url);
            return;
        }
        tab.View.CoreWebView2.NavigationStarting += (sender, e) => TabNavigationStarting(tab, e);
        tab.View.CoreWebView2.NavigationCompleted += (sender, e) => TabNavigationCompleted(tab, e);
        tab.View.CoreWebView2.HistoryChanged += (sender, e) =>
        {
            if (tab.View.Source != null)
                tab.Url = tab.View.Source.AbsoluteUri;
            if (tab == activeTab)
                txtAddress.Text = tab.Url;
        };
        tab.View.CoreWebView2.DocumentTitleChanged += (sender, e) =>
        {
            var newTitle = tab.View.CoreWebView2?.DocumentTitle;
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                tab.Title = newTitle;
                tab.TabButton.Text = newTitle;
            }
        };
        tab.View.CoreWebView2.NewWindowRequested += (sender, e) =>
        {
            e.Handled = true;
            if (!string.IsNullOrWhiteSpace(e.Uri))
            {
                AddTab(e.Uri);
            }
        };
        tab.NavigationStartedAt = DateTime.UtcNow;
        tab.View.Source = new Uri(tab.Url);
    }

    private void SetActiveTab(BrowserTab tab)
    {
        activeTab = tab;

        if (tab.IsSettingsTab)
        {
            settingsPanel.Visible = true;
            settingsPanel.BringToFront();
            foreach (var item in tabs)
            {
                item.View.Visible = false;
                var backColor = item == tab ? accentColor : (isDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(210, 210, 210));
                item.TabButton.BackColor = backColor;
                item.TabButton.ForeColor = item == tab ? Color.White : (isDarkMode ? Color.White : Color.Black);
                item.HeaderPanel.BackColor = backColor;
            }
            txtAddress.Text = string.Empty;
            toolStripStatusLabel.Text = "Settings";
            toolStripLoadTimeLabel.Text = "Website Loaded In: --";
            ApplyTheme();
            UpdateTabHeaderSizes();
            return;
        }

        settingsPanel.Visible = false;
        foreach (var item in tabs)
        {
            item.View.Visible = item == tab;
            var backColor = item == tab ? accentColor : (isDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(210, 210, 210));
            item.TabButton.BackColor = backColor;
            item.TabButton.ForeColor = item == tab ? Color.White : (isDarkMode ? Color.White : Color.Black);
            item.HeaderPanel.BackColor = backColor;
        }

        txtAddress.Text = tab.Url;
        toolStripStatusLabel.Text = "Ready";
        toolStripLoadTimeLabel.Text = tab.HasLoadDuration ? $"Website Loaded In: {FormatLoadTime(tab.LastLoadDuration)}" : "Website Loaded In: --";
        ApplyTheme();
        UpdateTabHeaderSizes();
    }

    private void UpdateTabHeaderSizes()
    {
        if (tabsPanel.ClientSize.Width <= 0 || tabs.Count == 0)
            return;

        var newTabWidth = btnNewTab?.Width ?? 0;
        var newTabMarginH = btnNewTab?.Margin.Horizontal ?? 0;
        var tabMarginH = tabs[0].HeaderPanel.Margin.Horizontal;

        var width = (tabsPanel.ClientSize.Width - tabs.Count * tabMarginH - newTabWidth - newTabMarginH) / tabs.Count;
        if (width > 420)
            width = 420;
        if (width < 0)
            width = 0;

        foreach (var tab in tabs)
        {
            tab.HeaderPanel.Width = width;
            ApplyRoundedTabHeader(tab.HeaderPanel);
            ApplyRoundedTopCorners(tab.TabButton);
        }
    }

    private static void ApplyRoundedTabHeader(Control tabHeader)
    {
        if (tabHeader.Width <= 0 || tabHeader.Height <= 0)
            return;

        const int radius = 12;
        var rect = new Rectangle(0, 0, tabHeader.Width, tabHeader.Height);
        using var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90);
        path.AddLine(rect.Left + radius, rect.Top, rect.Right - radius, rect.Top);
        path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90);
        path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom);
        path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
        path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + radius);
        path.CloseFigure();
        tabHeader.Region = new Region(path);
    }

    private static void ApplyRoundedTopCorners(Control control)
    {
        if (control.Width <= 0 || control.Height <= 0)
            return;

        const int radius = 12;
        var rect = new Rectangle(0, 0, control.Width, control.Height);
        using var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90);
        path.AddLine(rect.Left + radius, rect.Top, rect.Right - radius, rect.Top);
        path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90);
        path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom);
        path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
        path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top + radius);
        path.CloseFigure();
        control.Region = new Region(path);
    }

    private static void DrawTabButtonExtras(Button btn, PaintEventArgs e, BrowserTab tab)
    {
        if (tab.IsPlayingAudio && tab.AudioBlinkOn)
        {
            DrawSpeakerIcon(btn, e.Graphics, Color.Red);
        }

        const int closeWidth = 28;
        var closeRect = new Rectangle(btn.ClientSize.Width - closeWidth - 2, (btn.ClientSize.Height - closeWidth) / 2, closeWidth, closeWidth);

        using (var font = new Font("Segoe UI", 14f, FontStyle.Regular))
        using (var brush = new SolidBrush(btn.ForeColor))
        {
            var textSize = e.Graphics.MeasureString("×", font);
            var x = closeRect.X + (closeRect.Width - textSize.Width) / 2;
            var y = closeRect.Y + (closeRect.Height - textSize.Height) / 2;
            e.Graphics.DrawString("×", font, brush, x, y);
        }
    }

    private static void DrawSpeakerIcon(Button btn, Graphics g, Color color)
    {
        const int iconSize = 18;
        var rect = new Rectangle(
            btn.ClientSize.Width - 28 - iconSize - 8,
            (btn.ClientSize.Height - iconSize) / 2,
            iconSize, iconSize);

        using var brush = new SolidBrush(color);

        int bodyW = 5;
        int bodyH = 7;
        int bodyX = rect.X + 1;
        int bodyY = rect.Y + (rect.Height - bodyH) / 2;
        g.FillRectangle(brush, bodyX, bodyY, bodyW, bodyH);

        var conePoints = new Point[]
        {
            new Point(bodyX + bodyW, bodyY + 1),
            new Point(bodyX + bodyW + 5, bodyY - 2),
            new Point(bodyX + bodyW + 5, bodyY + bodyH + 2),
            new Point(bodyX + bodyW, bodyY + bodyH - 1)
        };
        g.FillPolygon(brush, conePoints);

        using var pen = new Pen(color, 1);
        g.DrawArc(pen, bodyX + bodyW + 3, bodyY - 1, 5, 9, -40, 80);
    }

    private void CloseTab(BrowserTab tab)
    {
        if (tab.IsSettingsTab)
        {
            settingsPanel.Visible = false;
        }

        if (!ReferenceEquals(tab.View, webView21))
        {
            tab.View.Dispose();
        }
        else
        {
            tab.View.Visible = false;
        }

        tabsPanel.Controls.Remove(tab.HeaderPanel);
        tabs.Remove(tab);

        if (activeTab == tab)
        {
            if (tabs.Count > 0)
                SetActiveTab(tabs.Last());
            else
                AddTab(GetNewTabUrl());
        }
        else
        {
            UpdateTabHeaderSizes();
        }
    }

    private static string FormatLoadTime(TimeSpan elapsed)
    {
        if (elapsed.TotalMilliseconds < 1000)
            return $"{Math.Max(1, (int)elapsed.TotalMilliseconds)} ms";

        return $"{elapsed.TotalSeconds:0.00} s";
    }

    private void TabNavigationStarting(BrowserTab tab, CoreWebView2NavigationStartingEventArgs e)
    {
        tab.NavigationStartedAt = DateTime.UtcNow;
        if (tab == activeTab)
        {
            toolStripLoadTimeLabel.Text = "Website Loaded In: --";
            toolStripStatusLabel.Text = "Loading...";
        }
    }

    private void TabNavigationCompleted(BrowserTab tab, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (tab.View.Source != null)
            tab.Url = tab.View.Source.AbsoluteUri;

        var title = tab.View.CoreWebView2?.DocumentTitle;
        if (!string.IsNullOrWhiteSpace(title))
        {
            tab.Title = title;
            tab.TabButton.Text = title;
        }

        tab.TabButton.Image = GetBookmarkIcon(tab.Url);

        var elapsed = DateTime.UtcNow - tab.NavigationStartedAt;
        tab.LastLoadDuration = elapsed;
        tab.HasLoadDuration = true;
        tab.NavigationSucceeded = e.IsSuccess;

        if (tab == activeTab)
        {
            txtAddress.Text = tab.Url;
            toolStripStatusLabel.Text = e.IsSuccess ? "Done" : $"Navigation failed ({e.WebErrorStatus})";
            toolStripLoadTimeLabel.Text = $"Website Loaded In: {FormatLoadTime(elapsed)}";
        }

        UpdateTabHeaderSizes();
    }

    private void UpdateTabHeader(BrowserTab tab)
    {
        tab.TabButton.Text = tab.Title;
    }

    private void LoadBookmarks()
    {
        try
        {
            if (File.Exists(bookmarksFilePath))
            {
                var json = File.ReadAllText(bookmarksFilePath);
                var loaded = JsonSerializer.Deserialize<List<Bookmark>>(json);
                if (loaded != null)
                {
                    // Ensure unique bookmarks by URL (case-insensitive), keep first occurrence
                    var unique = loaded
                        .GroupBy(b => NormalizeUrl(b.Url), StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First() with { Url = NormalizeUrl(g.First().Url) })
                        .ToList();
                    bookmarks.AddRange(unique);
                }
            }
        }
        catch
        {
            // ignore load errors
        }
    }

    private void SaveBookmarks()
    {
        try
        {
            // Ensure we persist a URL-unique list (first occurrence wins)
            var unique = bookmarks
                .GroupBy(b => NormalizeUrl(b.Url), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First() with { Url = NormalizeUrl(g.First().Url) })
                .ToList();
            // Replace in-memory list with deduped, normalized list to keep UI consistent
            bookmarks.Clear();
            bookmarks.AddRange(unique);

            var json = JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(bookmarksFilePath, json);
        }
        catch
        {
            // ignore save errors
        }
    }

    private Image GetBookmarkIcon(string url)
    {
        try
        {
            var uri = new Uri(NormalizeUrl(url));
            var cacheKey = uri.Host;
            if (faviconCache.TryGetValue(cacheKey, out var cachedImage))
                return cachedImage;

            lock (faviconLock)
            {
                if (!faviconLoading.Contains(cacheKey))
                {
                    faviconLoading.Add(cacheKey);
                    LoadFaviconAsync(uri.Scheme, uri.Host, cacheKey);
                }
            }
        }
        catch
        {
            // ignore favicon parsing errors
        }

        return defaultFavicon;
    }

    private static Image CreateDefaultFavicon()
    {
        var faviconPath = Path.Combine(Application.StartupPath, "images", "favicon.ico");
        if (File.Exists(faviconPath))
        {
            try
            {
                using var stream = File.OpenRead(faviconPath);
                return Image.FromStream(stream);
            }
            catch { }
        }
        var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.FromArgb(64, 64, 64));
        g.FillEllipse(brush, 0, 0, 15, 15);
        using var pen = new Pen(Color.White, 2);
        g.DrawEllipse(pen, 1, 1, 13, 13);
        return bitmap;
    }

    private async void LoadFaviconAsync(string scheme, string host, string cacheKey)
    {
        try
        {
            var faviconUrl = new UriBuilder(scheme, host, -1, "/favicon.ico").Uri;
            using var response = await httpClient.GetAsync(faviconUrl);
            if (!response.IsSuccessStatusCode)
                return;

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var iconImage = Image.FromStream(stream);
            var favicon = new Bitmap(iconImage, new Size(16, 16));
            lock (faviconLock)
            {
                faviconCache[cacheKey] = favicon;
            }
            if (!IsDisposed)
                Invoke((Action)(() =>
                {
                    RefreshBookmarkButtons();
                    UpdateTabHeaderSizes();
                }));
        }
        catch
        {
            // ignore favicon download failures
        }
        finally
        {
            lock (faviconLock)
            {
                faviconLoading.Remove(cacheKey);
            }
        }
    }

    private void RefreshBookmarkButtons()
    {
        bookmarksPanel.Controls.Clear();

        if (moreButton == null)
        {
            moreButton = new Button
            {
                Text = "⋯",
                AutoSize = true,
                BackColor = isDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(224, 224, 224),
                ForeColor = isDarkMode ? Color.White : Color.Black,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(4, 4, 0, 4)
            };
            moreButton.FlatAppearance.BorderSize = 1;
            moreButton.FlatAppearance.BorderColor = Color.Gray;
            moreButton.Click += (s, e) =>
            {
                if (overflowMenu != null && overflowMenu.Items.Count > 0)
                {
                    overflowMenu.Show(moreButton, new Point(0, moreButton.Height));
                }
            };
        }

        overflowMenu = new ContextMenuStrip();

        // Normalize and dedupe the live bookmark list before rendering
        var bookmarkList = bookmarks
            .GroupBy(b => NormalizeUrl(b.Url), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First() with { Url = NormalizeUrl(g.First().Url) })
            .ToList();
        if (bookmarkList.Count != bookmarks.Count)
        {
            bookmarks.Clear();
            bookmarks.AddRange(bookmarkList);
            SaveBookmarks();
        }

        var buttons = new List<Button>();
        foreach (var bookmark in bookmarkList)
        {
            var button = new Button
            {
                Text = bookmark.Title,
                Image = GetBookmarkIcon(bookmark.Url),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                AutoSize = true,
                BackColor = isDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(224, 224, 224),
                ForeColor = isDarkMode ? Color.White : Color.Black,
                FlatStyle = FlatStyle.Flat,
                Padding = new Padding(4, 3, 8, 3),
                Margin = new Padding(4, 4, 0, 4),
                Tag = bookmark.Url
            };
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.Gray;
            button.Click += (sender, e) => Navigate(bookmark.Url);
            button.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right && sender is Button btn && btn.Tag is string url)
                {
                    var bm = bookmarks.FirstOrDefault(b => string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase));
                    if (bm != null)
                        ShowBookmarkContextMenu(btn, bm, e.Location);
                }
            };
            buttons.Add(button);
        }

        // Compute available width inside the panel
        var availableWidth = bookmarksPanel.ClientSize.Width;
        var moreButtonWidth = moreButton.PreferredSize.Width + moreButton.Margin.Left + moreButton.Margin.Right;

        var used = 0;
        var overflowStart = -1;
        for (int i = 0; i < buttons.Count; i++)
        {
            var b = buttons[i];
            var needed = b.PreferredSize.Width + b.Margin.Left + b.Margin.Right;
            if (used + needed > availableWidth - moreButtonWidth)
            {
                overflowStart = i;
                break;
            }
            used += needed;
        }

        if (overflowStart == -1)
        {
            foreach (var b in buttons)
                bookmarksPanel.Controls.Add(b);
        }
        else
        {
            for (int i = 0; i < overflowStart; i++)
            {
                bookmarksPanel.Controls.Add(buttons[i]);
            }

            for (int i = overflowStart; i < buttons.Count; i++)
            {
                var url = buttons[i].Tag as string;
                if (string.IsNullOrEmpty(url))
                    continue;
                var bm = bookmarks.FirstOrDefault(b => string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase));
                if (bm == null)
                    continue;

                var item = new ToolStripMenuItem(bm.Title) { Tag = url };
                item.Click += (s, e) => Navigate(bm.Url);
                item.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right && s is ToolStripMenuItem it && it.Tag is string u)
                    {
                        var contextMenu = new ContextMenuStrip();

                        var openItem = new ToolStripMenuItem("Open") { Tag = u };
                        openItem.Click += (sender, args) =>
                        {
                            if (sender is ToolStripMenuItem openIt && openIt.Tag is string openUrl)
                            {
                                var bkm = bookmarks.FirstOrDefault(b => string.Equals(b.Url, openUrl, StringComparison.OrdinalIgnoreCase));
                                if (bkm != null) Navigate(bkm.Url);
                            }
                        };

                        var renameItem = new ToolStripMenuItem("Rename") { Tag = u };
                        renameItem.Click += (sender, args) =>
                        {
                            if (sender is ToolStripMenuItem renameIt && renameIt.Tag is string renameUrl)
                            {
                                var bkm = bookmarks.FirstOrDefault(b => string.Equals(b.Url, renameUrl, StringComparison.OrdinalIgnoreCase));
                                if (bkm != null)
                                {
                                    var newTitle = PromptForText("Rename Bookmark", "Enter new bookmark title:", bkm.Title);
                                    if (!string.IsNullOrWhiteSpace(newTitle) && newTitle != bkm.Title)
                                    {
                                        var idx = bookmarks.FindIndex(b => string.Equals(b.Url, bkm.Url, StringComparison.OrdinalIgnoreCase));
                                        if (idx >= 0)
                                        {
                                            bookmarks[idx] = bookmarks[idx] with { Title = newTitle };
                                            SaveBookmarks();
                                            RefreshBookmarkButtons();
                                            toolStripStatusLabel.Text = "Bookmark renamed.";
                                        }
                                    }
                                }
                            }
                        };

                        var deleteItem = new ToolStripMenuItem("Delete") { Tag = u };
                        deleteItem.Click += (sender, args) =>
                        {
                            if (sender is ToolStripMenuItem deleteIt && deleteIt.Tag is string deleteUrl)
                            {
                                var idx = bookmarks.FindIndex(b => string.Equals(b.Url, deleteUrl, StringComparison.OrdinalIgnoreCase));
                                if (idx >= 0)
                                    bookmarks.RemoveAt(idx);
                                SaveBookmarks();
                                RefreshBookmarkButtons();
                                toolStripStatusLabel.Text = "Bookmark deleted.";
                            }
                        };

                        contextMenu.Items.Add(openItem);
                        contextMenu.Items.Add(renameItem);
                        contextMenu.Items.Add(deleteItem);
                        contextMenu.Show(Cursor.Position);
                    }
                };

                overflowMenu.Items.Add(item);
            }

            bookmarksPanel.Controls.Add(moreButton);
        }
    }

    private void ShowBookmarkContextMenu(Button button, Bookmark bookmark, Point location)
    {
        var menu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Open");
        openItem.Click += (sender, e) => Navigate(bookmark.Url);
        menu.Items.Add(openItem);

        var renameItem = new ToolStripMenuItem("Rename")
        {
            Tag = bookmark.Url
        };
        renameItem.Click += (sender, e) =>
        {
            var newTitle = PromptForText("Rename Bookmark", "Enter new bookmark title:", bookmark.Title);
            if (!string.IsNullOrWhiteSpace(newTitle) && newTitle != bookmark.Title)
            {
                var idx = bookmarks.FindIndex(b => string.Equals(b.Url, bookmark.Url, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    bookmarks[idx] = bookmarks[idx] with { Title = newTitle };
                    SaveBookmarks();
                    RefreshBookmarkButtons();
                    toolStripStatusLabel.Text = "Bookmark renamed.";
                }
            }
        };
        menu.Items.Add(renameItem);

        var deleteItem = new ToolStripMenuItem("Delete")
        {
            Tag = bookmark.Url
        };
        deleteItem.Click += (sender, e) =>
        {
            var idx = bookmarks.FindIndex(b => string.Equals(b.Url, bookmark.Url, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
                bookmarks.RemoveAt(idx);
            SaveBookmarks();
            RefreshBookmarkButtons();
            toolStripStatusLabel.Text = "Bookmark deleted.";
        };
        menu.Items.Add(deleteItem);
        menu.Show(button, location);
    }

    private string? PromptForText(string title, string prompt, string defaultText)
    {
        using var dlg = new Form()
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ClientSize = new Size(400, 110),
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false
        };

        var lbl = new Label() { AutoSize = true, Text = prompt, Location = new Point(12, 10) };
        var txt = new TextBox() { Text = defaultText ?? string.Empty, Location = new Point(12, 34), Width = 360 };
        var btnOk = new Button() { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(216, 70), Width = 75 };
        var btnCancel = new Button() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(297, 70), Width = 75 };

        dlg.Controls.Add(lbl);
        dlg.Controls.Add(txt);
        dlg.Controls.Add(btnOk);
        dlg.Controls.Add(btnCancel);
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;

        var result = dlg.ShowDialog(this);
        return result == DialogResult.OK ? txt.Text.Trim() : null;
    }

    private async Task<string> GetPageTitleAsync()
    {
        try
        {
            if (activeTab?.View?.CoreWebView2 != null)
            {
                var titleJson = await activeTab.View.ExecuteScriptAsync("document.title");
                if (!string.IsNullOrWhiteSpace(titleJson))
                {
                    var title = titleJson.Trim();
                    if (title.Length >= 2 && title.StartsWith("\"") && title.EndsWith("\""))
                        title = title[1..^1];
                    return title;
                }
            }
        }
        catch
        {
            // ignore title retrieval errors
        }

        return string.Empty;
    }

    private async void btnSaveBookmark_Click(object? sender, EventArgs e)
    {
        if (activeTab?.View?.Source == null)
        {
            MessageBox.Show("No page is loaded to bookmark.", "Bookmark", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var uri = activeTab.View.Source;
        var url = NormalizeUrl(uri.AbsoluteUri);
        // Remove existing entries with the same URL (prevents accidental duplicates)
        bookmarks.RemoveAll(b => string.Equals(NormalizeUrl(b.Url), url, StringComparison.OrdinalIgnoreCase));

        var title = await GetPageTitleAsync();
        if (string.IsNullOrWhiteSpace(title))
            title = uri.Host;

        bookmarks.Add(new Bookmark(title, url));
        SaveBookmarks();
        RefreshBookmarkButtons();
        toolStripStatusLabel.Text = "Bookmark saved.";
    }

    private void btnNewTab_Click(object? sender, EventArgs e)
    {
        AddTab(GetNewTabUrl());
    }

    private void ApplyTheme()
    {
        var formBack = isDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);
        var panelBack = isDarkMode ? Color.FromArgb(34, 34, 34) : Color.FromArgb(230, 230, 230);
        var buttonBack = isDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(210, 210, 210);
        var textBack = isDarkMode ? Color.FromArgb(48, 48, 48) : Color.FromArgb(250, 250, 250);
        var foreColor = isDarkMode ? Color.White : Color.Black;

        BackColor = formBack;
        EnableTransparentTitleBar();
        topContainerPanel.BackColor = panelBack;
        tabsPanel.BackColor = panelBack;
        topPanel.BackColor = panelBack;
        bookmarksPanel.BackColor = isDarkMode ? Color.FromArgb(28, 28, 28) : Color.FromArgb(240, 240, 240);
        statusStrip1.BackColor = Color.Transparent;
        statusStrip1.Renderer = new TransparentStatusStripRenderer();
        toolStripStatusLabel.ForeColor = foreColor;
        toolStripLoadTimeLabel.ForeColor = foreColor;
        btnAbout.BackColor = panelBack;
        btnAbout.ForeColor = foreColor;
        btnBack.BackColor = buttonBack;
        btnBack.ForeColor = foreColor;
        btnForward.BackColor = buttonBack;
        btnForward.ForeColor = foreColor;
        btnRefresh.BackColor = buttonBack;
        btnRefresh.ForeColor = foreColor;
        btnHome.BackColor = buttonBack;
        btnHome.ForeColor = foreColor;
        btnHome.Visible = showHomeButton;
        btnGo.BackColor = buttonBack;
        btnGo.ForeColor = foreColor;
        btnSaveBookmark.BackColor = buttonBack;
        btnSaveBookmark.ForeColor = foreColor;
        btnNewTab.BackColor = panelBack;
        btnNewTab.ForeColor = foreColor;
        btnNewTab.FlatAppearance.MouseDownBackColor = panelBack;
        btnNewTab.FlatAppearance.MouseOverBackColor = panelBack;
        txtAddress.BackColor = textBack;
        txtAddress.ForeColor = foreColor;
        if (urlContextMenu != null)
        {
            urlContextMenu.BackColor = isDarkMode ? Color.FromArgb(48, 48, 48) : Color.FromArgb(250, 250, 250);
            urlContextMenu.ForeColor = foreColor;
            foreach (ToolStripItem item in urlContextMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.BackColor = urlContextMenu.BackColor;
                    menuItem.ForeColor = foreColor;
                }
            }
        }
        btnSettings.BackColor = panelBack;
        btnSettings.ForeColor = foreColor;

        foreach (var webView in contentPanel.Controls.OfType<Microsoft.Web.WebView2.WinForms.WebView2>())
        {
            webView.DefaultBackgroundColor = isDarkMode ? Color.FromArgb(18, 18, 18) : Color.FromArgb(250, 250, 250);
        }

        settingsPanel.BackColor = isDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);
        UpdateSettingsPanelTheme(settingsPanel);

        foreach (Control child in bookmarksPanel.Controls)
        {
            if (child is Button button)
            {
                button.BackColor = buttonBack;
                button.ForeColor = foreColor;
            }
        }

        var tabHeaderBack = isDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(230, 230, 230);
        var activeTabBack = accentColor;

        foreach (var tab in tabs)
        {
            tab.HeaderPanel.BackColor = tabHeaderBack;
            tab.TabButton.BackColor = tab == activeTab ? activeTabBack : buttonBack;
            tab.TabButton.ForeColor = tab == activeTab ? Color.White : foreColor;
            tab.TabButton.FlatAppearance.BorderSize = tab == activeTab ? 1 : 0;
            tab.TabButton.FlatAppearance.BorderColor = tab.TabButton.BackColor;
            tab.TabButton.Image = GetBookmarkIcon(tab.Url);
            tab.TabButton.ImageAlign = ContentAlignment.MiddleLeft;
            tab.TabButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            ApplyRoundedTopCorners(tab.TabButton);
        }
    }

    private void UpdateSettingsPanelTheme(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl is Label lbl)
            {
                lbl.ForeColor = isDarkMode ? Color.White : Color.Black;
            }
            else if (ctrl is ListBox lb)
            {
                lb.BackColor = isDarkMode ? Color.FromArgb(28, 28, 28) : Color.FromArgb(230, 230, 230);
                lb.ForeColor = isDarkMode ? Color.White : Color.Black;
                lb.Invalidate();
            }
            else if (ctrl is ToggleSwitch ts)
            {
                ts.BackColor = settingsPanel.BackColor;
                ts.Invalidate();
            }
            else if (ctrl is Panel || ctrl is FlowLayoutPanel)
            {
                ctrl.BackColor = settingsPanel.BackColor;
                UpdateSettingsPanelTheme(ctrl);
            }
        }
    }

    private void btnSettings_Click(object? sender, EventArgs e)
    {
        var existing = tabs.FirstOrDefault(t => t.IsSettingsTab);
        if (existing != null)
        {
            SetActiveTab(existing);
            return;
        }

        AddSettingsTab();
    }

    private void AddSettingsTab()
    {
        var webView = new Microsoft.Web.WebView2.WinForms.WebView2
        {
            Dock = DockStyle.Fill,
            Visible = false
        };
        contentPanel.Controls.Add(webView);

        var tabHeader = new Panel
        {
            AutoSize = false,
            BackColor = isDarkMode ? Color.FromArgb(28, 28, 28) : Color.FromArgb(230, 230, 230),
            Margin = new Padding(0, 0, 4, 0),
            Padding = new Padding(4, 4, 4, 4),
            Height = 54,
            Width = 120
        };

        var tabButton = new Button
        {
            Text = "Settings",
            AutoSize = false,
            BackColor = isDarkMode ? Color.FromArgb(36, 36, 36) : Color.FromArgb(235, 235, 235),
            ForeColor = isDarkMode ? Color.White : Color.Black,
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(12, 10, 32, 10),
            Margin = new Padding(0),
            Height = 46,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Font = new Font(Font.FontFamily, 9.0f, FontStyle.Regular)
        };
        tabButton.FlatAppearance.BorderSize = 1;
        tabButton.FlatAppearance.BorderColor = Color.Gray;
        tabButton.SizeChanged += (s, e) => ApplyRoundedTopCorners(tabButton);

        tabHeader.Controls.Add(tabButton);
        tabHeader.SizeChanged += (s, e) => ApplyRoundedTabHeader(tabHeader);

        if (tabsPanel.Controls.Contains(btnNewTab))
        {
            var insertIndex = tabsPanel.Controls.GetChildIndex(btnNewTab);
            tabsPanel.Controls.Add(tabHeader);
            tabsPanel.Controls.SetChildIndex(tabHeader, insertIndex);
        }
        else
        {
            tabsPanel.Controls.Add(tabHeader);
        }

        var tab = new BrowserTab("Settings", string.Empty, webView, tabHeader, tabButton)
        {
            IsSettingsTab = true
        };
        tabs.Add(tab);

        tabButton.MouseClick += (sender, e) =>
        {
            if (sender is not Button btn)
                return;
            var closeRect = new Rectangle(btn.ClientSize.Width - 28, 0, 28, btn.ClientSize.Height);
            if (closeRect.Contains(e.Location) && e.Button == MouseButtons.Left)
            {
                CloseTab(tab);
            }
            else
            {
                SetActiveTab(tab);
            }
        };
        tabButton.Paint += (sender, e) =>
        {
            if (sender is not Button btn)
                return;
            DrawTabButtonExtras(btn, e, tab);
        };

        UpdateTabHeaderSizes();
        SetActiveTab(tab);
    }

    private void Navigate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (!url.Contains("://"))
            url = "https://" + url;

        try
        {
            if (activeTab?.View != null)
                activeTab.View.Source = new Uri(url);
        }
        catch
        {
            MessageBox.Show("The URL is invalid. Please enter a valid web address.", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddToAddressHistory(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        addressHistory.Remove(url);
        addressHistory.Insert(0, url);
        while (addressHistory.Count > 10)
            addressHistory.RemoveAt(addressHistory.Count - 1);

        RefreshAddressHistoryDropdown();
    }

    private void RefreshAddressHistoryDropdown()
    {
        txtAddress.Items.Clear();
        foreach (var item in addressHistory)
            txtAddress.Items.Add(item);
    }

    private void RemoveHistoryItem(int index)
    {
        if (index >= 0 && index < addressHistory.Count)
        {
            addressHistory.RemoveAt(index);
            RefreshAddressHistoryDropdown();
        }
    }

    private void btnGo_Click(object? sender, EventArgs e)
    {
        var url = txtAddress.Text.Trim();
        AddToAddressHistory(url);
        Navigate(url);
    }

    private void btnBack_Click(object? sender, EventArgs e)
    {
        if (activeTab?.View?.CanGoBack == true)
            activeTab.View.GoBack();
    }

    private void btnForward_Click(object? sender, EventArgs e)
    {
        if (activeTab?.View?.CanGoForward == true)
            activeTab.View.GoForward();
    }

    private void btnRefresh_Click(object? sender, EventArgs e)
    {
        activeTab?.View?.Reload();
    }

    private void btnHome_Click(object? sender, EventArgs e)
    {
        if (homePageIsNewTab)
        {
            AddTab("about:blank");
        }
        else
        {
            if (activeTab != null && !activeTab.IsSettingsTab)
            {
                activeTab.View.Source = new Uri(customHomeUrl);
            }
            else
            {
                AddTab(customHomeUrl);
            }
        }
    }

    private void txtAddress_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            var url = txtAddress.Text.Trim();
            AddToAddressHistory(url);
            Navigate(url);
            txtAddress.DroppedDown = false;
        }
        else if (e.KeyCode == Keys.Delete && txtAddress.DroppedDown && txtAddress.SelectedIndex >= 0)
        {
            e.SuppressKeyPress = true;
            var index = txtAddress.SelectedIndex;
            RemoveHistoryItem(index);
            if (index < txtAddress.Items.Count)
                txtAddress.SelectedIndex = index;
            else if (txtAddress.Items.Count > 0)
                txtAddress.SelectedIndex = txtAddress.Items.Count - 1;
        }
    }

    private void txtAddress_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (txtAddress.SelectedIndex >= 0 && !string.IsNullOrWhiteSpace(txtAddress.Text))
        {
            Navigate(txtAddress.Text.Trim());
        }
    }

    private void txtAddress_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
            return;

        var text = txtAddress.Items[e.Index]?.ToString() ?? string.Empty;
        var backColor = isDarkMode ? Color.FromArgb(48, 48, 48) : Color.FromArgb(250, 250, 250);
        var foreColor = isDarkMode ? Color.White : Color.Black;

        if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
        {
            backColor = isDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(190, 190, 190);
        }

        e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);

        const int xWidth = 28;
        var textRect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - xWidth - 8, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, e.Font!, textRect, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var xRect = new Rectangle(e.Bounds.Right - xWidth, e.Bounds.Y, xWidth, e.Bounds.Height);
        using (var font = new Font("Segoe UI", 12f, FontStyle.Regular))
        using (var brush = new SolidBrush(foreColor))
        {
            var size = e.Graphics.MeasureString("\u00d7", font);
            var x = xRect.X + (xRect.Width - size.Width) / 2;
            var y = xRect.Y + (xRect.Height - size.Height) / 2;
            e.Graphics.DrawString("\u00d7", font, brush, x, y);
        }

        e.DrawFocusRectangle();
    }

    private void txtAddress_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!txtAddress.DroppedDown || e.Button != MouseButtons.Left)
            return;

        var comboRect = txtAddress.RectangleToScreen(txtAddress.ClientRectangle);
        var mouseScreen = Control.MousePosition;

        if (mouseScreen.X < comboRect.Left || mouseScreen.X > comboRect.Right || mouseScreen.Y < comboRect.Bottom)
            return;

        int itemIndex = (mouseScreen.Y - comboRect.Bottom) / txtAddress.ItemHeight;
        if (itemIndex < 0 || itemIndex >= txtAddress.Items.Count)
            return;

        int relX = mouseScreen.X - comboRect.Left;
        if (relX > txtAddress.Width - 30)
        {
            RemoveHistoryItem(itemIndex);
        }
    }


    private void btnAbout_Click(object? sender, EventArgs e)
    {
        using var about = new AboutForm();
        about.ShowDialog(this);
    }

    private async void AudioPollTimer_Tick(object? sender, EventArgs e)
    {
        foreach (var tab in tabs.ToList())
        {
            try
            {
                if (tab.View?.CoreWebView2 != null)
                {
                    var result = await tab.View.ExecuteScriptAsync(
                        "Array.from(document.querySelectorAll('audio,video')).some(e=>!e.paused&&e.currentTime>0)");
                    var isPlaying = result?.Trim() == "true";
                    if (tab.IsPlayingAudio != isPlaying)
                    {
                        tab.IsPlayingAudio = isPlaying;
                        tab.TabButton.Invalidate();
                    }
                }
            }
            catch
            {
                // Ignore errors (e.g., page not loaded yet)
            }
        }
    }
}
