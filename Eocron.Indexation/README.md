# Indexation

Used for several projects, to speed up fixed value domain queries. 
Basically an OLAP cube in C# based on RoaringBitmap.
Support simple Include/Exclude lists. Example:

    Prop1 IN (A, B, C) //include Prop1
    AND
    Prop2 IN (1, 2, 3) //include Prop2
    AND
    Prop3 NOT IN (d, e, f) //exclude Prop3