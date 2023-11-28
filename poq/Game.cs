using System.Text;
using Silk.NET.Windowing;

namespace poq;

public struct GameSpecification
{
    public string Name = "Untitled Game";
    public string TitleFormat = "<GameName>";
    public string DebugTitleFormat = "<GameName> (<API>, FPS: <FPS>)";

    public GameSpecification()
    { }
}

public class Game
{
    public bool Running { get; private set; } = false;
    public bool CanClose { get; private set; } = true;
    public string? TitleOverride = null;

    protected GameSpecification Spec;
    protected bool RequestingRestart = true;
    protected IWindow? GameWindow;

    protected Game(GameSpecification gameSpec)
    {
        Spec = gameSpec;
    }

    protected bool Init()
    {
        if (GameWindow is null)
        {
            GameWindow = Window.Create(WindowOptions.DefaultVulkan with
            {
                API = GraphicsAPI.DefaultVulkan with { Version = new APIVersion(1, 2) },
                Title = GetWindowTitle()
            });
            
            if (GameWindow is null)
            {
                // TODO: Log error
                return false;
            }
        }

        return true;
    }

    public void Close()
    {
        if (CanClose)
            GameWindow!.Close();
    }

    /// <summary>
    /// Ensures the game will not restart, and then closes.
    /// </summary>
    public void FullClose()
    {
        RequestingRestart = false;
        Close();
    }

    public void Restart()
    {
        RequestingRestart = true;
        Close();
    }
    
    protected void CleanUp()
    {
        Running = false;
        if (GameWindow is not null)
        {
            GameWindow.Close();
            GameWindow.Dispose();
        }
    }

    public void Run()
    {
        do
        {
            if (!Init()) return;
            GameWindow!.Run();
            CleanUp();
        } while (RequestingRestart);
    }

    public string GetWindowTitle()
    {
        if (TitleOverride is not null)
            return GetFormattedTitle(TitleOverride);
        
#if DEBUG
        return GetFormattedTitle(Spec.DebugTitleFormat);
#else
        return GetFormattedTitle(Spec.TitleFormat);
#endif
    }

    public string GetFormattedTitle(string format)
    {
        // StringBuilder should have less allocs, but chained string.Replace is apparently faster?
        // Not sure, probably not worth thinking about right now
        // TODO anyways
        
        StringBuilder builder = new(format);
        builder.Replace("<GameName>", Spec.Name);
        builder.Replace("<API>", "Vulkan");
        builder.Replace("<FPS>", "100000000");
        return builder.ToString();
    }
}
