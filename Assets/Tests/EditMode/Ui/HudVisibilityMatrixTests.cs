using System.Reflection;
using GatekeepersProtocol.Game;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GatekeepersProtocol.Tests
{
    /// <summary>The HUD's visibility matrix in one place: which combination of chrome is up
    /// in each mode, driven through BastionHudView's public mode calls on a reflection-bound
    /// rig. The load-bearing rule: the protocol bar and line belong to the fight chrome — a
    /// loadout with no protocol changes their CONTENT ("NO PROTOCOL", em-dash slots), never
    /// their visibility. The rig lives under an inactive root so no Awake fires; activeSelf
    /// still records every toggle.</summary>
    public class HudVisibilityMatrixTests
    {
        GameObject _rig;
        BastionHudView _view;

        GameObject _profile, _score, _vitals, _weapon, _hatch, _crosshair, _prompt;
        GameObject _prep, _assault, _shortcut, _leftDrawer, _rightDrawer, _emergency, _endRoot;
        GameObject _bar;
        TMP_Text _protocolLine;
        Slot _passive, _personal, _crew;

        sealed class Slot
        {
            public GameObject Root;
            public TMP_Text Key;
            public TMP_Text Name;
            public Image Fill;
            public TMP_Text Countdown;
        }

        [SetUp]
        public void BuildRig()
        {
            _rig = new GameObject("HudRig");
            _rig.SetActive(false);
            _view = _rig.AddComponent<BastionHudView>();

            _profile = Bind("_profileRoot");
            _score = Bind("_scoreRoot");
            _vitals = Bind("_vitalsRoot");
            _weapon = Bind("_weaponCardRoot");
            _hatch = Bind("_hatchRoot");
            _crosshair = Bind("_crosshairRoot");
            _prompt = Bind("_promptRoot");
            _prep = Bind("_prepRoot");
            _assault = Bind("_assaultRoot");
            _shortcut = Bind("_shortcutRoot");
            _leftDrawer = Bind("_leftDrawerRoot");
            _rightDrawer = Bind("_rightDrawerRoot");
            _emergency = Bind("_emergencyRoot");
            Bind("_vignetteRoot");
            Bind("_msgGeneralRoot");
            Bind("_msgPlayerRoot");

            _protocolLine = Text("ProtocolLine");
            Set(_view, "_protocolLine", _protocolLine);

            var barGo = Child("ProtocolBar");
            var bar = barGo.AddComponent<ProtocolAbilityBarView>();
            _bar = barGo;
            _passive = MakeSlot("Passive");
            _personal = MakeSlot("Personal");
            _crew = MakeSlot("Crew");
            Set(bar, "_passive", Binding(_passive));
            Set(bar, "_personal", Binding(_personal));
            Set(bar, "_crew", Binding(_crew));
            Set(_view, "_protocolBar", bar);

            var endGo = Child("EndScreen");
            var end = endGo.AddComponent<EndScreenView>();
            _endRoot = Child("EndRoot");
            Set(end, "_root", _endRoot);
            Set(end, "_returnButton", Child("Return").AddComponent<Button>());
            Set(_view, "_endScreen", end);
        }

        [TearDown]
        public void TearDownRig()
        {
            if (_rig != null)
                Object.DestroyImmediate(_rig);
        }

        [Test]
        public void ShowFight_RaisesEveryCornerCard_TheProtocolChrome_AndTheCrosshair()
        {
            _view.ShowFight(prep: false);

            Assert.IsTrue(_profile.activeSelf, "profile card");
            Assert.IsTrue(_score.activeSelf, "score card");
            Assert.IsTrue(_vitals.activeSelf, "vitals card");
            Assert.IsTrue(_weapon.activeSelf, "weapon card");
            Assert.IsTrue(_hatch.activeSelf, "hatch card");
            Assert.IsTrue(_bar.activeSelf, "protocol bar rises with the fight chrome");
            Assert.IsTrue(_crosshair.activeSelf, "crosshair");
            Assert.IsTrue(_protocolLine.gameObject.activeSelf, "protocol line");
            Assert.AreEqual("NO PROTOCOL", _protocolLine.text,
                "an empty loadout reads NO PROTOCOL — it does not hide the line");
            Assert.IsFalse(_endRoot.activeSelf, "end screen");
            Assert.IsFalse(_emergency.activeSelf, "emergency");
            Assert.IsFalse(_prep.activeSelf, "prep banner is the controller's push, not ShowFight's");
            Assert.IsFalse(_assault.activeSelf, "assault banner");
        }

        [Test]
        public void HideChrome_DropsEverything()
        {
            _view.ShowFight(prep: false);
            _view.SetLeftDrawerOpen(true);
            _view.HideChrome();

            Assert.IsFalse(_profile.activeSelf, "profile card");
            Assert.IsFalse(_score.activeSelf, "score card");
            Assert.IsFalse(_vitals.activeSelf, "vitals card");
            Assert.IsFalse(_weapon.activeSelf, "weapon card");
            Assert.IsFalse(_hatch.activeSelf, "hatch card");
            Assert.IsFalse(_bar.activeSelf, "protocol bar");
            Assert.IsFalse(_crosshair.activeSelf, "crosshair");
            Assert.IsFalse(_prompt.activeSelf, "prompt");
            Assert.IsFalse(_prep.activeSelf, "prep banner");
            Assert.IsFalse(_assault.activeSelf, "assault banner");
            Assert.IsFalse(_shortcut.activeSelf, "shortcut panel");
            Assert.IsFalse(_leftDrawer.activeSelf, "left drawer");
            Assert.IsFalse(_rightDrawer.activeSelf, "right drawer");
            Assert.IsFalse(_emergency.activeSelf, "emergency");
            Assert.IsFalse(_endRoot.activeSelf, "end screen");
        }

        [Test]
        public void ProtocolBar_VisibilityIsTheFightChrome_NeverTheLoadout()
        {
            _view.ShowFight(prep: false);
            Assert.IsTrue(_bar.activeSelf);

            // No protocol equipped: placeholders arrive, the bar stays.
            _view.SetProtocolLine("NO PROTOCOL");
            _view.SetProtocolAbilities(
                "—", 0f, "",
                "Q", "—", 0f, "", false,
                "E", "—", 0f, "", false);
            Assert.IsTrue(_bar.activeSelf, "an empty loadout must not hide the bar");
            Assert.IsTrue(_protocolLine.gameObject.activeSelf, "or the line");

            // Protocol equipped: content changes, visibility does not.
            _view.SetProtocolLine("VEIL");
            _view.SetProtocolAbilities(
                "SILENT RUNNING", 0f, "",
                "Q", "CLOAK", 0.5f, "4", false,
                "E", "GHOST", 0f, "", false);
            Assert.IsTrue(_bar.activeSelf);
            Assert.AreEqual("VEIL", _protocolLine.text);
        }

        [Test]
        public void ShowEnd_SwapsTheChromeForTheEndScreen_WithTheDrawerOpen()
        {
            _view.ShowFight(prep: false);
            _view.ShowEnd();

            Assert.IsTrue(_endRoot.activeSelf, "end screen");
            Assert.IsTrue(_leftDrawer.activeSelf, "left drawer opens on the end screen");
            Assert.IsTrue(_shortcut.activeSelf, "shortcut panel rides the drawer");
            Assert.IsFalse(_bar.activeSelf, "fight chrome is down");
            Assert.IsFalse(_vitals.activeSelf, "fight chrome is down");
            Assert.IsFalse(_crosshair.activeSelf, "crosshair is down");
        }

        [Test]
        public void ShowEmergency_RaisesTheEmergencyPanel_WithTheDrawerOpen()
        {
            _view.ShowFight(prep: false);
            _view.ShowEmergency();

            Assert.IsTrue(_emergency.activeSelf, "emergency panel");
            Assert.IsTrue(_leftDrawer.activeSelf, "left drawer");
            Assert.IsFalse(_bar.activeSelf, "fight chrome is down");
            Assert.IsFalse(_endRoot.activeSelf, "end screen stays down");
        }

        [Test]
        public void LeftDrawer_CarriesTheShortcutPanelBothWays()
        {
            _view.SetLeftDrawerOpen(true);
            Assert.IsTrue(_leftDrawer.activeSelf);
            Assert.IsTrue(_shortcut.activeSelf);

            _view.SetLeftDrawerOpen(false);
            Assert.IsFalse(_leftDrawer.activeSelf);
            Assert.IsFalse(_shortcut.activeSelf);
        }

        [Test]
        public void ProtocolSlots_KeycapHidesWhenEmpty_NameHoldsWhileTheAbilityRuns()
        {
            _view.SetProtocolAbilities(
                "SILENT RUNNING", 0f, "",
                "Q", "CLOAK", 0.5f, "4", false,
                "E", "GHOST", 0.2f, "9", true);

            Assert.IsFalse(_passive.Key.gameObject.activeSelf, "passive has no keycap");
            Assert.IsTrue(_passive.Name.gameObject.activeSelf, "passive name shows when idle");
            Assert.IsFalse(_passive.Fill.gameObject.activeSelf, "no countdown, no fill");

            Assert.IsTrue(_personal.Key.gameObject.activeSelf, "personal keycap");
            Assert.IsFalse(_personal.Name.gameObject.activeSelf,
                "cooling drops the name — that is the tell it is not running");
            Assert.IsTrue(_personal.Fill.gameObject.activeSelf, "cooling shows the fill");
            Assert.IsTrue(_personal.Countdown.gameObject.activeSelf, "cooling shows the countdown");

            Assert.IsTrue(_crew.Name.gameObject.activeSelf,
                "the name stays up while the ability runs");
            Assert.IsTrue(_crew.Fill.gameObject.activeSelf, "running burns the fill hot");
        }

        // ---- rig plumbing ----

        GameObject Bind(string field)
        {
            GameObject go = Child(field.TrimStart('_'));
            Set(_view, field, go);
            return go;
        }

        GameObject Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rig.transform, false);
            return go;
        }

        TMP_Text Text(string name)
        {
            return Child(name).AddComponent<TextMeshProUGUI>();
        }

        Slot MakeSlot(string name)
        {
            return new Slot
            {
                Root = Child(name + "Root"),
                Key = Text(name + "Key"),
                Name = Text(name + "Name"),
                Fill = Child(name + "Fill").AddComponent<Image>(),
                Countdown = Text(name + "Countdown"),
            };
        }

        static AbilitySlotBinding Binding(Slot slot)
        {
            return new AbilitySlotBinding
            {
                root = slot.Root,
                key = slot.Key,
                name = slot.Name,
                cooldownFill = slot.Fill,
                countdown = slot.Countdown,
            };
        }

        static void Set(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(
                field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, target.GetType().Name + " has no field " + field
                + " — the rig no longer matches the view");
            f.SetValue(target, value);
        }
    }
}
