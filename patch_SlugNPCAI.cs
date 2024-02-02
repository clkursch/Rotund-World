using System;
using RWCustom;
using UnityEngine;
using MoreSlugcats;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace RotundWorld;

public class patch_SlugNPCAI
{
	//public delegate bool orig_IsFull(SlugNPCAI self); //NEAT...
	
	public static void Patch()
    {
		On.MoreSlugcats.SlugNPCAI.Update += BP_Update;
		On.MoreSlugcats.SlugNPCAI.Move += SlugNPCAI_Move;

		//ACTUALLY JUST SWITCH "ISFULL" TO SEE IF OUR CURRENT GIFT IS A FOOD, I GUESS
		//BindingFlags propFlags2 = BindingFlags.Instance | BindingFlags.Public;
		//BindingFlags myMethodFlags2 = BindingFlags.Static | BindingFlags.Public;


		//typeof(SlugNPCAI).GetProperty(nameof(SlugNPCAI.IsFull), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod()
		//      Hook myNPCHook = new Hook(
		//          typeof(MoreSlugcats.SlugNPCAI).GetProperty("IsFull", propFlags2).GetGetMethod(), // This gets the getter 
		//          typeof(patch_SlugNPCAI).GetMethod("SlugNPCAI_get_IsFull", myMethodFlags2) // This gets our hook method
		//      );
		// Debug.Log("SLUGNPC DEBUG! INIT");

        //new Hook(typeof(SlugNPCAI).GetProperty(nameof(SlugNPCAI.IsFull)).GetGetMethod(),
        //    (Func<Func<SlugNPCAI, bool>, SlugNPCAI, bool>)SlugNPCAI_get_IsFull);
        //try
        //      {
        //          //new Hook(typeof(SlugNPCAI).GetProperty(nameof(SlugNPCAI.FunStuff)).GetGetMethod(),
        //          //(Func<Func<SlugNPCAI, bool>, SlugNPCAI, bool>)SlugNPCAI_test);
        //}
        //catch (Exception e)
        //      {
        //	Debug.Log("CATCH " + e);
        //}

        //MAKE CATS THINK THEY ARE HUNGY
        On.MoreSlugcats.SlugNPCAI.DecideBehavior += BPSlugNPCAI_DecideBehavior;
        On.MoreSlugcats.SlugNPCAI.WantsToEatThis += BPSlugNPCAI_WantsToEatThis;
		//APPLIED TO EVERYTHING EXCEPT SOCIAL INTERATION
	}

    

    public static void FixFood(SlugNPCAI self)
	{
		self.cat.playerState.foodInStomach = self.cat.abstractCreature.GetAbsBelly().myFoodInStomach;
	}
	
	public static bool WantsToOvereat(SlugNPCAI self)
	{
		if (!BPOptions.fatPups.Value)
			return false;
		
		int limit = 0;
		if (self.creature.personality.energy < 0.3)
			limit = 20;
		else if (self.creature.personality.energy < 0.45)
			limit = 6;
		else if (self.creature.personality.energy < 0.65)
			limit = 4;
		else if (self.creature.personality.energy < 0.85)
			limit = 2;

        //Debug.Log("SLUGNPC DEBUG! MY LIMIT " + limit + " - " + self.creature.personality.energy);
        return self.IsFull && self.cat.playerState.foodInStomach < self.cat.MaxFoodInStomach + limit;
	}
    //this.creature.personality.energy > 0.6f



    public static void BPSlugNPCAI_DecideBehavior(On.MoreSlugcats.SlugNPCAI.orig_DecideBehavior orig, SlugNPCAI self)
    {
        //BRIEFLY PRETEND WE AREN'T FULL
        // int myFood = self.cat.playerState.foodInStomach;
        bool hungry = WantsToOvereat(self);
        if (hungry)
            self.cat.playerState.foodInStomach = self.cat.MaxFoodInStomach - 1;

        orig.Invoke(self);

        //PUT IT BACK TO NORMAL
        // self.cat.playerState.foodInStomach = myFood;
        if (hungry)
            FixFood(self);
    }


    private static bool BPSlugNPCAI_WantsToEatThis(On.MoreSlugcats.SlugNPCAI.orig_WantsToEatThis orig, SlugNPCAI self, PhysicalObject obj)
    {
        bool hungry = WantsToOvereat(self);
        if (hungry)
            self.cat.playerState.foodInStomach = self.cat.MaxFoodInStomach - 1;

        bool result = orig.Invoke(self, obj);

        //PUT IT BACK TO NORMAL
        if (hungry)
            FixFood(self);

        return result;
    }


    /*
	public static bool SlugNPCAI_get_IsFull(Func<SlugNPCAI, bool> orig, SlugNPCAI self)
	{
		Debug.Log("SLUGNPC DEBUG! DO I WORK");
		//THE ANSWER WAS NO. IT DOES NOT WORK. THE COMPILER REPLACES IT, SO WE CAN'T EDIT THIS...
		return orig.Invoke(self);
	}
	*/



    private static void SlugNPCAI_Move(On.MoreSlugcats.SlugNPCAI.orig_Move orig, SlugNPCAI self)
	{
		bool hungry = WantsToOvereat(self);
		if (hungry)
			self.cat.playerState.foodInStomach = self.cat.MaxFoodInStomach - 1;
		
		orig.Invoke(self);
		
		if (hungry)
			FixFood(self);
		
		
		if (BPOptions.fatPups.Value && self.IsFull && (self.behaviorType == SlugNPCAI.BehaviorType.Following || self.behaviorType == SlugNPCAI.BehaviorType.Idle))
		{
			for (int i = 0; i < self.cat.grasps.Length; i++)
			{
				if (self.cat.grasps[i] != null && patch_Player.GetForceEatTarget(self.cat) != null && self.cat.grasps[i].grabbed == patch_Player.GetForceEatTarget(self.cat))
				{
					self.cat.input[0].pckp = true;
					self.cat.dontGrabStuff = 2; //LETS TRY KEEPING THEM FROM PICKING ANYTHING UP WHILE EATING
					// Debug.Log("SLUGNPC DEBUG! FOR ME? YOU SHOULDN'T HAVE...");
				}
			}
		}
		
		if (!BellyPlus.VisualsOnly())
		{
			//Debug.Log("BEEP BOOP! ");
			patch_LizardAI.RemoteControl(self.cat, 0);
		}
	}

	
	
	
	public static Player FindStuckFriend(Creature self)
	{
		Player closestPlayer = null;
		for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
		{
			Player checkPlayer = null;
			Creature checkCreature = self.room.abstractRoom.creatures[i].realizedCreature;
			if (checkCreature != null && checkCreature is Player)
				checkPlayer = (checkCreature as Player);

			if (
				checkPlayer != null
				&& checkPlayer != self
				&& checkPlayer.room == self.room
				&& checkPlayer.dead == false
				&& patch_Player.IsStuck(checkPlayer)
			)
			{
				return checkPlayer;
			}
		}
		return null;
	}
	
	
	
	
	
	
    public static void BP_Update(On.MoreSlugcats.SlugNPCAI.orig_Update orig, SlugNPCAI self)
    {

		if (self.IsFull && self.grabTarget != null && self.CanGrabItem(self.grabTarget))
		{
			//self.cat.NPCForceGrab(self.grabTarget);
			if (self.friendTracker.giftOfferedToMe != null && self.friendTracker.giftOfferedToMe.item != null && self.friendTracker.giftOfferedToMe.item == self.grabTarget
				&& ((self.grabTarget is IPlayerEdible && (self.grabTarget as IPlayerEdible).Edible) || (self.grabTarget is Creature && self.TheoreticallyEatMeat(self.grabTarget as Creature, false) && (self.grabTarget as Creature).dead)))
				patch_Player.SetForceEatTarget(self.cat, self.grabTarget);
		}
		
		
		orig.Invoke(self);
		
		if (BellyPlus.VisualsOnly())
			return;
		
		
		if (patch_Player.IsStuckOrWedged(self.cat))
		{
			self.stuckTracker.stuckCounter = 0; //BECAUSE WE ACTUALLY ARE STUCK
			if (UnityEngine.Random.value < 0.05f)
				//Debug.Log("SLUGNPC DEBUG! " + self.behaviorType);

			if (self.behaviorType == SlugNPCAI.BehaviorType.Following && self.friendTracker.friend != null && self.friendTracker.friend.room != null)
            {
				Player myFriend = self.friendTracker.friend as Player;
				WorldCoordinate myDest = myFriend.room.GetWorldCoordinate(myFriend.bodyChunks[1].pos);
				self.creature.abstractAI.SetDestination(myDest);
			}
			//DON'T JUST STAND THERE!
			else if (self.cat.input[0].IntVec == new IntVector2(0, 0))
            {
				WorldCoordinate myDest = self.cat.room.GetWorldCoordinate(self.cat.bodyChunks[1].pos + (patch_Player.ObjGetStuckVector(self.cat) * 20f));
				self.creature.abstractAI.SetDestination(myDest);
			}
		}
		
		//DON'T RUN THIS IF WE AREN'T IN THE ROOM
		else if (self.cat.onBack == null) //AND DON'T RUN IF WE'RE BEING PIGGYBACKED (ONLY NEEDED FOR PUP AI MODS)
		{
			// Player myFriend = self.friendTracker.friend as Player; 
			Player myFriend = FindStuckFriend(self.cat);

			//Debug.Log("SLUGNPC DEBUG! " + self.behaviorType);

			if ((self.behaviorType == SlugNPCAI.BehaviorType.Following || self.behaviorType == SlugNPCAI.BehaviorType.Idle) && self.cat.graphicsModule != null && myFriend != null && patch_Player.IsStuckOrWedged(myFriend) && myFriend.room != null)
			{
				Vector2 stuckPos = new Vector2(0, 0);

				//CHECK IF WE ARE IN A POSITION TO TUG OR PUSH
				bool tugPosition = false;
				if (patch_Player.IsVerticalStuck(myFriend))
				{
					if ((patch_Player.ObjGetYFlipDirection(myFriend) == 1) == self.cat.bodyChunks[1].pos.y > myFriend.bodyChunks[1].pos.y)
					{
						tugPosition = true;
						stuckPos = new Vector2(0, patch_Player.ObjGetYFlipDirection(myFriend));
					}
				}
				else if ((patch_Player.ObjGetXFlipDirection(myFriend) == 1) == self.cat.bodyChunks[1].pos.x > myFriend.bodyChunks[1].pos.x)
				{
					tugPosition = true;
					stuckPos = new Vector2(patch_Player.ObjGetXFlipDirection(myFriend), 0);
				}
				
				
				
				//IN POSITION FOR PUSHING
				if (!tugPosition)
				{
					//Debug.Log("SLUGNPC! MY FRIEND IS STUCK! I WILL GO PUSH: ");
					//WorldCoordinate myDest = self.friendTracker.friend.room.GetWorldCoordinate(self.friendTracker.friend.bodyChunks[0].pos - (stuckPos * 20));
					WorldCoordinate myDest = myFriend.room.GetWorldCoordinate(myFriend.bodyChunks[0].pos);
					
					self.friendTracker.friendMovingCounter = 50;
					self.friendTracker.friendDest = myDest;
					self.friendTracker.tempFriendDest = myDest;

					self.creature.abstractAI.SetDestination(myDest);
				}


				//FOR PULLING
				else if (tugPosition)
				{
					//Debug.Log("SLUGNPC! MY FRIEND IS STUCK! I WILL GO PULL: ");

					WorldCoordinate myDest = myFriend.room.GetWorldCoordinate(myFriend.bodyChunks[1].pos + (stuckPos * 10));
					
					
					if (patch_Player.IsGraspingActualSlugcat(self.cat))
					{
						myDest = myFriend.room.GetWorldCoordinate(myFriend.bodyChunks[1].pos + (stuckPos * 60));
					}
					else
					{
						if (Custom.DistLess(self.cat.bodyChunks[0].pos, myFriend.bodyChunks[0].pos, 35f))
						{
							// Debug.Log("WE'VE JUST GRABBED ON, NOW PULL! ");
							self.cat.NPCForceGrab(myFriend);
						}
					}

					self.creature.abstractAI.SetDestination(myDest);
				}
			}
			
		}
		
	}
}