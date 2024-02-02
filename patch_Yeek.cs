using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using MoreSlugcats;

namespace RotundWorld;

public class patch_Yeek
{
    public static void Patch()
    {
        //On.MoreSlugcats.Yeek.GetSegmentRadForCollision += Yeek_GetSegmentRadForCollision;
    }

    

    public static Yeek FindYeekInRange(Creature self)
    {
        
        return null;
    }

    /*
	public static void Patch()
	{
		On.MoreSlugcats.Yeek.ctor += BP_YeekPatch;
		On.MoreSlugcats.Yeek.Update += BPYeek_Update;
		On.MoreSlugcats.Yeek.SpitOutOfShortCut += Yeek_SpitOutOfShortCut;
        On.MoreSlugcats.Yeek.Hop_Vector2_Vector2_bool_bool_bool += Yeek_Hop_Vector2_Vector2_bool_bool_bool;
		//On.MoreSlugcats.Yeek.Die += BP_Die;

		On.MoreSlugcats.YeekGraphics.InitiateSprites += YeekGraphics_InitiateSprites;
        On.MoreSlugcats.YeekState.Feed += YeekState_Feed;
	}

    private static void Yeek_Hop_Vector2_Vector2_bool_bool_bool(On.MoreSlugcats.Yeek.orig_Hop_Vector2_Vector2_bool_bool_bool orig, Yeek self, Vector2 currentPos, Vector2 goalPos, bool forced, bool allowInTunnel, bool calledFromJump)
    {
		int critNum = self.abstractCreature.ID.RandomSeed;
		if (!BellyPlus.isStuck[critNum])
		{
			orig(self, currentPos, goalPos, forced, allowInTunnel, calledFromJump);
		}

		//NO DON'T GET UNSTUCK THAT WAY
		//if (self.AI.behavior == YeekAI.Behavior.GetUnstuck)
		//	self.AI.behavior = YeekAI.Behavior.Idle;
	}

    private static void YeekState_Feed(On.MoreSlugcats.YeekState.orig_Feed orig, YeekState self, int CycleTimer)
	{
		orig(self, CycleTimer);
		Creature mySelf = self.creature.realizedCreature;
		BellyPlus.myFoodInStomach[BellyPlus.GetRef(mySelf)] += 2;
		UpdateBellySize(mySelf as Yeek);
		Debug.Log("YEEK IN DEN - EATING A TASTY SNACK! " + BellyPlus.myFoodInStomach[patch_Lizard.GetRef(mySelf)]);
	}


	private static void BP_YeekPatch(On.MoreSlugcats.Yeek.orig_ctor orig, Yeek self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);

		if (BellyPlus.yeeksOn == false)
			return;

		int critNum = self.abstractCreature.ID.RandomSeed;
		bool critterExists = false;
        try
        {
			patch_Yeek.yeekBook.Add(critNum, self); //ADD OURSELVES TO THE GUESTBOOK
		}
		catch (ArgumentException)
        {
			critterExists = true;
		}

		if (critterExists)
        {
			patch_Yeek.yeekBook[critNum] = self; //WELL HOLD ON! WE STALL NEED THE REFERENCE FROM THAT BOOK TO POINT TO US!
			UpdateBellySize(self);
			return;
		}
		
		BellyPlus.InitializeCreature(critNum);

		//NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
		int seed = UnityEngine.Random.seed;
		UnityEngine.Random.seed = critNum;
		
		int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
		critChub = 8;
		BellyPlus.myFoodInStomach[critNum] = critChub;
		
		UpdateBellySize(self);
	}
	
	public static int GetRef(Yeek self)
	{
		return self.abstractCreature.ID.RandomSeed;
	}

	//I DON'T ACTUALLY KNOW IF WE CAN USE THE LIZARD VERSION FOR THIS OR NOT...
	public static IntVector2 GetMouseVector(Yeek self)
	{
		Vector2 mouseVec = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
		int xVec = Math.Max(Mathf.FloorToInt(mouseVec.x * 2f), -1);
		int yVec = Math.Max(Mathf.FloorToInt(mouseVec.y * 2f), -1);
		IntVector2 vector = new IntVector2(xVec, yVec);
		return vector;
	}
	
	//FIND THE NEAREST MOUSEY~
	public static Yeek FindYeekInRange(Creature self)
	{
		foreach(KeyValuePair<int, Yeek> kvp in patch_Yeek.yeekBook)
        {
            if (
				kvp.Value != null
				&& kvp.Value != self
				&& kvp.Value.room == self.room
				&& kvp.Value.dead == false
				&& Custom.DistLess(self.mainBodyChunk.pos, kvp.Value.bodyChunks[1].pos, 35f)
			)
			{
				return kvp.Value as Yeek;
			}
        }
		return null;
	}


	public static void UpdateBellySize(Yeek self)
	{
		float baseWeight = 0.05f; 
		int currentFood = BellyPlus.myFoodInStomach[GetRef(self)];

		//YEEKS CONSTANTLY UPDATE THEIR BODY MASS SO THIS WON'T WORK
		//
		patch_Lizard.UpdateChubValue(self);
	}
	
	
	//private static readonly float maxStamina = 120f;
	public static float GetExhaustionMod(Yeek self, float startAt)
	{
		return 0;
	}


	public static bool IsStuck(Yeek self)
	{
		return BellyPlus.isStuck[GetRef(self)];
	}

	public static void Yeek_SpitOutOfShortCut(On.MoreSlugcats.Yeek.orig_SpitOutOfShortCut orig, Yeek self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
	{
		orig.Invoke(self, pos, newRoom, spitOutAllSticks);
		patch_Lizard.Creature_SpitOutOfShortCut(self, pos, newRoom);
	}

	//public static void BP_Die(On.MoreSlugcats.Yeek.orig_Die orig, Yeek self)
	//{
	//	BellyPlus.isStuck[GetRef(self)] = false;
	//	orig.Invoke(self);
	//}
	
	public static void BP_Collide(On.MoreSlugcats.Yeek.orig_Collide orig, Yeek self, PhysicalObject otherObject, int myChunk, int otherChunk)
	{
		
		//if (self.Charging && otherObject is Creature && patch_Player.ObjIsStuckable(otherObject as Creature) && patch_Player.ObjIsStuck(otherObject as Creature))
		//{
		//	patch_Player.ObjSetFwumpDelay(otherObject as Creature, 12);
		//	patch_Player.ObjGainBoostStrain(otherObject as Creature, 5, 15, 22);
		//	self.chargeCounter = 0;
		//	self.Stun(10);
		//}
		orig.Invoke(self, otherObject, myChunk, otherChunk);
	}

	public static void CheckStuckage(Yeek self)
	{
		patch_Lizard.CheckStuckage(self); //WE CAN MERGE THESE... NICE
	}



	public static void BPUUpdatePass1(Yeek self, int critNum)
	{
		//Debug.Log("MS!-----DEBUG!: " + self.AI.fear + " _ " + self.runSpeed + " _BE:" + self.AI.behavior + " _BT:" + BellyPlus.boostCounter[critNum] + " _BT:" + BellyPlus.lungsExhausted[critNum]);
		
		//if (self.currentlyLiftingPlayer)
		//{
		//	//JUST RUNS THEIR JUMP MODIFIER MULTIPLE TIMES, SO THE UPWARD BOOST WEARS OFF FASTER
		//	if (patch_Lizard.GetChubValue(self) >= 3)
		//		self.playerJumpBoost = Mathf.Max(0f, self.playerJumpBoost * 0.9f - 0.033333335f);
		//	if (patch_Lizard.GetChubValue(self) == 4)
		//		self.playerJumpBoost = Mathf.Max(0f, self.playerJumpBoost * 0.9f - 0.033333335f);
		//}
		
		//RECALCULATE RUN SPEED
		if (BellyPlus.isStuck[critNum])
        {
			//self.flyingPower = Mathf.Min(self.flyingPower, 0.1f);
			//MAKE THEM FACE THE WAY THEY NEED TO
			//self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Pull;
			
		}
		//else
		//	self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Normal;

		//(1f - (patch_Lizard.GetChubValue(self) / 12f)) * (IsStuck(self) ? 0.01f : 1f)
	}

	
	public static void BPUUpdatePass2(Yeek self, int critNum)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass2(self, critNum);
	}
	
	
	
	public static void BPUUpdatePass3(Yeek self, int critNum)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass3(self, critNum);
	}
	
	
	public static void BPUUpdatePass4(Yeek self, int critNum)
	{
		patch_Lizard.BPUUpdatePass4(self, critNum);
	}
	
	
	public static void BPUUpdatePass5(Yeek self, int critNum)
	{
		//----- CICADAS WON'T PUSH! BUT MAYBE THEY'LL PULL WHOEVER IS HOLDING THEM?------
	}
		
		
	public static void BPUUpdatePass5_2(Yeek self, int critNum)
	{
		bool isTowingOther = false; //self.flying && self.grabbedBy.Count > 0 && (self.grabbedBy[0].grabber is Player) && patch_Player.IsStuck(self.grabbedBy[0].grabber as Player);
		
		//LET MICE BOOST TOO! JUST DO IT DIFFERENTLY...
		// bool matchingStuckDir = (IsVerticalStuck(self) && self.input[0].y != 0) || (!IsVerticalStuck(self) && self.input[0].x != 0);
		if (BellyPlus.boostCounter[critNum] < 1 && !BellyPlus.lungsExhausted[critNum] && (IsStuck(self) || isTowingOther)) //|| self.GetBelly().pushingOther)
		{
			if (patch_Player.ObjIsWedged(self))
				BellyPlus.boostStrain[critNum] += 4;
			else
				BellyPlus.boostStrain[critNum] += 10;

			BellyPlus.corridorExhaustion[critNum] += 30;
			int boostAmnt = 15;
			float strainMag = 15f * GetExhaustionMod(self, 60);
			Debug.Log("MS!----- BOOSTING! ");

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

			BellyPlus.boostCounter[critNum] += 14 + (Mathf.FloorToInt(UnityEngine.Random.value * 4)); // - Mathf.FloorToInt(Mathf.Lerp(10, 30, self.AI.fear));

			if (IsStuck(self))
			{
				BellyPlus.stuckStrain[critNum] += boostAmnt;
				BellyPlus.loosenProg[critNum] += boostAmnt / 4000f;
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
	
	
	public static void BPUUpdatePass6(Yeek self, int critNum)
	{
		patch_Lizard.BPUUpdatePass6(self, critNum);
	}



	public static void BPYeek_Update(On.MoreSlugcats.Yeek.orig_Update orig, Yeek self, bool eu)
	{
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


	public static void PopFree(Yeek self, float power, bool inPipe)
	{
		patch_Lizard.PopFree(self, power, inPipe);
	}
	
	
	
	
	
	
	
	
	
	private static void YeekGraphics_InitiateSprites(On.MoreSlugcats.YeekGraphics.orig_InitiateSprites orig, YeekGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		orig.Invoke(self, sLeaser, rCam);
		BP_UpdateFatness(self, sLeaser);

	}

	public static void BP_UpdateFatness(YeekGraphics self, RoomCamera.SpriteLeaser sLeaser)
    {
        //orig.Invoke(self, sLeaser, rCam);

		float bodySize = 0.95f;
        switch (patch_Lizard.GetChubValue(self.myYeek))
		{
			case 4:
				bodySize *= 1.4f;
				break;
			case 3:
				bodySize *= 1.3f;
				break;
			case 2:
				bodySize *= 1.1f;
				break;
			case 1:
				bodySize *= 1.0f;
				break;
			case 0:
			default:
				bodySize *= 1.0f;
				break;
		}

		sLeaser.sprites[self.HeadSpritesStart + 1].scale = bodySize; // self.iVars.fatness;
        //sLeaser.sprites[self.BodySprite].scaleY = bodySize;
    }
	*/
}