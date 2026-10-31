using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    public enum PowerState
    {
        Off,
        Booting,
        On,
        ShuttingDown,
    }

    /// <summary>
    /// The screen of a terminal: a computer that can be switched on and off. When it is on there is a desktop: a
    /// wallpaper, icons for the programs, windows that open on top of it (they can be dragged and closed) and a taskbar
    /// with a clock and a Start menu to shut down. Built in code on a 1280 x 720 area; the look comes from a
    /// <see cref="DesktopTheme"/>. Programs are <see cref="ITerminalApp"/>s. Off, the screen is black; switching on
    /// shows a short boot screen first.
    /// </summary>
    public sealed class Desktop
    {
        public const float Width = 1280f;
        public const float Height = 720f;
        public const float TaskbarHeight = 44f;
        public const float TitleBarHeight = 36f;
        public const float BootSeconds = 3f;
        public const float ShutdownSeconds = 1.2f;

        private readonly DesktopTheme theme;
        private readonly RectTransform system;
        private readonly RectTransform iconLayer;
        private readonly RectTransform windowLayer;
        private readonly RectTransform taskLayer;
        private readonly RectTransform startMenu;
        private readonly Button menuBlocker;
        private readonly RectTransform bootPanel;
        private readonly Text bootStatus;
        private readonly RectTransform bootFill;
        private readonly Button blackout;
        private readonly Text blackoutHint;
        private readonly Text clock;
        private readonly List<ITerminalApp> apps = new List<ITerminalApp>();
        private readonly List<DesktopWindow> windows = new List<DesktopWindow>();
        private readonly float startMinutes;
        private int shownMinute = -1;
        private float powerTimer;

        public IReadOnlyList<ITerminalApp> Apps => apps;

        public IReadOnlyList<DesktopWindow> Windows => windows;

        public PowerState Power { get; private set; }

        public bool IsOn => Power == PowerState.On;

        /// <summary>The root of everything that exists while the system runs; the tests find the icons and the taskbar below it.</summary>
        public RectTransform SystemRoot => system;

        /// <summary>Raised when the computer goes off, starts booting, is on, or starts shutting down.</summary>
        public event Action<PowerState> PowerChanged;

        public Desktop(RectTransform root, DesktopTheme theme, bool startOn = true, float startMinutes = 2f * 60f + 13f, Texture wallpaperPicture = null)
        {
            this.theme = theme;
            this.startMinutes = startMinutes;

            root.sizeDelta = new Vector2(Width, Height);
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                UiFactory.Dispose(root.GetChild(i).gameObject);
            }

            // The running system: wallpaper, icons, windows, taskbar, Start menu.
            system = UiFactory.Rect("System", root);
            UiFactory.Fill(system);
            var wallpaper = UiFactory.Image("Wallpaper", system, theme.wallpaper);
            UiFactory.Fill(wallpaper.rectTransform);
            if (wallpaperPicture != null)
            {
                var picture = UiFactory.Rect("Picture", wallpaper.transform).gameObject.AddComponent<RawImage>();
                picture.texture = wallpaperPicture;
                picture.uvRect = CoverRect(wallpaperPicture.width / (float)wallpaperPicture.height, Width / Height);
                picture.raycastTarget = false;
                UiFactory.Fill(picture.rectTransform);

                var veil = UiFactory.Image("Tint", wallpaper.transform, theme.wallpaperTint, false);
                UiFactory.Fill(veil.rectTransform);
            }
            else
            {
                var band = UiFactory.Image("WallpaperBand", wallpaper.transform, theme.wallpaperBand, false);
                UiFactory.Place(band.rectTransform, UiFactory.BottomLeft, new Vector2(0f, TaskbarHeight), new Vector2(Width, 210f));
            }

            iconLayer = UiFactory.Rect("Icons", system);
            UiFactory.Fill(iconLayer);
            windowLayer = UiFactory.Rect("Windows", system);
            UiFactory.Fill(windowLayer);

            taskLayer = UiFactory.Rect("Taskbar", system);
            UiFactory.Fill(taskLayer);
            var bar = UiFactory.Image("Bar", taskLayer, theme.taskbar);
            UiFactory.Place(bar.rectTransform, UiFactory.BottomLeft, Vector2.zero, new Vector2(Width, TaskbarHeight));

            var start = UiFactory.Button("Start", bar.transform, DesktopTexts.Start, theme, theme.smallSize + 2, theme.accent, Color.white);
            UiFactory.Place((RectTransform)start.transform, UiFactory.BottomLeft, Vector2.zero, new Vector2(96f, TaskbarHeight));
            clock = UiFactory.Text("Clock", bar.transform, "", theme.bodySize, theme.taskbarText, TextAnchor.MiddleRight);
            UiFactory.Place(clock.rectTransform, UiFactory.BottomRight, new Vector2(-16f, 0f), new Vector2(120f, TaskbarHeight));

            // The Start menu: a click anywhere else closes it.
            menuBlocker = UiFactory.Button("MenuBlocker", system, "", theme, theme.smallSize, new Color(0f, 0f, 0f, 0.004f));
            UiFactory.Fill((RectTransform)menuBlocker.transform, 0f, 0f, 0f, TaskbarHeight);
            menuBlocker.transition = Selectable.Transition.None;
            menuBlocker.onClick.AddListener(CloseStartMenu);
            var menu = UiFactory.Image("StartMenu", system, theme.taskbar);
            startMenu = menu.rectTransform;
            UiFactory.Place(startMenu, UiFactory.BottomLeft, new Vector2(0f, TaskbarHeight), new Vector2(230f, 64f));
            var shutDown = UiFactory.Button("ShutDown", startMenu, DesktopTexts.ShutDown, theme, theme.bodySize, new Color(1f, 1f, 1f, 0.10f), theme.taskbarText, TextAnchor.MiddleLeft);
            UiFactory.Place((RectTransform)shutDown.transform, UiFactory.TopLeft, new Vector2(8f, -8f), new Vector2(214f, 48f));
            shutDown.onClick.AddListener(ShutDown);
            start.onClick.AddListener(ToggleStartMenu);
            CloseStartMenu();

            // The boot and shutdown screen.
            var boot = UiFactory.Image("Boot", root, new Color(0.04f, 0.05f, 0.08f));
            bootPanel = boot.rectTransform;
            UiFactory.Fill(bootPanel);
            var bootTitle = UiFactory.Text("Title", bootPanel, DesktopTexts.BootTitle, theme.headingSize + 8, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Place(bootTitle.rectTransform, UiFactory.Center, new Vector2(0f, 60f), new Vector2(700f, 50f));
            var barBack = UiFactory.Image("BarBack", bootPanel, new Color(1f, 1f, 1f, 0.15f), false);
            UiFactory.Place(barBack.rectTransform, UiFactory.Center, new Vector2(0f, -10f), new Vector2(420f, 8f));
            var barFill = UiFactory.Image("BarFill", barBack.transform, theme.accent, false);
            bootFill = barFill.rectTransform;
            bootFill.anchorMin = Vector2.zero;
            bootFill.anchorMax = new Vector2(0f, 1f);
            bootFill.offsetMin = Vector2.zero;
            bootFill.offsetMax = Vector2.zero;
            bootStatus = UiFactory.Text("Status", bootPanel, "", theme.bodySize, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
            UiFactory.Place(bootStatus.rectTransform, UiFactory.Center, new Vector2(0f, -44f), new Vector2(700f, 30f));

            // The black screen: a click on it switches the computer on.
            blackout = UiFactory.Button("Blackout", root, "", theme, theme.smallSize, Color.black);
            UiFactory.Fill((RectTransform)blackout.transform);
            blackout.transition = Selectable.Transition.None;
            blackout.onClick.AddListener(PowerOn);
            blackoutHint = UiFactory.Text("Hint", blackout.transform, DesktopTexts.PowerOnHint, theme.bodySize, new Color(1f, 1f, 1f, 0.25f), TextAnchor.MiddleCenter);
            UiFactory.Fill(blackoutHint.rectTransform);
            blackoutHint.gameObject.SetActive(false);

            SetPower(startOn ? PowerState.On : PowerState.Off);
            Tick(0f);
        }

        /// <summary>Puts a program on the desktop with an icon.</summary>
        public void AddApp(ITerminalApp app)
        {
            apps.Add(app);
            BuildIcon(app, apps.Count - 1);
        }

        public bool IsOpen(ITerminalApp app) => windows.Any(window => window.App == app);

        public DesktopWindow WindowOf(ITerminalApp app) => windows.FirstOrDefault(window => window.App == app);

        /// <summary>Opens the window of a program, or brings it to the front if it is open already. Nothing happens while the computer is not on.</summary>
        public DesktopWindow Open(ITerminalApp app)
        {
            if (!IsOn) return null;

            CloseStartMenu();
            var existing = WindowOf(app);
            if (existing != null)
            {
                Focus(existing);
                return existing;
            }

            var window = new DesktopWindow(this, app, windowLayer, taskLayer, theme, windows.Count);
            windows.Add(window);
            app.Build(window.Content, theme);
            Focus(window);
            RebuildTaskButtons();
            return window;
        }

        public void Close(ITerminalApp app)
        {
            var window = WindowOf(app);
            if (window == null) return;

            windows.Remove(window);
            app.Closed();
            window.Destroy();
            RebuildTaskButtons();
        }

        public void CloseAll()
        {
            foreach (var window in windows.ToList())
            {
                Close(window.App);
            }
        }

        public void Focus(DesktopWindow window)
        {
            window.Root.SetAsLastSibling();
            foreach (var other in windows)
            {
                other.SetFocused(other == window);
            }
        }

        /// <summary>Switches the computer on: the boot screen, then the desktop.</summary>
        public void PowerOn()
        {
            if (Power == PowerState.Off) SetPower(PowerState.Booting);
        }

        /// <summary>Shuts the computer down from the Start menu: every window is closed, then the screen goes black.</summary>
        public void ShutDown()
        {
            if (Power != PowerState.On) return;

            CloseStartMenu();
            CloseAll();
            SetPower(PowerState.ShuttingDown);
        }

        /// <summary>The black screen only asks to be clicked while the player sits at the computer.</summary>
        public void SetSeated(bool seated) => blackoutHint.gameObject.SetActive(seated);

        /// <summary>The player left the computer: programs stop what needs stopping (reading pauses the game).</summary>
        public void Suspend()
        {
            foreach (var window in windows) window.App.Suspended();
        }

        public void Resume()
        {
            foreach (var window in windows) window.App.Resumed();
        }

        /// <summary>Keeps the clock and the boot going. Give it real time: it must run while the game is paused. The clock is the night shift's: it starts at 02:13.</summary>
        public void Tick(float deltaTime)
        {
            int minute = Mathf.FloorToInt(startMinutes + Time.realtimeSinceStartup / 60f) % (24 * 60);
            if (minute != shownMinute)
            {
                shownMinute = minute;
                clock.text = $"{minute / 60:00}:{minute % 60:00}";
            }

            if (Power != PowerState.Booting && Power != PowerState.ShuttingDown) return;

            powerTimer += deltaTime;
            float duration = Power == PowerState.Booting ? BootSeconds : ShutdownSeconds;
            bootFill.anchorMax = new Vector2(Mathf.Clamp01(powerTimer / duration), 1f);
            if (powerTimer >= duration)
            {
                SetPower(Power == PowerState.Booting ? PowerState.On : PowerState.Off);
            }
        }

        private void SetPower(PowerState state)
        {
            Power = state;
            powerTimer = 0f;
            system.gameObject.SetActive(state == PowerState.On);
            bootPanel.gameObject.SetActive(state == PowerState.Booting || state == PowerState.ShuttingDown);
            blackout.gameObject.SetActive(state == PowerState.Off);

            bootFill.anchorMax = new Vector2(0f, 1f);
            bootStatus.text = state == PowerState.ShuttingDown ? DesktopTexts.BootShuttingDown : DesktopTexts.BootStarting;
            PowerChanged?.Invoke(state);
        }

        private void ToggleStartMenu()
        {
            bool open = !startMenu.gameObject.activeSelf;
            startMenu.gameObject.SetActive(open);
            menuBlocker.gameObject.SetActive(open);
            if (open) startMenu.SetAsLastSibling();
        }

        private void CloseStartMenu()
        {
            startMenu.gameObject.SetActive(false);
            menuBlocker.gameObject.SetActive(false);
        }

        /// <summary>
        /// The part of a picture to show so that it fills a target of another shape without being stretched: the
        /// middle of the picture, cropped on the long side. Both are width divided by height.
        /// </summary>
        public static Rect CoverRect(float pictureAspect, float targetAspect)
        {
            if (pictureAspect <= 0f || targetAspect <= 0f) return new Rect(0f, 0f, 1f, 1f);

            if (pictureAspect > targetAspect)
            {
                float width = targetAspect / pictureAspect;
                return new Rect((1f - width) / 2f, 0f, width, 1f);
            }

            float height = pictureAspect / targetAspect;
            return new Rect(0f, (1f - height) / 2f, 1f, height);
        }

        internal static RawImage AddPicture(Transform parent, Texture picture, float margin)
        {
            var image = UiFactory.Rect("Icon", parent).gameObject.AddComponent<RawImage>();
            image.texture = picture;
            image.raycastTarget = false;
            UiFactory.Fill(image.rectTransform, margin, margin, margin, margin);
            return image;
        }

        private void BuildIcon(ITerminalApp app, int index)
        {
            const int perColumn = 5;
            int column = index / perColumn;
            int row = index % perColumn;

            var icon = UiFactory.Rect("Icon_" + app.Id, iconLayer);
            UiFactory.Place(icon, UiFactory.TopLeft, new Vector2(24f + column * 120f, -24f - row * 124f), new Vector2(104f, 112f));

            var picture = theme.IconFor(app.Id);
            Button tile;
            if (picture != null)
            {
                // A picture on the wallpaper: no tile, only a soft glow while the mouse is over it.
                tile = UiFactory.Button("Tile", icon, "", theme, theme.smallSize, new Color(1f, 1f, 1f, 0.004f));
                var colors = tile.colors;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.20f);
                colors.pressedColor = new Color(1f, 1f, 1f, 0.32f);
                tile.colors = colors;
                UiFactory.Place((RectTransform)tile.transform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(76f, 76f));
                AddPicture(tile.transform, picture, 6f);
            }
            else
            {
                tile = UiFactory.Button("Tile", icon, app.IconLetter, theme, theme.headingSize + 8, app.IconColor, Color.white);
                UiFactory.Place((RectTransform)tile.transform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(68f, 68f));
            }
            tile.onClick.AddListener(() => Open(app));

            var label = UiFactory.Text("Label", icon, app.Title, theme.smallSize + 1, theme.iconLabel, TextAnchor.UpperCenter);
            UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(104f, 34f));
            // Readable on any picture.
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1f, -1.5f);
        }

        private void RebuildTaskButtons()
        {
            for (int i = taskLayer.childCount - 1; i >= 0; i--)
            {
                var child = taskLayer.GetChild(i);
                if (child.name.StartsWith("Task_")) UiFactory.Dispose(child.gameObject);
            }

            for (int i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                var button = UiFactory.Button("Task_" + window.App.Id, taskLayer, window.App.Title, theme, theme.smallSize + 1,
                    new Color(1f, 1f, 1f, 0.14f), theme.taskbarText);
                UiFactory.Place((RectTransform)button.transform, UiFactory.BottomLeft, new Vector2(108f + i * 176f, 4f), new Vector2(168f, TaskbarHeight - 8f));
                button.onClick.AddListener(() => Focus(window));

                var picture = theme.IconFor(window.App.Id);
                if (picture != null)
                {
                    var caption = button.GetComponentInChildren<Text>();
                    caption.alignment = TextAnchor.MiddleLeft;
                    UiFactory.Fill(caption.rectTransform, 40f, 0f, 8f, 0f);
                    var small = AddPicture(button.transform, picture, 0f);
                    UiFactory.Place(small.rectTransform, UiFactory.BottomLeft, new Vector2(8f, 4f), new Vector2(28f, 28f));
                }
            }
        }
    }

    /// <summary>A window on the desktop: title bar, close button, and an empty content area for the program.</summary>
    public sealed class DesktopWindow
    {
        private readonly Desktop desktop;
        private readonly Image titleBar;
        private readonly DesktopTheme theme;

        public ITerminalApp App { get; }

        public RectTransform Root { get; }

        /// <summary>The area below the title bar that the program fills.</summary>
        public RectTransform Content { get; }

        public Button CloseButton { get; }

        internal DesktopWindow(Desktop desktop, ITerminalApp app, RectTransform layer, RectTransform taskLayer, DesktopTheme theme, int openCount)
        {
            this.desktop = desktop;
            this.theme = theme;
            App = app;

            var size = app.WindowSize;
            var background = UiFactory.Image("Window_" + app.Id, layer, theme.windowBackground);
            Root = background.rectTransform;
            UiFactory.Place(Root, UiFactory.Center, new Vector2(-24f * openCount, 20f - 24f * openCount), size);
            Root.gameObject.AddComponent<PointerDownRelay>().onPointerDown = () => desktop.Focus(this);

            titleBar = UiFactory.Image("TitleBar", Root, theme.titleBar);
            UiFactory.Place(titleBar.rectTransform, UiFactory.TopLeft, Vector2.zero, new Vector2(size.x, Desktop.TitleBarHeight));
            var drag = titleBar.gameObject.AddComponent<WindowDragHandle>();
            drag.window = Root;
            drag.bounds = layer;
            drag.onPointerDown = () => desktop.Focus(this);

            var picture = theme.IconFor(app.Id);
            var title = UiFactory.Text("Title", titleBar.transform, app.Title, theme.titleSize - 2, theme.titleText);
            UiFactory.Fill(title.rectTransform, picture != null ? 44f : 14f, 0f, 60f, 0f);
            if (picture != null)
            {
                var small = Desktop.AddPicture(titleBar.transform, picture, 0f);
                UiFactory.Place(small.rectTransform, UiFactory.TopLeft, new Vector2(10f, -4f), new Vector2(28f, 28f));
            }

            CloseButton = UiFactory.Button("Close", titleBar.transform, DesktopTexts.Close, theme, theme.bodySize, theme.closeButton, Color.white);
            UiFactory.Place((RectTransform)CloseButton.transform, UiFactory.TopRight, Vector2.zero, new Vector2(46f, Desktop.TitleBarHeight));
            CloseButton.onClick.AddListener(() => desktop.Close(app));

            Content = UiFactory.Rect("Content", Root);
            UiFactory.Fill(Content, 0f, Desktop.TitleBarHeight, 0f, 0f);
        }

        internal void SetFocused(bool focused)
        {
            titleBar.color = focused ? theme.titleBar : Color.Lerp(theme.titleBar, theme.windowBackground, 0.35f);
        }

        internal void Destroy()
        {
            UiFactory.Dispose(Root.gameObject);
        }
    }

    /// <summary>Tells a window that something inside it was pressed, so the window comes to the front.</summary>
    internal sealed class PointerDownRelay : MonoBehaviour, IPointerDownHandler
    {
        public Action onPointerDown;

        public void OnPointerDown(PointerEventData eventData) => onPointerDown?.Invoke();
    }

    /// <summary>Lets a title bar drag its window around the screen, never off it.</summary>
    internal sealed class WindowDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        public RectTransform window;
        public RectTransform bounds;
        public Action onPointerDown;

        private Vector2 grabOffset;

        public void OnPointerDown(PointerEventData eventData) => onPointerDown?.Invoke();

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, eventData.position, eventData.pressEventCamera, out var local))
            {
                grabOffset = window.anchoredPosition - local;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bounds, eventData.position, eventData.pressEventCamera, out var local)) return;

            var position = local + grabOffset;
            float halfWidth = bounds.rect.width / 2f - window.rect.width / 2f;
            float halfHeight = bounds.rect.height / 2f - window.rect.height / 2f;
            position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
            // The title bar stays reachable above the taskbar.
            position.y = Mathf.Clamp(position.y, -halfHeight + Desktop.TaskbarHeight, halfHeight);
            window.anchoredPosition = position;
        }
    }
}
