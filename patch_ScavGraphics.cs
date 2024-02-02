using UnityEngine;

namespace RotundWorld;
public class patch_ScavengerGraphics
{
    
    public static void Patch()
    {
        On.ScavengerGraphics.ctor += ScavengerGraphics_ctor;
        On.ScavengerGraphics.InitiateSprites += ScavengerGraphics_InitiateSprites;
		
		On.ScavengerGraphics.DrawSprites += PG_DrawSprites;
    }

    

    private static void ScavengerGraphics_ctor(On.ScavengerGraphics.orig_ctor orig, ScavengerGraphics self, PhysicalObject ow)
    {
        orig.Invoke(self, ow);
        float scavFatness = self.scavenger.GetBelly().myFatness;
        self.iVars.fatness *= scavFatness;
        self.iVars.narrowWaist = Mathf.Lerp(Mathf.Lerp(Random.value, 1f - self.iVars.fatness, Random.value), 1f - self.scavenger.abstractCreature.personality.energy, Random.value);
        self.iVars.neckThickness = Mathf.Lerp(Mathf.Pow(Random.value, 1.5f - self.scavenger.abstractCreature.personality.aggression), 1f - self.iVars.fatness, Random.value * 0.5f);
        self.iVars.armThickness = Mathf.Lerp(Random.value, Mathf.Lerp(self.scavenger.abstractCreature.personality.dominance, self.iVars.fatness, 0.5f), Random.value);

        self.iVars.neckThickness *= scavFatness;
        self.iVars.armThickness *= scavFatness;
        self.iVars.narrowWaist *= scavFatness;
        //sLeaser.sprites[this.NeckSprite] = TriangleMesh.MakeLongMesh(4, false, true);
    }


    private static void ScavengerGraphics_InitiateSprites(On.ScavengerGraphics.orig_InitiateSprites orig, ScavengerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig.Invoke(self, sLeaser, rCam);
        float myFat = self.scavenger.GetBelly().myFatness;
        sLeaser.sprites[self.ChestSprite].scaleX *= myFat;
        sLeaser.sprites[self.ChestSprite].scaleY *= myFat;
        sLeaser.sprites[self.HipSprite].scale *= myFat;
    }

    public static void PG_DrawSprites(On.ScavengerGraphics.orig_DrawSprites orig, ScavengerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.scavenger.room == null || BellyPlus.VisualsOnly())
            return;

        int critNum = patch_Scavenger.GetRef(self.scavenger);

        if (self.scavenger.GetBelly().pullingOther && self.scavenger.grasps[0] != null && self.scavenger.grasps[0].grabbed != null && self.scavenger.grasps[0].grabbed is Player)
        {
            Limb myHand = self.hands[0];
            myHand.mode = Limb.Mode.HuntAbsolutePosition;
            myHand.absoluteHuntPos = self.scavenger.grasps[0].grabbedChunk.pos;
            myHand.pos = self.scavenger.grasps[0].grabbedChunk.pos;
        }




        //RESET
        if (self.scavenger.GetBelly().pushingOther > 0)
            self.scavenger.GetBelly().pushingOther--;

        //STOLEN FROM SLUGCAT HANDS
        Creature myHelper = patch_Player.FindPlayerInRange(self.scavenger);
        if (myHelper == null)
            myHelper = patch_Scavenger.FindScavInRange(self.scavenger);
		if (myHelper == null)
			myHelper = patch_LanternMouse.FindMouseInRange(self.scavenger);

        if (myHelper != null)
			if (patch_Player.IsStuckOrWedged(myHelper) || patch_Player.ObjIsPushingOther(myHelper))
			{

				// for (int l = 0; l < 2; l++)
                if (UnityEngine.Random.value < 0.125f)
                {
                    self.hands[0].pos = myHelper.bodyChunks[1].pos;
                    self.hands[0].lastPos = myHelper.bodyChunks[1].pos;
                    self.hands[0].vel *= 0f;
                }
                    

                // bool vertStuck = patch_LanternMouse.IsVerticalStuck(myHelper);
                // if (!vertStuck && patch_LanternMouse.GetMouseAngle(self.scavenger).x == patch_LanternMouse.GetMouseAngle(myHelper).x
                // || (vertStuck && patch_LanternMouse.GetMouseAngle(self.scavenger).y == patch_LanternMouse.GetMouseAngle(myHelper).y))
                //FORGET THAT. JUST ALWAYS PUSH IF WE'RE CLOSE ENOUGH
                patch_Player.ObjPushedOn(myHelper);
                self.scavenger.GetBelly().pushingOther = 3;
            }
    }
}