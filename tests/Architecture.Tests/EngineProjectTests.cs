using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace LethalDungeon.ArchitectureTests
{
    public class EngineProjectTests
    {
        private static string Read(string path)
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
            Assert.That(root, Is.Not.Null);
            string file = Path.Combine(root!.FullName, "engine", "Validation", path);
            Assert.That(File.Exists(file), Is.True, "Required engine configuration: " + path);
            return File.ReadAllText(file);
        }

        // ENGINE-001
        [Test]
        public void EditorVersionIsPinnedToTheVerifiedDownloadMetadata()
        {
            string version = Read("ProjectSettings/ProjectVersion.txt");
            Assert.That(version, Does.Contain("m_EditorVersion: 2022.3.62t14"));
            Assert.That(version, Does.Contain("2022.3.62t14 (1f04f7aba499)"));
        }

        // ENGINE-002
        [Test]
        public void EnginePackagesHaveFixedVersions()
        {
            using var manifest = JsonDocument.Parse(Read("Packages/manifest.json"));
            var dependencies = manifest.RootElement.GetProperty("dependencies");
            Assert.That(dependencies.GetProperty("com.unity.test-framework").GetString(), Is.EqualTo("1.1.33"));
            Assert.That(dependencies.GetProperty("com.qq.weixin.minigame").GetString(), Is.EqualTo(
                "https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk.git#d288776c50578926c732496882bd6ab6684c778c"));
        }

        // ENGINE-003/004: Static packaging guard, not an editor-runtime result.
        [Test]
        public void ImportTestsAreEditorOnlyAndReferenceOnlyTheRequiredPrecompiledAssemblies()
        {
            using var document = JsonDocument.Parse(Read("Assets/Tests/LethalDungeon.ImportTests.asmdef"));
            var definition = document.RootElement;
            Assert.That(definition.GetProperty("includePlatforms").EnumerateArray().Select(x => x.GetString()).ToArray(), Is.EqualTo(new[] { "Editor" }));
            Assert.That(definition.GetProperty("overrideReferences").GetBoolean(), Is.True);
            Assert.That(definition.GetProperty("precompiledReferences").EnumerateArray().Select(x => x.GetString()).ToArray(), Is.EquivalentTo(new[] { "nunit.framework.dll", "LethalDungeon.Domain.dll" }));
            Assert.That(definition.GetProperty("autoReferenced").GetBoolean(), Is.False);
        }
    }
}
