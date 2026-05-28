namespace NovaBrowser;

partial class Form1
{
    private System.ComponentModel.IContainer components = null!;
    private Panel topContainerPanel;
    private FlowLayoutPanel tabsPanel;
    private Panel topPanel;
    private FlowLayoutPanel bookmarksPanel;
    private Button btnNewTab;
    private Button btnSaveBookmark;
    private Button btnBack;
    private Button btnForward;
    private Button btnRefresh;
    private Button btnHome;
    private UrlComboBox txtAddress;
    private Button btnGo;
    private Panel contentPanel;
    private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
    private StatusStrip statusStrip1;
    private ToolStripStatusLabel toolStripStatusLabel;
    private ToolStripStatusLabel toolStripLoadTimeLabel;
    private ToolStripButton btnSettings;
    private ToolStripButton btnAbout;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        topContainerPanel = new Panel();
        bookmarksPanel = new FlowLayoutPanel();
        topPanel = new Panel();
        btnGo = new Button();
        btnSaveBookmark = new Button();
        btnRefresh = new Button();
        btnHome = new Button();
        btnForward = new Button();
        btnBack = new Button();
        txtAddress = new UrlComboBox();
        tabsPanel = new FlowLayoutPanel();
        btnNewTab = new Button();
        contentPanel = new Panel();
        webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
        statusStrip1 = new StatusStrip();
        toolStripStatusLabel = new ToolStripStatusLabel();
        toolStripLoadTimeLabel = new ToolStripStatusLabel();
        btnSettings = new ToolStripButton();
        btnAbout = new ToolStripButton();
        topContainerPanel.SuspendLayout();
        topPanel.SuspendLayout();
        tabsPanel.SuspendLayout();
        contentPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)webView21).BeginInit();
        statusStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // topContainerPanel
        // 
        topContainerPanel.BackColor = Color.FromArgb(34, 34, 34);
        topContainerPanel.Controls.Add(bookmarksPanel);
        topContainerPanel.Controls.Add(topPanel);
        topContainerPanel.Controls.Add(tabsPanel);
        topContainerPanel.Dock = DockStyle.Top;
        topContainerPanel.Location = new Point(0, 0);
        topContainerPanel.Name = "topContainerPanel";
        topContainerPanel.Size = new Size(1200, 132);
        topContainerPanel.TabIndex = 0;
        // 
        // bookmarksPanel
        // 
        bookmarksPanel.BackColor = Color.FromArgb(28, 28, 28);
        bookmarksPanel.Dock = DockStyle.Top;
        bookmarksPanel.Location = new Point(0, 92);
        bookmarksPanel.Name = "bookmarksPanel";
        bookmarksPanel.Padding = new Padding(3, 5, 3, 3);
        bookmarksPanel.Size = new Size(1200, 40);
        bookmarksPanel.TabIndex = 2;
        bookmarksPanel.WrapContents = false;
        // 
        // topPanel
        // 
        topPanel.BackColor = Color.FromArgb(34, 34, 34);
        topPanel.Controls.Add(btnGo);
        topPanel.Controls.Add(btnSaveBookmark);
        topPanel.Controls.Add(btnRefresh);
        topPanel.Controls.Add(btnHome);
        topPanel.Controls.Add(btnForward);
        topPanel.Controls.Add(btnBack);
        topPanel.Controls.Add(txtAddress);
        topPanel.Dock = DockStyle.Top;
        topPanel.Location = new Point(0, 56);
        topPanel.Name = "topPanel";
        topPanel.Size = new Size(1200, 36);
        topPanel.TabIndex = 1;
        // 
        // btnGo
        // 
        btnGo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnGo.BackColor = Color.FromArgb(64, 64, 64);
        btnGo.FlatStyle = FlatStyle.Flat;
        btnGo.ForeColor = Color.White;
        btnGo.Location = new Point(585, 6);
        btnGo.Name = "btnGo";
        btnGo.Size = new Size(75, 23);
        btnGo.TabIndex = 6;
        btnGo.Text = "Go";
        btnGo.UseVisualStyleBackColor = false;
        btnGo.Click += btnGo_Click;
        // 
        // btnSaveBookmark
        // 
        btnSaveBookmark.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSaveBookmark.BackColor = Color.FromArgb(64, 64, 64);
        btnSaveBookmark.FlatStyle = FlatStyle.Flat;
        btnSaveBookmark.ForeColor = Color.White;
        btnSaveBookmark.Location = new Point(664, 6);
        btnSaveBookmark.Name = "btnSaveBookmark";
        btnSaveBookmark.Size = new Size(75, 23);
        btnSaveBookmark.TabIndex = 6;
        btnSaveBookmark.Text = "Bookmark";
        btnSaveBookmark.UseVisualStyleBackColor = false;
        btnSaveBookmark.Click += btnSaveBookmark_Click;
        // 
        // btnRefresh
        // 
        btnRefresh.BackColor = Color.FromArgb(64, 64, 64);
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.ForeColor = Color.White;
        btnRefresh.Location = new Point(169, 6);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(75, 23);
        btnRefresh.TabIndex = 2;
        btnRefresh.Text = "Refresh";
        btnRefresh.UseVisualStyleBackColor = false;
        btnRefresh.Click += btnRefresh_Click;
        // 
        // btnHome
        // 
        btnHome.BackColor = Color.FromArgb(64, 64, 64);
        btnHome.FlatStyle = FlatStyle.Flat;
        btnHome.ForeColor = Color.White;
        btnHome.Location = new Point(250, 6);
        btnHome.Name = "btnHome";
        btnHome.Size = new Size(75, 23);
        btnHome.TabIndex = 3;
        btnHome.Text = "Home";
        btnHome.UseVisualStyleBackColor = false;
        btnHome.Click += btnHome_Click;
        // 
        // btnForward
        // 
        btnForward.BackColor = Color.FromArgb(64, 64, 64);
        btnForward.FlatStyle = FlatStyle.Flat;
        btnForward.ForeColor = Color.White;
        btnForward.Location = new Point(88, 6);
        btnForward.Name = "btnForward";
        btnForward.Size = new Size(75, 23);
        btnForward.TabIndex = 1;
        btnForward.Text = "Forward";
        btnForward.UseVisualStyleBackColor = false;
        btnForward.Click += btnForward_Click;
        // 
        // btnBack
        // 
        btnBack.BackColor = Color.FromArgb(64, 64, 64);
        btnBack.FlatStyle = FlatStyle.Flat;
        btnBack.ForeColor = Color.White;
        btnBack.Location = new Point(7, 6);
        btnBack.Name = "btnBack";
        btnBack.Size = new Size(75, 23);
        btnBack.TabIndex = 0;
        btnBack.Text = "Back";
        btnBack.UseVisualStyleBackColor = false;
        btnBack.Click += btnBack_Click;
        // 
        // txtAddress
        // 
        txtAddress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtAddress.BackColor = Color.FromArgb(48, 48, 48);
        txtAddress.ForeColor = Color.White;
        txtAddress.Location = new Point(331, 6);
        txtAddress.Name = "txtAddress";
        txtAddress.Size = new Size(250, 23);
        txtAddress.TabIndex = 3;
        txtAddress.DropDownStyle = ComboBoxStyle.DropDown;
        txtAddress.DrawMode = DrawMode.OwnerDrawFixed;
        txtAddress.ItemHeight = 22;
        txtAddress.DropDownHeight = 230;
        txtAddress.DropDownWidth = 250;
        txtAddress.MaxDropDownItems = 10;
        txtAddress.KeyDown += txtAddress_KeyDown;
        txtAddress.DrawItem += txtAddress_DrawItem;
        txtAddress.SelectedIndexChanged += txtAddress_SelectedIndexChanged;
        txtAddress.MouseDown += txtAddress_MouseDown;
        // 
        // tabsPanel
        // 
        tabsPanel.BackColor = Color.FromArgb(30, 30, 30);
        tabsPanel.Controls.Add(btnNewTab);
        tabsPanel.Dock = DockStyle.Top;
        tabsPanel.Location = new Point(0, 0);
        tabsPanel.Name = "tabsPanel";
        tabsPanel.Size = new Size(1200, 56);
        tabsPanel.TabIndex = 0;
        tabsPanel.WrapContents = false;
        // 
        // btnNewTab
        // 
        btnNewTab.BackColor = Color.FromArgb(30, 30, 30);
        btnNewTab.FlatAppearance.BorderSize = 0;
        btnNewTab.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 30, 30);
        btnNewTab.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 30, 30);
        btnNewTab.FlatStyle = FlatStyle.Flat;
        btnNewTab.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        btnNewTab.ForeColor = Color.White;
        btnNewTab.Location = new Point(3, 8);
        btnNewTab.Margin = new Padding(3, 8, 3, 8);
        btnNewTab.Name = "btnNewTab";
        btnNewTab.Padding = new Padding(0);
        btnNewTab.Size = new Size(32, 32);
        btnNewTab.TabIndex = 4;
        btnNewTab.Text = "+";
        btnNewTab.TextAlign = ContentAlignment.MiddleCenter;
        btnNewTab.UseVisualStyleBackColor = false;
        btnNewTab.Click += btnNewTab_Click;
        // 
        // contentPanel
        // 
        contentPanel.Controls.Add(webView21);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(0, 126);
        contentPanel.Name = "contentPanel";
        contentPanel.Size = new Size(1200, 524);
        contentPanel.TabIndex = 3;
        // 
        // webView21
        // 
        webView21.AllowExternalDrop = true;
        webView21.CreationProperties = null;
        webView21.DefaultBackgroundColor = Color.FromArgb(18, 18, 18);
        webView21.Dock = DockStyle.Fill;
        webView21.Location = new Point(0, 0);
        webView21.Name = "webView21";
        webView21.Size = new Size(1200, 524);
        webView21.TabIndex = 1;
        webView21.ZoomFactor = 1D;
        // 
        // statusStrip1
        // 
        statusStrip1.BackColor = Color.FromArgb(34, 34, 34);
        statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel, toolStripLoadTimeLabel, btnSettings, btnAbout });
        statusStrip1.Location = new Point(0, 650);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Size = new Size(1200, 22);
        statusStrip1.TabIndex = 2;
        statusStrip1.Text = "statusStrip1";
        // 
        // toolStripStatusLabel
        // 
        toolStripStatusLabel.Name = "toolStripStatusLabel";
        toolStripStatusLabel.Size = new Size(1065, 17);
        toolStripStatusLabel.Spring = true;
        toolStripStatusLabel.Text = "Ready";
        toolStripStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // toolStripLoadTimeLabel
        // 
        toolStripLoadTimeLabel.Name = "toolStripLoadTimeLabel";
        toolStripLoadTimeLabel.Size = new Size(120, 17);
        toolStripLoadTimeLabel.Text = "Website Loaded In: -- | ";
        toolStripLoadTimeLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // btnAbout
        // 
        btnSettings.Name = "btnSettings";
        btnSettings.Size = new Size(55, 22);
        btnSettings.Text = "Settings";
        btnSettings.Click += btnSettings_Click;
        // 
        // btnAbout
        // 
        btnAbout.Name = "btnAbout";
        btnAbout.Size = new Size(50, 22);
        btnAbout.Text = "About";
        btnAbout.Click += btnAbout_Click;
        // 
        // Form1
        // 
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(24, 24, 24);
        ClientSize = new Size(1200, 672);
        Controls.Add(contentPanel);
        Controls.Add(statusStrip1);
        Controls.Add(topContainerPanel);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Nova Browser";
        WindowState = FormWindowState.Maximized;
        topContainerPanel.ResumeLayout(false);
        topPanel.ResumeLayout(false);
        topPanel.PerformLayout();
        tabsPanel.ResumeLayout(false);
        contentPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)webView21).EndInit();
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
}
