# hglib-cli

PersistentClient client uses -> `serve --cmdserver pipe --noninteractive --encoding cp1252`

Disposing of PersistentClient closes the internal client

EXAMPLES

~~~~
using Mercurial;

var client = new PersistentClient(@"C:\...\path_to_repo\");

var command = new LogCommand();

client.Execute(command);

Debug.WriteLine(command.Result);
~~~~
---

Fork of https://bitbucket.org/TakUnity/hglib-cli/src

This project contains code to wrap the [Mercurial][1] command-line tool
so that its functions can be used by a .NET application.

Additionally, you can control the [TortoiseHg][4] GUI clients for
Windows, both the old (PyGTK port) and the new (PyQT port.)

Important links:

* [Repository and project at CodePlex][2]  
* [Email me, the maintainer: Lasse V. Karlsen][3]

NOTE
---

As this code is getting closer to 1.0, more and more will be stabilized.

At the moment, only the Diff command is slated for a bigger change,
to produce an object model instead of just a text dump. A "RawDiff" command
will be added as well, doing what the current command does.

DISCLAIMER
---

This project is not in any way an official .NET class
library for the Mercurial or TortoiseHg projects.

I am not affiliated with the Mercurial or TortoiseHg projects in any way.

This project is not sanctioned or in any way approved by the Mercurial or
TortoiseHg projects.

Any bugs in this library is entirely mine (Lasse V. Karlsen).

  [1]: http://mercurial.selenic.com/
  [2]: http://mercurialnet.codeplex.com/
  [3]: mailto:lasse@vkarlsen.no
  [4]: http://tortoisehg.bitbucket.org/



The examples that are shipped as part of the Mercurial.Net
[repository][1] requires [LINQPad][2] to run, a free tool made
by the author of [C# 40 in a Nutshell][3], a [O'Reilly][4]
book about C# 4.0, LINQ and various other topics.

Note that you need [LINQPad][2] version 4.27.1 or higher, as
the scripts here rely on a relatively new feature where the script
can discover its location on disk. This is used so that the scripts work against the
repository of Mercurial.Net automatically.

Ensure that you load up the solution file in Visual Studio 2010,
and do a full rebuild for the Debug target before testing any
of the examples. They rely on the binary produced as part of the
build output.

The examples numbered 100 and upwards are excercising the GUI client,
TortoiseHg.