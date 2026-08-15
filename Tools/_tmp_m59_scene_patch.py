import re
from pathlib import Path


def patch_tmp(text, name, *, auto=1, overflow=0, wrap=1):
    idx = 0
    changed = 0
    while True:
        m = text.find(f"m_Name: {name}\n", idx)
        if m < 0:
            break
        go = text.rfind("--- !u!1 &", 0, m)
        block_end = text.find("\n--- !u!", go + 5)
        go_block = text[go:block_end]
        comps = re.findall(r"component: \{fileID: (\d+)\}", go_block)
        for c in comps:
            s = text.find(f"--- !u!114 &{c}\n")
            if s < 0:
                continue
            e = text.find("\n--- !u!", s + 5)
            chunk = text[s:e]
            if "m_overflowMode:" not in chunk and "m_enableAutoSizing:" not in chunk:
                continue
            new = chunk
            if "m_enableAutoSizing:" in new:
                new = re.sub(r"m_enableAutoSizing: \d", f"m_enableAutoSizing: {auto}", new)
            if "m_overflowMode:" in new:
                new = re.sub(r"m_overflowMode: \d", f"m_overflowMode: {overflow}", new)
            if "m_enableWordWrapping:" in new:
                new = re.sub(r"m_enableWordWrapping: \d", f"m_enableWordWrapping: {wrap}", new)
            if new != chunk:
                text = text[:s] + new + text[e:]
                changed += 1
            break
        idx = m + 1
    return text, changed


def set_active(text, name, active):
    for mm in re.finditer(rf"m_Name: {re.escape(name)}\n", text):
        go = text.rfind("--- !u!1 &", 0, mm.start())
        e = text.find("\n--- !u!", go + 5)
        block = text[go:e]
        nb = re.sub(r"(m_IsActive: )\d", rf"\g<1>{active}", block, count=1)
        if nb != block:
            text = text[:go] + nb + text[e:]
            return text, True
    return text, False


def set_image_alpha_under_named(text, name, alpha):
    changed = 0
    idx = 0
    while True:
        m = text.find(f"m_Name: {name}\n", idx)
        if m < 0:
            break
        go = text.rfind("--- !u!1 &", 0, m)
        e = text.find("\n--- !u!", go + 5)
        go_block = text[go:e]
        comps = re.findall(r"component: \{fileID: (\d+)\}", go_block)
        for c in comps:
            s = text.find(f"--- !u!114 &{c}\n")
            if s < 0:
                continue
            ee = text.find("\n--- !u!", s + 5)
            chunk = text[s:ee]
            if "m_Color:" not in chunk or "m_Sprite:" not in chunk:
                continue
            new = re.sub(
                r"(m_Color: \{r: [-\d.]+, g: [-\d.]+, b: [-\d.]+, a: )[-\d.]+(\})",
                rf"\g<1>{alpha}\2",
                chunk,
                count=1,
            )
            if new != chunk:
                text = text[:s] + new + text[ee:]
                changed += 1
            break
        idx = m + 1
    return text, changed


bpath = Path("Assets/Scenes/Bastion.unity")
b = bpath.read_text(encoding="utf-8")
for n in [
    "Score Salvage",
    "Wave Counter",
    "Weapon Name",
    "Drawer Left Header",
    "Drawer Right Header",
    "Msg General Text",
    "Msg Player Text",
]:
    b, nchg = patch_tmp(b, n, auto=1, overflow=0, wrap=1)
    print("Bastion TMP", n, nchg)

b, ok = set_active(b, "Profile Exp", 0)
print("Bastion Profile Exp inactive", ok)
b, n = set_image_alpha_under_named(b, "Scrim", 0.65)
print("Bastion Scrim alphas set", n)
bpath.write_text(b, encoding="utf-8")

spath = Path("Assets/Scenes/Ship.unity")
s = spath.read_text(encoding="utf-8")
s, ok1 = set_active(s, "Categories", 0)
s, ok2 = set_active(s, "Preview Image", 0)
print("Ship Categories inactive", ok1, "Preview", ok2)
s, n = set_image_alpha_under_named(s, "Background Blur", 0.65)
print("Ship Background Blur alphas", n)
s, ok = set_active(s, "Profile Exp", 0)
print("Ship Profile Exp", ok)
for n in ["Score Salvage", "Wave Counter", "Weapon Name"]:
    s, nchg = patch_tmp(s, n, auto=1, overflow=0, wrap=1)
    print("Ship TMP", n, nchg)
spath.write_text(s, encoding="utf-8")
print("done")
