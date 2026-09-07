using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LethalDungeon.ArchitectureTests
{
    public class CiWorkflowTests
    {
        private static string Workflow()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                directory = directory.Parent;
            Assert.That(directory, Is.Not.Null, "Repository root must be available.");
            string path = Path.Combine(directory!.FullName, ".github", "workflows", "ci.yml");
            Assert.That(File.Exists(path), Is.True, "CI workflow must exist before it can enforce rules.");
            return File.ReadAllText(path);
        }

        // CI-001
        [Test]
        public void WorkflowRunsForPushPullRequestAndManualDispatchWithReadOnlyAccess()
        {
            string yaml = Workflow();
            foreach (string trigger in new[] { "push", "pull_request", "workflow_dispatch" })
                Assert.That(Regex.IsMatch(yaml, @"(?m)^  " + trigger + @":\s*$"), Is.True, trigger);
            Assert.That(yaml, Does.Contain("contents: read"));
            Assert.That(yaml, Does.Not.Contain("write-all"));
            Assert.That(yaml, Does.Not.Contain("secrets."));
        }

        // CI-002
        [Test]
        public void WorkflowBuildsAndExecutesBothSuitesWithPinnedSdkAndLockedRestore()
        {
            string yaml = Workflow();
            foreach (string command in new[] {
                "global-json-file: global.json", "dotnet restore LethalDungeon.sln --locked-mode",
                "dotnet build LethalDungeon.sln --configuration Release --no-restore",
                "dotnet test tests/Architecture.Tests/Architecture.Tests.csproj",
                "dotnet test tests/Domain.Tests/Domain.Tests.csproj" })
                Assert.That(yaml, Does.Contain(command));
            Assert.That(yaml, Does.Not.Contain("--filter"), "CI must execute the full suites.");
        }

        // CI-003
        [Test]
        public void WorkflowPreservesFailureAndUploadsResultsEvenWhenTestsFail()
        {
            string yaml = Workflow();
            Assert.That(yaml, Does.Not.Contain("continue-on-error"));
            Assert.That(yaml, Does.Not.Contain("|| true"));
            Assert.That(yaml, Does.Contain("if: always()"));
            Assert.That(yaml, Does.Contain("retention-days: 14"));
            Assert.That(yaml, Does.Contain("evidence/latest/**/*.trx"));
        }

        // CI-004
        [Test]
        public void WorkflowPinsOfficialActionsAndBoundsExecution()
        {
            string yaml = Workflow();
            var references = Regex.Matches(yaml, @"uses:\s*(\S+)").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
            Assert.That(references.Length, Is.EqualTo(3));
            foreach (string reference in references)
                Assert.That(Regex.IsMatch(reference, @"^actions/(checkout|setup-dotnet|upload-artifact)@[0-9a-f]{40}$"), Is.True);
            Assert.That(yaml, Does.Contain("persist-credentials: false"));
            Assert.That(yaml, Does.Contain("timeout-minutes: 15"));
        }
    }
}
