using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using MoreSlugcats;



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

	public static Dictionary<int, Lizard> lizardBook = new Dictionary<int, Lizard>(0);

	private static void MainPatch(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);
		int lizardNum = self.abstractCreature.ID.RandomSeed;

		//MAKE SURE THERE ISN'T ALREADY A CREATURE WITH OUR NAME ON THIS!
		bool mouseExists = false;
        try
        {
			//ADD OURSELVES TO THE GUESTBOOK
			patch_Lizard.lizardBook.Add(lizardNum, self);
		}
		catch (ArgumentException)
        {
			mouseExists = true;
		}

		if (mouseExists)
        {
			// Debug.Log("LIZARD ALREADY EXISTS! CANCELING: " + lizardNum);
			patch_Lizard.lizardBook[lizardNum] = self; //WELL HOLD ON! WE STALL NEED THE REFERENCE FROM THAT BOOK TO POINT TO US!
			UpdateBellySize(self);
			return;
		}
		
		BellyPlus.InitializeCreature(lizardNum);
        

        // int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
        //NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
        int seed = UnityEngine.Random.seed;
		UnityEngine.Random.seed = lizardNum;
		int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));

		if (self.Template.type == CreatureTemplate.Type.YellowLizard)
			critChub += 1; //BECAUSE IT'S HILARIOUS
		else if (self.Template.type == MoreSlugcatsEnums.CreatureTemplateType.SpitLizard)
			critChub = UnityEngine.Random.Range(3, 8);
		
		if (patch_DLL.CheckFattable(self) == false)
			critChub = 0;
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("LIZARD SPAWNED! CHUB SIZE: " + critChub);
		BellyPlus.myFoodInStomach[GetRef(self)] = critChub;
		
		UpdateBellySize(self);

        if (BellyPlus.parasiticEnabled)
            BellyPlus.InitPSFoodValues(abstractCreature);
    }


	void OnEnable()
	{

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
	
	
	//FIND THE NEAREST
	public static Lizard FindLizardInRange(Creature self)
	{
		foreach(KeyValuePair<int, Lizard> kvp in patch_Lizard.lizardBook)
        {
            if (
				kvp.Value != null
				&& kvp.Value != self
				&& kvp.Value.room == self.room
				&& kvp.Value.dead == false
				&& Custom.DistLess(self.mainBodyChunk.pos, kvp.Value.bodyChunks[1].pos, 35f)
			)
			{
				return kvp.Value as Lizard;
				// break;
			}
        }
		return null;
	}


	//WEIGHTINESS
	public static void UpdateChubValue(Creature self)
	{
		int critNum = GetRef(self);
		int currentFood = Math.Min(BellyPlus.myFoodInStomach[critNum], 8);
		
		switch (currentFood)
		{
			case 8:
				BellyPlus.myChubValue[critNum] = 4;
				break;
			case 7:
				BellyPlus.myChubValue[critNum] = 3;
				break;
			case 6:
				BellyPlus.myChubValue[critNum] = 2;
				break;
			case 5:
				BellyPlus.myChubValue[critNum] = 1;
				break;
			case 4:
				BellyPlus.myChubValue[critNum] = 0;
				break;
			case 3:
				BellyPlus.myChubValue[critNum] = -1;
				break;
			case 2:
				BellyPlus.myChubValue[critNum] = -2;
				break;
			case 1:
				BellyPlus.myChubValue[critNum] = -3;
				break;
			case 0:
			default:
				BellyPlus.myChubValue[critNum] = -4;
				break;
		}
	}
	
	
	public static int GetChubValue(Creature self)
	{
		/*
		int currentFood = BellyPlus.myFoodInStomach[GetRef(self)];
		int maxFood = 7;
		if (maxFood - currentFood <= -1)
			return 4;
		else if (maxFood - currentFood == 0)
			return 3;
		else if (maxFood - currentFood == 1)
			return 2;
		else if (maxFood - currentFood == 2)
			return 1;
		else if (maxFood - currentFood == 3)
			return 0;
		else if (maxFood - currentFood == 4)
			return -1;
		else if (maxFood - currentFood == 5)
			return -2;
		else if (maxFood - currentFood == 6)
			return -3;
		else
		{
			return -4;
		}
		*/
		return BellyPlus.myChubValue[GetRef(self)]; //OPTIMIZED
	}




	public static void ObjUpdateBellySize(Creature self)
    {
		if (self is Lizard)
			UpdateBellySize(self as Lizard);
		else if (self is Cicada)
			patch_Cicada.UpdateBellySize(self as Cicada);
		else if (self is Vulture)
			patch_Vulture.UpdateBellySize(self as Vulture);
	}


	

	public static void UpdateBellySize(Lizard self)
	{
		int myLiz = GetRef(self);
		float baseRad = 8f * self.lizardParams.bodySizeFac * self.lizardParams.bodyRadFac;
		int currentFood = BellyPlus.myFoodInStomach[GetRef(self)];

		//BIG LIZARDS NEED TO CHILL
		float chonkMod = (self.lizardParams.bodySizeFac >= 1.4f ? 0.2f : 0);

		float newChunkRad = 1f;
		switch (Math.Min(currentFood, 8))
		{
			case 8:
				newChunkRad = baseRad * 1.3f;
				BellyPlus.myFatness[myLiz] = 1.5f - chonkMod + (GetOverstuffed(self) / 25f);
				break;
			case 7:
				newChunkRad = baseRad * 1.2f;
				BellyPlus.myFatness[myLiz] = 1.35f - chonkMod;
				break;
			case 6:
				newChunkRad = baseRad * 1.1f;
				BellyPlus.myFatness[myLiz] = 1.2f - chonkMod;
				break;
			case 5:
				newChunkRad = baseRad * 1.05f;
				BellyPlus.myFatness[myLiz] = 1.1f - chonkMod;
				break;
			case 4:
				newChunkRad = baseRad * 1f;
				BellyPlus.myFatness[myLiz] = 1.0f - chonkMod;
				break;
			case 3:
			default:
				newChunkRad = baseRad * 1f;
				BellyPlus.myFatness[myLiz] = 0.9f - chonkMod;
				break;
		}
		

		self.bodyChunks[1].rad = newChunkRad;
        //self.bodyChunks[1].rad = Mathf.Min(newChunkRad, 10.3f); //WE CAN'T HAVE A RAD LARGER THAN 10 OR IT WILL OUTGROW PIPES!!!
        // Debug.Log("LZ!----NEW BODYCHUNK SIZE! " + self.bodyChunks[1].rad + " - " + BellyPlus.myFoodInStomach[myLiz]);

        UpdateChubValue(self);
	}
	
	
	public static int GetOverstuffed(Creature self)
    {
		int currentFood = BellyPlus.myFoodInStomach[GetRef(self)];
		if (currentFood > 8)
			return currentFood - 8;
		else
			return 0;
	}
	
	

	private static readonly float maxStamina = 120f;
	public static float GetExhaustionMod(Creature self, float startAt)
	{
		float exh = BellyPlus.corridorExhaustion[GetRef(self)];
		return Mathf.Max(0f, exh - startAt) / (maxStamina - startAt);
	}

	public static bool IsCramped(Creature self)
	{
		//NAH, JUST RETURNS TRUE IF IN A PASSAGE AT ALL
		return (
			(self.room != null && self.room.aimap != null && self.room.aimap.getAItile(self.bodyChunks[1].pos).narrowSpace)
			|| BellyPlus.isStuck[GetRef(self)]);

	}


	public static bool IsStuck(Creature self)
	{
		//PRESSED AGAINST AN ENTRANCE
		return BellyPlus.isStuck[GetRef(self)];
	}

	public static float GetBoostStrain(Creature self)
	{
		return BellyPlus.boostStrain[GetRef(self)];
	}

	public static float GetStuckPercent(Creature self)
	{
		float squeezeThresh = BellyPlus.tileTightnessMod[GetRef(self)];
		return (BellyPlus.stuckStrain[GetRef(self)] / squeezeThresh);
	}


	public static void PushedOn(Creature self) //, Lizard pusher)
	{
		BellyPlus.beingPushed[GetRef(self)] = 3;
		// pushingOther[GetRef(pusher)] = true;
	}
	public static void PushedOther(Creature self)
	{
		BellyPlus.pushingOther[GetRef(self)] = true;
	}

	public static bool IsPushingOther(Creature self)
	{
		return BellyPlus.pushingOther[GetRef(self)];
	}

	public static bool IsPullingOther(Creature self)
	{
		return BellyPlus.pullingOther[GetRef(self)];
	}

	public static bool IsVerticalStuck(Creature self)
	{
		return BellyPlus.verticalStuck[GetRef(self)];
	}

	public static int GetYFlipDirection(Creature self)
	{
		return BellyPlus.myFlipValY[GetRef(self)];
	}

	public static int GetXFlipDirection(Creature self)
	{
		return BellyPlus.myFlipValX[GetRef(self)];
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
		int playerNum = GetRef(self);
		BellyPlus.inPipeStatus[playerNum] = true;
		BellyPlus.noStuck[playerNum] = 0;
		BellyPlus.boostStrain[playerNum] = 0;
		BellyPlus.stuckStrain[playerNum] = 0;
		BellyPlus.stuckCoords[playerNum] = new Vector2(0, 0);
		BellyPlus.timeInNarrowSpace[playerNum] = 100; //ENOUGH TO TRIGGER THE IN-PIPE STATUS
		
		BellyPlus.myFlipValX[playerNum] = newRoom.ShorcutEntranceHoleDirection(pos).x;
		BellyPlus.myFlipValY[playerNum] = newRoom.ShorcutEntranceHoleDirection(pos).y;
		
		// Debug.Log("CRT!----SHORTCUT EJECT! " + playerNum);
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
            else if (self is MirosBird && patch_DLL.GetChub(self as MirosBird) >= 4)
            {
				self.State.meatLeft = Mathf.FloorToInt(self.State.meatLeft * 1.5f);
            }
            else if (self is BigNeedleWorm)
            {
                if (patch_DLL.GetChub(self as NeedleWorm) == 4)
                    self.State.meatLeft += 2;
            }
			else if (self is Centipede && patch_DLL.GetChub(self as Centipede) == 4)
			{
				if ((self as Centipede).Centiwing) // || self.Small)
                    self.State.meatLeft += 2;  //self.abstractCreature.state.meatLeft += 2;
				else
                    self.State.meatLeft += 3;
			}
            else if (self is DropBug && BellyPlus.myFoodInStomach[BellyPlus.GetRef(self)] >= 3)
            {
                self.State.meatLeft += 2;
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
		int lizNum = GetRef(self);
		bool inPipe = BellyPlus.inPipeStatus[lizNum];
		float posMod = inPipe ? 0.5f : 0f;

		//CHECK FOR GRACE PERIOD
		if (BellyPlus.noStuck[lizNum] > 0)
		{
			BellyPlus.noStuck[lizNum]--;
			BellyPlus.isStuck[lizNum] = false;
			BellyPlus.verticalStuck[lizNum] = false;
			//Debug.Log("LZ!----NO STUCKS ALLOWED! ");
			return;
		}

		if (self.room == null || self.graphicsModule == null) //MOUSE SPECIFIC
			return;

		//AREA SLIGHTLY IN FRONT OF HIPS
		float myxF = (0.5f + posMod) * BellyPlus.myFlipValX[lizNum]; //NOO DON'T ADD OUR INPUT
		float myxB = (-0.0f + posMod) * BellyPlus.myFlipValX[lizNum]; //AREA SLIGHTLY BEHIND HIPS
																		  //FOR THE Y VERSION
		float myyF = (0.5f + posMod) * BellyPlus.myFlipValY[lizNum]; //AREA SLIGHTLY IN FRONT OF HIPS
		float myyB = (-0.0f + posMod) * BellyPlus.myFlipValY[lizNum]; //AREA SLIGHTLY BEHIND HIPS													


		//DON'T GET CONFUSED, THESE ARE BOTH FOR THE BOTTOM CHUNK. BUT IT'S CHECKING FOR THE FRONT/REAR OF THE BOTTOM CHUNK
		bool frontInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, myxF, 0);
		bool rearInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, myxB, 0);

		bool vertFrontInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, 0, myyF);
		bool vertRearInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 1, 0, myyB);

		//USE VERTICAL STUCK CHECKS TO SET THIS ONE, SINCE WE CAN BE HORIZONTAL STUCK WHILE STANDING UP
		bool isVertical = false;

		//THIS MIGHT FIX IT; IF WE'RE VERTICAL STUCK, STAY THAT WAY UNTIL WE ARE NOT, REGARDLESS OF OUR ANGLE
		// if (BellyPlus.verticalStuck[lizNum]) //ACTUALLY, THIS MIGHT BE CAUSING ISSUES. LETS TRY REVERTING THIS
			// isVertical = true;
		if (inPipe)
			isVertical = Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) < Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y);
		else
			isVertical = (vertFrontInCorridor != vertRearInCorridor);
		
		bool topInCorridor = patch_Player.IsTileNarrowFloat(self as Creature, 0, 0f, BellyPlus.myFlipValY[lizNum]);
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



		if (BellyPlus.fwumpDelay[lizNum] > 0)
			BellyPlus.fwumpDelay[lizNum]--;

		//IF WE'RE NOT STUCK, RETURN
		if (!wedgedInFront && !wedgedBehind && !vertStuck)
		{
			
			//IF WE JUST CHEATED THE STUCK STRAIN, MAKE AN ATTEMPT TO POP US BACK INTO PLACE!
			int myZard = lizNum;
			bool backedOut = false;
			if (BellyPlus.isStuck[myZard] && BellyPlus.stuckStrain[myZard] > 0 && BellyPlus.stuckCoords[myZard] != new Vector2(0, 0))
			{

				Vector2 newCoords = BellyPlus.stuckCoords[myZard];
				float nudge = inPipe ? 5 : -5f;
				bool isClipping = self.IsTileSolid(1, 0, 0); //OKAY WEIRDO. WE NEED TO CHECK TO MAKE SURE OUR UNSTICK CAME FROM PHASING INTO THE WALL :/
				if (!BellyPlus.verticalStuck[myZard])
				{
					wedgedInFront = true;
					if(BellyPlus.myFlipValX[myZard] == 1 && self.bodyChunks[1].pos.x > BellyPlus.stuckCoords[myZard].x || isClipping)
						newCoords = new Vector2(newCoords.x - nudge, newCoords.y);
					else if (BellyPlus.myFlipValX[myZard] == -1 && self.bodyChunks[1].pos.x < BellyPlus.stuckCoords[myZard].x || isClipping)
						newCoords = new Vector2(newCoords.x + nudge, newCoords.y);
					else
						backedOut = true;
					// self.bodyChunks[0].pos = newCoords + new Vector2(5 * BellyPlus.myFlipValX[myZard], 0);
					self.bodyChunks[0].pos.y = newCoords.y;
				}
				else
				{
					vertStuck = true;
					if(BellyPlus.myFlipValY[myZard] == 1 && self.bodyChunks[1].pos.y > BellyPlus.stuckCoords[myZard].y || isClipping)
						newCoords = new Vector2(newCoords.x, newCoords.y - nudge);
					else if (BellyPlus.myFlipValY[myZard] == -1 && self.bodyChunks[1].pos.y < BellyPlus.stuckCoords[myZard].y || isClipping)
						newCoords = new Vector2(newCoords.x, newCoords.y + nudge);
					else
						backedOut = true;
					self.bodyChunks[0].pos.x = newCoords.x;
				}
					
				if (backedOut)
				{
					// Debug.Log("LZ! WE JUST BACKED OUT! NOTHING SPECIAL " + self.bodyChunks[1].pos + " STUCK COORDS:" + BellyPlus.stuckCoords[myZard] + " FLIPDIR:" + BellyPlus.myFlipValX[myZard] + "," + BellyPlus.myFlipValY[myZard] + " VERTSTK:" + BellyPlus.verticalStuck[myZard] + self.bodyChunks[0].terrainSqueeze);
					BellyPlus.isStuck[lizNum] = false;
					BellyPlus.stuckInShortcut[myZard] = false;
					BellyPlus.verticalStuck[lizNum] = false;
					BellyPlus.stuckCoords[myZard] = new Vector2(0, 0);
					return;
				}
				else
				{
					float stretchMag = (self.bodyChunks[1].vel.magnitude / 1f);
					// Debug.Log("LZ! REDIRECTING TO STUCK COORDS! " + BellyPlus.stuckCoords[myZard] + " CURRENT:" + self.bodyChunks[1].pos + " ADJST:" + newCoords + " STRETCH:" + stretchMag + " CLIPPING" + isClipping); // + " BODYCOORDS:" + self.bodyChunks[2].pos);
					
					self.bodyChunks[1].HardSetPosition(newCoords);
					
					//OKAY, HOW BAD ARE WE STRETCHING? INCREASE STUCKSTRAIN BASED ON THAT.
					BellyPlus.stuckStrain[myZard] += 5 + stretchMag; //MAYBE HELP

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
					// Debug.Log("LZ!FWOMP!! EJECTED " + inPipe + "-" + BellyPlus.boostStrain[lizNum]);
					PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.03f, 1f);
					// self.Stun(20); //I THINK THIS MAKES THEM DROP THINGS
					BellyPlus.boostStrain[myZard] /= 2; //MAYBE THIS WILL HELP THE REPEATING POPS
				}
			}
			
			else
			{
				//Debug.Log("LZ! NOT STUCK... " + inPipe + " - " + BellyPlus.stuckCoords[myZard]);
				if (BellyPlus.stuckStrain[myZard] > 0)
					BellyPlus.stuckStrain[myZard] -= 2;
				else
                {
					BellyPlus.isStuck[myZard] = false;
					BellyPlus.stuckInShortcut[myZard] = false;
					BellyPlus.stuckCoords[myZard] = new Vector2(0, 0);
				}
				return; // NORMAL CASE OF NOT BEING STUCK
			}
		}

		//DETERMINES THE VALUE THEY MUST PASS IN ORDER TO SLIDE THROUGH
		Vector2 tilePosMod = new Vector2(vertStuck ? 0 : (2f * posMod * BellyPlus.myFlipValX[lizNum]), vertStuck ? (2f * posMod) * BellyPlus.myFlipValY[lizNum] : 0);
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
                sizeMod = 2;
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
            


			//if ((self.Template.type == CreatureTemplate.Type.GreenLizard || self.Template.type == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard))
			//	sizeMod = 2;
			//else if (self.Template.type == CreatureTemplate.Type.YellowLizard || self.Template.type == CreatureTemplate.Type.RedLizard)
			//	sizeMod = 1;
			//else if (self.Template.type == MoreSlugcatsEnums.CreatureTemplateType.EelLizard || self.Template.type == MoreSlugcatsEnums.CreatureTemplateType.ZoopLizard)
			//	sizeMod = -2;
			//else if (self.Template.type == MoreSlugcatsEnums.CreatureTemplateType.SpitLizard)
			//{
			//	naturalChub = 2;
			//	sizeMod = 2;
			//}
			

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
		
		BellyPlus.tileTightnessMod[lizNum] = 30 * (myChub + patch_Player.GetTileSizeMod(self, tilePosMod, GetMouseVector(self), tileSizeMod, inPipe, self.Submersion > 0, false));
		float squeezeThresh = BellyPlus.tileTightnessMod[lizNum];

		//IF OUR RESULT TURNS OUT TO BE 0 ANYWAYS, CANCEL THE STUCK
		if (squeezeThresh <= 0)
		{
			BellyPlus.noStuck[lizNum] = 30;
			if (squeezeThresh == 0 || squeezeThresh == -0.5f) //IF IT WAS EXACTLY OUR SIZE, PLAY A FUNNY SOUND
			{
				self.room.PlaySound(BPEnums.BPSoundID.Squinch1, self.mainBodyChunk, false, 0.1f, 1f - GetChubValue(self) / 10f);
				BellyPlus.shortStuck[lizNum] = 5; //TO ACCOMPANY THE SQUINCH~
				// Debug.Log("LZ!SQUINCH " + inPipe);
			}
			return;
		}


		//WE JUST NOW GOT STUCK --- PREPARE STUCK BWOMP!
		if (!BellyPlus.isStuck[lizNum] && (vertStuck || wedgedInFront || wedgedBehind))
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
				BellyPlus.stuckVector[lizNum] = new Vector2(0, flipper);
				crashVel = Mathf.Abs((self.bodyChunks[1].vel + (new Vector2(0, self.gravity) * 6f)).y) * 0.8f;
			}
			else
			{
				int flipper = (self.bodyChunks[1].vel.x > 0) ? 1 : -1; 
				BellyPlus.stuckVector[lizNum] = new Vector2(flipper, 0);
				crashVel = Mathf.Abs((self.bodyChunks[1].vel).x);
			}
			

			BellyPlus.stuckCoords[lizNum] = self.room.MiddleOfTile(self.bodyChunks[1].pos);
			
			//KEEP TRACK OF IF WE WERE COMING OUT OF A SHORTCUT
			if (self.shortcutDelay > 0)//(BellyPlus.freshFromShortcut[lizNum] > 0)
				BellyPlus.stuckInShortcut[lizNum] = true;
			
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
				PlayExternalSound(self, BPEnums.BPSoundID.Squinch1, 0.2f, 1.3f);

			if (crashVel > 8f) //WE'RE GOING PRETTY FAST! CUT OUR VEL IN HALF
			{
				PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.12f, 1f);
				for (int i = 0; i < self.bodyChunks.Length; i++)
				{
					self.bodyChunks[i].vel *= 0.2f;
				}
				//Debug.Log("LZ!WO-HOA THERE COWBOY! " + crashVel + " LOUNGING?" + (self.animation == Lizard.Animation.Lounge));
			}
			

			//LIZARDS ALWAYS DO THIS
			BellyPlus.fwumpDelay[lizNum] = 4;
			//BellyPlus.terrSqzMemory[lizNum] = self.bodyChunks[0].terrainSqueeze; //WE NEED FAT LIZARDS TO REMEMBER THIS SO THAT THEIR TERRAIN SQUEEZE DOESN'T REVERT TO 0 AND SEND THEM FLYING BACK
			if (BPOptions.debugLogs.Value)
				Debug.Log("NEW CRITTER STUCK VECTOR! " + BellyPlus.stuckVector[lizNum] + " SIZEMOD " + sizeMod );
			patch_Player.GetTileSizeMod(self, tilePosMod, GetMouseVector(self), tileSizeMod, inPipe, self.Submersion > 0, true); //	JUST FOR LOGS
		}


		//STUCK BWOMP!
		if (BellyPlus.fwumpDelay[lizNum] == 1)
        {
			float velMag = 0.0f + Mathf.Sqrt(crashVel * 2f);
			float vol = Mathf.Min((velMag / 5f), 0.25f);
			self.room.PlaySound(BPEnums.BPSoundID.Fwump2, self.mainBodyChunk, false, vol, 1.1f);
			// Debug.Log("LZ!-----BWOMP! JUST GOT STUCK " + velMag);

			for (int n = 0; n < 3; n++) //STRAIN DRIPS
			{
				Vector2 pos3 = self.bodyChunks[0].pos;
				float xvel = 4;
				self.room.AddObject(new WaterDrip(pos3, new Vector2((float)BellyPlus.myFlipValX[lizNum] * xvel, Mathf.Lerp(-2f, 6f, UnityEngine.Random.value)), false));
			}
		}
		

		//FROM THE DELAYED SHOVE
		if (BellyPlus.fwumpDelay[lizNum] == 8)
		{
			BellyPlus.stuckStrain[lizNum] += 60f;
			BellyPlus.fwumpDelay[lizNum] = 0;
		}


		//ASSISTED SQUEEZES VISUAL BOOST
		if (BellyPlus.assistedSqueezing[lizNum])
		{
			if (BellyPlus.boostStrain[lizNum] < 8)
				BellyPlus.boostStrain[lizNum] += 2;
			//WE NEED TI IMPOSE SOME SORT OF LIMIT ON THIS...
			BellyPlus.boostStrain[lizNum] = Math.Min(BellyPlus.boostStrain[lizNum], 18);
		}



		if (wedgedInFront || wedgedBehind)
		{
			BellyPlus.isStuck[lizNum] = true;
			BellyPlus.verticalStuck[lizNum] = false;
			float tileCheckOffset = (inPipe ? 0 : 10f) * BellyPlus.myFlipValX[lizNum]; //WELP, NOW IT WORKS WELL
			float pushBack = (self.room.MiddleOfTile(self.bodyChunks[1].pos).x + tileCheckOffset - self.bodyChunks[1].pos.x); // * BellyPlus.myFlipValX[lizNum];
			pushBack = (pushBack - (((xMove && !BellyPlus.lungsExhausted[lizNum]) ? 9.0f : 7.5f) * BellyPlus.myFlipValX[lizNum])) * 1.0f;
			//Debug.Log("LZ!-----SQUEEZED AGAINST AN X ENTRANCE!: " + BellyPlus.myFlipValX[lizNum] + " " + BellyPlus.inPipeStatus[lizNum] + " " + pushBack + " " + " ORIG VELOCITY " + self.bodyChunks[1].vel.x + " FORCE APPLIED: " + (pushBack + (BellyPlus.boostStrain[lizNum] * BellyPlus.myFlipValX[lizNum] / 5f)));

			if (Math.Abs(pushBack) > 15f)
				pushBack = 0; //SOMETHINGS GONE HORRIBLY WRONG


			if (xMove || BellyPlus.assistedSqueezing[lizNum])
			{
				//MAKE PROGRESS AS WE STRAIN. SELF STRUGGLING AND ASSISTED STRUGGLING CAN STACK
				if (xMove && !BellyPlus.lungsExhausted[lizNum])
					BellyPlus.stuckStrain[lizNum] += 2f;
				//OUR PUSHERS STRAIN WILL BE ADED ELSEWHERE, WHERE WE CAN CHECK THAT THEY ARENT EXHAUSTED FIRST

				if (BellyPlus.stuckStrain[lizNum] < squeezeThresh)
				{
					//NOW, DO WE WANT TO SLOW DOWN OUR MOVEMENT SPEED? OR OUR PHYSICAL VELOCITY?...
					//IF WE DO CORRCLIMBSPEED, PUT THIS IN UPDATEBODYMODE. IF WE DO VELOCITY, PUT IT AT THE END OF NORMAL UPDATE, SO IT UPDATES IF PLAYERS GRAB US
					float wornOut = 1 - GetExhaustionMod(self, 80);
					
					//MOVED TAIL WAGGLE INTO THE GRAPHICS MODULE
					
					//MICE ONLY
					if (self is LanternMouse && !BellyPlus.lungsExhausted[lizNum] && self.graphicsModule != null)
					{
						//NO TAIL WAGGLE. MICE GOT TEENY TAILS
						self.bodyChunks[0].vel.y = 0;
						//RE-ALIGN THE BODY CHUNKS (MOUSE SPECIFIC)
						if (self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y)
							self.bodyChunks[0].vel.y += 1f * wornOut;
						self.graphicsModule.bodyParts[4].vel.y += (0.1f) * BellyPlus.myFlipValY[lizNum] * wornOut; //I THINK THIS IS TOO MUCH
					}

					//LIZ TAILS
					if (self is Lizard && BellyPlus.beingPushed[lizNum] > 0)
					{
						self.bodyChunks[2].pos.y = self.bodyChunks[1].pos.y;
					}
				}
				else
				{
					PopFree(self, BellyPlus.stuckStrain[lizNum], BellyPlus.inPipeStatus[lizNum]);
					// Debug.Log("LZ!-----SLIIIIIIIIIDE THROUGH AN X ENTRANCE!: ");
					pushBack = 0;
				}
			}
			else
			{
				BellyPlus.stuckStrain[lizNum] = 0;
			}

			//OK WE NEED A FORMULA WHERE THAT, WHEN OUR X >= 9.5 FROM MID, VOL APPROACHES 0
			self.bodyChunks[1].vel.x += pushBack + (BellyPlus.boostStrain[lizNum] * BellyPlus.myFlipValX[lizNum] / 5f);
			if (self is Lizard)
					self.bodyChunks[2].vel.x += pushBack + (BellyPlus.boostStrain[lizNum] * BellyPlus.myFlipValX[lizNum] / 5f);
			//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
			if (self.bodyChunks[1].vel.x < 0 != BellyPlus.stuckVector[lizNum].x < 0)
				self.bodyChunks[1].vel.x /= 4; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING

		}


		//VERTICAL SQUEEZES
		else if (vertStuck)
		{
			BellyPlus.isStuck[lizNum] = true;
			BellyPlus.verticalStuck[lizNum] = true;
			float tileCheckOffset = (inPipe ? 0 : 10f) * BellyPlus.myFlipValY[lizNum]; //WELP, NOW IT WORKS WELL
			float pushBack = (self.room.MiddleOfTile(self.bodyChunks[1].pos).y + tileCheckOffset - self.bodyChunks[1].pos.y); // * BellyPlus.myFlipValY[lizNum];
			pushBack = (pushBack - ((yMove ? 9.5f : 7.5f) * BellyPlus.myFlipValY[lizNum])) * 1.0f;
			//Debug.Log("LZ!-----SQUEEZED AGAINST AN Y ENTRANCE!: " + " " + BellyPlus.inPipeStatus[lizNum] + " - " + pushBack + " YFLIP:" + BellyPlus.myFlipValY[lizNum] + " - " + BellyPlus.stuckStrain[lizNum] + " " + self.room.MiddleOfTile(self.bodyChunks[1].pos).y + " " + self.bodyChunks[1].pos.y);

			if (yMove || BellyPlus.assistedSqueezing[lizNum])
			{
				//MAKE PROGRESS AS WE STRAIN. SELF STRUGGLING AND ASSISTED STRUGGLING CAN STACK
				if (yMove && !BellyPlus.lungsExhausted[lizNum])
					BellyPlus.stuckStrain[lizNum] += 2;

				if (BellyPlus.stuckStrain[lizNum] < squeezeThresh)
				{
					//MOVED TAIL WAGGLE TO GRAPHICS MODULE
					
					//MICE ONLY
					if (self is LanternMouse && !BellyPlus.lungsExhausted[lizNum] && self.graphicsModule != null)
					{
						float wornOut = 1 - GetExhaustionMod(self, 60);
						if (BellyPlus.beingPushed[lizNum] < 1 && BellyPlus.myFlipValY[lizNum] < 0)
						{
							self.graphicsModule.bodyParts[5].vel.y += Mathf.Sqrt(BellyPlus.stuckStrain[lizNum] / 30f) * -BellyPlus.myFlipValY[lizNum] * wornOut; //TAIL OUT
							self.graphicsModule.bodyParts[5].vel.y += (BellyPlus.boostStrain[lizNum]) * -BellyPlus.myFlipValY[lizNum] * wornOut;
						}
						else if (BellyPlus.myFlipValY[lizNum] > 0)
						{
							self.graphicsModule.bodyParts[5].vel.x += (BellyPlus.boostStrain[lizNum] / 2) * BellyPlus.myFlipValX[lizNum] * wornOut;
							if (inPipe)
								self.bodyChunks[0].pos.x = self.bodyChunks[1].pos.x; //KEEP THE HEAD LEVEL SO IT DOESNT SQUEEZE OUT DIAGONALLY
						}
						self.graphicsModule.bodyParts[4].vel.y += (Mathf.Min(Mathf.Sqrt(BellyPlus.stuckStrain[lizNum] / 30f), 6f) + (BellyPlus.boostStrain[lizNum] / 2f)) * BellyPlus.myFlipValY[lizNum] * wornOut; //SNOUT OUT
					}


					//LIZ TAILS
					if (self is Lizard && BellyPlus.beingPushed[lizNum] > 0)
					{
						self.bodyChunks[2].pos.x = self.bodyChunks[1].pos.x;
					}
				}
				else
				{
					//HIGHER BOOST VALUE IF LAUNCHING UPWARDS
					float boostVel = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) ? BellyPlus.stuckStrain[lizNum] : BellyPlus.stuckStrain[lizNum] / 2f;
					PopFree(self, boostVel, BellyPlus.inPipeStatus[lizNum]);
					// Debug.Log("LZ!-----SLIIIIIIIIIDE THROUGH AN Y ENTRANCE!: ");
					pushBack = 0;
					
				}
			}
			else
			{
				BellyPlus.stuckStrain[lizNum] = 0;
			}

			//OK WE NEED A FORMULA WHERE THAT, WHEN OUR X >= 9.5 FROM MID, VOL APPROACHES 0
			self.bodyChunks[1].vel.y += pushBack + (BellyPlus.boostStrain[lizNum] * BellyPlus.myFlipValY[lizNum] / 6f);
			if (self is Lizard)
				self.bodyChunks[2].vel.y += pushBack + (BellyPlus.boostStrain[lizNum] * BellyPlus.myFlipValY[lizNum] / 6f);
			//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
			if (self.bodyChunks[1].vel.y < 0 != BellyPlus.stuckVector[lizNum].y < 0)
				self.bodyChunks[1].vel.y /= 4; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING
		}
		


		//------REDUCE STUCK STRAIN BASED ON THE FORMULA F(x) = (X(A-C))/500---------
		if (BellyPlus.stuckStrain[lizNum] > 0)
		{
			//A NEW VAR TO TRACK LOOSENING PROGRESS AS STRAINING CONTINUES
			BellyPlus.loosenProg[lizNum] += Mathf.Sqrt(Mathf.Max(Mathf.Min(BellyPlus.stuckStrain[lizNum], 250) - 180f, 0)) / 1000f; //LIZARDS LOOSEN MUCH FASTER
			//float counterStrain = (BellyPlus.stuckStrain[lizNum] * GetChubValue(self)) / 700f;
			float counterStrain = (BellyPlus.stuckStrain[lizNum] * ((BellyPlus.tileTightnessMod[lizNum] / 30) - BellyPlus.loosenProg[lizNum])) / 500f; //500f
			//Debug.Log("LZ!--------COUNTER STRAIN!: " + counterStrain + " TIGHTNESS: " + (BellyPlus.tileTightnessMod[lizNum] / 30) + " - " + (BellyPlus.tileTightnessMod[lizNum]) + " " + "STRAIN: " + BellyPlus.stuckStrain[lizNum] + "  EXHAUSTION:" + BellyPlus.corridorExhaustion[lizNum] + "  PROGRESS:" + BellyPlus.loosenProg[lizNum] + "  BOOST:" + BellyPlus.boostStrain[lizNum]);
			BellyPlus.stuckStrain[lizNum] -= counterStrain;
		}
	}




	public static void BPUUpdatePass1(Lizard self, int lizNum)
	{
		//Debug.Log("LZ!-----DEBUG!: " + BellyPlus.myFlipValX[lizNum] + " " + BellyPlus.inPipeStatus[lizNum] + " "  + " " + BellyPlus.stuckStrain[lizNum] + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);
		float myRunSpeed = (1f - (GetChubValue(self) / 10f) - Mathf.Lerp(0f, 0.2f, (GetOverstuffed(self) / 12f))) * ((IsStuck(self) || BellyPlus.pushingOther[lizNum]) ? 0.03f : 1f);
		//1.0 - 0.6 at chub 4.   0.4 at 12 overstuffed

		//11-28-22 CAN WE MAKE THESE GUYS WEDGE TOO? ONLY IF ON SCREEN, TO SAVE RESOURCES
		if (self.graphicsModule != null && BellyPlus.inPipeStatus[lizNum])
			myRunSpeed *= patch_Player.CheckWedge(self, false);
		
		self.AI.runSpeed = Mathf.Min(self.AI.runSpeed, 1f * myRunSpeed);
		
		//DISABLE THIS BODY CHUNK WHEN STUCK IN SHORTCUT
		self.bodyChunkConnections[2].active = !BellyPlus.stuckInShortcut[lizNum];

		//HEY OUR AI DOESN'T RUN WHILE GRABBED! WE HAVE TO RUN THIS HERE
		if (self.grabbedBy.Count > 0 && (self.grabbedBy[0].grabber is Player) && (self.AI.friendTracker.friend != null && self.AI.friendTracker.friend is Player)) //IF OUR FRIEND IS A PLAYER, ASSUME ALL PLAYERS ARE CHILL
		{
			self.grabbedAttackCounter = 0;
			self.JawOpen = 0;
		}
	}


	public static void BPUUpdatePass2(Creature self, int lizNum)
	{
		//Debug.Log("LZ LIZARD DEBUG!: NUM:" + lizNum + " BE:" + self.AI.behavior + " POS:" + self.bodyChunks[1].pos + " VEL:" + self.bodyChunks[1].vel + " STRAIN:" + BellyPlus.corridorExhaustion[lizNum] + " PIPE:" + BellyPlus.inPipeStatus[lizNum]);
		
		BellyPlus.assistedSqueezing[lizNum] = false;
		//-----THE MAIN SQUEEZE CHECK-----
		if ((patch_Player.IsGrabbedByHelper(self) || BellyPlus.beingPushed[lizNum] > 0) && GetChubValue(self) >= 0)
		{
			//FIRST CHECK IF WE'RE BEING GRABBED BY A HELPER
			if (patch_Player.IsCramped(self)) //(self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace)
			{
				BellyPlus.isSqueezing[lizNum] = true;
				BellyPlus.assistedSqueezing[lizNum] = true;
			}
			else
			{
				BellyPlus.isSqueezing[lizNum] = false;
				BellyPlus.assistedSqueezing[lizNum] = false;
			}
		}

		//IF WE HAVE ANY STUCK STRAIN AT ALL
		else if (BellyPlus.isStuck[lizNum] && BellyPlus.stuckStrain[lizNum] > 0)
			BellyPlus.isSqueezing[lizNum] = true;
		else if (BellyPlus.wedgeStrain[lizNum] > 0) //11/29/22 I THINK NPCS NEED THIS SPECIAL
			BellyPlus.isSqueezing[lizNum] = true;
		else
		{
			BellyPlus.isSqueezing[lizNum] = false;
			BellyPlus.assistedSqueezing[lizNum] = false;
		}


		if (BellyPlus.slicked[lizNum] > 0 )
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
			BellyPlus.slicked[lizNum]--;
		}


		//FAT LIZARDS NEED TO CONTINUE TO SUCK IN THEIR GUT AND NOT GET LAUNCHED OUT 
		//if (IsStuck(self))
		//{
		//	for (int n = 0; n < 3; n++)
		//	{
		//		self.bodyChunks[n].terrainSqueeze = BellyPlus.terrSqzMemory[lizNum];
		//	}
		//}

		//for (int n = 0; n < 3; n++)
		//{
		//	self.bodyChunks[n].terrainSqueeze = 1f;
		//}

	}


	//ALRIGHT. MAYBE WE JUST NEED A VALUE TO MANUALLY REFRESH ALL STUCK SOUNDS
	public static bool refreshSounds = false;

	public static void BPUUpdatePass3(Creature self, int lizNum)
	{
		bool offscreen = false;
		if (self.graphicsModule == null) // || )
        {
			//Debug.Log("NO GRAPHICS MODULE! END THE SOUND");
            //return;
            offscreen = true;
			if (BellyPlus.stuckLoop[lizNum] != null) //OTHERWISE WE CRASH
            {
				BellyPlus.stuckLoop[lizNum].alive = false;
				BellyPlus.stuckLoop[lizNum] = null;
			}
		}
		
		//OK, --------SECOND ONE FOR STUCK LOOP --------
		if (BellyPlus.stuckLoop[lizNum] != null && offscreen == false)
		{
			if (BellyPlus.isSqueezing[lizNum] == false || !patch_Player.IsStuckOrWedged(self) || refreshSounds)
			{
				BellyPlus.stuckLoop[lizNum].alive = false;
				BellyPlus.stuckLoop[lizNum] = null;
				if (refreshSounds)
					refreshSounds = false;
			}
			else
			{
				float myVel = self.bodyChunks[1].vel.magnitude / 3f; //THIS IS SIMPLER
				if (BellyPlus.lungsExhausted[lizNum]) //IF WE'RE OUT OF BREATH AND NOT BEING ASSISTED, CUT THE VOLUME. WE AREN'T MAKING PROGRESS.
					myVel = 0f;
				//TAKE THE VALUE HALFWAY BETWEEN OUR CURRENT VEL AND OUR PREVIOUS VALUE
				float maxVol = 2.5f;  //2.0f
				float speedVar = Mathf.Min(Mathf.Lerp(BellyPlus.myLastVel[lizNum], myVel, 0.3f), maxVol) + (BellyPlus.boostStrain[lizNum] / 100f) + (BellyPlus.assistedSqueezing[lizNum] ? 0.0f : 0f);
				float squeezeThresh = BellyPlus.tileTightnessMod[lizNum]; //ONE UNNESSESARY VARIABLE COMING UP // 60 * (GetChubValue(self) - 1);
				//speedVar += 4 * Mathf.Pow(((BellyPlus.stuckStrain[lizNum] - (squeezeThresh/3)) / squeezeThresh), 4); //f(x) = 4(x-(B/3)/B)^4  where B = 60
				speedVar += 1f / (100f * Mathf.Pow((BellyPlus.stuckStrain[lizNum] / squeezeThresh) - 1.11f, 2f)); //f(x) = 1/(100*(x-1.11)^2) == 0.87f AT FULL THRESHOLD
				//Debug.Log("LZ!-----MATH CHECK!: " + Mathf.Pow(((BellyPlus.stuckStrain[lizNum] - (squeezeThresh / 3)) / squeezeThresh), 4) + " " + squeezeThresh);

				//MAIN CHARACTERS (PETS) GET A BOOST TO VOLUME!
				float softenValue = 0.85f;
				if (self.safariControlled || (self is Lizard && (self as Lizard).AI.friendTracker.friend != null))
					softenValue = 1f;
				
				
				float volMod = 0;
				if (BellyPlus.wedgeStrain[lizNum] > 0f)
                {
					speedVar = ((myVel * 1f) + (BellyPlus.boostStrain[lizNum] / 50f)) / 1f; //+ BellyPlus.stuckLoop[lizNum].wedgeStrain
					volMod = Mathf.Max(BellyPlus.wedgeStrain[lizNum] - 0.3f, 0) * 1.3f;
					if (patch_Player.GetAxisMagnitude(self) < 0.04f && BellyPlus.boostStrain[lizNum] < 1)
                    {
						volMod = -1f;
					}
					 // Debug.Log("-----MATH CHECK!:" + GetAxisMagnitude(self));
                }
				
				
				float pitchMod = (patch_Player.ObjIsSlick(self) ? 0.1f : 0f);

				speedVar /= 2;//Mathf.Pow(speedVar, 2);
				BellyPlus.stuckLoop[lizNum].alive = true;
				BellyPlus.stuckLoop[lizNum].volume = Mathf.Lerp(0.24f + BPOptions.sfxVol.Value, 0.07f, speedVar - volMod) * softenValue;
				BellyPlus.stuckLoop[lizNum].pitch = Mathf.Lerp(0.4f, maxVol, speedVar) + pitchMod;
				BellyPlus.myLastVel[lizNum] = speedVar; //REMEMBER THAT VAL FOR NEXT TIME
			}
		}
		else if (patch_Player.IsStuckOrWedged(self) && offscreen == false && (BellyPlus.isSqueezing[lizNum] || BellyPlus.assistedSqueezing[lizNum])) //(num > 0f)
		{
			BellyPlus.stuckLoop[lizNum] = self.room.PlaySound(BPEnums.BPSoundID.SqueezeLoop, self.mainBodyChunk, true, 0f, 0f, true); //Vulture_Jet_LOOP
			BellyPlus.stuckLoop[lizNum].requireActiveUpkeep = true;
		}
	}

	
	//STUCK CHECK
	public static void BPUUpdatePass4(Creature self, int lizNum)
	{
		int bdy = patch_Player.ObjGetBodyChunkID(self, "middle");
		if (!IsStuck(self))
		{
			// FOR NPCS, THIS IS JUST ALWAYS BASED ON THE DIRECTION THEY'RE POINTING
			BellyPlus.myFlipValX[lizNum] = (self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x) ? 1 : -1;
			BellyPlus.myFlipValY[lizNum] = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) ? 1 : -1;
			
			if (self is Scavenger)
			{
				if (self.bodyChunks[bdy].vel.x > 0.1f)
					BellyPlus.myFlipValX[lizNum] = 1;
				else if (self.bodyChunks[bdy].vel.x < -0.1f)
					BellyPlus.myFlipValX[lizNum]= -1;
				
				if (self.bodyChunks[bdy].vel.y > 0.1f)
					BellyPlus.myFlipValY[lizNum] = 1;
				else if (self.bodyChunks[bdy].vel.y < -0.1f)
					BellyPlus.myFlipValY[lizNum] = -1;
			}
		}



		//------------MAKE PIPE ENTRANCES MORE DIFFICULT TO GET INTO-------
		if (GetChubValue(self) >= -1 && self.room != null) //ALRIGHT, I GUESS THIS SHOULD RUN FOR EVERYONE...
		{
			//IF WE ENDED UP IN A SHORTCUT WAY TOO FAST, MAKE A FORCED SQUINCH SOUND
			//if (!BellyPlus.inPipeStatus[lizNum] && self.inShortcut)
			if (self.enteringShortCut != null && BellyPlus.noStuck[lizNum] <= 0 && (self.bodyChunks[bdy].vel.magnitude > 7f))
			{
				self.room.PlaySound(BPEnums.BPSoundID.Squinch1, self.mainBodyChunk, false, 0.15f, 1.3f);
				PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.12f, 1f);
				if (BPOptions.debugLogs.Value)
					Debug.Log("LZ!---WE HIT THAT PIPE AWFULLY SPEEDY!: " + self.bodyChunks[bdy].vel.magnitude);
			}



			//BASIC CHECK TO SEE IF WE'RE ALL THE WAY INSIDE A CORRIDOR OR NOT.
			if (!BellyPlus.isStuck[lizNum] && self.room != null && self.room.aimap != null)
			{
				bool hipsInNarrow = self.room.aimap.getAItile(self.bodyChunks[1].pos).narrowSpace;
				//bool torsoInNarrow = self.room.aimap.getAItile(self.bodyChunks[0].pos).narrowSpace;
				bool torsoInNarrow = self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace;
				if ((hipsInNarrow && torsoInNarrow && BellyPlus.timeInNarrowSpace[lizNum] > 20) || self.inShortcut)
				{
					BellyPlus.inPipeStatus[lizNum] = true;
				}
				else if (!hipsInNarrow && !torsoInNarrow)
				{
					BellyPlus.inPipeStatus[lizNum] = false;
					BellyPlus.timeInNarrowSpace[lizNum] = 0;
				}
			}
			if (self is Lizard)
				CheckStuckage(self as Lizard);
			else if (self is LanternMouse)
				patch_LanternMouse.CheckStuckage(self as LanternMouse);
			else
				CheckStuckage(self);
		}

		//STRETCH OUT BASED ON STRAIN!
		/*
		float bodyStretch = Mathf.Min(BellyPlus.boostStrain[lizNum], 15f) * ((self.bodyMode == Player.BodyModeIndex.CorridorClimb) ? 2f : 0.7f );
		if (BellyPlus.beingPushed[lizNum] > 0 || (BellyPlus.verticalStuck[lizNum] && BellyPlus.myFlipValY[lizNum] > 0))
			bodyStretch *= 0.6f;
		self.bodyChunkConnections[0].distance += Mathf.Sqrt(bodyStretch);
		*/
		
		//OKAY BUT FOR LIZARDS...
		//HANDLE IT IN THEIR GRAPHICS MODULE
		
	}
	
	
	
	public static void BPUUpdatePass5(Creature self, int lizNum)
	{
		//----- CHECK IF WE'RE PUSHING ANOTHER CREATURE.------
		if (BellyPlus.pushingOther[lizNum] && self.graphicsModule != null)
		{
			//CAN I TENCHINCALLY JUST PASTE THE PLAYER VERSION IN HERE AT THIS POINT?
			Player myPartner = patch_Player.FindPlayerInRange(self);
			// LanternMouse mousePartner = FindLizardInRange(self);
			Lizard lizardPartner = FindLizardInRange(self);
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
				bool horzPushLine = patch_Player.ObjIsPushingOther(myObject) && BellyPlus.myFlipValX[lizNum] != 0 && BellyPlus.myFlipValX[lizNum] == patch_Player.ObjGetXFlipDirection(myObject);
				bool vertPushLine = patch_Player.ObjIsPushingOther(myObject) && BellyPlus.myFlipValY[lizNum] != 0 && BellyPlus.myFlipValY[lizNum] == patch_Player.ObjGetYFlipDirection(myObject);
				bool matchingShoveDir = ((patch_Player.ObjIsVerticalStuck(myObject) || vertPushLine) && BellyPlus.myFlipValY[lizNum] == patch_Player.ObjGetYFlipDirection(myObject)) || ((!patch_Player.ObjIsVerticalStuck(myObject) || horzPushLine) && BellyPlus.myFlipValX[lizNum] == patch_Player.ObjGetXFlipDirection(myObject));

				float slouch = 0f; //(Mathf.Max(Mathf.Min(bellyStats[playerNum].holdJump, 40) - 5f, 0f) / 2.5f) * (matchingShoveDir ? 1 : 0);

				if (!BellyPlus.lungsExhausted[lizNum] && matchingShoveDir)
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
						BellyPlus.boostStrain[lizNum] += 10;
						//BellyPlus.myHeat[lizNum] += 50;
						self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.2f, 0.6f);
						self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Medium, self.mainBodyChunk.pos, 1.0f, 1f);
					}

					
				}
				//IF IT'S A PUSHING LINE, PASS FORWARD THE BENEFITS!
				if (patch_Player.ObjBeingPushed(myObject) > 0)
					patch_Player.ObjGainStuckStrain(myObject, 0.25f);

				//CALCULATE A BOOST STRAIN MODIFIER THAT LOOKS A BIT SMOOTHER
				float pushBoostStrn = ((BellyPlus.boostStrain[lizNum] > 4) ? 4 : BellyPlus.boostStrain[lizNum]) + slouch;

				//WE NEED TWO SEPERATE FNS FOR VERTICAL/HORIZONTAL
				if (vertPushLine || patch_Player.ObjIsVerticalStuck(myObject) && (matchingShoveDir || slouch > 0))
				{
					//ACTUALLY, WE SHOULD MAKE LIZARDS RUN ALL PUSH SEARCHES FOR REARS INSTEAD OF MIDDLES. BECAUSE LIZARDS ARE NOT COORDINATED ENOUGH TO FIND EACH OTHERS TAILS
					float pushBack = 22f - Mathf.Abs(patch_Player.ObjGetBodyChunkPos(myObject, "middle").y - self.bodyChunks[0].pos.y) + (vertPushLine ? 10 : 0); // + (bellyStats[playerNum].boostStrain / 5f);
					pushBack -= pushBoostStrn; // (bellyStats[playerNum].boostStrain / 2); //BOOST STRAIN VISUALS
					// Debug.Log("LZ! ---I'M PUSHING Y! LETS SHOW SOME EFFORT: " + pushBack);
					pushBack = Mathf.Max(pushBack, 0);
					pushBack *= BellyPlus.myFlipValY[lizNum];
					self.bodyChunks[0].vel.y -= pushBack;
					
					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.y < 0 != BellyPlus.myFlipValY[lizNum] < 0)
						self.bodyChunks[0].vel.y /= 3f;
					
					if (self.bodyChunks[1].vel.y < 0 != BellyPlus.myFlipValY[lizNum] < 0)
						self.bodyChunks[1].vel.y /= 3f;
					
					if (self.bodyChunks[2].vel.y < 0 != BellyPlus.myFlipValY[lizNum] < 0)
						self.bodyChunks[2].vel.y /= 3f;
				}
				else if (horzPushLine || !patch_Player.ObjIsVerticalStuck(myObject) && (matchingShoveDir  || slouch > 0))
				{
					float pushBack = Mathf.Max(25f - pushBoostStrn + (horzPushLine ? 10 : 0) - Mathf.Abs(patch_Player.ObjGetBodyChunkPos(myObject, "middle").x - self.bodyChunks[0].pos.x), 0f);
					//pushBack = Mathf.Max(pushBack * (1f - slouch/20f), 0);
					pushBack *= BellyPlus.myFlipValX[lizNum];
					// Debug.Log("LZ!---I'M PUSHING X! LETS SHOW SOME EFFORT: " + pushBack + " " + self.bodyChunks[0].vel.x + " PUSHING LINE?" + horzPushLine);

					//IF THEYRE A TILE ABOVE US, REDUCE ALL THIS
					if (Mathf.Abs(patch_Player.ObjGetBodyChunkPos(myObject, "middle").y - self.bodyChunks[0].pos.y) > 10)
						pushBack /= 3f;

					// Debug.Log("LZ! PUSHBACK SHOVE!: " + pushBack);
					self.bodyChunks[0].vel.x -= pushBack * (1.0f);
					self.bodyChunks[1].vel.x -= pushBack * (1.4f); 
					self.bodyChunks[2].vel.x -= pushBack * (1.8f); //LIZARD SPECIFIC

					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.x < 0 != BellyPlus.myFlipValX[lizNum] < 0) //Mathf.Abs(self.bodyChunks[0].vel.x) > 4 || 
						self.bodyChunks[0].vel.x /= (matchingShoveDir ? 2.5f : 1); //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING

					if (self.bodyChunks[1].vel.x < 0 != BellyPlus.myFlipValX[lizNum] < 0) //Mathf.Abs(self.bodyChunks[0].vel.x) > 4 || 
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

		if (BellyPlus.noStuck[lizNum] > 0)
			BellyPlus.noStuck[lizNum]--;
		
	}

	
	
	
	
	public static void BPUUpdatePass5_2(Creature self, int lizNum)
	{
		//LET CREATURES BOOST TOO! JUST DO IT DIFFERENTLY...
		// bool matchingStuckDir = (IsVerticalStuck(self) && self.input[0].y != 0) || (!IsVerticalStuck(self) && self.input[0].x != 0);
		if (((BellyPlus.boostTimer[lizNum] < 1 && BellyPlus.stuckStrain[lizNum] > 65) || (BellyPlus.SafariJumpButton(self) && BellyPlus.boostTimer[lizNum] < 10)) && !BellyPlus.lungsExhausted[lizNum] && ((BellyPlus.isStuck[lizNum] || patch_Player.ObjIsWedged(self)) || BellyPlus.pushingOther[lizNum] || BellyPlus.pullingOther[lizNum])) //self.AI.excitement > 0.4f && 
		{
			if (patch_Player.ObjIsWedged(self))
				BellyPlus.boostStrain[lizNum] += 4;
			else
				patch_Player.ObjGainBoostStrain(self, 0, 10, 18);

			BellyPlus.corridorExhaustion[lizNum] += 22; //30
			int boostAmnt = 15;
			// self.AerobicIncrease(1f);
			float strainMag = 15f * GetExhaustionMod(self, 60);
			// Debug.Log("LZ!----- LIZARD BOOSTING! " + lizNum + "- Pushing other?" + BellyPlus.pushingOther[lizNum]);

			//EXTRA STRAIN PARTICALS!

			if (self.graphicsModule != null)
			{
				for (int n = 0; n < 5 + (strainMag / 4); n++)
				{
					//Vector2 pos = self.graphicsModule.bodyParts[4 + self.lizardParams.tailSegments].pos;
					Vector2 pos = patch_Player.ObjGetHeadPos(self);
					if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
					{
						//self.room.AddObject(new WaterDrip(pos3, new Vector2((float)BellyPlus.myFlipValX[lizNum] * 10, Mathf.Lerp(-4f, 4f, UnityEngine.Random.value)), false));
						//self.room.AddObject(new WaterDrip(pos3, new Vector2((float)BellyPlus.myFlipValX[lizNum] * -10, Mathf.Lerp(-4f, 4f, UnityEngine.Random.value)), false));
						self.room.AddObject(new StrainSpark(pos, GetMouseAngle(self).ToVector2() + Custom.DegToVec(180f * UnityEngine.Random.value) * 6f * UnityEngine.Random.value, 15f, Color.white));
					}
				}
			}
			//self.slowMovementStun += 15;
			// self.jumpChunkCounter = 15;
			BellyPlus.boostTimer[lizNum] = 12 + (Mathf.FloorToInt(UnityEngine.Random.value * 10)); // - Mathf.FloorToInt(Mathf.Lerp(10, 30, self.AI.fear));

			if (BellyPlus.isStuck[lizNum])
			{
				BellyPlus.stuckStrain[lizNum] += boostAmnt;
				//BellyPlus.loosenProg[lizNum] += boostAmnt / 1000f; //LIZARDS LOOSEN MUCH FASTER
				patch_Player.ObjGainLoosenProg(self, (boostAmnt / 2000f));
            }
			else if (patch_Player.ObjIsWedged(self))
			{
				BellyPlus.stuckStrain[lizNum] += boostAmnt;
				BellyPlus.loosenProg[lizNum] += (boostAmnt * (patch_Player.ObjIsSlick(self) ? 3f : 1f)) / 8000; //boostAmnt / 2000f;
				//bellyStats[playerNum].loosenProg += (boostAmnt * (ObjIsSlick(self) ? 3f : 1f)) / loosenMod;
				self.room.PlaySound(SoundID.Slugcat_In_Corridor_Step, self.mainBodyChunk, false, 0.6f + BellyPlus.wedgeStrain[lizNum] * 2, 0.6f + BellyPlus.wedgeStrain[lizNum] / 2f);
				if (patch_Player.ObjIsSlick(self))
					self.room.PlaySound(SoundID.Tube_Worm_Shoot_Tongue, self.mainBodyChunk, false, 1.0f, 1f);
			}
			if (BellyPlus.pushingOther[lizNum])
            {
				Player myPartner = patch_Player.FindPlayerInRange(self);
				Lizard lizardPartner = patch_Player.FindLizardInRange(self, 0, 2);
				Scavenger scavPartner = null;

				Creature myObject = null;
				if (myPartner != null)
					myObject = (myPartner as Creature);
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
			else if (BellyPlus.pullingOther[lizNum] && self.grasps[0] != null && self.grasps[0].grabbed != null && self.grasps[0].grabbed is Creature)
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
	
	
	public static void BPUUpdatePass6(Creature self, int lizNum)
	{
		//THIS PART WE CAN RUN LIKE A NORMAL PERSON
		if (BellyPlus.boostStrain[lizNum] > 0)
			BellyPlus.boostStrain[lizNum]--;
		
		if (BellyPlus.beingPushed[lizNum] > 0)
			BellyPlus.beingPushed[lizNum]--;
		
		if (BellyPlus.shortStuck[lizNum] > 0)
			BellyPlus.shortStuck[lizNum]--;
		
		if (BellyPlus.boostTimer[lizNum] > 0)
			BellyPlus.boostTimer[lizNum]--;

		if (!IsStuck(self) && BellyPlus.loosenProg[lizNum] > 0)
			BellyPlus.loosenProg[lizNum] -= 1 / 2000f;


		if (BellyPlus.corridorExhaustion[lizNum] > 0 && !BellyPlus.lungsExhausted[lizNum])
		{
			BellyPlus.corridorExhaustion[lizNum]--;
			if (BellyPlus.corridorExhaustion[lizNum] > maxStamina)
			{
				// Debug.Log("LZ!----- OOF, IM EXHAUSTED! ");
				BellyPlus.lungsExhausted[lizNum] = true;
				BellyPlus.corridorExhaustion[lizNum] = -200; //A LITTLE WEIRD BUT TRUST ME.
			}
		}
		else if (BellyPlus.lungsExhausted[lizNum]) //IF EXHAUSTED, WE COUNT UPWARDS
		{
			BellyPlus.corridorExhaustion[lizNum]++;
			if (BellyPlus.corridorExhaustion[lizNum] > 0)
				BellyPlus.lungsExhausted[lizNum] = false; //LUNGS BACK TO NORMAL
		}
		
	}


	public static void BPLizard_Update(On.Lizard.orig_Update orig, Lizard self, bool eu)
	{
		int lizNum = GetRef(self);
		int origTimeSpentTrying = self.timeSpentTryingThisMove;

		BPUUpdatePass1(self, lizNum);

		orig.Invoke(self, eu);

		//THIS PART IS IMPORTANT BECAUSE IF THIS RESETS TO 0 WHEN THEY GET STUCK, IT'S LIKE THEIR TERRAIN SQUEEZE IS RESET AND THEY GO FLYING OUT IF THEIR BODYCHUNK RAD WAS TOO FAT FOR THE SPACE
		if (self.timeSpentTryingThisMove == 0 && IsStuck(self))
			self.timeSpentTryingThisMove = origTimeSpentTrying;

		if (self == null || self.dead) //OKAY??? I GUESS THIS WORKS???
		{
			return;
		}

		//bool mlungsExhausted = BellyPlus.pushingOther[GetRef(self)];
		if (self.room != null)
		{ 
			BPUUpdatePass2(self, lizNum);
			BPUUpdatePass3(self, lizNum);
			BPUUpdatePass4(self, lizNum);
			if (self.stun <= 0 )
			{
				BPUUpdatePass5(self, lizNum);
				BPUUpdatePass5_2(self, lizNum);
			}
		}
		BPUUpdatePass6(self, lizNum);
	}





	public static void PopFree(Creature self, float power, bool inPipe)
	{
		int lizNum = GetRef(self);
		float popMag = Mathf.Min(power / 120f, 2f); //CAP OUT AT 2
		BellyPlus.noStuck[lizNum] = 25;
		BellyPlus.loosenProg[lizNum] = 0;
		if (BPOptions.debugLogs.Value)
			Debug.Log("LZ!-----POP!: " + popMag + " - " + BellyPlus.stuckStrain[lizNum]);
		float popVol = Mathf.Lerp(0.12f, 0.28f, Mathf.Min(popMag, 1f));
		float stuckStrainMemory = BellyPlus.stuckStrain[lizNum];
		BellyPlus.stuckStrain[lizNum] = 6; //FAST PASS TO FIX VOLUME N STUFF
		BellyPlus.squeezeStrain[lizNum] = 0; //SO WE DON'T ALSO GET THE POP
		BellyPlus.inPipeStatus[lizNum] = !BellyPlus.inPipeStatus[lizNum]; //FLIPFLOP OUR PIPE STATUS
		BellyPlus.isStuck[lizNum] = false;
		BellyPlus.verticalStuck[lizNum] = false;
		BellyPlus.stuckInShortcut[lizNum] = false;
		BellyPlus.stuckCoords[lizNum] = new Vector2(0, 0);
		// BellyPlus.slicked[lizNum] /= 2;


        //TELEPORT US 0.5 OUT THE HOLE B) - DOESN'T ACTUALLY SEEM TO MAKE A BIG DIFFERENCE... -okay, maybe they do... for dumb green lizards
        self.bodyChunks[0].pos += BellyPlus.stuckVector[lizNum] * 10f;
		self.bodyChunks[1].pos += BellyPlus.stuckVector[lizNum] * 10f; //new Vector2(GetMouseVector(self).x * 10f, GetMouseVector(self).y * 10f);

		
		if (!inPipe)
			self.room.PlaySound(SoundID.Slugcat_Turn_In_Corridor, self.mainBodyChunk, false, popMag, Mathf.Sqrt(popMag));
		
		float launchSpeed = inPipe ? 6f : 9f;
		launchSpeed *= (BellyPlus.slicked[lizNum] > 0 ? 1.5f : 1f);
		Vector2 inputVect = BellyPlus.stuckVector[lizNum] * launchSpeed * popMag;
		
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
		if (self is Lizard && (self as Lizard).AI.friendTracker.friend != null)
			softenValue = 1f;


		//-------------POP SOUND-----------
		popVol += (-0.1f + BPOptions.sfxVol.Value);
		PlayExternalSound(self, BPEnums.BPSoundID.Pop1, (popVol / (!inPipe ? 2.5f : 2f)) * softenValue, 1f);

		//CREATE SOME FUN SPARK FX
		int sparkCount = Mathf.FloorToInt(Mathf.Lerp(0f, 12f, popMag)); //DEFAULT WAS 8
		if (popMag < 0.1)
			sparkCount = 0;
		
		patch_Player.MakeSparks(self, 0, sparkCount);
	}

}