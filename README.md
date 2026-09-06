# NPC Replacements Helper for Baldi's Basics Plus (BepInEx)
**Have you ever wondered that one feature from Baldi's Basics Times came back as an independent mod and is for any mod to use for?**

Now with the power of science, mods can now have NPCs have a chance to replace an existing NPC!
This took at least 4 hours to make and is an overhaul of BBT's system, which is out of date for this current BBP version.

# Usage
There are extension functions that you can use with this plugin for your mod.
```c#
customLevelObject.AddReplacementNPC(npcToUse, npcToReplace) // for a single NPC, the standard but requires an NPC prefab to register.
sceneObject.AddReplacementNPC(npcToUse, characterToReplace) // for multiple NPCs with that same character enum, mainly if NPC prefab is different than the default one.
```
As there are two `AddReplacementNPC` function that is the main purpose of this helper, you might be using the one that targets `SceneObjects` instead because it's easier to target existing NPCs with that same character enum with different prefabs.

**Don't forget to add a BepInDependency for your plugin that REQUIRES this mod!** (HardDependency or SoftDependency. Hard dependency for when the replacement utility is really needed for your mod!)
