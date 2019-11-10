# Algorithm
Library gathers some wide (and not so wide) known algorithms. You are free to use them, abuse them, upgrade them.
Main mottos in descending order:

  - Make it simple.
  - Make it clean.
  - Make it fast.
  - Make it.
     
# Currently implemented

## Levenstain distance/remarks

Levenstain is Levenstain. More here - https://en.wikipedia.org/wiki/Levenshtein_distance

  - Weight matrix calculation
  - Remarks calculation (sequence of actions to perform to transform source to target)

## File system caching of files

This implementation will use file system as storage for really-really-really big files, which can't be stored in-memory, and by all means should reside in file system.
Also, when operating on big files you may want to cancell operation at some point, so there is handy built-in cancellation token support. No more 1 TB stream download infinitelly.
Also, it is async, so blocking CPU resources is minimized.
Also, any review is appreciated to lower my confidence in myself.

  - Support absolute timeout caching policy: file will be removed from cache at specified time.
  - Support slide timeout caching policy: each access for read/write to file will refresh it's timeout.
  - You can inject custom IFileSystem interface, which enables testing or caching in-memory for fast access. Default is std file system, no long-path support.
  - Cache can be invalidated. Particular files can be invalidated.