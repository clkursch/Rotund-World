using System;
using System.Collections;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using MoreSlugcats;
using MonoMod.RuntimeDetour;

namespace RotundWorld;
public class patch_MiscCreatures
{

    public static void Patch()
	{
		On.NeedleWormGraphics.ctor += NootGraphics;
		On.NeedleWormGraphics.GraphSegmentRad += NootRad;
		
		On.DaddyLongLegs.Update += DDL_Update;
		On.DaddyGraphics.DrawSprites += DaddyGraphics_DrawSprites;

		On.Centipede.ctor += Centipede_ctor;
        On.Centipede.Shock += BPCentipede_Shock; //FOR DRONEMASTER
        On.Centipede.Die += BPCentipede_Die;
        //On.CentipedeGraphics.DrawSprites += Centipede_DrawSprites;

        //On.PoleMimicGraphics.DrawSprites += BPPoleMimic_DrawSprites;
        //On.PoleMimicGraphics.PoleMimicRopeGraphics.DrawSprite += BPPoleMimicRopeGraphics_DrawSprite;

        On.MirosBirdGraphics.DrawSprites += BPMirosBirdGraphics_DrawSprites;
        On.MirosBird.Act += BPMirosBird_Act;
        On.MirosBird.ctor += BPMirosBird_ctor;
        //On.MirosBird.BirdLeg.Update += BPMirosBird_Update;
        On.MirosBird.BirdLeg.Update += BPMirosBird_Update;

        On.BigSpiderGraphics.ctor += BPBigSpiderGraphics_ctor;
        On.BigEel.ctor += BPBigEel_ctor;
        On.BigEel.Swallow += BPBigEel_Swallow;

        //On.PoleMimic.Rad += BP_Rad;
        On.DropBug.ctor += DropBug_ctor;
        //On.DropBugGraphics.ctor += DropBugGraphics_ctor;
        // On.DropBugGraphics.InitiateSprites += DropBugGraphics_InitiateSprites;
        On.DropBugGraphics.DrawSprites += DropBugGraphics_DrawSprites;
        On.DropBug.MoveTowards += DropBug_MoveTowards;
        On.DropBug.Update += DropBug_Update;

        On.JetFish.ctor += JetFish_ctor;
		On.JetFish.Act += JetFish_Act;
        On.JetFishGraphics.Update += JetFishGraphics_Update;
        On.JetFishGraphics.InitiateSprites += JetFishGraphics_InitiateSprites;

        On.Deer.ctor += Deer_ctor;
        On.Deer.Act += Deer_Act;
        On.DeerGraphics.DrawSprites += DeerGraphics_DrawSprites;

        On.MoreSlugcats.Yeek.ctor += Yeek_ctor;
        On.MoreSlugcats.YeekState.Feed += YeekState_Feed;
        On.MoreSlugcats.Yeek.SetPlayerHoldingBodyMass += Yeek_SetPlayerHoldingBodyMass;
        On.MoreSlugcats.Yeek.GetSegmentRadForCollision += Yeek_GetSegmentRadForCollision;

        On.Leech.ctor += Leech_ctor;
        On.Leech.Attached += Leech_Attached;
        On.LeechGraphics.Radius += LeechGraphics_Radius;

        On.StaticWorld.InitStaticWorld += StaticWorld_InitStaticWorld;
        On.Creature.ctor += Creature_ctor;
    }

    private static void Creature_ctor(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        //CHECK IF OUR FATNESS WAS DISABLED IN THE REMIX MENU
        self.GetBelly().fatDisabled = !patch_MiscCreatures.CheckFattable(self);
    }

    /*
    public static void YeekFixPatch()
    {
        //On.Y
        _ = new Hook(typeof(YeekFix.FixedYeekGraphics).GetMethod(nameof(YeekFix.FixedYeekGraphics.GraphSegmentRad)), FixedYeekGraphics_GraphSegmentRad);
        _ = new Hook(typeof(YeekFix.FixedYeekState).GetMethod(nameof(YeekFix.FixedYeekState.Feed)), FixedYeekState_Feed);
    }
    */

    private static void StaticWorld_InitStaticWorld(On.StaticWorld.orig_InitStaticWorld orig)
    {
        orig();

        foreach (CreatureTemplate critTempl in StaticWorld.creatureTemplates)
        {
            if (critTempl.type == CreatureTemplate.Type.Deer)
            {
                StaticWorld.creatureTemplates[critTempl.type.Index].meatPoints = 30;
            }
        }
    }

    

	public static int GetChub(Creature self)
	{
		if (self.GetBelly().fatDisabled)
			return 0; //NO FATTING ALLOWED
		
		int critNum = self.abstractCreature.ID.RandomSeed;
		UnityEngine.Random.seed = critNum;
		int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
		if (critChub == 8)
			return 4;
		else
			return 0;
	}
	
	public static bool CheckFattable(Creature crit)
	{
		if (crit is Player player)
        {
            int playerNum = player.playerState.playerNumber;
            if (player.isNPC)
                return BPOptions.fatPups.Value;
            else
            {
                if (playerNum == 0)
                    return BPOptions.fatP1.Value;
                if (playerNum == 1)
                    return BPOptions.fatP2.Value;
                if (playerNum == 2)
                    return BPOptions.fatP3.Value;
                if (playerNum == 3)
                    return BPOptions.fatP4.Value;
            } 
        }
        if (crit is Lizard && !BPOptions.fatLiz.Value)
			return false;
		if (crit is LanternMouse && !BPOptions.fatMice.Value)
			return false;
		if (crit is Scavenger && !BPOptions.fatScavs.Value)
			return false;
		if (crit is Cicada && !BPOptions.fatSquids.Value)
			return false;
		if (crit is NeedleWorm && !BPOptions.fatNoots.Value)
			return false;
		if (crit is Centipede && !BPOptions.fatCentis.Value)
			return false;
		if (crit is DaddyLongLegs && !BPOptions.fatDll.Value)
			return false;
		if (crit is Vulture && !BPOptions.fatVults.Value)
			return false;
		if (crit is MirosBird && !BPOptions.fatMiros.Value)
			return false;
		if (crit is DropBug && !BPOptions.fatWigs.Value)
			return false;
		if (crit is BigEel && !BPOptions.fatEels.Value)
			return false;
        if (crit is JetFish && !BPOptions.fatJets.Value)
            return false;
        if (crit is Deer && !BPOptions.fatDeer.Value)
            return false;
        if (crit is Yeek && !BPOptions.fatYeeks.Value)
            return false;
        if (crit is Leech && !BPOptions.fatLeechs.Value)
            return false;

        return true;
	}


    public static void CheckIn(Creature self)
	{
        int critNum = self.abstractCreature.ID.RandomSeed;
        bool guestExists = false;

        if (self.abstractCreature.GetAbsBelly().myFoodInStomach != -1)
        {
            guestExists = true;
        }

        if (guestExists)
        {
            //ALREADY EXISTS
            if (self is BigEel)
                FeedBigEel(self as BigEel, self.abstractCreature.GetAbsBelly().myFoodInStomach);
            //else if (self is DropBug) //I GUESS THEIR BELLY SIZE IS JUST REMEMBERED AUTOMARICALLY??? HUH...
            //    UpdateBellySize(self as DropBug, self.abstractCreature.GetAbsBelly().myFoodInStomach);
            else if (self is DropBug) //BUT THEY HAVE ANOTHER REALLY ANNOYING ISSUE WITH THEIR DICTIONARY VALUES NOT ALWAYS EXISTING >:(
                BellyPlus.InitializeCreatureMini(self); //WE SHOULD DO THIS ANYWAYS
            return;
        }
        BellyPlus.InitializeCreatureMini(self);

        if (self is DropBug && CheckFattable(self))
        {
            int critChub = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
            if (critChub == 8)
                self.abstractCreature.GetAbsBelly().myFoodInStomach = 3; //DROPWIGS DON'T GET A 1 TO 1 RATIO
            //self.abstractCreature.GetAbsBelly().myFoodInStomach = critChub;
            //UpdateBellySize(self as DropBug, self.abstractCreature.GetAbsBelly().myFoodInStomach);
        }

        else if (CheckFattable(self))
        {
            self.abstractCreature.GetAbsBelly().myFoodInStomach = Mathf.FloorToInt(Mathf.Lerp(3, 9, UnityEngine.Random.value));
        }
    }




    public static void UpdateBellySize(Creature self, int amnt)
    {
        if (self is DropBug && self.graphicsModule != null)
			(self.graphicsModule as DropBugGraphics).bodyThickness += 0.3f * amnt;
	
		else if (self is Deer && !BellyPlus.VisualsOnly())
		{
			float massMod = 1f + Mathf.Max(0f, (self.abstractCreature.GetAbsBelly().myFoodInStomach - 2) / 20f);
			for (int i = 1; i < 5; i++)
			{
                float num = (float)i / 4f;
                num = (1f - num) * 0.5f + Mathf.Sin(Mathf.Pow(num, 0.5f) * 3.1415927f) * 0.5f;
                num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(num, 1f, 0.2f)), 0.7f);
                self.bodyChunks[i].rad = Mathf.Lerp(10f, 35f, num) * massMod;
			}
		}
    }



    private static void BPBigEel_ctor(On.BigEel.orig_ctor orig, BigEel self, AbstractCreature abstractCreature, World world)
    {
		orig(self, abstractCreature, world);

        if (self.abstractCreature.GetAbsBelly().myFoodInStomach != -1)
        {
            //ALREADY EXISTS
            FeedBigEel(self, self.abstractCreature.GetAbsBelly().myFoodInStomach);
            return;
        }
        BellyPlus.InitializeCreatureMini(self);
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("--SPAWNING BIGEEL " + self.bodyChunks[0].rad);

        if (GetChub(self) >= 4)
		{
			self.abstractCreature.GetAbsBelly().myFoodInStomach += 2;
            FeedBigEel(self, 2);
		}
	}

    private static void BPBigEel_Swallow(On.BigEel.orig_Swallow orig, BigEel self)
    {
		//LEVIATHAN FEEEED!
		for (int i = 0; i < self.clampedObjects.Count; i++)
		{
			if (self.clampedObjects[i].chunk.owner is Creature && !(self.clampedObjects[i].chunk.owner as Creature).Template.smallCreature && !self.GetBelly().fatDisabled)
			{
				self.abstractCreature.GetAbsBelly().myFoodInStomach += 1;
                FeedBigEel(self, 1);
			}
		}
		orig(self);
	}
	
	public static void FeedBigEel(BigEel self, int amnt)
	{
		//int amnt = self.abstractCreature.GetAbsBelly().myFoodInStomach;

        for (int i = 0; i < self.bodyChunks.Length; i++)
		{
			float num = (float)i / (float)(self.bodyChunks.Length - 1);
			num = (1f - num) * 0.5f + Mathf.Sin(Mathf.Pow(num, 0.5f) * 3.1415927f) * 0.5f;
			self.bodyChunks[i].rad += Mathf.Lerp(1f, 3f, num) * amnt;   //(10f, 60f, num);
			if (i == 0)
				Debug.Log("FEED BIGEEL " + self.bodyChunks[i].rad);
        }
	}


    private static void BPBigSpiderGraphics_ctor(On.BigSpiderGraphics.orig_ctor orig, BigSpiderGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (GetChub(self.bug) >= 4)
        {
            self.bodyThickness += 2f;
        }
    }


    public static void NootGraphics(On.NeedleWormGraphics.orig_ctor orig, NeedleWormGraphics self, PhysicalObject ow)
	{
		orig.Invoke(self, ow);
		
		if (GetChub(self.worm) >= 4)
        {
			if (self.fatness != 1.35f)
				self.worm.bodyChunks[0].mass *= 1.5f; //TRY NOT TO RUN THIS MULTIPLE TIMES..
			self.fatness = 1.35f;
		}
	}
	
	private static float NootRad(On.NeedleWormGraphics.orig_GraphSegmentRad orig, NeedleWormGraphics self, int i)
	{
		float result = orig.Invoke(self, i);
		if (i - self.snout.Length < self.worm.bodyChunks.Length && self.fatness > 1.25f)
		{
			float radMod = Mathf.Max(1f, self.fatness);
			//return self.worm.GetSegmentRadForCollision(i - self.snout.Length) * Mathf.Lerp(0.75f, 1.35f * capMod, self.fatness);
			result *= radMod;
		}
		return result;
	}
	
	

    private static void BPMirosBird_Update(On.MirosBird.BirdLeg.orig_Update orig, MirosBird.BirdLeg self)
    {
        orig(self);

        //YEESH, THIS SHOULD REALLY BE AN IL HOOK BUT...
        if (GetChub(self.bird) >= 4 && !BellyPlus.VisualsOnly())
        {
            if (self.footSecurePos != null && self.lastFootSecurePos == null)
            {
                self.room.PlaySound(SoundID.Miros_Piston_Ground_Impact, self.Foot.pos, 1.5f, 0.8f);
                //self.room.InGameNoise(new InGameNoise(self.footSecurePos.Value, 800f, self.bird, 1f));
            }
            if (self.footSecurePos != null && self.lastFootSecurePos == null && !Custom.DistLess(self.Foot.pos, self.Foot.lastPos, 60f))
            {
                self.room.PlaySound(SoundID.Miros_Piston_Sharp_Impact, self.Foot.pos, 1.5f, 0.75f);
                //this.SmallSparks(this.Foot.pos, this.Foot.pos);
            }
        }
    }


    private static void BPMirosBird_ctor(On.MirosBird.orig_ctor orig, MirosBird self, AbstractCreature abstractCreature, World world)
    {
		orig(self, abstractCreature, world);

		if (GetChub(self) >= 4 && !BellyPlus.VisualsOnly())
		{
            for (int j = 0; j < self.bodyChunks.Length; j++)
            {
                self.bodyChunks[j].mass *= 1.45f;
            }
        }
    }

    public static bool IsPlayerOnscreen(Creature self)
	{
		if (self.room != null)
		{
			float myPos = self.bodyChunks[0].pos.x;
			for (int i = 0; i < self.room.game.Players.Count; i++)
			{
				if (self.room.game.Players[i].Room.index == self.room.abstractRoom.index
					&& self.room.game.Players[i].realizedCreature != null
					&& self.room.game.Players[i].realizedCreature.Consious 
					&& self.room.game.Players[i].realizedCreature.room != null
					&& self.room.game.Players[i].realizedCreature.room == self.room
					&& Mathf.Abs(self.room.game.Players[i].realizedCreature.bodyChunks[0].pos.x - myPos) < (60 * 20)
				)
				{
					return true;
				}
			}
		}
		return false;
	}
	
	//MAKE THE FAT ONES SLOW DOWN WHEN THEY ARE VISUALLY ON THE SAME SCREEN AS A PLAYER
    private static void BPMirosBird_Act(On.MirosBird.orig_Act orig, MirosBird self)
    {
		orig(self);

		if (GetChub(self) >= 4 && self.Consious && !BellyPlus.VisualsOnly())
		{
			//self.mainBodyChunk.vel.x *= 0.6f;
			if (Mathf.Abs(self.mainBodyChunk.vel.x) > 4 && IsPlayerOnscreen(self))
                self.mainBodyChunk.vel.x *= 0.8f;
        }
    }

    public static bool centiSkipDeath = false;
    private static void BPCentipede_Shock(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
    {
        //Debug.Log("----SUBTR PLR " + (shockObj as Player).slugcatStats.name.value);
        if (shockObj != null && shockObj is Player && (shockObj as Player).slugcatStats?.name?.value == "thedronemaster" && (shockObj as Player).playerState.foodInStomach == (shockObj as Player).MaxFoodInStomach)
		{
			//IF WE GOT THIS FAR THAT MEANS THE DRONEMASTER WAS FULL WHEN THE SHOCK OCCURRED
			(shockObj as Player).playerState.foodInStomach--; //SO LETS JUST... UNDO THAT FOR A QUICK SEC
			BellyPlus.fullBellyOverride = true;
			centiSkipDeath = true;
            //Debug.Log("----SUBTR FOOD " + (shockObj as Player).playerState.foodInStomach);
            //orig.Invoke(self, shockObj); //NOT QUITE
            self.Shock(shockObj);
			(shockObj as Player).stun *= BPOptions.fatArmor.Value ? 4 : 2; //BIGGER STUN (PLUS THEY'LL PROBABLY HAVE FAT RESISTANCE)
        }
		else
			orig.Invoke(self, shockObj);
    }

    private static void BPCentipede_Die(On.Centipede.orig_Die orig, Centipede self)
    {
        if(centiSkipDeath)
			centiSkipDeath = false; //SHRIMPLY DON'T
		else
			orig.Invoke(self);
    }


    private static void BPMirosBirdGraphics_DrawSprites(On.MirosBirdGraphics.orig_DrawSprites orig, MirosBirdGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (GetChub(self.bird) >= 4)
        {
            sLeaser.sprites[self.BodySprite].element = Futile.atlasManager.GetElementWithName("Cicada8body");
			//MAKE THEM LOOK PHAT
            sLeaser.sprites[self.BodySprite].scaleX = 3;
            sLeaser.sprites[self.BodySprite].scaleY = 2.5f;
			sLeaser.sprites[self.BodySprite].rotation += 180;
            
        }
    }

    private static void BPPoleMimicRopeGraphics_DrawSprite(On.PoleMimicGraphics.PoleMimicRopeGraphics.orig_DrawSprite orig, PoleMimicGraphics.PoleMimicRopeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (GetChub(self.owner.pole) >= 0) //3)
        {
			float extraChonk = 3f;
			
			if ((sLeaser.sprites[0] as TriangleMesh).vertices.Length != self.segments.Length * 4)
            {
                self.InitiateSprites(sLeaser, rCam);

            }
            Vector2 vector = self.owner.pole.rootPos - self.owner.pole.stickOutDir * 30f;
            vector += Custom.DirVec(Vector2.Lerp(self.segments[1].lastPos, self.segments[1].pos, timeStacker), vector) * 1f;
            float num = 2f;
            for (int i = 0; i < self.segments.Length; i++)
            {
                float num2 = (float)i / (float)(self.segments.Length - 1);
                Vector2 vector2 = Vector2.Lerp(self.segments[i].lastPos, self.segments[i].pos, timeStacker);
                Vector2 normalized = (vector - vector2).normalized;
                Vector2 vector3 = Custom.PerpendicularVector(normalized) ;
                float num3 = Vector2.Distance(vector, vector2) / 3f * extraChonk;
                float num4 = self.owner.StemLookLikePole(num2, timeStacker);
                float num5 = Mathf.Lerp((i % 2 == 0) ? Mathf.Lerp(4f, 1.5f, num2) : Mathf.Lerp(1.4f, 0.75f, num2), 2f, Mathf.Pow(num4, 0.75f));
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, vector - normalized * num3 - vector3 * Mathf.Lerp(num, num5, 0.5f) - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, vector - normalized * num3 + vector3 * Mathf.Lerp(num, num5, 0.5f) - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 + normalized * num3 - vector3 * num5 - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + normalized * num3 + vector3 * num5 - camPos);
                float num6 = (1f + rCam.room.lightAngle.magnitude / 10f) * Mathf.Pow(num4, 1.8f);
                (sLeaser.sprites[1] as TriangleMesh).MoveVertice(i * 4, vector - normalized * num3 - vector3 * (Mathf.Lerp(num, num5, 0.5f) + num6) - camPos);
                (sLeaser.sprites[1] as TriangleMesh).MoveVertice(i * 4 + 1, vector - normalized * num3 + vector3 * (Mathf.Lerp(num, num5, 0.5f) + num6) - camPos);
                (sLeaser.sprites[1] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 + normalized * num3 - vector3 * (num5 + num6) - camPos);
                (sLeaser.sprites[1] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + normalized * num3 + vector3 * (num5 + num6) - camPos);
                vector = vector2;
                num = num5;
            }
        }
        else
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
    }



    private static void DropBug_ctor(On.DropBug.orig_ctor orig, DropBug self, AbstractCreature abstractCreature, World world)
    {
        orig.Invoke(self, abstractCreature, world);

        CheckIn(self);
    }


    private static void DropBug_Update(On.DropBug.orig_Update orig, DropBug self, bool eu)
    {
        bool ceilingJump = self.fromCeilingJump;
        orig(self, eu);
        //IF WE JUST STOPPED A FROMCEILINJUMP AND WE'RE FAT, MAKE A BIG IMPACT
        if (self.graphicsModule != null && ceilingJump != self.fromCeilingJump && !self.fromCeilingJump && self.abstractCreature.GetAbsBelly().myFoodInStomach >= 3)
        {
            self.room.PlaySound(SoundID.Lizard_Heavy_Terrain_Impact, self.mainBodyChunk, false, 1, 1);
            self.room.game.cameras[0].screenShake += 0.5f;
        }
    }

    private static void DropBug_MoveTowards(On.DropBug.orig_MoveTowards orig, DropBug self, Vector2 moveTo)
    {
        orig(self, moveTo);

        if (self.graphicsModule != null && self.Footing && !BellyPlus.VisualsOnly() && self.abstractCreature.GetAbsBelly().myFoodInStomach >= 2)
        {
            self.bodyChunks[0].vel *= 0.8f;
            self.bodyChunks[1].vel *= 0.8f;
            self.bodyChunks[2].vel *= 0.8f;
        }
    }

    private static void DropBugGraphics_DrawSprites(On.DropBugGraphics.orig_DrawSprites orig, DropBugGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        //REMEMBER OUR BODY THICKNESS SO WE CAN RESET IT. WE CAN'T KEEP IT IF WE'RE HANING ON THE CEILING (TOO FAT...)
        float origThicc = self.bodyThickness;
        self.bodyThickness = Mathf.Lerp(self.bodyThickness, 1.4f, self.ceilingMode); //TRANSITION TO A NORMAL THICKNESS AS WE ENTER CEILING MODE
        orig(self, sLeaser, rCam, timeStacker, camPos);
        //AND THEN REVERT TO OUR REAL SIZE
        self.bodyThickness = origThicc;
    }


    private static void JetFish_ctor(On.JetFish.orig_ctor orig, JetFish self, AbstractCreature abstractCreature, World world)
    {
        orig.Invoke(self, abstractCreature, world);
        CheckIn(self);
        float massMod = 1f + Mathf.Max(0f, (self.abstractCreature.GetAbsBelly().myFoodInStomach - 6) / 10f);
        self.bodyChunks[0].mass *= massMod;
        self.bodyChunks[1].mass *= massMod;
    }
	
	private static void JetFishGraphics_InitiateSprites(On.JetFishGraphics.orig_InitiateSprites orig, JetFishGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        orig(self, sLeaser, rCam);
		sLeaser.sprites[self.BodySprite].scale *= Custom.LerpMap(self.fish.abstractCreature.GetAbsBelly().myFoodInStomach, 3f, 10f, 1f, 2f);
        //sLeaser.sprites[self.BodySprite].scaleY *= 3f;
    }
	
	private static void JetFish_Act(On.JetFish.orig_Act orig, JetFish self)
    {
        MovementConnection movementConnection = (self.AI.pathFinder as FishPather).FollowPath(self.room.GetWorldCoordinate(self.mainBodyChunk.pos), true);
        if (movementConnection != null && (movementConnection.type == MovementConnection.MovementType.ShortCut || movementConnection.type == MovementConnection.MovementType.NPCTransportation))
        {
            if (!self.GetBelly().fatDisabled && ModManager.MMF && self.AI.denFinder.GetDenPosition() != null && movementConnection.destinationCoord == self.AI.denFinder.GetDenPosition() && self.AI.behavior == JetFishAI.Behavior.ReturnPrey && self.grasps[0] != null && !(self.grasps[0].grabbed is Creature))
            {
                self.abstractCreature.GetAbsBelly().myFoodInStomach += 1;
                UpdateBellySize(self, 0);
            }
        }
        orig(self);
    }


    private static void JetFishGraphics_Update(On.JetFishGraphics.orig_Update orig, JetFishGraphics self)
    {
		orig(self);
		for (int i = 0; i < 2; i++)
		{
			for (int k = 0; k < self.tails.GetLength(1); k++)
			{
				self.tails[i, k].rad *= 1 + Mathf.Max(0f, (self.fish.abstractCreature.GetAbsBelly().myFoodInStomach - 6) / 3f);
                //self.tails[i, k].rad *= 5f;

            }
		}
	}


    private static void Deer_ctor(On.Deer.orig_ctor orig, Deer self, AbstractCreature abstractCreature, World world)
    {
        orig.Invoke(self, abstractCreature, world);
        CheckIn(self);
        UpdateBellySize(self, 0);
        self.Template.meatPoints = 30;
    }

    private static void Deer_Act(On.Deer.orig_Act orig, Deer self, bool eu, float support, float forwardPower)
    {
        if (self.eatCounter == 50 && !self.GetBelly().fatDisabled)
        {
            self.abstractCreature.GetAbsBelly().myFoodInStomach += 1;
            UpdateBellySize(self, 0);
            //self.graphicsModule.Reset();
            //self.graphicsModule.
        }
        orig(self, eu, support, forwardPower);
    }


    private static void DeerGraphics_DrawSprites(On.DeerGraphics.orig_DrawSprites orig, DeerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        //IT'S MESSY BUT IT'LL DO. A SMALL WINDOW WHERE SPRITES CAN RESIZE
        if (self.deer.eatCounter < 50 && self.deer.eatCounter > 40)
        { //THIS IS JUST A COPY OF WHAT THE CTOR DOES
            for (int i = 0; i < 5; i++)
            {
                sLeaser.sprites[self.BodySprite(i)].scaleX = self.owner.bodyChunks[i].rad / 8f * 1.05f;
                sLeaser.sprites[self.BodySprite(i)].scaleY = self.owner.bodyChunks[i].rad / 8f * 1.3f;
            }
        }
    }


    private static void Yeek_ctor(On.MoreSlugcats.Yeek.orig_ctor orig, Yeek self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        CheckIn(self);
        //elf.creature.GetAbsBelly().myFoodInStomach = 10;
    }

    private static void YeekState_Feed(On.MoreSlugcats.YeekState.orig_Feed orig, YeekState self, int CycleTimer)
    {
        //if (CheckFattable())
        if (BPOptions.fatYeeks.Value)
            self.creature.GetAbsBelly().myFoodInStomach += 2;
        //Debug.Log(" YEEKERTON " + self.creature.GetAbsBelly().myFoodInStomach);
    }


    private static float Yeek_GetSegmentRadForCollision(On.MoreSlugcats.Yeek.orig_GetSegmentRadForCollision orig, Yeek self, int seg)
    {
        float fatMod = 1f + Mathf.Max(0f, (self.abstractCreature.GetAbsBelly().myFoodInStomach - 5) / 5f);
        return orig(self, seg) * fatMod;
    }

    private static void Yeek_SetPlayerHoldingBodyMass(On.MoreSlugcats.Yeek.orig_SetPlayerHoldingBodyMass orig, Yeek self)
    {
        bool wasStandard = self.usingStandardMass;
        orig(self);
        if (wasStandard && !BellyPlus.VisualsOnly())
        {
            float fatMod = 1f + Mathf.Max(0f, (self.abstractCreature.GetAbsBelly().myFoodInStomach - 5) / 10f);
            for (int i = 0; i < self.bodyChunks.Length; i++)
            {
                self.bodyChunks[i].mass *= fatMod;
            }
        }
    }

    //FOR THE YEEKFIX VERSION (soon to be the real version)
    public static float FixedYeekGraphics_GraphSegmentRad(Func<YeekFix.FixedYeekGraphics, int, float> orig, YeekFix.FixedYeekGraphics self, int i)
    {
        float fatMod = 1f + Mathf.Max(0f, (self.myYeek.abstractCreature.GetAbsBelly().myFoodInStomach - 5) / 5f);
        return orig(self, i) * fatMod;
    }

    public static void FixedYeekState_Feed(Action<YeekFix.FixedYeekState, int> orig, YeekFix.FixedYeekState self, int CycleTimer)
    {

        orig(self, CycleTimer);
        self.creature.GetAbsBelly().myFoodInStomach += 2;
    }


    private static void Leech_ctor(On.Leech.orig_ctor orig, Leech self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        CheckIn(self);
    }

    private static float LeechGraphics_Radius(On.LeechGraphics.orig_Radius orig, LeechGraphics self, float bodyPos)
    {
        float fatMod = 1f + Mathf.Max(0f, (self.leech.abstractCreature.GetAbsBelly().myFoodInStomach - 6) / 10f);
        return orig(self, bodyPos) * fatMod;
    }

    private static void Leech_Attached(On.Leech.orig_Attached orig, Leech self)
    {
        orig(self);
        float drainRate = 0.012f;
        if (self.jungleLeech)
            drainRate *= 0.1f;
        if (self.abstractCreature.GetAbsBelly().myFoodInStomach >= 16)
            drainRate *= 0.1f;

        if (UnityEngine.Random.value < drainRate && !self.GetBelly().fatDisabled)
        {
            self.abstractCreature.GetAbsBelly().myFoodInStomach += 1;
            //Debug.Log("BLOAT " + self.abstractCreature.GetAbsBelly().myFoodInStomach);
        }
    }


    public static void Centipede_ctor(On.Centipede.orig_ctor orig, Centipede self, AbstractCreature abstractCreature, World world)
    {
		orig.Invoke(self, abstractCreature, world);
		
		if (GetChub(self) == 4)
		{
			for (int i = 0; i < self.bodyChunks.Length; i++)
			{
				//PHAT
				self.bodyChunks[i].rad += Mathf.Lerp(1.5f, 2.5f, self.size) / 1.5f; //-2, 3.5
            }
		}
    }
	
	
	
	
	
	public static void Centipede_DrawSprites(On.CentipedeGraphics.orig_DrawSprites orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
		orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
		
		if (GetChub(self.centipede) == 4)
		{
			for (int i = 0; i < self.owner.bodyChunks.Length; i++)
			{
				//PHAT
				// self.bodyChunks[i].rad += Mathf.Lerp(1.5f, 2.5f, self.size) / 1.5f; //-2/3.5
				sLeaser.sprites[self.SegmentSprite(i)].scaleY *= 1.5f;
            }
		}
    }
	
	

    private static void DaddyGraphics_DrawSprites(On.DaddyGraphics.orig_DrawSprites orig, DaddyGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
		//Debug.Log("DO I EVEN RUN..." + self.daddy.digestingCounter);
		orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
		if (BellyPlus.VisualsOnly() || self.daddy.GetBelly().fatDisabled)
			return;
		
		//if (self.daddy.digestingCounter > 0)
		if (self.daddy.eatObjects.Count > 0)
        {
			//THIS WAS JUST FROM INITIATESPRITES MOVED INTO HERE
			for (int i = 0; i < self.daddy.bodyChunks.Length; i++)
			{
				sLeaser.sprites[self.BodySprite(i)].scale = (self.owner.bodyChunks[i].rad * 1.1f + 2f) / 8f;
				if (self.daddy.HDmode)
				{
					sLeaser.sprites[self.EyeSprite(i, 2)].scale = 0.0625f * self.owner.bodyChunks[i].rad * 2f;
				}
			}
		}
	}

    public static int GetRef(NeedleWorm self)
	{
		return self.abstractCreature.ID.RandomSeed;
	}
	

	public static void DDL_Update(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
	{
		//if (self.digestingCounter > 15)
		if (self.eatObjects.Count > 0 && !BellyPlus.VisualsOnly() && !self.GetBelly().fatDisabled)
		{
			for (int i = 0; i < self.bodyChunks.Length; i++)
			{
				self.bodyChunks[i].rad += 1/30f;
			}

            for (int j = 0; j < self.bodyChunkConnections.Length; j++)
            {
                self.bodyChunkConnections[j].distance += 1/80f;
            }
			
			//ALRIGHT ALRIGHT, FOR THE MORBID ONES -NOOO THIS IS TOO HARD. WE CAN'T EVEN REFERENCE THE TORSO SPRITES WITHOUT THE LEASER...
			// if (self.HDmode && self.dummy != null)
			// {
				// this.dummy = new DaddyGraphics.HunterDummy(this, this.DummySprite());
			// }

            //TENTACLE THICCNESS
            for (int k = 0; k < self.tentacles.Length; k++)
            {
                //self.tentacles[k].tChunks
                for (int l = 0; l < self.tentacles[k].tChunks.Length; l++)
                {
					//self.tentacles[k].tChunks[l].rad += 10f;
                    //OKAY THIS INCREASES LIKE, GRAB RANGE... BUT NOT THE VISUAL SIZE. NOT EXACTLY WHAT WE WANT
                }
            }
        }
		
		orig.Invoke(self, eu);

		//if (self.digestingCounter == 1 && self.graphicsModule != null)
		//	self.graphicsModule.Reset();
	}
	
	
	public static float BP_Rad(On.PoleMimic.orig_Rad orig, PoleMimic self, int index)
	{
		return (orig.Invoke(self, index) * 8f);
	}
	
}