using RainMeadow;
using System;
using UnityEngine;
using static TheFriend.FriendThings.FriendCWT;

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
            //Debug.Log("MEADOW!! INITIALIZE WEIGHT " + food); //DISABLING BECAUSE THIS IS GETTING SPAMMED
            
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
		
		
		[RainMeadow.RPCMethod]
        public static void StartFeeding(RPCEvent rpcEvent, OnlinePhysicalObject feederOpo, OnlinePhysicalObject feedeeOpo, OnlinePhysicalObject foodOpo, int grsp)
        {
            Debug.Log("----START FEEDING RPC");
            
			if (feederOpo.apo.realizedObject is Player feeder && feedeeOpo.apo.realizedObject is Player feedee && foodOpo.apo.realizedObject is PhysicalObject food)
            {
                Debug.Log("----FLAG 1");
                if (feeder == null || feedee == null || food == null)
					return; //ITS GONE
                Debug.Log("----FLAG 2");
                //feeder.wantToPickUp = 0; //WE DON'T NEED THESE FOR MEADOW
				//feeder.dontGrabStuff = 15; 
                feeder.GetBelly().frFeed = feedee;
				feedee.GetBelly().frFed = true;
                //GUESS WE GOTTA RELEASE IT FIRST
                //feeder.ReleaseGrasp(grsp); //I DON'T THINK WE CAN EXPECT THE HOST CLIENT INTERACTION TO HANDLE THIS BEFORE WE NEED IT...
                //WE MIGHT HAVE TO TRY

                Debug.Log("----FLAG 3");
                bool eu = false; //I GUESS?????
                food.firstChunk.MoveFromOutsideMyUpdate(eu, feedee.bodyChunks[0].pos);
				food.firstChunk.vel *= 0f;
                Debug.Log("----FLAG 4");

                //FORCE THEM TO GRAB OUR OBJECT 
                if (feedee.IsLocal()) //OKAY BUT ONLY WE CAN DO IT
                {
                    feedee.SlugcatGrab(food, Math.Abs(feedee.FreeHand()));
                }
				feedee.room.PlaySound(SoundID.Slugcat_Switch_Hands_Complete, feedee.mainBodyChunk, false, 1.3f, 1f);
                feedee.room.PlaySound(SoundID.Slugcat_Down_On_Fours, feedee.mainBodyChunk, false, 1.4f, 1f);
                Debug.Log("----FLAG 5");
            }
        }
		
		
		[RainMeadow.RPCMethod]
        public static void EndFeeding(RPCEvent rpcEvent, OnlinePhysicalObject feederOpo, OnlinePhysicalObject feedeeOpo)
        {
            Debug.Log("----END FEEDING RPC");
            
			if (feederOpo.apo.realizedObject is Player feeder && feedeeOpo.apo?.realizedObject is Player feedee)
            {
                if (feeder != null)
                    feeder.GetBelly().frFeed = null;

                //THIS ONE CAN BE NULL SOMETIMES, IF CALLED BY THE FEEDEE BEING UNABLE TO EAT MORE
                if (feedee != null)
                    feedee.GetBelly().frFed = false;
            }
        }
		
    }
}