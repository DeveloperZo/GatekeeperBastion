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
        box=False, renderer=True, collider=False, quat=None, hint=None):
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
            mat = MAT
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
        low = ix in (-1, 0)
        obj('C%d_%d_%d' % (deck, ix, iz), DRESS, (5 * ix + 5, y, 5 * iz), 0.0,
            mesh=MESH['CeilLow'] if low else MESH['Ceil'])

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

for deck, y, k, px, pz, yaw, ix, iz in perimeter:
    if deck == 2 and k == 'E' and (ix, iz) in HATCH_EDGES:
        continue                                # the airlock shell fills this bay
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
for (ix, iz) in sorted(VOID):
    for k, (dx, dz) in (('N', (0, 1)), ('S', (0, -1)), ('E', (1, 0)), ('W', (-1, 0))):
        if (ix + dx, iz + dz) in VOID:
            continue
        if k == 'N':   px, pz, yaw = 5 * ix + 5, 5 * iz + 5, 0.0
        elif k == 'S': px, pz, yaw = 5 * ix + 5, 5 * iz, 0.0
        elif k == 'E': px, pz, yaw = 5 * ix + 5, 5 * iz, 90.0
        else:          px, pz, yaw = 5 * ix, 5 * iz, 90.0
        if k == 'W' and ix == -1 and iz == 5:
            continue                              # the stair lands here, left open
        for half in (0, 1):
            ox = 2.5 * half if yaw == 0 else 0
            oz = 2.5 * half if yaw != 0 else 0
            obj('VoidRail_%s_%d_%d_%d' % (k, ix, iz, half), DRESS,
                (px - ox, 5.0, pz + oz), yaw, mesh=MESH['Rail'])
        if yaw == 0:
            gbox('GVoid_%s_%d_%d' % (k, int(px * 10), int(pz * 10)),
                 (px - 2.5, 5.6, pz), (5, 1.2, 0.4))
        else:
            gbox('GVoid_%s_%d_%d' % (k, int(px * 10), int(pz * 10)),
                 (px, 5.6, pz + 2.5), (0.4, 1.2, 5))

# the kit stair rises 5.798 m; the decks are 5.0 apart, so scale it to land exactly
STAIR_S = 5.0 / 5.798
RUN = 8.022 * STAIR_S
obj('Stair', DRESS, (-5.0, 0.0, 25.0), 0.0, (STAIR_S, STAIR_S, STAIR_S), mesh=MESH['Stairs'])
obj('StairLanding', DRESS, (-5.0, 0.0, 25.0 + RUN), 0.0,
    (STAIR_S, STAIR_S, STAIR_S), mesh=MESH['StairPlat'])

# walkable ramp under the stair: 5 m rise over RUN, inside the 45 deg slopeLimit
ang = math.degrees(math.atan2(5.0, RUN))
hq = math.radians(-ang) / 2.0
gbox('GStairRamp', (-6.0, 2.5, 25.0 + RUN / 2), (2.0, 0.4, math.hypot(RUN, 5.0)),
     quat='{x: %.7f, y: 0, z: 0, w: %.7f}' % (math.sin(hq), math.cos(hq)), hint=-ang)
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
    'Ship Terminal':        ((-5.0, 5.0, 7.5), 90.0),
    'Demo Teleporter':      ((-7.5, 0.0, 42.5), 180.0),
    'Spawn_Teleporter':     ((-7.5, 1.1, 41.0), 180.0),
    'Player':               ((-7.5, 1.1, 41.0), 180.0),
    'Spawn_Nav':            ((0.0, 5.0, 42.5), 0.0),   # CIC; (0,6) is the void
    'Spawn_Intruder_1':     ((7.5, 5.0, 17.5), 180.0),
    'Spawn_Intruder_2':     ((7.5, 5.0, 47.5), 180.0),
}
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
            if deck == 1 and abs(L - 25.0) < 0.01 and px == -1.25:
                continue                      # the stair foot rises through this bay
            obj('Part%d_%d_%d' % (deck, int(L * 10), int(px * 10)), DRESS,
                (px, y, L), 0.0, (sx_, 1, 1), mesh=MESH['Wall'])
            gbox('GPart%d_%d_%d' % (deck, int(L * 10), int(px * 10)),
                 (mid, y + 2, L + 0.25), (hi - lo, 4, 0.5))
            parts += 1
print("  transverse partitions: %d panels" % parts)

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
