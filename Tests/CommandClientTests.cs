// 
//  CommandClientTests.cs
//  
//  Author:
//       Levi Bard <levi@unity3d.com>
//  
//  Copyright (c) 2011 Levi Bard
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using NUnit.Framework;

using Mercurial;

namespace Mercurial.Tests
{
	[TestFixture()]
	public class CommandClientTests
	{
		static readonly string TestRepo = "https://bitbucket.org/TakUnity/monodevelop-hg";
		static List<string> garbage = new List<string> ();
		static readonly string MercurialPath = "hg";
		
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);
		
		// From Managed.Windows.Forms/XplatUI
		static bool IsRunningOnMac ()
		{
			if (IsRunningOnWindows ()) return false;
			
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					return (os == "Darwin");
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
			
			return false;
		}
		
		static bool IsRunningOnWindows ()
		{
			return (Path.DirectorySeparatorChar == '\\' && Environment.NewLine == "\r\n");
		}
		
		[SetUp]
		public void Setup ()
		{
		}
		
		[TearDown]
		public void Teardown ()
		{
			foreach (string garbageDir in garbage) {
				try {
					Directory.Delete (garbageDir, true);
				} catch {
					// Don't care
				}
			}
		}
		
		[Test]
		public void TestConnection ()
		{
			string path = GetTemporaryPath ();
			CommandClient.Initialize (path, MercurialPath);
		}
		
		[Test]
		public void TestConfiguration ()
		{
			string path = GetTemporaryPath ();
			CommandClient.Initialize (path, MercurialPath);
			
			using (CommandClient client = new CommandClient (path, null, null, MercurialPath)) {
				IDictionary<string,string > config = client.Configuration;
				Assert.IsNotNull (config);
				Assert.Greater (config.Count, 0, "Expecting nonempty configuration");
				// Console.WriteLine (config.Aggregate (new StringBuilder (), (s,pair) => s.AppendFormat ("{0} = {1}\n", pair.Key, pair.Value), s => s.ToString ()));
			}
		}
		
		[Test]
		public void TestInitialize ()
		{
			string path = GetTemporaryPath ();
			CommandClient.Initialize (path, MercurialPath);
			Assert.That (Directory.Exists (Path.Combine (path, ".hg")), string.Format ("Repository was not created at {0}", path));
		}
		
		[Test]
		public void TestRoot ()
		{
			string path = GetTemporaryPath ();
			CommandClient.Initialize (path, MercurialPath);
			Assert.That (Directory.Exists (Path.Combine (path, ".hg")), string.Format ("Repository was not created at {0}", path));
			
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				Assert.AreEqual (path, client.Root, "Unexpected repository root");
			}
		}
		
		[Test]
		[Ignore("Don't thrash bitbucket")]
		public void TestCloneRemote ()
		{
			string path = GetTemporaryPath ();
			CommandClient.Clone (TestRepo, path, true, null, "10", null, false, true, MercurialPath);
			Assert.That (Directory.Exists (Path.Combine (path, ".hg")), string.Format ("Repository was not cloned from {0} to {1}", TestRepo, path));
		}
		
		[Test]
		public void TestCloneLocal ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			
			using (var client = new CommandClient (firstPath, null, null, MercurialPath)) {
				File.WriteAllText (file, "1");
				client.Add (file);
				client.Commit ("1");
			}
			try {
			CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
			} catch (Exception ex) {
				Console.WriteLine (ex);
				Assert.That (false, ex.Message);
			}
			Assert.That (Directory.Exists (Path.Combine (secondPath, ".hg")), string.Format ("Repository was not cloned from {0} to {1}", firstPath, secondPath));
			Assert.That (File.Exists (Path.Combine (secondPath, "foo")), "foo doesn't exist in cloned working copy");
				
			using (var client = new CommandClient (secondPath, null, null, MercurialPath)) {
				IList<Revision> log = client.Log (null);
				Assert.AreEqual (1, log.Count, "Unexpected number of log entries");
			}
		}
		
		[Test]
		public void TestAdd ()
		{
			string path = GetTemporaryPath ();
			IDictionary<string,Status > statuses = null;
			
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (Path.Combine (path, "foo"), string.Empty);
				File.WriteAllText (Path.Combine (path, "bar"), string.Empty);
				client.Add (Path.Combine (path, "foo"), Path.Combine (path, "bar"));
				statuses = client.Status (null);
			}
			
			Assert.IsNotNull (statuses);
			Assert.That (statuses.ContainsKey ("foo"), "No status received for foo");
			Assert.That (statuses.ContainsKey ("bar"), "No status received for bar");
			Assert.AreEqual (Status.Added, statuses ["foo"]);
			Assert.AreEqual (Status.Added, statuses ["bar"]);
		}
		
		[Test]
		public void TestCommit ()
		{
			string path = GetTemporaryPath ();
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (Path.Combine (path, "foo"), string.Empty);
				File.WriteAllText (Path.Combine (path, "bar"), string.Empty);
				client.Add (Path.Combine (path, "foo"));
				client.Commit ("Commit all");
				Assert.That (!client.Status ().ContainsKey ("foo"), "Default commit failed for foo");
				
				File.WriteAllText (Path.Combine (path, "foo"), "foo");
				client.Add (Path.Combine (path, "bar"));
				client.Commit ("Commit only bar", Path.Combine (path, "bar"));
				Assert.That (!client.Status ().ContainsKey ("bar"), "Commit failed for bar");
				Assert.That (client.Status ().ContainsKey ("foo"), "Committed unspecified file!");
				Assert.AreEqual (Mercurial.Status.Modified, client.Status ()["foo"], "Committed unspecified file!");
				Assert.AreEqual (2, client.Log (null).Count, "Unexpected revision count");
			}
		}
		
		[Test]
		public void TestLog ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, "1");
				client.Add (file);
				client.Commit ("1");
				File.WriteAllText (file, "2");
				client.Commit ("2");
				Assert.AreEqual (2, client.Log (null).Count, "Unexpected revision count");
			}
		}
		
		[Test]
		public void TestAnnotate ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, "1");
				client.Add (file);
				client.Commit ("1", null, false, false, null, null, null, null, "user");
				Assert.AreEqual ("user 0: 1\n", client.Annotate (null, file));
			}
		}
		
		[Test]
		public void TestDiff ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			string diffText = string.Empty;
			CommandClient.Initialize (path, MercurialPath);
			
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, "1\n");
				client.Add (file);
				client.Commit ("1", null, false, false, null, null, null, null, "user");
				File.WriteAllText (file, "2\n");
				diffText = client.Diff (null, file);
			}
			
			string[] lines = diffText.Split (new[]{"\n"}, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual (6, lines.Length, "Unexpected diff length");
			Assert.AreEqual ("@@ -1,1 +1,1 @@", lines [3]);
			Assert.AreEqual ("-1", lines [4]);
			Assert.AreEqual ("+2", lines [5]);
		}
		
		[Test]
		public void TestExport ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			string diffText = string.Empty;
			CommandClient.Initialize (path, MercurialPath);
			
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, "1\n");
				client.Add (file);
				client.Commit ("1");
				File.WriteAllText (file, "2\n");
				client.Commit ("2");
				diffText = client.Export ("1");
			}
			string[] lines = diffText.Split (new[]{ "\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Where (line => !line.StartsWith ("#")).ToArray();
			Assert.AreEqual (7, lines.Count(), "Unexpected diff length");
			Assert.AreEqual ("@@ -1,1 +1,1 @@", lines [4]);
			Assert.AreEqual ("-1", lines [5]);
			Assert.AreEqual ("+2", lines [6]);
		}
		
		[Test]
		public void TestForget ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			IDictionary<string,Status > statuses = null;
			
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				client.Add (file);
				statuses = client.Status (null);
				
				Assert.IsNotNull (statuses);
				Assert.That (statuses.ContainsKey ("foo"), "No status received for foo");
				Assert.AreEqual (Status.Added, statuses ["foo"]);
				
				client.Forget (file);
				statuses = client.Status ();
			}
			Assert.That (statuses.ContainsKey ("foo"), "foo is no longer known");
			Assert.AreEqual (Status.Unknown, statuses ["foo"], "foo was not forgotten");
		}
		
		[Test]
		public void TestPull ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			CommandClient firstClient = null,
			              secondClient = null;
			
			try {
				// Create repo with one commit
				firstClient = new CommandClient (firstPath, null, null, MercurialPath);
				File.WriteAllText (file, "1");
				firstClient.Add (file);
				firstClient.Commit ("1");
			
				// Clone repo
				CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new CommandClient (secondPath, null, null, MercurialPath);
				Assert.AreEqual (1, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				// Add changeset to original repo
				File.WriteAllText (file, "2");
				firstClient.Commit ("2");
				
				// Pull from clone
				Assert.IsTrue (secondClient.Pull (null), "Pull unexpectedly resulted in unresolved files");
				Assert.AreEqual (2, secondClient.Log (null).Count, "Unexpected number of log entries");
			} finally {
				if (null != firstClient)
					firstClient.Dispose ();
				if (null != secondClient)
					secondClient.Dispose ();
			}
		}
		
		[Test]
		[Ignore("Merge tool popup")]
		public void TestMerge ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			CommandClient firstClient = null,
			              secondClient = null;
			
			try {
				// Create repo with one commit
				firstClient = new CommandClient (firstPath, null, null, MercurialPath);
				File.WriteAllText (file, "1\n");
				firstClient.Add (file);
				firstClient.Commit ("1");
			
				// Clone repo
				CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new CommandClient (secondPath, null, null, MercurialPath);
				Assert.AreEqual (1, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				// Add changeset to original repo
				File.WriteAllText (file, "2\n");
				firstClient.Commit ("2");
				
				// Add non-conflicting changeset to child repo
				File.WriteAllText (Path.Combine (secondPath, "foo"), "1\na\n");
				secondClient.Commit ("a");
				
				// Pull from clone
				Assert.IsTrue (secondClient.Pull (null), "Pull unexpectedly resulted in unresolved files");
				Assert.AreEqual (3, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				Assert.AreEqual (2, secondClient.Heads ().Count (), "Unexpected number of heads");
				
				Assert.IsTrue (secondClient.Merge (null), "Merge unexpectedly resulted in unresolved files");
			} finally {
				if (null != firstClient)
					firstClient.Dispose ();
				if (null != secondClient)
					secondClient.Dispose ();
			}
		}
		
		[Test]
		public void TestHeads ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			CommandClient firstClient = null,
			              secondClient = null;
			
			try {
				// Create repo with one commit
				firstClient = new CommandClient (firstPath, null, null, MercurialPath);
				File.WriteAllText (file, "1\n");
				firstClient.Add (file);
				firstClient.Commit ("1");
			
				// Clone repo
				CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new CommandClient (secondPath, null, null, MercurialPath);
				Assert.AreEqual (1, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				// Add changeset to original repo
				File.WriteAllText (file, "2\n");
				firstClient.Commit ("2");
				
				// Add non-conflicting changeset to child repo
				File.WriteAllText (Path.Combine (secondPath, "foo"), "1\na\n");
				secondClient.Commit ("a");
				
				// Pull from clone
				Assert.IsTrue (secondClient.Pull (null), "Pull unexpectedly resulted in unresolved files");
				Assert.AreEqual (3, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				Assert.AreEqual (2, secondClient.Heads ().Count (), "Unexpected number of heads");
			} finally {
				if (null != firstClient)
					firstClient.Dispose ();
				if (null != secondClient)
					secondClient.Dispose ();
			}
		}
		
		[Test]
		public void TestPush ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			CommandClient firstClient = null,
			              secondClient = null;
			
			try {
				// Create repo with one commit
				firstClient = new CommandClient (firstPath, null, null, MercurialPath);
				File.WriteAllText (file, "1\n");
				firstClient.Add (file);
				firstClient.Commit ("1");
			
				// Clone repo
				CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new CommandClient (secondPath, null, null, MercurialPath);
				Assert.AreEqual (1, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				// Add changeset to child repo
				File.WriteAllText (Path.Combine (secondPath, "foo"), "1\na\n");
				secondClient.Commit ("a");
				
				// Push to parent
				Assert.IsTrue (secondClient.Push (firstPath, null), "Nothing to push");
				
				// Assert that the first repo now has two revisions in the log
				Assert.AreEqual (2, firstClient.Log (null, firstPath).Count, "Known commandserver bug: server is out of sync");
			} finally {
				if (null != firstClient)
					firstClient.Dispose ();
				if (null != secondClient)
					secondClient.Dispose ();
			}
		}
		
		[Test]
		public void TestSummary ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			string summary = string.Empty;
			CommandClient.Initialize (path, MercurialPath);
			
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, "1");
				client.Add (file);
				client.Commit ("1", null, false, false, null, null, null, null, "user");
				summary = client.Summary (false);
			}
			
			Assert.IsTrue (summary.Contains ("branch: default"));
		}
		
		[Test]
		public void TestIncoming ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			CommandClient firstClient = null,
			              secondClient = null;
			
			try {
				// Create repo with one commit
				firstClient = new CommandClient (firstPath, null, null, MercurialPath);
				File.WriteAllText (file, "1");
				firstClient.Add (file);
				firstClient.Commit ("1");
			
				// Clone repo
				CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new CommandClient (secondPath, null, null, MercurialPath);
				Assert.AreEqual (1, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				// Add changesets to original repo
				File.WriteAllText (file, "2");
				firstClient.Commit ("2");
				File.WriteAllText (file, "3");
				firstClient.Commit ("3");
				
				IList<Revision > incoming = secondClient.Incoming (null, null);
				Assert.AreEqual (2, incoming.Count, "Unexpected number of incoming changesets");
			} finally {
				if (null != firstClient)
					firstClient.Dispose ();
				if (null != secondClient)
					secondClient.Dispose ();
			}
		}

		[Test]
		public void TestOutgoing ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			CommandClient firstClient = null,
			              secondClient = null;
			
			try {
				// Create repo with one commit
				firstClient = new CommandClient (firstPath, null, null, MercurialPath);
				File.WriteAllText (file, "1");
				firstClient.Add (file);
				firstClient.Commit ("1");
			
				// Clone repo
				CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new CommandClient (secondPath, null, null, MercurialPath);
				Assert.AreEqual (1, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				// Add changeset to original repo
				File.WriteAllText (file, "2");
				firstClient.Commit ("2");
				File.WriteAllText (file, "3");
				firstClient.Commit ("3");
				
				IList<Revision > outgoing = firstClient.Outgoing (secondPath, null);
				Assert.AreEqual (2, outgoing.Count, "Unexpected number of outgoing changesets");
			} finally {
				if (null != firstClient)
					firstClient.Dispose ();
				if (null != secondClient)
					secondClient.Dispose ();
			}
		}
		
		[Test]
		public void TestVersion ()
		{
			string path = GetTemporaryPath ();
			CommandClient.Initialize (path, MercurialPath);
			Regex versionRegex = new Regex (@"^\d\.\d\.\d.*$");
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				Match match = versionRegex.Match (client.Version);
				Assert.IsNotNull (match, "Invalid version string");
				Assert.That (match.Success, "Invalid version string");
			}
		}
		
		[Test]
		public void TestRevert ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				client.Add (file);
				client.Commit ("Commit all");
				Assert.That (!client.Status ().ContainsKey ("foo"), "Default commit failed for foo");
				
				File.WriteAllText (file, "Modified!");
				Assert.That (client.Status ().ContainsKey ("foo"), "Failed to modify file");
				client.Revert (null, file);
				Assert.That (!client.Status ().ContainsKey ("foo"), "Revert failed for foo");
			}
		}
		
		[Test]
		public void TestRename ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				client.Add (file);
				client.Commit ("Commit all");
				Assert.That (!client.Status ().ContainsKey ("foo"), "Default commit failed for foo");

				client.Rename ("foo", "foo2");
				IDictionary<string,Status > statuses = client.Status ();
				statuses = client.Status (new[]{path}, quiet: false);
				Assert.AreEqual (Status.Removed, statuses ["foo"], string.Format ("Incorrect status for foo: {0}", statuses ["foo"]));
				Assert.AreEqual (Status.Added, statuses ["foo2"], string.Format ("Incorrect status for foo2: {0}", statuses ["foo2"]));
				
				client.Commit ("Commit rename");
				Assert.That (!client.Status ().ContainsKey ("foo"), "Failed to rename file");
				Assert.That (!client.Status ().ContainsKey ("foo2"), "Failed to rename file");
				Assert.That (!File.Exists (file));
				Assert.That (File.Exists (Path.Combine (path, "foo2")));
			}
		}

		[Test]
		public void TestCat ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, "foo\n");
				client.Add (Path.Combine (path, "foo"));
				client.Commit ("Commit all");
				Assert.That (!client.Status ().ContainsKey ("foo"), "Default commit failed for foo");
				
				var contents = client.Cat (null, file);
				Assert.AreEqual (1, contents.Count, "Unexpected size of file set");
				Assert.That (contents.ContainsKey (file), "foo not in file set");
				Assert.AreEqual ("foo\n", contents [file]);
			}
		}
		
		[Test]
		public void TestCatMore ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			Console.WriteLine (Environment.CurrentDirectory);
			CommandClient.Initialize (path, MercurialPath);
			File.Copy ("../../../Mercurial/CommandClient.cs", file);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				client.Add (file);
				client.Commit ("Commit all");
				Assert.That (!client.Status ().ContainsKey ("foo"), "Default commit failed for foo");
				
				var contents = client.Cat (null, file);
				Assert.AreEqual (1, contents.Count, "Unexpected size of file set");
				Assert.That (contents.ContainsKey (file), "foo not in file set");
				Assert.AreEqual (File.ReadAllText (file), contents [file]);
			}
		}
		
		[Test]
		public void TestRemove ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				client.Add (file);
				client.Commit ("Commit all");
				Assert.That (!client.Status ().ContainsKey (file), "Default commit failed for foo");
				
				client.Remove (file);
				Assert.That (!File.Exists (file));
				
				IDictionary<string,Status > statuses = client.Status ();
				Assert.That (statuses.ContainsKey ("foo"), "No status for foo");
				Assert.AreEqual (Status.Removed, statuses ["foo"], string.Format ("Incorrect status for foo: {0}", statuses ["foo"]));
			}
		}
		
		[Test]
		[Ignore("Merge client popup")]
		public void TestResolve ()
		{
			string firstPath = GetTemporaryPath ();
			string secondPath = GetTemporaryPath ();
			string file = Path.Combine (firstPath, "foo");
			CommandClient.Initialize (firstPath, MercurialPath);
			CommandClient firstClient = null,
			              secondClient = null;
			
			try {
				// Create repo with one commit
				firstClient = new CommandClient (firstPath, null, null, MercurialPath);
				File.WriteAllText (file, "1\n");
				firstClient.Add (file);
				firstClient.Commit ("1");
			
				// Clone repo
				CommandClient.Clone (source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new CommandClient (secondPath, null, null, MercurialPath);
				Assert.AreEqual (1, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				// Add changeset to original repo
				File.WriteAllText (file, "2\n");
				firstClient.Commit ("2");
				
				// Add non-conflicting changeset to child repo
				File.WriteAllText (Path.Combine (secondPath, "foo"), "1\na\n");
				secondClient.Commit ("a");
				
				// Pull from clone
				Assert.IsTrue (secondClient.Pull (null), "Pull unexpectedly resulted in unresolved files");
				Assert.AreEqual (3, secondClient.Log (null).Count, "Unexpected number of log entries");
				
				Assert.AreEqual (2, secondClient.Heads ().Count (), "Unexpected number of heads");
				
				Assert.IsTrue (secondClient.Merge (null), "Merge unexpectedly resulted in unresolved files");
				
				IDictionary<string,bool > statuses = secondClient.Resolve (null, true, true, false, false, null, null, null);
				Assert.That (statuses.ContainsKey ("foo"), "No merge status for foo");
				Assert.AreEqual (true, statuses ["foo"], "Incorrect merge status for foo");
			} finally {
				if (null != firstClient)
					firstClient.Dispose ();
				if (null != secondClient)
					secondClient.Dispose ();
			}
		}
		
		[Test]
		public void TestParents ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				client.Add (file);
				client.Commit ("Commit all");
				Assert.That (!client.Status ().ContainsKey (file), "Default commit failed for foo");
				
				IEnumerable<Revision > parents = client.Parents (file, null);
				Assert.AreEqual (1, parents.Count (), "Unexpected number of parents");
			}
		}
		
		[Test]
		public void TestStatus ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			string unknownFile = Path.Combine (path, "bar");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				File.WriteAllText (unknownFile, string.Empty);
				client.Add (file);
				IDictionary<string,Status> statuses = client.Status (path);
				Assert.That (statuses.ContainsKey ("foo"), "foo not found in status");
				Assert.That (statuses.ContainsKey ("bar"), "bar not found in status");
				Assert.AreEqual (Status.Added, statuses ["foo"], "Incorrect status for foo");
				Assert.AreEqual (statuses ["bar"], Status.Unknown, "Incorrect status for bar");
				
				statuses = client.Status (new[]{path}, quiet: true);
				Assert.That (statuses.ContainsKey ("foo"), "foo not found in status");
				Assert.AreEqual (Status.Added, statuses ["foo"], "Incorrect status for foo");
				Assert.That (!statuses.ContainsKey ("bar"), "bar listed in quiet status output");
				
				statuses = client.Status (new[]{path}, onlyFilesWithThisStatus: Status.Added);
				Assert.That (statuses.ContainsKey ("foo"), "foo not found in status");
				Assert.AreEqual (Status.Added, statuses ["foo"], "Incorrect status for foo");
				Assert.That (!statuses.ContainsKey ("bar"), "bar listed in added-only status output");
			}
		}
		
		[Test]
		public void TestRollback ()
		{
			string path = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				client.Add (file);
				client.Commit (file);
				File.WriteAllText (file, file);
				client.Commit (file);
				Assert.AreEqual (2, client.Log (null).Count, "Unexpected history length");
				Assert.That (client.Rollback ());
				Assert.AreEqual (1, client.Log (null).Count, "Unexpected history length after rollback");
				Assert.AreEqual (Mercurial.Status.Modified, client.Status (file) ["foo"], "Unexpected file status after rollback");
			}
		}
		
		[Test]
		public void VerifyArchiveTypeCoverage ()
		{
			foreach (ArchiveType type in Enum.GetValues (typeof (ArchiveType))) {
				Console.WriteLine (Mercurial.CommandClient.archiveTypeToArgumentStringMap [type]);
			}
		}
		
		[Test]
		public void TestArchive ()
		{
			string path = GetTemporaryPath ();
			string archivePath = GetTemporaryPath ();
			string file = Path.Combine (path, "foo");
			CommandClient.Initialize (path, MercurialPath);
			using (var client = new CommandClient (path, null, null, MercurialPath)) {
				File.WriteAllText (file, string.Empty);
				client.Add (file);
				client.Commit (file);
				client.Archive (archivePath);
				Assert.That (Directory.Exists (archivePath));
				Assert.That (!Directory.Exists (Path.Combine (archivePath, ".hg")));
				Assert.That (File.Exists (Path.Combine (archivePath, "foo")));
			}
		}

		static string GetTemporaryPath ()
		{
			string dirName = string.Format("Mercuria.Tests.{0}", Path.GetRandomFileName());
			string path = Path.Combine (Path.GetTempPath (), dirName);
			if (IsRunningOnMac ()) {
				// HACK: tmp path is weird on osx
				path = "/private" + path;
			}
			garbage.Add (path);
			return path;
		}
	}
}

