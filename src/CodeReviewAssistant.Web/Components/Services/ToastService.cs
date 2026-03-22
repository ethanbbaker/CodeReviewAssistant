namespace CodeReviewAssistant.Components.Services;

public enum ToastType { Success, Error, Warning, Info }

/// <summary>
/// A single toast notification item.
/// </summary>
/// <param name="Id">Unique identifier used as the React key and for dismissal.</param>
/// <param name="Message">Text to display.</param>
/// <param name="Type">Visual style / icon variant.</param>
/// <param name="AutoDismissMs">
///   Milliseconds after which the toast dismisses automatically.
///   Zero means the toast persists until the user dismisses it manually.
/// </param>
public sealed record ToastItem(Guid Id, string Message, ToastType Type, int AutoDismissMs);

/// <summary>
/// Scoped service (one instance per Blazor circuit) for showing transient notifications.
/// Components fire <c>ShowXxx()</c> methods; <see cref="ToastContainer"/> subscribes to
/// <see cref="OnToast"/> and renders the visible stack.
/// </summary>
public sealed class ToastService
{
    /// <summary>Raised on the calling thread whenever a new toast is requested.</summary>
    public event Action<ToastItem>? OnToast;

    public void ShowSuccess(string message, int autoDismissMs = 3_000)
        => OnToast?.Invoke(new(Guid.NewGuid(), message, ToastType.Success, autoDismissMs));

    public void ShowError(string message, int autoDismissMs = 0)
        => OnToast?.Invoke(new(Guid.NewGuid(), message, ToastType.Error, autoDismissMs));

    public void ShowWarning(string message, int autoDismissMs = 5_000)
        => OnToast?.Invoke(new(Guid.NewGuid(), message, ToastType.Warning, autoDismissMs));

    public void ShowInfo(string message, int autoDismissMs = 3_000)
        => OnToast?.Invoke(new(Guid.NewGuid(), message, ToastType.Info, autoDismissMs));
}
