# BITSPLAT

A really crude alternative to bittorrent where file transfer is done by physical devices.

The idea is that two parties can meet, exchange external drives and share files with each other
on those drives, reliably and without any need for intermediate networking.

Much of the final solution will mimic [medesync](https://github.com/fluffynuts/medesync) since this
should eventually be a (practically) drop-in replacement for that program and what it currently does
(synchronising media between my master PC and my mede8er player), but it should also:

- be tested
- be able to operate with a database at the target instead of relying on
  the target always keeping files, such that the target may be able to be
  pruned and pruned files should not be replaced on the next run
- use dotnet core. I <3 Python, but I'm at the point where I want the
  quickest dev-path to Getting This Done and I just don't know enough
  about the following in Python:
  - testing
  - dependency injection (so that I can swap out, at run-time)
    - file-system abstraction (for local vs ftp vs whatever)
    - "watched marker" abstractions (eg mede8er vs Kodi)
    - whatever else
