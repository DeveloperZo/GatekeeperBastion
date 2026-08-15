#!/usr/bin/env python3
"""One-shot scan of Ship.unity station panel RectTransforms. Delete after use."""
import re

path = r"F:/Programming/Gatekeeper Bastion/Assets/Scenes/Ship.unity"
with open(path, "r", encoding="utf-8") as f:
    text = f.read()

go_blocks = re.split(r"\n--- !u!", text)
gos = {}
rects = {}
hlgs = {}
vlgs = {}
tmp = {}
panel_rows = {}
go_of_comp = {}
layout_elem = {}
content_size = {}

for block in go_blocks:
    if not block.strip():
        continue
    m = re.match(r"(\d+) &(-?\d+)\n", block)
    if not m:
        continue
    typ, fid = int(m.group(1)), int(m.group(2))
    body = block[m.end() :]

    if typ == 1:
        name_m = re.search(r"m_Name: (.+)", body)
        name = name_m.group(1).strip() if name_m else "?"
        comps = re.findall(r"- component: \{fileID: (-?\d+)\}", body)
        active_m = re.search(r"m_IsActive: ([01])", body)
        gos[fid] = {
            "name": name,
            "comps": [int(c) for c in comps],
            "active": active_m.group(1) if active_m else "1",
        }
        for c in comps:
            go_of_comp[int(c)] = fid
    elif typ == 224:
        father_m = re.search(r"m_Father: \{fileID: (-?\d+)\}", body)
        amin = re.search(r"m_AnchorMin: \{x: ([-\d.]+), y: ([-\d.]+)\}", body)
        amax = re.search(r"m_AnchorMax: \{x: ([-\d.]+), y: ([-\d.]+)\}", body)
        apos = re.search(r"m_AnchoredPosition: \{x: ([-\d.]+), y: ([-\d.]+)\}", body)
        size = re.search(r"m_SizeDelta: \{x: ([-\d.]+), y: ([-\d.]+)\}", body)
        pivot = re.search(r"m_Pivot: \{x: ([-\d.]+), y: ([-\d.]+)\}", body)
        ch_m = re.search(r"m_Children:\n((?:  - \{fileID: -?\d+\}\n)*)", body)
        kids = []
        if ch_m:
            kids = [int(x) for x in re.findall(r"fileID: (-?\d+)", ch_m.group(1))]
        go_m = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
        rects[fid] = {
            "go": int(go_m.group(1)) if go_m else 0,
            "father": int(father_m.group(1)) if father_m else 0,
            "amin": (float(amin.group(1)), float(amin.group(2))) if amin else None,
            "amax": (float(amax.group(1)), float(amax.group(2))) if amax else None,
            "apos": (float(apos.group(1)), float(apos.group(2))) if apos else None,
            "size": (float(size.group(1)), float(size.group(2))) if size else None,
            "pivot": (float(pivot.group(1)), float(pivot.group(2))) if pivot else None,
            "kids": kids,
        }
    elif typ == 114:
        go_m = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", body)
        go_id = int(go_m.group(1)) if go_m else 0
        script = re.search(r"m_Script: \{fileID: 11500000, guid: ([a-f0-9]+)", body)
        guid = script.group(1) if script else ""
        if "m_Spacing:" in body and "m_ChildForceExpandWidth" in body:
            spacing = re.search(r"m_Spacing: ([-\d.]+)", body)
            pad = re.search(
                r"m_Padding:\n    m_Left: (\d+)\n    m_Right: (\d+)\n    m_Top: (\d+)\n    m_Bottom: (\d+)",
                body,
            )
            kind = (
                "HLG"
                if guid == "30649d3a9faa99c48a7b1166e147e36a"
                else ("VLG" if guid == "59f8146938fff824cb5fd77236b75775" else None)
            )
            if kind:
                entry = {
                    "go": go_id,
                    "kind": kind,
                    "spacing": float(spacing.group(1)) if spacing else None,
                    "pad": tuple(int(x) for x in pad.groups()) if pad else None,
                    "childCtrlW": re.search(r"m_ChildControlWidth: ([01])", body),
                    "childCtrlH": re.search(r"m_ChildControlHeight: ([01])", body),
                    "childForceW": re.search(r"m_ChildForceExpandWidth: ([01])", body),
                    "childForceH": re.search(r"m_ChildForceExpandHeight: ([01])", body),
                }
                for k in ("childCtrlW", "childCtrlH", "childForceW", "childForceH"):
                    mm = entry[k]
                    entry[k] = mm.group(1) if mm else "?"
                if kind == "HLG":
                    hlgs[fid] = entry
                else:
                    vlgs[fid] = entry
        if "m_PreferredWidth:" in body and "m_MinWidth:" in body:
            pw = re.search(r"m_PreferredWidth: ([-\d.]+)", body)
            mw = re.search(r"m_MinWidth: ([-\d.]+)", body)
            flex = re.search(r"m_FlexibleWidth: ([-\d.]+)", body)
            layout_elem[fid] = {
                "go": go_id,
                "minW": float(mw.group(1)) if mw else None,
                "prefW": float(pw.group(1)) if pw else None,
                "flexW": float(flex.group(1)) if flex else None,
            }
        if "m_text:" in body and "m_fontSize:" in body:
            txt = re.search(r"m_text: (.*)", body)
            fs = re.search(r"m_fontSize: ([-\d.]+)", body)
            ov = re.search(r"m_overflowMode: (\d+)", body)
            auto = re.search(r"m_enableAutoSizing: ([01])", body)
            wrap = re.search(r"m_enableWordWrapping: ([01])", body)
            tmp[fid] = {
                "go": go_id,
                "text": (txt.group(1).strip() if txt else "")[:90],
                "font": float(fs.group(1)) if fs else None,
                "overflow": int(ov.group(1)) if ov else None,
                "auto": auto.group(1) if auto else None,
                "wrap": wrap.group(1) if wrap else None,
            }
        if (
            "_name:" in body
            and "_cost:" in body
            and "_action:" in body
            and "_reason:" in body
        ):

            def ref(field):
                mm = re.search(rf"{field}: \{{fileID: (-?\d+)\}}", body)
                return int(mm.group(1)) if mm else 0

            panel_rows[go_id] = {
                "name": ref("_name"),
                "cost": ref("_cost"),
                "level": ref("_level"),
                "effect": ref("_effect"),
                "reason": ref("_reason"),
                "action": ref("_action"),
            }

go_rect = {r["go"]: rid for rid, r in rects.items()}


def path_of(go_id, limit=24):
    parts = []
    cur = go_id
    seen = set()
    for _ in range(limit):
        if cur not in gos or cur in seen:
            break
        seen.add(cur)
        parts.append(gos[cur]["name"])
        rid = go_rect.get(cur)
        if not rid:
            break
        father_rid = rects[rid]["father"]
        if father_rid == 0 or father_rid not in rects:
            break
        cur = rects[father_rid]["go"]
    return "/".join(reversed(parts))


def fmt_rect(rid):
    r = rects[rid]
    return (
        f"apos={r['apos']} size={r['size']} "
        f"amin={r['amin']} amax={r['amax']} pivot={r['pivot']}"
    )


def station_of(path):
    for s in ("ARMORY", "NAV MAP", "FABRICATION", "CREW QUARTERS"):
        if f"/{s}/" in f"/{path}/" or path.endswith(s) or path.startswith(s):
            # more precise: look for segment
            segs = path.split("/")
            if s in segs:
                return s
    return "?"


OV = {0: "Overflow", 1: "Ellipsis", 2: "Masking", 3: "Truncate"}

print("=== Station-named GameObjects ===")
for gid, g in sorted(gos.items(), key=lambda x: (x[1]["name"], path_of(x[0]))):
    if g["name"] in ("ARMORY", "NAV MAP", "FABRICATION", "CREW QUARTERS"):
        print(f"{g['name']} active={g['active']} path={path_of(gid)}")

print("\n=== PanelRowView hosts (all) ===")
for go_id in sorted(panel_rows.keys(), key=lambda g: path_of(g)):
    p = path_of(go_id)
    st = station_of(p)
    print(f"\n[{st}] {gos[go_id]['name']}")
    print(f"  path={p}")
    rid = go_rect.get(go_id)
    if rid:
        print(f"  ROW: {fmt_rect(rid)}")
        for c in gos[go_id]["comps"]:
            if c in hlgs:
                h = hlgs[c]
                print(
                    f"  HLG spacing={h['spacing']} padLRTB={h['pad']} "
                    f"ctrlW/H={h['childCtrlW']}/{h['childCtrlH']} "
                    f"forceW/H={h['childForceW']}/{h['childForceH']}"
                )
            if c in vlgs:
                v = vlgs[c]
                print(
                    f"  VLG spacing={v['spacing']} padLRTB={v['pad']} "
                    f"ctrlW/H={v['childCtrlW']}/{v['childCtrlH']} "
                    f"forceW/H={v['childForceW']}/{v['childForceH']}"
                )
            if c in layout_elem:
                le = layout_elem[c]
                print(f"  LayoutElement minW={le['minW']} prefW={le['prefW']} flexW={le['flexW']}")
        for kid in rects[rid]["kids"]:
            if kid not in rects:
                print(f"  child fileID={kid} (missing RectTransform — prefab/external)")
                continue
            kg = rects[kid]["go"]
            kn = gos.get(kg, {}).get("name", "?")
            print(f"  child `{kn}`: {fmt_rect(kid)}")
            for c in gos.get(kg, {}).get("comps", []):
                if c in tmp:
                    t = tmp[c]
                    print(
                        f"    TMP font={t['font']} ov={OV.get(t['overflow'], t['overflow'])} "
                        f"auto={t['auto']} wrap={t['wrap']} text={t['text']!r}"
                    )
                if c in layout_elem:
                    le = layout_elem[c]
                    print(
                        f"    LayoutElement minW={le['minW']} prefW={le['prefW']} flexW={le['flexW']}"
                    )
                if c in hlgs:
                    h = hlgs[c]
                    print(f"    HLG spacing={h['spacing']} pad={h['pad']}")
        # Also dump PanelRowView field targets (action may be nested)
        pr = panel_rows[go_id]
        for field, comp_id in pr.items():
            if not comp_id:
                print(f"  bind _{field}=null")
                continue
            tg = go_of_comp.get(comp_id)
            if tg is None:
                # Button/TMP may be the component id; look up via go_of_comp
                print(f"  bind _{field} comp={comp_id} (no GO map)")
                continue
            tr = go_rect.get(tg)
            tname = gos.get(tg, {}).get("name", "?")
            extra = ""
            if tr:
                extra = " " + fmt_rect(tr)
            for c in gos.get(tg, {}).get("comps", []):
                if c in tmp:
                    t = tmp[c]
                    extra += (
                        f" TMP font={t['font']} ov={OV.get(t['overflow'], t['overflow'])} "
                        f"text={t['text']!r}"
                    )
            print(f"  bind _{field} -> `{tname}`{extra}")

# Header / body / close chrome for each station
chrome_names = (
    "Title",
    "Purpose",
    "Banked Salvage",
    "Lifetime",
    "Deal",
    "Footer",
    "List Content",
    "Close",
    "Wheel Slot",
    "Catalog Heading",
    "Equipped Line",
    "Stat Line",
    "Rotate Hint",
    "Ring Note",
    "Rows Missing",
    "Start Wave Line",
    "Wave Value",
    "Embark",
    "EMBARK",
    "Wave Prev",
    "Wave Next",
    "Destination",
    "Premise",
    "No Weapon Warning",
    "No Rig Warning",
    "Load Failed",
    "Header",
    "Body",
    "Content",
    "Panel Body",
    "Window",
    "Background",
)

print("\n=== Chrome / body objects under each station ===")
for st in ("ARMORY", "NAV MAP", "FABRICATION", "CREW QUARTERS"):
    print(f"\n---- {st} ----")
    matches = []
    for gid, g in gos.items():
        p = path_of(gid)
        if st not in p.split("/"):
            continue
        if g["name"] in chrome_names or "Row" in g["name"] or "Heading" in g["name"] or g["name"] in (
            "Primary",
            "Secondary",
            "Slot Buttons",
            "Wave Stepper",
            "Map Panel",
            "Nav Panel",
            "Bastion Node",
        ):
            rid = go_rect.get(gid)
            matches.append((p.count("/"), p, g["name"], rid, gid))
    for _, p, name, rid, gid in sorted(matches)[:80]:
        line = f"  {name}: path={p}"
        if rid:
            line += f"\n    {fmt_rect(rid)}"
        for c in gos[gid]["comps"]:
            if c in vlgs:
                v = vlgs[c]
                line += f"\n    VLG spacing={v['spacing']} pad={v['pad']}"
            if c in hlgs:
                h = hlgs[c]
                line += f"\n    HLG spacing={h['spacing']} pad={h['pad']}"
            if c in tmp:
                t = tmp[c]
                line += (
                    f"\n    TMP font={t['font']} ov={OV.get(t['overflow'], t['overflow'])} "
                    f"auto={t['auto']} text={t['text']!r}"
                )
        print(line)

# Also dump direct children of List Content per station
print("\n=== List Content children + VLG ===")
for gid, g in gos.items():
    if g["name"] != "List Content":
        continue
    p = path_of(gid)
    st = station_of(p)
    if st == "?":
        continue
    rid = go_rect[gid]
    print(f"\n[{st}] List Content {fmt_rect(rid)}")
    for c in gos[gid]["comps"]:
        if c in vlgs:
            v = vlgs[c]
            print(f"  VLG spacing={v['spacing']} pad={v['pad']} ctrl={v['childCtrlW']}/{v['childCtrlH']}")
        if c in hlgs:
            h = hlgs[c]
            print(f"  HLG spacing={h['spacing']} pad={h['pad']}")
    for kid in rects[rid]["kids"]:
        if kid not in rects:
            print(f"  - fileID={kid} (missing)")
            continue
        kg = rects[kid]["go"]
        print(f"  - {gos[kg]['name']}: {fmt_rect(kid)}")
