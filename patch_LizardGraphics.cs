using System;
using UnityEngine;
using RWCustom;

public class patch_LizardGraphics
{
    public static void Patch()
    {
        On.LizardGraphics.DrawSprites += PG_DrawSprites;
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

    public static void PG_DrawSprites(On.LizardGraphics.orig_DrawSprites orig, LizardGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.lizard.room == null)
            return;

        int myLizard = patch_Lizard.GetRef(self.lizard);

		//BellyPlus.myFatness[patch_Lizard.GetRef(self.lizard)] = 3f;

        //MINI VERSION FOR VISUALS ONLY
        if (BellyPlus.VisualsOnly())
		{
			float lizFatness = BellyPlus.myFatness[patch_Lizard.GetRef(self.lizard)];
			self.iVars.fatness = lizFatness; //NO BELLY BULGE CALCULATION HERE
			self.iVars.tailFatness = (1f - ((lizFatness - 1f) / 2f)); 
		
		}
		else
		{
			//DEAL WITH THIS IN HERE, SO IT DOESN'T RUN OFF SCREEN.
			//STRETCH OUT BASED ON STRAIN!
			float bodyStretch = Mathf.Min(BellyPlus.boostStrain[myLizard], 13f) * 1.0f;
			if (BellyPlus.beingPushed[myLizard] > 0 || (patch_Lizard.IsVerticalStuck(self.lizard) && patch_Lizard.GetYFlipDirection(self.lizard) > 0))
				bodyStretch *= 0.6f;

            float fatStretch = Mathf.Max(1, BellyPlus.myFatness[myLizard] - 1);
			self.headConnectionRad = 11 * self.lizard.lizardParams.headSize * fatStretch; 

            //self.lizard.bodyChunkConnections[0].distance += Mathf.Sqrt(bodyStretch);
            self.lizard.bodyChunkConnections[0].distance = (17f * self.lizard.lizardParams.bodyLengthFac * ((self.lizard.lizardParams.bodySizeFac + 1f) / 2f)) * fatStretch + Mathf.Sqrt(bodyStretch);
            self.lizard.bodyChunkConnections[1].distance = (17f * self.lizard.lizardParams.bodyLengthFac * ((self.lizard.lizardParams.bodySizeFac + 1f) / 2f)) * fatStretch; //FOR TAILS FOR EXTRA FATTIES

            //IF WE'RE BEING PUSHED, SQUISH THE TAIL
            Lizard liz = self.lizard as Lizard;
			float tailSquish = Mathf.InverseLerp(25, 0, BellyPlus.boostStrain[myLizard]);
			float bellyBulge = (BellyPlus.inPipeStatus[myLizard] || !BellyPlus.isStuck[myLizard]) ? 0 : Mathf.InverseLerp(0, 60, BellyPlus.boostStrain[myLizard]);
			//float baseChunkConnSize = 17f * ((liz.lizardParams.bodySizeFac + 1f) / 2f) * (1f + liz.lizardParams.bodyStiffnes);
			//self.bodyChunkConnections[2].distance = baseChunkConnSize * tailSquish;
			//WAIT. BODYCONNECTION 2 IS PROBABLY NOT WHAT I THINK IT IS. WHY WOULD THERE BE A 3RD CONNECTION? TO KEEP FRONT AND BACK SEPERATED PROBABLY
			float baseChunkConnSize = 17f * self.lizard.lizardParams.bodyLengthFac * ((liz.lizardParams.bodySizeFac + 1f) / 2f);
			liz.bodyChunkConnections[1].distance = baseChunkConnSize * tailSquish; //HMM, CHANGES ARE TOO ABRUPT! LETS TRY SOMETHING SNEAKY~
			//liz.bodyChunkConnections[1].distance = Mathf.Lerp(liz.bodyChunkConnections[1].distance, baseChunkConnSize * tailSquish, 0.2f);
			//liz.bodyChunkConnections[1].elasticity = 2;
			
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


			//if (patch_Lizard.IsStuck(self.lizard))
			//    liz.bodyChunkConnections[1].type = PhysicalObject.BodyChunkConnection.Type.Push;
			//else
			//    liz.bodyChunkConnections[1].type = PhysicalObject.BodyChunkConnection.Type.Normal;



			//MOVING THE TAIL WAGGLE STUFF IN HERE SO IT DOESN'T RUN OFFSCREEN I GUESS.
			//DON'T RAISE TAIL FOR ASSISTED SQUEEZING
			//ALSO, DON'T MOVE THE TAIL AT ALL IF EXHAUSTED... IDK WHY IT SEEMS TO CAUSE ISSUES!
			if (patch_Lizard.IsStuck(self.lizard) && BellyPlus.beingPushed[myLizard] < 1 && !BellyPlus.lungsExhausted[myLizard]) 
			{
				float wornOut = 1 - patch_Lizard.GetExhaustionMod(self.lizard, 80);
				
				if (!patch_Lizard.IsVerticalStuck(self.lizard)) 
					self.bodyParts[4].vel.y += (0.7f) * patch_Lizard.GetYFlipDirection(self.lizard) * wornOut;
				else
				{
					if (patch_Lizard.GetYFlipDirection(self.lizard) < 0)
					{
						self.bodyParts[5].vel.y += Mathf.Sqrt(BellyPlus.stuckStrain[myLizard] / 30f) * -patch_Lizard.GetYFlipDirection(self.lizard) * wornOut; //TAIL OUT
						self.bodyParts[5].vel.y += (BellyPlus.boostStrain[myLizard]) * -patch_Lizard.GetYFlipDirection(self.lizard) * wornOut;
					}
					else if (patch_Lizard.GetYFlipDirection(self.lizard) > 0)
					{
						self.bodyParts[5].vel.x += (BellyPlus.boostStrain[myLizard] / 2) * patch_Lizard.GetXFlipDirection(self.lizard) * wornOut;
					}
				}

				//SQUISH! - IF I WERE TO GUESSS... BODY CHUNK SPRITES ARE 4,5,6
				//sLeaser.sprites[5].scaleY = 1f + 0.5f * Mathf.InverseLerp(0, 25, BellyPlus.boostStrain[myLizard]);
				//THIS IS LIKE 1.5 AT MAX BOOSSTRAIN
			}
			

			//RESET
			BellyPlus.pushingOther[myLizard] = false;

			//STOLEN FROM SLUGCAT HANDS (THEN IMPROVED)
			Player myPartner = patch_Player.FindPlayerInRange(self.lizard);
			Lizard lizardPartner = patch_Player.FindLizardInRange(self.lizard, 0, 1);

			Creature myObject = null;

			if (myPartner != null)
				myObject = (myPartner as Creature);
			else if (lizardPartner != null)
				myObject = (lizardPartner as Creature);

			
			if (myObject != null)
				if (patch_Player.IsStuckOrWedged(myObject) || patch_Player.ObjIsPushingOther(myObject))
				{
					//JUST PUSH. NO NEED TO REACH OUT OR ANYTHING WEIRD
					patch_Lizard.PushedOther(self.lizard);
					//BUUUUT SPECIFIC SPECIES NEED SPECIFIC TYPES
					if (myObject is Player)
						patch_Player.PushedOn(myPartner);
					else if (myObject is Lizard)
						patch_Lizard.PushedOn(lizardPartner);
				}
			
			
			
			// WHAT IS A LIZARDS FATNESS...
			float lizFatness = BellyPlus.myFatness[patch_Lizard.GetRef(self.lizard)];
			self.iVars.fatness = lizFatness * (1 + bellyBulge);
            //self.iVars.tailFatness = (1f - ((lizFatness - 1f) / 2f)); // * (1 + bellyBulge/2f);
            //self.iVars.tailFatness = Mathf.Min(0.8f, lizFatness);
            self.iVars.tailFatness = Mathf.Max(Mathf.Min(1f, lizFatness), 0f);
            self.iVars.tailFatness = Mathf.Min(1f, 1f/Mathf.Sqrt(lizFatness));
            //self.iVars.tailFatness = (1f - (lizFatness - 1f) ); // * (1 + bellyBulge/2f);


            bool debugBar = false;
			if (debugBar)
			{
				sLeaser.sprites[11].alpha = 1f;
				sLeaser.sprites[11].element = Futile.atlasManager.GetElementWithName("pixel");
				sLeaser.sprites[11].scale = 5f;
				//float barLen = patch_Lizard.GetExhaustionMod(self.lizard, 0f);
				float barLen = patch_Lizard.GetStuckPercent(self.lizard);
				sLeaser.sprites[11].scaleX = (8f * (1f - barLen) * 5f);

				//sLeaser.sprites[11].x += self.bodyParts[5].pos.x;
				//sLeaser.sprites[11].y += self.bodyParts[5].pos.y + 10;
				sLeaser.sprites[11].y += 10;
			}
		}

    }

}