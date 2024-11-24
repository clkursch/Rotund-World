using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RotundWorld;

public class patch_LanternMouse
{
	
	public static void Patch()
	{
		On.LanternMouse.ctor += (MousePatch);
		On.LanternMouse.Update += BPLanternMouse_Update;
		On.LanternMouse.SpitOutOfShortCut += LanternMouse_SpitOutOfShortCut;
		On.LanternMouse.Die += BP_Die;

        On.MouseGraphics.DrawSprites += PG_DrawSprites;
        On.MouseGraphics.Update += MouseGraphics_Update;

    }

	private static void MousePatch(On.LanternMouse.orig_ctor orig, LanternMouse self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);

		//MAKE SURE THERE ISN'T ALREADY A MOUSE WITH OUR NAME ON THIS!
		if (self.abstractCreature.GetAbsBelly().myFoodInStomach != -1)
        {
			UpdateBellySize(self);
			return;
		}

		//NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
		UnityEngine.Random.seed = self.abstractCreature.ID.RandomSeed;

        int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));

        //EXTRA RARE CHANCE FOR AN EVEN FATTER CREATURE
        int coinFlip = Mathf.FloorToInt(Mathf.Lerp(0, 5, UnityEngine.Random.value)); ////20% CHANCE TO BE TRUE
        if (critChub == 8 && coinFlip >= 4)
            critChub += 4;

        if (patch_MiscCreatures.CheckFattable(self) == false)
			critChub = 0;

		if (BPOptions.debugLogs.Value)
			Debug.Log("MOUSE SPAWNED! CHUB SIZE: " + critChub); // + " - " + coinFlip);

        self.abstractCreature.GetAbsBelly().myFoodInStomach = critChub;
		
		UpdateBellySize(self);

        if (BellyPlus.parasiticEnabled)
            BellyPlus.InitPSFoodValues(abstractCreature);
    }

	

	//I DON'T ACTUALLY KNOW IF WE CAN USE THE LIZARD VERSION FOR THIS OR NOT...
	public static IntVector2 GetMouseVector(LanternMouse self)
	{
		Vector2 mouseVec = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
		int xVec = Math.Max(Mathf.FloorToInt(mouseVec.x * 2f), -1);
		int yVec = Math.Max(Mathf.FloorToInt(mouseVec.y * 2f), -1);
		IntVector2 vector = new IntVector2(xVec, yVec);
		return vector;
	}
	
	//FIND THE NEAREST MOUSEY~
	public static LanternMouse FindMouseInRange(Creature self)
	{
		if (self.room == null)
			return null;
		
		for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
        {
            if (self.room.abstractRoom.creatures[i].realizedCreature != null
                && self.room.abstractRoom.creatures[i].realizedCreature is LanternMouse crit
                && crit != self && crit.room != null && crit.room == self.room && !crit.dead
                && Custom.DistLess(self.mainBodyChunk.pos, crit.bodyChunks[1].pos, 35f)
            )
            {
                return crit;
            }
        }

        return null;
	}


	public static void UpdateBellySize(LanternMouse self)
	{
		float baseWeight = 0.2f; //I THINK...
		int currentFood = 7 - self.abstractCreature.GetAbsBelly().myFoodInStomach;
		patch_Lizard.UpdateChubValue(self);
		
		if (BellyPlus.VisualsOnly())
			return;

		switch (Math.Max(8 - currentFood, -1))
		{
			case -1:
				self.bodyChunks[0].mass = baseWeight * 1.5f;
				self.bodyChunks[1].mass = baseWeight * 1.5f;
				break;
			case 0:
				self.bodyChunks[0].mass = baseWeight * 1.3f;
				self.bodyChunks[1].mass = baseWeight * 1.3f;
				break;
			case 1:
				self.bodyChunks[0].mass = baseWeight * 1.1f;
				self.bodyChunks[1].mass = baseWeight * 1.1f;
				break;
			case 2:
				self.bodyChunks[0].mass = baseWeight * 1f;
				self.bodyChunks[1].mass = baseWeight * 1f;
				break;
			case 3:
			default:
				self.bodyChunks[0].mass = baseWeight;
				self.bodyChunks[1].mass = baseWeight;
				break;
		}
		
	}

	//private static readonly float maxStamina = 120f;
	public static float GetExhaustionMod(LanternMouse self, float startAt)
	{
		return 0; //FOR MICE
	}


	public static bool IsStuck(LanternMouse self)
	{
		//PRESSED AGAINST AN ENTRANCE
		return self.GetBelly().isStuck;
	}

	public static bool IsVerticalStuck(LanternMouse self)
	{
		return self.GetBelly().verticalStuck;
	}

	public static void LanternMouse_SpitOutOfShortCut(On.LanternMouse.orig_SpitOutOfShortCut orig, LanternMouse self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
	{
		orig.Invoke(self, pos, newRoom, spitOutAllSticks);
		patch_Lizard.Creature_SpitOutOfShortCut(self, pos, newRoom);
	}

	public static void BP_Die(On.LanternMouse.orig_Die orig, LanternMouse self)
	{
		self.GetBelly().isStuck = false;
		orig.Invoke(self);
	}


	public static void BPUUpdatePass1(LanternMouse self)
	{
		//VERSION FOR MICE!
		if ((self.GetBelly().isStuck || self.GetBelly().pushingOther > 0) && self.runCycle > 0)
			self.runCycle = 0.8f;

		//Debug.Log("MS!-----DEBUG!: " + self.GetBelly().myFlipValX + " " + self.GetBelly().inPipeStatus + " "  + " " + self.GetBelly().stuckStrain + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);

		if (self.GetBelly().myHeat < 1250 && (self.GetBelly().assistedSqueezing || self.GetBelly().pushingOther > 0 || self.GetBelly().isStuck))
		{
			self.GetBelly().myHeat++;
			//Debug.Log("MS!-----DEBUG!: " + self.GetBelly().myHeat);
		}
		else if (self.GetBelly().myHeat > 0)
		{
			self.GetBelly().myHeat--;
		}

        //Debug.Log("MS!-----DEBUG!: " + self.AI.fear + " _ " + self.runSpeed + " _BE:" + self.AI.behavior + " _BT:" + self.GetBelly().boostCounter + " _BT:" + self.GetBelly().lungsExhausted);
        //Debug.Log("MS!-----DEBUG!: " + self.GetBelly().isStuck + " _ " + self.GetBelly().isSqueezing + " _BE:" + self.GetBelly().boostCounter + " _BT:" + self.GetBelly().pushingOther + " _BT:" + self.GetBelly().stuckStrain);

        //RECALCULATE RUN SPEED
        float myRunSpeed = (1f - (patch_Lizard.GetChubValue(self) / 12f)) * (IsStuck(self) ? 0.05f : 1f);

		if (self.graphicsModule != null && self.GetBelly().inPipeStatus)
			myRunSpeed *= patch_Player.CheckWedge(self, false);

		//FINE I GUESS
		if (self.AI.behavior == MouseAI.Behavior.Idle)
			self.runSpeed = Mathf.Min(self.runSpeed, 0.5f * myRunSpeed); //ALTERING RUN SPEED BY WEIGHT
		else if (self.AI.behavior == MouseAI.Behavior.Flee || self.AI.behavior == MouseAI.Behavior.EscapeRain)
			self.runSpeed = Mathf.Min(self.runSpeed, 1f * myRunSpeed);
	}

	
	public static void BPUUpdatePass2(LanternMouse self)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass2(self);
	}
	
	public static void BPUUpdatePass3(LanternMouse self)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass3(self);
	}
	
	public static void BPUUpdatePass4(LanternMouse self)
	{
		patch_Lizard.BPUUpdatePass4(self);
	}
	
	public static void BPUUpdatePass5(LanternMouse self)
	{
		//----- CHECK IF WE'RE PUSHING ANOTHER CREATURE.------
		if (self.GetBelly().pushingOther > 0)
		{
			LanternMouse myPartner = FindMouseInRange(self);
			if (myPartner != null)
			{
				bool horzPushLine = patch_Player.ObjIsPushingOther(myPartner) && self.GetBelly().myFlipValX == myPartner.GetBelly().myFlipValX;
				bool vertPushLine = patch_Player.ObjIsPushingOther(myPartner) && self.GetBelly().myFlipValY == myPartner.GetBelly().myFlipValY;
				bool matchingShoveDir = ((IsVerticalStuck(myPartner) || vertPushLine) && true) || ((!IsVerticalStuck(myPartner) || horzPushLine)); // && self.input[0].x == myFlipValX[GetRef(myPartner)]);
				if (!self.GetBelly().lungsExhausted && matchingShoveDir)
				{
					myPartner.GetBelly().stuckStrain += 0.5f;
				}
				//IF IT'S A PUSHING LINE, PASS FORWARD THE BENEFITS!
				if (self.GetBelly().beingPushed > 0)
					myPartner.GetBelly().stuckStrain += 0.25f;

				//CALCULATE A BOOST STRAIN MODIFIER THAT LOOKS A BIT SMOOTHER
				float pushBoostStrn = (self.GetBelly().boostStrain > 4) ? 4 : self.GetBelly().boostStrain;

				//WE NEED TWO SEPERATE FNS FOR VERTICAL/HORIZONTAL
				if (vertPushLine || IsVerticalStuck(myPartner) && matchingShoveDir)
				{
					float pushBack = 22f - Mathf.Abs(myPartner.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y) + (vertPushLine ? 10 : 0); // + (self.GetBelly().boostStrain / 5f);
					pushBack -= pushBoostStrn; // (self.GetBelly().boostStrain / 2); //BOOST STRAIN VISUALS
											   //Debug.Log("MS!---I'M PUSHING Y! LETS SHOW SOME EFFORT: " + pushBack);
					pushBack = Mathf.Max(pushBack, 0);
					pushBack *= self.GetBelly().myFlipValY;

					self.bodyChunks[0].vel.y -= pushBack;
					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.y < 0 != GetMouseVector(self).y < 0)
						self.bodyChunks[0].vel.y /= 3f;


				}
				else if (horzPushLine || !IsVerticalStuck(myPartner) && matchingShoveDir)
				{
					float pushBack = 25f - Mathf.Abs(myPartner.bodyChunks[1].pos.x - self.bodyChunks[0].pos.x) + (horzPushLine ? 10 : 0);
					pushBack -= pushBoostStrn / 1f; //(self.GetBelly().boostStrain / 2); //BOOST STRAIN VISUALS
													//Debug.Log("MS!---I'M PUSHING X! LETS SHOW SOME EFFORT: " + pushBack + " " + self.bodyChunks[0].vel.x);
					pushBack = Mathf.Max(pushBack, 0);
					pushBack *= self.GetBelly().myFlipValX;

					//IF THEYRE A TILE ABOVE US, REDUCE ALL THIS
					if (Mathf.Abs(myPartner.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y) > 10)
					{
						pushBack /= 3f;
					}

					//CHECK FOR RUNNING START!
					//NOT FOR MICE!

					self.bodyChunks[0].vel.x -= pushBack;
					self.bodyChunks[1].vel.x -= pushBack * (1.4f); // + (self.GetBelly().boostStrain / 10f));
																   //CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.x < 0 != GetMouseVector(self).x < 0) //Mathf.Abs(self.bodyChunks[0].vel.x) > 4 || 
					{
						self.bodyChunks[0].vel.x /= 3f;
						self.bodyChunks[1].vel.x /= 3f; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING
					}
				}
			}
		}
		
		if (self.GetBelly().noStuck > 0)
			self.GetBelly().noStuck--;
	}
		
		
	public static void BPUUpdatePass5_2(LanternMouse self)
	{
		//LET MICE BOOST TOO! JUST DO IT DIFFERENTLY...
		if (((self.GetBelly().boostCounter < 1 && self.GetBelly().stuckStrain > 65) || (BellyPlus.SafariJumpButton(self) && self.GetBelly().boostCounter < 10))&& !self.GetBelly().lungsExhausted && (IsStuck(self) || self.GetBelly().pushingOther > 0))
		{
			if (patch_Player.ObjIsWedged(self))
				self.GetBelly().boostStrain += 4;
			else
				self.GetBelly().boostStrain += 10;

			self.GetBelly().corridorExhaustion += 30;
			int boostAmnt = 15;
			// self.AerobicIncrease(1f);
			float strainMag = 15f * GetExhaustionMod(self, 60);
			//if (BPOptions.debugLogs.Value)
			//	Debug.Log("MS!----- MOUSE BOOSTING! ");

			//EXTRA STRAIN PARTICALS!
			
			if (self.graphicsModule != null)
			{
				for (int n = 0; n < 3 + (strainMag / 5); n++)
				{
					Vector2 pos = self.graphicsModule.bodyParts[4].pos;
					if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
					{
						self.room.AddObject(new StrainSpark(pos, self.mainBodyChunk.vel + Custom.DegToVec(180f * UnityEngine.Random.value) * 6f * UnityEngine.Random.value, 15f, Color.white));
					}
				}
			}

			self.GetBelly().boostCounter += 15 + (Mathf.FloorToInt(UnityEngine.Random.value * 8)) - (self.AI.fear > 0.4f ? 8 : 0); // - Mathf.FloorToInt(Mathf.Lerp(10, 30, self.AI.fear));

			if (IsStuck(self))
			{
				self.GetBelly().stuckStrain += boostAmnt;
				self.GetBelly().loosenProg += boostAmnt / 4000f;
			}
			else if (self.GetBelly().pushingOther > 0)
            {
				//WE'LL SKIP THE SLUGCAT'S VERSION. IT PROBABLY ISNT TOO EAGER TO HELP...
				LanternMouse mousePartner = FindMouseInRange(self);
				if (mousePartner != null) // && self.input[0].x == bellyStats[myPartner.playerState.playerNumber].myFlipValX)
				{
					//EH, ALL THEY NEED TO DO IS BE CLOSE
					mousePartner.GetBelly().stuckStrain += boostAmnt / 2;
					mousePartner.GetBelly().loosenProg += boostAmnt / 8000f;
				}
			}
		}
	}
	
	
	public static void BPUUpdatePass6(LanternMouse self)
	{
		patch_Lizard.BPUUpdatePass6(self);
	}

	public static void BPLanternMouse_Update(On.LanternMouse.orig_Update orig, LanternMouse self, bool eu)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, eu);
			return;
		}

		BPUUpdatePass1(self);
		
		orig.Invoke(self, eu);


		if (self == null || self.dead)
			return;
		
		if (self.room != null)
		{ 
			BPUUpdatePass2(self);
			BPUUpdatePass3(self);
			BPUUpdatePass4(self);
			BPUUpdatePass5(self);
			BPUUpdatePass5_2(self);
		}
		BPUUpdatePass6(self);
	}



    private static void MouseGraphics_Update(On.MouseGraphics.orig_Update orig, MouseGraphics self)
    {
        orig(self);

        if (self.mouse.room == null)
            return;

        //DEAL WITH THIS IN HERE, SO IT DOESN'T RUN OFF SCREEN.
        //self.mouse.bodyChunkConnections[0].distance = 12f + (4 + patch_Lizard.GetChubValue(self.mouse) / 4); //EW WHY DID I BUILD IT LIKE THIS
        self.mouse.bodyChunkConnections[0].distance = 12f + Mathf.Max(0, patch_Lizard.GetChubValue(self.mouse));

        //STRETCH OUT BASED ON STRAIN!
        float bodyStretch = Mathf.Min(self.mouse.GetBelly().boostStrain, 15f) * 1.5f;
        if ((patch_LanternMouse.IsVerticalStuck(self.mouse) && patch_Player.GetYFlipDirection(self.mouse) > 0))
            bodyStretch *= 0.6f;
        self.mouse.bodyChunkConnections[0].distance += Mathf.Sqrt(bodyStretch);


        //RESET
        if (self.mouse.GetBelly().pushingOther > 0)
            self.mouse.GetBelly().pushingOther--;

        //STOLEN FROM SLUGCAT HANDS
        LanternMouse myHelper = patch_LanternMouse.FindMouseInRange(self.mouse);
        if (myHelper != null && !BellyPlus.VisualsOnly())
        {
            if (patch_Player.IsStuckOrWedged(myHelper) || patch_Player.ObjIsPushingOther(myHelper))
            {
                //SPRITE ROTATION HAS TO HAPPEN IN DRAWSPRITES

                //JUST PUSH IF CLOSE ENOUGH, NO NEED TO CHECK IF ANGLE LINES UP OR ANYTHING
                patch_Player.ObjPushedOn(myHelper);
                self.mouse.GetBelly().pushingOther = 3;
            }
        }
    }


    public static void PG_DrawSprites(On.MouseGraphics.orig_DrawSprites orig, MouseGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.mouse.GetBelly().pushingOther > 0 && self.mouse.room != null)
        {
            LanternMouse myHelper = patch_LanternMouse.FindMouseInRange(self.mouse);
            if (myHelper != null && (patch_Player.IsStuckOrWedged(myHelper) || patch_Player.ObjIsPushingOther(myHelper)))
            {
                //WAIT... WHAT ABOUT THIS. OK THAT'S EASIER
                for (int l = 0; l < 2; l++)
                {
                    self.limbs[0, l].pos += (myHelper.bodyChunks[1].pos - self.mouse.bodyChunks[0].pos) / 2;
                    //ISN'T THIS BACKWARDS? BUT OK
                    sLeaser.sprites[self.LimbSprite(0, l)].rotation = Custom.AimFromOneVectorToAnother(myHelper.bodyChunks[1].pos, self.mouse.bodyChunks[0].pos);
                }
            }
        }

        //IF THEY'RE STUCK, MAYBE DON'T SHOW THEIR FORELEGS?
        if (patch_LanternMouse.IsStuck(self.mouse))
        {
            // sLeaser.sprites[this.LimbSprite(l, m)] //L:0 = FRONT LEGS. L:1 = HIND LEGS
            //sLeaser.sprites[self.LimbSprite(0, 0)].isVisible = false; 
            //sLeaser.sprites[self.LimbSprite(0, 1)].isVisible = false;
        }

        float hipScale = 0;
		float torsoScale = 0f;
        switch (patch_Lizard.GetChubValue(self.mouse))
        {
            case 0:
                hipScale = 0f;
                break;
            case 1:
                hipScale = 0f;
                break;
            case 2:
                hipScale = 2f;
                break;
            case 3:
                hipScale = 5f;
                break;
            case 4:
                hipScale = 10f + (patch_Lizard.GetOverstuffed(self.mouse) * 2f);
				torsoScale += patch_Lizard.GetOverstuffed(self.mouse) * 2f;
                break;
        }

        //OK, WE MIGHT NEED TO CHEAT A BIT WITH THE MICE...
        float stuckBonus = 1;
        if (patch_LanternMouse.IsStuck(self.mouse))
        {
            stuckBonus = 1.8f - Mathf.Min((torsoScale / 10f), 0.8f);
        }

        sLeaser.sprites[self.BodySprite(0)].scaleX = 1 + 0.03f * stuckBonus * (hipScale + torsoScale);
        //THESE REFRESH EVERY FRAME, SO JUST ADD TO THEM
        sLeaser.sprites[self.BodySprite(1)].scaleX += 0.065f * stuckBonus * hipScale;
        sLeaser.sprites[self.BodySprite(1)].scaleY += 0.04f * hipScale;
        sLeaser.sprites[self.HeadSprite].scaleX += 0.015f * hipScale;
        //Debug.Log("----SQUEAK!: " + sLeaser.sprites[self.BodySprite(1)].scaleX);


        if (self.mouse.GetBelly().lungsExhausted)
        {
            //self.blink = -15; //WE'RE EXHAUSTED! EYES CLOSED...
            self.ouchEyes = 10; //THAT'S THE WRONG EXPRESSION
        }

        else if (((patch_LanternMouse.IsStuck(self.mouse)) || patch_Player.ObjIsPushingOther(self.mouse))) // && !self.mouse.lungsExhausted)
        {
            //THIS IS ALL WE NEED
            self.ouchEyes = 10;
            if (!patch_LanternMouse.IsVerticalStuck(self.mouse))
            {
                for (int num = 15; num < 19; num++)
                {
                    sLeaser.sprites[num].rotation += 180; //FLIP THE EYES!
                }
            }
        }
    }

}