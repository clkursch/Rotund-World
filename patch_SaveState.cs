using UnityEngine;
using System;
using MoreSlugcats;

namespace RotundWorld;
public class patch_SaveState
{
    public static void Patch()
    {
        On.SaveState.SessionEnded += BP_SessionEnded;
        On.SaveState.ApplyCustomEndGame += BP_ApplyCustomEndGame;
    }

    public static void OnDoorClosed(RainWorldGame self, bool survived)
    {
        //NO MORE TRYCATCH TRAINING WHEELS. SAVE THE GAME FOR REAL OR FIX IT
        if (survived) //self.manager.upcomingProcess != null)
        {
            //Debug.Log("-DOOR CLOSED: " + self.Players.Count);

            //NOTE THIS ONLY APPLIES TO PLAYERS AND NOT SLUGPUPS
            for (int i = 0; i < self.Players.Count; i++) //WOW AREN'T YOU DUMB //self.Players.Count
            {
                Player player = (self.Players[i].realizedCreature as Player);
                if (player != null)
                {
                    if (i == 0) //WE ONLY NEED TO RUN THIS ONCE (Plus it breaks Individual Food Bars if it runs after this)
                        patch_Player.CheckBonusFood(player, true);

                    //STORE OUR LONG TERM FOOD VALUE FOR LATER
                    int myID = self.Players[i].ID.number;
                    int myFood = self.Players[i].GetAbsBelly().myFoodInStomach; //patch_Player.bellyStats[myID].myFoodInStomach;
                    int extraFoodCount = myFood - player.MaxFoodInStomach;
                    int hibernateCost = player.slugcatStats.foodToHibernate;
                    //SPECIAL EXCEPTIONS FOR INDIVIDUAL FOOD BARS MOD
                    if (BellyPlus.individualFoodEnabled)
                    {
                        extraFoodCount = myFood - SlugcatStats.SlugcatFoodMeter((self.Players[0].state as PlayerState).slugcatCharacter).x;
                        hibernateCost = SlugcatStats.SlugcatFoodMeter((self.Players[0].state as PlayerState).slugcatCharacter).y;
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
                    //self.Players[i].GetAbsBelly().myFoodInStomach = Math.Max(myFood, 0);

                    BellyPlus.foodMemoryBank[player.playerState.playerNumber] = Math.Max(myFood, 0);
                    //Debug.Log("CHECKING FOOD BANK " + self.playerState.playerNumber + " FOOD:" + self.abstractCreature.GetAbsBelly().myFoodInStomach);
                    Debug.Log("-SAVING MY NEW FOOD AS: " + Math.Max(myFood, 0));
                    //DO THIS SO THE GAME DOESN'T ADJUST OUR FOOD
                    BellyPlus.lockEndFood = true;
                }
            }
			
			
			//PUTTING THIS PART BEHIND THESE SAFETY CHECKS BECAUSE IDK HOW DO DO ROOMLESS CHECKS FOR THEM
			if (self.FirstAlivePlayer != null && self.FirstAlivePlayer.pos != null)
			{
				AbstractRoom thisRoom = self.world.GetAbstractRoom(self.FirstAlivePlayer.pos);
				
				//SPECIFICLLY FOR SLUGPUP NPCS
				for (int j = 0; j < thisRoom.creatures.Count; j++)
				{
					if (ModManager.MSC
						&& thisRoom.creatures[j].realizedCreature != null
						&& thisRoom.creatures[j].creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.SlugNPC
					)
					{
						Player player = thisRoom.creatures[j].realizedCreature as Player;
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


				//FIND OUR LIZARDS?
				for (int j = 0; j < thisRoom.creatures.Count; j++)
				{
					if (thisRoom.creatures[j].creatureTemplate.TopAncestor().type == CreatureTemplate.Type.LizardTemplate)
					{
						//MAKE SURE IT'S A LARGER VALUE FIRST
						if (thisRoom.creatures[j].GetAbsBelly().myFoodInStomach > BellyPlus.lizardFood)
						{
							BellyPlus.lizardFood = thisRoom.creatures[j].GetAbsBelly().myFoodInStomach;
							//Debug.Log("LZ! REMEMBERING MY LIZARDS FOOD VALUE!" + BellyPlus.lizardFood);
						}
					}
				}

				if (BellyPlus.lizardFood > 4)
					BellyPlus.lizardFood -= 2;
				//OKAY SOME LIZARDS GET WAY TOO FAT. LET'S HELP PEOPLE WITH THE OBESITY PROBLEM
				if (BellyPlus.lizardFood > 8)
					BellyPlus.lizardFood = 8 - ((BellyPlus.lizardFood - 8) / 2);




				//7-3-23 FINDING POPCORN PLANTS IN THE SHELTER
				BellyPlus.StoredCorn = 0;
				BellyPlus.StoredStumps = 0;
				Debug.Log("COUNTING THE CORN");
				for (int j = 0; j < thisRoom.entities.Count; j++)
				{
					if ((thisRoom.entities[j] as AbstractPhysicalObject).realizedObject != null && (thisRoom.entities[j] as AbstractPhysicalObject).realizedObject is SeedCob myCobb)
					{
						//BUT LIKE DO ALL THE STUFF FIRST
						if (!myCobb.AbstractCob.dead)
						{
							if (myCobb.AbstractCob.opened)
								BellyPlus.StoredStumps++;
							else if (!myCobb.AbstractCob.opened)
								BellyPlus.StoredCorn++;
						}
						Debug.Log("FOUND A CORN " + BellyPlus.StoredCorn + " - " + BellyPlus.StoredStumps);

						//IF IT'S DEAD IT WILL JUST GET DESTROYED WITHOUT BEING SAVED
						thisRoom.entities[j].Destroy();
						thisRoom.entities.RemoveAt(j);
						//break;
					}
				}
			}
        }
    }

    public static void BP_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
	{
        if (BellyPlus.VisualsOnly())
		{
            orig.Invoke(self, game, survived, newMalnourished);
            return;
		}

		//CALL THE DOOR CLOSED THING BECAUSE THE GAME IS REALLY BAD AT DOING THAT
		OnDoorClosed(game, survived);
		
		self.deathPersistentSaveData.foodReplenishBonus++; //DEATH REGROWS FOOD TWICE AS FAST
		
		//REMEMBER ANY BONUS PIPS IF WE HAD STARTED THE DAY STARVING, BECAUSE THE GAME WONT!
		int tailPips = 0;
		if (self.lastMalnourished && !newMalnourished)
		{
			tailPips = self.food - SlugcatStats.SlugcatFoodMeter(self.saveStateNumber).x;
			Debug.Log("WE WERE STARVIN! APPEND EXTRRA FOOD " + tailPips);
		}
			
		
		orig.Invoke(self, game, survived, newMalnourished);

        if (survived && BellyPlus.bonusFood > 0)
        {
            self.food += BellyPlus.bonusFood + tailPips;
            self.deathPersistentSaveData.foodReplenishBonus++; //GIVE US EXTRA FOOD EVEN IF WE DIE
            game.rainWorld.progression.SaveWorldStateAndProgression(self.malnourished);
        }
    }


	private static void BP_ApplyCustomEndGame(On.SaveState.orig_ApplyCustomEndGame orig, SaveState self, RainWorldGame game, bool addFiveCycles)
	{
		int origFood = self.food;

		orig.Invoke(self, game, addFiveCycles);
		if (BellyPlus.VisualsOnly())
			return;

		self.food = Math.Max(self.food, origFood - SlugcatStats.SlugcatFoodMeter(self.saveStateNumber).y);
		game.rainWorld.progression.SaveWorldStateAndProgression(false);
	}
	
	
	
}