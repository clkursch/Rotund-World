using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace RotundWorld;
public class patch_Scavenger
{
	public delegate float orig_MovementSpeed(Scavenger self); //NEAT...

	public static void Patch()
	{
		On.Scavenger.ctor += (ScavPatch);
		
		
		On.Scavenger.Update += BPScavenger_Update;
		//On.Scavenger.Collide += BP_Collide;
		On.Scavenger.PickUpAndPlaceInInventory += Scavenger_PickUpAndPlaceInInventory;

        On.ScavengerGraphics.ctor += ScavengerGraphics_ctor;
        On.ScavengerGraphics.InitiateSprites += ScavengerGraphics_InitiateSprites;
        // On.ScavengerGraphics.DrawSprites += PG_DrawSprites;
        On.ScavengerGraphics.Update += ScavengerGraphics_Update;

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

	private static void ScavPatch(On.Scavenger.orig_ctor orig, Scavenger self, AbstractCreature abstractCreature, World world)
	{
		orig(self, abstractCreature, world);

		if (self.abstractCreature.GetAbsBelly().myFoodInStomach != -1)
        {
			//Debug.Log("SCAV ALREADY EXISTS! CANCELING: " + critNum);
			UpdateBellySize(self);
			return;
		}
		
		//NEW, LETS BASE OUR RANDOM VALUE ON OUR ABSTRACT CREATURE ID
		UnityEngine.Random.seed = self.abstractCreature.ID.RandomSeed;

        int mouseChub = Mathf.FloorToInt(Mathf.Lerp(2, 9, UnityEngine.Random.value));
		if (mouseChub != 8 || !patch_MiscCreatures.CheckFattable(self))
			mouseChub = 4;
		if (BPOptions.debugLogs.Value)
			Debug.Log("SCAV SPAWNED! CHUB SIZE: " + mouseChub);
		self.abstractCreature.GetAbsBelly().myFoodInStomach = mouseChub;

		UpdateBellySize(self);
        //Debug.Log("SCAV SPAWNED! - KARMA? " + self.abstractCreature.karmicPotential);
        if (BellyPlus.parasiticEnabled)
            BellyPlus.InitPSFoodValues(abstractCreature);
    }
	
	
	
	public static void UpdateBellySize(Scavenger self)
	{
		
		float baseWeight = 0.5f; //I THINK...
		int currentFood = self.abstractCreature.GetAbsBelly().myFoodInStomach;

		//new BodyChunk(this, 0, new Vector2(0f, 0f), 9.5f, 0.5f);
		float newMass = baseWeight;
		switch (System.Math.Min(currentFood, 8))
        {
            case 8:
                newMass = baseWeight * 1.4f;
				//self.bodyChunks[0].rad = baseRad * 1.5f;
				self.GetBelly().myFatness = 1.4f;
				break;
            default:
				newMass = baseWeight * 1f;
				self.GetBelly().myFatness = 1f;
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
            if (self.GetBelly().pushingOther > 0 || self.GetBelly().isStuck)
                return Mathf.Min(0.5f, orig.Invoke(self)); //RETURN 10% SPEED (OR 0, IF WE WOULD BE STANDING STILL)
        }
        return orig.Invoke(self); //OTHERWISE, JUST RUN AS NORMAL
	}
	


	public static Scavenger FindScavInRange(Creature self)
	{
        if (self.room == null)
            return null; 
		
		for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
        {
            if (self.room.abstractRoom.creatures[i].realizedCreature != null
                && self.room.abstractRoom.creatures[i].realizedCreature is Scavenger crit
                && crit != self && crit.room != null && crit.room == self.room && !crit.dead
                && Custom.DistLess(self.mainBodyChunk.pos, crit.bodyChunks[1].pos, 35f)
            )
            {
                return crit;
            }
        }
        return null;
	}
	
	
	
	public static Scavenger FindStuckScavInRange(Creature self)
	{
        if (self.room == null)
            return null; 
		
		for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
        {
            if (self.room.abstractRoom.creatures[i].realizedCreature != null
                && self.room.abstractRoom.creatures[i].realizedCreature is Scavenger crit
				&& crit != self && crit.room != null && crit.room == self.room && !crit.dead
                && patch_Player.ObjIsStuck(crit)
                && Custom.DistLess(self.mainBodyChunk.pos, crit.bodyChunks[0].pos, 60f)
            )
            {
                return crit;
            }
        }
        return null;
	}
	
	
	//OKAY, THESE GUYS CAN HAVE ONE STEP.
	public static void BPUUpdatePass1(Scavenger self)
	{
		//Debug.Log("SC!-----DEBUG!: " + self.GetBelly().myFlipValX + " " + self.GetBelly().inPipeStatus + " "  + " " + self.GetBelly().stuckStrain + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);
		
		self.GetBelly().myFlipValX = (self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x) ? 1 : -1;
		self.GetBelly().myFlipValY = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) ? 1 : -1;
		self.GetBelly().pullingOther = false;
		
		//CHECK IF WE'RE PUSHING, PULLING, ETC
		if (self.GetBelly().pushingOther > 0)
			self.stuckCounter = 0;
		
		
		else if (patch_Player.IsGraspingActualSlugcat(self))
		{
			Player mySlug = patch_Player.GetGraspedCreature(self) as Player;
			
			if ((self.AI.behavior == ScavengerAI.Behavior.Flee || self.AI.behavior == ScavengerAI.Behavior.Attack) && UnityEngine.Random.value < 0.1f)
			{
				self.ReleaseGrasp(0);
				Debug.Log("SC! RELEASING THE SLUG BECAUSE OF BEHAVIOR: " + self.AI.behavior);
			}
				
			
			//MAKE SURE WE ARE BOTH STUCK AND NOT RESISTING
			else if (patch_Player.IsStuck(mySlug) && mySlug.input[0].IntVec.ToVector2() != -patch_Player.ObjGetStuckVector(mySlug))
			{
				self.GetBelly().pullingOther = true;
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
			// BPUUpdatePass5(self);
			BPUUpdatePass1(self);
			
			if (patch_Lizard.GetChubValue(self) >= 3)
			{
				patch_Lizard.BPUUpdatePass2(self);
				patch_Lizard.BPUUpdatePass3(self);
				patch_Lizard.BPUUpdatePass4(self);
			}
			
			
			patch_Lizard.BPUUpdatePass5(self);
			patch_Lizard.BPUUpdatePass5_2(self);

			if (self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther)
				self.bodyChunks[0].vel.y += 0.2f;
		}
		patch_Lizard.BPUUpdatePass6(self);
	}




    private static void ScavengerGraphics_ctor(On.ScavengerGraphics.orig_ctor orig, ScavengerGraphics self, PhysicalObject ow)
    {
        orig.Invoke(self, ow);
        float scavFatness = self.scavenger.GetBelly().myFatness;
        self.iVars.fatness *= scavFatness;
        self.iVars.narrowWaist = Mathf.Lerp(Mathf.Lerp(Random.value, 1f - self.iVars.fatness, Random.value), 1f - self.scavenger.abstractCreature.personality.energy, Random.value);
        self.iVars.neckThickness = Mathf.Lerp(Mathf.Pow(Random.value, 1.5f - self.scavenger.abstractCreature.personality.aggression), 1f - self.iVars.fatness, Random.value * 0.5f);
        self.iVars.armThickness = Mathf.Lerp(Random.value, Mathf.Lerp(self.scavenger.abstractCreature.personality.dominance, self.iVars.fatness, 0.5f), Random.value);

        self.iVars.neckThickness *= scavFatness;
        self.iVars.armThickness *= scavFatness;
        self.iVars.narrowWaist *= scavFatness;
        //sLeaser.sprites[this.NeckSprite] = TriangleMesh.MakeLongMesh(4, false, true);
    }


    private static void ScavengerGraphics_InitiateSprites(On.ScavengerGraphics.orig_InitiateSprites orig, ScavengerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig.Invoke(self, sLeaser, rCam);
        float myFat = self.scavenger.GetBelly().myFatness;
        sLeaser.sprites[self.ChestSprite].scaleX *= myFat;
        sLeaser.sprites[self.ChestSprite].scaleY *= myFat;
        sLeaser.sprites[self.HipSprite].scale *= myFat;
    }

    private static void ScavengerGraphics_Update(On.ScavengerGraphics.orig_Update orig, ScavengerGraphics self)
    {
		orig(self);

        if (self.scavenger.room == null || BellyPlus.VisualsOnly())
            return;

        if (self.scavenger.GetBelly().pullingOther && self.scavenger.grasps[0] != null && self.scavenger.grasps[0].grabbed != null && self.scavenger.grasps[0].grabbed is Player)
        {
            Limb myHand = self.hands[0];
            myHand.mode = Limb.Mode.HuntAbsolutePosition;
            myHand.absoluteHuntPos = self.scavenger.grasps[0].grabbedChunk.pos;
            myHand.pos = self.scavenger.grasps[0].grabbedChunk.pos;
        }




        //RESET
        if (self.scavenger.GetBelly().pushingOther > 0)
            self.scavenger.GetBelly().pushingOther--;

        //STOLEN FROM SLUGCAT HANDS
        Creature myHelper = patch_Player.FindPlayerInRange(self.scavenger);
        if (myHelper == null)
            myHelper = patch_Scavenger.FindScavInRange(self.scavenger);
        if (myHelper == null)
            myHelper = patch_LanternMouse.FindMouseInRange(self.scavenger);

        if (myHelper != null)
        {
            if (patch_Player.IsStuckOrWedged(myHelper) || patch_Player.ObjIsPushingOther(myHelper))
            {
                if (UnityEngine.Random.value < 0.125f)
                {
                    self.hands[0].pos = myHelper.bodyChunks[1].pos;
                    self.hands[0].lastPos = myHelper.bodyChunks[1].pos;
                    self.hands[0].vel *= 0f;
                }


                // bool vertStuck = patch_LanternMouse.IsVerticalStuck(myHelper);
                // if (!vertStuck && patch_LanternMouse.GetMouseAngle(self.scavenger).x == patch_LanternMouse.GetMouseAngle(myHelper).x
                // || (vertStuck && patch_LanternMouse.GetMouseAngle(self.scavenger).y == patch_LanternMouse.GetMouseAngle(myHelper).y))
                //FORGET THAT. JUST ALWAYS PUSH IF WE'RE CLOSE ENOUGH
                patch_Player.ObjPushedOn(myHelper);
                self.scavenger.GetBelly().pushingOther = 3;
            }
        }
    }

}