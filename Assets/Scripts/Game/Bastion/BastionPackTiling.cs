using System.Collections.Generic;
using UnityEngine;

namespace GatekeeperBastion.Game
{
    /// <summary>Measures pack piece prefabs and computes snap-grid tile runs for the bastion
    /// kit. The pack is authored on a 5 m grid with a 2.5 m small grid and pieces measure
    /// ~5.08 m because of overlap lips, so the tile step is the snap size and the measured
    /// bounds are the sanity check, not the other way round.</summary>
    internal static class BastionPackTiling
    {
        internal const float SnapGrid = 2.5f;
        internal const float RemainderEpsilon = 0.01f;

        static readonly Dictionary<GameObject, Bounds> _boundsCache = new Dictionary<GameObject, Bounds>();
        static readonly HashSet<string> _metricsLogged = new HashSet<string>();

        /// <summary>Clears the log-once set so the next emit batch logs each piece type's step
        /// and measured bounds again. Called by <see cref="BastionSegment.ResetRebuildCounters"/>.</summary>
        internal static void ResetMetricsLog()
        {
            _metricsLogged.Clear();
        }

        /// <summary>Local AABB of a piece prefab, computed without instantiating: every child
        /// MeshFilter's sharedMesh.bounds transformed into the prefab root's frame (root treated
        /// as identity). Cached per prefab asset. Logs step and measured bounds once per piece
        /// type per emit batch.</summary>
        internal static Bounds Measure(GameObject prefab)
        {
            if (!_boundsCache.TryGetValue(prefab, out Bounds total))
            {
                total = ComputeBounds(prefab);
                _boundsCache[prefab] = total;
            }

            if (_metricsLogged.Add(prefab.name))
                Debug.Log(
                    "[BastionSegment] piece " + prefab.name + " step=" + Step(total.size.x)
                    + " measured=" + total.size.x + "x" + total.size.y + "x" + total.size.z);

            return total;
        }

        static Bounds ComputeBounds(GameObject prefab)
        {
            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            Matrix4x4 worldToRoot = prefab.transform.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds total = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null)
                    continue;

                Matrix4x4 toRoot = worldToRoot * filters[i].transform.localToWorldMatrix;
                Bounds meshBounds = mesh.bounds;
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = new Vector3(
                        (c & 1) == 0 ? min.x : max.x,
                        (c & 2) == 0 ? min.y : max.y,
                        (c & 4) == 0 ? min.z : max.z);
                    Vector3 point = toRoot.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        total = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                        total.Encapsulate(point);
                }
            }

            if (!hasBounds)
                total = new Bounds(Vector3.zero, Vector3.one);

            return total;
        }

        /// <summary>Snap-grid tile step for a measured piece size: nearest multiple of the pack's
        /// 2.5 m small grid, minimum one small grid. ~5.08 m pieces (overlap lips) step at 5.</summary>
        internal static float Step(float measuredSize)
        {
            float step = SnapGrid * Mathf.Round(measuredSize / SnapGrid);
            if (step < SnapGrid)
                step = SnapGrid;
            return step;
        }

        /// <summary>Full pieces of <paramref name="step"/> along <paramref name="length"/>.
        /// <paramref name="remainder"/> is the leftover metres (0 when negligible), filled by one
        /// extra piece scaled remainder/step along the run — non-uniform scale, no small-piece
        /// infill.</summary>
        internal static int RunCount(float length, float step, out float remainder)
        {
            int full = Mathf.FloorToInt(length / step + 0.001f);
            if (full < 0)
                full = 0;
            remainder = length - full * step;
            if (remainder <= RemainderEpsilon)
                remainder = 0f;
            return full;
        }

        /// <summary>Centre and along-axis scale of tile <paramref name="index"/> in a run starting
        /// at <paramref name="start"/>. Indices past <paramref name="fullCount"/> address the one
        /// remainder tile.</summary>
        internal static void RunAt(
            int index,
            int fullCount,
            float start,
            float step,
            float remainder,
            out float center,
            out float scale)
        {
            if (index < fullCount)
            {
                center = start + (index + 0.5f) * step;
                scale = 1f;
                return;
            }

            center = start + fullCount * step + remainder * 0.5f;
            scale = remainder / step;
        }
    }
}
