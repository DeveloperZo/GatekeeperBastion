from pathlib import Path
import importlib.util

spec = importlib.util.spec_from_file_location("tmp_parse_hud", "tmp_parse_hud.py")
mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mod)

gos, t2g = mod.parse_scene(
    Path(r"F:/Programming/Gatekeeper Bastion/Assets/Scenes/Bastion.unity")
)


def walk(gid, depth=0):
    g = gos[gid]
    yield depth, g["name"], g["active"], len(g["children"])
    for ct in g["children"]:
        cg = t2g.get(ct)
        if cg:
            yield from walk(cg, depth + 1)


def ancestors(gid):
    chain = []
    cur = gid
    while cur:
        chain.append(gos[cur]["name"])
        ft = gos[cur]["father"]
        cur = t2g.get(ft) if ft else None
    return chain


for name in ["Health", "Stamina", "Armor", "Armor & Health", "Hud Vitals"]:
    for gid, g in mod.find_named(gos, name):
        print(f"\n=== {name} path={' / '.join(reversed(ancestors(gid)))}")
        for d, n, a, nc in walk(gid):
            print(f"{'  '*d}{n} active={a} kids={nc}")

# Check for Background / Handle / + under each vitals-related row
print("\n=== name presence under Health/Stamina/Armor ===")
for name in ["Health", "Stamina", "Armor"]:
    for gid, g in mod.find_named(gos, name):
        names = {n for _, n, _, _ in walk(gid)}
        for want in ["Background", "Handle", "Handle Slide Area", "+", "Icon Border", "Glow", "Border", "Bar", "Filled", "Icon", "Text", "Slider"]:
            print(f"  {name}: {want}={'YES' if want in names else 'NO'}")

# Sample Armor row children order + HLG check
gos2, t2g2 = mod.parse_scene(
    Path(
        r"F:/Programming/Gatekeeper Bastion/Assets/Shift - Complete Sci-Fi UI/Scenes/Simple HUD/Simple HUD (Desktop).unity"
    )
)
print("\n=== SAMPLE Armor/Health component layouts + child order ===")
for name in ["Armor", "Health"]:
    for gid, g in mod.find_named(gos2, name):
        if mod.parent_name(gos2, t2g2, gid) != "Armor & Health":
            continue
        print(f"{name} layouts={g['layouts']}")
        print(f"  children transforms: {g['children']}")
        for ct in g["children"]:
            cg = t2g2.get(ct)
            if cg:
                print(f"  - {gos2[cg]['name']} active={gos2[cg]['active']} layouts={gos2[cg]['layouts']}")

# Resolve sprite guids to names via meta
print("\n=== sprite guid -> asset ===")
import os

guids = [
    "671a5b730f15d06488ea1d7cf4e65e6f",
    "264ea33392537064c9608be6535ec11b",
    "7724d2fa1a180bc43968a0a414b55e79",
    "dc2366542d8657842a8bf5378c77bee0",
    "447f19d95ff0c504da9f0a9dacc358d7",
]
root = Path(r"F:/Programming/Gatekeeper Bastion/Assets")
for guid in guids:
    found = None
    for meta in root.rglob("*.meta"):
        try:
            t = meta.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            continue
        if f"guid: {guid}" in t:
            found = str(meta)[:-5]
            break
    print(f"  {guid} -> {found}")
