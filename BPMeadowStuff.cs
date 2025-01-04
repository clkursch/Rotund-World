using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RotundWorld;

public class BPMeadowStuff
{
    public static void Patch()
    {
        On.GameSession.ctor += GameSession_ctor;

        On.AbstractCreature.Realize += AbstractCreature_MeadowInit;
        On.Creature.Update += Creature_MeadowInputCheck;
        On.Player.Update += MeadowBreathOverride;
        On.Player.LungUpdate += Player_LungUpdate;
    }

    

    public static float lobbyDifficulty;
    public static int startThresh;
    public static float gapVariance;
    public static bool slugSlams;
    public static bool backFoodStorage;
    public static bool foodLoverPerk;


    //MEADOW MODE TRIES TO GIVE US INFINITE BREATH. DIAL IT BACK A BIT, WE NEED IT FOR THE STAMINA SYSTEM
    private static void MeadowBreathOverride(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (IsMeadowGameMode())
        {
            float origBreath = self.airInLungs;
            //Debug.Log("---INITIAL VALS: " + self.airInLungs + " - " + self.lungsExhausted);
            orig(self, eu);
            //Debug.Log("POST PLAYERUPDATE 1: " + self.airInLungs + " - " + self.lungsExhausted);
            //THIS IS THE MOST CONVOLUTED THING I'VE EVER HAD TO DO TO HIJACK SOMEONE ELSES HIJACK
            self.airInLungs = Mathf.Max(self.airInLungs >= 1f ? origBreath : self.airInLungs, 0.6f);
            self.LungUpdate(); //SINCE SETTING IT AFTER ORIG ESSENTIALLY SKIPPED IT
            //CHECK IF OUR BREATH IS GOOD NOW. WE HAD TO CUT THE PART THAT UNEXHAUSTS YOU OUT OF LUNGUPDATE...
            //Debug.Log("POST PLAYERUPDATE 2: " + self.airInLungs + " - " + self.lungsExhausted);
            if (self.airInLungs >= 1f)
                self.lungsExhausted = false;
        }
        else
        {
            orig(self, eu);
        }
    }

    //STUPID RAIN MEADOW STEALING MY LUNGS...
    private static void Player_LungUpdate(On.Player.orig_LungUpdate orig, Player self)
    {
        if (IsMeadowGameMode())
        {
            if (self.airInLungs >= 1f)
                return; //DON'T EVEN REDUCE IT
            
            //BY THIS POINT MEADOW HAS ALREADY HIJACKED THE AIRINLUNGS VALUE TO BE 1. BUT IT HASN'T TURNED OFF LUNGSEXHAUSTED YET. SO WE GOTTA CATCH IT
            bool origLungEx = self.lungsExhausted;
            //Debug.Log("PRE-LUNGUPDATE: " + self.airInLungs + " - " + self.lungsExhausted);
            orig(self);
            //Debug.Log("POST LUNGUPDATE: " + self.airInLungs + " - " + self.lungsExhausted);
            if (self.lungsExhausted == false && origLungEx == true)
                self.lungsExhausted = true;
            //WE'LL LET PLAYERUPDATE DETERMINE WHEN TO REALLY TURN THIS OFF
        }
        else
        {
            orig(self);
        }
    }

    private static void Creature_MeadowInputCheck(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);

        //if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
        //{
        //    if (mgm.avatars[0].realizedCreature != null && CreatureController.creatureControllers.TryGetValue(mgm.avatars[0].realizedCreature, out var c))
        //    {
        //        if (c.input[0].jmp)
        //            Debug.Log("JMP!!!");
        //    }
        //}

        if (BellyPlus.meadowEnabled)
            CheckMeadowInput(self);
    }

    public static void CheckMeadowInput(Creature self)
    {
        if (self.IsLocal() && OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
        {
            if (mgm.avatars[0].realizedCreature != null && CreatureController.creatureControllers.TryGetValue(mgm.avatars[0].realizedCreature, out var c))
            {
                if (c.input[0].jmp && !c.input[1].jmp)
                {
                    mgm.avatars[0].realizedCreature.GetBelly().manualBoost = true; //SET IT LOCAL - I GUESS THIS SHOULD ONLY BE FOR NON-SLUGCATS BUT IT WON'T BREAK ANYTHING IF IT DOES
                                                                                   //GET OUR ONLINEPHYSICALOBJECT
                    if (!OnlinePhysicalObject.map.TryGetValue(mgm.avatars[0].realizedCreature.abstractPhysicalObject, out var onlineEntity))
                        throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
                    //RPC EVERYONE ELSE
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(typeof(RotundRPCs).GetMethod("ManualStruggle").CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineEntity);
                        }
                    }
                }
            }
        }
    }

    //RELIABLY RUNS FOR EVERY CREATURE TYPE FOR ALL RAIN MEADOW GAMEMODES
    private static void AbstractCreature_MeadowInit(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
    {
        orig(self);
        if (BellyPlus.isMeadowSession)
            GetMeadowWeight();
    }


    private static void GameSession_ctor(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig(self, game);

        if (BellyPlus.meadowEnabled)
            MeadowGameSession(orig, self, game);
    }


    public static void SyncRemixOptions()
    {
        //Debug.Log("SYNC REMIX OPTIONS");
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
            BellyPlus.isMeadowClient = false;
            BellyPlus.isMeadowSession = false;
            SyncRemixOptions();
            orig(self, game);
            return; //THIS IS A NON ONLINE GAME. SKIP ALL THIS
        }
        else
        {
            Debug.Log("MEADOW SESSION = TRUE!");
            BellyPlus.isMeadowSession = true;
            if (!OnlineManager.lobby.isOwner)
            {
                BellyPlus.isMeadowClient = true;
                //Debug.Log("MEADOW LOBBY CLIENT! WE ARE NOT THE OWNER");

                //SEND AN RPC TO THE HOST REQUESTING THAT IT SYNCREMIX WITH EVERYONE
                //Debug.Log("REQUESTING A REMIX SYNC FROM THE HOST!!!");
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


    //OKAY SO WE CAN ACTUALLY SET OUR myFoodInStomach VALUE AS EARLY AS WE WANT BECAUSE THE LIZARD CONSTRUCTOR WILL NOT ROLL THE STARTING VALUE IF ABSTRACTCREATURE ALREADY HAS myFoodInStomach DEFINED 

    public static void GetMeadowWeight()
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
        {
            //Debug.Log("IN A MEADOW GAMEMODE!");
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) 
                    continue; // not in game

                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.owner.isMe && opo.apo is AbstractCreature ac)
                {
                    //Debug.Log("THIS IS MYSELF IN A MEADOW LOBBY!");
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


    public static void BroadcastMeadowBellySize(Player self)
    {
        if (self.isNPC && self.isSlugpup)
            return;

        //MAP THE OPO, IF IT EXISTS
        if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var opo))
            return; //throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");

        //IF THIS IS MY BELLY, TELL EVERYONE ELSE TO UPDATE THEIR VALUE OF MY BELLY
        if (opo.isMine)
        {
            int food = self.abstractCreature.GetAbsBelly().myFoodInStomach;
            foreach (var player in OnlineManager.players)
            {
                if (!player.isMe)
                {
                    player.InvokeRPC(typeof(RotundRPCs).GetMethod("InitializeWeight").CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject, int>)), opo, food);
                }
            }
        }
    }

    public static bool IsMeadowGameMode()
    {
        if (BellyPlus.meadowEnabled)
            return CheckMGM();
        else
            return false;
    }

    public static bool CheckMGM()
    {
        return (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode);
    }

}
