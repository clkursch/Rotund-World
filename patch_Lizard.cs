using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using MoreSlugcats;

namespace RotundWorld;

public class patch_Lizard
{
	private static readonly bool xMove = true;
	private static readonly bool yMove = true;
	
	public static void Patch()
	{
		On.Lizard.ctor += (MainPatch);
		On.Lizard.Update += BPLizard_Update;
		On.Lizard.SpitOutOfShortCut += Lizard_SpitOutOfShortCut;
		On.Lizard.AttemptBite += Lizard_AttemptBite;
		On.Lizard.JawsSnapShut += Lizard_JawsSnapShut;
		On.Lizard.ActAnimation += BP_ActAnimation;
		On.Lizard.Collide += BP_Collide;
		On.Creature.Die += BP_Die;
	}

	private static void MainPatch(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);

		if (self.abstractCreature.GetAbsBelly().myFoodInStomach != -1)
        {
			//Debug.Log("LIZARD ALREADY EXISTS! CANCELING: ");
			UpdateBellySize(self);
			return;
		}
        
        //NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
		UnityEngine.Random.seed = self.abstractCreature.ID.RandomSeed;
		int critChub = Mathf.FloorToInt(Mathf.Lerp(2, 9, UnityEngine.Random.value));

        //EXTRA RARE CHANCE FOR AN EVEN FATTER CREATURE
        int coinFlip = Mathf.FloorToInt(Mathf.Lerp(0, 5, UnityEngine.Random.value)); ////20% CHANCE TO BE TRUE
        if (critChub == 8 && coinFlip >= 4)
            critChub += 4;

        if (self.Template.type == CreatureTemplate.Type.YellowLizard)
			critChub += 1; //BECAUSE IT'S HILARIOUS
		else if (self.Template.type == DLCSharedEnums.CreatureTemplateType.SpitLizard)
			critChub = UnityEngine.Random.Range(2, 8);
		
		if (patch_MiscCreatures.CheckFattable(self) == false)
			critChub = 0;
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("LIZARD SPAWNED! CHUB SIZE: " + critChub);
		self.abstractCreature.GetAbsBelly().myFoodInStomach = critChub;
		
		UpdateBellySize(self);

        if (BellyPlus.parasiticEnabled)
            BellyPlus.InitPSFoodValues(abstractCreature);
    }
	
	
	public static int GetRef(Creature self)
	{
		return self.abstractCreature.ID.RandomSeed;
	}

	
	public static bool IsTamed(Lizard self)
	{
		return (self.AI.friendTracker.friend != null && self.AI.friendTracker.friend is Player);
	}
	
	public static bool IsFriendInRoom(Lizard self)
	{
		if (IsTamed(self) && self.room != null)
			return (self.room == self.AI.friendTracker.friend.room);
		return false;
	}
	

	public static IntVector2 GetMouseAngle(Creature self)
	{
		Vector2 mouseVec = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
		// float xVec = Mathf.FloorToInt(vector.x + 0.5f);
		// float yVec = Mathf.FloorToInt(vector.y + 0.5f);
		IntVector2 vector = new IntVector2((mouseVec.x > 0) ? 1 : -1, (mouseVec.y > 0) ? 1 : -1);
		return vector;
	}


	public static IntVector2 GetMouseVector(Creature self)
	{
		return patch_Player.GetCreatureVector(self);
	}



	//SOUNDS THAT DON'T GET INTERRUPTED!
	public static void PlayExternalSound(Creature self, SoundID soundId, float sVol, float sPitch)
	{
		Vector2 pos = self.mainBodyChunk.pos;
		self.room.PlaySound(soundId, pos, sVol, sPitch);
	}
	
	
	//WEIGHTINESS
	public static void UpdateChubValue(Creature self)
	{
		
		int currentFood = Math.Min(self.abstractCreature.GetAbsBelly().myFoodInStomach, 8);
		
		switch (currentFood)
		{
			case 8:
				self.GetBelly().myChubValue = 4;
				break;
			case 7:
				self.GetBelly().myChubValue = 3;
				break;
			case 6:
				self.GetBelly().myChubValue = 2;
				break;
			case 5:
				self.GetBelly().myChubValue = 1;
				break;
			case 4:
				self.GetBelly().myChubValue = 0;
				break;
			case 3:
				self.GetBelly().myChubValue = -1;
				break;
			case 2:
				self.GetBelly().myChubValue = -2;
				break;
			case 1:
				self.GetBelly().myChubValue = -3;
				break;
			case 0:
			default:
				self.GetBelly().myChubValue = -4;
				break;
		}
	}
	
	
	public static float GetChubValue(Creature self)
	{
		return self.GetBelly().myChubValue; //OPTIMIZED
	}
	

	public static void UpdateBellySize(Lizard self)
	{
		int myLiz = GetRef(self);
		float baseRad = 8f * self.lizardParams.bodySizeFac * self.lizardParams.bodyRadFac;
		int currentFood = self.abstractCreature.GetAbsBelly().myFoodInStomach;

		//BIG LIZARDS NEED TO CHILL
		float chonkMod = (self.lizardParams.bodySizeFac >= 1.4f ? 0.2f : 0);

		float newChunkRad = 1f;
		switch (Math.Min(currentFood, 8))
		{
			case 8:
				newChunkRad = baseRad * 1.3f;
				self.GetBelly().myFatness = 1.5f - chonkMod + (GetOverstuffed(self) / 25f);
				break;
			case 7:
				newChunkRad = baseRad * 1.2f;
				self.GetBelly().myFatness = 1.35f - chonkMod;
				break;
			case 6:
				newChunkRad = baseRad * 1.1f;
				self.GetBelly().myFatness = 1.2f - chonkMod;
				break;
			case 5:
				newChunkRad = baseRad * 1.05f;
				self.GetBelly().myFatness = 1.1f - chonkMod;
				break;
			case 4:
				newChunkRad = baseRad * 1f;
				self.GetBelly().myFatness = 1.0f - chonkMod;
				break;
			case 3:
			default:
				newChunkRad = baseRad * 1f;
				self.GetBelly().myFatness = 0.9f - chonkMod;
				break;
		}
		

		self.bodyChunks[1].rad = newChunkRad;
        //self.bodyChunks[1].rad = Mathf.Min(newChunkRad, 10.3f); //WE CAN'T HAVE A RAD LARGER THAN 10 OR IT WILL OUTGROW PIPES!!!
        // Debug.Log("LZ!----NEW BODYCHUNK SIZE! " + self.bodyChunks[1].rad + " - " + BellyPlus.myFoodInStomach[myLiz]);

        UpdateChubValue(self);
	}
	
	
	public static int GetOverstuffed(Creature self)
    {
		int currentFood = self.abstractCreature.GetAbsBelly().myFoodInStomach;
		if (currentFood > 8)
			return currentFood - 8;
		else
			return 0;
	}
	
	

	private static readonly float maxStamina = 120f;
	public static float GetExhaustionMod(Creature self, float startAt)
	{
		float exh = self.GetBelly().corridorExhaustion;
		return Mathf.Max(0f, exh - startAt) / (maxStamina - startAt);
	}


	public static bool IsStuck(Creature self)
	{
		//PRESSED AGAINST AN ENTRANCE
		return self.GetBelly().isStuck;
	}


	public static void Lizard_SpitOutOfShortCut(On.Lizard.orig_SpitOutOfShortCut orig, Lizard self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
	{
		orig.Invoke(self, pos, newRoom, spitOutAllSticks);
		//UNIVERSAL FOR NPCS
		Creature_SpitOutOfShortCut(self, pos, newRoom);
		//LIZARDS STILL GET THIS
		self.straightenOutNeeded = 1f;
	}
	
	
	
	public static void Creature_SpitOutOfShortCut(Creature self, IntVector2 pos, Room newRoom)
	{
		
		self.GetBelly().inPipeStatus = true;
		self.GetBelly().noStuck = 0;
		self.GetBelly().boostStrain = 0;
		self.GetBelly().stuckStrain = 0;
		self.GetBelly().stuckCoords = new Vector2(0, 0);
		self.GetBelly().timeInNarrowSpace = 100; //ENOUGH TO TRIGGER THE IN-PIPE STATUS
		
		self.GetBelly().myFlipValX = newRoom.ShorcutEntranceHoleDirection(pos).x;
		self.GetBelly().myFlipValY = newRoom.ShorcutEntranceHoleDirection(pos).y;
		
		// Debug.Log("CRT!----SHORTCUT EJECT! " + critNum);
	}
	


	public static void BP_Collide(On.Lizard.orig_Collide orig, Lizard self, PhysicalObject otherObject, int myChunk, int otherChunk)
	{
		orig.Invoke(self, otherObject, myChunk, otherChunk);
		
		if (otherObject is Creature && IsStuck(self))
		{
			if (otherObject is Creature)
			{
				if (IsStuck(self) && (otherObject as Creature).shortcutDelay > 0)
				{
					patch_Player.PipeExitCollide(self, otherObject as Creature, myChunk, otherChunk);
				}
			}
		}
	}
	
	
	public static void BP_Die(On.Creature.orig_Die orig, Creature self)
	{
		
		if(!self.dead)
		{
            if (self is Lizard)
            {
                if (GetChubValue(self) == 3)
                    self.State.meatLeft = Mathf.FloorToInt(self.State.meatLeft * 1.35f);
                else if (GetChubValue(self) == 4)
                    self.State.meatLeft = Mathf.FloorToInt(self.State.meatLeft * 1.5f);
            }
            else if (self is LanternMouse)
            {
                if (GetChubValue(self) == 4)
                    self.State.meatLeft += 1;
            }
            else if (self is Scavenger)
            {
                if (GetChubValue(self) == 4)
                    self.State.meatLeft += 2;
            }
            else if (self is Vulture)
            {
                if (GetChubValue(self) == 4)
                    self.State.meatLeft = Mathf.FloorToInt(self.State.meatLeft * 1.5f);
            }
            else if (self is MirosBird && patch_MiscCreatures.GetChub(self as MirosBird) >= 4)
            {
				self.State.meatLeft = Mathf.FloorToInt(self.State.meatLeft * 1.5f);
            }
            else if (self is BigNeedleWorm)
            {
                if (patch_MiscCreatures.GetChub(self as NeedleWorm) == 4)
                    self.State.meatLeft += 2;
            }
			else if (self is Centipede && patch_MiscCreatures.GetChub(self as Centipede) == 4)
			{
				if ((self as Centipede).Centiwing) // || self.Small)
                    self.State.meatLeft += 2;  //self.abstractCreature.state.meatLeft += 2;
				else
                    self.State.meatLeft += 3;
			}
            else if (self is DropBug && self.abstractCreature.GetAbsBelly().myFoodInStomach >= 3)
            {
                self.State.meatLeft += 2;
            }
			else if (self.abstractCreature.GetAbsBelly().myFoodInStomach >= 7)
			{
				self.State.meatLeft = Mathf.CeilToInt(self.State.meatLeft * 1.5f);
            }
        }

        orig.Invoke(self);
    }
	


	public static void HaltMomentum(Lizard self)
	{
		for (int i = 0; i < self.bodyChunks.Length; i++)
		{
			self.bodyChunks[i].vel = new Vector2(0, 0);
		}
	}
	
	
	public static void Lizard_AttemptBite(On.Lizard.orig_AttemptBite orig, Lizard self, Creature creature)
	{
		orig.Invoke(self, creature);
		//HEY, NO CHEATING NOW! THAT GIVES THE BODY A CHANCE TO FORCE THROUGH
		if (patch_Player.IsStuckOrWedged(self))
			HaltMomentum(self);
	}
	
	public static void Lizard_JawsSnapShut(On.Lizard.orig_JawsSnapShut orig, Lizard self, Vector2 pos)
	{
		orig.Invoke(self, pos);
		//HEY, NO CHEATING NOW! THAT GIVES THE BODY A CHANCE TO FORCE THROUGH
		if (patch_Player.IsStuckOrWedged(self))
			HaltMomentum(self);
	}
	
	
	
	public static float BP_ActAnimation(On.Lizard.orig_ActAnimation orig, Lizard self)
	{
		float orignal =  orig.Invoke(self);
		//SPITTING WHILE STUCK IS TOO WIGGLY!! UNDO MOMENTUM AND POSITION CHANGES IF WE SPIT
		if (self.animation == Lizard.Animation.Spit && patch_Player.IsStuckOrWedged(self))
		{
			Vector2? vector = self.AI.redSpitAI.AimPos();
			if (vector != null)
			{
				if (self.AI.redSpitAI.AtSpitPos)
				{
					HaltMomentum(self);
				}
				if (!self.AI.UnpleasantFallRisk(self.room.GetTilePosition(self.mainBodyChunk.pos)))
				{
					HaltMomentum(self);
				}
				if (self.AI.redSpitAI.delay < 1)
				{
					Vector2 vector3 = self.bodyChunks[0].pos + Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos) * 10f;
					Vector2 vector4 = Custom.DirVec(vector3, vector.Value);
					if (Vector2.Dot(vector4, Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos)) > 0.3f || self.safariControlled)
					{
						self.bodyChunks[2].pos += vector4 * 8f;
						self.bodyChunks[1].pos += vector4 * 4f;
						HaltMomentum(self);
					}
				}
			}
		}
		return orignal;
	}




	private static float crashVel = 0f;
	public static void CheckStuckage(Creature self)
	{
		
		bool inPipe = self.GetBelly().inPipeStatus;
		float posMod = inPipe ? 0.5f : 0f;

		//CHECK FOR GRACE PERIOD
		if (self.GetBelly().noStuck > 0)
		{
			self.GetBelly().noStuck--;
			self.GetBelly().isStuck = false;
			self.GetBelly().verticalStuck = false;
			//Debug.Log("LZ!----NO STUCKS ALLOWED! ");
			return;
		}

		if (self.room == null || self.graphicsModule == null) //MOUSE SPECIFIC
			return;

		//AREA SLIGHTLY IN FRONT OF HIPS
		float myxF = (0.5f + posMod) * self.GetBelly().myFlipValX; //NOO DON'T ADD OUR INPUT
		float myxB = (-0.0f + posMod) * self.GetBelly().myFlipValX; //AREA SLIGHTLY BEHIND HIPS
																		  //FOR THE Y VERSION
		float myyF = (0.5f + posMod) * self.GetBelly().myFlipValY; //AREA SLIGHTLY IN FRONT OF HIPS
		float myyB = (-0.0f + posMod) * self.GetBelly().myFlipValY; //AREA SLIGHTLY BEHIND HIPS													


		//DON'T GET CONFUSED, THESE ARE BOTH FOR THE BOTTOM CHUNK. BUT IT'S CHECKING FOR THE FRONT/REAR OF THE BOTTOM CHUNK
		bool frontInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, myxF, 0);
		bool rearInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, myxB, 0);

		bool vertFrontInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, 0, myyF);
		bool vertRearInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, 0, myyB);

		//USE VERTICAL STUCK CHECKS TO SET THIS ONE, SINCE WE CAN BE HORIZONTAL STUCK WHILE STANDING UP
		bool isVertical = false;

		//THIS MIGHT FIX IT; IF WE'RE VERTICAL STUCK, STAY THAT WAY UNTIL WE ARE NOT, REGARDLESS OF OUR ANGLE
		// if (self.GetBelly().verticalStuck) //ACTUALLY, THIS MIGHT BE CAUSING ISSUES. LETS TRY REVERTING THIS
			// isVertical = true;
		if (inPipe)
			isVertical = Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) < Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y);
		else
			isVertical = (vertFrontInCorridor != vertRearInCorridor);
		
		bool topInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 0, 0f, self.GetBelly().myFlipValY);
		bool bottomInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, 0f, 0f);
		bool pipeTwisting = topInCorridor && bottomInCorridor; // && self.backwardsCounter <= 0; //TO PREVENT FALSE JAMS FROM BENDS IN PIPES

		bool wedgedInFront = (
			!isVertical &&
			((!inPipe && (frontInCorridor && !rearInCorridor))
			|| (inPipe && (!frontInCorridor && rearInCorridor) && !pipeTwisting))
			);

		bool wedgedBehind = false; //DOESN'T MATTER

		bool vertStuck = (isVertical &&
			((!inPipe && (vertFrontInCorridor && !vertRearInCorridor))
			|| (inPipe && (!vertFrontInCorridor && vertRearInCorridor) && !pipeTwisting))
			);

		// Vector2 myRearChunkCheck = self.bodyChunks[1].pos + new Vector2(0 * 20, myyB * 20);
		// Debug.Log("LZ! REAR CHUNK CHECK!! " + myRearChunkCheck + "----LZ! VERSTUCK?? " + inPipe + " 1vertFrontInCorridor:" + vertFrontInCorridor + " vertRearInCorridor:" + vertRearInCorridor + " pipeTwisting:" + pipeTwisting + " topInCorridor:" + topInCorridor + " bottomInCorridor:" + bottomInCorridor);

		//QUICK CHECK TO MAKE SURE LIZARD LUNGES ARENT GETTING CAUGHT ON FLOOR PIPES
		if (vertStuck && !inPipe && self is Lizard && (self as Lizard).animation == Lizard.Animation.Lounge)
			vertStuck = false; //NO VERSTUCKING



		if (self.GetBelly().fwumpDelay > 0)
			self.GetBelly().fwumpDelay--;

		//IF WE'RE NOT STUCK, RETURN
		if (!wedgedInFront && !wedgedBehind && !vertStuck)
		{
			
			//IF WE JUST CHEATED THE STUCK STRAIN, MAKE AN ATTEMPT TO POP US BACK INTO PLACE!
			// int critNum = [critNum]
			bool backedOut = false;
			if (self.GetBelly().isStuck && self.GetBelly().stuckStrain > 0 && self.GetBelly().stuckCoords != new Vector2(0, 0))
			{

				Vector2 newCoords = self.GetBelly().stuckCoords;
				float nudge = inPipe ? 5 : -5f;
				bool isClipping = self.IsTileSolid(1, 0, 0); //OKAY WEIRDO. WE NEED TO CHECK TO MAKE SURE OUR UNSTICK CAME FROM PHASING INTO THE WALL :/
				if (!self.GetBelly().verticalStuck)
				{
					wedgedInFront = true;
					if(self.GetBelly().myFlipValX == 1 && self.bodyChunks[1].pos.x > self.GetBelly().stuckCoords.x || isClipping)
						newCoords = new Vector2(newCoords.x - nudge, newCoords.y);
					else if (self.GetBelly().myFlipValX == -1 && self.bodyChunks[1].pos.x < self.GetBelly().stuckCoords.x || isClipping)
						newCoords = new Vector2(newCoords.x + nudge, newCoords.y);
					else
						backedOut = true;
					// self.bodyChunks[0].pos = newCoords + new Vector2(5 * self.GetBelly().myFlipValX, 0);
					self.bodyChunks[0].pos.y = newCoords.y;
				}
				else
				{
					vertStuck = true;
					if(self.GetBelly().myFlipValY == 1 && self.bodyChunks[1].pos.y > self.GetBelly().stuckCoords.y || isClipping)
						newCoords = new Vector2(newCoords.x, newCoords.y - nudge);
					else if (self.GetBelly().myFlipValY == -1 && self.bodyChunks[1].pos.y < self.GetBelly().stuckCoords.y || isClipping)
						newCoords = new Vector2(newCoords.x, newCoords.y + nudge);
					else
						backedOut = true;
					self.bodyChunks[0].pos.x = newCoords.x;
				}
					
				if (backedOut)
				{
					// Debug.Log("LZ! WE JUST BACKED OUT! NOTHING SPECIAL " + self.bodyChunks[1].pos + " STUCK COORDS:" + self.GetBelly().stuckCoords + " FLIPDIR:" + self.GetBelly().myFlipValX + "," + self.GetBelly().myFlipValY + " VERTSTK:" + self.GetBelly().verticalStuck + self.bodyChunks[0].terrainSqueeze);
					self.GetBelly().isStuck = false;
					self.GetBelly().stuckInShortcut = false;
					self.GetBelly().verticalStuck = false;
					self.GetBelly().stuckCoords = new Vector2(0, 0);
					return;
				}
				else
				{
					float stretchMag = (self.bodyChunks[1].vel.magnitude / 1f);
					// Debug.Log("LZ! REDIRECTING TO STUCK COORDS! " + self.GetBelly().stuckCoords + " CURRENT:" + self.bodyChunks[1].pos + " ADJST:" + newCoords + " STRETCH:" + stretchMag + " CLIPPING" + isClipping); // + " BODYCOORDS:" + self.bodyChunks[2].pos);
					
					self.bodyChunks[1].HardSetPosition(newCoords);
					
					//OKAY, HOW BAD ARE WE STRETCHING? INCREASE STUCKSTRAIN BASED ON THAT.
					self.GetBelly().stuckStrain += 5 + stretchMag; //MAYBE HELP

                    //IT KEEPS TRYING TO SUCK THEIR TAILS IN FIRST IF THEY JUMP AHEAD. DONT LET THAT HAPPEN.
                    for (int i = 0; i < self.bodyChunks.Length; i++)
                    {
                        //self.bodyChunks[i].pos = newCoords + patch_Player.GetCreatureVector(self).ToVector2() * 20 * (1-i);
                        self.bodyChunks[i].HardSetPosition(newCoords + patch_Player.GetCreatureVector(self).ToVector2() * 20 * (1 - i));
                    }

                    //OR MAYBE ITS JUST THIS... OKAY IT WAS, LOL. OKAY BUT I GUESS WE ALSO NEED THE REPOSITIONING TOO
                    self.enteringShortCut = null;

					if (self is Lizard)
						(self as Lizard).bodyWiggleCounter = 0;

					for (int i = 0; i < self.bodyChunks.Length; i++)
					{
						self.bodyChunks[i].vel = new Vector2(0, 0);
					}
					// Debug.Log("LZ!FWOMP!! EJECTED " + inPipe + "-" + self.GetBelly().boostStrain);
					PlayExternalSound(self, BellyPlus.Fwump1, 0.03f, 1f);
					// self.Stun(20); //I THINK THIS MAKES THEM DROP THINGS
					self.GetBelly().boostStrain /= 2; //MAYBE THIS WILL HELP THE REPEATING POPS
				}
			}
			
			else
			{
				//Debug.Log("LZ! NOT STUCK... " + inPipe + " - " + self.GetBelly().stuckCoords);
				if (self.GetBelly().stuckStrain > 0)
					self.GetBelly().stuckStrain = Mathf.Max(0, self.GetBelly().stuckStrain - 2f);
				else
                {
					self.GetBelly().isStuck = false;
					self.GetBelly().stuckInShortcut = false;
					self.GetBelly().stuckCoords = new Vector2(0, 0);
				}
				return; // NORMAL CASE OF NOT BEING STUCK
			}
		}

		//DETERMINES THE VALUE THEY MUST PASS IN ORDER TO SLIDE THROUGH
		Vector2 tilePosMod = new Vector2(vertStuck ? 0 : (2f * posMod * self.GetBelly().myFlipValX), vertStuck ? (2f * posMod) * self.GetBelly().myFlipValY : 0);
		// 60 * ((GetChubValue(self) - 1)
		
		//BIG LIZORDS GET TREATED AS BIGGER
		int tileSizeMod = 0;
		float sizeMod = 0;
        float naturalChub = 0;
		if (self is Lizard)
		{
			float mySize = (self as Lizard).lizardParams.bodySizeFac * (self as Lizard).lizardParams.bodyRadFac;

			//SOME SPECIFICS FOR SOME VANILLAS
			if (self.Template.type == CreatureTemplate.Type.GreenLizard)
                sizeMod = 1.5f;
			else if (mySize >= 2) //HYUUUGE BOIS I GUESS
			{
                naturalChub = 2;
                sizeMod = 4;
            }
            else if (mySize >= 1.5) //CARAMELS & LARGER
            {
                naturalChub = 2;
                sizeMod = 2;
            }
            else if (mySize >= 1.3) //TRAINS & LARGER
                sizeMod = 2;
            else if (mySize >= 1.1) //REDS & LARGER
                sizeMod = 1;
            else if (mySize >= 0.9) //BLUES AND LARGER
                sizeMod = 0;
            else if (mySize >= 0.75) //DUNNO, SOME MOD LIZARS OR SOMETHING
                sizeMod = -1;
            else if (mySize >= 0.5) //EELS/ZOOP & LARGER
                sizeMod = -2;
            else if (mySize < 0.5) //MODDED BABBS
                sizeMod = -2.5f;
		}
		

		//ALRIGHT FINE IM DOING IT MANUALLY
		float myChub = 0;
		switch (GetChubValue(self))
		{
			case 4:
				myChub = 8.5f; //9.5
				break;
			case 3:
				myChub = 7.0f;
				break;
			case 2:
				myChub = 5 + naturalChub;
				break;
			case 1:
				myChub = 2 + naturalChub * 2;
				break;
			default:
				myChub = 0 + naturalChub * 2;
				break;
		}
		myChub += sizeMod;
		if (self is Lizard)
		{
			myChub += (GetOverstuffed(self) / 4f);
			//TAMED LIZAR LENIENCY
			if (IsTamed(self as Lizard) && myChub >= 7 && !BPOptions.hardMode.Value)
			{
				myChub = 7f + ((myChub-7f) / 2f);
			}
		}

		//SLIGHTLY LESS CHUNKY CRITTERS
		if (self is Cicada || self is Scavenger)
		{
			myChub = Mathf.Max(0, myChub - 0.5f);
		}
		
		self.GetBelly().tileTightnessMod = 30 * (myChub + patch_Player.GetTileSizeMod(self, tilePosMod, GetMouseVector(self), tileSizeMod, inPipe, self.Submersion > 0, false));
		float squeezeThresh = self.GetBelly().tileTightnessMod;

		//IF OUR RESULT TURNS OUT TO BE 0 ANYWAYS, CANCEL THE STUCK
		if (squeezeThresh <= 0)
		{
			self.GetBelly().noStuck = 30;
			if (squeezeThresh == 0 || squeezeThresh == -0.5f) //IF IT WAS EXACTLY OUR SIZE, PLAY A FUNNY SOUND
			{
				self.room.PlaySound(BellyPlus.Squinch1, self.mainBodyChunk, false, 0.1f, 1f - GetChubValue(self) / 10f);
				//BellyPlus.shortStuck[critNum] = 5; //TO ACCOMPANY THE SQUINCH~
				// Debug.Log("LZ!SQUINCH " + inPipe);
			}
			return;
		}


		//WE JUST NOW GOT STUCK --- PREPARE STUCK BWOMP!
		if (!self.GetBelly().isStuck && (vertStuck || wedgedInFront || wedgedBehind))
		{
			crashVel = self.bodyChunks[1].vel.magnitude;
			// Debug.Log("LZ!PRE-PLUG VELOCITY! " + crashVel);
			if (self is Lizard && UnityEngine.Random.value < 0.5f)
				(self as Lizard).voice.MakeSound(LizardVoice.Emotion.Submission);
			
			
			//STUCK VECTOR
			//ACTUALLY, LIZARDS MIGHT NOT NEED THIS LIKE WE DO. WE'LL SEE
			if (isVertical)
			{
				int flipper = (self.bodyChunks[1].vel.y > 0) ? 1 : -1; 
				self.GetBelly().stuckVector = new Vector2(0, flipper);
				crashVel = Mathf.Abs((self.bodyChunks[1].vel + (new Vector2(0, self.gravity) * 6f)).y) * 0.8f;
			}
			else
			{
				int flipper = (self.bodyChunks[1].vel.x > 0) ? 1 : -1; 
				self.GetBelly().stuckVector = new Vector2(flipper, 0);
				crashVel = Mathf.Abs((self.bodyChunks[1].vel).x);
			}
			

			self.GetBelly().stuckCoords = self.room.MiddleOfTile(self.bodyChunks[1].pos);
			
			//KEEP TRACK OF IF WE WERE COMING OUT OF A SHORTCUT
			if (self.shortcutDelay > 0)//(BellyPlus.freshFromShortcut[critNum] > 0)
				self.GetBelly().stuckInShortcut = true;
			
			//LIZARD LOUNGING FWUMP
			if (self is Lizard && (self as Lizard).animation == Lizard.Animation.Lounge)
			{
				(self as Lizard).EnterAnimation(Lizard.Animation.Standard, true);
				patch_Player.ObjGainBoostStrain(self, 0, 12, 16);
				patch_Player.ObjGainStuckStrain(self, 60f);
				patch_Player.MakeSparks(self, 1, 8);
				crashVel = Mathf.Max(crashVel, 8f);
			}

			if (crashVel > 15f) //AT THIS SPEED WE'LL BREACH THE ENTRANCE EVEN WITH THE REDUCED VEL. 
				PlayExternalSound(self, BellyPlus.Squinch1, 0.2f, 1.3f);

			if (crashVel > 8f) //WE'RE GOING PRETTY FAST! CUT OUR VEL IN HALF
			{
				PlayExternalSound(self, BellyPlus.Fwump1, 0.12f, 1f);
				for (int i = 0; i < self.bodyChunks.Length; i++)
				{
					self.bodyChunks[i].vel *= 0.2f;
				}
				//Debug.Log("LZ!WO-HOA THERE COWBOY! " + crashVel + " LOUNGING?" + (self.animation == Lizard.Animation.Lounge));
			}
			

			//LIZARDS ALWAYS DO THIS
			self.GetBelly().fwumpDelay = 4;
			//BellyPlus.terrSqzMemory[critNum] = self.bodyChunks[0].terrainSqueeze; //WE NEED FAT LIZARDS TO REMEMBER THIS SO THAT THEIR TERRAIN SQUEEZE DOESN'T REVERT TO 0 AND SEND THEM FLYING BACK
			if (BPOptions.debugLogs.Value)
				Debug.Log("NEW CRITTER STUCK VECTOR! " + self.GetBelly().stuckVector + " SIZEMOD " + sizeMod );
			patch_Player.GetTileSizeMod(self, tilePosMod, GetMouseVector(self), tileSizeMod, inPipe, self.Submersion > 0, true); //	JUST FOR LOGS
		}


		//STUCK BWOMP!
		if (self.GetBelly().fwumpDelay == 1)
        {
			float velMag = 0.0f + Mathf.Sqrt(crashVel * 2f);
			float vol = Mathf.Min((velMag / 5f), 0.25f);
			self.room.PlaySound(BellyPlus.Fwump2, self.mainBodyChunk, false, vol, 1.1f);
			// Debug.Log("LZ!-----BWOMP! JUST GOT STUCK " + velMag);

			for (int n = 0; n < 3; n++) //STRAIN DRIPS
			{
				Vector2 pos3 = self.bodyChunks[0].pos;
				float xvel = 4;
				self.room.AddObject(new WaterDrip(pos3, new Vector2((float)self.GetBelly().myFlipValX * xvel, Mathf.Lerp(-2f, 6f, UnityEngine.Random.value)), false));
			}
		}
		

		//FROM THE DELAYED SHOVE
		if (self.GetBelly().fwumpDelay == 8)
		{
			self.GetBelly().stuckStrain += 60f;
			self.GetBelly().fwumpDelay = 0;
		}


		//ASSISTED SQUEEZES VISUAL BOOST
		if (self.GetBelly().assistedSqueezing)
		{
			if (self.GetBelly().boostStrain < 8)
				self.GetBelly().boostStrain += 2;
			//WE NEED TI IMPOSE SOME SORT OF LIMIT ON THIS...
			self.GetBelly().boostStrain = Math.Min(self.GetBelly().boostStrain, 18);
		}



		if (wedgedInFront || wedgedBehind)
		{
			self.GetBelly().isStuck = true;
			self.GetBelly().verticalStuck = false;
			float tileCheckOffset = (inPipe ? 0 : 10f) * self.GetBelly().myFlipValX; //WELP, NOW IT WORKS WELL
			float pushBack = (self.room.MiddleOfTile(self.bodyChunks[1].pos).x + tileCheckOffset - self.bodyChunks[1].pos.x); // * self.GetBelly().myFlipValX;
			pushBack = (pushBack - (((xMove && !self.GetBelly().lungsExhausted) ? 9.0f : 7.5f) * self.GetBelly().myFlipValX)) * 1.0f;
			//Debug.Log("LZ!-----SQUEEZED AGAINST AN X ENTRANCE!: " + self.GetBelly().myFlipValX + " " + self.GetBelly().inPipeStatus + " " + pushBack + " " + " ORIG VELOCITY " + self.bodyChunks[1].vel.x + " FORCE APPLIED: " + (pushBack + (self.GetBelly().boostStrain * self.GetBelly().myFlipValX / 5f)));

			if (Math.Abs(pushBack) > 15f)
				pushBack = 0; //SOMETHINGS GONE HORRIBLY WRONG


			if (xMove || self.GetBelly().assistedSqueezing)
			{
				//MAKE PROGRESS AS WE STRAIN. SELF STRUGGLING AND ASSISTED STRUGGLING CAN STACK
				if (xMove && !self.GetBelly().lungsExhausted)
					self.GetBelly().stuckStrain += 2f;
				//OUR PUSHERS STRAIN WILL BE ADED ELSEWHERE, WHERE WE CAN CHECK THAT THEY ARENT EXHAUSTED FIRST

				if (self.GetBelly().stuckStrain < squeezeThresh)
				{
					//NOW, DO WE WANT TO SLOW DOWN OUR MOVEMENT SPEED? OR OUR PHYSICAL VELOCITY?...
					//IF WE DO CORRCLIMBSPEED, PUT THIS IN UPDATEBODYMODE. IF WE DO VELOCITY, PUT IT AT THE END OF NORMAL UPDATE, SO IT UPDATES IF PLAYERS GRAB US
					float wornOut = 1 - GetExhaustionMod(self, 80);
					
					//MOVED TAIL WAGGLE INTO THE GRAPHICS MODULE
					
					//MICE ONLY
					if (self is LanternMouse && !self.GetBelly().lungsExhausted && self.graphicsModule != null)
					{
						//NO TAIL WAGGLE. MICE GOT TEENY TAILS
						self.bodyChunks[0].vel.y = 0;
						//RE-ALIGN THE BODY CHUNKS (MOUSE SPECIFIC)
						if (self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y)
							self.bodyChunks[0].vel.y += 1f * wornOut;
						self.graphicsModule.bodyParts[4].vel.y += (0.1f) * self.GetBelly().myFlipValY * wornOut; //I THINK THIS IS TOO MUCH
					}

					//LIZ TAILS
					if (self is Lizard && self.GetBelly().beingPushed > 0)
					{
						self.bodyChunks[2].pos.y = self.bodyChunks[1].pos.y;
					}
				}
				else
				{
					if (BellyPlus.isMeadowSession)
					{
						patch_Player.MeadowPopFree(self, self.GetBelly().stuckStrain, self.GetBelly().inPipeStatus);
					}
					else
					{
                        PopFree(self, self.GetBelly().stuckStrain, self.GetBelly().inPipeStatus);
                        pushBack = 0;
                    }
				}
			}
			else
			{
				self.GetBelly().stuckStrain = 0;
			}

			//OK WE NEED A FORMULA WHERE THAT, WHEN OUR X >= 9.5 FROM MID, VOL APPROACHES 0
			self.bodyChunks[1].vel.x += pushBack + (self.GetBelly().boostStrain * self.GetBelly().myFlipValX / 5f);
			if (self is Lizard)
					self.bodyChunks[2].vel.x += pushBack + (self.GetBelly().boostStrain * self.GetBelly().myFlipValX / 5f);
			//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
			if (self.bodyChunks[1].vel.x < 0 != self.GetBelly().stuckVector.x < 0)
				self.bodyChunks[1].vel.x /= 4; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING

		}


		//VERTICAL SQUEEZES
		else if (vertStuck)
		{
			self.GetBelly().isStuck = true;
			self.GetBelly().verticalStuck = true;
			float tileCheckOffset = (inPipe ? 0 : 10f) * self.GetBelly().myFlipValY; //WELP, NOW IT WORKS WELL
			float pushBack = (self.room.MiddleOfTile(self.bodyChunks[1].pos).y + tileCheckOffset - self.bodyChunks[1].pos.y); // * self.GetBelly().myFlipValY;
			pushBack = (pushBack - ((yMove ? 9.5f : 7.5f) * self.GetBelly().myFlipValY)) * 1.0f;
			//Debug.Log("LZ!-----SQUEEZED AGAINST AN Y ENTRANCE!: " + " " + self.GetBelly().inPipeStatus + " - " + pushBack + " YFLIP:" + self.GetBelly().myFlipValY + " - " + self.GetBelly().stuckStrain + " " + self.room.MiddleOfTile(self.bodyChunks[1].pos).y + " " + self.bodyChunks[1].pos.y);

			if (yMove || self.GetBelly().assistedSqueezing)
			{
				//MAKE PROGRESS AS WE STRAIN. SELF STRUGGLING AND ASSISTED STRUGGLING CAN STACK
				if (yMove && !self.GetBelly().lungsExhausted)
					self.GetBelly().stuckStrain += 2;

				if (self.GetBelly().stuckStrain < squeezeThresh)
				{
					//MOVED TAIL WAGGLE TO GRAPHICS MODULE
					
					//MICE ONLY
					if (self is LanternMouse && !self.GetBelly().lungsExhausted && self.graphicsModule != null)
					{
						float wornOut = 1 - GetExhaustionMod(self, 60);
						if (self.GetBelly().beingPushed < 1 && self.GetBelly().myFlipValY < 0)
						{
							self.graphicsModule.bodyParts[5].vel.y += Mathf.Sqrt(self.GetBelly().stuckStrain / 30f) * -self.GetBelly().myFlipValY * wornOut; //TAIL OUT
							self.graphicsModule.bodyParts[5].vel.y += (self.GetBelly().boostStrain) * -self.GetBelly().myFlipValY * wornOut;
						}
						else if (self.GetBelly().myFlipValY > 0)
						{
							self.graphicsModule.bodyParts[5].vel.x += (self.GetBelly().boostStrain / 2) * self.GetBelly().myFlipValX * wornOut;
							if (inPipe)
								self.bodyChunks[0].pos.x = self.bodyChunks[1].pos.x; //KEEP THE HEAD LEVEL SO IT DOESNT SQUEEZE OUT DIAGONALLY
						}
						self.graphicsModule.bodyParts[4].vel.y += (Mathf.Min(Mathf.Sqrt(self.GetBelly().stuckStrain / 30f), 6f) + (self.GetBelly().boostStrain / 2f)) * self.GetBelly().myFlipValY * wornOut; //SNOUT OUT
					}


					//LIZ TAILS
					if (self is Lizard && self.GetBelly().beingPushed > 0)
					{
						self.bodyChunks[2].pos.x = self.bodyChunks[1].pos.x;
					}
				}
				else
				{
					//HIGHER BOOST VALUE IF LAUNCHING UPWARDS
					float boostVel = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) ? self.GetBelly().stuckStrain : self.GetBelly().stuckStrain / 2f;
					if (BellyPlus.isMeadowSession)
					{
						patch_Player.MeadowPopFree(self, boostVel, self.GetBelly().inPipeStatus);
					}
					else
					{
                        PopFree(self, boostVel, self.GetBelly().inPipeStatus);
                        pushBack = 0;
                    }
				}
			}
			else
			{
				self.GetBelly().stuckStrain = 0;
			}

			//OK WE NEED A FORMULA WHERE THAT, WHEN OUR X >= 9.5 FROM MID, VOL APPROACHES 0
			self.bodyChunks[1].vel.y += pushBack + (self.GetBelly().boostStrain * self.GetBelly().myFlipValY / 6f);
			if (self is Lizard)
				self.bodyChunks[2].vel.y += pushBack + (self.GetBelly().boostStrain * self.GetBelly().myFlipValY / 6f);
			//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
			if (self.bodyChunks[1].vel.y < 0 != self.GetBelly().stuckVector.y < 0)
				self.bodyChunks[1].vel.y /= 4; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING
		}
		


		//------REDUCE STUCK STRAIN BASED ON THE FORMULA F(x) = (X(A-C))/500---------
		if (self.GetBelly().stuckStrain > 0)
		{
			//A NEW VAR TO TRACK LOOSENING PROGRESS AS STRAINING CONTINUES
			self.GetBelly().loosenProg += Mathf.Sqrt(Mathf.Max(Mathf.Min(self.GetBelly().stuckStrain, 250) - 180f, 0)) / 1000f; //LIZARDS LOOSEN MUCH FASTER
			//float counterStrain = (self.GetBelly().stuckStrain * GetChubValue(self)) / 700f;
			float counterStrain = (self.GetBelly().stuckStrain * ((self.GetBelly().tileTightnessMod / 30) - self.GetBelly().loosenProg)) / 500f; //500f
			//Debug.Log("LZ!--------COUNTER STRAIN!: " + counterStrain + " TIGHTNESS: " + (self.GetBelly().tileTightnessMod / 30) + " - " + (self.GetBelly().tileTightnessMod) + " " + "STRAIN: " + self.GetBelly().stuckStrain + "  EXHAUSTION:" + self.GetBelly().corridorExhaustion + "  PROGRESS:" + self.GetBelly().loosenProg + "  BOOST:" + self.GetBelly().boostStrain);
			self.GetBelly().stuckStrain = Mathf.Max(0, self.GetBelly().stuckStrain - counterStrain);
		}
	}




	public static void BPUUpdatePass1(Lizard self)
	{
		//Debug.Log("LZ!-----DEBUG!: " + self.GetBelly().myFlipValX + " " + self.GetBelly().inPipeStatus + " "  + " " + self.GetBelly().stuckStrain + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);
		float myRunSpeed = (1f - (GetChubValue(self) / 10f) - Mathf.Lerp(0f, 0.2f, (GetOverstuffed(self) / 12f))) * ((IsStuck(self) || self.GetBelly().pushingOther > 0) ? 0.03f : 1f);
		//1.0 - 0.6 at chub 4.   0.4 at 12 overstuffed

		//11-28-22 CAN WE MAKE THESE GUYS WEDGE TOO? ONLY IF ON SCREEN, TO SAVE RESOURCES
		if (self.graphicsModule != null && self.GetBelly().inPipeStatus)
			myRunSpeed *= patch_Player.CheckWedge(self, false);
		
		self.AI.runSpeed = Mathf.Min(self.AI.runSpeed, 1f * myRunSpeed);
		
		//DISABLE THIS BODY CHUNK WHEN STUCK IN SHORTCUT (IDK IF THIS ACTUALLY HELPS...)
		self.bodyChunkConnections[2].active = !self.GetBelly().stuckInShortcut;

        //HEY OUR AI DOESN'T RUN WHILE GRABBED! WE HAVE TO RUN THIS HERE
        if (!BellyPlus.ridableLizEnabled && self.grabbedBy.Count > 0 && (self.grabbedBy[0].grabber is Player) && (self.AI.friendTracker.friend != null && self.AI.friendTracker.friend is Player)) //IF OUR FRIEND IS A PLAYER, ASSUME ALL PLAYERS ARE CHILL
		{
			self.grabbedAttackCounter = 0;
			self.JawOpen = 0;
		}
	}


	public static void BPUUpdatePass2(Creature self)
	{
		//Debug.Log("LZ LIZARD DEBUG!: NUM:" + critnum + " BE:" + self.AI.behavior + " POS:" + self.bodyChunks[1].pos + " VEL:" + self.bodyChunks[1].vel + " STRAIN:" + self.GetBelly().corridorExhaustion + " PIPE:" + self.GetBelly().inPipeStatus);
		
		self.GetBelly().assistedSqueezing = false;
		//-----THE MAIN SQUEEZE CHECK-----
		if ((patch_Player.IsGrabbedByHelper(self) || self.GetBelly().beingPushed > 0) && GetChubValue(self) >= 0)
		{
			//FIRST CHECK IF WE'RE BEING GRABBED BY A HELPER
			if (patch_Player.IsCramped(self)) //(self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace)
			{
				self.GetBelly().isSqueezing = true;
				self.GetBelly().assistedSqueezing = true;
			}
			else
			{
				self.GetBelly().isSqueezing = false;
				self.GetBelly().assistedSqueezing = false;
			}
		}

		//IF WE HAVE ANY STUCK STRAIN AT ALL
		else if (self.GetBelly().isStuck && self.GetBelly().stuckStrain > 0)
			self.GetBelly().isSqueezing = true;
		else if (self.GetBelly().wedgeStrain > 0) //11/29/22 I THINK NPCS NEED THIS SPECIAL
			self.GetBelly().isSqueezing = true;
		else
		{
			self.GetBelly().isSqueezing = false;
			self.GetBelly().assistedSqueezing = false;
		}


		if (self.GetBelly().slicked > 0 )
		{
			if (UnityEngine.Random.value < 0.25f)
            {
				Vector2 pos3 = patch_Player.ObjGetBodyChunkPos(self, "middle") + new Vector2(Mathf.Lerp(-9f, 9f, UnityEngine.Random.value), 9);
				//self.room.AddObject(new WaterDrip(pos3, new Vector2(Mathf.Lerp(-1f, 1f, UnityEngine.Random.value), Mathf.Lerp(2f, 4f, UnityEngine.Random.value)), false));
				self.room.AddObject(new WaterDrip(pos3, new Vector2(0, 1), false));

				if (self is Lizard)
                {
					Vector2 pos2 = patch_Player.ObjGetBodyChunkPos(self, "rear") + new Vector2(Mathf.Lerp(-9f, 9f, UnityEngine.Random.value), 9);
					self.room.AddObject(new WaterDrip(pos2, new Vector2(0, 1), false));
				}
			}
			self.GetBelly().slicked--;
		}

	}


	//ALRIGHT. MAYBE WE JUST NEED A VALUE TO MANUALLY REFRESH ALL STUCK SOUNDS
	public static bool refreshSounds = false;

	public static void BPUUpdatePass3(Creature self)
	{
		bool offscreen = false;
		if (self.graphicsModule == null) // || )
        {
			//Debug.Log("NO GRAPHICS MODULE! END THE SOUND");
            //return;
            offscreen = true;
			if (self.GetBelly().stuckLoop != null) //OTHERWISE WE CRASH
            {
				self.GetBelly().stuckLoop.alive = false;
				self.GetBelly().stuckLoop = null;
			}
		}
		
		//OK, --------SECOND ONE FOR STUCK LOOP --------
		if (self.GetBelly().stuckLoop != null && offscreen == false)
		{
			if (self.GetBelly().isSqueezing == false || !patch_Player.IsStuckOrWedged(self) || refreshSounds)
			{
				self.GetBelly().stuckLoop.alive = false;
				self.GetBelly().stuckLoop = null;
				if (refreshSounds)
					refreshSounds = false;
			}
			else
			{
				float myVel = self.bodyChunks[1].vel.magnitude / 3f; //THIS IS SIMPLER
				if (self.GetBelly().lungsExhausted) //IF WE'RE OUT OF BREATH AND NOT BEING ASSISTED, CUT THE VOLUME. WE AREN'T MAKING PROGRESS.
					myVel = 0f;
				//TAKE THE VALUE HALFWAY BETWEEN OUR CURRENT VEL AND OUR PREVIOUS VALUE
				float maxVol = 2.5f;  //2.0f
				float speedVar = Mathf.Min(Mathf.Lerp(self.GetBelly().myLastVel, myVel, 0.3f), maxVol) + (self.GetBelly().boostStrain / 100f) + (self.GetBelly().assistedSqueezing ? 0.0f : 0f);
				float squeezeThresh = self.GetBelly().tileTightnessMod; //ONE UNNESSESARY VARIABLE COMING UP // 60 * (GetChubValue(self) - 1);
				//speedVar += 4 * Mathf.Pow(((self.GetBelly().stuckStrain - (squeezeThresh/3)) / squeezeThresh), 4); //f(x) = 4(x-(B/3)/B)^4  where B = 60
				speedVar += 1f / (100f * Mathf.Pow((self.GetBelly().stuckStrain / squeezeThresh) - 1.11f, 2f)); //f(x) = 1/(100*(x-1.11)^2) == 0.87f AT FULL THRESHOLD
				//Debug.Log("LZ!-----MATH CHECK!: " + Mathf.Pow(((self.GetBelly().stuckStrain - (squeezeThresh / 3)) / squeezeThresh), 4) + " " + squeezeThresh);

				//MAIN CHARACTERS (PETS) GET A BOOST TO VOLUME!
				float softenValue = 0.85f;
				if (self.safariControlled || BPMeadowStuff.IsMeadowGameMode() || (self is Lizard && (self as Lizard).AI.friendTracker.friend != null))
					softenValue = 1f;
				
				
				float volMod = 0;
				if (self.GetBelly().wedgeStrain > 0f)
                {
					speedVar = ((myVel * 1f) + (self.GetBelly().boostStrain / 50f)) / 1f; //+ self.GetBelly().stuckLoop.wedgeStrain
					volMod = Mathf.Max(self.GetBelly().wedgeStrain - 0.3f, 0) * 1.3f;
					if (patch_Player.GetAxisMagnitude(self) < 0.04f && self.GetBelly().boostStrain < 1)
                    {
						volMod = -1f;
					}
					 // Debug.Log("-----MATH CHECK!:" + GetAxisMagnitude(self));
                }
				
				
				float pitchMod = (patch_Player.ObjIsSlick(self) ? 0.1f : 0f);

				speedVar /= 2;//Mathf.Pow(speedVar, 2);
				self.GetBelly().stuckLoop.alive = true;
				self.GetBelly().stuckLoop.volume = Mathf.Lerp(0.24f + BPOptions.sfxVol.Value, 0.07f, speedVar - volMod) * softenValue;
				self.GetBelly().stuckLoop.pitch = Mathf.Lerp(0.4f, maxVol, speedVar) + pitchMod;
				self.GetBelly().myLastVel = speedVar; //REMEMBER THAT VAL FOR NEXT TIME
			}
		}
		else if (patch_Player.IsStuckOrWedged(self) && offscreen == false && (self.GetBelly().isSqueezing || self.GetBelly().assistedSqueezing)) //(num > 0f)
		{
			self.GetBelly().stuckLoop = self.room.PlaySound(BellyPlus.SqueezeLoop, self.mainBodyChunk, true, 0f, 0f, true); //Vulture_Jet_LOOP
			self.GetBelly().stuckLoop.requireActiveUpkeep = true;
		}
	}

	
	//STUCK CHECK
	public static void BPUUpdatePass4(Creature self)
	{
		int bdy = patch_Player.ObjGetBodyChunkID(self, "middle");
		if (!IsStuck(self))
		{
			// FOR NPCS, THIS IS JUST ALWAYS BASED ON THE DIRECTION THEY'RE POINTING
			self.GetBelly().myFlipValX = (self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x) ? 1 : -1;
			self.GetBelly().myFlipValY = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) ? 1 : -1;
			
			if (self is Scavenger)
			{
				if (self.bodyChunks[bdy].vel.x > 0.1f)
					self.GetBelly().myFlipValX = 1;
				else if (self.bodyChunks[bdy].vel.x < -0.1f)
					self.GetBelly().myFlipValX= -1;
				
				if (self.bodyChunks[bdy].vel.y > 0.1f)
					self.GetBelly().myFlipValY = 1;
				else if (self.bodyChunks[bdy].vel.y < -0.1f)
					self.GetBelly().myFlipValY = -1;
			}
		}



		//------------MAKE PIPE ENTRANCES MORE DIFFICULT TO GET INTO-------
		if (GetChubValue(self) >= -1 && self.room != null) //ALRIGHT, I GUESS THIS SHOULD RUN FOR EVERYONE...
		{
			//IF WE ENDED UP IN A SHORTCUT WAY TOO FAST, MAKE A FORCED SQUINCH SOUND
			//if (!self.GetBelly().inPipeStatus && self.inShortcut)
			if (self.enteringShortCut != null && self.GetBelly().noStuck <= 0 && (self.bodyChunks[bdy].vel.magnitude > 7f))
			{
				self.room.PlaySound(BellyPlus.Squinch1, self.mainBodyChunk, false, 0.15f, 1.3f);
				PlayExternalSound(self, BellyPlus.Fwump1, 0.12f, 1f);
				if (BPOptions.debugLogs.Value)
					Debug.Log("LZ!---WE HIT THAT PIPE AWFULLY SPEEDY!: " + self.bodyChunks[bdy].vel.magnitude);
			}



			//BASIC CHECK TO SEE IF WE'RE ALL THE WAY INSIDE A CORRIDOR OR NOT.
			if (!self.GetBelly().isStuck && self.room != null && self.room.aimap != null)
			{
				bool hipsInNarrow = self.room.aimap.getAItile(self.bodyChunks[1].pos).narrowSpace;
				//bool torsoInNarrow = self.room.aimap.getAItile(self.bodyChunks[0].pos).narrowSpace;
				bool torsoInNarrow = self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace;
				if ((hipsInNarrow && torsoInNarrow && self.GetBelly().timeInNarrowSpace > 20) || self.inShortcut)
				{
					self.GetBelly().inPipeStatus = true;
				}
				else if (!hipsInNarrow && !torsoInNarrow)
				{
					self.GetBelly().inPipeStatus = false;
					self.GetBelly().timeInNarrowSpace = 0;
				}
			}
			if (self is Lizard || self is LanternMouse)
				CheckStuckage(self);
			else
				CheckStuckage(self);
		}

		//STRETCH OUT BASED ON STRAIN!
		/*
		float bodyStretch = Mathf.Min(self.GetBelly().boostStrain, 15f) * ((self.bodyMode == Player.BodyModeIndex.CorridorClimb) ? 2f : 0.7f );
		if (self.GetBelly().beingPushed > 0 || (self.GetBelly().verticalStuck && self.GetBelly().myFlipValY > 0))
			bodyStretch *= 0.6f;
		self.bodyChunkConnections[0].distance += Mathf.Sqrt(bodyStretch);
		*/
		
		//OKAY BUT FOR LIZARDS...
		//HANDLE IT IN THEIR GRAPHICS MODULE
		
	}
	
	
	
	public static void BPUUpdatePass5(Creature self)
	{
		//----- CHECK IF WE'RE PUSHING ANOTHER CREATURE.------
		if (self.GetBelly().pushingOther > 0 && self.graphicsModule != null)
		{
			//CAN I TENCHINCALLY JUST PASTE THE PLAYER VERSION IN HERE AT THIS POINT?
			Player myPartner = patch_Player.FindPlayerInRange(self);
			// LanternMouse mousePartner = FindLizardInRange(self);
			Lizard lizardPartner = patch_Player.FindLizardInRange(self, 0, 1);
			Scavenger scavPartner = null;
			
			Creature myObject = null;
			if (myPartner != null)
				myObject = (myPartner as Creature);
            // else if (mousePartner != null)
            // myObject = (mousePartner as Creature);
            else if (lizardPartner != null)
                myObject = (lizardPartner as Creature);
			else if (self is Scavenger)
            {
				scavPartner = patch_Scavenger.FindScavInRange(self);
				if (scavPartner != null)
					myObject = (scavPartner as Creature);
			}
				


			if (myObject != null)
			{
				bool horzPushLine = patch_Player.ObjIsPushingOther(myObject) && self.GetBelly().myFlipValX != 0 && self.GetBelly().myFlipValX == patch_Player.ObjGetXFlipDirection(myObject);
				bool vertPushLine = patch_Player.ObjIsPushingOther(myObject) && self.GetBelly().myFlipValY != 0 && self.GetBelly().myFlipValY == patch_Player.ObjGetYFlipDirection(myObject);
				bool matchingShoveDir = ((patch_Player.ObjIsVerticalStuck(myObject) || vertPushLine) && self.GetBelly().myFlipValY == patch_Player.ObjGetYFlipDirection(myObject)) || ((!patch_Player.ObjIsVerticalStuck(myObject) || horzPushLine) && self.GetBelly().myFlipValX == patch_Player.ObjGetXFlipDirection(myObject));

				float slouch = 0f; //(Mathf.Max(Mathf.Min(bellyStats[critNum].holdJump, 40) - 5f, 0f) / 2.5f) * (matchingShoveDir ? 1 : 0);

				if (!self.GetBelly().lungsExhausted && matchingShoveDir)
				{
					patch_Player.ObjGainStuckStrain(myObject, 0.5f);
					
					//Debug.Log("LZ!---SHOVE STATS!: " + slouch + " _ " + patch_Player.ObjBeingPushed(myObject));

					//OR FULL BODY SHOVES. RUN SPECIFICALLY ON THE FIRST FRAME OUR PARTNER RECEIVES A PUSH I GUESS
					//OR IN THE LIZARDS CASE, JUST ON THE FIRST PUSH I GUESS
					if (patch_Player.ObjBeingPushed(myObject) == 4)
					{
						// Debug.Log("LZ! SLOUCH SHOVE!: " + slouch);
						// BoostPartner(myPartner, 14, 16);
						patch_Player.ObjGainBoostStrain(myObject, 5, 14, 16);
						patch_Player.ObjGainSquishForce(myObject, 14, 16);
						patch_Player.ObjGainHeat(myObject, 75);
						self.GetBelly().boostStrain += 10;
						//self.GetBelly().myHeat += 50;
						self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.2f, 0.6f);
						self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Medium, self.mainBodyChunk.pos, 1.0f, 1f);
					}

					
				}
				//IF IT'S A PUSHING LINE, PASS FORWARD THE BENEFITS!
				if (patch_Player.ObjBeingPushed(myObject) > 0)
					patch_Player.ObjGainStuckStrain(myObject, 0.25f);

				//CALCULATE A BOOST STRAIN MODIFIER THAT LOOKS A BIT SMOOTHER
				float pushBoostStrn = ((self.GetBelly().boostStrain > 4) ? 4 : self.GetBelly().boostStrain) + slouch;

				//WE NEED TWO SEPERATE FNS FOR VERTICAL/HORIZONTAL
				if (vertPushLine || patch_Player.ObjIsVerticalStuck(myObject) && (matchingShoveDir || slouch > 0))
				{
					//ACTUALLY, WE SHOULD MAKE LIZARDS RUN ALL PUSH SEARCHES FOR REARS INSTEAD OF MIDDLES. BECAUSE LIZARDS ARE NOT COORDINATED ENOUGH TO FIND EACH OTHERS TAILS
					float pushBack = 22f - Mathf.Abs(patch_Player.ObjGetBodyChunkPos(myObject, "middle").y - self.bodyChunks[0].pos.y) + (vertPushLine ? 10 : 0); // + (bellyStats[critNum].boostStrain / 5f);
					pushBack -= pushBoostStrn; // (bellyStats[critNum].boostStrain / 2); //BOOST STRAIN VISUALS
					// Debug.Log("LZ! ---I'M PUSHING Y! LETS SHOW SOME EFFORT: " + pushBack);
					pushBack = Mathf.Max(pushBack, 0);
					pushBack *= self.GetBelly().myFlipValY;
					self.bodyChunks[0].vel.y -= pushBack;
					
					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.y < 0 != self.GetBelly().myFlipValY < 0)
						self.bodyChunks[0].vel.y /= 3f;
					
					if (self.bodyChunks[1].vel.y < 0 != self.GetBelly().myFlipValY < 0)
						self.bodyChunks[1].vel.y /= 3f;
					
					if (self.bodyChunks[2].vel.y < 0 != self.GetBelly().myFlipValY < 0)
						self.bodyChunks[2].vel.y /= 3f;
				}
				else if (horzPushLine || !patch_Player.ObjIsVerticalStuck(myObject) && (matchingShoveDir  || slouch > 0))
				{
					float pushBack = Mathf.Max(25f - pushBoostStrn + (horzPushLine ? 10 : 0) - Mathf.Abs(patch_Player.ObjGetBodyChunkPos(myObject, "middle").x - self.bodyChunks[0].pos.x), 0f);
					//pushBack = Mathf.Max(pushBack * (1f - slouch/20f), 0);
					pushBack *= self.GetBelly().myFlipValX;
					// Debug.Log("LZ!---I'M PUSHING X! LETS SHOW SOME EFFORT: " + pushBack + " " + self.bodyChunks[0].vel.x + " PUSHING LINE?" + horzPushLine);

					//IF THEYRE A TILE ABOVE US, REDUCE ALL THIS
					if (Mathf.Abs(patch_Player.ObjGetBodyChunkPos(myObject, "middle").y - self.bodyChunks[0].pos.y) > 10)
						pushBack /= 3f;

					// Debug.Log("LZ! PUSHBACK SHOVE!: " + pushBack);
					self.bodyChunks[0].vel.x -= pushBack * (1.0f);
					self.bodyChunks[1].vel.x -= pushBack * (1.4f); 
					self.bodyChunks[2].vel.x -= pushBack * (1.8f); //LIZARD SPECIFIC

					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.x < 0 != self.GetBelly().myFlipValX < 0) //Mathf.Abs(self.bodyChunks[0].vel.x) > 4 || 
						self.bodyChunks[0].vel.x /= (matchingShoveDir ? 2.5f : 1); //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING

					if (self.bodyChunks[1].vel.x < 0 != self.GetBelly().myFlipValX < 0) //Mathf.Abs(self.bodyChunks[0].vel.x) > 4 || 
						self.bodyChunks[1].vel.x /= (matchingShoveDir ? 2.8f : 1); //OK THEY NEED TO BE SEPERATE
				
					if (self.bodyChunks[2].vel.x < 0 != GetMouseVector(self).x < 0)
						self.bodyChunks[2].vel.x /= 3f;
				}
			}
			



			//STRAIGHTEN OUT!  //WAS THIS EFFECTIVE? EH...
			if (self is Lizard && patch_Player.ObjIsStuck(self))
            {
				if (patch_Player.ObjIsVerticalStuck(self))
				{
					self.bodyChunks[0].pos.x = self.bodyChunks[1].pos.x;
					self.bodyChunks[0].vel.x = 0;
				}
				else
				{
					self.bodyChunks[0].pos.y = self.bodyChunks[1].pos.y;
					self.bodyChunks[0].vel.y = 0;
				}
			}
			
		}

		if (self.GetBelly().noStuck > 0)
			self.GetBelly().noStuck--;
		
	}

	
	
	
	
	public static void BPUUpdatePass5_2(Creature self)
	{
		//LET CREATURES BOOST TOO! JUST DO IT DIFFERENTLY...
		// bool matchingStuckDir = (IsVerticalStuck(self) && self.input[0].y != 0) || (!IsVerticalStuck(self) && self.input[0].x != 0);
		if (((self.GetBelly().boostCounter < 1 && self.GetBelly().stuckStrain > 65 && !(self.safariControlled || BPMeadowStuff.IsMeadowGameMode())) || ((BellyPlus.SafariJumpButton(self) || self.GetBelly().manualBoost) && self.GetBelly().boostCounter < 10)) && !self.GetBelly().lungsExhausted && ((self.GetBelly().isStuck || patch_Player.ObjIsWedged(self)) || self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther)) //self.AI.excitement > 0.4f && 
		{
			if (patch_Player.ObjIsWedged(self))
				self.GetBelly().boostStrain += 4;
			else
				patch_Player.ObjGainBoostStrain(self, 0, 10, 18);

			if (self.GetBelly().manualBoost)
				self.GetBelly().manualBoost = false;

            self.GetBelly().corridorExhaustion += 22; //30
			int boostAmnt = 15;
			// self.AerobicIncrease(1f);
			float strainMag = 15f * GetExhaustionMod(self, 60);
			// Debug.Log("LZ!----- LIZARD BOOSTING! " + critnum + "- Pushing other?" + self.GetBelly().pushingOther);

			//EXTRA STRAIN PARTICALS!

			if (self.graphicsModule != null)
			{
				for (int n = 0; n < 5 + (strainMag / 4); n++)
				{
					//Vector2 pos = self.graphicsModule.bodyParts[4 + self.lizardParams.tailSegments].pos;
					Vector2 pos = patch_Player.ObjGetHeadPos(self);
					if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
					{
						//self.room.AddObject(new WaterDrip(pos3, new Vector2((float)self.GetBelly().myFlipValX * 10, Mathf.Lerp(-4f, 4f, UnityEngine.Random.value)), false));
						//self.room.AddObject(new WaterDrip(pos3, new Vector2((float)self.GetBelly().myFlipValX * -10, Mathf.Lerp(-4f, 4f, UnityEngine.Random.value)), false));
						self.room.AddObject(new StrainSpark(pos, GetMouseAngle(self).ToVector2() + Custom.DegToVec(180f * UnityEngine.Random.value) * 6f * UnityEngine.Random.value, 15f, Color.white));
					}
				}
			}
			//self.slowMovementStun += 15;
			// self.jumpChunkCounter = 15;
			self.GetBelly().boostCounter = 12 + (Mathf.FloorToInt(UnityEngine.Random.value * 10)); // - Mathf.FloorToInt(Mathf.Lerp(10, 30, self.AI.fear));

			if (self.GetBelly().isStuck)
			{
				self.GetBelly().stuckStrain += boostAmnt;
				//self.GetBelly().loosenProg += boostAmnt / 1000f; //LIZARDS LOOSEN MUCH FASTER
				patch_Player.ObjGainLoosenProg(self, (boostAmnt / 2000f));
            }
			else if (patch_Player.ObjIsWedged(self))
			{
				self.GetBelly().stuckStrain += boostAmnt;
				self.GetBelly().loosenProg += (boostAmnt * (patch_Player.ObjIsSlick(self) ? 3f : 1f)) / 8000; //boostAmnt / 2000f;
				//bellyStats[critNum].loosenProg += (boostAmnt * (ObjIsSlick(self) ? 3f : 1f)) / loosenMod;
				self.room.PlaySound(SoundID.Slugcat_In_Corridor_Step, self.mainBodyChunk, false, 0.6f + self.GetBelly().wedgeStrain * 2, 0.6f + self.GetBelly().wedgeStrain / 2f);
				if (patch_Player.ObjIsSlick(self))
					self.room.PlaySound(SoundID.Tube_Worm_Shoot_Tongue, self.mainBodyChunk, false, 1.0f, 1f);
			}
			if (self.GetBelly().pushingOther > 0)
            {
				Player myPartner = patch_Player.FindPlayerInRange(self);
				Lizard lizardPartner = patch_Player.FindLizardInRange(self, 0, 2);

				Creature myObject = null;
				if (myPartner != null)
					myObject = (myPartner as Creature);
				else if (lizardPartner != null)
					myObject = (lizardPartner as Creature);
				else if (self is Scavenger)
				{
                    Scavenger scavPartner = patch_Scavenger.FindScavInRange(self);
					if (scavPartner != null)
						myObject = (scavPartner as Creature);
				}

				if (myObject != null)
				{
					patch_Player.ObjGainStuckStrain(myObject, boostAmnt / 2);
					patch_Player.ObjGainLoosenProg(myObject, (boostAmnt / 8000f) * (patch_Player.ObjIsSlick(myObject) ? 3f : 1f));
					patch_Player.ObjGainBoostStrain(myObject, 0, 8, 14);
					patch_Player.ObjGainSquishForce(myObject, 8, 14);
					
					//CHECK FOR LATHER
					if (self is Scavenger && myObject is Player && patch_Player.ObjIsStuck(myObject) && patch_Player.CheckApplyLather(self, myObject))
					{
						patch_Player.ObjApplySlickness(myObject);
					}
				}

				if (self is Lizard && UnityEngine.Random.value < 0.10f)
				{
					//self.lizard.voice.MakeSound(LizardVoice.Emotion.GeneralSmallNoise); //MANY LIZARDS DON'T HAVE THESE
					(self as Lizard).voice.MakeSound(LizardVoice.Emotion.Frustration, 0.8f);
				}
			}
			else if (self.GetBelly().pullingOther && self.grasps[0] != null && self.grasps[0].grabbed != null && self.grasps[0].grabbed is Creature)
			{
				Creature myGrasped = self.grasps[0].grabbed as Creature;
				patch_Player.PassDownBenifits(myGrasped, boostAmnt / 2f, 10, 14);
				patch_Player.ObjGainLoosenProg(myGrasped, (boostAmnt / 8000f) * (patch_Player.ObjIsSlick(myGrasped) ? 3f : 1f));
			}
		}
		
		
		
		//MOVING THIS CHECK DOWN HERE BECAUSE THIS MIGHT BE CRASHING WITHOUT A ROOM.
		//Debug.Log("SPEED TEST: " + self.bodyChunks[1].vel.x + " : " + self.bodyChunks[1].vel.y);
		if (patch_Player.ObjIsWedged(self) && patch_Player.GetAxisMagnitude(self) < 0.05f) //DON'T MOVE AT ALL IF WE AINT REALLY MOVING //self.bodyChunks[1].vel.magnitude
		{
			//Debug.Log("LZ!- TOO SLOW! WEDGING IN PLACE: " + self.bodyChunks[1].vel);
			for (int i = 0; i < self.bodyChunks.Length; i++)
			{
				self.bodyChunks[i].vel = new Vector2(0, 0); //self.gravity
			}
		}
		
	}
	
	
	public static void BPUUpdatePass6(Creature self)
	{
		//THIS PART WE CAN RUN LIKE A NORMAL PERSON
		if (self.GetBelly().boostStrain > 0)
			self.GetBelly().boostStrain--;
		
		if (self.GetBelly().beingPushed > 0)
			self.GetBelly().beingPushed--;
		
		if (self.GetBelly().boostCounter > 0)
			self.GetBelly().boostCounter--;

		if (!IsStuck(self) && self.GetBelly().loosenProg > 0 && !self.Stunned)
			self.GetBelly().loosenProg -= 1 / 2000f;


		if (self.GetBelly().corridorExhaustion > 0 && !self.GetBelly().lungsExhausted)
		{
			self.GetBelly().corridorExhaustion--;
			if (self.GetBelly().corridorExhaustion > maxStamina)
			{
				// Debug.Log("LZ!----- OOF, IM EXHAUSTED! ");
				self.GetBelly().lungsExhausted = true;
				self.GetBelly().corridorExhaustion = -200; //A LITTLE WEIRD BUT TRUST ME.
			}
		}
		else if (self.GetBelly().lungsExhausted) //IF EXHAUSTED, WE COUNT UPWARDS
		{
			self.GetBelly().corridorExhaustion++;
			if (self.GetBelly().corridorExhaustion > 0)
				self.GetBelly().lungsExhausted = false; //LUNGS BACK TO NORMAL
		}
		
	}


	public static void BPLizard_Update(On.Lizard.orig_Update orig, Lizard self, bool eu)
	{
		
		int origTimeSpentTrying = self.timeSpentTryingThisMove;

		BPUUpdatePass1(self);

		orig.Invoke(self, eu);

		//THIS PART IS IMPORTANT BECAUSE IF THIS RESETS TO 0 WHEN THEY GET STUCK, IT'S LIKE THEIR TERRAIN SQUEEZE IS RESET AND THEY GO FLYING OUT IF THEIR BODYCHUNK RAD WAS TOO FAT FOR THE SPACE
		if (self.timeSpentTryingThisMove == 0 && IsStuck(self))
			self.timeSpentTryingThisMove = origTimeSpentTrying;

		if (self == null || self.dead) //OKAY??? I GUESS THIS WORKS???
		{
			return;
		}

		//bool mlungsExhausted = self.GetBelly().pushingOther;
		if (self.room != null)
		{ 
			BPUUpdatePass2(self);
			BPUUpdatePass3(self);
			BPUUpdatePass4(self);
			if (self.stun <= 0 )
			{
				BPUUpdatePass5(self);
				BPUUpdatePass5_2(self);
			}
		}
		BPUUpdatePass6(self);
	}





	public static void PopFree(Creature self, float power, bool inPipe)
	{
		
		float popMag = Mathf.Min(power / 120f, 2f); //CAP OUT AT 2
		self.GetBelly().noStuck = 25;
		self.GetBelly().loosenProg = 0;
		if (BPOptions.debugLogs.Value)
			Debug.Log("LZ!-----POP!: " + popMag + " - " + self.GetBelly().stuckStrain);
		float popVol = Mathf.Lerp(0.12f, 0.28f, Mathf.Min(popMag, 1f));
		float stuckStrainMemory = self.GetBelly().stuckStrain;
		self.GetBelly().stuckStrain = 6; //FAST PASS TO FIX VOLUME N STUFF
		// BellyPlus.squeezeStrain[critNum] = 0; //SO WE DON'T ALSO GET THE POP
		self.GetBelly().inPipeStatus = !self.GetBelly().inPipeStatus; //FLIPFLOP OUR PIPE STATUS
		self.GetBelly().isStuck = false;
		self.GetBelly().verticalStuck = false;
		self.GetBelly().stuckInShortcut = false;
		self.GetBelly().stuckCoords = new Vector2(0, 0);
		// self.GetBelly().slicked /= 2;


        //TELEPORT US 0.5 OUT THE HOLE B) - DOESN'T ACTUALLY SEEM TO MAKE A BIG DIFFERENCE... -okay, maybe they do... for dumb green lizards
        self.bodyChunks[0].pos += self.GetBelly().stuckVector * 10f;
		self.bodyChunks[1].pos += self.GetBelly().stuckVector * 10f; //new Vector2(GetMouseVector(self).x * 10f, GetMouseVector(self).y * 10f);

		
		if (!inPipe)
			self.room.PlaySound(SoundID.Slugcat_Turn_In_Corridor, self.mainBodyChunk, false, popMag, Mathf.Sqrt(popMag));
		
		float launchSpeed = inPipe ? 6f : 9f;
		launchSpeed *= (self.GetBelly().slicked > 0 ? 1.5f : 1f);
		Vector2 inputVect = self.GetBelly().stuckVector * launchSpeed * popMag;
		
		for (int i = 0; i < self.bodyChunks.Length; i++)
		{
			self.bodyChunks[i].vel = inputVect;
		}
		// Debug.Log("LZ!-----LAUNCH!!!: " + inputVect);
		
		
		//DID WE HAVE ANY PUSHERS? IF SO, HAVE GRATTITUDE
		if (self is Lizard && patch_Player.ObjBeingPushed(self) > 0 && stuckStrainMemory > 100f)
		{
			for (int i = 0; i < self.room.game.Players.Count; i++)
			{
				Player helper = self.room.game.Players[i].realizedCreature as Player;
				if (helper != null
					&& helper != self
					&& patch_Player.ObjIsPushingOther(helper)
					&& Custom.DistLess(self.bodyChunks[2].pos, helper.bodyChunks[0].pos, 90f)
				)
				{
					Lizard lizSelf = self as Lizard;
					float amnt = 0.5f;
					if (stuckStrainMemory < 120)
                        amnt = 0.25f;
                    if (stuckStrainMemory > 300)
                        amnt = 0.75f;
                    lizSelf.AI.LizardPlayerRelationChange(amnt / lizSelf.AI.friendTracker.tamingDifficlty, helper.abstractCreature);
					Debug.Log("LZ! SOMEONE HELPED PUSH US THROUGH! GAIN STATUS " + amnt);
					if (ModManager.MMF && MMF.cfgExtraLizardSounds.Value && lizSelf.voice.articulationIndex != MMFEnums.LizardVoiceEmotion.Love.Index)
						lizSelf.voice.MakeSound(MMFEnums.LizardVoiceEmotion.Love, 1f);
					break;
				}
			}
		}
		

		if (self is Lizard && self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player && !BellyPlus.ridableLizEnabled)  // NOT IF WE'RE PLAYING RIDABLE LIZARDS!
		{
			self.grabbedBy[0].grabber.ReleaseGrasp(self.grabbedBy[0].graspUsed);
			self.bodyChunks[0].vel = inputVect;
		}

		//MAIN CHARACTERS (PETS) GET A BOOST TO VOLUME!
		float softenValue = 0.95f;
		if ((self is Lizard && (self as Lizard).AI.friendTracker.friend != null) || BPMeadowStuff.IsMeadowGameMode())
			softenValue = 1f;


		//-------------POP SOUND-----------
		popVol += (-0.1f + Mathf.Max(0, BPOptions.sfxVol.Value));
		PlayExternalSound(self, BellyPlus.Pop1, (popVol / (!inPipe ? 2.5f : 2f)) * softenValue, 1f);

		//CREATE SOME FUN SPARK FX
		int sparkCount = Mathf.FloorToInt(Mathf.Lerp(0f, 12f, popMag)); //DEFAULT WAS 8
		if (popMag < 0.1)
			sparkCount = 0;
		
		patch_Player.MakeSparks(self, 0, sparkCount);
	}

}