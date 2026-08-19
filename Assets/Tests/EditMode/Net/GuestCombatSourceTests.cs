using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Source pins for guest combat: FireRay must RPC from NetworkManager, not
    /// GameController.Net; replicated death must not say unknown + 0.</summary>
    public class GuestCombatSourceTests
    {
        [Test]
        public void FireRay_ClientHit_UsesNetworkManagerNotGameNet()
        {
            string src = File.ReadAllText(Path.GetFullPath("Assets/Scripts/Game/Player/PlayerGun.Firing.cs"));
            StringAssert.Contains("NetworkManager.Singleton", src);
            StringAssert.Contains("SubmitHitServerRpc", src);
            StringAssert.Contains("nm.IsClient && !nm.IsServer", src);
            Assert.IsFalse(src.Contains("_game.Net.IsClientOnly"),
                "guest hit RPC must not require GameController.Net");
        }

        [Test]
        public void DeathLine_ReplicatedHp_UsesMirrorAliveAndGatekeeper()
        {
            string src = File.ReadAllText(Path.GetFullPath("Assets/Scripts/Game/Player/PlayerHealth.cs"));
            StringAssert.Contains("killer = \"gatekeeper\"", src);
            StringAssert.Contains("Alive.Value", src);
            StringAssert.Contains("IsClientOnly", src);
            StringAssert.Contains("NotifyHitFlash", File.ReadAllText(
                Path.GetFullPath("Assets/Scripts/Game/Combat/Damageable.cs")));
        }
    }
}
