using System;
using System.IO;
using System.Linq;
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
            while (dir != null && !IsGitRoot(dir.FullName))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new InvalidOperationException("Repository root containing a .git directory or worktree file could not be located.");
            }

            return dir.FullName;
        }

        private static bool IsGitRoot(string directory)
        {
            string gitPath = Path.Combine(directory, ".git");
            // Git worktrees use a .git file that points at the main repository.
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

