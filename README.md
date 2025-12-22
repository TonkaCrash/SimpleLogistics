# KSP Simple Logistics
Simple resource sharing between landed vessels. This spreads any resources among all vessels connected to the network.

The idea behind this mod is that it represents buried cables and pipes between fixed structures making up a base, so only landed vessels are eligible to connect to the network.

The way this mod is implemented has a few limitations:
* This mod only works while a vessel is loaded, so many background processing tasks will stop when unloaded craft can't reach the network and run out of resources.
* You must have some storage space for a resource in your craft to receive the resource from the network. For example if your ISRU's and ore are in different craft, the ISRU can't reach the ore unless there is a small storage tank on the same craft.

If all you are worried about is keeping the lights on when you visit a base this mod is probably fine.

## Dependencies
* [Module Manager][mm]
* [Toolbar Controller][tbc]
* [ClickThroughBlocker][ctb]

## Changelog
### Version 2.1.1
- Recompile for 1.12.5
- Update to .Net 4.8
- Tonka Crash minimalist approach. Hack out anything that doesn't make sense or work.
- Started from Real Gecko version as baseline of required functionality and carefully picked thourgh later changes for improvements to keep.
- Major change from Real Gecko version is GUI related to support Click Through Blocker and Toolbar Controller.
- Probably broke Localization which were never complete anyway.
### Version 2.0.2-2.0.6
- Forked and modified by zer0Kerbal last compiled for 1.12.3
- Lots of zero value added garbage inserted into the mod and github
- Check his fork for full content.
- zer0Kerbal also attempts to change to GPL V3 license in violation of the original CC BY-NC-SA license Real Gecko used.

### Version 2.0.2
- Recompile for KSP 1.3.1

### Version 2.0.1
- Fixed MM patch
