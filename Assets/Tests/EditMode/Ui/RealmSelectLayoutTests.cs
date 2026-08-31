using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Every widget on the chooser has to be on the screen.
    ///
    /// This exists because it caught nothing and a player did. The open-to-friends panel was
    /// authored with positive y on labels that anchor to the TOP of the canvas, where y counts
    /// downward — so the join code, its heading and the Relay status line were all placed above
    /// the top edge. The buttons anchor to the bottom and drew correctly, which made the panel
    /// look like it had simply failed rather than like it was mispositioned, and hid the very
    /// status line that would have said what went wrong.
    ///
    /// Structural, so it runs offline in a second. It cannot tell you the panel is ugly; it can
    /// tell you the panel is not where anyone can see it.</summary>
    public class RealmSelectLayoutTests
    {
        const string Scene = "Assets/Scenes/RealmSelect.unity";
        const string Canvas = "Canvas";
        const string Panel = "Session Panel";

        // The chooser's own reference resolution.
        const float W = 1920f;
        const float H = 1280f;

        static void AssertOnScreen(SceneHudYaml scene, string widget, string ancestor)
        {
            Aabb box = scene.CanvasAabb(widget, ancestor, W, H);
            Assert.IsTrue(box.X0 >= -1f && box.X1 <= W + 1f && box.Y0 >= -1f && box.Y1 <= H + 1f,
                widget + " is off the screen at " + box + " (screen is 0,0 to " + W + "," + H + ")");
        }

        [Test]
        public void TheOpenToFriendsPanel_IsOnTheScreen()
        {
            var scene = SceneHudYaml.Load(Scene);
            // The code first: it is the only thing a friend actually needs.
            AssertOnScreen(scene, "Session Code", Panel);
            AssertOnScreen(scene, "Session Head", Panel);
            // The status line carries the Relay refusal. Off-screen, hosting fails silently.
            AssertOnScreen(scene, "Session Status", Panel);
            AssertOnScreen(scene, "COPYButton", Panel);
            AssertOnScreen(scene, "TOSHIPButton", Panel);
            AssertOnScreen(scene, "HOSTONLANButton", Panel);
            AssertOnScreen(scene, "CLOSEButton", Panel);
        }

        /// <summary>Bottom-anchored buttons at y≈600 once sat on top of the top-anchored join
        /// code. On-screen is not enough — the widgets must not share pixels.</summary>
        [Test]
        public void TheOpenToFriendsPanel_CodeDoesNotCoverTheButtons()
        {
            var scene = SceneHudYaml.Load(Scene);
            Aabb code = scene.CanvasAabb("Session Code", Panel, W, H);
            Aabb copy = scene.CanvasAabb("COPYButton", Panel, W, H);
            Aabb ship = scene.CanvasAabb("TOSHIPButton", Panel, W, H);
            Assert.IsFalse(Overlaps(code, copy),
                "Session Code overlaps COPY at " + code + " / " + copy);
            Assert.IsFalse(Overlaps(code, ship),
                "Session Code overlaps TO SHIP at " + code + " / " + ship);
        }

        static bool Overlaps(Aabb a, Aabb b)
        {
            return a.X0 < b.X1 && a.X1 > b.X0 && a.Y0 < b.Y1 && a.Y1 > b.Y0;
        }

        [Test]
        public void TheChooserItself_IsOnTheScreen()
        {
            var scene = SceneHudYaml.Load(Scene);
            AssertOnScreen(scene, "REALMS", Canvas);
            AssertOnScreen(scene, "REALM", Canvas);
            AssertOnScreen(scene, "BOARDER", Canvas);
            AssertOnScreen(scene, "NameField", Canvas);
            AssertOnScreen(scene, "BoarderNameField", Canvas);
            AssertOnScreen(scene, "NEWREALMButton", Canvas);
            AssertOnScreen(scene, "NEWBOARDERButton", Canvas);
            AssertOnScreen(scene, "DELETEREALMButton", Canvas);
            AssertOnScreen(scene, "DELETEBOARDERButton", Canvas);
            AssertOnScreen(scene, "ENTERButton", Canvas);
            AssertOnScreen(scene, "OPENTOFRIENDSButton", Canvas);
            AssertOnScreen(scene, "BACKButton", Canvas);
            AssertOnScreen(scene, "DEMOButton", Canvas);
        }

        /// <summary>One BACK per screen. The panel's own dismiss is CLOSE, because two buttons
        /// reading BACK on one screen is a guess for the player and an ambiguous lookup for
        /// every test that addresses widgets by name.</summary>
        [Test]
        public void ThePanelDismiss_IsNotASecondBack()
        {
            var scene = SceneHudYaml.Load(Scene);
            Assert.IsTrue(scene.Has("CLOSEButton", Panel), "the panel dismisses with CLOSE");
            Assert.IsFalse(scene.Has("BACKButton", Panel), "and BACK belongs to the chooser");
        }
    }
}
