#!/usr/bin/env python3
"""Prove every walkable room is reachable from the player spawn.

    python Tools/scene-check/reachability.py "Assets/Scenes/Ship - CoOp.unity"

Builds an adjacency graph from GRAYBOX colliders only (they are gameplay truth here:
dress is cosmetic, Environment blocks movement) and BFS's from the Player spawn.

The ship is ONE deck. There is no stair, no void and no inter-deck edge: every room is
reached by walking the single floor plane, so a cell that cannot be reached is a real
hole in the layout and never "you needed the stair for that one".

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

# Height the corridor/room blocking probe is fired at: chest height on the one floor
# plane. It used to be picked per deck (0.9 or 5.9); with a single deck it is one
# number, but it is still a NAMED one, because it is the reason a partition counts as
# a partition. Graybox wall boxes span floor..floor+4, so any probe inside that band
# reads the same - 0.9 is kept so the numbers stay comparable with older runs.
PROBE_Y = 0.9

# CharacterController stepOffset 0.3. Used as the slack when judging whether a surface
# or a pivot sits at the floor plane rather than somewhere else entirely.
WALK_Y = 0.3
# The Player capsule's pivot sits 1.10 m above the floor it stands on. A pivot may be
# up to this far above the plane and still be standing on it; a whole deck height above
# (5 m, the old upper plane) is an object the collapse forgot to bring down.
PIVOT_ABOVE = 2.0

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
            rr = re.search(r'm_LocalRotation: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+), w: (-?[\d.eE+-]+)\}', t)
            tf[i] = {'go': int(gg.group(1)) if gg else 0,
                     'father': int(ff.group(1)) if ff else 0,
                     'pos': tuple(float(v) for v in pp.groups()) if pp else (0, 0, 0),
                     'scale': tuple(float(v) for v in ss.groups()) if ss else (1, 1, 1),
                     'rot': tuple(float(v) for v in rr.groups()) if rr else (0, 0, 0, 1)}
        elif cls == 65:
            gg = re.search(r'm_GameObject: \{fileID: (\d+)\}', t)
            en = re.search(r'm_Enabled: (\d)', t)
            sz = re.search(r'm_Size: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}', t)
            ce = re.search(r'm_Center: \{x: (-?[\d.eE+-]+), y: (-?[\d.eE+-]+), z: (-?[\d.eE+-]+)\}', t)
            box[int(gg.group(1))] = (en.group(1) == '1', tuple(float(v) for v in sz.groups()),
                                     tuple(float(v) for v in ce.groups()))
    name = {i: go_name.get(v['go'], '') for i, v in tf.items()}
    return name, tf, box

def chain_of(tid, tf):
    """Transform nodes leaf -> root. Cycles guard against a corrupt scene."""
    chain, seen = [], set()
    while tid in tf and tid not in seen:
        seen.add(tid)
        chain.append(tf[tid])
        tid = tf[tid]['father']
    return chain

def to_world(chain, p):
    """Compose scale -> rotate -> translate up the whole parent chain. The old
    position-sum shortcut silently misplaced any collider under a scaled or rotated
    parent, and this scene has ~490 non-unit-scale transforms, so the shortcut is not
    safe to reintroduce even now that the rotated stair prop is gone."""
    for node in chain:
        s = node['scale']
        p = (p[0] * s[0], p[1] * s[1], p[2] * s[2])
        p = qrot(node.get('rot', (0.0, 0.0, 0.0, 1.0)), p)
        o = node['pos']
        p = (p[0] + o[0], p[1] + o[1], p[2] + o[2])
    return p

def world(tid, tf):
    return to_world(chain_of(tid, tf), (0.0, 0.0, 0.0))

def qrot(q, v):
    x, y, z, w = q
    vx, vy, vz = v
    tx = 2.0 * (y * vz - z * vy)
    ty = 2.0 * (z * vx - x * vz)
    tz = 2.0 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty),
            vy + w * ty + (z * tx - x * tz),
            vz + w * tz + (x * ty - y * tx))

def box_aabb(tid, tf, boxes):
    """World AABB of an enabled BoxCollider, full parent chain applied."""
    go = tf[tid]['go']
    b = boxes.get(go)
    if not b or not b[0]:
        return None
    chain = chain_of(tid, tf)
    hx, hy, hz = b[1][0] / 2.0, b[1][1] / 2.0, b[1][2] / 2.0
    cx, cy, cz = b[2]
    xs, ys, zs = [], [], []
    for dx in (-hx, hx):
        for dy in (-hy, hy):
            for dz in (-hz, hz):
                wx, wy, wz = to_world(chain, (cx + dx, cy + dy, cz + dz))
                xs.append(wx)
                ys.append(wy)
                zs.append(wz)
    return (min(xs), max(xs), min(zs), max(zs), min(ys), max(ys))

def main():
    scene = next((a for a in sys.argv[1:] if not a.startswith('--')),
                 'Assets/Scenes/Ship - CoOp.unity')
    docs = load(scene)
    name, tf, boxes = build(docs)

    floor_cells = set()
    floor_tops = collections.Counter()   # walk-surface y -> how many graybox tiles
    decks_seen = collections.Counter()
    blockers = []
    player = None
    for i, n in name.items():
        m = re.match(r'^GFloor_(\d)_(-?\d+)_(-?\d+)$', n)
        if m:
            # Deck-BLIND on purpose: one deck means the token carries no information.
            # It is counted so a leftover second deck is at least visible here; the
            # hard failure on that lives in enclosure.py, which owns the inventory.
            decks_seen[m.group(1)] += 1
            floor_cells.add((int(m.group(2)), int(m.group(3))))
            aabb = box_aabb(i, tf, boxes)
            if aabb is not None:
                floor_tops[round(aabb[5], 3)] += 1
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

    if not floor_cells:
        print("FAIL: no GFloor_* graybox tiles in %s" % scene)
        return 1
    if len(decks_seen) != 1:
        print("WARNING: floor names carry %d different deck tokens (%s) - this ship is"
              " meant to have one deck; run enclosure.py, which fails on it"
              % (len(decks_seen), ", ".join(sorted(decks_seen))))

    # The one floor plane, measured from the graybox tops rather than assumed to be 0
    # so the whole ship can still be moved in Y. Disagreement is fatal: with the stair
    # gone there is nothing that can carry the capsule between two heights, so a tile
    # at a different height is an unreachable island the room graph would not notice
    # (the graph joins cells by index, not by geometry).
    plane = max(floor_tops) if floor_tops else 0.0
    print("floor plane y=%g  (graybox tile tops: %s)"
          % (plane, ", ".join("%g x%d" % (y, c) for y, c in sorted(floor_tops.items()))))
    if len(floor_tops) != 1:
        print("FAIL: graybox floor tiles sit at %d different heights - one deck has one"
              " plane, and nothing here can climb between them" % len(floor_tops))
        return 1

    def blocked_between(a, b):
        """Name of the first graybox box the straight walk a->b passes through."""
        ax, az = a; bx, bz = b
        n = max(4, int(math.hypot(bx - ax, bz - az) / 0.2))
        y = plane + PROBE_Y
        for k in range(1, n):
            t = k / n
            px, pz = ax + (bx - ax) * t, az + (bz - az) * t
            for (cx, cy, cz, sx, sy, sz, bn) in blockers:
                if (abs(px - cx) < sx / 2 - 0.03 and abs(pz - cz) < sz / 2 - 0.03
                        and abs(cy - y) < sy / 2 + 1.0):
                    return bn
        return None

    rows = sorted(set(iz for (ix, iz) in floor_cells))

    ROOM_X_OF = lambda ix: (5 * ix + 2.5)
    adj = collections.defaultdict(list)

    def add_edge(a, b):
        adj[a].append(b)
        adj[b].append(a)

    for iz in rows:
        has_corridor = any((ix, iz) in floor_cells for ix in (-1, 0))
        if not has_corridor:
            continue
        nxt = iz + 1
        if nxt in rows and any((ix, nxt) in floor_cells for ix in (-1, 0)):
            add_edge(('corridor', iz), ('corridor', nxt))
        if iz in DOOR_ROWS:
            for ix in (-2, -1, 0, 1):
                if (ix, iz) in floor_cells:
                    add_edge(('corridor', iz), ('room', ix, iz))
    for (ix, iz) in sorted(floor_cells):
        nb = (ix, iz + 1)
        if nb in floor_cells:
            cx = ROOM_X_OF(ix)
            hit = blocked_between((cx, 5 * iz + 2.5), (cx, 5 * nb[1] + 2.5))
            if hit is None:
                add_edge(('room', ix, iz), ('room', ix, nb[1]))
        nb2 = (ix + 1, iz)
        # every ix boundary is a plain grid line with no wall, EXCEPT ix=-1/ix=0:
        # the room parts of those two columns are separated by the corridor sliver
        # itself (room x[-5,-1.25] and x[1.25,5] don't touch - a 2.5 m gap sits
        # between them), so that one crossing must route through the corridor node.
        if nb2 in floor_cells and not (ix == -1 and nb2[0] == 0):
            add_edge(('room', ix, iz), ('room', nb2[0], iz))

    if player is None:
        print("FAIL: no Player object in %s" % scene)
        return 1
    start_ix = int(math.floor(player[0] / CELL))
    start_iz = int(math.floor(player[2] / CELL))
    start = ('room', start_ix, start_iz)
    if (start_ix, start_iz) not in floor_cells:
        print("FAIL: player spawn %s is not on a floor cell" % (player,))
        return 1
    # The deck the spawn belonged to used to be inferred from its height. There is one
    # deck now, so that inference is gone - but the height itself still has to be
    # checked. A Player left at the old upper plane sits over a valid cell in XZ, so
    # the BFS below would happily report every room reachable from a spawn point
    # hanging 5 m in the air.
    if not (plane - WALK_Y <= player[1] <= plane + PIVOT_ABOVE):
        print("FAIL: player spawn y=%g is not on the floor plane y=%g (allowed %g..%g)"
              % (player[1], plane, plane - WALK_Y, plane + PIVOT_ABOVE))
        return 1

    seen = {start}
    q = collections.deque([start])
    while q:
        cur = q.popleft()
        for nb in adj[cur]:
            if nb not in seen:
                seen.add(nb)
                q.append(nb)

    all_room_nodes = {('room', ix, iz) for (ix, iz) in floor_cells}
    unreached = sorted(all_room_nodes - seen)
    print("player spawn: cell (%d,%d) at y=%g" % (start_ix, start_iz, player[1]))
    print("total floor cells: %d   reached: %d   unreached: %d"
          % (len(all_room_nodes), len(all_room_nodes) - len(unreached), len(unreached)))
    if unreached:
        print("UNREACHED cells:")
        for u in unreached[:20]:
            print("  ", u)
        return 1
    print("all cells reachable")
    return 0

if __name__ == '__main__':
    sys.exit(main())
