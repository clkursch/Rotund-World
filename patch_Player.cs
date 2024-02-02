using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
//using System.Reflection;
//using MonoMod.RuntimeDetour; //AN EXTRA LITTLE REFERENCE JUST FOR THIS HOOK MOD
using MoreSlugcats;

using Mono.Cecil.Cil;
using MonoMod.Cil;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using BepInEx;
using DressMySlugcat;
using NoirCatto;
using System.Linq;




/*

//check grabbing stuck player while holding centipede
//attempted fix for expedition enhanced exception

*/


namespace RotundWorld;


public class patch_Player
{
	private static readonly int seedKey = 0;
	public static float partialPip = 0; //SOMETHING TO KEEP TRACK INBETWEEN
	public static bool expRotund = false;

	//public delegate int orig_MaxFoodInStomach(Player self); //NEAT...
	//public delegate int orig_FoodInStomach(Player self);
	//public delegate int orig_CanRetrieveSlugFromBack(Player self);

	public static void Patch()
	{
		On.Player.ctor += (PlayerPatch);

		// using (new DetourContext(33)) //DUNNO IF THIS HELPED
		On.Player.AddFood += Player_AddFood; 
		On.Player.SubtractFood += Player_SubtractFood;
		On.Player.AddQuarterFood += Player_AddQuarterFood;
		On.Player.EatMeatUpdate += Player_EatMeatUpdate;
		On.Player.TerrainImpact += Player_TerrainImpact; //THIS ONE CAN STAY ON WITH VISUALS ONLY
		On.Player.GrabUpdate += Player_GrabUpdate;
		
        // ANYTHING BELOW HERE IS SKIPPED FOR VISUALS ONLY MODE
		// using (new DetourContext(-333))
		//JOLLY FIXES MAKES US RELEASE OUR PULLER WHEN WE PRESS JUMP
		On.Player.Jump += Player_Jump;
		On.Player.Update += BPPlayer_Update; //SO JOLLYFIXES DOESNT LET GO OF GRABBED TUGGERS WHEN BOOSTING

		
		//JOLLY CO-OP ALSO EDITS THESE, BUT WE PROBABLY WANT OURS TO RUN LAST
		// using (new DetourContext(333))
		On.Player.ObjectEaten += Player_ObjectEaten;
		On.Player.Grabability += Player_Grabability;
		On.Player.CanIPickThisUp += Player_CanIPickThisUp; //TO UNDO SOME STUFF THAT JOLLY CO-OP DOES
        On.Player.ReleaseObject += Player_ReleaseObject;

		On.Player.MovementUpdate += Player_MovementUpdate;
		IL.Player.MovementUpdate += BPPlayer_MovementUpdate;
		IL.Player.MovementUpdate += BPPlayer_MovementUpdate2;
        IL.Player.MovementUpdate += RadToTerrainRad; //MULTI USE IL! SWAPS ALL BODYCHUNK.RAD TO TERRAINRAD
		IL.Player.UpdateBodyMode += RadToTerrainRad;
        IL.ShortcutHelper.Update += BPShortcutHelper;
        //IL.ShortcutHelper.ObjectPusher.Update += BPObjectPusher;
        

        On.Player.UpdateBodyMode += Player_UpdateBodyMode;
		On.Player.UpdateAnimation += BPUpdateAnimation;
        
        On.Player.ThrowObject += Player_ThrowObject;
		On.Player.SlugcatGrab += Player_SlugcatGrab;
		On.Player.PickupCandidate += Player_PickupCandidate;
		On.Player.SpitOutOfShortCut += Player_SpitOutOfShortCut;
		On.Player.Collide += BP_Collide;
		
		On.Player.checkInput += BP_CheckInput;
		On.Player.Die += BP_Die;
		On.Player.Tongue.decreaseRopeLength += BPdecreaseRopeLength;
		On.Player.DeathByBiteMultiplier += BP_DeathByBiteMultiplier;
		On.Player.Stun += BP_Stun;
        On.Spear.HitSomething += BPSpear_HitSomething;
		// On.Player.SpearStick += ;

		On.Player.ClassMechanicsGourmand += Player_ClassMechanicsGourmand;
		On.Player.SlugSlamConditions += BP_SlugSlamConditions;
		On.Player.CanBeSwallowed += BP_CanBeSwallowed;
        On.Player.CanEatMeat += BPPlayer_CanEatMeat;
		//I'M TIRED OF THIS NOT WORKING!
		On.Player.JollyUpdate += BP_JollyUpdate;
        On.Player.JollyEmoteUpdate += Player_JollyEmoteUpdate;
        //On.Player.ChangeCameraToPlayer +=  //IT'S A STATIC SO WE CAN'T DO IT? D:
        On.Player.TriggerCameraSwitch += BP_TriggerCameraSwitch; //JUST TO CATCH A CRASH I GUESS

        On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;
		On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
        On.Player.UpdateMSC += BPPlayer_UpdateMSC;
        On.Player.SwallowObject += Player_SwallowObject;
        On.Player.Regurgitate += Player_Regurgitate;
        On.Creature.Abstractize += Creature_Abstractize; //CERTAIN PARTS OF ROTUND WORLD BREAK (FOODONBACK) WHEN THIS HAPPENS

		//OKAY THIS ONE IS FOR ALL CREATURES
		On.Creature.Violence += BP_Violence;
		// }

        On.StoryGameSession.CreateJollySlugStats += StoryGameSession_CreateJollySlugStats;
		On.RainWorldGame.Update += RainWorldGame_Update;
        //SLIME_CUBED REPORTED THAT THIS IS NEEDED TO WORK AROUND A BUG WHERE MOD PRIORITIES FOR OTHER MODS AREN'T RESET UNLESS YOU RESET IT MANUALLY
        // using (new DetourContext()) { } //ISN'T A THING IN 1.9 UNFORTUNATELY...
    }

    //THE GAME SETS EVERYONES WEIGHTFAC TO 1 IN CO-OP. THAT SUCKS. UNDO THAT
    private static void StoryGameSession_CreateJollySlugStats(On.StoryGameSession.orig_CreateJollySlugStats orig, StoryGameSession self, bool m)
    {
        if (self.game.Players.Count == 0)
        {
            orig(self, m); //JollyCustom.Log("[JOLLY] NO PLAYERS IN SESSION!!", false);
            return;
        }

        orig(self, m); //RUN THE ORIG THAT SETS OUR WEIGHT WRONG

        //self.characterStatsJollyplayer = new SlugcatStats[4];
        PlayerState playerState = self.game.Players[0].state as PlayerState;
        for (int i = 0; i < self.game.world.game.Players.Count; i++)
        {
			//RECREATED PRETTY MUCH MOST OF THIS... THIS SHOULD REALLY BE AN IL BUT WHATEV
            playerState = (self.game.Players[i].state as PlayerState);
            SlugcatStats.Name playerClass = self.game.rainWorld.options.jollyPlayerOptionsArray[playerState.playerNumber].playerClass;
            if (playerClass == null)
            {
                playerClass = self.saveState.saveStateNumber;
            }
			//GET OUR REAL CLASS'S WEIGHT AND REASSIGN IT TO US
            SlugcatStats reStats = new SlugcatStats(playerClass, m);
			self.characterStatsJollyplayer[playerState.playerNumber].bodyWeightFac = reStats.bodyWeightFac;
        }
    }

    private static void PlayerPatch(On.Player.orig_ctor orig, Player player, AbstractCreature abstractCreature, World world)
	{
		orig(player, abstractCreature, world);
		
		//THIS IS AN ATTEMPT TO CATCH ANY PLAYERS THAT TRY AND SPAWN OUTSIDE OF THE NORMAL CONDITIONS (LIKE BEING RESPAWNED BY THE ENDGAME REVIVE PROCESS)
		bool illegalSpawn = false; //CHECK FOR 400 BECAUSE FAST SHELTER DOORS BUMPS US UP TO 340
		if (player.abstractCreature.world.rainCycle.timer > 400 && !player.isNPC && player.abstractCreature.world.game.IsStorySession)
		{
			illegalSpawn = true;
		}
		

        if (player.isNPC || player.playerState.isGhost)
		{
			//SetNPCID(player);
		}


		int playerNumber = player.playerState.playerNumber; // GetPlayerNum(player);
		
		//IF WE'RE AN ILLEGAL SPAWN, ONLY CONTINUE IF OUR BELLYSTATE DOESN'T EXIST YET (BECAUSE WE MAY WANT TO KEEP THIS ILLEGAL)
		if (illegalSpawn || player.abstractCreature.GetAbsBelly().myFoodInStomach != -1) // && bellyStats.Length <= playerNumber) //THERE DOESN'T SEEM TO BE A GOOD WAY TO CHECK IF THIS EXISTS?
		{
			Debug.Log("ILLEGAL SPAWN! CANCEL THE STARTUP " + playerNumber);
			UpdateBellySize(player); //STILL DO THIS THOUGH. THIS WEIRD CLONE MAY WANT IT
			if (player.GetBelly().foodOnBack != null)
				player.GetBelly().foodOnBack.ReplaceOwner(player);
            return;
		}

		IntVector2 personalBelly = SlugcatStats.SlugcatFoodMeter(player.slugcatStats.name); //BECAUSE PLAYERSTATS.MAXFOOD ONLY GIVES PLAYER 1'S BELLY SIZE

		player.GetBelly().bigBelly = player.isGourmand || (personalBelly.x > 10 && personalBelly.x - personalBelly.y >= 4);
		player.GetBelly().foodOnBack = new FoodOnBack(player);
		//INITIALIZE FOOD LEVEL IF IT HASN'T BEEN ALREADY
		if (player.abstractCreature.GetAbsBelly().myFoodInStomach == -1)
		{
			player.abstractCreature.GetAbsBelly().myFoodInStomach = player.FoodInStomach;
			player.abstractCreature.GetAbsBelly().origPoleSpeed = player.slugcatStats.poleClimbSpeedFac;
		}
		


		expRotund = ModManager.Expedition && Custom.rainWorld.ExpeditionMode && Expedition.ExpeditionGame.activeUnlocks.Contains("bur-rotund");

        if (BPOptions.debugLogs.Value)
			Debug.Log("-FK YOU GIMME MY GODDANG BONUS FRUIT!" + player.abstractCreature.GetAbsBelly().myFoodInStomach + " OVERSTUFFED: " + GetOverstuffed(player) + " CURRENT FOOD:" + player.FoodInStomach + " BONUS:" + player.GetBelly().bigBelly + " " + player.slugcatStats.maxFood);

        //HOLDUP!... CURRENTFOOD ACTUALLY SAVES CORRECLTY!!! WOW THAT IS INCREDIBLE
		//SO THIS TRIMS OFF THE EXTRA FAT FROM OUR SAVE FILE AND ADDS IT TO OUR P1 INDIVIDUAL WEIGHT. WHICH IS WHAT WE WANT, EVEN IN 1.6
        int blubberStored = player.CurrentFood - player.MaxFoodInStomach;
		if (player.playerState.playerNumber == 0 && !player.isNPC) // || BellyPlus.individualFoodEnabled -IT DOESN'T SAVE THAT DATA. DOESN'T WORK
        {
			if (blubberStored > 0)
			{
				Debug.Log("-BLUBBER CHECK! " + blubberStored + " CURRENT FOOD" + player.CurrentFood + " MAX FOOD " + player.MaxFoodInStomach + " PERSONALFOOD" + player.abstractCreature.GetAbsBelly().myFoodInStomach);
				player.abstractCreature.GetAbsBelly().myFoodInStomach += blubberStored;
				//BellyPlus.tomorrowsBonusFood = blubberStored; //DEPRECIATED
				player.playerState.foodInStomach -= blubberStored;
			}
			//else
			//	BellyPlus.tomorrowsBonusFood = 0; //DEPRECIATED
		}
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("BIG BELLIED SLUG? " + player.GetBelly().bigBelly + " CURR FOOD?" + player.abstractCreature.GetAbsBelly().myFoodInStomach + " - " + player.slugcatStats.name + " - " + player.playerState.slugcatCharacter + " - " + SlugcatStats.SlugcatFoodMeter(player.slugcatStats.name) + " - " + player.slugcatStats.bodyWeightFac + " - " + player.bodyChunks[0].mass);
        //Debug.Log(" CURRENT FOOD:" + player.CurrentFood + " BONUS:" + BellyPlus.bonusFood + " " + BellyPlus.bonusHudPip);


        //IF THERE ARE MULTIPLE PLAYERS, CHECK OUR INDIVIDUAL LONG TERM STORED FOOD FROM PREVIOUS SESSIONS, IF ANY
        if (player.abstractCreature.world.game.IsStorySession && !player.isSlugpup && player.abstractCreature.world.game.Players.Count > 1)
		{
            
			//IF OUR STORED FOOD VALUE WAS THE HIGHEST (OR TIED) REMAIN LORGE
			WinState myWinState = player.abstractCreature.world.game.GetStorySession.saveState.deathPersistentSaveData.winState;
			int playerCount = player.abstractCreature.world.game.Players.Count;
            Debug.Log("--- PLAYERCOUNTE " + playerCount);
            int biggestFood = patch_Misc.FindHighestLongtermFood(myWinState, playerCount);
			int myLongTrmFood = patch_Misc.MyLongtermFood(myWinState, playerNumber);
			if (myLongTrmFood == -1)
            {
				Debug.Log("-ERROR STATE -NO LONGTERM FOOD HAS BEEN STORED YET! p" + playerNumber );
				//UNLESS WE'RE NOT PLAYER 1 AND WE'RE JOINING IN A GAME
				if (playerNumber >= 1 && !BellyPlus.individualFoodEnabled) //WE DO NEED THE FIRST PLAYER TO ALWAYS RUN THIS CHECK AND SKIP THE REST, IN CASE SOMEONE HAD DISABLED AND RE-ENABLED THE MOD
                    player.abstractCreature.GetAbsBelly().myFoodInStomach = Math.Min(biggestFood, player.slugcatStats.foodToHibernate);
			}
			else if (BellyPlus.sharedPips && myLongTrmFood >= biggestFood)
			{ //OKAY WE NEED TO SKIP THIS FOR NON-SHARED PIPS OTHERWISE A FAT PLAYER LEAVING THE SESSION WILL HAVE ALL SORTS OF WEIRD EFFECTS
                Debug.Log("-WE'VE GOT THE BIGGEST FOOD! DO NOTHING. P" + playerNumber + " -TO: " + myLongTrmFood);
            }
            //SPECIAL CASE FOR INDIVIDUAL FOOD BARS MOD!
            else if (BellyPlus.individualFoodEnabled)
			{
                player.playerState.foodInStomach = Mathf.Min(Mathf.Max(player.playerState.foodInStomach, myLongTrmFood), SlugcatStats.SlugcatFoodMeter(player.slugcatStats.name).x);
				player.abstractCreature.GetAbsBelly().myFoodInStomach = Mathf.Max(player.playerState.foodInStomach, myLongTrmFood);
            }
			else
			{
				int newfood = myLongTrmFood;
                //IF ANYONE'S GOT AT LEAST 1, THEN WE GET 1
                //newfood = Math.Max(newfood, Math.Min(biggestFood, 1));
				//OKAY MAKE IT UP TO 3 - MAXIMUM FOOD
				//TAKE OUR STARTING THRESHOLD INT ACCOUNT FOR THIS CALCULATION!
				int threshMod = (expRotund && !player.isNPC) ? (player.slugcatStats.maxFood - player.slugcatStats.foodToHibernate) : 0;
                newfood = Math.Max(newfood, Math.Min(biggestFood, player.slugcatStats.maxFood - 3 - threshMod));
				newfood = Math.Max(newfood, 0); //SAFETY

                player.abstractCreature.GetAbsBelly().myFoodInStomach = newfood;
				Debug.Log("-LONG TERM FOOD ADJUSTMENT! P" + playerNumber + " -TO: " + newfood);
			}
		}
		else
		{
			Debug.Log("-NOT A CO-OP GAME! FALLING BACK ON OUR HUD'S FOOD VALUE ");
		}

		
        // IF WE'RE IN ARENA MODE, MAKE ALL OUR BELLY SIZES THE SAME (Monk sized)
        if (player.abstractCreature.world.game.IsArenaSession)
        {
            player.slugcatStats.maxFood = 5;
            player.slugcatStats.foodToHibernate = 3;
        }

        //CHECK IF OUR FATNESS IS DISABLED
        if ((playerNumber == 0 && BPOptions.fatP1.Value == false && !player.isNPC) ||
			(playerNumber == 1 && BPOptions.fatP2.Value == false) ||
			(playerNumber == 2 && BPOptions.fatP3.Value == false) ||
			(playerNumber == 3 && BPOptions.fatP4.Value == false) ||
            (player.isNPC && BPOptions.fatPups.Value == false))
		{
			player.abstractCreature.GetAbsBelly().myFoodInStomach = 0;
		}


		if (!illegalSpawn)
			BellyPlus.lockEndFood = false;
        UpdateBellySize(player);
		if (BPOptions.debugLogs.Value)
			Debug.Log("-FK YOU PT 2 - GIMME MY GODDANG BONUS FRUIT!" + player.abstractCreature.GetAbsBelly().myFoodInStomach + " OVERSTUFFED: " + GetOverstuffed(player) + " CURRENT FOOD:" + player.CurrentFood + " BONUS:" + BellyPlus.bonusFood + " " + BellyPlus.bonusHudPip);
	}


    

    /*
	public static void BPShortcutHelper(ILContext il)
	{
        BellyPlus.Logger.LogInfo("ATTEMPTING TO ADD SHORTCUT IL! ");
        var cursor = new ILCursor(il);
		int count = 0;

        var player = 0;
        var k = 0;
        while (cursor.TryGotoNext(MoveType.After, 
			i => i.MatchLdloc(out player), //HERE
			i => i.MatchCallOrCallvirt<PhysicalObject>("get_bodyChunks"),
			i => i.MatchLdloc(out k), //AND HERE
			i => i.MatchLdelemRef(),
			i => i.MatchLdflda<BodyChunk>(nameof(BodyChunk.rad))
			))
		{
            count++;
            cursor.Emit(OpCodes.Ldloc, player); //OUT TO HERE
			cursor.Emit(OpCodes.Ldloc, k);

            cursor.EmitDelegate<System.Func<float, Player, int, float>>((float oldRad, Player player2, int k2) =>
            {
				return player2.bodyChunks[k2].TerrainRad;
			});


		}
        BellyPlus.Logger.LogInfo("SHORTCUT IL LINES ADDED! " + count);
    }
	*/




    



    private static void BPShortcutHelper(ILContext il)
    {
        var cursor = new ILCursor(il);

        //IL_0355: ldc.r4 20
        //IL_035a: ldloc.1
        //IL_035b: callvirt instance class BodyChunk[] PhysicalObject::get_bodyChunks()
        //IL_0360: ldloc.s 6
        //IL_0362: ldelem.ref
        //IL_0363: ldfld float32 BodyChunk::rad
        //IL_0368: add

        var player = 0;
        var k = 0;
        var x = 0;
        while (cursor.TryGotoNext(MoveType.After, 
			i => i.MatchLdloc(out player),
            i => i.MatchCallOrCallvirt<PhysicalObject>("get_bodyChunks"),
            i => i.MatchLdloc(out k),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<BodyChunk>(nameof(BodyChunk.rad))))
        {
            x++;
            cursor.Emit(OpCodes.Ldloc, player);
            cursor.Emit(OpCodes.Ldloc, k);

            cursor.EmitDelegate((float rad, Player player, int k) =>
            {
                return player.bodyChunks[k].TerrainRad;
            });
        }
		
        BellyPlus.Logger.LogInfo("BELLYPLUS SHORTCUT IL LINES ADDED! " + x);
    }
	




	//OKAY THIS DOESN'T ACCTUALLY WORK. BUT TURNS OUT WE DON'T NEED IT TO WORK! THANK GOD...
    public static void BPObjectPusher(ILContext il)
	{
        BellyPlus.Logger.LogInfo("ATTEMPTING TO ADD OBJECPUSHER IL! ");
        var cursor = new ILCursor(il);
		int count = 0;

        //IL_00f8: ldarg.0
        //IL_00f9: ldfld class PhysicalObject ShortcutHelper/ObjectPusher::obj
        //IL_00fe: callvirt instance class BodyChunk[] PhysicalObject::get_bodyChunks()
        //IL_0103: ldloc.s 4
        //IL_0105: ldelem.ref
        //IL_0106: ldfld float32 BodyChunk::rad

        var player = 0;
        var k = 0;
        while (cursor.TryGotoNext(MoveType.After, 
			// i => i.MatchLdloc(out player), //HERE
			// i => i.MatchLdfld<ShortcutHelper/ObjectPusher>("obj"),
			i => i.MatchLdfld<ShortcutHelper.ObjectPusher>(nameof(ShortcutHelper.ObjectPusher.obj)),
			i => i.MatchCallOrCallvirt<PhysicalObject>("get_bodyChunks"),
			i => i.MatchLdloc(out k), //AND HERE
			i => i.MatchLdelemRef(),
			i => i.MatchLdflda<BodyChunk>(nameof(BodyChunk.rad))
			))
		{
			// cursor.Emit(OpCodes.Ldloc, player); //OUT TO HERE
			cursor.Emit(OpCodes.Ldarg_0); //THIS REFERENCES 'this.' INSTANCE
			cursor.Emit(OpCodes.Ldloc, k);
			count++;

            //cursor.EmitDelegate<System.Func<float, ShortcutHelper.ObjectPusher, int, float>>((float oldRad, ShortcutHelper.ObjectPusher mySelf, int k2) =>
            cursor.EmitDelegate((float rad, ShortcutHelper.ObjectPusher mySelf, int k) =>
            {
				return mySelf.obj.bodyChunks[k].TerrainRad;
			}
			);
		}
        BellyPlus.Logger.LogInfo("OBJECPUSHER IL LINES ADDED! " + count);
    }

    /*
    private static void BPShortcutHelper(ILContext il)
    {
        Debug.Log("ATTEMPTING TO ADD SHORTCUT IL! ");
        var cursor = new ILCursor(il);
        int count = 0;
		
				
					
				

        var player = 0;
        var k = 0;
        try
        {
            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out player), //HERE
                i => i.MatchCallOrCallvirt<PhysicalObject>("get_bodyChunks"),
                i => i.MatchLdloc(out k),
                i => i.MatchLdelemRef(),
                i => i.MatchLdflda<BodyChunk>(nameof(BodyChunk.rad))
                ))
            {
                throw new Exception(" NOT MATCHES ARE ALLOWED HERE");
            }

            while (cursor.TryGotoNext(MoveType.After,
                i => i.MatchLdloc(out player), //HERE
                i => i.MatchCallOrCallvirt<PhysicalObject>("get_bodyChunks"),
                i => i.MatchLdloc(out k),
                i => i.MatchLdelemRef(),
                i => i.MatchLdflda<BodyChunk>(nameof(BodyChunk.rad))
                ))


                Debug.Log("IL HOOKS MATCHED");
            {
                cursor.Emit(OpCodes.Ldloc, player);
                cursor.Emit(OpCodes.Ldloc, k);
                count++;

                //cursor.EmitDelegate((float oldRad, Player player2, int k2) =>
                cursor.EmitDelegate<System.Func<float, Player, int, float>>((float oldRad, Player player2, int k2) =>
                {
                    return player2.bodyChunks[k2].TerrainRad;
                }
                );
            }
            Debug.Log("Shorcut IL LINES ADDED! " + count);
        }
        catch (Exception ex)
        {
            Debug.LogError("IL HOOK SEEMS TO NOT BE FINE, PRAY FOR HELP!!!!");
            Debug.LogException(ex);
            Debug.LogError(il);
            throw;
        }
    }
	*/


    private static void BPPlayer_MovementUpdate(ILContext il)
    {
        var cursor = new ILCursor(il);

        if (!cursor.TryGotoNext(MoveType.After, 
			i => i.MatchLdarg(0),
			i => i.MatchCallOrCallvirt<PhysicalObject>("get_bodyChunks"),
			i => i.MatchLdcI4(0),
			i => i.MatchLdelemRef(),
			i => i.MatchLdflda<BodyChunk>(nameof(BodyChunk.vel)),
			i => i.MatchLdflda<Vector2>(nameof(Vector2.y)),
			i => i.MatchDup(),
			i => i.MatchLdindR4(),
			i => i.MatchLdarg(0),
			i => i.MatchCallOrCallvirt<PhysicalObject>("get_EffectiveRoomGravity"),
			i => i.MatchSub(),
			i => i.MatchStindR4()))
        {
            throw new Exception("Couldn't match in whatever hook this is");
        }

        var label = il.DefineLabel();
        cursor.MarkLabel(label);

        if (!cursor.TryGotoPrev(MoveType.Before, 
			i => i.MatchLdarg(0),
			i => i.MatchLdsfld<Player.AnimationIndex>(nameof(Player.AnimationIndex.None)),
			i => i.MatchStfld<Player>(nameof(Player.animation))))
        {
            throw new Exception("Couldn't match whatever bla bla bla error you can recognize later");
        }
		else
            BellyPlus.Logger.LogInfo("PB PLAYERMOVEMENT IL ADDED! ");

        cursor.Emit(OpCodes.Br, label);
        /*
		we get a label to the end
		then move back to the start
		and emit a br
		which tells the code to skip to that label no matter what 
		(br stands for branch)
		this is the same as putting an if (false) { } around the code
		br is the same as a goto in C#
		*/


    }


    //OKAY WHILE THIS TECHNICALLY WORKED, THIS WAS NOT THE RIGHT WAY TO DO IT 
    /*
    private static void BPPlayer_MovementUpdate2(ILContext il)
    {
        //OKAY WE'RE DOING ANOTHER ONE. CAN WE DO THEM BOTH IN THE SAME HOOK?
        BellyPlus.Logger.LogInfo("ATTEMPTING TO ADD JUMP REMOVAL IL! ");
        var cursor = new ILCursor(il);
		
		// ... && wantToJump > 0)
		//IL_26d6: ldarg.0
		//IL_26d7: ldfld int32 Player::wantToJump
		//IL_26dc: ldc.i4.0
		//IL_26dd: ble.s IL_26f5
		
        int count = 0;
        var wantJmp = 0;
        while (cursor.TryGotoNext(MoveType.After,
            i => i.MatchLdarg(0),
			i => i.MatchLdfld<Player>(nameof(Player.wantToJump)),
            i => i.MatchLdcI4(0)
			// i => i.MatchLdloc(out k), //AND HERE
			// i => i.MatchLdelemRef(),
			// i => i.MatchLdflda<BodyChunk>(nameof(BodyChunk.rad))
			))
        {
            // cursor.Emit(OpCodes.Ldloc, player); 
            cursor.Emit(OpCodes.Ldarg_0); //OUT TO HERE //THIS REFERENCES 'this.' INSTANCE
                                          // cursor.Emit(OpCodes.Ldloc, k);
            count++;

            cursor.EmitDelegate((int wantToJump, Player mySelf) =>
            {
                int playerNum = GetPlayerNum(mySelf);
                if (!(BellyPlus.VisualsOnly() || IsStuck(mySelf) || self.GetBelly().pushingOther || self.GetBelly().pullingOther || self.GetBelly().boostCounter > 0))
                {
                    Debug.Log("IL SKIP REALJUMP");
                    Heave(mySelf);
                    return 0; //SKIP JUMP()
                }
                else
                {
                    Debug.Log("IL REALJUMP ");
                    return wantToJump;
                }
            });
        }
        BellyPlus.Logger.LogInfo("JUMP REMOVAL IL LINES ADDED! " + count);
    }
	*/


    /*
	private static void BPPlayer_MovementUpdate2NOT(ILContext il)
    {
        //OKAY WE'RE DOING ANOTHER ONE. CAN WE DO THEM BOTH IN THE SAME HOOK?
        BellyPlus.Logger.LogInfo("ATTEMPTING TO ADD JUMP REMOVAL IL! ");
        var cursor = new ILCursor(il);
		
		// // Jump();
		//IL_26ed: ldarg.0
		//IL_26ee: call instance void Player::Jump()
		
        int count = 0;
        while (cursor.TryGotoNext(MoveType.After,
            i => i.MatchLdarg(0),
			//i => i.MatchCallOrCallvirt<Player>("Jump")
			i => i.MatchCallOrCallvirt<Player>(nameof(Player.Jump))

			))
        {
            cursor.Emit(OpCodes.Ldarg_0); 
            count++;

            cursor.EmitDelegate((Player mySelf) =>
            {
                int playerNum = GetPlayerNum(mySelf);
				if (BellyPlus.VisualsOnly())
					mySelf.Jump(); //JUST JUMP
                else if (!(IsStuck(mySelf) || self.GetBelly().pushingOther || self.GetBelly().pullingOther || self.GetBelly().boostCounter > 0))
                {
                    Debug.Log("JUMP, BUT WITH A TWIST");
                    Heave(mySelf);
                }
                else
                {
					Debug.Log("OK I THINK WE DON'T JUMP (SKIP BC WE'RE STUCK OR SOMETH ");
                }
            });
        }
        BellyPlus.Logger.LogInfo("JUMP REMOVAL IL LINES ADDED! " + count);
    }
	*/

    private static void BPPlayer_MovementUpdate2(ILContext il)
    {
        //OKAY WE'RE DOING ANOTHER ONE. CAN WE DO THEM BOTH IN THE SAME HOOK?
        BellyPlus.Logger.LogInfo("ATTEMPTING TO ADD JUMP REMOVAL IL! ");
        var cursor = new ILCursor(il);

        // // Jump();
        //IL_26ed: ldarg.0
        //IL_26ee: call instance void Player::Jump()
        if (!cursor.TryGotoNext(MoveType.After, 
			i => i.MatchLdarg(0),
            i => i.MatchCallOrCallvirt<Player>(nameof(Player.Jump))))
        {
            throw new Exception("Failed to match IL for MOVEMENTUPDATE! (first)");
        }
        //CREATE A LABEL TO JUMP FORWARD TO LATER
        var label = cursor.DefineLabel();
        cursor.MarkLabel(label);
		//THEN JUMP BACK BEFORE THE SPOT WHERE WE DECIDE IF WE SHOULD RUN THE NEXT LINE OR NOT
        cursor.GotoPrev(MoveType.Before, i => i.MatchLdarg(0));
        
		cursor.Emit(OpCodes.Ldarg_0); //P SURE WE NEED THIS TO EMIT THE VALUE OF THIS (THE PLAYER) TO THE DELEGATE
        //cursor.EmitDelegate(() => myThing);
        cursor.EmitDelegate((System.Func<Player, bool>)((Player self) =>
        {
            
			if (BellyPlus.VisualsOnly() || !(IsStuck(self) || self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther || self.GetBelly().boostCounter > 0))
            {
                //Debug.Log("JUMP, BUT WITH A TWIST");
                return true; // self.Jump(); //JUST JUMP
            }
            else
            {
                //IN THE VERY RARE CASE THAT WE ARE CLIMBING A POLE AND ALSO STANDING ON TOP OF A PIPE ENTRANCE...
                if (IsStuck(self) && self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam && self.GetBelly().inPipeStatus == false && self.GetBelly().stuckVector.y == -1 && self.input[0].y == 0)
                {
					return true; //6-9-23 DON'T FORGER THIS
                }

                //Debug.Log("OK I THINK WE DON'T JUMP (SKIP BC WE'RE STUCK OR SOMETH ");
                return false;
            }
        }));
        cursor.Emit(OpCodes.Brfalse, label);
		//var label = cursor.DefineLabel();
		//cursor.MarkLabel(label);

		BellyPlus.Logger.LogInfo("JUMP REMOVAL IL LINES ADDED! 1 probably");
    }



	//WE NEED THE PLAYER TERRAIN RADIUS TO ACTUALLY BE REPLACED. WE CAN'T DO IT THE CHEAP WAY (THIS TIME...)
    private static void RadToTerrainRad(ILContext il)
    {
        var cursor = new ILCursor(il);

        //		+ base.bodyChunks[1].rad)
        //IL_01f4: ldarg.0
        //IL_01f5: call instance class BodyChunk[] PhysicalObject::get_bodyChunks()
        //IL_01fa: ldc.i4.1
        //IL_01fb: ldelem.ref
        //IL_01fc: ldfld float32 BodyChunk::rad
        //IL_0201: add


        //var player = 0;
        var x = 0;
        while (cursor.TryGotoNext(MoveType.After,
            //i => i.MatchLdloc(out player),
            i => i.MatchLdarg(0),
            i => i.MatchCallOrCallvirt<PhysicalObject>("get_bodyChunks"),
            i => i.MatchLdcI4(1),
            //i => i.MatchLdloc(out k),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<BodyChunk>(nameof(BodyChunk.rad))))
        {
            x++;
            cursor.Emit(OpCodes.Ldarg_0); //P SURE WE NEED THIS TO EMIT THE VALUE OF THIS (THE PLAYER) TO THE DELEGATE

            cursor.EmitDelegate((float rad, Player player) =>
            {
                return player.bodyChunks[1].TerrainRad;
            });
        }

        BellyPlus.Logger.LogInfo("BELLYPLUS RAD-TO-TERRAIN_RAD IL LINES ADDED! " + x);
    }

	
	public static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);
		//BRIEF POPCORN TANGIBILITY FOR SPEARMASTER
        if (self.processActive && !self.GamePaused && BellyPlus.popcornSpearable > 0)
			BellyPlus.popcornSpearable--;
    }
	
	
	//SOUNDS THAT DON'T GET INTERRUPTED! -they already don't get interrupted I'm stupid
	public static void PlayExternalSound(Creature self, SoundID soundId, float sVol, float sPitch)
    {
		Vector2 pos = self.mainBodyChunk.pos;
		self.room.PlaySound(soundId, pos, sVol, sPitch);
	}
	
	
	
	public static void Player_ClassMechanicsGourmand(On.Player.orig_ClassMechanicsGourmand orig, Player self)
    {
		bool preExhCheck = self.lungsExhausted;
		orig(self);
		//IF RUNNING THIS CHECK CAUSED OUR LUNGSEXHAUSTED VALUE TO CHANGE TO TRUE, UNDO THAT IF WE'RE STUCK!
		if (!BellyPlus.VisualsOnly() && preExhCheck == false && self.lungsExhausted == true && (IsStuckOrWedged(self) || ObjIsPushingOrPullingOther(self)))
			self.lungsExhausted = false;
	}
	
	public static bool ChunkyEvergreen(Player self)
	{
		return self.slugcatStats?.name?.value == "TrynsEvergreen" && GetOverstuffed(self) > 4;
	}
	
	//THESE CHARACTERS CAN SLAM EVEN IF SLUGSLAMS ARE DISABLED
	public static bool ChunkyFallOverride(Player self)
	{
		string slugName = self.slugcatStats?.name?.value;
		return (slugName == "TrynsEvergreen" || slugName == "Citrus") && ChunkyFallRequirement(self); //GetOverstuffed(self) > 4 && 
    }

    public static bool ChunkyFallRequirement(Player self)
    {
		float weightRequired = 3f + self.GetBelly().slamThreshold;
		float myWeight = GetChubValue(self) + GetOverstuffed(self);
        string slugName = self.slugcatStats?.name?.value;

        if (slugName == "TrynsEvergreen" || slugName == "Citrus")
			weightRequired += 5f;

        if (slugName == "MMWingCat" || slugName == "Outsider")
            weightRequired += 5f;

        return myWeight >= weightRequired;
    }


    public static bool CheckFriendlyFire(Player self)
	{
		bool flag2 = ModManager.CoopAvailable && Custom.rainWorld.options.friendlyFire;
		bool flag3 = self.room != null && self.room.game.IsArenaSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.spearsHitPlayers;
		return (flag2 || flag3);
	}
	
	public static bool BP_SlugSlamConditions(On.Player.orig_SlugSlamConditions orig, Player self, PhysicalObject otherObject)
    {
		if (BellyPlus.VisualsOnly() || (!BPOptions.slugSlams.Value && !ChunkyFallOverride(self)) || !ChunkyFallRequirement(self))
			return orig(self, otherObject);
		
		
		
		//REGARDLESS OF SETTINGS, NO ONE WANTS THIS. JUST REETURN FALSE
		if (!CheckFriendlyFire(self) && (otherObject is Player || (otherObject is Lizard && patch_Lizard.IsTamed(otherObject as Lizard))))
			return false;
		//THIS WOULD BE A STUPID WAY TO DIE - CHECK FOR THIS, EVEN IF THESE IDIOTS HAVE FRIENDLY FIRE ON
		//THIS DOESN'T ACTUALLY DO ANYTHING HERE >:/ MOVING DOWN TO ONCOLLIDE
		// else if (otherObject is Player && otherObject.GetBelly().rollingOther > 0)
			// return false;
		//YEA BECAUSE ROLLING RUNS REGARDLESS OF SLUGSLAM CONDITIONS
		//THIS PART WILL RETURN TRUE IF WE LET IT RUN IN ORIG, SO DON'T LET IT
		// else if (otherObject is Player && !ModManager.CoopAvailable && !CheckFriendlyFire(self))
			// return false;
		//NO WAIT, THE ABOVE PORTION SHOULD HAVE COVERED THAT!? AAAUGH I DON'T KNOW
		else
		{
			//BRIEFLY TRANSFORM INTO GOURMAND TO PERFORM A SLAM
			SlugcatStats.Name origClass = self.SlugCatClass;
			self.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
			bool origSlam = orig(self, otherObject);
			self.SlugCatClass = origClass;
			return origSlam;
		}
		
	}
	
	
	
	public static bool BP_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
    {
		//LET US EAT NEURONS IF OUR BELLY IS FULL (NAH FIRE EGGS WE DON'T CARE ABOUT I THINK) -NO WE DO! GOURMAND NEEDS THOSE FOR SILLY BOMBS
		if (!BellyPlus.VisualsOnly() && ((testObj is SSOracleSwarmer) || (testObj is FireEgg)) && (self.objectInStomach != null) && (self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear))
			return false;
		else
			return orig(self, testObj);
	}
	
	public static bool IsFoodLover()
	{
		return !BellyPlus.VisualsOnly() && (BPOptions.foodLoverPerk.Value || (ModManager.Expedition && Custom.rainWorld.ExpeditionMode && Expedition.ExpeditionGame.activeUnlocks.Contains("unl-foodlover")));
	}
	
	public static int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
	{
		//MOVED THE REST OF THIS HOOK TO SOMEHERE ELSE, TO FIX SLUGBASE COMPAT

		int result = orig.Invoke(slugcatIndex, eatenobject);
		
		if (BellyPlus.VisualsOnly() || result == -1)
			return result;
		
		//MAKE FAT NOODLEFLIES GIVE EXTRA
		if (eatenobject is SmallNeedleWorm && (eatenobject as SmallNeedleWorm).graphicsModule != null)
        {
			float fatness = (eatenobject as SmallNeedleWorm).bodyChunks[0].mass;
			// Debug.Log("NOODLEFLY THICKNESS! " + fatness + " RES: " + result);
			if (fatness > 0.06f) //(0.15f * 0.7f)
				result *= 2;
		}
        if (eatenobject is Centipede && patch_DLL.GetChub(eatenobject as Creature) >= 4) //&& (eatenobject as Centipede).Small 
        {
            result *= 2;
        }
		
		//THESE ARE WORTH FULL ON THE COB. I'M MAKING THEM FULL FROM THE GROUND TOO
		if (ModManager.MSC && eatenobject is SlimeMold && (eatenobject as PhysicalObject).abstractPhysicalObject.type == MoreSlugcatsEnums.AbstractObjectType.Seed)
        {
            result = 4; //ALWAYS A FULL FOOD PIP
        }
        return result;
	}


    
    

	//THERE ARE A NUMBER OF OTHER FACTORS INVOLVED TAT MAKE THIS TRICKY. SO RATHER THAN RETURN TRUE WHEN NEEDED, WE'LL PRETEND WE'RE HUNTER WHEN NEEDED
	private static bool BPPlayer_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
	{
		if (IsFoodLover())
		{
			SlugcatStats.Name origClass = self.SlugCatClass;
            SlugcatStats.Name origName = self.slugcatStats.name;
            self.SlugCatClass = SlugcatStats.Name.Red;
            self.slugcatStats.name = SlugcatStats.Name.Red;
            bool result = orig(self, crit);
			self.SlugCatClass = origClass;
			self.slugcatStats.name = origName;
            return result;
		}
        return orig(self, crit);
	}
	
	
	
	public static void BP_JollyUpdate(On.Player.orig_JollyUpdate orig, Player self, bool eu)
	{
        orig.Invoke(self, eu);
		if (BellyPlus.VisualsOnly())
			return;

        //IF WE'D NORMALLY SKIP THIS, RUN IT INSTEAD
        if (!ModManager.CoopAvailable && !self.isNPC && self.room != null)
		{
			self.JollyInputUpdate();	
			self.JollyPointUpdate();
			if (self.jollyButtonDown && self.room.game.cameras[0].hud != null && self.room.game.cameras[0].hud.map != null)
			{
                // self.input[0].mp = false; //self.standStillOnMapButton = true;
                // self.input[1].mp = false; //self.standStillOnMapButton = true;
                self.room.game.cameras[0].hud.map.fadeCounter = 0; //.fade
           }
		}
        //else
        //orig.Invoke(self, eu);

        

        //ALLOW US TO FORCE SLEEP WITHOUT LEAVING IF WE REALLY WANT TO
        if (self.stillInStartShelter && !self.isNPC && self.room != null && self.room.game.IsStorySession && self.FoodInRoom(self.room, false) >= (self.abstractCreature.world.game.GetStorySession.saveState.malnourished ? self.slugcatStats.maxFood : self.slugcatStats.foodToHibernate))
		{
			Player.InputPackage i = self.input[0];
			bool y = i.y < 0 && !i.jmp; // || i.y == 0 && self.input.Take(10).All(inp => inp.jmp);
			bool x = i.x == 0 || self.IsTileSolid(0, i.x, 0) && (!self.IsTileSolid(0, -1, 0) || !self.IsTileSolid(0, 1, 0));
			
			if (self.GetBelly().pushingOther <= 0 && !self.GetBelly().isStuck)
			{
				
				if (self.GetBelly().bpForceSleep >= 90)
				{
					//OKAY DON'T USE FORCESLEEP THAT WILL PROBABLY STARVE US
					//BUT APPLY THAT TO ALL PLAYERS IN THE SHELTER
					for (int j = 0; j < self.room.game.Players.Count; j++)
					{
						if (self.room.game.Players[j].realizedCreature != null && self.room.game.Players[j].realizedCreature is Player player)
						{
                            player.touchedNoInputCounter = 60; //THIS SHOULD TRIGGER THE SHELTER DOOR CLOSE BEFORE IT RECOGNIZES OUR INPUTS HAVE CHANGED
                            player.stillInStartShelter = false;
                            player.GetBelly().bpForceSleep = 91;

                        }
					}
				}

                if (x && y)
                    self.GetBelly().bpForceSleep++;
				else
                    self.GetBelly().bpForceSleep = 0;
            }
			else
				self.GetBelly().bpForceSleep = 0;
		}
		
		
		//IF WE'RE BEING frFed!?
		if (self.GetBelly().frFed)
		{
			for (int k = 0; k < 2; k++)
			{
				self.input[k].pckp = true;
				self.input[k].thrw = false;
				self.input[k].x = 0;
				self.input[k].y = 0;
			}
			//THEN TURN IT OFF TO ENSURE OUR FEEDER HAS TO CONTINUE HOLDING IT DOWN FOR NEXT TICK
			// self.GetBelly().frFed = false;
		}
		
		
		
		//FORCE ROLLS TO BE MORE COOPERATIVE!
		if (self.GetBelly().beingRolled > 0 && self.animation == Player.AnimationIndex.Roll)
		{
			self.input[0].x = self.rollDirection;
			self.input[1].x = self.rollDirection;
		}


        //BULLY A CHUNKY OUTSIDER >:3c
        if (self.slugcatStats?.name?.value == "Outsider")
        {
            //Debug.Log("IM AN OUTSIDER!! ");
            //if (self.gravity < 0.9f && self.gravity < self.room.gravity)
            if (self.airFriction < 0.999f) //THIS MEANS WE'RE FLYING
            {
                float reduction = Mathf.Pow(self.GetBelly().runSpeedMod, 1.5f);
				Player backPlayer = GetHeaviestPlayerOnBack(self);
				if (backPlayer != null)
					reduction = Mathf.Min(reduction, Mathf.Pow(backPlayer.GetBelly().runSpeedMod, 1.5f));

                if (self.gravity < self.room.gravity)
                    self.gravity = Mathf.Lerp(self.room.gravity, self.gravity, reduction);

                self.bodyChunks[0].vel.x *= reduction;
                self.bodyChunks[1].vel.x *= reduction;
                float falling = Mathf.Pow(self.GetBelly().runSpeedMod, 4);
                self.bodyChunks[0].vel.y = Mathf.Lerp(fallspeed1, self.bodyChunks[0].vel.y, falling);
                self.bodyChunks[1].vel.y = Mathf.Lerp(fallspeed2, self.bodyChunks[1].vel.y, falling);

                if (self.GetBelly().runSpeedMod <= 0.9)
                {
                    MakeStrainSparks(self, 2);
                    if (!self.lungsExhausted)
                        self.GetBelly().corridorExhaustion += 3;
                    else
                    {

                    }

                }
            }
        }
		
		//WINGCAT GETS HANDLED A BIT DIFFERENTLY
		if (self.slugcatStats?.name?.value == "MMWingCat")
			WingcatUpdate(self, eu);
    }

    public static float fallspeed1 = 0;
    public static float fallspeed2 = 0;


    public static void BPPlayer_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
    {
		//MORE OUTSIDER BULLYING
        if (self.slugcatStats?.name?.value == "Outsider" && !BellyPlus.VisualsOnly())
		{
            fallspeed1 = self.bodyChunks[0].vel.y;
            fallspeed2 = self.bodyChunks[1].vel.y;
			if (self.lungsExhausted && self.airFriction < 0.999f && self.Submersion == 0)
			{
                self.Stun(2);
                self.input[0].y = -1;
                self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1f, 0.6f);
            }
        }

        orig.Invoke(self);
    }
	
	
	public static void WingcatUpdate(Player self, bool eu)
	{

        MMWingCat.WingSlugcatClass.WingSlugcat cat = MMWingCat.WingSlugcatClass.GetCat(self);
		if (cat.flightmode)
		{
			Player fattest = GetHeaviestOnStack(self, true);
			float runMod = fattest.GetBelly().runSpeedMod - Mathf.Max((GetChubFloatValue(fattest) / 40f), 0);
            float reduction = Mathf.Pow(runMod, 1.5f);
			
			for (int i = 0; i < self.bodyChunks.Length; i++)
			{
                //self.bodyChunks[i].vel.x *= reduction;
                self.bodyChunks[i].vel.x *= runMod;
				//LOSE ALTITUDE
				if (self.bodyChunks[i].vel.y > 0)
					self.bodyChunks[i].vel.y *= runMod * Mathf.Min((1.5f - GetExhaustionMod(self, 60)), 1f);
                //if (self.bodyChunks[i].vel.y >= -2)
                //    self.bodyChunks[i].vel.y -= Mathf.Lerp(5.5f, 0f, reduction);
			}
			
			//DRAIN STAMINA IF WE'RE STRUGGLING
			if (runMod <= 0.9)
			{
				if (self.input[0].jmp)
					MakeStrainSparks(self, 2);
				if (!self.lungsExhausted)
					self.GetBelly().corridorExhaustion += ((eu && runMod > 0.5) || !self.input[0].jmp) ? 1 : 2;
			}

            if (self.lungsExhausted && self.Submersion == 0)
            {
                self.Stun(2);
                self.input[0].y = 0;
                self.input[0].jmp = false;
                cat.flightmode = false;
                self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1f, 0.6f);
            }
        }
		else if (self.lungsExhausted && self.Submersion == 0)
		{
            cat.flightmode = false;
            cat.hovermode = false;
            
			if (self.bodyChunks[1].ContactPoint.y != -1)
			{
                //self.input[0].y = 0; //WE CAN'T DO THAT, WE'LL FALL PAST POLES
                self.input[0].jmp = false;
                MakeStrainSparks(self, 1);
			}
        }
    }


    public static void BP_TriggerCameraSwitch(On.Player.orig_TriggerCameraSwitch orig, Player self)
    {
        try //OKAY I DON'T ACTUALLY KNOW HOW TO FIX THIS SO WE'RE JUST GOING TO BE LAZY AND CATCH IT BECAUSE WE DON'T EVEN NEED IT TO RUN ANYWAYS
        {
            orig.Invoke(self);
        }
        catch
        {
            Debug.Log("BP - CAMERA TRIGGER FAILED TO SWITCH ");
        }
    }
	
	
	
	public static IntVector2 GetCreatureVector(Creature self)
	{
		Vector2 myVec = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
		//int xVec = Math.Max(Mathf.FloorToInt(myVec.x * 1.5f), -1);
		int xVec = Mathf.RoundToInt(myVec.x);
		int yVec = Mathf.RoundToInt(myVec.y);
		
		IntVector2 vector = new IntVector2(xVec, yVec);
		//Debug.Log("CREATURE VECTOR:" + myVec.x + " : " + Mathf.FloorToInt(myVec.x * 1.5f));
		return vector;
	}
	
	
	public static float GetAxisMagnitude(Creature self)
	{
		Vector2 myVec = GetCreatureVector(self).ToVector2();
		//WE ALSO SUBTRACT GRAVITY IF OUR Y VALUE IS CHOSEN, I THINK
		float xMag = Math.Abs(self.bodyChunks[1].vel.x * myVec.x);
		float yMag = Math.Abs((self.bodyChunks[1].vel.y - self.gravity) * myVec.y);
		//Debug.Log("MY MAGNITUDE:" + xMag + " -Y:" + yMag);
		return Mathf.Max(xMag, yMag);
	}


	public static bool IsBackwardsStuck(Creature self)
	{
		Vector2 myVec = GetCreatureVector(self).ToVector2();
		Vector2 stuckVec = self.GetBelly().stuckVector; //i'M SURE ILL USE THIS ON CREATURES EVENTUALY...
		if (stuckVec.x != 0 && myVec.x == -stuckVec.x)
			return true;
		else if (stuckVec.y != 0 && myVec.y == -stuckVec.y)
			return true;
		else
			return false;
	}


	public static float CheckWedge(Creature self, bool rigged)
    {
		//f(x)= sin(2x/pi) : y dips to -1 every 10 x units for 5 blocks
		//f(x)= sin(x/pi) + 0.5 : y dips to -0.5 every 10 x units for 3.~ blocks
		//f(x)= -|cos(x/pi)| + 0.5 : y dips to -0.5 every 10 x units for 3.~ blocks

		if (self.room == null || self.room.aimap == null)
			return 1f; //NOT READY YET
		
		//THIS SEED WILL HAVE SEPERATE X AND Y VECTORS
		IntVector2 myTilePos = self.room.GetTilePosition(self.bodyChunks[1].pos);
		IntVector2 myVector = GetCreatureVector(self); //.ToVector2();
		
		//float tilePos = self.room.GetTilePosition(self.bodyChunks[1].pos).x;
		float tilePos = 0f;
		float tileSeed = 0f;
		float mySpeed = 0f;
		bool coinFlip = false;
		if (myVector.x != 0)
		{
			tilePos = self.bodyChunks[1].pos.x / 20f;
			//tileSeed = (myTilePos.y % 4) - 2; //THIS SHOULD THEORETICALLY GIVE US A VALUE (-2, -1, 0, 1)
			//tileSeed = (myTilePos.y % 4) - 1; //AND THIS SHOULD GIVE US  (-1, 0, 1, 2)
			tileSeed = (myTilePos.y % 3) - 1; //AND THIS SHOULD GIVE US  (-1, 0, 1)
			coinFlip = ((myTilePos.y % 2) == 0);
			mySpeed = Mathf.Abs(self.bodyChunks[1].vel.x);
		}
		else if (myVector.y != 0)
		{
			tilePos = self.bodyChunks[1].pos.y / 20f;
			tileSeed = (myTilePos.x % 3) - 1;
			coinFlip = ((myTilePos.x % 2) == 0);
			mySpeed = Mathf.Abs(self.bodyChunks[1].vel.y);
		}
        //tileSeed = -1;

		if (rigged) //FOR TUTORIAL LEVEL SHENANIGANS
        {
			tileSeed = -0.5f;
			coinFlip = true;
		}

		//EXTRA CHONKY FELLAS GET EVEN SLOWER
		if (self is Player)
        {
			tileSeed -= ((Math.Min(GetOverstuffed(self as Player), 12) - self.shortcutDelay) * 0.2f) * (1 + (Mathf.Min(BPOptions.bpDifficulty.Value, 0) / 10f)); //LET EASY MODE PLAYERS GET UP MUCH EASIER ;
			//DON'T TRAP PLAYERS BEHIND US FROM LEAVING PIPES WE'RE BLOCKING
			tileSeed += ((self.GetBelly().fwumpFlag) * 0.5f) + (self.shortcutDelay * 0.2f);
		}


		//RETURN EARLY IF TILE SEED ISN'T NEGATIVE, SINCE THIS WON'T SQUEEZE US AT ALL
		if (tileSeed >= 0)
			return 1f;

		float lengthMod = 0;
		for (int i = -5; i <= 6; i++)
		{
			Vector2 pos = (self.bodyChunks[1].pos + (myVector.ToVector2() * 20 * i));
			//Debug.Log("NARROW SPACE?:" + i + " POS:" + pos + " TERR:" + self.room.GetTile(self.room.GetTilePosition((self.bodyChunks[1].pos + (myVector * 20 * i)))).Terrain);
			if (self.room.aimap.getAItile(pos).narrowSpace == false || self.IsTileSolid(1, (myVector * i).x, (myVector * i).y))
				lengthMod+= 0.05f;
			//ALSO CHECK FOR OPENINGS AT OUR SIDES
			if (Math.Abs(i) <= 1)
            {
				Vector2 parralelAx = new Vector2(Math.Abs(myVector.ToVector2().y * 20), Mathf.Abs(myVector.ToVector2().x * 20)); //ILL GET TO THIS LATER
				//Debug.Log("NARROW SPACE?:" + ((myVector * i).x + Mathf.Abs(myVector.y)) + ":" + ((myVector * i).y + Mathf.Abs(myVector.x)) + " SOLID?" + self.IsTileSolid(1, (myVector * i).x + Mathf.Abs(myVector.y), (myVector * i).y + Mathf.Abs(myVector.x)));
				//Debug.Log("NARROW SPACE2?:" + ((myVector * i).x - Mathf.Abs(myVector.y)) + ":" + ((myVector * i).y - Mathf.Abs(myVector.x)) + " SOLID?" + self.room.aimap.getAItile(pos - parralelAx).narrowSpace + " - " +  self.IsTileSolid(1, (myVector * i).x - Mathf.Abs(myVector.y), (myVector * i).y - Mathf.Abs(myVector.x)));
				//IMPORTANT TO REMEMBER!! ALL SOLID TILES ARE ALSO NARROW SPACES (OR AT LEAST MOST OF THEM)
				//if (self.room.aimap.getAItile(pos + parralelAx).narrowSpace == true && !self.IsTileSolid(1, (myVector * i).x + Mathf.Abs(myVector.y), (myVector * i).y + Mathf.Abs(myVector.x)))
				if (!self.IsTileSolid(1, (myVector * i).x + Mathf.Abs(myVector.y), (myVector * i).y + Mathf.Abs(myVector.x)))
					lengthMod += 0.15f;
				if (!self.IsTileSolid(1, (myVector * i).x - Mathf.Abs(myVector.y), (myVector * i).y - Mathf.Abs(myVector.x)))
					lengthMod+= 0.15f;
			}
		}
		
		//THESE SHORTCUT WEDGES ARE DRIVING ME CRAZY. QUIT IT!
		//IntVector2 behind = (self.bodyChunks[1].pos + (myVector * -20));
		//if (self.IsTileSolid(1, behind.x, behind.y))
		//	lengthMod+= 0.25f;
			
		
		lengthMod += 0.05f * self.shortcutDelay; 
		//lengthMod = 0;

		//float randomSize = 1f + (tileSeed / 4f); //PRODUCE A VALUE BETWEEN 0.5 AND 1.0
		float randomSize = 1f + (tileSeed / 2f); //ACTUALLY, FORGET THE INBETWEEN VALUES. LETS JUST DO ALL OR NOTHING
		//float wedgeSlow = Mathf.Min(-Mathf.Abs(Mathf.Cos(tilePos / 3.1415f)) + randomSize + self.GetBelly().loosenProg + lengthMod, 0f);
		float wedgeSlow = Mathf.Min(-Mathf.Cos(tilePos / 3.1415f) * (coinFlip ? -1f : 1f) + randomSize + ObjGetLoosenProg(self) + lengthMod, 0f);

		//WAIT... I THINK THESE ARE ALWAYS 0 IN PIPES ANYWAYS.
		//float myChub = Mathf.Min(ObjGetChubValue(self) + 0.5f, 4) - (ObjIsSlick(self) ? 0.5f : 0f); //SLIGHT BOOST TO CHUBSTERS
		float myChub = ObjGetChubValue(self) - (ObjIsSlick(self) ? 0.5f : 0f) - (self.Submersion >= 0.9f ? 1.5f : 0f); //AND THEN CUTTING BACK
		float velReduce = (Mathf.Max(myChub / 1.8f, 0) * -wedgeSlow);
		float velMult = Mathf.Max(0, 1f - (velReduce));
		//WE STILL DO THIS TO HALT ANY MOVEMENT
		for (int i = 0; i < self.bodyChunks.Length; i++)
		{
			self.bodyChunks[i].vel *= velMult;
		}

		//THIS NEEDS TO BE MOVED TO AFTER WE CALCULATE THE VELOCITY OF OUR INPUT MOVEMENT, OTHERWISE WE STAY WEDGED WHEN WE STOP.
		// if (mySpeed < 0.3f) //DON'T MOVE AT ALL IF WE AINT REALLY MOVING //self.bodyChunks[1].vel.magnitude
		// {
		// self.bodyChunks[0].vel *= 0f;
		// self.bodyChunks[1].vel *= 0f;
		// velMult = 0f;
		// }

		if (self is Player && velReduce > 0.3f) //NO BENIFITS FROM CORRIDOR BOOSTING IF WEDGED TOO TIGHT
        {
			(self as Player).horizontalCorridorSlideCounter = Math.Min((self as Player).horizontalCorridorSlideCounter, 1);
			(self as Player).verticalCorridorSlideCounter = Math.Min((self as Player).verticalCorridorSlideCounter, 1);
		}

		//if (self.graphicsModule != null)
		//	Debug.Log("WEDGEMULT:" + velMult + " -WEDGESTRAIN:" + velReduce + " -MYVECTOR:" + myVector + " -lengthMod:" + lengthMod + " -VEL:" + self.bodyChunks[1].vel + " SIZE:" + tileSeed + " MAGNITUDE:" + self.bodyChunks[1].vel.magnitude + " SPEED:" + mySpeed);
		
		self.GetBelly().wedgeStrain = Mathf.Min(velReduce, 0.55f);

		return velMult;
	}
	
	
	public static bool IsWedged(Player self)
    {
		return (self.GetBelly().inPipeStatus && self.GetBelly().wedgeStrain > 0.3f);
	}
	
	public static bool IsStuckOrWedged(Creature self)
    {
		return (ObjIsStuck(self) || ObjIsWedged(self));
	}
	
	
	//THIS THING SUCKS! LETS BUILD OUR OWN
	public static bool IsTileNarrowFloat(Creature self, int bChunk, float relativeX, float relativeY)
	{
		Vector2 myNewChunkPos = self.bodyChunks[bChunk].pos + new Vector2(relativeX * 20, relativeY * 20);
		if (self.room != null)
		{
			bool isTight = false;
			IntVector2 tilePos = self.room.GetTilePosition(myNewChunkPos);
			Room.Tile.TerrainType terrain = self.room.GetTile(tilePos).Terrain;
			// Debug.Log("TERRAIN: " + terrain + " SHORTCUT DATA: " + self.room.GetTile(tilePos).shortCut);

			//GODDANG THIS IS COMPLICATED... OK IF IT'S A FLOOR (OR AIR), CHECK THE TILE ON THE OTHER SIDE, BECAUSE IT MAY BE AN INVALID SHORTCUT ENTRANCE TYPE TO PLAYERS
			if (self is Player && (terrain == Room.Tile.TerrainType.Floor || terrain == Room.Tile.TerrainType.Air))
            {
				IntVector2 shortCheckPos = tilePos;
				
				if (relativeY < 0 && relativeX == 0 && terrain == Room.Tile.TerrainType.Floor)
					shortCheckPos += new IntVector2(0, -1);
				else if (relativeY == 0 && relativeX != 0 && terrain == Room.Tile.TerrainType.Air)
					shortCheckPos += new IntVector2((relativeX > 0) ? 1 : -1 , 0);
				//IF IT'S ABOVE US, WE DON'T CARE. THE BARRIER BLOCKS OUR HEAD FROM GETTING UP THERE.
				
				if (self.room.GetTile(shortCheckPos).shortCut > 0)
                {
					Room.Tile.TerrainType deepTerrain = self.room.GetTile(shortCheckPos).Terrain;
					//Debug.Log("SUPER SECRET SHORTCUT DATA: " + deepTerrain);
                    if (deepTerrain == Room.Tile.TerrainType.ShortcutEntrance)
                    {
						ShortcutData.Type deepShortCutType = self.room.shortcutData(shortCheckPos).shortCutType;
						if ((deepShortCutType != ShortcutData.Type.RoomExit && deepShortCutType != ShortcutData.Type.Normal))
							return false; //PLAYERS CAN ONLY ENTER THESE TWO TYPES OF SHORTCUTS
					}
				}
			}

			//bool log2 = false;
			if (terrain == Room.Tile.TerrainType.ShortcutEntrance)
			//if (self.room.shortcutData(tilePos).shortCutType != null)
			{
				ShortcutData.Type shortCutType = self.room.shortcutData(tilePos).shortCutType;
				if (shortCutType == ShortcutData.Type.DeadEnd)
					isTight = false;
				else if (self is Player && (shortCutType != ShortcutData.Type.RoomExit && shortCutType != ShortcutData.Type.Normal))
					isTight = false; //PLAYERS CAN ONLY ENTER THESE TWO TYPES OF SHORTCUTS
				else
					isTight = true; //TREATED AS A CORRIDOR TILE
			}
			
			else if (terrain != Room.Tile.TerrainType.Air && terrain != Room.Tile.TerrainType.Floor)
			{
                //isTight = false; //IF IT'S SOLID, WE AINT CRAWLING THROUGH IT - !FLOORS AREN'T ALWAYS SOLID!
                //4/30/23 OKAY THIS DOESN'T SEEM TO WORK SPECIFICALLY WHEN BACKING INTO A WALL INSIDE A PIPE. MAYBE THIS COULD WORK?...
                isTight = PipeStatus(self);
            }
				
			else
			{
				//SHOULD RETURN TRUE IF THERE ARE SOLID TILES ON OPPOSITE SIDES, OR INTERSECTING AT LEAST TWO OTHER CORRIDORS
				//SOLID TILE LOCATIONS VIA NUMPAD CONFIGURATION
				bool s7 = self.room.GetTile(tilePos.x - 1, tilePos.y + 1).Terrain == Room.Tile.TerrainType.Solid;
				bool s8 = self.room.GetTile(tilePos.x + 0, tilePos.y + 1).Terrain == Room.Tile.TerrainType.Solid;
				bool s9 = self.room.GetTile(tilePos.x + 1, tilePos.y + 1).Terrain == Room.Tile.TerrainType.Solid;
				bool s4 = self.room.GetTile(tilePos.x - 1, tilePos.y + 0).Terrain == Room.Tile.TerrainType.Solid;
				bool s6 = self.room.GetTile(tilePos.x + 1, tilePos.y + 0).Terrain == Room.Tile.TerrainType.Solid;
				bool s1 = self.room.GetTile(tilePos.x - 1, tilePos.y - 1).Terrain == Room.Tile.TerrainType.Solid;
				bool s2 = self.room.GetTile(tilePos.x + 0, tilePos.y - 1).Terrain == Room.Tile.TerrainType.Solid;
				bool s3 = self.room.GetTile(tilePos.x + 1, tilePos.y - 1).Terrain == Room.Tile.TerrainType.Solid;
				
				//SOLID BLOCKS ON TWO OPPOSING SIDES
				if ((s4 && s6) || (s2 && s8))
				{
					isTight = true;
					
					//HEY, THOSE SINGLE TILE GAPS UNDER THE SHADED CITADEL BALLS ARE NOT THINGS TO GET STUCK IN
					if (!PipeStatus(self) && (s2 && s8) && (s4 || s6))
						isTight = false;
				}
				else
				{
					//CHECK THE 4 MAIN DIRECTIONS FOR ADJACENT CORRIDORS. IF AT LEAST 2, RETURN TRUE
					int adjCorCount = 0;
					if (s7 && !s8 && s9)
						adjCorCount++;
					if (s9 && !s6 && s3)
						adjCorCount++;
					if (s7 && !s4 && s1)
						adjCorCount++;
					if (s1 && !s2 && s3)
						adjCorCount++;
					
					if (adjCorCount >= 2)
						isTight = true;
					
					//SIGH... OKAY PART 2. IF WE'RE INTERSECTING TWO CORRIDOR BLOCKS, IT ISN'T ACTUALLY TIGHT IF THERE IS ALSO A 90 DEGREE CORNER OF OPEN AIR.
					if (adjCorCount == 2)
					{
						if ((!s4 && !s7 && !s8) 
							|| (!s8 && !s9 && !s6)
							|| (!s6 && !s3 && !s2)
							|| (!s2 && !s1 && !s4)
						)
							isTight = false;
					}

                    //Debug.Log("PATH FINDER:" + bChunk + " X/Y:" + relativeX + "/" + relativeY + " TIGHT: " + isTight + " RECHEK " + (s1 && (s6 && (s7 || s8))));
                    //I HAVE A BETTER SOLUTION THAT DOESN'T INVOLVE CHECKING OUTSIDE THE BOUNDARIES
                    if (isTight == false)
					{
						
                        if (s3)
                        {
                            if (s4 && (s8 || s9))
                                return true;
                            else if (s8 && (s1 || s4))
                                return true;
                        }
                        if (s7)
                        {
                            if (s6 && (s1 || s2))
                                return true;
                            else if (s2 && (s6 || s9))
                                return true;
                        }
						//TWO OPPOSING CORNERS... THAT SHOULD... PROBABLY COVER IT ALL, RIGHT?
						//if (!isTight && (s1 || s9))
						
                        if (s1)
                        {
                            if (s6 && (s7 || s8))
                                return true;
                            else if (s8 && (s6 || s3))
                                return true;
                        }
                        if (s9)
                        {
                            if (s4 && (s2 || s3))
                                return true;
                            else if (s2 && (s4 || s7))
                                return true;
                        }
                        
					}
					//WAIT... MAYBE THIS WAS ALL WE NEEDED... LOOK INTO THAT LATER
				}
				//log2 = true;
					//Debug.Log("CHUNK:" + bChunk + " X/Y:" + relativeX + "/" + relativeY + " S1: " + s1 + " S2:" + s2 + " S3:" + s3 + " S4:" + s4 + " S6:" + s6 + " S7:" + s7 + " S8:" + s8 + " S9:" + s9 + " TIGHT: " + isTight);
			}

			//if (!log2)
			//	Debug.Log("CHUNK:" + bChunk + " X/Y:" + relativeX + "/" + relativeY + "NON STANDARD RETURN       -  " + isTight);

            return isTight;
		}
		else
			return false; //ROOM HASN'T LOADED YET. JUST RETURN FALSE
	}
	
	
	

	public static float GetTileSizeMod(Creature self, Vector2 posMod, IntVector2 inputVec, int sizeMod, bool inPipe, bool submerged, bool showLogs)
	{
		IntVector2 myTilePos = self.room.GetTilePosition(self.bodyChunks[1].pos + new Vector2(posMod.x * 20, posMod.y * 20));
		float tileSeed = (myTilePos.x + myTilePos.y) + seedKey;
		float tileSizeMod = sizeMod;
		//Debug.Log("-----TILE SEED!: " + myTilePos.x + "+" + myTilePos.y + " = "+ tileSeed);
		bool coinFlip = (tileSeed%4 == 0); //25% CHANCE TO BE TRUE ON ANY GIVEN TILE

		while (tileSeed > 8)
		{
			tileSeed -= 9;
		}
		//tileSeed -= 4; //SO THE RANGE IS AN EVEN SPREAD OF POSITIVES AND NEGATIVES
		
		if (!BPOptions.debugLogs.Value)
			showLogs = false; 

		//CHECK FOR SHORTCUT TYPE. WE NEED TO CHECK BOTH ENDS I GUESS.
		bool shortcutFlag = false;
		IntVector2 myShortcutTile = new IntVector2(0, 0);
		//HIPS INTO SHORTCUT
		if (self.room.GetTile(self.room.GetTilePosition(self.bodyChunks[1].pos) + inputVec).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
		{
			myShortcutTile = self.room.GetTilePosition(self.bodyChunks[1].pos) + inputVec;
			shortcutFlag = true;
		}
		//HIPS OUT OF SHORTCUT
		else if (self.room.GetTile(self.room.GetTilePosition(self.bodyChunks[1].pos) - inputVec).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
		{
			myShortcutTile = self.room.GetTilePosition(self.bodyChunks[1].pos) - inputVec;
			shortcutFlag = true;
		}
		//HEAD INTO SHORTCUT
		else if (self.room.GetTile(self.room.GetTilePosition(self.bodyChunks[0].pos) + inputVec).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
		{
			myShortcutTile = self.room.GetTilePosition(self.bodyChunks[0].pos) + inputVec;
			shortcutFlag = true;
		}
		//IF IT'S A SHORTCUT, FIND OUT WHAT TYPE IT IS
		if (shortcutFlag)
		{
			ShortcutData.Type shortCutType = self.room.shortcutData(myShortcutTile).shortCutType;
			if (shortCutType == ShortcutData.Type.Normal)
			{
				tileSizeMod = -4;
			}
			else if (shortCutType == ShortcutData.Type.RoomExit)
			{
				tileSizeMod = -2;
			}
			// else if (self is Player)
				// tileSizeMod = -20; //PLAYERS IGNORE OTHER SHORTCUT TYPES (but this doesnt work)
			
			// MAYBE WE COULD BE LAAYZEEE (SO THEY CAN'T GET SUCKED INTO SHORTCUTS WHILE STILL STUCK
			if (self is Player && !inPipe)
			{
				self.shortcutDelay = 4;
				(self as Player).gourmandAttackNegateTime = Math.Max(3, (self as Player).gourmandAttackNegateTime); //PHYSICS IS WEIRD. THIS IS NESSESARY APPARENTLY
			}
				

			//OKAY HOLD UP. IF WE'RE PUSHED AGAINST A SHORTCUT ENTRANCE, BUT INPIPESTATUS IS TRUE... WE SHOULDN'T GET STUCK
			if (self is Player && inPipe && inputVec == self.room.ShorcutEntranceHoleDirection(myShortcutTile) * -1)
				tileSizeMod = -20; //MAYBE THIS TIME IT'LL WORK


			if (showLogs)
				Debug.Log("----SHORTCUT TYPE!: " + shortCutType + " MOD:" + tileSizeMod);
        }

		bool zeroG = (self.EffectiveRoomGravity == 0f);
		float myChub = Math.Max(ObjGetChubValue(self), 0);

		//CHECK FOR THE OPPOSITE, SHEER CLIFFS ON EITHER END OF US. THOSE ARE NO FUN.
		int xFlip = ObjGetXFlipDirection(self);
		bool rearInaccessible = false;
		bool sheerDropAhead = false;
		bool predictVertStuck = showLogs && inputVec.y != 0; //FOR THE DEBUG OUTPUT, BECAUSE THE FIRST FRAME OF STUCKS DOESN'T ACTUALLY GIVE US VERTICALSTUCK
		bool verticalStuck = ObjIsVerticalStuck(self) || predictVertStuck;
		if (self is Player && !inPipe && !verticalStuck && !self.IsTileSolid(1, -xFlip, -1) && !self.IsTileSolid(1, -xFlip, -2) && !self.IsTileSolid(1, -xFlip, -3))
		{
			if (!self.room.GetTile(self.bodyChunks[1].pos + new Vector2(-xFlip * 30, 0)).AnyBeam) //3 EMPTY TILES BEHIND US. ITS PROBABLY UNREACHABLE
			{
				rearInaccessible = true;
				if (showLogs)
					Debug.Log("--REAR INACCESSIBLE ");
			}
		}
		//POPPING OUT INTO A SHEER DROP
		else if (self is Player && inPipe && !verticalStuck)
		{
			if (!self.IsTileSolid(1, xFlip*3, -1) && !self.IsTileSolid(1, xFlip * 3, -3) && !self.IsTileSolid(1, xFlip * 3, -6) && !self.IsTileSolid(1, xFlip * 3, -10) && !self.room.GetTile(self.bodyChunks[0].pos + new Vector2(xFlip * 20, 0)).AnyBeam) 
			{
				sheerDropAhead = true;
				if (showLogs)
					Debug.Log("--SHEER DROP AHEAD ");
			}
		}
		
		//REDUCE TIGHTNESS BECAUSE IT'S LESS FUN WHEN THEY WE CAN'T BE ACCESSED
		if (rearInaccessible || sheerDropAhead)
			tileSizeMod -= 6;


		//IF WE'VE GOT A FUN SHOVING PLATFORM, MAKE ROOM FOR IT!
		bool rearPlatform = !inPipe && !self.IsTileSolid(1, -xFlip * 2, -1) && self.IsTileSolid(1, -xFlip * 2, -2);
		if (rearPlatform && !verticalStuck && !zeroG)
			tileSizeMod += 3;


		//MAKE TINY SHELTERS JUST A BIT MORE SNUG!
		if (self.room.abstractRoom.shelter) //&& !inPipe
		{
			tileSizeMod += 2;  //YOU KNOW WHAT... MAKE ALL SHELTERS A BIT MORE SNUG!~
			Vector2 myStuckVec = ObjGetStuckVector(self) * (inPipe ? 3 : -3);
			if (self.IsTileSolid(1, (int)myStuckVec.x, (int)myStuckVec.y))
			{
				if (tileSeed < 5) //TINY SHELTERS ARE NOT ALLOWED TO HAVE WIDE PIPES
					tileSeed += 4; //:saintsmirk:
				tileSizeMod += 4;
				if (showLogs)
					Debug.Log("--TINY SHELTER MODIFIER ");
			}
		}
		

		//IF WET, -1 ON TOP OF EXISTING MODS
		bool wetHazard = submerged || self.rainDeath > 0;
		if (wetHazard)
			tileSizeMod -= 6;
		
		//RAIN IS COMING!
		bool rainWarning = false;
		if (self.room.world.rainCycle.TimeUntilRain < 800 && !(self is Player && (self as Player).stillInStartShelter || zeroG))
			rainWarning = true;


		
		if (self is Player)
        {
			//ROOM SPECIFIC!
			string myRoom = self.room.roomSettings.name.ToString();
			if (myRoom == "SS_AI" || myRoom == "DM_AI")
			{
				tileSizeMod += 14;
				rainWarning = false;
			}
			else if (myRoom == "SU_A44" || myRoom == "SL_C16") //TUTORIAL BERRY ROOM AND THAT PEBBLES CRACKED ROOM
            {
				tileSizeMod += 8 + myChub;
			}
			else if (myRoom =="SH_C12") //SHADED SQUEEZE  -IDEK WHAT THIS ONE WAS || myRoom == "CL_B15"
            {
				tileSizeMod += 6;
			}
			//else if ("SU_A41")


			//IF WE'VE GOT FRIENDS... LETS MAKE IT MORE INTERESTING~
			if (GetLivingPlayers(self as Player) > 1 && !wetHazard && zeroG == false && !expRotund) //  || !(self is Player) IDK WHY THIS WAS HERE
			{
				tileSizeMod += 1 + Math.Min(GetLivingPlayers(self), 4); //WE SHOULD CAP AT 4. MORE THAN THIS IS A BIT MUCH
				if (showLogs)
					Debug.Log("--PLAYER MODS! " + (1 + GetLivingPlayers(self)));
			}
		}
		
		

		
		


		//NEW DYNAMIC SUPER TIGHT!~ RARE OCCURENCES. ONLY 50% OF SIZE 7'S & 8'S, AND ONLY IF THEY'RE NON SHORTCUTS.
		if (tileSeed >= 7 && !shortcutFlag && coinFlip && !wetHazard && !rainWarning)
        {
			if (self is Player)
				tileSizeMod += myChub * (BPOptions.hardMode.Value ? 2 : 1) + Mathf.Min((GetOverstuffed(self as Player) / 2), 12);
			else
				tileSizeMod += myChub; //NON-PLAYERS  GET IT EASIER OFF, BECAUSE WE DON'T WANT THEM IN OUR WAY...

			if (showLogs)
				Debug.Log("--DYNAMIC SUPERTIGHT! ");
		}
		
		
		//OH BOY, THIS COULD BE FUN~ THIS COULD MAKE EVEN LENIENT STARTING SHELTERS A VERY TIGHT SQUEEZE~
		if (self is Player && (self as Player).stillInStartShelter && self.GetBelly().breakfasted)
			tileSizeMod += (myChub * 2) + (GetOverstuffed(self as Player) / 2); //OKAY... MAYBE 2 IS JUST A LITTLE TOO MUCH. OR NOT?



		//MOVING THIS UP HERE
		//return tileSeed /= 2f; //f(x)
		//if (tileSeed <= 4)
		//	tileSizeMod -= 2f + myChub; //NOW REDUCES PROPORTIONALLY BY CHUB VALUE
	
		//OKAY MAYBE WE NEED TO MAKE ALL GAP SIZES A LITTLE BIT DYNAMIC FOR ALL THE HECKIN CHONKERS OUT THERE...
		// float chonkStretch = Mathf.Lerp(2f, 3f, ObjGetOverstuffed(self) / 20f); //2.5
		float chonkStretch = Mathf.Lerp(1.75f, 2.5f, ObjGetOverstuffed(self) / 15f); //2.5
		//Debug.Log("-----WHAT AM I!: " + ObjGetOverstuffed(self) + "+" + ObjGetOverstuffed(self) / 20f +" AM PLAYER " + (self is Player));


		//I WANNA TRY A NEW WAY OF PROPORTIONALLY SCALING THESE VALUES
		// tileSizeMod += (tileSeed - 8); //EFFECTIVELY DOUBLES THE SCALE OF TIGHTNESS WITHOUT INCREASING IT'S MAXIMUM
		// tileSizeMod += ((tileSeed*1.5f) - 12); //EFFECTIVELY TRIPLES!
		tileSizeMod += ((tileSeed * chonkStretch) - (8 * chonkStretch)); //ADDING ON SOME SCALING TO STRETCH OUT THE SCALE FOR HECKIN CHONKERS
		
		//tileSeed /= 2f;
		if (showLogs)
			Debug.Log("-----TILE SIZE MODIFIER!: " + tileSeed + "+" + tileSizeMod + (shortcutFlag ? (" -SHORTCUT TYPE!: " + self.room.shortcutData(myShortcutTile).shortCutType) : " ") + " " + self.room.GetTilePosition(self.bodyChunks[1].pos) + " " + posMod + " -COIN:" + coinFlip + " ROOM:" + self.room.roomSettings.name + " HARD? " + BPOptions.hardMode.Value + " WETHZD: " + wetHazard + " BONUS: " + chonkStretch);
		
		tileSeed += tileSizeMod;
		
		
		//SPIDERS...
		bool slickOverride = false;
		if (self is Player && self.grabbedBy.Count > 0)
			for (int i = 0; i < self.grabbedBy.Count; i++)
			{
				if (self.grabbedBy[i].grabber is Spider)
					slickOverride = true;
			}
		
		//SLIPPERY SHOULD MAKE THINGS EASIER!
		if (ObjIsSlick(self) || slickOverride)
		{
			tileSeed *= 0.8f; //+= 4;
			if (showLogs)
				Debug.Log("-----TILE SIZE (WITH SLICKNESS!): " + tileSeed);
		}
		
		if (rainWarning && tileSeed > 0)
		{
			tileSeed *= 0.8f;
			if (showLogs)
				Debug.Log("-----TILE SIZE (RAIN WARNING!): " + tileSeed);
		}
		
		//PET LIZARDS NEED TO KEEP UP BETTER!
		if ((self is Lizard) && patch_Lizard.IsTamed(self as Lizard) && !patch_Lizard.IsFriendInRoom(self as Lizard))
		{
			tileSeed *= 0.5f;
			if (showLogs)
				Debug.Log("-----LIZARD TO CATCH UP!): " + tileSeed);
		}

        //if (showLogs)
        //    Debug.Log("-----ORIG GAP SIZE!): " + (tileSeed /= 2f));
		//float extra = (self is Player player && player.playerState.playerNumber == 0) ? BPOptions.gapVariance.Value : 1f;
        return -4f + (tileSeed /= 2f) * BPOptions.gapVariance.Value;
	}
	
	
	//GET THE NUMBER OF ACTIVE SLUGCATS IN THE GAME (DOESN'T INCLUDE PUPS)
	public static int GetLivingPlayers(Creature self)
	{
		int pCount = 0;
		if (self.room != null) //DON'T FORGET THIS. APPARENTLY WEIRDNESS CAN OCCUR IF REVIVED 
		{
            for (int i = 0; i < self.room.game.Players.Count; i++)
            {
                if (self.room.game.Players[i].realizedCreature != null && !self.room.game.Players[i].realizedCreature.dead)
                    pCount += 1;
            }
        }
		return pCount;
	}


	//FIND THE NEAREST NON-PLAYER SLUGCAT, IF AVAILABLE.
	//OKAY WE UPDATED THIS ONE TO INCLUDE SLUGPUPS!... BUT NOT THE REST OF THEM. BUT... WHATEVER IT'S PROBABLY FINE
	public static Player FindPlayerInRange(Creature self)
	{
        if (self.room == null)
            return null;

        Player closestPlayer = null;
		//for (int i = 0; i < self.room.game.Players.Count; i++)
		for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
		{
			//Player checkPlayer = self.room.game.Players[i].realizedCreature as Player;
			Player checkPlayer = null;
			Creature checkCreature = self.room.abstractRoom.creatures[i].realizedCreature;
			if (checkCreature != null && checkCreature is Player)
				checkPlayer = (checkCreature as Player);

			if (//self.room.game.Players[i].Room.index == self.room.abstractRoom.index
				checkPlayer != null
				&& checkPlayer != self
				//&& (self.data.pointPlayerBack || self.room.game.Players[i].pos.abstractNode != self.data.exit) 
				&& checkPlayer.room == self.room
				&& checkPlayer.dead == false
				&& Custom.DistLess(self.mainBodyChunk.pos, checkPlayer.bodyChunks[1].pos, 35f)
			)
			{
				//return self.room.game.Players[i].realizedCreature as Player;
				if (closestPlayer == null)
					closestPlayer = checkPlayer;
				else if (IsStuck(checkPlayer)) //IF WE DETECTED MULTIPLE OF THEM BUT ONE OF THEM IS STUCK, PRIORITIZE THE STUCK ONE
					closestPlayer = checkPlayer;
			}
		}
		return closestPlayer;
	}
	
	
	//WHAT A...
	public static Player FindPlayerTopInRange(Creature self, float range)
	{
		if (self.room == null)
			return null; 
		
		Player closestPlr = null;
		float closest = range;
		float fattest = -5;
		for (int i = 0; i < self.room.game.Players.Count; i++)
		{
			if (//self.room.game.Players[i].Room.index == self.room.abstractRoom.index ?? WAS THIS FOR AGAIN??
				self.room.game.Players[i].realizedCreature != null
				&& self.room.game.Players[i].realizedCreature != self
				&& self.room.game.Players[i].realizedCreature.room == self.room
                && self.room.game.Players[i].realizedCreature.Consious //WE ADDED THIS SO WE DON'T TRY AND FEED US TO OURSELVES
                && Custom.DistLess(self.bodyChunks[0].pos, self.room.game.Players[i].realizedCreature.bodyChunks[0].pos, range)
			)
			{
				float myFatness = GetChubValue(self.room.game.Players[i].realizedCreature as Player);
				float myDist = Custom.Dist(self.bodyChunks[0].pos, self.room.game.Players[i].realizedCreature.bodyChunks[0].pos);
				if (myFatness > fattest)
				{
                    closestPlr = self.room.game.Players[i].realizedCreature as Player;
					fattest = myFatness;
					closest = myDist;
				}
				else if (myDist < closest)
				{ //UPDATE SO NOW WE RETURN THE CLOSEST MATCH
					closest = myDist;
                    closestPlr = self.room.game.Players[i].realizedCreature as Player;
                }
                //return self.room.game.Players[i].realizedCreature as Player;
				// break;
			}
		}
		return closestPlr;
	}


	public static int GetClosestBodyChunk(Vector2 myPos, Creature target)
	{
		int closestChunkID = 0;
		float closestDist = 100f;
		for (int i = 0; i < target.bodyChunks.Length; i++)
		{
			float dist = Vector2.Distance(myPos, target.bodyChunks[i].pos);
			if (dist < closestDist)
            {
				closestDist = dist;
				closestChunkID = i;
			}	
		}
		return closestChunkID;
	}


	//AND LIZAR!
	public static Lizard FindLizardInRange(Creature self, int myChunk, int lizChunk)
	{
        /*foreach (KeyValuePair<int, Lizard> kvp in patch_Lizard.lizardBook)
		{
			if (
				kvp.Value != null
				&& kvp.Value != self
				&& kvp.Value.room == self.room
				&& kvp.Value.dead == false
				&& Custom.DistLess(self.bodyChunks[myChunk].pos, kvp.Value.bodyChunks[lizChunk].pos, 35f)
			)
			{
				return kvp.Value as Lizard;
			}
		}*/

        if (self.room == null)
            return null;

        for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
		{
			if (self.room.abstractRoom.creatures[i].realizedCreature != null 
				&& self.room.abstractRoom.creatures[i].realizedCreature is Lizard checkLizard
				&& checkLizard != self
                && checkLizard.room != null 
				&& checkLizard.room == self.room
                && !checkLizard.dead
				&& Custom.DistLess(self.bodyChunks[myChunk].pos, checkLizard.bodyChunks[lizChunk].pos, 35f)
            )
			{
                return checkLizard;
			}
		}
		
		
		return null;
	}

	
	public static BodyPart GetHead(Player self) //lol
	{
		return (self.graphicsModule as PlayerGraphics).head;
    }



	//COPIES OF METHODS USED TO RETURN UNIVERSAL VALUES BETWEEN SPECIES
	public static bool ObjIsStuck(Creature obj)
	{
		return obj.GetBelly().isStuck;
	}
	
	
	public static bool ObjIsWedged(Creature obj)
    {
		if (obj is Player)
			return IsWedged((obj as Player));
		else
			return (PipeStatus(obj) && obj.GetBelly().wedgeStrain > 0.3f);
	}
	
	public static bool ObjIsPushingOther(Creature obj)
	{
		return obj.GetBelly().pushingOther > 0;
	}

	public static bool ObjIsPullingOther(Creature obj)
	{
		return obj.GetBelly().pullingOther;
	}

	public static bool ObjIsPushingOrPullingOther(Creature obj)
	{
		return ObjIsPushingOther(obj) || ObjIsPullingOther(obj);
	}


	public static void ObjPushedOn(Creature self)
	{
		if (self.GetBelly().beingPushed == 0)
			self.GetBelly().beingPushed = 7; //HEAR ME OUT... FOR SLOUCH SHOVING DELAYS
		else if (self.GetBelly().beingPushed <= 3)
			self.GetBelly().beingPushed = 3;
	}


	public static int ObjGetYFlipDirection(Creature obj)
	{
		return obj.GetBelly().myFlipValY;
	}
	
	public static int ObjGetXFlipDirection(Creature obj)
	{
		return obj.GetBelly().myFlipValX;
	}
	
	public static bool ObjIsVerticalStuck(Creature obj)
	{
		return obj.GetBelly().verticalStuck;
	}
	
	public static Vector2 ObjGetStuckVector(Creature obj)
	{
		return obj.GetBelly().stuckVector;
	}
	
	public static void ObjGainStuckStrain(Creature obj, float amnt)
	{
		obj.GetBelly().stuckStrain += amnt;
	}
	
	public static void ObjSetFwumpFlag(Creature obj, int amnt)
	{
		obj.GetBelly().fwumpFlag = amnt;
	}
	
	public static void ObjGainHeat(Creature obj, int amnt)
	{
		obj.GetBelly().myHeat = Math.Min(1500, obj.GetBelly().myHeat + amnt);
	}
	
	public static void ObjFeatherHeat(Player obj, int amnt, int max)
	{
		if (patch_Player.GetHeat(obj) < max)
			obj.GetBelly().myHeat += amnt;
	}
	
	public static int ObjGetHeat(Creature obj)
	{
		return obj.GetBelly().myHeat;
	}
	
	public static int ObjGetChubValue(Creature obj)
	{
		return GetChubValue(obj);
	}
	
	
	
	public static void ObjGainBoostStrain(Creature obj, int amnt, int bst, int cap)
	{
		BoostCreature(obj, bst, cap);
	}

    //squishForce
    public static void ObjGainSquishForce(Creature obj, int amnt, int max)
    {
		if (obj is Player self && IsStuck(self) && PipeStatus(self) == false)
		{
            int newBoost = Math.Min(obj.GetBelly().squishForce + amnt, max);
			
			//BIG BOOSTS WOULD BENIFIT FROM A DELAY, I THINK.
			if (amnt > 5)
			{
				obj.GetBelly().squishDelay = 3;
				obj.GetBelly().squishMemory = newBoost;
			}
			
            if (obj.GetBelly().squishForce >= max || obj.GetBelly().squishDelay > 0)
                return; //DO NOTHING. WE'RE ALREADY AT THE MAX, AND DON'T WANT TO LOWER IT. OR SQUISH DELAY IS ACTIVE
            else
                obj.GetBelly().squishForce = newBoost;
        }
    }


    public static int GetSquishForce(Creature obj)
    {
		return obj.GetBelly().squishForce;
    }


    public static float ObjGetLoosenProg(Creature obj)
	{
		return obj.GetBelly().loosenProg;
	}
	
	public static void ObjGainLoosenProg(Creature obj, float amnt)
	{
		if (obj is Player)
			obj.GetBelly().loosenProg += amnt * (GetLivingPlayers(obj as Player) == 1 ? 2f : 1.75f) * (BPOptions.hardMode.Value ? 1f : 6f);
		else
			obj.GetBelly().loosenProg += amnt * (BPOptions.hardMode.Value ? 2f : 6f);
	}
	
	public static int ObjBeingPushed(Creature obj)
	{
		return obj.GetBelly().beingPushed;
	}
	
	public static void ObjSetFwumpDelay(Creature obj, int amnt)
	{
		obj.GetBelly().fwumpDelay = amnt; 
	}
	
	public static void ObjSetWideEyes(Creature obj, int wideEyes)
	{
		obj.GetBelly().wideEyes = wideEyes;
	}


	public static void ObjSetNoStuck(Creature obj, int add)
	{
		obj.GetBelly().noStuck = add;
	}

	public static int ObjGetNoStuck(Creature obj)
	{
		return obj.GetBelly().noStuck;
	}

	public static void ObjApplySlickness(Creature obj)
	{
		int amnt = 3600;
		if (obj.room != null && obj.room.abstractRoom.shelter)
			amnt *= 2;
			
		obj.GetBelly().slicked += amnt;
	}

	public static bool ObjIsSlick(Creature obj)
	{
		return obj.GetBelly().slicked > 0;
	}

	public static Vector2 ObjGetBodyChunkPos(Creature obj, string body)
	{
		return obj.bodyChunks[ObjGetBodyChunkID(obj, body)].pos;
	}

    public static float BonusChunkRad(Creature obj, int chunk)
    {
		if (obj is Player)
			return (obj as Player).bodyChunks[chunk].rad - 8f;

        return 0;
    }


    public static int ObjGetBodyChunkID(Creature obj, string body)
	{
		int num = 0;
		if (obj is Yeek)
		{
			if (body == "upper")
				num = 1;
			else
				num = 2;
		}
		else
		{
			if (body == "upper")
				num = (obj is Scavenger) ? 1 : 0;
			else if (body == "middle" || obj is Scavenger)
				num = (obj is Scavenger) ? 0 : 1;
			else if (body == "rear")
				num = ((obj is Lizard) ? 2 : 1);
		}
		return num;
	}

	//NEEDS UPDATING IF NEW STUCKABLES ARE ADDED
	public static Vector2 ObjGetHeadPos(Creature obj)
	{
		if (obj is Player)
		{
			if ((obj as Player).graphicsModule != null)
				return GetHead(obj as Player).pos;//(obj as Player).graphicsModule.head.pos;
			else
				return obj.bodyChunks[0].pos;
		}
		else if (obj is LanternMouse)
		{
			if ((obj as LanternMouse).graphicsModule != null)
				return obj.graphicsModule.bodyParts[4].pos;
			else
				return obj.bodyChunks[0].pos;
		}
		else if (obj is Scavenger)
			return obj.bodyChunks[2].pos; //SCAVS HEAD IS CHUNK[2] I THINK
		else //if (obj is Lizard || obj is Scavenger)
			return obj.bodyChunks[0].pos;
	}
	
	//NEEDS UPDATING IF NEW STUCKABLES ARE ADDED
	public static bool ObjIsStuckable(Creature obj)
	{
		return (obj is Player || obj is LanternMouse || obj is Lizard || obj is Scavenger || obj is Cicada);// || obj is Yeek);
	}


    public static void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        bool skipCheck = false;
		int playerNumber = self.playerState.playerNumber;
		if ((playerNumber == 0 && BPOptions.fatP1.Value == false && !self.isNPC) ||
			(playerNumber == 1 && BPOptions.fatP2.Value == false) ||
			(playerNumber == 2 && BPOptions.fatP3.Value == false) ||
			(playerNumber == 3 && BPOptions.fatP4.Value == false)
			|| BellyPlus.VisualsOnly())
		{
			skipCheck = true;
		}
		
		if (grasp >= 0 && self.grasps[grasp] != null && !skipCheck)
        {
            PhysicalObject item = self.grasps[grasp].grabbed;
            CheckExternal(self, item);
        }
        orig(self, grasp);
    }

    public static void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player self)
    {
        orig(self);
        self.abstractCreature.GetAbsBelly().externalMass = 0;
        UpdateBellySize(self);
    }
	
	public static void CheckExternal(Player self, PhysicalObject item)
	{
		if (item == null || BellyPlus.VisualsOnly())
			return;

		
		if (item is Player player)
		{
			self.abstractCreature.GetAbsBelly().externalMass = (GetChubValue(player) + 4) + GetOverstuffed(player);
		}
		else if (item.TotalMass > 1 || self.Grabability(item) != Player.ObjectGrabability.OneHand)
		{
			self.abstractCreature.GetAbsBelly().externalMass = Mathf.CeilToInt(item.TotalMass) * 2;
		}
		// Debug.Log("EXTERNAL " + self.abstractCreature.GetAbsBelly().externalMass);
		UpdateBellySize(self);
	}


    //TO FIX BUGS WITH FOOD ON BACK RELATED TO SWALLOWING
    private static void Creature_Abstractize(On.Creature.orig_Abstractize orig, Creature self)
    {
		if (self is Player player)
		{
            if (self.GetBelly().foodOnBack != null)
			{
				//AbstractPhysicalObject food = self.GetBelly().foodOnBack.spear.abstractPhysicalObject;
				//food.realizedObject.RemoveFromRoom();
				//food.Abstractize(food.pos);
				//food.Room.RemoveEntity(food);
				//NOO NO THAT'S TOO COMPLICATED. JUST DROP IT
				self.GetBelly().foodOnBack.DropFood();
            }
		}
		orig(self);
    }
	
	//MORE MOD FRIENDLY WAY TO HANDLE TAIL MOVEMENT
	public static void WagTailSegment(Player self, int segment, Vector2 vel)
	{
		if (self.graphicsModule == null)
			return;

		int i = 0;
		foreach (var bodyPart in self.graphicsModule.bodyParts.OfType<TailSegment>())
		{
			if (i == segment)
			{
				bodyPart.vel += vel;
				break;
			}
			i++;
		}
	}


    //WEIGHTINESS (SLUGCAT VERSION ONLY!)
    public static int GetChubValue(Creature self)
	{
		return Mathf.FloorToInt(self.GetBelly().myChubValue);
		/*
		int currentFood = self.abstractCreature.GetAbsBelly().myFoodInStomach;
		if (self.slugcatStats.maxFood - currentFood <= -1)
			return 4;
		else if (self.slugcatStats.maxFood - currentFood == 0)
			return 3;
		else if (self.slugcatStats.maxFood - currentFood == 1)
			return 2;
		else if (self.slugcatStats.maxFood - currentFood == 2)
			return 1;
		else if (self.slugcatStats.maxFood - currentFood == 3)
			return 0;
		else if (self.slugcatStats.maxFood - currentFood == 4)
			return -1;
		else if (self.slugcatStats.maxFood - currentFood == 5)
			return -2;
		else if (self.slugcatStats.maxFood - currentFood == 6)
			return -3;
		else
		{
			return -4;
		}
		*/
	}
	
	
	public static float GetChubFloatValue(Player self)
	{
		return self.GetBelly().myChubValue;
	}
	
	public static void SetChubModOffset(Player self, int amnt)
	{
		self.abstractCreature.GetAbsBelly().chubModOffset = amnt;
		UpdateBellySize(self);
	}
	
	public static float GetAdjChubValue(Player self)
	{
		
		int currentFood = self.abstractCreature.GetAbsBelly().myFoodInStomach + self.abstractCreature.GetAbsBelly().externalMass;
		int bellyMod = (self.GetBelly().bigBelly ? 2 : 0) + BellyOffset(self);
		if (self.isNPC && self.isSlugpup)
			bellyMod -= 1;

		if (self.slugcatStats.maxFood - currentFood <= -2 + bellyMod)
			return 4f;
		else if (self.slugcatStats.maxFood - currentFood == -1 + bellyMod)
			return 3.5f;
		else
			return 3f - (self.slugcatStats.maxFood - currentFood) + bellyMod; //(bellyMod / 2f)
	}


	public static int GetPersonalFood(Player self)
	{
		int myFood = self.abstractCreature.GetAbsBelly().myFoodInStomach;
		int maxFood = self.slugcatStats.maxFood;
		if (myFood > self.slugcatStats.maxFood)
			myFood = maxFood + Mathf.FloorToInt((myFood - maxFood) / 2);
		//Mathf.Max(currentFood - (AdjMaxFood(self)), 0)
		return myFood;
	}

	public static int GetOverstuffed(Player self)
	{
		//ALL THE TIME!
		int currentFood = self.abstractCreature.GetAbsBelly().myFoodInStomach + self.abstractCreature.GetAbsBelly().externalMass;
		return Mathf.Max(currentFood - (AdjMaxFood(self)), 0);
	}

	public static float GetScaledOverstuffed(Player self)
	{
		float stuffing = GetOverstuffed(self);
		if (stuffing > 10)
			stuffing = 10 + ((stuffing / 2f) - 10);
		return stuffing;
	}
	
	
	public static int ObjGetOverstuffed(Creature self)
	{
		if (self is Player)
			return GetOverstuffed(self as Player);
		else if (self is Lizard)
			return patch_Lizard.GetOverstuffed(self);
		else
			return 0;
	}


	public static int AdjMaxFood(Player self)
	{
		return self.slugcatStats.maxFood + (self.GetBelly().bigBelly ? 0 : 2) - BellyOffset(self);
	}

	public static int BellyOffset(Player self)
	{
		if (ModManager.Expedition && Custom.rainWorld.ExpeditionMode && Expedition.ExpeditionGame.activeUnlocks.Contains("bur-rotund") && !self.isNPC)
			return ((self.slugcatStats.maxFood - self.slugcatStats.foodToHibernate) + 2); // (obj.GetBelly().bigBelly ? 0 : 2));
		else
			return -(BPOptions.startThresh.Value + self.abstractCreature.GetAbsBelly().chubModOffset); // - 2;
	}

	//FINALLY... SHORTCUT TO ONE OF MY MOST USED FORMULA EQUATIONS
	public static float ScaledBack(float val, float at, float div)
	{
		float result = val;
        if (result > at)
            result = at + ((result - at) / div);
		return result;
    }


	public static float CalcRunSpeedMod(Player self) //, float chub)
	{
		float spd = 1f;
		
		/*
		if (BPOptions.easilyWinded.Value)
		{
			if (chub <= 3f)
				spd = 1f;
			else if (chub <= 3.5f)
				spd = 0.9f;
			else // chub >= 4f
			{
				spd = 0.8f;
                spd -= Mathf.Min((chub - 4 + GetOverstuffed(self)) * (0.05f * (1f - (BPOptions.bpDifficulty.Value * 0.15f))), 0.3f);
            }
		}
		else
		{
            //NOW, BY DEFAULT, ONLY OVERSTUFFED COUNTS TOWARDS NEGATIVE RUNSPEED AND ALSO 
            spd -= Mathf.Min(GetOverstuffed(self) * (0.05f * (1f + (BPOptions.bpDifficulty.Value * 0.15f))), 0.5f);
        }
		*/


		float reduce = 0f;
		//NOTE THIS DOES MEAN THAT IF OUR PARTNER DIES AND WE EAT AGAIN OUR SPEED WILL SUDDENLY SHOOT BACK UP... BUT WHATEVER LOL
		bool extFloor = GetLivingPlayers(self) > 1 || (self.room != null && self.room.game.IsArenaSession || BPOptions.easilyWinded.Value);
        float minSpd = extFloor ? 333f : 0.5f;
		//START EARLIER FOR EASILY WINDED
        if (BPOptions.easilyWinded.Value)
            reduce += Mathf.Max(GetChubFloatValue(self) / 20f, 0f); // (0 - 0.2)   -0.2f if 4 chub.  -0.1f at 3 chub

        reduce = Mathf.Min(reduce + GetOverstuffed(self) * (0.05f * (1f + (BPOptions.bpDifficulty.Value * 0.15f))), minSpd); //0.5f

		if (self.slugcatStats?.name?.value == "Cloudtail")
			reduce = Mathf.Max(0, (reduce - 0.15f) * 0.1f);

        //IF WE'RE IN MULTIPLAUER/ARENA MODE, GO EVEN FURTHER
        if (extFloor)
		{
			if (BPOptions.easilyWinded.Value)
				reduce = Mathf.Min(ScaledBack(reduce, 0.5f, 2.5f), 0.9f);
			else
				reduce = Mathf.Min(ScaledBack(reduce, 0.5f, 3f), 0.9f);
		}
			
        spd -= reduce;



        if (BPOptions.debugLogs.Value)
			Debug.Log("NEW RUN SPEED: " + spd);

        //WAIT A MINUTE! THIS IS RUNNING AT A TERRIBLE TIME BECAUSE THIS ONLY CHECKS ONCE WHEN WE EAT AND THEN APPLIES TO A PERMINANT VAR
        //if (IsPullingOther(self))
        //	spd /= 3f;

        return spd;
	}


	public static bool flip = false;
	public static void UpdateBellySize(Player self) //WE ALSO SET THE BODY CHUNK MASS HERE
	{
		
		float baseWeight = (0.7f * self.slugcatStats.bodyWeightFac) / 2f;
		float radMod = 1f;
		float oldChub = self.GetBelly().myChubValue;
        self.GetBelly().myChubValue = GetAdjChubValue(self);
		self.GetBelly().runSpeedMod = CalcRunSpeedMod(self);//, self.GetBelly().myChubValue); //+ (BPOptions.easilyWinded.Value ? 2 : 0)

        //MAKE SURE THE GRAPHICS EXIST FIRST. IN SOME WEIRD SPAWNS THEY WON'T ALWAYS EXIST YET
        if (self.graphicsModule != null)
			patch_PlayerGraphics.UpdateTailThickness(self.graphicsModule as PlayerGraphics); //, tailThick);
		
		if (BellyPlus.VisualsOnly())
			return;
		
		//USES OUR MOVEMENT MODIFIER FOR OUR POLE CLIMB SPEED
		// self.slugcatStats.poleClimbSpeedFac = self.GetBelly().origPoleSpeed * self.GetBelly().runSpeedMod; 
		self.slugcatStats.poleClimbSpeedFac = self.abstractCreature.GetAbsBelly().origPoleSpeed * Mathf.Lerp(-0.15f, 1f, Mathf.Max(self.GetBelly().runSpeedMod, 0.3f));  //MAKE POLE CLIMBING SPEED CHANGES MORE NOTICEABLE //-0.35f, 1f,

        float newWeight = baseWeight;
		float corridorSpeedFact = 1f;
		switch (GetChubValue(self))
		{
			case 4:
				corridorSpeedFact = 0.7f - 0.025f * Mathf.Min(GetOverstuffed(self), 12); //55
				newWeight *= 1.5f + (0.1f * GetOverstuffed(self));
                radMod = 1.4f + Mathf.Min(0.05f * GetOverstuffed(self), 0.5f);
                break;
			case 3:
				corridorSpeedFact = 0.8f; //65
				newWeight *= 1.3f;
                radMod = 1.2f;
                break;
			case 2:
				corridorSpeedFact = 0.9f; //85
				newWeight *= 1.1f;
                radMod = 1f;
                break;
			case 1:
				corridorSpeedFact = 1.0f; //85
				newWeight *= 1f;
                radMod = 1f;
                break;
			case 0:
			default:
				//self.slugcatStats.corridorClimbSpeedFac = 0.75f;
				//self.slugcatStats.bodyWeightFac = 1f; //NO DON'T EDIT THESE! WE USE THESE AS THE BASE POINT
				corridorSpeedFact = 1f;
				newWeight *= 1f;
				radMod = 1f;
				break;
		}

		//if (BPOptions.debugLogs.Value)
			// Debug.Log("UPDAT BELLY SIZE: NEW MASS: " + newWeight);

		//OKAY WE DON'T NEED UPPER TORSO TO BECOME ENOURMOUS
		self.bodyChunks[0].mass = Mathf.Min(newWeight, baseWeight * 3f);
		self.bodyChunks[1].mass = newWeight;

		//if (flip)
		//{
		//	radMod = 2.8f;
		//	flip = false;
		//}
		//else
		//{
		//	radMod = 1f;
		//	flip = true;
		//}


        self.bodyChunks[0].rad = 9f * (1 + ((radMod - 1) / 3f));
		self.bodyChunks[0].terrainSqueeze = 1f / (1 + ((radMod - 1) / 3f));
		self.bodyChunks[1].rad = 8f * radMod;
		self.bodyChunks[1].terrainSqueeze = 1f / radMod;
		//baseRad

		if (BPOptions.debugLogs.Value) 
             Debug.Log("UPDAT BELLY RAD: NEW MASS: " + self.bodyChunks[0].rad + " - " + self.bodyChunks[0].TerrainRad + " - " + self.bodyChunks[0].terrainSqueeze);

		//FIX THIS AGAIN
		self.GetBelly().myCooridorSpeed = self.slugcatStats.corridorClimbSpeedFac * corridorSpeedFact;

		//MAKE A FUNNY SOUND IF WE'RE NOW FAT ENOUGH TO SLAM
		if (BPOptions.slugSlams.Value && BPOptions.hudHints.Value && !self.isGourmand && !self.GetBelly().canSlugSlam && ChunkyFallRequirement(self))//&& oldChub < 3 && GetChubValue(self) >= 3)
		{
			self.GetBelly().canSlugSlam = true; //THIS IS MOSTLY JUST FOR THE NOISE I GUESS...
            self.PlayHUDSound(SoundID.HUD_Karma_Reinforce_Bump);
        }
			

        //CHECK IF OUR BONUS PIP NEES TO BE SAVED FOR END OF CYCLE
        CheckBonusFood(self, false);
	}
	
	
	public static bool CheckFattable(Creature crit)
	{
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
		
		return true;
	}
	
	
	public static void MakeSparks(PhysicalObject self, int chunk, int sparkCount)
	{
		for (int n = 0; n < sparkCount; n++)
		{
			Vector2 pos3 = self.bodyChunks[chunk].pos;
			//ALRIGHT/ LETS BE BIG BOYS HERE
			for (int i = 1; i > -2; i-= 2)
            {
				//WaterDrip drip1 = new WaterDrip(pos3, new Vector2(ObjGetXFlipDirection(self) * xvel * i, Mathf.Lerp(-4f, 8f, UnityEngine.Random.value)), false);
				WaterDrip drip1 = new WaterDrip(pos3, new Vector2((Mathf.Lerp(0f, 6f, UnityEngine.Random.value) + sparkCount) * i, Mathf.Lerp(-4f, 8f, UnityEngine.Random.value)), false);
				self.room.AddObject(drip1);
				drip1.lifeTime = UnityEngine.Random.Range(8, 15);
				drip1.colors = new Color[]
				{
					//Color.Lerp(palette.waterColor2, palette.waterColor1, 0.5f),
					new Color(1f, 1f, 1f),
					new Color(1f, 1f, 1f),
					new Color(1f, 1f, 1f)
				};
			}
		}
	}



	

	public static void MakeStrainSparks(Creature self, int sparkCount)
	{
		for (int n = 0; n < sparkCount; n++)
		{
			Vector2 pos3 = ObjGetHeadPos(self);
			self.room.AddObject(new StrainSpark(pos3, new Vector2(0, 0) + Custom.DegToVec((360f * UnityEngine.Random.value)) * 6f * UnityEngine.Random.value, 15f, Color.white));
		}
	}


	public static void MakeSquealch(Creature self, bool big)
	{
		if (big)
			self.room.PlaySound(SoundID.Swollen_Water_Nut_Terrain_Impact, self.mainBodyChunk, false, 0.8f, 0.8f + Mathf.Lerp(-0.2f, 0.2f, UnityEngine.Random.value));
		else
			self.room.PlaySound(SoundID.Tube_Worm_Shoot_Tongue, self.mainBodyChunk, false, 1.2f, 1f + Mathf.Lerp(-0.2f, 0.2f, UnityEngine.Random.value)); //Tube_Worm_Shoot_Tongue //Slime_Mold_Terrain_Impact

		int drips = big ? 12 : 8;
		for (int n = 0; n < drips; n++)
		{
			self.room.AddObject(new StrainSpark(ObjGetBodyChunkPos(self, "middle"), new Vector2(0, 0) + Custom.DegToVec((360f * UnityEngine.Random.value)) * 3f, 15f, Color.white));
		}
	}


	private static readonly float maxStamina = 120f;
	public static float GetExhaustionMod(Player self, float startAt)
	{
		float exh = self.GetBelly().corridorExhaustion;
		//Debug.Log("-----STAMM!: " + Mathf.Max(0f, exh - startAt) + " - " + (maxStamina - startAt));
		return Mathf.Max(0f, exh - startAt) / (maxStamina - startAt);
	}


	public static int GetHeat(Player self)
	{
		return self.GetBelly().myHeat;
	}

	public static bool GetPant(Player self)
	{
		return self.GetBelly().breathIn;
	}

	public static void SetPant(Player self, bool set)
	{
        self.GetBelly().breathIn = set;
	}

	public static int GetWideEyes(Player self)
	{
		return self.GetBelly().wideEyes;
	}

	public static bool IsPlayerSqueezing(Player self)
	{
		return self.GetBelly().isSqueezing;
	}
	
	public static void SetForceEatTarget(Player self, PhysicalObject obj)
	{
		self.GetBelly().forceEatTarget = obj;
	}
	
	public static PhysicalObject GetForceEatTarget(Player self)
	{
		return self.GetBelly().forceEatTarget;
	}
	

	public static bool IsCramped(Creature self)
	{
		//RETURNS TRUE IF IN A CORRIDOR OR PRESSING SELF AGAINST AN ENTRANCE
		//return (self.bodyMode == Player.BodyModeIndex.CorridorClimb || obj.GetBelly().stuckStrain > 0);
		//NAH, JUST RETURNS TRUE IF IN A PASSAGE AT ALL
		// if (self.room == null)
			// return false;
		// else if (self.room.aimap == null)
			// return false;
		// else
			// return ((self.room.aimap.getAItile(self.bodyChunks[1].pos).narrowSpace) || ObjIsStuck(self));
		return (PipeStatus(self) || ObjIsStuck(self)) && !self.dead;
	}

	public static bool IsGrabbedByPlayer(Creature self)
	{
		if (self.grabbedBy.Count <= 0)
			return false;
		else if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player)
			return true;
		else
			return false;
    }

	public static bool IsGrabbedByHelper(Creature self)
	{
		if (self.grabbedBy.Count <= 0 || (self is Player && (self as Player).dangerGrasp != null))
			return false;
		else if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Creature)
			return true;
		else
			return false;
	}


	public static bool IsStuck(Player self)
	{
		return self.GetBelly().isStuck;
	}

	public static float GetStuckStrain(Player self)
	{
		return self.GetBelly().stuckStrain;
	}

	public static float GetBoostStrain(Player self)
	{
		return self.GetBelly().boostStrain;
	}
	
	public static float GetProgress(Player self)
	{
		return self.GetBelly().loosenProg;
	}

	public static float GetStuckPercent(Creature self)
	{
		float squeezeThresh = self.GetBelly().tileTightnessMod;
		//SAFETY MEASURE TO AVOID DEVIDING BY 0
		if (squeezeThresh == 0)
			return 0;
		else
			return (self.GetBelly().stuckStrain / squeezeThresh);
	}
	
	
	//THIS VERSION OF "START AT" EXPECTS A 0.0 - 1.0 PERCENT MOD
	public static float GetStuckMod(Player self, float startAt)
	{
		float exh = self.GetBelly().stuckStrain;
		float squeezeThresh = self.GetBelly().tileTightnessMod;
		float floorCheck = squeezeThresh * startAt;
		if (squeezeThresh - floorCheck <= 0)
			return 0;
		else
			return Mathf.Max(0f, exh - floorCheck) / (squeezeThresh - floorCheck);
	}
	

	public static float GetCappedBoostStrain(Player self)
	{
		return Mathf.Min(GetBoostStrain(self), 18f);
	}

	public static bool IsPushingOther(Creature self)
	{
		return self.GetBelly().pushingOther > 0;
	}

	public static bool IsPullingOther(Creature self)
	{
		return self.GetBelly().pullingOther;
	}

	public static Creature GetGraspedCreature(Creature self)
	{
        for (int j = self.grasps.Length - 1; j >= 0; j--) //CHECK BOTH GRASPS, DUMMY
        {
            if (self.grasps[j] != null && self.grasps[j].grabbed is Creature crit && !crit.dead)
               return crit;
        }
		return null;
	}

	//THIS IS A LIE NOW
	public static bool IsGraspingStuckable(Creature self)
	{
		//return self.grasps[0] != null && self.grasps[0].grabbed is Creature && ObjIsStuckable(self.grasps[0].grabbed as Creature);
		Creature crit = GetGraspedCreature(self);
        return crit != null && ObjIsStuckable(crit);
    }

	public static bool IsGraspingActualSlugcat(Creature self)
	{
        Creature crit = GetGraspedCreature(self);
        return crit != null && crit is Player;
	}

	public static bool IsGraspingStuckCreature(Creature self)
	{
        Creature crit = GetGraspedCreature(self);
        return crit != null && ObjIsStuckable(crit) && ObjIsStuck(crit);
	}

	//TRUE IF WE ARE STUCK OR GRABBING ONTO A CREATURE THAT IS PULLING SOME OTHER CREATURE
	public static bool InPullingChain(Creature self)
	{
        Creature crit = GetGraspedCreature(self);
        return IsStuckOrWedged(self) || (IsGraspingStuckable(self) && InPullingChain(crit));
	}

	public static void PassDownBenifits(Creature target, float strain, int boost, int cap)
	{
		if (IsStuckOrWedged(target))
        {
			ObjGainStuckStrain(target, strain);
			ObjGainBoostStrain(target, 0, boost, cap);
		}
		else if (IsGraspingStuckable(target))
        {
			ObjGainBoostStrain(target, 0, boost / 2, cap);
            Creature crit = GetGraspedCreature(target);
            PassDownBenifits(crit, strain, boost, cap);
		}
	}

	public static bool IsVerticalStuck(Creature self)
	{
		return self.GetBelly().verticalStuck;
	}
	
	public static bool PipeStatus(Creature self)
	{
		return self.GetBelly().inPipeStatus;
	}

	public static int GetYFlipDirection(Creature self)
	{
		return self.GetBelly().myFlipValY;
	}



	/*public static void BoostPartner(Player self, int amnt, int max)
	{
		
		int newBoost = Math.Min(self.GetBelly().boostStrain + amnt, max);

		if (self.GetBelly().boostStrain >= max)
			return; //DO NOTHING. WE'RE ALREADY AT THE MAX, AND DON'T WANT TO LOWER IT
		else
			self.GetBelly().boostStrain = newBoost;
	}*/


	public static void BoostCreature(Creature self, int amnt, int max)
	{
		int currentBoost = self.GetBelly().boostStrain;
		int newBoost = Math.Min(currentBoost + amnt, max);

		if (currentBoost >= max)
			return; //DO NOTHING. WE'RE ALREADY AT THE MAX, AND DON'T WANT TO LOWER IT
		else
			self.GetBelly().boostStrain = newBoost;
	}
	
	
	public static bool IsFoodSmearable(PhysicalObject obj)
	{
		return (obj is DangleFruit || 
				obj is EggBugEgg || 
				obj is SwollenWaterNut || 
				obj is KarmaFlower || 
				obj is Mushroom || 
				obj is SlimeMold || 
				obj is DandelionPeach ||
				obj is GlowWeed);
	}
	
	
	public static bool CheckApplyLather(Creature self, Creature otherCrit)
	{
		bool correctMaterial = false;
		bool crushFood = false;
		Vector2 foodPos = new Vector2(0, 0);
		
		for (int j = self.grasps.Length - 1; j >= 0; j--)
		{
			if (self.grasps[j] != null) 
			{
				PhysicalObject obj = self.grasps[j].grabbed;
				if (IsFoodSmearable(obj))
				{
					for (int m = 0; m < self.abstractCreature.stuckObjects.Count; m++)
					{
						if (self.abstractCreature.stuckObjects[m].A == self.abstractCreature && self.abstractCreature.stuckObjects[m].B == obj.abstractPhysicalObject)
						{
							self.abstractCreature.stuckObjects[m].Deactivate();
							break;
						}
					}
					
					correctMaterial = true;
					
					//DON'T SMEAR ON THE FIRST ATTEMPT
					if (otherCrit is Player && (otherCrit.GetBelly().smearTimer == 0))
					{
						//DO NOTHING :3c
					}
					else if (UnityEngine.Random.value < 0.33f) //1 IN 4
					{
						crushFood = true;
						foodPos = obj.firstChunk.pos;
						obj.Destroy();
						self.room.RemoveObject(obj);
						self.room.abstractRoom.RemoveEntity(obj.abstractPhysicalObject);
						self.ReleaseGrasp(j);
						BellyPlus.smearHintGiven = true; //THEY ALREADY KNOW HOW TO USE THAT!
					}
					
					if (otherCrit is Player)
						otherCrit.GetBelly().smearTimer = 40;
					
					break;
				}
			}
		}
		
		
		//RANDOM CHANCE IF MATERIAL IS CRUSHED
		if (correctMaterial)
		{
			int drips = crushFood ? 12 : 8;
			float dripVel = crushFood ? 4f : 3f;
			float dripVol = crushFood ? 1.5f : 0.8f;
			float dripPitch = crushFood ? 1f : 0.8f;
			
			for (int n = 0; n < drips; n++)
			{
				Vector2 pos3 = ObjGetBodyChunkPos(otherCrit, "rear");
				self.room.AddObject(new StrainSpark(pos3, new Vector2(0, 0) + Custom.DegToVec((360f * UnityEngine.Random.value)) * dripVel, 15f, Color.white));
			}
			self.room.PlaySound(SoundID.Swollen_Water_Nut_Terrain_Impact, self.mainBodyChunk, false,  dripVol, dripPitch);
			
			if (otherCrit == self && self is Player)
			{
				(self as Player).Blink(12);
				self.bodyChunkConnections[0].distance /= 1.3f; //SCRUNCH BACK A LITTLE BIT
			}
			
			//EXTRA PARTICLES IF CRUSHED
			if (crushFood)
			{
				for (int n = 0; n < 15; n++) //STRAIN DRIPS
				{
					Vector2 pos3 = foodPos; //ObjGetBodyChunkPos(otherCrit, "middle");
					self.room.AddObject(new WaterDrip(pos3, new Vector2(Mathf.Lerp(-3f, 3f, UnityEngine.Random.value), Mathf.Lerp(-2f, 6f, UnityEngine.Random.value)), false));
				}
			}
		}
		return crushFood;
	}




	//APPETITE
	public static int MaxBonusFood(Player self)
	{
		if (self.isNPC)
			return 0;

		return BellyPlus.MaxBonusPips;

		//if (self.room != null && self.room.game.Players.Count > 1)
		//	return Math.Max(self.slugcatStats.foodToHibernate - 2, 1);
		//else
		//	return 10;
	}


    
	//THIS SHOULD RETURN THE FATTEST BELLY VALUE OF ALL PEOPLE IN THE GAME (REGARDLESS OF IF THEY ARE DEAD OR GONE OR NOT)
    public static int FindFattestSlug(Player self)
    {
		//.abstractCreature.world.game.Players.Count
		int fattest = 0;
        for (int i = 0; i < self.abstractCreature.world.game.Players.Count; i++)
        {
			if (self.abstractCreature.world.game.Players[i].GetAbsBelly().myFoodInStomach > fattest)
				fattest = self.abstractCreature.world.game.Players[i].GetAbsBelly().myFoodInStomach;
        }
		return fattest;
    }


    public static Player ReturnFattestSlug(Player self)
    {
        int fattest = 0;
		Player slug = null;
        for (int i = 0; i < self.abstractCreature.world.game.Players.Count; i++)
        {
            if (self.abstractCreature.world.game.Players[i].GetAbsBelly().myFoodInStomach > fattest
				&& self.abstractCreature.world.game.Players[i].realizedCreature != null
                && self.abstractCreature.world.game.Players[i].realizedCreature.dead == false)
			{
                fattest = self.abstractCreature.world.game.Players[i].GetAbsBelly().myFoodInStomach;
				slug = self.abstractCreature.world.game.Players[i].realizedCreature as Player;
            }
        }
        return slug;
    }


    public static void CheckBonusFood(Player self, bool init)
	{
		//4-7-23 SCRATCH EVERYTHING YOU ONCE KNEW! WE'RE THROWING IT OUT BEACUASE PLAYER FOOD NOW ONLY LOOKS AT THE LARGEST FOOD VALUE IN THE PARTY
		//BUT ONLY WHEN PAST OUR MAXIMUM... YEA, KINDA WEIRD I KNOW
		if (!self.isNPC && (!BellyPlus.lockEndFood || BellyPlus.individualFoodEnabled)) //FOR INDIVIDUAL FOOD BARS, WE CAN RUN THIS STILL!
		{
			int fattest = FindFattestSlug(self);
			BellyPlus.bonusHudPip = Math.Max(0, fattest - self.slugcatStats.maxFood) * 2; //EVERY POINT OF FOOD PAST MAXIMUM COUNTS AS TWO BONUS HUD PIPS
            //Debug.Log("CHECKING BONUS FOOD " + BellyPlus.bonusHudPip + " FATEST:" + fattest);
        }
		else
            Debug.Log("REJECT BONUS FOOD UPDATE! DOOR ALREADY CLOSED " + BellyPlus.bonusHudPip);

        //PIPS FOR THE HUD DISPLAY
        if (!self.isNPC && (BellyPlus.bonusHudPip >= 1 || init))
		{
			//WE'RE REMOVING THE CAP
			BellyPlus.bonusFood = Mathf.Min(Mathf.FloorToInt(BellyPlus.bonusHudPip / 4f));
		}

		//SOME EXTRA BS FOR INDIVIDUAL FOOD BARS
		if (BellyPlus.individualFoodEnabled && !self.isNPC && self.abstractCreature.world.game.IsStorySession)
		{
            //FOR THE VISUAL VERSION, LOOK AT ONLY P1'S BELLY
			int p1MaxBelly = SlugcatStats.SlugcatFoodMeter((self.abstractCreature.world.game.Players[0].state as PlayerState).slugcatCharacter).x;
			BellyPlus.bonusHudPip = Math.Max(0, self.abstractCreature.world.game.Players[0].GetAbsBelly().myFoodInStomach - p1MaxBelly) * 2; //EVERY POINT OF FOOD PAST MAXIMUM COUNTS AS TWO BONUS HUD PIPS
        }
	}
	

	public static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
	{
		//HEY! MODDERS! THIS IS NOT HOW YOU SUBTRACT FOOD! LET ME FIX IF FOR YOU
		if (add < 0)
		{
			self.SubtractFood(-add);
			return;
		}
		add *= BPOptions.foodMult.Value;
		
		//SPECIAL EXCEPTIONS FOR SOME CHALLENGE MODE LEVELS 
		if (ModManager.MSC && self.room != null && self.room.game.IsArenaSession 
			&& self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == MoreSlugcatsEnums.GameTypeID.Challenge)
		{
			if (add == 5 && (self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeID == 63 || self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeID == 45))
			{
				add = 1;
				self.room.game.GetArenaGameSession.arenaSitting.players[self.playerState.playerNumber].AddSandboxScore(4);
			}
			else if ((self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeID == 24 || self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeID == 25))
			{
				if (UnityEngine.Random.value < 0.75f)
				{
					self.room.game.GetArenaGameSession.arenaSitting.players[self.playerState.playerNumber].AddSandboxScore(add);
					add = 0;
				}
			}
		}


        //REDS ILLNESS AFFECTS HOW MUCH FOOD OUR FOOD IS WORTH!
        int newAdd = add;
        if (self.redsIllness != null)
		{
			float adjustAdd = add * self.redsIllness.FoodFac;

			//RECALCULATE FOOD GAINS
			newAdd = 0;
			while (adjustAdd > 1)
			{
                newAdd += 1;
				adjustAdd -= 1f;
            }

            partialPip += adjustAdd;

            if (partialPip >= 1f)
            {
				newAdd += 1;
                partialPip -= 1f;
			}
        }

		
		//SINCE THE UPDATE, I THINK IT'S SAFE FOR US TO ADD THIS UP HERE
		AddPersonalFood(self, newAdd);
		bool skipOrig = false;
		
		if (BellyPlus.VisualsOnly())
		{
            //WE CAN STILL MAKE THE SOUND THO
            if (self.FoodInStomach >= self.slugcatStats.maxFood && self.abstractCreature.world.game.IsStorySession)
                if (self.room?.game?.cameras[0]?.hud?.foodMeter != null)
                    self.PlayHUDSound(SoundID.HUD_Food_Meter_Fill_Quarter_Plop);

            orig.Invoke(self, add);
			return;
		}

		// if (BPOptions.debugLogs.Value)
		// Debug.Log("-----  ADDFOOD " + self.playerState.foodInStomach + " MAX:" + self.slugcatStats.maxFood + " MAX2" + self.MaxFoodInStomach);

		if (BellyPlus.fullBellyOverride) //FOR DRONEMASTER SO WE COULD BRIEFLY FAKE A NON-FULL BELLY
			self.playerState.foodInStomach++;

        //DOWNPOUR FIXED THE AWEFUL JOLLYCOOP MESS SO WE CAN SAFELY DO THINGS NORMALLY
        if (self.FoodInStomach >= self.slugcatStats.maxFood && self.abstractCreature.world.game.IsStorySession)
		{
			if (BPOptions.debugLogs.Value)
				Debug.Log("-----ADDING FOOD PAST FULL " + self + " ADD:" + newAdd + "  CURRENT CHUB:" + GetChubFloatValue(self) + " + " + GetOverstuffed(self) );
			
			if (self.AI == null)
				self.abstractCreature.world.game.GetStorySession.saveState.totFood += newAdd;
			
			//PUPS DON'T TRACK BONUS FOOD! THEIR FOODINSTOMACH SAVES PROPERLY SO WE CAN JUST TRACK THAT
			if (self.isNPC && self.isSlugpup)
            {
				self.playerState.foodInStomach += add;
				skipOrig = true;
			}

			if (self.room?.game?.cameras[0]?.hud?.foodMeter != null) // && BellyPlus.bonusFood < MaxBonusFood(self))
				self.PlayHUDSound(SoundID.HUD_Food_Meter_Fill_Quarter_Plop);

		}
		//NO LONGER AN ELSE
		if (self.playerState.foodInStomach < self.slugcatStats.maxFood)
		{
			int overflowFood = Math.Max((self.playerState.foodInStomach + add) - self.slugcatStats.maxFood, 0);
			if (BPOptions.debugLogs.Value)
				Debug.Log("-----RUNNING ORIGINAL ADDFOOD " + add + " OVRFL:" + overflowFood);
			orig.Invoke(self, add - overflowFood);
			if (overflowFood > 0)
			{
				self.AddFood(overflowFood); //GOTTA DO IT AGAIN CUZ OUR BELLY WILL CUT OFF THE EXTRA.
			}
		}
		else
		{
			//if (BPOptions.debugLogs.Value)
			//	Debug.Log("-----TOOO FULL! SKIPPING ADDFOOD" + self);
			//BLUH. I GUESS THIS WAS NEEDED TOO...
			if (!skipOrig)
				orig.Invoke(self, add);
		}
		//CHECK IF OUR BONUS PIP NEES TO BE SAVED FOR END OF CYCLE
		CheckBonusFood(self, false);
	}
	
	
	
	
	public static void Player_SubtractFood(On.Player.orig_SubtractFood orig, Player self, int sub)
	{
		
		
		if (BellyPlus.VisualsOnly())
		{
			self.abstractCreature.GetAbsBelly().myFoodInStomach -= sub;
			orig.Invoke(self, sub);
			UpdateBellySize(self);
			return;
		}
		
		//IF WE HAVE ANY BONUS AT ALL, SUBTRACT FROM THAT BEFORE WE SUBTRACT ANYTHING REAL
		bool takeBonusPips = (!BellyPlus.individualFoodEnabled && BellyPlus.bonusHudPip >= 2) || (BellyPlus.individualFoodEnabled && self.abstractCreature.GetAbsBelly().myFoodInStomach > self.slugcatStats.maxFood);
		if (takeBonusPips && !(self.isNPC && self.isSlugpup))
		{
			if (self.room?.game?.cameras[0]?.hud?.foodMeter != null)
			{
				//THIS WON'T PLAY ON IT'S OWN, SINCE OUR BELLY SIZE ISN'T CHANGING
				self.PlayHUDSound(SoundID.HUD_Food_Meter_Deplete_Plop_A); 
				self.room.game.cameras[0].hud.foodMeter.refuseCounter = 20; //THIS BRIEFLY FORCES THE HUD TO SHOW UP
			}

			//WE CAN'T DO THIS IF WE HAVE NO FOOD!
			if (self.abstractCreature.GetAbsBelly().myFoodInStomach <= 0)
			{
				Player fatSlug = ReturnFattestSlug(self); //FIND THE FATTEST SLUG THAT ISN'T US
				if (fatSlug != null && fatSlug != self)
                    fatSlug.SubtractFood(sub); //MAKE THIS THEIR PROBLEM INSTEAD
				return; //AND CUT THE REST
            }

			
			//WE NEED TO CAREFULLY SUBTRACT 1 AT A TIME IF USING THIS METHOD
			self.abstractCreature.GetAbsBelly().myFoodInStomach -= 1;

			//SUBTRACT FROM THE POINT COUNTER TOO
			if (self.abstractCreature.world.game.IsStorySession && self.AI == null)
				self.abstractCreature.world.game.GetStorySession.saveState.totFood -= 1;
			else if (self.abstractCreature.world.game.IsArenaSession)
				self.room.game.GetArenaGameSession.arenaSitting.players[self.playerState.playerNumber].AddSandboxScore(-1);

            //self.room.game.GetArenaGameSession.arenaSitting.players[self.playerState.playerNumber].AddSandboxScore(1);

            //WE DON'T CURRENTLY DO THIS, BUT IF ANYONE EVER SUBTRACTS MORE THAN ONE, JUST RUN THE SINGLE MULTIPLE TIMES.
            if (sub > 1)
				self.SubtractFood(sub -1);
		}
		else
		{
			self.abstractCreature.GetAbsBelly().myFoodInStomach -= sub;
			orig.Invoke(self, sub);
		}

		//DON'T LET NEGATIVE CHUB VALUES BE A THING, SINCE WE CAN'T HAVE NEGATIVE FOOD
		if (self.abstractCreature.GetAbsBelly().myFoodInStomach < 0)
			self.abstractCreature.GetAbsBelly().myFoodInStomach = 0;

        CheckBonusFood(self, false); //NOW TRULY SETS OUR BONUS PIP COUNT

        UpdateBellySize(self);
	}
	
	
	
	
	


	public static void AddPersonalFood(Player self, int add)
	{
		
		
		//SPEARMASTER NEEDS TO SLOW DOWN
		if (BellyPlus.needleFatResistance && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && GetChubValue(self) >= 3)//self.abstractCreature.GetAbsBelly().myFoodInStomach >= self.slugcatStats.maxFood)
		{
			if (UnityEngine.Random.value < 0.1f - (GetOverstuffed(self) * 0.015)) //50% IF FULL BELLY. EVEN LESS AS WE GET OVERSTUFFED
				return; //HE SKIPS GAINING THE WEIGHT THIS TIME...
		}

		//CHECK IF WE'RE DISABLED
		int playerNum = self.playerState.playerNumber;
        if ((playerNum == 0 && BPOptions.fatP1.Value == false && self.isNPC) ||
			(playerNum == 1 && BPOptions.fatP2.Value == false) ||
			(playerNum == 2 && BPOptions.fatP3.Value == false) ||
			(playerNum == 3 && BPOptions.fatP4.Value == false) ||
			(self.isNPC && BPOptions.fatPups.Value == false))
			return;
		
		//if (BPOptions.debugLogs.Value)
		//	Debug.Log("----ADDING PERSONAL FOOD " + add + " CURRENT:" + self.abstractCreature.GetAbsBelly().myFoodInStomach); // + " BNS" + self.GetBelly().bonusFoodPoints);
		
		self.abstractCreature.GetAbsBelly().myFoodInStomach += add;
		self.GetBelly().bloated = true;
		if (self.stillInStartShelter) //DON'T FORGET THIS
			self.GetBelly().breakfasted = true;
		//IF WE'RE OVER OUR MAX, STRUGGLE WITH THIS
		if (GetOverstuffed(self) > 0 && self.eatMeat <= 0 && self.graphicsModule != null && self.room != null && self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Spear)
		{
			self.AerobicIncrease(0.9f);
			self.slowMovementStun = 30 + 5 * Math.Min(GetOverstuffed(self), 4);
			self.Blink(30 + 5 * Math.Min(GetOverstuffed(self), 4));
			
			//SWALLOW!
			self.mainBodyChunk.vel.y = self.mainBodyChunk.vel.y + 0.5f * Math.Min(GetOverstuffed(self), 4); //2f;
			self.room.PlaySound(SoundID.Slugcat_Swallow_Item, self.mainBodyChunk.pos, 0.2f + 0.1f * Math.Min(GetOverstuffed(self), 6), 1f);
			(self.graphicsModule as PlayerGraphics).swallowing = 5 * Math.Min(GetOverstuffed(self), 4); //20;
		}
		
		if (self.abstractCreature.GetAbsBelly().myFoodInStomach >= self.slugcatStats.maxFood && add > 0 && self.room != null && self.room.game.cameras[0].hud != null && self.room.game.cameras[0].hud.foodMeter != null && !BellyPlus.VisualsOnly())
		{
			//SHOW THE PIPS TOO
			self.room.game.cameras[0].hud.foodMeter.refuseCounter = 20; //THIS BRIEFLY FORCES THE HUD TO SHOW UP
		}
		UpdateBellySize(self); //OUR BELLY LOOKS AT THIS VALUE, SO WE SHOULD UPDATE IT HERE
	}


	//NOT NEEDED ANYMORE -OKAY BRINGING IT BACK TO FIX SOMETHING ELSE
	public static void Player_AddQuarterFood(On.Player.orig_AddQuarterFood orig, Player self)
	{
        if (BellyPlus.fullBellyOverride) //FOR DRONEMASTER SO WE COULD BRIEFLY FAKE A NON-FULL BELLY
            self.playerState.foodInStomach++;

        //JUST A DIRECT COPY OF THE PART THAT SKIPS IF OUR BELLY IS FULL
        //ACTUALLY, INSTEAD OF RISKING HUD SHENANIGANS WITH THE CIRCLES.... LETS DO IT THE STUPID WAY
        if (self.FoodInStomach >= self.MaxFoodInStomach)
		{
			//if (UnityEngine.Random.value <= 0.25)
			//	self.AddFood(1);
			
			partialPip += 0.25f;
			if (partialPip >= 1f)
			{
                partialPip -= 1f;
                self.AddFood(1);
			}
		}
		else
		{
			orig.Invoke(self);
		}
	}
	

	//DON'T HURL OTHER SLUGCATS ACROSS THE MAP
	public static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
	{
		if (BellyPlus.VisualsOnly() || self.grasps[grasp] == null)
		{
			orig.Invoke(self, grasp, eu);
			return;
		}
		
		if (self.grasps[grasp].grabbed is Spear spear && spear.spearmasterNeedle && spear.spearmasterNeedle_hasConnection)
		{
			BellyPlus.popcornSpearable = 15;
		}
		if ((self.grasps[grasp].grabbed is Player && IsCramped(self.grasps[grasp].grabbed as Player)) || self.grasps[grasp].grabbed is Lizard ) //we can still throw mice, thats fine
		{
			if (BPOptions.debugLogs.Value)
				Debug.Log("GENTLY RELEASE..... ");
			self.ReleaseGrasp(grasp); //GENTLY release them
		}
		else if (IsStuck(self) && (self.grasps[grasp].grabbed is Weapon))
		{
			orig.Invoke(self, grasp, eu); //ORIGINAL
			self.bodyChunks[0].vel.x = 0; //HALT OUR MOMENTUM, WE JUST GAINED A BUNCH OF VELOCITY
			self.bodyChunks[1].vel.x = 0;
		}
		else
		{
			orig.Invoke(self, grasp, eu); //ORIGINAL
		}
        self.GetBelly().ignoreSpears = 0; //END ANY REMAINING TIME ON IGNORING SPEAR PICKUPS
    }


	//I THINK THIS ACTUALLY NEEDS TO BE HANDLED IN THE MAIN ADDFOOD, BECAUSE THERE ARE OTHER WAYS TO GAIN FOOD
	public static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
	{
		//HUD HINTS FOR CERTAIN EDIBLES
		if (((edible is SSOracleSwarmer) || (edible is FireEgg)) && BPOptions.hudHints.Value && self.playerState.foodInStomach >= self.slugcatStats.maxFood && !BellyPlus.neuronHintGiven && self.room.game.cameras[0].hud != null)
		{
			self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("This item can only be swallowed if there are no items stored in your belly"), 0, 200, false, false);
			BellyPlus.neuronHintGiven = true;
		}
		//AddPersonalFood(self, edible.FoodPoints); //NOT NEEDED ANYMORE
		orig.Invoke(self, edible);

        // if (self.stillInStartShelter) //CHECK THIS IN ADDFOOD DUMMY
        // obj.GetBelly().breakfasted = true;

        //if (BPOptions.debugLogs.Value)
        //	Debug.Log("-OBJECT EATEN!  CURR:" + self.CurrentFood + " BONUS:" + BellyPlus.bonusFood + " FOOD POINTS:" + edible.FoodPoints);

        self.GetBelly().frFed = false;
	}


	//A STRIPPED DOWN VERSION OF THE EATMEAT FN THAT ONLY ADDS FOOD, PERSONALLY
	public static void Player_EatMeatUpdate(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
	{
		if (self.grasps[graspIndex] == null || !(self.grasps[graspIndex].grabbed is Creature))
		{
			return;
		}
		
		SlugcatStats.Name origClass = self.SlugCatClass;
		bool foodLover = IsFoodLover();
		if (foodLover)
		{
			if (SlugcatStats.SlugcatCanMaul(self.SlugCatClass))
				self.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Artificer; //MAULING IS ALSO HANDLED HERE, SO PRETEND WE'RE ARTI IF WE CAN MAUL
			else
				self.SlugCatClass = SlugcatStats.Name.Red;
		}
			

		//THIS JUST RUNS THE SAME COPY OF EATMEATUPDATE EXCEPT IT RUNS WHEN OUR BELLY IS FULL
		if (self.eatMeat > 20 && self.FoodInStomach >= self.MaxFoodInStomach && self.slugcatStats?.name?.value != "SprobParasite") //SKIP THIS FOR THE PARASITE!
		{
			//CORPSE JIGGLE STUFF PROBABLY
			if (self.eatMeat % 5 == 0)
			{
				Vector2 vector = Custom.RNV() * 3f;
				self.mainBodyChunk.pos += vector;
				self.mainBodyChunk.vel += vector;
			}
			Vector2 vector2 = self.grasps[graspIndex].grabbedChunk.pos * self.grasps[graspIndex].grabbedChunk.mass;
			float num = self.grasps[graspIndex].grabbedChunk.mass;
			for (int j = 0; j < self.grasps[graspIndex].grabbed.bodyChunkConnections.Length; j++)
			{
				if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1 == self.grasps[graspIndex].grabbedChunk)
				{
					vector2 += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
					num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
				}
				else if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2 == self.grasps[graspIndex].grabbedChunk)
				{
					vector2 += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
					num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
				}
			}
			vector2 /= num;




			if (self.graphicsModule != null && (self.grasps[graspIndex].grabbed as Creature).State.meatLeft > 0)// && self.FoodInStomach < self.MaxFoodInStomach)
			{
				if (!Custom.DistLess(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos, self.grasps[graspIndex].grabbedChunk.rad))
					(self.graphicsModule as PlayerGraphics).head.vel += Custom.DirVec(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos) * (self.grasps[graspIndex].grabbedChunk.rad - Vector2.Distance(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos));
				else if (self.eatMeat % 5 == 3)
					(self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * 4f;
				
				// if (self.eatMeat > 40 && self.eatMeat % 15 == 3)
				if (self.eatMeat > 40 && self.eatMeat % 21 == 3) //LOL WHAT IS THIS MATH??
				{
					self.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * 4f;
					self.grasps[graspIndex].grabbedChunk.vel += Custom.DirVec(vector2, self.mainBodyChunk.pos) * 0.9f / self.grasps[graspIndex].grabbedChunk.mass;
                    for (int k = UnityEngine.Random.Range(0, 3); k >= 0; k--)
					{
						self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[graspIndex].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(vector2, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
					}
                    if (self.SessionRecord != null)
                    {
                        self.SessionRecord.AddEat(self.grasps[graspIndex].grabbed);
                    }
                    (self.grasps[graspIndex].grabbed as Creature).State.meatLeft--;
					if (ModManager.MSC && (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel || self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand) && !(self.grasps[graspIndex].grabbed is Centipede))
					{
						self.AddQuarterFood();
						self.AddQuarterFood();
					}
					else
					{
						self.AddFood(1);
					}
					self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
					if (foodLover) //THROW THIS IN HERE
						self.SlugCatClass = origClass;
					return;
				}
				if (self.eatMeat % 21 == 3)
					self.room.PlaySound(SoundID.Slugcat_Eat_Meat_A, self.mainBodyChunk);
			}
			
			//THEN, SINCE WE PLAN TO SKIP THE REST OF GRABUPDATE(), TAKE CARE OF A FEW LAST THINGS
			//ACTUALLY I MIGHT BE WRONG ABOUT THIS NOW THAT WE'VE FIXED IT. JUST RESETING THE EATMEAT NUMBER MIGHT BE ALL WE NEED, AND MOST OF THE BELOW STUFF MIGHT BE UNNEEDED
			int num11 = 0;
			if (ModManager.MMF && (self.grasps[0] == null || !(self.grasps[0].grabbed is Creature)) && self.grasps[1] != null && self.grasps[1].grabbed is Creature)
			{
				num11 = 1;
			}

			
			if (self.spearOnBack != null)
			{
				self.spearOnBack.increment = false;
				self.spearOnBack.interactionLocked = true;
			}
			if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
			{
				self.slugOnBack.increment = false;
				self.slugOnBack.interactionLocked = true;
			}
			//RUN THE USUAL, BUT DON'T CHECK IF OUR BELLY IS FULL
			if (self.grasps[num11] != null && self.eatMeat % 80 == 0 && ((self.grasps[num11].grabbed as Creature).State.meatLeft <= 0)) // || this.FoodInStomach >= this.MaxFoodInStomach))
			{
				self.eatMeat = 0;
				self.wantToPickUp = 0;
				self.TossObject(num11, false); // ??? eu?
				self.ReleaseGrasp(num11);
				self.standing = true;
                //Debug.Log("-EATA DA MEAT");
                self.GetBelly().frFed = false;
			}
			
			//THIS IS THE CHECK THE GAME USES TO SEE WHEN WE SHOULD TOSS MEAT IF WE'RE FULL. JUST DON'T LET IT GET TO THIS POINT
			if (self.eatMeat % 80 == 0)
            {
				self.eatMeat = 20;
				//Debug.Log("-RESET DA MEAT");
			}
			
			if (foodLover)
				self.SlugCatClass = origClass;
			
			//IN THIS CASE, WE ALWAYS WANT TO RETURN HERE OR ELSE THE GAME WILL TRY AND THROW OUR FOOD BEFORE WE'RE DONE WITH IT
			return;
		}
		
		//AND THEN WE RUN THE ORIGINAL
		//WE DON'T RUN THE ORIGINAL FIRST ELSE WE RISK TRIGGERING IT TWICE IF IT PUT US AT A FULL BELLY
		orig.Invoke(self, graspIndex);
		
		if (foodLover)
			self.SlugCatClass = origClass;
	}
	
	
	//IT'S LITERALLY JUST A 3RD COPY OF EATMEATUPDATE BUT FOR CORN... MAN THIS THING SUCKS
	public static void EatCornUpdate(Player self, int graspIndex)
	{
        
        if (self.grasps[graspIndex] == null || !(self.grasps[graspIndex].grabbed is SeedCob))
		{
			return;
		}
		if (self.GetBelly().eatCorn > 20)
		{
			self.standing = false;
			self.Blink(5);
			if (self.GetBelly().eatCorn % 5 == 0)
			{
				Vector2 vector = Custom.RNV() * 3f;
				self.mainBodyChunk.pos += vector;
				self.mainBodyChunk.vel += vector;
			}
			Vector2 vector2 = self.grasps[graspIndex].grabbedChunk.pos * self.grasps[graspIndex].grabbedChunk.mass;
			float num = self.grasps[graspIndex].grabbedChunk.mass;
			for (int j = 0; j < self.grasps[graspIndex].grabbed.bodyChunkConnections.Length; j++)
			{
				if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1 == self.grasps[graspIndex].grabbedChunk)
				{
					vector2 += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
					num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2.mass;
				}
				else if (self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk2 == self.grasps[graspIndex].grabbedChunk)
				{
					vector2 += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.pos * self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
					num += self.grasps[graspIndex].grabbed.bodyChunkConnections[j].chunk1.mass;
				}
			}
			vector2 /= num;
			self.mainBodyChunk.vel += Custom.DirVec(self.mainBodyChunk.pos, vector2) * 0.5f;
			self.bodyChunks[1].vel -= Custom.DirVec(self.mainBodyChunk.pos, vector2) * 0.6f;
			if (self.graphicsModule != null) // && (self.grasps[graspIndex].grabbed as Creature).State.meatLeft > 0 && self.FoodInStomach < self.MaxFoodInStomach)
			{
				if (!Custom.DistLess(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos, self.grasps[graspIndex].grabbedChunk.rad))
				{
					(self.graphicsModule as PlayerGraphics).head.vel += Custom.DirVec(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos) * (self.grasps[graspIndex].grabbedChunk.rad - Vector2.Distance(self.grasps[graspIndex].grabbedChunk.pos, (self.graphicsModule as PlayerGraphics).head.pos));
				}
				else if (self.GetBelly().eatCorn % 5 == 3)
				{
					(self.graphicsModule as PlayerGraphics).head.vel += Custom.RNV() * 4f;
				}
				
				bool eatTimer = self.GetBelly().eatCorn % 21 == 3;
				if (self.GetBelly().frFed)
				{
                    eatTimer = self.GetBelly().eatCorn % 15 == 3; //EAT FASTER IF WE'RE BEING FED
					//OKAY THIS COULD ACTUALLY BE A PROBLEM THOUGH IF SOMEONE WON'T STOP. LETS TRY AND WIGGLE FREE
					//if (self.input[0].IntVec != new IntVector2(0,0))
					//{
					//	self.ReleaseGrasp(0);
					//	self.standing = true;
					//}
					//OKAY THAT WONT WORK... ANOTHER TIME MAYBE
						
                }
					
					
				if (self.GetBelly().eatCorn > 40 && eatTimer) //15 == 3
                {
					self.mainBodyChunk.pos += Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * 4f;
					self.grasps[graspIndex].grabbedChunk.vel += Custom.DirVec(vector2, self.mainBodyChunk.pos) * 0.9f / self.grasps[graspIndex].grabbedChunk.mass;
					for (int k = UnityEngine.Random.Range(0, 3); k >= 0; k--)
					{
						self.room.AddObject(new WaterDrip(Vector2.Lerp(self.grasps[graspIndex].grabbedChunk.pos, self.mainBodyChunk.pos, UnityEngine.Random.value) + self.grasps[graspIndex].grabbedChunk.rad * Custom.RNV() * UnityEngine.Random.value, Custom.RNV() * 6f * UnityEngine.Random.value + Custom.DirVec(vector2, (self.mainBodyChunk.pos + (self.graphicsModule as PlayerGraphics).head.pos) / 2f) * 7f * UnityEngine.Random.value + Custom.DegToVec(Mathf.Lerp(-90f, 90f, UnityEngine.Random.value)) * UnityEngine.Random.value * self.EffectiveRoomGravity * 7f, false));
					}
					if (self.SessionRecord != null)
					{
						self.SessionRecord.AddEat(self.grasps[graspIndex].grabbed);
					}
					// (self.grasps[graspIndex].grabbed as Creature).State.meatLeft--;

					self.AddFood(1);
					
					self.room.PlaySound(SoundID.Slugcat_Eat_Meat_B, self.mainBodyChunk);
					return;
				}
				if (eatTimer)
				{
					self.room.PlaySound(SoundID.Slugcat_Eat_Meat_A, self.mainBodyChunk);
				}
			}
		}
	}
	


    //OK THE FOOD EATING THING IS REALLY GETTING ON MY NERVES. LETS TRY A DIFFERENT METHOD.
    public static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
	{
		if (!self.isNPC && !BellyPlus.VisualsOnly())
		{
			if (BP_GrabUpdate0(self, eu))
				return; //WE'RE BUSY FEEDING SOMEONE!
			BP_GrabUpdate1(self, eu);
		}

		//THIS IS REALLY DUMB BUT IF WE'RE CROUCHING WHEN WE ATTEMPT TO GRAB A PARTNER, IT AUTO EQUIPS THEM TO OUR BACK. AND THAT'S NO GOOD. SO PRETEND WE AREN'T CROUCHING
		bool crawlFix = false;
		if (self.bodyMode == Player.BodyModeIndex.Crawl && !BellyPlus.VisualsOnly())
		{
            crawlFix = true;
			self.bodyMode = Player.BodyModeIndex.Default; 
			//THIS IS THE ONLY BODYMODE CHECK VANILLA GRABUPDATE DOES, SO HOPEFULLY THIS IS FINE TO CHANGE FOR SUCH A BIG UPDATE (well except for something something energy cell)
        }

		//FOR THE SAKE OF CRAFTING FOOD COSTS, PRETEND OUR FOOD IS OUR ACTUAL PERSONAL FOOD FOR A SEC (SO ATTEMPTING TO CRAFT WITH A PERSONAL FOOD VALUE OF 0 WILL FAIL)
		int foodFix = 0;
        if (ModManager.CoopAvailable && !BellyPlus.individualFoodEnabled && self.input[0].pckp && BellyPlus.bonusHudPip > 0 && self.abstractCreature.GetAbsBelly().myFoodInStomach == 0 && !self.isNPC)
		{
			foodFix = self.playerState.foodInStomach; //MAKE IT A SUBTRACTABLE VALUE INSTEAD OF A BOOL, SO IF WE ADDED VALUE DURING CRAFTING, WE KEEP IT
			self.playerState.foodInStomach -= foodFix; // !!! DO NOT LEAVE IT LIKE THIS!!!! IT COUNTS AS STARVING IF A PLAYER DOES NOT MEET THE THRESHOLD!!
        }
		

        int preFreeHand = self.FreeHand(); //WE NEED TO KNOW THIS OTHERWISE IT GETS EL WEIRD
		orig.Invoke(self, eu);
		//IF WE'VE SUCCESSFULLY INTERACTED WITH SOMETHING (LIKE PICKED UP A FRUIT OR SOMETHING) THEN ANYTHING UNDER THIS WILL BE SKIPPED
		//RIGHT?... OR WILL IT?  (PSST, I DON'T THINK IT WILL)

		//OKAY, ORIG IS DONE. WE CAN CHANGE OUR BODYMODE BACK IF WE NEED
		if (crawlFix)
			self.bodyMode = Player.BodyModeIndex.Crawl;
		if (foodFix > 0)
				self.playerState.foodInStomach += foodFix;
				//self.JollyFoodUpdate(); //THIS DOESN'T WORK ON PLAYER 1, DUMMY!! >:(

        if (!self.isNPC && !BellyPlus.VisualsOnly() && self.room != null)
        {
			BP_GrabUpdate2(self, eu);
			BP_GrabUpdate3(self, eu, preFreeHand);
			BP_GrabUpdate4(self, eu);
			BP_GrabUpdate6(self, eu); //FOR DETACHED POPCORN
		}

		BP_GrabUpdate5(self, eu); //WE WANT THIS TO RUN EVEN IN VISUALS ONLY MODE
	}
	
	
	
	public static bool BP_GrabUpdate0(Player self, bool eu)
    {
		
		
		//FIRST TAP
		if (IsFeedPressed(self))
		{
            if (BPOptions.debugLogs.Value)
				Debug.Log("--TRY AND FEED A PLAYER! ");
            //MEAT
            int grsp = 0;
			PhysicalObject obj = null; //ModManager.MMF && 
			//bool twoHanded = false;
			if (obj == null)
			{
				for (int j = self.grasps.Length - 1; j >= 0; j--)
				{
                    //IF IT'S AN EDIBLE OBJECT OR A CREATURE
                    if (self.grasps[j] != null && (self.grasps[j].grabbed is IPlayerEdible || self.grasps[j].grabbed is SeedCob || (self.grasps[j].grabbed is Creature && (self.grasps[j].grabbed as Creature).dead && (self.grasps[j].grabbed as Creature).Template.meatPoints > 0)))
                    {
                        obj = self.grasps[j].grabbed;
                        grsp = j;
                    }
                }
			}
			
			Player myFriend = patch_Player.FindPlayerTopInRange(self, 90f);
			bool fromBehind = false;
			//THIS IS A MORE RELIABLE WAY TO MAKE SURE WE DON'T FEED PLAYERS FROM BEHIND
			if (myFriend != null && obj != null && IsCramped(myFriend))
			{
				if (!IsVerticalStuck(myFriend))
					fromBehind = (self.bodyChunks[0].pos.x < myFriend.bodyChunks[0].pos.x) == (myFriend.flipDirection > 0);
				else
                    fromBehind = (self.bodyChunks[0].pos.y < myFriend.bodyChunks[0].pos.y) == (myFriend.bodyChunks[0].pos.y > myFriend.bodyChunks[1].pos.y);
            }
			
			if (myFriend != null && obj != null && !fromBehind) //CHECK FROMBEHIND INSTEAD. THE OTHER ONE IS FLAWED
                //&& !(IsCramped(myFriend) && PipeStatus(myFriend) == PipeStatus(self))) //NO YOU CAN'T FEED THEM FROM BEHIND...
            {
				//self.wantToGrab = 0; //WILL ATTEMPT TO REGRAB VIA BUFFER IF WE GRAB AND THROW TOO QUICKLY
				self.wantToPickUp = 0; //DON'T CONFUSE THIS WITH wantToGrab
				self.dontGrabStuff = 15; //THESE MIGHT NOT BE NEEDED NOW THAT WE FIXED IT 
                self.GetBelly().frFeed = myFriend;
				myFriend.GetBelly().frFed = true;
                //GUESS WE GOTTA RELEASE IT FIRST
                self.ReleaseGrasp(grsp);
                //self.ReleaseObject(grsp, eu);
                //Debug.Log("- THEIR GRASP! " + myFriend.FreeHand());

                //obj.firstChunk.HardSetPosition(self.bodyChunks[0].pos); //THIS DOESN'T ALWAYS GET IT CLOSE ENOUGH FOR THEM TO GRAB IT
                obj.firstChunk.MoveFromOutsideMyUpdate(eu, myFriend.bodyChunks[0].pos);
				obj.firstChunk.vel *= 0f;

                //FORCE THEM TO GRAB OUR OBJECT 
                // myFriend.Grab(obj, 1, obj.mainBodyChunk, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, true, false);
                myFriend.SlugcatGrab(obj, Math.Abs(myFriend.FreeHand()));
				self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Complete, self.mainBodyChunk, false, 1.3f, 1f); //Slugcat_Regain_Footing
                self.room.PlaySound(SoundID.Slugcat_Down_On_Fours, self.mainBodyChunk, false, 1.4f, 1f);
                self.slowMovementStun = 10;
            }
		}
		
		if (self.GetBelly().frFeed != null)
		{
			Player myFriend = self.GetBelly().frFeed;
            //Debug.Log("--continue frFeed " + self.input[0].thrw + " - " + self.input[0].pckp + " - " + myFriend.GetBelly().frFed);

            if (IsFeedHeld(self) && myFriend.GetBelly().frFed)
			{
				myFriend.GetBelly().frFed = true;
				myFriend.swallowAndRegurgitateCounter = 75;
				myFriend.Blink(10);
				if (myFriend.eatCounter > 10)
					myFriend.eatCounter = 10;
                //GRAVITATE TOGETHER
                self.bodyChunks[0].vel.x += 1f * (self.bodyChunks[0].pos.x < myFriend.bodyChunks[0].pos.x ? 1 : -1);
				myFriend.bodyChunks[0].vel.x -= 1f * (self.bodyChunks[0].pos.x < myFriend.bodyChunks[0].pos.x ? 1 : -1);
                //MOVE OUR HANDS TOWARDS THEM -NOT EVEN CLOSE BUT WHATEVER IT'S MOVEMENT
                //WE MOVED THIS TO PATCH_SLUGCATHANDS WHERE IT BELONGS

                return true;
			}
				//THINGS
			else //WE STOPPED HOLDING THE KEYS, CANCEL THE STUFF
			{
                Debug.Log("--ENDING frFeed " + self.input[0].thrw + " - " + self.input[0].pckp + " - " + myFriend.GetBelly().frFed);
                self.GetBelly().frFeed = null;
				myFriend.GetBelly().frFed = false; //THIS SHOULD TURN OFF BY ITSELF I THINK
				
			}
		}

		//DON'T CONTINUE TRYING TO EAT IF WE'RE ALSO HOLDING THROW. WE'RE PROBABLY TRYING TO FEED SOMEONE
		if (self.input[0].thrw)
		{
			if(self.eatCounter > 1)
                self.eatCounter--;
            if (self.eatMeat > 1)
                self.eatMeat--;
        }
			

        return false; //WE'RE NOT DOING ANY OF THAT. CONTINUE AS USUAL
	}
	
	


	public static void BP_GrabUpdate1(Player self, bool eu)
    {
		//SLUGONBACK UPDATE IS RUN HERE, BUT INCRIMENT CHECKS ARE DECIED AFTER
		//NEVER ALLOW US TO STOW OUR PARTNER TO OUR BACK IF THEY ARE STUCK - OR PULLING!!
		if (self.slugOnBack != null)
		{
			Creature crit = GetGraspedCreature(self);
			if (crit != null && crit is Player myPartner && (IsCramped(myPartner) || IsGraspingStuckable(myPartner)))
				self.slugOnBack.increment = false;
		}
	}


	public static void BP_GrabUpdate2(Player self, bool eu)
	{
		//DO THE HINT MESSAGES IF WE JUST PICKED UP A FRUIT! AND ARE PHAT
		if (BPOptions.hudHints.Value && !BellyPlus.smearHintGiven && (BellyPlus.backFoodHint2 || !BPOptions.backFoodStorage.Value) && self.room.game.cameras[0].hud != null
			&& self.pickUpCandidate != null && IsFoodSmearable(self.pickUpCandidate)
			&& (GetChubValue(self) >= 3 && UnityEngine.Random.value <= 1f)
			&& self.input[0].pckp && !self.input[1].pckp)
		{
			self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Some fruits can be smeared on you to help you slip through gaps easier."), 0, 200, false, false);
			self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("While stuck, repeatedly tap the grab button with fruit in hand to smear it on yourself."), 0, 200, false, false);
			BellyPlus.smearHintGiven = true;
		}
	}

	public static void BP_GrabUpdate3(Player self, bool eu, int preFreeHand)
	{
        

        //WANTS TO PICKUP WILL ALWAYS BE 0 IF AN ITEM WAS IN RANGE, EVEN IF WE COULDNT PICK IT UP
        if (BPOptions.backFoodStorage.Value && self.input[0].pckp && !self.input[1].pckp && self.pickUpCandidate != null && self.pickUpCandidate is IPlayerEdible && self.Grabability(self.pickUpCandidate) == Player.ObjectGrabability.OneHand && self.GetBelly().foodOnBack != null) //self.wantToPickUp > 0 
		{
			// Debug.Log("--PICKUP CANDIDATE " + preFreeHand);
			//IF WE'RE HOLDING TWO ITEMS OR ONE BIG TWO HANDED ITEM
			//if ((self.grasps[0] != null && self.grasps[1] != null) || (self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands || self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.Drag)))
			if (preFreeHand == -1) //LETS TRY THIS INSTEAD
			{
				if (!self.GetBelly().foodOnBack.HasAFood)
				{
					if (self.pickUpCandidate is IPlayerEdible)
					{
						// Debug.Log("--WE WANT THIS FOOD TO GO STRAIGHT TO OUR BACK (unless it's occupied) ");
						if (self.GetBelly().foodOnBack.FoodToBack(self.pickUpCandidate)) //IDK MAN, IT'S HERE SOMEWHERE...
						{
							self.wantToPickUp = 0;
							return;
						}
					}
				}
			}
		}


        //CHECK FOR THE BACK FOOD STOWE
        bool backFoodBtn = IsBackFoodPressed(self);
        if (BPOptions.backFoodStorage.Value && self.GetBelly().foodOnBack != null && ((self.input[0].pckp && !self.input[1].pckp && self.pickUpCandidate == null && !IsBackFoodBtnBound(self)) || backFoodBtn)) // && self.switchHandsProcess == 0f)
		{
			bool flag4 = true; // self.grasps[0] != null || self.grasps[1] != null;
			if (self.grasps[0] != null && (self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.TwoHands || self.Grabability(self.grasps[0].grabbed) == Player.ObjectGrabability.Drag))
				flag4 = false;

			//WE SHOULD ONLY RUN THIS UNDER A FEW CONDITIONS:
			//1. HANDS EMPTY: RUN THE CHECK, AND TRIGGER THE SWAP HANDS COUNTER
			//2. 1 ITEM HELD: RUN THE CHECK BUT DON'T TRIGGER THE COUNTER. WE ALREADY TRIGGERED IT ONCE NORMALLY
			//3. 2 ITEMS HELD: RUN THE CHECK. DONT TRIGGER THE COUNTER. ONLY TRIGGER THE SWAP TO MOVE FOOD TO BACK
			//4. HOLDING A TWO HANDED ITEM: DON'T RUN ANYTHING AT ALL
			//SO... THIS MIGHT WORK AS IS WITH THE CHECK FOR 14

			//DO THE HINT MESSAGES IF OUR HANDS ARE FULL AND ONE OF THEM IS A FOOD ITEM!
			if (BPOptions.hudHints.Value && !BellyPlus.backFoodHint1 && self.room.game.cameras[0].hud != null
				&& self.pickUpCandidate != null && self.grasps[0] != null && self.grasps[1] != null
				&& (self.grasps[0].grabbed is IPlayerEdible || self.grasps[1].grabbed is IPlayerEdible))
			{
				self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Food items can be stored on your back for later by double-tapping the grab button"), 0, 200, false, false);
				BellyPlus.backFoodHint1 = true;
			}


			// Debug.Log("--SWITCH HANDS? " + flag4 + " - " + self.switchHandsCounter);
			
			if (flag4 && (backFoodBtn || self.input[0].IntVec == new IntVector2(0, 0)))
			{
				
				if ((self.switchHandsCounter == 0 || self.switchHandsCounter == 14) && !IsBackFoodBtnBound(self)) //ALSO CHECK FOR 14, SINCE IT WILL BE THAT NUMBER IF ACTIVATED THIS FRAME
				{
					self.switchHandsCounter = 15;
				}
				else
				{
					//THAT WAS THE TRIGGER TO SWAP HANDS!
					if (self.GetBelly().foodOnBack.HasAFood)
					{
						// Debug.Log("-DOUBLE TAPPED - FOOD TO HAND! ");
						self.GetBelly().foodOnBack.FoodToHand(eu);
                        return;
					}
					else if (IsStuckOrWedged(self) == false)
					{
						// Debug.Log("-DOUBLE TAPPED - FOOD TO BACK! ");
						self.GetBelly().foodOnBack.counter = 21; //THIS FORCES FOODTOBACK
						self.GetBelly().foodOnBack.Update();
						return;
					}
				}
			}
		}
	}

	public static void BP_GrabUpdate4(Player self, bool eu)
	{
		//CHECK IF WE SHOULD CRUSH FRUIT ON OURSELVES
		if (self.input[0].pckp && !self.input[1].pckp)
		{
			//APPLY LATHER, IF POSSIBLE!
			if (IsStuckOrWedged(self))
			{
				if (CheckApplyLather(self, self))
				{
					// Debug.Log("LATHER! ");
					ObjApplySlickness(self);
				}
				return;
			}
		}

		//AND SPEARMASTER STUFF TOO
		if (BPOptions.detachNeedles.Value && ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && (GetLivingPlayers(self) > 1 || self.CurrentFood >= self.slugcatStats.foodToHibernate) && self.switchHandsProcess >= 0.5f)
		{
			for (int j = self.grasps.Length - 1; j >= 0; j--)
			{
				if (self.grasps[j] != null && self.grasps[j].grabbed is Spear && (self.grasps[j].grabbed as Spear).spearmasterNeedle_hasConnection)
				{
					(self.grasps[j].grabbed as Spear).spearmasterNeedle_hasConnection = false;
					//this.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, base.firstChunk, false, 0.8f, 1.6f + Random.value / 10f);
					PlayExternalSound(self, SoundID.Lizard_Jaws_Shut_Miss_Creature, 0.5f, 1.6f + UnityEngine.Random.value / 10f);
				}
			}
		}
	}

	public static void BP_GrabUpdate5(Player self, bool eu)
	{
		//OVEREATING! (NPCS CAN DO THIS TOO)
		bool flag = self.input[0].x == 0 && self.input[0].y == 0 && !self.input[0].jmp && !self.input[0].thrw && (self.mainBodyChunk.submersion < 0.5f || self.isRivulet);
		if (flag)
		{
			int num3 = -1;
			int num5 = 0;
			while (num3 < 0 && num5 < 2)
			{
				if (self.grasps[num5] != null && self.grasps[num5].grabbed is IPlayerEdible && (self.grasps[num5].grabbed as IPlayerEdible).Edible)
				{
					num3 = num5;
					self.swallowAndRegurgitateCounter = 0; //SO GOURMAND DOESNT FABRICATE STUFF WHILE WE'RE OVEREATING
				}

                //SOME SPECIALTIES FOR THE OUTSIDER
    //            if (self.slugcatStats?.name?.value == "Outsider") // && self.FoodInStomach >= self.MaxFoodInStomach)
				//{
    //                Debug.Log("BP - CAMERA TRIGGER FAILED TO SWITCH " + self.swallowAndRegurgitateCounter);
    //                if (self.swallowAndRegurgitateCounter >= 29 && self.grasps[num5] != null && (self.grasps[num5].grabbed is FlareBomb || self.grasps[num5].grabbed is FirecrackerPlant || self.grasps[num5].grabbed is FlyLure || self.grasps[num5].grabbed is BubbleGrass || self.grasps[num5].grabbed is PuffBall))
    //                {
				//		self.SwallowObject(num5);
    //                    self.swallowAndRegurgitateCounter = 0;
    //                    (self.graphicsModule as PlayerGraphics).swallowing = 20;
				//		return;
    //                }
    //            }

                num5++;
			}

			if (num3 > -1 && self.wantToPickUp < 1 && (self.input[0].pckp || self.eatCounter <= 15) && self.Consious && Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 3.6f))
			{
				if (true) //BUT WHAT IF WE JUST... CUT THAT ALL OUT!~
				{
					//WAIT, SHOULDN'T THE ORIGINAL CALL THIS??? I DON'T ACTUALLY UNDERSTAND WHY THIS ISN'T RUNNING IN ORIG. IT SHOULD...
					//OH! IT ONLY RUNS WHEN EATCOUNTER REACHES 0, AND THIS IS THE FIRST PART IT REACHES BEFORE THAT HAPPENS...
					//SAINT'S SPECIAL ABILITY TO POP HANDHELD CREATURES!
					if (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint && (self.KarmaCap == 9 
						|| (self.room.game.IsArenaSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType != MoreSlugcatsEnums.GameTypeID.Challenge) 
						|| (self.room.game.session is ArenaGameSession && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == MoreSlugcatsEnums.GameTypeID.Challenge && self.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.challengeMeta.ascended)) 
						&& self.grasps[num3].grabbed is Fly && self.eatCounter < 1
						&& !self.GetBelly().frFed //FORCE FEEDING US 
                        && !IsFoodLover()) //SKIP IF FOOD-LOVER IS ENABLED! SAIMNT CAN EAT EM
					{
                        //ORIG WILL HANDLE THIS NEXT TICK
                        if (self.room.game.IsArenaSession)
						{
							//NO DON'T MAKE US FATTER PLS. UNDO THE UPCOMING FOOD GAIN BUT STILL GIVE US A POINT
							self.SubtractFood(1);
                            self.room.game.GetArenaGameSession.arenaSitting.players[self.playerState.playerNumber].AddSandboxScore(1);
                        }
                    }


                    else if (self.eatCounter < 1)
					{
						//HEY DON'T FORGET THIS PART
						if (self.spearOnBack != null)
							self.spearOnBack.increment = false;
						if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
							self.slugOnBack.increment = false;
						
						self.eatCounter = 15;
						self.BiteEdibleObject(eu);
					}
				}
			}
		}
	}

	
	//MY FOOLISH ATTEMPT TO MAKE SEEDCOBS EDIBLE LIKE MEAT EATERS EATING CORPSES
	public static void BP_GrabUpdate6(Player self, bool eu)
	{
        //SPEARMASTER CAN'T DO THIS
		if (BPOptions.detachablePopcorn.Value == false || (ModManager.MSC && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear && !IsFoodLover()))
			return;
		
		
		int myGrasp = 0;
		if (self.input[0].pckp && self.grasps[myGrasp] != null && self.grasps[myGrasp].grabbed is SeedCob seedCob && (seedCob.open > 0.8f) && !seedCob.AbstractCob.dead)
		{
			self.GetBelly().eatCorn++;
			EatCornUpdate(self, myGrasp); //GOOFY CUSTOM METHOD
			if (self.spearOnBack != null)
			{
				self.spearOnBack.increment = false;
				self.spearOnBack.interactionLocked = true;
			}
			if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
			{
				self.slugOnBack.increment = false;
				self.slugOnBack.interactionLocked = true;
			}
			//DON'T CRAFT OR SPIT UP THINGS
			if (self.swallowAndRegurgitateCounter > 0)
				self.swallowAndRegurgitateCounter--;
            return;
		}
		//WE DON'T REALLY NEED TO TOSS IT
		self.GetBelly().eatCorn = Custom.IntClamp(self.GetBelly().eatCorn - 1, 0, 50);
	}
	
	
	
	public static PhysicalObject Player_PickupCandidate(On.Player.orig_PickupCandidate orig, Player self, float favorSpears)
	{
		if (BellyPlus.VisualsOnly())
			return orig.Invoke(self, favorSpears);
		
		//IF WE JUST DROPPED A SPEAR, DON'T PICK UP ANOTHER ONE 
		if (self.GetBelly().ignoreSpears > 0)
			favorSpears -= 40;
		
		PhysicalObject result = orig.Invoke(self, favorSpears);
		
		PhysicalObject myFriend = patch_Player.FindPlayerTopInRange(self, 40f);
		if (myFriend != null && self.CanIPickThisUp(myFriend) && IsStuckOrWedged(myFriend as Player))
        {
			result = myFriend;
		}
		//Debug.Log("ARE YOU FRIEND? " + result + "-" + myFriend + "-" + (myFriend != null && self.CanIPickThisUp(myFriend)));
		return result;
	}


    private static void Player_ReleaseObject(On.Player.orig_ReleaseObject orig, Player self, int grasp, bool eu)
    {
        if (self.grasps[grasp].grabbed is Spear)
            self.GetBelly().ignoreSpears = 40;
		else
            self.GetBelly().ignoreSpears = 0;

        orig(self, grasp, eu);
    }
	
	
	public static bool IsFeedPressed(Player player)
    {
        if (BellyPlus.improvedInputEnabled)
            return player.WantsToFeed();
        else
			return player.input[0].thrw && !player.input[1].thrw && player.input[0].pckp;
    }

    public static bool IsFeedHeld(Player player)
    {
        if (BellyPlus.improvedInputEnabled)
            return player.HoldingFeed() || (player.input[0].thrw && player.input[0].pckp); //WEIRD HOW THIS ONE DIDN'T WORK WITHOUT THE SECOND CHECK? HUH...
        else
            return player.input[0].thrw && player.input[0].pckp;
    }

    public static bool IsBackFoodPressed(Player player)
    {
        if (BellyPlus.improvedInputEnabled)
            return player.WantsBackFood();
        else
			return false;
    }
	
	public static bool IsBackFoodBtnBound(Player player)
    {
        if (BellyPlus.improvedInputEnabled)
            return CheckFoodBtnBound(player);
        else
			return false;
    }

    public static bool CheckFoodBtnBound(Player player)
    {
        return RWInputMod.BackFoodBound(player);
    }


    //HAPPENS IN PLACE OF JUMP
    public static void Heave(Player self)
	{
		
	}

 //   public static void Player_Jump(On.Player.orig_Jump orig, Player self)
	//{
 //       Debug.Log("-----JUMP PRESSED!: " + self.canJump);
 //       orig.Invoke(self);
 //   }
    //IF WE'RE STUCK, DON'T JUMP. SPACEBAR IS FOR SOMETHING ELSE NOW
    //I THINK THIS IS UNUSED NOW THAT WE IL-HOOKED SELF.WANTSTOJUMP FOR BETTER COMPATIBILITY
    
	public static void Player_Jump(On.Player.orig_Jump orig, Player self)
	{
        if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self);
			return;
		}
		
		//Debug.Log("-----JUMP PRESSED!: " + self.canJump);
		
		if (!(IsStuck(self) || self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther || self.GetBelly().boostCounter > 0) )
        {
            //MOVED TO HEAVE() - NEVERMIND IT'S BACK
            //
			
            //Debug.Log("RUN ORIGINAL JUMP!!: " + self.Malnourished + " AEROBIC "+ self.aerobicLevel);
			bool onBeamTip = self.animation == Player.AnimationIndex.BeamTip;//WE NEED TO DECLARE THIS EARLY, ITS ABOUT TO CHANGE.
            bool climbBeam = (self.animation == Player.AnimationIndex.ClimbOnBeam);
            bool standBeam = (self.animation == Player.AnimationIndex.StandOnBeam);
            bool specialJump = (self.animation == Player.AnimationIndex.Flip || self.animation == Player.AnimationIndex.Roll || self.animation == Player.AnimationIndex.BellySlide);
            //TRY TO GUESS IF WE'RE TRYING TO LINE OURSELVES UP WITH A PIPE BELOW US FROM A BEAM TIP
            if ((onBeamTip || (climbBeam && self.input[0].y < 1)) && self.input[0].x == 0)
            {
                //MOVED THIS TO A METHOD
                DeadFall(self);

                //WE NEED TO PRETEND THAT WE WERE HOLDING DOWN THIS FRAME OR THE GAME ADDS RANDOM DRIFT TO OUR JUMP :/
                if (climbBeam)
                    self.input[0].y = -1;
            }

			if (onBeamTip)
				self.GetBelly().weightless = 5; //SO JUMPING FROM BEAM TIPS ISN'T NEARLY AS PUNISHING

            orig.Invoke(self);
			//self.Jump();

			//INCREASING THIS VALUE SHOULDN'T CHANGE THE GAME, BUT WILL EXTEND THE RANGE OUR POUNCE SHOVES ARE REGISTERED
			if (self.simulateHoldJumpButton == 6)
                self.simulateHoldJumpButton = 10;


            //IF WE'RE REAL FAT, LOWER OUR JUMP (INCURS WAY EARLY WITH EASILY WINDED. SLIGHTLY EARLIER IN HARD MODE)
            // float stuffing = GetOverstuffed(self) + ((GetChubFloatValue(self) * (BPOptions.easilyWinded.Value ? 1.5f : 1)) - (BPOptions.hardMode.Value ? 2 : 3) - (self.gourmandExhausted ? 2 : 0));
            float stuffing = GetOverstuffed(self) + ((GetChubFloatValue(self)) - (BPOptions.easilyWinded.Value ? 1 : 3) - (self.gourmandExhausted ? 2 : 0));
            //float preStuffing = stuffing;
            //WITH THIS SETUP, WEIGHT PENALTY CAPS AT 14 (OR LIKE 7+ EXTRA FOOD PIPS)
            stuffing *= (1 + (Mathf.Min(BPOptions.bpDifficulty.Value * 1.2f, 0) / 10f)); //LET EASY MODE PLAYERS GET UP MUCH EASIER 
                                                                                         //Debug.Log("-----STUFFING TEST!: " + stuffing + " PRE: " + preStuffing);

            if (self.slugcatStats?.name?.value == "Cloudtail")
                stuffing = Mathf.Max(0, (stuffing - 5) * 0.8f);

            //OR IF WE'RE CARRYING A FAT PARTNER!
            // if (BellyPlus.jollyCoopEnabled)
            Player backPlayer = GetHeaviestPlayerOnBack(self); //, playerNum);
            if (backPlayer != null)
                stuffing = Mathf.Max(stuffing, GetOverstuffed(backPlayer) + GetChubFloatValue(backPlayer) - 2);

            //Debug.Log("-----STUFFING!: " + stuffing);
            //DON'T LOWER OUR JUMP IF IT'S FROM A BEAM TIP. IT'S REALLY EASY TO SOFTLOCK PEOPLE THAT WAY
            if (stuffing > 0 && !onBeamTip && self.simulateHoldJumpButton <= 0)
            {
                //OKAY JUST SO OUR MODIFIERS AREN'T MADE OBSOLETE, LET'S CAP THINGS AT THE TOP HERE INSTEAD OF AT THE END
                stuffing = Mathf.Min(stuffing, 14); //THIS SCALES, SO LOWER DIFFICULTIES STILL HAVE A HIGHER RANGE
                                                    //WAIT BUT WE SHOULD STILL ALSO CAP AT THE BOTTOM TOO, IN CASE EASILY WINDED ADDED A TON OF PENALTY. THAT'S FINE
				
				//5-24-23 POLE AND BEAM JUMPING STILL TOO HARD... RAISE THE MINIMUM INSTEAD OF CONTINUING THE DECLINE
				float poleLn = 1f;
				if (climbBeam || standBeam)
					poleLn = 1.5f;
				
                float powerMod = 1f;
                if (!(self.isNPC && self.isSlugpup))
                    self.GetBelly().corridorExhaustion += Mathf.CeilToInt(Mathf.Min(20 + (stuffing * poleLn) * 3f, 45f));
                float exMod = GetExhaustionMod(self, 45); //60
                if (exMod > 0.5f || self.lungsExhausted)
                    MakeStrainSparks(self, 7);

                if (self.lungsExhausted && stuffing > 2 && UnityEngine.Random.value < 0.3f)
                {
                    powerMod = 0.8f;
                    self.standing = false;
                }
                else
                    self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, exMod / 1f, 0.6f);

                if (self.input[0].x == 0)
                    stuffing *= 0.5f;
                if (standBeam || specialJump) //IF FLIPPING, ROLLING, OR BELLY SLIDING
                    stuffing *= 0.5f;

                //IF WE'RE SPENDING THE LAST OF OUR STAM... GO ALL OUT!
                if (!self.lungsExhausted)
                {
                    if (exMod >= 1f)
                        stuffing *= 0.1f;
                    // else if (self.input[0].x == 0)
                    // stuffing *= 0.5f;
                    //else
                    //	stuffing *= Mathf.Clamp(1.65f - exMod, 0.7f, 1f);
                    //LETS LET THEM STACK


                    stuffing *= Mathf.InverseLerp(1f, 0.2f, exMod);
                }

                //LOWER THE JUMPING MODIFIER I THINK...
                if (BPOptions.easilyWinded.Value)
                    stuffing *= 2f;

                //POLE JUMPING IS WAY TOO HARD
                if (climbBeam)
                    stuffing /= 2f;
				

                //IF THEY'RE POLE CLIMBING,WE NEED THIS. POLES ARE WONKY
                if (stuffing > 3 && climbBeam)
                    self.noGrabCounter = 15; // + Mathf.CeilToInt(stuffing);

                //RIVULET IS GENERALLY DOUBLE OUR JUMP HEIGHT AND ABILITIES, BUT THIS IS GOOD ENOUGH
                float rivScale = self.isRivulet ? 1.5f : 1f;
                stuffing *= rivScale;

                if (self.jumpBoost > 0f)
                    self.jumpBoost = Mathf.Max(self.jumpBoost - (stuffing / 4f), 4f * poleLn * rivScale) * powerMod;
                else
                {
                    //float cap = (self.input[0].x == 0) ? 0.7f : 0.5f;
                    float dampen = Mathf.Max(1f - (0.05f * stuffing), 0.30f * poleLn * rivScale) * powerMod;
                    self.bodyChunks[0].vel *= dampen;
                    self.bodyChunks[1].vel *= dampen;
                }
            }
        }
        else
        {
            //IN THE VERY RARE CASE THAT WE ARE CLIMBING A POLE AND ALSO STANDING ON TOP OF A PIPE ENTRANCE...
            if (IsStuck(self) && self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam && self.GetBelly().inPipeStatus == false && self.GetBelly().stuckVector.y == -1 && self.input[0].y == 0)
            {
                //self.noGrabCounter = Math.Max(2, self.noGrabCounter);
                //Debug.Log("---SITTING ON PIPE? " + IsStuck(self) + " - " + self.bodyMode + " - " + self.GetBelly().inPipeStatus + " - " + self.GetBelly().stuckVector.y);
                //THIS STRAIGHT UP DOESN'T WOOOOORK WKY TH NOT!?!?!
                orig.Invoke(self);
            }

            //Debug.Log("JUMP DISABLED! ");
            return;
        }
	}
	
	
	//FOR JUMPING OFF OF POLES INTO PIPES BELOW
	public static void DeadFall(Player self)
	{
		Vector2 jumpPoint = new Vector2(self.room.MiddleOfTile(self.bodyChunks[0].pos).x, self.bodyChunks[0].pos.y);
		self.bodyChunks[0].HardSetPosition(jumpPoint);
		self.bodyChunks[1].pos.x = self.bodyChunks[0].pos.x;
		self.bodyChunks[0].vel *= 0;
		self.bodyChunks[1].vel *= 0;
	}


	public static bool IsPiggyBacked(Player self)
	{
		return self.onBack != null;
	}

	public static Player GetPlayerOnBack(Player self) //, int playerNum)
	{
		if (self.slugOnBack != null && self.slugOnBack.slugcat != null)
			return self.slugOnBack.slugcat;
		else
			return null;
	}
	
	public static Player GetHeaviestPlayerOnBack(Player self)
	{
        //TIME FOR A MAKEOVER
		return GetHeaviestOnStack(self, false);
	}
	
	public static Player GetHeaviestOnStack(Player self, bool includeSelf)
	{
		Player slug = self;
        Player heaviestPlayer = slug;
        float heaviestWeight = GetChubFloatValue(slug) + GetOverstuffed(slug);
		//LOOP THROUGH ANY SLUGCATS ON OUR BACK
        while (slug != null && slug.slugOnBack != null && slug.slugOnBack.slugcat != null)
        {
            slug = slug.slugOnBack.slugcat;  //IS... THIS LEGAL?
            if ((GetChubFloatValue(slug) + GetOverstuffed(slug)) >= heaviestWeight)
            {
                heaviestWeight = GetChubFloatValue(slug) + GetOverstuffed(slug);
                heaviestPlayer = slug;
            }
        }
		
		//IF THE ANSWER WAS OURSELF, RETURN NULL I GUESS
		if (heaviestPlayer == self && !includeSelf)
			heaviestPlayer = null;

        return heaviestPlayer;
	}
	

	public static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
	{
		if (BellyPlus.VisualsOnly())
			return orig.Invoke(self, obj);
		
		//HOPEFULLY BYPASSES JOLLYCOOP THING....
		if (obj is Player && obj != self && !(obj as Player).dead && IsStuckOrWedged(obj as Player))
		{
			return Player.ObjectGrabability.BigOneHand;
		}
		else if (obj is Lizard && !(obj as Lizard).dead && patch_Lizard.IsStuck(obj as Lizard))
        {
			return Player.ObjectGrabability.TwoHands;
		}
		else if (obj is SeedCob && BPOptions.detachablePopcorn.Value && !self.isNPC)
        {
			return Player.ObjectGrabability.TwoHands;
		}
		else if (obj is Deer der && der.dead && self.CanEatMeat(der) && !self.isNPC)
        {
			return Player.ObjectGrabability.TwoHands;
		}
		else
		{
			return orig.Invoke(self, obj);
		}
	}
	
	
	public static bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
	{
		if (BellyPlus.VisualsOnly() || self.room == null)
		{
			return orig.Invoke(self, obj);
		}
		
		
		bool isPlayer = obj != null && obj is Player && (obj as Player) != self && !(obj as Player).dead;

		//DON'T PICK UP OUR OWN BACK FOOD - OR OTHERS!!
		bool deny = false;
		for (int i = 0; i < self.room.game.Players.Count; i++)
		{
			if (self.room.game.Players[i].realizedCreature != null && self.room.game.Players[i].realizedCreature is Player player) //SAFETY CHECK!
			{
				if ((self.room.game.Players[i].realizedCreature as Player).dead == false && (player.GetBelly().foodOnBack != null && player.GetBelly().foodOnBack.spear == obj))
					deny = true;
			}
		}
		if (deny)
			return false;
		
		//FIX AN EXPLOIT INVOLVING GRABBING FATTER PLAYERS INTO PIPE ENTRANCES
		if (isPlayer && IsCramped(self) && !IsCramped(obj as Player))
			return false;

		if (isPlayer && (IsStuckOrWedged(obj as Creature) || InPullingChain(obj as Player)) && (obj as Player).dead == false )
		{
			//Debug.Log("CAN I PICK THIS UP? " + (!self.GetBelly().pushingOther && !IsPushingOther(obj as Player) && !IsGrabbedByPlayer(obj as Player)) + " - " + self.Grabability(obj));
			if (self.GetBelly().pushingOther <= 0 && !IsPushingOther(obj as Player) && !IsGrabbedByPlayer(obj as Player) && self.FreeHand() != -1) //IDK MAN - && Custom.DistLess(self.bodyChunks[0].pos, player.bodyChunks[0].pos, 20f))
				return true;
			else
				return false;
		}
		//IF WE'RE STUCK, DON'T TRY AND GRAB ANYONE BEHIND US
		else if (isPlayer && (IsStuck(self) || (ObjIsWedged(self) && GetClosestBodyChunk((obj as Player).bodyChunks[0].pos, self) == 0))) //IsPushingOther(obj as Player)
			return false;
		else if (obj is Lizard && patch_Lizard.IsStuck(obj as Lizard) && !((obj as Lizard).grasps[0] != null && (obj as Lizard).grasps[0].grabbed is Player))
			return true;
		else
			return orig.Invoke(self, obj);
	}
	
	
	//IF WE GRAB A FREN, DO A QUICK TELEPORT TO CLOSE IN THE DISTANCE AND AVOID ANY WEIRD SHENANIGANS
	
	public static void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, obj, graspUsed);
			return;
		}
		
		if ((obj is Player targPlayer) && (obj as Creature).Consious) //!(obj as Creature).Stunned)
		{
			//OKAY WE CAN'T JUST GRAB THEM WITH AN OCCUPIED HAND... TRY THIS INSTEAD
			//NOPE THIS CAN'T WORK, WE NEVER CALL THIS IF FREE HAND IS -1
			//Debug.Log("FREE HAND "+ self.FreeHand());
			//if (self.FreeHand() == -1)
			//{
			//	PhysicalObject myItem;
			//	if (self.grasps[0] != null && self.grasps[0].grabbed is PhysicalObject)
			//	{
            //        myItem = self.grasps[0].grabbed;
            //        self.ReleaseGrasp(0); //DROP OUR FIRST GRASP 
            //        Debug.Log("TAKE MY ITEM " + targPlayer.FreeHand());
            //        targPlayer.SlugcatGrab(myItem, targPlayer.FreeHand()); //MAKE THEM TAKE IT (IF THEY CAN)
            //    }
            //}

			//SET OUR HELD ITEM INTO OUR OFF-HAND SO THAT THROWING WILL THROW THE PLAYER FIRST
			if (self.FreeHand() == 1 && graspUsed == 1)
			{
				self.SwitchGrasps(0, 1);
				graspUsed = 0;
            }

			self.GetBelly().targetStuck = 40; //SO WE DON'T LET GO RIGHT AWAY

			//12-10-22 JOLLYCOOP HANDLES THESE CASES BY REPLACING THE ORIGINAL SO WE DON'T GO LIMP. LETS TRY AND FOLLOW IN THOSE FOOTSTEPS...
			if (IsGrabbedByPlayer(self))
				return; //ABORT!

			int chunkGrabbed = 0; //ACCORDING TO JOLLYCOOP, THIS IS SOMETIMES SOMETHING ELSE, BUT IT LOOKS COMPLICATED SO IM SKIPPING IT...
			//UNLESS...
			if (obj is Player && (IsStuck(obj as Player) == false && InPullingChain(obj as Creature)) && (obj as Player).standing == false) //
            {
				chunkGrabbed = GetClosestBodyChunk(self.bodyChunks[0].pos, (obj as Creature)); //INSTEAD OF JUST GRABBING THEIR BOTTOM, LETS GRAB WHATS CLOSEST!
				// Debug.Log("FIND THE CLOSEST CHUNK! ");
			}
			//Debug.Log("I'M A STANDIN? " + (obj as Player).standing + " CHAIN?" + InPullingChain(obj as Creature));
			// Debug.Log("CHUNK GRABBED " + chunkGrabbed);

			self.switchHandsCounter = 0;
			//typeof(Player).GetField("wantToPickUp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(self, 0); //??
			self.noPickUpOnRelease = 20;
			self.Grab(obj, graspUsed, chunkGrabbed, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, true, false);
			
			//ALSO, TO PREVENT SOME CLEVER CHEATERS...
			if ((!IsCramped(obj as Player) || InPullingChain(obj as Player)) && !(obj as Player).isSlugpup && !(obj as Player).dead && GetChubValue(obj as Player) >= 4)
				self.GetBelly().landLocked = true;
		}
		else
        {
			orig.Invoke(self, obj, graspUsed);
		}
		

		//if (obj is Lizard)
		//{
		//	int chunkGrabbed = 0;
		//	self.Grab(obj, graspUsed, chunkGrabbed, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, true, false);
		//	if (ObjIsStuck(obj as Lizard))
		//		self.grasps[0].pacifying = false;
		//}
	}
	
	
	//GONNA TRY A WHOLE NEW VERSION, FOR COMPATABILITY SAKE
	//ALRIGHT THIS MIGHT BE OUTTA MY LEAGUE
	/*
	public static void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
	{
		orig.Invoke(self, obj, graspUsed);
		
		if (BellyPlus.VisualsOnly())
			return;
		
		if ((obj is Player) && (obj as Creature).Consious)
		{
			// obj.GetBelly().targetStuck = 40; //SO WE DON'T LET GO RIGHT AWAY - WAIT SURELY THIS DOESN'T WORK OR WE'D DROP THEM BEFORE WE COOULD PIGGYBACK
			int chunkGrabbed = 0;
			// Debug.Log("FIND THE CLOSEST CHUNK! ");
			if (obj is Player && (IsStuck(obj as Player) == false && InPullingChain(obj as Creature)) && (obj as Player).standing == false) //
				chunkGrabbed = GetClosestBodyChunk(self.bodyChunks[0].pos, (obj as Creature)); //INSTEAD OF JUST GRABBING THEIR BOTTOM, LETS GRAB WHATS CLOSEST!
			
			self.Grab(obj, graspUsed, chunkGrabbed, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, true, false);
			
			//ALSO, TO PREVENT SOME CLEVER CHEATERS...
			if (!IsCramped(obj as Player) && !(obj as Player).isSlugpup)
				obj.GetBelly().landLocked = true;
		}
	}
	*/

	public static void Player_SpitOutOfShortCut(On.Player.orig_SpitOutOfShortCut orig, Player self, IntVector2 pos, Room newRoom, bool spitOutAllSticks)
	{
		orig.Invoke(self, pos, newRoom, spitOutAllSticks);
		
		if (BellyPlus.VisualsOnly())
			return;
		

		if (!self.dead) //DON'T DO ANY OF THIS IF WE'RE DEAD. WE DON'T CARE
        {
			
			self.GetBelly().inPipeStatus = true;

			self.GetBelly().isStuck = false;
			self.GetBelly().boostStrain = 0;
			self.GetBelly().stuckCoords = new Vector2(0, 0);
			self.GetBelly().stuckVector = new Vector2(0, 0);
			//self.GetBelly().lastShortcut = pos;
			//self.GetBelly().clipCheck = 15;
			self.GetBelly().autoPilot = new IntVector2(0, 0);
			self.GetBelly().noStuck = 1;

			//SET OUR X/YFLIP TO MATCH THE PIPE OUTPUT DIRECTION
			self.GetBelly().myFlipValX = newRoom.ShorcutEntranceHoleDirection(pos).x;
			self.GetBelly().myFlipValY = newRoom.ShorcutEntranceHoleDirection(pos).y;

			//??? I HAVE NO IDEA WHY THIS ISN'T WORKING BUT I HAVE A BETTER IDEA ANYWAYS
			//if (self.input[0].IntVec == newRoom.ShorcutEntranceHoleDirection(pos) * -1)
			//{
			//	self.GetBelly().noStuck = 25;
			//	Debug.Log("----REVERSE HOLD! DIR:" + newRoom.ShorcutEntranceHoleDirection(pos) * -1);
			//}

			//MAYBE THIS WILL STOP HELPERS FROM TELEPORTING THROUGH US OUT OF PIPES?
			if (GetChubValue(self) >= 3)
            {
				self.bodyChunks[1].pos += newRoom.ShorcutEntranceHoleDirection(pos).ToVector2() * 5f;
				self.bodyChunks[0].pos += newRoom.ShorcutEntranceHoleDirection(pos).ToVector2() * 5f;

			}


			//HM. I GUESS THESE WERE CAUSING SLUGCATS TO GET TELEFRAGGED INTO THEIR LAST SHORTCUT LOCATION WHEN EXITING A PIPE IF THEY HELD BACK ON THEIR LAST EXIT
			//self.bodyChunks[1].vel = new Vector2(0, 0);
			//self.bodyChunks[0].vel = new Vector2(0, 0);

			self.verticalCorridorSlideCounter = 0;
			self.horizontalCorridorSlideCounter = 0;
			self.GetBelly().timeInNarrowSpace = 100; //ENOUGH TO TRIGGER THE IN-PIPE STATUS
			if (self.slowMovementStun < 3) //SO WE DONT GO CAREENING OUT OF PIPES
				self.slowMovementStun = 3;
			self.canCorridorJump = 0;

			//DON'T STRETCH TORSO IF WE'RE ON SOMEONES BACK
			bool skipResize = false;
			// if (BellyPlus.jollyCoopEnabled)
			if (IsPiggyBacked(self))
				skipResize = true;
			
			if (skipResize == false)
				self.bodyChunkConnections[0].distance /= 2f;

			//CHECK IF WE RE-ENTERED OUR STARTING SHELTER WITH A PARTNER THAT NEVER LEFT
			if (newRoom.abstractRoom.shelter && BellyPlus.AnyPlayersInStartShelter(newRoom))
				self.stillInStartShelter = true;
			
			if (BPOptions.debugLogs.Value)
				Debug.Log("----SHORTCUT EJECT! " + self.playerState.playerNumber + " DIR:" + newRoom.ShorcutEntranceHoleDirection(pos));
			//self.slideCounter = 1; //FOR A BIG BOOST AS WE ARIVE?
		}

	}



	public static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
	{
		//IF AUTO-CORRECT FOR GAPS!
		float num3 = self.lastGroundY - self.firstChunk.pos.y;
		if (num3 > 60f && firstContact && direction.y < 0 && self.input[0].x == 0 && self.input[0].y == -1 && !BellyPlus.VisualsOnly())
		{
			float correction = 0f;
			if (IsTileNarrowFloat(self, chunk, 0.5f, -1))
				correction = 10f;
			else if (IsTileNarrowFloat(self, chunk, -0.5f, -1))
				correction = -10f;
			
			if (correction != 0)
			{
				self.bodyChunks[0].pos.x = self.room.MiddleOfTile(self.bodyChunks[chunk].pos + new Vector2(correction, 0)).x;
				self.bodyChunks[1].pos.x = self.room.MiddleOfTile(self.bodyChunks[chunk].pos + new Vector2(correction, 0)).x;
				self.room.PlaySound(SoundID.Slugcat_Skid_On_Ground_Init, self.mainBodyChunk, false, 0.8f, 1.2f);

				int otherChunk = (chunk == 1) ? 0 : 1;
				self.bodyChunks[chunk].vel = self.bodyChunks[otherChunk].vel;
			}
			
			//COULD BE DANGEROUS WHEN IT COMES TO COMBINING OTHER MODS BUT...
			//RE-RUN THE ENTIRE THING BUT SKIP THE REST OF THIS INSTANCE.
			//self.TerrainImpact(self, chunk, direction, speed, firstContact);
			return;
		}
		
		
		//VISUAL OOMF TO LANDING IMPACT
		if (firstContact && direction.y < 0)
        {
			float velBoost = 1 + Mathf.Min(((GetChubValue(self) + GetScaledOverstuffed(self)) * 0.1f), 2.5f);
			float newSpd = speed * velBoost;
			//DON'T PUSH US INTO THE NEXT TIER OF FALL PUNISHMENTS
			float deathVel = self.isGourmand ? 80f : 60f;
			float stunVel = self.isGourmand ? 40f : 35f;
			
			//FUNNY FALLING SOUNDS
			if (chunk == 1 && newSpd >= 30 && GetOverstuffed(self) > 6)
                self.room.PlaySound(SoundID.Lizard_Heavy_Terrain_Impact, self.mainBodyChunk, false, 1, 1);
            
			if (speed < stunVel && newSpd >= stunVel)
				speed = stunVel - 2f;
			else if (speed < deathVel && newSpd >= deathVel)
				speed = deathVel - 2f;
		}

		//NOW RUN THE ORIG WITH NEW CALCULATIONS
		orig.Invoke(self, chunk, direction, speed, firstContact);
	}




	public static void BP_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, otherObject, myChunk, otherChunk);
			return;
		}
		
		Vector2 initVel = self.mainBodyChunk.vel;
		// Player.AnimationIndex initAnim = self.animation;
        SlugcatStats.Name origClass = self.SlugCatClass;// == MoreSlugcatsEnums.SlugcatStatsName.Gourmand
        bool slugSlammed = false;
        bool transformed = false;

        //CHECK FOR THE WRECKING BALL COMBO
        if (BPOptions.slugSlams.Value && self.tongue != null)
		{
			if (self.tongue.Attached && Mathf.Abs(initVel.x) > 6f && otherObject is Creature && !(otherObject as Creature).dead && self.SlugSlamConditions(otherObject) && (otherObject as Creature).abstractCreature.creatureTemplate.smallCreature == false)
			{
				//IS IT GOURM OR SOME OTHER FATTY
				Player wreckingBall = GetHeaviestPlayerOnBack(self);
				if (self.slugOnBack?.slugcat != null && (self.slugOnBack?.slugcat as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
					wreckingBall = self.slugOnBack?.slugcat as Player;
				//FIIINE. IF WE ARE FAT ENOUGH WE CAN BE THE WRECKING BALL TOO
				if (wreckingBall == null && GetChubValue(self) >= 4)
					wreckingBall = self;
				
				if(wreckingBall != null && (wreckingBall.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand || GetChubValue(wreckingBall) >= 3))
				{
					//SINCE THIS IS ROLL DAMAGE, WE NEED TO BECOME GOURMAND FOR A SEC FOR IT TO APPLY
					// self.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
					// transformed = true;
					// self.animation = Player.AnimationIndex.Roll; //JUST BRIEFLY!
					//GOURMAND'S ROLL DAMAGE CODE TAKES CARE OF ALL THE VELOCITY AND DMG STUFF. DMG IS FLAT 1 SO WE DON'T NEED TO TOUCH OUR SPEED
				
					//ACTUALLY INSTEAD OF DOING ALL THIS WEIRD NONSENSE. WHAT IF WE JUST REPLICATED THE DAMAGE CODE?
					float stuffing = GetOverstuffed(wreckingBall) * (1 + (BPOptions.bpDifficulty.Value / 10f));
					float slamIncrease = 1 + (stuffing * 0.1f);
					if (wreckingBall.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
						slamIncrease += (Mathf.Max(GetChubFloatValue(wreckingBall) * 2f - 3f, 0) * 0.1f);
					float slamDamage = 1f * slamIncrease * ((wreckingBall == self) ? 0.5f : 1f); //IF WE'RE THE BALL, WE DO REDUCED DAMAGE
					Debug.Log("SLUGSLAM WRECKING-BALL BONUS: * " + slamIncrease + " SPEED " + Mathf.Abs(initVel.x));
					
					self.room.ScreenMovement(new Vector2?(self.bodyChunks[0].pos), self.mainBodyChunk.vel * self.bodyChunks[0].mass * 5f * 0.1f, Mathf.Max((self.bodyChunks[0].mass - 30f) / 50f, 0f));
					self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
					(otherObject as Creature).SetKillTag(self.abstractCreature);
					(otherObject as Creature).Violence(self.mainBodyChunk, new Vector2?(new Vector2(self.mainBodyChunk.vel.x * 5f, self.mainBodyChunk.vel.y)), otherObject.firstChunk, null, Creature.DamageType.Blunt, slamDamage, 120f);
					self.mainBodyChunk.vel.Scale(new Vector2(-0.5f, -0.5f));
					if (((otherObject as Creature).State is HealthState && ((otherObject as Creature).State as HealthState).ClampedHealth == 0f) || (otherObject as Creature).State.dead)
					{
						self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.mainBodyChunk, false, 1.7f, 1f);
					}
					else
					{
						self.room.PlaySound(SoundID.Big_Needle_Worm_Impale_Terrain, self.mainBodyChunk, false, 1.2f, 1f);
					}
				
				}
			}
		}
		
		
		//BONUS SLUGSLAM DAMAGE FROM GOURMAND
		if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand || BPOptions.slugSlams.Value)
		{
			//ADHERE TO THE NORMAL SLUG SLAM BASE REQUIREMENTS SO WE DON'T DEAL DAMAGE FROM LIKE 1 INCH OFF THE GROUND
			float fallSpeed = (120 * Mathf.Floor(Mathf.Abs(self.mainBodyChunk.vel.magnitude) / 7f)); //OKAY THIS SUCKS
			float fallHeight = self.lastGroundY - self.firstChunk.pos.y;

			if (otherObject is Creature && !(otherObject as Creature).dead && !(otherObject is Player)
				&& self.animation != Player.AnimationIndex.Roll
				&& self.animation != Player.AnimationIndex.BellySlide
				&& self.animation != Player.AnimationIndex.RocketJump
				&& self.SlugSlamConditions(otherObject)
				// && fallSpeed > 20
				&& fallHeight > 60
				&& (otherObject as Creature).abstractCreature.creatureTemplate.smallCreature == false) //SMALL CREATURES WON'T SLOW OUR FALL! (AND DIE IN 1 HIT ANYWAYS)
			{
				float stuffing = GetOverstuffed(self) * (1 + (BPOptions.bpDifficulty.Value / 10f));
				float slamIncrease = 1 + (stuffing * 0.1f);
				if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
					slamIncrease += (Mathf.Max(GetChubFloatValue(self) * 2f - 3f, 0) * 0.1f);
				self.mainBodyChunk.vel *= slamIncrease;
				Debug.Log("SLUGSLAM BONUS: * " + slamIncrease + " HEIGHT " + fallHeight);
				slugSlammed = true;
			}

			//PROTECT PLAYERS FROM BEING SLUGROLLED
			//THIS WOULD BE A STUPID WAY TO DIE - CHECK FOR THIS, EVEN IF THESE IDIOTS HAVE FRIENDLY FIRE ON
			else if (otherObject is Player && (otherObject as Player).GetBelly().rollingOther > 0)
				return; //DON'T FLATTEN OUR ROLLER
			else if (!CheckFriendlyFire(self) && (self.animation == Player.AnimationIndex.Roll || self.animation == Player.AnimationIndex.BellySlide)
                 && (otherObject is Player || (otherObject is Lizard && patch_Lizard.IsTamed(otherObject as Lizard))))
			{
                return; //FRIENDLY FIRE!
            }
            
        }

		//SPECIAL CASE FOR EVERGREEN
        if (ChunkyEvergreen(self) && ModManager.MSC)
		{
			if (otherObject is Creature && !(otherObject as Creature).dead
				&& self.animation == Player.AnimationIndex.Roll
				//&& self.SlugSlamConditions(otherObject)
				)
			{
				self.SlugCatClass = MoreSlugcatsEnums.SlugcatStatsName.Gourmand;
                transformed = true;
            }
		 }



		Vector2 realRamSpeed = new Vector2(self.bodyChunks[myChunk].vel.x, self.bodyChunks[myChunk].vel.y);
		
		orig.Invoke(self, otherObject, myChunk, otherChunk);

		if (transformed)
		{
			self.SlugCatClass = origClass; //RETURN TO ORIGINAL
			// self.animation = initAnim;
		}

        //MAKE SURE A SLUGSLAM ISN'T ABOUT TO SQUASH US WITH GRAVITY
        if (slugSlammed && self.mainBodyChunk.vel.magnitude >= 40f) //initVel.magnitude < 40f && 
		{
			//self.mainBodyChunk.vel.Scale(new Vector2(-0.5f, -0.5f)); //TOO BOUNCY!
			self.mainBodyChunk.vel = initVel * -0.5f;
			self.bodyChunks[0].vel = self.mainBodyChunk.vel;
			self.bodyChunks[1].vel = self.mainBodyChunk.vel;
		}
		
		//RAMMING SPEED
		if (otherObject is Player && IsStuck(otherObject as Player))
		{
			
			float ramSpeed = 0f;
			BodyChunk bodyChunk = self.bodyChunks[myChunk];
			//this.AI.CollideWithMouse(otherObject as LanternMouse);
			
			if (!IsVerticalStuck(otherObject as Player))
            {
				
				//RAMMING SPEED BONUS
				if (otherChunk == 1 && Mathf.Abs(self.bodyChunks[myChunk].vel.x) > 10f)
				{
                    ramSpeed = Mathf.Abs(self.bodyChunks[myChunk].vel.x);
                    patch_Player.ObjGainStuckStrain(otherObject as Player, ramSpeed * 5f);
					//PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.11f, 1f);
				}

				if (self.bodyChunks[myChunk].pos.x < otherObject.bodyChunks[otherChunk].pos.x)
				{
					bodyChunk.vel.x = Mathf.Min(bodyChunk.vel.x, 0);
					bodyChunk.vel.x -= 0.05f;
				}
				else if (self.bodyChunks[myChunk].pos.x > otherObject.bodyChunks[otherChunk].pos.x)
				{
					bodyChunk.vel.x = Mathf.Max(bodyChunk.vel.x, 0);
					bodyChunk.vel.x += 0.05f;
				}
			}
            
			else
            {
				//Debug.Log("FOUL! " + self.bodyChunks[myChunk].vel.y + " ABS: " + Mathf.Abs(self.bodyChunks[myChunk].vel.y) + " REAL" + realRamSpeed.y);
				if (otherChunk == 1 && self.bodyChunks[myChunk].vel.y < -10f)
				{
                    ramSpeed = Mathf.Abs(self.bodyChunks[myChunk].vel.y);
                    patch_Player.ObjGainStuckStrain(otherObject as Player, ramSpeed * 5f);
					//PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.11f, 1f);
				}
				
				if (self.bodyChunks[myChunk].pos.y < otherObject.bodyChunks[otherChunk].pos.y)
				{
					bodyChunk.vel.y = Mathf.Min(bodyChunk.vel.y, 0);
					bodyChunk.vel.y -= 0.05f;
				}
				else if (self.bodyChunks[myChunk].pos.y > otherObject.bodyChunks[otherChunk].pos.y)
				{
					bodyChunk.vel.y = Mathf.Max(bodyChunk.vel.y, 0);
					bodyChunk.vel.y += 0.05f;
				}
			}
            
			if (ramSpeed != 0f)
            {
				ObjGainBoostStrain(otherObject as Player, 5, 15, 22);
				ObjGainSquishForce(otherObject as Player, 17, 22);
				//1/5 OF FALL SPEED PROGRESS GAIN
				float progBoost = ((ramSpeed * 0.01f) * (1f + GetScaledOverstuffed(self) * 0.05f)) * (ObjIsSlick(self) ? 3f : 1f);
				Debug.Log("PHYSICS BOOST PROGRESS! " + progBoost + " CRASH VEL:" + ramSpeed);
				ObjGainLoosenProg(self, progBoost);

				//WEHH, IM GONNA BE LAZY...
				Vector2 pos = otherObject.bodyChunks[1].pos;
				float lifetime = 6f;
				float innerRad = 8f;
				float width = 6f;
				float length = 15f;
				int spikes = 8;
				ExplosionSpikes myPop = new ExplosionSpikes(self.room, pos, spikes, innerRad, lifetime, width, length, new Color(1f, 1f, 1f, 0.5f));
				self.room.AddObject(myPop);
							
				self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.6f, 0.6f);
				self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Medium, self.mainBodyChunk.pos, 1.4f, 1f);
            }
			

            //AND QUIT DRY HUMPING THEM >:( GEEZ
            if (self.GetBelly().holdJump > 5)
            {
				self.GetBelly().holdJump -= 1f;
				//3-14-23 //THIS MIGHT BE CAUSING ISSUES
				// otherObject.bodyChunks[otherChunk].vel -= self.bodyChunks[myChunk].vel * 3f;
				if (!IsVerticalStuck(otherObject as Player))
                {
					//self.bodyChunks[myChunk].vel.x -= self.GetBelly().myFlipValX * 0.2f;
					self.bodyChunks[0].vel.x -= self.GetBelly().myFlipValX * 0.4f;
					self.bodyChunks[1].vel.x -= self.GetBelly().myFlipValX * 0.5f;
				}
				else
                {
					self.bodyChunks[0].vel.y -= self.GetBelly().myFlipValY * 0.2f;
					//self.bodyChunks[1].vel.y -= self.GetBelly().myFlipValY * 0.2f;
				}
			}
		}
		
		//IF WE BUMP INTO A WEDGED CORRIDOR CRAWLER PLAYER, TURN AROUND SO WE ARENT WIGGLING BACKWARDS AGAINST THEM
		else if (otherObject is Player && ObjIsWedged(otherObject as Player) && myChunk == 1)
		{
			self.corridorTurnDir = self.input[0].IntVec;
			self.corridorTurnCounter = 0;
			self.canCorridorJump = 0;
			//IF OUR FRIEND IS ALSO FACING A FUNNY WAY, FLIP THEM TOO IF WE ARE BOTH TRYING TO GO THE SAME WAY
			if (otherChunk == 0 && (self.input[0].IntVec == (otherObject as Player).input[0].IntVec))
			{
				(otherObject as Player).corridorTurnDir = self.input[0].IntVec;
				(otherObject as Player).corridorTurnCounter = 0;
				(otherObject as Player).canCorridorJump = 0;
			}
		}

        //SLUGBED 
        else if (otherObject is Player bedge && (bedge.emoteSleepCounter > 0.1 || bedge.GetBelly().slugBed != 0 || (bedge.touchedNoInputCounter > 6 && bedge.bodyMode == Player.BodyModeIndex.Crawl) || (GetChubValue(self) > GetChubValue(bedge) && bedge.bodyMode == Player.BodyModeIndex.Crawl)) 
			&& !self.standing && self.bodyChunks[1].ContactPoint.y != -1 && self.bodyChunks[0].ContactPoint.y != -1 && !bedge.isNPC && !self.isNPC && !self.input[0].jmp //THIS IS A GOOD ONE TO MAKE SURE WE AREN'T TRYING TO GET OFF
            && !IsCramped(bedge) && !IsCramped(self) && !bedge.dead && self.bodyChunks[myChunk].pos.y > bedge.bodyChunks[otherChunk].pos.y) //(GetChubValue(self) >= 3 || GetChubValue(bedge) >= 3 || bedge.GetBelly().slugBed != 0) && 
        {
            int targChunk = (self.flipDirection == bedge.flipDirection) ? 1 : 0;
			float strgt = (self.input[0].x == 0 || self.input[5].x == 0) ? 0.25f : 0f;
            self.bodyChunks[1].pos.x = Mathf.Lerp(self.bodyChunks[1].pos.x, bedge.bodyChunks[targChunk].pos.x, strgt);
            self.bodyChunks[1].vel.x *= 0.1f;
            self.bodyChunks[1].vel.y = Mathf.Max(-1, self.bodyChunks[1].vel.y);
			if (myChunk == 1)
				self.bodyChunks[1].pos.y += 3;
			else
                self.bodyChunks[1].pos.y += 1;
			
            //DON'T SLIDE THE OTHER OBJECT AROUND. A LITTLE CHEESY BUT LETS TRY IT
            bedge.bodyChunks[0].vel.x *= 0.1f;
            bedge.bodyChunks[1].vel.x *= 0.1f;

            self.GetBelly().slugBed = 2;
            
			if (self.graphicsModule != null) //BRING OUR GRAPHICS TO FRONT
                self.graphicsModule.BringSpritesToFront();

            //IF THE TOP IS HEAVIER THAN THE BOTTOM
            int myChub = GetChubValue(self) + (GetOverstuffed(self) / 4);
            if (myChub >= 3 && myChub > (GetChubValue(bedge) + 3))
			{
                bedge.GetBelly().slugBed = -2; //SQUASH THE BEDGE
                //self.bodyChunks[0].vel.y = Mathf.Max(-1, self.bodyChunks[0].vel.y);
                //self.bodyChunks[0].pos.y += 1;
            }
				
		}

        //ROLLY POLY!!
        if (otherObject is Player && !self.isSlugpup && !IsCramped(otherObject as Player) && (otherObject as Player).Consious
			&& (GetOverstuffed(otherObject as Player) > 4 || ((otherObject as Player).isGourmand && GetChubValue(otherObject as Player) >= 3))// && self.flipDirection == (otherObject as Player).flipDirection
			&& ((otherObject as Player).rollCounter > 0 || (otherObject as Player).bodyMode == Player.BodyModeIndex.Crawl || (otherObject as Player).bodyMode == Player.BodyModeIndex.Stand)
			&& !IsPushingOther(otherObject as Player)
			&& self.standing && self.bodyChunks[1].ContactPoint.y == -1 && self.input[0].x != 0 //self.input[0].x == (otherObject as Player).flipDirection
			&& !self.IsTileSolid(0, self.input[0].x * 2, 0)
		)
		{
			Player myPartner = otherObject as Player;
			
			myPartner.GetBelly().beingRolled += 2;
			if (self.input[0].x == myPartner.input[0].x) //WILL THIS LOOP ITSELF DUE TO US FALSIFYING THE X INPUT?
				myPartner.GetBelly().beingRolled++;

            //THIS SUGGESTS WE'RE TRYING TO GO DOWN INTO A PIPE. DON'T ROLL US
            if (myPartner.input[0].IntVec == new IntVector2(0, -1))
                myPartner.GetBelly().beingRolled = 0;
			
			int delay = (self.input[0].x == (otherObject as Player).flipDirection) ? 16 : 26;
            if (myPartner.GetBelly().beingRolled >= delay)
			{
				if (myPartner.rollCounter == 0 || myPartner.animation != Player.AnimationIndex.Roll)
					myPartner.animation = Player.AnimationIndex.Roll;
				myPartner.standing = false;
				myPartner.slowMovementStun = 20;
				(otherObject as Player).flipDirection = self.flipDirection;
            }
			
            myPartner.rollCounter = 8;
			myPartner.rollDirection = self.flipDirection;
			myPartner.stopRollingCounter = 0;
            self.GetBelly().rollingOther = 10;
            //Debug.Log("ROLLY POLLY! " + myPartner.animation);
			
            //MOVE OUR HANDS TOWARDS THEM -NOT EVEN CLOSE BUT WHATEVER IT'S MOVEMENT
            if (self.graphicsModule != null && (self.graphicsModule as PlayerGraphics).hands.Length >= 2)
			{
                (self.graphicsModule as PlayerGraphics).hands[0].absoluteHuntPos = myPartner.bodyChunks[otherChunk].pos - Custom.DirVec(self.bodyChunks[0].pos, myPartner.bodyChunks[otherChunk].pos) * 8f;
                (self.graphicsModule as PlayerGraphics).hands[1].absoluteHuntPos = myPartner.bodyChunks[otherChunk].pos - Custom.DirVec(self.bodyChunks[0].pos, myPartner.bodyChunks[otherChunk].pos) * 8f;
			}
		}



		
		if (otherObject is Creature)
		{
			if (IsStuck(self) && (otherObject as Creature).shortcutDelay > 2)
			{
				PipeExitCollide(self, otherObject as Creature, myChunk, otherChunk);
			}
		}
	}




	public static void PipeExitCollide(Creature self, Creature otherObject, int myChunk, int otherChunk)
    {
		//
		float critMass = (otherObject as Creature).TotalMass;
		// if (self.TotalMass >= critMass * 3) //THIS DOESNT WORK SINCE LIZARDS WOULD NEVER BE BUDGED BY SLUGCATS!
		if (critMass <= 0.25f) //SHOUULD COVER BABY NOODLEFLIES, BUT THATS IT...
			return; //TEENY INSECT. DO NOTHING

		//multiplied by 20
		// baby noolde: 3.5  adult: 9.5
		//slugcat 14
		//lizard 18
		// green liz 32
		// mice 8
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("BUMPER CARS " + (critMass * 20));

		if (PipeStatus(self)) // self.GetBelly().inPipeStatus)
		{
			patch_Player.ObjGainStuckStrain(self, critMass * 50);
			ObjSetFwumpFlag(self, 5);
			// PopFree(self, self.GetBelly().stuckStrain, self.GetBelly().inPipeStatus);
		}
		else
		{
			if (self is Player)
            {
				self.GetBelly().stuckStrain = 0;
                self.GetBelly().isStuck = false;
			}
			else
            {
				patch_Lizard.PopFree(self, 10, true);
			}
				

			Vector2 launchVect = GetCreatureVector(self).ToVector2() * -(critMass * 6);
			launchVect.x = Mathf.Clamp(launchVect.x, -20f, 20f);
			launchVect.y = Mathf.Clamp(launchVect.y, -20f, 20f);
			self.Stun(5);
			for (int i = 0; i < self.bodyChunks.Length; i++)
            {
				self.bodyChunks[i].vel = launchVect;
			}
			if (BPOptions.debugLogs.Value)
				Debug.Log("FROM THE TOP ROPE! " + launchVect);

			float soundVol = 0.1f + (critMass / 75f);// 0.17f  //FROM 100
			PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, soundVol, 1f);
		}
	}

    public static void Player_JollyEmoteUpdate(On.Player.orig_JollyEmoteUpdate orig, Player self)
    {
		//LET US SLEEP EMOTE IF WE ARE USING A SLUGBED
		if (self.GetBelly().slugBed > 0 && self.input[0].y < 0 && !self.input[0].jmp && !self.input[0].pckp && self.input[0].x == 0)
		{
			self.emoteSleepCounter += 0.028f;
			if (self.emoteSleepCounter > 1.4f)
				self.sleepCurlUp = Mathf.SmoothStep(self.sleepCurlUp, 1f, self.emoteSleepCounter - 1.4f);
			else
				self.sleepCurlUp = Mathf.Max(0f, self.sleepCurlUp - 0.1f);
		}
		else
			orig(self);
    }



    public static void BP_CheckInput(On.Player.orig_checkInput orig, Player self)
    {
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self);
			return;
		}
		
		
		
		//THIS SEEMS A LITTLE SUS BUT... IT'S ONLY POINTING. I THINK IT SHOULD BE FINE
		bool origCoOpStatus = ModManager.CoopAvailable;
		ModManager.CoopAvailable = true; //BREIFLY PRETEND CO-OP IS ENABLED EVEN IF IT'S NOT. FOR POINTING STUFF
		orig.Invoke(self);
		ModManager.CoopAvailable = origCoOpStatus;

		//ALRIGHT WE GONNA TRY SOMETHING STUPID. PRESS GRAB WHILE PUSHING SOMEONE TO HOLD THE PUSH IN THAT DIRECITON
		
		if (self.GetBelly().autoPilot != new IntVector2(0, 0))
		{
			//Debug.Log("-----AUTO PILOT!: ");
			//CANCEL THE AUTO PILOT BY PRESSING ANY (OTHER) DIRECTION, OR BY PRESSING GRAB AGAIN
			if ((self.input[0].IntVec != new IntVector2(0, 0) && self.input[0].IntVec != self.input[2].IntVec) || self.input[0].pckp && !self.input[1].pckp)
				self.GetBelly().autoPilot = new IntVector2(0, 0);
			else
			{
				//Debug.Log("-----APPLYING AUTO PILOT!: " + self.GetBelly().autoPilot);
				self.input[0].x = self.GetBelly().autoPilot.x;
				self.input[0].y = self.GetBelly().autoPilot.y;
			}
		}

		if (self.input[0].pckp && !self.input[1].pckp && (self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther || self.GetBelly().isStuck) && self.input[0].IntVec != new IntVector2(0, 0))
			self.GetBelly().autoPilot = self.input[0].IntVec;

		//if (self.animation == Player.AnimationIndex.HangFromBeam)
		//Debug.Log("TOUCHED INPUT! " + self.animation + " COUNTER" + self.touchedNoInputCounter + " Y0" + self.input[0].y + " Y1" + self.input[1].y);
		//Debug.Log("TOUCHED INPUT! " + self.animation + " - " + (self.animation == Player.AnimationIndex.HangFromBeam) + (self.input[0].y > 0) + (self.input[1].y != 1) + GetOverstuffed(self) + " - " + (GetOverstuffed(self) > 3) + !self.lungsExhausted);

		//THIS IS THE ONLY PLACE THIS CAN REALLY HAPPEN WITHOUT TOUCHINPUT BEING RESET
		//IF WE'RE HANGING UNDER A VERTICAL BEAM FOR A LONG TIME... GIVE US A STAMINA BONUS SO WE CAN GET UP EVENTUALLY!!!
		if (self.animation == Player.AnimationIndex.HangFromBeam && ((self.input[0].y == 1 && self.input[1].y == 0) || (self.input[0].jmp && !self.input[1].jmp)) && GetOverstuffed(self) > 0 && !self.lungsExhausted)
        {
			self.GetBelly().corridorExhaustion -= Mathf.Clamp(self.touchedNoInputCounter - 30, 0, 400);
			//Debug.Log("TOUCHED  HERE WE GO! " + self.touchedNoInputCounter);
		}
			
	}
	
	
	//CLEAN UP SOME VALUES THAT COULD CAUSE ISSUES FOR OUR CORPSE
	public static void BP_Die(On.Player.orig_Die orig, Player self)
	{
		
		//self.GetBelly().isStuck = false;
		//self.GetBelly().stuckCoords = new Vector2(0, 0);
		//self.GetBelly().stuckStrain = 0;
		self.GetBelly().landLocked = false;

        if (self.GetBelly().foodOnBack != null)
		{
            self.GetBelly().foodOnBack.DropFood();
			//self.GetBelly().foodOnBack = null; //THIS HAS BEEN FIXED NOW, WE DON'T NEED THIS
		}
			
		
		orig.Invoke(self);
	}
	
	
	public static void BP_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
	{
		bool wasStunned = self.Stunned;
		
		//THIS DOESN'T WORK, PERMINANT DNG TRACKING STILL KILLS US
		// if (!BellyPlus.VisualsOnly() && self is Player && BPOptions.fatArmor.Value && type == Creature.DamageType.Stab)
		// {
			// float myFat = (GetOverstuffed(self as Player) + Mathf.Max(GetChubValue(self as Player) - 1f, 0)) * (1 + ((BPOptions.bpDifficulty.Value * 1.2f) / 10f));
			// float saveChance = Mathf.InverseLerp(0, 15, myFat) / 2f;
			// if (UnityEngine.Random.value < saveChance)
			// {
				// damage /= 10f;
				// Debug.Log("LECHONK SAVE! " + saveChance);
			// }
		// }
		
		
		orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
		
		if (BellyPlus.VisualsOnly())
			return;
		
		//UNDO VELOCITY CHANGES DONE TO US IF STUCK (AND NOT DEAD)
		if (directionAndMomentum != null && self.dead == false )
		{
			if (ObjIsStuckable(self) && ObjIsStuck(self)) //ONLY APPLIES TO STUCKABLES
			{
				
				if (hitChunk != null)
					hitChunk.vel -= Vector2.ClampMagnitude(directionAndMomentum.Value / hitChunk.mass, 10f);
				// else if (hitAppendage != null && this is PhysicalObject.IHaveAppendages) //THESE DON'T HAVE THE SAME PHYSICS, SO WE DON'T CARE
				
				//BUT IT CAN BOOST US A BIT IF IT MATCHES OUR DIRECTION...
				Vector2 stuckAngle = ObjGetStuckVector(self);
				bool boost = false;
				if (stuckAngle.x > 0)
					boost = directionAndMomentum.Value.x > 1;
				else if (stuckAngle.x < 0)
					boost = directionAndMomentum.Value.x < -1;
				else if (stuckAngle.y > 0)
					boost = directionAndMomentum.Value.y > 1;
				else if (stuckAngle.y < 0)
					boost = directionAndMomentum.Value.y < -1;
				
				if (boost && !wasStunned)
				{
					int rockBoost = (self is Player) ? 50 : 100;
					patch_Player.ObjGainStuckStrain(self, (damage > 0.1f) ? 150 : rockBoost);
					patch_Player.ObjGainBoostStrain(self, 5, 15, 25);
					ObjGainSquishForce(self, 15, 22);
					//IDK IF THE STUN WILL CANCEL OUT ANY OF THIS
				}
			}
		}
	}
	
	
	
	
	public static float BP_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
	{
		if (BellyPlus.VisualsOnly())
			return orig.Invoke(self);
		
		float fbase = Mathf.Max(GetChubValue(self) - 2f, 0) / 10f;
		float fptwo = (fbase + (GetOverstuffed(self) / 20f)) * (1 + ((BPOptions.bpDifficulty.Value * 1.2f) / 10f));
		float armorMod = BPOptions.fatArmor.Value ? Mathf.Min(fptwo, 0.9f) : 0f;
		if (BPOptions.debugLogs.Value)
			Debug.Log("FAT ARMOR MOD! * " + armorMod);
		
		return (orig.Invoke(self) * (1 - armorMod));
	}
	
	
	public static void BP_Stun(On.Player.orig_Stun orig, Player self, int st)
	{
		if (!BellyPlus.VisualsOnly() && BPOptions.fatArmor.Value && st > 5)
		{
			float fbase = Mathf.Max(GetChubValue(self) - 1f, 0) / 15f; // (0.1 - 0.2)
			float fptwo = (fbase + (GetOverstuffed(self) / 50f)) * (1 + ((BPOptions.bpDifficulty.Value * 1.2f) / 10f));  //(hits 0.50 at 7 overstuffed)
			float stunResist = Mathf.Min(1f - fptwo, 0.5f); //0.35
			orig.Invoke(self, Mathf.CeilToInt(st * stunResist));
		}
		else
			orig.Invoke(self, st);
	}
	
	
	// public override bool SpearStick(Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
	// {
		// bool result = orig(self, source, dmg, chunk, appPos, direction);
		// if (!BellyPlus.VisualsOnly() && BPOptions.fatArmor.Value && result)
		// {
			
		// }
		// return !this.isGourmand || (!(this.animation == Player.AnimationIndex.Roll) && !(this.animation == Player.AnimationIndex.BellySlide));
	// }
	
	
	// public override bool HitSomethingWithoutStopping(PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
	// {
		// float origDmg = self.spearDamageBonus;
		
		// // bool result = orig(self, SharedPhysics.CollisionResult result, bool eu);
		// if (!BellyPlus.VisualsOnly() && BPOptions.fatArmor.Value)
		// {
			// self.spearDamageBonus /= 10f;
			// orig.Invoke(self, obj, chunk, appendage);
			// self.spearDamageBonus = origDmg;
		// }
		// else
			// orig.Invoke(self, obj, chunk, appendage);
		
	// }
	

    public static bool BPSpear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
    {
        if (result.obj == null || BellyPlus.VisualsOnly() || !BPOptions.fatArmor.Value)
            return orig.Invoke(self, result, eu);

        //WE CAN JUST EDIT THE SPEAR DAMAGE, IT'S NOT AN ISSUE. IT RESETS ITSELF EVERYT TIME IT'S PICKED UP OR THROWN
        if ((result.obj is Player) && (result.obj as Creature).SpearStick(self, Mathf.Lerp(0.55f, 0.62f, UnityEngine.Random.value), result.chunk, result.onAppendagePos, self.firstChunk.vel))
        {
            Player myTarget = result.obj as Player;

            float myFat = (GetOverstuffed(myTarget) + Mathf.Max(GetChubValue(myTarget) - 1f, 0)) * (1 + ((BPOptions.bpDifficulty.Value * 1.2f) / 10f));
            float saveChance = Mathf.Lerp(0f, 0.65f, Mathf.InverseLerp(0, 15, myFat));
            if (UnityEngine.Random.value < saveChance)
            {
                self.spearDamageBonus /= 10f;
                Debug.Log("LECHONK SAVE! " + saveChance);
            }
        }
        return orig.Invoke(self, result, eu);
    }


    public static void BPdecreaseRopeLength(On.Player.Tongue.orig_decreaseRopeLength orig, Player.Tongue self, float amount)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, amount);
			return;
		}
		
		float mod = self.player.GetBelly().runSpeedMod;
		
		Player backPlayer = GetHeaviestPlayerOnBack(self.player);
		if (backPlayer != null)
			mod = Mathf.Min(mod, backPlayer.GetBelly().runSpeedMod);
		
		if (self.player.lungsExhausted)
			mod *= 0.8f;


		//OKAY ELASTICITY IS NOT WHAT WE THOUGHT IT WAS. IT'S JUST LIKE A NUMBER THAT STARTS AT 1 AND QUICKLY SHRINKS TO 0 AFTER CONNECTING TO SOMETHING
		bool stretch = false;
		float totalLength = self.rope.totalLength;
        float num2 = Custom.LerpMap(Mathf.Abs(0.5f - self.onRopePos), 0.5f, 0.4f, 1.1f, 0.7f);
        float num3 = self.RequestRope() * Mathf.Lerp(num2, 1f, self.elastic);
        //if (totalLength > num3)
        if (totalLength - num3 > 2f) //MATH!.... OR SOMETHING
        {
			stretch = true;
            
        }
        //Debug.Log("ELASTIC! " + stretch + " - " + (totalLength - num3));

        //CHECK IF WE'RE HOLDING A STUCK PLAYER
        bool pulling = false;
		if (IsGraspingStuckCreature(self.player)) //self.elastic < -0.8f)
		{
            self.player.bodyChunkConnections[0].distance /= 2f;
            
			//BRO. BE CHILL YOUR BODY
            self.player.bodyChunks[0].vel /= 2f;
            self.player.bodyChunks[1].vel /= 4f;
            self.player.bodyChunks[1].vel.x -= self.player.input[0].x;

            if (stretch)
			{
                mod = 0f;
                pulling = true;
                //self.player.standing = false; //GET THOSE FEET OFF THE GROUND
                //self.player.bodyMode = Player.BodyModeIndex.Default;
                if (!self.player.lungsExhausted)
                {
                    Creature myPartner = GetGraspedCreature(self.player);
                    if (ObjIsStuck(myPartner))
                    {
                        ObjGainStuckStrain(myPartner, 2);
                        ObjGainBoostStrain(myPartner, 0, 2, 12);
                        ObjGainLoosenProg(myPartner, 0.0003f * (ObjIsSlick(myPartner) ? 3f : 1f));
                    }
                    MakeStrainSparks(self.player, 1);
                    if (UnityEngine.Random.value < 0.5f)
						self.player.GetBelly().corridorExhaustion += 1;
                }

                //BACK IT UP A LITTLE BIT 
                self.increaseRopeLength(amount / 5f);
            }
			
		}
		
		orig.Invoke(self, amount * (mod * mod));
		if (mod <= 0.65 && !pulling)
		{
			MakeStrainSparks(self.player, 1);
			if (!self.player.lungsExhausted)
				self.player.GetBelly().corridorExhaustion += 2;
		}
			
	}
	
	
	
	
	//IF WE JUST CHEATED THE STUCK STRAIN, MAKE AN ATTEMPT TO POP US BACK INTO PLACE!
	//0 = END STATMENT.   1 = RE-WEDGE   2 = DON'T RE-WEDGE, I GUESS
	public static bool RedirectStuckage(Player self, bool forced, bool eu)
	{
		
		bool inPipe = self.GetBelly().inPipeStatus;
		float posMod = inPipe ? 0.5f : 0f;

		bool backedOut = false;
		if (self.GetBelly().isStuck && self.GetBelly().stuckCoords != new Vector2(0,0)) //&& self.GetBelly().stuckStrain > 0  
        {
			Vector2 newCoords = self.GetBelly().stuckCoords;
			if (!self.GetBelly().verticalStuck)
			{
				//if(self.GetBelly().myFlipValX == 1 && self.bodyChunks[1].pos.x > self.GetBelly().stuckCoords.x)
				//OUR FLIPVAL WILL BE CHANGING WHEN WE TRY AND BACK OUT, AND WE DON'T WANT THAT TO GET US STUCK ON THE WAY BACK (USUALLY)
				// if (self.GetBelly().stuckVector.x == 1 && self.bodyChunks[1].pos.x > self.GetBelly().stuckCoords.x)
				if (self.GetBelly().stuckVector.x == 1 && self.bodyChunks[1].pos.x > self.GetBelly().stuckCoords.x + 5)
					newCoords = new Vector2(newCoords.x - posMod * 10, newCoords.y);
				else if (self.GetBelly().stuckVector.x == -1 && self.bodyChunks[1].pos.x < self.GetBelly().stuckCoords.x - 5)
					newCoords = new Vector2(newCoords.x + posMod * 10, newCoords.y); // Vector2(newCoords.x + 5, newCoords.y); //NO THIS IS TOO FAR FOR OUT OF PIPE SQUEEZES posMod
				else
					backedOut = true;
				//wedgedInFront = true; //WE SET THIS DOWN BELOW NOW
			}
			else
			{
				if(self.GetBelly().stuckVector.y == 1 && self.bodyChunks[1].pos.y > self.GetBelly().stuckCoords.y + 5)
					newCoords = new Vector2(newCoords.x, newCoords.y - posMod * 10);
				if (self.GetBelly().stuckVector.y == -1 && self.bodyChunks[1].pos.y < self.GetBelly().stuckCoords.y - 5)
					newCoords = new Vector2(newCoords.x, newCoords.y + posMod * 10);
				else
					backedOut = true;
				//vertStuck = true;
			}

			if (backedOut && !forced)
			{
				if (BPOptions.debugLogs.Value)
					Debug.Log("WE JUST BACKED OUT!" + self.GetBelly().stuckCoords);
				self.GetBelly().isStuck = false;
				self.GetBelly().verticalStuck = false;
				return true;
			}
			else
			{
				if (BPOptions.debugLogs.Value)
					Debug.Log("REDIRECTING TO STUCK COORDS! " + self.GetBelly().stuckCoords + " CURRENT:" + self.bodyChunks[1].pos + " ADJST:" + newCoords);
				self.bodyChunks[1].MoveFromOutsideMyUpdate(eu, newCoords);
				self.enteringShortCut = null; //STOPS US FROM GETTING SUCKED INTO SHORTCUTS

				//if it's FORCED, WE LIKELY WANT OUR HEAD TO BE REPOSITIONED TOO
				if (forced)
					self.bodyChunks[0].MoveFromOutsideMyUpdate(eu, newCoords + self.GetBelly().stuckVector * 10f);

				self.bodyChunks[0].vel = new Vector2(0, 0);
				self.bodyChunks[1].vel = new Vector2(0, 0);
				// Debug.Log("FWOMP!! EJECTED " + inPipe);
				PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.03f, 1f);
				self.GetBelly().stuckStrain += 4; //MAYBE HELP 
				return false; //FALSE MEANS WE SHOULD RE-WEDGE
			}
		}
		
		//IF WE WERE STUCK IN AN ENTRANCE, POP US BACK JUST A BIT TO PREVENT CHEATING PAST THE BARRIER
		else
		{
			if (self.GetBelly().stuckStrain > 0)
				self.GetBelly().stuckStrain -= 2;
			else
			{
				self.GetBelly().stuckCoords = new Vector2(0, 0); //WE'VE GIVEN UP, 
				self.GetBelly().stuckVector = new Vector2(0, 0);
				self.GetBelly().isStuck = false;
			}
			//Debug.Log("WE AINT STUCK... " + self.GetBelly().stuckCoords + " STRAIN:" + self.GetBelly().stuckStrain);
			return true;
		}
	}





    public static void DoNoirSounds(Player player, bool alt)
    {
		if (player.GetBelly().miscTimer > 0)
			return; //TOO MUCH MEOWING! WAIT A BIT

		if (BellyPlus.noircatEnabled)
			PlayNoircattoFiles(player, alt);
		else
			PlaySolaceFiles(player, alt);
    }

    public static void PlayNoircattoFiles(Player player, bool alt)
    {
		float mass = NoirCatto.NoirCatto.DefaultFirstChunkMass;
		SoundID sound = alt ? NoirCatto.NoirCatto.Meow2SND : NoirCatto.NoirCatto.MeowFrustratedSND;
        PlayNoirMeow(player, alt, mass, sound);
    }

    public static void PlaySolaceFiles(Player player, bool alt)
    {
		float mass = TheFriend.NoirThings.NoirCatto.DefaultFirstChunkMass;
        SoundID sound = alt ? TheFriend.NoirThings.NoirCatto.Meow2SND : TheFriend.NoirThings.NoirCatto.MeowFrustratedSND;
        PlayNoirMeow(player, alt, mass, sound);
    }

    public static void PlayNoirMeow(Player player, bool alt, float bodyMass, SoundID sound)
    {
        float pitch = Mathf.Max(1f - (player.bodyChunks[1].mass - bodyMass) * 0.65f, 0.15f) - 0.1f;
        pitch += (UnityEngine.Random.value * 0.2f);
        if (alt)
        {
            player.room?.PlaySound(sound, player.firstChunk, false, 0.9f, pitch + 0.1f);
        }
        else
        {
            player.room?.PlaySound(sound, player.firstChunk, false, 0.7f, pitch * 0.6f);
        }

        player.GetBelly().miscTimer = 40;
    }





    //WHY ARE YOU WAY UP HERE?? - OH, IT'S BECAUSE WE'RE HOPING WE CAN STILL REFERENCE YOU AGAIN IN LIKE 3 FRAMES...
    public static float crashVel = 0f;
	public static float bonusMomentum = 0f;
	
	public static void CheckStuckage(Player self, bool eu)
	{
		
		bool inPipe = self.GetBelly().inPipeStatus;
		float posMod = inPipe ? 0.5f : 0f;
		
		//CHECK FOR GRACE PERIOD
		if (self.GetBelly().noStuck > 0)
        {
			self.GetBelly().noStuck--;
			self.GetBelly().isStuck = false;
			self.GetBelly().verticalStuck = false;
			//Debug.Log("----NO STUCKS ALLOWED! ");
			return;
		}

		//IF WE'RE BEING PIGGYBACKED, USE OUR PARTNERS INPUT TEMPORARILY...
		IntVector2 myInputVec = self.input[0].IntVec;
		if (IsPiggyBacked(self))
        {
			myInputVec = self.onBack.input[0].IntVec;
		}

		//CHECK IF WE'RE MOVING BACKWARDS, AND FLIP THE POS MODIFIER IF WE ARE --NAH THIS SUCKS - OKAY NVM I GUESS IT WORKS
		IntVector2 myVector = GetCreatureVector(self);
		if (inPipe && myInputVec.x != 0 && myInputVec.x == -myVector.x)
			posMod -= 0.5f;
			//posMod = inPipe ? 0.0f : 0.5f; //FLIPPED!
		else if (inPipe && myInputVec.y != 0 && myInputVec.y == -myVector.y)
			posMod -= 0.5f;
		//Debug.Log("POSMOD! " + posMod);

		//int myxF = Mathf.FloorToInt((self.input[0].x) + (0.2f * self.input[0].x)); //AREA SLIGHTLY IN FRONT OF HIPS
		float myxF = (0.5f + posMod) * self.GetBelly().myFlipValX; //NOO DON'T ADD OUR INPUT
		float myxB = (-0.0f + posMod) * self.GetBelly().myFlipValX; //AREA SLIGHTLY BEHIND HIPS
		//FOR THE Y VERSION
		float myyF = (0.5f + posMod) * self.GetBelly().myFlipValY; //AREA SLIGHTLY IN FRONT OF HIPS
		float myyB = (-0.0f + posMod) * self.GetBelly().myFlipValY; //AREA SLIGHTLY BEHIND HIPS													


		//THREW IT ALL OUT AND REBUILT IT

		//DON'T GET CONFUSED, THESE ARE BOTH FOR THE BOTTOM CHUNK. BUT IT'S CHECKING FOR THE FRONT/REAR OF THE BOTTOM CHUNK
		bool frontInCorridor = false;
		bool rearInCorridor = false;
		bool vertFrontInCorridor = false;
		bool vertRearInCorridor = false;

		//IF PIGGYBACKING, CHECK THIS FROM OUR MOUNTS PERSPECTIVE INSTEAD, AND FROM THE UPPER BODY CHUNK
		if (IsPiggyBacked(self) && self.onBack != null)
		{
			frontInCorridor = IsTileNarrowFloat(self.onBack, 0, myxF, 0);
			rearInCorridor = IsTileNarrowFloat(self.onBack, 0, myxB, 0);
			vertFrontInCorridor = IsTileNarrowFloat(self.onBack, 0, 0, myyF);
			vertRearInCorridor = IsTileNarrowFloat(self.onBack, 0, 0, myyB);
		}
		else
        {
			frontInCorridor = IsTileNarrowFloat(self, 1, myxF, 0);
			rearInCorridor = IsTileNarrowFloat(self, 1, myxB, 0);
			vertFrontInCorridor = IsTileNarrowFloat(self, 1, 0, myyF);
			vertRearInCorridor = IsTileNarrowFloat(self, 1, 0, myyB);
		}


		// bool isVertical = Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) < Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y);
		//USE VERTICAL STUCK CHECKS TO SET THIS ONE, SINCE WE CAN BE HORIZONTAL STUCK WHILE STANDING UP
		//FINALLY!!! I THINK THIS IS THE ONE. HANDLES DIFFERENTLY OUTSIDE PIPES (FOR SIDEWAYS STUCKS) AND CLEANER WHILE INSIDE PIPES
		bool isVertical = false; // (vertFrontInCorridor != vertRearInCorridor);
		if (inPipe)
			isVertical = Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) < Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y);
		else
			isVertical = (vertFrontInCorridor != vertRearInCorridor);

		bool topInCorridor = IsTileNarrowFloat(self as Creature, 0, 0f, 0f);
		// bool pipeTwisting = false; //REMOVE THIS IF WE DON'T NEED IT

		bool wedgedInFront = (
			!isVertical &&
			((!inPipe && (frontInCorridor && !rearInCorridor))
			|| (inPipe && (!frontInCorridor && rearInCorridor)))
			);

		bool wedgedBehind = false; //DOESN'T MATTER

		bool vertStuck = (isVertical && self.bodyMode != Player.BodyModeIndex.Stand && self.animation != Player.AnimationIndex.BellySlide && 
			((!inPipe && (vertFrontInCorridor && !vertRearInCorridor))
			|| (inPipe && (!vertFrontInCorridor && vertRearInCorridor)))
			);

		//Debug.Log("STUCK CHECK!!! " + inPipe + "-" + frontInCorridor + "-" + rearInCorridor + "-" + topInCorridor + " --- " + vertFrontInCorridor + "-" + vertRearInCorridor + " -- " + IsBackwardsStuck(self));

		//IntVector2 tilePos = self.room.GetTilePosition(self.bodyChunks[1].pos);
		//Debug.Log("s1:" + self.room.GetTile(tilePos.x - 1, tilePos.y - 1).Terrain +
		//" s2:" + self.room.GetTile(tilePos.x + 0, tilePos.y - 1).Terrain +
		//" s3:" + self.room.GetTile(tilePos.x + 1, tilePos.y - 1).Terrain +
		//" s4:" + self.room.GetTile(tilePos.x - 1, tilePos.y + 0).Terrain +
		//" s5:" + self.room.GetTile(tilePos.x + 0, tilePos.y + 0).Terrain +
		//" s6:" + self.room.GetTile(tilePos.x + 1, tilePos.y + 0).Terrain +
		//" s7:" + self.room.GetTile(tilePos.x - 1, tilePos.y + 1).Terrain +
		//" s8:" + self.room.GetTile(tilePos.x + 0, tilePos.y + 1).Terrain +
		//" s9:" + self.room.GetTile(tilePos.x + 1, tilePos.y + 1).Terrain);


		if (self.GetBelly().fwumpDelay > 0)
			self.GetBelly().fwumpDelay--;

		//IF WE'RE NOT STUCK, RETURN
		if (!wedgedInFront && !wedgedBehind && !vertStuck)
		{
			//12/12/22 MOVING TO ITS OWN METHOD - THIS BOTH DOES THE REDIRECTING, AND ENDS THE STATEMENT EARLY IF THE REST ISN'T NEEDED
			if (RedirectStuckage(self, false, eu))
				return; 
			else
            {
				if (!self.GetBelly().verticalStuck)
					wedgedInFront = true;
				else
					vertStuck = true;
			}
		}

		//DETERMINES THE VALUE THEY MUST PASS IN ORDER TO SLIDE THROUGH
		Vector2 tilePosMod = new Vector2(vertStuck ? 0 : (2f * posMod * self.GetBelly().myFlipValX), vertStuck ? (2f * posMod) * self.GetBelly().myFlipValY : 0);
		//int tileSizeMod = doubleWedged ? 0 : 0; //-1 MOD FOR WEDGED AGAINST EDGES - MEH... LETS TRY WITHOUT IT
		//int tileSizeMod = (self.GetBelly().bonusFoodPoints == 1) ? 2 : 0; //A THING OF THE PAST
		int tileSizeMod = 0; //LEGACY

		//if (ModManager.MSC && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
		float naturalChub = 0f;
		if (self.slugcatStats.bodyWeightFac >= 1.3f) //GOURMANDS IS 1.35
			naturalChub = 1f;
		//Debug.Log("MY BODY WEIGHT! " + self.slugcatStats.bodyWeightFac + " NUM" + self.slugcatStats.name);
		if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear)
			naturalChub = -0.5f;

		//ALRIGHT FINE IM DOING IT MANUALLY
		float myChub = 0 + naturalChub * 2;
		switch (GetChubFloatValue(self))
		{
			case 4f:
				myChub = 7.5f; //9.5f
				break;
			case 3.5f:
				myChub = 7.0f; //8f
				break;
			case 3f:
				myChub = 6.0f; //7f
				break;
			case 2f:
				myChub = 5f + naturalChub; //LOWER LEVELS GET MORE NATURAL CHUB
				break;
			case 1f:
				myChub = 2f + naturalChub * 2;
				break;
			default:
				myChub = 0f + (naturalChub * 2) + (self.GetBelly().bigBelly ? 2 : 0);
				break;
		}

		//IF WE'RE IN (any) MODE, WE CAN COUNT ANY ADDITIONAL SNACKING WE'VE DONE
		float extraChub = GetOverstuffed(self);
		if (extraChub > 6)
			extraChub = 6 + ((extraChub - 6) / 2); //ANYTHING PAST 4 COUNTS AS HALF
		myChub += 0.5f * extraChub;

		//IF WE'RE PIGGYBACKING... CUT US SOME SLACK. WE DON'T WANT TO GET BOOTED OFF FOR EVERY TINY LITTLE SQUEEZE.
		// if (BellyPlus.jollyCoopEnabled)
		if (IsPiggyBacked(self))
			myChub -= 6f;

		float tileSize = (myChub + GetTileSizeMod(self, tilePosMod, myInputVec, 0, inPipe, self.submerged, false));
		float preTileSize = tileSize;
		
		float diff = BPOptions.bpDifficulty.Value - (Mathf.Max(0, BPOptions.gapVariance.Value - 1) * 5f); //SCALE GAP TIGHTNESS BY VARIANCE SLIGHTLY
        //AND THEN THIS ONES FOR THE DIFFICULTY MODIFIER. BUT LETS LEAVE THE FIRST 5 STACKS OF THIS ALONE...
        if (tileSize > 3)
			tileSize = 3 + ((tileSize - 3) * (1 + (diff / 10f)));
		tileSize -= Mathf.Lerp(3, 0, (5 + diff) /5f); // -3 - 0  from easiest to default. HARDER DOESNT INCREASE IT I GUESS
		
		//SLIGHT BLOAT IF WE JUST ATE. +1 REGARDLESS OF MODIFIERS
		if (self.GetBelly().bloated && GetChubValue(self) >= -2)
			tileSize += 1;

        float squeezeThresh = 30 * (tileSize);
		// int scaleCap = 260; //HOW HIGH WE GET BEFORE THE REST IS SCALED DOWN
		// if (squeezeThresh > scaleCap)
			// squeezeThresh = scaleCap + ((squeezeThresh - scaleCap) / (BPOptions.hardMode.Value ? 1 : 2));
		if (squeezeThresh > 450)
			squeezeThresh = 450 + ((squeezeThresh - 450) / (BPOptions.hardMode.Value ? 1 : 2));
		
		// if (squeezeThresh > 0f && squeezeThresh <= 30f)
			// squeezeThresh = 0; //CLOSE ENOUGH. WE DON'T NEED TO BOTHER WITH SQUEEZES THIS SHORT
		
		//REWRITE! WE'RE CHANGING THIS LENIANCY THRESHOLD TO BITE BACK AGAINST SUPER CHUBBY SLUGS
		//SLIGHT CHUB, WE IGNORE GAPS IN THE THRESHOLD THAT ARE CLOSE TO 0. HUGE CHUB; WE HAVE A MINIMUM THAT WE HAVE TO SQUEEZE THROUGH, AND FOR AN EVEN LARGER WINDOW
		if ((squeezeThresh > Mathf.Lerp(0, -60f, extraChub / 10f)) && squeezeThresh <= 45f)
			squeezeThresh = (extraChub > 4) ? 45 : 0;
		
		self.GetBelly().tileTightnessMod = squeezeThresh;
		
		//IF OUR RESULT TURNS OUT TO BE 0 ANYWAYS, CANCEL THE STUCK
		if (squeezeThresh <= 0)
		{
			self.GetBelly().noStuck = 30;
			if (squeezeThresh == 0 && self.input[0].IntVec != new IntVector2(0,0)) //IF IT WAS EXACTLY OUR SIZE, PLAY A FUNNY SOUND
			{
				self.room.PlaySound(BPEnums.BPSoundID.Squinch1, self.mainBodyChunk, false, 0.1f, 1.3f - GetChubValue(self)/12f);
				self.GetBelly().shortStuck = 5; //TO ACCOMPANY THE SQUINCH~
				if (BPOptions.debugLogs.Value)
					Debug.Log("SQUINCH " + inPipe);
			}
			// Debug.Log("NOT TIGHT ENOUGH TO BE STUCK! " + squeezeThresh);
			if (!inPipe) //SO WE CAN TRANSITION INTO PIPESTATUS RIGHT AWAY
				self.GetBelly().timeInNarrowSpace = 30;
			return;
		}

		
		//WE JUST NOW GOT STUCK --- PREPARE STUCK BWOMP!
		if (!self.GetBelly().isStuck &&  (vertStuck || wedgedInFront || wedgedBehind))
        {
			//WHAT DIRECTION ARE WE STUCK?
			if (isVertical)
			{
				// int flipper = (vertFrontInCorridor && !vertRearInCorridor) ? 1 : -1;  //WAIT NO THIS SHOULDNT MATTER. 
				// int flipper = (self.bodyChunks[1].vel.y > 0) ? 1 : -1; 
				//int flipper = (self.bodyChunks[1].pos.y > self.bodyChunks[1].lastPos.y) ? 1 : -1; //NO DICE. THIS IS WRONG SOMETIMES (LIKE IF LEAPING UP INTO AN ELEVATED X SQUEEZE WHILE LEANING BACK, I THINK)
				int flipper = self.GetBelly().myFlipValY; //BUT CAN WE REALLY TRUST THIS ONE MORE?...
				self.GetBelly().stuckVector = new Vector2(0, flipper);
				crashVel = Mathf.Abs((self.bodyChunks[1].vel + (new Vector2(0, self.gravity) * 6f)).y) * 0.8f;
			}
			else
			{
				int flipper = self.GetBelly().myFlipValX;
				self.GetBelly().stuckVector = new Vector2(flipper, 0);
				crashVel = Mathf.Abs((self.bodyChunks[1].vel).x);
			}

			//INTERACTION WITH STICK TOGETHER! IF WE GET STUCK VIA TELEPORT, MAKE IT AN AUTOPILOT! FINALLY A USE FOR THIS THING...
			if (self.enteringShortCut != null)
				self.GetBelly().autoPilot = new IntVector2((int)self.GetBelly().stuckVector.x, (int)self.GetBelly().stuckVector.y);

            //CHECK IF THIS IS THE SAME GAP WE'VE ALREADY TRIED (SO WE DON'T CHEEZE PROGRESS) //WAIT THIS DOESN'T MATTER WE SHOULD DO THIS EVERY TIME
            //bool sameGap = false;
            //if (self.room.MiddleOfTile(self.bodyChunks[1].pos) == self.GetBelly().stuckCoords)
            //	sameGap = true;

            self.GetBelly().stuckCoords = self.room.MiddleOfTile(self.bodyChunks[1].pos);
			self.GetBelly().stuckStrain += 10; //TO START THEM OFF
			if (BPOptions.debugLogs.Value)
				Debug.Log("NEW PLAYER STUCK VECTOR! " + self.GetBelly().stuckVector + " ----BASE TILE SIZE: " + preTileSize + " ADJ: " + tileSize + "---- ADJ CHUB:" + myChub);
			GetTileSizeMod(self, tilePosMod, myInputVec, tileSizeMod, inPipe, self.submerged, true); //JUST FOR LOGS

			//IF WE WERE SUPER JUMPING
			if (self.simulateHoldJumpButton > 0)
				crashVel = Mathf.Max(crashVel, 12);

			if (self.animation == Player.AnimationIndex.BellySlide)
            {
				crashVel = Mathf.Max(crashVel, 12);
				self.animation = Player.AnimationIndex.None;
				self.room.AddObject(new ExplosionSpikes(self.room, self.bodyChunks[1].pos - new Vector2(0f, -self.bodyChunks[1].rad), 8, 8f, 4f, 5.0f, 20f, new Color(1f, 1f, 1f, 0.5f)));
				MakeSparks(self, 1, 6);
			}

			//BWOMP VISUALS!
			//GRAVITY IS REDUCING OUR YVEL BY LIKE 6.0 AT ALL TIMES BY THIS POINT. TAKE THAT INTO ACCOUNT
			//crashVel = (self.bodyChunks[1].vel).magnitude;
			if (BPOptions.debugLogs.Value)
				Debug.Log("PRE-PLUG VELOCITY! " + crashVel + " VECT:" + self.bodyChunks[1].vel + " GRAV:" + self.gravity);

			if (crashVel > 15f) //AT THIS SPEED WE'LL BREACH THE ENTRANCE EVEN WITH THE REDUCED VEL. 
			{
				PlayExternalSound(self, BPEnums.BPSoundID.Squinch1, 0.2f, 1.3f);
				MakeSparks(self, 1, 4);
			}
			if (crashVel > 8f) //WE'RE GOING PRETTY FAST! CUT OUR VEL IN HALF
			{
				//self.room.PlaySound(BPEnums.BPSoundID.Fwump1, self.mainBodyChunk, false, 0.12f, 1f); //WAIT WAT? HOW IS THIS VOLUME FLUCTUATING...
				PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.12f, 1f);
				self.bodyChunks[0].vel *= 0.3f;
				self.bodyChunks[1].vel *= 0.3f;
				MakeSparks(self, 1, 4);
				if (BPOptions.debugLogs.Value)
					Debug.Log("WO-HOA THERE COWBOY! " + crashVel + " NEW VEL:" + self.bodyChunks[1].vel + " PROGR BOOST: " + (crashVel * 0.25f));
				self.GetBelly().stuckStrain += crashVel * (ObjIsSlick(self) ? 15f : 10f);
				if (ObjIsSlick(self))
					MakeSquealch(self, true);
			}
			if (crashVel > 6f) //ADD MOMENTUM TO THE STUCK
			{
				float progBoost = ((crashVel * 0.05f) * (1f + GetScaledOverstuffed(self) * 0.05f)) * (ObjIsSlick(self) ? 3f : 1f) - 0.25f;
                if (BPOptions.debugLogs.Value)
                    Debug.Log("BOOST PROGRESS! " + progBoost + " CRASH VEL:" + crashVel );
				float oldLoosenProg = self.GetBelly().loosenProg;
				self.GetBelly().loosenProg = 0f; //BACK TO 0 SO WE CAN CHOOSE THE HIGHER OF THE TWO
				ObjGainLoosenProg(self, progBoost);
				//NOW CHOOSE THE NEW ONE
				self.GetBelly().loosenProg = Mathf.Max(self.GetBelly().loosenProg, oldLoosenProg);
				ObjGainBoostStrain(self, 0, Mathf.CeilToInt(6 + crashVel), 22);
				ObjGainSquishForce(self, Mathf.CeilToInt(6 + crashVel), 22);
				//ADJUST OUR HEAD PLEASE GOD
				if (!inPipe && !topInCorridor && self.GetBelly().stuckVector.y == -1)
                {
					self.bodyChunks[0].pos.x = self.bodyChunks[1].pos.x;
					self.bodyChunks[0].pos.y = self.bodyChunks[1].pos.y - 5f;
				}
                
                if (self.slugcatStats?.name?.value == "NoirCatto" && UnityEngine.Random.value < ((crashVel - 5f) / 10f))
                    DoNoirSounds(self, true);
            }
			//ONLY DO THE JOLT IF OUR HEAD IS IN A TUNNEL 
			if (!inPipe && (topInCorridor))// && (wedgedInFront || wedgedBehind)))
			{
				self.GetBelly().fwumpDelay = 4;
			}
			else if (crashVel > 6f)
			{
                self.room.PlaySound(BPEnums.BPSoundID.Fwump2, self.mainBodyChunk, false, Mathf.Min(crashVel / 20f, 0.7f), 1.1f);
            }

            //DISMOUNT IF WE'RE ON SOMEONE SHOULDERS
            if (self.onBack != null)
            {
                self.onBack.slugOnBack.DropSlug();
                self.input[0].x = (int)self.GetBelly().stuckVector.x;
                self.input[0].y = (int)self.GetBelly().stuckVector.y;
				RedirectStuckage(self, true, eu);
                self.GetBelly().stuckStrain += 10; //JUST TO KEEP US WEDGED
				self.GetBelly().autoPilot = new IntVector2((int)self.GetBelly().stuckVector.x, (int)self.GetBelly().stuckVector.y);
				self.GetBelly().stuckLock = 15; //HOLD THEM IN PLACE FOR THIS LONG
            }
        }


		if ((vertStuck || wedgedInFront || wedgedBehind))
			self.GetBelly().stuckCoords = self.room.MiddleOfTile(self.bodyChunks[1].pos);

		//DON'T LET PIGGYBACKING PHYSICS JANK CHEAT US THROUGH GAPS! LOCK THEM IN PLACE BRIEFLY WHEN THE DISMOUNT HAPPENS
        if (self.GetBelly().stuckLock > 0)
        {
            self.bodyChunks[1].pos = self.room.MiddleOfTile(self.GetBelly().stuckCoords);
			self.GetBelly().stuckLock--;
			if (self.GetBelly().stuckLock == 0) //DISABLE THE AUTO PILOT AFTER A TIME
				self.GetBelly().autoPilot = new IntVector2(0, 0);
        }

        //STUCK BWOMP!
        if (self.GetBelly().fwumpDelay == 1 && self.graphicsModule != null)
        {
			//THE SLUGCAT'S TAIL WILL GO INVISIBLE IF CRASHVEL IS NEGATIVE
			float velMag = 0.0f + Mathf.Sqrt(crashVel);
			float vol = Mathf.Min((velMag / 5f), 0.25f);  //(velMag / 8f), 0.2f
			self.room.PlaySound(BPEnums.BPSoundID.Fwump2, self.mainBodyChunk, false, vol, 1.1f);

			if (!vertStuck)
			{
				//RE- TAIL STUFF
				int i = 0;
				foreach (var bodyPart in self.graphicsModule.bodyParts.OfType<TailSegment>())
				{
					bodyPart.vel.y += velMag * (4f * i);
					i++;
				}
                if (self.graphicsModule != null)
					GetHead(self).vel.y -= velMag * 10f; //HEAD
				self.bodyChunks[0].vel.y -= velMag * 6f;
				self.bodyChunks[1].vel.y += velMag * 6f;
			}
			else
			{
				//RE- TAIL STUFF
				int i = 0;
				foreach (var bodyPart in self.graphicsModule.bodyParts.OfType<TailSegment>())
				{
					bodyPart.vel.y = velMag * (4f * i * -self.GetBelly().myFlipValY);
					i++;
				}
			}
			// Debug.Log("-----BWOMP! JUST GOT STUCK " + velMag);
			MakeSparks(self, 1, Mathf.CeilToInt(velMag));
		}

        //FROM THE DELAYED SHOVE
        if (self.GetBelly().fwumpDelay == 8)
        {
			self.GetBelly().stuckStrain += (130f + bonusMomentum + (BPOptions.hardMode.Value ? 0 : 30)) * (ObjIsSlick(self) ? 1.25f : 1f);
			self.GetBelly().corridorExhaustion += 40;
			self.AerobicIncrease(2f);
			self.GetBelly().fwumpDelay = 0;
			bonusMomentum = 0f;
			if (ObjIsSlick(self))
            {
				for (int n = 0; n < 12; n++)
				{
					Vector2 pos3 = ObjGetBodyChunkPos(self, "middle");
					self.room.AddObject(new StrainSpark(pos3, new Vector2(0, 0) + Custom.DegToVec((360f * UnityEngine.Random.value)) * 4, 15f, Color.white));
				}
				self.room.PlaySound(SoundID.Swollen_Water_Nut_Terrain_Impact, self.mainBodyChunk, false, 1.5f, 1f);
			}
		}


		//ASSISTED SQUEEZES VISUAL BOOST
		float counterPush = 0f;
		if (self.GetBelly().beingPushed > 0) //assistedSqueezing - OKAY BUT PULLING WILL GET IT;S OWN SOURCE
		{
			BoostCreature(self, 2, 8);
			//WE NEED TO IMPOSE SOME SORT OF LIMIT ON THIS...
			//self.GetBelly().boostStrain = Math.Min(self.GetBelly().boostStrain, 18); //NOT ANYMORE! WE'VE GOT A CAP FOR THAT
			if (self.input[0].IntVec != new IntVector2(0,0)) //BUT SKIP IT IF WE'RE BEING LAZY
				counterPush = 0.5f; //OTHERWISE, THE COMBINED BOOST TENDS TO PUSH THEM PAST THE STUCK BARRIER
		}

		

		if (wedgedInFront || wedgedBehind)
		{
			self.GetBelly().isStuck = true;
			self.GetBelly().verticalStuck = false;
			float tileCheckOffset = ((inPipe && !IsBackwardsStuck(self)) ? 0 : 10f) * self.GetBelly().myFlipValX; //WELP, NOW IT WORKS WELL
			float pushBack = (self.room.MiddleOfTile(self.bodyChunks[1].pos).x + tileCheckOffset - self.bodyChunks[1].pos.x); // * self.GetBelly().myFlipValX;
			//Debug.Log("---PUSHBACK!: " + pushBack + " " + self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x + " " + tileCheckOffset);
			//SEEMS THE AVERAGE STARTS AROUND -6.0 AND GETS ABOVE -7.0 BEFORE STOPPING.
			//SOOO DO WE JUST.. SUBTRACT BY 6? ORRR... MAYBE ((NUM-6) * 2) FOR A MORE DIVERSE RESULT?
			//AND THEN ADD A VERY SMALL CONST PUSH IN THE DIRECTION OF MOVEMENT TO SIMULATE STRAINING
			pushBack = (pushBack - ((((myInputVec.x != 0 && !self.lungsExhausted) ? 8.5f : 6.8f) + counterPush) * self.GetBelly().myFlipValX)) * 1.0f;
			//pushBack = (pushBack - ((self.input[0].x != 0 ? 7.5f : 6f) * self.GetBelly().myFlipValX)) * 1.0f;
			//Debug.Log("-----SQUEEZED AGAINST AN X ENTRANCE!: " + self.GetBelly().myFlipValX + " " + self.GetBelly().inPipeStatus + " " + pushBack + " " + self.bodyMode + " " + self.GetBelly().stuckStrain + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);

			if (Math.Abs(pushBack) > 15f)
				pushBack = 0; //SOMETHINGS GONE HORRIBLY WRONG

			//if (self.input[0].x != 0 || self.GetBelly().assistedSqueezing)
			if (myInputVec.x == self.GetBelly().stuckVector.x || (self.GetBelly().assistedSqueezing && myInputVec.x != -self.GetBelly().stuckVector.x) || self.Stunned)
			{
				//MAKE PROGRESS AS WE STRAIN. SELF STRUGGLING AND ASSISTED STRUGGLING CAN STACK
				if (myInputVec.x == self.GetBelly().stuckVector.x && !self.lungsExhausted)
					self.GetBelly().stuckStrain+=2f;
				//OUR PUSHERS STRAIN WILL BE ADED ELSEWHERE, WHERE WE CAN CHECK THAT THEY ARENT EXHAUSTED FIRST

				if (!self.lungsExhausted) //(self.GetBelly().stuckStrain < squeezeThresh)
				{
					//NOW, DO WE WANT TO SLOW DOWN OUR MOVEMENT SPEED? OR OUR PHYSICAL VELOCITY?...
					//IF WE DO CORRCLIMBSPEED, PUT THIS IN UPDATEBODYMODE. IF WE DO VELOCITY, PUT IT AT THE END OF NORMAL UPDATE, SO IT UPDATES IF PLAYERS GRAB US
					float wornOut = 1 - GetExhaustionMod(self, 80);
					if (self.GetBelly().beingPushed < 1) //DON'T RAISE TAIL FOR ASSISTED SQUEEZING
					{
						WagTailSegment(self, 1, new Vector2( Mathf.Sqrt(self.GetBelly().stuckStrain / 30f) * -self.GetBelly().myFlipValX, 0)); //TAIL OUT
						WagTailSegment(self, 1, new Vector2(0, GetCappedBoostStrain(self) / 4f) * (1f - GetExhaustionMod(self, 80)));
					}
					if (inPipe || (!inPipe && topInCorridor)) //SO WE DON'T SLIDE UP WALLS WHEN BOOSTING
                    {
						//self.graphicsModule.bodyParts[6].vel.y += Mathf.Min(Mathf.Sqrt(self.GetBelly().boostStrain / 10f), 0.1f - 0.2f * GetExhaustionMod(self, 50)); //SNOUT UP
						//THIS DOESN'T PAIR WELL WITH THE STRETCHED TORSO...
					}
					self.bodyChunks[0].vel.y = 0;
					WagTailSegment(self, 3, new Vector2(0, 0.5f * self.GetBelly().myFlipValY * wornOut));
				}

				if (self.GetBelly().stuckStrain > (squeezeThresh + ((squeezeThresh > 300 && !(self.GetBelly().boostStrain >= 18 || self.lungsExhausted)) ? 15 : 0)))
				{
					PopFree(self, self.GetBelly().stuckStrain, self.GetBelly().inPipeStatus);
					self.horizontalCorridorSlideCounter = 0;
					// Debug.Log("-----SLIIIIIIIIIDE THROUGH AN X ENTRANCE!: ");
					pushBack = 0;
					self.GetBelly().stuckStrain = 6; //FAST PASS TO FIX VOLUME N STUFF
					self.GetBelly().squeezeStrain = 0; //SO WE DON'T ALSO GET THE POP
				}
			}
            else
            {
                self.GetBelly().stuckStrain = 0;
            }

			//THIS GETS HANDLED RIGHT ABOVE US
            //IF WE'RE FIGHTING THE PUSH DIRECION WITH OUR PARTNER, CANCEL THIS. OTHERWISE WEIRD THINGS HAPPEN IF THEY STOP PUSHING WHILE WE WERE SWITCHING DIRECTIONS
			//if (self.input[0].x == -self.GetBelly().stuckVector.x)
				//self.GetBelly().stuckStrain = 0;
			
			//OK WE NEED A FORMULA WHERE THAT, WHEN OUR X >= 9.5 FROM MID, VEL APPROACHES 0
			self.bodyChunks[1].vel.x += pushBack + (GetCappedBoostStrain(self) * self.GetBelly().myFlipValX / 5f);
			//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
			if (self.bodyChunks[1].vel.x < 0 != self.input[0].x < 0)
				self.bodyChunks[1].vel.x /= 2; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING

		}
		
		
		//VERTICAL SQUEEZES
		else if (vertStuck)
		{
			self.GetBelly().isStuck = true;
			self.GetBelly().verticalStuck = true;
			float tileCheckOffset = ((inPipe && !IsBackwardsStuck(self)) ? 0 : 10f) * self.GetBelly().myFlipValY; //WELP, NOW IT WORKS WELL
			float pushBack = (self.room.MiddleOfTile(self.bodyChunks[1].pos).y + tileCheckOffset - self.bodyChunks[1].pos.y); // * self.GetBelly().myFlipValY;
			pushBack = (pushBack - (((myInputVec.y != 0 ? 8.7f : 7f) + counterPush) * self.GetBelly().myFlipValY)) * 1.0f;
			//Debug.Log("-----SQUEEZED AGAINST AN Y ENTRANCE!: " + self.GetBelly().myFlipValY + " - " + self.GetBelly().inPipeStatus + " " + pushBack + " " + self.bodyMode + " " + self.GetBelly().stuckStrain + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).y + " " + self.bodyChunks[1].pos.y);

			if (myInputVec.y == self.GetBelly().stuckVector.y || (self.GetBelly().assistedSqueezing && self.input[0].y != -self.GetBelly().stuckVector.y) || self.Stunned)
			{
				//MAKE PROGRESS AS WE STRAIN. SELF STRUGGLING AND ASSISTED STRUGGLING CAN STACK
				if (myInputVec.y == self.GetBelly().stuckVector.y && !self.lungsExhausted)
					self.GetBelly().stuckStrain+=2;

				//WE SHOULDNT BE TRYING TO STAND WHILE SQUEEZING DOWN!
				// if (self.bodyMode == Player.BodyModeIndex.Stand) //DON'T EVEN CHECK IF WE'RE STANDING. JUST DON'T STAND
				self.standing = false;

				if (!self.lungsExhausted) //(self.GetBelly().stuckStrain < squeezeThresh)
				{
					float wornOut = 1 - GetExhaustionMod(self, 60);
					if (self.GetBelly().beingPushed < 1 && self.GetBelly().myFlipValY < 0)
					{
						WagTailSegment(self, 1, new Vector2(0, Mathf.Sqrt(self.GetBelly().stuckStrain / 30f) * -self.GetBelly().myFlipValY * wornOut)); //TAIL OUT
						WagTailSegment(self, 1, new Vector2(0, GetCappedBoostStrain(self) * -self.GetBelly().myFlipValY * wornOut));
					}
					else if (self.GetBelly().myFlipValY > 0)
					{
						WagTailSegment(self, 1, new Vector2((GetCappedBoostStrain(self) / 2) * self.GetBelly().myFlipValX * wornOut, 0));
						if (inPipe)
							self.bodyChunks[0].pos.x = self.bodyChunks[1].pos.x; //KEEP THE HEAD LEVEL SO IT DOESNT SQUEEZE OUT DIAGONALLY
					}
					if (self.graphicsModule != null)
						GetHead(self).vel.y += (Mathf.Min(Mathf.Sqrt(self.GetBelly().stuckStrain / 30f), 6f) + (GetCappedBoostStrain(self) / 2f)) * self.GetBelly().myFlipValY * wornOut; //SNOUT OUT
				}

				if (self.GetBelly().stuckStrain > (squeezeThresh + ((squeezeThresh > 300 && !(self.GetBelly().boostStrain >= 18 || self.lungsExhausted)) ? 15 : 0)))
				{
					//HIGHER BOOST VALUE IF LAUNCHING UPWARDS
					float boostVel = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y) ? self.GetBelly().stuckStrain : self.GetBelly().stuckStrain / 2f;
					PopFree(self, boostVel, self.GetBelly().inPipeStatus);
					self.horizontalCorridorSlideCounter = 0;
					// Debug.Log("-----SLIIIIIIIIIDE THROUGH AN Y ENTRANCE!: ");
					pushBack = 0;
					self.GetBelly().stuckStrain = 6; //FAST PASS TO FIX VOLUME N STUFF
					self.GetBelly().squeezeStrain = 0; //SO WE DON'T ALSO GET THE POP
				}
			}
            else
            {
                self.GetBelly().stuckStrain = 0; //OK BUT WE SHOULD STOP STRAINING WHEN NOBODY IS PUSHING
            }

            //IF WE'RE FIGHTING THE PUSH DIRECION WITH OUR PARTNER, CANCEL THIS. OTHERWISE WEIRD THINGS HAPPEN IF THEY STOP PUSHING WHILE WE WERE SWITCHING DIRECTIONS
            //if (self.GetBelly().assistedSqueezing && self.input[0].y != 0 && self.input[0].y != self.GetBelly().myFlipValY)

			//REPURPOSING. GRAVITY WANTS TO KEEP US DOWN WHEN BACKING OUT OF A DOWNWARDS SQUEEZE INTO THE FLOOR. SO BRING US UP A BIT TO UNSTUCK US
            if (self.GetBelly().stuckVector.y == -1 && self.input[0].y == -self.GetBelly().stuckVector.y)
            {
				self.GetBelly().stuckStrain = 0;
				self.bodyChunks[1].vel.y += 2;
			}

			//CENTER US IN THE PIPE A BIT...
			self.bodyChunks[1].pos.x = Vector2.Lerp(self.bodyChunks[1].pos, self.room.MiddleOfTile(self.bodyChunks[1].pos), 0.5f).x;

            //OK WE NEED A FORMULA WHERE THAT, WHEN OUR X >= 9.5 FROM MID, VOL APPROACHES 0
            self.bodyChunks[1].vel.y += pushBack + (GetCappedBoostStrain(self) * self.GetBelly().myFlipValY / 6f);
			//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
			if (self.bodyChunks[1].vel.y < 0 != self.input[0].y < 0)
				self.bodyChunks[1].vel.y /= 2; //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING
		}

		//I DON'T THINK THE CODE EVER REACHES THIS POINT.
		else
		{
			self.GetBelly().isStuck = false;
			if (self.GetBelly().stuckStrain > 0)
			{
				self.GetBelly().stuckStrain -= 2;
			}
			Debug.Log("--------NO LONGER STUCK!: " + self.GetBelly().inPipeStatus + " " + self.bodyMode);
		}
		
		
		//------REDUCE STUCK STRAIN BASED ON THE FORMULA F(x) = (X(A-C))/500---------
		if (self.GetBelly().stuckStrain > 0)
		{
			//A NEW VAR TO TRACK LOOSENING PROGRESS AS STRAINING CONTINUES

			//self.GetBelly().loosenProg += Mathf.Sqrt(Mathf.Max(self.GetBelly().stuckStrain - 200f, 0)) / ((GetLivingPlayers(self) > 1) ? 2000f : 1500f); //MULTIPLYING THIS BY 4. LETS SEE HOW IT GOES .stuckStrain - 100f, 0)) / 6000f
			float loosenAmnt = Mathf.Sqrt(Mathf.Max(Mathf.Min(self.GetBelly().stuckStrain, 280) - 200f, 0)) / ((GetLivingPlayers(self) > 1) ? 2000f : 1500f);
			//RESISTANCE TO SNOWBALLING PROGRESS (DISABLED IN EASY MODE)
			if (BPOptions.hardMode.Value)
            {
				float loosenResist = Mathf.Max(1f, self.GetBelly().loosenProg / 3);
				self.GetBelly().loosenProg += (loosenAmnt / loosenResist);
			}
			
			float counterStrain = (self.GetBelly().stuckStrain * ((self.GetBelly().tileTightnessMod / 30) - self.GetBelly().loosenProg)) / 500f; //500f
			// Debug.Log("--------COUNTER STRAIN!: " + counterStrain + " TIGHTNESS: " + (self.GetBelly().tileTightnessMod / 30) + " - "+ (self.GetBelly().tileTightnessMod) + " " +  "STRAIN: " + self.GetBelly().stuckStrain + "  EXHAUSTION:" + obj.GetBelly().corridorExhaustion + " - " + GetExhaustionMod(self, 60) + "  PROGRESS:" + self.GetBelly().loosenProg + "  BOOST:" + self.GetBelly().boostStrain);
			if (BPOptions.debugLogs.Value && UnityEngine.Random.value < 0.033f)
				Debug.Log("-----STUCKAGE DEBUG!: " + self.GetBelly().stuckVector + " INPIPE:" + self.GetBelly().inPipeStatus + " " + self.bodyMode + " TIGHTNESS:" + (self.GetBelly().tileTightnessMod / 30) + " - "+ (self.GetBelly().tileTightnessMod) + " STRAIN:" + Mathf.CeilToInt(self.GetBelly().stuckStrain) + " PROGRESS:" + (Mathf.Round(self.GetBelly().loosenProg * 100) / 100) + "  BOOST:" + self.GetBelly().boostStrain + " STKCOORDS:" + self.GetBelly().stuckCoords + " CURRENT:" + self.bodyChunks[1].pos);
			self.GetBelly().stuckStrain -= counterStrain;
		}
	}




	public static void BPUUpdatePass1(Player self, bool eu)
    {
		//IF WE'RE PUSHING SOMEONE, PAUSE OUR RUN ANIMATION SO OUR LEGS DON'T MOVE
		if (self.bodyMode == Player.BodyModeIndex.Stand && self.input[0].x != 0 && (self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther) && self.animationFrame > 0 && self.slowMovementStun < 8)
			self.animationFrame--; //REDUCE THE RUN ANIMATION TICK BY 1 SO IT LOOKS LIKE WE'RE STANDING STILL
		

		else if (self.bodyMode == Player.BodyModeIndex.Crawl && self.input[0].x != 0 && (self.GetBelly().isStuck) && self.animationFrame > 0)
			self.animationFrame--; //REDUCE THE CRAWL ANIMATION TICK BY 1 SO IT LOOKS LIKE WE'RE STANDING STILL

		//SLOW DOWN OUR RUNNING ANIMATION!
		else if (self.bodyMode == Player.BodyModeIndex.Stand && eu && self.input[0].x != 0 && self.animationFrame > 0 && UnityEngine.Random.value > self.GetBelly().runSpeedMod)
		{
            self.animationFrame--;
			float runMod = self.GetBelly().runSpeedMod;
            Player backPlayer = GetHeaviestPlayerOnBack(self);
            if (backPlayer != null)
                runMod = Mathf.Min(runMod, backPlayer.GetBelly().runSpeedMod);
			
            if (runMod < (BPOptions.easilyWinded.Value ? 0.5f : 0.4f) && !self.isGourmand)
			{
                MakeStrainSparks(self, 1);
                if (!self.lungsExhausted)
				{
                    self.GetBelly().corridorExhaustion += 5;
                    if (GetExhaustionMod(self, 0) > 0.6f)
						self.Blink(6);
				}
				else
				{
                    self.GetBelly().corridorExhaustion += 2;
                    if (runMod < 0.2f)
						self.standing = false;
				}
            }
        }
            

        //DON'T ENTER SHORTCUTS WHILE STUCK (OR WHILE PULLING SOMEONE WHO IS STUCK!)
        if (IsStuck(self) || IsGraspingStuckCreature(self))
		{
            self.shortcutDelay = (Math.Max(self.shortcutDelay, 2));
			//HEY IF WE'VE BEEN CAPTURED BY A PREDATOR THAT IS STRUGGLING TO PULL US THROUGH A GAP, WE SHOULD TRY AND FIX THAT
			if (self.dangerGraspTime > 30)
				ObjGainLoosenProg(self, 0.05f);
        }
			
		//Debug.Log("-----MY INPUT!: " + playerNum + " " + (self.bodyMode == Player.BodyModeIndex.Stand) + "-" + (self.input[0].x != 0) + "-" + (self.GetBelly().pushingOther || self.GetBelly().pullingOther) + "-" + (self.animationFrame > 0) + "-" + (self.slowMovementStun < 8));
		//Debug.Log("-----MY INPUT2!: " + playerNum );
		//Debug.Log("-----DEBUG!: " + self.GetBelly().myFlipValX + " " + self.GetBelly().inPipeStatus + " " + self.bodyMode + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);
		//Debug.Log("-----DEBUG!: " + self.GetBelly().myFlipValX + " " + self.GetBelly().inPipeStatus + " " + self.bodyMode + " " + self.GetBelly().stuckStrain + " " + +self.room.MiddleOfTile(self.bodyChunks[1].pos).x + " " + self.bodyChunks[1].pos.x);

		if (self.GetBelly().myHeat < 1350 && (self.GetBelly().isSqueezing || self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther))
			self.GetBelly().myHeat++;
		//else if (self.GetBelly().myHeat < 1350 && self.GetBelly().isSqueezing) //-NAH THAT'S STUPID
		//	self.GetBelly().myHeat +=  eu ? 1 : 0;
		
		else if (self.GetBelly().myHeat > 0 && self.slowMovementStun < 1)
			self.GetBelly().myHeat -= 1;

		if (self.GetBelly().wideEyes > 0)
			self.GetBelly().wideEyes--;
		
		if (self.GetBelly().slicked > 0)
			self.GetBelly().slicked--;
		
		if (self.GetBelly().fwumpFlag > 0)
			self.GetBelly().fwumpFlag--;
		
		if (self.GetBelly().smearTimer > 0)
			self.GetBelly().smearTimer--;

        if (self.GetBelly().miscTimer > 0)
            self.GetBelly().miscTimer--;

        if (self.GetBelly().weightless > 0)
            self.GetBelly().weightless--;
		
		if (self.GetBelly().ignoreSpears > 0)
			self.GetBelly().ignoreSpears--;

        //DON'T DO THE CLIMBUPONBEAM ANIMATION IF WE'RE PUSHING SOMEONE
        if (self.animation == Player.AnimationIndex.GetUpOnBeam && (self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther))
			self.animation = Player.AnimationIndex.HangFromBeam;

		//ADJUST RUNSPEED ANIM - I DON'T ACTUALLY KNOW IF THIS EVEN WORKED.....
		if (self.animation == Player.AnimationIndex.StandUp && Mathf.Abs(self.input[0].x) > 0 && GetOverstuffed(self) > 0 && (self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther) && Mathf.Abs(self.bodyChunks[0].vel.x) > 2f)
        {
			if (UnityEngine.Random.value > self.GetBelly().runSpeedMod && self.animationFrame > 0)
				self.animationFrame--;
        }

		//1-9-23 HEAVY SLUGCATS SHOULD STRUGGLE WITH GETUPONBEAM!
		if (self.animation == Player.AnimationIndex.GetUpOnBeam) // && self.straightUpOnHorizontalBeam == false)
		{
			float stuffing = GetOverstuffed(self) + ((GetChubFloatValue(self) - 1) * (BPOptions.easilyWinded.Value ? 2 : 1.5f));
			stuffing *= (1 + (Mathf.Min(BPOptions.bpDifficulty.Value * 1.2f, 0) / 10f)); //LET EASY MODE PLAYERS GET UP MUCH EASIER 
			
			Player backPlayer = GetHeaviestPlayerOnBack(self); //, playerNum);
			if (backPlayer != null)
				stuffing = Mathf.Max(stuffing, GetOverstuffed(backPlayer) + (GetChubFloatValue(backPlayer) * 2f));
			
			//Debug.Log("-----MY STUFFING!: " + stuffing);
			// if (stuffing > 4)
				// stuffing = Mathf.Min(4 + ((stuffing - 4) / 4), 7);
			if (stuffing > 3)
				stuffing = Mathf.Min(3 + ((stuffing - 3) / 4), 7);
			
			//WE WTILL WANT TO BE ABLE TO PULL UP AS GOURMAND IF WE'RE GOURMAND EXHAUSTED. BUT MAKE IT HARDER
			if ((self.lungsExhausted && !self.gourmandExhausted) || (self.gourmandExhausted && self.GetBelly().corridorExhaustion >= 80) && !(self.input[0].jmp && !self.input[1].jmp))
			{
				MakeStrainSparks(self, 8);
				self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.2f, 0.6f);
				self.animation = Player.AnimationIndex.HangFromBeam;
				self.touchedNoInputCounter = 0;

				if (BPOptions.hudHints.Value && !BellyPlus.pullupHintGiven && self.room.game.cameras[0].hud != null
					&& self.pullupSoftlockSafety > 60 && UnityEngine.Random.value < 0.35f)
				{
					self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Holding still and touching no inputs for a while will boost your chances of pulling yourself up."), 0, 200, false, false);
					BellyPlus.pullupHintGiven = true;
				}
			}
			else if (stuffing > 0)
			{
				//IF WE'RE PRESSING THE JUMP KEY, SPEND STAM AND INCREASE OUR PROGRESS!
				if ((self.input[0].jmp && !self.input[1].jmp) || (self.input[1].jmp && !self.input[2].jmp)) // || (self.input[2].jmp && !self.input[3].jmp))
				{
					
					
					if (self.input[0].jmp && !self.input[1].jmp)
                    {
						if (self.GetBelly().boostCounter <= 0)
							stuffing /= 4f; //ONLY GET BOTH FRAMES OF BOOST IF WE WERE PATIENT
						else
							self.room.PlaySound(SoundID.Slugcat_Skid_On_Ground_Init, self.mainBodyChunk, false, 0.8f, 1.2f);

						self.GetBelly().corridorExhaustion += 5; //12
						self.GetBelly().boostCounter = 15;
						MakeStrainSparks(self, 8);
						self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 0.8f, 0.7f);
						self.canJump = 0;
						self.wantToJump = 0;
					}
					else
						stuffing /= 4f;
				}
				
				//LET US CANCEL THE DUMB PULLUP IF WE PRESS DOWN
				if (self.input[0].y == -1 && self.input[1].y != -1)
					self.animation = Player.AnimationIndex.HangFromBeam;

				//DON'T RUN THE LOCK WHATEVER THING (IT ACTIVATES AT 200)
				//if (self.pullupSoftlockSafety > 195)
				//	self.GetBelly().corridorExhaustion = Mathf.CeilToInt(maxStamina) + 1;
				self.pullupSoftlockSafety = 0; //YOU KNOW WHAT, WHO EVEN NEEDS IT


                if (self.pullupSoftlockSafety > 60)
					self.gourmandExhausted = false;
				
				self.bodyChunks[0].vel /= (1 + (stuffing / 12));
				self.bodyChunks[1].vel /= (1 + (stuffing / 6));
				if (!self.isSlugpup)
					self.GetBelly().corridorExhaustion += 2; // Mathf.CeilToInt(eu ? 2 : 3);
				if (stuffing > 1)
					MakeStrainSparks(self, 1);
				self.Blink(5);
			}
		}
		
		//WALLSTUCK CHECKERS MIGHT MESS US UP! DON'T LET THAT HAPPEN
		if (self.room != null && self.GetBelly().isStuck)
		{
			if (self.bodyChunks[0].ContactPoint.y != 0 
				&& self.bodyChunks[0].ContactPoint.y == -self.bodyChunks[1].ContactPoint.y 
				&& self.room.GetTile((self.bodyChunks[0].pos + self.bodyChunks[1].pos) / 2f).Solid 
				&& self.grabbedBy.Count < 1 
				&& !self.room.VisualContact(self.bodyChunks[0].pos, self.bodyChunks[1].pos) 
				&& !self.room.VisualContact(self.bodyChunks[0].pos + Custom.PerpendicularVector(self.bodyChunks[0].pos, self.bodyChunks[1].pos) * self.bodyChunks[0].rad, self.bodyChunks[1].pos + Custom.PerpendicularVector(self.bodyChunks[0].pos, self.bodyChunks[1].pos) * self.bodyChunks[1].rad) 
				&& !self.room.VisualContact(self.bodyChunks[0].pos - Custom.PerpendicularVector(self.bodyChunks[0].pos, self.bodyChunks[1].pos) * self.bodyChunks[0].rad, self.bodyChunks[1].pos - Custom.PerpendicularVector(self.bodyChunks[0].pos, self.bodyChunks[1].pos) * self.bodyChunks[1].rad))
			{
				// Debug.Log("COUNTER-WALLSTUCK");
				// base.bodyChunks[1].HardSetPosition(base.bodyChunks[0].pos + Custom.DirVec(base.bodyChunks[0].pos, base.bodyChunks[1].pos) * 2f);
				RedirectStuckage(self, true, eu); //CORRECT OUR POSITION BEFORE THE BASE GAME DOES
			}
		}
		
		
		//IF WE'RE PIGGYBACKED AND WE GET STUCK, CANCEL THE STUCK
		if (IsPiggyBacked(self) && self.GetBelly().isStuck)
		{
			self.onBack.slugOnBack.DropSlug(); //DROP US
			//self.bodyChunks[1].pos -= new Vector2(0f, 10f); //UNDO THE CHANGE THEY GAVE
			self.GetBelly().stuckStrain += 10; //JUST TO KEEP US WEDGED
			RedirectStuckage(self, true, eu);
			self.GetBelly().autoPilot = new IntVector2((int)self.GetBelly().stuckVector.x, (int)self.GetBelly().stuckVector.y);
        }
		
		
		//LETS GET A BIT MORE TOASTY WARM
		if (self.room != null && self.room.blizzard)
		{
			float stuffing = (GetChubFloatValue(self) / 8f) + (GetOverstuffed(self) / 8f);
			float warmth = RainWorldGame.DefaultHeatSourceWarmth * Mathf.Clamp(stuffing, 0f, 5f);
			//self.Hypothermia -= Mathf.Lerp(warmth, 0f, self.HypothermiaExposure); //WITH THIS MATH, A FULL STRENGTH BLIZARD IS UNEFFECTED BY OUR FATNESS AT ALL. BUT LANTERNS WORK THIS WAY TOO SO WHATEVER
            self.Hypothermia -= Mathf.Lerp(warmth, 0f, (self.HypothermiaExposure / 2f)); //FORGET IT. HALF EFFECTIVENESS IN FULL BLIZARD INSTEAD OF NO EFFECTIVENESS
        }

		//IF WE'RE SUBMERGED MAKE US SLICK (AND WASH OFF PREVIOUS SLICKNESS)
		if (self.bodyChunks[1].submersion > 0.5)
		{
            self.GetBelly().slicked = 450;
            //THE GUIDE IS AQUATIC AND CAN BE EXTRA SLIPPERY!
            if (self.slugcatStats?.name?.value == "Guide")
                self.GetBelly().slicked = 450 * 5;
        }
			




		//WIGGLE STRAIN!
		if (IsStuck(self) && self.GetBelly().boostCounter <= 0)
		{
			float stuckPerc = GetProgress(self) / Mathf.Max(((self.GetBelly().tileTightnessMod) / 30) - 6, 0.1f);
			stuckPerc = Mathf.Min(1f, stuckPerc);
			float decay = 1f - Mathf.Max(0, (stuckPerc - 0.66f) * 3f);

			IntVector2 lastWigDir = self.GetBelly().wiggleMem; //self.lastWiggleDir.x
			int wigVec = self.GetBelly().verticalStuck ? lastWigDir.x : lastWigDir.y;
			int wigInp = self.GetBelly().verticalStuck ? self.input[0].x : self.input[0].y;
			bool wigTwo = (new Vector2( self.input[0].x, self.input[0].y) == self.GetBelly().stuckVector && self.input[1].IntVec == new IntVector2(0,0));
			
			//Debug.Log("WIGGLE CHECK " + wigVec + " INP " + wigInp);

			if (self.GetBelly().wiggleCount < (10 * decay) && ((wigInp != 0 && wigInp != wigVec) || wigTwo))
			{
				self.room.PlaySound(SoundID.Slugcat_Turn_In_Corridor, self.mainBodyChunk, false, 1.3f * (1.0f - Mathf.Min((self.GetBelly().wiggleCount) / (5f), 1f)), 1f + stuckPerc);
				ObjGainBoostStrain(self, 0, Mathf.CeilToInt(3 * (1.0f - Mathf.Min((self.GetBelly().wiggleCount) / (5f), 1f))), 15);
				self.GetBelly().wiggleCount += (self.GetBelly().wiggleCount < 5) ? 2 : 1;
				//TAIL WAGGLE
				WagTailSegment(self, 1, self.input[0].IntVec.ToVector2() * 3f); //self.graphicsModule.bodyParts[1].vel += self.input[0].IntVec.ToVector2() * 3f;
			}

			if (wigInp != 0)
            {
				if (self.GetBelly().verticalStuck)
					self.GetBelly().wiggleMem = new IntVector2(wigInp, 0);
				else
					self.GetBelly().wiggleMem = new IntVector2(0, wigInp);
			}
		}

		
		
		//CHECK FOR CHEATING PARTNERS!
		if (self.GetBelly().landLocked)
		{
			self.shortcutDelay = Math.Max(2, self.shortcutDelay);
			if (IsGraspingActualSlugcat(self) == false)
			{
				self.GetBelly().landLocked = false;
				Debug.Log("ALRIGHT LAND LUBBER, YOU'RE FREE TO GO ");
			}
		}





		//IF JOLLYCOOP IS A THING, CHECK FOR STUFF
		// if (BellyPlus.jollyCoopEnabled)
		// BPUUpdatePassJolly(self, playerNum, eu);

		//if (BPOptions.holdShelterDoor.Value && self.room != null && self.room.abstractRoom.shelter) //SPECIAL REQUEST!~ HOLD DOORS UNTIL WE SAY SO
		//	self.touchedNoInputCounter = 0;
	}
	
	
	//SQUEEZE CHECKS
	public static void BPUUpdatePass2(Player self, bool eu)
    {
		//AT THIS PPINT, EVERYTHING IN THIS METHOD IS SO OLD I DON'T KNOW IF ANY OF IT IS EVEN RELEVANT ANYMORE. BUT IM TOO AFRAID TO CHANGE IT
		
		
		//DOUBLE CHECK WE'RE STILL BEING HELD, IF WE HAD A HELPER
		if (self.GetBelly().assistedSqueezing && !IsGrabbedByHelper(self) && self.dangerGrasp == null)
		{
			self.GetBelly().assistedSqueezing = false;
			//self.GetBelly().squeezeStrain = 0; //AND CUT THE STRAIN VALUE
		}


		//-----THE MAIN SQUEEZE CHECK-----  -12-4-23 QUICK QUESTION; WHY DOES THIS HAPPEN OUTSIDE OF OUR NORMAL STUCKAGECHECK?...
		if (GetChubValue(self) >= -1 || self.GetBelly().bigBelly)
		{
            if (IsGrabbedByHelper(self) || self.GetBelly().beingPushed > 0 || self.LickedByPlayer != null)
            {
                //FIRST CHECK IF WE'RE BEING GRABBED BY A HELPER
                if (IsCramped(self as Creature)) //(self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace)
                {
                    self.GetBelly().isSqueezing = true;
                    self.GetBelly().assistedSqueezing = true;
                    self.touchedNoInputCounter = 12; //SO SCAVS WILL CHILL TF OUT
                                                     //WE INCREASE STRAIN DIRECTLY HERE BECAUSE THE MOVEMENT UPDATE ISN'T RUNNING WHILE GRABBED
                    self.GetBelly().squeezeStrain = Math.Min(self.GetBelly().squeezeStrain + 1, 60);
                    if (IsGrabbedByPlayer(self) && self.graphicsModule != null)
                        self.grabbedBy[0].grabber.graphicsModule.BringSpritesToFront(); //SO WE CAN SEE THEIR HANDS :)
                    if (self.LickedByPlayer != null && self.GetBelly().isStuck)
                    {
                        ObjGainBoostStrain(self, 1, 3, 15);
                        ObjGainStuckStrain(self, 2f);
                    }
                }
                else
                {
                    self.GetBelly().isSqueezing = false;
                    self.GetBelly().assistedSqueezing = false;
                }
            }
            //IF WE HAVE ANY STUCK STRAIN AT ALL
            else if (IsStuck(self) && self.GetBelly().stuckStrain > 0)
            {
                self.GetBelly().isSqueezing = true;
                self.touchedNoInputCounter = 12; //SO SCAVS WILL CHILL TF OUT
            }
            else if ((self.bodyMode == Player.BodyModeIndex.CorridorClimb) && ((self.input[0].x == self.flipDirection) || (self.input[0].y != 0)))
            {
                self.GetBelly().isSqueezing = true;
            }
            else
            {
                self.GetBelly().isSqueezing = false;
                self.GetBelly().assistedSqueezing = false;
                //Debug.Log("-----NOT IN TUNNEL: " + GetPlayerNum(self));
            }
        }
		else //SOMEHOW WE'VE REVOKED OUR STUCKAGE THRESHOLD, AND VALUES WON'T RESET CORRECTLY IF WE DON'T CORRECT THEM OURSELVES
		{
			self.GetBelly().isSqueezing = false;
			self.GetBelly().assistedSqueezing = false;
			self.GetBelly().stuckStrain = 0;
            self.GetBelly().isStuck = false;
        }
		
		
		//OKAY BUT THIS STUFF CAN BE NEW
		//THIS DOES NOT REALLY SEEM LIKE THE RIGHT PLACE FOR THIS... BUT WHATEVER. AS LONG AS ONLY PLAYER 1 RUNS IT, IT WON'T DOUBLE UP
		if (eu && self.playerState.playerNumber == 0 && !self.isNPC)
		{
			if (BPOptions.hardMode.Value && BellyPlus.AllPlayersInStartShelter(self.room) && self.room.world.rainCycle.AmountLeft > 0f)
            {
				self.room.world.rainCycle.Update();
				// Debug.Log("-----SPEED UP THE RAIN TICKS!: ");
			}
		}
		
		if (self.GetBelly().slicked > 0 && self.bodyChunks[1].submersion < 0.5f && !IsPiggyBacked(self) && self.graphicsModule != null)
        {
			//BODY DRIPS
			if (UnityEngine.Random.value < 0.25f)
			{
				Vector2 pos3 = self.bodyChunks[1].pos + new Vector2(Mathf.Lerp(-9f, 9f, UnityEngine.Random.value), 9f + Mathf.Lerp(-2f, 2f, UnityEngine.Random.value));
				//self.room.AddObject(new WaterDrip(pos3, new Vector2(Mathf.Lerp(-1f, 1f, UnityEngine.Random.value), Mathf.Lerp(2f, 4f, UnityEngine.Random.value)), false));
				self.room.AddObject(new WaterDrip(pos3, new Vector2(0, 1), false));
			}

            //MAYBE SOME TAIL DRIPS TOO
            foreach (var bodyPart in self.graphicsModule.bodyParts.OfType<TailSegment>()) //for (int i = 0; i < 4; i++)
            {
                if (UnityEngine.Random.value < 0.08f)
                {
                    Vector2 pos3 = bodyPart.pos + new Vector2(Mathf.Lerp(-4f, 4f, UnityEngine.Random.value), 2f + Mathf.Lerp(-2f, 2f, UnityEngine.Random.value));
                    self.room.AddObject(new WaterDrip(pos3, new Vector2(0, 1), false));
                }
            }
        }
		
		
		//CHECK FOR ATTEMPTS TO STRUGGLE OUT OF DANGER
		if (BPOptions.fatArmor.Value && self.dangerGrasp != null && self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber != null)
		{
			float fatness = Mathf.Max(((GetChubValue(self) -2f) * 2f)+ (GetOverstuffed(self) / 2f), 0);
			fatness *= (1 + ((BPOptions.bpDifficulty.Value * 1.5f) / 10f));
			float escapeThresh = 0.003f * fatness; //0.06666667f
			//Debug.Log("ESCAPE THRESH " + escapeThresh + "WIGGLE " + self.wiggle);
			//if (UnityEngine.Random.value < Mathf.Lerp(0f, escapeThresh, self.Wiggle) && self.dangerGraspTime < 60)
			if (UnityEngine.Random.value < escapeThresh && self.dangerGraspTime >= 30 && self.dangerGraspTime < 60)
				self.grabbedBy[0].grabber.ReleaseGrasp(0);
				// self.grabbedBy[0].grabber.Stun(Random.Range(8, 16));
		}

        //EVERGREEN ROLLING
        if (ChunkyEvergreen(self))
        {
            if (self.bodyMode == Player.BodyModeIndex.Crawl && self.input[0].jmp && self.input[0].IntVec == new IntVector2(0, -1) && self.gourmandAttackNegateTime <= 0)
            {
                self.rollCounter = 8;
                self.rollDirection = self.flipDirection;
                self.stopRollingCounter = 0;
                self.canJump = 0;
                self.wantToJump = 0;
				self.animation = Player.AnimationIndex.Roll;
				self.input[1].jmp = true; //PRETEND WE WERE ALWAYS HOLDING THIS DOWN
            }
        }
    }



	public static void BPUUpdatePass3(Player self)
    {
		//MAKING THIS LIZARD CHANGE TO PLAYERS AS WELL SO SOUND DOESN'T CUT OUT WHEN RE-ENTERING A ROOM ANOTHER PLAYER IS STUCK IN
		bool offscreen = true;
		//if (self.graphicsModule == null)
		//OKAY THERE COULD BE LOTS OF CAMERAS... CHECK IF WE'RE ON ANY OF THEM
		for (int i = 0; i < self.room.game.cameras.Length; i++)
		{
			if (self.room.game.cameras[i].room == self.room)
				offscreen = false;
		}

		if (offscreen)
		{
			//Debug.Log("NO GRAPHICS MODULE! END THE SOUND");
            //offscreen = true;
			if (self.GetBelly().stuckLoop != null) //OTHERWISE WE CRASH
            {
				self.GetBelly().stuckLoop.alive = false;
				self.GetBelly().stuckLoop = null;
			}
		}
		
		
		//7/25/22 LETS GO WILD --------MAIN SQUEEZE LOOP ----------------
		if (self.GetBelly().squeezeLoop != null && GetChubValue(self) >= 2)
		{
			if (IsStuck(self) || self.GetBelly().isSqueezing == false || self.inShortcut) //(num == 0)
			{
				self.GetBelly().squeezeLoop.alive = false;
				self.GetBelly().squeezeLoop = null;
			}
			else
			{
				//FIND OUR X/Y VEL (NON-NEGATIVE) CHOOSE THE HIGHER OF THE TWO
				//float myVel = Mathf.Clamp(Math.Max(Math.Abs(self.mainBodyChunk.vel.x), Math.Abs(self.mainBodyChunk.vel.y) / 3f), 0f, 1f);
				float myVel = self.bodyChunks[1].vel.magnitude / 3f; //THIS IS SIMPLER
																	 //IF WE'RE OUT OF BREATH AND NOT BEING ASSISTED, CUT THE VOLUME. WE AREN'T MAKING PROGRESS.
				if (self.lungsExhausted) //DON'T WORRY ABOUT HELPER BOOST, THAT'S FACTORED IN BELOW
					myVel = 0f;

				//TAKE THE VALUE HALFWAY BETWEEN OUR CURRENT VEL AND OUR PREVIOUS VALUE
				float maxVol = 4.0f;
				float speedVar = Mathf.Min(Mathf.Lerp(self.GetBelly().myLastVel, myVel, 0.3f), maxVol) + (GetCappedBoostStrain(self) / 20f); // + (self.GetBelly().assistedSqueezing ? 0.3f : 0f);
				self.GetBelly().squeezeLoop.alive = true;
				//self.GetBelly().squeezeLoop.volume = Mathf.Lerp(0.8f, 0.3f, speedVar); //FOR SQUEEZE3 0.3f, 0.15f   //FOR SQUEEZE4 0.8f, 0.6f
				self.GetBelly().squeezeLoop.volume = Mathf.Lerp(0.4f, 0.6f, speedVar) * 2.5f; //0.4f, 0.8f
				self.GetBelly().squeezeLoop.pitch = Mathf.Lerp(1.5f, maxVol, speedVar); //0.4, 1.5
				//REMEMBER THAT VAL FOR NEXT TIME
				self.GetBelly().myLastVel = speedVar;
				//Debug.Log("-----SQUEEEAK!: " + speedVar + " " + self.GetBelly().inPipeStatus + " " + self.GetBelly().squeezeLoop.volume + " " + self.GetBelly().squeezeLoop.pitch + " " + self.GetBelly().stuckStrain);

			}
		}
		else if (!self.inShortcut && !IsStuck(self) && self.GetBelly().noStuck < 1 && (self.GetBelly().isSqueezing || self.GetBelly().assistedSqueezing)) //(num > 0f)
		{
			// self.GetBelly().squeezeLoop = self.room.PlaySound(BPEnums.BPSoundID.Pop1, self.mainBodyChunk, true, 0f, 0f, true); //Vulture_Jet_LOOP
			//CAN'T WE USE THE IN-GAME VERSION INSTEAD?...
			self.GetBelly().squeezeLoop = self.room.PlaySound(SoundID.Slugcat_Wall_Slide_LOOP, self.mainBodyChunk, true, 0f, 0f, true);
			self.GetBelly().squeezeLoop.requireActiveUpkeep = true;
		}


		//OK, --------SECOND ONE FOR STUCK LOOP --------
		if (self.GetBelly().stuckLoop != null && offscreen == false)
		{
			if (self.GetBelly().isSqueezing == false || !IsStuckOrWedged(self)) //(num == 0)
			{
				self.GetBelly().stuckLoop.alive = false;
				self.GetBelly().stuckLoop = null;
			}
			else
			{
				float myVel = self.bodyChunks[1].vel.magnitude / 3f; //THIS IS SIMPLER
				if (self.lungsExhausted) //IF WE'RE OUT OF BREATH AND NOT BEING ASSISTED, CUT THE VOLUME. WE AREN'T MAKING PROGRESS.
				{ //DON'T WORRY ABOUT HELPER BOOST, THAT'S FACTORED IN BELOW
					myVel = 0f;
				}
				//TAKE THE VALUE HALFWAY BETWEEN OUR CURRENT VEL AND OUR PREVIOUS VALUE
				float maxVol = 2.5f;
				float speedVar = Mathf.Min(Mathf.Lerp(self.GetBelly().myLastVel, myVel, 0.3f), maxVol) + (GetCappedBoostStrain(self) / 100f) + (self.GetBelly().assistedSqueezing ? 0.0f : 0f);
				//f(x) = 1/(100*(x-1.11)^2) == 0.87f AT FULL THRESHOLD
				float speedMod = (1f / (100f * Mathf.Pow(GetStuckPercent(self) - 1.11f, 2f))); //WOA WOA WOA, THIS LOOKS LIKE BAD STUFF WAITING TO HAPPEN!
				speedMod -= (Mathf.Max(self.GetBelly().tileTightnessMod - 200f, 0f)) / 3000; //NICE... THIS WORKED SURPRISINGLY WELL. DEEPENS PITCH AT EXSESSIVE TIGHTNESS
				//Debug.Log("-----MATH CHECK!: " + Mathf.Pow(((self.GetBelly().stuckStrain - (squeezeThresh / 3)) / squeezeThresh), 4) + " " + squeezeThresh);
				speedVar += speedMod;
				float volMod = 0;
				float myWedgeStrain = self.GetBelly().wedgeStrain;
                if (myWedgeStrain > 0f)
                {
					speedVar = ((myVel * 1f) + (GetCappedBoostStrain(self) / 50f)) / 1f; //+ self.GetBelly().wedgeStrain
					volMod = Mathf.Max(myWedgeStrain - 0.3f, 0) * 1.3f;
					if (GetAxisMagnitude(self) < 0.04f && self.GetBelly().boostStrain < 1)
                    {
						volMod = -1f;
					}
					 // Debug.Log("-----MATH CHECK!:" + GetAxisMagnitude(self));
                }
				//SAFETY CHECK IN CASE WE DEVIDED BY 0 UP THERE SOMEWHERE
				if (Double.IsNaN(speedVar))
					speedVar = 0f;
				//speedVar = Mathf.Min(1.9f, speedVar /= 2); //WAIT...WUT? /= ??
				speedVar = Mathf.Min(1.9f, speedVar / 2);

				//Debug.Log("-----MATH CHECK!:" + self.GetBelly().stuckStrain + " PERC" + GetStuckPercent(self) + " THRESHOLD " + (self.GetBelly().tileTightnessMod / 3) + " SPEEDMOD " + (speedMod * 1)); // Mathf.Abs(speedVar - self.GetBelly().myLastVel));
				if ((self.GetBelly().stuckStrain > (120 - self.GetBelly().tileTightnessMod / 4) || self.lungsExhausted) && self.GetBelly().tileTightnessMod > 300 && GetStuckPercent(self) < 0.5f && self.GetBelly().boostStrain < (self.GetBelly().assistedSqueezing ? 8 : 1))
					volMod = -1f;// 1f - (16f - GetCappedBoostStrain(self))/16f;//-1f;

                float pitchMod = (ObjIsSlick(self) ? 0.1f : 0f);
				float sfxVol = BPOptions.sfxVol.Value;
				//Debug.Log("SQUEEZE SFX" + (Mathf.Lerp(0.4f, maxVol, speedVar) + pitchMod));

				self.GetBelly().stuckLoop.alive = true;
				//self.GetBelly().stuckLoop.volume = Mathf.Lerp(0.35f, 0.1f, speedVar); //FOR SQUEEZE3 0.3f, 0.15f   //FOR SQUEEZE4 0.8f, 0.6f
				//self.GetBelly().stuckLoop.volume = Mathf.Lerp(0.26f, 0.07f, speedVar - volMod); //0.2f, 0.05f
				self.GetBelly().stuckLoop.volume = Mathf.Max(Mathf.Lerp(0.255f + sfxVol, 0.07f, speedVar - volMod), 0.15f + volMod); //0.2f, 0.05f
				self.GetBelly().stuckLoop.pitch = Mathf.Min(Mathf.Lerp(0.38f, maxVol, speedVar) + pitchMod, 1.9f); //0.5, 3.0
				//Debug.Log("-----MATH CHECK!:" + self.GetBelly().stuckLoop.pitch + " VOL " + self.GetBelly().stuckLoop.volume);
				self.GetBelly().myLastVel = speedVar; //REMEMBER THAT VAL FOR NEXT TIME
			}
		}
		else if (IsStuckOrWedged(self) && offscreen == false && (self.GetBelly().isSqueezing || self.GetBelly().assistedSqueezing)) //(num > 0f)
		{
			//self.GetBelly().stuckLoop = self.room.PlaySound(BPEnums.BPSoundID.SqueezeLoop, self.mainBodyChunk, true, 0f, 0f, true); //Vulture_Jet_LOOP
			self.GetBelly().stuckLoop = self.room.PlaySound(BPEnums.BPSoundID.SqueezeLoop, self.mainBodyChunk, true, 0f, 0f, true); //Vulture_Jet_LOOP
			self.GetBelly().stuckLoop.requireActiveUpkeep = true;
		}
	}



	//STUCK CHECK
	public static void BPUUpdatePass4(Player self, bool eu)
    {
		//SET OUR FLIP VALUES!
		//THIS NEEDS TO HAPPEN OUTSIDE OF CHECK STUCKAGE BECAUSE OF OUR PUSHERS
		//HOLD IT! LETS DO THIS A BIT MORE SMART... IF INP.X = 0, DEFAULT TO THE LAST USED VALUE, SO IT CONTINUES TO CALCULATE FOR OUR POSITION WE ARE STUCK IN UNTIL WE PUSH THE OTHER DIRECTION.
		//IF WE LET GO OF OUR X KEY, WE STILL WANT IT TO CALCULATE AS IF WE WERE FACING THAT DIRECTION. SO CONTINUE TO REMEMBER THAT DIRECTION UNTIL WE PRESS THE OPPOSITE DIRECTION
		if (!IsStuck(self)) //OK BUT ALSO... DON'T SWAP THEM WHILE STUCK, I GUESS.
		{
			
			//IF WE'RE WEDGED, IT SHOULD ALWAYS BE THIS
			if (ObjIsWedged(self))
            {
				//ALSO APPARENTLY OUR FLIP VALUES ARENT ALWAYS CORRECT SO LETS CORRECT THAT
				IntVector2 myVec = GetCreatureVector(self);
				if (myVec.x != 0)
                {
					self.GetBelly().myFlipValX = myVec.x;
					self.GetBelly().verticalStuck = false;
				}
				else if (myVec.y != 0)
                {
					self.GetBelly().myFlipValY = myVec.y;
					self.GetBelly().verticalStuck = true;
				}
			}

			//IF WE'RE ON SOMEONE BACK, IT SHOULD ALWAYS BE THIS
			if (self.onBack != null)
			{
                if (self.onBack.input[0].x != 0)
                    self.GetBelly().myFlipValX = self.onBack.input[0].x;
                if (self.onBack.input[0].y != 0)
                    self.GetBelly().myFlipValY = self.onBack.input[0].y;
            }
			
			//IF WE'RE CORRIDOR-FLIPPING, IT SHOULD BE THE OPPOSITE OF THE NONZERO NUMBER
			else if (self.corridorTurnDir != null)
			{
				if (self.corridorTurnDir.Value.x != 0)
					self.GetBelly().myFlipValX = -self.corridorTurnDir.Value.x;
				else
					self.GetBelly().myFlipValY = -self.corridorTurnDir.Value.y;
				
			}
			else
            {
				if (self.input[0].x != 0)
					self.GetBelly().myFlipValX = self.input[0].x;
				else if (self.bodyMode == Player.BodyModeIndex.Crawl)
					if (self.bodyChunks[1].vel.x > 0.1f)
						self.GetBelly().myFlipValX = 1;
					else if (self.bodyChunks[1].vel.x < -0.1f)
						self.GetBelly().myFlipValX = -1;


				//IF WE'RE IN THE AIR, DETERMINE YFLIP BY VELOCITY
				bool specialMode = self.bodyMode == Player.BodyModeIndex.CorridorClimb || self.bodyMode == Player.BodyModeIndex.ClimbIntoShortCut || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam;
				float gravMod = 0f;
				//HOLDING CORPSES WEIGHS US DOWN WHICH CAN BE USED TO CHEAT!
                if (specialMode)
				{
                    if ((self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed)) || (self.grasps[1] != null && self.HeavyCarry(self.grasps[1].grabbed)))
                         gravMod = specialMode ? 1.0f : 0f;//FOR FIXING HEAVY CORPSES BEING ABLE TO VERY SLOWLY PULL US DOWN THROUGH GAPS
                }
				
				if (self.input[0].y != 0 && specialMode)
					self.GetBelly().myFlipValY = self.input[0].y;
				else if (self.bodyChunks[1].vel.y > 0.1f + gravMod)
					self.GetBelly().myFlipValY = 1;
				else if (self.bodyChunks[1].vel.y < -0.1f + gravMod)
					self.GetBelly().myFlipValY = -1;
			}
		}


        //Debug.Log("---BODY WEIGHT CHECK: " + self.slugcatStats.bodyWeightFac + " - " + self.slugcatStats.name);
        //------------MAKE PIPE ENTRANCES MORE DIFFICULT TO GET INTO-------
        if ((GetChubValue(self) >= -1 || (self.slugcatStats.bodyWeightFac > 1.3 && self.GetBelly().bigBelly)) && self.room != null) //ALRIGHT, I GUESS THIS SHOULD RUN FOR EVERYONE...
		{
            

            //IF WE ENDED UP IN A SHORTCUT WAY TOO FAST, MAKE A FORCED SQUINCH SOUND
            if (self.enteringShortCut != null && self.GetBelly().noStuck <= 0 && self.bodyChunks[0].vel.magnitude > 7f && GetChubValue(self) >= 2)
			{
				self.room.PlaySound(BPEnums.BPSoundID.Squinch1, self.mainBodyChunk, false, 0.15f, 1.3f);
				PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.12f, 1f);
				if (BPOptions.debugLogs.Value)
					Debug.Log("---WE HIT THAT PIPE AWFULLY SPEEDY!: " + self.bodyChunks[0].vel.magnitude);
			}


			//BASIC CHECK TO SEE IF WE'RE ALL THE WAY INSIDE A CORRIDOR OR NOT.
			bool postHipCheck = false;
			if (!self.GetBelly().isStuck && self.room != null && self.room.aimap != null) //OH... GOTTA CHECK IF ROOM EXISTS FIRST
			{
				//bool hipsInNarrow = self.room.aimap.getAItile(self.room.MiddleOfTile(self.bodyChunks[1].pos)).narrowSpace;
				//OKAY THIS ISN'T WORKING. THEIR VERSION OF NARROWSPACE DOESN'T WORK. WE NEED TO USE OURS
				//Vector2 tChk = new Vector2(self.GetBelly().myFlipValX, self.GetBelly().myFlipValY - 0.5f) * -0.5f; //self.input[0].IntVec.ToVector2() 
				Vector2 tChk = new Vector2(0, 0); //FINE... THIS IS TOO BUGGY TO TRY AND GET IT FRAME PERFECT. WE'LL HAVE TO LEAVE IT IMPERFECT AND GIVE IT A FEW FRAMES
                bool hipsInNarrow = IsTileNarrowFloat(self, 1, tChk.x, tChk.y);
				bool torsoInNarrow = self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace;
				postHipCheck = hipsInNarrow; //SOME OPTIMIZATION I GUESS
				if (hipsInNarrow && !self.GetBelly().inPipeStatus) // || self.GetBelly().assistedSqueezing) //self.bodyMode == Player.BodyModeIndex.CorridorClimb
				{
					if (self.GetBelly().timeInNarrowSpace > 20) // && torsoInNarrow) //AFTER LONG ENOUGH, GIVE UP AND ACCEPT THE CHEAT
						self.GetBelly().inPipeStatus = true;
					else if (self.GetBelly().timeInNarrowSpace > 5) //ATTEMPT TO FIX CHEATING INTO PIPES!
						FixStuckCheat(self);
                    self.GetBelly().timeInNarrowSpace++; //WE'RE CHANGING THIS SO THAT ONLY OUR HIPS DETERMINE THIS
                }
				else if (!hipsInNarrow && !torsoInNarrow)
				{
					self.GetBelly().inPipeStatus = false;
					if (self.GetBelly().noStuck <= 0)
						self.GetBelly().timeInNarrowSpace = 0;
				}
			}
            //Debug.Log("-CHECKCHECK!: " + self.GetBelly().inPipeStatus + " - " + self.GetBelly().myFlipValY + " - " + self.bodyChunks[1].vel.y);
            CheckStuckage(self, eu);
			
			//A SPECIFIC CHECK TO CATCH CASES WHERE WE MIGHT BE FALLING INTO A PIPE SO FAST THAT WE DON'T REGISTER IT AT ALL
			if (postHipCheck && !self.GetBelly().isStuck && self.GetBelly().inPipeStatus == false && self.input[0].IntVec == new IntVector2(0, -1) && self.bodyChunks[1].vel.y < -8 && GetChubValue(self) >= 4 && self.GetBelly().noStuck <= 0)
			{
				//IF WE GET HERE, SOMEHOW OUR HIPS HAVE FALLEN INTO A PIPE WITHOUT GETTING STUCK
				if (IsTileNarrowFloat(self, 1, 0f, 1f) == false)
				{
					Debug.Log("---PIPE FALL SKIP DETECTED, REROUTING TO TRY AGAIN! ");
					self.bodyChunks[1].pos.y = self.room.MiddleOfTile(self.bodyChunks[1].pos).y + 13f;
					CheckStuckage(self, eu);
				}
				else
					Debug.Log("---PIPE FALL SKIP DETECTED, BUT NO VALID REROUTE DESTINATION FOUND! MAYBE WE MISSED IT? ");
			}
		}
		else if (self.room != null && self.room.aimap != null)
        {
			//FOR SKINNIES THAT DON'T RUN THAT UPDATE, CHANGE THIS VALUE SO WE DONT COUNT AS "CRAMPED" FOR THE REST OF OUR LIFE
			self.GetBelly().inPipeStatus = self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace;
		}


		//IF WE ARE STUCK, HOLDING A POLE, AND SITTING ON A DOWNWARDS PIPE ENTRANCE, LET GO THE POLE. NO GRABBING. OR ELSE PRESSING JUMP HERE WILL DO NOTHING
		if (IsStuck(self) && self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam && self.GetBelly().inPipeStatus == false && self.GetBelly().stuckVector.y == -1)
		{
            self.noGrabCounter = Math.Max(2, self.noGrabCounter);
            //Debug.Log("---SITTING ON PIPE? " + IsStuck(self) + " - " + self.bodyMode + " - " + self.GetBelly().inPipeStatus + " - " + self.GetBelly().stuckVector.y);
			//THIS STRAIGHT UP DOESN'T WOOOOORK WKY TH NOT!?!?!
        }


        //ACNHOR THE STRETCHING TO OUR LOWER BODY CHUNK WHILE STUCK
        //LETS TRY IT SLIGHTLY DIFFERENTLY...
        if (self.corridorTurnDir == null) //THE GAME NEEDS TO HANDLE THIS IF WE'RE FLIPPING DIRECTIONS
        {
			if (IsStuck(self) || (self.GetBelly().slugBed > 0 && self.input[0].x != 0)) //FOR SLUGBED ANIMATION STUFF
			{
				self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Pull;
			}
			else
				self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Normal;
		}

		//PREVENT THE WEIRD GLITCHY STRETCHING SLUGCAT WHEN LIZARDS GRAB US
		if (self.dangerGrasp != null || self.Stunned)
        {
			self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Normal;
			return; //AND THEN RETURN BEFORE ANY OF OUR BODY STRETCH STUFF CAN RUN, I THINK
		}
		
		//DON'T LET OUR TORSO STRETCH OUT AND DIP DOWN TO THE FLOOR FOR HORIZONTAL STUCKS
		if (IsStuck(self) && !self.GetBelly().verticalStuck)
		{
			if (self.bodyChunks[0].pos.y < self.GetBelly().stuckCoords.y - 2)
				self.bodyChunks[0].vel.y += 1f;

			//5/14/23 - SPECIAL FIXES
			if (!PipeStatus(self)) //ONLY APPLIES TO ENTERING AN ENTRANCE FROM OUTSIDE
			{
				//IF WE'RE TRYING TO PULL OURSELVES UP ONTO A LEDGE WHILE OUR HIPS HAPPEN TO BE PRESSED AGAINST AN ENTRANCE BELOW IT, DON'T GET STUCK ON IT
				if (self.animation == Player.AnimationIndex.LedgeGrab
                    && (self.bodyChunks[0].pos.y - self.GetBelly().stuckCoords.y > 10)) //AND OUR HEAD IS WAY ABOVE OUR STUCK POINT)
                {
					self.bodyChunks[1].pos.y += 5;
					self.bodyChunks[1].pos.x -= 1 * self.GetBelly().stuckVector.x;
					self.GetBelly().isStuck = false;
					self.GetBelly().verticalStuck = false;
                    //Debug.Log("--PIPE UN-SUCTION?");
                }
                
				//ALSO FIX THIS WEIRDNESS WITH GRAVITY SOMETIMES PULLING US OUT FROM THE CORNER. BUT NOT IF WE'RE TRYING TO JUMP PAST/OUT OF IT
                else if (!(self.input[0].x != 0 && self.input[0].x == -self.GetBelly().stuckVector.x) //AND WE'RE NOT TRYING TO BACK OUT
                && !(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y > 10) //AND OUR HEAD ISN'T HIGH ABOVE OUR TORSO
                && (self.bodyChunks[0].pos.x > self.bodyChunks[1].pos.x) == (self.GetBelly().stuckVector.x > 0)) //AND HEAD IS IN THE TUNNEL
                {
                    self.bodyChunks[1].pos.y = Mathf.Lerp(self.bodyChunks[1].pos.y, self.GetBelly().stuckCoords.y, 0.25f);
                    //Debug.Log("--PIPE SUCTION?");
				}
            }
        }

        //STRETCH OUT BASED ON STRAIN!
        //self.bodyChunkConnections[0].distance += Mathf.Min(self.GetBelly().boostStrain / 1.5f, 8f);
        float bodyStretch = Mathf.Min(GetCappedBoostStrain(self), 15f) * ((self.bodyMode == Player.BodyModeIndex.CorridorClimb) ? 2f : 0.7f);
        if (self.GetBelly().beingPushed > 0 || (self.GetBelly().verticalStuck && self.GetBelly().myFlipValY < 0))
            bodyStretch *= 0.6f;

		//GRAVITY IS TOO STRETCHY!
		if (self.GetBelly().verticalStuck && self.GetBelly().myFlipValY < 0)
			bodyStretch *= 0.25f;

		if (self.GetBelly().assistedSqueezing && IsGrabbedByPlayer(self) && !(self.GetBelly().verticalStuck && self.GetBelly().myFlipValY < 0))
			bodyStretch *= 3f;

		//bodyStretch *= 1.5f;
		//Debug.Log("BODY STRECHING!:" + Mathf.Sqrt(bodyStretch) + " ("+ bodyStretch + ")" + " TYPE:" + self.bodyChunkConnections[0].type + " DIST:" + self.bodyChunkConnections[0].distance);
		self.bodyChunkConnections[0].distance += Mathf.Sqrt(bodyStretch);
		
		//STRETCH THE HEAD!
		//(self.graphicsModule as PlayerGraphics).head.vel += self.GetBelly().stuckVector * 0.0007f * bodyStretch;

		//SAFETY MEASURE, BECAUSE APPARENTLY IT CAN STILL HAPPEN AND IDK WHY
		if (self.bodyChunkConnections[0].distance > 25f)
			self.bodyChunkConnections[0].distance = 25f;
		
		//IF WE'RE GETTING GIGANTIC, LET'S LET IMAGINATION TAKE HOLD
		float stuffing = GetOverstuffed(self) - (self.playerState.isPup ? 5 : 10);
        if (stuffing > 0f && !BellyPlus.VisualsOnly() && (self.animation != Player.AnimationIndex.GetUpToBeamTip))// || self.animation != Player.AnimationIndex.BeamTip)) //BeamTip
        {
            //if (stuffing > 10) //STUFFING PAST 10 WILL ONLY COUNT AS A THIRD OF IT'S NORMAL SIZE
            stuffing = 10 + ((stuffing - 10) / 2);

			if (IsCramped(self) || self.animation == Player.AnimationIndex.ClimbOnBeam || self.animation == Player.AnimationIndex.HangFromBeam) //CLIMBONBEAM NEEDS A SHRUNKEN TORSO TOO OR ELSE WE FALL OFF POLE TIPS
				stuffing /= 2f;
            if (self.GetBelly().inPipeStatus)
				stuffing = Mathf.Min(stuffing, 10f);
            self.bodyChunkConnections[0].distance += 0.25f * stuffing;

            //CAP THIS VALUE WHEN TRYING TO GET UP, IT CAUSES ISSUES!
            if (self.animation == Player.AnimationIndex.GetUpOnBeam && self.straightUpOnHorizontalBeam || self.animation == Player.AnimationIndex.GetUpToBeamTip)
                self.bodyChunkConnections[0].distance = Mathf.Min(20, self.bodyChunkConnections[0].distance); 
        }
		//Debug.Log("---SITTING ON PIPE? " + self.bodyChunkConnections[0].distance);
        

    }


    public static void FixStuckCheat(Player self)
	{
        //Debug.Log("FIXING PIPE STATUS! " + self.GetBelly().timeInNarrowSpace);
		//FLIP PIPE STATUS FOR THESE CHECKS, ELSE SOLID TILES WILL COUNT AS TIGHT
		self.GetBelly().inPipeStatus = !self.GetBelly().inPipeStatus;
        Vector2 setPos = self.bodyChunks[1].pos;
		if (!IsTileNarrowFloat(self, 1, 0f, -1.2f)) //CHECK 0.2 FARTHER OUT IN HOPES THAT WILL COVER OUR ROUGHLY 1.0 HEIGHT TORSO
			self.bodyChunks[1].HardSetPosition(setPos + new Vector2(0, -20f));
		else if (!IsTileNarrowFloat(self, 1, 0f, 1.2f))
			self.bodyChunks[1].HardSetPosition(setPos + new Vector2(0, 20f));
		else if (!IsTileNarrowFloat(self, 1, 1.2f, 0f))
			self.bodyChunks[1].HardSetPosition(setPos + new Vector2(20, 0f));
		else if (!IsTileNarrowFloat(self, 1, -1.2f, 0f))
			self.bodyChunks[1].HardSetPosition(setPos + new Vector2(-20, 0f));
		else
			//Debug.Log("UNABLE TO FIX!");
		self.GetBelly().inPipeStatus = !self.GetBelly().inPipeStatus;
    }




    public static float GetSweetSpotPerc(Player self)
    {
		
		float stuckPerc = GetProgress(self) / Mathf.Max(((self.GetBelly().tileTightnessMod) / 30) - 6, 0.1f);
		int boostCnt = self.GetBelly().boostCounter;
		//OKAY THIS MEANS WE SHOULD NEVER CALL THIS FROM OURSELF.. OR WE COULD CHEAT BY SPAMMING
		if (((boostCnt < -3 && boostCnt > -12) || boostCnt > 15) && stuckPerc != 0 && IsStuckOrWedged(self) && self.GetBelly().tileTightnessMod > 280)
		{
			float timingStrn = 0.2f + Mathf.Pow(-(boostCnt / 10f), 1.8f);
			if (timingStrn > 1f || boostCnt > 15)
				timingStrn = 1f;

			stuckPerc = Mathf.Min(1f, stuckPerc);
			float decay = 1f - Mathf.Max(0, (stuckPerc - 0.66f) * 3f);

			return (1f - stuckPerc) * timingStrn * decay;
		}
		else
			return 1f;
	}


	//PUSHING AND STRAINING
	public static void BPUUpdatePass5(Player self)
    {
		//----- MOVING SOME OF THE PUSHING CHECKS FROM SLUGCATHANDS IN HERE WHERE THEY BELONG-----
		
		//OKAY NO DON'T DO THIS IF WE'RE PIGGYBACKED. THAT MAKES WEIRD THINGS HAPPEN
		if (IsPiggyBacked(self))
			return;

        //----- CHECK IF WE'RE PUSHING ANOTHER CREATURE.------

        Player myPartner = FindPlayerInRange(self);
		LanternMouse mousePartner = patch_LanternMouse.FindMouseInRange(self);
		Cicada cicadaPartner = patch_Cicada.FindCicadaInRange(self);
		Yeek yeekPartner = patch_Yeek.FindYeekInRange(self);
		Lizard lizardPartner = FindLizardInRange(self, 0, 2);
		Scavenger scavPartner = patch_Scavenger.FindScavInRange(self);
		
		//DON'T HELP LIZARDS THAT ARE TRYING TO EAT YOUR FRIENDS
        if (lizardPartner != null)
        {
            if (lizardPartner.grasps[0] != null && lizardPartner.grasps[0].grabbed is Player)
                lizardPartner = null; //NOT VALID IF THE LIZARD IS CARRYING OUR FRIEND
        }

		Creature myObject = null;
		if (myPartner != null)
			myObject = (myPartner as Creature);
		else if (mousePartner != null)
			myObject = (mousePartner as Creature);
		else if (cicadaPartner != null)
			myObject = (cicadaPartner as Creature);
		else if (yeekPartner != null)
			myObject = (yeekPartner as Creature);
		else if (scavPartner != null)
			myObject = (scavPartner as Creature);
		else if (lizardPartner != null)
		{
			myObject = (lizardPartner as Creature);
		}
		
		//RESET BEFORE EACH CALCULATION IS RUN
        // self.GetBelly().pushingOther = false;
		
		if (myObject != null
			&& (IsStuckOrWedged(myObject) || ObjIsPushingOther(myObject)) 
			&& (!(myObject is Player) || !IsGraspingStuckable(self))
			&& (self.corridorTurnDir == null) //SO WE DON'T DO THAT WEIRD BUG WHERE WE FLING THEM FORWARD WHILE FLIPPING
		)
		{
			self.GetBelly().pushingCreature = myObject; //FOR OUR HANDS TO REFERENCE
			bool vertStuck = ObjIsVerticalStuck(myObject);
			if (!vertStuck && self.input[0].x == ObjGetXFlipDirection(myObject)
				|| (vertStuck && self.input[0].y == ObjGetYFlipDirection(myObject))
				|| self.simulateHoldJumpButton > 0 || self.isNPC)
            {
				//Debug.Log("-----PUSHING PLAYER!: " + vertStuck); //+ self.input[0].y + "_" + GetYFlipDirection(myObject));
                ObjPushedOn(myObject);
				self.GetBelly().pushingOther = 3; // PushedOther(self);
            }
		}
		else
			self.GetBelly().pushingCreature = null;
		
		

		
		//------MOVING THE SHIFT HERE------
		if (self.GetBelly().pushingOther > 0 || self.GetBelly().holdJump > 5)
		{
			self.forceSleepCounter = 0; //STOP FALLING ASLEEP!
			self.sleepCurlUp = 0;
			self.initSlideCounter = 0; //SO WE DON'T PIVOT-DASH THE WRONG WAY WHEN TRYING TO RAM
			
			if (myObject != null)
			{
				bool horzPushLine = ObjIsPushingOrPullingOther(myObject) && self.input[0].x != 0 && self.input[0].x == ObjGetXFlipDirection(myObject);
				bool vertPushLine = ObjIsPushingOrPullingOther(myObject) && self.input[0].y != 0 && self.input[0].y == ObjGetYFlipDirection(myObject);
				bool matchingShoveDir = ((ObjIsVerticalStuck(myObject) || vertPushLine) && self.input[0].y == ObjGetYFlipDirection(myObject)) || ((!ObjIsVerticalStuck(myObject) || horzPushLine) && self.input[0].x == ObjGetXFlipDirection(myObject)) || (ObjIsPushingOther(myObject) && self.input[0].IntVec != new IntVector2(0, 0)) || self.simulateHoldJumpButton > 0 || self.isNPC;
				float gravMod = Mathf.Lerp(0f, 0.9f, self.gravity);
				float slouch = (Mathf.Max(Mathf.Min(self.GetBelly().holdJump, 35) - 10f, 0f) / 2.3f) * (matchingShoveDir ? 1 : 0);

				//IF WE'RE AN NPC, TRY AND PUSH THE RIGHT DIRECTIONAL BUTTONS
				if (self.isNPC)
                {
					if (ObjIsVerticalStuck(myObject))
						self.input[0].y = (int)ObjGetStuckVector(myObject).y;
					else
						self.input[0].x = (int)ObjGetStuckVector(myObject).x;
				}

				if (!self.lungsExhausted && matchingShoveDir)
                    ObjGainSquishForce(myObject, 2, 2);

                if (!self.lungsExhausted && matchingShoveDir)
				{
					ObjGainStuckStrain(myObject, 0.5f);
                    ObjGainSquishForce(myObject, 2, 4);

                    // Debug.Log("---SHOVE STATS!: " + slouch + " _ " + self.GetBelly().holdJump);

                    if (PipeStatus(self) == false)
					{
						//OR FULL BODY SHOVES. RUN SPECIFICALLY ON THE FIRST FRAME OUR PARTNER RECEIVES A PUSH I GUESS
						if (slouch > 0 && ObjBeingPushed(myObject) == 4)
						{
							float shoveStr = Mathf.InverseLerp(32f, 25f, self.GetBelly().holdJump);
							// Debug.Log("SLOUCH SHOVE!: " + slouch + " STR:" + shoveStr + " HLD:" + self.GetBelly().holdJump);
							self.GetBelly().holdJump = 35;
							self.GetBelly().corridorExhaustion += 10 + Mathf.RoundToInt(10f * shoveStr);
							// BoostPartner(myPartner, 14, 16);
							ObjGainBoostStrain(myObject, 5, 12, 12);
							ObjGainSquishForce(myObject, 12, 12);
							ObjGainLoosenProg(myObject, 0.015f * shoveStr * (ObjIsSlick(myObject) ? 3f : 1f));
							ObjGainHeat(myObject, Mathf.RoundToInt(6f * shoveStr));
							self.AerobicIncrease(1f);
							self.GetBelly().boostStrain += Mathf.RoundToInt(10f * shoveStr);
							self.GetBelly().myHeat += Mathf.RoundToInt(50f * shoveStr);
							self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.2f * shoveStr, 0.6f);
							self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Medium, self.mainBodyChunk.pos, 1.0f, 1f);
							if (ObjIsSlick(myObject))
								MakeSquealch(myObject, shoveStr > 0.5f); //BIG SQUEALCH IF > 0.5
						}

						//ALSO. IF SLOUCHING. DO SOME EXTRA.
						if (slouch > 0)
						{
							self.GetBelly().boostStrain += 1;
							self.GetBelly().corridorExhaustion += 1;
							
							if (UnityEngine.Random.value < 0.5f)
								self.GetBelly().corridorExhaustion += 1; //HALF THE EXHAUSTION GAIN
							//bellyStats[FindPlayerInRange(self).playerState.playerNumber].stuckStrain += 1; //WAIT, WHY NOT?... MM MAYBE THAT WAS AN OOPSIE
							ObjGainStuckStrain(myObject, 0.5f);
							ObjGainBoostStrain(myObject, 1, 1, 10);
                            ObjGainSquishForce(myObject, 1, 5);
                            ObjGainLoosenProg(myObject, 0.0003f * (ObjIsSlick(self) ? 3f : 1f));
							if (UnityEngine.Random.value < 0.2f && self.graphicsModule != null)
							{
								Vector2 pos = GetHead(self).pos;
								self.room.AddObject(new StrainSpark(pos, new Vector2(self.input[0].x, 0) + Custom.DegToVec((360f * UnityEngine.Random.value) + 0f) * 6f * UnityEngine.Random.value, 15f, Color.white));
							}
							//SHOVE SHOULDER DOWN INTO THE REAR
							if (self.input[0].y == -1)
							{
								self.bodyChunks[0].pos.x = Mathf.Lerp(self.bodyChunks[0].pos.x, ObjGetBodyChunkPos(myObject, "rear").x, 0.2f);
							}
							if (self.input[0].x != 0 && self.bodyChunks[0].vel.y < 0)
							{
								self.bodyChunks[0].vel.y += 0.2f * gravMod;
							}

							if (self.graphicsModule != null)
                            {
                                GetHead(self).vel += self.input[0].IntVec.ToVector2();
								//self.graphicsModule.objectLooker.LookAtPoint(self.bodyChunks[0].pos + self.input[0].IntVec.ToVector2() * 60, 10);
							}
						}
					}
				}

				//IF IT'S A PUSHING/PULLING LINE, PASS FORWARD THE BENEFITS!
				//MORE LIKE; IF WE, THE PUSHER, IS ALSO BEING PUSHED, INCREASE THE STRENGTHH OF OUR PUSHING!
				if (self.GetBelly().beingPushed > 0)
                {
					ObjGainStuckStrain(myObject, 0.35f);
					//SO THENNN I GUESS EVERY HELPER AFTER THE SECOND PUSHER IS NOT REALLY RELEVANT. BUT OH WELL
					// Debug.Log("---OBJ BEING PUSHED?: ");
				}
					

				//CALCULATE A BOOST STRAIN MODIFIER THAT LOOKS A BIT SMOOTHER
				float pushBoostStrn = ((self.GetBelly().boostStrain > 4) ? 4 : GetCappedBoostStrain(self)) + slouch;

				float stuckPerc = GetProgress(self) / Mathf.Max(((self.GetBelly().tileTightnessMod) / 30) - 6, 0.1f);

				//WE NEED TWO SEPERATE FNS FOR VERTICAL/HORIZONTAL
				if (vertPushLine || ObjIsVerticalStuck(myObject) && (matchingShoveDir || slouch > 0))
				{
					//ALRIGHT, TRYING TO PUSH DOWN ON STUCK LIZARDS IS TOO WONKY. WE NEED TO BE PUSHING ON THEIR MIDDLE INSTEAD, UNLESS IN A CORRIDOR
					string pushLocation = (ObjGetStuckVector(myObject).y == -1 && !PipeStatus(myObject)) ? "middle" : "rear";
					
					//CORRECT OUR X FACING SO OUR BACK BENDS THE CORRECT WAY
					if (self.input[0].y == -1)
					{
						if (self.bodyChunks[1].pos.x < ObjGetBodyChunkPos(myObject, "middle").x)
						{
                            self.GetBelly().myFlipValX = 1;
							self.flipDirection = 1;
                        }
						else
						{
                            self.GetBelly().myFlipValX = -1;
                            self.flipDirection = -1;
                        }
					}

					
					//DON'T LET THIS WEIRDNESS HAPPEN IF WE DON'T COLLIDE WITH PLAYERS (TO FIX PUSHING DOWNWARD UPROOTING OUR STUCK PLAYERS
					if (ObjGetStuckVector(myObject).y == -1 && self.bodyChunks[0].pos.y < ObjGetBodyChunkPos(myObject, pushLocation).y )
                    {
						//self.bodyChunks[0].pos.y = ObjGetBodyChunkPos(myObject, pushLocation).y + 3f;
						//self.bodyChunks[0].pos.y = ObjGetBodyChunkPos(myObject, pushLocation).y + myObject.bodyChunks[ObjGetBodyChunkID(myObject, pushLocation)].rad + self.bodyChunks[0].rad;
						self.bodyChunks[0].pos.y += 2; //IDK MAN...
                        self.bodyChunks[0].vel.y = 0;
                    }
					
					float pushBack = 22f - Mathf.Abs(ObjGetBodyChunkPos(myObject, pushLocation).y - self.bodyChunks[0].pos.y) + (vertPushLine ? 10 : 0); // + (self.GetBelly().boostStrain / 5f);
					pushBack -= pushBoostStrn; // (self.GetBelly().boostStrain / 2); //BOOST STRAIN VISUALS
					// Debug.Log("---I'M PUSHING Y! LETS SHOW SOME EFFORT: " + pushBack);
					pushBack = Mathf.Max(pushBack, 0);
					pushBack *= self.GetBelly().myFlipValY * gravMod;
					self.bodyChunks[0].vel.y -= pushBack;
					
					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.y < 0 != self.input[0].y < 0)
						self.bodyChunks[0].vel.y /= 3f;

					//DON'T CLIMB POLES WHEN PUSHING DOWNWARDS
					if (self.input[0].y == -1 && self.EffectiveRoomGravity > 0.5f)
						self.noGrabCounter = 2;

					if (self.bodyMode == Player.BodyModeIndex.Crawl)
						self.bodyMode = Player.BodyModeIndex.Stand;
				}
				else if (horzPushLine || !ObjIsVerticalStuck(myObject) && (matchingShoveDir  || slouch > 0))
				{
					float pushBack = Mathf.Max((25f - pushBoostStrn + (horzPushLine ? 1 : 0) - (self.GetBelly().inPipeStatus ? 0f : 0f) - Mathf.Abs(ObjGetBodyChunkPos(myObject, "rear").x - self.bodyChunks[0].pos.x - (BonusChunkRad(myObject, 1) * self.GetBelly().myFlipValX))) * (self.GetBelly().inPipeStatus ? 0.5f : 1f), 0f);
					//pushBack = Mathf.Max(pushBack * (1f - slouch/20f), 0);
					pushBack *= self.GetBelly().myFlipValX * gravMod;
					// Debug.Log("---I'M PUSHING X! LETS SHOW SOME EFFORT: " + pushBack + " " + self.bodyChunks[0].vel.x);

					//IF THEYRE A TILE ABOVE US, REDUCE ALL THIS
					if (Mathf.Abs(ObjGetBodyChunkPos(myObject, "middle").y - self.bodyChunks[0].pos.y) > 10)
						pushBack /= 3f;

					//CHECK FOR RUNNING START!
					if (self.slideCounter > 0 || self.simulateHoldJumpButton > 0 || self.animation == Player.AnimationIndex.BellySlide) //self.animation == Player.AnimationIndex.RocketJump
					{
						self.slideCounter = 0;
						self.rocketJumpFromBellySlide = false;
						self.simulateHoldJumpButton = 0; //THIS IS FROM THE SUPER CROUCH JUMP
						if (self.animation == Player.AnimationIndex.BellySlide)
							self.animation = Player.AnimationIndex.None;
						
						if (!self.lungsExhausted && ObjIsStuck(myObject))
						{
							//APPLY A BIG BOOST!
							if (BPOptions.debugLogs.Value)
								Debug.Log("MOMENTUM SHOVE!: ");
							self.GetBelly().corridorExhaustion += 80;
							ObjSetFwumpDelay(myObject, 12);
							ObjGainBoostStrain(myObject, 5, 15, 22);
                            ObjGainSquishForce(myObject, 15, 22);
                            ObjGainHeat(myObject, 100);
							ObjSetFwumpFlag(myObject, 6);
							bonusMomentum = Mathf.Max((GetChubValue(self) * 4 ) + (GetOverstuffed(self) * 3), 0f) + (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Gourmand ? 40 : 0);
							self.AerobicIncrease(2f);
							//IF WE WERE JUST SHOVED, POP OUR EYES OPEN
							if (ObjGetHeat(myObject) < 450)
							{
								// bellyStats[myPartner.playerState.playerNumber].myHeat = 350;
								ObjGainHeat(myObject, 250);
								ObjSetWideEyes(myObject, 45);
								// myPartner.bodyChunks[0].vel.y += 8f; //THIS DON'T DO JACK ANYWAYS 
								Vector2 pos = ObjGetHeadPos(myObject); 
								float lifetime = 6f;
								float innerRad = 8f;
								float width = 6f;
								float length = 20f;
								int spikes = 6;
								ExplosionSpikes myPop = new ExplosionSpikes(self.room, pos, spikes, innerRad, lifetime, width, length, new Color(1f, 1f, 1f, 0.5f));
								self.room.AddObject(myPop);
							}
							else
                            {
								//WEHH, IM GONNA BE LAZY...
								Vector2 pos = myObject.bodyChunks[1].pos;
								float lifetime = 6f;
								float innerRad = 8f;
								float width = 6f;
								float length = 10f;
								int spikes = 8;
								ExplosionSpikes myPop = new ExplosionSpikes(self.room, pos, spikes, innerRad, lifetime, width, length, new Color(1f, 1f, 1f, 0.5f));
								self.room.AddObject(myPop);
							}
							self.GetBelly().boostStrain += 15;
							self.GetBelly().myHeat += 75;
							self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.6f, 0.6f);
							self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Medium, self.mainBodyChunk.pos, 1.4f, 1f);
							self.jumpChunkCounter = 15; //DON'T COMBINE WITH A NORMAL SHOVE. TOO OP

							//FALL OVER IF THEY'RE SLICK
							if (ObjIsSlick(myObject) && UnityEngine.Random.value > 0.5f)
                            {
								self.standing = false;
                            }
							//self.standing = false;
							self.bodyChunks[0].vel.y += 4f;
							self.bodyChunks[0].vel *= 1.5f;
							self.bodyChunks[1].vel *= 1.5f;
							//self.Stun(5);
						}
						else 
						{
							//IF WE WERE EXHAUSTED. JUST SLOUCH ON EM INSTEAD
							//THIS'LL KINDA BE LIKE A SLOUCH SHOVE.... FOR 1 FRAME...
							pushBoostStrn += 35;
							self.standing = false; //FALLING OVER IS MORE FUN IMO~
							ObjGainBoostStrain(myObject, 0, 6, 12);
							self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Medium, self.mainBodyChunk.pos, 1.1f, 1f);
						}
					}

                    //Debug.Log("PUSHBACK SHOVE!: " + pushBack);
					bool isPullLine = ObjIsPullingOther(myObject);
					self.bodyChunks[0].vel.x -= pushBack * (isPullLine ? 1.5f : 1f);
					self.bodyChunks[1].vel.x -= (pushBack + (1.2f * self.GetBelly().myFlipValX) * (self.GetBelly().inPipeStatus ? 0f : 1f))  * 1.2f * (isPullLine ? 0.5f : 1f) ;

					//CHECK IF WE'RE MOVING BACKWARDS WHILE PUSHING. IF WE ARE, CUT THE VEL IN HALF. WE DON'T WANT TO BE MOVING BACKWARDS
					if (self.bodyChunks[0].vel.x < 0 != self.input[0].x < 0)
						self.bodyChunks[0].vel.x /= (matchingShoveDir ? 2.5f : 1); //THIS PREVENTS THE HEAVY JITTER WHILE STRUGGLING

					if (self.bodyChunks[1].vel.x < 0 != self.input[0].x < 0)
						self.bodyChunks[1].vel.x /= (matchingShoveDir ? 2.5f : 0.8f); //OK THEY NEED TO BE SEPERATE
				}
			}

			//IF WE'RE PUSHING DOWNWARDS, DEFAULT BACK TO STANDING!
			if (self.input[0].y != -1 && self.input[1].y == -1)
				self.standing = true;
		}


		//ALRIGHT. FUN NEW ALT WAY TO PUSH BY HOLDING DOWN SPACE.
		if (self.input[1].jmp && self.input[0].jmp && !self.lungsExhausted && (self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther))
		{
			if (self.GetBelly().holdJump < 35)
				self.GetBelly().holdJump += 2;
		}
		else if (self.GetBelly().holdJump > 1)
		{
			self.GetBelly().holdJump--;
			if (self.GetBelly().holdJump < 15) //DIP BACK DOWN. OTHERWISE REPEATED TAPPING WILL EVENTUALLY OUTWEIGH THE DECAY
				self.GetBelly().holdJump = 0;
		}


		//PRESS JUMP TO GIVE OURSELVES A BOOST
		bool matchingStuckDir = (IsVerticalStuck(self) && self.input[0].y != 0) || (!IsVerticalStuck(self) && self.input[0].x != 0) || self.isNPC;
		if (((self.input[0].jmp && !self.input[1].jmp && self.GetBelly().boostCounter < 10) || (self.isNPC && self.GetBelly().boostCounter <= 0)) && (!self.lungsExhausted || self.submerged) && ((self.GetBelly().isStuck && matchingStuckDir) || self.GetBelly().pushingOther > 0 || self.GetBelly().pullingOther || ObjIsWedged(self)))
		{
			
			//BEING WEDGED, FOR SOME REASON, IS HUGELY AFFECTED BY BOOSTSTRAIN. SO WE GOTTA LOWER IT FOR STRAINS
			if (ObjIsWedged(self))
				ObjGainBoostStrain(self, 0, 4, 8);
			else
                ObjGainBoostStrain(self, 0, 10, 18);


			self.GetBelly().corridorExhaustion += 25; //30
			float boostAmnt = 18;
			float loosenAmnt = boostAmnt;
			self.AerobicIncrease(0.2f);

			float strainMag = 15f * GetExhaustionMod(self, 60);
			if (self.slugcatStats?.name?.value == "Estranged")
                boostAmnt = 14;

            if (self.GetBelly().boostCounter > 1)
			{
				boostAmnt /= 3;
				boostAmnt *= 2; //OK SO NOW IT ENDS UP 2/3rds
				//self.GetBelly().boostStrain = Math.Min(self.GetBelly().boostStrain - 4, 18);
				self.GetBelly().boostStrain /= 2;
				self.room.PlaySound(SoundID.Slugcat_Skid_On_Ground_Init, self.mainBodyChunk, false, 1.2f, 1.2f);
				//Slugcat_Skid_On_Ground_Init //Slugcat_Regain_Footing
				strainMag += 15f;
				self.GetBelly().struggleHintCount += 28;
			}
			/*
			else if (self.GetBelly().boostCounter < -(8 + stuckPerc * 10) && self.GetBelly().boostCounter > -(12 + stuckPerc * 30))
			{
				float timingStrn = Mathf.InverseLerp(-(22 + stuckPerc * 30), -(8 + stuckPerc * 10), self.GetBelly().boostCounter);
				strainMag += 15f * (1 - stuckPerc);
				boostAmnt *= 1.5f * (1 - stuckPerc);
				loosenAmnt *= 6f * (1 - stuckPerc);
				//self.GetBelly().boostStrain *= Mathf.CeilToInt(2f * (1f - stuckPerc));
				ObjGainBoostStrain(self, 0, Mathf.CeilToInt(10 * (1 - stuckPerc)), 20);
				//self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.6f * timingStrn, 0.5f);
				self.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.mainBodyChunk.pos, 0.2f + 0.8f * timingStrn, 0.7f + 0.4f * timingStrn);
				
				Debug.Log("---SUPER SHOMVE " + stuckPerc + " TIMING " + timingStrn);
				//self.GetBelly().boostBeef = Mathf.CeilToInt(5 + stuckPerc * 20);
				self.GetBelly().boostBeef = Mathf.CeilToInt(15 + stuckPerc * 10);
			}
			*/
			//Debug.Log("---SSTRAIN COMPARE " + self.GetBelly().myLastVel + " 2ND: " + (self.GetBelly().myLastVel - self.GetBelly().lastBoost));

			/*
			//self.GetBelly().myLastVel >= self.GetBelly().lastBoost - 0.001f && self.GetBelly().boostCounter <= 0 && 
			if (self.GetBelly().boostCounter < -5 && self.GetBelly().boostCounter > -12 && stuckPerc != 0 && !Double.IsNaN(stuckPerc) && IsStuckOrWedged(self) && self.GetBelly().tileTightnessMod > 280)
            {
				float timingStrn = 1f; // Mathf.InverseLerp(-(22 + stuckPerc * 30), -(8 + stuckPerc * 10), self.GetBelly().boostCounter);
				// timingStrn += (self.GetBelly().boostCounter / 30f);
				timingStrn = 0.2f + Mathf.Pow(-(self.GetBelly().boostCounter / 10f), 1.8f);
				if (timingStrn > 1f)
					timingStrn = 1f;

				stuckPerc = Mathf.Min(1f, stuckPerc);
				float decay = 1f - Mathf.Max(0, (stuckPerc - 0.66f) * 3f);
				
				if (decay <= 0.1)
					self.GetBelly().boostStrain /= 2;
				else
				{
					// strainMag += 15f * (1 - stuckPerc);
					strainMag += (10f + (5f * (1 - timingStrn))) * decay;
					boostAmnt *= 0.7f + 0.8f * (1 - stuckPerc) * timingStrn * decay;
					loosenAmnt *= 2f + 5f * (1 - stuckPerc) * timingStrn * decay;
					//ObjGainBoostStrain(self, 0, Mathf.CeilToInt(10 * (1f - (stuckPerc * 2f))), 20);
					ObjGainBoostStrain(self, 0, Mathf.CeilToInt(10 * (1f - (stuckPerc * 2f)) * decay), 20);
					//self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.6f * timingStrn, 0.5f);
					self.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.mainBodyChunk.pos, (0.2f + 0.6f * timingStrn) * decay, 0.7f + 0.4f * timingStrn);
					if (BPOptions.debugLogs.Value)
						Debug.Log("---SUPER SHOMVE " + stuckPerc + " TIMING " + timingStrn + " DECAY " + decay);
					//self.GetBelly().boostBeef = Mathf.CeilToInt(5 + stuckPerc * 20);
					self.GetBelly().boostBeef = Mathf.CeilToInt((10 + stuckPerc * 30) * decay); // * timingStrn);
					self.GetBelly().boostCounter += Mathf.CeilToInt(2 * decay);
				}
			}
			*/

			else if (self.GetBelly().wiggleCount > 0f)
            {
				float timingStrn = self.GetBelly().wiggleCount / 10f;

				strainMag += (10f + (5f * (1 - timingStrn))) ;
				boostAmnt *= 0.7f + 0.8f * timingStrn ;
				loosenAmnt *= 2f + 10f * timingStrn;
				ObjGainBoostStrain(self, 0, Mathf.CeilToInt(10 * timingStrn), 20);
				//self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.6f * timingStrn, 0.5f);
				self.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.mainBodyChunk.pos, (0.2f + 0.6f * timingStrn), 0.7f + 0.4f * timingStrn);
				if (BPOptions.debugLogs.Value)
					Debug.Log("---SUPER SHOMVE " + 0 + " TIMING " + timingStrn + " DECAY " + 0);
				//self.GetBelly().boostBeef = Mathf.CeilToInt(5 + stuckPerc * 20);
				self.GetBelly().boostBeef = Mathf.CeilToInt((5 + timingStrn * 20)); // * timingStrn);
				self.GetBelly().boostCounter += Mathf.CeilToInt(1.5f * timingStrn);

				//self.GetBelly().corridorExhaustion += Mathf.CeilToInt(10 + 25f * timingStrn);
				if (self.GetBelly().wiggleCount > 2f)
					self.GetBelly().corridorExhaustion *= 2;

				self.GetBelly().wiggleCount = 0f;
			}


			else
				self.GetBelly().boostBeef = 0;

			//EXTRA STRAIN PARTICALS!
			if (self.graphicsModule != null)
			{
				Vector2 pos = GetHead(self).pos;
				float lifetime = 5f;
				self.room.AddObject(new ExplosionSpikes(self.room, pos, 8, 8f, lifetime, 5.0f, strainMag, new Color(1f, 1f, 1f, 0.5f)));
				//STRAIN SOUND WHEN RUNNING LOW ON STAMINA
				self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, GetExhaustionMod(self, 60) / 1f, 0.6f);
			}
			
			self.GetBelly().boostCounter = 15;

			if (self.GetBelly().isStuck)
			{
				self.GetBelly().stuckStrain += boostAmnt * ((GetLivingPlayers(self) > 1) ? 0.6f : 1f);
				float loosenMod = (GetLivingPlayers(self) > 1) ? 3000f : 2000f;
				ObjGainLoosenProg(self, (loosenAmnt * (ObjIsSlick(self) ? 3f : 1f)) / loosenMod); //BOOSTING NOW CONTRIBUTES TO LOOSEN PROGRESS 
				//self.bodyChunks[0].vel = new Vector2(self.input[0].x *2, self.input[0].y * 2); //IT'S A FUN ATTEMPT, BUT I THINK IT'S LEADING TO SOME CHEATING
				if (self.GetBelly().verticalStuck)
					self.GetBelly().myFlipValX *= -1; //SO OUR TAIL WAGGLES~
				else
					self.GetBelly().myFlipValY *= -1;
				//UNIQUE INTERACTION WITH NOIRCAT!
                if (self.slugcatStats?.name?.value == "NoirCatto" && UnityEngine.Random.value < 0.2f)
					DoNoirSounds(self, false);
			}
			if (self.GetBelly().pushingOther > 0)
			{
				boostAmnt += Math.Max(GetChubValue(self), 0);
				//UNIVERSAL!!
				//Player myPartner = FindPlayerInRange(self);
				//LanternMouse mousePartner = patch_LanternMouse.FindMouseInRange(self);
				//Cicada cicadaPartner = patch_Cicada.FindCicadaInRange(self);
				//Yeek yeekPartner = patch_Yeek.FindYeekInRange(self);
				//Lizard lizardPartner = FindLizardInRange(self, 0, 2);
				
				//Creature myObject = null;


				if (myObject is Player)
                {
					boostAmnt *= 1 + (1 - GetSweetSpotPerc(myObject as Player));
					loosenAmnt *= 1 + 2f * (1 - GetSweetSpotPerc(myObject as Player));
				}
				else if (myObject is Lizard)
				{
					if (!patch_Lizard.IsTamed(myObject as Lizard))
					{
						boostAmnt *= 2f; //BIG ADRENALINE BOOST IF WE ARE PLAYING WITH DANGER
                        loosenAmnt *= 3f;
                    }
                }
					

				//Debug.Log("CREATURRR?!: " + myObject + " ISNULL?" + );
				if (myObject != null && (self.input[0].x != 0 || self.input[0].y != 0))
				{					
					ObjGainStuckStrain(myObject, boostAmnt / 2f);
					ObjGainLoosenProg(myObject, (loosenAmnt / 6000f) * (ObjIsSlick(myObject) ? 3f : 1f));  // 8000f
					//if (ObjIsPushingOther(myObject) == false) //DON'T APPLY THIS TO PUSHING LINES. WE USE BOOSTSTRAIN AS A TIMER
					ObjGainBoostStrain(myObject, 0, 8, 14); //WHY DID WE SKIP THIS?
                    ObjGainSquishForce(myObject, 9, 15);
                    // Debug.Log("HEAVE!: ");

                    //SQUEALCH SOUNDS IF SLICK!
                    if (ObjIsSlick(myObject))
					{
						MakeSquealch(myObject, BPOptions.blushEnabled.Value); //false
						ObjGainHeat(self, 20);
					}

					//APPLY LATHER, IF POSSIBLE!
					if (IsStuckOrWedged(myObject) && CheckApplyLather(self, myObject))
					{
						ObjApplySlickness(myObject);
					}
				}
			}
			else if (self.GetBelly().pullingOther && self.grasps[0] != null)
			{
                Creature myGrasped = GetGraspedCreature(self);
				if (myGrasped != null)
				{
                    boostAmnt += Math.Max(GetChubValue(self), 0);
                    if (myGrasped is Player)
                    {
                        boostAmnt *= 1 + (1 - GetSweetSpotPerc(myGrasped as Player));
                        loosenAmnt *= 1 + 2f * (1 - GetSweetSpotPerc(myGrasped as Player));
                    }
                    PassDownBenifits(myGrasped, boostAmnt / 2f, 10, 14);
                    ObjGainLoosenProg(myGrasped, (loosenAmnt / 4000f) * (ObjIsSlick(myGrasped) ? 3f : 1f));
                }
			}
			if (ObjIsWedged(self))
			{
				float loosenMod = (GetLivingPlayers(self) > 1) ? 10000f : 7000f;
				//self.GetBelly().loosenProg += (boostAmnt * (ObjIsSlick(self) ? 3f : 1f)) / loosenMod;
				ObjGainLoosenProg(self, (boostAmnt * (ObjIsSlick(self) ? 3f : 1f)) / loosenMod);
				self.room.PlaySound(SoundID.Slugcat_In_Corridor_Step, self.mainBodyChunk, false, 0.6f + self.GetBelly().wedgeStrain * 2, 0.6f + self.GetBelly().wedgeStrain / 2f);
				if (patch_Player.ObjIsSlick(self))
					self.room.PlaySound(SoundID.Swollen_Water_Nut_Terrain_Impact, self.mainBodyChunk, false, 1.0f, 1f); //Tube_Worm_Shoot_Tongue
			}

			if (self.graphicsModule != null)
			{
				for (int n = 0; n < 3; n++) //STRAIN DRIPS
				{
					Vector2 pos3 = GetHead(self).pos;
					float xvel = self.GetBelly().isStuck ? 4 : 2;
					self.room.AddObject(new StrainSpark(pos3, new Vector2((float)self.flipDirection * xvel, Mathf.Lerp(-2f, 6f, UnityEngine.Random.value)), 20f, Color.white));
				}
			}
		}
	}



	//POST UPDATE 
	public static void BPUUpdatePass6(Player self)
    {

		//THIS MIGHT HELP OUR FACING WHILE PULLING - IT REALLY DOESN'T
		//if (self.GetBelly().pullingOther)
		//	self.flipDirection = -self.GetBelly().myFlipValX;

		if (self.GetBelly().noStuck > 0)
			self.GetBelly().noStuck--;
		
		//SLOWLY REDUCE THIS VALUE IF NOT STUCK.
		if (!IsStuck(self) && !ObjIsWedged(self) && self.GetBelly().loosenProg > 0)
			self.GetBelly().loosenProg -= 1f / 2000f;

		if (self.GetBelly().boostCounter > 0 || (self.GetBelly().myLastVel >= self.GetBelly().lastBoost - 0.001f && self.GetBelly().boostCounter > -30))
			self.GetBelly().boostCounter--;

        if (self.GetBelly().squishForce > 0)
            self.GetBelly().squishForce--;
		
		if (self.GetBelly().squishDelay > 0)
            self.GetBelly().squishDelay--;
		else if (self.GetBelly().squishMemory > 0) //ONCE THE DELAY REACHES 0, ADD OUR MEMORY TO THE CURRENT SQUISH
		{
			self.GetBelly().squishForce = self.GetBelly().squishMemory;
			self.GetBelly().squishMemory = 0;
		}
		
		if (self.GetBelly().rollingOther > 0)
			self.GetBelly().rollingOther--;
		
		if (self.GetBelly().beingRolled > 0)
			self.GetBelly().beingRolled--;

        if (self.GetBelly().slugBed > 0)
            self.GetBelly().slugBed--;
		else if (self.GetBelly().slugBed < 0)
            self.GetBelly().slugBed++;
		
		if (self.GetBelly().pushingOther > 0)
			self.GetBelly().pushingOther--;

        //IF WE'RE ABOUT TO EAT POPCORN, ADD IT TO OUR BELLY.
        //if (self.eatExternalFoodSourceCounter == 2) //1 FRAME AWAY FROM TAKING A CHOMP
        //	AddPersonalFood(self, 1); //NOT NEEDED ANYMORE


        //SLOW DOWN OUR EATING IF WE'RE STUFFED
        int stuffing = GetOverstuffed(self);
		if (stuffing > 0)
		{
			//stuffing += 0;
			float chance = Mathf.Max(1f - (0.2f * stuffing), 0.5f);
			//1 - 0.5 BASED ON HOW STUFFED
			if (self.eatExternalFoodSourceCounter > 2)
			{
				self.Blink(5);
				if (UnityEngine.Random.value > chance)
					self.eatExternalFoodSourceCounter++;
			}
			
			if (self.dontEatExternalFoodSourceCounter > 0 && UnityEngine.Random.value > chance && self.abstractCreature.world.game.IsArenaSession)
				self.dontEatExternalFoodSourceCounter++;
			
			if (self.eatCounter > 1 && self.eatCounter < 39)
			{
				//GOOEY DUCKS BREAK THIS, SO SKIP IT FOR GOOEY DUCKS
				bool duckSkip = false;
				for (int j = self.grasps.Length - 1; j >= 0; j--)
				{
					if ((self.grasps[j] != null && self.grasps[j].grabbed is GooieDuck))
						duckSkip = true;
				}
				if (!duckSkip)
				{
					self.Blink(5);
					if (self.eatCounter <= 15 && UnityEngine.Random.value > 1f- Mathf.Min((0.15f * stuffing), 0.6f))
						self.eatCounter++;
				}
			}
		}
		
		//MAKE SURE WE DIDN'T GET EJECTED INTO A SHORTCUT EXIT THAT WAS OCCUPIED AND SHOVED THROUGH A WALL
		//if (self.GetBelly().clipCheck > 0)
		//	self.GetBelly().clipCheck--;
		
		//THIS CHECK HARDLY SEES ANY PRACTICAL USE AND ONLY SEEMED TO GET TRIGGERED BY ACCIDENT. I'M GETTING RID OF IT
		// if (self.GetBelly().clipCheck == 1 && self.room != null && self.IsTileSolid(1, 0, 0) && self.IsTileSolid(0, 0, 0))
		// {
			// self.enteringShortCut = self.GetBelly().lastShortcut;
			// Debug.Log("-CONGRADULATIONS! YOU'RE GOING TO BRAZIL");
		// }
		
		
		//SO SAINT TONGUE WON'T UNSTICK IF PUSHING SOMEONE
		if (ObjIsPushingOrPullingOther(self) && self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
		{
			self.tongueAttachTime = 0; 
		}
		
		//BRING BACK THIS CLASSIC JOLLYCOOP FEATURE THAT IS VERY MUCH NEEDED IN CO-OP
		if (self.input[0].jmp && !self.input[1].jmp && self.grabbedBy?.Count > 0 && !IsStuckOrWedged(self) && !InPullingChain(self))
		{
			for (int graspIndex = self.grabbedBy.Count - 1; graspIndex >= 0; graspIndex--)
			{
				if (self.grabbedBy[graspIndex] is Creature.Grasp grasp && grasp.grabber is Player player_)
				{
					if (!self.isNPC || (player_.isNPC)) //PUPS SHOULD LET GO OF OTHER PUPS
					player_.ReleaseGrasp(grasp.graspUsed); // list is modified
				}
			}
		}

		//NEW OVERDRIVE METER! - EHH SCREW IT
		/*
		if (self.GetBelly().overDrive < 600 && self.touchedNoInputCounter > 15)
		{
			self.GetBelly().overDrive += 5;
			if (self.touchedNoInputCounter > 90)
            {
				self.GetBelly().overDrive += 3;
				self.Blink(2);
			}
		}
		else if (self.GetBelly().overDrive > 0 && self.touchedNoInputCounter < 15)
			self.GetBelly().overDrive -= 2;
		*/

		if (BPOptions.hudHints.Value && !BellyPlus.struggleHintGiven && self.lungsExhausted && IsStuckOrWedged(self) && !self.submerged)
        {
			// self.GetBelly().struggleHintCount += 2;

			if (self.GetBelly().struggleHintCount > BellyPlus.struggleHint && self.room.game.cameras[0].hud != null) //400
            {
				//self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.manager.rainWorld.inGameTranslator.Translate("Grab your prey and hold the eat button to feed"), 0, 160, true, true);
				self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Struggling too quickly will make you exhausted and slow down your progress."), 0, 200, false, false);
				self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Try to slow down and save stamina for when you're close to being free."), 15, 200, false, false);
				// BellyPlus.struggleHintGiven = true; //PEOPLE JUST DON'T GODDANG READ IT!! >:(
				self.GetBelly().struggleHintCount = 0;
				BellyPlus.struggleHint += 50;
			}
		}
		else if (BPOptions.hudHints.Value && !BellyPlus.struggleHintGiven && self.GetBelly().struggleHintCount > 0)
        {
			self.GetBelly().struggleHintCount--;
		}
		else if (!BPOptions.hudHints.Value || BellyPlus.struggleHintGiven)
			self.GetBelly().struggleHintCount = 0;
		

		if (self.slideCounter == 4 && self.lungsExhausted)
        {
			self.standing = false;
			MakeStrainSparks(self, 8);
			self.slideCounter = 0;
			self.room.PlaySound(SoundID.Slugcat_Turn_In_Corridor, self.mainBodyChunk, false, 1f, 1f);
		}
		
		//SLIGHTLY EXTEND OUR RUNNING SLIDE by 16%
		if (self.slideCounter > 10 && self.bodyMode == Player.BodyModeIndex.Stand && UnityEngine.Random.value < 0.33f)
		{
			self.slideCounter--;
		}

		//RUN MOVEMENT UPDATE STUFF THAT WON'T RUN OTHERWISE
		if (self.inShortcut) //BUT THIS SEEMS TO ALMOST NEVER RUN SINCE THIS PART OF THE CODE IS NORMALLY SKIPPED WHILE IN SHORTCUTS
		{
			self.GetBelly().isSqueezing = false;
			self.GetBelly().timeInNarrowSpace = 100; //ENOUGH TO TRIGGER THE IN-PIPE STATUS
			if (self.slowMovementStun < 3) //SO WE DONT GO CAREENING OUT OF PIPES
				self.slowMovementStun = 3;
		}
	}


	public static void BPUUpdatePass7(Player self, bool eu)
    {
		//KARMA BOOST
		/*
		else
		{
			int x = this.room.game.cameras[0].hud.karmaMeter.displayKarma.x;
			int num3 = Mathf.Min(this.Karma, this.KarmaCap);
			if (x > num3)
			{
				this.room.game.cameras[0].hud.karmaMeter.DropScavengerFlash();
			}
			this.room.game.cameras[0].hud.karmaMeter.displayKarma = new IntVector2(num3, Mathf.Clamp(this.KarmaCap, 4, 9));
		}
		this.room.game.cameras[0].hud.karmaMeter.karmaSprite.element = Futile.atlasManager.GetElementWithName(KarmaMeter.KarmaSymbolSprite(true, this.room.game.cameras[0].hud.karmaMeter.displayKarma));
		
		
		if (this.room.game.cameras[0].hud.karmaMeter.forceVisibleCounter > 0)
		{
			
			
			
		}
		*/
		
		
		
		//THIS ALWAYS RUNS ANYWAYS DUMMY!
        // if (self.graphicsModule != null && self.timeSinceSpawned < 60 && !ModManager.MSC)
			// self.timeSinceSpawned++;
		
		//MAKE SURE OUR GRAPHICS ARENT BEING STOLEN - THIS IS PRETTY OUTDATED. NO MODS SHOULD DO THIS ANYMORE...
		// if (self.timeSinceSpawned == 60 && !self.isNPC && self.graphicsModule != null && patch_PlayerGraphics.InspectGraphics(self.graphicsModule as PlayerGraphics) == false && self.room != null && self.room.game.cameras[0].hud != null)
			// self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Rotund World Warning - This mod's graphics module was overwritten by another active mod. Visual changes may not take affect."), 90, 1000, false, false);

		//CHECK EXTERNALS
		if (self.timeSinceSpawned == 2 && !self.isNPC && self.objectInStomach != null)
		{
            self.room.abstractRoom.AddEntity(self.objectInStomach);
            //self.objectInStomach.pos = self.abstractCreature.pos;
            self.objectInStomach.RealizeInRoom();
            CheckExternal(self, self.objectInStomach.realizedObject);
            //THEN PUT IT BACK
			if (self.objectInStomach.realizedObject != null)
				self.objectInStomach.realizedObject.RemoveFromRoom(); //ONE OF THESE IS NULL SOMEHOW??
            self.objectInStomach.Abstractize(self.abstractCreature.pos);
            self.objectInStomach.Room.RemoveEntity(self.objectInStomach);
        }

        //if (self.timeSinceSpawned == 60 && self.graphicsModule != null && (RainWorld.GAME_VERSION_STRING == "v1.9.01" || RainWorld.GAME_VERSION_STRING == "v1.9.02" || RainWorld.GAME_VERSION_STRING == "v1.9.03"))
        //    self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Rotund World Warning - Outdated game version will crash on hibernation! Use version v1.9.04 or higher"), 90, 1000, false, false);
		//THAT JUST WASN'T RELIABLE ENOUGH, AND/OR DOESN'T WORK IN THE OLD VERSIONS. TRY THIS INSTEAD
		if (!BellyPlus.versionCheck && self.graphicsModule != null && self.room.game.cameras[0].hud != null)
		{
			string myVersion = Custom.rainWorld.options.lastGameVersion; //RainWorld.GAME_VERSION_STRING -THIS IS A CONST, IT WILL ONLY EVER BE MY VERSION OF THE GAME
			if (myVersion == "v1.9.01" || myVersion == "v1.9.02" || myVersion == "v1.9.03")
				self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("Rotund World Warning - Outdated game version will crash on hibernation! Use version v1.9.04 or higher"), 90, 1000, false, false);
			BellyPlus.versionCheck = true;
		}
		

		//SOME DEBUG TOOLS
		if (BPOptions.debugTools.Value && self.input[0].jmp && !self.input[1].jmp && self.input[0].thrw)
		{
			Lizard lizardPartner = FindLizardInRange(self, 0, 0);
			if (lizardPartner != null)
			{
				lizardPartner.abstractCreature.GetAbsBelly().myFoodInStomach++;
				patch_Lizard.UpdateBellySize(lizardPartner);
				self.room.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, self.mainBodyChunk, false, 1f, 1.2f);
                //self.iVars.tailFatness
                Debug.Log("--LIZAR FAT :" + (lizardPartner.graphicsModule as LizardGraphics).iVars.tailFatness + " MORE " + (lizardPartner.graphicsModule as LizardGraphics).iVars.fatness );
            }
			else
			{
				if (self.input[0].y == -1)
                {
					//AddPersonalFood(self, -1);
					self.SubtractFood(1);
					self.room.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, self.mainBodyChunk, false, 1f, 0.5f);
				}
				else
                {
					//AddPersonalFood(self, 1);
					self.AddFood(self.input[0].pckp ? 10 : 1);
					
				}

				//Debug.Log("--OK BUT WHATS MY REAL FOOD? CHUB:" + GetAdjChubValue(self) + " OVERSTUFF: " + GetOverstuffed(self) + " SHARED: " + self.FoodInStomach + "-" + self.abstractCreature.GetAbsBelly().myFoodInStomach + "+" + BellyPlus.bonusFood + "+" + BellyPlus.bonusHudPip);
				//Debug.Log("--- ULTRA DEBUG BONUS FRUIT!" + self.abstractCreature.GetAbsBelly().myFoodInStomach + " OVERSTUFFED: " + GetOverstuffed(self) + " CURRENT FOOD:" + self.CurrentFood + " BONUS:" + BellyPlus.bonusFood + " " + BellyPlus.bonusHudPip + " TMR: " + BellyPlus.tomorrowsBonusFood);
				//Debug.Log("--OK BUT WHATS MY REAL FOOD? :" + BellyOffset(self) + " MORE " + (ModManager.Expedition && Expedition.ExpeditionGame.activeUnlocks.Contains("bur-rotund")));
			}
		}


        if (ModManager.JollyCoop && self.room?.game.Players.Count > 1 && BPOptions.blushEnabled.Value && self.input[0].mp && !self.input[1].mp)
		{
			//SKIP IF PLAYERS ARE WAITING IN PIPES
			bool pipeWait = false;
			for (int i = 0; i < self.room.game.Players.Count; i++)
			{
				if (self.room.game.Players[i].realizedCreature != null && self.room.game.Players[i].realizedCreature is Player plr && plr.inShortcut)
					pipeWait = true;
			}
			
			if (!pipeWait)
			{
				ObjGainHeat(self, 150);
				if (self.input[0].y == 1)
					ObjSetWideEyes(self, 45);
			}
				
		}
        //self.input[0].mp


    }


    public static void BPPlayer_Update(On.Player.orig_Update orig, Player self, bool eu)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, eu);
			return;
		}
		
		

		BPUUpdatePass1(self, eu);

        orig.Invoke(self, eu); //ORIGINAL (BTW, EU = EVERY OTHER (TICK) )
        
        if (self == null || self.dead) //OKAY??? I GUESS THIS WORKS???
		{
			return;
		}

		if (self.room != null)
		{
			//Debug.Log("POS: " + self.mainBodyChunk.pos);
			
			BPUUpdatePass2(self, eu);

			BPUUpdatePass3(self);

			BPUUpdatePass4(self, eu);

			BPUUpdatePass5(self);

			BPUUpdatePass7(self, eu);
		}
		

		BPUUpdatePass6(self);

        //Debug.Log("---ME! " + self.bodyMode + " - " + self.animation);

        //OK THIS JOLLY BINCH... GONNA TRICK IT'S PLAYER UPDATE METHOD~ TO FIX LETTING GO OF OUR PARTNER WHEN BOOSTING
        //if (self.GetBelly().isStuck && IsGrabbedByPlayer(self) && self.input[0].jmp) //if (self.GetBelly().pullingOther == true)
        if (InPullingChain(self) && IsGrabbedByPlayer(self) && self.input[0].jmp)
			self.input[1].jmp = true; //HAHAA, TRY THAT ON FOR SIZE >:3
        
		//FANCY SLUGCATS STOLE OUR GRAPHICS UPDATER! BUT THAT'S OKAY, WE CAN JUST TACK OURS ON THE END...
		//if (BellyPlus.fancySlugsEnabled)
		//{
		//	if (self.enteringShortCut != null && self.graphicsModule != null)
		//		self.graphicsModule.DrawSprites(); //HMMM, THIS IS A DELEMA...
		//}
	}
	
	
	
	
	
	public static void PopFree(Player self, float power, bool inPipe)
	{
		
		float popMag = Mathf.Min(power / 120f, 2f); //CAP OUT AT 2
		self.GetBelly().noStuck = 10;
		self.GetBelly().loosenProg = 0;
		float popVol = Mathf.Lerp(0.12f, 0.28f, Mathf.Min(popMag, 1f));
		if (BPOptions.debugLogs.Value)
			Debug.Log("-----POP!: " + popMag + " - " + " -POPVOL:" + popVol + " -" + self.GetBelly().stuckStrain);
		self.GetBelly().inPipeStatus = !self.GetBelly().inPipeStatus; //FLIPFLOP OUR PIPE STATUS
		self.GetBelly().isStuck = false;
		self.GetBelly().verticalStuck = false;
		self.GetBelly().stuckCoords = new Vector2(0,0);
		// self.GetBelly().stuckVector = new Vector2(0,0);
		self.GetBelly().wedgeStrain = 0;
		self.GetBelly().autoPilot = new IntVector2(0, 0);
		self.GetBelly().bloated = false;
		self.GetBelly().stuckLock = 0;
        // self.GetBelly().slicked /= 2;
        self.bodyChunkConnections[0].type = PhysicalObject.BodyChunkConnection.Type.Normal; //IN CASES OF UNCONCOUSNESS, THE GAME MIGHT OTHERWISE FORGET TO RESET THIS
		self.shortcutDelay = 1;
		self.gourmandAttackNegateTime = Math.Max(6, self.gourmandAttackNegateTime); //PHYSICS IS WEIRD. THIS IS NESSESARY APPARENTLY
		
		
		//TELEPORT US 0.5 OUT THE HOLE B) - DOESN'T ACTUALLY SEEM TO MAKE A BIG DIFFERENCE...
		self.bodyChunks[0].pos += new Vector2(self.input[0].IntVec.x * 10f, self.input[0].IntVec.y * 10f);
		self.bodyChunks[1].pos += new Vector2(self.input[0].IntVec.x * 10f, self.input[0].IntVec.y * 10f);


		if (ObjIsSlick(self))
			self.room.PlaySound(BPEnums.BPSoundID.Squinch1, self.mainBodyChunk, false, 0.12f * popMag, 1.3f);

		else if (!inPipe)
			self.room.PlaySound(SoundID.Slugcat_Turn_In_Corridor, self.mainBodyChunk, false, popMag, Mathf.Sqrt(popMag));
		
		//EXTRA FWUMP SOUND IF WE SAY SO
		if (self.GetBelly().fwumpFlag > 0)
			PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, 0.12f, 1f);


		//CREATE SOME FUN SPARK FX
		int sparkCount = Mathf.FloorToInt(Mathf.Lerp(0f, 12f, popMag)); //DEFAULT WAS 8
		


		float launchSpeed = inPipe ? 5f : 8f;
		if (ObjIsSlick(self))
        {
			launchSpeed *= 1.5f;
			popVol *= 1.3f;
			sparkCount += 4;
		}
		
		if (self.GetBelly().fwumpFlag > 0)
        {
			launchSpeed *= 1.5f;
			popVol *= 1.3f;
			sparkCount += 4;

			//IF BOTH... CHILL ON THE LAUNCH A BIT
			if (ObjIsSlick(self))
				launchSpeed *= 0.8f; //WILL LEAVE THEM AT x1.8 TOTAL
		}
		
		
		// Vector2 inputVect = self.input[0].IntVec.ToVector2() * launchSpeed * popMag;
		Vector2 inputVect = self.GetBelly().stuckVector * launchSpeed * popMag;
		self.GetBelly().stuckVector = new Vector2(0,0); //OKAY NOW WE CAN RESET THIS
	
		self.bodyChunks[0].vel = inputVect;
		self.bodyChunks[1].vel = inputVect;
		
		if (BPOptions.debugLogs.Value)
			Debug.Log("-----LAUNCH!!!: " + (launchSpeed * popMag));

        if (self.slugcatStats?.name?.value == "NoirCatto" && UnityEngine.Random.value < ((launchSpeed * popMag) - 10f) / 10f)
            DoNoirSounds(self, true);

        //IF WE'RE BEING TUGGED (BY A SLUGCAT) RELEASE US 
        if (IsGrabbedByPlayer(self))
		{
			Creature myHelper = self.grabbedBy[0].grabber;
			if (myHelper != null)
            {
				// Debug.Log("-----LAUNCH OUR PULLER!!!: " + self.grabbedBy[0].graspUsed);
				if (!(self.isNPC && self.isSlugpup))
					myHelper.ReleaseGrasp(self.grabbedBy[0].graspUsed);
				//FORGET THAT, JUST MIRROR THE LAUNCH VALUE
				myHelper.bodyChunks[0].vel = inputVect;
				myHelper.bodyChunks[1].vel = inputVect;
			}
			myHelper.GetBelly().autoPilot = new IntVector2(0, 0);
		}

		//LIZARD PUSHERS COULD USE SOME VISUAL OOMF
		Lizard lizardPartner = FindLizardInRange(self, 1, 0);
		if (lizardPartner != null)
		{
			lizardPartner.mainBodyChunk.vel = self.mainBodyChunk.vel;
			lizardPartner.bodyChunks[1].vel = self.mainBodyChunk.vel;
			lizardPartner.bodyChunks[2].vel = self.mainBodyChunk.vel;
			// Debug.Log("-----LIZARD SHOVED US! ");
		}
		
		//SO COULD OUR NORMAL PUSHERS
		if (ObjBeingPushed(self) > 0)
		{
			for (int i = 0; i < self.room.game.Players.Count; i++)
			{
				Player helper = self.room.game.Players[i].realizedCreature as Player;
				if (helper != null
					&& helper != self
					&& helper.dead == false
					&& ObjIsPushingOther(helper)
					&& Custom.DistLess(self.bodyChunks[1].pos, helper.bodyChunks[0].pos, 50f)
				)
				{
					helper.bodyChunks[0].vel = inputVect;
					helper.GetBelly().pushingOther = 0;
					// Debug.Log("-----LAUNCH OUR PUSHER!!!: " + inputVect / 2);
				}
			}
		}

		popVol += (-0.1f + Mathf.Max(0, BPOptions.sfxVol.Value));
		
		//-------------POP SOUND-----------
		//Debug.Log("-----POP!: " + popMag + " " + self.GetBelly().isSqueezing + " " + self.GetBelly().wasSqueezing);
		//POP REACHES ITS MAX VOLUME (0.32F) WHEN SQUEEZE STRAIN CAPS OUT AT 60
		PlayExternalSound(self, BPEnums.BPSoundID.Pop1, popVol / (!inPipe ? 2.5f : 2f), 1f + (ObjIsSlick(self) ? 0.2f : 0f) + (self.GetBelly().fwumpFlag > 0 ? 0.2f : 0f));

		if (popMag < 0.1)
		{
			sparkCount = 0;
		}
		MakeSparks(self, 0, sparkCount);


		//IF WE WERE AAALMOST EXHAUSTED TOO, ADD A SPAT OF MINI EXHAUSTION~
		if (self.GetBelly().corridorExhaustion > (maxStamina - 20) && self.GetBelly().corridorExhaustion < maxStamina && self.airInLungs >= 1)
        {
			self.airInLungs = 0.8f;
			self.lungsExhausted = true;
		}
	}







	//THIS STOPS UPDATING WHILE IN SHORTCUTS OR STUNNED
	public static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, eu);
			return;
		}
		
		
		int origFlipDir = self.flipDirection;

		//DON'T ATTEMPT TO SPRINT UNDERWATER IF WE'RE STUCK
		if (IsStuck(self))
			self.waterJumpDelay = 20;
		
		//BEAM TIP CORRECTIONS, BECAUSE WE MIGHT HAVE PRESSED DOWN INSTEAD OF JUMP
		if (self.animation == Player.AnimationIndex.BeamTip && self.input[0].y < 0 && self.input[1].y == 0)
		{
			DeadFall(self);
		}

        //CURSE THIS GAME... FEETSTICKPOSITION FORGETS TO CHECK THE TERRAIN RAD T_T SO WE NEED TO BRIEFLY PRETEND OUR RAD IS DIFFERENT
        //float myOrigBodyRad = self.bodyChunks[1].rad;
        //self.bodyChunks[1].rad = 8f;
		//OKAY! THIS WAS ACTUALLY CAUSING BIG PROBLEMS WITH TERRAIN RADIUS, SO I ADDED AN IL HOOK TO SWAP THIS METHOD'S RAD WITH TERRAIN RAD THE CORRECT WAY.

        orig.Invoke(self, eu); //ORIGINAL

		if (self.GetBelly().slugBed < 0)
		{
            self.flipDirection = origFlipDir;
			self.standing = false;
			//if (self.inp)
        }
		//self.bodyChunks[1].rad = myOrigBodyRad;
		//Debug.Log("BODY CHUNK RAD: " + self.bodyChunks[1].rad);

		if (GetChubValue(self) >= 1)
		{
			//IF AIMAP HASN'T INITIALIZED YET, SKIP THIS PART
			if (self.room != null && self.room.aimap != null)
			{
				if (self.room.aimap.getAItile(self.bodyChunks[0].pos).narrowSpace && self.GetBelly().fwumpDelay == 0) //self.IsTileSolid(0, -self.flipDirection, 1) && 
				{
					//IF WE'RE BELLY SLIDING, STOP SUDDENLY
					if (self.animation == Player.AnimationIndex.BellySlide)
					{
						// Debug.Log("FWUMP COMING SOON... ");
						self.GetBelly().fwumpDelay = 3; //SO THIS WILL KICK IN, AND 2 FRAMES LATER THE ACTUAL FWUMP WILL OCCUR //7?? no 3
						self.shortcutDelay = (Math.Max(self.shortcutDelay, 2));
						//WE SHOULD PROBABLY SLOW DOWN ENOUGH FOR THE STUCK COUNTER THOUGH...
						self.bodyChunks[0].vel.x *= 0.25f;
						self.bodyChunks[1].vel.x = 0f;
						//self.animation = Player.AnimationIndex.None;
						Debug.Log("-----BELLY SLIDING INTO TUNNEL!!: ");
					}
				}
			}
			
			
			//THIS MAY SEEM A BIT GOOFY, BUT IM USING IT LIKE A SECOND LANE.
			//GOT RID OF IT. JUST USING THE NORMAL FORMULAS FOR THIS
			if (self.GetBelly().fwumpDelay == 5)
			{
				
				float velSound = Mathf.Min(Mathf.Abs(self.bodyChunks[1].vel.x) / 40f, 0.18f);
				self.GetBelly().stuckStrain += Mathf.Abs(self.bodyChunks[1].vel.x) * 10f * (ObjIsSlick(self) ? 3f : 1f );
				// Debug.Log("-----FWUMP!: " + velSound);
				PlayExternalSound(self, BPEnums.BPSoundID.Fwump1, velSound, 1f);
				self.bodyChunks[0].vel.x = 0;
				self.bodyChunks[1].vel.x = 0;
				if (velSound > 0.08f)
				{
					self.room.AddObject(new ExplosionSpikes(self.room, self.bodyChunks[1].pos - new Vector2(0f, -self.bodyChunks[1].rad), 8, 8f, 4f, 5.0f, 20f, new Color(1f, 1f, 1f, 0.5f)));
					MakeSparks(self, 1, 6);
				}
				self.GetBelly().fwumpDelay = 0; //AND BACK TO 0 SO THE OTHER FWUMP IN OUR STUCK CHECKER DOESN'T TRIGGER
			}
		}


		//----PAST HERE RUNS REGARDLESS OF CHUB VALUE----


		//INCREASED DRAG RESISTANCE ON TUGGING OUR PARTNERS
		self.GetBelly().pullingOther = false;
        int myGrasp = -1;
        //CHECK BOTH GRASPS, DUMMY
        for (int j = self.grasps.Length - 1; j >= 0; j--)
        {
            if (self.grasps[j] != null && self.grasps[j].grabbed is Creature crit && !crit.dead)
                myGrasp = j;
        }

        //if (self.grasps[0] != null && ObjIsStuckable(self.grasps[0].grabbed as Creature) && !(self.grasps[0].grabbed as Creature).dead) //&& self.HeavyCarry(self.grasps[0].grabbed)
        if (myGrasp != -1 && self.grasps[myGrasp].grabbed is Creature myPartner && ObjIsStuckable(myPartner) && !myPartner.dead)
        {
			// Player myPartner = self.grasps[0].grabbed as Player;
			//Creature myPartner = self.grasps[0].grabbed as Creature;

			if (ObjIsStuck(myPartner) || InPullingChain(myPartner))
			{
				//IF WE'RE PULLING, BUT NOT ON THE STUCK ONE
				bool chainLink = InPullingChain(myPartner) && !ObjIsStuck(myPartner);

				if (self.room.aimap.getAItile(self.mainBodyChunk.pos).narrowSpace)
					self.grasps[myGrasp].grabbedChunk.vel += self.input[0].IntVec.ToVector2().normalized * self.GetBelly().myCooridorSpeed * 1f / Mathf.Max(0.75f, self.grasps[myGrasp].grabbed.TotalMass);
				
				//DON'T CLIMB POLES! WAIT...
				self.noGrabCounter = 2;
				self.GetBelly().targetStuck = 20;
				
				//WE CAN SKIP THIS IF WE'RE CRAMPED. CORRIDOR MOVEMENT SPEED MODS HANDLE THE TETHER STUFF
				if (true)
				{
					//HALT ME AS WELL AS MY PARTNER
					//12-3-22 MAYBE WE'LL TRY REPLACING THIS WITH SOMETHING MORE REASONABLE...
					self.grasps[myGrasp].grabbedChunk.vel /= 2f; //MAYBE EXCEPT THIS ONE

					//bool matchingShoveDir = (ObjIsVerticalStuck(myPartner as Creature) && self.input[0].y == ObjGetYFlipDirection(myPartner as Creature)) || (!ObjIsVerticalStuck(myPartner as Creature) && self.input[0].x == ObjGetXFlipDirection(myPartner as Creature));
					bool matchingShoveDir = (!chainLink && ObjIsVerticalStuck(myPartner) && self.input[0].y == ObjGetYFlipDirection(myPartner))
						|| (!chainLink && !ObjIsVerticalStuck(myPartner) && self.input[0].x == ObjGetXFlipDirection(myPartner))
						|| (chainLink && (self.bodyChunks[1].ContactPoint.y < 0) && self.input[0].IntVec != new IntVector2(0, 0))
						|| (chainLink && self.input[0].y == -1) //IF WE'RE DANGLING, WE MUST PULL DOWN TO PULL
						|| (self.isNPC);
					// Debug.Log("-----OUR GRABBD PLAYER IS STUCK!: " + matchingShoveDir);

					if (matchingShoveDir)
                    {
						self.GetBelly().pullingOther = true;
						if (self.graphicsModule != null)
							(self.graphicsModule as PlayerGraphics).LookAtObject(myPartner); //LOOK AT IT
						if (!self.lungsExhausted) //GIVE THEM THAT BOOST IF WE'RE PULLING THEM WHILE THEY'RE STUCK
                        {
							ObjGainStuckStrain(myPartner, 1);
							ObjGainBoostStrain(myPartner, 0, 2, 8 + (ObjBeingPushed(myPartner) > 0 ? 2 : 0));
						}

						//PASS FORWARD PULLING LINE BENEFITS
						if (self.GetBelly().beingPushed > 0)
							ObjGainStuckStrain(myPartner, 0.35f);

						//IF STANDING, DIG OUR HEELS INTO THE TUG
						if (self.bodyMode != Player.BodyModeIndex.CorridorClimb && self.standing && self.graphicsModule != null)
						{
							float pushVal = ((self.grasps[myGrasp].grabbedChunk.pos.x + (3 * self.GetBelly().myFlipValX)) - self.bodyChunks[1].pos.x) / 20f;
							//DON'T DIG OUR HEELS IN IF THAT WOULD KICK THEM IN THE FACE
							if (Mathf.Abs(self.bodyChunks[1].pos.y - myPartner.bodyChunks[0].pos.y) > 10)
								self.bodyChunks[1].vel.x = pushVal;

							float str = 0.5f; //1.3f
							self.bodyChunks[1].vel.y -= str;
							self.bodyChunks[0].vel.y += str;
							 //Debug.Log("-----TUG!!! " + pushVal);

                            // self.graphicsModule.bodyParts[1].vel.y += (GetCappedBoostStrain(self) / 6f); // * -self.GetBelly().myFlipValX;
							WagTailSegment(self, 1, new Vector2(0, GetCappedBoostStrain(self) / 6f));
                            GetHead(self).vel.x += GetCappedBoostStrain(self) * self.GetBelly().myFlipValX;
						}
					}
				}
				
				//GET OFF THE GROUND WHEN YOU'RE PULLING!!!
				if (self.bodyMode == Player.BodyModeIndex.Crawl || (self.bodyMode == Player.BodyModeIndex.Default && self.bodyChunks[0].ContactPoint.y > -1))
					self.standing = true;

				//IF WE'RE HANGIN OFF SOME POOR CEILING STUCKEE, SWANG~
				if ((self.bodyChunks[1].ContactPoint.y < 0) == false)
                {
					self.standing = false;
					self.animation = Player.AnimationIndex.HangUnderVerticalBeam;
				}
			}
			else
            {
				//THIS DOESN'T REALLY SEEM TO WORK - NVM IT WORKS
				//IF OUR TARGET IS NOT STUCK. WE SHOULD LET GO. EVENTUALLY.
				if (self.GetBelly().targetStuck > 1)
					self.GetBelly().targetStuck --;

				if (self.GetBelly().targetStuck < 1)
                {
					if (ObjIsStuckable(myPartner as Creature) && myPartner is Player && !((myPartner as Player).isNPC && (myPartner as Player).isSlugpup)) // && !(myPartner is LanternMouse) && !(myPartner is Cicada) && !(myPartner is Player && (myPartner as Player).isNPC &&  (myPartner as Player).isSlugpup))
					{
						self.ReleaseGrasp(myGrasp); //GENTLY release them
					}
				}
			}
		}

		if (IsStuck(self))
			self.enteringShortCut = null;


		if (self.GetBelly().isSqueezing)
        {
			self.GetBelly().squeezeStrain = Math.Min(self.GetBelly().squeezeStrain +1, 60);
        }
		else if (self.GetBelly().squeezeStrain > 0)
		{
			self.GetBelly().squeezeStrain -= 2;
		}


		//CHECK IF BOOSTING IN COORIDOORS TOO OFTEN (AND REDUCE THE DECAY IF BOOST BEEF IS ACTIVE
		if (self.GetBelly().boostStrain > 0 && (self.GetBelly().boostBeef <= 0 || eu) && self.GetBelly().boostBeef <= 15f)
			self.GetBelly().boostStrain--;

		if (self.GetBelly().boostBeef > 0)
			self.GetBelly().boostBeef--;

		self.GetBelly().lastBoost = self.GetBelly().myLastVel; // self.GetBelly().boostStrain; //REMEMBER THIS


		if (self.GetBelly().beingPushed > 0)
			self.GetBelly().beingPushed--;
		
		if (self.GetBelly().shortStuck > 0)
			self.GetBelly().shortStuck--; //THIS SYNTAX ACTUALLY WORK?...


		if (self.GetBelly().corridorExhaustion > 0)
		{
			self.GetBelly().corridorExhaustion--;
			if (self.GetBelly().corridorExhaustion > maxStamina)
			{
				if (self.airInLungs >= 1f)
				{
					// Debug.Log("----- OOF, IM EXHAUSTED! ");
					self.airInLungs = 0.6f; //THIS SHOULD MAKE US TIRED
					self.lungsExhausted = true;
					self.GetBelly().stuckStrain += 25; //LAST DITCH SHOVING~
                    self.room.PlaySound(SoundID.Slugcat_Normal_Jump, self.mainBodyChunk.pos, 1.2f, 0.6f);
                }
				//DON'T LET US GO OVER MAX STAMINA
				self.GetBelly().corridorExhaustion = Mathf.CeilToInt(maxStamina);
			}
		}
		
	}



	private const int outOfShape = 0; //DETERMINES HOW MUCH EXHAUSTIONS IS BUILT BY BOOSTING IN PIPES
	public static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self);
			return;
		}
		
		
		//CORRIDOR SPECIFIC STUFF, REPLICATED FROM UPDATE BODY MODE, JUST TO GET VARIABLE CRAWLING SPEEDS :/
		//CHANGES:
		//-DON'T LET US CORRIDOR DROP IF WE TOO CHUBBY
		

		//WAIT WAIT! I JUST HAD THE DUMBEST IDEA ON THE FACE OF THE PLANET...
		//WE COULD FLICKER THE SLUGCATSTATS.RUNSPEEDFAC TO EDIT THE VALUE TEMPORARILY FOR THE BODYMODEUPDATE, THEN QUICKLY FLICK IT BACK TO NORMAL FOR THE REST OF THE SLUGCATS TO RUN...
		//I HATE IT. BUT IT JUST MIGHT WORK...
		//OR, I GUESS JUST SET IT TO THE SLUGCATS INDIVIDUAL RUNSPEED BEFORE RUNNING EACH TIME
		//self.slugcatStats.runspeedFac = self.GetBelly().runSpeedMod;

		//CURSE THIS GAME... FEETSTICKPOSITION FORGETS TO CHECK THE TERRAIN RAD T_T SO WE NEED TO BRIEFLY PRETEND OUR RAD IS DIFFERENT
		//float myOrigBodyRad = self.bodyChunks[1].rad;
		//self.bodyChunks[1].rad = 8f;
		//THIS SHOULD BE HANDLED BE THE RADTOTERRAINRAD IL HOOK NOW


        //FIRST!!! CHECK IF WE'RE STUCK. IF WE ARE, CHANGE OUR BODYMODE TO CRAWL AND RUN THE ORIGINAL.
        if (self.GetBelly().isStuck
			&& (self.bodyMode == Player.BodyModeIndex.CorridorClimb || self.bodyMode == Player.BodyModeIndex.Default || self.bodyMode == Player.BodyModeIndex.Stunned || self.rollCounter > 0)
			&& self.animation != Player.AnimationIndex.ClimbOnBeam
			&& self.GetBelly().stuckVector.y == 0)//Math.Abs(GetCreatureVector(self).y) == 0)//Math.Abs(self.bodyChunks[1].pos.x - self.bodyChunks[0].pos.x) > 3f) //MAKE SURE OUR UPPER AND LOWER BODY ARE NOT SOMEWHAT PARALEL
		{
			self.bodyMode = Player.BodyModeIndex.Crawl;
			self.goIntoCorridorClimb = 0;
			
			int yMem = self.input[0].y;
			if (self.input[0].x == self.GetBelly().stuckVector.x)
				self.input[0].y = 0; //PREVENT US FROM WIBBLING AROUND IF PRESSING UP OR DOWN WHILE STRUGGLING
				
			orig.Invoke(self); //ORIGINAL
			
			if (yMem != 0)
				self.input[0].y = yMem;
		}
		else if (self.bodyMode == Player.BodyModeIndex.CorridorClimb)
		{
			//self.GetBelly().timeInNarrowSpace++;
			//PRE CORRIDOR SPECIFIC STUFF
			self.diveForce = Mathf.Max(0f, self.diveForce - 0.05f);
			self.waterRetardationImmunity = Mathf.InverseLerp(0f, 0.3f, self.diveForce) * 0.85f;
			if (self.dropGrabTile != null && self.bodyMode != Player.BodyModeIndex.Default && self.bodyMode != Player.BodyModeIndex.CorridorClimb)
			{
				self.dropGrabTile = default(IntVector2?);
			}
			if (self.bodyChunks[0].ContactPoint.y < 0)
			{
				self.upperBodyFramesOnGround++;
				self.upperBodyFramesOffGround = 0;
			}
			else
			{
				self.upperBodyFramesOnGround = 0;
				self.upperBodyFramesOffGround++;
			}
			if (self.bodyChunks[1].ContactPoint.y < 0)
			{
				self.lowerBodyFramesOnGround++;
				self.lowerBodyFramesOffGround = 0;
			}
			else
			{
				self.lowerBodyFramesOnGround = 0;
				self.lowerBodyFramesOffGround++;
			}






			// case Player.BodyModeIndex.CorridorClimb:
			self.GoThroughFloors = true;
			self.rollDirection = 0;
			if (self.corridorTurnDir != null)
			{
				for (int k = 0; k < 2; k++)
				{
					BodyChunk bodyChunk22 = self.bodyChunks[k];
					bodyChunk22.vel.y = bodyChunk22.vel.y + self.gravity;
				}
			}
			else
			{
				if (self.input[0].y < 0 && !self.input[0].jmp && !self.IsTileSolid(0, 0, -1) && !self.IsTileSolid(0, 0, -2) && !self.IsTileSolid(0, 0, -3) && ((self.mainBodyChunk.pos.y < self.bodyChunks[1].pos.y && self.IsTileSolid(0, -1, 1) && self.IsTileSolid(0, 1, 1) && (!self.IsTileSolid(0, -1, 0) || !self.IsTileSolid(0, 1, 0))) || (self.mainBodyChunk.pos.y > self.bodyChunks[1].pos.y && self.IsTileSolid(1, -1, 1) && self.IsTileSolid(1, 1, 1) && (!self.IsTileSolid(1, -1, 0) || !self.IsTileSolid(1, 1, 0)))))
				{
					if (self.mainBodyChunk.pos.y < self.bodyChunks[1].pos.y)
					{
						if (self.room.GetTile(self.mainBodyChunk.pos).AnyBeam)
						{
							self.dropGrabTile = new IntVector2?(self.room.GetTilePosition(self.mainBodyChunk.pos));
						}
						else if (self.room.GetTile(self.mainBodyChunk.pos + new Vector2(0f, -20f)).AnyBeam)
						{
							self.dropGrabTile = new IntVector2?(self.room.GetTilePosition(self.mainBodyChunk.pos + new Vector2(0f, -20f)));
						}
					}
					else if (self.room.GetTile(self.bodyChunks[1].pos).AnyBeam)
					{
						self.dropGrabTile = new IntVector2?(self.room.GetTilePosition(self.bodyChunks[1].pos));
					}
					else if (self.room.GetTile(self.bodyChunks[1].pos + new Vector2(0f, -20f)).AnyBeam)
					{
						self.dropGrabTile = new IntVector2?(self.room.GetTilePosition(self.bodyChunks[1].pos + new Vector2(0f, -20f)));
					}
				}
				else
				{
					self.dropGrabTile = default(IntVector2?);
				}
				bool flag3 = Mathf.Abs(self.bodyChunks[0].pos.x - self.bodyChunks[1].pos.x) < 5f && self.IsTileSolid(0, -1, 0) && self.IsTileSolid(0, 1, 0);
				bool flag4 = Mathf.Abs(self.bodyChunks[0].pos.y - self.bodyChunks[1].pos.y) < 7.5f && self.IsTileSolid(0, 0, -1) && self.IsTileSolid(0, 0, 1);
				bool flag5 = false;
				if (GetChubValue(self) < 2 && self.input[0].jmp && self.EffectiveRoomGravity > 0f && !self.input[1].jmp && self.input[0].y < 0 && !self.IsTileSolid(0, 0, -1) && !self.IsTileSolid(1, 0, -1))
				{
					//DON'T LET US CORRIDOR DROP IF WE TOO CHUBBY
					if (GetChubValue(self) < 2) //BUT CHECK FOR IT UP THERE SO FLAG 3 AND OTHER STUFF STILL HAPPENS IF WE ARE TOO CHUBBY
					{
						self.corridorDrop = true;
						self.canCorridorJump = 0;
					}
				}
				else if (flag3) //IN A VERTICAL CORRIDOR
				{
					//SLIGHT ALTERATION FOR THE CHUMBY ONES
					if (GetChubValue(self) >= 2) //BUT CHECK FOR IT UP THERE SO FLAG 3 AND OTHER STUFF STILL HAPPENS IF WE ARE TOO CHUBBY
					{
						//DON'T ALLOW US TO CORRIDOR TURN IF WEDGED
						if (!IsStuckOrWedged(self) && !IsPullingOther(self) && self.input[0].jmp && !self.input[1].jmp && self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y == self.input[0].y > 0) //CHANGING THE && TO == SO THAT IT'S ONE STATEMEMNT THAT RETURNS TRUE IF WE'RE HOLDING THE DIRECTION WE'RE POINTING
						{
							self.corridorTurnDir = new IntVector2?(new IntVector2(0, self.input[0].y)); //SO IT CAN GO EITHER WAY
							self.corridorTurnCounter = 0;
							self.canCorridorJump = 0;
						}
						//CHECKING FOR CORNER BOOST ELIGIBILITY, BUT NOW FOR BOTH SIDES
						else if (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
						{
							flag5 = self.IsTileSolid(1, 0, -1);
						}
						else if (self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y)
						{
							flag5 = self.IsTileSolid(1, 0, 1);
						}
					}
					//ELSE, IF WE'RE SKINNY, RUN THE ORIGINAL
					else
					{
						//EVERYTHING PAST HERE SHOULD BE ORIGINAL I THINK
						if (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
						{
							flag5 = self.IsTileSolid(1, 0, -1);
						}
						else
						{
							if (self.EffectiveRoomGravity == 0f)
							{
								flag5 = self.IsTileSolid(1, 0, 1);
							}
							if (!IsStuckOrWedged(self) && !IsPullingOther(self) && self.input[0].jmp && !self.input[1].jmp && self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y && self.input[0].y > 0)
							{
								self.corridorTurnDir = new IntVector2?(new IntVector2(0, 1));
								self.corridorTurnCounter = 0;
								self.canCorridorJump = 0;
							}
						}
					}
				}
				else if (flag4)
				{
					if (!IsStuckOrWedged(self) && !IsPullingOther(self) && self.input[0].jmp && !self.input[1].jmp && self.input[0].x != 0 && self.bodyChunks[0].pos.x < self.bodyChunks[1].pos.x == self.input[0].x > 0)
					{
						self.corridorTurnDir = new IntVector2?(new IntVector2(self.input[0].x, 0));
						self.corridorTurnCounter = 0;
						self.canCorridorJump = 0;
					}
					else
					{
						flag5 = (self.IsTileSolid(1, -self.flipDirection, 0) && !self.IsTileSolid(0, self.flipDirection, 0));
					}
				}
				if (flag5)
				{
					self.canCorridorJump = 5;
				}
				else if (self.canCorridorJump > 0)
				{
					self.canCorridorJump--;
				}
				if (self.input[0].jmp && !self.input[1].jmp && self.slowMovementStun < 1 && !IsStuckOrWedged(self) && self.GetBelly().pushingOther <= 0) //DON'T BOOST IF WE'RE STUCK OR PUSHING
				{
					if (self.EffectiveRoomGravity == 0f)
					{
						Vector2 vector;
						/*
						vector..ctor(0f, 0f);
						if (flag3 && self.IsTileSolid(1, 0, (int)Mathf.Sign(self.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y)))
						{
							vector..ctor(0f, -Mathf.Sign(self.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y));
						}
						else if (flag4 && self.IsTileSolid(1, (int)Mathf.Sign(self.bodyChunks[1].pos.x - self.bodyChunks[0].pos.x), 0))
						{
							vector..ctor(-Mathf.Sign(self.bodyChunks[1].pos.x - self.bodyChunks[0].pos.x), 0f);
						}
						*/
						//GAH. STUPID THING DOESN'T KNOW HOW TO WORK
						vector.x = 0f;
						vector.y = 0f;
						if (flag3 && self.IsTileSolid(1, 0, (int)Mathf.Sign(self.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y)))
						{
							vector.y = -Mathf.Sign(self.bodyChunks[1].pos.y - self.bodyChunks[0].pos.y);
						}
						else if (flag4 && self.IsTileSolid(1, (int)Mathf.Sign(self.bodyChunks[1].pos.x - self.bodyChunks[0].pos.x), 0))
						{
							vector.x = -Mathf.Sign(self.bodyChunks[1].pos.x - self.bodyChunks[0].pos.x);
						}
						//HOPEFULLY THAT'S THE SAME THING?

						if (vector.x != 0f || vector.y != 0f)
						{
							Vector2 pos = self.room.MiddleOfTile(self.bodyChunks[1].pos) - vector * 9f;
							for (int l = 0; l < 4; l++)
							{
								if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
								{
									self.room.AddObject(new WaterDrip(pos, vector * 5f + Custom.RNV() * 3f, false));
								}
							}
							self.room.PlaySound(SoundID.Slugcat_Corridor_Horizontal_Slide_Success, self.mainBodyChunk);
							self.bodyChunks[0].pos += 12f * vector * (self.isSlugpup ? 0.5f : 1f);
							self.bodyChunks[1].pos += 12f * vector * (self.isSlugpup ? 0.5f : 1f);
							self.bodyChunks[0].vel += 7f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * vector * (self.isSlugpup ? 0.5f : 1f);
							self.bodyChunks[1].vel += 7f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * vector * (self.isSlugpup ? 0.5f : 1f);
							self.horizontalCorridorSlideCounter = 25;
							self.horizontalCorridorSlideCounter /= (Mathf.Max(GetChubValue(self), 1)); //NEW - SUBTRACTING FROM THE SOURCE
							self.GetBelly().corridorExhaustion += (outOfShape * Mathf.Max(GetChubValue(self), 0)); //NEW
							self.slowMovementStun = 5;
						}
						else
						{
							if (flag4 && self.input[0].x != 0 && self.input[0].y == 0)
							{
								vector.x = (float)self.input[0].x;
							}
							else if (flag3 && self.input[0].y != 0 && self.input[0].x == 0)
							{
								vector.y = (float)self.input[0].y;
							}
							if (vector.x != 0f || vector.y != 0f)
							{
								self.room.PlaySound(SoundID.Slugcat_Corridor_Horizontal_Slide_Fail, self.mainBodyChunk);
								self.bodyChunks[0].vel += 6f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * vector;
								self.bodyChunks[1].vel += 4f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * vector;
								self.horizontalCorridorSlideCounter = 15;
								self.horizontalCorridorSlideCounter /= (Mathf.Max(GetChubValue(self), 1)); //NEW - SUBTRACTING FROM THE SOURCE
								self.GetBelly().corridorExhaustion += (outOfShape * Mathf.Max(GetChubValue(self), 0)); //NEW
								self.slowMovementStun = 15;
							}
						}
					}
					else if (self.verticalCorridorSlideCounter < 1)
					{
						// if (flag3 && self.input[0].y > -1 && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y)
						if (flag3 && self.input[0].y != 0 && self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y == self.input[0].y > 0)  //NEW
						{
							BodyChunk bodyChunk23 = self.bodyChunks[0];
							// bodyChunk23.vel.y = bodyChunk23.vel.y + 15f * Mathf.Lerp(1f, 1.2f, self.Adrenaline);
							bodyChunk23.vel.y = bodyChunk23.vel.y + 15f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (self.isSlugpup ? 0.5f : 1f) * self.input[0].y / Mathf.Max(GetChubValue(self) / 2, 1);  //NEW
							BodyChunk bodyChunk24 = self.bodyChunks[1];
							//bodyChunk24.vel.y = bodyChunk24.vel.y + 10f * Mathf.Lerp(1f, 1.2f, self.Adrenaline);
							bodyChunk24.vel.y = bodyChunk24.vel.y + 10f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (self.isSlugpup ? 0.5f : 1f) * self.input[0].y / Mathf.Max(GetChubValue(self) / 2, 1);  //NEW
							if (self.canCorridorJump > 0)
							{
								self.shootUpCounter = 30;
								Vector2 pos2 = self.room.MiddleOfTile(self.bodyChunks[1].pos) + new Vector2(0f, -9f);
								for (int m = 0; m < 4; m++)
								{
									if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
									{
										self.room.AddObject(new WaterDrip(pos2, new Vector2(Mathf.Lerp(-3f, 3f, UnityEngine.Random.value), 5f), false));
									}
								}
								self.room.PlaySound(SoundID.Slugcat_Vertical_Chute_Jump_Success, self.mainBodyChunk);
								self.verticalCorridorSlideCounter = 22;
								self.slowMovementStun = 2;
							}
							else
							{
								self.room.PlaySound(SoundID.Slugcat_Vertical_Chute_Jump_Fail, self.mainBodyChunk);
								self.verticalCorridorSlideCounter = 34;
								self.slowMovementStun = 18;
							}
							//NEW - SUPBTRACTING FROM THE SOURCE
							self.verticalCorridorSlideCounter /= (Mathf.Max(GetChubValue(self)+2, 1)); //NEW
							self.GetBelly().corridorExhaustion += (outOfShape * Mathf.Max(GetChubValue(self), 0)); //NEW
							self.canCorridorJump = 0;
						}
						else if (flag4)
						{
							self.flipDirection = ((self.bodyChunks[0].pos.x <= self.bodyChunks[1].pos.x) ? -1 : 1);
							if (self.input[0].x == self.flipDirection || self.input[0].x == 0)
							{
								if (self.canCorridorJump > 0 && self.input[0].x != 0)
								{
									Vector2 pos3 = self.room.MiddleOfTile(self.bodyChunks[1].pos) + new Vector2(-9f * (float)self.flipDirection, 0f);
									for (int n = 0; n < 4; n++)
									{
										if (UnityEngine.Random.value < Mathf.InverseLerp(0f, 0.5f, self.room.roomSettings.CeilingDrips))
										{
											self.room.AddObject(new WaterDrip(pos3, new Vector2((float)self.flipDirection * 5f, Mathf.Lerp(-3f, 3f, UnityEngine.Random.value)), false));
										}
									}
									self.room.PlaySound(SoundID.Slugcat_Corridor_Horizontal_Slide_Success, self.mainBodyChunk);
									BodyChunk bodyChunk25 = self.bodyChunks[0];
									bodyChunk25.pos.x = bodyChunk25.pos.x + 12f * (float)self.flipDirection * (self.isSlugpup ? 0.5f : 1f);
									BodyChunk bodyChunk26 = self.bodyChunks[1];
									bodyChunk26.pos.x = bodyChunk26.pos.x + 12f * (float)self.flipDirection * (self.isSlugpup ? 0.5f : 1f);
									self.bodyChunks[0].pos.y = self.room.MiddleOfTile(self.bodyChunks[0].pos).y;
									self.bodyChunks[1].pos.y = self.bodyChunks[0].pos.y;
									BodyChunk bodyChunk27 = self.bodyChunks[0];
									bodyChunk27.vel.x = bodyChunk27.vel.x + 7f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (float)self.flipDirection * (self.isSlugpup ? 0.5f : 1f);
									BodyChunk bodyChunk28 = self.bodyChunks[1];
									bodyChunk28.vel.x = bodyChunk28.vel.x + 7f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (float)self.flipDirection * (self.isSlugpup ? 0.5f : 1f);
									self.horizontalCorridorSlideCounter = 25;
									self.slowMovementStun = 5;
								}
								else
								{
									self.room.PlaySound(SoundID.Slugcat_Corridor_Horizontal_Slide_Fail, self.mainBodyChunk);
									BodyChunk bodyChunk29 = self.bodyChunks[0];
									bodyChunk29.vel.x = bodyChunk29.vel.x + 6f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (float)self.flipDirection;
									BodyChunk bodyChunk30 = self.bodyChunks[1];
									bodyChunk30.vel.x = bodyChunk30.vel.x + 4f * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * (float)self.flipDirection;
									self.horizontalCorridorSlideCounter = 15;
									self.slowMovementStun = 15;
								}
								//NEW - SUPBTRACTING FROM THE SOURCE
								self.horizontalCorridorSlideCounter /= (Mathf.Max(GetChubValue(self), 1)); //NEW
								self.GetBelly().corridorExhaustion += outOfShape * Mathf.Max(GetChubValue(self), 0); //NEW
							}
						}
					}
				}
				if (self.verticalCorridorSlideCounter == 1 || self.horizontalCorridorSlideCounter == 1)
				{
					self.slowMovementStun = 15;
				}
				float num2 = Mathf.InverseLerp(0f, 10f, (float)Math.Max(self.verticalCorridorSlideCounter, self.horizontalCorridorSlideCounter)) * (self.isSlugpup ? 0.5f : 1f);
				self.bodyChunks[0].vel *= 0.9f - 0.3f * self.surfaceFriction * (1f - num2);
				self.bodyChunks[1].vel *= 0.9f - 0.3f * self.surfaceFriction * (1f - num2);
				
				//NUM3 IS EFFECTIVELY OUR FINAL CORR CRAWL SPEED
				float num3 = 2.4f * Mathf.Clamp(self.surfaceFriction + 0.2f, 0.2f, 0.5f) * Mathf.Lerp(1f, 1.2f, self.Adrenaline) * self.GetBelly().myCooridorSpeed; // self.slugcatStats.corridorClimbSpeedFac;
				num3 *= Mathf.Lerp(0.1f, 1f, Mathf.InverseLerp(10f, 0f, (float)self.slowMovementStun));
				if (self.input[0].x != 0 && self.input[0].y != 0)
				{
					num3 *= 0.4f;
				}
				if (self.input[0].x != 0 && self.input[0].x > 0 == self.bodyChunks[0].pos.x < self.bodyChunks[1].pos.x)
				{
					self.backwardsCounter += 2;
				}
				else if (self.input[0].y > 0 && self.bodyChunks[0].pos.y < self.bodyChunks[1].pos.y)
				{
					self.backwardsCounter += 2;
				}
				if (self.backwardsCounter > 20)
				{
					self.backwardsCounter = 20;
				}
				if (self.backwardsCounter > 10)
				{
					num3 *= 0.6f;
				}
				//IF WE'RE PUSHING SOMEONE, OUR CRAWL SPEED SHOULD BASICALLY STOP.
				if (ObjIsPushingOrPullingOther(self)) //self.GetBelly().pushingOther ||
				{
					num3 *= 0.1f;
				}
				//IF WE HAVE THE NOSTUCK TAG AND ARE NOT IN A PIPE, RECALCULATE PIPE CRAWL SPEED AS IF WE HAD NO MODIFIERS

				if(self.GetBelly().noStuck > 0) //NAH, WE CAN BE IN A PIPE TOO && !self.GetBelly().inPipeStatus)
                {
					num3 = 1f; //SURPRISE. FORGET ALL THE REST
					if (self.GetBelly().shortStuck > 0)
                    {
						num3 *= -0.5f;
					}
					// Debug.Log("-----SWOOOOOOSH THROUGH PIPE!: " + self.bodyChunks[1].vel);
				}
				
				
				//CHECK FOR PIPE BOTTLENECKS!~
				if (self.GetBelly().inPipeStatus && self.onBack == null)
				{
					//SOME SILLY RIGGING FOR TUTORIAL STUFF
					bool rigged = (self.room.roomSettings.name.ToString() == "SU_A44") ? true : false;

					num3 *= CheckWedge(self, rigged);
                    //WE PROBABLY DO NEED TO CHECK THIS CONSTANTLY IN PIPES, SO WE REMAIN "STUCK" SO FRIENDLIES CAN PUSH US IF WE STAY STILL.
                    //OK HOLDUP, WE CAN'T RUN THE PART THAT HALTS US IN PLACE IF OUR VELOCITY IS TINY HERE, BECAUSE THEN WE'D NEVER START MOVING AGAIN
                    //self.horizontalCorridorSlideCounter = 0;
                    //self.verticalCorridorSlideCounter = 0;
					//self.bodyChunks[0].vel *= 0f;
					//self.bodyChunks[1].vel *= 0f;
					//Debug.Log("-----MYVEL!: " + self.bodyChunks[1].vel + " JMP?" + self.input[0]);
				}



				//OKAY, THIS IS MOVING DOWN HERE SO WE DONT UNDO THE BOYANT GRAVITY TO KEEP OURSELVES AFLOAT IN VERTICAL PIPES
				BodyChunk bodyChunk31 = self.bodyChunks[0];
				//THESE AREN'T PULLING YOU DOWN,IT'S JUST A 0.9 MODIFIER TO SLIGHTLY REDUCE CLIMB SPEED
				bodyChunk31.vel.y = bodyChunk31.vel.y + self.gravity * Mathf.Clamp(self.surfaceFriction * 8f, 0.2f, 1f) * Mathf.Lerp(1f, 0.2f, Mathf.InverseLerp(0f, 10f, (float)self.verticalCorridorSlideCounter));
				BodyChunk bodyChunk32 = self.bodyChunks[1];
				bodyChunk32.vel.y = bodyChunk32.vel.y + self.gravity * Mathf.Clamp(self.surfaceFriction * 8f, 0.2f, 1f) * Mathf.Lerp(1f, 0.2f, Mathf.InverseLerp(0f, 10f, (float)self.verticalCorridorSlideCounter));
				BodyChunk bodyChunk33 = self.bodyChunks[0];
				bodyChunk33.vel.y = bodyChunk33.vel.y - self.buoyancy * self.bodyChunks[0].submersion * (ModManager.MMF ? self.EffectiveRoomGravity : 1f);
				BodyChunk bodyChunk34 = self.bodyChunks[1];
				bodyChunk34.vel.y = bodyChunk34.vel.y - self.buoyancy * self.bodyChunks[1].submersion * (ModManager.MMF ? self.EffectiveRoomGravity : 1f);
				self.dynamicRunSpeed[0] = 0f;
				self.dynamicRunSpeed[1] = 0f;


				for (int num4 = 0; num4 < 2; num4++)
				{
					//IF HOLDING A DIRECTION AND WE ARE CAPABLE OF MOVING THAT DIRECTION
					if (self.input[0].x != 0 && !self.IsTileSolid(num4, self.input[0].x, 0))
					{
						BodyChunk bodyChunk35 = self.bodyChunks[num4];
						bodyChunk35.vel.x = bodyChunk35.vel.x + num3 * (float)self.input[0].x;
						self.bodyChunks[num4].vel.y = self.bodyChunks[num4].vel.y * 0.8f - (self.bodyChunks[num4].pos.y - self.room.MiddleOfTile(self.room.GetTilePosition(self.bodyChunks[num4].pos)).y) * 0.2f;
						if (self.input[0].y == 0)
						{
							self.bodyChunks[1 - num4].vel.y = self.bodyChunks[1 - num4].vel.y * 0.8f - (self.bodyChunks[num4].pos.y - self.room.MiddleOfTile(self.room.GetTilePosition(self.bodyChunks[num4].pos)).y) * 0.2f;
						}
						break;
					}
					if (self.input[0].y != 0 && !self.IsTileSolid(num4, 0, self.input[0].y))
					{
						BodyChunk bodyChunk36 = self.bodyChunks[num4];
						bodyChunk36.vel.y = bodyChunk36.vel.y + num3 * (float)self.input[0].y;
						//checkthisout("remove the below line if stuck?"); // @@@@@@@@@@ NOPE THIS IS FINE, NOT THE CAUSE...
						self.bodyChunks[num4].vel.x = self.bodyChunks[num4].vel.x * 0.8f - (self.bodyChunks[num4].pos.x - self.room.MiddleOfTile(self.room.GetTilePosition(self.bodyChunks[num4].pos)).x) * 0.2f;
						if (self.input[0].x == 0)
						{
							self.bodyChunks[1 - num4].vel.x = self.bodyChunks[1 - num4].vel.x * 0.8f - (self.bodyChunks[num4].pos.x - self.room.MiddleOfTile(self.room.GetTilePosition(self.bodyChunks[num4].pos)).x) * 0.2f;
						}
						break;
					}
				}
				BodyChunk bodyChunk37 = self.bodyChunks[0];
				bodyChunk37.vel.x = bodyChunk37.vel.x + num3 * (float)self.input[0].x * 0.1f;
				BodyChunk bodyChunk38 = self.bodyChunks[0];
				bodyChunk38.vel.y = bodyChunk38.vel.y + num3 * (float)self.input[0].y * 0.1f;
				self.standing = (self.bodyChunks[0].pos.y > self.bodyChunks[1].pos.y + 5f);
				if ((self.input[0].x != 0 || self.input[0].y != 0) && self.verticalCorridorSlideCounter < 16)
				{
					// self.animationFrame++;
					//NOW LETS TRY AND SLOW DOWN THE ANIMATION BASED ON CHUB.
					if (GetChubValue(self) >= 2)
					{
						if (UnityEngine.Random.value < 1f / GetChubValue(self) - self.GetBelly().wedgeStrain)
						{
							self.animationFrame++;
						}
					}
					else
					{
						self.animationFrame++;
					}
				}
				else
				{
					self.animationFrame = 0;
				}
				if (self.animationFrame > 12)
				{
					self.animationFrame = 1;
					if (GetChubValue(self) < 2) //ONLY PLAY IF WE AREN'T TOO CHUBBY
					{
						self.room.PlaySound(SoundID.Slugcat_In_Corridor_Step, self.mainBodyChunk);
					}
				}
				if (self.input[0].y > 0 && (ModManager.MMF || self.shootUpCounter < 1) && (!self.IsTileSolid(0, -1, 0) || !self.IsTileSolid(0, 1, 0)) && self.room.GetTile(self.mainBodyChunk.pos).verticalBeam && (!ModManager.MSC || !self.monkAscension))
				{
					self.room.PlaySound(SoundID.Slugcat_Grab_Beam, self.mainBodyChunk, false, 0.2f, 1f);
					self.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
					self.animation = Player.AnimationIndex.ClimbOnBeam;
				}
				if (self.input[0].x != 0 && self.room.GetTile(self.mainBodyChunk.pos).horizontalBeam && self.IsTileSolid(1, 0, -1) && self.IsTileSolid(1, 0, 1) && !self.IsTileSolid(0, self.input[0].x, -1) && (!ModManager.MSC || !self.monkAscension))
				{
					self.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
					self.animation = Player.AnimationIndex.HangFromBeam;
				}
			}

			//MOVED BOTTLENECK CHECK
			//float checkr = CheckWedge(self, playerNum);
			//Debug.Log("SPEED TEST: " + self.bodyChunks[1].vel.x + " : " + self.bodyChunks[1].vel.y);
			if (ObjIsWedged(self) && GetAxisMagnitude(self) < 0.05f) //DON'T MOVE AT ALL IF WE AINT REALLY MOVING //self.bodyChunks[1].vel.magnitude
			{
				 // Debug.Log("TOO SLOW! WEDGING IN PLACE: " + self.bodyChunks[1].vel + " MAG:" + GetAxisMagnitude(self));
                self.bodyChunks[0].vel = new Vector2(0, self.gravity);
				self.bodyChunks[1].vel = new Vector2(0, self.gravity);
				////ALSO APPARENTLY OUR FLIP VALUES ARENT ALWAYS CORRECT SO LETS CORRECT THAT
				//IntVector2 myVec = GetCreatureVector(self);
				//self.GetBelly().myFlipValX = myVec.x;
				//self.GetBelly().myFlipValY = myVec.y;
			}

		}
		else
		{
			orig.Invoke(self); //ORIGINAL
							   //OK BUT AFTER WE'VE RUN THE ORIGINAL... MAKE SOME TWEAKS BEFORE CONTINUING WITH THE MOVEMENT UPDATE
							   //INCREASED DRAG RESISTANCE ON TUGGING OUR PARTNERS

			if (self.GetBelly().wedgeStrain > 0)
				self.GetBelly().wedgeStrain = 0f;

			//12-4-22 OKAY, THIS NEEDS SOME REPLACING
			if (IsPullingOther(self))
            {
				self.dynamicRunSpeed[1] *= 0.1f; //HALT HIP MOVEMENT. ONLY UPPER TORSO MOVEMENT
			}

			float runspeedMult = self.GetBelly().runSpeedMod;
            //IF WE'VE GOT A CHONKY PLAYER PIGGYBACKING...
            if (GetPlayerOnBack(self) != null)
				runspeedMult *= (GetHeaviestOnStack(self, true)).GetBelly().runSpeedMod - 0.05f;

            //GREATLY REDUCE THESE PENALTIES IF JUMPING FROM A BEAM TIP!
            //if (self.animation == Player.AnimationIndex.BeamTip)
            if (self.GetBelly().weightless > 0)
			{
				//runspeedMult = Mathf.Lerp(runspeedMult, 1.0f, 0.85f);
				runspeedMult = 1.0f; //OVERRIDE WEIGHT PENALTY
            }
			
			
			self.dynamicRunSpeed[0] *= runspeedMult;//self.GetBelly().runSpeedMod;
			self.dynamicRunSpeed[1] *= Mathf.Pow(runspeedMult, 1.25f);
				//Debug.Log("-----RUNSPEED: " + self.dynamicRunSpeed[0]);
			
		}

		
		//CORRECT OURSELF IF OUTSIDE FORCES ARE DISPLACING THE END STICKING OUT OF AN ENTRANCE
		bool topInCorridor = IsTileNarrowFloat(self as Creature, 0, 0f, 0f);
		if (IsStuck(self) && topInCorridor)
		{
			if (IsVerticalStuck(self))
				self.bodyChunks[1].vel.x = 0;
			else
				self.bodyChunks[1].vel.y = 0;
			
			
			//WHILE WE'RE HERE... LET'S TRY THIS OUT
			//IT LOOKS LIKE THIS STUFF WAS ALMOST WORKING (ON FLOOR LEVEL HOLES ONLY) BUT WE CHANGED OUR MINDS. DONT WANT ANYMORE
			/*
			if (self.GetBelly().loosenProg > 0 &&
				((IsVerticalStuck(self) && self.input[0].y == -self.GetBelly().myFlipValY)
				|| (!IsVerticalStuck(self) && self.input[0].x == -self.GetBelly().myFlipValX)))
			{
				self.bodyChunks[1].vel = new Vector2(0,0);
				self.bodyChunks[0].vel = new Vector2(0,0);
				self.dynamicRunSpeed[0] = 0f;
				self.dynamicRunSpeed[1] = 0f;
				self.GetBelly().loosenProg -= 1 / 2000f;
			}
			*/
		}


        //BODY CHUNK BACK TO NORMAL!
        //self.bodyChunks[1].rad = myOrigBodyRad;


        //NO POLE JUMPING IF WE'RE STUCK OR PUSHING!
        if (self.GetBelly().isStuck || ObjIsPushingOther(self))
			self.slideUpPole = 0;
		

        //WE NEED TO RUN THIS HERE BECAUSE IT'S THE ONLY THING INBETWEEN THE SETTING AND CHECKING OF CANJUMP
        if (self.GetBelly().isStuck && IsGrabbedByPlayer(self)) //if (self.GetBelly().pullingOther == true)
            self.canJump = 0;
        //AND THEN THE JOLLYCOOP GUY CHANGED HIS MOD AGAIN!! WAH >:( QUIT THAT!

    }

	
	
	public static void BPUpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
	{
        //CHECK TO MAKE SURE WE HAVEN'T FAILED A PULLUP DUE TO OUR SHEER MASS
        //bool beamCheck = (self.animation == Player.AnimationIndex.HangFromBeam && self.input[0].y > 0 && self.input[1].y == 0);

        if (self.animation == Player.AnimationIndex.GetUpOnBeam && GetOverstuffed(self) > 80)
            self.straightUpOnHorizontalBeam = true;


        orig.Invoke(self);
		/*
        Debug.Log("---ME2! " + self.bodyMode + " - " + self.animation);

        if (beamCheck && self.animation == Player.AnimationIndex.GetUpOnBeam && GetOverstuffed(self) > 100)
		{
            //OKAY CHECK IF WE'RE ABOUT TO FAIL
            if (self.bodyChunks[1].ContactPoint.y > 0 && self.room.GetTile(self.mainBodyChunk.pos + new Vector2(0f, 20f)).Solid)
            {
                self.straightUpOnHorizontalBeam = true;
				self.bodyMode = Player.BodyModeIndex.ClimbingOnBeam;
				self.noGrabCounter = 0;
                Debug.Log("-PULLUP FAILED! BACKUP! ");
            }

            //PULLUP FAILED! BACKUP 
            Debug.Log("-PULLUP FAILED! BACKUP! ");
			//self.animation = Player.AnimationIndex.GetUpOnBeam;
			//self.pullupSoftlockSafety = 0;
			//self.straightUpOnHorizontalBeam = true;
		}
		*/
	}

	
	
	//UPDATE SHORTCUT HELPER TO PRETEND OUR RAD IS REDUCED :(
	// public static void ShortcutHelperUpdate(On.Player.orig_Update orig, ShortcutHelper self, bool eu)
	// {
		// for (int i = 0; i < this.room.game.Players.Count; i++)
		// {
			// if (this.room.game.Players[i].realizedCreature != null && this.room.game.Players[i].realizedCreature.room == this.room && this.room.game.Players[i].realizedCreature.Consious && this.room.game.Players[i].realizedCreature.grabbedBy.Count == 0)
			// {
				
			// }
		// }
	// }
	
	
	
	public static void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self, actuallyViewed, eu);
			return;
		}
		
		
		if (self.GetBelly().foodOnBack != null)
			self.GetBelly().foodOnBack.GraphicsModuleUpdated(actuallyViewed, eu);
		
		bool skipOrig = false;
		//OKAY BUT WE NEED TO STOP SKIPPING ORIG SO LETS INSTEAD TEMPORARILY NULL ANY GRABS CONTAINING A STUCK CREATURE WE ARE PULLING
		//Creature.Grasp[] origGrasps = self.grasps; //NO THIS KEEPS LIVE REFERENCES
		int grpIndex = 0;
		Creature stuckCrit = null;
		int chunk = 0;
		Creature.Grasp.Shareability grpShare = Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive;
		float dom = 0f;

        //THIS PART OF THE CODE ALSO DETERMINES HOW CLOSELY WE HOLD OUR GRABBED OBJECTS TO US. SO UNDER THOSE SPECIFIC CONDTIONS, SKIP THE NORMAL VERSION.
        for (int i = 0; i < 2; i++)
		{
			if (self.grasps[i] != null && self.grasps[i].grabbed is Creature myTarget && self.HeavyCarry(self.grasps[i].grabbed) && ObjIsStuckable(myTarget) && (IsStuckOrWedged(myTarget) || InPullingChain(myTarget)))
			{
				//AND RUNNING OUR SLIGHTLY ALTERED VERSION OF THE BASE CODE INSTEAD, WHERE EVERYTHING IS THE SAME EXCEPT WE PRETENT WE'RE HOLDING A SQUIDCADA
				Vector2 vector = Custom.DirVec(self.mainBodyChunk.pos, self.grasps[i].grabbedChunk.pos);
				float num = Vector2.Distance(self.mainBodyChunk.pos, self.grasps[i].grabbedChunk.pos);
				// float num2 = 5f + self.grasps[i].grabbedChunk.rad;
				float num2 = 22f; //30f FOR SQUIDCADAS
				if (ObjIsStuck(myTarget) && ObjGetStuckVector(myTarget) == new Vector2(0, 1)
					&&!(self.tongue != null && self.tongue.Attached)) //BUT NOT IF WE'RE IN WINCH MODE
					num2 -= 12f; //FOR PULLING UP WE WANT A CLOSER GRAB

				num2 += Mathf.Min(Mathf.Max(GetCappedBoostStrain(self) - 0, 0) / 3f, 6);
				float num3 = self.grasps[i].grabbedChunk.mass / (self.mainBodyChunk.mass + self.grasps[i].grabbedChunk.mass);
				if (self.grasps[i].grabbed.TotalMass < self.TotalMass)
				{
					num3 /= 2f;
				}
				if (self.enteringShortCut == null || num > num2)
				{
					self.mainBodyChunk.pos += vector * (num - num2) * num3;
					self.mainBodyChunk.vel += vector * (num - num2) * num3;
					//self.grasps[i].grabbedChunk.pos -= vector * (num - num2) * (1f - num3); //LETS GET RID OF THIS ONE
					self.grasps[i].grabbedChunk.vel -= vector * (num - num2) * (1f - num3);
				}

				//IT'S TOO LATE I'M ALREADY IN TOO DEEP
                stuckCrit = myTarget;
                grpIndex = i;
				chunk = self.grasps[i].chunkGrabbed;
                grpShare = self.grasps[i].shareability;
                dom = self.grasps[i].dominance;

                //THE IMPORTANT PART! TEMPORARILY SET THE GRASP TO NULL SO IT DOESN'T UPDATE AGAIN
                self.grasps[i] = null;
            }
		}
		
		//OKAY NEW PLAN, WE AREN'T ACTUALLY SKIPPING THE ORIGINAL. JUST MODIFYING OUR GRASPS BEFORE RUNNING IT
		orig.Invoke(self, actuallyViewed, eu);
		
		if (stuckCrit != null)
		{
			self.grasps[grpIndex] = new Creature.Grasp(self, stuckCrit, grpIndex, chunk, grpShare, dom, false);
        }
	}
	
	
	//11-22-22 BOUT TO TRY SOMETHING WILD! DOES THIS EVEN WORK?...
	public class FoodOnBack
	{
		public FoodOnBack(Player owner)
		{
			this.owner = owner;
			this.inFrontOfObjects = -1;
		}

		//WE NEED THIS WHEN RUNNING CTOR AGAIN SINCE PLAYERS CAN BE RECREATED
		public void ReplaceOwner(Player owner)
		{
            this.owner = owner;
        }

		public bool HasAFood
		{
			get
			{
				return this.spear != null;
			}
		}

		public void Update() //bool eu
		{
			if (this.spear == null && this.counter > 20)
			{
				//ON SECOND THOUGHT, WE SHOULD ONLY SWAP FOOD WITH OUR OFF HAND
				//for (int i = 0; i < 2; i++)
				int i = 1;
			
				//SMALL TWEAKS. ALLOW BACK FOOD STORAGE IF OUR OTHER HAND IS EMPTY
				if (this.owner.grasps[0] != null && this.owner.grasps[1] == null)
					i = 0;
			
				if (this.owner.grasps[i] != null && this.owner.grasps[i].grabbed is PhysicalObject && this.owner.grasps[i].grabbed is IPlayerEdible && this.owner.Grabability(this.owner.grasps[i].grabbed as PhysicalObject) == Player.ObjectGrabability.OneHand)
				{
					//WHY DON'T WE DO MOST OF THIS DOWN THERE?
					//this.owner.bodyChunks[0].pos += Custom.DirVec(this.owner.grasps[i].grabbed.firstChunk.pos, this.owner.bodyChunks[0].pos) * 2f;
					this.FoodToBack(this.owner.grasps[i].grabbed as PhysicalObject);
					//this.counter = 0;
					//break;
				}
			}
		}


		public void GraphicsModuleUpdated(bool actuallyViewed, bool eu)
		{
			if (this.spear == null)
				return;
			
			if (this.spear.slatedForDeletetion || this.spear.grabbedBy.Count > 0)
			{
				if (this.abstractStick != null)
					this.abstractStick.Deactivate();
				this.spear = null;
				return;
			}
			Vector2 vector = this.owner.mainBodyChunk.pos;
			Vector2 vector2 = this.owner.bodyChunks[1].pos;
			if (this.owner.graphicsModule != null)
			{
				vector = Vector2.Lerp((this.owner.graphicsModule as PlayerGraphics).drawPositions[0, 0], (this.owner.graphicsModule as PlayerGraphics).head.pos, 0.2f);
				vector2 = (this.owner.graphicsModule as PlayerGraphics).drawPositions[1, 0];
			}
			Vector2 vector3 = Custom.DirVec(vector2, vector);
			if (this.owner.Consious && this.owner.bodyMode != Player.BodyModeIndex.ZeroG && this.owner.EffectiveRoomGravity > 0f)
			{
				//NO FLIP
				//OKAY MAYBE SOME FLIP...
				if (this.owner.bodyMode == Player.BodyModeIndex.Default && this.owner.animation == Player.AnimationIndex.None && this.owner.standing && this.owner.bodyChunks[1].pos.y < this.owner.bodyChunks[0].pos.y - 6f)
					this.flip = Custom.LerpAndTick(this.flip, (float)this.owner.input[0].x * 0.3f, 0.05f, 0.02f);
				else if (this.owner.bodyMode == Player.BodyModeIndex.Stand && this.owner.input[0].x != 0)
					this.flip = Custom.LerpAndTick(this.flip, (float)this.owner.input[0].x, 0.02f, 0.1f);
				else
					this.flip = Custom.LerpAndTick(this.flip, (float)this.owner.flipDirection * Mathf.Abs(vector3.x), 0.15f, 0.16666667f);
				//OVERLAP??? --I DON'T THINK NORMAL OBJECTS HAVE THIS
				//this.spear.ChangeOverlap(vector3.y < -0.1f && this.owner.bodyMode != Player.BodyModeIndex.ClimbingOnBeam);
				//THEY DO NOW :)
				//this.ChangeOverlap(vector3.y < -0.1f && this.owner.bodyMode != Player.BodyModeIndex.ClimbingOnBeam);
				//Debug.Log("----FOOD OVERLAP!: ");
				//this.ChangeOverlap(true);
				this.ChangeOverlap(false); //OKAY... SEEMS LIKE THIS SHOULD JUST BE FALSE ALL THE TIME
			}
			else
			{
				this.flip = Custom.LerpAndTick(this.flip, 0f, 0.15f, 0.14285715f);
				// this.spear.setRotation = new Vector2?(vector3 - Custom.PerpendicularVector(vector3) * 0.9f);
				//this.spear.ChangeOverlap(false);
				this.ChangeOverlap(false);

			}
			this.spear.firstChunk.MoveFromOutsideMyUpdate(eu, Vector2.Lerp(vector2, vector, 0.6f) - Custom.PerpendicularVector(vector2, vector) * 7.5f * this.flip);
			this.spear.firstChunk.vel = this.owner.mainBodyChunk.vel;
			//this.spear.rotationSpeed = 0f;
		}


		public void FoodToHand(bool eu)
		{
			if (this.spear == null)
				return;

			if (this.spear.room != this.owner.room) //IN CASE MODDING SHENANIGANS DESYNCED THIS
                this.owner.room.AddObject(this.spear);

            if (this.owner.grasps[1] != null)
				return;

			int num = -1;
			int num2 = 0;
			while (num2 < 2 && num == -1)
			{
				if (this.owner.grasps[num2] == null)
					num = num2;
				num2++;
			}
			if (num == -1)
				return;
			if (this.owner.graphicsModule != null)
				this.spear.firstChunk.MoveFromOutsideMyUpdate(eu, (this.owner.graphicsModule as PlayerGraphics).hands[num].pos);
			//RETURN OUR ORIGINAL COLLISION
			//this.spear.collisionLayer = this.origCollisionLayer;
			this.spear.ChangeCollisionLayer(this.origCollisionLayer);
			this.spear.bodyChunks[0].collideWithTerrain = true;
			this.ChangeOverlap(true);
			this.owner.SlugcatGrab(this.spear, num);
			this.spear = null;
			this.interactionLocked = true;
			this.owner.noPickUpOnRelease = 20;
			if (this.owner.room != null)
			{
				this.owner.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, this.owner.mainBodyChunk);
				this.owner.room.PlaySound(SoundID.Scavenger_Knuckle_Hit_Ground, this.owner.mainBodyChunk);
			}
			if (this.abstractStick != null)
			{
				this.abstractStick.Deactivate();
				this.abstractStick = null;
			}
		}


		//MADE IT A BOOLEAN SO WE CAN TELL IF IT RAN TO COMPLETION OR WAS CANCELED
		public bool FoodToBack(PhysicalObject spr)
		{
			if (this.spear != null)
				return false;

			//IF IT'S A BATFLY, KILL IT. IF IT'S A DIFFERENT LIVING CREATURE, DON'T STOW IT
			if (spr is Creature && (spr as Creature).dead == false)
            {
				if (spr is Fly)
					(spr as Fly).Die();
				else
					return false;
            }
			
			if (spr is Mushroom)
				(spr as Mushroom).growPos = null;
			if (spr is KarmaFlower)
				(spr as KarmaFlower).growPos = null;
			if (spr is SlimeMold)
				(spr as SlimeMold).stuckPos = null;

			//THIS USED TO HAPPEN IN UPDATE() BUT I MOVED IT DOWN HERE
			this.owner.bodyChunks[0].pos += Custom.DirVec(spr.firstChunk.pos, this.owner.bodyChunks[0].pos) * 2f;
			this.counter = 0;

			for (int i = 0; i < 2; i++)
			{
				if (this.owner.grasps[i] != null && this.owner.grasps[i].grabbed == spr)
				{
					this.owner.ReleaseGrasp(i);
					break;
				}
			}
			this.spear = spr;
			// this.spear.ChangeMode(Weapon.Mode.OnBack); --A FRUIT IS NOT A WEAPON
			//MAKE THE FOOD NOT COLLIDE WITH US
			this.origCollisionLayer = this.spear.collisionLayer;
			//this.spear.collisionLayer = 2; //LIKE KARMA FLOWERS
			this.spear.ChangeCollisionLayer(0); //0 - OKAY BUT THIS LAYER STILL COLLIDES WITH TERRAIN AND THATS WEIRD
			this.spear.bodyChunks[0].collideWithTerrain = false;
			this.ChangeOverlap(false);
			this.interactionLocked = true;
			this.owner.noPickUpOnRelease = 20;
			if (this.owner.room != null)
			{
				this.owner.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, this.owner.mainBodyChunk);
				this.owner.room.PlaySound(SoundID.Scavenger_Knuckle_Hit_Ground, this.owner.mainBodyChunk);
			}
			if (this.spear is PlayerCarryableItem) //FLASHY
				(this.spear as PlayerCarryableItem).Blink();
			if (this.abstractStick != null)
				this.abstractStick.Deactivate();
			this.abstractStick = new Player.AbstractOnBackStick(this.owner.abstractPhysicalObject, spr.abstractPhysicalObject);
			
			
			//DO THE HINT MESSAGES!
			if (BPOptions.hudHints.Value && !BellyPlus.backFoodHint2 && this.owner.room.game.cameras[0].hud != null)
			{
				if (!BellyPlus.backFoodHint1)
				{
					this.owner.room.game.cameras[0].hud.textPrompt.AddMessage(this.owner.room.game.rainWorld.inGameTranslator.Translate("Food items can be stored on your back for later by double-tapping the grab button"), 0, 200, false, false);
					BellyPlus.backFoodHint1 = true;
				}
				this.owner.room.game.cameras[0].hud.textPrompt.AddMessage(this.owner.room.game.rainWorld.inGameTranslator.Translate("Double tap grab again to unstore food items on your back."), 0, 200, false, false);
				BellyPlus.backFoodHint2 = true;
			}
			
			
			return true; //EVERYTHINGS A-OKAY HERE, BOSS!
			//this.owner.graphicsModule.BringSpritesToFront();
		}


		public void DropFood()
		{
			if (this.spear == null)
				return;
			
			this.spear.firstChunk.vel = this.owner.mainBodyChunk.vel + Custom.RNV() * 3f * UnityEngine.Random.value;
			// this.spear.ChangeMode(Weapon.Mode.Free);
			//this.spear.collisionLayer = this.origCollisionLayer;
			this.spear.ChangeCollisionLayer(this.origCollisionLayer);
			this.spear.bodyChunks[0].collideWithTerrain = true;
			this.spear = null;
			if (this.abstractStick != null)
			{
				this.abstractStick.Deactivate();
				this.abstractStick = null;
			}
		}

		//BARROWING THIS FROM WEAPON.CS
		public virtual void NewRoom(Room newRoom)
		{
			//SHRUG
			this.inFrontOfObjects = -1;
			
		}

		public void ChangeOverlap(bool newOverlap)
		{
            /*
			if (this.inFrontOfObjects == ((!newOverlap) ? 0 : 1) || this.owner.room == null)
			{
				return;
			}
			for (int i = 0; i < this.owner.room.game.cameras.Length; i++)
			{
				this.owner.room.game.cameras[i].MoveObjectToContainer(this as IDrawable, this.owner.room.game.cameras[i].ReturnFContainer((!newOverlap) ? "Background" : "Items"));
			}
			this.inFrontOfObjects = ((!newOverlap) ? 0 : 1);
			*/
            //MAYBE WE JUST GO AS SIMPLE AS POSSIBLE?

            for (int i = 0; i < this.owner.room?.game.cameras.Length; i++)
            {
                //DIFFERENT OBJECT TYPES NEED TO PASS IN DIFFERENT THINGS TO SWAP LAYERS CORRECTLY
                IDrawable objTarget; // = this.spear;
                if (this.spear is IDrawable)
                    objTarget = this.spear as IDrawable;
                else
                    objTarget = this.spear.graphicsModule;
                this.owner.room.game.cameras[i].MoveObjectToContainer(objTarget, this.owner.room.game.cameras[i].ReturnFContainer((!newOverlap) ? "Background" : "Items"));
            }


            //IF IT'S LIKE A CREATURE OR SOMETHING, RESET IT'S GRAPHICS MODULE
            //if (newOverlap == false && this.spear.graphicsModule != null)
            //	this.spear.graphicsModule.Reset();
        }

		public Player owner;
		// public Spear spear;
		public PhysicalObject spear;
		public bool increment;
		public int counter;
		public float flip;
		public bool interactionLocked;
		public Player.AbstractOnBackStick abstractStick;
		//A NEW ONE
		public int origCollisionLayer;
		public int inFrontOfObjects;
	}
}