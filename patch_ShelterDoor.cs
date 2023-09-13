using UnityEngine;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;

public class patch_ShelterDoor
{
    public static void Patch()
    {
		// using (new DetourContext(333))
		On.ShelterDoor.DoorClosed += BP_DoorClosed;
        //On.RainWorldGame.Win += BPRainWorldGame_Win;
		
		//SLIME_CUBED REPORTED THAT THIS IS NEEDED TO WORK AROUND A BUG WHERE MOD PRIORITIES FOR OTHER MODS AREN'T RESET UNLESS YOU RESET IT MANUALLY
		// using (new DetourContext()) { }
	}

    //private static void BPRainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
    public static void OnDoorClosed(RainWorldGame self)
    {

        try
        {
            if (self.FirstAlivePlayer != null) //self.manager.upcomingProcess != null)
            {
                Room thisRoom = self.world.GetAbstractRoom(self.FirstAlivePlayer.pos).realizedRoom;
                bool shelterTrapped = false;

                try
                {
                    //BellyPlus.ClearThins();
                    Debug.Log("-DOOR CLOSED: " + thisRoom.game.Players.Count);

                    //NOTE THIS ONLY APPLIES TO PLAYERS AND NOT SLUGPUPS
                    for (int i = 0; i < thisRoom.game.Players.Count; i++)
                    {
                        Player player = (thisRoom.game.Players[i].realizedCreature as Player);
                        if (player != null)
                        {
                            if (i == 0) //WE ONLY NEED TO RUN THIS ONCE (Plus it breaks Individual Food Bars if it runs after this)
                                patch_Player.CheckBonusFood(player, true);

                            //CAN WEEEE LOWER OUR FOOD INTAKE IF WE NEVER LEFT THE SHELTER?
                            if (player.stillInStartShelter)
                                shelterTrapped = true;


                            //STORE OUR LONG TERM FOOD VALUE FOR LATER
                            int myID = thisRoom.game.Players[i].ID.number;
                            int myFood = patch_Player.bellyStats[myID].myFoodInStomach;
                            int extraFoodCount = myFood - player.MaxFoodInStomach;
                            int hibernateCost = player.slugcatStats.foodToHibernate;
                            //SPECIAL EXCEPTIONS FOR INDIVIDUAL FOOD BARS MOD
                            if (BellyPlus.individualFoodEnabled)
                            {
                                extraFoodCount = myFood - SlugcatStats.SlugcatFoodMeter((player.abstractCreature.world.game.Players[0].state as PlayerState).slugcatCharacter).x;
                                hibernateCost = SlugcatStats.SlugcatFoodMeter((player.abstractCreature.world.game.Players[0].state as PlayerState).slugcatCharacter).y;
                            }

                            Debug.Log("-PLAYER FOOD PIPS: " + myID + " - " + myFood + " MAX " + player.MaxFoodInStomach + " EXTRA " + extraFoodCount + " TO HIBERNATE " + player.slugcatStats.foodToHibernate + " BONUS " + BellyPlus.bonusFood);
                            if (extraFoodCount > 0)
                            {
                                //REMOVE INVALID HALF PIPS
                                if ((extraFoodCount % 2) == 1)
                                {
                                    extraFoodCount--;
                                    myFood--;
                                }

                                //NO WAIT. OKAY FOR EACH EXTRA POINT, JUST TAKE OFF AN EXTRA ONE TO SIMULATE THE DOUBLE PIPS
                                //hibernateCost += extraFoodCount;
                                //THAT IS NOT HOW THAT WORKS DUMMY
                                //OKAY JUST ADD OUR SUBTRACTED ...NOPE NOT THAT EITHER

                                //BURN BONUS PIPS AT TWICE THE RATE
                                for (int j = 0; j < (extraFoodCount / 2); j++)
                                {
                                    if (hibernateCost > 0)
                                    {
                                        //player.playerState.foodInStomach--;
                                        hibernateCost--;
                                        myFood -= 2;
                                        Debug.Log("BURNING A BONUS PIP: " + Math.Max(myFood, 0));
                                    }
                                }

                            }
                            //else

                            myFood -= hibernateCost; //BURN THE REST AT A 1-TO-1 RATE

                            //SET THIS VALUE SO THAT THE ACHEIVMENT TRACKERS CAN SAVE IT FOR LATER
                            patch_Player.bellyStats[myID].myFoodInStomach = Math.Max(myFood, 0);
                            Debug.Log("-SAVING MY NEW FOOD AS: " + Math.Max(myFood, 0));
                            //DO THIS SO THE GAME DOESN'T ADJUST OUR FOOD
                            BellyPlus.lockEndFood = true;
                        }
                    }
                }
                catch
                {
                    Debug.Log("CATCH! SHELTER DOOR INITIAL CHECK FAILURE");
                }


                try
                {
                    //SPECIFICLLY FOR SLUGPUP NPCS
                    for (int j = 0; j < thisRoom.abstractRoom.creatures.Count; j++)
                    {
                        if (ModManager.MSC
                            && thisRoom.abstractRoom.creatures[j].realizedCreature != null
                            && thisRoom.abstractRoom.creatures[j].creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC
                        )
                        {
                            Player player = thisRoom.abstractRoom.creatures[j].realizedCreature as Player;
                            if (player != null && player.isNPC && player.isSlugpup)
                            {
                                int extraFoodCount = (player.CurrentFood - player.MaxFoodInStomach);
                                Debug.Log("-PUP FOOD PIPS: " + player.CurrentFood + " MAX " + player.MaxFoodInStomach + " EXTRA " + extraFoodCount + " TO HIBERNATE " + player.slugcatStats.foodToHibernate);
                                if (extraFoodCount >= 0)
                                {
                                    //REMOVE ANY HALF PIPS
                                    //if ((extraFoodCount % 2) == 1)
                                    //	player.playerState.foodInStomach--;

                                    ////REMOVE THE CORRECT AMNT OF PIPS (SHOULD BE 2 (OR 3 IF STARVED))
                                    //if (extraFoodCount >= player.slugcatStats.foodToHibernate && extraFoodCount > 1)
                                    //	player.playerState.foodInStomach -= (player.slugcatStats.foodToHibernate / 2);

                                    if ((extraFoodCount % 2) == 1)
                                    {
                                        extraFoodCount--;
                                        player.playerState.foodInStomach--;
                                    }


                                    ////REPLACE OUR FAKE FOOD VALUE WITH WHAT THE ACTUAL NUMBER OF PIPS WOULD BE, R-RIGHT?
                                    //int newFoodVal = player.MaxFoodInStomach + (extraFoodCount / 2) - player.slugcatStats.foodToHibernate;
                                    //int leftoverBonusFood = Mathf.Max(0, newFoodVal - player.MaxFoodInStomach);
                                    //Debug.Log("-MORE PUP FOOD PIPS: NEWFOOD " + newFoodVal + " LEFTOVER " + leftoverBonusFood);
                                    //player.playerState.foodInStomach = newFoodVal + (leftoverBonusFood * 1) + player.slugcatStats.foodToHibernate;


                                    int hibernateCount = player.slugcatStats.foodToHibernate;
                                    for (int i = 0; i < (extraFoodCount / 2); i++)
                                    {
                                        if (hibernateCount > 0)
                                        {
                                            player.playerState.foodInStomach--;
                                            hibernateCount--;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    Debug.Log("CATCH! SHELTER DOOR PUP PIP FAILURE");
                }


                //FIND OUR LIZARDS?
                try
                {
                    for (int j = 0; j < thisRoom.abstractRoom.creatures.Count; j++)
                    {
                        if (thisRoom.abstractRoom.creatures[j].realizedCreature != null
                            && thisRoom.abstractRoom.creatures[j].realizedCreature is Lizard
                        )
                        {
                            Lizard myLiz = thisRoom.abstractRoom.creatures[j].realizedCreature as Lizard;
                            //MAKE SURE IT'S A LARGER VALUE FIRST
                            if (BellyPlus.myFoodInStomach[BellyPlus.GetRef(myLiz)] > BellyPlus.lizardFood)
                            {
                                BellyPlus.lizardFood = BellyPlus.myFoodInStomach[BellyPlus.GetRef(myLiz)];
                                Debug.Log("LZ! REMEMBERING MY LIZARDS FOOD VALUE!" + BellyPlus.lizardFood);
                            }
                        }
                    }

                    if (BellyPlus.lizardFood > 4)
                        BellyPlus.lizardFood -= 2;
                    //OKAY SOME LIZARDS GET WAY TOO FAT. LET'S HELP PEOPLE WITH THE OBESITY PROBLEM
                    if (BellyPlus.lizardFood > 8)
                        BellyPlus.lizardFood = 8 - ((BellyPlus.lizardFood - 8) / 2);
                }
                catch
                {
                    Debug.Log("CATCH! SHELTER DOOR LIZARD FATNESS FAILURE");
                }




                //7-3-23 FINDING POPCORN PLANTS IN THE SHELTER
                BellyPlus.StoredCorn = 0;
                BellyPlus.StoredStumps = 0;
                try
                {
                    Debug.Log("COUNTING THE CORN");
                    for (int j = 0; j < thisRoom.abstractRoom.entities.Count; j++)
                    {
                        if ((thisRoom.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject != null && (thisRoom.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject is SeedCob myCobb)
                        {
                            //BUT LIKE DO ALL THE STUFF FIRST
                            if (!myCobb.AbstractCob.dead)
                            {
                                if (myCobb.open >= 1)
                                    BellyPlus.StoredStumps++;
                                else if (!myCobb.AbstractCob.opened)
                                    BellyPlus.StoredCorn++;
                            }
                            Debug.Log("FOUND A CORN " + BellyPlus.StoredCorn + " - " + BellyPlus.StoredStumps);

                            //IF IT'S DEAD IT WILL JUST GET DESTROYED WITHOUT BEING SAVED
                            thisRoom.abstractRoom.entities[j].Destroy();
                            thisRoom.abstractRoom.entities.RemoveAt(j);
                            //break;
                        }
                    }
                }
                catch
                {
                    Debug.Log("CATCH! CORN CHECK FAILURE");
                }

            }
        }
        catch
        {
            Debug.Log("CATCH! GENERIC SAVE FAILURE");
        }
        
    }

    public static void BP_DoorClosed(On.ShelterDoor.orig_DoorClosed orig, ShelterDoor self)
	{
		if (BellyPlus.VisualsOnly())
		{
			orig.Invoke(self);
			return;
		}
		
		//THIS THING RUNS MULTIPLE TIMES AS THE DOOR CLOSES! RUDE... LET'S MOVE THIS UP TO WIN() INSTEAD

		orig.Invoke(self);
	}
}