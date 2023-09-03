using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;



public class patch_LanternMouse
{
	
	public static void Patch()
	{
		On.LanternMouse.ctor += (MousePatch);
		
		On.LanternMouse.Update += BPLanternMouse_Update;
		On.LanternMouse.SpitOutOfShortCut += LanternMouse_SpitOutOfShortCut;
		On.LanternMouse.Die += BP_Die;
		
	}


	public static Dictionary<int, LanternMouse> mouseBook = new Dictionary<int, LanternMouse>(0);

	private static void MousePatch(On.LanternMouse.orig_ctor orig, LanternMouse self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);
		
		int mouseNum = self.abstractCreature.ID.RandomSeed;

		//MAKE SURE THERE ISN'T ALREADY A MOUSE WITH OUR NAME ON THIS!
		bool mouseExists = false;
        try
        {
			//ADD OURSELVES TO THE GUESTBOOK
			patch_LanternMouse.mouseBook.Add(mouseNum, self);
		}
		catch (ArgumentException)
        {
			mouseExists = true;
		}

		if (mouseExists)
        {
			// Debug.Log("MOUSE ALREADY EXISTS! CANCELING: " + mouseNum);
			patch_LanternMouse.mouseBook[mouseNum] = self; //WELL HOLD ON! WE STALL NEED THE REFERENCE FROM THAT BOOK TO POINT TO US!
			UpdateBellySize(self);
			return;
		}
		
		BellyPlus.InitializeCreature(mouseNum);

		//NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
		int seed = UnityEngine.Random.seed;
		UnityEngine.Random.seed = mouseNum;
		
		int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
		if (patch_DLL.CheckFattable(self) == false)
			critChub = 0;
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("MOUSE SPAWNED! CHUB SIZE: " + critChub);
		
		BellyPlus.myFoodInStomach[mouseNum] = critChub;
		
		UpdateBellySize(self);

        if (BellyPlus.parasiticEnabled)
            BellyPlus.InitPSFoodValues(abstractCreature);
    }
	
	public static int GetRef(LanternMouse self)
	{
		return self.abstractCreature.ID.RandomSeed;
	}

	
	public static IntVector2 GetMouseAngle(LanternMouse self)
	{
		return patch_Lizard.GetMouseAngle(self);
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



	//SOUNDS THAT DON'T GET INTERRUPTED!
	public static void PlayExternalSound(LanternMouse self, SoundID soundId, float sVol, float sPitch)
	{
		Vector2 pos = self.mainBodyChunk.pos;
		self.room.PlaySound(soundId, pos, sVol, sPitch);
	}
	
	//FIND THE NEAREST MOUSEY~
	public static LanternMouse FindMouseInRange(Creature self)
	{
		foreach(KeyValuePair<int, LanternMouse> kvp in patch_LanternMouse.mouseBook)
        {
            if (
				kvp.Value != null
				&& kvp.Value != self
				&& kvp.Value.room == self.room
				&& kvp.Value.dead == false
				&& Custom.DistLess(self.mainBodyChunk.pos, kvp.Value.bodyChunks[1].pos, 35f)
			)
			{
				return kvp.Value as LanternMouse;
			}
        }
		return null;
	}


	public static void UpdateBellySize(LanternMouse self)
	{
		float baseWeight = 0.2f; //I THINK...
		int currentFood = 7 - BellyPlus.myFoodInStomach[GetRef(self)];
		patch_Lizard.UpdateChubValue(self);
		
		if (BellyPlus.VisualsOnly())
			return;

		switch (Math.Max(8 - currentFood, -1))
		{
			case -1:
				BellyPlus.myCooridorSpeed[GetRef(self)] = 0.30f; //10
				self.bodyChunks[0].mass = baseWeight * 1.5f;
				self.bodyChunks[1].mass = baseWeight * 1.5f;
				break;
			case 0:
				BellyPlus.myCooridorSpeed[GetRef(self)] = 0.45f; //15
				self.bodyChunks[0].mass = baseWeight * 1.3f;
				self.bodyChunks[1].mass = baseWeight * 1.3f;
				break;
			case 1:
				BellyPlus.myCooridorSpeed[GetRef(self)] = 0.65f; //35
				self.bodyChunks[0].mass = baseWeight * 1.1f;
				self.bodyChunks[1].mass = baseWeight * 1.1f;
				break;
			case 2:
				BellyPlus.myCooridorSpeed[GetRef(self)] = 0.85f;
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
		return BellyPlus.isStuck[GetRef(self)];
	}

	public static bool IsVerticalStuck(LanternMouse self)
	{
		return BellyPlus.verticalStuck[GetRef(self)];
	}

	public static void LanternMouse_SpitOutOfShortCut(On.LanternMouse.orig_SpitOutOfShortCut orig, LanternMouse self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
	{
		orig.Invoke(self, pos, newRoom, spitOutAllSticks);
		patch_Lizard.Creature_SpitOutOfShortCut(self, pos, newRoom);
	}

	public static void BP_Die(On.LanternMouse.orig_Die orig, LanternMouse self)
	{
		BellyPlus.isStuck[GetRef(self)] = false;
		orig.Invoke(self);
	}

	public static void CheckStuckage(LanternMouse self)
	{
		patch_Lizard.CheckStuckage(self); //WE CAN MERGE THESE... NICE
	}



	public static void BPUUpdatePass1(LanternMouse self, int critNum)
	{
		//VERSION FOR MICE!
		if ((BellyPlus.isStuck[critNum] || BellyPlus.pushingOther[critNum]) && self.runCycle > 0)
			self.runCycle = 0.8f;

		//Debug.Log("MS!-----DEBUG!: " + BellyPlus.myFlipValX[critNum] + " " + BellyPlus.inPipeStatus[critNum] + " "  + " " + BellyPlus.stuckStrain[critNum] + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);

		if (BellyPlus.myHeat[critNum] < 1250 && (BellyPlus.assistedSqueezing[critNum] || BellyPlus.pushingOther[critNum] || BellyPlus.isStuck[critNum]))
		{
			BellyPlus.myHeat[critNum]++;
			//Debug.Log("MS!-----DEBUG!: " + BellyPlus.myHeat[critNum]);
		}
		else if (BellyPlus.myHeat[critNum] > 0)
		{
			BellyPlus.myHeat[critNum]--;
		}

		if (BellyPlus.wideEyes[critNum] > 0)
			BellyPlus.wideEyes[critNum]--;

		//Debug.Log("MS!-----DEBUG!: " + self.AI.fear + " _ " + self.runSpeed + " _BE:" + self.AI.behavior + " _BT:" + BellyPlus.boostTimer[critNum] + " _BT:" + BellyPlus.lungsExhausted[critNum]);
		

		//RECALCULATE RUN SPEED
		float myRunSpeed = (1f - (patch_Lizard.GetChubValue(self) / 12f)) * (IsStuck(self) ? 0.01f : 1f);

		if (self.graphicsModule != null && BellyPlus.inPipeStatus[critNum])
			myRunSpeed *= patch_Player.CheckWedge(self, false);

		//LOL, I GUESS WE CAN'T USE SWITCHES IN THE NEW VERSION?...
		/*
		switch (self.AI.behavior)
        {
			case MouseAI.Behavior.Idle:
				self.runSpeed = Mathf.Min(self.runSpeed , 0.5f * myRunSpeed); //ALTERING RUN SPEED BY WEIGHT
				break;
			case MouseAI.Behavior.Flee:
			case MouseAI.Behavior.EscapeRain:
				self.runSpeed = Mathf.Min(self.runSpeed, 1f * myRunSpeed);
				break;
		}
		*/

		//FINE I GUESS
		if (self.AI.behavior == MouseAI.Behavior.Idle)
			self.runSpeed = Mathf.Min(self.runSpeed, 0.5f * myRunSpeed); //ALTERING RUN SPEED BY WEIGHT
		else if (self.AI.behavior == MouseAI.Behavior.Flee || self.AI.behavior == MouseAI.Behavior.EscapeRain)
			self.runSpeed = Mathf.Min(self.runSpeed, 1f * myRunSpeed);



	}

	
	public static void BPUUpdatePass2(LanternMouse self, int critNum)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass2(self, critNum);
	}
	
	
	
	public static void BPUUpdatePass3(LanternMouse self, int critNum)
	{
		//LIZARDS ACTUALLY WORKS FOR US TOO! DON'T MIND IF I DO...
		patch_Lizard.BPUUpdatePass3(self, critNum);
	}
	
	
	public static void BPUUpdatePass4(LanternMouse self, int critNum)
	{
		patch_Lizard.BPUUpdatePass4(self, critNum);
	}
	
	
	public static void BPUUpdatePass5(LanternMouse self, int critNum)
	{
		//----- CHECK IF WE'RE PUSHING ANOTHER CREATURE.------
		if (BellyPlus.pushingOther[critNum])
		{
			LanternMouse myPartner = FindMouseInRange(self);
			if (myPartner != null)
			{
				bool horzPushLine = patch_Player.ObjIsPushingOther(myPartner) && BellyPlus.myFlipValX[critNum] == BellyPlus.myFlipValX[GetRef(myPartner)];
				bool vertPushLine = patch_Player.ObjIsPushingOther(myPartner) && BellyPlus.myFlipValY[critNum] == BellyPlus.myFlipValY[GetRef(myPartner)];
				bool matchingShoveDir = ((IsVerticalStuck(myPartner) || vertPushLine) && true) || ((!IsVerticalStuck(myPartner) || horzPushLine)); // && self.input[0].x == myFlipValX[GetRef(myPartner)]);
				if (!BellyPlus.lungsExhausted[critNum] && matchingShoveDir)
				{
					BellyPlus.stuckStrain[GetRef(myPartner)] += 0.5f;
				}
				//IF IT'S A PUSHING LINE, PASS FORWARD THE BENEFITS!
				if (BellyPlus.beingPushed[critNum] > 0)
					BellyPlus.stuckStrain[GetRef(myPartner)] += 0.25f;

				//CALCULATE A BOOST STRAIN MODIFIER THAT LOOKS A BIT SMOOTHER
				float pushBoostStrn = (BellyPlus.boostStrain[critNum] > 4) ? 4 : BellyPlus.boostStrain[critNum];

				//WE NEED TWO SEPERATE FNS FOR VERTICAL/HORIZONTAL
				if (vertPushLine || IsVerticalStuck(myPartner) && matchingShoveDir)
				{
					float pushBack = 22f - Mathf.Abs(myPartner.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y) + (vertPushLine ? 10 : 0); // + (BellyPlus.boostStrain[critNum] / 5f);
					pushBack -= pushBoostStrn; // (BellyPlus.boostStrain[critNum] / 2); //BOOST STRAIN VISUALS
											   //Debug.Log("MS!---I'M PUSHING Y! LETS SHOW SOME EFFORT: " + pushBack);
					pushBack = Mathf.Max(pushBack, 0);
					pushBack *= BellyPlus.myFlipValY[critNum];

					self.bodyChunks[0].vel.y -= pushBack;
					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.y < 0 != GetMouseVector(self).y < 0)
						self.bodyChunks[0].vel.y /= 3f;


				}
				else if (horzPushLine || !IsVerticalStuck(myPartner) && matchingShoveDir)
				{
					float pushBack = 25f - Mathf.Abs(myPartner.bodyChunks[1].pos.x - self.bodyChunks[0].pos.x) + (horzPushLine ? 10 : 0);
					pushBack -= pushBoostStrn / 1f; //(BellyPlus.boostStrain[critNum] / 2); //BOOST STRAIN VISUALS
													//Debug.Log("MS!---I'M PUSHING X! LETS SHOW SOME EFFORT: " + pushBack + " " + self.bodyChunks[0].vel.x);
					pushBack = Mathf.Max(pushBack, 0);
					pushBack *= BellyPlus.myFlipValX[critNum];

					//IF THEYRE A TILE ABOVE US, REDUCE ALL THIS
					if (Mathf.Abs(myPartner.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y) > 10)
					{
						pushBack /= 3f;
					}

					//CHECK FOR RUNNING START!
					//NOT FOR MICE!

					self.bodyChunks[0].vel.x -= pushBack;
					self.bodyChunks[1].vel.x -= pushBack * (1.4f); // + (BellyPlus.boostStrain[critNum] / 10f));
																   //CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.x < 0 != GetMouseVector(self).x < 0) //Mathf.Abs(self.bodyChunks[0].vel.x) > 4 || 
					{
						self.bodyChunks[0].vel.x /= 3f;
						self.bodyChunks[1].vel.x /= 3f; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING
					}
				}
			}
		}
		
		if (BellyPlus.noStuck[critNum] > 0)
			BellyPlus.noStuck[critNum]--;
	}
		
		
	public static void BPUUpdatePass5_2(LanternMouse self, int critNum)
	{
		//LET MICE BOOST TOO! JUST DO IT DIFFERENTLY...
		// bool matchingStuckDir = (IsVerticalStuck(self) && self.input[0].y != 0) || (!IsVerticalStuck(self) && self.input[0].x != 0);
		if (((BellyPlus.boostTimer[critNum] < 1 && BellyPlus.stuckStrain[critNum] > 65) || (BellyPlus.SafariJumpButton(self) && BellyPlus.boostTimer[critNum] < 10))&& !BellyPlus.lungsExhausted[critNum] && (IsStuck(self) || BellyPlus.pushingOther[critNum]))
		{
			if (patch_Player.ObjIsWedged(self))
				BellyPlus.boostStrain[critNum] += 4;
			else
				BellyPlus.boostStrain[critNum] += 10;

			BellyPlus.corridorExhaustion[critNum] += 30;
			int boostAmnt = 15;
			// self.AerobicIncrease(1f);
			float strainMag = 15f * GetExhaustionMod(self, 60);
			if (BPOptions.debugLogs.Value)
				Debug.Log("MS!----- MOUSE BOOSTING! ");

			//EXTRA STRAIN PARTICALS!
			
			if (self.graphicsModule != null)
			{
				for (int n = 0; n < 3 + (strainMag / 5); n++)
				{
					Vector2 pos = self.graphicsModule.bodyParts[4].pos;
					if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
					{
						//self.room.AddObject(new WaterDrip(pos3, new Vector2((float)BellyPlus.myFlipValX[critNum] * 10, Mathf.Lerp(-4f, 4f, UnityEngine.Random.value)), false));
						//self.room.AddObject(new WaterDrip(pos3, new Vector2((float)BellyPlus.myFlipValX[critNum] * -10, Mathf.Lerp(-4f, 4f, UnityEngine.Random.value)), false));

						self.room.AddObject(new StrainSpark(pos, self.mainBodyChunk.vel + Custom.DegToVec(180f * UnityEngine.Random.value) * 6f * UnityEngine.Random.value, 15f, Color.white));
					}
				}
			}


			//self.slowMovementStun += 15;
			// self.jumpChunkCounter = 15;
			BellyPlus.boostTimer[critNum] += 15 + (Mathf.FloorToInt(UnityEngine.Random.value * 8)) - (self.AI.fear > 0.4f ? 8 : 0); // - Mathf.FloorToInt(Mathf.Lerp(10, 30, self.AI.fear));

			if (IsStuck(self))
			{
				BellyPlus.stuckStrain[critNum] += boostAmnt;
				BellyPlus.loosenProg[critNum] += boostAmnt / 4000f;
			}
			else if (BellyPlus.pushingOther[critNum])
            {
				//WE'LL SKIP THE SLUGCAT'S VERSION. IT PROBABLY ISNT TOO EAGER TO HELP...
				LanternMouse mousePartner = FindMouseInRange(self);
				if (mousePartner != null) // && self.input[0].x == bellyStats[myPartner.playerState.playerNumber].myFlipValX)
				{
					//EH, ALL THEY NEED TO DO IS BE CLOSE
					BellyPlus.stuckStrain[GetRef(mousePartner)] += boostAmnt / 2;
					BellyPlus.loosenProg[GetRef(mousePartner)] += boostAmnt / 8000f;
				}
			}
		}
	}
	
	
	public static void BPUUpdatePass6(LanternMouse self, int critNum)
	{
		patch_Lizard.BPUUpdatePass6(self, critNum);
	}



	public static void BPLanternMouse_Update(On.LanternMouse.orig_Update orig, LanternMouse self, bool eu)
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


	public static void PopFree(LanternMouse self, float power, bool inPipe)
	{
		patch_Lizard.PopFree(self, power, inPipe);
	}

}