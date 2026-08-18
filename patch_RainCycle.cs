
using UnityEngine;

namespace RotundWorld;

public class patch_RainCycle
{
    public static void Patch()
    {
        On.RainCycle.ctor += BP_RainCycle_ctor;
        On.RainCycle.Update += RainCycle_Update;
    }

    private static void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
    {
        orig(self);
        if (BPMeadowStuff.IsMeadowSandboxMode())
        {
            self.timer = 2000;
        }
    }

    public static void BP_RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
	{
        float newMinutes = minutes;
		if (BPOptions.extraTime.Value && !BellyPlus.VisualsOnly())
			newMinutes *= 1.3f + Mathf.Max(0f, (BellyPlus.BPODifficulty() / 5f));
		orig.Invoke(self, world, newMinutes);
	}

}