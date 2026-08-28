"""M101 — rebuild the Ship - CoOp hull as ONE deck.

Authored offline as YAML with the editor closed, per the practice for this scene.
Strips every child of Environment (graybox) and ShipDress (Synty kit), emits the new
hull from the M101 layout table, and rehomes the fixed objects.

DESTRUCTIVE, and deliberately so: it deletes every child of Environment and ShipDress
and emits the hull from scratch. Running it discards any hand authoring inside those two
subtrees, exactly like SceneBuilder.Ship did. It is committed anyway because the scene
diff it produces is ~386k lines and cannot be reviewed directly - review this file and
Tools/scene-check/enclosure.py instead, and treat a re-run as a decision, never a chore.

Run against the scene as it stands - NOT against a scene reset to HEAD, which is what
this said until the reconciliation below. --dry prints the plan without writing, and
GKB_SHIP_SCENE points it at a copy so a run can be checked before it touches the real
one. Verify a change with Tools/scene-check/*, and by diffing the object graph of a
generated copy against the live scene; the raw text diff is unreadable.

RECONCILED 2026-08-26 against the live scene, not against HEAD. Hand passes landed
after this file last ran (the stair rebuild of 2026-08-23, the glazing and corner
pillars, the bow crown/keel patches, the deck-2 floor over the old stair well, the
terminal move). Re-keying the generator without folding those back in would have
silently deleted them inside a ~386k-line diff nobody can read. Every one of them is
now emitted from here, so a re-run reproduces today's ship rather than yesterday's.
Sections that carry hand geometry say so in a comment; see HAND-PASS markers.

COLLAPSED TO ONE DECK 2026-08-26, on top of that reconciliation. Deck 2 (y=5), the
double-height void, the void rail ring and the whole stair assembly are gone; everything
that stood upstairs came down to y=0 keeping its x and z, and the three airlocks came
with it. Two consequences worth stating rather than discovering:
  - The stair was a HAND PASS (rebuilt 2026-08-23) that the reconciliation above went to
    some trouble to preserve. It is DELETED here knowingly, not lost - see the block that
    used to hold it, kept as a comment where the code was.
  - The four void cells (-1,5) (0,5) (-1,6) (0,6) already had deck-1 floor and now take
    ceiling and light from the ordinary loops, because emptying VOID is all that was ever
    holding them out. Their ceiling is Ceiling_02 like the rest of the ix in (-1,0) spine
    columns, so the plane stays flat instead of stepping 0.51 m at the old void edge.
The exterior hull is deliberately NOT reprofiled for one deck - the ship's outside is to
be presented as an image, not modelled, so the shell above the deck stays hollow and
ROOF_Y, CH_Y, the pillars, prow, fins, engines, pods and pylons keep their two-deck
proportions. Tools/scene-check/enclosure.py says the same and inspects none of it.

LENGTHENED 2026-08-26, after the collapse and for the damage the collapse did. Two decks
of 76 cells became one deck of 40, and every wall in the ship went from having a use to
having three: the collapse note below ends by admitting the twelve realm-record marks
could not be placed at all, because at M97's 1.5 m spacing they want 16.5 m of clear wall
and the longest clear run on either side was 10 m. The owner's call was to lengthen the
interior rather than re-space or re-scale anything: "lengthen the space as needed. We wont
show the exterior. The exterior image planned would not need any changes."
So ROWS runs 0..15 instead of 0..11 - four more wide rows, 56 cells, z 0..80 - and the
three hatch bays re-space from 15 m apart to 20 m. What the extra 20 m actually buys is
listed against the ROWS table; the short version is that every zone now owns a
compartment instead of sharing one, and the marks have a room of their own.
TWO THINGS THE LENGTHENING DOES NOT TOUCH, both deliberate:
  - The exterior, on the owner's explicit instruction above. The shell still ends at
    z 73.5 while the interior now runs to z 80, so the bow rows stand outside their own
    hull. That is accepted, not overlooked: nothing renders the outside.
  - DOOR_ROWS. Tools/scene-check/reachability.py hardcodes the same five rows and this
    file is not allowed to drift from it, so the ship got longer without getting more
    spine doors. That is a constraint the layout below is built around rather than
    around it - see DOOR_ROWS and PARTITIONS.
"""
import re, sys, math, os

import os
SCENE = os.environ.get("GKB_SHIP_SCENE", "Assets/Scenes/Ship - CoOp.unity")
DRY = '--dry' in sys.argv

# ---------------------------------------------------------------- layout (M101 table)

ROWS = {0: (-1, 0), 1: (-1, 0),                                    # stern narrows
        2: (-2, 1), 3: (-2, 1), 4: (-2, 1), 5: (-2, 1),            # wide body,
        6: (-2, 1), 7: (-2, 1), 8: (-2, 1), 9: (-2, 1),            # twelve rows
        10: (-2, 1), 11: (-2, 1), 12: (-2, 1), 13: (-2, 1),
        14: (-1, 0), 15: (-1, 0)}                                  # bow narrows
# 56 cells over sixteen rows, z 0..80. The silhouette is unchanged - narrow stern, wide
# body, narrow bow - and only the wide body got longer, from eight rows to twelve.
#
# WHY 56 AND NOT 40, AND NOT 76. The number is not a target, it is what the following
# four things cost once each of them is given room to be itself:
#   1. A HALL FOR THE MARKS. Twelve marks split into two panels of six (owner's call)
#      want two facing walls at least 7.5 m long with clear ends. The only walls in this
#      ship that run unbroken for that far are the port hull wall and the port face of
#      the spine - and the spine face is only unbroken across rows that carry NO door.
#      DOOR_ROWS is fixed at {1,3,4,8,11}, so the longest doorless run amidships is rows
#      5-7: fifteen metres. That room has to exist and hold nothing else, which by
#      itself takes three rows out of circulation.
#   2. HATCH BAYS THAT ARE NOT ON TOP OF EACH OTHER. Three bays on the starboard wall
#      at 15 m centres (the collapsed ship) leave 10 m clear runs; at 20 m they leave
#      15 m, which is what lets the Armory, the Record Plate and a window bay each have
#      a bay of their own between them.
#   3. A PORT WALL LONG ENOUGH TO SEAT THREE THINGS. Fabrication, the M97 mount rack and
#      the hall need 10 + 5 + 15 m of port hull wall with partition lines between them.
#      The collapsed ship had 50 m of port wall and four things wanting it.
#   4. A BOW THAT IS NOT ALSO THE BRIDGE AND THE CREW DECK. Rows 11-15 split into a
#      hatch-3 vestibule with the CrewQuarters station and, past the z=65 bulkhead, the
#      NAV MAP at the bow bulkhead with the Nav spawn in front of it.
# Sixteen rows is the shortest table that does all four. The owner suggested 55-60 as a
# sanity range and 56 sits in it, but the range is a check on the reasoning above, not
# the source of it.
#
# There is no VOID any more, and no STAIR_HOLE before it. Both are deleted rather than
# emptied, for the same reason each time: every loop that skipped them also carried a
# special case for what to put over the hole instead, and with the hole gone the cells
# want the ORDINARY treatment, not a skip plus a compensating block.
#   STAIR_HOLE was {(-2,5), (-2,6)}, the deck-2 floor punched out over the port stair
#   well; it died when the 2026-08-23 rebuild moved the stair inboard to the ix=-1
#   column, and the floor went back in by hand.
#   VOID was {(-1,5), (0,5), (-1,6), (0,6)}, the double-height well over the drive core:
#   deck 1 had floor there but no ceiling and no light, deck 2 had neither floor nor
#   spine, and a rail ring plus eight GVoid boxes fenced the drop. One deck has no
#   two-storey space to open, so all of that goes and the four cells become ordinary
#   deck - they are ix in (-1, 0), the spine columns, so they take Ceiling_02 and the
#   3.49 m light drop from their neighbours and the ceiling plane stays flat.
DECK_Y = {1: 0.0}                               # ONE deck. Deck 2 stood at y=5.
CORRIDOR_HALF = 1.25                            # spine runs x[-1.25, 1.25]

def cells():
    out = set()
    for iz, (a, b) in ROWS.items():
        for ix in range(a, b + 1):
            out.add((ix, iz))
    return out

# D1 is kept as a name (rather than folded into `cells()` at each use) because the
# generated names still carry a `1` deck token - enclosure.py parses it, purely to prove
# it is always the same value - and reading `D1` next to `F1_`/`W1_` keeps the two tied.
D1 = cells()

MESH = {   # Synty PolygonSciFiSpace, guids are stable across the pack
    'Floor': '549f0d5274edea6458f754e1aac98efb',         # SM_Bld_Floor_01
    'Ceil': '2a5a2a9a9489337489fb0458762db8eb',          # SM_Bld_Ceiling_01
    'CeilLow': '09bfb280021fbcd41ad1d71823655ea1',       # SM_Bld_Ceiling_02
    'Wall': '8d217d0e1fada5e4591985711fc87b86',          # SM_Bld_Wall_01
    'Door': '31d557d4571f9d84c939fd1ce47b5cd8',          # SM_Bld_Wall_Doorframe_01
    'Skin': '3bdbd97455c79064c8b39a7764c17bd6',          # SM_Bld_Wall_Exterior_01
    'SkinPillar': 'ad3df2d4b491d454c96764f575db0130',    # SM_Bld_Wall_Exterior_Pillar_01
    'Window': '4f630417843179749aa8ba87d708a645',        # SM_Bld_Wall_Exterior_Window_01
    # 'Stairs'/'StairPlat'/'Rail' lived here until the one-deck collapse. Stairs drove the
    # kit stair, Rail the void ring, StairPlat nothing at all (dead since the 2026-08-23
    # rebuild). Nothing climbs or overlooks anything now, so all three guids go with the
    # geometry rather than sitting here inviting a re-use with no upper floor to reach:
    # SM_Prop_Stairs_01, SM_Prop_StairsPlatform_01, SM_Bld_Floor_Small_Hand_Rail_01.
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
    # A string passes through verbatim. Everything this generator computes is round to
    # well within 4 dp, but hand-authored transforms transcribed out of the scene carry
    # the editor's 7 significant figures, and rounding them here would re-serialise the
    # hand pass as something slightly different from what is in the file - a diff that
    # looks like drift and is not. Transcribe those as strings.
    if isinstance(v, str):
        return v
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

# HAND-PASS: hull dress that was authored as LOOSE SCENE ROOTS, outside both groups
# this generator owns. Stripping only the Environment and ShipDress subtrees left all
# eighteen of these floating at their old world coordinates after a re-run - the ship
# would move and its glazing, corner pillars and bow patches would not. They are
# emitted from here now (see "hand-pass hull closures" below), so they must also be
# torn down here, or a re-run would end up with two of each.
#   Glass_<side><deck>_<n>  glazing panes, one per window bay
#   SkinPillar_<corner>     vertical corner caps over the skin band's mitre seams
#   Hull_RoofPatch_0 / Hull_BellyPatch_0   bow crown and keel notch fillers
LOOSE_HULL_ROOTS = re.compile(r'^(Glass_[EW][12]_\d+|SkinPillar_\w+|Hull_(Roof|Belly)Patch_\d+)$')
loose_tf = sorted(t for t, v in tf.items()
                  if v['father'] == 0 and LOOSE_HULL_ROOTS.match(name_of.get(t, '')))
for t in loose_tf:
    doomed_tf.append(t)
    doomed_tf.extend(subtree(t))

# Wayfinding that outlived what it pointed at. These hang under Stations/Station_Map,
# NOT under either group this generator owns, so a strip would never reach them and they
# have to be named. Deleting rather than re-aiming, and the unlink from the father's
# m_Children is done in the write step alongside the ENV/DRESS one.
#   Sign_StairsToNav  "STAIRS TO NAV", world (-4, 3.2, 33) - the port stair the one-deck
#                     collapse just deleted. A sign to a staircase that is not there is
#                     worse than no sign: it sends the player looking for a way up.
# Sign_TerminalDoor is NOT here. It is stale too, but stale in a way that has a right
# answer, so it is re-aimed instead - see SIGN_FIX below.
STALE_SIGNS = ('Sign_StairsToNav',)
sign_unlink = []
for _snm in STALE_SIGNS:
    for t in by_name.get(_snm, []):
        sign_unlink.append((tf[t]['father'], t))
        doomed_tf.append(t)
        doomed_tf.extend(subtree(t))
        print("deleting stale sign %s (was under %s)"
              % (_snm, name_of.get(tf[t]['father'], '<ROOT>')))

doomed_docs = set()
for t in doomed_tf:
    gid = tf[t]['go']
    doomed_docs.add(gid); doomed_docs.add(t)
    for c in go_comps.get(gid, []):
        doomed_docs.add(c)
print("stripping %d old hull objects (%d docs), %d of them loose scene roots"
      % (len(doomed_tf), len(doomed_docs), len(loose_tf)))

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
# Index of each emitted Transform's slot in EMIT, so an object emitted by this run can
# itself be a parent. The write step used to reach for byid[parent], which only exists
# for docs that were already in the scene; the Hull group worked around that by popping
# its own kids and hand-building its Transform. The hand-rebuilt stair needs the same
# thing (two ramp boxes under an emitted Stair), so it is a mechanism now, not a
# one-off. Hull keeps its bespoke path - it has no mesh and reads fine as it is.
EMIT_AT = {}

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
        box=False, renderer=True, collider=False, mesh_col=None, quat=None, hint=None, mat=None,
        box_mat=None, hint3=None):
    # box_mat turns a graybox into a VISIBLE primitive-cube panel: same Unity Cube mesh
    # and unit BoxCollider, but the renderer stays on and wears the given material.
    # The hand passes used exactly this shape for the window glazing, the hull corner
    # pillars and the bow patches - cheap flat slabs that had to be seen, not just felt.
    gid, tid = nid(), nid()
    comps = ["  - component: {fileID: %d}\n" % tid]
    body = []
    if mesh or box:
        fid, rid = nid(), nid()
        comps.append("  - component: {fileID: %d}\n" % fid)
        comps.append("  - component: {fileID: %d}\n" % rid)
        if box:
            mref = "{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}"
            mat = box_mat or "{fileID: 10303, guid: 0000000000000000f000000000000000, type: 0}"
        else:
            mref = "{fileID: %d, guid: %s, type: 3}" % (submesh, mesh)
            mat = mat or MAT
        body.append("--- !u!33 &%d\nMeshFilter:\n  m_ObjectHideFlags: 0\n"
                    "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                    "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  m_Mesh: %s\n"
                    % (fid, gid, mref))
        body.append(RENDERER.format(rid=rid, gid=gid, mat=mat,
                                    en=(0 if (box and not box_mat) else 1)))
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
    if mesh_col:
        cid = nid()
        comps.append("  - component: {fileID: %d}\n" % cid)
        body.append("--- !u!64 &%d\nMeshCollider:\n  m_ObjectHideFlags: 0\n"
                    "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                    "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n"
                    "  m_Material: {fileID: 0}\n  m_IncludeLayers:\n    serializedVersion: 2\n"
                    "    m_Bits: 0\n  m_ExcludeLayers:\n    serializedVersion: 2\n    m_Bits: 0\n"
                    "  m_LayerOverridePriority: 0\n  m_IsTrigger: 0\n  m_ProvidesContacts: 0\n"
                    "  m_Enabled: 1\n  serializedVersion: 5\n  m_Convex: 0\n"
                    "  m_CookingOptions: 30\n  m_Mesh: {fileID: 4300000, guid: %s, type: 3}\n"
                    % (cid, gid, mesh_col))
    # m_LocalEulerAnglesHint is editor-only bookkeeping, but it is what the inspector
    # shows, so a wrong one reads as a wrong object to whoever opens the scene next.
    # hint3 gives it verbatim for objects whose rotation came from a hand pass and is
    # not a plain yaw; hint keeps the old single-pitch shorthand for a tilted graybox.
    if hint3 is not None:
        hx, hy, hz = (num(v) for v in hint3)
    elif hint is not None:
        hx, hy, hz = ('%.1f' % hint), '0', '0'
    else:
        hx, hy, hz = '0', num(yaw), '0'
    EMIT.append("--- !u!1 &%d\nGameObject:\n  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n%s"
                "  m_Layer: 0\n  m_Name: %s\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n"
                "  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n"
                % (gid, ''.join(comps), name))
    EMIT_AT[tid] = len(EMIT)          # slot of the Transform doc appended next
    EMIT.append("--- !u!4 &%d\nTransform:\n  m_ObjectHideFlags: 0\n"
                "  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n"
                "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: %d}\n  serializedVersion: 2\n"
                "  m_LocalRotation: %s\n  m_LocalPosition: {x: %s, y: %s, z: %s}\n"
                "  m_LocalScale: {x: %s, y: %s, z: %s}\n  m_ConstrainProportionsScale: 0\n"
                "  m_Children: []\n  m_Father: {fileID: %d}\n"
                "  m_LocalEulerAnglesHint: {x: %s, y: %s, z: %s}\n"
                % (tid, gid, quat or yawq(yaw), num(pos[0]), num(pos[1]), num(pos[2]),
                   num(scale[0]), num(scale[1]), num(scale[2]), parent,
                   hx, hy, hz))
    EMIT.extend(body)
    KIDS.setdefault(parent, []).append(tid)
    return tid

def gbox(name, pos, scale, quat=None, hint=None):
    return obj(name, ENV, pos, 0.0, scale, box=True, collider=True,
               quat=quat, hint=hint)

# ---- floors, ceilings ----------------------------------------------------------
for deck, y in DECK_Y.items():
    for (ix, iz) in sorted(D1):
        gbox('GFloor_%d_%d_%d' % (deck, ix, iz), (5 * ix + 2.5, y - 0.25, 5 * iz + 2.5), (5, .5, 5))
        obj('F%d_%d_%d' % (deck, ix, iz), DRESS, (5 * ix + 5, y, 5 * iz), 0.0, mesh=MESH['Floor'])
        # No VOID skip any more, which is the whole of what the four ex-void cells needed:
        # (-1,5) (0,5) (-1,6) (0,6) already had floor and now get the ceiling they never
        # had. They are ix in (-1, 0), so `low` is True and they take Ceiling_02 (3.49 m)
        # like the rest of the spine columns - Ceiling_01 would leave a 0.51 m step in the
        # ceiling plane right where the void used to open.
        low = ix in (-1, 0)
        obj('C%d_%d_%d' % (deck, ix, iz), DRESS, (5 * ix + 5, y, 5 * iz), 0.0,
            mesh=MESH['CeilLow'] if low else MESH['Ceil'])

# `CVoid_<ix>_<iz>` used to cap the double-height well at the deck-2 plane so the drive
# core did not look up into nothing. There is no well and no deck-2 plane; the four cells
# take their ceiling from the loop above at y=0 + the ordinary drop. enclosure.py fails on
# any CVoid_* left in the scene, so this is not a silent deletion.

# Ceiling lights. Identity rotation, seated so the panel's 0.102 m body tucks under the
# ceiling it hangs from - the same rule BastionSegmentVisuals uses. The old hull rotated
# these 90 degrees about X, which stood them on edge through the ceiling plane.
LIGHT_H = 0.102
lights = 0
for deck, y in DECK_Y.items():
    for (ix, iz) in sorted(D1):
        # Same story as the ceilings: the VOID skip and the `LightVoid_*` panels that hung
        # 4 m over the deck-2 plane are both gone, so the four ex-void cells take the
        # ordinary panel. `under` is 3.49 for them because ix is -1 or 0 - the light has to
        # follow the ceiling it is tucked under, or it hangs 0.51 m proud of the plane.
        under = 3.49 if ix in (-1, 0) else 4.0        # Ceiling_02 over the spine columns
        obj('Light%d_%d_%d' % (deck, ix, iz), DRESS,
            (5 * ix + 2.5, y + under - LIGHT_H, 5 * iz + 2.5), 0.0, mesh=MESH['LightPanel'])
        lights += 1
print("  ceiling lights: %d" % lights)

# ---- hull walls: one line, one owner -------------------------------------------
# constant-z line -> N piece owned by the cell to the south
# constant-x line -> E piece owned by the cell to the west
# RE-SPACED for the longer hull. Rows 3/6/9 put the bays at z 17.5/32.5/47.5, which was
# centred on a 60 m ship and is a third of the way up an 80 m one. Rows 3/7/11 put them
# at z 17.5/37.5/57.5: 20 m apart, symmetric about z 37.5, and the middle bay lands 2.5 m
# aft of the walkable mid-length (z 40) - inside M101 criterion 6's ~5 m, and printed by
# enclosure.py every run so it stays honest.
# The ORDER is the thing to be careful with, not the spacing. `sorted(HATCH_EDGES)`
# feeds the _hatchId binding below, so these must ascend in z and the middle one must
# stay the middle one; nothing in the repo tests it.
HATCH_EDGES = {(1, 3), (1, 7), (1, 11)}         # starboard cells carrying a hatch
perimeter = []
for deck, y in DECK_Y.items():
    cs = D1
    solid = cs                    # no void to hold out of the hull outline any more
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
# graybox stays, which keeps the opening sealed for the player. The mesh is authored
# offset (Unity-local x[4.75,9.75]) and reversed, hence the +4.75 pivot shift along the
# wall's local +x and the 180-degree yaw flip.
#
# RE-CUT AGAIN for the lengthened hull, two a side instead of two port and one starboard.
# A bay must be clear of everything that owns wall: the three hatch bays (E rows 3/7/11),
# the four wall-mounted stations, the M97 mount rack (port hull wall, z 20.5..24.5) and
# mark panel A (port hull wall, z 28.5..36.5 - see REALM; panel B hangs on the spine and
# takes no bay). The glazing loop at the foot of this
# file walks WINDOW_BAYS on its own, so a bay listed here that collides with any of those
# hangs a 4.9 m pane through it; the perimeter loop's hatch skip would hide the mistake.
#
# All four are inside z 10..50 on purpose. The exterior is not being reprofiled, so the
# mid-body skin band only exists between z 8.55 and z 51.45; a bay forward of that would
# be an opening onto the prow taper rather than onto space. Rows 9 both sides is the
# amidships pair the player spawns in front of.
WINDOW_BAYS = {
    (1, 'W', -2, 3), (1, 'W', -2, 9),           # port: shop end, muster
    (1, 'E', 1, 5), (1, 'E', 1, 9),             # starboard: gallery, muster
}
windows = 0
for deck, y, k, px, pz, yaw, ix, iz in perimeter:
    if k == 'E' and (ix, iz) in HATCH_EDGES:
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
# NOT WIDENED WITH THE SHIP, and that is a constraint rather than an oversight.
# Tools/scene-check/reachability.py carries the same five row numbers as a literal and
# says "must match Tools/ship-hull/build_hull.py"; it models a room as joined to the
# corridor ONLY at these rows. Adding a sixth door here would make the checker's model
# quietly less connected than the ship, which is the direction that still passes and so
# the direction nobody would ever catch. So the ship got four rows longer on five doors.
# Two things fall out of that and are load-bearing everywhere below:
#   - Rows 5-7 and 12-15 carry solid spine wall for their whole length. Row 5-7's port
#     face is the only 15 m of unbroken inboard wall in the ship, and it is what the
#     second mark panel is mounted on (see REALM).
#   - Every compartment must contain a door row or touch one through a gap in a
#     partition. PARTITIONS below is written to that rule and says so.
DOOR_ROWS = {1: 1, 3: 1, 4: 1, 8: 1, 11: 1}
for iz, (a, b) in SPINE_Z:
    for deck, y in DECK_Y.items():
        # Rows 5 and 6 used to be skipped up here: deck 2 rang the void with a landing
        # instead of running the corridor through it. One deck means the spine runs the
        # full sixteen rows with nothing to detour around.
        door = DOOR_ROWS.get(iz)
        for side, x, yaw in ((-1, -CORRIDOR_HALF, -90.0), (1, CORRIDOR_HALF, 90.0)):
            mesh = MESH['Door'] if door else MESH['Wall']
            obj('Spine%d_%s_%d' % (deck, 'P' if side < 0 else 'S', iz), DRESS,
                (x, y, 5 * iz + (5 if side < 0 else 0)), yaw, mesh=mesh)
            if not door:
                gbox('GSpine_%d_%s_%d' % (deck, 'P' if side < 0 else 'S', iz),
                     (x + side * .25, y + 2, 5 * iz + 2.5), (.5, 4, 5))



# ---- the void ring and the stair: BOTH DELETED for one deck --------------------
# Nothing is emitted between the spine above and the airlocks below any more. Two whole
# assemblies died here, and one of them was a hand pass the 2026-08-26 reconciliation had
# just finished folding back in, so neither is allowed to vanish without a record.
#
# THE VOID RING. Sixteen `VoidRail_*` halves of SM_Bld_Floor_Small_Hand_Rail_01 fencing
# the four void cells at y=5, plus eight `GVoid_*` collider bars at y=5.6. Every one of
# them existed to stop a two-storey fall into the drive-core well. There is no well and
# no second storey, so a rail around four ordinary floor cells would be a fence in the
# middle of a room. The two facts that block reasoned most carefully about are both moot
# and are recorded here rather than carried: the rail mesh runs along LOCAL Z pivoted at
# its z=0 end (so a flush 5 m edge wanted yaw -90 on N/S runs and 180 on E/W, not the
# 0/90 wall convention), and the south edge of cell (-1,5) carried a HALF-width collider
# because the stair ramp crossed the boundary there.
#
# THE STAIR (HAND-PASS, rebuilt 2026-08-23; Docs/Changelog.md:297). DELETED KNOWINGLY.
# `Stair` was SM_Prop_Stairs_01 at (-5, 0.0058, 25.218) scaled 0.8624 = 5.0/5.798, the
# factor that made the kit's 5.798 m rise land exactly on the 5.0 m deck gap, with two
# grayboxes parented under it: `GStairRamp`, the walk face, and `GStairRamp (1)`, the head
# pad. The reconciliation went out of its way to preserve all three - they were authored
# by eye in the editor, transcribed rather than re-derived so re-serialising would not
# move them. They are removed here because the only thing they did was carry the player
# between two floor planes, and there is one plane now; keeping a staircase to nowhere
# would fail reachability.py's single-plane assertion on the head pad alone.
# The one lesson in that block worth outliving it, because it is about the rig and not
# about the stair: a walk surface must be THICK. The first ramp was 0.05 m (half-thickness
# 0.025), thinner than the CharacterController's own skinWidth of 0.08, and a live
# FirstPersonController on it lifted, popped and fell back through - classic thin-collider
# instability that the straight scripted path in ShipCoopStairWalkTests never saw. The
# rebuilt ramp was 0.40 m in world, a 5x margin. If a ramp is ever wanted here again,
# start there, and do not put a MeshCollider on SM_Prop_Stairs_01: the step faces wall the
# capsule and that mesh measures 54 deg, past the rig's 45 deg slopeLimit.

# ---- airlock shells at the three hatch bays ------------------------------------
# Down from y=5 with the rest of the ship in the collapse, and re-spaced along z by the
# lengthening: the bays are now z 17.5 / 37.5 / 57.5 (rows 3/7/11, see HATCH_EDGES), not
# the 17.5 / 32.5 / 47.5 of the 60 m hull. The x is unchanged. Only bays 2 and 3 moved,
# and they moved FORWARD, so `sorted(HATCH_EDGES)` still ascends and id 1 is still the
# aft door - see the _hatchId note below, which is the thing a re-space breaks silently.
HATCH_Z, LEAF_R = {}, {}
for (ix, iz) in sorted(HATCH_EDGES):
    obj('AirlockShell_%d' % iz, DRESS, (5 * ix + 5, DECK_Y[1], 5 * iz), 90.0,
        mesh=MESH['Airlock'], submesh=AIRLOCK_SUBMESH)
    obj('AirlockLeaf_%d' % iz, DRESS, (5 * ix + 5, DECK_Y[1], 5 * iz), 90.0,
        mesh=MESH['Airlock'], submesh=AIRLOCK_LEAF)
    LEAF_R[iz] = LAST_R[0]
    HATCH_Z[iz] = 5 * iz + 2.5

# ShipHatchDoor tints the leaf renderer; rebind each to the shell it now owns.
# _hatchId is 1-based and ascends with z, matching Hatch_1..3 and ShipHatches 0..2.
# The binding is by fileID, so it MUST be re-made every run - and the ordering below is
# the only thing keeping id 1 on the aft door. Nothing in the repo tests hatch ordering:
# flip the z order and _hatchId quietly addresses the wrong door for the rest of the
# game's life. The printed lines are the check; read them.
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
# RE-LAID for the sixteen-row hull. Yaw is the wall the console stands against, not the
# way the player faces: -90 is the starboard hull wall (x=10) looking inboard, 90 the
# port hull wall (x=-10), 180 a forward bulkhead looking aft. Every entry names the
# compartment it belongs to, because a compartment here is a partition-bounded band of
# rows and "z=22.5" on its own does not say which room that is.
#
# The six compartments, stern to bow (PARTITIONS below cuts them):
#   rows 0-1    z 0..10   stern narrows, machinery. Empty, and meant to be.
#   rows 2-4    z 10..25  the shop end: Fabrication and the M97 mount rack to port, the
#                         hatch-1 bay and the Armory to starboard. One working space.
#   rows 5-7    z 25..40  Hall of Record (port, see REALM) / gallery with the Record
#                         Plate and hatch bay 2 (starboard).
#   rows 8-10   z 40..55  muster: Ship Terminal, teleporter, player spawn, both windows.
#   rows 11-12  z 55..65  hatch bay 3 and the CrewQuarters station.
#   rows 13-15  z 65..80  the bridge: NAV MAP on the bow bulkhead, Nav spawn facing it.
PLACE = {
    # The shop end. The Armory moved forward from z 12.5 to row 4 so it stands directly
    # abeam the M97 mount rack, which the owner put on the port wall at z 20..25 -
    # "opposite the Armory across the spine" only means anything if the Armory is in the
    # same band of rows as the rack.
    'Station_Armory':       ((10.0, 0.0, 22.5), -90.0),
    # Fabrication moves two rows aft to the far end of the same compartment, so the port
    # wall reads Fabrication - window bay - rack over its 15 m instead of stacking the
    # station and the rack in one bay. 8 m of clear wall between the two.
    'Station_Fabrication':  ((-10.0, 0.0, 12.5), 90.0),
    # Forward, in the hatch-3 vestibule rather than amidships: crew quarters next to the
    # airlock the crew comes back through. 2.5 m clear of the z=65 bulkhead.
    'Station_CrewQuarters': ((10.0, 0.0, 62.5), -90.0),
    # The NAV MAP is the bridge now, not a console in a corridor. It stands against the
    # bow bulkhead at z=80 in the port slot of the narrows, facing aft (yaw 180) at
    # whoever walks up the room to it. Its pivot is ON the z=80 plane, so enclosure.py
    # reaches cell (-1,15) through its half-metre probe, exactly as it did when this
    # sat on the z=50 partition plane.
    # x=-3.571 is the CENTRE of that slot, not the centre of the cell: the cell runs
    # x[-5,-1.25] but the port spine wall's 0.892 m body eats back to x=-2.142, so the
    # clear width is 2.858 and its middle is -3.571. The visible console is
    # SM_Prop_ControlPanel_02 at 0.1284 scale = 1.20 m wide, so it leaves 0.83 m of
    # standing room on either side. Put it on the cell centre instead and it crowds the
    # spine wall.
    'Station_Map':          ((-3.571, 0.0, 80.0), 180.0),
    # HAND-PASS lineage: (-4.2, 5.0, 7.5) on deck 2, then (-4.0, 0.0, 21.0) after the
    # collapse. It is at z=41 now for the same reason it was at z=21 then - it wants to
    # be beside a spine door, in the compartment the player starts in, because it is the
    # first terminal they are meant to find. The player spawn moved to the muster
    # compartment (rows 8-10) when the ship grew, so the terminal came with it: cell
    # (-1,8), 5 m from the spawn, on the port door of row 8 and at the same z as the
    # Sign_TerminalDoor that points at it.
    # z=42.5 is NOT the 1.0 m standoff it carried before, and dropping that is a repair.
    # It is a free-standing pedestal, not a wall fitting - its children straddle its own
    # origin - so the "off the x=-5 wall face" in the note it inherited was never true,
    # and 1.0 m from a partition LINE is not 1.0 m from a partition: the wall body sits
    # on the +Z side of the line, so at z=21 the visible console (ControlPanel_02 at
    # 0.1284 scale, 1.20 m wide, turned across z by the yaw of 90) ran z 20.4..21.6 and
    # spent 0.49 m of that inside Part1_200_-12. It has been buried in that panel since
    # the collapse placed it. Centring it on the door row instead clears the z=40 panel
    # by 0.51 m and leaves 1.38 m between the console and the spine wall.
    'Ship Terminal':        ((-4.0, 0.0, 42.5), 90.0),
    # Muster, port side, in front of the row-9 window bay. The teleporter/spawn pair
    # keeps its authored 1.5 m offset.
    'Demo Teleporter':      ((-7.5, 0.0, 47.5), 180.0),
    'Spawn_Teleporter':     ((-7.5, 1.1, 46.0), 180.0),
    'Player':               ((-7.5, 1.1, 46.0), 180.0),
    # Nav spawns AT the bridge, 4 m in front of the map and facing it (yaw 0). It used
    # to spawn in the corridor; with the bow narrows walled off the corridor's whole
    # length, a corridor spawn at z=76 would be a 25 m walk back to the row-11 door and
    # forward again to reach the console it exists to use.
    'Spawn_Nav':            ((-3.571, 0.0, 76.0), 0.0),  # bridge, cell (-1,15)
    'Spawn_Intruder_1':     ((7.5, 0.0, 17.5), 180.0),  # inboard of hatch bay 1
    'Spawn_Intruder_2':     ((7.5, 0.0, 57.5), 180.0),  # inboard of hatch bay 3
}
# Wayfinding that is stale but fixable, as opposed to STALE_SIGNS above which is deleted.
#   Sign_TerminalDoor sits under Station_Map, so its local transform has to be re-solved
#   every time EITHER the sign's target or the map moves - and this change moves both.
#   The station is at (-3.571, 0, 80.0) yaw 180, so a local (lx, ly, lz) reads as world
#   (-3.571 - lx, ly, 80.0 - lz). The world position wanted is unchanged in kind and only
#   in kind: (-1.2, 1.9, 42.5), which is 0.05 off the x=-1.25 door plane on the PORT
#   SPINE DOOR OF ROW 8 - the door you walk through to reach the Ship Terminal at its new
#   home in the muster compartment. Solving back: lx = -2.371, lz = 37.5, ly untouched.
#   (It has been re-aimed twice now for the same reason, and it is worth saying why the
#   parenting is the real problem: a sign for a door 37 m away hangs off the bridge
#   console purely because that is where it was authored. Re-parenting it is a scene
#   edit outside what this generator owns, so it is re-solved instead, not fixed.)
SIGN_FIX = {
    'Sign_TerminalDoor': ((-2.371, 1.9, 37.5), None),
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
for nm, (pos, yaw) in SIGN_FIX.items():
    ids = by_name.get(nm)
    if not ids:
        missing.append(nm); continue
    set_tf(ids[0], pos, yaw)
    print("  re-aimed sign %s -> local %s" % (nm, pos))
for i, iz in enumerate(sorted(HATCH_Z)):
    ids = by_name.get('Hatch_%d' % (i + 1))
    if ids:
        set_tf(ids[0], (9.65, DECK_Y[1], HATCH_Z[iz]), -90.0)
    else:
        missing.append('Hatch_%d' % (i + 1))


# ---- M97 realm record: authored at fixed world coordinates on the OLD hull ------
# Marks sat on the old gallery wall at x=25 and Mounts on the old armory wall at
# x=20. The rebuilt hull ends at x=10, so both were left outside the ship. Each
# group is repositioned as a whole, which preserves the spacing M97 authored.
#   Marks  local x 24.85 -> starboard hull wall, faced inboard (see below)
#   Mounts local x 19.55 -> starboard armory wall, same facing
#   Plate  local x  9.54 -> nudged out to the new starboard face
#
# THE MARKS, THIRD PLACEMENT. History, because the two before this one were both wrong
# in ways worth not repeating. M97 authored them on the deck-2 PORT wall: group (15,5,30)
# yaw 180, children local (24.85, 2.2, -8.25..8.25) = world x -9.85, y 7.2, z 21.75..38.25.
# The one-deck collapse could not drop that group straight to y=0 (all twelve would have
# landed inside the deck-1 port glazing), so it moved them to the starboard wall - where
# they crossed the Armory console, the mount rack and hatch bay 1, because a 16.5 m run
# does not fit between hatch bays 10 m apart. That block ended by saying re-spacing,
# moving the rack, or splitting the run were shape decisions for a later change.
# This is that change, and the owner made all three calls: the rack goes to the port wall
# at z 20..25, the run splits into two panels of six, and the ship gets longer so both
# panels fit somewhere deliberate instead of somewhere left over.
#
# WHERE THEY WENT: the HALL OF RECORD, the port compartment of rows 5-7 (z 25..40).
# Six marks on the port hull wall, six on the port face of the spine, facing each other
# across the room - 7.86 m of clear floor between the two wall faces - each panel 7.5 m
# long and centred on z 32.5 with 3.75 m clear at either end. The room was chosen, not found: rows 5-7 are the ONLY three consecutive
# rows amidships that carry no spine door (DOOR_ROWS is {1,3,4,8,11}), which is what
# makes the spine face an unbroken 15 m wall instead of a wall with a doorway in it -
# and the second panel has nowhere else in the ship to hang. It is entered from the shop
# at row 4 through the gap the z=25 partition leaves beside the corridor, so it is a room
# you find rather than a corridor you pass along.
#
# SPACING AND FACING, both of which are easy to get silently wrong here:
#   Group (15, 0, 28) yaw 180. World = (15 - lx, ly, 28 - lz). Panel A is M97's first six
#   children with their authored local transforms COMPLETELY UNTOUCHED - lx 24.85 puts
#   them at world x -9.85, the same 0.15 off the port wall plane they were authored with,
#   and lz -8.25..-0.75 at the authored 1.5 m spacing puts them at z 28.75..36.25. Only
#   the group moved. Panel B is the same six local z values on lx 17.292, so each of the
#   second six faces one of the first six across the room.
#   Group yaw is 180 because the children carry a local -90: 180 + -90 = 90 maps their
#   front face (local +Z) to world +X, inboard from the PORT wall. Panel B has to face
#   the other way, so its six get local yaw +90 instead: 180 + 90 = 270 = -90, front face
#   to world -X, off the spine and back across the room. Get that backwards and six marks
#   face into the wall they are mounted on and nothing fails.
#   lx 17.292 = world x -2.292, which is -(1.25 + 0.892 + 0.15) and every term is doing
#   work: 1.25 is the spine wall's pivot plane (the CORRIDOR's face), 0.892 is the wall
#   mesh's own thickness, which sits entirely on the room side of that pivot, and 0.15 is
#   the standoff M97 authored for panel A off the x=-10 plane. So both panels hang the
#   same distance off the face they are mounted on, measured from opposite conventions.
#   SM_Prop_Detail_Panel_01 is 0.443 x 0.819 x 0.984 m and the marks carry a 0.08 z
#   scale, so each is a 0.44 m plate 0.08 thick standing 0.82 tall from y 2.2 - clear of
#   the 3.49 m Ceiling_02 over the ix=-1 column panel B hangs in, and 1.06 m apart at the
#   authored 1.5 m spacing.
MARK_PANEL_A = ['Mark_%02d' % i for i in range(1, 7)]     # port hull wall, as authored
MARK_PANEL_B = ['Mark_%02d' % i for i in range(7, 13)]    # port face of the spine
MARK_LZ = (-8.25, -6.75, -5.25, -3.75, -2.25, -0.75)      # M97's 1.5 m spacing, forward
                                                          # to aft under the group's 180
REALM = {
    # Group moved so panel A lands on the port wall of rows 5-7; see the note above.
    'Marks':        ((15.0, 0.0, 28.0), 180.0),
    # THE RACK, to the port wall at z 20..25 (owner's call). It was starboard at
    # z 16..19.6, which the collapse put inside hatch bay 1 - a real collision, and the
    # reason it had to move rather than a preference. Group (9.7, 0, 40.3) yaw 180:
    # world = (9.7 - 19.55, 1.5, 40.3 - lz) with lz 16.0..19.6, so the seven mounts run
    # x -9.85 (the same 0.15 off the port plane the marks use), y 1.5, z 20.7..24.3 of
    # centres - 20.478..24.522 of actual panel, centred in the band with 0.478 m clear of
    # the z=25 bulkhead forward and the same length of open wall aft. See PARTITIONS for
    # why there is no bulkhead at z=20 for it to be jammed against. The yaw flip 0 -> 180
    # is the same -90-local story as the marks: it is what turns the rack to face inboard
    # off the PORT wall instead of inboard off the starboard one.
    # It sits directly across the spine from Station_Armory at (10, 0, 22.5), which is
    # the "shop end" reading the owner asked to keep: kit on one side, weapons on the
    # other, one door row between them.
    'Mounts':       ((9.7, 0.0, 40.3), 180.0),
    # THE PLATE STAYS STARBOARD, and this is deliberate, not leftover. `Plate Text` is a
    # legacy TextMesh: mirroring its group (which is what a yaw of 180 does) would render
    # the record's text backwards. Its backing faces -X, so the only walls in the ship it
    # can hang on are the ones whose inboard normal is -X - the starboard hull wall. It
    # moves along that wall from z 22.5 (now the Armory's bay) to z 32.5, which puts it
    # in the starboard gallery directly abeam the middle of the Hall of Record, two rows
    # clear of hatch bay 2. Group z 10.0 + the child's authored local 22.5 = 32.5.
    'Record Plate': ((0.31, 0.0, 10.0), None),
}
for nm, (pos, yaw) in REALM.items():
    ids = by_name.get(nm)
    if not ids:
        print("  MISSING realm-record group", nm); continue
    set_tf(ids[0], pos, yaw)
    print("  realm record: %s group -> %s%s" % (nm, pos, '' if yaw is None else ' yaw %g' % yaw))

# The split itself. Panel A is skipped on purpose - not "nothing to do", but a promise
# that M97's six authored transforms are re-serialised byte for byte, so the only thing
# that moved them is the group above. Panel B is written child by child because a group
# transform cannot split a run into two panels, and because these six need a DIFFERENT
# local yaw from their siblings to face back across the room.
for _panel, _lx, _yaw in ((MARK_PANEL_B, 17.292, 90.0),):
    for _nm, _lz in zip(_panel, MARK_LZ):
        _ids = by_name.get(_nm)
        if not _ids:
            print("  MISSING realm-record mark", _nm); continue
        set_tf(_ids[0], (_lx, 2.2, _lz), _yaw)
    print("  realm record: %s -> local x %g yaw %g, z %g..%g (world x %g, z %g..%g)"
          % (', '.join((_panel[0], _panel[-1])), _lx, _yaw, MARK_LZ[0], MARK_LZ[-1],
             15.0 - _lx, 28.0 - MARK_LZ[0], 28.0 - MARK_LZ[-1]))

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
# Deck 2's two lines (z 10 and 50, Part2_100_50 and Part2_500_50) went with deck 2.
#
# RE-CUT for the sixteen-row hull, and the lines are not a rescaling of the old five -
# they are where the six compartments listed at PLACE actually want to divide:
#   z 10  stern narrows | shop end          (the hull steps in here anyway)
#   z 25  shop end      | Hall of Record + starboard gallery   [spine flanks OPEN]
#   z 40  hall/gallery  | muster            (was 35; the hall is three rows, not two)
#   z 55  muster        | hatch-3 vestibule (was 50)
#   z 65  vestibule     | the bridge        [spine flanks OPEN]   NEW
# There is deliberately no line at z 70, where the hull narrows to the bow: the step
# from ix -2..1 down to ix -1..0 already puts wall across x[-10,-5] and x[5,10] there,
# so a partition would be a second wall on the same line.
#
# AND DELIBERATELY NONE AT z 20, which the first draft of this change did have, cutting
# the shop off from the aft hold. The M97 mount rack does not fit behind it. Measured,
# not guessed: SM_Bld_Wall_01 is 0.892 m thick and its body sits on the +Z side of its
# pivot, so a line at z=20 puts wall across z[20, 20.892] and leaves the shop 4.108 m of
# clear port wall; the rack is seven SM_Prop_Detail_Panel_01 at M97's 0.6 m spacing,
# which is 3.6 m of centres plus 0.443 m of panel = 4.043 m. That is a rack jammed
# wall-to-wall with 3 cm at each end, and the aft-most mount was buried IN the partition
# before it was centred. Dropping the line is the better answer than shaving the rack:
# rows 2-4 become one 15 m "shop end" - Fabrication, the hatch-1 bay, the Armory and the
# rack in one working space - and the rack gets 0.48 m of clear wall at each end inside
# the z 20..25 band the owner named for it.
PARTITIONS = {1: [10.0, 25.0, 40.0, 55.0, 65.0]}
# Lines whose two SPINE-FLANK panels (px -1.25 and 5.0) are left out, so the compartments
# either side join through a 3.75 m opening beside the corridor instead of being sealed
# from each other. This is a reachability mechanism before it is a scenic one - see the
# block inside the loop, which is where the argument for each line lives.
OPEN_SPINE_FLANK = (25.0, 65.0)
FILL = 3.75 / 5.0

parts = 0
for deck, lines in PARTITIONS.items():
    y = DECK_Y[deck]
    cs = D1
    for L in lines:
        iz_s, iz_n = int(L / 5) - 1, int(L / 5)
        for px, sx_, lo, hi in ((-5.0, 1.0, -10.0, -5.0), (-1.25, FILL, -5.0, -1.25),
                                (5.0, FILL, 1.25, 5.0), (10.0, 1.0, 5.0, 10.0)):
            mid = (lo + hi) / 2.0
            ixm = int(math.floor(mid / 5.0))
            if (ixm, iz_s) not in cs or (ixm, iz_n) not in cs:
                continue                      # no room either side of this stretch
            # RECONCILED: the two skips that used to live here did not match the scene,
            # and had not matched it at HEAD either - the committed generator has never
            # reproduced the committed partition layout. Both are removed rather than
            # patched, and what the scene actually does is stated instead.
            #
            #   1. "starboard stays one open gallery" (attributed to Zo, 2026-08-22)
            #      skipped every px 5.0 and 10.0 panel. The scene has all nine of them
            #      - Part1_100_50, Part1_200_50/100, Part1_250_100, Part1_350_50/100,
            #      Part1_500_50, Part2_100_50, Part2_500_50. Whether the open-gallery
            #      call was reversed or never emitted, the ship on disk has a
            #      partitioned starboard side and that pass did not change its shape.
            #      If the gallery was the intent, that is a shape decision and belongs
            #      in its own change, with the scene and this file moving together.
            #      (The lengthening moves the LINES - see PARTITIONS - but not this: the
            #      starboard side is still partitioned at every line the port side is.)
            #   2. the port stair well at px -5.0, L 25 and 35, which was cut for a
            #      stair that has not stood there since 2026-08-23. Those two panels
            #      are in the scene (Part1_250_-50, Part1_350_-50).
            #
            # SPINE-FLANK SKIPS. Both of these are load-bearing for REACHABILITY, not
            # decoration, and the rule behind them is worth stating once because adding a
            # row to ROWS is exactly what recreates the trap.
            #
            # The rule: a room only meets the corridor at a DOOR_ROW, and DOOR_ROWS is
            # frozen at {1,3,4,8,11} by Tools/scene-check/reachability.py. So every
            # compartment must either contain one of those five rows or touch one through
            # a gap. The ix -2/-1 boundary and the ix 0/1 boundary are open grid lines, so
            # a side is only sealed when BOTH its panels close - the outer one (px -5 or
            # 10) and the spine-flank one (px -1.25 or 5.0). Leaving the flank pair out
            # keeps the two compartments joined along the corridor wall while the outer
            # panel still reads the division from the hull side.
            #
            #   z=25 feeds the HALL OF RECORD and the starboard gallery. Rows 5-7 carry no
            #   spine door and are boxed by full lines at z=40 north and z=25 south. Close
            #   these two panels and twelve cells become two isolated six-cell pockets;
            #   reachability.py drops to 44/56. The gap feeds them from the row-4 door.
            #   (Same skip, same reason, one line further north than before the ship was
            #   lengthened - the hall grew from two rows to three.)
            #   z=65 feeds the BRIDGE. Rows 13-15 carry no spine door either, and row 11
            #   is the last door in the ship, so the whole bow drains back through this
            #   line. Close it and reachability.py drops to 48/56 - eight cells, the NAV
            #   MAP and the Nav spawn among them.
            # Both numbers above were measured, not reasoned: set OPEN_SPINE_FLANK to ()
            # and re-run the checker to see them.
            if deck == 1 and any(abs(L - k) < 0.01 for k in OPEN_SPINE_FLANK) \
                    and px in (-1.25, 5.0):
                continue
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

# -- hand-pass hull closures (HAND-PASS) ------------------------------------------
# Everything below this line was authored by hand as LOOSE SCENE ROOTS after the last
# generator run, which is why a re-run used to orphan it. All of it is the same cheap
# primitive: a Unity Cube with a visible renderer and a unit BoxCollider, scaled into a
# slab. The colliders are incidental - the graybox perimeter is what actually stops the
# player, and none of these sit on a room-centre line the reachability graph walks - but
# they are reproduced as authored rather than trimmed, because this pass is about
# reproducing the ship, not improving it.
MAT_SHIPWALL = "{fileID: 2100000, guid: f25497c1b2f782a45a300203d2337abd, type: 2}"  # Mat_ShipWall

# Glazing. The Exterior_Window piece has a real opening in it, so from inside a lit deck
# the bay read as a hole punched to space. One pane per window bay, 0.1 m inboard of the
# x=+-10 wall plane, 3 m tall from 2.55 above the deck, 4.9 long inside the 5 m bay.
#   The name suffix is NOT one rule: deck 1 panes are numbered by the bay's low z edge
#   and deck 2 panes by the bay centre. That is a hand-pass inconsistency, kept because
#   the names are what anyone looking at the scene has already learned.
GLASS_H, GLASS_INSET = 2.55, 9.9
panes = 0
for deck, k, ix, iz in sorted(WINDOW_BAYS):
    z0 = 5.0 * iz                                   # bay spans z [z0, z0+5]
    tag = int(z0 * 10) if deck == 1 else int((z0 + 2.5) * 10)
    obj('Glass_%s%d_%d' % ('E' if k == 'E' else 'W', deck, tag), DRESS,
        (GLASS_INSET if k == 'E' else -GLASS_INSET, DECK_Y[deck] + GLASS_H, z0 + 2.5),
        0.0, (0.08, 3.0, 4.9), box=True, collider=True, box_mat=MAT_GLASS)
    panes += 1

# Corner pillars. The octagon ring closes its corners by letting the chamfer and skirt
# bands overlap, which is a clean mitre in plan but leaves the VERTICAL corner of the
# straight skin band between them showing its edge. These six cap that seam. y 4 with a
# 9.6 y-scale spans y [-0.8, 8.8] - exactly the skirt top to the chamfer bottom - so
# each pillar covers both decks by SCALE, not by being two objects.
# Placed 0.8 out along both axes from each corner of the wide body (x +-10, z 10 and 50)
# and of the stern face (x +-5, z 0). The bow corners have no pillar: the prow taper
# starts there and there is no straight corner to cap.
PILLAR_S = (1.7, 9.6, 1.7)
for tag, (px, pz) in (('NE', (10.8, 50.8)), ('NW', (-10.8, 50.8)),
                      ('SE', (10.8, 9.2)), ('SW', (-10.8, 9.2)),
                      ('SternE', (5.8, -0.8)), ('SternW', (-5.8, -0.8))):
    obj('SkinPillar_%s' % tag, DRESS, (px, 4.0, pz), 0.0, PILLAR_S,
        box=True, collider=True, box_mat=MAT_SHIPWALL)

# Bow crown and keel. The narrow-body roof/belly tiles stop short of where the first
# prow valley tile begins, leaving a notch on the centreline at z [59.9, 61.5] over the
# full 7.4 m width. Two slabs laid in the roof and belly face planes close it.
for tag, py in (('Roof', 11.45), ('Belly', -2.6)):
    obj('Hull_%sPatch_0' % tag, HULL_TID, (0.0, py, 60.7), 0.0, (7.4, 0.2, 1.6),
        box=True, collider=True, box_mat=MAT_SHIPWALL)
print("  hand-pass closures: %d glazing panes, 6 corner pillars, 2 bow patches" % panes)

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

print("deck cells %d (one deck at y=%g)" % (len(D1), DECK_Y[1]))
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
# Same unlink for the stale signs, whose father is a Station and not one of the two group
# roots above. Without this the deleted sign stays listed in Station_Map's m_Children and
# the dangling-ref assert at the bottom fires.
for _father, _t in sign_unlink:
    d = byid[_father]
    d[2] = d[2].replace('\n  - {fileID: %d}' % _t, '', 1)
for parent, kids in KIDS.items():
    add = ''.join('  - {fileID: %d}\n' % k for k in kids)
    if parent in EMIT_AT:
        # A parent this run emitted. Nothing uses it since the Stair (which parented its
        # two ramp boxes) was deleted, but it is the general mechanism and stays: the
        # write step below reaches for byid[parent], which only holds docs that were
        # already in the scene. obj() always writes `m_Children: []`, so there is exactly
        # one to replace.
        i = EMIT_AT[parent]
        EMIT[i] = EMIT[i].replace('  m_Children: []\n', '  m_Children:\n' + add, 1)
        continue
    d = byid[parent]
    if re.search(r'^  m_Children: \[\]$', d[2], re.M):
        d[2] = re.sub(r'^  m_Children: \[\]$', '  m_Children:\n' + add.rstrip('\n'),
                      d[2], count=1, flags=re.M)
    else:
        d[2] = re.sub(r'(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)',
                      lambda m: m.group(1) + add, d[2], count=1)

# The loose hull roots stripped above are still named in SceneRoots, which is a plain
# list of root transforms and is not reachable from any m_Children. Leaving them there
# would dangle - the assert below catches it, but the fix belongs here.
for d in keep:
    if d[1] != 1660057539:
        continue
    for t in loose_tf:
        d[2] = d[2].replace('  - {fileID: %d}\n' % t, '', 1)

out = head_txt + ''.join(d[2] for d in keep) + ''.join(EMIT)
ids = [int(x) for x in re.findall(r'^--- !u!\d+ &(\d+)', out, re.M)]
assert len(ids) == len(set(ids)), 'duplicate fileID emitted'
dang = sorted(x for x in set(int(v) for v in re.findall(r'fileID: (\d+)\}(?!, guid)', out))
              if x in doomed_docs)
assert not dang, 'refs to stripped objects remain: %s' % dang[:5]
open(SCENE, 'w', encoding='utf-8', newline='\n').write(out)
print("wrote %s (%d docs)" % (SCENE, len(ids)))
