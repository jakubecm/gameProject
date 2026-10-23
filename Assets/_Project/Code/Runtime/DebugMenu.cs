using ColdShutdown.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Developer menu on F1: change the seed and the map size, regenerate the level while playing, and see
    /// the map as text with the player's cell marked. Only active in the Editor and in development builds.
    /// While it is open the player stands still and the cursor is free.
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
        private const int WindowId = 7311;
        // Every room is a heavy prefab (hundreds of renderers, many realtime lights). Until distance culling
        // (task 3.5) exists, a map much bigger than this exhausts memory: 30 x 30 used up 32 GB.
        private const int MaxMapSize = 12;

        // The window is laid out for a 720 px high screen and scaled up from there.
        private const float ReferenceHeight = 720f;

        [SerializeField] private LevelBuilder levelBuilder;

        private bool open;
        private string seedText = "";
        private string widthText = "";
        private string heightText = "";
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
            if (open) ReadFromBuilder();

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
            if (open) ReadFromBuilder();
        }

        private void ReadFromBuilder()
        {
            if (levelBuilder == null) return;

            seedText = levelBuilder.Seed.ToString();
            widthText = levelBuilder.Width.ToString();
            heightText = levelBuilder.Height.ToString();
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

            GUILayout.BeginHorizontal();
            GUILayout.Label("Seed", GUILayout.Width(50f));
            seedText = GUILayout.TextField(seedText, 11);
            if (GUILayout.Button("Random", GUILayout.Width(70f)))
            {
                seedText = LevelBuilder.NewRandomSeed().ToString();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Size", GUILayout.Width(50f));
            widthText = GUILayout.TextField(widthText, 2, GUILayout.Width(40f));
            GUILayout.Label("x", GUILayout.Width(12f));
            heightText = GUILayout.TextField(heightText, 2, GUILayout.Width(40f));
            GUILayout.Label($"(1 to {MaxMapSize})");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate")) Regenerate();
            if (GUILayout.Button("Random seed + regenerate"))
            {
                seedText = LevelBuilder.NewRandomSeed().ToString();
                Regenerate();
            }
            GUILayout.EndHorizontal();

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

        private void Regenerate()
        {
            if (!int.TryParse(seedText, out int seed))
            {
                status = "The seed has to be a whole number.";
                return;
            }
            if (!int.TryParse(widthText, out int newWidth) || !int.TryParse(heightText, out int newHeight) ||
                newWidth < 1 || newHeight < 1 || newWidth > MaxMapSize || newHeight > MaxMapSize)
            {
                status = $"The size has to be between 1 and {MaxMapSize}.";
                return;
            }

            status = levelBuilder.Build(seed, newWidth, newHeight)
                ? $"Built seed {seed}, {newWidth} x {newHeight}."
                : "Build failed, see the Console.";
        }

        private void DrawMap()
        {
            var map = levelBuilder.Map;
            int loops = map.ConnectionCount - (map.Width * map.Height - 1);
            GUILayout.Label($"Seed {levelBuilder.Seed}   {map.Width} x {map.Height}   {map.ConnectionCount} passages ({loops} loops)");

            RefreshMapText();
            mapStyle ??= CreateMapStyle();
            GUILayout.Label(mapText, mapStyle);
            GUILayout.Label("@ you   C corridor   S server aisle   O office");
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
            mapText = layout.Map.ToText((c, r) => c == column && r == row ? '@' : layout.GetRoom(c, r).Type.ToString()[0]);
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
