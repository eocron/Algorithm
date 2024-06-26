# Eocron.ProxyHost

This library contains simple Proxy class with single purpose: escaping intranet in safe manner.
It still doesn't guarantee you performance, because your intranet proxy can decide to chop your
packets as it wishes, throttle them, etc, so if you want maximum performance - don't use proxy at all 
or obliterate your intranet with as much connections as possible.

Still, it has pros you might need:
  
  - Avoid complex authentication on your intranets, such as Kerberos/NTLM/certificate/etc.
  - Safety. You can add any authentication you want on TCP port or make it so only your process has access to it.
  - In memory hosting. It means you don't need to constantly monitor it.
  - HTTP(s) integration, meaning you can basically send anything you want through it, even binary protocols: Kafka/Cassandra/etc
