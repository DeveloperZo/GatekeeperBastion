"""M59 scene assertions — mirrors EditMode chrome/overflow tests without Unity."""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(msg):
    print("FAIL:", msg)
    sys.exit(1)


def ok(msg):
    print("OK:", msg)


def assert_inactive(yaml, name):
    m = re.search(
        rf"m_Name: {re.escape(name)}\r?\n(?:.*\r?\n){{0,16}}\s*m_IsActive: (\d)", yaml
    )
    if not m:
        fail(f"{name} GO not found")
    if m.group(1) != "0":
        fail(f"{name} still active")
    ok(f"{name} inactive")


def image_alphas(yaml, name):
    out = []
    idx = 0
    needle = f"m_Name: {name}\n"
    needle_cr = f"m_Name: {name}\r\n"
    while True:
        m = yaml.find(needle, idx)
        if m < 0:
            m = yaml.find(needle_cr, idx)
        if m < 0:
            break
        go = yaml.rfind("--- !u!1 &", 0, m)
        end = yaml.find("\n--- !u!", go + 5)
        go_block = yaml[go:end]
        for cm in re.finditer(r"component: \{fileID: (\d+)\}", go_block):
            cid = cm.group(1)
            s = yaml.find(f"--- !u!114 &{cid}\n")
            if s < 0:
                s = yaml.find(f"--- !u!114 &{cid}\r\n")
            if s < 0:
                continue
            e = yaml.find("\n--- !u!", s + 5)
            chunk = yaml[s:e]
            if "m_Sprite:" not in chunk or "m_Color:" not in chunk:
                continue
            am = re.search(
                r"m_Color: \{r: [-\d.]+, g: [-\d.]+, b: [-\d.]+, a: ([-\d.]+)\}", chunk
            )
            if am:
                out.append(float(am.group(1)))
                break
        idx = m + 1
    return out


def tmp_flags(yaml, name, ancestor):
    idx = 0
    while True:
        m = yaml.find(f"m_Name: {name}\n", idx)
        if m < 0:
            m = yaml.find(f"m_Name: {name}\r\n", idx)
        if m < 0:
            break
        go = yaml.rfind("--- !u!1 &", 0, m)
        end = yaml.find("\n--- !u!", go + 5)
        go_block = yaml[go:end]
        comps = re.findall(r"component: \{fileID: (\d+)\}", go_block)
        rect = None
        for c in comps:
            if yaml.find(f"--- !u!224 &{c}\n") >= 0 or yaml.find(f"--- !u!224 &{c}\r\n") >= 0:
                rect = c
                break
        if rect and has_ancestor(yaml, rect, ancestor):
            for c in comps:
                s = yaml.find(f"--- !u!114 &{c}\n")
                if s < 0:
                    s = yaml.find(f"--- !u!114 &{c}\r\n")
                if s < 0:
                    continue
                e = yaml.find("\n--- !u!", s + 5)
                chunk = yaml[s:e]
                if "m_overflowMode:" not in chunk:
                    continue
                auto = int(re.search(r"m_enableAutoSizing: (\d)", chunk).group(1))
                ov = int(re.search(r"m_overflowMode: (\d)", chunk).group(1))
                return auto, ov
        idx = m + 1
    return None


def has_ancestor(yaml, rect_id, ancestor):
    cur = rect_id
    for _ in range(24):
        s = yaml.find(f"--- !u!224 &{cur}\n")
        if s < 0:
            return False
        e = yaml.find("\n--- !u!", s + 5)
        chunk = yaml[s:e]
        # name of this rect's GO
        for mm in re.finditer(rf"- component: \{{fileID: {cur}\}}", yaml):
            g = yaml.rfind("--- !u!1 &", 0, mm.start())
            nm = re.search(r"m_Name: (.+)", yaml[g : g + 500])
            if nm and nm.group(1).strip() == ancestor:
                return True
            break
        fm = re.search(r"m_Father: \{fileID: (\d+)\}", chunk)
        if not fm or fm.group(1) == "0":
            return False
        cur = fm.group(1)
    return False


def main():
    bastion = (ROOT / "Assets/Scenes/Bastion.unity").read_text(encoding="utf-8")
    ship = (ROOT / "Assets/Scenes/Ship.unity").read_text(encoding="utf-8")

    if "m_Name: Pause Shortcuts" not in bastion:
        fail("Pause Shortcuts missing")
    m = re.search(r"HudController[\s\S]*?_pauseShortcuts: \{fileID: (\d+)\}", bastion)
    if not m or m.group(1) == "0":
        fail("pauseShortcuts unbound")
    ok(f"pauseShortcuts bound fileID={m.group(1)}")

    assert_inactive(ship, "Categories")
    assert_inactive(ship, "Preview Image")

    alphas = image_alphas(ship, "Background Blur")
    if len(alphas) != 4:
        fail(f"expected 4 Background Blur, got {len(alphas)}")
    for i, a in enumerate(alphas):
        if a < 0.65:
            fail(f"Background Blur[{i}] alpha={a}")
    ok(f"Background Blur alphas={alphas}")

    scrims = image_alphas(bastion, "Scrim")
    if not scrims or scrims[0] < 0.65:
        fail(f"Scrim alphas={scrims}")
    ok(f"Scrim alpha={scrims[0]}")

    # Profile Exp inactive
    m = re.search(r"m_Name: Profile Exp\r?\n(?:.*\r?\n){0,16}\s*m_IsActive: (\d)", bastion)
    if not m or m.group(1) != "0":
        fail(f"Bastion Profile Exp active={m.group(1) if m else None}")
    ok("Bastion Profile Exp inactive")

    for n in ["Score Salvage", "Wave Counter", "Weapon Name"]:
        flags = tmp_flags(bastion, n, "Canvas - Dynamic")
        if flags is None:
            fail(f"TMP {n} not found under Canvas - Dynamic")
        auto, ov = flags
        if ov in (1, 3):
            fail(f"{n} truncates overflow={ov}")
        if auto != 1 and ov != 0:
            # wrap not checked here; auto or wrap required — we set auto=1
            fail(f"{n} auto={auto} overflow={ov}")
        ok(f"Bastion {n} auto={auto} overflow={ov}")

    print("ALL ASSERTIONS PASSED")


if __name__ == "__main__":
    main()
