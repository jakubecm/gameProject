using ColdShutdown.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Developer menu on F1: FPS, rebuild the level, and the map as text with the player's cell marked. Only active in the Editor
    /// and in development builds. While it is open the player stands still and the cursor is free.
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
        private const int WindowId = 7311;

        // The window is laid out for a 720 px high screen and scaled up from there.
        private const float ReferenceHeight = 720f;

        [SerializeField] private LevelBuilder levelBuilder;

        private bool open;
        private string status = "";
        private float frameTime = 1f / 60f;

        private string mapText = "";
        private int mapPlayerColumn = int.MinValue;
        private int mapPlayerRow = int.MinValue;
        private bool mapDirty = true;

        private Rect windowRect = new Rect(20f, 20f, 460f, 100f);
        private GUIStyle mapStyle;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (levelBuilder != null) levelBuilder.LevelBuilt += OnLevelBuilt;
        }

        private void OnDisable()
        {
            if (levelBuilder != null) levelBuilder.LevelBuilt -= OnLevelBuilt;
            if (open) SetOpen(false);
        }

        private void Update()
        {
            frameTime = Mathf.Lerp(frameTime, Time.unscaledDeltaTime, 0.05f);

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                SetOpen(!open);
            }
        }

        private void SetOpen(bool value)
        {
            open = value;
            if (open) mapDirty = true;

            // The player stands still while this window needs the mouse.
            var reader = FindAnyObjectByType<PlayerInputReader>();
            if (reader != null)
            {
                reader.InputEnabled = !open;
            }
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        private void OnLevelBuilt(LevelBuilder builder)
        {
            mapDirty = true;
        }

        private void OnGUI()
        {
            if (!open) return;

            // IMGUI works in pixels, so on a high resolution screen the window would be tiny: scale it with the screen height.
            float scale = Mathf.Clamp(Screen.height / ReferenceHeight, 1f, 4f);
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            windowRect.height = 0f; // shrink to the content every frame
            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, "Cold Shutdown debug  (F1 closes)");

            GUI.matrix = previousMatrix;
        }

        private void DrawWindow(int id)
        {
            if (levelBuilder == null)
            {
                GUILayout.Label("No LevelBuilder assigned.");
                return;
            }

            if (GUILayout.Button("Rebuild the level"))
            {
                status = levelBuilder.Build() ? "Rebuilt." : "Build failed, see the Console.";
            }

            if (!string.IsNullOrEmpty(status))
            {
                GUILayout.Label(status);
            }

            GUILayout.Label($"{1f / Mathf.Max(frameTime, 0.0001f):0} FPS  ({frameTime * 1000f:0.0} ms)");

            if (levelBuilder.HasLevel)
            {
                DrawMap();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
        }

        private void DrawMap()
        {
            var map = levelBuilder.Map;
            GUILayout.Label($"{map.Width} x {map.Height} cells   {levelBuilder.Layout.Rooms.Count} rooms");

            RefreshMapText();
            mapStyle ??= CreateMapStyle();
            GUILayout.Label(mapText, mapStyle);
            GUILayout.Label("@ you   C corridor   E lift   O office   G security   P power   V cooling   S storage   H hall");
        }

        // The map is redrawn only when the level changes or the player enters another cell.
        private void RefreshMapText()
        {
            int column = int.MinValue;
            int row = int.MinValue;
            if (levelBuilder.Player != null)
            {
                var position = levelBuilder.Player.position;
                column = RoomContract.ColumnAt(position.x);
                row = RoomContract.RowAt(position.z);
            }

            if (!mapDirty && column == mapPlayerColumn && row == mapPlayerRow) return;

            mapDirty = false;
            mapPlayerColumn = column;
            mapPlayerRow = row;
            var layout = levelBuilder.Layout;
            mapText = layout.Map.ToText((c, r) => c == column && r == row ? '@' : layout.HasRoom(c, r) ? layout.GetRoom(c, r).Type.Symbol() : ' ');
        }

        // Walls are drawn with characters, so the font must have equal widths to line up.
        private static GUIStyle CreateMapStyle()
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                richText = false,
            };
            style.font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Lucida Console" }, 13);
            return style;
        }
    }
}
