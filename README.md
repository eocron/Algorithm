# Algorithm
Library gathers some wide (and not so wide) known algorithms. You are free to use them, abuse them, upgrade them.
Main mottos in descending order:

  - Make it simple.
  - Make it clean.
  - Make it fast.
  - Make it.
     
## Algorithms implemented:

### Levenstain distance/remarks

https://en.wikipedia.org/wiki/Levenshtein_distance

  - Weight matrix calculation
  - Remarks calculation (sequence of actions to perform to transform source to target)

### Red Black Tree

https://en.wikipedia.org/wiki/Red%E2%80%93black_tree

  - Complete implementation of IDictionary
  - Duplicate keys are not allowed

### Fibonacci heap as PriorityQueue

https://en.wikipedia.org/wiki/Fibonacci_heap

  - Enqueue/Dequeue/EnqueueOrUpdate/Peek
  - Simple O(1) amortized time priority queue

### Dijkstra

https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm

  - Advanced version with Fibonacci heap speed up, so complexety is O(E + V*log(V))
  - Supports infinity graphs, where edges/verticies known only at runtime: large graphs, dynamic algorithms, greedy algoritms, etc.
  - Supports various ways to calculate weight in graph: weight of vertex, weight of edge
  - Supports stop condition on reaching target, eliminating excess calculations.
  - Supports search everything to build shortest path tree (useful for finding shortest path to any node).
  - Supports weight comparer.

### Binary Search

https://en.wikipedia.org/wiki/Binary_search_algorithm

  - Allows to perform binary search on collections which you know sorted.
  - Search lower bound.
  - Search upper bound.
  - Custom comparer support.
  - Descending order support.


## Utility implemented:

### Stream as `IEnumerable<Memory<byte>>`/`IAsyncEnumerable<Memory<byte>>`

Ever wondered why similar concepts does not provide similar interface?
Now you can forget about streams, use enumerators and convert them however you want without loosing performance and creating RAM spikes.
Example:

    var goods = File.OpenRead("how_to_jackoff.txt")
        .AsEnumerable(leaveOpen:false)
        .GZip(CompressionMode.Compress) //compressing using gzip
        .CryptoTransform(encryptor, CryptoStreamMode.Write)//encrypting using your encryptor
        .CryptoTransform(decryptor, CryptoStreamMode.Read)
        .GZip(CompressionMode.Decompress)
        .Convert(Encoding.UTF8)//converting byte chunks into char chunks
        .ToString();

PS:
  - Those enumerables are single-call, so it cannot be enumerated again unless you leave root stream open and seek it to beginning. 
  - Disposes original stream by default and it can be turned off (optional).
  - Each chunk of enumerable is actually same memory buffer, so for random access it should be serialized to `byte[]`, usually this is not needed.
  - Has async overload through entire pipeline, so can be used in IO-compliant processing.
  - Internally it uses `MemoryPool<T>.Shared` for all of its buffering.

### Byte Array Comparer

As silly as it sounds, byte array comparer is widely used, but it's very slow,
and it is not present in common libraries. This one competes with its base64 variant of string.

  - Has even faster but not precise hash code (tails + count + some inside is analysed). Disabled by default. Good for large files/blobs with random content.
  - Performance similar to memcmp.
  - No intrinsics involved (.netstandard 2.0 support)
  - Due to usage of xxHash64 algorithm hash distribution is almost perfect which lead to more speed in dictionaries. https://en.wikipedia.org/wiki/List_of_hash_functions#Non-cryptographic_hash_functions

#### Benchmark vs base64 string of same byte array of size 16 kb and 15 bytes

|                   Method |  Categories | TestDataId |          Mean |       Error |      StdDev | Ratio | RatioSD |
|------------------------- |------------ |----------- |--------------:|------------:|------------:|------:|--------:|
|              Equals_fast |      Equals |          0 |     22.749 ns |   0.2878 ns |   0.2692 ns |  6.83 |    0.13 |
|      Equals_Base64String |      Equals |          0 |      3.333 ns |   0.0473 ns |   0.0442 ns |  1.00 |    0.00 |
|                          |             |            |               |             |             |       |         |
|      Equals_Base64String |      Equals |          1 |    708.847 ns |   7.3631 ns |   6.5272 ns |  1.00 |    0.00 |
|              Equals_fast |      Equals |          1 |    538.450 ns |   5.9964 ns |   5.6090 ns |  0.76 |    0.01 |
|                          |             |            |               |             |             |       |         |
|         GetHashCode_fast | GetHashCode |          0 |     20.533 ns |   0.1867 ns |   0.1746 ns |  1.37 |    0.02 |
| GetHashCode_Base64String | GetHashCode |          0 |     15.006 ns |   0.1682 ns |   0.1573 ns |  1.00 |    0.00 |
|                          |             |            |               |             |             |       |         |
| GetHashCode_Base64String | GetHashCode |          1 | 15,029.590 ns | 159.9461 ns | 149.6137 ns |  1.00 |    0.00 |
|         GetHashCode_fast | GetHashCode |          1 |  1,031.287 ns |   1.0731 ns |   0.8378 ns |  0.07 |    0.00 |
|                          |             |            |               |             |             |       |         |
|           NotEquals_fast |   NotEquals |          0 |     23.158 ns |   0.2980 ns |   0.2787 ns |  7.20 |    0.15 |
|   NotEquals_Base64String |   NotEquals |          0 |      3.219 ns |   0.0438 ns |   0.0410 ns |  1.00 |    0.00 |
|                          |             |            |               |             |             |       |         |
|           NotEquals_fast |   NotEquals |          1 |      9.602 ns |   0.0867 ns |   0.0768 ns |  2.94 |    0.03 |
|   NotEquals_Base64String |   NotEquals |          1 |      3.265 ns |   0.0338 ns |   0.0282 ns |  1.00 |    0.00 |

### Hex format parsing

You can parse practically any HEX format there is and convert to it any byte array:

    0x01aB
    01aB
    \\x01aB
    %01aB
    &#x01aB
    U+01aB
    #01aB

No more BitConvert garbage, and it is efficient (probably).

### File system caching of files

This implementation will use file system as storage for really-really-really big files, which can't be stored in-memory, and by all means should reside in file system.
Also, when operating on big files you may want to cancell operation at some point, so there is handy built-in cancellation token support. No more 1 TB stream download infinitelly.
Also, it is async, so blocking CPU resources is minimized.
Also, any review is appreciated to lower my confidence in myself.

  - Support absolute timeout caching policy: file will be removed from cache at specified time.
  - Support slide timeout caching policy: each access for read/write to file will refresh it's timeout.
  - You can inject custom `IFileSystem` interface, which enables testing or caching in-memory for fast access. Default is std file system, no long-path support.
  - Cache can be invalidated. Particular files can be invalidated.

### Random class extensions

  - `NextStream`
  - `NextString`
  - `NextBool`
  - `NextLong`