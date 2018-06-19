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