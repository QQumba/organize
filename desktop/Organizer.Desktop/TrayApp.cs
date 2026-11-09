using System.Runtime.InteropServices;
using System.Windows.Interop;
using Organizer.Client;

namespace Organizer.Desktop;

/// <summary>Tray icon, global hotkey and the popup. Startup problems never crash the app: they go to the tray tooltip and one balloon.</summary>
public sealed class TrayApp : IDisposable
{
    private const int HotkeyId = 1;
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    private readonly System.Windows.Forms.NotifyIcon _icon = new();
    private readonly OrganizerClient? _client;
    private readonly PopupWindow _popup;
    private readonly HwndSource _source;
    private readonly bool _hotkeyRegistered;

    public TrayApp(string? configPath = null)
    {
        var errors = new List<string>();
        var path = configPath ?? ClientConfig.DefaultPath("desktop.json");

        string? configError = null;
        try
        {
            _client = new OrganizerClient(ClientConfig.Load(path, "desktop.json"));
        }
        catch (OrganizerException e)
        {
            configError = e.Message;
            errors.Add(e.Message);
        }

        _popup = new PopupWindow(new CapturePopupModel(_client is null ? null : new ClientInboxApi(_client), configError));

        // The popup's window handle is created without showing it; it receives WM_HOTKEY.
        var hwnd = new WindowInteropHelper(_popup).EnsureHandle();
        _source = HwndSource.FromHwnd(hwnd);
        _source.AddHook(WndProc);
        if (!Hotkey.TryParse(DesktopSettings.ReadHotkey(path), out var hotkey, out var hotkeyError))
        {
            errors.Add(hotkeyError!);
        }
        else if (!RegisterHotKey(hwnd, HotkeyId, (uint)hotkey!.Modifiers | ModNoRepeat, hotkey.VirtualKey))
        {
            errors.Add($"Hotkey {hotkey.Text} could not be registered: another program probably uses it. Set a different \"Hotkey\" in {path}.");
        }
        else
        {
            _hotkeyRegistered = true;
        }

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => _popup.ShowPopup());
        menu.Items.Add("Quit", null, (_, _) => System.Windows.Application.Current.Shutdown());
        _icon.Icon = System.Drawing.SystemIcons.Application;
        _icon.ContextMenuStrip = menu;
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) _popup.ShowPopup();
        };
        _icon.Text = Truncate(errors.Count == 0 ? "Organizer" : "Organizer: " + string.Join(" ", errors), 127);
        _icon.Visible = true;
        if (errors.Count > 0)
        {
            _icon.ShowBalloonTip(10000, "Organizer", string.Join("\n", errors), System.Windows.Forms.ToolTipIcon.Error);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            _popup.ShowPopup();
        }
        return IntPtr.Zero;
    }

    private static string Truncate(string text, int max)
    {
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        return text.Length <= max ? text : text[..(max - 1)] + "…";
    }

    public void Dispose()
    {
        if (_hotkeyRegistered) UnregisterHotKey(_source.Handle, HotkeyId);
        _icon.Visible = false;
        _icon.Dispose();
        _client?.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
