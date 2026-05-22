# CasualtiesMiner

A suite of Data Mining tool for [Casualties: Unknown](https://store.steampowered.com/app/4576490/Casualties_Unknown/).

Used for the Wiki project where automation of data dumping is needed

## CasualtiesDumper

The data dumper, analyze the Assembly-CSharp's IL code to give us the game's data, the current limitation is Delegate (as seen in OnUse, LimbUse etc etc), I don't wanna write a complex parser, so I just dump C# code lmao

### Usage

Windows
```
CasualtiesDumper.exe path\to\Assembly-CSharp.dll
```

macOS / Linux
```
CasualtiesDumper path/to/Assembly-CSharp.dll
```