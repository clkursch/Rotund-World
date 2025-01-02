using RainMeadow;
using System.Linq;
using UnityEngine;

namespace RotundWorld
{
    public static class RotundRPCs
    {
        /*
        [RainMeadow.RPCMethod]
        public static void Arena_IncrementPlayerScore(RPCEvent rpcEvent, int score, ushort userWhoScored)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                if (!game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers) // team work makes the dream work
                {
                    DrownMode.currentPoints++;
                }
                var oe = ArenaHelpers.FindOnlinePlayerByLobbyId(userWhoScored);
                var playerWhoScored = ArenaHelpers.FindOnlinePlayerNumber(arena, oe);
                game.GetArenaGameSession.arenaSitting.players[playerWhoScored].score = score;
            }
        }

        [RainMeadow.RPCMethod]
        public static void Arena_OpenDen(RPCEvent rpcEvent, bool denOpen)
        {

           DrownMode.openedDen = denOpen;

        }
        */

        [RainMeadow.RPCMethod]
        public static void SyncRemix2(RPCEvent rpcEvent, float bpDifficulty, int startThresh, float gapVariance) //, bool slugSlams, bool backFoodStorage, bool foodLoverPerk)
        {
            //RainMeadow.RainMeadow.Debug("Recieved Remix values: " + bpDifficulty +  startThresh + gapVariance + slugSlams + backFoodStorage + foodLoverPerk);
            Debug.Log("MEADOW!! Recieved Remix values: " + bpDifficulty); // + startThresh + gapVariance + slugSlams + backFoodStorage + foodLoverPerk);

            //BPOptions.bpDifficulty = bpDifficulty; //THIS PROBABLY WON'T WORK LIKE THIS BUT WE CAN TRY...
            BellyPlus.lobbyDifficulty = bpDifficulty;
            BellyPlus.startThresh = startThresh;
            BellyPlus.gapVariance = gapVariance;
            //BellyPlus.slugSlams = slugSlams;
            //BellyPlus.backFoodStorage = backFoodStorage;
            //BellyPlus.foodLoverPerk = foodLoverPerk;
            // THERE WILL BE MORE BUT THIS IS OKAY FOR NOW...
        }



        [RainMeadow.RPCMethod]
        public static void SyncRemix(RPCEvent rpcEvent, float bpDifficulty, int startThresh, float gapVariance)
        {
            Debug.Log("MEADOW!! Recieved Remix values: " + bpDifficulty); 
            BellyPlus.lobbyDifficulty = bpDifficulty;
            BellyPlus.startThresh = startThresh;
            BellyPlus.gapVariance = gapVariance;
        }



        [RainMeadow.RPCMethod]
        public static void MeadowPopFree(RPCEvent rpcEvent, OnlinePhysicalObject onlinePlayer, float power, bool inPipe)
        {
            //Debug.Log("MEADOW!! POP FREE RPC " + power); // + startThresh + gapVariance + slugSlams + backFoodStorage + foodLoverPerk);

            Player player = null;
            //CONVERT ONLINEPHYSICALOBJECT TO PLAYER - HELPFUL CODE PROVIDED BY UO! THNX :3
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.owner == onlinePlayer.owner && opo.apo is AbstractCreature ac && ac.realizedCreature is not null)
                {
                    player = (ac.realizedCreature as Player); // now we have the player who did the thing.
                }
            }

            patch_Player.PopFree(player, power, inPipe);
        }



        [RainMeadow.RPCMethod]
        public static void InitializeWeight(RPCEvent rpcEvent, OnlinePhysicalObject onlinePlayer, int food)
        {
            Debug.Log("MEADOW!! INITIALIZE WEIGHT " + food);

            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.owner == onlinePlayer.owner && opo.apo is AbstractCreature ac && ac.realizedCreature is not null)
                {
                    ac.GetAbsBelly().myFoodInStomach = food;
                    if (ac.realizedCreature is Player)
                        patch_Player.UpdateBellySize(ac.realizedCreature as Player);
                    else
                        patch_MiscCreatures.ObjUpdateBellySize(ac.realizedCreature);
                }
            }
        }
    }
}