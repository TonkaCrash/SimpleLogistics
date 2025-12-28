# KSP Simple Logistics
Simple resource sharing between landed vessels. This spreads any resources among all nearby vessels connected to the network. Docking or connecting craft together is not required. This also allows a disconnected vessel to pull resources from the pool of resources on the base.

Conceptually to me this mod represents buried cables and pipes between fixed structures making up a base, so only landed or splashed (assuming some kind of floating city maybe) vessels are eligible to connect to the network. It starts to feel a bit cheaty to allow rovers to connect, but to keep the mod simple if you are landed and in physics range of another vessel you can share resources.

The original author, Real Gecko, never went beyond checks for landed. I extended this to splashed and pre-launch. Pre-launch was mainly so I could test things by spawning to the runway or launchpad. But zer0Kerbal started complicating the code by adding a setting page and allowing all sorts of choices for the player. Want resource sharing during re-entry or just refuel by flying over your base. Or maybe just keep your ship fully fueled during a launch until you fly out of physics range. You could with his versions. Sometimes just because you can do something doesn't mean you should. I just fall back on the old buried cable and pipes analogy for what makes sense and if you as a player want something different, download and modify the code like I did. Just about everything zer0Kerbal did has been stripped out. I forked from Real Gecko's version and only referred to zer0Kerbal's fork for the how the Toolbar Controller and Click Through Blocker needed to be integrated and ended up rewriting the GUI.

## How it works
The part module, LogisticsModule, is added with a Module Manager patch to every part with ModuleCommand. This covers cockpits, pods, probe cores and a few other parts. For the user this adds a toggle switch in the PAW menu for the Simple Logistics Network. The status is indicated as either either Connected or Unplugged. If you toggle it on in flight or orbit expect an error on the screen. There are no settings, just toggle it on or off on each craft and it will share with over vessels in physics range.

When you load a vessel, Simple Logistics checks the status of all vessels in physics range to see if they are connected to the netowrk. Any connected craft are inventoried to build up a Resource Pool to divide resource, but gives a preference to smallest tanks first.

There is also a Toolbar icon for this mod that opens an GUI that provides inventories of your current craft's resources and the resource pool, if one is active. A toggle switch for the network is available there along with quantities of resources. Using the menu you can request resources from the network for disconnected craft, so you could land a transport and request a fill up of resources from the network.

## Limitations
If all you are worried about is keeping the lights on when you visit a base this mod is probably fine, but this mod has a few limitations:
* This mod only works while a vessel is loaded and affects only craft in physics range, so many background processing tasks will stop when unloaded craft can't reach the network and run out of resources.
* You must have some storage space for a resource in your craft to receive the resource from the network. For example if your ISRU's and ore are in different craft, the ISRU can't reach the ore unless there is a small storage tank on the same craft.
* GUI repaint sometimes glitches and doesn't do a full refressh (my inexperience showing here) changinge vessel and back again usually resets it.

## TODO
* Enable two-way transfers for disconnected vessels. It looks easy enough to implement in the code, but the GUI is cluttered enough as is. Need to thenk about an elegant solution.

## Dependencies
* [Module Manager][mm]
* [Toolbar Controller][tbc]
* [ClickThroughBlocker][ctb]

## Changelog
### Version 2.1.1
- Make one little change and it snowballs into a near total rewrite. Conceptually little has changed or with the underlying algorithms, but the GUI presents more info to the user.
- Scales with UI_Scale in game settings
- Previous Localization files are obsolete with all the GUI changes and deleted at this point. English is all that's currently implemented.
- Implemented a fix for ISRUs adding resources to Pool on other vessels by leaving a little free space in each part until the available free space runs out. Needs testing.
- Now excludes resources defined as not transferrable in their Resource Definition. This addresses things like Uranium in NFE reactors that were moving around in the old version I was using.
- Resources that cannot be transferred do not show in Resource Pool.
- Disconnected vessels only see pool resources that apply to them.
### Version 2.1.0
- Recompile for 1.12.5
- Update to .Net 4.8
- KISS, Minimalist approach. Essentials only on UI. Only log problems.
- Started from Real Gecko version as baseline of required functionality.
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
