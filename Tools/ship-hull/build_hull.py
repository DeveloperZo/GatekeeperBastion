"""M101 — rebuild the Ship - CoOp hull as two decks joined by a stair.

Authored offline as YAML with the editor closed, per the practice for this scene.
Strips every child of Environment (graybox) and ShipDress (Synty kit), emits the new
hull from the M101 layout table, and rehomes the fixed objects.

DESTRUCTIVE, and deliberately so: it deletes every child of Environment and ShipDress
and emits the hull from scratch. Running it discards any hand authoring inside those two
subtrees, exactly like SceneBuilder.Ship did. It is committed anyway because the scene
diff it produces is ~386k lines and cannot be reviewed directly - review this file and
Tools/scene-check/enclosure.py instead, and treat a re-run as a decision, never a chore.

Run against a scene reset to HEAD. --dry prints the plan without writing.
"""
import re, sys, math, os

import os
SCENE = os.environ.get("GKB_SHIP_SCENE", "Assets/Scenes/Ship - CoOp.unity")
DRY = '--dry' in sys.argv

# ---------------------------------------------------------------- layout (M101 table)

ROWS = {0: (-1, 0), 1: (-1, 0),
        2: (-2, 1), 3: (-2, 1), 4: (-2, 1), 5: (-2, 1),
        6: (-2, 1), 7: (-2, 1), 8: (-2, 1), 9: (-2, 1),
        10: (-1, 0), 11: (-1, 0)}
VOID = {(-1, 5), (0, 5), (-1, 6), (0, 6)}      # deck 2 has no floor here
DECK_Y = {1: 0.0, 2: 5.0}
CORRIDOR_HALF = 1.25                            # spine runs x[-1.25, 1.25]

def cells(deck):
    out = set()
    for iz, (a, b) in ROWS.items():
        for ix in range(a, b + 1):
            if deck == 2 and (ix, iz) in VOID:
                continue
            out.add((ix, iz))
    return out

D1, D2 = cells(1), cells(2)

MESH = {   # Synty PolygonSciFiSpace, guids are stable across the pack
    'Floor': '549f0d5274edea6458f754e1aac98efb',         # SM_Bld_Floor_01
    'Ceil': '2a5a2a9a9489337489fb0458762db8eb',          # SM_Bld_Ceiling_01
    'CeilLow': '09bfb280021fbcd41ad1d71823655ea1',       # SM_Bld_Ceiling_02
    'Wall': '8d217d0e1fada5e4591985711fc87b86',          # SM_Bld_Wall_01
    'Door': '31d557d4571f9d84c939fd1ce47b5cd8',          # SM_Bld_Wall_Doorframe_01
    'Skin': '3bdbd97455c79064c8b39a7764c17bd6',          # SM_Bld_Wall_Exterior_01
    'SkinPillar': 'ad3df2d4b491d454c96764f575db0130',    # SM_Bld_Wall_Exterior_Pillar_01
    'Window': '4f630417843179749aa8ba87d708a645',        # SM_Bld_Wall_Exterior_Window_01
    'Stairs': '3a9167268fb73cb48b6f3226667f05f2',        # SM_Prop_Stairs_01
    'StairPlat': 'edc81c185e0ff9142b2c988d0a2dae60',     # SM_Prop_StairsPlatform_01
    'Rail': 'b3987b6acf41dc643a6332babc3a7dc9',          # SM_Bld_Floor_Small_Hand_Rail_01
    'LightPanel': '170064085127c8748ae64c26a823cd8c',  # SM_Prop_Light_Panel_01
    'Airlock': '031ef83336706614e9aac66905586671',       # SM_Bld_Wall_EscPod_Hatch_01
}
AIRLOCK_SUBMESH = 4300004
AIRLOCK_LEAF = 4300006
MAT = "{fileID: 2100000, guid: 4cee7a5983b187943adc589b9cb1f084, type: 2}"

WALL_T = 0.892
SKIN_T = 0.713

# ---------------------------------------------------------------- scene io

txt = open(SCENE, encoding='utf-8').read()
head_txt, _, rest = txt.partition('--- !u!')
rest = '--- !u!' + rest
docs = []
for d in re.split(r'(?=^--- !u!)', rest, flags=re.M):
    m = re.match(r'--- !u!(\d+) &(\d+)', d)
    if m:
        docs.append([int(m.group(2)), int(m.group(1)), d])
    elif docs:
        docs[-1][2] += d
    else:
        head_txt += d
byid = {d[0]: d for d in docs}

go_name, go_comps, tf = {}, {}, {}
for i, cls, t in docs:
    if cls == 1:
        mm = re.search(r'^  m_Name: (.*)$', t, re.M)
        go_name[i] = mm.group(1) if mm else ''
        go_comps[i] = [int(c) for c in re.findall(r'component: \{fileID: (\d+)\}', t)]
    elif cls in (4, 224):
        gg = re.search(r'm_GameObject: \{fileID: (\d+)\}', t)
        ff = re.search(r'm_Father: \{fileID: (-?\d+)\}', t)
        kids = []
        if 'm_Children:' in t:
            seg = t.split('m_Children:')[1].split('m_Father:')[0]
            kids = [int(c) for c in re.findall(r'fileID: (\d+)', seg)]
        tf[i] = {'go': int(gg.group(1)) if gg else 0,
                 'father': int(ff.group(1)) if ff else 0, 'kids': kids}
name_of = {i: go_name.get(v['go'], '') for i, v in tf.items()}
by_name = {}
for i, n in name_of.items():
    by_name.setdefault(n, []).append(i)

def num(v):
    s = ('%.4f' % v).rstrip('0').rstrip('.')
    return s if s not in ('', '-0') else '0'

def yawq(deg):
    h = math.radians(deg) / 2.0
    y, w = math.sin(h), math.cos(h)
    f = lambda v: ('%.7f' % v).rstrip('0').rstrip('.') if abs(v) > 1e-7 else '0'
    return "{x: -0, y: %s, z: -0, w: %s}" % (f(y), f(w))

def set_tf(tid, pos=None, yaw=None):
    d = byid[tid]
    if pos is not None:
        d[2] = re.sub(r'm_LocalPosition: \{[^}]*\}',
                      'm_LocalPosition: {x: %s, y: %s, z: %s}' % tuple(num(v) for v in pos),
                      d[2], count=1)
    if yaw is not None:
        d[2] = re.sub(r'm_LocalRotation: \{[^}]*\}', 'm_LocalRotation: ' + yawq(yaw), d[2], count=1)
        d[2] = re.sub(r'm_LocalEulerAnglesHint: \{[^}]*\}',
                      'm_LocalEulerAnglesHint: {x: 0, y: %s, z: 0}' % num(yaw), d[2], count=1)

# ---------------------------------------------------------------- strip old hull

ENV = by_name['Environment'][0]
DRESS = by_name['ShipDress'][0]

def subtree(root):
    out, stack = [], list(tf[root]['kids'])
    while stack:
        t = stack.pop()
        if t not in tf:
            continue
        out.append(t)
        stack.extend(tf[t]['kids'])
    return out

subtree_before = {ENV: subtree(ENV), DRESS: subtree(DRESS)}
doomed_tf = subtree_before[ENV] + subtree_before[DRESS]
doomed_docs = set()
for t in doomed_tf:
    gid = tf[t]['go']
    doomed_docs.add(gid); doomed_docs.add(t)
    for c in go_comps.get(gid, []):
        doomed_docs.add(c)
print("stripping %d old hull objects (%d docs)" % (len(doomed_tf), len(doomed_docs)))

# ---------------------------------------------------------------- emit

# Re-running against a scene that already holds this generator's output must not
# reuse its ids, so skip anything already present.
TAKEN = set(byid)
NEXT = [4100000001]
def nid():
    NEXT[0] += 4
    while NEXT[0] in TAKEN:
        NEXT[0] += 4
    return NEXT[0]

EMIT = []
KIDS = {}
LAST_R = [0]

RENDERER = """--- !u!23 &{rid}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gid}}}
  m_Enabled: {en}
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {mat}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""

def obj(name, parent, pos, yaw=0.0, scale=(1, 1, 1), mesh=None, submesh=4300000,
        box=False, renderer=True, collider=False, quat=None, hint=None, mat=None):
    gid, tid = nid(), nid()
    comps = ["  - component: {fileID: %d}\n" % tid]
    body = []
    if mesh or box:
        fid, rid = nid(), nid()
        comps.append("  - component: {fileID: %d}\n" % fid)
        comps.append("  - component: {fileID: %d}\n" % rid)
        if box:
            mref = "{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}"
            mat = "{fileID: 10303, guid: 0000000000000000f000000000000000, type: 0}"
        else:
            mref = "{fileID: %d, guid: %s, type: 3}" % (submesh, mesh)
            mat = mat or MAT
        body.append("--- !u!33 &%d\nMeshFilter:\n  m_ObjectHideFlags: 0\n"
                    "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                    "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  m_Mesh: %s\n"
                    % (fid, gid, mref))
        body.append(RENDERER.format(rid=rid, gid=gid, mat=mat, en=(0 if box else 1)))
        LAST_R[0] = rid
    if collider:
        cid = nid()
        comps.append("  - component: {fileID: %d}\n" % cid)
        body.append("--- !u!65 &%d\nBoxCollider:\n  m_ObjectHideFlags: 0\n"
                    "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                    "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n"
                    "  m_Material: {fileID: 0}\n  m_IncludeLayers:\n    serializedVersion: 2\n"
                    "    m_Bits: 0\n  m_ExcludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n"
                    "  m_LayerOverridePriority: 0\n  m_IsTrigger: 0\n  m_ProvidesContacts: 0\n"
                    "  m_Enabled: 1\n  serializedVersion: 3\n  m_Size: {x: 1, y: 1, z: 1}\n"
                    "  m_Center: {x: 0, y: 0, z: 0}\n" % (cid, gid))
    EMIT.append("--- !u!1 &%d\nGameObject:\n  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n%s"
                "  m_Layer: 0\n  m_Name: %s\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n"
                "  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n"
                % (gid, ''.join(comps), name))
    EMIT.append("--- !u!4 &%d\nTransform:\n  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  serializedVersion: 2\n"
                "  m_LocalRotation: %s\n  m_LocalPosition: {x: %s, y: %s, z: %s}\n"
                "  m_LocalScale: {x: %s, y: %s, z: %s}\n  m_ConstrainProportionsScale: 0\n"
                "  m_Children: []\n  m_Father: {fileID: %d}\n"
                "  m_LocalEulerAnglesHint: {x: %s, y: %s, z: 0}\n"
                % (tid, gid, quat or yawq(yaw), num(pos[0]), num(pos[1]), num(pos[2]),
                   num(scale[0]), num(scale[1]), num(scale[2]), parent,
                   ('%.1f' % hint) if hint is not None else '0',
                   num(yaw) if hint is None else '0'))
    EMIT.extend(body)
    KIDS.setdefault(parent, []).append(tid)
    return tid

def gbox(name, pos, scale, quat=None, hint=None):
    return obj(name, ENV, pos, 0.0, scale, box=True, collider=True,
               quat=quat, hint=hint)

# ---- floors, ceilings ----------------------------------------------------------
for deck, y in DECK_Y.items():
    cs = D1 if deck == 1 else D2
    for (ix, iz) in sorted(cs):
        gbox('GFloor_%d_%d_%d' % (deck, ix, iz), (5 * ix + 2.5, y - 0.25, 5 * iz + 2.5), (5, .5, 5))
        obj('F%d_%d_%d' % (deck, ix, iz), DRESS, (5 * ix + 5, y, 5 * iz), 0.0, mesh=MESH['Floor'])
        if deck == 1 and (ix, iz) in VOID:
            continue          # open to the double-height void; CVoid caps it higher up
        low = ix in (-1, 0)
        obj('C%d_%d_%d' % (deck, ix, iz), DRESS, (5 * ix + 5, y, 5 * iz), 0.0,
            mesh=MESH['CeilLow'] if low else MESH['Ceil'])

# The void has no deck 2 floor, but it still needs the deck 2 ceiling over it or the
# drive core looks up into nothing.
for (ix, iz) in sorted(VOID):
    obj('CVoid_%d_%d' % (ix, iz), DRESS, (5 * ix + 5, DECK_Y[2], 5 * iz), 0.0,
        mesh=MESH['Ceil'])

# Ceiling lights. Identity rotation, seated so the panel's 0.102 m body tucks under the
# ceiling it hangs from - the same rule BastionSegmentVisuals uses. The old hull rotated
# these 90 degrees about X, which stood them on edge through the ceiling plane.
LIGHT_H = 0.102
lights = 0
for deck, y in DECK_Y.items():
    for (ix, iz) in sorted(D1 if deck == 1 else D2):
        if deck == 1 and (ix, iz) in VOID:
            continue          # LightVoid covers this column from higher up
        under = 3.49 if ix in (-1, 0) else 4.0        # Ceiling_02 over the spine columns
        obj('Light%d_%d_%d' % (deck, ix, iz), DRESS,
            (5 * ix + 2.5, y + under - LIGHT_H, 5 * iz + 2.5), 0.0, mesh=MESH['LightPanel'])
        lights += 1
for (ix, iz) in sorted(VOID):                          # high over the drive core
    obj('LightVoid_%d_%d' % (ix, iz), DRESS,
        (5 * ix + 2.5, DECK_Y[2] + 4.0 - LIGHT_H, 5 * iz + 2.5), 0.0, mesh=MESH['LightPanel'])
    lights += 1
print("  ceiling lights: %d" % lights)

# ---- hull walls: one line, one owner -------------------------------------------
# constant-z line -> N piece owned by the cell to the south
# constant-x line -> E piece owned by the cell to the west
HATCH_EDGES = {(1, 3), (1, 6), (1, 9)}          # deck 2 starboard cells carrying a hatch
perimeter = []
for deck, y in DECK_Y.items():
    cs = D1 if deck == 1 else D2
    solid = cs | (VOID if deck == 2 else set())   # the void is interior, not hull
    for (ix, iz) in sorted(cs):
        for k, (dx, dz) in (('N', (0, 1)), ('S', (0, -1)), ('E', (1, 0)), ('W', (-1, 0))):
            nb = (ix + dx, iz + dz)
            if nb in solid:
                continue
            if k == 'N':   own, px, pz, yaw = (ix, iz), 5 * ix + 5, 5 * iz + 5, 0.0
            elif k == 'E': own, px, pz, yaw = (ix, iz), 5 * ix + 5, 5 * iz, 90.0
            elif k == 'S': own, px, pz, yaw = nb, 5 * ix, 5 * iz, 180.0
            else:          own, px, pz, yaw = nb, 5 * ix, 5 * iz + 5, -90.0
            if own != (ix, iz) and own in solid:
                continue                        # the other cell owns this line
            perimeter.append((deck, y, k, px, pz, yaw, ix, iz))

# Window bays: the Exterior_Window piece has a real opening and is a full-thickness
# exterior wall, so it replaces BOTH the interior Wall and the Skin on its bay. The
# graybox stays, which keeps the opening sealed for the player. Bays are chosen away
# from the hatch bays, the wall-mounted stations, the M97 Mounts (starboard deck 1,
# z 16..19.6) and the M97 Marks (port deck 2, z 21.75..38.25 - which rules the port
# void rows out); the starboard void row still gives the drive core its view. The
# mesh is authored offset (Unity-local x[4.75,9.75]) and reversed, hence the +4.75
# pivot shift along the wall's local +x and the 180-degree yaw flip.
WINDOW_BAYS = {
    (1, 'W', -2, 5), (1, 'W', -2, 7), (1, 'E', 1, 9), (1, 'E', 1, 6),
    (2, 'W', -2, 2), (2, 'W', -2, 3), (2, 'W', -2, 8),
    (2, 'E', 1, 4), (2, 'E', 1, 5), (2, 'E', 1, 7),
}
windows = 0
for deck, y, k, px, pz, yaw, ix, iz in perimeter:
    if deck == 2 and k == 'E' and (ix, iz) in HATCH_EDGES:
        continue                                # the airlock shell fills this bay
    if (deck, k, ix, iz) in WINDOW_BAYS:
        ux, uz = math.cos(math.radians(yaw)), -math.sin(math.radians(yaw))
        obj('Win%d_%s_%d_%d' % (deck, k, int(px * 10), int(pz * 10)), DRESS,
            (px + 4.75 * ux, y, pz + 4.75 * uz), yaw + 180.0, mesh=MESH['Window'])
        windows += 1
        continue
    obj('W%d_%s_%d_%d' % (deck, k, int(px * 10), int(pz * 10)), DRESS,
        (px, y, pz), yaw, mesh=MESH['Wall'])
    ox, oz = {0.0: (0, WALL_T), 180.0: (0, -WALL_T),
              90.0: (WALL_T, 0), -90.0: (-WALL_T, 0)}[yaw]
    obj('Skin%d_%s_%d_%d' % (deck, k, int(px * 10), int(pz * 10)), DRESS,
        (px + ox, y, pz + oz), yaw, mesh=MESH['Skin'])

# graybox perimeter
for deck, y, k, px, pz, yaw, ix, iz in perimeter:
    if yaw in (0.0, 180.0):
        cx = px - 2.5 if yaw == 0.0 else px + 2.5
        cz = pz + .25 if yaw == 0.0 else pz - .25
        gbox('GW%d_%s_%d_%d' % (deck, k, int(px * 10), int(pz * 10)),
             (cx, y + 2, cz), (5, 4, .5))
    else:
        cx = px + .25 if yaw == 90.0 else px - .25
        cz = pz + 2.5 if yaw == 90.0 else pz - 2.5
        gbox('GW%d_%s_%d_%d' % (deck, k, int(px * 10), int(pz * 10)),
             (cx, y + 2, cz), (.5, 4, 5))

# ---- spine corridor ------------------------------------------------------------
# port line uses W pieces, starboard line uses E pieces, so the 2.5 m clear width is kept
SPINE_Z = [(iz, ROWS[iz]) for iz in sorted(ROWS)]
DOOR_ROWS = {1: 1, 3: 1, 4: 1, 8: 1, 11: 1}
for iz, (a, b) in SPINE_Z:
    for deck, y in DECK_Y.items():
        if deck == 2 and any((ix, iz) in VOID for ix in range(a, b + 1)):
            continue                            # landing rings the void, no spine here
        door = DOOR_ROWS.get(iz)
        for side, x, yaw in ((-1, -CORRIDOR_HALF, -90.0), (1, CORRIDOR_HALF, 90.0)):
            mesh = MESH['Door'] if door else MESH['Wall']
            obj('Spine%d_%s_%d' % (deck, 'P' if side < 0 else 'S', iz), DRESS,
                (x, y, 5 * iz + (5 if side < 0 else 0)), yaw, mesh=mesh)
            if not door:
                gbox('GSpine_%d_%s_%d' % (deck, 'P' if side < 0 else 'S', iz),
                     (x + side * .25, y + 2, 5 * iz + 2.5), (.5, 4, 5))



# ---- the void: railing on deck 2, stair up its port edge -----------------------
# Rail mesh (SM_Bld_Floor_Small_Hand_Rail_01) has its length along LOCAL Z, pivoted at
# the z=0 end, extending to z=-2.5. To lay it flush along a 5 m edge in two 2.5 m halves,
# the pivot for each half goes at the FAR end of that half (in the direction of travel)
# with yaw chosen so local +Z maps to the direction BACK toward the near end:
#   edge running +X (N/S walls): yaw = -90   edge running +Z (E/W walls): yaw = 180
# The previous version reused the WALL yaw convention (0/90), which is 90 deg off for a
# mesh whose long axis is Z instead of X - every rail ran perpendicular to its edge.
for (ix, iz) in sorted(VOID):
    for k, (dx, dz) in (('N', (0, 1)), ('S', (0, -1)), ('E', (1, 0)), ('W', (-1, 0))):
        if (ix + dx, iz + dz) in VOID:
            continue
        # box-orientation yaw: independent of the rail fix, kept as the wall convention
        if k == 'N':   bpx, bpz, byaw = 5 * ix + 5, 5 * iz + 5, 0.0
        elif k == 'S': bpx, bpz, byaw = 5 * ix + 5, 5 * iz, 0.0
        elif k == 'E': bpx, bpz, byaw = 5 * ix + 5, 5 * iz, 90.0
        else:          bpx, bpz, byaw = 5 * ix, 5 * iz, 90.0
        if k == 'W' and ix == -1 and iz == 6:
            continue          # the landing (z 30-35) opens onto the deck-2 ring here
        A = {'N': (5 * ix, 5 * iz + 5), 'S': (5 * ix, 5 * iz),
             'E': (5 * ix + 5, 5 * iz), 'W': (5 * ix, 5 * iz)}[k]
        ux, uz = (1, 0) if k in 'NS' else (0, 1)
        rail_yaw = -90.0 if k in 'NS' else 180.0
        mid = (A[0] + 2.5 * ux, A[1] + 2.5 * uz)
        far = (A[0] + 5.0 * ux, A[1] + 5.0 * uz)
        for half, piv in enumerate((mid, far)):
            obj('VoidRail_%s_%d_%d_%d' % (k, ix, iz, half), DRESS,
                (piv[0], 5.0, piv[1]), rail_yaw, mesh=MESH['Rail'])
        if byaw == 0.0:
            gbox('GVoid_%s_%d_%d' % (k, int(bpx * 10), int(bpz * 10)),
                 (bpx - 2.5, 5.6, bpz), (5, 1.2, 0.4))
        else:
            gbox('GVoid_%s_%d_%d' % (k, int(bpx * 10), int(bpz * 10)),
                 (bpx, 5.6, bpz + 2.5), (0.4, 1.2, 5))

# the kit stair rises 5.798 m; the decks are 5.0 apart, so scale it to land exactly
STAIR_S = 5.0 / 5.798
RUN = 8.022 * STAIR_S
obj('Stair', DRESS, (-5.0, 0.0, 25.0), 0.0, (STAIR_S, STAIR_S, STAIR_S), mesh=MESH['Stairs'])
obj('StairLanding', DRESS, (-5.0, 0.0, 25.0 + RUN), 0.0,
    (STAIR_S, 1.0, STAIR_S), mesh=MESH['StairPlat'])   # kit height (5.002 m) already fits;
                                                        # STAIR_S squashed it 0.687 m short

# walkable ramp under the stair: 5 m rise over RUN, inside the 45 deg slopeLimit
ang = math.degrees(math.atan2(5.0, RUN))
hq = math.radians(-ang) / 2.0
gbox('GStairRamp', (-4.0, 2.5, 25.0 + RUN / 2), (2.0, 0.4, math.hypot(RUN, 5.0)),
     quat='{x: %.7f, y: 0, z: 0, w: %.7f}' % (math.sin(hq), math.cos(hq)), hint=-ang)
# x=-4 (not -6): the visible Stair mesh spans x[-5.07,-3.11] inside the void's own
# footprint (ix=-1 is x[-5,0]) - it never crosses the void's west wall at x=-5. The old
# ramp sat one full bay further west, under solid deck-2 floor and grazing the z=25
# partition; at x=-4 it falls inside that partition's own excluded gap (x[-5,-1.25]).
print("stair: 5.0 m rise over %.2f m run = %.1f deg" % (RUN, ang))

# ---- airlock shells at the three hatch bays ------------------------------------
HATCH_Z, LEAF_R = {}, {}
for (ix, iz) in sorted(HATCH_EDGES):
    obj('AirlockShell_%d' % iz, DRESS, (5 * ix + 5, 5.0, 5 * iz), 90.0,
        mesh=MESH['Airlock'], submesh=AIRLOCK_SUBMESH)
    obj('AirlockLeaf_%d' % iz, DRESS, (5 * ix + 5, 5.0, 5 * iz), 90.0,
        mesh=MESH['Airlock'], submesh=AIRLOCK_LEAF)
    LEAF_R[iz] = LAST_R[0]
    HATCH_Z[iz] = 5 * iz + 2.5

# ShipHatchDoor tints the leaf renderer; rebind each to the shell it now owns.
# _hatchId is 1-based and ascends with z, matching Hatch_1..3 and ShipHatches 0..2.
order = sorted(HATCH_Z)
for d in docs:
    if d[1] != 114 or 'ShipHatchDoor' not in d[2]:
        continue
    hid = int(re.search(r'_hatchId: (\d+)', d[2]).group(1))
    d[2] = re.sub(r'_leaf: \{fileID: \d+\}',
                  '_leaf: {fileID: %d}' % LEAF_R[order[hid - 1]], d[2], count=1)
    print("  ShipHatchDoor %d -> leaf renderer %d (z %.1f)"
          % (hid, LEAF_R[order[hid - 1]], HATCH_Z[order[hid - 1]]))

# ---- rehome the fixed objects --------------------------------------------------
PLACE = {
    'Station_Armory':       ((10.0, 0.0, 12.5), -90.0),   # clear of the rack and plate
    'Station_Fabrication':  ((-10.0, 0.0, 17.5), 90.0),
    'Station_CrewQuarters': ((10.0, 0.0, 42.5), -90.0),
    'Station_Map':          ((-7.5, 5.0, 50.0), 180.0),
    'Ship Terminal':        ((-4.2, 5.0, 7.5), 90.0),   # clear of the x=-5 wall face
    'Demo Teleporter':      ((-7.5, 0.0, 42.5), 180.0),
    'Spawn_Teleporter':     ((-7.5, 1.1, 41.0), 180.0),
    'Player':               ((-7.5, 1.1, 41.0), 180.0),
    'Spawn_Nav':            ((0.0, 5.0, 42.5), 0.0),   # CIC; (0,6) is the void
    'Spawn_Intruder_1':     ((7.5, 5.0, 17.5), 180.0),
    'Spawn_Intruder_2':     ((7.5, 5.0, 47.5), 180.0),
}
# Scene hygiene: canvas-driven RectTransforms serialized at scale 0 while authoring
# offline (no canvas system running) - StationSceneGate hard-fails on them, and they
# ride along every regeneration unless normalised here.
for cnm in ('Canvas', 'Ship HUD Canvas'):
    for cid in by_name.get(cnm, []):
        d = byid[cid]
        d[2] = d[2].replace('m_LocalScale: {x: 0, y: 0, z: 0}',
                            'm_LocalScale: {x: 1, y: 1, z: 1}', 1)
print("  canvas scales normalised")

missing = []
for nm, (pos, yaw) in PLACE.items():
    ids = by_name.get(nm)
    if not ids:
        missing.append(nm); continue
    set_tf(ids[0], pos, yaw)
for i, iz in enumerate(sorted(HATCH_Z)):
    ids = by_name.get('Hatch_%d' % (i + 1))
    if ids:
        set_tf(ids[0], (9.65, 5.0, HATCH_Z[iz]), -90.0)
    else:
        missing.append('Hatch_%d' % (i + 1))


# ---- M97 realm record: authored at fixed world coordinates on the OLD hull ------
# Marks sat on the old gallery wall at x=25 and Mounts on the old armory wall at
# x=20. The rebuilt hull ends at x=10, so both were left outside the ship. Each
# group is repositioned as a whole, which preserves the spacing M97 authored.
#   Marks  local x 24.85 -> port wall of deck 2, faced back inboard by a 180 yaw
#   Mounts local x 19.55 -> starboard armory wall of deck 1, same facing
#   Plate  local x  9.54 -> nudged out to the new starboard face
REALM = {
    'Marks':        ((15.0, 5.0, 30.0), 180.0),
    'Mounts':       ((-9.7, 0.0, 0.0), None),
    'Record Plate': ((0.31, 0.0, 0.0), None),
}
for nm, (pos, yaw) in REALM.items():
    ids = by_name.get(nm)
    if not ids:
        print("  MISSING realm-record group", nm); continue
    set_tf(ids[0], pos, yaw)
    print("  realm record: %s group -> %s%s" % (nm, pos, '' if yaw is None else ' yaw %g' % yaw))

# ---- sealed hatches wore the hull atlas, so they read as wall ------------------
MAT_HATCH_SEALED = '660fe0a96d9d4de40aa50d53e1ba9e23'
ATLAS = '4cee7a5983b187943adc589b9cb1f084'
sealed = 0
for d in docs:
    if d[1] != 114 or 'ShipHatchDoor' not in d[2]:
        continue
    new = re.sub(r'(_sealedMaterial: \{fileID: 2100000, guid: )' + ATLAS,
                 r'\g<1>' + MAT_HATCH_SEALED, d[2], count=1)
    if new != d[2]:
        d[2] = new
        sealed += 1
print("  sealed-hatch material repointed on %d doors" % sealed)

# ---- per-station console colour was on Desk/Screen, both authored inactive -----
# Move it onto the visible ConsoleScreen so the station actually reads its colour;
# the console body stays on the shared atlas so it still reads as ship hardware.
STATION_MAT = {
    'Station_Armory': 'bcb552d38c7d16947bd2a3cf808141f9',
    'Station_CrewQuarters': 'c8fc703cbdc178340824051f016eb1ab',
    'Station_Fabrication': '926c67abdc4ca044493a75819238065e',
    'Station_Map': 'cda6a55ddf5f69e4e969e114ae94bc7e',
}
painted = 0
for sname, mguid in STATION_MAT.items():
    stf = by_name.get(sname)
    if not stf:
        continue
    for t, v in tf.items():
        if v['father'] != stf[0] or name_of.get(t) != 'ConsoleScreen':
            continue
        for c in go_comps.get(v['go'], []):
            if byid.get(c) and byid[c][1] == 23:
                byid[c][2] = byid[c][2].replace(
                    '- {fileID: 2100000, guid: %s, type: 2}' % ATLAS,
                    '- {fileID: 2100000, guid: %s, type: 2}' % mguid, 1)
                painted += 1
print("  station colour painted on %d console screens" % painted)


# ---- transverse partitions: rooms, not one 60 m strip ---------------------------
# A partition has to run from the hull to the spine wall, which is 8.75 m - not a
# multiple of the 5 m panel. It is closed with one full panel plus one scaled 0.75
# in X. Scaling a plain panel horizontally is safe: unlike the vertical scaling
# rejected above, there is no door aperture in it to distort, and the kit's
# flat-colour atlas has no horizontal detail to smear.
PARTITIONS = {1: [10.0, 20.0, 25.0, 35.0, 50.0], 2: [10.0, 50.0]}
FILL = 3.75 / 5.0

parts = 0
for deck, lines in PARTITIONS.items():
    y = DECK_Y[deck]
    cs = D1 if deck == 1 else D2
    for L in lines:
        iz_s, iz_n = int(L / 5) - 1, int(L / 5)
        for px, sx_, lo, hi in ((-5.0, 1.0, -10.0, -5.0), (-1.25, FILL, -5.0, -1.25),
                                (5.0, FILL, 1.25, 5.0), (10.0, 1.0, 5.0, 10.0)):
            mid = (lo + hi) / 2.0
            ixm = int(math.floor(mid / 5.0))
            if (ixm, iz_s) not in cs or (ixm, iz_n) not in cs:
                continue                      # no room either side of this stretch
            if deck == 1 and abs(L - 25.0) < 0.01 and px in (-1.25, 5.0):
                continue                      # port: the stair foot rises through this
                                               # bay; starboard: without this the void's
                                               # starboard rooms (ix=0,1 rows 5-6) have no
                                               # opening anywhere and are fully sealed -
                                               # found by the reachability checker.
            obj('Part%d_%d_%d' % (deck, int(L * 10), int(px * 10)), DRESS,
                (px, y, L), 0.0, (sx_, 1, 1), mesh=MESH['Wall'])
            gbox('GPart%d_%d_%d' % (deck, int(L * 10), int(px * 10)),
                 (mid, y + 2, L + 0.25), (hi - lo, 4, 0.5))
            parts += 1
print("  transverse partitions: %d panels" % parts)

# ---- M102 exterior: Normandy-style silhouette (Hull_* dress, no colliders) ------
# The box hull reads as a shipping container from outside. This section dresses it:
# an octagonal cross-section (45-degree chamfer ring above, skirt ring below, flat
# roof and belly), a tapered prow with a glazed canopy hump, a blunt engine stern
# with twin canted tail fins, and nacelle pods standing off the hull on pylons whose
# roots sit AFT of the pods (pods forward - the signature read). Dress only: the
# graybox perimeter already blocks the player and space outside is unreachable, so
# none of these carry colliders. Everything lives under one Hull group in ShipDress.

MESH.update({
    'Pod': 'c1c7be44267cd2c4c9c3c79e6fd94048',        # SM_Ship_Stealth_02
    'Fin': '4b9415ac1f63fc6448cd52da04c145db',        # SM_Veh_Part_Wing_06
    'Pylon': '29693e2d4b0dfbd4bb48c2b34e3a7707',      # SM_Veh_Part_Wing_07
    'EngineBig': '6441e525436b8ee4ea012960b80fa290',  # SM_Veh_Part_Engine_05
    'Tube': 'e19ea463faef7494480b9c6020d71082',       # SM_Prop_CenterTube_01
    'Canopy': '21c222af3ed37cd4b9e7f9b3671ca428',     # SM_Bld_Bridge_Walls_01
})
CANOPY_GLASS = 4300002                                # SM_Bld_Bridge_Walls_Glass_01
MAT_GLASS = "{fileID: 2100000, guid: 3fa8c21d7b6e4d0a92c5e8b1f4a6d973, type: 2}"

# -- quaternion helpers (Unity convention, same algebra yawq already uses) --------
def qmul(a, b):
    ax, ay, az, aw = a; bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)

def qaxis(axis, deg):
    l = math.sqrt(sum(c * c for c in axis))
    h = math.radians(deg) / 2.0
    s = math.sin(h) / l
    return (axis[0] * s, axis[1] * s, axis[2] * s, math.cos(h))

def qrot(q, v):
    x, y, z, w = q
    tx = 2 * (y * v[2] - z * v[1])
    ty = 2 * (z * v[0] - x * v[2])
    tz = 2 * (x * v[1] - y * v[0])
    return (v[0] + w * tx + y * tz - z * ty,
            v[1] + w * ty + z * tx - x * tz,
            v[2] + w * tz + x * ty - y * tx)

def qfmt(q):
    f = lambda v: ('%.7f' % v).rstrip('0').rstrip('.') if abs(v) > 1e-7 else '0'
    return '{x: %s, y: %s, z: %s, w: %s}' % tuple(f(v) for v in q)

def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0])

def qfrom_cols(c0, c1, c2):
    """Quaternion whose rotation maps local x/y/z to the given world columns."""
    m00, m10, m20 = c0; m01, m11, m21 = c1; m02, m12, m22 = c2
    tr = m00 + m11 + m22
    if tr > 0:
        s = math.sqrt(tr + 1.0) * 2
        q = ((m21 - m12) / s, (m02 - m20) / s, (m10 - m01) / s, s / 4)
    elif m00 > m11 and m00 > m22:
        s = math.sqrt(1.0 + m00 - m11 - m22) * 2
        q = (s / 4, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s)
    elif m11 > m22:
        s = math.sqrt(1.0 + m11 - m00 - m22) * 2
        q = ((m01 + m10) / s, s / 4, (m12 + m21) / s, (m02 - m20) / s)
    else:
        s = math.sqrt(1.0 + m22 - m00 - m11) * 2
        q = ((m02 + m20) / s, (m12 + m21) / s, s / 4, (m10 - m01) / s)
    for axis, col in (((1, 0, 0), c0), ((0, 1, 0), c1), ((0, 0, 1), c2)):
        got = qrot(q, axis)
        assert all(abs(g - c) < 1e-4 for g, c in zip(got, col)), 'bad quat %s' % (q,)
    return q

HULL_GID, HULL_TID = nid(), nid()
HN = [0]
def hpiece(name, pos, scale=(1, 1, 1), mesh=None, submesh=4300000, yaw=None,
           quat=None, mat=None):
    HN[0] += 1
    return obj('Hull_' + name, HULL_TID, pos, yaw or 0.0, scale, mesh=mesh,
               submesh=submesh, quat=quat, mat=mat)

# -- one flat exterior panel, placed by an edge line and a facing direction -------
# SM_Bld_Wall_Exterior_01 in Unity-local metres: x[-5,0] (run), y[-1.037,4] (height),
# z[0,0.713] (thickness, detail face outboard at z=0.713).
SKIN_YMIN, SKIN_YMAX = -1.037, 4.0

def panel(name, start, end, face, sy=1.0, anchor='pivot'):
    d = (end[0] - start[0], end[1] - start[1], end[2] - start[2])
    L = math.sqrt(d[0] ** 2 + d[1] ** 2 + d[2] ** 2)
    dn = (d[0] / L, d[1] / L, d[2] / L)
    fd = face[0] * dn[0] + face[1] * dn[1] + face[2] * dn[2]
    f = (face[0] - fd * dn[0], face[1] - fd * dn[1], face[2] - fd * dn[2])
    fl = math.sqrt(f[0] ** 2 + f[1] ** 2 + f[2] ** 2)
    f = (f[0] / fl, f[1] / fl, f[2] / fl)
    c0 = (-dn[0], -dn[1], -dn[2])       # local +x image; x[-5,0] spans start->end
    c1 = cross(f, c0)                   # local +y image (z cross x = y)
    if c1[1] < -1e-6:                   # run sense made local up point down: flip it
        start, end = end, start
        c0 = (dn[0], dn[1], dn[2])
        c1 = cross(f, c0)
    q = qfrom_cols(c0, c1, f)
    ay = {'ymin': SKIN_YMIN, 'ymax': SKIN_YMAX, 'pivot': 0.0}[anchor] * sy
    pos = (start[0] - ay * c1[0], start[1] - ay * c1[1], start[2] - ay * c1[2])
    return hpiece(name, pos, scale=(L / 5.0, sy, 1.0), mesh=MESH['Skin'],
                  quat=qfmt(q))

# -- P1: octagon ring. 45-degree chamfer band above the skin, skirt band below. ---
# Band cross-sections (from the panel above, before rotation):
#   chamfer: sy 0.8 -> 4.03 m of panel at 45 deg = edge y 8.8 up-in to y 11.65
#   skirt:   sy 0.55 -> 2.77 m of panel at 45 deg = edge y -0.75 down-in to y -2.71
# At every outline corner the two rectangular bands cross and mutually cover, which
# reads as a mitred hip; no corner pieces are needed.
CH_Y, CH_SY = 8.8, 0.8
SK_Y, SK_SY = -0.75, 0.55

def ring_strip(base, p0, p1, nrm):
    L = math.hypot(p1[0] - p0[0], p1[1] - p0[1])
    nseg = max(1, int(math.ceil(L / 5.0 - 1e-6)))
    for i in range(nseg):
        a = (p0[0] + (p1[0] - p0[0]) * i / nseg, p0[1] + (p1[1] - p0[1]) * i / nseg)
        b = (p0[0] + (p1[0] - p0[0]) * (i + 1) / nseg,
             p0[1] + (p1[1] - p0[1]) * (i + 1) / nseg)
        panel('Cham_%s_%d' % (base, i), (a[0], CH_Y, a[1]), (b[0], CH_Y, b[1]),
              (nrm[0], 1.0, nrm[1]), sy=CH_SY, anchor='ymin')
        panel('Skirt_%s_%d' % (base, i), (a[0], SK_Y, a[1]), (b[0], SK_Y, b[1]),
              (nrm[0], -1.0, nrm[1]), sy=SK_SY, anchor='ymax')

# Outline edge lines, tucked 0.15 inboard of the outer skin faces (mid sides
# x 11.605, narrow sides x 6.605, shoulders z 8.395 / 51.605, stern face z -1.605).
PW = 0.9217, 0.3879                     # prow flank normal (unit, starboard)
RING = [
    ('sm', (11.45, 8.55), (11.45, 51.45), (1, 0)),      # starboard mid
    ('pm', (-11.45, 8.55), (-11.45, 51.45), (-1, 0)),   # port mid
    ('ssn', (6.45, -1.45), (6.45, 8.55), (1, 0)),       # stern narrows
    ('psn', (-6.45, -1.45), (-6.45, 8.55), (-1, 0)),
    ('sbn', (6.45, 51.45), (6.45, 61.45), (1, 0)),      # bow narrows
    ('pbn', (-6.45, 51.45), (-6.45, 61.45), (-1, 0)),
    ('sbs', (6.45, 51.45), (11.45, 51.45), (0, 1)),     # bow shoulders
    ('pbs', (-11.45, 51.45), (-6.45, 51.45), (0, 1)),
    ('sss', (6.45, 8.55), (11.45, 8.55), (0, -1)),      # stern shoulders
    ('pss', (-11.45, 8.55), (-6.45, 8.55), (0, -1)),
    ('sf', (-6.45, -1.45), (6.45, -1.45), (0, -1)),     # stern face
    ('spf', (7.215, 59.79), (2.015, 73.69), (PW[0], PW[1])),   # prow flanks
    ('ppf', (-7.215, 59.79), (-2.015, 73.69), (-PW[0], PW[1])),
]
for base, p0, p1, nrm in RING:
    ring_strip(base, p0, p1, nrm)

# -- P1: roof and belly planes. Panels laid flat, detail face out. ----------------
ROOF_Y, BELLY_Y = 10.74, -1.887         # faces at 11.45 and -2.6
def flat(name, px, y, pz, w, sy, up):
    panel(name, (px, y, pz), (px - w, y, pz), (0, 1.0 if up else -1.0, 0), sy=sy)

for c, px in enumerate((10.0, 5.0, 0.0, -5.0)):         # mid body x[-10,10]
    for r in range(8):
        flat('Roof_m%d_%d' % (c, r), px, ROOF_Y, 14.0 + 5 * r, 5, 1.0, True)
        flat('Belly_m%d_%d' % (c, r), px, BELLY_Y, 11.0 + 5 * r, 5, 1.0, False)
for c, px in enumerate((7.5, 2.5, -2.5)):               # narrow bow/stern x[-7.5,7.5]
    for r, pz in enumerate((4.0, 9.0, 54.0, 59.0)):
        flat('Roof_n%d_%d' % (c, r), px, ROOF_Y, pz, 5, 1.0, True)
    for r, pz in enumerate((1.0, 6.0, 51.0, 56.0)):
        flat('Belly_n%d_%d' % (c, r), px, BELLY_Y, pz, 5, 1.0, False)

# -- P2: prow. Plan-tapered flanks z 61.5 -> 73.5 (half-width 6.55 -> 1.5), the
# chamfer/skirt bands continued along them (emitted with RING above), tapering
# roof/belly valley tiles between the band edges, and a vertical chisel cap. The
# profile keeps a level ridge; the taper is carried by the plan view.
# Start aft of the body corner (skin outer face x=6.605 ends at z=60) so the
# flank OVERLAPS the side skin - closure by overlap, like the corner mitres.
# The old start (6.55, 61.5) left an open notch x[5,6.6] z[60,61.5] both sides.
PROW_A, PROW_B = (6.7, 59.6), (1.5, 73.5)
for side in (1, -1):
    sfx = 's' if side > 0 else 'p'
    nrm = (side * PW[0], 0, PW[1])
    for row, (py, rsy) in enumerate(((0.0, 1.0), (5.0, 1.0))):
        a = (side * PROW_A[0], py, PROW_A[1])
        b = (side * PROW_B[0], py, PROW_B[1])
        d = math.hypot(PROW_B[0] - PROW_A[0], PROW_B[1] - PROW_A[1])
        for i in range(3):
            aa = tuple(a[j] + (b[j] - a[j]) * i / 3.0 for j in range(3))
            bb = tuple(a[j] + (b[j] - a[j]) * (i + 1) / 3.0 for j in range(3))
            panel('Prow_%s%d_%d' % (sfx, row, i), aa, bb, nrm, sy=rsy)
# valley tiles between the prow chamfer top edges (roof) / skirt bottom edges (belly)
for i, (pz, w) in enumerate(((63.8, 4.0), (66.8, 2.8), (69.8, 1.55), (72.8, 0.7))):
    flat('ProwRoof_%d' % i, w, ROOF_Y, pz, 2 * w, 0.6, True)
for i, (pz, w) in enumerate(((62.0, 4.7), (65.0, 3.5), (68.0, 2.2), (71.0, 1.0))):
    flat('ProwBelly_%d' % i, w, BELLY_Y, pz, 2 * w, 0.6, False)
# chisel cap: two full rows plus a short ridge cap
panel('ProwCap_0', (1.55, 0.0, 73.4), (-1.55, 0.0, 73.4), (0, 0, 1))
panel('ProwCap_1', (1.55, 5.0, 73.4), (-1.55, 5.0, 73.4), (0, 0, 1))
panel('ProwCap_2', (1.55, 8.9, 73.35), (-1.55, 8.9, 73.35), (0, 0, 1), sy=0.6)

# glazed canopy hump over the cockpit rows: the Bridge_Walls shell scaled onto the
# 10 m bow, glass band forward, seated just into the roof plane.
CANOPY_S = (0.61, 0.75, 0.71)
CANOPY_POS = (4.577, 12.1, 50.3)        # local x [-16.5,1.5] scaled, centred on 0
hpiece('Canopy', CANOPY_POS, scale=CANOPY_S, mesh=MESH['Canopy'])
hpiece('CanopyGlass', CANOPY_POS, scale=CANOPY_S, mesh=MESH['Canopy'],
       submesh=CANOPY_GLASS, mat=MAT_GLASS)

# -- P3: stern. Two big engines, a centre tube nozzle, twin canted tail fins. -----
for side in (1, -1):
    hpiece('Engine_%s' % ('s' if side > 0 else 'p'), (side * 3.6, 4.2, -5.6),
           scale=(1.3, 1.3, 1.3), mesh=MESH['EngineBig'])
hpiece('EngineCore', (0, 4.5, -0.2), scale=(1.2, 1.2, 1.2), mesh=MESH['Tube'],
       quat=qfmt(qaxis((1, 0, 0), -90.0)))            # tube axis y -> -z (aft)
FIN_S = (1.2, 1.4, 1.4)
FIN_HALF = 3.81 * FIN_S[1]
for side in (1, -1):
    ydir = qrot(qaxis((0, 0, 1), -30.0 * side), (0, 1, 0))   # cant top outboard
    base = (side * 3.8, 10.9, 3.5)                            # tip tucked in roof
    hpiece('Fin_%s' % ('s' if side > 0 else 'p'),
           (base[0] + FIN_HALF * ydir[0], base[1] + FIN_HALF * ydir[1], base[2]),
           scale=FIN_S, mesh=MESH['Fin'], quat=qfmt(qaxis((0, 0, 1), -30.0 * side)))

# -- P4: nacelle pods on forward-swept pylons. Roots on the mid hull at z 11 and
# z 16, pods standing off at x +-17 with noses reaching z 37 - pods FORWARD of the
# pylon roots, which is the read that matters.
POD_S = (0.55, 0.85, 1.15)
for side in (1, -1):
    sfx = 's' if side > 0 else 'p'
    hpiece('Pod_%s' % sfx, (side * 17.0, 6.0, 27.0), scale=POD_S, mesh=MESH['Pod'])
    for k, rz in enumerate((11.0, 16.0)):
        root = (side * 10.6, 6.0, rz)
        tip = (side * 17.2, 6.0, rz + 14.5)
        yaw = math.degrees(math.atan2(side * (tip[0] - root[0]), tip[2] - root[2]))
        length = math.hypot(tip[0] - root[0], tip[2] - root[2])
        hpiece('Pylon_%s%d' % (sfx, k),
               ((root[0] + tip[0]) / 2, 6.0, (root[2] + tip[2]) / 2),
               scale=(1.2, 1.3, length / 17.826), mesh=MESH['Pylon'],
               yaw=side * yaw)

# -- the Hull group itself, wired under ShipDress ---------------------------------
hull_kids = KIDS.pop(HULL_TID, [])
EMIT.append("--- !u!1 &%d\nGameObject:\n  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n"
            "  - component: {fileID: %d}\n"
            "  m_Layer: 0\n  m_Name: Hull\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n"
            "  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n"
            % (HULL_GID, HULL_TID))
EMIT.append("--- !u!4 &%d\nTransform:\n  m_ObjectHideFlags: 0\n"
            "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
            "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  serializedVersion: 2\n"
            "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n"
            "  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_ConstrainProportionsScale: 0\n"
            "  m_Children:\n%s"
            "  m_Father: {fileID: %d}\n"
            "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n"
            % (HULL_TID, HULL_GID,
               ''.join('  - {fileID: %d}\n' % k for k in hull_kids), DRESS))
KIDS.setdefault(DRESS, []).append(HULL_TID)
print("  exterior hull: %d pieces, %d window bays" % (HN[0], windows))

print("deck 1 cells %d, deck 2 cells %d, total %d" % (len(D1), len(D2), len(D1) + len(D2)))
print("emitted %d objects" % (len(KIDS.get(ENV, [])) + len(KIDS.get(DRESS, []))))
print("hatch z stations:", sorted(HATCH_Z.values()))
if missing:
    print("MISSING from scene:", missing)
if DRY:
    sys.exit(0)

# ---------------------------------------------------------------- write
keep = [d for d in docs if d[0] not in doomed_docs]
for p in (ENV, DRESS):
    d = byid[p]
    for t in subtree_before[p]:
        d[2] = d[2].replace('\n  - {fileID: %d}' % t, '', 1)
for parent, kids in KIDS.items():
    d = byid[parent]
    add = ''.join('  - {fileID: %d}\n' % k for k in kids)
    if re.search(r'^  m_Children: \[\]$', d[2], re.M):
        d[2] = re.sub(r'^  m_Children: \[\]$', '  m_Children:\n' + add.rstrip('\n'),
                      d[2], count=1, flags=re.M)
    else:
        d[2] = re.sub(r'(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)',
                      lambda m: m.group(1) + add, d[2], count=1)

out = head_txt + ''.join(d[2] for d in keep) + ''.join(EMIT)
ids = [int(x) for x in re.findall(r'^--- !u!\d+ &(\d+)', out, re.M)]
assert len(ids) == len(set(ids)), 'duplicate fileID emitted'
dang = sorted(x for x in set(int(v) for v in re.findall(r'fileID: (\d+)\}(?!, guid)', out))
              if x in doomed_docs)
assert not dang, 'refs to stripped objects remain: %s' % dang[:5]
open(SCENE, 'w', encoding='utf-8', newline='\n').write(out)
print("wrote %s (%d docs)" % (SCENE, len(ids)))
