#!/usr/bin/env python3
"""Prove every walkable room is reachable from the player spawn.

    python Tools/scene-check/reachability.py "Assets/Scenes/Ship - CoOp.unity"

Builds an adjacency graph from GRAYBOX colliders only (they are gameplay truth here:
dress is cosmetic, Environment blocks movement) and BFS's from the Player spawn.

The ship's rooms are NOT one node per 5 m cell: the 2.5 m spine corridor is a sliver
straddling the ix=-1/ix=0 boundary, never touched by a partition along its length, and
a room only connects to it at the specific rows that carry a door. A per-cell-center
model misses this entirely - it can't represent "walk the corridor past a partitioned
room segment" - so this graph has two kinds of node per row: the corridor itself
(always open along z), and a room cell per (ix,iz), connected to the corridor only
where a door exists at that row.
"""
import re, sys, math, collections

CELL = 5.0
DOOR_ROWS = {1, 3, 4, 8, 11}          # must match Tools/ship-hull/build_hull.py

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
    go_name, tf = {}, {}
    box = {}
    for i, cls, t in docs:
        if cls == 1:
            mm = re.search(r'^  m_Name: (.*)$', t, re.M)
            go_name[i] = mm.group(1) if mm else ''
        elif cls in (4, 224):
            gg = re.search(r'm_GameObject: \{fileID: (\d+)\}', t)
            ff = re.search(r'm_Father: \{fileID: (-?\d+)\}', t)
            pp = re.search(r'm_LocalPosition: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}', t)
            ss = re.search(r'm_LocalScale: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}', t)
            tf[i] = {'go': int(gg.group(1)) if gg else 0,
                     'father': int(ff.group(1)) if ff else 0,
                     'pos': tuple(float(v) for v in pp.groups()) if pp else (0, 0, 0),
                     'scale': tuple(float(v) for v in ss.groups()) if ss else (1, 1, 1)}
        elif cls == 65:
            gg = re.search(r'm_GameObject: \{fileID: (\d+)\}', t)
            en = re.search(r'm_Enabled: (\d)', t)
            sz = re.search(r'm_Size: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}', t)
            ce = re.search(r'm_Center: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}', t)
            box[int(gg.group(1))] = (en.group(1) == '1', tuple(float(v) for v in sz.groups()),
                                     tuple(float(v) for v in ce.groups()))
    name = {i: go_name.get(v['go'], '') for i, v in tf.items()}
    return name, tf, box

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
    scene = next((a for a in sys.argv[1:] if not a.startswith('--')),
                 'Assets/Scenes/Ship - CoOp.unity')
    docs = load(scene)
    name, tf, boxes = build(docs)

    floor_cells = collections.defaultdict(set)
    blockers = []
    player = None
    for i, n in name.items():
        m = re.match(r'^GFloor_(\d)_(-?\d+)_(-?\d+)$', n)
        if m:
            floor_cells[int(m.group(1))].add((int(m.group(2)), int(m.group(3))))
            continue
        if n == 'GStairRamp':
            continue
        go = tf[i]['go']
        b = boxes.get(go)
        if b and b[0] and n.startswith('G'):
            wx, wy, wz = world(i, tf)
            lsx, lsy, lsz = tf[i]['scale']
            sx, sy, sz = b[1][0] * lsx, b[1][1] * lsy, b[1][2] * lsz
            cx, cy, cz = b[2]
            blockers.append((wx + cx * lsx, wy + cy * lsy, wz + cz * lsz, sx, sy, sz, n))
        if n == 'Player':
            player = world(i, tf)

    def blocked_between(a, b, deck):
        ax, az = a; bx, bz = b
        n = max(4, int(math.hypot(bx - ax, bz - az) / 0.2))
        y = 0.9 if deck == 1 else 5.9
        for k in range(1, n):
            t = k / n
            px, pz = ax + (bx - ax) * t, az + (bz - az) * t
            for (cx, cy, cz, sx, sy, sz, bn) in blockers:
                if (abs(px - cx) < sx / 2 - 0.03 and abs(pz - cz) < sz / 2 - 0.03
                        and abs(cy - y) < sy / 2 + 1.0):
                    return bn
        return None

    all_cells = {(d, c) for d, cs in floor_cells.items() for c in cs}
    rows = sorted(set(iz for (d, (ix, iz)) in all_cells))

    ROOM_X_OF = lambda ix: (5 * ix + 2.5)
    adj = collections.defaultdict(list)

    def add_edge(a, b):
        adj[a].append(b)
        adj[b].append(a)

    for deck in floor_cells:
        for iz in rows:
            has_corridor = any((ix, iz) in floor_cells[deck] for ix in (-1, 0))
            if not has_corridor:
                continue
            nxt = iz + 1
            if nxt in rows and any((ix, nxt) in floor_cells[deck] for ix in (-1, 0)):
                add_edge((deck, 'corridor', iz), (deck, 'corridor', nxt))
            if iz in DOOR_ROWS:
                for ix in (-2, -1, 0, 1):
                    if (ix, iz) in floor_cells[deck]:
                        add_edge((deck, 'corridor', iz), (deck, 'room', ix, iz))
        for (ix, iz) in sorted(floor_cells[deck]):
            nb = (ix, iz + 1)
            if nb in floor_cells[deck]:
                cx = ROOM_X_OF(ix)
                hit = blocked_between((cx, 5 * iz + 2.5), (cx, 5 * nb[1] + 2.5), deck)
                if hit is None:
                    add_edge((deck, 'room', ix, iz), (deck, 'room', ix, nb[1]))
            nb2 = (ix + 1, iz)
            # every ix boundary is a plain grid line with no wall, EXCEPT ix=-1/ix=0:
            # the room parts of those two columns are separated by the corridor sliver
            # itself (room x[-5,-1.25] and x[1.25,5] don't touch - a 2.5 m gap sits
            # between them), so that one crossing must route through the corridor node.
            if nb2 in floor_cells[deck] and not (ix == -1 and nb2[0] == 0):
                add_edge((deck, 'room', ix, iz), (deck, 'room', nb2[0], iz))

    # the one deck-1 <-> deck-2 edge: the stair, foot in deck-1's void room, head
    # opening onto deck-2's (-2,6) room through the landing's unrailed west edge.
    add_edge((1, 'room', -1, 5), (2, 'room', -2, 6))

    start_ix = int(math.floor(player[0] / CELL))
    start_iz = int(math.floor(player[2] / CELL))
    start_deck = 1 if player[1] < 2.5 else 2
    start = (start_deck, 'room', start_ix, start_iz)
    if (start_ix, start_iz) not in floor_cells[start_deck]:
        print("FAIL: player spawn %s is not on a floor cell" % (player,))
        return 1

    seen = {start}
    q = collections.deque([start])
    while q:
        cur = q.popleft()
        for nb in adj[cur]:
            if nb not in seen:
                seen.add(nb)
                q.append(nb)

    all_room_nodes = {(d, 'room', ix, iz) for d, cs in floor_cells.items() for (ix, iz) in cs}
    unreached = sorted(all_room_nodes - seen)
    print("player spawn: deck %d cell (%d,%d)" % (start_deck, start_ix, start_iz))
    print("total floor cells: %d   reached: %d   unreached: %d"
          % (len(all_room_nodes), len(all_room_nodes) - len(unreached), len(unreached)))
    for d in sorted(floor_cells):
        total = len(floor_cells[d])
        r = sum(1 for (ix, iz) in floor_cells[d] if (d, 'room', ix, iz) in seen)
        print("  deck %d: %d/%d reached" % (d, r, total))
    if unreached:
        print("UNREACHED cells:")
        for u in unreached[:20]:
            print("  ", u)
        return 1
    print("all cells reachable")
    return 0

if __name__ == '__main__':
    sys.exit(main())
