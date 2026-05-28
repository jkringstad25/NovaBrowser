using System;
using System.Drawing;
using System.Windows.Forms;

namespace NovaBrowser;

public partial class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About Nova";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(400, 220);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.White;

        var titleLabel = new Label
        {
            Text = "Nova Browser",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var versionLabel = new Label
        {
            Text = $"Version {Form1.AppVersion}",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = true,
            Location = new Point(20, 55)
        };

        var descriptionLabel = new Label
        {
            Text = "A lightweight, custom web browser built with .NET and WebView2. Created by Justin Kringstad",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(200, 200, 200),
            AutoSize = false,
            Size = new Size(360, 40),
            Location = new Point(20, 90),
            TextAlign = ContentAlignment.TopLeft
        };

        var copyrightLabel = new Label
        {
            Text = $"\u00a9 {DateTime.Now.Year} Nova Browser. All rights reserved. Astro Dynamics 2026",
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Color.FromArgb(150, 150, 150),
            AutoSize = true,
            Location = new Point(20, 140)
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(300, 170),
            Size = new Size(75, 28),
            BackColor = Color.FromArgb(64, 64, 64),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        okButton.FlatAppearance.BorderSize = 1;
        okButton.FlatAppearance.BorderColor = Color.Gray;

        Controls.Add(titleLabel);
        Controls.Add(versionLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(copyrightLabel);
        Controls.Add(okButton);

        AcceptButton = okButton;
    }
}
