using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using GatekeepersProtocol.Sim;

namespace GatekeepersProtocol.Tests
{
    public class ProductVersionTests
    {
        [Test]
        public void Stamp_Release034_BuildAtLeastOne_DisplayMatches()
        {
            Assert.AreEqual("0.34", ProductVersion.Release);
            Assert.GreaterOrEqual(ProductVersion.Build, 1);
            Assert.AreEqual("0.34." + ProductVersion.Build, ProductVersion.Display);
        }

        [Test]
        public void ProjectSettings_BundleVersionMatchesRelease()
        {
            string yaml = File.ReadAllText(Path.GetFullPath("ProjectSettings/ProjectSettings.asset"));
            Assert.IsTrue(
                Regex.IsMatch(yaml, @"(?m)^  bundleVersion: " + Regex.Escape(ProductVersion.Release) + @"\s*$"),
                "ProjectSettings.asset bundleVersion must match ProductVersion.Release");
        }
    }
}
