using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RotundWorld;

public class BPLastWishFixes
{
    public static void Patch()
    {
        //DO NOTHING
    }
    
    public static void LastWishContent()
    {
        VoidTemplate._Plugin.ModsInited = true; //YOINK~ I'LL BE TAKING THAT
        //IF YOU WANT THIS BACK, JUST DISABLE THE LOCK ON ROTUND WORLD ON YOUR MOD AND I'LL TAKE THIS OUT.

        foreach (ModManager.Mod mod in ModManager.ActiveMods)
        {
            switch (mod.id)
            {
                case "blood":
                    VoidTemplate.ModsCompatibilty.Blood.Init();
                    break;
                case "mosquitoes":
                    VoidTemplate.ModsCompatibilty.MosquitoCompat.Init();
                    break;
                case "swalloweverything":
                    throw new VoidTemplate.ModsCompatibilty.LWIncompatibleModException(mod.name);
            }
        }

        On.Player.AddFood += Player_AddFood;
        On.Player.GrabUpdate += Player_GrabUpdate;

        //SO THIS ACTUALLY WORKED I THINK, BUT WE CAN'T SKIP ORIG ON PostModsInit OR IT FORCE CRASHES...
        //_ = new Hook(typeof(VoidTemplate.ModsCompatibilty._ModsMeta).GetMethod(nameof(VoidTemplate.ModsCompatibilty._ModsMeta.PostModsInit)), VoidTemplate_PostModsInit);
    }

    private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        //VOID IS ABOUT TO SKIP OUR ENTIRE GRAB UPDATE :/ SO, HE GETS HIS OWN I GUESS.
        if (self.slugcatStats.name == VoidTemplate.VoidEnums.SlugcatID.Void)
        {
            if (!self.isNPC && !BellyPlus.VisualsOnly())
            {
                if (patch_Player.BP_GrabUpdate0(self, eu))
                    return; //WE'RE BUSY FEEDING SOMEONE!
                patch_Player.BP_GrabUpdate1(self, eu);
            }

            bool crawlFix = false;
            if (self.bodyMode == Player.BodyModeIndex.Crawl && !BellyPlus.VisualsOnly())
            {
                crawlFix = true;
                self.bodyMode = Player.BodyModeIndex.Default;
            }

            int foodFix = 0;
            if (ModManager.CoopAvailable && !BellyPlus.individualFoodEnabled && self.input[0].pckp && BellyPlus.bonusHudPip > 0 && self.abstractCreature.GetAbsBelly().myFoodInStomach == 0 && !self.isNPC)
            {
                foodFix = self.playerState.foodInStomach; //MAKE IT A SUBTRACTABLE VALUE INSTEAD OF A BOOL, SO IF WE ADDED VALUE DURING CRAFTING, WE KEEP IT
                self.playerState.foodInStomach -= foodFix; // !!! DO NOT LEAVE IT LIKE THIS!!!! IT COUNTS AS STARVING IF A PLAYER DOES NOT MEET THE THRESHOLD!!
            }


            int preFreeHand = self.FreeHand();
            orig.Invoke(self, eu);

            if (crawlFix)
                self.bodyMode = Player.BodyModeIndex.Crawl;
            if (foodFix > 0)
                self.playerState.foodInStomach += foodFix;

            if (!self.isNPC && !BellyPlus.VisualsOnly() && self.room != null)
            {
                patch_Player.BP_GrabUpdate2(self, eu);
                patch_Player.BP_GrabUpdate3(self, eu, preFreeHand);
                patch_Player.BP_GrabUpdate4(self, eu);
                patch_Player.BP_GrabUpdate6(self, eu);
            }

            patch_Player.BP_GrabUpdate5(self, eu);
        }

        else
        {
            orig.Invoke(self, eu);
        }

    }

    private void Player_JollyFoodUpdate(On.Player.orig_JollyFoodUpdate orig, Player self)
    {
        SlugcatStats.Name origClass = self.SlugCatClass;
        if (self.slugcatStats.name == VoidTemplate.VoidEnums.SlugcatID.Void)
        {
            self.SlugCatClass = SlugcatStats.Name.White;
        }
        orig(self);
        //self.SlugCatClass = origClass;
    }

    private static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
    {
        SlugcatStats.Name origName = self.slugcatStats.name;
        if (self.slugcatStats.name == VoidTemplate.VoidEnums.SlugcatID.Void)
        {
            //self.SlugCatClass = SlugcatStats.Name.White;
            self.slugcatStats.name = SlugcatStats.Name.White;
        }
        orig(self, add);
        self.slugcatStats.name = origName;
    }

    //SO THIS ACTUALLY WORKED I THINK, BUT WE CAN'T SKIP ORIG ON PostModsInit OR IT FORCE CRASHES...
    /*
    public static void VoidTemplate_PostModsInit(Action<VoidTemplate.ModsCompatibilty._ModsMeta> orig)
    {
        foreach (ModManager.Mod mod in ModManager.ActiveMods)
        {
            string id = mod.id;
            string a = id;
            if (!(a == "blood"))
            {
                if (!(a == "mosquitoes"))
                {
                    if (a == "willowwisp.bellyplus")
                    {
                        //throw new VoidTemplate.ModsCompatibilty.LWIncompatibleModException(mod.name);
                    }
                }
                else
                {
                    VoidTemplate.ModsCompatibilty.MosquitoCompat.Init();
                }
            }
            else
            {
                VoidTemplate.ModsCompatibilty.Blood.Init();
            }
        }
        // orig(self);
    }
    */

}
