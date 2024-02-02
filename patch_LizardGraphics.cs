using System;
using UnityEngine;
using RWCustom;

namespace RotundWorld;
public class patch_LizardGraphics
{
    public static void Patch()
    {
        On.LizardGraphics.DrawSprites += PG_DrawSprites;
        On.LizardGraphics.Update += LizardGraphics_Update;
		On.LizardCosmetics.JumpRings.DrawSprites += JumpRings_DrawSprites;
    }

	

	//FIX CYAN RINGS LOOKING TOO DERPY
	private static void JumpRings_DrawSprites(On.LizardCosmetics.JumpRings.orig_DrawSprites orig, LizardCosmetics.JumpRings self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (self.lGraphics.iVars.fatness > 2f)
		{
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    sLeaser.sprites[self.RingSprite(i, j, 0)].scale *= self.lGraphics.iVars.fatness;
                    sLeaser.sprites[self.RingSprite(i, j, 1)].scale *= self.lGraphics.iVars.fatness;
                }
                //Vector2 adjPos = Custom.DirVec(sLeaser.sprites[self.RingSprite(i, 0, 0)].GetPosition(), sLeaser.sprites[self.RingSprite(i, 0, 1)].GetPosition());
                //sLeaser.sprites[self.RingSprite(i, 1, 1)].x += adjPos.x * 20;
                //sLeaser.sprites[self.RingSprite(i, 1, 1)].y += adjPos.y * 20;
                //THESE GO SOMEWHERE WEIRD AT CERTAIN ANGLES I GUESS
            }
        }
    }


    //TODO! LIZARD AND MOUSE GRAPHICS NEED AN OVERHAUL SO THINGS UPDATE AT THE CORRECT TIME
    private static void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
    {
        orig(self);

        //MINI VERSION FOR VISUALS ONLY
        if (BellyPlus.VisualsOnly())
        {
            float lizFatness = self.lizard.GetBelly().myFatness;
            self.iVars.fatness = lizFatness; //NO BELLY BULGE CALCULATION HERE
            self.iVars.tailFatness = (1f - ((lizFatness - 1f) / 2f));

        }
        else
        {
            //DEAL WITH THIS IN HERE, SO IT DOESN'T RUN OFF SCREEN.
            //STRETCH OUT BASED ON STRAIN!
            float bodyStretch = Mathf.Min(self.lizard.GetBelly().boostStrain, 13f) * 1.0f;
            if (self.lizard.GetBelly().beingPushed > 0 || (patch_Player.IsVerticalStuck(self.lizard) && patch_Player.GetYFlipDirection(self.lizard) > 0))
                bodyStretch *= 0.6f;

            float fatStretch = Mathf.Max(1, self.lizard.GetBelly().myFatness - 1);
            self.headConnectionRad = 11 * self.lizard.lizardParams.headSize * fatStretch;

            self.lizard.bodyChunkConnections[0].distance = (17f * self.lizard.lizardParams.bodyLengthFac * ((self.lizard.lizardParams.bodySizeFac + 1f) / 2f)) * fatStretch + Mathf.Sqrt(bodyStretch);
            self.lizard.bodyChunkConnections[1].distance = (17f * self.lizard.lizardParams.bodyLengthFac * ((self.lizard.lizardParams.bodySizeFac + 1f) / 2f)) * fatStretch; //FOR TAILS FOR EXTRA FATTIES

            //IF WE'RE BEING PUSHED, SQUISH THE TAIL
            Lizard liz = self.lizard as Lizard;
            float tailSquish = Mathf.InverseLerp(25, 0, self.lizard.GetBelly().boostStrain);
            float bellyBulge = (self.lizard.GetBelly().inPipeStatus || !self.lizard.GetBelly().isStuck) ? 0 : Mathf.InverseLerp(0, 60, self.lizard.GetBelly().boostStrain);
            //WAIT. BODYCONNECTION 2 IS PROBABLY NOT WHAT I THINK IT IS. WHY WOULD THERE BE A 3RD CONNECTION? TO KEEP FRONT AND BACK SEPERATED PROBABLY
            float baseChunkConnSize = 17f * self.lizard.lizardParams.bodyLengthFac * ((liz.lizardParams.bodySizeFac + 1f) / 2f);
            liz.bodyChunkConnections[1].distance = baseChunkConnSize * tailSquish; //HMM, CHANGES ARE TOO ABRUPT! LETS TRY SOMETHING SNEAKY~

            if (patch_Lizard.IsStuck(self.lizard))
            {
                liz.bodyChunkConnections[0].weightSymmetry = 1f;
                liz.bodyChunkConnections[1].weightSymmetry = 0f; //OKAY THIS IS THE ONE THAT MATTERS. AND WE ONLY NEED TO SET IT ONCE I THINK
                liz.straightenOutNeeded = 1f;
                self.breath = 0; //MAYBE TO MAKE THE BODY SQUISHING MORE VISIBLE?
            }
            else
            {
                liz.bodyChunkConnections[0].weightSymmetry = 0.5f;
                liz.bodyChunkConnections[1].weightSymmetry = 0.5f;
            }

            //MOVING THE TAIL WAGGLE STUFF IN HERE SO IT DOESN'T RUN OFFSCREEN I GUESS.
            //DON'T RAISE TAIL FOR ASSISTED SQUEEZING
            //ALSO, DON'T MOVE THE TAIL AT ALL IF EXHAUSTED... IDK WHY IT SEEMS TO CAUSE ISSUES!
            if (patch_Lizard.IsStuck(self.lizard) && self.lizard.GetBelly().beingPushed < 1 && !self.lizard.GetBelly().lungsExhausted)
            {
                float wornOut = 1 - patch_Lizard.GetExhaustionMod(self.lizard, 80);

                if (!patch_Player.IsVerticalStuck(self.lizard))
                    self.bodyParts[4].vel.y += (0.7f) * patch_Player.GetYFlipDirection(self.lizard) * wornOut;
                else
                {
                    if (patch_Player.GetYFlipDirection(self.lizard) < 0)
                    {
                        self.bodyParts[5].vel.y += Mathf.Sqrt(self.lizard.GetBelly().stuckStrain / 30f) * -patch_Player.GetYFlipDirection(self.lizard) * wornOut; //TAIL OUT
                        self.bodyParts[5].vel.y += (self.lizard.GetBelly().boostStrain) * -patch_Player.GetYFlipDirection(self.lizard) * wornOut;
                    }
                    else if (patch_Player.GetYFlipDirection(self.lizard) > 0)
                    {
                        self.bodyParts[5].vel.x += (self.lizard.GetBelly().boostStrain / 2) * self.lizard.GetBelly().myFlipValX * wornOut;
                    }
                }

                //SQUISH! - IF I WERE TO GUESSS... BODY CHUNK SPRITES ARE 4,5,6
                //sLeaser.sprites[5].scaleY = 1f + 0.5f * Mathf.InverseLerp(0, 25, self.GetBelly().boostStrain);
                //THIS IS LIKE 1.5 AT MAX BOOSSTRAIN
            }


            //RESET
            if (self.lizard.GetBelly().pushingOther > 0)
                self.lizard.GetBelly().pushingOther--;

            //STOLEN FROM SLUGCAT HANDS (THEN IMPROVED)
            Player myPartner = patch_Player.FindPlayerInRange(self.lizard);
            Lizard lizardPartner = patch_Player.FindLizardInRange(self.lizard, 0, 1);

            Creature myObject = null;

            if (myPartner != null)
                myObject = (myPartner as Creature);
            else if (lizardPartner != null)
                myObject = (lizardPartner as Creature);


            if (myObject != null)
            {
                if (patch_Player.IsStuckOrWedged(myObject) || patch_Player.ObjIsPushingOther(myObject))
                {
                    //JUST PUSH. NO NEED TO REACH OUT OR ANYTHING WEIRD
                    self.lizard.GetBelly().pushingOther = 3;
                    //BUUUUT SPECIFIC SPECIES NEED SPECIFIC TYPES
                    patch_Player.ObjPushedOn(myObject);
                }
            }

            // WHAT IS A LIZARDS FATNESS...
            float lizFatness = self.lizard.GetBelly().myFatness;
            self.iVars.fatness = lizFatness * (1 + bellyBulge);
            self.iVars.tailFatness = Mathf.Max(Mathf.Min(1f, lizFatness), 0f);
            self.iVars.tailFatness = Mathf.Min(1f, 1f / Mathf.Sqrt(lizFatness));
        }
    }



    public static void PG_DrawSprites(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        bool debugBar = false;
        if (debugBar)
        {
            sLeaser.sprites[11].alpha = 1f;
            sLeaser.sprites[11].element = Futile.atlasManager.GetElementWithName("pixel");
            sLeaser.sprites[11].scale = 5f;
            //float barLen = patch_Lizard.GetExhaustionMod(self.lizard, 0f);
            float barLen = patch_Player.GetStuckPercent(self.lizard);
            sLeaser.sprites[11].scaleX = (8f * (1f - barLen) * 5f);

            //sLeaser.sprites[11].x += self.bodyParts[5].pos.x;
            //sLeaser.sprites[11].y += self.bodyParts[5].pos.y + 10;
            sLeaser.sprites[11].y += 10;
        }
    }

}