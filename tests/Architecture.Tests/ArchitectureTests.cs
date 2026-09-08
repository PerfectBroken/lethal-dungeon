using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LethalDungeon.Domain;
using NUnit.Framework;

namespace LethalDungeon.ArchitectureTests
{
    public class ArchitectureTests
    {
        private static string Root
        {
            get
            {
                var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                while (directory != null)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                        Directory.Exists(Path.Combine(directory.FullName, "src", "Domain")))
                        return directory.FullName;
                    directory = directory.Parent;
                }
                throw new InvalidOperationException("Cannot locate project root.");
            }
        }

        // HARNESS-001, DOM-002
        [Test]
        public void EveryProjectAndDomainModuleHasSpecification()
        {
            Assert.That(File.Exists(Path.Combine(Root, "README.md")), Is.True);
            foreach (string area in new[] { "src", "tests" })
            foreach (string project in Directory.GetFiles(Path.Combine(Root, area), "*.csproj", SearchOption.AllDirectories))
                Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(project)!, "README.md")), Is.True, project);

            foreach (string folder in Directory.GetDirectories(Path.Combine(Root, "src", "Domain")))
            {
                if (new[] { "bin", "obj" }.Contains(Path.GetFileName(folder))) continue;
                string readme = Path.Combine(folder, "README.md");
                Assert.That(File.Exists(readme), Is.True, folder);
                string text = File.ReadAllText(readme);
                foreach (string section in new[] { "必须遵守的规范", "接口与依赖", "测试与执行", "AI修改约束", "验证与已知限制" })
                    Assert.That(text, Does.Contain(section), readme);
            }
        }

        // HARNESS-002, DOM-001, DOM-003
        [Test]
        public void DomainProjectHasOnlyPortableStandardLibraryDependencies()
        {
            var project = XDocument.Load(Path.Combine(Root, "src", "Domain", "Domain.csproj"));
            Assert.That(project.Descendants("TargetFramework").Single().Value, Is.EqualTo("netstandard2.1"));
            Assert.That(project.Descendants("LangVersion").Single().Value, Is.EqualTo("9.0"));
            Assert.That(project.Descendants("PackageReference"), Is.Empty);
            Assert.That(project.Descendants("ProjectReference"), Is.Empty);
        }

        // HARNESS-003, DOM-001
        [Test]
        public void DomainAssemblyReferencesOnlyStandardAssemblies()
        {
            foreach (var reference in typeof(BackpackLoad).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name, Is.AnyOf("netstandard", "System.Runtime"), reference.FullName);
        }

        [Test]
        public void DomainDoesNotDirectlyUseEngineIoNetworkOrWallClock()
        {
            string[] prohibited = { @"\bUnityEngine\b", @"\bSystem\.IO\b", @"\bSystem\.Net\b",
                @"\bDateTime(?:Offset)?\s*\.\s*(?:Now|UtcNow|Today)\b", @"\bThread\s*\.\s*Sleep\b",
                @"\bTask\s*\.\s*Delay\b", @"\bEnvironment\s*\.\s*TickCount(?:64)?\b" };
            foreach (string file in Directory.GetFiles(Path.Combine(Root, "src", "Domain"), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("/obj/") || file.Contains("/bin/")) continue;
                string text = File.ReadAllText(file);
                foreach (string pattern in prohibited)
                    Assert.That(Regex.IsMatch(text, pattern), Is.False, $"{file}: {pattern}");
            }
        }

        // HARNESS-004: Traceability presence, not a substitute for assertion review.
        [TestCase("BackpackLoad", "BackpackLoadTests.cs", "LOAD-")]
        [TestCase("WorldClock", "WorldClockTests.cs", "CLOCK-")]
        [TestCase("DungeonLayout", "DungeonLayoutTests.cs|DungeonLoopTests.cs|DungeonBranchLoopTests.cs|DungeonSeedTests.cs|DungeonExplorationBranchTests.cs|DungeonSpatialTests.cs|ConfiguredDungeonTests.cs", "MAP-")]
        public void EveryRuleIdIsMappedInTests(string module, string testFile, string prefix)
        {
            string spec = File.ReadAllText(Path.Combine(Root, "src", "Domain", module, "README.md"));
            string tests = string.Join("\n", testFile.Split('|').Select(file => File.ReadAllText(Path.Combine(Root, "tests", "Domain.Tests", file))));
            var ids = Regex.Matches(spec, prefix + @"\d{3}").Cast<Match>().Select(m => m.Value).Distinct().ToArray();
            Assert.That(ids, Is.Not.Empty);
            foreach (string id in ids) Assert.That(tests, Does.Contain(id));
        }
    }
}
