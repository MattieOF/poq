using System.Runtime.CompilerServices;
using Silk.NET.SDL;

namespace poq;

public enum AlertType
{
    Info,
    Warning,
    Error
}

public static class OS
{
    internal static GameSpecification GlobalGame;
    
    public static unsafe void ShowAlert(AlertType alertType, string message, string? title = null)
    {
        title ??= $"{GlobalGame.Name} Alert";
        
        uint flags = alertType switch
        {
            AlertType.Info => (uint)MessageBoxFlags.Information,
            AlertType.Warning => (uint)MessageBoxFlags.Warning,
            AlertType.Error => (uint)MessageBoxFlags.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(alertType), alertType, null)
        };

        Sdl.GetApi().ShowSimpleMessageBox(flags, title, message, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShowInfoAlert(string message, string? title = null) => ShowAlert(AlertType.Info, message, title);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShowWarnAlert(string message, string? title = null) => ShowAlert(AlertType.Warning, message, title);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static void ShowErrorAlert(string message, string? title = null) => ShowAlert(AlertType.Error, message, title);
}
