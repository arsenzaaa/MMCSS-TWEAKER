using System.Windows;
using System.Windows.Input;

namespace MMCSSTweaker.Controls;

internal enum DarkDialogKind
{
    Info,
    Warning,
    Error,
}

public partial class DarkDialog : Window
{
    public string TitleText { get; }
    public string MessageText { get; }

    private DarkDialog(string title, string message, DarkDialogKind kind)
    {
        TitleText = title;
        MessageText = message;

        Title = title;
        DataContext = this;
        InitializeComponent();
    }

    internal static void Show(Window? owner, string title, string message, DarkDialogKind kind)
    {
        var dialog = new DarkDialog(title, message, kind)
        {
            Owner = owner,
            WindowStartupLocation = owner == null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner,
        };
        _ = dialog.ShowDialog();
    }

    private void OkButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
            return;

        try
        {
            DragMove();
        }
        catch
        {
        }
    }

    private void Window_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
    }
}
