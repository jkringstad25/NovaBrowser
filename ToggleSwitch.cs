using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NovaBrowser;

public class ToggleSwitch : Control
{
    private bool _checked;
    private bool _mouseDown;
    private float _animationPosition;
    private System.Windows.Forms.Timer? _animationTimer;

    public event EventHandler? CheckedChanged;

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked != value)
            {
                _checked = value;
                StartAnimation();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ToggleSwitch()
    {
        DoubleBuffered = true;
        Size = new Size(50, 26);
        Cursor = Cursors.Hand;
        _animationPosition = 0f;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _mouseDown = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left && _mouseDown)
        {
            _mouseDown = false;
            Checked = !_checked;
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _mouseDown = false;
        Invalidate();
    }

    private void StartAnimation()
    {
        _animationTimer?.Stop();
        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        float target = _checked ? 1f : 0f;
        _animationTimer.Tick += (s, e) =>
        {
            float step = 0.15f;
            if (Math.Abs(_animationPosition - target) < step)
            {
                _animationPosition = target;
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
                _animationTimer = null;
            }
            else
            {
                _animationPosition += _animationPosition < target ? step : -step;
            }
            Invalidate();
        };
        _animationTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var trackRect = new Rectangle(1, 1, Width - 3, Height - 3);
        int radius = trackRect.Height / 2;

        Color trackColor;
        if (_checked)
            trackColor = Color.FromArgb(0, 122, 204);
        else
            trackColor = BackColor.GetBrightness() < 0.5f ? Color.FromArgb(64, 64, 64) : Color.FromArgb(200, 200, 200);

        using (var trackBrush = new SolidBrush(trackColor))
        using (var trackPath = GetRoundedRect(trackRect, radius))
        {
            g.FillPath(trackBrush, trackPath);
        }

        int thumbDiameter = trackRect.Height - 4;
        float thumbX = 2 + _animationPosition * (trackRect.Width - thumbDiameter - 4);
        var thumbRect = new RectangleF(thumbX, 3, thumbDiameter, thumbDiameter);

        Color thumbColor = BackColor.GetBrightness() < 0.5f ? Color.FromArgb(220, 220, 220) : Color.White;
        using (var thumbBrush = new SolidBrush(thumbColor))
        {
            g.FillEllipse(thumbBrush, thumbRect);
        }
    }

    private static GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
