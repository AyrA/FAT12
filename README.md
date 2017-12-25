# FAT12

FAT12 Interpreter

## About

This component can read FAT12 image files, which are a common result of Floppy Disk imaging Software.
This can also be used if you have one of those floppy drive emulators and want to inspect the images.

The entire source is fully commented to allow easy usage and inspection.

## Usage

`Program.cs` is hardcoded at the moment.
Your best way to use this is to delete said file and compile it as a class library.

## Features

- Read Boot Sector
- Read and draw FAT12 Table (Clustermap) in console and bitmap file
- Read all FAT12 Directory Entries, not only root directory
- Read Cluster Chains and Files, including fragmented files.

## TODO

- [X] Comment all the things.
- [ ] Make this into a library.
