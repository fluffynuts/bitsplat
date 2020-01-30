# BITSPLAT

A really crude alternative to bittorrent where file transfer is done by physical devices.

The idea is that two parties can meet, exchange external drives and share files with each other
on those drives, reliably and without any need for intermediate networking.


## Quick start

```
# sync with history
bitsplat -s /path/to/media -t /path/to/removable/drive/media

# sync with history and archiving of files at the target which have a corresponding .t file
# -> this is how the mede8er players mark files as watched
# -> other 'watched marker' mechanisms can be observed, with a little code, when the need arises
bitsplat -s /path/to/media -t /path/to/removable/drive/media -a /path/to/archive/media
```

There are many more options; read the help for more info:
```
$ bitsplat --help

BitSplat
A simple file synchroniser aimed at media sync
  --keep-stale, -k
    Keep stale files (ie files removed from source and still at the target
  --no-resume
    Disable resume
    The default is to resume from the current target byte offset if less than
    the source size
  --resume-check-bytes
    How many bytes to check at the end of a partial file when considering resu
    me
  -a, --archive
    Path / URI to use for archiving
  -h, --history-db
    Override path to history database
    The default location for the database is at the root of the target
  -m, --mode
    Set the sync mode to one of: All or Opt-In (case-insensitive)
    All synchronises everything
    OptIn only synchronises folders which already exist or have been recorded
    in the history database
  -n, --no-history
    Disables the history database
    If you don't need to have a sparse target, you can disable to history data
    base to fall back on simpler synchronisation: what's at the source must en
    d up at the target
  -q, --quiet
    Produces less output, good for non-interactive scripts
  -r, --retries
    Retry failed synchronisations at most this many times
  -s, --source
    Path / URI to use for source
  -t, --target
    Path / URI to use for target
```

Whilst the examples above display how bitsplat is used under Linux, there are binaries available for
Linux, Windows and OSX (the latter is untested -- feedback welcome). The windows binary can be obtained
from `scoop` too:
```
scoop bucket add fluffynuts https://github.com/fluffynuts/scoop-bucket
scoop install bitsplat
```

## More blathering

Functionally, it replaces [medesync](https://github.com/fluffynuts/medesync), providing the same
workflow (sync & archive), as well as providing other flows like:
- opt-in sync
- items found in history but not at the target are not re-synced, facilitating smaller, portable
  media to be used to carry partial synchronisations from a source

Internally, concepts are abstracted to facilitate replacement:
- file systems
- methods for determining if source material should be archived