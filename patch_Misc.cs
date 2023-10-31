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
using ExpeditionEnhanced; //FOR MOD COMPAT!

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
        //On.Expedition.ChallengeTools.GenerateAchievementScores += BP_GenerateAchievementScores;

        On.Expedition.ExpeditionProgression.SetupBurdenGroups += ExpeditionProgression_SetupBurdenGroups;
        On.Expedition.ExpeditionProgression.BurdenName += ExpeditionProgression_BurdenName;
        On.Expedition.ExpeditionProgression.BurdenManualDescription += ExpeditionProgression_BurdenManualDescription;
        On.Expedition.ExpeditionProgression.BurdenScoreMultiplier += ExpeditionProgression_BurdenScoreMultiplier;
        On.Expedition.ExpeditionProgression.BurdenMenuColor += ExpeditionProgression_BurdenMenuColor;
        On.Expedition.ExpeditionProgression.SetupPerkGroups += ExpeditionProgression_SetupPerkGroups;
        On.Expedition.ExpeditionProgression.UnlockName += ExpeditionProgression_UnlockName;
        On.Expedition.ExpeditionProgression.UnlockDescription += ExpeditionProgression_UnlockDescription;
        On.Expedition.ExpeditionProgression.UnlockSprite += ExpeditionProgression_UnlockSprite;
        On.Expedition.ExpeditionProgression.UnlockColor += ExpeditionProgression_UnlockColor;

        On.Menu.BurdenManualPage.ctor += BurdenManualPage_ctor;
        On.Menu.UnlockDialog.ctor += UnlockDialog_ctor; 
		On.Menu.UnlockDialog.UpdateBurdens += UnlockDialog_UpdateBurdens;
        On.Menu.UnlockDialog.Update += UnlockDialog_Update;

        On.Menu.SlugcatSelectMenu.SlugcatPageContinue.ctor += BPSlugcatPageContinue_ctor;
        On.Menu.ControlMap.ctor += BPControlMap_ctor;
        On.SlugcatStats.SlugcatFoodMeter += BPSlugcatStats_SlugcatFoodMeter;
        On.HUD.KarmaMeter.ctor += BPKarmaMeter_ctor; //ACTUAL NONSENSE



        On.FliesRoomAI.CreateFlyInHive += BP_CreateFlyInHive;
        On.HardmodeStart.HardmodePlayer.Update += BPHardmodePlayer_Update;
		
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

		//NEED REWIRED_CORE REFERENCE FOR THIS ONE
        //On.Menu.InputOptionsMenu.InputSelectButton.ButtonText += InputSelectButton_ButtonText;
        //On.Menu.InputOptionsMenu.InputSelectButton.RefreshLabelText += InputSelectButton_RefreshLabelText;
    }

    private static void InputSelectButton_RefreshLabelText(On.Menu.InputOptionsMenu.InputSelectButton.orig_RefreshLabelText orig, InputOptionsMenu.InputSelectButton self)
    {
        Debug.Log("SHOW YOURSELF " + (self.menu as InputOptionsMenu).manager.rainWorld.options.playerToSetInputFor);
        Debug.LogWarning("SHOW YOURSELF ");
        orig(self);
		//Debug.Log("SHOW YOURSELF 1 " + (self.menu as InputOptionsMenu).manager.rainWorld.options.playerToSetInputFor);
		//Debug.Log("SHOW YOURSELF 2 " + self.index);
		//self.textLabel.text = InputOptionsMenu.InputSelectButton.ButtonText(self.menu, self.gamepad, (self.menu as InputOptionsMenu).manager.rainWorld.options.playerToSetInputFor, self.index, false);
		//public Options.ControlSetup CurrentControlSetup
		//{
		//	get
		//	{
		//		return this.manager.rainWorld.options.controls[this.manager.rainWorld.options.playerToSetInputFor];
		//	}
		//}
	}
	/*
    private static string InputSelectButton_ButtonText(On.Menu.InputOptionsMenu.InputSelectButton.orig_ButtonText orig, Menu.Menu menu, bool gamePadBool, int player, int button, bool inputTesterDisplay)
    {
        Debug.Log("FLOATER 1 " + player);
        Options.ControlSetup controlSetup = (menu as InputOptionsMenu).manager.rainWorld.options.controls[player];
        Debug.Log("FLOATER 2 ");
        bool flag = controlSetup.GetControlPreference() == Options.ControlSetup.ControlToUse.ANY && controlSetup.GetActiveController() != null && inputTesterDisplay;
        Debug.Log("FLOATER 3 ");
        if (!InputOptionsMenu.InputSelectButton.IsInputDeviceCurrentlyAvailable(menu, gamePadBool, player) && !flag)
        {
            return "-";
        }
        ActionElementMap actionElementMap = null;
        Debug.Log("FLOATER 4 ");
        for (int i = 0; i < (menu as InputOptionsMenu).inputActions[button].Length; i++)
        {
            actionElementMap = controlSetup.GetActionElement((menu as InputOptionsMenu).inputActions[button][0], (menu as InputOptionsMenu).inputActionCategories[button][0], (menu as InputOptionsMenu).inputAxesPositive[button]);
            if (actionElementMap != null)
            {
                break;
            }
        }
        Debug.Log("FLOATER 5 ");
        if (actionElementMap == null)
        {
            return "???";
        }
        return actionElementMap.elementIdentifierName;
    }
	*/
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
        try
        {
            BellyPlus.fakeFoodVal = true;
            orig(self, menu, owner, pageIndex, slugcatNumber);
            //fakeFoodVal = false; //THIS ISN'T FAST ENOUGH! BY NOW HUD HAS ALREADY INITIATED WITH THE BROKEN VALIUES. WE NEED TO GO DEEPER...
        }
        catch (Exception e)
        {
            Debug.Log("CATCH! ERROR 2 " + e);
        }
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
			patch_Player.bellyStats[self.playerNumber].myFoodInStomach = self.Player.playerState.foodInStomach; //UPDATE OUR FOOD VALUE AFTER HUNTER JUMPS ONSCREEN
    }
	
	
	
	
	
    private static void UnlockDialog_Update(On.Menu.UnlockDialog.orig_Update orig, UnlockDialog self)
    {
		orig.Invoke(self);
		
		if (BellyPlus.expdEnhancedEnabled)
			return;

		if (rotundBurden.Selected || rotundBurden.IsMouseOverMe)
		{
			// self.perkNameLabel.text = self.burdenNames[4];
			// self.perkDescLabel.text = self.burdenDescriptions[4];
			//FORGET THAT, THAT'S JUST ASKING FOR TROUBLE
			self.perkNameLabel.text = self.Translate("OBESE") + "+ 35%";
			self.perkDescLabel.text = self.Translate("Makes the player exceptionally prone to getting fat, making you struggle with your size even at the minimum food requirment.");
		}
	}

    public static BigSimpleButton rotundBurden;

    private static void UnlockDialog_ctor(On.Menu.UnlockDialog.orig_ctor orig, UnlockDialog self, ProcessManager manager, ChallengeSelectPage owner)
    {
        if (BellyPlus.expdEnhancedEnabled)
		{
            orig.Invoke(self, manager, owner);
            return;
        }
		
		
		//UNLOCK THE FOOD LOVER PERK BY DEFAULT
        if (!ExpeditionData.unlockables.Contains("unl-foodlover"))
            ExpeditionData.unlockables.Add("unl-foodlover");

        orig.Invoke(self, manager, owner);

        //RECCONECT ALL THE BUTTON SELECTION PATHS SO CONTROLLERS CAN SELECT OUR NEW BUTTON
        rotundBurden.nextSelectable[3] = self.cancelButton;
        rotundBurden.nextSelectable[2] = self.blindedBurden;
        rotundBurden.nextSelectable[0] = (ModManager.MSC ? self.pursuedBurden : self.huntedBurden);

        self.blindedBurden.nextSelectable[0] = rotundBurden;
        if (ModManager.MSC)
            self.pursuedBurden.nextSelectable[2] = rotundBurden;
        else
            self.huntedBurden.nextSelectable[2] = rotundBurden;
    }

    public static void UnlockDialog_UpdateBurdens(On.Menu.UnlockDialog.orig_UpdateBurdens orig, UnlockDialog self)
    {
		if (BellyPlus.expdEnhancedEnabled)
        {
            orig.Invoke(self);
            return;
        }

        Vector2 vector1 = new Vector2(680f - (ModManager.MSC ? 325f : 248f), 310f);
		float num5 = 170f;
		if (true) //rotundBurden == null)
		{
			rotundBurden = new BigSimpleButton(self, self.pages[0], self.Translate(ExpeditionProgression.BurdenName("bur-rotund")), "bur-rotund", vector1 + new Vector2(num5 * 4f, -15f), new Vector2(150f, 50f), FLabelAlignment.Center, true);
			rotundBurden.buttonBehav.greyedOut = false; // !ExpeditionData.unlockables.Contains("bur-rotund");
			self.pages[0].subObjects.Add(rotundBurden);
		}


		//THIS MEANS ENABLED, NOT UNLOCKED
		if (ExpeditionGame.activeUnlocks.Contains("bur-rotund"))
		{
			Vector3 vector = Custom.RGB2HSL(ExpeditionProgression.BurdenMenuColor("bur-rotund"));
			rotundBurden.labelColor = new HSLColor(vector.x, vector.y, vector.z);
		}
		else
		{
			rotundBurden.labelColor = new HSLColor(1f, 0f, 0.35f);
		}

		orig.Invoke(self);
	}


    private static void BurdenManualPage_ctor(On.Menu.BurdenManualPage.orig_ctor orig, Menu.BurdenManualPage self, Menu.Menu menu, Menu.MenuObject owner)
    {
		orig.Invoke(self, menu, owner);
		
		if (BellyPlus.expdEnhancedEnabled)
			return;
		
		float num = 0f;
		if (menu.CurrLang == InGameTranslator.LanguageID.German)
		{
			num = -15f;
		}
		MenuLabel menuLabel9 = new MenuLabel(menu, owner, ExpeditionProgression.BurdenName("bur-rotund") + " +" + ExpeditionProgression.BurdenScoreMultiplier("bur-rotund").ToString() + "%", new Vector2(35f + (menu as ExpeditionManualDialog).contentOffX, -30f + num), default(Vector2), true, null);
		menuLabel9.label.alignment = FLabelAlignment.Left;
		menuLabel9.label.color = ExpeditionProgression.BurdenMenuColor("bur-rotund");
		self.subObjects.Add(menuLabel9);
		string[] array5 = Regex.Split(ExpeditionProgression.BurdenManualDescription("bur-rotund").WrapText(false, 500f + (menu as ExpeditionManualDialog).wrapTextMargin, false), "\n");
		for (int m = 0; m < array5.Length; m++)
		{
			MenuLabel menuLabel10 = new MenuLabel(menu, owner, array5[m], new Vector2(35f + (menu as ExpeditionManualDialog).contentOffX, menuLabel9.pos.y - 15f - 15f * (float)m + num), default(Vector2), false, null);
			menuLabel10.label.SetAnchor(0f, 1f);
			menuLabel10.label.color = new Color(0.7f, 0.7f, 0.7f);
			self.subObjects.Add(menuLabel10);
		}
	}

    private static Color ExpeditionProgression_BurdenMenuColor(On.Expedition.ExpeditionProgression.orig_BurdenMenuColor orig, string key)
    {
		if (key == "bur-rotund")
		{
			return new Color(0.55f, 0.35f, 0f);
		}
		return orig.Invoke(key);
	}

    private static float ExpeditionProgression_BurdenScoreMultiplier(On.Expedition.ExpeditionProgression.orig_BurdenScoreMultiplier orig, string key)
    {
		if (key == "bur-rotund")
		{
			return 35f;
		}
		return orig.Invoke(key);
	}

    private static string ExpeditionProgression_BurdenManualDescription(On.Expedition.ExpeditionProgression.orig_BurdenManualDescription orig, string key)
    {
		if (key == "bur-rotund")
		{
			return ExpeditionProgression.IGT.Translate("Makes the player exceptionally prone to getting fat, making you struggle with your size even at the minimum food requirment.");
		}
		return orig.Invoke(key);
	}

    private static string ExpeditionProgression_BurdenName(On.Expedition.ExpeditionProgression.orig_BurdenName orig, string key)
    {
		if (key == "bur-rotund")
		{
			return ExpeditionProgression.IGT.Translate("OBESE");
		}
		return orig.Invoke(key);
	}

    private static void ExpeditionProgression_SetupBurdenGroups(On.Expedition.ExpeditionProgression.orig_SetupBurdenGroups orig)
    {
		//orig();
		orig.Invoke();
		
		if (BellyPlus.expdEnhancedEnabled)
			return;
		
		//List<string> oldGroups = ExpeditionProgression.burdenGroups["expedition"];
		//List<string> value2 = new List<string>
		//{
		//	"bur-rotund"
		//};
		//ExpeditionProgression.burdenGroups.Add("expedition", value2);
		ExpeditionProgression.burdenGroups["expedition"].Add("bur-rotund");
	}

    
    
	private static void ExpeditionProgression_SetupPerkGroups(On.Expedition.ExpeditionProgression.orig_SetupPerkGroups orig)
	{
		orig.Invoke();
		
		if (BellyPlus.expdEnhancedEnabled)
			return;
		
		ExpeditionProgression.perkGroups["expedition"].Add("unl-foodlover");
	}


    private static string ExpeditionProgression_UnlockName(On.Expedition.ExpeditionProgression.orig_UnlockName orig, string key)
    {
		if (key == "unl-foodlover")
			return ExpeditionProgression.IGT.Translate("Food Lover");
		else
			return orig.Invoke(key);
	}

    private static string ExpeditionProgression_UnlockDescription(On.Expedition.ExpeditionProgression.orig_UnlockDescription orig, string key)
    {
		if (key == "unl-foodlover")
			return ExpeditionProgression.IGT.Translate("Allows the player to eat all food types for their full value");
		else
			return orig.Invoke(key);
	}


    private static string ExpeditionProgression_UnlockSprite(On.Expedition.ExpeditionProgression.orig_UnlockSprite orig, string key, bool alwaysShow)
    {
		//if (ExpeditionData.unlockables.Contains(key) || alwaysShow)
		//ALWAYS AVAILABLE
		if (key == "unl-foodlover")
		{
			return "Symbol_EggBugEgg";
		}
		
		return orig.Invoke(key, alwaysShow);
	}

    private static Color ExpeditionProgression_UnlockColor(On.Expedition.ExpeditionProgression.orig_UnlockColor orig, string key)
    {
		if (key == "unl-foodlover")
			return new Color(0f, 1f, 0.47058824f);
		else
			return orig.Invoke(key);
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
	
	
	
	// public static void BP_ChangeRoom(On.RoomCamera.ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
	// {
		// orig.Invoke(self, newRoom, cameraPosition);
		// //REFRESH ANY SLUGCAT SQUEEZING SOUNDS
		// if (this.room != null)
		// {
			
		// }
	// }

    

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
		//LUCKILY, myFoodInStomach VALUES REMAIN IN MEMORY LONG AFTER THE PLAYER HAS BEEN REMOVED FROM THE WORLD
		int myFood = patch_Player.bellyStats[plrNum].myFoodInStomach;
        intTrck.SetProgress(myFood);
        //WE JUST NEED TO PRETEND WE NEVER MAKE PROGRESS OKAY?
        intTrck.showFrom = myFood;
        intTrck.lastShownProgress = myFood;
		if (BPOptions.debugLogs.Value)
			Debug.Log("SAVING INDIVIDUAL PLAYER WEIGHT P:" + plrNum + " FOOD: " + myFood);
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
				integerTracker5.SetProgress(integerTracker5.progress - Mathf.Max(2 - Mathf.CeilToInt(BPOptions.bpDifficulty.Value / 2), 1) );
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
public class Obese : CustomBurden
{
	public override float ScoreMultiplier => 35f;
	public override string ID => "bur-rotund";
	
	public override string Name => "OBESE";
	public override string ManualDescription => "Makes the player exceptionally prone to getting fat, making you struggle with your size even at the minimum food requirment.";
	public override Color Color => new Color(0.55f, 0.35f, 0f);
	public override bool AlwaysUnlocked => true;
}


public class FoodLover : CustomPerk
{
    public override string ID => "unl-foodlover"; 
    public override string Name => "Food Lover"; 
    
    public override string Description => "Allows the player to eat all food types for their full value";
    public override string ManualDescription => "Allows the player to eat all food types for their full value";
    
    public override string SpriteName => "Symbol_EggBugEgg";
    public override Color Color => new Color(0f, 1f, 0.47058824f); 
    public override bool AlwaysUnlocked => true;
    public override CustomPerkType PerkType => CustomPerkType.Custom;
}





