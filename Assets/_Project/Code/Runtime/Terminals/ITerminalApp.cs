using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// A program on the terminal desktop: it has an icon, and a window with its own content. The desktop makes the
    /// window around it (title bar, close button, taskbar entry); the program fills the content area.
    /// </summary>
    public interface ITerminalApp
    {
        string Id { get; }

        string Title { get; }

        /// <summary>One or two characters on the icon.</summary>
        string IconLetter { get; }

        Color IconColor { get; }

        /// <summary>Size of the window in the 1280 x 720 screen, including the 36 pixel title bar.</summary>
        Vector2 WindowSize { get; }

        /// <summary>Makes the content in the empty area of a new window. Called every time the window is opened.</summary>
        void Build(RectTransform content, DesktopTheme theme);

        /// <summary>The window was closed.</summary>
        void Closed();

        /// <summary>The player left the computer (the window stays open for the next visit).</summary>
        void Suspended();

        /// <summary>The player came back to the computer.</summary>
        void Resumed();
    }
}
