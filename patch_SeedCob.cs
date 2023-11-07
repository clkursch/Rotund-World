using RWCustom;
using UnityEngine;
using MoreSlugcats;
using System.Collections.Generic;
using static patch_Misc;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using static Rewired.Controller;

public class patch_SeedCob
{
    public static int shrinkStalks = 0;
    public static int shrinkLimit = 80;

    public static void Patch()
    {
		On.SeedCob.Update += BP_Update;
        //On.SeedCob.Update += SeedCob_Update; //THE OLD ONE

        On.SeedCob.DrawSprites += SeedCob_DrawSprites;
        On.Room.Loaded += Room_Loaded;

        IL.SeedCob.Update += SeedCob_Update;

        //THESE DIDN'T EVEN WORK
        //On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
        //On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;
    }

    

    //THIS ONE WAS WRONG. IT WAS BORKED. THE TRYNEXT WAS CORRECT, BUT THE REST WAS NOT
    public static void SeedCob_Update_OLD(ILContext il)
    {
        BellyPlus.Logger.LogInfo("BELLYPLUS - ADDING SEED COB UPDATE ");
        var cursor = new ILCursor(il);

        //			// if (AbstractCob.dead || !(open > 0.8f))
        //IL_0880: ldarg.0
        //IL_0881: ldfld float32 SeedCob::open
        //IL_0886: ldc.r4 0.8

        if (!cursor.TryGotoNext(MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<SeedCob>(nameof(SeedCob.open)),
            i => i.MatchLdcR4(0.8f)
        ))
        {
            throw new Exception("Failed to match IL for SEEDCOB UPDATE! ");
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((SeedCob myCorn) =>
        {
            return (myCorn.open > 0.8f && myCorn.rootPos != myCorn.bodyChunks[1].pos);
        });
        BellyPlus.Logger.LogInfo("BELLYPLUS - SEED COB UPDATE COMPLETE");
		
		
		//TO CONSIDER...
		// if (player.room != room || player.handOnExternalFoodSource.HasValue || player.eatExternalFoodSourceCounter >= 1 || player.dontEatExternalFoodSourceCounter >= 1 || player.FoodInStomach >= player.MaxFoodInStomach || (player.touchedNoInputCounter <= 5 && !player.input[0].pckp && (!ModManager.MSC || !(player.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC))) || (ModManager.MSC && !(player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear)) || player.FreeHand() <= -1)		
		// IL_096f: ldloc.s 12
		// IL_0971: ldfld int32 Player::touchedNoInputCounter
		// IL_0976: ldc.i4.5
		// IL_0977: bgt.s IL_09b7
    }


    //THANKS FOR THE ATTEMPT FORETHOUGHT! UNFORTUNATELY THIS DOESN'T SEEM TO WORK EITHER. BUT I'VE GOT SOMETHING ELSE IN MIND...
    public static void SeedCob_Update_OLD2(ILContext il)
    {
        BellyPlus.Logger.LogInfo("BELLYPLUS - ADDING SEED COB UPDATE ");
        //// if (AbstractCob.dead || !(open > 0.8f))
        //IL_0880: ldarg.0
        //IL_0881: ldfld float32 SeedCob::open
        //IL_0886: ldc.r4 0.8
        // IL_088b: ble.un IL_0b52
        // This branches if less than, so we need to skip it

        var cursor = new ILCursor(il);

        var checkFailDest = cursor.DefineLabel();
        var checkSucceedDest = cursor.DefineLabel();

        if (!cursor.TryGotoNext(MoveType.Before,
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<SeedCob>(nameof(SeedCob.open)),
            i => i.MatchLdcR4(0.8f),
            i => i.MatchBleUn(out checkFailDest)))
        {
            throw new Exception("Failed to match IL for SEEDCOB UPDATE!");
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((SeedCob myCorn) =>
        {
            return (myCorn.open > 0.8f && myCorn.rootPos != myCorn.bodyChunks[1].pos);
        });

        cursor.Emit(OpCodes.Brtrue, checkSucceedDest);
        cursor.Emit(OpCodes.Br, checkFailDest);

        if (!cursor.TryGotoNext(MoveType.After,
            i => i.MatchBleUn(out _)))
        {
            throw new Exception("Failed to match IL for SEEDCOB UPDATE, branch!");
        }

        checkSucceedDest = cursor.MarkLabel();

        //Plugin.Logger.LogWarning(cursor.Context);
        BellyPlus.Logger.LogInfo("BELLYPLUS - SEED COB UPDATE COMPLETE");
    }





    public static void SeedCob_Update(ILContext il)
    {
        BellyPlus.Logger.LogInfo("BELLYPLUS - ADDING SEED COB UPDATE ");
        //// if (AbstractCob.dead || !(open > 0.8f))
        //IL_0880: ldarg.0
        //IL_0881: ldfld float32 SeedCob::open
        //IL_0886: ldc.r4 0.8
        // IL_088b: ble.un IL_0b52
        // This branches if less than, so we need to skip it

        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After,
            i => i.MatchLdarg(0),
            i => i.MatchCallOrCallvirt< SeedCob > ("get_AbstractCob"),
            i => i.MatchLdfld<SeedCob.AbstractSeedCob>(nameof(SeedCob.AbstractSeedCob.dead))
        ))
        {
            throw new Exception("Failed to match IL for SEEDCOB UPDATE!");
        }

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate((SeedCob myCorn) =>
        {
            //return (myCorn.open > 0.8f && myCorn.rootPos != myCorn.bodyChunks[1].pos);
            return true;
        });
        //var checkSucceedDest = cursor.DefineLabel();
        cursor.Emit(OpCodes.Or);

        //if (!cursor.TryGotoNext(MoveType.After,
        //    i => i.MatchBleUn(out _)))
        //{
        //    throw new Exception("Failed to match IL for SEEDCOB UPDATE, branch!");
        //}

        //checkSucceedDest = cursor.MarkLabel();

        BellyPlus.Logger.LogInfo("BELLYPLUS - SEED COB UPDATE COMPLETE");
    }





    public static void SeedCob_Update_UNFINISHED(ILContext il)
    {
        BellyPlus.Logger.LogInfo("BELLYPLUS - ADDING SEED COB UPDATE ");
        //// for (int k = 0; k < (ModManager.MSC ? room.abstractRoom.creatures.Count : room.game.Players.Count); k++)
	    //IL_0890: ldc.i4.0
	    //IL_0891: stloc.s 11

        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, //EITHER BEFORE OR AFTER WILL WORK HERE, SINCE THIS IS JUST SETTING AN UNUSED VAR NOW
            i => i.MatchLdcI4(0),
            //i => i.MatchStloc(11) //"btw, when matching locals, in this case stloc 11, you should use the version with an out parameter, since that number can change if they edit the code
            i => i.MatchStloc(11)

            //i => i.MatchLdelemRef(),
            //i => i.MatchLdflda<BodyChunk>(nameof(BodyChunk.vel)),
            //i => i.MatchLdflda<Vector2>(nameof(Vector2.y)),
            //i => i.MatchDup(),
            //i => i.MatchLdindR4(),
            //i => i.MatchLdarg(0),
            //i => i.MatchCallOrCallvirt<PhysicalObject>("get_EffectiveRoomGravity"),
            //i => i.MatchSub(),
            //i => i.MatchStindR4()
        ))
        {
            throw new Exception("Couldn't match in whatever hook this is");
        }


        var label = cursor.DefineLabel();
        cursor.MarkLabel(label);
        //THEN JUMP BACK BEFORE THE SPOT WHERE WE DECIDE IF WE SHOULD RUN THE NEXT LINE OR NOT
        cursor.GotoPrev(MoveType.Before,
            i => i.MatchLdcI4(0),
            i => i.MatchStloc(11)
        );

        //THIS IS THE BEFORE. WE NEED TO FIND THE AFTER FIRST
        if (!cursor.TryGotoNext(MoveType.After,
            
            i => i.MatchLdcI4(0),
            i => i.MatchStloc(11)
        ))
        {
            throw new Exception("Couldn't match in whatever hook this is");
        }

    }




    private static Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        Color result = orig(itemType, intData);

        if (itemType == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb)
            result = new Color(0.9019608f, 0.05490196f, 0.05490196f);

        return result;
    }

    private static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        string result = orig(itemType, intData);

        if (itemType == AbstractPhysicalObject.AbstractObjectType.SeedCob)
        {
            result = "Symbol_Lantern";
        }
        return result;
    }


    private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
    {
        if (self.game == null)
        {
            return;
        }

		orig(self);

        int num14 = 0; //IDK

        if (self.abstractRoom.shelter && self.game.world.rainCycle.CycleProgression <= 0f && self.world.game.IsStorySession) // self.abstractRoom.shelter && self.abstractRoom.firstTimeRealized)
        {

            //HOW MANY CORNS DO WE GOT?
            WinState myWinState = self.world.game.GetStorySession.saveState.deathPersistentSaveData.winState;
            int cornCnt = patch_Misc.GetStoredCorn(myWinState, true);
            int stumpCnt = patch_Misc.GetStoredCorn(myWinState, false);
            Debug.Log("HOW MUCH CORN HAVE WE STORED? " + cornCnt);

            //ONCE FOR EACH CORN
            for (int i = 0; i < (cornCnt + stumpCnt); i++)
            {
                //WorldCoordinate spawnPos = self.room.GetWorldCoordinate(self.bodyChunks[0].pos);
                WorldCoordinate spawnPos = new WorldCoordinate(self.abstractRoom.index, self.shelterDoor.playerSpawnPos.x, self.shelterDoor.playerSpawnPos.y, 0);
                //EntityID ID = self.room.game.GetNewID();


                //AbstractPhysicalObject abstractPhysicalObject = new SeedCob.AbstractSeedCob(self.world, null, self.GetWorldCoordinate(self.roomSettings.placedObjects[num14].pos), self.game.GetNewID(), self.abstractRoom.index, num14, false, self.roomSettings.placedObjects[num14].data as PlacedObject.ConsumableObjectData);
                AbstractPhysicalObject abstractPhysicalObject = new SeedCob.AbstractSeedCob(self.world, null, spawnPos, self.game.GetNewID(), self.abstractRoom.index, num14, false, null);
                (abstractPhysicalObject as AbstractConsumable).isConsumed = false;
                self.abstractRoom.entities.Add(abstractPhysicalObject);
                abstractPhysicalObject.Realize();
                abstractPhysicalObject.realizedObject.PlaceInRoom(self);
                

                SeedCob myCorn = abstractPhysicalObject.realizedObject as SeedCob;
                SnapStalk(myCorn);
                myCorn.bodyChunkConnections[0].distance = 40f;
                

                //FAR ENOUGH AWAY FROM CENTER
                myCorn.rootPos = new Vector2(20, 0);
                myCorn.placedPos = new Vector2(20, 0);


                bool live = (i >= stumpCnt);
                Debug.Log("CORN SPAWN ATTEMPT " + live);
                if (live)
                {
                    myCorn.open = 1;
                    myCorn.lastOpen = 1;
                    myCorn.AbstractCob.opened = true;
                    myCorn.AbstractCob.dead = false;

                    //this.seedsPopped = new bool[this.seedPositions.Length];
                    //MAKE THEM POPPED
                    if (myCorn.open >= 1)
                    {
                        for (int l = 0; l < myCorn.seedsPopped.Length; l++)
                        {
                            myCorn.seedsPopped[l] = true;
                        }
                    }
                }
                else
                {
                    spawnFakeUtilityFoods(myCorn);
                }

                
            }
        }

    }




    //THIS CAN ONLY HAPPEN WITH MSC ENABLED.... SO NON MSC NEEDS THEIR OWN VERSION
    public static void spawnFakeUtilityFoods(SeedCob self)
    {
        Debug.Log("SPAWN KERNALS");
        self.AbstractCob.opened = true;
        self.AbstractCob.dead = true;
        self.AbstractCob.Consume();
        self.canBeHitByWeapons = false;
        
        for (int i = 0; i < 6; i++)
        {
            AbstractConsumable abstractConsumable = null;
            //NON MSC GAMES NEED A REPLACEMENT
            if (ModManager.MSC)
                abstractConsumable = new AbstractConsumable(self.room.world, MoreSlugcatsEnums.AbstractObjectType.Seed, null, self.room.GetWorldCoordinate(self.placedPos), self.room.game.GetNewID(), -1, -1, null);
            else
                abstractConsumable = new AbstractConsumable(self.room.world, AbstractPhysicalObject.AbstractObjectType.EggBugEgg, null, self.room.GetWorldCoordinate(self.placedPos), self.room.game.GetNewID(), -1, -1, null);

            self.room.abstractRoom.AddEntity(abstractConsumable);
            abstractConsumable.pos = self.room.GetWorldCoordinate(self.placedPos);
            abstractConsumable.RealizeInRoom();
            abstractConsumable.realizedObject.firstChunk.HardSetPosition(Vector2.Lerp(self.bodyChunks[0].pos, self.bodyChunks[1].pos, (float)i / 5f));
        }
        self.AbstractCob.spawnedUtility = true;
    }




    public static void SnapStalk(SeedCob self)
    {
        if (self.room != null)
        {
            self.room.PlaySound(SoundID.Seed_Cob_Open, self.firstChunk);
            self.room.PlaySound(SoundID.Drop_Bug_Grab_Creature, self.firstChunk);
            patch_Player.MakeSparks(self, 1, 6);
        }

        self.rootPos = new Vector2(0, 0);
        //self.placedPos = new Vector2(0, 0);
        self.stalkSegments = 1;

        //GIVE THEM MORE ITEM-LIKE PHYSICS
        //self.airFriction = 0.999f;
        self.airFriction = 0.95f;
        self.gravity = 0.9f;
        self.buoyancy = 0.8f;
        self.bodyChunks[0].mass = 0.15f;
        self.bodyChunks[1].mass = 0.01f;

        //self.bodyChunks[1].collideWithTerrain = false;

        Debug.Log("CORN UPROOTED");
        shrinkStalks = shrinkLimit;
        self.AbstractCob.Consume(); //WE SHOULD INCLUDE THIS SO IT WON'T GROW BACK
    }


    private static void SeedCob_Update(On.SeedCob.orig_Update orig, SeedCob self, bool eu)
    {
        orig.Invoke(self, eu);

        bool uprooted = false;

        //ALL WE'RE DOING HERE IS RE-RUNNING THE CHECK EXCLUDING THE PART THAT CHECKS IF YOUR BELLY IS FULL
        if (!self.AbstractCob.dead && self.open > 0.8f)
        {
            //for (int k = 0; k < self.room.game.Players.Count; k++)
            for (int k = 0; k < self.room.abstractRoom.creatures.Count; k++)
            {
                Creature realizedCreature = self.room.abstractRoom.creatures[k].realizedCreature;
                if (realizedCreature != null
                    && realizedCreature is Player player //OKAY THIS SHOULD DO THE TRICK. INCLUDE SLUGPUPS
                    && realizedCreature.room == self.room
                    && player.handOnExternalFoodSource == null
                    && player.eatExternalFoodSourceCounter < 1
                    && player.dontEatExternalFoodSourceCounter < 1
                    && player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear
                    //&& (realizedCreature as Player).FoodInStomach < (realizedCreature as Player).MaxFoodInStomach 
                    && player.dead == false //HOW DID I FORGET THIS?
                                            //AND DON'T AUTO EAT IF WE'RE UPROOTED
                    && ((player.touchedNoInputCounter > 5 && !uprooted) || (realizedCreature as Player).input[0].pckp))
                {
                    int num5 = player.FreeHand();
                    if (num5 > -1)
                    {
                        Vector2 pos2 = realizedCreature.mainBodyChunk.pos;
                        Vector2 vector3 = Custom.ClosestPointOnLineSegment(self.bodyChunks[0].pos, self.bodyChunks[1].pos, pos2);
                        if (Custom.DistLess(pos2, vector3, 25f))
                        {
                            player.handOnExternalFoodSource = new Vector2?(vector3 + Custom.DirVec(pos2, vector3) * 5f);
                            player.eatExternalFoodSourceCounter = 15;
                            if (self.room.game.IsStorySession && player.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat && !(player.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC) && self.room.game.GetStorySession.playerSessionRecords != null)
                            {
                                //self.room.game.GetStorySession.playerSessionRecords[(self.room.game.Players[k].state as PlayerState).playerNumber].AddEat(self);
                                self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(self);
                            }
                            self.delayedPush = new Vector2?(Custom.DirVec(pos2, vector3) * 1.2f);
                            self.pushDelay = 4;
                            if (player.graphicsModule != null)
                            {
                                (player.graphicsModule as PlayerGraphics).LookAtPoint(vector3, 100f);
                            }
                        }
                    }
                }
            }
        }
    }



    private static void SeedCob_DrawSprites(On.SeedCob.orig_DrawSprites orig, SeedCob self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        //DO THE SAME THING AS UPDATE
        bool uprooted = false;
        Vector2 origPlacement = self.placedPos;
        if (self.rootPos == new Vector2(0, 0))
        {
            uprooted = true;
            self.rootPos = self.bodyChunks[1].pos; //PRETEND OUR STALK IS RIGHT UNDERNEATH US
            self.placedPos = self.bodyChunks[1].pos;
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (uprooted)
        {
            self.rootPos = new Vector2(0, 0);
            self.placedPos = origPlacement ;// new Vector2(0, 0);

            //SHRINK THE STALKS IF THEY ARE STILL VISIBLE
            if (shrinkStalks > 0 && sLeaser.sprites[self.StalkSprite(0)].scale > 0)
            {
                

                //sLeaser.sprites[self.StalkSprite(0)].scale *= 0.98f;
                //sLeaser.sprites[self.StalkSprite(1)].scale *= 0.02f;
                float stalkScale = Mathf.Lerp( 0f, 1f, ((shrinkStalks -1) / shrinkLimit));
                //sLeaser.sprites[self.StalkSprite(0)].scale = stalkScale;
                //sLeaser.sprites[self.StalkSprite(1)]. = stalkScale;
                Debug.Log("CAM " + camPos);

                Vector2 rootPlacement = new Vector2(origPlacement.x, origPlacement.y - (self.stalkLength + self.bodyChunkConnections[0].distance ));
                sLeaser.sprites[self.StalkSprite(0)].ScaleAroundPointAbsolute(rootPlacement, stalkScale, stalkScale);
                sLeaser.sprites[self.StalkSprite(1)].ScaleAroundPointAbsolute(rootPlacement, stalkScale, stalkScale);
                //sLeaser.sprites[self.StalkSprite(0)].ScaleAroundPointRelative(rootPlacement, stalkScale, 1);
                //sLeaser.sprites[self.StalkSprite(1)].ScaleAroundPointRelative(rootPlacement, stalkScale, 1);
            }

        }

    }






    public static void BP_Update(On.SeedCob.orig_Update orig, SeedCob self, bool eu)
	{
        //IF THE CORN IS PULLED TWICE IT'S STALKS NORMAL LENGTH
        if (!BellyPlus.VisualsOnly() && BPOptions.detachablePopcorn.Value && self.rootPos != new Vector2(0, 0) && !Custom.DistLess(self.bodyChunks[1].pos, self.rootPos, self.stalkLength + 28f) && (self.grabbedBy.Count > 0 || self.abstractPhysicalObject.Room.shelter))
        {
            //STALK BREAK!! 
            SnapStalk(self);
            return; //DON'T UPDATE THE REST
        }

        bool uprooted = false;
        bool carried = (self.grabbedBy.Count > 0);
        Vector2 origPlacement = self.placedPos;
        if (self.rootPos == new Vector2(0, 0))
        {
            uprooted = true;
            self.rootPos = self.bodyChunks[1].pos; //PRETEND OUR STALK IS RIGHT UNDERNEATH US
            self.placedPos = self.bodyChunks[1].pos;
            self.cobDir = Custom.DegToVec(Custom.AimFromOneVectorToAnother(self.bodyChunks[1].pos, self.bodyChunks[0].pos));
            self.rootDir = self.cobDir;

            //UNDO VELOCITY GAINS
            self.firstChunk.vel -= (self.placedPos - self.firstChunk.pos) / Custom.LerpMap(Vector2.Distance(self.placedPos, self.firstChunk.pos), 5f, 100f, 2000f, 150f, 0.8f);
            self.bodyChunks[1].vel -= (self.placedPos + self.cobDir * self.bodyChunkConnections[0].distance - self.bodyChunks[1].pos) / Custom.LerpMap(Vector2.Distance(self.placedPos + self.cobDir * self.bodyChunkConnections[0].distance, self.bodyChunks[1].pos), 5f, 100f, 800f, 50f, 0.2f);

            //FIDDLE WITH CHUNK COLLISION IF WE'RE BEING CARRIED
            if (self.grabbedBy.Count > 0)
            {
                Vector2 checkPoint = new Vector2((self.bodyChunks[1].pos.x + self.bodyChunks[0].pos.x) / 2f, (self.bodyChunks[1].pos.y + self.bodyChunks[0].pos.y) / 2f);
                //IF OUR MIDDLE CHUNK HAS WALL BETWEEN US, UNDO COLLISION
                if (self.room.GetTile(checkPoint * 1f).Terrain == Room.Tile.TerrainType.Solid)
                    self.bodyChunks[1].collideWithTerrain = false;

                //IF NEITHER OUR MIDDLE NOR END CHUNK IS IN A WALL, RESUME COLLISION
                else if (self.room.GetTile(self.bodyChunks[1].pos * 1f).Terrain != Room.Tile.TerrainType.Solid)
                    self.bodyChunks[1].collideWithTerrain = true;

                //IF OUR LOWER CHUNK IS IN A WALL AND UNDERNEATH OUR UPPER CHUNK, TRY TO LIFT IT
                if (!self.bodyChunks[1].collideWithTerrain && self.room.GetTile(self.bodyChunks[1].pos * 1f).Terrain == Room.Tile.TerrainType.Solid && self.bodyChunks[1].pos.y <= self.bodyChunks[0].pos.y)
                {
                    self.bodyChunks[1].vel.y = Mathf.Max(self.bodyChunks[1].vel.y, 0f);
                    self.bodyChunks[1].pos.y += 2f;
                }
            }
            else
            {
                self.bodyChunks[1].collideWithTerrain = true;
            }
            shrinkStalks--; //THIS WILL SHRINK FASTER IF MULTIPLE CORNS ARE ONSCREEN BUT WHATEVER 
        }





        orig.Invoke(self, eu);

        //bool uprooted = self.rootPos == self.bodyChunks[1].pos;
        //bool uprooted = self.rootPos == new Vector2(0,0);

        //ALL WE'RE DOING HERE IS RE-RUNNING THE CHECK EXCLUDING THE PART THAT CHECKS IF YOUR BELLY IS FULL
        if (!self.AbstractCob.dead && self.open > 0.8f)
		{
			//for (int k = 0; k < self.room.game.Players.Count; k++)
			for (int k = 0; k < self.room.abstractRoom.creatures.Count; k++)
			{
				Creature realizedCreature = self.room.abstractRoom.creatures[k].realizedCreature;
				if (realizedCreature != null 
					&& realizedCreature is Player player //OKAY THIS SHOULD DO THE TRICK. INCLUDE SLUGPUPS
					&& realizedCreature.room == self.room 
					&& player.handOnExternalFoodSource == null 
					&& player.eatExternalFoodSourceCounter < 1 
					&& player.dontEatExternalFoodSourceCounter < 1
					&& player.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear
                    //&& player.FoodInStomach < player.MaxFoodInStomach 
                    && player.dead == false //HOW DID I FORGET THIS?
                    //AND DON'T AUTO EAT IF WE'RE UPROOTED - OKAY PUPS CAN EAT, BUT ONLY IF IT'S NOT HELD BY ANYONE
					&& ((player.touchedNoInputCounter > 5 && (!uprooted || (player.isNPC && !carried))) || (realizedCreature as Player).input[0].pckp)
                    //&& (!player.isNPC || BPOptions.fatPups.Value || (player.npcCharacterStats))//DON'T LET PUPS OVEREAT IF THEY AREN'T ALLOWED
                    
                    )
				{
					int num5 = player.FreeHand();
					if (num5 > -1)
					{
						Vector2 pos2 = realizedCreature.mainBodyChunk.pos;
						Vector2 vector3 = Custom.ClosestPointOnLineSegment(self.bodyChunks[0].pos, self.bodyChunks[1].pos, pos2);
						if (Custom.DistLess(pos2, vector3, 25f))
						{
							player.handOnExternalFoodSource = new Vector2?(vector3 + Custom.DirVec(pos2, vector3) * 5f);
							player.eatExternalFoodSourceCounter = 15;
							if (self.room.game.IsStorySession && player.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat && !(player.abstractCreature.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC) && self.room.game.GetStorySession.playerSessionRecords != null)
							{
								//self.room.game.GetStorySession.playerSessionRecords[(self.room.game.Players[k].state as PlayerState).playerNumber].AddEat(self);
								self.room.game.GetStorySession.playerSessionRecords[(player.abstractCreature.state as PlayerState).playerNumber].AddEat(self);
							}
							self.delayedPush = new Vector2?(Custom.DirVec(pos2, vector3) * 1.2f);
							self.pushDelay = 4;
							if (player.graphicsModule != null)
							{
								(player.graphicsModule as PlayerGraphics).LookAtPoint(vector3, 100f);
							}
						}
					}
				}
			}
		}

        if (uprooted)
        {
            self.rootPos = new Vector2(0, 0);
            //self.placedPos = new Vector2(0, 0);
            self.placedPos = origPlacement;
        }
    }


}