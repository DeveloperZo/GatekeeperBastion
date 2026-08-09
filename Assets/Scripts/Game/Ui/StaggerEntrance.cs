using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeeperBastion.Game.Ui
{
    /// <summary>On enable, children fade and slide in over ~200ms, staggered 40–60ms apart.
    /// Uses unscaled time so pause (non-blocking) does not freeze the entrance.</summary>
    public sealed class StaggerEntrance : MonoBehaviour
    {
        [SerializeField] float _slidePixels = 25f;
        [SerializeField] float _duration = 0.2f;
        [SerializeField] float _staggerMin = 0.04f;
        [SerializeField] float _staggerMax = 0.06f;
        [SerializeField] bool _includeInactive;

        Coroutine _routine;

        void OnEnable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Play());
        }

        void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        IEnumerator Play()
        {
            int count = transform.childCount;
            var items = new Item[count];
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                var child = transform.GetChild(i);
                if (!_includeInactive && !child.gameObject.activeSelf) continue;
                items[n++] = Capture(child);
            }

            float stagger = Mathf.Lerp(_staggerMin, _staggerMax, 0.5f);
            for (int i = 0; i < n; i++)
                Apply(items[i], 0f);

            for (int i = 0; i < n; i++)
            {
                float start = Time.unscaledTime;
                while (true)
                {
                    float t = _duration <= 0f
                        ? 1f
                        : Mathf.Clamp01((Time.unscaledTime - start) / _duration);
                    Apply(items[i], EaseOutCubic(t));
                    if (t >= 1f) break;
                    yield return null;
                }
                if (i < n - 1)
                {
                    float waitUntil = Time.unscaledTime + stagger;
                    while (Time.unscaledTime < waitUntil)
                        yield return null;
                }
            }

            _routine = null;
        }

        static Item Capture(Transform child)
        {
            var rt = child as RectTransform;
            var cg = child.GetComponent<CanvasGroup>();
            var graphic = cg == null ? child.GetComponent<Graphic>() : null;
            return new Item
            {
                Rt = rt,
                Cg = cg,
                Graphic = graphic,
                RestPos = rt != null ? rt.anchoredPosition : Vector2.zero,
                RestAlpha = cg != null ? cg.alpha : (graphic != null ? graphic.color.a : 1f)
            };
        }

        void Apply(Item item, float t)
        {
            float alpha = Mathf.Lerp(0f, item.RestAlpha, t);
            if (item.Cg != null)
                item.Cg.alpha = alpha;
            else if (item.Graphic != null)
            {
                var c = item.Graphic.color;
                c.a = alpha;
                item.Graphic.color = c;
            }

            if (item.Rt != null)
            {
                float y = Mathf.Lerp(item.RestPos.y - _slidePixels, item.RestPos.y, t);
                item.Rt.anchoredPosition = new Vector2(item.RestPos.x, y);
            }
        }

        static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        struct Item
        {
            public RectTransform Rt;
            public CanvasGroup Cg;
            public Graphic Graphic;
            public Vector2 RestPos;
            public float RestAlpha;
        }
    }
}
