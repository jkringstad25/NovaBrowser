using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NovaBrowser;

public class UrlComboBox : ComboBox
{
    private const int WM_PAINT = 0x000F;
    private const int WM_RBUTTONUP = 0x0205;
    private MouseButtons _mouseButton = MouseButtons.None;
    private EditHook? _editHook;

    public UrlComboBox()
    {
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (DropDownStyle == ComboBoxStyle.DropDown)
        {
            _editHook?.ReleaseHandle();
            IntPtr editHandle = GetWindow(Handle, GW_CHILD);
            if (editHandle != IntPtr.Zero)
            {
                _editHook = new EditHook(this);
                _editHook.AssignHandle(editHandle);
            }
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _editHook?.ReleaseHandle();
        _editHook = null;
        base.OnHandleDestroyed(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _mouseButton = e.Button;
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _mouseButton = MouseButtons.None;
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Right && ContextMenuStrip != null)
        {
            DroppedDown = false;
            ContextMenuStrip.Show(this, e.Location);
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left)
            DroppedDown = true;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        if (_mouseButton != MouseButtons.Right)
            DroppedDown = true;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_PAINT)
        {
            base.WndProc(ref m);
            PaintOverArrow();
            return;
        }
        base.WndProc(ref m);
    }

    private void PaintOverArrow()
    {
        using var g = Graphics.FromHwnd(Handle);
        var arrowWidth = SystemInformation.VerticalScrollBarWidth;
        var arrowRect = new Rectangle(
            ClientSize.Width - arrowWidth,
            0,
            arrowWidth,
            ClientSize.Height);

        using (var brush = new SolidBrush(BackColor))
        {
            g.FillRectangle(brush, arrowRect);
        }

        using var pen = new Pen(Color.Gray);
        g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    private const uint GW_CHILD = 5;

    private sealed class EditHook : NativeWindow
    {
        private readonly UrlComboBox _owner;

        public EditHook(UrlComboBox owner)
        {
            _owner = owner;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_RBUTTONUP && _owner.ContextMenuStrip != null)
            {
                _owner.DroppedDown = false;
                var pos = _owner.PointToClient(Cursor.Position);
                _owner.ContextMenuStrip.Show(_owner, pos);
                return;
            }
            base.WndProc(ref m);
        }
    }
}
