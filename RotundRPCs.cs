using RainMeadow;
using System;
using UnityEngine;

namespace RotundWorld
{
    public static class RotundRPCs
    {

        [RainMeadow.RPCMethod]
        public static void SyncRemix(RPCEvent rpcEvent, float bpDifficulty, int startThresh, float gapVariance, bool slugSlams, bool backFoodStorage, bool foodLoverPerk)
        {
            Debug.Log("MEADOW!! Recieved Remix values: " + bpDifficulty); 
            BPMeadowStuff.lobbyDifficulty = bpDifficulty;
            BPMeadowStuff.startThresh = startThresh;
            BPMeadowStuff.gapVariance = gapVariance;
            BPMeadowStuff.slugSlams = slugSlams;
            BPMeadowStuff.backFoodStorage = backFoodStorage;
            BPMeadowStuff.foodLoverPerk = foodLoverPerk;
            // THERE WILL BE MORE BUT THIS IS OKAY FOR NOW...
        }


        [RainMeadow.RPCMethod]
        public static void RequestRemixSync(RPCEvent rpcEvent)
        {
            if (OnlineManager.lobby.isOwner)
            {
                Debug.Log("--REQUEST FOR REMIX SYNC RECEIVED! SENDING OUT RPCS");
                foreach (var player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeRPC(typeof(RotundRPCs).GetMethod("SyncRemix").CreateDelegate(typeof(Action<RPCEvent, float, int, float, bool, bool, bool>)), BPMeadowStuff.lobbyDifficulty, BPMeadowStuff.startThresh, BPMeadowStuff.gapVariance, BPMeadowStuff.slugSlams, BPMeadowStuff.backFoodStorage, BPMeadowStuff.foodLoverPerk);
                    }
                }
            }
        }



        [RainMeadow.RPCMethod]
        public static void MeadowPopFree(RPCEvent rpcEvent, OnlinePhysicalObject opo, float power, bool inPipe)
        {
            //Debug.Log("MEADOW!! POP FREE RPC " + power); // + startThresh + gapVariance + slugSlams + backFoodStorage + foodLoverPerk);
            //CONVERT ONLINEPHYSICALOBJECT TO PLAYER - HELPFUL CODE PROVIDED BY UO! THNX :3
            if (opo.apo is AbstractCreature ac && ac.realizedCreature is not null)
            {
                if (ac.realizedCreature is Player)
                    patch_Player.PopFree((ac.realizedCreature as Player), power, inPipe);
                else
                    patch_Lizard.PopFree(ac.realizedCreature, power, inPipe);
            }
        }



        [RainMeadow.RPCMethod]
        public static void InitializeWeight(RPCEvent rpcEvent, OnlinePhysicalObject opo, int food)
        {
            Debug.Log("MEADOW!! INITIALIZE WEIGHT " + food);
            
			if (opo.apo is AbstractCreature ac)
            {
                ac.GetAbsBelly().myFoodInStomach = food;
				if (ac.realizedCreature is not null)
				{
					if (ac.realizedCreature is Player)
						patch_Player.UpdateBellySize(ac.realizedCreature as Player);
					else
						patch_MiscCreatures.ObjUpdateBellySize(ac.realizedCreature);
				}
            }
        }
		
		
		[RainMeadow.RPCMethod]
        public static void ManualStruggle(RPCEvent rpcEvent, OnlinePhysicalObject opo)
        {
			if (opo.apo is AbstractCreature ac && ac.realizedCreature is not null)
            {
                ac.realizedCreature.GetBelly().manualBoost = true;
            }
        }
    }
}