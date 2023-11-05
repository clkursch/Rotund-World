using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using ExpeditionEnhanced;


//TO REFERENCE PRIVATED VARIABLES (thnx again slime_cubed)
//REPORTADLY, THIS IS ONLY NEEDED BECAUSE OF A BUG (THAT MAY GET PATCHED)
using System.Security;
using System.Security.Permissions;
using BepInEx.Logging;
using SprobParasiticScug;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


/*
If you want something to reference for how to store more info for a creature.... 
have a look at ConcealedGarden -> CGYellowThoughtsAdaptor, it's the code that gives the yellow lizards the silly speech bubbles if you played that mod


------------------

# Become certifiably Rotund and make the game more challenging by getting fatter the closer you are to being full.
# Get too chubby and you'll struggle to squeeze through pipes or pull yourself onto beams.
# Eat past your maximum belly size to gain even more food pips at a reduced rate! Just don't get too greedy...
# Pipes all have a random size, so some will be harder to squeeze through than others

[h1]Sprinkle obesity into the rain world wildlife[/h1]
Other creatures like lizards and Lantern mice start with random levels of chub and will continue to get chunky as they eat
Haul overweight lantern mice through the shaded citadel
Point and laugh at fat lizards that get stuck trying to chase you through pipes
Become the roundest lizard yourself in Safari Mode by dragging prey into your den to eat


[h1]Struggle with your friends in Jolly Co-op[/h1] @@Fat shame your friends in Jolly Co-op
Help the fat ones in the group through small gaps by grabbing and pulling them or shoving against them
Use the jump button while pushing or pulling to spend stamina and shove harder
Dash, belly slide, or pounce into stuck creatures to ram into them with your momentum
No friends to play with? Enlist the help of friendly scavengers or tamed lizards to help you through tight spots


Other random features:
[list]
[*]Goofy sound effects
[*]Iterator Shenanigans
[*]Should work with most custom slugcats!
[*](Until fancySlugcats comes along to ruin that)
[*]Store food items on your back for later 
[*]Point tamed lizards in a direction you want them to go
[*]Play as Gourmand for LeChonk hard mode
[*]Gourmand bodyslams do extra damage based on how fat he is
[/list]


[hr][/hr]
Please report any bugs you find so I can fix them! 
I am not very active on steam, so feel free to send me bug reports, error logs, suggestions and feedback to my Discord! WillowWisp#3565

Suggestions and feedback on the mod are encouraged and appreciated! (rude comments unrelated to gameplay are not so much appreciated, thnx)


---------

If your game freezes or crashes, it would REALLY help if you could send me the error logs (\Program Files (86)\Steam\steamapps\common\Rain World\exceptionLog.txt) before re-launching the game!
The logs are wiped every time the game is opened. But if you don't see the exception logs in there, they probably didn't get generated.



-----------------------------
TRANSLATABLE

Becoming fat will make you struggle to fit through pipes and become stuck, and struggle to climb on poles.
Other creatures become fat too! Lizards get a random level of fat, and will gain weight when you feed them




Make yourself and the creatures around you round and chubby!
Add a new challenge by becoming fat as you eat more food. Become obese by eating more than you can handle.
Struggle to run, climb, and jump, or get too fat to fit through pipes and get stuck in small tunnels. 
Pipes all have a random size, and the difficulty can be adjusted in the mod configuration menu.

[]Sprinkle obesity into the wildlife[]
Other creatures start with random levels of chub and will continue to get chunky as they eat.
Haul overweight lantern mice through the shaded citadel.
Point and laugh at fat lizards that get stuck trying to chase you through pipes.
Become the roundest lizard in Safari Mode by dragging food into your den to eat it.

[]Struggle with your friends in cooperative mode[]
Help the fat group members through small gaps by pulling or shoving against them.
Use the jump button while pushing or pulling to spend stamina and shove harder.
Dash, slide, or pounce into stuck creatures to ram into them.
Enlist the help of friendly scavengers or tamed lizards to help you through tight spots.

Certain fruits can be used as a lubricant to slip through pipes. (berries, mushrooms, slime, etc)
While stuck, tab the [Pickup/Eat] button to smear fruit on yourself.
Push against stuck creatures with fruit in hand and press [Jump] to smear it on them.


Other features:
[list]
[*]Goofy sound effects.
[*]Robot Shenanigans.
[*]Store food items on your back for later.
[*]Point tamed lizards in the direction you want them to go.
[*]Gourmand bodyslam does extra damage based on how fat he is.
[/list]


(This is not my native language, so pleae correct me if I translated something wrong!)
This item can only be swallowed if there are no items stored in your belly.
*/
[BepInPlugin("willowwisp.bellyplus", "Rotund World", "1.8.10")]
//[BepInProcess("RainWorld.exe")]

public class BellyPlus : BaseUnityPlugin
{

    public static BellyPlus instance;
	public static BPOptions myOptions;
	public static new ManualLogSource Logger;
	public static bool is_post_mod_init_initialized = false;

	//public override void OnEnable()
	public void OnEnable()
    {
		try
		{
			//patch_SoundImporter.Patch();
			//base.OnEnable();
			//RainWorld.Start += new RainWorld.hook_Start(this.RainWorld_Start);
			Logger = base.Logger;

			On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;
			On.RainWorld.PostModsInit += RainWorld_PostModsInit;
			//if (ModManager.InstalledMods)

			//On.RainWorld.Start += BP_RainWorld_Start;
			
			patch_Player.Patch();
			patch_PlayerGraphics.Patch();
			patch_SlugNPCAI.Patch();

			patch_LanternMouse.Patch();
			patch_MouseGraphics.Patch();
			patch_MouseAI.Patch();

			patch_Lizard.Patch();
			patch_LizardGraphics.Patch();
			patch_LizardAI.Patch();

			patch_Scavenger.Patch();
			patch_ScavengerGraphics.Patch();

			patch_Cicada.Patch();
			patch_CicadaGraphics.Patch();
			
			//patch_Yeek.Patch(); //MAYBE SOMEDAY...
			// patch_Noot.Patch(); //MOVED TO GENERIC
            patch_DLL.Patch();

            patch_AbstractCreature.Patch();
			
			// if (!BellyPlus.VisualsOnly())
			//------
			patch_Vulture.Patch();
			//if (ModManager.MSC) //DON'T DO THAT

			patch_SlugcatHand.Patch();
			patch_FoodMeter.Patch();
			patch_SaveState.Patch();

			patch_VirtualMicrophone.Patch(); //DUNNO IF THIS ACTUALLY WORKED
			patch_ShelterDoor.Patch();//HOW DID I MISS THIS???
			patch_RainCycle.Patch();
			//------
			
            patch_SeedCob.Patch();
			patch_Misc.Patch();
			patch_ProcessManager.Patch();
			patch_OracleBehavior.Patch(); //THESE COUNT AS VISUALS. THE CHANGES CAN STAY
			patch_OverseerTutorial.Patch();
			
			//OUTDATED 
			// BellyPlus.theThinOnes.Add(0, false);
			// BellyPlus.theThinOnes.Add(1, false);
			// BellyPlus.theThinOnes.Add(2, false);
			// BellyPlus.theThinOnes.Add(3, false);
			//IT WILL ALWAYS BE 4, SO WE WON'T NEED TO FIDDLE WITH RESETS 

		}
		catch (Exception arg)
		{
            BellyPlus.Logger.LogInfo("BELLYPLUS LOAD FAILURE DETECTED!");
            BellyPlus.Logger.LogInfo(arg);
            base.Logger.LogError(string.Format("Failed to initialize Rotund World", arg));
			throw;
		}

		
	}

	



	private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		orig(self);
		BPEnums.BPSoundID.RegisterValues();
		MachineConnector.SetRegisteredOI("willowwisp.bellyplus", new BPOptions());

		//CHECK MODS TO SEE IF RIDABLE LIZARS IS THERE...
		for (int i = 0; i < ModManager.ActiveMods.Count; i++)
		{
			if (ModManager.ActiveMods[i].id == "NoirCat.RideableLizards")
            {
				ridableLizEnabled = true;
			}
			if (ModManager.ActiveMods[i].id == "dressmyslugcat")
            {
				dressMySlugcatEnabled = true;
			}
            if (ModManager.ActiveMods[i].id == "NoirCatto.NoirCatto")
            {
                noircatEnabled = true;
            }
			if (ModManager.ActiveMods[i].id == "expeditionenhanced")
            {
                expdEnhancedEnabled = true;
				ExpdEnhancedContent();
            }
            if (ModManager.ActiveMods[i].id == "sprobgik.parasitescug")
            {
                parasiticEnabled = true;
                //"id": "SprobParasite",
				//"name": "The Parasite",
            }
            if (ModManager.ActiveMods[i].id == "sprobgik.individualfoodbars")
            {
                individualFoodEnabled = true;
            }
			if (ModManager.ActiveMods[i].id == "improved-input-config")
            {
                improvedInputEnabled = true;
            }
        }
    }
	
	private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);
        //I BARELY UNDERSTAND HOW THIS WORKS BUT SHUAMBUAM SEEMS TO HAVE IT ON LOCK SO I'LL JUST FOLLOW HIS LEAD
        if (is_post_mod_init_initialized) return;
        is_post_mod_init_initialized = true;
        if (improvedInputEnabled)
            Initialize_Custom_Input();
    }
    public static void Initialize_Custom_Input()
    {
        // wrap it in order to make it a soft dependency only;
        Debug.Log("Initialize custom input.");
        RWInputMod.Initialize_Custom_Keybindings();
        PlayerMod.OnEnable();
    }


    public void ExpdEnhancedContent()
    {
        ExpeditionsEnhanced.RegisterExpeditionContent(new Obese(), new FoodLover());
    }


    private void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
    {
		orig(self, newlyDisabledMods);
		BPEnums.BPSoundID.UnregisterValues();
	}

    //MAYBE WE CAN USE THIS TO DETERMINE IF THE BONUS FOOD PIP IS SAVED 
    public static int bonusFood = 0;
	public static int tomorrowsBonusFood = 0;
	// public static bool jollyCoopEnabled = false;
    // public static bool jollyFixesEnabled = false;
	// public static bool fancySlugsEnabled = false;
    public static int bonusHudPip = 0;
	public static int lizardFood = 0;
	public static int MaxBonusPips = 40;
    public static int StoredCorn = 0;
    public static int StoredStumps = 0;

    //1.9 MOD CHEKCS
    public static bool ridableLizEnabled = false;
	public static bool dressMySlugcatEnabled = false;
    public static bool noircatEnabled = false;
	public static bool expdEnhancedEnabled = false;
    public static bool parasiticEnabled = false;
	public static bool individualFoodEnabled = false;
	public static bool improvedInputEnabled = false;

    public static bool fullBellyOverride = false;
	public static bool versionCheck = false;

    public static bool struggleHintGiven = false; //BUT PEOPLE JUST IGNORE IT :/
	public static int struggleHint = 350;
	public static bool backFoodHint1 = false;
	public static bool backFoodHint2 = false;
	public static bool smearHintGiven = false;
	public static bool pullupHintGiven = false;
	public static bool neuronHintGiven = false;


	public static bool noRain = false;
	public static bool needleFatResistance = false;
	public static bool yeeksOn = false;
    public static bool sharedPips = false;
    public static bool lockEndFood = false;
    public static bool fakeFoodVal = false; //THIS IS THE CRAZIEST THING I'VE EVER HAD TO DO FOR A HOOK

	
	/* //NOW OUTDATED AND UNUSED
    //THIS ONE IS SPECIFICALLY FOR THE 4 MAIN PLAYERS THAT COULD POTENTIALLY NEED TO REMEMBER THEIR WEIGHT AT THE END OF A CYCLE
    public static Dictionary<int, bool> theThinOnes = new Dictionary<int, bool>(4);
	
	public static void ThinAll()
	{
		for (int i = 0; i < 4; i++)
		{
			BellyPlus.theThinOnes[i] = true;
		}
	}
	
	public static void ClearThins()
	{
		for (int i = 0; i < 4; i++)
		{
			BellyPlus.theThinOnes[i] = false;
		}
	}
	*/
	
	
	//
	//THESE ARE CURRENTLY USED FOR NON-PLAYERS, CUZ I'M A DUMMY 
	static int dictSize = 0;
	public static Dictionary<int, int> myFoodInStomach = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, int> myChubValue = new Dictionary<int, int>(dictSize);
	// public static Dictionary<int, int> bonusFoodPoints = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, int> fwumpDelay = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, bool> isSqueezing = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, bool> assistedSqueezing = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, bool> pushingOther = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, bool> pullingOther = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, int> targetStuck = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, bool> isStuck = new Dictionary<int, bool>(dictSize);            //True if our hips are pressed against an entrance to a narrow space
	public static Dictionary<int, bool> verticalStuck = new Dictionary<int, bool>(dictSize);      //True if our stuck orientation is vertical
	public static Dictionary<int, bool> inPipeStatus = new Dictionary<int, bool>(dictSize);       //true once we've transitioned all the way into a narrow space. false after leaving it
	public static Dictionary<int, int> corridorExhaustion = new Dictionary<int, int>(dictSize);  //Stamina meter that, if it passes a threshold, puts slugcat into an exhausted state
	public static Dictionary<int, int> timeInNarrowSpace = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, int> squeezeStrain = new Dictionary<int, int>(dictSize);       //Collective effort spent sliding through a corridor
	public static Dictionary<int, float> stuckStrain = new Dictionary<int, float>(dictSize);           //Collective effort spent trying to squeeze into an entrance corridor
	public static Dictionary<int, float> loosenProg = new Dictionary<int, float>(dictSize);
	public static Dictionary<int, float> tileTightnessMod = new Dictionary<int, float>(dictSize);
	public static Dictionary<int, int> noStuck = new Dictionary<int, int>(dictSize);             //Grace period after popping free in which we can't get stuck again
	public static Dictionary<int, int> shortStuck = new Dictionary<int, int>(dictSize);          //A brief counter for temporary stucks when matching the gap size
	public static Dictionary<int, int> boostStrain = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, int> beingPushed = new Dictionary<int, int>(dictSize);         //A soft boolean. Treat any value > 0 as true. 
	public static Dictionary<int, int> myFlipValX = new Dictionary<int, int>(dictSize);          //The current horizontal facing direction for trying to squeeze into corridors
	public static Dictionary<int, int> myFlipValY = new Dictionary<int, int>(dictSize);          //^^^ same but for vertical corridors
	public static Dictionary<int, float> myCooridorSpeed = new Dictionary<int, float>(dictSize); //A NEW VERSION THAT TRACKS BETWEEN PLAYERS
	public static Dictionary<int, float> myLastVel = new Dictionary<int, float>(dictSize);
	public static Dictionary<int, bool> breathIn = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, int> myHeat = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, int> wideEyes = new Dictionary<int, int>(dictSize);
	// Dictionary<int, ChunkSoundEmitter squeezeLoop = new Dictionary<int, >(dictSize);
	public static Dictionary<int, bool> lungsExhausted = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, int> boostTimer = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, float> myFatness = new Dictionary<int, float>(dictSize);
	// public static Dictionary<int, int> freshFromShortcut = new Dictionary<int, int>(dictSize); //THIS ALREADY EXISTS. ITS self.shortcutdelay
	public static Dictionary<int, bool> stuckInShortcut = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, bool> scoreMeal = new Dictionary<int, bool>(dictSize);
	public static Dictionary<int, int> slicked = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, float> wedgeStrain = new Dictionary<int, float>(dictSize);
	public static Dictionary<int, Vector2> stuckCoords = new Dictionary<int, Vector2>(dictSize);
	public static Dictionary<int, Vector2> stuckVector = new Dictionary<int, Vector2>(dictSize);
	//public static Dictionary<int, float> terrSqzMemory = new Dictionary<int, float>(dictSize);
	public static Dictionary<int, ChunkSoundEmitter> stuckLoop = new Dictionary<int, ChunkSoundEmitter>(dictSize);
	
	
	//THESE ARE THE IMPORTANT ONES THAT NEED TO GET SAVED IF DICTIONARIES ARE GARBAGE COLLECTED
	public static Dictionary<int, int> tempMyFoodInStomach = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, int> tempMyChubValue = new Dictionary<int, int>(dictSize);
	public static Dictionary<int, Creature> tempGuestBook = new Dictionary<int, Creature>(dictSize);


    //FOR CREATURES THAT STORY BELLY SIZE AND NOTHING ELSE
    public static void InitializeCreatureMini(int creatureID)
	{
        try
        {
            BellyPlus.myFoodInStomach.Add(creatureID, 0);
        }
        catch (ArgumentException)
        {
            if (BPOptions.debugLogs.Value)
				Console.WriteLine("FOODINSTOMACH ALREADY EXISTS!!!");
        }
    }


	//FOR FULLY STUCKABLE CREATURES
    public static void InitializeCreature(int creatureID)
	{
		//WE BETTER WRITE SOMETHING FOR THIS...
		try
		{
			BellyPlus.myFoodInStomach.Add(creatureID, 0);
		}
		catch (ArgumentException)
		{
			Console.WriteLine("FOODINSTOMACH ALREADY EXISTS!!! WE BETTER SKIP THE REST");
			return;
		}
		
		BellyPlus.myChubValue.Add(creatureID, 0);
		BellyPlus.fwumpDelay.Add(creatureID, 0);
		BellyPlus.isSqueezing.Add(creatureID, false);
		BellyPlus.assistedSqueezing.Add(creatureID, false);
		BellyPlus.pushingOther.Add(creatureID, false);
		BellyPlus.pullingOther.Add(creatureID, false);
		// targetStuck.Add(creatureID, 0);
		BellyPlus.isStuck.Add(creatureID, false);
		BellyPlus.verticalStuck.Add(creatureID, false);
		BellyPlus.inPipeStatus.Add(creatureID, false);
		BellyPlus.corridorExhaustion.Add(creatureID, 0);
		BellyPlus.timeInNarrowSpace.Add(creatureID, 0);
		BellyPlus.squeezeStrain.Add(creatureID, 0);
		BellyPlus.stuckStrain.Add(creatureID, 0);
		BellyPlus.loosenProg.Add(creatureID, 0);
		BellyPlus.tileTightnessMod.Add(creatureID, 0);
		BellyPlus.noStuck.Add(creatureID, 0);
		BellyPlus.shortStuck.Add(creatureID, 0);
		BellyPlus.boostStrain.Add(creatureID, 0);
		BellyPlus.beingPushed.Add(creatureID, 0);
		BellyPlus.myFlipValX.Add(creatureID, 1);
		BellyPlus.myFlipValY.Add(creatureID, 1);
		BellyPlus.myCooridorSpeed.Add(creatureID, 1f);
		BellyPlus.myLastVel.Add(creatureID, 0f);
		BellyPlus.breathIn.Add(creatureID, false);
		BellyPlus.myHeat.Add(creatureID, 0);
		BellyPlus.wideEyes.Add(creatureID, 0);
		//slideLoop2.Add(creatureID, null);
		// squeezeLoop.Add(creatureID, null);
		BellyPlus.stuckLoop.Add(creatureID, null);
		BellyPlus.lungsExhausted.Add(creatureID, false);
		BellyPlus.boostTimer.Add(creatureID, 0);
		BellyPlus.myFatness.Add(creatureID, 1f);
		// BellyPlus.freshFromShortcut.Add(creatureID, 0);
		BellyPlus.stuckInShortcut.Add(creatureID, false); //I DON'T THINK THIS WAS EVEN EFFECTIVE BUT WHATEVER
		BellyPlus.scoreMeal.Add(creatureID, false);
		BellyPlus.slicked.Add(creatureID, 0);
		BellyPlus.wedgeStrain.Add(creatureID, 0f);
		BellyPlus.stuckCoords.Add(creatureID, new Vector2(0,0));
		BellyPlus.stuckVector.Add(creatureID, new Vector2(0,0));
		//BellyPlus.terrSqzMemory.Add(creatureID, 0f);
	}


	public static int GetRef(Creature self)
	{
		return self.abstractCreature.ID.RandomSeed;
	}
	
	
	
	
	public static void GarbageCollect()
	{
		Debug.Log("GARBAGE COLLECTING! BP");
		
		BellyPlus.myFoodInStomach.Clear();
		BellyPlus.myChubValue.Clear();
		BellyPlus.fwumpDelay.Clear();
		BellyPlus.isSqueezing.Clear();
		BellyPlus.assistedSqueezing.Clear();
		BellyPlus.pushingOther.Clear();
		BellyPlus.pullingOther.Clear();
		BellyPlus.isStuck.Clear();
		BellyPlus.verticalStuck.Clear();
		BellyPlus.inPipeStatus.Clear();
		BellyPlus.corridorExhaustion.Clear();
		BellyPlus.timeInNarrowSpace.Clear();
		BellyPlus.squeezeStrain.Clear();
		BellyPlus.stuckStrain.Clear();
		BellyPlus.loosenProg.Clear();
		BellyPlus.tileTightnessMod.Clear();
		BellyPlus.noStuck.Clear();
		BellyPlus.shortStuck.Clear();
		BellyPlus.boostStrain.Clear();
		BellyPlus.beingPushed.Clear();
		BellyPlus.myFlipValX.Clear();
		BellyPlus.myFlipValY.Clear();
		BellyPlus.myCooridorSpeed.Clear();
		BellyPlus.myLastVel.Clear();
		BellyPlus.breathIn.Clear();
		BellyPlus.myHeat.Clear();
		BellyPlus.wideEyes.Clear();
		//slideLoop2.Clear();
		// squeezeLoop.Clear();
		BellyPlus.stuckLoop.Clear();
		BellyPlus.lungsExhausted.Clear();
		BellyPlus.boostTimer.Clear();
		BellyPlus.myFatness.Clear();
		// BellyPlus.freshFromShortcut.Clear();
		BellyPlus.stuckInShortcut.Clear();
		BellyPlus.scoreMeal.Clear();
		BellyPlus.slicked.Clear();
		BellyPlus.wedgeStrain.Clear();
		BellyPlus.stuckCoords.Clear();
		BellyPlus.stuckVector.Clear();
		//BellyPlus.terrSqzMemory.Clear();

		//CREATURE SPECIFIC BOOKS
		patch_LanternMouse.mouseBook.Clear();
		patch_Lizard.lizardBook.Clear();
		patch_Scavenger.scavBook.Clear();
		patch_Cicada.cicadaBook.Clear();
		//patch_Yeek.yeekBook.Clear();
		patch_Vulture.vultureBook.Clear();
        patch_DLL.leviathanBook.Clear();
        patch_DLL.miscBook.Clear();

		
    }


    public static void InitPSFoodValues(AbstractCreature self)
    {
        //THIS ONE IS TO SET THEIR INTERNAL FOOD BASED ON OUR FATNESS INSTEAD OF THE OTHER WAY AROUND
        if (self.vars() != null) //&& !CheckForParasite(self) //patch_DLL.CheckFattable(self.realizedCreature) && 
        {
			int spawningChub = patch_Lizard.GetChubValue(self.realizedCreature);
            if (spawningChub > 1) //IF WE'RE SKINNY, DON'T START US WITH MUCH EXTRA FOOD
            {
                self.vars().food = self.vars().maxFood - 4 + spawningChub;
                //Debug.Log("STARTING CHUB! " + self.vars().food);
            }
        }
    }


    public static void RefreshDictionaries(Room room)
	{
		Debug.Log("BP-INITIATING DICTIONARY REFRESH!");
		//STORE IMPORTANT TEMP DATA OF ANY VALID CREATURES IN THE ROOM
		for (int i = 0; i < room.abstractRoom.creatures.Count; i++)
		{
			Creature myCrit = room.abstractRoom.creatures[i].realizedCreature;
			if (myCrit != null 
				&& BellyPlus.myFoodInStomach.ContainsKey(GetRef(myCrit)) //IF THEY HAVE THIS, THEY'RE VALID
				// && room.abstractRoom.creatures[i].realizedCreature is Lizard
			)
			{
				//BellyPlus.myFoodInStomach.Add(GetRef(myCrit), BellyPlus.myFoodInStomach[GetRef(myCrit)]); //WTF? THIS IS NOT CORRECT
				BellyPlus.tempMyFoodInStomach.Add(GetRef(myCrit), BellyPlus.myFoodInStomach[GetRef(myCrit)]);
				BellyPlus.tempMyChubValue.Add(GetRef(myCrit), BellyPlus.myChubValue[GetRef(myCrit)]);
				BellyPlus.tempGuestBook.Add(GetRef(myCrit), myCrit);
				Debug.Log("STORING CRITTER DATA FOR GARBAGE COLLECTION!" + GetRef(myCrit) + " : " + BellyPlus.myFoodInStomach[GetRef(myCrit)]);
			}
		}

		//CLEAR ALL THE DICTIONARIES
		GarbageCollect();
		
		//RE-ADD ANY IMPORTANT CREATURE VALUES
		foreach (KeyValuePair<int, Creature> kvp in BellyPlus.tempGuestBook)
		{
			if (kvp.Value is LanternMouse)
				patch_LanternMouse.mouseBook.Add(kvp.Key, kvp.Value as LanternMouse);
			else if (kvp.Value is Lizard)
				patch_Lizard.lizardBook.Add(kvp.Key, kvp.Value as Lizard);
			else if (kvp.Value is Cicada)
				patch_Cicada.cicadaBook.Add(kvp.Key, kvp.Value as Cicada);
			else if (kvp.Value is MoreSlugcats.Yeek)
				patch_Yeek.yeekBook.Add(kvp.Key, kvp.Value as MoreSlugcats.Yeek);
			else if (kvp.Value is Scavenger)
				patch_Scavenger.scavBook.Add(kvp.Key, kvp.Value as Scavenger);

			//RE-INITIALIZE
			InitializeCreature(kvp.Key);
		}
		
		foreach (KeyValuePair<int, int> kvp in BellyPlus.tempMyFoodInStomach)
		{
			BellyPlus.myFoodInStomach[kvp.Key] = kvp.Value;
			Debug.Log("RESTORING CRITTER BELLY DATA!" + kvp.Key + " : " + kvp.Value);
		}
		foreach (KeyValuePair<int, int> kvp in BellyPlus.tempMyChubValue)
		{
			BellyPlus.myChubValue[kvp.Key] = kvp.Value;
		}
		
		//DON'T UNFAT OUR LIZARDS PLEASE
		foreach (KeyValuePair<int, Creature> kvp in BellyPlus.tempGuestBook)
		{
			patch_Lizard.ObjUpdateBellySize(kvp.Value);
		}
		
		
		BellyPlus.tempMyFoodInStomach.Clear();
		BellyPlus.tempMyChubValue.Clear();
		BellyPlus.tempGuestBook.Clear();
	}

	
	
	public static bool SafariJumpButton(Creature self)
	{
		if (self.safariControlled && self.inputWithDiagonals != null && self.inputWithDiagonals.Value.jmp && self.lastInputWithDiagonals != null && !self.lastInputWithDiagonals.Value.jmp)
			return true;
		else
			return false;
	}
	
	
	public static bool VisualsOnly()
	{
		return BPOptions.visualsOnly.Value;
	}
	
	

	public static bool AnyPlayersInStartShelter(Room room)
	{
		bool shelterTrapped = false;
		for (int i = 0; i < room.game.Players.Count; i++)
		{
			//Debug.Log("-STILL IN START?" + i + " -:" + (room.game.Players[i].realizedCreature as Player).stillInStartShelter);
			if (room.game.Players[i].realizedCreature != null && (room.game.Players[i].realizedCreature as Player).stillInStartShelter && !(room.game.Players[i].realizedCreature as Player).dead)
				shelterTrapped = true;
		}
		return shelterTrapped;
	}


	public static bool AllPlayersInStartShelter(Room room)
	{
		bool shelterTrapped = true;
		for (int i = 0; i < room.game.Players.Count; i++)
		{
			if (room.game.Players[i].realizedCreature != null && (room.game.Players[i].realizedCreature as Player).stillInStartShelter == false)
				shelterTrapped = false;
		}
		return shelterTrapped;
	}

}