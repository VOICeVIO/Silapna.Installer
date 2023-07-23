# Silapna

Voice installer for VP.

## 功能

### 重打包vppk
将不可分享格式的vppk重新打包为可分享格式，从而可以拖到VP上安装。当然，安装后仍然要输入序列号。

### 安装vppk
无需打开VP，直接安装vppk，无论vppk是否可分享。当然，使用前仍然要输入序列号。

### 生成虚拟声源库
*什么是“虚拟声源库”，我要这玩意作甚？*

虚拟声源库（VStorage）是将每个单一声源复制到一个单独的声源文件夹中（声源文件使用HardLink，不会实际占用多份空间）。当你使用命令行模式时，VP在启动时仍然会扫描所有声源。如果让VP读取到仅有1个声源的虚拟声源库文件夹，则可以帮助你缩短命令行模式的启动时间，更快地生成音频。

## Features

### Repack vppk
You can repack a vppk to a shareable one. No more `Failed to load package`.

### Install vppk
You can also directly install a vppk whether it is shareable or not, without need of launching VP.

### Build Virtual Storage
*What is a "Virtual Storage" and why do I need it?*

Virtual Storage (VStorage) is a setting and voice library folder copy. Silapna generates VStorages for each single voice, and generates an index file (`voice_map.json`) for them. Hard links are used for sylapack files, so it won't take too much disk space.

If you use the commandline mode of VP frequently, you may want to use VStorages. The fact is, our dear VP will scan every voice when launching no matter in GUI mode or commandline mode. But when you're using commandline mode, you may only need 1 voice and you just want the audio output ASAP. VStorages can help you to reduce the launching time in commandline mode.


## LICENSE
CC BY-NC-SA 4.0

People associated with SVKey/YumeKey are not welcomed to use this software.

---

by Ulysses from VOICeVIO
