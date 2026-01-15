using System.Windows;
using MMCSSTweaker.Controls;

namespace MMCSSTweaker.Core;

internal static class UiDialog
{
    internal static void Info(Window? owner, string title, string message)
        => DarkDialog.Show(owner, title, message, DarkDialogKind.Info);

    internal static void Warning(Window? owner, string title, string message)
        => DarkDialog.Show(owner, title, message, DarkDialogKind.Warning);

    internal static void Error(Window? owner, string title, string message)
        => DarkDialog.Show(owner, title, message, DarkDialogKind.Error);

    internal static void Info(FrameworkElement ownerElement, string title, string message)
        => Info(Window.GetWindow(ownerElement), title, message);

    internal static void Warning(FrameworkElement ownerElement, string title, string message)
        => Warning(Window.GetWindow(ownerElement), title, message);

    internal static void Error(FrameworkElement ownerElement, string title, string message)
        => Error(Window.GetWindow(ownerElement), title, message);
}

