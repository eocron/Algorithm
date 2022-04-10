# Algorithm
Library gathers some wide (and not so wide) known algorithms. You are free to use them, abuse them, upgrade them.
Main mottos in descending order:

  - Make it simple.
  - Make it clean.
  - Make it fast.
  - Make it.
     
# Currently implemented

## Levenstain distance/remarks

https://en.wikipedia.org/wiki/Levenshtein_distance

  - Weight matrix calculation
  - Remarks calculation (sequence of actions to perform to transform source to target)

## Red Black Tree

https://en.wikipedia.org/wiki/Red%E2%80%93black_tree

  - Complete implementation of IDictionary
  - Duplicate keys are not allowed

## Fibonacci heap as PriorityQueue

https://en.wikipedia.org/wiki/Fibonacci_heap

  - Enqueue/Dequeue/EnqueueOrUpdate/Peek
  - Simple O(1) amortized time priority queue

## Dijkstra algoritm

https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm

  - Advanced version with Fibonacci heap speed up, so complexety is O(E + V*log(V))
  - Supports infinity graphs, where edges/verticies known only at runtime: large graphs, dynamic algorithms, greedy algoritms, etc.
  - Supports various ways to calculate weight in graph: weight of vertex, weight of edge
  - Supports stop condition on reaching target, eliminating excess calculations.
  - Supports weight comparer.

## File system caching of files

This implementation will use file system as storage for really-really-really big files, which can't be stored in-memory, and by all means should reside in file system.
Also, when operating on big files you may want to cancell operation at some point, so there is handy built-in cancellation token support. No more 1 TB stream download infinitelly.
Also, it is async, so blocking CPU resources is minimized.
Also, any review is appreciated to lower my confidence in myself.

  - Support absolute timeout caching policy: file will be removed from cache at specified time.
  - Support slide timeout caching policy: each access for read/write to file will refresh it's timeout.
  - You can inject custom IFileSystem interface, which enables testing or caching in-memory for fast access. Default is std file system, no long-path support.
  - Cache can be invalidated. Particular files can be invalidated.

## Byte Array Comparer

As silly as it sounds, byte array comparer is widely used, but it's speed is very low,
and it is not present in common libraries. 

  - Around 180 faster than SequenceEquals almost like memcmp.
  - Has fast but not precise hash code. By default, invoked when byte array is greater than certain bound.
    This makes hashing of same size arrays more efficient than just by length comparison.
  - Has slow but through hash code. Choose yourself.

## Hex format parsing

You can parse practically any HEX format there is and convert to it any byte array:

    0x01aB
    01aB
    \\x01aB
    %01aB
    &#x01aB
    U+01aB
    #01aB

No more BitConvert garbage, and it is efficient (probably).

## Binary Search

https://en.wikipedia.org/wiki/Binary_search_algorithm

  - Allows to perform binary search on collections which you know sorted.
  - Search lower bound.
  - Search upper bound.
  - Custom comparer support.
  - Descending order support.

## Random class extensions

  - NextStream.
  - NextString.
  - NextBool.
  - NextLong.