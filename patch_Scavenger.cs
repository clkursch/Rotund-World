using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;


public class patch_Scavenger
{
	public delegate float orig_MovementSpeed(Scavenger self); //NEAT...

	public static void Patch()
	{
		On.Scavenger.ctor += (ScavPatch);
		
		
		On.Scavenger.Update += BPScavenger_Update;
		//On.Scavenger.Collide += BP_Collide;
		On.Scavenger.PickUpAndPlaceInInventory += Scavenger_PickUpAndPlaceInInventory;

		//TO EDIT THEIR MOVEMENT SPEED FLOAT
		BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
		BindingFlags myMethodFlags = BindingFlags.Static | BindingFlags.Public;

		Hook myMovespdHook = new Hook(
			typeof(Scavenger).GetProperty("MovementSpeed", propFlags).GetGetMethod(), // This gets the getter 
			typeof(patch_Scavenger).GetMethod("Scavenger_get_MovementSpeed", myMethodFlags) // This gets our hook method
		);
		
	}

    private static void Scavenger_PickUpAndPlaceInInventory(On.Scavenger.orig_PickUpAndPlaceInInventory orig, Scavenger self, PhysicalObject obj)
    {
		//IF WE'RE PULLING SOMEONE, DON'T INTERRUPT US. UPDATEPASS1 WILL TAKE CARE OF THAT
		if (patch_Player.IsGraspingActualSlugcat(self))
			return;
		else
			orig.Invoke(self, obj);

	}

    public static Dictionary<int, Scavenger> scavBook = new Dictionary<int, Scavenger>(0);

	private static void ScavPatch(On.Scavenger.orig_ctor orig, Scavenger self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);
		
		int critNum = self.abstractCreature.ID.RandomSeed;

		//MAKE SURE THERE ISN'T ALREADY A CREATURE WITH OUR NAME ON THIS!
		bool critExists = false;
        try
        {
			patch_Scavenger.scavBook.Add(critNum, self); //ADD OURSELVES TO THE GUESTBOOK
		}
		catch (ArgumentException)
        {
			critExists = true;
		}

		if (critExists)
        {
			//Debug.Log("SCAV ALREADY EXISTS! CANCELING: " + critNum);
			patch_Scavenger.scavBook[critNum] = self; //WELL HOLD ON! WE STALL NEED THE REFERENCE FROM THAT BOOK TO POINT TO US!
			return;
		}
		
		BellyPlus.InitializeCreature(critNum);
		
		
		//NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
		int seed = UnityEngine.Random.seed;
		UnityEngine.Random.seed = critNum;

		int mouseChub = Mathf.FloorToInt(Mathf.Lerp(2, 9, UnityEngine.Random.value));
		if (mouseChub != 8 || !patch_DLL.CheckFattable(self))
			mouseChub = 4;
		if (BPOptions.debugLogs.Value)
			Debug.Log("SCAV SPAWNED! CHUB SIZE: " + mouseChub);
		BellyPlus.myFoodInStomach[critNum] = mouseChub;

		UpdateBellySize(self);
        //Debug.Log("SCAV SPAWNED! - KARMA? " + self.abstractCreature.karmicPotential);
        if (BellyPlus.parasiticEnabled)
            BellyPlus.InitPSFoodValues(abstractCreature);
    }
	
	public static int GetRef(Creature self)
	{
		return self.abstractCreature.ID.RandomSeed;
	}
	
	
	
	public static void UpdateBellySize(Scavenger self)
	{
		int myLiz = GetRef(self);
		float baseWeight = 0.5f; //I THINK...
		float baseRad = 9.5f;
		int currentFood = BellyPlus.myFoodInStomach[GetRef(self)];

		//new BodyChunk(this, 0, new Vector2(0f, 0f), 9.5f, 0.5f);
		float newMass = baseWeight;
		switch (Math.Min(currentFood, 8))
        {
            case 8:
                newMass = baseWeight * 1.4f;
				//self.bodyChunks[0].rad = baseRad * 1.5f;
				BellyPlus.myFatness[myLiz] = 1.4f;
				break;
            default:
				newMass = baseWeight * 1f;
				BellyPlus.myFatness[myLiz] = 1f;
				break;
        }
		
		if (!BellyPlus.VisualsOnly())
			self.bodyChunks[0].mass = newMass;

        patch_Lizard.UpdateChubValue(self);
	}
	
	
	
	
	public static float Scavenger_get_MovementSpeed(orig_MovementSpeed orig, Scavenger self)
	{
        //IF WE'RE ON SCREEN AND PUSHING SOMEONE, SLOW US DOWN
        if (self.graphicsModule != null && !BellyPlus.VisualsOnly())
        {
            int critNum = self.abstractCreature.ID.RandomSeed;
            if (BellyPlus.pushingOther[critNum] || BellyPlus.isStuck[critNum])
                return Mathf.Min(0.5f, orig.Invoke(self)); //RETURN 10% SPEED (OR 0, IF WE WOULD BE STANDING STILL)
        }
        return orig.Invoke(self); //OTHERWISE, JUST RUN AS NORMAL
	}
	


	public static Scavenger FindScavInRange(Creature self)
	{
		foreach (KeyValuePair<int, Scavenger> kvp in patch_Scavenger.scavBook)
		{
			if (
				kvp.Value != null
				&& kvp.Value != self
				&& kvp.Value.room == self.room
				&& kvp.Value.dead == false
				&& Custom.DistLess(self.mainBodyChunk.pos, kvp.Value.bodyChunks[0].pos, 35f)
			)
			{
				return kvp.Value as Scavenger;
			}
		}
		return null;
	}
	
	
	
	public static Scavenger FindStuckScavInRange(Creature self)
	{
		foreach (KeyValuePair<int, Scavenger> kvp in patch_Scavenger.scavBook)
		{
			if (
				kvp.Value != null
				&& kvp.Value != self
				&& kvp.Value.room == self.room
				&& kvp.Value.dead == false
				&& patch_Player.ObjIsStuck(kvp.Value)
				&& Custom.DistLess(self.mainBodyChunk.pos, kvp.Value.bodyChunks[0].pos, 60f)
			)
			{
				return kvp.Value as Scavenger;
			}
		}
		return null;
	}
	
	
	//OKAY, THESE GUYS CAN HAVE ONE STEP.
	public static void BPUUpdatePass1(Scavenger self, int critNum)
	{
		//Debug.Log("SC!-----DEBUG!: " + BellyPlus.myFlipValX[critNum] + " " + BellyPlus.inPipeStatus[critNum] + " "  + " " + BellyPlus.stuckStrain[critNum] + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);
		
		BellyPlus.myFlipValX[critNum] = (self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x) ? 1 : -1;
		BellyPlus.myFlipValY[critNum] = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) ? 1 : -1;
		BellyPlus.pullingOther[critNum] = false;
		
		//CHECK IF WE'RE PUSHING, PULLING, ETC
		if (BellyPlus.pushingOther[critNum])
			self.stuckCounter = 0;
		
		
		else if (patch_Player.IsGraspingActualSlugcat(self))
		{
			Player mySlug = self.grasps[0].grabbed as Player;
			
			if ((self.AI.behavior == ScavengerAI.Behavior.Flee || self.AI.behavior == ScavengerAI.Behavior.Attack) && UnityEngine.Random.value < 0.1f)
			{
				self.ReleaseGrasp(0);
				Debug.Log("SC! RELEASING THE SLUG BECAUSE OF BEHAVIOR: " + self.AI.behavior);
			}
				
			
			//MAKE SURE WE ARE BOTH STUCK AND NOT RESISTING
			else if (patch_Player.IsStuck(mySlug) && mySlug.input[0].IntVec.ToVector2() != -patch_Player.ObjGetStuckVector(mySlug))
			{
				BellyPlus.pullingOther[critNum] = true;
				self.bodyChunks[0].vel = patch_Player.ObjGetStuckVector(mySlug) * 0.25f;
				self.shortcutDelay = 10;
				if (BPOptions.debugLogs.Value)
					Debug.Log("SC! HEAVE!!: " + self.bodyChunks[0].vel);
				
			}
			else
			{
				self.ReleaseGrasp(0);
				Debug.Log("SC! RELEASE THE SLUG!!: ");
			}
		}
		
		//IF WE'RE NOT PULLING, CHECK IF WE SHOULD
		else if (self.AI.behavior != ScavengerAI.Behavior.Flee && self.AI.behavior != ScavengerAI.Behavior.Attack)
		{
			//CHECK FOR PLAYERS TO GRAB.
			Creature nearbyPlugged = null;
			
			Player nearbySlug = patch_Player.FindPlayerTopInRange(self, 65f);
			if (nearbySlug != null)
				nearbyPlugged = nearbySlug;
			//GAH, THIS MIGHT JUST BE TOO MUCH RIGHT NOW...
			// else
			// {
				// Scavenger nearbyScav = FindStuckScavInRange(self); 
				// if (nearbyScav == !null)
					// nearbyPlugged = nearbyScav;
			// }
				
			if (nearbySlug != null && patch_Player.IsStuck(nearbySlug) && self.shortcutDelay <= 0)
			{
				Debug.Log("SC! GRABBING A SCUG or scav ");
				self.Grab(nearbySlug, 0, 0, Creature.Grasp.Shareability.CanNotShare, 0.5f, true, false);
				self.shortcutDelay = 10;
			}
		}
	}


	public static void BPScavenger_Update(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
	{
		int critNum = self.abstractCreature.ID.RandomSeed;
		
		orig.Invoke(self, eu);
		if (BellyPlus.VisualsOnly())
			return;


		if (self == null || self.dead)
			return;
		
		if (self.room != null && self.graphicsModule != null && self.stun < 1)
		{
			// BPUUpdatePass5(self, critNum);
			BPUUpdatePass1(self, critNum);
			
			if (patch_Lizard.GetChubValue(self) >= 3)
			{
				patch_Lizard.BPUUpdatePass2(self, critNum);
				patch_Lizard.BPUUpdatePass3(self, critNum);
				patch_Lizard.BPUUpdatePass4(self, critNum);
			}
			
			
			patch_Lizard.BPUUpdatePass5(self, critNum);
			patch_Lizard.BPUUpdatePass5_2(self, critNum);

			if (BellyPlus.pushingOther[critNum] || BellyPlus.pullingOther[critNum])
				self.bodyChunks[0].vel.y += 0.2f;
		}
		patch_Lizard.BPUUpdatePass6(self, critNum);
	}

}