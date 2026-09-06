using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NPCReplacementsHelper;

[HarmonyPatch(typeof(LevelGenerator))]
internal class NPCReplacerPatch
{
    [HarmonyPatch(nameof(LevelGenerator.Generate), MethodType.Enumerator), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> NPCReplacementDoings(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions)
        .Start()
        .MatchForward(true,
        new(OpCodes.Ldloc_2),
        new(OpCodes.Call, AccessTools.PropertyGetter(typeof(CoreGameManager), nameof(CoreGameManager.Instance))),
        new(OpCodes.Callvirt, AccessTools.Method(typeof(CoreGameManager), nameof(CoreGameManager.Seed))),
        new(OpCodes.Ldloc_2),
        new(CodeInstruction.LoadField(typeof(LevelBuilder), nameof(LevelBuilder.seedOffset))),
        new(OpCodes.Add),
        new(OpCodes.Add, AccessTools.Constructor(typeof(System.Random), [typeof(int)])),
        new(CodeInstruction.StoreField(typeof(LevelBuilder), nameof(LevelBuilder.controlledRNG))))
        .Advance(1)
        .InsertAndAdvance(
        new(OpCodes.Ldloc_2),
        Transpilers.EmitDelegate<Action<LevelGenerator>>((lg) =>
        {
            for (int i = 0; i < lg.Ec.npcsToSpawn.Count; i++)
            {
                var npc = lg.Ec.npcsToSpawn[i];
                if (npc.IsAvailable(lg))
                    lg.Ec.npcsToSpawn[i] = npc.ReplaceNPC(lg);
            }
            lg.controlledRNG = new System.Random(CoreGameManager.Instance.Seed() + lg.seedOffset);
        })
        )
        .InstructionEnumeration();
}
