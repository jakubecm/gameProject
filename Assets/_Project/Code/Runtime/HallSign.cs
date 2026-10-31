using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// A sign hanging from the hall ceiling. Every face shows the same code: a tile sign shows the tile's
    /// grid code ("B3"), a perimeter sign the column letter or row number of the wall it hangs by.
    /// <see cref="HallKit"/> sets the code when it assembles a tile; set it by hand for any other text.
    /// </summary>
    public class HallSign : MonoBehaviour
    {
        [SerializeField] private string code = "--";
        [SerializeField] private Text[] faces;

        public string Code
        {
            get => code;
            set
            {
                code = value ?? string.Empty;
                Apply();
            }
        }

        private void Awake() => Apply();

        private void OnValidate() => Apply();

        private void Apply()
        {
            if (faces == null) return;
            foreach (var face in faces)
            {
                if (face != null) face.text = code;
            }
        }
    }
}
