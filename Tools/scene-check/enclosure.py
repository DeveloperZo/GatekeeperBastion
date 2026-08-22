#!/usr/bin/env python3
"""Structural checks on a ship scene, run without opening Unity.

    python Tools/scene-check/enclosure.py "Assets/Scenes/Ship - CoOp.unity" [--decks]

Reports, and exits non-zero if any of the first three are non-empty:

  OPEN HULL EDGES   a cell edge with no cell beyond it and no wall piece covering it
  UNDRESSED         a graybox wall with no visible counterpart, or the reverse
  SEAMS             a wall line carrying pieces of two opposing kinds, which makes the
                    wall plane step by one wall thickness and leaves an open return
  --decks           floor-cell count and plane height per deck, hatch stations, and
                    whether each fixed object stands on a floor cell

Naming contract this relies on (see M101): graybox floors `GFloor_<deck>_<ix>_<iz>`,
dress floors `F<deck>_<ix>_<iz>`, hull walls `W<deck>_<N|S|E|W>_<x*10>_<z*10>`, exterior
skin `Skin<deck>_...`, spine `Spine<deck>_<P|S>_<iz>`, airlocks `AirlockShell_<iz>`.
"""
import re, sys, math, os, collections

WALL_T = 0.892
CELL = 5.0

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

    cells = collections.defaultdict(set)     # deck -> {(ix, iz)}
    gfloor = collections.defaultdict(set)
    walls = collections.defaultdict(list)    # deck -> [(kind, px, pz)]
    hatches, objs = [], {}
    for i, n in name.items():
        m = re.match(r'^F(\d)_(-?\d+)_(-?\d+)$', n)
        if m:
            cells[int(m.group(1))].add((int(m.group(2)), int(m.group(3)))); continue
        m = re.match(r'^GFloor_(\d)_(-?\d+)_(-?\d+)$', n)
        if m:
            gfloor[int(m.group(1))].add((int(m.group(2)), int(m.group(3)))); continue
        m = re.match(r'^W(\d)_([NSEW])_(-?\d+)_(-?\d+)$', n)
        if m:
            walls[int(m.group(1))].append((m.group(2), int(m.group(3)) / 10.0,
                                           int(m.group(4)) / 10.0)); continue
        # A window bay (M102) carries SM_Bld_Wall_Exterior_Window_01 instead of the
        # Wall+Skin pair. The mesh is authored offset (Unity-local x[4.75,9.75]) and
        # placed flipped, so the covered wall line's pivot is recovered from the
        # window's actual transform, not trusted from the name's suffix.
        m = re.match(r'^Win(\d)_([NSEW])_(-?\d+)_(-?\d+)$', n)
        if m:
            t = {'N': 0.0, 'E': 90.0, 'S': 180.0, 'W': -90.0}[m.group(2)]
            wx, wy, wz = world(i, tf)
            walls[int(m.group(1))].append((m.group(2),
                                           wx - 4.75 * math.cos(math.radians(t)),
                                           wz + 4.75 * math.sin(math.radians(t))))
            continue
        m = re.match(r'^AirlockShell_(-?\d+)$', n)
        if m:
            hatches.append(world(i, tf)); continue
        if n in ('Station_Armory', 'Station_Fabrication', 'Station_Map',
                 'Station_CrewQuarters', 'Ship Terminal', 'Demo Teleporter', 'Player',
                 'Spawn_Teleporter', 'Spawn_Nav', 'Spawn_Intruder_1', 'Spawn_Intruder_2',
                 'Hatch_1', 'Hatch_2', 'Hatch_3'):
            objs[n] = world(i, tf)

    fail = 0

    # --- open hull edges -------------------------------------------------------
    DIR = {'N': (0, 1), 'S': (0, -1), 'E': (1, 0), 'W': (-1, 0)}
    anydeck = set()
    for cs in cells.values():
        anydeck |= cs
    open_edges = []
    for deck, cs in sorted(cells.items()):
        have = set()
        for k, px, pz in walls[deck]:
            have.add((k, round(px, 2), round(pz, 2)))
        for (ix, iz) in sorted(cs):
            for k, (dx, dz) in DIR.items():
                if (ix + dx, iz + dz) in cs or (ix + dx, iz + dz) in anydeck:
                    continue        # interior: floor here or a void over the deck below
                want = {'N': ('N', CELL * ix + 5, CELL * iz + 5),
                        'S': ('S', CELL * ix, CELL * iz),
                        'E': ('E', CELL * ix + 5, CELL * iz),
                        'W': ('W', CELL * ix, CELL * iz + 5)}[k]
                key = (want[0], round(want[1], 2), round(want[2], 2))
                if key in have:
                    continue
                # an airlock or the void may legitimately fill the bay
                cx, cz = CELL * ix + 2.5, CELL * iz + 2.5
                if any(abs(h[0] - (want[1])) < 0.6 and abs(h[2] - want[2]) < 0.6
                       for h in hatches):
                    continue
                open_edges.append((deck, ix, iz, k))
    print("OPEN HULL EDGES: %d" % len(open_edges))
    for e in open_edges[:12]:
        print("   deck %d cell(%3d,%3d) %s" % e)
    fail += len(open_edges)

    # --- seams -----------------------------------------------------------------
    seams = 0
    for deck, ws in sorted(walls.items()):
        lines = collections.defaultdict(list)
        for k, px, pz in ws:
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
                    print("   SEAM deck %d %s=%g at %g: %s | %s"
                          % (deck, key[0], key[1], a[0][1], a[1], b[1]))
    print("SEAMS (wall plane steps %.3f m): %d" % (WALL_T, seams))
    fail += seams

    # --- graybox vs dress ------------------------------------------------------
    undressed = []
    for deck in sorted(cells):
        if gfloor[deck] != cells[deck]:
            undressed.append("deck %d: %d graybox floors vs %d dress floors"
                             % (deck, len(gfloor[deck]), len(cells[deck])))
    print("UNDRESSED: %d" % len(undressed))
    for u in undressed:
        print("  ", u)
    fail += len(undressed)

    # --- --decks ---------------------------------------------------------------
    if '--decks' in sys.argv:
        print()
        for deck in sorted(cells):
            ys = set()
            for i, n in name.items():
                if re.match(r'^F%d_' % deck, n):
                    ys.add(round(world(i, tf)[1], 3))
            print("deck %d: %d floor cells, plane y=%s"
                  % (deck, len(cells[deck]), sorted(ys)))
        print("hatch bay centres z:", sorted(round(h[2] + 2.5, 2) for h in hatches))
        print()
        allc = {(d, c) for d, cs in cells.items() for c in cs}
        for nm in sorted(objs):
            x, y, z = objs[nm]
            deck = 1 if y < 2.5 else 2
            ok, cell = False, None
            for ex, ez in ((0, 0), (0.5, 0), (-0.5, 0), (0, 0.5), (0, -0.5)):
                c = (int(math.floor((x + ex) / CELL)), int(math.floor((z + ez) / CELL)))
                if (deck, c) in allc:
                    ok, cell = True, c
                    break
            if cell is None:
                cell = (int(math.floor(x / CELL)), int(math.floor(z / CELL)))
            print("  %-22s (%7.2f,%5.2f,%7.2f)  deck %d cell%s  %s"
                  % (nm, x, y, z, deck, cell, "ok" if ok else "OFF-FLOOR"))
            if not ok:
                fail += 1

    return 1 if fail else 0

if __name__ == '__main__':
    sys.exit(main())
