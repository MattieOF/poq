using System.Text;
using Silk.NET.Windowing;

namespace poq;

/// <summary>
/// <para>Structure containing basic information about the game</para>
/// Pretty much the "project settings"
/// </summary>
public struct GameSpecification
{
    /// <summary>
    /// Name of the game, used for the title and more
    /// </summary>
    public string Name = "Untitled Game";
    /// <summary>
    /// Format of the title in Release mode.
    /// <b>Elements (surround with angular brackets)</b>: <i>GameName, API, FPS</i>.
    /// </summary>
    public string TitleFormat = "<GameName>";
    /// <summary>
    /// Format of the title in Debug mode.
    /// <b>Elements (surround with angular brackets)</b>: <i>GameName, API, FPS</i>.
    /// </summary>
    public string DebugTitleFormat = "<GameName> (<API>, FPS: <FPS>)";

    public GameSpecification()
    { }
}

/// <summary>
/// Base class for a poq Game.
/// </summary>
public class Game
{
    /// <summary>
    /// Is the game currently running? Set to true by Init, set to false by CleanUp.
    /// </summary>
    public bool Running { get; private set; } = false;
    /// <summary>
    /// If false, the game will not be closed when <see cref="Close"/> is called.
    /// </summary>
    public bool CanClose { get; private set; } = true;
    /// <summary>
    /// If non-null, this string will be used as the title format instead of the ones provided in <see cref="Spec"/>
    /// </summary>
    public string? TitleOverride = null;

    /// <summary>
    /// This games specification. Contains the main engine-level settings for the game.
    /// </summary>
    protected GameSpecification Spec;
    /// <summary>
    /// If true, the game will restart instead of fully shutting down when closed.
    /// </summary>
    protected bool RequestingRestart = true;
    /// <summary>
    /// The main game window
    /// </summary>
    protected IWindow? GameWindow;

    /// <summary>
    /// Construct a new Game. This function does nothing but set the internal Spec object; actual initialisation is done by <see cref="Run"/>
    /// </summary>
    /// <param name="gameSpec">Specification object to use - provides basic engine-level settings</param>
    protected Game(GameSpecification gameSpec)
    {
        Spec = gameSpec;
    }

    /// <summary>
    /// Initialise the Game, such as creating the window.
    /// </summary>
    /// <returns>True if successful, false if not. Any errors are logged by this function.</returns>
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

    /// <summary>
    /// If <see cref="CanClose"/> is true, close the game.
    /// </summary>
    public void Close()
    {
        if (CanClose)
            GameWindow!.Close();
    }

    /// <summary>
    /// Ensures the game will not restart, and then closes (via <see cref="Close"/>).
    /// </summary>
    public void FullClose()
    {
        RequestingRestart = false;
        Close();
    }

    /// <summary>
    /// Close the game, and then reinitialise the game.
    /// This is done by setting <see cref="RequestingRestart"/> to true, and then calling <see cref="Close"/>.
    /// </summary>
    public void Restart()
    {
        RequestingRestart = true;
        Close();
    }
    
    /// <summary>
    /// Set <see cref="Running"/> to false, and dispose of any resources created by the game.
    /// </summary>
    protected void CleanUp()
    {
        Running = false;
        if (GameWindow is not null)
        {
            GameWindow.Close();
            GameWindow.Dispose();
        }
    }

    /// <summary>
    /// Initialise the engine and game if needed, and run the game. This function will not return until the game is
    /// finished running, and the process is ready to terminate.
    /// </summary>
    public void Run()
    {
        do
        {
            if (!Init()) return;
            GameWindow!.Run();
            CleanUp();
        } while (RequestingRestart);
    }

    /// <summary>
    /// Use the current title format to get the desired window title
    /// </summary>
    /// <returns>The desired window title based of the current format; either defined by <see cref="TitleOverride"/> or <see cref="Spec"/></returns>
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

    /// <summary>
    /// Get a window title formatted with <see cref="format"/>
    /// <para>Elements (surround each with angular brackets):</para>
    /// <list type="bullet">
    /// <item>
    /// <term><b>GameName</b> </term>
    /// <description>The name of the game, provided by <see cref="Spec"/></description>
    /// </item>
    /// <item>
    /// <term><b>API</b> </term>
    /// <description>The current render API</description>
    /// </item>
    /// <item>
    /// <term><b>FPS</b> </term>
    /// <description>The current FPS</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="format">Format string to use.</param>
    /// <returns></returns>
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
