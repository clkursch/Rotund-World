using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
//using ExpeditionEnhanced;


//TO REFERENCE PRIVATED VARIABLES (thnx again slime_cubed)
//REPORTADLY, THIS IS ONLY NEEDED BECAUSE OF A BUG (THAT MAY GET PATCHED)
using System.Security;
using System.Security.Permissions;
using BepInEx.Logging;
using SprobParasiticScug;
using MonoMod.RuntimeDetour;
using RWCustom;
using System.Runtime.CompilerServices;
using System.Net;
using RainMeadow;
using MonoMod.Utils;
using System.Linq;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

//[BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.SoftDependency)] //FOR THAT ONE FOOD EATING HOOK - guess we didn't need it!

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


namespace RotundWorld;


[BepInPlugin("willowwisp.bellyplus", "Rotund World", "1.10.7")]

public class BellyPlus : BaseUnityPlugin
{

	public static BPOptions myOptions;
	public static new ManualLogSource Logger;
	public static bool is_post_mod_init_initialized = false;

	//public override void OnEnable()
	public void OnEnable()
    {
		try
		{
			Logger = base.Logger;

			On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsDisabled += RainWorld_OnModsDisabled;
			On.RainWorld.PostModsInit += RainWorld_PostModsInit;
			
			patch_Player.Patch();
			patch_PlayerGraphics.Patch();
			patch_SlugNPCAI.Patch();

			patch_LanternMouse.Patch();
			patch_Lizard.Patch();
			patch_LizardGraphics.Patch();
			patch_LizardAI.Patch();
			patch_Scavenger.Patch();
			patch_Cicada.Patch();

			patch_MiscCreatures.Patch();

            patch_AbstractCreature.Patch();
			
			// if (!BellyPlus.VisualsOnly())
			//------
			patch_Vulture.Patch();
			//if (ModManager.MSC) //DON'T DO THAT

			patch_SlugcatHand.Patch();
			patch_FoodMeter.Patch();
			patch_SaveState.Patch();
			patch_RainCycle.Patch();
			//------
			
            patch_SeedCob.Patch();
			patch_Misc.Patch();
			patch_OracleBehavior.Patch(); //THESE COUNT AS VISUALS. THE CHANGES CAN STAY
			patch_OverseerTutorial.Patch();

            On.GameSession.ctor += GameSession_ctor;
			//On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate;
        }
		catch (Exception arg)
		{
            Logger.LogInfo("BELLYPLUS LOAD FAILURE DETECTED!");
            Logger.LogInfo(arg);
            base.Logger.LogError(string.Format("Failed to initialize Rotund World", arg));
			throw;
		}
        
    }




    private bool IsInit;
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
	{
		orig(self);
		//BellyPlus.RegisterValues(); //NO LONGER USING THIS FOR SOUND
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
            if (ModManager.ActiveMods[i].id == "sprobgik.parasitescug")
            {
                parasiticEnabled = true;
            }
            if (ModManager.ActiveMods[i].id == "sprobgik.individualfoodbars")
            {
                individualFoodEnabled = true;
            }
			if (ModManager.ActiveMods[i].id == "improved-input-config")
            {
                improvedInputEnabled = true;
			}
            if (ModManager.ActiveMods[i].id == "SplatCat")
            {
                splatCatEnabled = true;
            }
			if (ModManager.ActiveMods[i].id == "henpemaz_rainmeadow")
            {
                meadowEnabled = true;
            }
        }

        //OKAY WE'VE GOT A FEW HOOKS THAT NEED TO GO IN HERE FOR LOAD ORDER REASONS
        try
        {
            if (IsInit) return;

            On.SlugcatStats.NourishmentOfObjectEaten += SlugcatStats_NourishmentOfObjectEaten;
            On.Player.CanEatMeat += patch_Player.BPPlayer_CanEatMeat;
            On.PlayerGraphics.DrawSprites += patch_PlayerGraphics.LatePriorityDrawSprites;

			//REGISTER CUSTOM THINGS
			Modding.Expedition.CustomBurdens.Register(new Obese());
            Modding.Expedition.CustomPerks.Register(new FoodLover());

            IsInit = true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    //OKAY IT'S TIME TO LET THE BIG BOYS (SLIMECUBED) FIGURE OUT HOW TO DO SOUNDS CORRECTLY
    private void LoadResources(RainWorld rainWorld)
    {
        SqueezeLoop = new SoundID("SqueezeLoop", true);
        Pop1 = new SoundID("Pop1", true);
        Fwump1 = new SoundID("Fwump1", true);
        Squinch1 = new SoundID("Squinch1", true);
        Fwump2 = new SoundID("Fwump2", true);
    }

    public static SoundID SqueezeLoop;
    public static SoundID Pop1;
    public static SoundID Fwump1;
    public static SoundID Squinch1;
    public static SoundID Fwump2;


    private int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
    {
        //FOOD LOVER PERK! SURVIVOR GETS MAX FOOD VALUE FROM ALL EDIBLE OBJECTS SO LETS JUST DO IT THAT WAY
        if (patch_Player.IsFoodLover())
            slugcatIndex = SlugcatStats.Name.White;

        return orig.Invoke(slugcatIndex, eatenobject);
    }

    private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig(self);
        //I BARELY UNDERSTAND HOW THIS WORKS BUT SHUAMBUAM SEEMS TO HAVE IT ON LOCK SO I'LL JUST FOLLOW HIS LEAD
        if (is_post_mod_init_initialized) return;
        is_post_mod_init_initialized = true;
		
		//SOME MEADOW SPECIFIC HOOKS
		On.Player.AddFood += Player_AddFood;
		On.Player.SubtractFood += Player_SubtractFood;
        //On.GameSession.ctor += GameSession_ctor;
        //On.GameSession.AddPlayer += GameSession_AddPlayer;
        //On.StoryGameSession.AddPlayer += StoryGameSession_AddPlayer;
        //On.RainWorldGame.SpawnPlayers_int_WorldCoordinate += RainWorldGame_SpawnPlayers_int_WorldCoordinate;
        //On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate;
        On.Creature.ctor += Creature_ctor;
        On.Player.ctor += Player_ctor;
        On.Lizard.ctor += Lizard_ctor;
        On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;

		if (improvedInputEnabled)
            Initialize_Custom_Input();
		
		//patch_Misc.PostPatch(); //NO LONGER NEEDED FOR EXPD ENHANCED
    }

    private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
		//if (isMeadowSession)
		//	GetMeadowWeight();
	}

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
		orig(self, abstractCreature, world);
		//if (isMeadowSession)
		//	GetMeadowWeight();
	}

    private void GameSession_ctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig(self, game);

        if (meadowEnabled)
			MeadowGameSession(orig, self, game);
	}

	public static bool isMeadowClient;
    public static bool isMeadowSession;

    public static float lobbyDifficulty;
    public static int startThresh;
    public static float gapVariance;
    public static bool slugSlams;
    public static bool backFoodStorage;
    public static bool foodLoverPerk;

	public static float BPODifficulty()
	{
		return meadowEnabled ? lobbyDifficulty : BPOptions.bpDifficulty.Value;
    }

    public static int BPOStartThreshold()
    {
		return meadowEnabled ? startThresh : BPOptions.startThresh.Value;
    }

    public static float BPOGapVariance()
    {
        return meadowEnabled ? gapVariance : BPOptions.gapVariance.Value;
    }

    public static bool BPOSlugSlams()
    {
        return meadowEnabled ? slugSlams : BPOptions.slugSlams.Value;
    }

    public static bool BPOBackFoodStorage()
    {
        return meadowEnabled ? backFoodStorage : BPOptions.backFoodStorage.Value;
    }

    public static bool BPOFoodLoverPerk()
    {
        return meadowEnabled ? foodLoverPerk : BPOptions.foodLoverPerk.Value;
    }

	public static void SyncRemixOptions()
	{
		Debug.Log("SYNC REMIX OPTIONS");
		lobbyDifficulty = BPOptions.bpDifficulty.Value;
        startThresh = BPOptions.startThresh.Value;
        gapVariance = BPOptions.gapVariance.Value;
        slugSlams = BPOptions.slugSlams.Value;
        backFoodStorage = BPOptions.backFoodStorage.Value;
        foodLoverPerk = BPOptions.foodLoverPerk.Value;
    }

    public static void MeadowGameSession(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
	{
		if (OnlineManager.lobby == null)
		{
            Debug.Log("OFFLINE MODE. THIS IS NOT A MEADOW SESSION");
            isMeadowClient = false;
            isMeadowSession = false;
            SyncRemixOptions();
			orig(self, game);
			return; //THIS IS A NON ONLINE GAME. SKIP ALL THIS
		}
		else
		{
            Debug.Log("MEADOW SESSION = TRUE!");
            isMeadowSession = true;
            if (!OnlineManager.lobby.isOwner)
            {
                isMeadowClient = true;
                Debug.Log("MEADOW LOBBY CLIENT! WE ARE NOT THE OWNER");

                //SEND AN RPC TO THE HOST REQUESTING THAT IT SYNCREMIX WITH EVERYONE
                Debug.Log("REQUESTING A REMIX SYNC FROM THE HOST!!!");
                foreach (var player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeRPC(typeof(RotundRPCs).GetMethod("RequestRemixSync").CreateDelegate(typeof(Action<RPCEvent>)));
                    }
                }
            }
        }
    }

    /*
    private AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
    {
        AbstractCreature value = orig(self, player1, player2, player3, player4, location);
		Debug.Log("I'M LOADING IN A PLAYER");
        if (isMeadowSession)
        {
            //GetMeadowWeight()
        }
        return value;
    }
	*/

    //THE CLIENTS CAN JUST ASK NICELY FOR THIS NOW!
    /*
    public static void MeadowAddPlayer()
	{
        //MAY AS WELL INITIALIZE THIS EVERY TIME SOMEONE JOINS...
        if (OnlineManager.lobby.isOwner)
        {
            ApplyLobbyRPCs();
        }
    }


    public static void ApplyLobbyRPCs()
	{
        Debug.Log("LOBBY OWNER, APPLYING RPCS!");
        SyncRemixOptions();
        foreach (var player in OnlineManager.players)
        {
            if (!player.isMe)
            {
                //player.InvokeRPC(RotundRPCs.SyncRemix, lobbyDifficulty, startThresh, gapVariance, slugSlams, backFoodStorage, foodLoverPerk);
                //_ = new Hook(typeof(Player).GetProperty(nameof(Player.CanPutSlugToBack))!.GetGetMethod(), Player_CanPutSlugToBack_get);
                //typeof(RotundRPCs).GetMethod("SyncRemix").Invoke(null, new object[] { RotundRPCs.SyncRemix, lobbyDifficulty, startThresh, gapVariance, slugSlams, backFoodStorage, foodLoverPerk });
                //typeof(RotundRPCs).GetMethod("SyncRemix").Invoke(null, new RainMeadow.RPCEvent[] { RotundRPCs.SyncRemix, lobbyDifficulty, startThresh, gapVariance, slugSlams, backFoodStorage, foodLoverPerk });

                //player.InvokeRPC(typeof(RotundRPCs).GetMethod("SyncRemix").CreateDelegate(typeof(Action<float, int, float, bool, bool, bool>)), lobbyDifficulty, startThresh, gapVariance);

                //player.InvokeRPC(typeof(RotundRPCs).GetMethod("SyncRemix").CreateDelegate(typeof(Action<RPCEvent, float, int, float>)), RotundRPCs.SyncRemix, lobbyDifficulty, startThresh, gapVariance);
                player.InvokeRPC(typeof(RotundRPCs).GetMethod("SyncRemix").CreateDelegate(typeof(Action<RPCEvent, float, int, float>)), lobbyDifficulty, startThresh, gapVariance);
            }
        }
    }
	*/


    private void Creature_ctor(On.Creature.orig_ctor orig, Creature self, AbstractCreature abstractCreature, World world)
    {
		orig(self, abstractCreature, world);
        //CheckIfMeadowWeight(self);
        //if (isMeadowSession)
        //    GetMeadowWeight(self);
    }

    private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
    {
        orig(self);
		Debug.Log("WORLDLOADED COMPLETE");
		//CheckIfMeadowWeight(self);
		if (isMeadowSession)
			GetMeadowWeight();
	}


    //   public static void CheckIfMeadowWeight(Creature self)
    //{
    //	if (isMeadowSession)
    //		return GetMeadowWeight(self, int);
    //	else
    //		return value;
    //}

    //OKAY SO WE CAN ACTUALLY SET OUR myFoodInStomach VALUE AS EARLY AS WE WANT BECAUSE THE LIZARD CONSTRUCTOR WILL NOT ROLL THE STARTING VALUE IF ABSTRACTCREATURE ALREADY HAS myFoodInStomach DEFINED 


    public static void GetMeadowWeight()
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
		{
            Debug.Log("IN A MEADOW GAMEMODE!");
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                Debug.Log("CHECKING PLAYER..." + (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo2 && opo2.owner.isMe));
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.owner.isMe && opo.apo is AbstractCreature ac)
                {
                    Debug.Log("THIS IS MYSELF IN A MEADOW LOBBY!");
					int food = BPOptions.meadowFoodStart.Value; //GRAB VALUE FROM OUR REMIX MENU
                    ac.GetAbsBelly().myFoodInStomach = food; //TO SET THE VALUE FOR US, LOCALLY 
                    
					//THEN APPLY IT FOR ALL PLAYERS
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
							player.InvokeRPC(typeof(RotundRPCs).GetMethod("InitializeWeight").CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, int>)), opo, food);
                        }
                    }

					//UPDATE THE VALUE LOCALLY, IF NEEDED
					if (ac.realizedCreature != null)
					{
                        if (ac.realizedCreature is Player)
                            patch_Player.UpdateBellySize(ac.realizedCreature as Player);
                        else
                            patch_MiscCreatures.ObjUpdateBellySize(ac.realizedCreature);
                    }
				}
            }
        }
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
        //ExpeditionsEnhanced.RegisterExpeditionContent(new Obese(), new FoodLover());
    }

    public void YeekFixContent()
    {
        //patch_MiscCreatures.YeekFixPatch();
    }


    private void RainWorld_OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld self, ModManager.Mod[] newlyDisabledMods)
    {
		orig(self, newlyDisabledMods);
		//BellyPlus.UnregisterValues();
	}
	
	
	public static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
	{
		//HEY! MODDERS! THIS IS NOT HOW YOU SUBTRACT FOOD! LET ME FIX IF FOR YOU
		if (add < 0)
		{
			self.SubtractFood(-add);
			return;
		}
		
		if (isMeadowSession)
			MeadowAddFood(orig, self, add);
		else
			orig(self, add);
	}
	
	public static void MeadowAddFood(On.Player.orig_AddFood orig, Player self, int add)
	{
		if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
		if (onlineEntity.isMine || (self.isNPC && self.isSlugpup))
		{
			//self.AddFood(add); //PRETTY SURE THIS WILL JUST INFINI LOOP
			orig(self, add);
			return;
		}
		//THIS IS THE CONDITION THAT MEADOW SKIPS ORIG
		
		
		add *= BPOptions.foodMult.Value;

		//CORRECT EATMEAT() SHENANIGANS
        if (self.GetBelly().maxFoodOverrideFlag)
        {
            self.slugcatStats.maxFood--;
            self.GetBelly().maxFoodOverrideFlag = false;
        }
		//BUT WHAT IF WE DIDN'T?... COME BACK TO THIS LATER

        //SKIPPING REDS ILLNESS STUFF SORRY
		patch_Player.AddPersonalFood(self, add);
		
		// ISN'T THIS ALL WE NEED NOW?
		Debug.Log("-----MEADOW! ADDING FOOD " + self + " ADD:" + add + "  CURRENT CHUB:" + (patch_Player.GetChubFloatValue(self) + patch_Player.GetOverstuffed(self)) );
		//orig.Invoke(self, add);
	}
	
	
	public static void Player_SubtractFood(On.Player.orig_SubtractFood orig, Player self, int sub)
	{
		if (isMeadowSession)
			MeadowSubtractFood(orig, self, sub);
		else
			orig(self, sub);
	}
	
	public static void MeadowSubtractFood(On.Player.orig_SubtractFood orig, Player self, int sub)
	{
		if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
		if (onlineEntity.isMine || (self.isNPC && self.isSlugpup))
		{
			orig.Invoke(self, sub);
			return;
		}
		//THIS IS THE CONDITION THAT MEADOW SKIPS ORIG
		
		//IN THIS VERSION WE ONLY SUBTRACT PERSONAL CHUB FROM OURSELVES
		self.abstractCreature.GetAbsBelly().myFoodInStomach -= sub;
		
		//DON'T LET NEGATIVE CHUB VALUES BE A THING, SINCE WE CAN'T HAVE NEGATIVE FOOD
		if (self.abstractCreature.GetAbsBelly().myFoodInStomach < 0)
			self.abstractCreature.GetAbsBelly().myFoodInStomach = 0;
		
		//orig.Invoke(self, sub);
		
		patch_Player.UpdateBellySize(self);
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
	public static int popcornSpearable = 0;

    //1.9 MOD CHEKCS
    public static bool ridableLizEnabled = false;
	public static bool dressMySlugcatEnabled = false;
    // public static bool noircatEnabled = false;
	public static bool expdEnhancedEnabled = false;
    public static bool parasiticEnabled = false;
	public static bool individualFoodEnabled = false;
	public static bool improvedInputEnabled = false;
	public static bool splatCatEnabled = false;
	public static bool meadowEnabled = false;

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


	public static Dictionary<int, int> foodMemoryBank = new Dictionary<int, int>(16);
	

    //FOR CREATURES THAT STORY BELLY SIZE AND NOTHING ELSE
    public static void InitializeCreatureMini(Creature self) //int creatureID
	{
		
		//OK I THINK WE JUST SET THE FOOD TO 0 UNLESS IT ALREADY EXISTS
		if (self.abstractCreature.GetAbsBelly().myFoodInStomach == -1)
			self.abstractCreature.GetAbsBelly().myFoodInStomach = 0;
		
    }


	//FOR FULLY STUCKABLE CREATURES
    public static void InitializeCreature(int creatureID)
	{
		
	}


    public static void InitPSFoodValues(AbstractCreature self)
    {
        //THIS ONE IS TO SET THEIR INTERNAL FOOD BASED ON OUR FATNESS INSTEAD OF THE OTHER WAY AROUND
        if (self.vars() != null) //&& !CheckForParasite(self) //patch_MiscCreatures.CheckFattable(self.realizedCreature) && 
        {
			float spawningChub = patch_Lizard.GetChubValue(self.realizedCreature);
            if (spawningChub > 1f) //IF WE'RE SKINNY, DON'T START US WITH MUCH EXTRA FOOD
            {
                self.vars().food = Mathf.CeilToInt(self.vars().maxFood - 4 + spawningChub);
                //Debug.Log("STARTING CHUB! " + self.vars().food);
            }
        }
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



public static class AbsBellyClass
{
    public class AbsBelly
    {
        //FOR ABSTRACT
		public int myFoodInStomach = -1; //MEANS UNINITIALIZED
		public float origPoleSpeed;
		public int externalMass;
		public int chubModOffset;
    }

    // This part lets you access the stored stuff by simply doing "self.GetAbsBelly()" in BellyPlus.cs or everywhere else!
    private static readonly ConditionalWeakTable<AbstractCreature, AbsBelly> CWT = new();
    public static AbsBelly GetAbsBelly(this AbstractCreature crit) => CWT.GetValue(crit, _ => new());
}


public static class BellyClass
{
    public class Belly
    {
        //UNIVERSAL
		public int fwumpDelay;
		public float myChubValue;
        public bool fatDisabled;

        public bool assistedSqueezing;
		public int pushingOther;
		public Creature pushingCreature;
		public bool pullingOther;
		
		public bool isStuck;            //True if our hips are pressed against an entrance to a narrow space
		public bool verticalStuck;      //True if our stuck orientation is vertical
		public bool inPipeStatus;       //true if once we've transitioned all the way into a narrow space. false after leaving it
		public int corridorExhaustion;  //Stamina meter that, if it passes a threshold, puts slugcat into an exhausted state
		public int timeInNarrowSpace;
		
		public float stuckStrain;           //Collective effort spent trying to squeeze into an entrance corridor
		public float loosenProg;		//a value that reduces the difficulty of a squeeze very slowly over time
		public float tileTightnessMod;
		public int noStuck;             //Grace period after popping free in which we can't get stuck again
		public int shortStuck;          //A brief counter for temporary stucks when matching the gap size
		public int boostStrain;
		public int beingPushed;         //A soft boolean. Treat any value > 0 as true. 
		public int myFlipValX;          //The current horizontal facing direction for trying to squeeze into corridors
		public int myFlipValY;          //^^^ same but for vertical corridors
		public float myLastVel;
		public float wedgeStrain;
		public bool breathIn;
		public int myHeat;
		public int wideEyes;
		public int slicked;
		public Vector2 stuckVector;     //The direction we are stuck in
		public Vector2 stuckCoords;			//Last known stuck coords. set to 0 when unused
		public int boostCounter; //boostTimer
		public ChunkSoundEmitter stuckLoop;
		
		//CREATURE SPECIFIC
		public bool lungsExhausted;
		public float myFatness = 1f;
		public bool stuckInShortcut;
		
		//PLAYER SPECIFIC
		public bool bigBelly;
		public float myCooridorSpeed = 1f; //A NEW VERSION THAT TRACKS BETWEEN PLAYERS
		public float runSpeedMod = 1f;
		public bool isSqueezing;
		public int squeezeStrain;       //Collective effort spent sliding through a corridor
		public int targetStuck;
		public int fwumpFlag;
		public IntVector2 autoPilot;    //Held direction
		public int pilotTimer;			//How long until auto pilot deactivates
		public float holdJump;          //Frames of continously held jump (up to a cap)
		public int struggleHintCount;
		public bool breakfasted;
		public int boostBeef;           //briefly reduce the rate of boost decay, for effect
		public float lastBoost;
		public float wiggleCount;
		public IntVector2 wiggleMem;
		public bool landLocked;			//prevent us from entering shortcuts if we've picked up another player
		public int bpForceSleep;
        public int squishForce;
		public int squishMemory;
		public int squishDelay;
		public int rollingOther;
		public int beingRolled;
		public bool bloated;
		public Player frFeed;
		public bool frFed;
		public int smearTimer;
        public int miscTimer;
		public int weightless;		//soft boolean timer - ignores weight speed penalties while active
		public int eatCorn;
		public Vector2 tuchShift;
        public bool tuching;
		public int ignoreSpears;
        public int slugBed;
		public int stuckLock;
		public bool canSlugSlam;
		public int slamThreshold; //for modders to adjust what level their modcat can activate slugslams
		public bool maxFoodOverrideFlag; //for eatMeat shenanigans because I hate IL hooks
		public int tailPushedAngle; //If our tail gets squished up or down by pushers
        public PhysicalObject forceEatTarget;
		public ChunkSoundEmitter squeezeLoop;
		public patch_Player.FoodOnBack foodOnBack;
    }

    private static readonly ConditionalWeakTable<Creature, Belly> CWT = new();
    public static Belly GetBelly(this Creature crit) => CWT.GetValue(crit, _ => new());
}


public static class YourGraphicsClass
{
	public class YourGraphics
	{
		public int randCycle = 1;
		public bool staring;
		public int blSprt = 12;
		public Color blColor = Color.red;
		public int bodySprt;
		public float lastSquish;
		public float[] tailBase;
		public float checkRad = 1f; //USED TO DETECT CHANGES IN RAD
		public bool verified;
		public bool cloakRipped;
	}

	private static readonly ConditionalWeakTable<PlayerGraphics, YourGraphics> CWT = new();
	public static YourGraphics GetGraph(this PlayerGraphics player) => CWT.GetValue(player, _ => new());
}