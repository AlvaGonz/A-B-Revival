using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

namespace ABRevival.Tests
{
    [TestFixture]
    public class ProjectConfigurationTests
    {
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (IsGitRoot(dir))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Repository root containing a .git file or directory could not be located.");
        }

        private static bool IsGitRoot(DirectoryInfo dir)
        {
            // Worktrees use a .git file instead of a directory.
            string gitPath = Path.Combine(dir.FullName, ".git");
            return Directory.Exists(gitPath) || File.Exists(gitPath);
        }

        private static string GetCoreCsprojPath()
        {
            return Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Scripts", "ABRevival.Core.csproj");
        }

        private static System.Collections.Generic.List<string> GetCompileRemoves(XDocument doc)
        {
            return doc.Descendants("Compile")
                .Select(e => e.Attribute("Remove")?.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v.Replace('/', '\\'))
                .ToList();
        }

        private static string ReadFirstValueAfterKey(string path, string key)
        {
            string prefix = key + ":";
            foreach (string line in File.ReadLines(path))
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return trimmed.Substring(prefix.Length).Trim();
                }
            }

            return null;
        }

        private static void AssertYamlFloat(string assetPath, string key, float expected, float tolerance)
        {
            string raw = ReadFirstValueAfterKey(assetPath, key);
            Assert.IsNotNull(raw, $"{key} not found in {assetPath}");
            float actual = float.Parse(raw, CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, actual, tolerance, $"{key} in {assetPath}");
        }

        [Test]
        public void TimeManager_FixedTimestep_IsSixtyHertz()
        {
            string path = Path.Combine(FindRepoRoot(), "A&B-U", "ProjectSettings", "TimeManager.asset");
            Assert.IsTrue(File.Exists(path), $"TimeManager.asset not found at {path}");

            string raw = ReadFirstValueAfterKey(path, "Fixed Timestep");
            Assert.IsNotNull(raw, "Fixed Timestep not found in TimeManager.asset");
            double actual = double.Parse(raw, CultureInfo.InvariantCulture);
            Assert.AreEqual(0.0166667, actual, 1e-9,
                "Fixed Timestep must be 1/60 s (0.0166667) per ADR 0013 and ADR 0017.");
        }

        [Test]
        public void PhysicsCoreSettings2D_IsAssignedWithPixelWorldUnits()
        {
            string root = FindRepoRoot();
            string projectSettingsPath = Path.Combine(root, "A&B-U", "ProjectSettings", "PhysicsCoreProjectSettings2D.asset");
            string assetPath = Path.Combine(root, "A&B-U", "Assets", "Settings", "PhysicsCoreSettings2D.asset");
            string metaPath = assetPath + ".meta";

            Assert.IsTrue(File.Exists(projectSettingsPath), $"Project settings asset not found at {projectSettingsPath}");
            Assert.IsTrue(File.Exists(assetPath), $"PhysicsCoreSettings2D asset not found at {assetPath}");
            Assert.IsTrue(File.Exists(metaPath), $"PhysicsCoreSettings2D meta not found at {metaPath}");

            string assignment = File.ReadLines(projectSettingsPath)
                .FirstOrDefault(line => line.Contains("m_PhysicsCoreSettings:"));
            Assert.IsNotNull(assignment, "m_PhysicsCoreSettings is not serialized in PhysicsCoreProjectSettings2D.asset.");
            StringAssert.DoesNotContain("fileID: 0", assignment,
                "PhysicsCoreProjectSettings2D.asset must reference the PhysicsCoreSettings2D asset.");

            Match assignedGuid = Regex.Match(assignment, @"guid:\s*([0-9a-f]{32})");
            Assert.IsTrue(assignedGuid.Success, $"No asset guid in assignment: {assignment}");
            Assert.AreEqual(ReadFirstValueAfterKey(metaPath, "guid"), assignedGuid.Groups[1].Value,
                "PhysicsCoreProjectSettings2D.asset references a different asset than Assets/Settings/PhysicsCoreSettings2D.asset.");

            AssertYamlFloat(assetPath, "m_LengthUnitsPerMeter", 100f, 0.0001f);
            AssertYamlFloat(assetPath, "m_MaximumLinearSpeed", 40000f, 0.001f);
            AssertYamlFloat(assetPath, "m_BounceThreshold", 100f, 0.001f);
            AssertYamlFloat(assetPath, "m_ContactHitEventThreshold", 100f, 0.001f);
            AssertYamlFloat(assetPath, "m_ContactSpeed", 300f, 0.001f);
            AssertYamlFloat(assetPath, "m_ContactRecycleDistance", 5f, 0.001f);
            AssertYamlFloat(assetPath, "m_SleepThreshold", 5f, 0.001f);

            string gravity = ReadFirstValueAfterKey(assetPath, "m_Gravity");
            Assert.IsNotNull(gravity, "m_Gravity not found in PhysicsCoreSettings2D.asset");
            Match gravityY = Regex.Match(gravity, @"y:\s*(-?[0-9.]+)");
            Assert.IsTrue(gravityY.Success, $"Could not parse gravity from {gravity}");
            Assert.AreEqual(-981f, float.Parse(gravityY.Groups[1].Value, CultureInfo.InvariantCulture), 0.001f,
                "Gravity must be -981 px/s^2 at lengthUnitsPerMeter 100.");
        }

        [Test]
        public void ABRevivalCoreCsproj_Exists_AtExpectedLocation()
        {
            string csprojPath = GetCoreCsprojPath();
            Assert.IsTrue(File.Exists(csprojPath), $"ABRevival.Core.csproj not found at {csprojPath}");
        }

        [Test]
        public void ABRevivalCoreCsproj_ExplicitlyExcludes_EditorFolder()
        {
            string csprojPath = GetCoreCsprojPath();
            Assert.IsTrue(File.Exists(csprojPath), $"Csproj file missing: {csprojPath}");

            XDocument doc = XDocument.Load(csprojPath);
            var compileRemoves = GetCompileRemoves(doc);

            bool editorExcluded = compileRemoves.Any(v => v.Equals(@"Editor\**", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(
                editorExcluded,
                "ABRevival.Core.csproj MUST contain <Compile Remove=\"Editor\\**\" /> to prevent UnityEditor compile errors in standalone .NET test runner."
            );
        }

        [Test]
        public void ABRevivalCoreCsproj_ExplicitlyExcludes_TestsFolder()
        {
            string csprojPath = GetCoreCsprojPath();
            Assert.IsTrue(File.Exists(csprojPath), $"Csproj file missing: {csprojPath}");

            XDocument doc = XDocument.Load(csprojPath);
            var compileRemoves = GetCompileRemoves(doc);

            bool testsExcluded = compileRemoves.Any(v => v.Equals(@"Tests\**", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(
                testsExcluded,
                "ABRevival.Core.csproj MUST contain <Compile Remove=\"Tests\\**\" /> to prevent circular test dependencies in core runtime."
            );
        }
    }
}

