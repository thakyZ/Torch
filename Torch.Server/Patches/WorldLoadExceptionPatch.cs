using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Sandbox;
using Torch.Managers.PatchManager;
using Torch.Managers.PatchManager.MSIL;

namespace Torch.Patches
{
    /// <summary>
    /// Patches MySandboxGame.InitQuickLaunch to rethrow exceptions caught during session load.
    /// </summary>
    [PatchShim]
    public static class WorldLoadExceptionPatch
    {
        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MySandboxGame).GetMethod("InitQuickLaunch", BindingFlags.Instance | BindingFlags.NonPublic))
               .Transpilers.Add(typeof(WorldLoadExceptionPatch).GetMethod(nameof(Transpile), BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static IEnumerable<MsilInstruction> Transpile(IEnumerable<MsilInstruction> method)
        {
            var msil = method.ToList();
            for (var i = 0; i < msil.Count; i++)
            {
                if (msil[i].TryCatchOperations.All(x => x.Type != MsilTryCatchOperationType.BeginClauseBlock))
                    continue;

                for (; i < msil.Count; i++)
                {
                    if (msil[i].OpCode != OpCodes.Leave)
                        continue;

                    msil[i] = new MsilInstruction(OpCodes.Rethrow);
                    break;
                }
            }
            return msil;
        }
    }
}