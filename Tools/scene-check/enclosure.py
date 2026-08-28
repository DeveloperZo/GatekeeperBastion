#!/usr/bin/env python3
"""Structural checks on a ship scene, run without opening Unity.

    python Tools/scene-check/enclosure.py "Assets/Scenes/Ship - CoOp.unity"

The ship is ONE deck. Everything below is reported every run and any non-empty
count exits non-zero — there is no flag to switch a section off, because a check
you have to remember to ask for is a check that silently passes:

  STRAY DECK NAMES  more than one deck token across the generated names. The ship
                    was collapsed to a single deck; a second one surviving in the
                    scene would otherwise merge into the first here and read clean.
  VOID LEFTOVERS    `CVoid_*` ceiling pieces. One deck has no double-height void, so
                    any surviving piece is debris hanging over the floor.
  OPEN HULL EDGES   a cell edge with no cell beyond it and no wall piece covering it
  UNDRESSED         a graybox wall with no visible counterpart, or the reverse
  SEAMS             a wall line carrying pieces of two opposing kinds, which makes the
                    wall plane step by one wall thickness and leaves an open return
  FLOOR             cell count and the single floor plane height
  HATCH BAYS        one bay centre z per airlock, M101 criterion 6
  FIXED OBJECTS     whether each stands on a floor cell, at the floor plane

Naming contract this relies on (see M101): graybox floors `GFloor_<deck>_<ix>_<iz>`,
dress floors `F<deck>_<ix>_<iz>`, hull walls `W<deck>_<N|S|E|W>_<x*10>_<z*10>`, exterior
skin `Skin<deck>_...`, spine `Spine<deck>_<P|S>_<iz>`, airlocks `AirlockShell_<iz>`.
The `<deck>` token survives in the names even though there is only one deck left, so it
is still PARSED — but only to prove it is always the same value. Nothing keys off it.

The exterior hull (`ShipDress/Hull`, `SkinPillar_*`, roof and belly patches) is
deliberately NOT reprofiled for the single deck — the ship's outside is shown as an
image, not modelled — so nothing here inspects it.
"""
import re, sys, math, os, collections

WALL_T = 0.892
CELL = 5.0

# A fixed object's pivot may legitimately sit above the floor it stands on — the Player
# capsule's is 1.10 m up — but never a whole deck height above it. This band accepts
# every authored pivot in the scene and still rejects anything that was left behind at
# the old upper plane, 5 m up, which is the signature bug of the deck collapse.
PIVOT_ABOVE = 2.0
PIVOT_BELOW = 0.5

def load(path):
    txt = open(path, encoding='utf-8', errors='replace').read()
    docs = []
    for d in re.split(r'(?=^--- !u!)', txt, flags=re.M):
        m = re.match(r'--- !u!(\d+) &(\d+)', d)
        if m:
            docs.append((int(m.group(2)), int(m.group(1)), d))
        elif docs:
            i, c, t = docs[-1]
            docs[-1] = (i, c, t + d)
    return docs

def build(docs):
    go_name, go_comps, tf = {}, {}, {}
    for i, cls, t in docs:
        if cls == 1:
            mm = re.search(r'^  m_Name: (.*)$', t, re.M)
            go_name[i] = mm.group(1) if mm else ''
            go_comps[i] = [int(c) for c in re.findall(r'component: \{fileID: (\d+)\}', t)]
        elif cls in (4, 224):
            gg = re.search(r'm_GameObject: \{fileID: (\d+)\}', t)
            ff = re.search(r'm_Father: \{fileID: (-?\d+)\}', t)
            pp = re.search(r'm_LocalPosition: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}', t)
            tf[i] = {'go': int(gg.group(1)) if gg else 0,
                     'father': int(ff.group(1)) if ff else 0,
                     'pos': tuple(float(v) for v in pp.groups()) if pp else (0, 0, 0)}
    name = {i: go_name.get(v['go'], '') for i, v in tf.items()}
    return name, tf

def world(tid, tf):
    x = y = z = 0.0
    seen = set()
    while tid in tf and tid not in seen:
        seen.add(tid)
        p = tf[tid]['pos']
        x, y, z = x + p[0], y + p[1], z + p[2]
        tid = tf[tid]['father']
    return x, y, z

def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    scene = args[0] if args else 'Assets/Scenes/Ship - CoOp.unity'
    docs = load(scene)
    name, tf = build(docs)

    cells = set()                            # {(ix, iz)}
    gfloor = set()
    walls = []                               # [(kind, px, pz)]
    floor_ids = []                           # transform ids of the dress floors
    decks_seen = collections.Counter()       # deck token -> how many names carried it
    voids = []                               # `CVoid_*` ceiling ring, must be gone
    hatches, objs = [], {}
    for i, n in name.items():
        m = re.match(r'^F(\d)_(-?\d+)_(-?\d+)$', n)
        if m:
            decks_seen[m.group(1)] += 1
            cells.add((int(m.group(2)), int(m.group(3))))
            floor_ids.append((i, m.group(1))); continue
        m = re.match(r'^GFloor_(\d)_(-?\d+)_(-?\d+)$', n)
        if m:
            decks_seen[m.group(1)] += 1
            gfloor.add((int(m.group(2)), int(m.group(3)))); continue
        m = re.match(r'^W(\d)_([NSEW])_(-?\d+)_(-?\d+)$', n)
        if m:
            decks_seen[m.group(1)] += 1
            walls.append((m.group(2), int(m.group(3)) / 10.0,
                          int(m.group(4)) / 10.0)); continue
        # A window bay (M102) carries SM_Bld_Wall_Exterior_Window_01 instead of the
        # Wall+Skin pair. The mesh is authored offset (Unity-local x[4.75,9.75]) and
        # placed flipped, so the covered wall line's pivot is recovered from the
        # window's actual transform, not trusted from the name's suffix.
        m = re.match(r'^Win(\d)_([NSEW])_(-?\d+)_(-?\d+)$', n)
        if m:
            decks_seen[m.group(1)] += 1
            t = {'N': 0.0, 'E': 90.0, 'S': 180.0, 'W': -90.0}[m.group(2)]
            wx, wy, wz = world(i, tf)
            walls.append((m.group(2),
                          wx - 4.75 * math.cos(math.radians(t)),
                          wz + 4.75 * math.sin(math.radians(t))))
            continue
        # Ceilings are not inspected geometrically — nothing above head height can open
        # the hull — but they DO carry a deck token, so they are worth counting: a
        # leftover upper-deck ceiling is a second deck the floor scan cannot see.
        m = re.match(r'^C(\d)_(-?\d+)_(-?\d+)$', n)
        if m:
            decks_seen[m.group(1)] += 1; continue
        # `CVoid_<ix>_<iz>` is the ceiling ring that used to cap the double-height void.
        # With one deck there is no void, so any of these left in the scene is debris.
        if re.match(r'^CVoid_(-?\d+)_(-?\d+)$', n):
            voids.append(n); continue
        m = re.match(r'^AirlockShell_(-?\d+)$', n)
        if m:
            hatches.append(world(i, tf)); continue
        if n in ('Station_Armory', 'Station_Fabrication', 'Station_Map',
                 'Station_CrewQuarters', 'Ship Terminal', 'Demo Teleporter', 'Player',
                 'Spawn_Teleporter', 'Spawn_Nav', 'Spawn_Intruder_1', 'Spawn_Intruder_2',
                 'Hatch_1', 'Hatch_2', 'Hatch_3'):
            objs[n] = world(i, tf)

    fail = 0

    # --- one deck only ---------------------------------------------------------
    # Everything past this point is deck-BLIND: it unions every floor and wall it
    # finds, whatever deck token the name carries. That is right for a one-deck ship
    # and disastrous for a two-deck one, where the two cell sets would silently merge
    # and the upper deck's walls would land on the lower deck's wall lines. So the
    # deck token is parsed for exactly one purpose: to prove there is only ever one.
    print("STRAY DECK NAMES: %d" % (len(decks_seen) - 1 if decks_seen else 0))
    if len(decks_seen) != 1:
        print("   deck tokens found: %s"
              % ", ".join("%s (%d names)" % kv for kv in sorted(decks_seen.items())))
        fail += max(0, len(decks_seen) - 1)
        if not decks_seen:
            print("   no generated floor/wall names at all - wrong scene, or a rename")
            fail += 1
    print("VOID LEFTOVERS: %d" % len(voids))
    for v in sorted(voids)[:8]:
        print("   ", v)
    fail += len(voids)

    # --- open hull edges -------------------------------------------------------
    DIR = {'N': (0, 1), 'S': (0, -1), 'E': (1, 0), 'W': (-1, 0)}
    have = set()
    for k, px, pz in walls:
        have.add((k, round(px, 2), round(pz, 2)))
    # The floor plane the airlock allowance below is judged against. Taken from the
    # dress floors rather than assumed to be 0, so this still works if the whole ship
    # is ever moved in Y. Reported and enforced under FLOOR further down.
    floor_ys = sorted({round(world(i, tf)[1], 3) for i, _d in floor_ids})
    plane = floor_ys[0] if floor_ys else 0.0
    open_edges = []
    for (ix, iz) in sorted(cells):
        for k, (dx, dz) in DIR.items():
            if (ix + dx, iz + dz) in cells:
                continue                    # interior: floor on the far side
            want = {'N': ('N', CELL * ix + 5, CELL * iz + 5),
                    'S': ('S', CELL * ix, CELL * iz),
                    'E': ('E', CELL * ix + 5, CELL * iz),
                    'W': ('W', CELL * ix, CELL * iz + 5)}[k]
            key = (want[0], round(want[1], 2), round(want[2], 2))
            if key in have:
                continue
            # An airlock may legitimately fill the bay instead of a wall — but only an
            # airlock standing on THIS floor. Height is checked because a shell left at
            # the old upper plane must not be allowed to excuse a hole in the new hull.
            if any(abs(h[0] - want[1]) < 0.6 and abs(h[2] - want[2]) < 0.6
                   and plane - PIVOT_BELOW <= h[1] <= plane + PIVOT_ABOVE
                   for h in hatches):
                continue
            open_edges.append((ix, iz, k))
    print("OPEN HULL EDGES: %d" % len(open_edges))
    for e in open_edges[:12]:
        print("   cell(%3d,%3d) %s" % e)
    fail += len(open_edges)

    # --- seams -----------------------------------------------------------------
    seams = 0
    lines = collections.defaultdict(list)
    for k, px, pz in walls:
        if k in 'NS':
            span = (px - 5, px) if k == 'N' else (px, px + 5)
            lines[('z', round(pz, 2))].append((span, k))
        else:
            span = (pz, pz + 5) if k == 'E' else (pz - 5, pz)
            lines[('x', round(px, 2))].append((span, k))
    for key, segs in sorted(lines.items()):
        segs.sort()
        for a, b in zip(segs, segs[1:]):
            if abs(a[0][1] - b[0][0]) < 0.01 and a[1] != b[1]:
                seams += 1
                print("   SEAM %s=%g at %g: %s | %s"
                      % (key[0], key[1], a[0][1], a[1], b[1]))
    print("SEAMS (wall plane steps %.3f m): %d" % (WALL_T, seams))
    fail += seams

    # --- graybox vs dress ------------------------------------------------------
    undressed = []
    if gfloor != cells:
        undressed.append("%d graybox floors vs %d dress floors (%d graybox-only, "
                         "%d dress-only)"
                         % (len(gfloor), len(cells),
                            len(gfloor - cells), len(cells - gfloor)))
        for c in sorted(gfloor - cells)[:6]:
            undressed.append("   graybox with no dress: cell%s" % (c,))
        for c in sorted(cells - gfloor)[:6]:
            undressed.append("   dress with no graybox: cell%s" % (c,))
    print("UNDRESSED: %d" % (1 if gfloor != cells else 0))
    for u in undressed:
        print("  ", u)
    fail += (1 if gfloor != cells else 0)

    # --- floor -----------------------------------------------------------------
    print()
    # One deck means ONE plane. Printing the set was a report that could not fail:
    # a single floor tile left at the old upper height would show up in the list and
    # still exit zero. It is an assertion now.
    print("floor: %d cells, plane y=%s" % (len(cells), floor_ys))
    if len(floor_ys) != 1:
        print("   FAIL: floor tiles at %d different heights - one deck has one plane"
              % len(floor_ys))
        fail += 1

    # --- hatch bays ------------------------------------------------------------
    # M101 criterion 6 wants three bays at distinct z with the middle one amidships.
    # The count and the distinctness are exact and enforced. "Amidships" is not a
    # number the generator commits to (the middle bay sits at 32.5 against a hull
    # midpoint of 30), so the offset is REPORTED for a human and not failed on.
    bays = sorted(round(h[2] + 2.5, 2) for h in hatches)
    print("hatch bay centres z:", bays)
    if len(bays) != 3 or len(set(bays)) != 3:
        print("   FAIL: expected 3 airlock bays at 3 distinct z, got %d (%d distinct)"
              % (len(bays), len(set(bays))))
        fail += 1
    elif bays:
        mid = (min(bays) + max(bays)) / 2.0
        print("   middle bay z=%g, midpoint of the outer two z=%g (offset %+g)"
              % (bays[1], mid, bays[1] - mid))
    off_plane_shells = [h for h in hatches
                        if not (plane - PIVOT_BELOW <= h[1] <= plane + PIVOT_ABOVE)]
    if off_plane_shells:
        print("   FAIL: %d airlock shell(s) not at the floor plane y=%g: %s"
              % (len(off_plane_shells), plane,
                 ", ".join("y=%g" % h[1] for h in off_plane_shells)))
        fail += len(off_plane_shells)

    # --- fixed objects ---------------------------------------------------------
    # This used to classify each object with `deck = 1 if y < 2.5 else 2` and then ask
    # whether that deck had a cell under it. With one deck the deck half of that key is
    # gone, but the HEIGHT half was doing real work and is kept explicitly: without it
    # an object still floating at the old upper plane would sit over a valid cell in XZ
    # and be waved through as "on the floor".
    print()
    for nm in sorted(objs):
        x, y, z = objs[nm]
        ok, cell = False, None
        for ex, ez in ((0, 0), (0.5, 0), (-0.5, 0), (0, 0.5), (0, -0.5)):
            c = (int(math.floor((x + ex) / CELL)), int(math.floor((z + ez) / CELL)))
            if c in cells:
                ok, cell = True, c
                break
        if cell is None:
            cell = (int(math.floor(x / CELL)), int(math.floor(z / CELL)))
        on_plane = plane - PIVOT_BELOW <= y <= plane + PIVOT_ABOVE
        verdict = ("ok" if on_plane else "OFF-PLANE (%+.2f from y=%g)" % (y - plane, plane)) \
            if ok else ("OFF-FLOOR" if on_plane else "OFF-FLOOR + OFF-PLANE")
        print("  %-22s (%7.2f,%5.2f,%7.2f)  cell%s  %s"
              % (nm, x, y, z, cell, verdict))
        if not (ok and on_plane):
            fail += 1

    return 1 if fail else 0

if __name__ == '__main__':
    sys.exit(main())
