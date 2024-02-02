using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace RotundWorld;

public class patch_Cicada
{
	
	public static void Patch()
	{
		On.Cicada.ctor += BP_CicadaPatch;

		On.Cicada.Update += BPCicada_Update;
		On.Cicada.SpitOutOfShortCut += Cicada_SpitOutOfShortCut;
		On.Cicada.Die += BP_Die;
		
	}

	private static void BP_CicadaPatch(On.Cicada.orig_ctor orig, Cicada self, AbstractCreature abstractCreature, World world, bool gender)
	{
		orig(self, abstractCreature, world, gender);

        if (self.abstractCreature.GetAbsBelly().myFoodInStomach != -1)
        {
            UpdateBellySize(self);
            return;
        }
		
		//BellyPlus.InitializeCreature(critNum);

		//NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
		UnityEngine.Random.seed = self.abstractCreature.ID.RandomSeed;

        int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
		if (patch_DLL.CheckFattable(self) == false)
			critChub = 0;
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("CREATURE SPAWNED! CHUB SIZE: " + critChub);
		
		self.abstractCreature.GetAbsBelly().myFoodInStomach = critChub;
		
		UpdateBellySize(self);
	}
	
	public static int GetRef(Cicada self)
	{
		return self.abstractCreature.ID.RandomSeed;
	}

	//I DON'T ACTUALLY KNOW IF WE CAN USE THE LIZARD VERSION FOR THIS OR NOT...
	public static IntVector2 GetMouseVector(Cicada self)
	{
		Vector2 mouseVec = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
		int xVec = Math.Max(Mathf.FloorToInt(mouseVec.x * 2f), -1);
		int yVec = Math.Max(Mathf.FloorToInt(mouseVec.y * 2f), -1);
		IntVector2 vector = new IntVector2(xVec, yVec);
		return vector;
	}



	//SOUNDS THAT DON'T GET INTERRUPTED!
	public static void PlayExternalSound(Cicada self, SoundID soundId, float sVol, float sPitch)
	{
		Vector2 pos = self.mainBodyChunk.pos;
		self.room.PlaySound(soundId, pos, sVol, sPitch);
	}
	
	//FIND THE NEAREST MOUSEY~
	public static Cicada FindCicadaInRange(Creature self)
	{
        if (self.room == null)
            return null; 
		
		for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
        {
            if (self.room.abstractRoom.creatures[i].realizedCreature != null
                && self.room.abstractRoom.creatures[i].realizedCreature is Cicada crit
                && crit != self && crit.room != null && crit.room == self.room && !crit.dead
                && Custom.DistLess(self.mainBodyChunk.pos, crit.bodyChunks[1].pos, 35f)
            )
            {
                return crit;
            }
        }

        return null;
	}


	public static void UpdateBellySize(Cicada self)
	{
		float baseWeight = 0.2f; //I THINK...
		int currentFood = self.abstractCreature.GetAbsBelly().myFoodInStomach;
		patch_Lizard.UpdateChubValue(self);
		
		if (BellyPlus.VisualsOnly())
			return;
		
		switch (Math.Min(currentFood, 8))
		{
			case 8:
				self.bodyChunks[0].mass = baseWeight * 1.5f;
				self.bodyChunks[1].mass = baseWeight * 1.5f;
				break;
			case 7:
				self.bodyChunks[0].mass = baseWeight * 1.3f;
				self.bodyChunks[1].mass = baseWeight * 1.3f;
				break;
			case 6:
				self.bodyChunks[0].mass = baseWeight * 1.1f;
				self.bodyChunks[1].mass = baseWeight * 1.1f;
				break;
			case 5:
				self.bodyChunks[0].mass = baseWeight * 1f;
				self.bodyChunks[1].mass = baseWeight * 1f;
				break;
			case 4:
			default:
				self.bodyChunks[0].mass = baseWeight;
				self.bodyChunks[1].mass = baseWeight;
				break;
		}
	}

	//private static readonly float maxStamina = 120f;
	public static float GetExhaustionMod(Cicada self, float startAt)
	{
		return 0; //FOR MICE
	}


	public static bool IsStuck(Cicada self)
	{
		//PRESSED AGAINST AN ENTRANCE
		return self.GetBelly().isStuck;
	}

	public static void Cicada_SpitOutOfShortCut(On.Cicada.orig_SpitOutOfShortCut orig, Cicada self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
	{
		orig.Invoke(self, pos, newRoom, spitOutAllSticks);
		patch_Lizard.Creature_SpitOutOfShortCut(self, pos, newRoom);
	}

	public static void BP_Die(On.Cicada.orig_Die orig, Cicada self)
	{
		self.GetBelly().isStuck = false;
		orig.Invoke(self);
	}
	
	public static void BP_Collide(On.Cicada.orig_Collide orig, Cicada self, PhysicalObject otherObject, int myChunk, int otherChunk)
	{
		
		if (self.Charging && otherObject is Creature && patch_Player.ObjIsStuckable(otherObject as Creature) && patch_Player.ObjIsStuck(otherObject as Creature))
		{
			patch_Player.ObjSetFwumpDelay(otherObject as Creature, 12);
			patch_Player.ObjGainBoostStrain(otherObject as Creature, 5, 15, 22);
			patch_Player.ObjGainSquishForce(otherObject as Creature, 15, 22);
			self.chargeCounter = 0;
			self.Stun(10);
		}
		orig.Invoke(self, otherObject, myChunk, otherChunk);
	}

	public static void CheckStuckage(Cicada self)
	{
		patch_Lizard.CheckStuckage(self); //WE CAN MERGE THESE... NICE
	}



	public static void BPUUpdatePass1(Cicada self, int critNum)
	{
		//Debug.Log("MS!-----DEBUG!: " + self.AI.fear + " _ " + self.runSpeed + " _BE:" + self.AI.behavior + " _BT:" + self.GetBelly().boostCounter + " _BT:" + self.GetBelly().lungsExhausted);
		
		if (self.currentlyLiftingPlayer)
		{
			//JUST RUNS THEIR JUMP MODIFIER MULTIPLE TIMES, SO THE UPWARD BOOST WEARS OFF FASTER
			if (patch_Lizard.GetChubValue(self) >= 3)
				self.playerJumpBoost = Mathf.Max(0f, self.playerJumpBoost * 0.9f - 0.033333335f);
			if (patch_Lizard.GetChubValue(self) == 4)
				self.playerJumpBoost = Mathf.Max(0f, self.playerJumpBoost * 0.9f - 0.033333335f);
		}
		
		//RECALCULATE RUN SPEED
		if (self.GetBelly().isStuck)
        {
			self.flyingPower = Mathf.Min(self.flyingPower, 0.1f);
			//MAKE THEM FACE THE WAY THEY NEED TO
			self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Pull;
		}
		else
			self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Normal;
		//(1f - (patch_Lizard.GetChubValue(self) / 12f)) * (IsStuck(self) ? 0.01f : 1f)
	}

	
	public static void BPUUpdatePass2(Cicada self, int critNum)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass2(self, critNum);
	}
	
	
	
	public static void BPUUpdatePass3(Cicada self, int critNum)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass3(self, critNum);
	}
	
	
	public static void BPUUpdatePass4(Cicada self, int critNum)
	{
		patch_Lizard.BPUUpdatePass4(self, critNum);
	}
	
	
	public static void BPUUpdatePass5(Cicada self, int critNum)
	{
		//----- CICADAS WON'T PUSH! BUT MAYBE THEY'LL PULL WHOEVER IS HOLDING THEM?------
	}
		
		
	public static void BPUUpdatePass5_2(Cicada self, int critNum)
	{
		bool isTowingOther = self.flying && self.grabbedBy.Count > 0 && (self.grabbedBy[0].grabber is Player) && patch_Player.IsStuck(self.grabbedBy[0].grabber as Player);
		
		//LET MICE BOOST TOO! JUST DO IT DIFFERENTLY...
		// bool matchingStuckDir = (IsVerticalStuck(self) && self.input[0].y != 0) || (!IsVerticalStuck(self) && self.input[0].x != 0);
		if (self.GetBelly().boostCounter < 1 && !self.GetBelly().lungsExhausted && (IsStuck(self) || isTowingOther)) //|| self.GetBelly().pushingOther)
		{
			if (patch_Player.ObjIsWedged(self))
				self.GetBelly().boostStrain += 4;
			else
				self.GetBelly().boostStrain += 10;

			self.GetBelly().corridorExhaustion += 30;
			int boostAmnt = 15;
			float strainMag = 15f * GetExhaustionMod(self, 60);
			//Debug.Log("MS!----- BOOSTING! ");

			//EXTRA STRAIN PARTICALS!
			if (self.graphicsModule != null)
			{
				for (int n = 0; n < 3 + (strainMag / 5); n++)
				{
					Vector2 pos = patch_Player.ObjGetHeadPos(self);
					if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
					{
						self.room.AddObject(new StrainSpark(pos, self.mainBodyChunk.vel + Custom.DegToVec(180f * UnityEngine.Random.value) * 6f * UnityEngine.Random.value, 15f, Color.white));
					}
				}
			}

			self.GetBelly().boostCounter += 14 + (Mathf.FloorToInt(UnityEngine.Random.value * 4)); // - Mathf.FloorToInt(Mathf.Lerp(10, 30, self.AI.fear));

			if (IsStuck(self))
			{
				self.GetBelly().stuckStrain += boostAmnt;
				self.GetBelly().loosenProg += boostAmnt / 4000f;
			}
			else if (isTowingOther)
            {
				//WE CAN ONLY TUG SLUGCATS
				Creature myPartner = self.grabbedBy[0].grabber;
				if (myPartner != null)
				{
					patch_Player.ObjGainStuckStrain(myPartner, boostAmnt / 2);
					patch_Player.ObjGainLoosenProg(myPartner, boostAmnt / 8000f);
					patch_Player.ObjGainBoostStrain(myPartner, 0, 10, 15);
				}
			}
		}
	}
	
	
	public static void BPUUpdatePass6(Cicada self, int critNum)
	{
		patch_Lizard.BPUUpdatePass6(self, critNum);
	}



	public static void BPCicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, eu);
			return;
		}
		
		int critNum = self.abstractCreature.ID.RandomSeed;

		BPUUpdatePass1(self, critNum);
		
		orig.Invoke(self, eu);


		if (self == null || self.dead)
			return;
		
		if (self.room != null)
		{ 
			BPUUpdatePass2(self, critNum);
			BPUUpdatePass3(self, critNum);
			BPUUpdatePass4(self, critNum);
			BPUUpdatePass5(self, critNum);
			BPUUpdatePass5_2(self, critNum);
		}
		BPUUpdatePass6(self, critNum);
	}


	public static void PopFree(Cicada self, float power, bool inPipe)
	{
		patch_Lizard.PopFree(self, power, inPipe);
	}

}