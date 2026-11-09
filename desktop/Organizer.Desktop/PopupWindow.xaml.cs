using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Organizer.Desktop;

/// <summary>The Capture popup. Created once, hidden on dismiss. All state lives in <see cref="CapturePopupModel"/>.</summary>
public partial class PopupWindow : Window
{
    private readonly CapturePopupModel _model;
    private CancellationTokenSource _cts = new();

    public PopupWindow(CapturePopupModel model)
    {
        _model = model;
        InitializeComponent();
        _model.Changed += Render;
        Input.TextChanged += (_, _) => _model.Text = Input.Text;
        Deactivated += (_, _) => Dismiss();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>Shows the popup near the top of the monitor with the cursor, or just refocuses it when already open.</summary>
    public void ShowPopup()
    {
        if (IsVisible)
        {
            Activate();
            Input.Focus();
            return;
        }
        var area = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(this);
        Left = (area.Left + (area.Width - Width * dpi.DpiScaleX) / 2) / dpi.DpiScaleX;
        Top = (area.Top + area.Height / 6.0) / dpi.DpiScaleY;

        Input.Text = _model.Text;
        Input.CaretIndex = Input.Text.Length;
        Render();
        Show();
        Activate();
        Input.Focus();
        Keyboard.Focus(Input);

        _cts.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = RunAsync(() => _model.OpenAsync(token));
    }

    private void Dismiss()
    {
        if (!IsVisible) return;
        _cts.Cancel();
        Hide();
        _model.Dismissed();
    }

    private void Render()
    {
        ErrorText.Text = _model.Error;
        ErrorText.Visibility = _model.Error is null ? Visibility.Collapsed : Visibility.Visible;
        List.ItemsSource = _model.Rows;
        List.Visibility = _model.Rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private async Task RunAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception e) { ErrorText.Text = e.Message; ErrorText.Visibility = Visibility.Visible; }
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Dismiss();
        }
        else if (ReferenceEquals(e.OriginalSource, Input))
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                var ok = false;
                await RunAsync(async () => ok = await _model.CaptureAsync(_cts.Token));
                if (ok) Dismiss();
            }
            else if (e.Key == Key.Down && _model.Rows.Count > 0)
            {
                e.Handled = true;
                FocusRow(0);
            }
        }
        else if ((e.Key == Key.Enter || e.Key == Key.Space) && List.SelectedItem is ItemRow row)
        {
            e.Handled = true;
            var index = List.SelectedIndex;
            await RunAsync(() => _model.CompleteAsync(row.Id, _cts.Token));
            if (_model.Rows.Count > 0) FocusRow(Math.Min(index, _model.Rows.Count - 1));
            else Input.Focus();
        }
        else if (e.Key == Key.Up && List.SelectedIndex == 0)
        {
            e.Handled = true;
            Input.Focus();
        }
    }

    private void FocusRow(int index)
    {
        List.SelectedIndex = index;
        List.UpdateLayout();
        (List.ItemContainerGenerator.ContainerFromIndex(index) as ListBoxItem)?.Focus();
    }

    private async void OnDoneClick(object sender, RoutedEventArgs e)
    {
        var box = (System.Windows.Controls.CheckBox)sender;
        if (box.DataContext is ItemRow row)
        {
            await RunAsync(() => _model.CompleteAsync(row.Id, _cts.Token));
            box.IsChecked = false; // still here only if the Core refused
        }
    }
}
