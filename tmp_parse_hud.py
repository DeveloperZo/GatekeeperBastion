import re
from pathlib import Path

LAYOUT = {
    "59f8146938fff824cb5fd77236b75775": "VLG",
    "30649d3a9faa99c48a7b9703309c13f2": "HLG",
    "3245ec927dfc4c40b2fa6913b290d300": "ContentSizeFitter",
    "30649d3a9faa99c48a7b9703309c13f2": "HLG",
}
# LayoutElement common Unity guid
LAYOUT_ELEMENT_HINT = "m_MinWidth:"
IMAGE_SCRIPT = "fe87c0e1cc204ed48ad3b37840f39efc"


def parse_scene(path: Path):
    text = path.read_text(encoding="utf-8", errors="replace")
    gos = {}
    for m in re.finditer(r"--- !u!1 &(\d+)\nGameObject:\n(.*?)(?=\n--- !u!|\Z)", text, re.S):
        fid, body = m.group(1), m.group(2)
        name = re.search(r"m_Name: (.*)", body)
        active = re.search(r"m_IsActive: (\d)", body)
        gos[fid] = {
            "name": name.group(1).strip() if name else "?",
            "active": active.group(1) if active else "?",
            "transform": None,
            "children": [],
            "father": None,
            "rect": {},
            "images": [],
            "layouts": [],
        }

    for m in re.finditer(r"--- !u!224 &(\d+)\nRectTransform:\n(.*?)(?=\n--- !u!|\Z)", text, re.S):
        fid, body = m.group(1), m.group(2)
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)
        if not go:
            continue
        gid = go.group(1)
        if gid not in gos:
            continue
        ch_m = re.search(r"m_Children:\n((?:  - \{fileID: \d+\}\n)*)", body)
        children = re.findall(r"fileID: (\d+)", ch_m.group(1)) if ch_m else []
        father = re.search(r"m_Father: \{fileID: (\d+)\}", body)
        amin = re.search(r"m_AnchorMin: \{x: ([^,]+), y: ([^}]+)\}", body)
        amax = re.search(r"m_AnchorMax: \{x: ([^,]+), y: ([^}]+)\}", body)
        pos = re.search(r"m_AnchoredPosition: \{x: ([^,]+), y: ([^}]+)\}", body)
        size = re.search(r"m_SizeDelta: \{x: ([^,]+), y: ([^}]+)\}", body)
        pivot = re.search(r"m_Pivot: \{x: ([^,]+), y: ([^}]+)\}", body)
        gos[gid]["transform"] = fid
        gos[gid]["children"] = children
        gos[gid]["father"] = father.group(1) if father else None
        gos[gid]["rect"] = {
            "anchorMin": (amin.group(1), amin.group(2)) if amin else None,
            "anchorMax": (amax.group(1), amax.group(2)) if amax else None,
            "pos": (pos.group(1), pos.group(2)) if pos else None,
            "size": (size.group(1), size.group(2)) if size else None,
            "pivot": (pivot.group(1), pivot.group(2)) if pivot else None,
        }

    t2g = {g["transform"]: gid for gid, g in gos.items() if g["transform"]}

    for m in re.finditer(r"--- !u!114 &(\d+)\nMonoBehaviour:\n(.*?)(?=\n--- !u!|\Z)", text, re.S):
        body = m.group(2)
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)
        if not go:
            continue
        gid = go.group(1)
        if gid not in gos:
            continue
        script = re.search(r"m_Script: \{fileID: 11500000, guid: ([a-f0-9]+)", body)
        sg = script.group(1) if script else ""
        if IMAGE_SCRIPT in body and "m_Sprite:" in body:
            spr = re.search(
                r"m_Sprite: \{fileID: (-?\d+), guid: ([a-f0-9]+), type: \d+\}", body
            )
            color = re.search(
                r"m_Color: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}", body
            )
            if spr:
                gos[gid]["images"].append(
                    {
                        "sprite_guid": spr.group(2),
                        "sprite_fileID": spr.group(1),
                        "color": color.groups() if color else None,
                    }
                )
            elif "m_Sprite: {fileID: 0}" in body:
                color = re.search(
                    r"m_Color: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}", body
                )
                gos[gid]["images"].append(
                    {
                        "sprite_guid": None,
                        "sprite_fileID": "0",
                        "color": color.groups() if color else None,
                    }
                )
        if sg in (
            "59f8146938fff824cb5fd77236b75775",
            "30649d3a9faa99c48a7b9703309c13f2",
        ):
            pad = re.search(
                r"m_Padding:\n    m_Left: (-?\d+)\n    m_Right: (-?\d+)\n    m_Top: (-?\d+)\n    m_Bottom: (-?\d+)",
                body,
            )
            spacing = re.search(r"m_Spacing: ([^\n]+)", body)
            align = re.search(r"m_ChildAlignment: (\d+)", body)
            gos[gid]["layouts"].append(
                {
                    "type": "VLG" if sg.startswith("59f") else "HLG",
                    "padding": pad.groups() if pad else None,
                    "spacing": spacing.group(1) if spacing else None,
                    "align": align.group(1) if align else None,
                }
            )
        if "m_MinWidth:" in body and "m_PreferredWidth:" in body and sg:
            mw = re.search(r"m_MinWidth: ([^\n]+)", body)
            mh = re.search(r"m_MinHeight: ([^\n]+)", body)
            pw = re.search(r"m_PreferredWidth: ([^\n]+)", body)
            ph = re.search(r"m_PreferredHeight: ([^\n]+)", body)
            gos[gid]["layouts"].append(
                {
                    "type": "LayoutElement",
                    "minW": mw.group(1) if mw else None,
                    "minH": mh.group(1) if mh else None,
                    "prefW": pw.group(1) if pw else None,
                    "prefH": ph.group(1) if ph else None,
                }
            )
    return gos, t2g


def print_tree(gos, t2g, root_go_id, indent=0, max_depth=10):
    g = gos[root_go_id]
    r = g["rect"]
    parts = [
        f"{'  ' * indent}- {g['name']} active={g['active']}",
        f"size={r.get('size')}",
        f"amin={r.get('anchorMin')} amax={r.get('anchorMax')}",
        f"pos={r.get('pos')} pivot={r.get('pivot')}",
    ]
    if g["layouts"]:
        lay = []
        for L in g["layouts"]:
            if L["type"] in ("VLG", "HLG"):
                lay.append(
                    f"{L['type']} pad={L['padding']} space={L['spacing']} align={L['align']}"
                )
            else:
                lay.append(
                    f"{L['type']} min={L.get('minW')}x{L.get('minH')} pref={L.get('prefW')}x{L.get('prefH')}"
                )
        parts.append("layouts=[" + "; ".join(lay) + "]")
    if g["images"]:
        imgs = []
        for i in g["images"]:
            a = i["color"][3] if i["color"] else "?"
            rgb = (
                f"rgb=({i['color'][0]},{i['color'][1]},{i['color'][2]})"
                if i["color"]
                else ""
            )
            imgs.append(f"spr={i['sprite_guid']} a={a} {rgb}".strip())
        parts.append("images=[" + "; ".join(imgs) + "]")
    print(" | ".join(parts))
    if indent >= max_depth:
        return
    for ct in g["children"]:
        cgid = t2g.get(ct)
        if cgid:
            print_tree(gos, t2g, cgid, indent + 1, max_depth)
        else:
            print(f"{'  ' * (indent + 1)}- <missing transform {ct}>")


def find_named(gos, name):
    return [(gid, g) for gid, g in gos.items() if g["name"] == name]


def parent_name(gos, t2g, gid):
    father_t = gos[gid]["father"]
    if not father_t:
        return None
    # father is transform fileID
    pg = t2g.get(father_t)
    return gos[pg]["name"] if pg else f"t:{father_t}"


if __name__ == "__main__":
    sample = Path(
        r"F:/Programming/Gatekeeper Bastion/Assets/Shift - Complete Sci-Fi UI/Scenes/Simple HUD/Simple HUD (Desktop).unity"
    )
    bastion = Path(r"F:/Programming/Gatekeeper Bastion/Assets/Scenes/Bastion.unity")

    print("=" * 80)
    print("SAMPLE: Armor & Health")
    print("=" * 80)
    gos, t2g = parse_scene(sample)
    for gid, g in find_named(gos, "Armor & Health"):
        print(f"root go={gid} parent={parent_name(gos, t2g, gid)}")
        print_tree(gos, t2g, gid)

    print("\n" + "=" * 80)
    print("BASTION: Armor & Health / Hud Vitals / Health / Stamina / Armor")
    print("=" * 80)
    gos, t2g = parse_scene(bastion)
    for name in ("Armor & Health", "Hud Vitals", "Health", "Stamina", "Armor"):
        hits = find_named(gos, name)
        print(f"\n### {name} ({len(hits)} hits)")
        for gid, g in hits:
            print(
                f"go={gid} parent={parent_name(gos, t2g, gid)} active={g['active']} children={len(g['children'])}"
            )
            print_tree(gos, t2g, gid, max_depth=8)
