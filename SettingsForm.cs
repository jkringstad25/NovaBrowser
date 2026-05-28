using System;
using System.Drawing;
using System.Windows.Forms;

namespace NovaBrowser;

public partial class SettingsForm : Form
{
    public bool IsDarkMode { get; set; }

    public SettingsForm(bool currentDarkMode)
    {
        IsDarkMode = currentDarkMode;

        Text = "Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(360, 180);
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        BackColor = currentDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245);
        ForeColor = currentDarkMode ? Color.White : Color.Black;

        var titleLabel = new Label
        {
            Text = "Settings",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = currentDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        var darkModeCheck = new CheckBox
        {
            Text = "Dark Mode",
            Checked = currentDarkMode,
            ForeColor = currentDarkMode ? Color.White : Color.Black,
            AutoSize = true,
            Location = new Point(20, 60),
            Font = new Font("Segoe UI", 10f, FontStyle.Regular)
        };
        darkModeCheck.CheckedChanged += (sender, e) =>
        {
            IsDarkMode = darkModeCheck.Checked;
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(260, 130),
            Size = new Size(75, 28),
            BackColor = currentDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(225, 225, 225),
            ForeColor = currentDarkMode ? Color.White : Color.Black,
            FlatStyle = FlatStyle.Flat
        };
        okButton.FlatAppearance.BorderSize = 1;
        okButton.FlatAppearance.BorderColor = Color.Gray;

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(175, 130),
            Size = new Size(75, 28),
            BackColor = currentDarkMode ? Color.FromArgb(64, 64, 64) : Color.FromArgb(225, 225, 225),
            ForeColor = currentDarkMode ? Color.White : Color.Black,
            FlatStyle = FlatStyle.Flat
        };
        cancelButton.FlatAppearance.BorderSize = 1;
        cancelButton.FlatAppearance.BorderColor = Color.Gray;

        Controls.Add(titleLabel);
        Controls.Add(darkModeCheck);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
