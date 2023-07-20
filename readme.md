# Silapna

Voice installer for VP.

## Features

### Repack vppk
You can repack a vppk to a share-able one. No more `Failed to load package`.

### Install vppk
You can also directly install a vppk whether it is shareable or not, without need of launching VP.

### Build Virtual Storage
*What is a "Virtual Storage" and why do I need it?*

Virtual Storage (VStorage) is a setting folder copy. Silapna generates VStorages for each single voice, and generates an index file (`voice_map.json`) for them.

If you use the commandline mode of VP frequently, you may want to use VStorages. The fact is, our dear VP will scan every voice when launching no matter in GUI mode or commandline mode. But when you're using commandline mode, you may only need 1 voice and you just want the audio output ASAP. VStorages can help you to reduce the launching time in commandline mode.


## LICENSE
CC BY-NC-SA 4.0

People associated with SVKey/YumeKey are not welcomed to use this software.

---

by Ulysses from VOICeVIO
