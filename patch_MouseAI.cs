using UnityEngine;

namespace RotundWorld;

public class patch_MouseAI
{

    public static void Patch()
    {
        On.MouseAI.Update += BP_Update;
    }

	 public static void BP_Update(On.MouseAI.orig_Update orig, MouseAI self)
    {
        orig.Invoke(self);

        //RELAAAAX A LITTLE...
        float heatFloor = 500;
        float heatCeil = 1000;
        float heatVal = Mathf.Min(Mathf.Max(self.mouse.GetBelly().myHeat - heatFloor, 0), heatCeil) / heatCeil;
        if (heatVal > 0)
        {
            self.fear *= (1f - heatVal);
        }
    }


    // public static void BPMouseAI_ctor(On.MouseAI.orig_ctor orig, MouseAI self, PhysicalObject ow)
    // {
        // orig.Invoke(self, ow);
    // }
}