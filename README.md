# DAD - Checkpoint
What's working:
- Client can read and interpret scripts.
- XL algorithm is working for the normal operation [1]. 
- SMR algorithm is working for the normal operation [1]. 
- Basic structure parser for the Puppet Master. 
- Puppet Master can parse script files. Cannot yet interpret them.

Our implementation doesn't ensure reliability with network delays, so the behavior can be unpredictable and might not work properly.<br />
[1] Normal Operation means when the system is fully operational wihout any failure. 
