

//RainCycle(World world, float minutes)
public class patch_RainCycle
{
    public static void Patch()
    {
        On.RainCycle.ctor += BP_RainCycle_ctor;
    }

	public static void BP_RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
	{
        float newMinutes = minutes;
		if (BPOptions.extraTime.Value && !BellyPlus.VisualsOnly())
			newMinutes *= 1.3f;
		orig.Invoke(self, world, newMinutes);
	}
	
	public static void BP_Update(On.RainCycle.orig_Update orig, RainCycle self)
	{
        if (BellyPlus.noRain && self.timer == 5000)
		{
			self.pause = 1073741823;
			self.timer++;
		}
		else
			orig.Invoke(self);
	}

}