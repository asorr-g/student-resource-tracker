namespace StudentResourceTracker.Services;

public class ToastService
{
    public event Action<string>? Shown;
    public void Show(string message) => Shown?.Invoke(message);
}
