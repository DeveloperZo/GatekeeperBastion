using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace GatekeepersProtocol.EditorTools
{
    /// <summary>Dresses the Ship with the game's look: near-black hull, districts of coloured
    /// light on the walls, a tinted mirror deck, and motes in the air. One-shot editor work —
    /// it writes authored scene content and nothing reconstructs any of it at runtime.
    ///
    /// Everything lands under one root the pass owns, so re-running replaces its own output and
    /// touches no hand-placed geometry. Walls are found by shape rather than by name.
    ///
    /// Menu: GatekeepersProtocol/Build/Ship Look Pass, Bake/Ship Look Lighting.
    /// </summary>
    public static class ShipLookPass
    {
        const string RootName    = "Ship Look";
        const string DressName   = "ShipDress";
        const string FixtureDir  = "Assets/Packs/ShipFixtures";
        const string ProfilePath = "Assets/Settings/ShipLookProfile.asset";

        [MenuItem("GatekeepersProtocol/Build/Ship Look Pass", false, 65)]
        public static void Apply()
        {
            var profile = LoadOrCreateProfile();
            var scene = EditorSceneManager.GetActiveScene();

            var dress = FindDress(scene, profile);
            if (dress.Count == 0)
            {
                Debug.LogWarning($"[ShipLook] Nothing to dress in {scene.name}. Add this " +
                                 "scene's geometry root to Dress Roots on the profile.");
                return;
            }

            if (profile.districtHues == null || profile.districtHues.Length == 0)
            {
                Debug.LogWarning("[ShipLook] The profile has no district hues. Nothing to dress.");
                return;
            }

            // measure BEFORE destroying: bailing out after the destroy would strip the scene's
            // existing dressing and leave an empty root in its place
            var walls = FindWalls(dress, profile, out Bounds deck);
            if (walls.Count == 0)
            {
                Debug.LogWarning("[ShipLook] Found no wall faces. Widen Wall Max Thickness on " +
                                 "the profile, or check that ShipDress holds the hull.");
                return;
            }

            // every root of this name, not the first: Find returns one, and an orphaned pass
            // would double the scene's cost invisibly
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == RootName) Undo.DestroyObjectImmediate(go);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Apply Ship Look");

            var mats = FixtureMaterials(profile);
            int rows = DressWalls(root.transform, walls, deck, profile, mats);
            Deck(root.transform, deck, profile);
            Motes(root.transform, deck, profile);
            Probes(root.transform, deck, profile);

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ShipLook] Dressed {walls.Count} wall faces with {rows} fixture rows in " +
                      $"{profile.districtHues.Length} districts, {mats.Count} shared materials, " +
                      "0 realtime lights added. Bake Lighting next — until then the fixtures " +
                      "glow but light nothing.");
        }

        // ---- walls, found by shape ---------------------------------------------------------

        struct Wall
        {
            public Vector3 face;     // a point on the inward face
            public Vector3 normal;   // pointing into the room
            public float length;
            public float floorY;
        }

        /// <summary>A wall is thin in one horizontal axis, long in the other, and tall. Name
        /// matching would not survive the Ship's rebuild; shape does.</summary>
        /// <summary>The geometry to dress, in whichever scene is open. Named roots first, so a
        /// scene can say what its hull is; failing that, everything with a renderer.</summary>
        static List<MeshRenderer> FindDress(UnityEngine.SceneManagement.Scene scene,
                                            ShipLookProfile p)
        {
            foreach (string name in p.dressRoots)
            {
                var go = Find(scene, name);
                if (go == null) continue;
                var found = new List<MeshRenderer>(go.GetComponentsInChildren<MeshRenderer>(false));
                if (found.Count > 0)
                {
                    Debug.Log($"[ShipLook] Dressing '{name}' — {found.Count} renderers.");
                    return found;
                }
            }

            var all = new List<MeshRenderer>();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == RootName) continue;          // never dress our own output
                all.AddRange(go.GetComponentsInChildren<MeshRenderer>(false));
            }
            Debug.Log($"[ShipLook] No named dress root; falling back to the whole scene — " +
                      $"{all.Count} renderers.");
            return all;
        }

        static List<Wall> FindWalls(List<MeshRenderer> renderers, ShipLookProfile p, out Bounds deck)
        {
            var walls = new List<Wall>();

            deck = new Bounds(Vector3.zero, Vector3.zero);
            bool any = false;
            foreach (var r in renderers)
            {
                if (!any) { deck = r.bounds; any = true; }
                else deck.Encapsulate(r.bounds);
            }
            if (!any) return walls;

            var centre = deck.center;
            foreach (var r in renderers)
            {
                var b = r.bounds;
                if (b.size.y < p.wallMinHeight) continue;

                bool thinX = b.size.x <= p.wallMaxThickness;
                bool thinZ = b.size.z <= p.wallMaxThickness;
                if (thinX == thinZ) continue;                       // neither, or a pillar

                float length = thinX ? b.size.z : b.size.x;
                if (length < p.wallMinLength) continue;

                var n = thinX ? Vector3.right : Vector3.forward;
                float side = thinX ? centre.x - b.center.x : centre.z - b.center.z;
                if (side < 0f) n = -n;                              // point into the room

                walls.Add(new Wall
                {
                    face = b.center + n * ((thinX ? b.size.x : b.size.z) * 0.5f + 0.03f),
                    normal = n,
                    length = length,
                    floorY = b.min.y,
                });
            }
            return walls;
        }

        // ---- fixtures ----------------------------------------------------------------------

        /// <summary>Six shared materials: one hot and one dim per district hue. Shared, because
        /// a per-instance material silently kills batching on the pack's one atlas.</summary>
        static Dictionary<string, Material> FixtureMaterials(ShipLookProfile p)
        {
            EnsureFolder(FixtureDir);
            var mats = new Dictionary<string, Material>();

            for (int d = 0; d < p.districtHues.Length; d++)
            for (int row = 0; row < 2; row++)
            {
                float peak = p.districtPeak * (row == 0 ? 1f : p.upperRowFraction);
                string key = $"Fixture_D{d}_{(row == 0 ? "Hot" : "Dim")}";
                mats[key] = Owned(key, m =>
                {
                    m.color = Color.black;
                    m.SetFloat("_Glossiness", 0f);
                    m.EnableKeyword("_EMISSION");
                    var hue = p.districtHues[d];
                    float top = Mathf.Max(hue.r, Mathf.Max(hue.g, hue.b));
                    m.SetColor("_EmissionColor", hue / Mathf.Max(top, 1e-4f) * peak);
                    // the fixtures ARE the light once this is baked
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                });
            }
            return mats;
        }

        static int DressWalls(Transform root, List<Wall> walls, Bounds deck, ShipLookProfile p,
                              Dictionary<string, Material> mats)
        {
            var holder = new GameObject("Districts");
            holder.transform.SetParent(root, false);

            int districts = Mathf.Max(1, p.districtHues.Length);
            float z0 = deck.min.z, span = Mathf.Max(deck.size.z, 0.01f);
            int placed = 0;

            foreach (var w in walls)
            {
                // districts partition the ship along its long axis, so walking the spine walks
                // the cast
                float t = Mathf.Clamp01((w.face.z - z0) / span);
                int d = Mathf.Clamp(Mathf.FloorToInt(t * districts), 0, districts - 1);

                for (int row = 0; row < 2; row++)
                {
                    float y = row == 0 ? p.rowHeights.x : p.rowHeights.y;
                    var mesh = DashRow(w.length, row);
                    var go = new GameObject($"Row_D{d}_{(row == 0 ? "Hot" : "Dim")}_{placed}");
                    go.transform.SetParent(holder.transform, false);
                    go.transform.position = new Vector3(w.face.x, w.floorY + y, w.face.z);
                    go.transform.rotation = Quaternion.LookRotation(w.normal, Vector3.up);

                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mats[$"Fixture_D{d}_{(row == 0 ? "Hot" : "Dim")}"];
                    mr.shadowCastingMode = ShadowCastingMode.Off;

                    GameObjectUtility.SetStaticEditorFlags(go,
                        StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                        StaticEditorFlags.OccludeeStatic);
                    placed++;
                }
            }
            return placed;
        }

        /// <summary>A run of glowing dashes at uneven pitch — the readout rhythm the look is
        /// built on. Cached per rounded length so a ship of walls costs a handful of meshes.</summary>
        static Mesh DashRow(float length, int row)
        {
            int metres = Mathf.Max(1, Mathf.RoundToInt(length));
            string name = $"Fixture_Dash_{metres}m_{row}";
            string path = $"{FixtureDir}/{name}.asset";
            var had = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (had != null) return had;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();

            float half = metres * 0.5f;
            float h = row == 0 ? 0.10f : 0.06f;
            int seed = 7919 + row * 31;
            float x = -half + 0.10f;

            while (x < half - 0.10f)
            {
                seed = (seed * 1103515245 + 12345) & 0x7fffffff;
                float w = 0.14f + (seed % 1000) / 1000f * 0.80f;
                seed = (seed * 1103515245 + 12345) & 0x7fffffff;
                float gap = 0.08f + (seed % 1000) / 1000f * 0.30f;
                if (x + w > half - 0.10f) w = half - 0.10f - x;

                if (w > 0.04f && (seed % 100) > 16)
                {
                    int o = verts.Count;
                    verts.Add(new Vector3(x, -h, 0f));
                    verts.Add(new Vector3(x + w, -h, 0f));
                    verts.Add(new Vector3(x + w, h, 0f));
                    verts.Add(new Vector3(x, h, 0f));
                    for (int i = 0; i < 4; i++) norms.Add(Vector3.back);
                    uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
                    uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                    tris.Add(o); tris.Add(o + 2); tris.Add(o + 1);
                    tris.Add(o); tris.Add(o + 3); tris.Add(o + 2);
                }
                x += w + gap;
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(mesh);   // lightmap UVs, or the bake ignores it

            EnsureFolder(FixtureDir);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---- deck, motes, probes -----------------------------------------------------------

        static void Deck(Transform root, Bounds deck, ShipLookProfile p)
        {
            if (p.deckWet <= 0.01f) return;

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Deck Sheen";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(deck.center.x, deck.min.y + 0.02f, deck.center.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(deck.size.x, deck.size.z, 1f);

            var mat = Owned("Deck_Sheen", m =>
            {
                m.SetFloat("_Mode", 3f);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
                var c = p.deckTint;
                m.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, 0.55f, p.deckWet));
                m.SetFloat("_Glossiness", Mathf.Lerp(0.2f, 0.97f, p.deckWet));
                // THE ACCENT, and it is the specular colour — tinting a reflection is exactly
                // multiplying it. The accent lived on emission here, which makes a deck glow
                // in a colour instead of shining in one; a floor that glows reads as painted.
                float hp = Mathf.Max(p.deckAccent.r, Mathf.Max(p.deckAccent.g, p.deckAccent.b));
                var spec = Color.Lerp(new Color(0.22f, 0.22f, 0.22f),
                                      p.deckAccent / Mathf.Max(hp, 1e-4f) * 0.35f,
                                      Mathf.Clamp01(p.deckAccentStrength));
                if (m.HasProperty("_SpecColor")) m.SetColor("_SpecColor", spec);
                // the metallic fallback shader has no specular colour, so there the albedo is
                // the reflection's colour and metallic is what hands it over
                if (m.HasProperty("_Metallic") && !m.HasProperty("_SpecColor"))
                    m.SetFloat("_Metallic", Mathf.Clamp01(p.deckAccentStrength));

                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }, Shader.Find("Standard (Specular setup)") ?? Shader.Find("Standard"));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // transparent, so it takes light from probes rather than contributing to the bake
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>One emitter per district, at the splash values. A ParticleSystem rather
        /// than a field of quads: same grammar, a fraction of the objects.</summary>
        static void Motes(Transform root, Bounds deck, ShipLookProfile p)
        {
            if (p.moteCount <= 0) return;
            var holder = new GameObject("Motes");
            holder.transform.SetParent(root, false);

            int districts = Mathf.Max(1, p.districtHues.Length);
            for (int d = 0; d < districts; d++)
            {
                float t = (d + 0.5f) / districts;
                var go = new GameObject($"Motes_D{d}");
                go.transform.SetParent(holder.transform, false);
                go.transform.position = new Vector3(
                    deck.center.x, deck.min.y + 0.4f,
                    Mathf.Lerp(deck.min.z, deck.max.z, t));

                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = true;
                main.duration = 5f;
                main.simulationSpeed = p.moteSimulationSpeed;
                main.startLifetime = 5f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0f, p.moteRise * 5f);
                main.startSize = new ParticleSystem.MinMaxCurve(p.moteSize.x, p.moteSize.y);
                main.maxParticles = p.moteCount;
                main.startColor = new Color(p.moteColor.r, p.moteColor.g, p.moteColor.b,
                                            p.moteAlpha);
                main.gravityModifier = 0f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var emission = ps.emission;
                emission.rateOverTime = p.moteCount / 5f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(deck.size.x * 0.8f, 0.5f,
                                          deck.size.z / districts * 0.8f);

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = MoteMaterial();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        /// <summary>Light probes, or every dynamic object in a baked ship reads unlit — the
        /// Boarder included.</summary>
        static void Probes(Transform root, Bounds deck, ShipLookProfile p)
        {
            var go = new GameObject("Light Probes");
            go.transform.SetParent(root, false);
            var group = go.AddComponent<LightProbeGroup>();

            var points = new List<Vector3>();
            for (float x = deck.min.x + 2f; x <= deck.max.x - 2f; x += 6f)
            for (float z = deck.min.z + 2f; z <= deck.max.z - 2f; z += 6f)
            for (float y = deck.min.y + 0.6f; y <= deck.min.y + 3.6f; y += 1.5f)
                points.Add(go.transform.InverseTransformPoint(new Vector3(x, y, z)));
            group.probePositions = points.ToArray();

            var pgo = new GameObject("Deck Reflection");
            pgo.transform.SetParent(root, false);
            pgo.transform.position = new Vector3(deck.center.x, deck.min.y + 2f, deck.center.z);
            var probe = pgo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;   // baked scene, baked reflections
            probe.boxProjection = true;
            probe.resolution = 128;
            probe.size = new Vector3(deck.size.x, 5f, deck.size.z);
        }

        // ---- bake ---------------------------------------------------------------------------

        [MenuItem("GatekeepersProtocol/Bake/Ship Look Lighting", false, 81)]
        public static void Bake()
        {
            var settings = new LightingSettings
            {
                name = "Ship Look Bake",
                bakedGI = true,
                realtimeGI = false,
                lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
                lightmapMaxSize = 1024,
                lightmapResolution = 10f,
                indirectResolution = 2f,
                maxBounces = 3,
                directSampleCount = 32,
                indirectSampleCount = 512,
                ao = true,
                aoMaxDistance = 1.5f,
                filteringMode = LightingSettings.FilterMode.Auto,
            };
            Lightmapping.lightingSettings = settings;

            var scene = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShipLook] Baking. The emissive fixtures become the ship's light; " +
                      "dynamic bodies read from the probe group.");
            Lightmapping.BakeAsync();
        }

        // ---- plumbing -----------------------------------------------------------------------

        static ShipLookProfile LoadOrCreateProfile()
        {
            var p = AssetDatabase.LoadAssetAtPath<ShipLookProfile>(ProfilePath);
            if (p != null) return p;

            EnsureFolder("Assets/Settings");
            p = ScriptableObject.CreateInstance<ShipLookProfile>();
            AssetDatabase.CreateAsset(p, ProfilePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShipLook] Created {ProfilePath} at the converged values. Tune it there.");
            return p;
        }

        static Material MoteMaterial()
        {
            return Owned("Mote_Additive", m =>
            {
                m.mainTexture = MoteTexture();
                m.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.5f));
            }, Shader.Find("Legacy Shaders/Particles/Additive") ??
               Shader.Find("Particles/Standard Unlit") ??
               Shader.Find("Sprites/Default"));
        }

        /// <summary>A soft radial dot. Squared falloff with a small hot core, so the centre
        /// reads and the rim vanishes rather than showing a card edge.</summary>
        static Texture2D MoteTexture()
        {
            EnsureFolder(FixtureDir);
            string path = $"{FixtureDir}/Mote.png";
            var had = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (had != null) return had;

            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f;
                float dy = (y + 0.5f) / N * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                a = a * a * (0.35f + 0.65f * a);
                px[y * N + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var im = AssetImporter.GetAtPath(path) as TextureImporter;
            if (im != null)
            {
                im.alphaIsTransparency = true;
                im.wrapMode = TextureWrapMode.Clamp;
                im.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Owned(string key, System.Action<Material> configure, Shader shader = null)
        {
            EnsureFolder(FixtureDir);
            string path = $"{FixtureDir}/{key}.mat";
            var want = shader != null ? shader : Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(want) { name = key };
                AssetDatabase.CreateAsset(mat, path);
            }
            // swap on existing assets too: a material authored under a different shader keeps
            // it forever otherwise, and every property the new shader adds is silently dropped
            if (mat.shader != want) mat.shader = want;
            configure(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject Find(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == name) return go;
                var t = go.transform.Find(name);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
