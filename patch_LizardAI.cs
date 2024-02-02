using System;
using RWCustom;
using UnityEngine;
using MoreSlugcats;


namespace RotundWorld;

public class patch_LizardAI
{
	public static void Patch()
    {
		On.LizardAI.Update += BP_Update;
		On.LizardAI.DoIWantToHoldThisWithMyTongue += Lizard_DoIWantToHoldThisWithMyTongue;
		// On.LizardAI.ctor += BPLizardAI_ctor;
	}
	
	public static void BPLizardAI_ctor(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world)
    {
        orig.Invoke(self, creature, world);
		
    }


	public static bool Lizard_DoIWantToHoldThisWithMyTongue(On.LizardAI.orig_DoIWantToHoldThisWithMyTongue orig, LizardAI self, BodyChunk chunk)
	{
		if (chunk != null && chunk.owner != null && chunk.owner is Player && patch_Player.IsStuckOrWedged(chunk.owner as Player))
		{
			//NOWS OUR CHANCE TO ADD SOME UTILITY!
			patch_Player.ObjGainStuckStrain(chunk.owner as Player, 2.0f);
			patch_Player.ObjGainLoosenProg(chunk.owner as Player, 2f / 1000f);
			return true;
		}
		else
			return orig.Invoke(self, chunk);
	}


	//THIS IS GONNA MAKE THE TIMING ALL WIERD IF WE HAVE MULTIPLE PET LIZARDS FOLLOWING US BUT... OH WELL
	private static Vector2 commandPos = new Vector2(0, 0);
	private static int commandTimer = 0;
	private static int pointCounter = 0;


	public static void BP_Update(On.LizardAI.orig_Update orig, LizardAI self)
    {
		float origRunspeed = self.runSpeed;

		orig.Invoke(self);
		
		 if (BellyPlus.VisualsOnly())
			 return;

        
		//MORNING UPDATE OUR SAVED BELLY SIZE
		if (self.friendTracker.friend != null && (self.friendTracker.friend as Player).stillInStartShelter && BellyPlus.lizardFood != 0)
		{
			self.creature.GetAbsBelly().myFoodInStomach = BellyPlus.lizardFood;
			if (BPOptions.debugLogs.Value)
				Debug.Log("LZ! RESTORING MY LIZARDS FOOD VALUE!" + BellyPlus.lizardFood);
			patch_Lizard.UpdateBellySize(self.lizard);
		}
		
		//if ()
		//{
		//	value = PlayerHK.PointDir(self.playerState.playerNumber).normalized;
		//	(self.grasps[i].grabbed as Weapon).setRotation = new Vector2?(value);
		//}
		
		
		//BEEP BOOP. REMOTE CONTROL
		bool remoteControl = true;
		if (remoteControl) // && BellyPlus.jollyCoopEnabled)
			RemoteControl(self.lizard, origRunspeed);

		//Debug.Log("LZ! SHOW MY MOVES!" + self.lizard.timeSpentTryingThisMove + " SQUEEZE " + self.lizard.bodyChunks[0].terrainSqueeze + " - " + self.lizard.bodyChunks[1].terrainSqueeze + " - " + self.lizard.bodyChunks[2].terrainSqueeze );

		if (patch_Player.IsStuckOrWedged(self.lizard))
		{
			float stuckcitement = ((self.lizard.GetBelly().pushingOther > 0) ? 0.5f : 0f) + Mathf.Min(self.lizard.GetBelly().stuckStrain / 150f, 0.7f);
			//self.excitement = Mathf.Max(self.excitement, stuckcitement); //OK I THINK THIS IS MAKING LIZARDS WEIRD OUT WITH STOPPING AND REPEATING VOCALS

			self.stuckTracker.stuckCounter = 0; //BECAUSE WE ACTUALLY ARE STUCK
			self.lizard.timeSpentTryingThisMove = Custom.IntClamp(self.lizard.timeSpentTryingThisMove, 0, 200); //I THINK THE DESPERATIONSMOOTHER ALSO MAKES US MAGICALLY TP
			
			//if (self.lizard.timeSpentTryingThisMove > 0)
			//	self.lizard.timeSpentTryingThisMove--;

			//if (UnityEngine.Random.value < 0.0125f)
			//    self.lizard.EnterAnimation(Lizard.Animation.PreyReSpotted, false);

			if (UnityEngine.Random.value < 0.125f && self.lizard.GetBelly().beingPushed == 0)
				self.lizard.bodyWiggleCounter = Math.Max(self.lizard.bodyWiggleCounter, (int)(UnityEngine.Random.value * 35f));

			//DON'T WIGGLE TOO HARD! THIS BREAKS THE STUCK.
			self.lizard.bodyWiggleCounter = Math.Min(self.lizard.bodyWiggleCounter, 25);


			if (UnityEngine.Random.value < 0.0125f)
			{
				//if (BellyPlus.lungsExhausted[patch_Lizard.GetRef(self.lizard)])
				//    self.lizard.voice.MakeSound(LizardVoice.Emotion.Submission);
				//else if (patch_Lizard.GetBoostStrain(self.lizard) > 0)
				//    self.lizard.voice.MakeSound(LizardVoice.Emotion.PainImpact, 0.7f);
				//else
				//    self.lizard.voice.MakeSound(LizardVoice.Emotion.Frustration);
				//self.lizard.voice.MakeSound(LizardVoice.Emotion.GeneralSmallNoise); //MANY LIZARDS DON'T HAVE THESE
				//else
				self.lizard.voice.MakeSound(LizardVoice.Emotion.Frustration);
			}

		}
		
		//DON'T RUN THIS IF WE AREN'T IN THE ROOM
		else if (self.behavior == LizardAI.Behavior.FollowFriend && self.friendTracker.friend != null && self.lizard.graphicsModule != null && patch_Player.IsStuckOrWedged(self.friendTracker.friend as Player) && self.friendTracker.friend.room != null)
		{
			//if (self.obstacleTracker != null)
			//    self.obstacleTracker.EraseObstacleObject(self.friendTracker.friend);

			Player myFriend = self.friendTracker.friend as Player;
			Vector2 stuckPos = new Vector2(0, 0);


			//CHECK IF WE ARE IN A POSITION TO TUG OR PUSH
			bool tugPosition = false;
			if (patch_Player.IsVerticalStuck(myFriend))
			{
				if ((patch_Player.ObjGetYFlipDirection(myFriend) == 1) == self.lizard.bodyChunks[1].pos.y > myFriend.bodyChunks[1].pos.y)
				{
					tugPosition = true;
					stuckPos = new Vector2(0, patch_Player.ObjGetYFlipDirection(myFriend));
				}
			}
			else if ((patch_Player.ObjGetXFlipDirection(myFriend) == 1) == self.lizard.bodyChunks[1].pos.x > myFriend.bodyChunks[1].pos.x)
			{
				tugPosition = true;
				stuckPos = new Vector2(patch_Player.ObjGetXFlipDirection(myFriend), 0);
			}


			if (self.lizard.graphicsModule != null)
			{
				(self.lizard.graphicsModule as LizardGraphics).lookPos = self.friendTracker.friend.bodyChunks[0].pos;
			}


			//IN POSITION FOR PUSHING
			if (!tugPosition)
			{
				//Debug.Log("LZ! MY FRIEND IS STUCK! I WILL GO PUSH: ");
				//WorldCoordinate myDest = self.friendTracker.friend.room.GetWorldCoordinate(self.friendTracker.friend.bodyChunks[0].pos - (stuckPos * 20));
				WorldCoordinate myDest = self.friendTracker.friend.room.GetWorldCoordinate(self.friendTracker.friend.bodyChunks[0].pos);
				
				self.friendTracker.friendMovingCounter = 50;
				self.friendTracker.friendDest = myDest;
				self.friendTracker.tempFriendDest = myDest;
				self.lizard.bodyWiggleCounter = 0; //OK, THAT'S ENOUGH WIGGLING

				self.creature.abstractAI.SetDestination(myDest);
				self.runSpeed = 0.5f;
			}


			//FOR PULLING
			else if (tugPosition)
			{
				//SHOOT TONGUE!

				self.focusCreature = self.tracker.RepresentationForObject(self.friendTracker.friend, false);

				//Debug.Log("LZ! MY FRIEND IS STUCK! I WILL GO PULL: ");

				WorldCoordinate myDest = myFriend.room.GetWorldCoordinate(myFriend.bodyChunks[1].pos + (stuckPos * 100));
				self.creature.abstractAI.SetDestination(myDest);
				
				//AIM OUR FACE AT OUR PARTNER
				if (self.lizard.graphicsModule != null)
					(self.lizard.graphicsModule as LizardGraphics).head.vel += Custom.DirVec(self.lizard.mainBodyChunk.pos, myFriend.bodyChunks[0].pos) * 10f;

				if (self.lizard.tongue != null
					&& self.lizard.tongue.Ready
					&& self.lizard.grasps[0] == null
					&& myFriend != null && self.focusCreature != null
                    && Custom.DistLess(self.creature.realizedCreature.mainBodyChunk.pos, myFriend.mainBodyChunk.pos, self.lizard.lizardParams.tongueAttackRange)
					&& (self.lizard.Submersion < 0.5f) && UnityEngine.Random.value < self.lizard.lizardParams.tongueChance * 0.5f //0.05f
					&& self.focusCreature.VisualContact)
				{
					self.lizard.EnterAnimation(Lizard.Animation.ShootTongue, false);
				}

			}

		}



		if (self.lizard.GetBelly().pushingOther > 0)
		{
			//DON'T WIGGLE SO MUCH WHILE PUSHING!
			self.stuckTracker.stuckCounter = Custom.IntClamp(self.stuckTracker.stuckCounter, 0, 25); //OKAY, MAYBE A BIT OF WIGGLE~...
			self.lizard.timeSpentTryingThisMove = Custom.IntClamp(self.lizard.timeSpentTryingThisMove, 0, 200);

		}

	}



	// public static void RemoteControl(LizardAI self, float origRunspeed)
	public static void RemoteControl(Creature self, float origRunspeed)
    {
		
		bool behaviorConditions = false;
		if (self is Lizard)
			behaviorConditions = ((self as Lizard).AI.behavior == LizardAI.Behavior.FollowFriend);
		if (self is Player && (self as Player).AI != null)
			behaviorConditions = ((self as Player).AI.behaviorType == SlugNPCAI.BehaviorType.Following || (self as Player).AI.behaviorType == SlugNPCAI.BehaviorType.Idle);
		
		if (self.room != null && behaviorConditions) // &&  && self.lizard.room == self.friendTracker.friend.room)
		{
			Player myFriend = null; // self.AI.friendTracker.friend as Player;
            if (self is Lizard)
                myFriend = (self as Lizard).AI.friendTracker.friend as Player;
			else if (self is Player)
				myFriend = (self as Player).AI.friendTracker.friend as Player;

			if (myFriend == null)
				return;
			
			Player.InputPackage pointInput = myFriend.pointInput; //JollyCoop.PlayerHK.pointInput[myFriend.playerState.playerNumber].IntVec
			bool pointBtnDown = myFriend.jollyButtonDown || pointInput.IntVec != new IntVector2(0, 0);

			//Debug.Log("BUTTON CHECK: " + (JollyCoop.PlayerHK.jollyButton[myFriend.playerState.playerNumber]) + " DIR:" + JollyCoop.PlayerHK.pointInput[myFriend.playerState.playerNumber].IntVec);

			//ONLY DO THIS AFTER WE'VE BEEN HOLDING IT DOWN A WHILE
			if (pointBtnDown && pointCounter < 30 && pointInput.IntVec != new IntVector2(0, 0))
			{
				pointCounter++;
				return;
			}
			else if (pointCounter > 0)
				pointCounter--;
			
			if (self.room != myFriend.room)
				return;

			//Debug.Log("POINT CHECK! " + pointInput.IntVec + " ISDOWN? " + pointBtnDown );


			//OBEY POINT COMMANDS!!
			if (pointBtnDown && pointInput.IntVec != new IntVector2(0, 0))
			{
				IntVector2 inputVec = pointInput.IntVec;
				//int friendFlipDir = pointInput.x;
				IntVector2 tilePos = myFriend.room.GetTilePosition(myFriend.bodyChunks[0].pos) + (inputVec);
				Vector2 finalDest = new Vector2(0, 0); //myFriend.bodyChunks[0].pos;

				float vecX = inputVec.x;
				float vecY = inputVec.y;

				//STRETCH THE DOWNWARD AXIS A BIT
				if (vecX == 0)
				{
					vecX = 0.5f;
					vecY *= 2; //IF WE'RE POINTING UPWARDS, THERE'S A CHANCE WE'LL MISS THE TILE WE WANT ENTIRELY IF IT'S RIGHT ABOVE US...
				}

				//OK HOW ABOUT FIRST WE CHECK FOR SHORTCUTS...
				for (int j = 6; j > 1; j--)
				{
					for (int i = -1; i < 4; i++)
					{
						Vector2 checkDest = myFriend.room.MiddleOfTile(myFriend.bodyChunks[0].pos) + new Vector2(inputVec.x + (j * vecX), (3 * vecY + i)) * 20;
						if (myFriend.room.GetTile(myFriend.room.GetTilePosition(checkDest)).Terrain == Room.Tile.TerrainType.ShortcutEntrance &&
							(myFriend.room.shortcutData(myFriend.room.GetTilePosition(checkDest)).shortCutType == ShortcutData.Type.Normal
							|| myFriend.room.shortcutData(myFriend.room.GetTilePosition(checkDest)).shortCutType == ShortcutData.Type.RoomExit))
						{
							finalDest = checkDest;
							j = 0; //BREAKS THE FIRST LOOP TOO
							break;
						}
					}
				}

				//AND THEN IF NO SHORTCUTS ARE FOUND, JUST LOOK FOR ANY AIR TILE
				if (finalDest == new Vector2(0, 0))
					for (int j = 6; j > 1; j--)
					{
						for (int i = -1; i < 4; i++)
						{
							Vector2 checkDest = myFriend.room.MiddleOfTile(myFriend.bodyChunks[0].pos) + new Vector2(inputVec.x + (j * vecX), (3 * vecY + i)) * 20;
							if (myFriend.room.GetTile(myFriend.room.GetTilePosition(checkDest)).Terrain == Room.Tile.TerrainType.Air)
							{
								finalDest = checkDest;
								j = 0; //BREAKS THE FIRST LOOP TOO
								break;
							}
						}
					}


				if (finalDest == new Vector2(0, 0))
				{
					Debug.Log("INVALID COMMAND POSITION ");
					return;
				}
				else
				{
					//TRYING TO LET GO OF BOTH DIRECTIONS AT THE SAME TIME IS TOO HARD, AND RESULTS IN A SUDDEN CHANGE OF COMMAND FOR THE LIZARD. SO LTS FIX THAT
					if (commandPos == new Vector2(0, 0) || (commandPos != new Vector2(0, 0) && UnityEngine.Random.value < 0.05f)) //NOW WE HAVE ~ROUGHLY~ 10 FRAMES TO LET GO OF BOTH KEYS WITHOUT CHANGING THE COMMAND
						commandPos = finalDest;
					//OTHERWISE, WE KEEP IT AS IT WAS
					commandTimer = 180;
				}
			}
			else if (commandTimer == 1)
			{
				commandPos = new Vector2(0, 0);
				commandTimer = 0;
			}
			else if (commandTimer > 1)
				commandTimer--;


			if (commandTimer > 0 && commandPos != new Vector2(0, 0))
			{
				WorldCoordinate myDest = self.room.GetWorldCoordinate(commandPos);
				
				if (self is Lizard)
				{
					((self as Lizard).AI as LizardAI).runSpeed = 0.5f;
					//OKAY, I GUESS THEY CAN KEEP THE STATIC RUNSPEED. BUT IT NEEDS TO SLOW DOWN ONCE THEY GET STUCK
					if (patch_Player.IsStuckOrWedged(self))
						((self as Lizard).AI as LizardAI).runSpeed = origRunspeed;

                    (self as Lizard).AI.creature.abstractAI.SetDestination(myDest);
                    (self as Lizard).AI.friendTracker.friendDest = myDest;
                    (self as Lizard).AI.friendTracker.tempFriendDest = myDest;
                }
				else
                {

                    (self as Player).AI.friendTracker.friendMovingCounter = 50;
                    (self as Player).AI.friendTracker.friendDest = myDest;
					(self as Player).AI.friendTracker.tempFriendDest = myDest;
                    (self as Player).AI.abstractAI.SetDestination(myDest);
                }
				
				//Vector2 pos2 = self.lizard.room.MiddleOfTile(commandPos);
				self.room.AddObject(new StrainSpark(commandPos, new Vector2(0, 0), 10f, Color.blue));
				return;
			}
		}
	}
}