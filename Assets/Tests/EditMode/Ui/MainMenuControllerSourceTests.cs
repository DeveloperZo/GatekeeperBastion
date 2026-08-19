using System.IO;
using NUnit.Framework;

namespace GatekeepersProtocol.Tests
{
    /// <summary>Join Connect with this machine's own Relay code must host, not
    /// <c>TryJoin</c> the allocation. Source pin — no running Relay.</summary>
    public class MainMenuControllerSourceTests
    {
        [Test]
        public void OnJoinConnect_OwnCodeHostPath_PresentInSource()
        {
            string path = Path.GetFullPath("Assets/Scripts/Game/Flow/MainMenuController.cs");
            Assert.IsTrue(File.Exists(path), path);
            string src = File.ReadAllText(path);
            StringAssert.Contains("join is this host's code — starting host", src);
            StringAssert.Contains("HasPayload(_pendingRelay)", src);
            StringAssert.Contains("EnterSessionHost(relay: true)", src);
        }
    }
}
