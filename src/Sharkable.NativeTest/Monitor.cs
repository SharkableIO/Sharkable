namespace Sharkable.NativeTest;

[ScopedService]
public class Monitor:IMonitor
{
    public void Show()
    {
        var logger = Shark.GetService<ILogger<Monitor>>();
        logger?.LogInformation("Monitor show");
    }
}

public interface IMonitor
{
    void Show();
}