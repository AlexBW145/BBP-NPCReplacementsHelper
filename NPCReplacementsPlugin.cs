using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NPCReplacementsHelper;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "11.0.0.0")]
public class NPCReplacementsPlugin : BaseUnityPlugin
{
    internal const string 
        PLUGIN_GUID = "alexbw145.bbplus.npcreplacementhelper",
        PLUGIN_NAME = "Level Generator NPC Replacements.",
        PLUGIN_VERSION = "1.0.0";
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        new Harmony(PLUGIN_GUID).PatchAllConditionals();
        LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
        {
            GeneratorManagement.Register(this, GenerationModType.Preparation, (name, num, sceneObject) =>
            {
                foreach (var levelObject in sceneObject.GetCustomLevelObjects())
                {
                    if (levelObject.IsModifiedByMod(Info)) continue;
                    levelObject.SetCustomModValue(Info, "replacementNPCs", new List<NPCReplacementData>());
                    levelObject.MarkAsModifiedByMod(Info);
                }
            });
#if DEBUG
            // TEST TEST, MAKING SURE THE HELPER WORKS.
            string[]
            BCPPTESTPROOF = [
                "RPSGuy",
                "ERRORBOT",
                "SiegeCannonCart",
                "MrPortalMan",
                "MrRobber"
                ];
            Character[]
            BPPTESTPROOF = [
                Character.Playtime,
                Character.Bully,
                Character.Prize,
                Character.Sweep,
                Character.Crafters
                ];
            GeneratorManagement.Register(this, GenerationModType.Finalizer, (name, num, sceneObject) =>
            {
                HashSet<WeightedNPC> BCPPChars = new();
                foreach (var npc in sceneObject.potentialNPCs)
                {
                    if (BCPPTESTPROOF.Contains(npc.selection.Character.ToStringExtended()))
                        BCPPChars.Add(npc);
                }
                foreach (var bcppchar in BCPPChars)
                {
                    var bcppstring = bcppchar.selection.Character.ToStringExtended();
                    if (sceneObject.potentialNPCs.Exists(x => x.selection.Character == BPPTESTPROOF[BCPPTESTPROOF.ToList().IndexOf(bcppstring)]))
                    {
                        switch (bcppstring)
                        {
                            case "RPSGuy":
                                sceneObject.AddReplacementNPC(bcppchar, BPPTESTPROOF[0]); // This rock spawns in F1, I dunno if it can spawn again or alongside.
                                break;
                            case "ERRORBOT":
                                sceneObject.AddReplacementNPC(bcppchar, BPPTESTPROOF[1]);
                                break;
                            case "SiegeCannonCart":
                                sceneObject.AddReplacementNPC(bcppchar, BPPTESTPROOF[2]);
                                break;
                            case "MrPortalMan":
                                sceneObject.AddReplacementNPC(bcppchar, BPPTESTPROOF[3]);
                                break;
                            case "MrRobber":
                                sceneObject.AddReplacementNPC(bcppchar, BPPTESTPROOF[4]);
                                break;
                        }
                        sceneObject.potentialNPCs.Remove(bcppchar);
                    }
                }
            });
#endif
        }, LoadingEventOrder.Pre);
    }
}

public static class NPCReplacementExtensions
{
    public static void AddReplacementNPC(this CustomLevelObject ld, WeightedNPC npcToUse, NPC npcToReplace)
    {
        List<NPCReplacementData> dataList = (List<NPCReplacementData>)ld.GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs");
        if (dataList == null)
        {
            NPCReplacementsPlugin.Logger.LogError($"Attempted to insert replacement NPC {npcToReplace.name} with {npcToUse.selection.name} but {ld.name} somehow lacks this mod value even though it should've had it in the first place!");
            return;
        }
        NPCReplacementData data;
        if (!dataList.Exists(x => x.npcToReplace == npcToReplace))
        {
            data = new NPCReplacementData(npcToReplace);
            dataList.Add(data);
        }
        else
            data = dataList.Find(x => x.npcToReplace == npcToReplace);
        if (data.npcsToUse.Exists(x => x.selection == npcToUse.selection))
        {
            NPCReplacementsPlugin.Logger.LogError($"Attempted to insert replacement NPC {npcToReplace.name} with {npcToUse.selection.name} but it already exists!");
            return;
        }
        data.npcsToUse.Add(npcToUse);
    }
    public static void AddReplacementNPC(this SceneObject sceneObject, WeightedNPC npcToUse, Character characterToReplace)
    {
        void ReplaceAllLevelObjects(NPC npcToReplace)
        {
            foreach (var ld in sceneObject.GetCustomLevelObjects())
                AddReplacementNPC(ld, npcToUse, npcToReplace);
        }
        foreach (var npc in sceneObject.potentialNPCs)
        {
            if (npc.selection.Character == characterToReplace)
                ReplaceAllLevelObjects(npc.selection);
        }
        foreach (var npc in sceneObject.forcedNpcs)
        {
            if (npc.Character == characterToReplace)
                ReplaceAllLevelObjects(npc);
        }
    }
    public static void SetReplacementNPCWeight(this CustomLevelObject ld, NPC npcToReplace, int weight)
    {
        List<NPCReplacementData> dataList = (List<NPCReplacementData>)ld.GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs");
        if (dataList == null)
        {
            NPCReplacementsPlugin.Logger.LogError($"Attempted to set weighted values but {ld.name} somehow lacks this mod value even though it should've had it in the first place!");
            return;
        }
        NPCReplacementData data;
        if (!dataList.Exists(x => x.npcToReplace == npcToReplace))
        {
            data = new NPCReplacementData(npcToReplace);
            dataList.Add(data);
        }
        else
            data = dataList.Find(x => x.npcToReplace == npcToReplace);
        data.originalWeight = weight;
    }
    internal static bool IsAvailable(this NPC npc, LevelBuilder lg)
    {
        if (lg.Ec.npcsToSpawn.Contains(npc))
        {
            List<NPCReplacementData> data = new();
            if (((CustomLevelGenerationParameters)lg.ld).GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs") != null)
                data.AddRange(((List<NPCReplacementData>)((CustomLevelGenerationParameters)lg.ld).GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs")));
            data.AddRange(lg.scene.previousLevels.SelectMany(x => (List<NPCReplacementData>)x.GetCurrentCustomLevelObject().GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs")));
            return data.Exists(d => d.npcToReplace == npc && d.npcsToUse.Count > 0);
        }
        return false;
    }
    internal static NPC ReplaceNPC(this NPC npc, LevelBuilder lg)
    {
        List<WeightedNPC> weightedList = new();
        int targetNPCWeight = -1;
        if (((CustomLevelGenerationParameters)lg.ld).GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs") != null)
        {
            List<NPCReplacementData> data = (List<NPCReplacementData>)((CustomLevelGenerationParameters)lg.ld).GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs");
            if (data.Exists(d => d.npcToReplace == npc))
                data.Do(d =>
                {
                    weightedList.AddRange(d.npcsToUse);
                    targetNPCWeight = d.originalWeight;
                });
        }
        foreach (var levelObject in lg.scene.previousLevels.Select(x => x.GetCurrentCustomLevelObject()))
        {
            if (((CustomLevelGenerationParameters)lg.ld).GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs") != null)
            {
                List<NPCReplacementData> data = (List<NPCReplacementData>)((CustomLevelGenerationParameters)lg.ld).GetCustomModValue(NPCReplacementsPlugin.PLUGIN_GUID, "replacementNPCs");
                if (data.Exists(d => d.npcToReplace == npc))
                    data.Do(d =>
                    {
                        weightedList.AddRange(d.npcsToUse);
                        if (targetNPCWeight < 0)
                            targetNPCWeight = d.originalWeight;
                    });
            }
        }
        if (weightedList.Count == 0)
            return npc; // Do not mess up the controlled rng.
        if (targetNPCWeight < 0)
            targetNPCWeight = 150;
        weightedList.Insert(0, new() { selection = npc, weight = targetNPCWeight});
        return WeightedSelection<NPC>.ControlledRandomSelectionList(WeightedNPC.Convert(weightedList), lg.controlledRNG);
    }
}

[Serializable]
public class NPCReplacementData(NPC npcToReplace)
{
    public NPC npcToReplace = npcToReplace;
    public int originalWeight = 150; // If this exists then what use does it even have other than to have a chance to be selected?
    public List<WeightedNPC> npcsToUse = new List<WeightedNPC>();
}