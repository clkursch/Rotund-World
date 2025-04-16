using RWCustom;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

using System.Reflection;
using MonoMod.RuntimeDetour;
using Expedition;
using Menu;

using System.Text.RegularExpressions;
using Menu.Remix.MixedUI;
using System.Linq;
using static Expedition.ExpeditionProgression;
//using Rewired;
using MoreSlugcats;
using Watcher;
using Unity.Jobs;
using Rewired.ControllerExtensions;

namespace RotundWorld;
public class patch_Misc
{
    
	public delegate bool orig_SwEdible(SSOracleSwarmer self);
	public delegate bool orig_FrEdible(FireEgg self);
	
	public static void Patch()
    {
        On.WinState.CycleCompleted += BP_CycleCompleted;
        On.WinState.CreateAndAddTracker += BP_CreateAndAddTracker;
        On.WinState.PassageDisplayName += WinState_PassageDisplayName;

        On.FSprite.ctor_string_bool += FSprite_ctor_string_bool;
        On.FAtlasManager.GetElementWithName += FAtlasManager_GetElementWithName;
		
		On.Menu.MenuScene.BuildScene += BP_BuildScene;
        On.Menu.CustomEndGameScreen.GetDataFromSleepScreen += BP_GetDataFromSleepScreen;

        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += BPSlugcatPageContinue_ctor;
        On.Menu.ControlMap.ctor += BPControlMap_ctor;
        On.SlugcatStats.SlugcatFoodMeter += BPSlugcatStats_SlugcatFoodMeter;
        On.HUD.KarmaMeter.ctor += BPKarmaMeter_ctor; //ACTUAL NONSENSE
        On.PlayerSessionRecord.AddEat += PlayerSessionRecord_AddEat;

        On.FliesRoomAI.CreateFlyInHive += BP_CreateFlyInHive;
        On.HardmodeStart.HardmodePlayer.Update += BPHardmodePlayer_Update;

        On.Watcher.FlameJet.UpdateDamage += FlameJet_UpdateDamage;
        On.Watcher.WarpPoint.NewWorldLoaded_Room += WarpPoint_NewWorldLoaded_Room;

        On.ItemSymbol.SymbolDataFromItem += ItemSymbol_SymbolDataFromItem;
        //On.SeedCob.ctor += SeedCob_ctor;
		
		//MAKE NEURONS EDITABLE
		BindingFlags propFlags = BindingFlags.Instance | BindingFlags.Public;
		BindingFlags myMethodFlags = BindingFlags.Static | BindingFlags.Public;

		Hook myCustomHook = new Hook(
			typeof(SSOracleSwarmer).GetProperty("Edible", propFlags).GetGetMethod(), // This gets the getter 
			typeof(patch_Misc).GetMethod("BP_Neuron_Editable", myMethodFlags) // This gets our hook method
		);

		BindingFlags propFlags2 = BindingFlags.Instance | BindingFlags.Public;
		BindingFlags myMethodFlags2 = BindingFlags.Static | BindingFlags.Public;

        Hook myCustomHook2 = new Hook(
			typeof(FireEgg).GetProperty("Edible", propFlags2).GetGetMethod(), // This gets the getter 
			typeof(patch_Misc).GetMethod("BP_FireEgg_Editable", myMethodFlags2) // This gets our hook method
		);

    }

    private static void SeedCob_ctor(On.SeedCob.orig_ctor orig, SeedCob self, AbstractPhysicalObject abstractPhysicalObject)
    {
        //Debug.Log("INIT SEED COB!" + self + " - " + abstractPhysicalObject);
        //if (abstractPhysicalObject is null)
        //    Debug.Log("ABSTRACT COB IS NULL");
        //if (self is null)
        //    Debug.Log("SELF COB IS NULL");
        //if (self.AbstractCob is null) //OK BUT THIS IS ALWAYS NULL EVEN WHEN ITS WORKING
        //    Debug.Log("ABSTRACT COB 2 IS NULL");

        orig(self, abstractPhysicalObject);
    }

    private static IconSymbol.IconSymbolData? ItemSymbol_SymbolDataFromItem(On.ItemSymbol.orig_SymbolDataFromItem orig, AbstractPhysicalObject item)
    {
		
        if (item.type == AbstractPhysicalObject.AbstractObjectType.SeedCob)
        {
            //Debug.Log("ITEM SYMBOL " + item.type);
            //Debug.Log("ITEM " + item);
            //Debug.Log("ITEM ABSTRACT " + (item as SeedCob.AbstractSeedCob));
            if ((item as SeedCob.AbstractSeedCob) is null)
            {
                Debug.Log("NULL ABSTRACT COBB DETECTED!!!");
                item.Destroy();
                return null;
            }
        }
        return orig(item);
    }

    private static void PlayerSessionRecord_AddEat(On.PlayerSessionRecord.orig_AddEat orig, PlayerSessionRecord self, PhysicalObject eatenObject)
    {
        for (int i = self.eats.Count - 1; i >= 0; i--)
        {
            if (self.eats[i].ID == eatenObject.abstractPhysicalObject.ID)
            {
                return;
            }
        }

        //RUN THE SAME CHECK WE NORMALLY WOULD, BUT ONLY IF WE AREN'T GOURM
        if (ModManager.MSC && eatenObject.room != null && eatenObject.room.game.IsStorySession && eatenObject.room.game.StoryCharacter == MoreSlugcatsEnums.SlugcatStatsName.Gourmand && (eatenObject.room.game.Players[self.playerNumber].realizedCreature as Player).SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
        {
            WinState.GourFeastTracker gourTracker = eatenObject.room.game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(MoreSlugcatsEnums.EndgameID.Gourmand, addIfMissing: true) as WinState.GourFeastTracker;
            for (int j = 0; j < gourTracker.currentCycleProgress.Length; j++)
            {
                if (eatenObject.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.Creature)
                {
                    int creatureIndex = WinState.GourmandPassageCreaturesAtIndexContains((eatenObject as Creature).Template.type, j);
                    if (creatureIndex > 0)
                    {
                        if (gourTracker.currentCycleProgress[j] <= 0)
                        {
                            (eatenObject.room.game.Players[self.playerNumber].realizedCreature as Player).showKarmaFoodRainTime = 300;
                            eatenObject.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Karma_Pitch_Discovery, 0f, 1f, 0.9f + UnityEngine.Random.value * 0.3f);
                        }
                        gourTracker.currentCycleProgress[j] = creatureIndex;
                    }
                }
                else if (WinState.GourmandPassageRequirementAtIndex(j) == eatenObject.abstractPhysicalObject.type || (WinState.GourmandPassageRequirementAtIndex(j) == AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer && eatenObject.abstractPhysicalObject.type == AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer))
                {
                    //Custom.Log("Item flagged for gourmand collection", eatenObject.abstractPhysicalObject.type.ToString());
                    if (gourTracker.currentCycleProgress[j] <= 0)
                    {
                        (eatenObject.room.game.Players[self.playerNumber].realizedCreature as Player).showKarmaFoodRainTime = 300;
                        eatenObject.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Karma_Pitch_Discovery, 0f, 1f, 0.9f + UnityEngine.Random.value * 0.3f);
                    }
                    gourTracker.currentCycleProgress[j] = 1;
                }
            }
        }

        orig(self, eatenObject);
    }


    /*public static void PostPatch()
    {
		//RUN THIS AFTER ALL OF THE MODLOADER STUFF US DONE SO WE DON'T DOUBLE-ADD BUR-ROTUND
		On.Expedition.ExpeditionProgression.SetupBurdenGroups += ExpeditionProgression_SetupBurdenGroups;
	}*/



    private static IntVector2 BPSlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
    {
		IntVector2 result = orig.Invoke(slugcat);
        if (BellyPlus.fakeFoodVal)
			return new IntVector2(result.x, 20);
		else
            return result;
    }

    private static void BPControlMap_ctor(On.Menu.ControlMap.orig_ctor orig, ControlMap self, Menu.Menu menu, MenuObject owner, Vector2 pos, Options.ControlSetup.Preset preset, bool showPickupInstructions)
    {
        orig.Invoke(self, menu, owner, pos, preset, showPickupInstructions);

        if (!BellyPlus.VisualsOnly() && self.pickupButtonInstructions != null)
        {
			self.pickupButtonInstructions.text += "\r\n" + menu.Translate("Rotund World Interactions:") + "\r\n";
            self.pickupButtonInstructions.text += "  - " + menu.Translate("While stuck, tab the Grab button to smear fruit on yourself") + "\r\n";
            self.pickupButtonInstructions.text += "  - " + menu.Translate("Push against stuck creatures with fruit in hand and press Jump to smear it on them") + "\r\n";
            self.pickupButtonInstructions.text += "  - " + menu.Translate("Point (double-tap grab + direction) tamed creatures in directions you want them to go") + "\r\n";
			self.pickupButtonInstructions.text += "  - " + menu.Translate("Hold Grab + Throw while holding food next to a co-op partner to feed them") + "\r\n";
        }
    }




    private static void BPSlugcatPageContinue_ctor(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_ctor orig, SlugcatSelectMenu.SlugcatPageContinue self, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber)
    {
        //WHY DOES THE GAME TRY SO HARD TO CORRECT THE SAVED FOOD VALUE THAT GETS DISPLAYED ONSCREEN?? IT'S NOT THAT DEEP

        //DANG THIS IS IMPOSSIBLE! IT DOESN'T UPDATE ITSELF, AND THAT VALUE IS IMPORTANT FOR THE CTOR, AND ITS SET LITERALLY THE LINE BEFORE...
        //OKAY WE'RE GONNA HAVE TO GET WEIRD WITH IT. 
        //RIG THE FOOD VALUES FOR JUST A SECOND, THEN SWITCH THEM BACK.

        BellyPlus.fakeFoodVal = true;
        orig(self, menu, owner, pageIndex, slugcatNumber);
        //fakeFoodVal = false; //THIS ISN'T FAST ENOUGH! BY NOW HUD HAS ALREADY INITIATED WITH THE BROKEN VALIUES. WE NEED TO GO DEEPER...
        
		BellyPlus.fakeFoodVal = false; //JUST TO BE SURE
    }
	
	
	private static void BPKarmaMeter_ctor(On.HUD.KarmaMeter.orig_ctor orig, HUD.KarmaMeter self, HUD.HUD hud, FContainer fContainer, IntVector2 displayKarma, bool showAsReinforced)
    {
        if (BellyPlus.fakeFoodVal) //I CAN'T BELEIVE I NEED TO DO THIS
        {
            BellyPlus.fakeFoodVal = false;
        }
		//KARMA HUD OBJECTS INITIALIZES BEFORE THE FOOD HUD DOES, SO THIS IS THE ONLY PLACE....
        orig.Invoke(self, hud, fContainer, displayKarma, showAsReinforced);
    }

    
    
	
    private static void BPHardmodePlayer_Update(On.HardmodeStart.HardmodePlayer.orig_Update orig, HardmodeStart.HardmodePlayer self)
    {
		orig.Invoke(self);
		if (self.Player != null) //APPARENTLY THIS CAN BE NULL HERE
			self.Player.abstractCreature.GetAbsBelly().myFoodInStomach = self.Player.playerState.foodInStomach; //UPDATE OUR FOOD VALUE AFTER HUNTER JUMPS ONSCREEN
    }


    public static bool ShouldBeEdible(PhysicalObject self)
	{
		return (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player player && (player.objectInStomach != null || player.FoodInStomach < player.MaxFoodInStomach));
	}

    public static bool BP_Neuron_Editable(orig_SwEdible orig, SSOracleSwarmer self)
	{
        if (ShouldBeEdible(self))
            return true;
		else
			return orig.Invoke(self); //OTHERWISE, JUST RUN AS NORMAL
	}
	
	public static bool BP_FireEgg_Editable(orig_FrEdible orig, FireEgg self)
	{
		if (ShouldBeEdible(self))
			return true;
		else
			return false;//orig.Invoke(self); //OTHERWISE, JUST RUN AS NORMAL
									  //return false;
	}

    private static void FlameJet_UpdateDamage(On.Watcher.FlameJet.orig_UpdateDamage orig, FlameJet self)
    {
        orig(self);

        for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
        {
            Creature realizedCreature = self.room.abstractRoom.creatures[i].realizedCreature;
            if (realizedCreature != null && realizedCreature.abstractPhysicalObject.rippleLayer == 0)
            {
                Vector2 vector = realizedCreature.mainBodyChunk.pos - self.pos;
                if (self.bounds.Vector2Inside(vector))
                {
                    for (int j = 0; j < self.jetPos.Length - 1; j++)
                    {
                        float t = (float)(Time.frameCount % 10) * 0.1f;
                        Vector2 b = Vector2.Lerp(self.jetPos[j], self.jetPos[j + 1], t);
                        float num = Mathf.Lerp(self.jetScale[j], self.jetScale[j + 1], t);
                        float num2 = Mathf.Clamp((float)j / 3f, 0.5f, 1f);
                        num2 *= Mathf.Clamp((float)(self.jetPos.Length - 1 - j) / 5f, 0.5f, 1f);
                        float num3 = Vector2.Distance(vector, b);
                        Player player = realizedCreature as Player;
                        if (player != null && num3 < num * 3f)
                        {
                            // UN-EXHAUST PLAYERS TRYING TO PULL UP ON BEAMS
                            if (player.animation == Player.AnimationIndex.GetUpOnBeam)
                                player.lungsExhausted = false;
                        }
                    }
                }
            }
        }
    }

	//DROP ANY BACKFOOD BEFORE WE WARP. THAT MESSES THINGS UP
    private static void WarpPoint_NewWorldLoaded_Room(On.Watcher.WarpPoint.orig_NewWorldLoaded_Room orig, WarpPoint self, Room newRoom)
    {
        for (int i = 0; i < newRoom.game.Players.Count; i++)
        {
            AbstractCreature abstractCreature = newRoom.game.Players[i];
            if (abstractCreature.realizedCreature != null)
            {
                Player player = abstractCreature.realizedCreature as Player;
                if (player.GetBelly().foodOnBack != null)
                {
                    player.GetBelly().foodOnBack.DropFood();
                }
            }
        }
        
        orig(self, newRoom);
    }



    private static string WinState_PassageDisplayName(On.WinState.orig_PassageDisplayName orig, WinState.EndgameID ID)
	{
		if (ID == EnumExt_MyMod.Glutton)
			return "The Glutton";
		else
			return orig.Invoke(ID);
	}


	private static FAtlasElement FAtlasManager_GetElementWithName(On.FAtlasManager.orig_GetElementWithName orig, FAtlasManager self, string elementName)
    {
		if (elementName == "GluttonA")
			return orig.Invoke(self, "foodSymbol"); //HunterA //smallKarma3
		else if (elementName == "GluttonB")
			return orig.Invoke(self, "foodSymbol"); //HunterB
		else
			return orig.Invoke(self, elementName);
	}

    

    private static void FSprite_ctor_string_bool(On.FSprite.orig_ctor_string_bool orig, FSprite self, string elementName, bool quadType)
    {
		if (elementName == "GluttonA")
			orig.Invoke(self, "foodSymbol", quadType); //HunterA
		else if (elementName == "GluttonB")
			orig.Invoke(self, "foodSymbol", quadType); //HunterB
		else
			orig.Invoke(self, elementName, quadType);
	}



	public static void BP_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, Menu.MenuScene self)
	{
		if (self.sceneID == EnumExt_MyScene.Endgame_Glutton)
		{
			//FIRST PART ALL OF THEM GET
			if (self is Menu.InteractiveMenuScene)
			{
				(self as Menu.InteractiveMenuScene).idleDepths = new List<float>();
			}
			Vector2 vector = new Vector2(0f, 0f);
			// vector..ctor(0f, 0f);

			//NOW THE CUSTOM PART
			self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "Endgame - Glutton";
			if (self.flatMode)
			{
				self.AddIllustration(new Menu.MenuIllustration(self.menu, self, self.sceneFolder, "Endgame - The Glutton - Flat", new Vector2(683f, 384f), false, true));
			}
			else
			{
				self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "Glutton - 6", new Vector2(71f, 49f), 2.2f, Menu.MenuDepthIllustration.MenuShader.Lighten));
				self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "Glutton - 5", new Vector2(71f, 49f), 1.5f, Menu.MenuDepthIllustration.MenuShader.Normal));
				self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "Glutton - 4", new Vector2(71f, 49f), 1.7f, Menu.MenuDepthIllustration.MenuShader.Normal));
				//self.depthIllustrations[self.depthIllustrations.Count - 1].setAlpha = new float?(0.5f);
				self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "Glutton - 3", new Vector2(71f, 49f), 1.7f, Menu.MenuDepthIllustration.MenuShader.LightEdges));
				self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "Glutton - 2", new Vector2(71f, 49f), 1.5f, Menu.MenuDepthIllustration.MenuShader.Normal));
				self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "Glutton - 1", new Vector2(171f, 49f), 1.3f, Menu.MenuDepthIllustration.MenuShader.Normal)); //LightEdges
				//(self as Menu.InteractiveMenuScene).idleDepths.Add(2.2f);
				(self as Menu.InteractiveMenuScene).idleDepths.Add(2.2f);
				(self as Menu.InteractiveMenuScene).idleDepths.Add(1.7f);
				(self as Menu.InteractiveMenuScene).idleDepths.Add(1.7f);
				(self as Menu.InteractiveMenuScene).idleDepths.Add(1.5f);
				(self as Menu.InteractiveMenuScene).idleDepths.Add(1.3f);
			}
			self.AddIllustration(new Menu.MenuIllustration(self.menu, self, self.sceneFolder, "Glutton - Symbol", new Vector2(683f, 35f), true, false));
			Menu.MenuIllustration MenuIllustration4 = self.flatIllustrations[self.flatIllustrations.Count - 1];
			MenuIllustration4.pos.x = MenuIllustration4.pos.x - (0.01f + self.flatIllustrations[self.flatIllustrations.Count - 1].size.x / 2f);

		}
		else
			orig.Invoke(self);
	}


	private static void BP_GetDataFromSleepScreen(On.Menu.CustomEndGameScreen.orig_GetDataFromSleepScreen orig, Menu.CustomEndGameScreen self, WinState.EndgameID endGameID)
	{
		if (endGameID == EnumExt_MyMod.Glutton)
		{
			//GOTTA REPLICATE THE MENU SCREEN
			Menu.MenuScene.SceneID sceneID = Menu.MenuScene.SceneID.Empty;
			sceneID = EnumExt_MyScene.Endgame_Glutton;
			self.scene = new Menu.InteractiveMenuScene(self, self.pages[0], sceneID);
			self.pages[0].subObjects.Add(self.scene);
			self.pages[0].Container.AddChild(self.blackSprite);
			if (self.scene.flatIllustrations.Count > 0)
			{
				self.scene.flatIllustrations[0].RemoveSprites();
				self.scene.flatIllustrations[0].Container.AddChild(self.scene.flatIllustrations[0].sprite);
				self.glyphIllustration = self.scene.flatIllustrations[0];
				self.glyphGlowSprite = new FSprite("Futile_White", true);
				self.glyphGlowSprite.shader = self.manager.rainWorld.Shaders["FlatLight"];
				self.pages[0].Container.AddChild(self.glyphGlowSprite);
				self.localBloomSprite = new FSprite("Futile_White", true);
				self.localBloomSprite.shader = self.manager.rainWorld.Shaders["LocalBloom"];
				self.pages[0].Container.AddChild(self.localBloomSprite);
			}
			self.titleLabel = new Menu.MenuLabel(self, self.pages[0], "", new Vector2(583f, 5f), new Vector2(200f, 30f), false, null);
			self.pages[0].subObjects.Add(self.titleLabel);
			self.titleLabel.text = self.Translate(WinState.PassageDisplayName(endGameID));
		}
		else
			orig.Invoke(self, endGameID);
	}

	private static void BP_GenerateAchievementScores(On.Expedition.ChallengeTools.orig_GenerateAchievementScores orig)
	{
		orig.Invoke();
		Expedition.ChallengeTools.achievementScores.Add(EnumExt_MyMod.Glutton, 50);
	}


	

    public static void BP_CreateFlyInHive(On.FliesRoomAI.orig_CreateFlyInHive orig, FliesRoomAI self)
	{
		orig.Invoke(self);
		//JUST REPEAT THE PROCESS. DOUBLING IT
		if (self.room.hives.Length == 0 || BellyPlus.VisualsOnly())
		{
			return;
		}
		AbstractCreature abstractCreature = new AbstractCreature(self.room.game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly), null, self.RandomHiveNode(), self.room.game.GetNewID());
		self.room.abstractRoom.AddEntity(abstractCreature);
		abstractCreature.Realize();
		self.inHive.Add(abstractCreature.realizedCreature as Fly);
		
	}
	
	
	public static void SetTrackerVal(WinState.IntegerTracker intTrck, int plrNum)
	{
        //LUCKILY, myFoodInStomach VALUES REMAIN IN MEMORY LONG AFTER THE PLAYER HAS BEEN REMOVED FROM THE WORLD - NOT ANYMORE. THE DOWNSIDE OF CWTS...
        //THIS ONE WON'T FAIL IF IT'S NULL
        if (BellyPlus.foodMemoryBank.TryGetValue(plrNum, out int myFood))
		{
            intTrck.SetProgress(myFood);
            //WE JUST NEED TO PRETEND WE NEVER MAKE PROGRESS OKAY?
            intTrck.showFrom = myFood;
            intTrck.lastShownProgress = myFood;
            if (BPOptions.debugLogs.Value)
                Debug.Log("SAVING INDIVIDUAL PLAYER WEIGHT P:" + plrNum + " FOOD: " + myFood);
            BellyPlus.foodMemoryBank[plrNum] = 0; //THEN PUT IT BACK
        }
    }
	
	
	
	public static int FindHighestLongtermFood(WinState wnState, int plrCount)
	{
        WinState.IntegerTracker foodTrkP1 = wnState.GetTracker(EnumExt_MyMod.P1food, false) as WinState.IntegerTracker;
		WinState.IntegerTracker foodTrkP2 = wnState.GetTracker(EnumExt_MyMod.P2food, false) as WinState.IntegerTracker;
		WinState.IntegerTracker foodTrkP3 = wnState.GetTracker(EnumExt_MyMod.P3food, false) as WinState.IntegerTracker;
		WinState.IntegerTracker foodTrkP4 = wnState.GetTracker(EnumExt_MyMod.P4food, false) as WinState.IntegerTracker;
        WinState.IntegerTracker foodTrkP5 = wnState.GetTracker(EnumExt_MyMod.P5food, false) as WinState.IntegerTracker;
        WinState.IntegerTracker foodTrkP6 = wnState.GetTracker(EnumExt_MyMod.P6food, false) as WinState.IntegerTracker;
        WinState.IntegerTracker foodTrkP7 = wnState.GetTracker(EnumExt_MyMod.P7food, false) as WinState.IntegerTracker;
        WinState.IntegerTracker foodTrkP8 = wnState.GetTracker(EnumExt_MyMod.P8food, false) as WinState.IntegerTracker;
        List<int> foodList = new List<int>();
		
		if (foodTrkP1 == null)
			return 0; //-1 ERROR STATE - NO JUST RETURN 0, THAT'S ALL WE NEED
		
		foodList.Add(foodTrkP1.progress);
		
		if (plrCount >= 2 && foodTrkP2 != null)
			foodList.Add(foodTrkP2.progress);
		if (plrCount >= 3 && foodTrkP3 != null)
			foodList.Add(foodTrkP3.progress);
		if (plrCount >= 4 && foodTrkP4 != null)
			foodList.Add(foodTrkP4.progress);

        if (plrCount >= 5 && foodTrkP5 != null)
            foodList.Add(foodTrkP5.progress);
        if (plrCount >= 6 && foodTrkP6 != null)
            foodList.Add(foodTrkP6.progress);
        if (plrCount >= 7 && foodTrkP7 != null)
            foodList.Add(foodTrkP7.progress);
        if (plrCount >= 8 && foodTrkP8 != null)
            foodList.Add(foodTrkP8.progress);


        return foodList.Max();
    }
	
	
	public static int MyLongtermFood(WinState wnState, int plrNum)
	{
        WinState.IntegerTracker foodTrkP1 = wnState.GetTracker(EnumExt_MyMod.P1food, false) as WinState.IntegerTracker;
		WinState.IntegerTracker foodTrkP2 = wnState.GetTracker(EnumExt_MyMod.P2food, false) as WinState.IntegerTracker;
		WinState.IntegerTracker foodTrkP3 = wnState.GetTracker(EnumExt_MyMod.P3food, false) as WinState.IntegerTracker;
		WinState.IntegerTracker foodTrkP4 = wnState.GetTracker(EnumExt_MyMod.P4food, false) as WinState.IntegerTracker;
        //EXTRAS
        WinState.IntegerTracker foodTrkP5 = wnState.GetTracker(EnumExt_MyMod.P5food, false) as WinState.IntegerTracker;
        WinState.IntegerTracker foodTrkP6 = wnState.GetTracker(EnumExt_MyMod.P6food, false) as WinState.IntegerTracker;
        WinState.IntegerTracker foodTrkP7 = wnState.GetTracker(EnumExt_MyMod.P7food, false) as WinState.IntegerTracker;
        WinState.IntegerTracker foodTrkP8 = wnState.GetTracker(EnumExt_MyMod.P8food, false) as WinState.IntegerTracker;

        if (plrNum == 0 && foodTrkP1 != null)
			return foodTrkP1.progress;
		if (plrNum == 1 && foodTrkP2 != null)
			return foodTrkP2.progress;
		if (plrNum == 2 && foodTrkP3 != null)
			return foodTrkP3.progress;
		if (plrNum == 3 && foodTrkP4 != null)
			return foodTrkP4.progress;

        if (plrNum == 4 && foodTrkP5 != null)
            return foodTrkP5.progress;
        if (plrNum == 5 && foodTrkP6 != null)
            return foodTrkP6.progress;
        if (plrNum == 6 && foodTrkP7 != null)
            return foodTrkP7.progress;
        if (plrNum == 7 && foodTrkP8 != null)
            return foodTrkP8.progress;
        else
			return -1; //ERROR STATE
    }


    public static int GetStoredCorn(WinState wnState, bool alive)
	{
        
		if (alive)
		{
            WinState.IntegerTracker cornTracker = wnState.GetTracker(EnumExt_MyMod.StoredCorn, false) as WinState.IntegerTracker;
            if (cornTracker != null)
                return cornTracker.progress;
        }
		else
		{
            WinState.IntegerTracker cornTracker = wnState.GetTracker(EnumExt_MyMod.StoredStumps, false) as WinState.IntegerTracker;
            if (cornTracker != null)
                return cornTracker.progress;
        }
		//NEITHER WAS STORED. WE HAVE NO CORN
		return 0;
    }





    public static void BP_CycleCompleted(On.WinState.orig_CycleCompleted orig, WinState self, RainWorldGame game)
	{
		orig.Invoke(self, game);


        Debug.Log("SHOULDNT WE BE STORING CORN? " + BellyPlus.StoredCorn);
        //CHECK THE CORN
        WinState.IntegerTracker integerTracker3 = self.GetTracker(EnumExt_MyMod.StoredStumps, true) as WinState.IntegerTracker;
        if (integerTracker3 != null && !BellyPlus.VisualsOnly())
        {
            if (BPOptions.debugLogs.Value)
                Debug.Log("STORED STUMP PROGRESS? " + BellyPlus.StoredStumps);
            integerTracker3.SetProgress(BellyPlus.StoredStumps);
            integerTracker3.showFrom = BellyPlus.StoredStumps;
            integerTracker3.lastShownProgress = BellyPlus.StoredStumps;
        }


        WinState.IntegerTracker integerTracker4 = self.GetTracker(EnumExt_MyMod.StoredCorn, true) as WinState.IntegerTracker;
        if (integerTracker4 != null && !BellyPlus.VisualsOnly())
        {
            if (BPOptions.debugLogs.Value)
                Debug.Log("STORED CORN PROGRESS? " + BellyPlus.StoredCorn);
            integerTracker4.SetProgress(BellyPlus.StoredCorn);
            integerTracker4.showFrom = BellyPlus.StoredCorn;
            integerTracker4.lastShownProgress = BellyPlus.StoredCorn;
        }



        // ON CYCLE COMPLETED
        // WinState.IntegerTracker integerTracker = self.GetTracker(WinState.EndgameID.Survivor, true) as WinState.IntegerTracker;
        WinState.IntegerTracker integerTracker5 = self.GetTracker(EnumExt_MyMod.Glutton, true) as WinState.IntegerTracker;
		if (integerTracker5 != null && !BellyPlus.VisualsOnly())// && integerTracker.GoalAlreadyFullfilled)
		{
			int gluttonProgress = BellyPlus.bonusFood;
			if (gluttonProgress > 4)
				gluttonProgress = 4 + ((gluttonProgress - 4) / 2);

			if (BPOptions.debugLogs.Value)
				Debug.Log("GLUTTON PROGRESS? " + gluttonProgress + "FILLED? " + integerTracker5.GoalAlreadyFullfilled);

			if (gluttonProgress > 0)
			{
				integerTracker5.SetProgress(integerTracker5.progress + gluttonProgress);
			}
			else if (integerTracker5 != null && !integerTracker5.GoalAlreadyFullfilled)
			{
				integerTracker5.SetProgress(integerTracker5.progress - Mathf.Max(2 - Mathf.CeilToInt(BellyPlus.BPODifficulty() / 2), 1) );
			}
		}
		
		
		
		
		//EXPERIMENTAL
		
		WinState.IntegerTracker integerTracker6 = self.GetTracker(EnumExt_MyMod.P1food, true) as WinState.IntegerTracker;
        WinState.IntegerTracker integerTracker7 = self.GetTracker(EnumExt_MyMod.P2food, true) as WinState.IntegerTracker;
		WinState.IntegerTracker integerTracker8 = self.GetTracker(EnumExt_MyMod.P3food, true) as WinState.IntegerTracker;
		WinState.IntegerTracker integerTracker9 = self.GetTracker(EnumExt_MyMod.P4food, true) as WinState.IntegerTracker;
		//EVEN MORE EXPERIMENTAL
        WinState.IntegerTracker integerTracker10 = self.GetTracker(EnumExt_MyMod.P5food, true) as WinState.IntegerTracker;
        WinState.IntegerTracker integerTracker11 = self.GetTracker(EnumExt_MyMod.P6food, true) as WinState.IntegerTracker;
        WinState.IntegerTracker integerTracker12 = self.GetTracker(EnumExt_MyMod.P7food, true) as WinState.IntegerTracker;
        WinState.IntegerTracker integerTracker13 = self.GetTracker(EnumExt_MyMod.P8food, true) as WinState.IntegerTracker;
        if (integerTracker6 != null)// && integerTracker.GoalAlreadyFullfilled)
		{
            //game.Players[0].
            // int gluttonProgress = BellyPlus.bonusFood;
            //Debug.Log("HOW MUCH WE TALKING?... " + patch_Player.bellyStats[0].myFoodInStomach + " -  " + patch_Player.bellyStats[1].myFoodInStomach);
            
			SetTrackerVal(integerTracker6, 0);

            if (game.Players.Count >= 2 && integerTracker7 != null)
				SetTrackerVal(integerTracker7, 1);
			if (game.Players.Count >= 3 && integerTracker8 != null)
				SetTrackerVal(integerTracker8, 2);
			if (game.Players.Count >= 4 && integerTracker9 != null)
				SetTrackerVal(integerTracker9, 3);

            if (game.Players.Count >= 5 && integerTracker10 != null)
                SetTrackerVal(integerTracker10, 4);
            if (game.Players.Count >= 6 && integerTracker11 != null)
                SetTrackerVal(integerTracker11, 5);
            if (game.Players.Count >= 7 && integerTracker12 != null)
                SetTrackerVal(integerTracker11, 6);
            if (game.Players.Count >= 8 && integerTracker13 != null)
                SetTrackerVal(integerTracker11, 7);


        }
		
		
	}
	
	
	public static WinState.EndgameTracker BP_CreateAndAddTracker(On.WinState.orig_CreateAndAddTracker orig, WinState.EndgameID ID, List<WinState.EndgameTracker> endgameTrackers)
	{
		// orig.Invoke(self, ID, endgameTrackers);
		
		WinState.EndgameTracker endgameTracker = null;
		
		//if (ID != EnumExt_MyMod.Glutton) //WinState.EndgameID.Glutton)
		//	return orig.Invoke(ID, endgameTrackers); //JUST RUN THE ORIGINAL AND NOTHING ELSE BELOW IT
		
		if (ID == EnumExt_MyMod.Glutton)
		{
			endgameTracker = new WinState.IntegerTracker(ID, 0, 0, 0, 20); //16
			Debug.Log("GLUTTON TRACKER CREATED! ");
		}

        else if (ID == EnumExt_MyMod.StoredCorn)
        {
            endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, 50);
            Debug.Log("CORN TRACKER CREATED! ");
        }

        else if (ID == EnumExt_MyMod.StoredStumps)
        {
            endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, 50);
        }


        //AND SOME EXPERIMENTAL STUFF
        else if (ID == EnumExt_MyMod.P1food)
			endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2); //NEED TO PUT THE MIN BELOW 0 OR ELSE IT WILL UNSTORE ITSELF AS A TRACKER AT 0
		else if (ID == EnumExt_MyMod.P2food)
			endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2);
		else if (ID == EnumExt_MyMod.P3food)
			endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2);
		else if (ID == EnumExt_MyMod.P4food)
			endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2);

        else if (ID == EnumExt_MyMod.P5food)
            endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2);
        else if (ID == EnumExt_MyMod.P6food)
            endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2);
        else if (ID == EnumExt_MyMod.P7food)
            endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2);
        else if (ID == EnumExt_MyMod.P8food)
            endgameTracker = new WinState.IntegerTracker(ID, -1, -1, 9999, BellyPlus.MaxBonusPips * 2);


        else
            return orig.Invoke(ID, endgameTrackers); //JUST RUN THE ORIGINAL AND NOTHING ELSE BELOW IT



        //AND THEN RUN THE ORIGINAL STUFF THAT WOULD OTHERWISE BE SKIPPED
        if (endgameTracker != null && endgameTrackers != null)
		{
			bool flag = false;
			for (int j = 0; j < endgameTrackers.Count; j++)
			{
				if (endgameTrackers[j].ID == ID)
				{
					flag = true;
					endgameTrackers[j] = endgameTracker;
					break;
				}
			}
			if (!flag)
			{
				endgameTrackers.Add(endgameTracker);
			}
		}
		return endgameTracker;
	}
	
	
	
	public static class EnumExt_MyMod
	{ // You can have multiple EnumExt_ classes in your assembly if you need multiple items with the same name for the different enum
		//public static SlugcatStats.Name YellowishWhite;
		//public static SlugcatStats.Name WhitishYellow;
		public static WinState.EndgameID Glutton = new WinState.EndgameID("Glutton", true);

        public static WinState.EndgameID StoredCorn = new WinState.EndgameID("StoredCorn", true);
        public static WinState.EndgameID StoredStumps = new WinState.EndgameID("StoredStumps", true);

        public static WinState.EndgameID P1food = new WinState.EndgameID("P1food", true);
		public static WinState.EndgameID P2food = new WinState.EndgameID("P2food", true);
		public static WinState.EndgameID P3food = new WinState.EndgameID("P3food", true);
		public static WinState.EndgameID P4food = new WinState.EndgameID("P4food", true);

        public static WinState.EndgameID P5food = new WinState.EndgameID("P5food", true);
        public static WinState.EndgameID P6food = new WinState.EndgameID("P6food", true);
        public static WinState.EndgameID P7food = new WinState.EndgameID("P7food", true);
        public static WinState.EndgameID P8food = new WinState.EndgameID("P8food", true);
    }
	
	
	public static class EnumExt_MyScene
	{
		public static Menu.MenuScene.SceneID Endgame_Glutton = new Menu.MenuScene.SceneID("Endgame_Glutton", true);
	}
}



//AN ATTEMPT TO MAKE THESE CUSTOM EXPEDITION THINGS COMPATIBLE WITH EXPEDITIONS EXPANDED
//OH THESE ARE BASICALLY BASEGAME AFTER 1.9.14
public class Obese : Modding.Expedition.CustomBurden //CustomBurden
{
	public override float ScoreMultiplier => 35f;
    public override string Group => "Other Burdens";
    public override string ID => "bur-rotund";
	
	public override string DisplayName => "OBESE";
    public override string Description => "Makes the player exceptionally prone to getting fat, making you struggle with your size even at the minimum food requirment.";
    public override string ManualDescription => "Makes the player exceptionally prone to getting fat, making you struggle with your size even at the minimum food requirment.";
	public override Color Color => new Color(0.55f, 0.35f, 0f);
	public override bool UnlockedByDefault => true;
}


public class FoodLover : Modding.Expedition.CustomPerk //CustomPerk
{
    public override string ID => "unl-foodlover";
    public override string Group => "Other Perks";
    public override string DisplayName => "Food Lover";  //Name

    public override string Description => "Allows the player to eat all food types for their full value";
    public override string ManualDescription => "Allows the player to eat all food types for their full value";
    
    public override string SpriteName => "Symbol_EggBugEgg";
    public override Color Color => new Color(0f, 1f, 0.47058824f); 
    public override bool UnlockedByDefault => true; //AlwaysUnlocked
    //public override CustomPerkType PerkType => CustomPerkType.Custom;
}



