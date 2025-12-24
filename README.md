# KSP Simple Logistics
Simple resource sharing between landed vessels. This spreads any resources among all nearby vessels connected to the network. Docking or connecting craft together is not required.

Conceptually to me this mod represents buried cables and pipes between fixed structures making up a base, so only landed or splashed (assuming some kind of floating city maybe) vessels are eligible to connect to the network. This mod is to address the issue of sharing resources between landed craft without needing to dock them together that invites Kraken attacks. The original author, Real Gecko, never went beyond checks for landed, but zer0Kerbal started complicating the code in ways that made little sense to me, so I forked from Real Gecko's version and only referred to zer0Kerbal's fork for the how the Toolbar Controller and Click Through Blocker needed to be integrated.

## How it works
The part module, LogisticsModule, is added with a Module Manager patch to every part with ModuleCommand. This covers cockpits, pods and probe cores. This adds a toggle switch in the PAW menu for the Simple Logistics Network. The status is indicated in the menu by either Connected or Unplugged. If you toggle it on in flight or orbit expect an error on the screen.

When you load a vessel, Simple Logistics checks the status of all vessels in physics range to see if they are connected to thenetowrk. Any connected craft are inventoried to build up a Resource Pool to divide resource evenly, but gives a preference to smallest tanks first.

There is also a Toolbar icon for this mod that opens an GUI. A toggle switch for the network is available there along with quatities of resources. Using the menu you can request resources from the network for disconnected craft, so you could land a transport and request a fill up of resources from the network.

## Limitations
If all you are worried about is keeping the lights on when you visit a base this mod is probably fine, but this mod has a few limitations:
* This mod only works while a vessel is loaded and affects only craft in physics range, so many background processing tasks will stop when unloaded craft can't reach the network and run out of resources.
* You must have some storage space for a resource in your craft to receive the resource from the network. For example if your ISRU's and ore are in different craft, the ISRU can't reach the ore unless there is a small storage tank on the same craft.
* I'm trying to think of a way around this next issue without needing to specifically address resource converters. If you have a resource converter adding resources to a vessel it must have empty space on that vessel for new production even if there is space on the network. For example, I land a big storage tank next to my ISRU and connect it to network. What happens is both craft immediately split resources between them and the ISRU starts producing more and the two craft fill in unison until the one with the ISRU hits max capacity and production stops leaving the storage tank with empty capacity.

## Dependencies
* [Module Manager][mm]
* [Toolbar Controller][tbc]
* [ClickThroughBlocker][ctb]

## Changelog
### Version 2.1.1
- Further stripping down the code
### Version 2.1.0
- Recompile for 1.12.5
- Update to .Net 4.8
- KISS, Minimalist approach. Essentials only on UI. Only log problems.
- Started from Real Gecko version as baseline of required functionality and carefully picked through later changes for improvements to keep.
- Major changes from Real Gecko code are all GUI code related related to localization and the toolbar menu.
- Probably broke existing localizations which were never complete anyway.
### Version 2.0.?-2.0.6 - not in this fork
- Fork created by zer0Kerbal
- Lots of zero value added garbage inserted into the mod and github. It was the UI clutter that prompte my fork.
- Check his fork for full content.

### Version 2.0.2
- Recompile for KSP 1.3.1

### Version 2.0.1
- Fixed MM patch
