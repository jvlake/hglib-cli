using System.Globalization;
using NUnit.Framework;

namespace Mercurial.Tests.Hooks
{
    [TestFixture]
    [Category("Integration")]
    [Ignore("Failed to pass from original repo")]
    public class IncomingHookTests : DualRepositoryTestsBase
    {
        [Test]
        public void Pull_NoChangesets_DoesNotInvokeIncomingHook()
        {
            Repo1.Init();
            Repo2.Clone(Repo1.Path);

            WriteTextFileAndCommit(Repo1, "test.txt", "dummy", "dummy", true);
            Repo2.Pull(Repo1.Path);

            Repo2.SetHook("incoming");

            var command = new CustomCommand("pull")
                .WithAdditionalArgument(string.Format(CultureInfo.InvariantCulture, "\"{0}\"", Repo1.Path));
            Repo2.Execute(command);

            Assert.That(command.RawExitCode, Is.EqualTo(0));
            Assert.That(command.RawStandardOutput, Is.Not.StringContaining("Revision:"));
            Assert.That(command.RawStandardOutput, Is.Not.StringContaining("Url:"));
            Assert.That(command.RawStandardOutput, Is.Not.StringContaining("Source:"));
        }

        [Test]
        public void Pull_OneChangeset_InvokesIncomingHookOnce()
        {
            Repo1.Init();
            Repo2.Clone(Repo1.Path);

            WriteTextFileAndCommit(Repo1, "test.txt", "dummy", "dummy", true);
            Repo2.SetHook("incoming");

            var command = new CustomCommand("pull")
                .WithAdditionalArgument(string.Format(CultureInfo.InvariantCulture, "\"{0}\"", Repo1.Path));
            Repo2.Execute(command);

            Assert.That(command.RawExitCode, Is.EqualTo(0));
            Assert.That(command.RawStandardOutput.Count("Revision:"), Is.EqualTo(1));
            Assert.That(command.RawStandardOutput.Count("Url:"), Is.EqualTo(1));
            Assert.That(command.RawStandardOutput.Count("Source:"), Is.EqualTo(1));
        }

        [Test]
        public void Pull_TwoChangesets_InvokesIncomingHookTwice()
        {
            Repo1.Init();
            Repo2.Clone(Repo1.Path);

            WriteTextFileAndCommit(Repo1, "test.txt", "dummy1", "dummy1", true);
            WriteTextFileAndCommit(Repo1, "test.txt", "dummy2", "dummy2", true);
            Repo2.SetHook("incoming");

            var command = new CustomCommand("pull")
                .WithAdditionalArgument(string.Format(CultureInfo.InvariantCulture, "\"{0}\"", Repo1.Path));
            Repo2.Execute(command);

            Assert.That(command.RawExitCode, Is.EqualTo(0));
            Assert.That(command.RawStandardOutput.Count("Revision:"), Is.EqualTo(2));
            Assert.That(command.RawStandardOutput.Count("Url:"), Is.EqualTo(2));
            Assert.That(command.RawStandardOutput.Count("Source:"), Is.EqualTo(2));
        }
    }
}
