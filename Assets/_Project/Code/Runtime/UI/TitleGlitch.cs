using UnityEngine;
using UnityEngine.UI;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The title's life: a cyan and a red copy sit behind each title line, almost on top of it (a slight colour fringe), and now and then the copies
    /// jump apart for a fraction of a second while the line flickers, like a failing screen. Uses unscaled time.
    /// </summary>
    internal class TitleGlitch : MonoBehaviour
    {
        private Text[] lines;
        private RectTransform[] cyan;
        private RectTransform[] red;
        private Vector2[] home;
        private float nextBurst;
        private float burstEnd;

        public void Init(params Text[] titleLines)
        {
            lines = titleLines;
            cyan = new RectTransform[lines.Length];
            red = new RectTransform[lines.Length];
            home = new Vector2[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                home[i] = lines[i].rectTransform.anchoredPosition;
                cyan[i] = Ghost(lines[i], new Color(0.2f, 0.9f, 1f, 0.5f));
                red[i] = Ghost(lines[i], new Color(1f, 0.15f, 0.2f, 0.45f));
            }
            nextBurst = Time.unscaledTime + 3f;
        }

        // A copy of the line right behind it.
        private static RectTransform Ghost(Text line, Color color)
        {
            var copy = Instantiate(line.gameObject, line.transform.parent);
            copy.name = line.name + " Ghost";
            var text = copy.GetComponent<Text>();
            text.color = color;
            copy.transform.SetSiblingIndex(line.transform.GetSiblingIndex());
            return text.rectTransform;
        }

        private void OnEnable()
        {
            nextBurst = Time.unscaledTime + 1.2f;
            burstEnd = 0f;
        }

        private void Update()
        {
            if (lines == null) return;

            float now = Time.unscaledTime;
            if (now >= nextBurst)
            {
                burstEnd = now + Random.Range(0.1f, 0.22f);
                nextBurst = burstEnd + Random.Range(2.5f, 6f);
            }
            bool burst = now < burstEnd;
            for (int i = 0; i < lines.Length; i++)
            {
                var fringe = burst ? new Vector2(Random.Range(-16f, 16f), Random.Range(-5f, 5f)) : new Vector2(2.5f, 0f);
                cyan[i].anchoredPosition = home[i] - fringe;
                red[i].anchoredPosition = home[i] + fringe;
                var jitter = burst ? new Vector2(Random.Range(-5f, 5f), 0f) : Vector2.zero;
                lines[i].rectTransform.anchoredPosition = home[i] + jitter;
                var color = UiKit.Bright;
                color.a = burst ? Random.Range(0.55f, 1f) : 0.94f + 0.06f * Mathf.Sin(now * 2f);
                lines[i].color = color;
            }
        }
    }
}
