using UnityEngine;
using System;

namespace RotundWorld;
public class patch_SaveState
{
    public static void Patch()
    {
        On.SaveState.SessionEnded += BP_SessionEnded;
        On.SaveState.ApplyCustomEndGame += BP_ApplyCustomEndGame;
    }

    

    public static void BP_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
	{
        if (BellyPlus.VisualsOnly())
		{
            orig.Invoke(self, game, survived, newMalnourished);
            return;
		}

		//CALL THE DOOR CLOSED THING BECAUSE THE GAME IS REALLY BAD AT DOING THAT
		patch_ShelterDoor.OnDoorClosed(game);
		
		self.deathPersistentSaveData.foodReplenishBonus++; //DEATH REGROWS FOOD TWICE AS FAST
		
		//REMEMBER ANY BONUS PIPS IF WE HAD STARTED THE DAY STARVING, BECAUSE THE GAME WONT!
		int tailPips = 0;
		if (self.lastMalnourished && !newMalnourished)
		{
			tailPips = self.food - SlugcatStats.SlugcatFoodMeter(self.saveStateNumber).x;
			Debug.Log("WE WERE STARVIN! APPEND EXTRRA FOOD " + tailPips);
		}
			
		
		orig.Invoke(self, game, survived, newMalnourished);
		
		try
		{
			if (survived && BellyPlus.bonusFood > 0)
			{
				self.food += BellyPlus.bonusFood + tailPips;
				self.deathPersistentSaveData.foodReplenishBonus++; //GIVE US EXTRA FOOD EVEN IF WE DIE
				game.rainWorld.progression.SaveWorldStateAndProgression(self.malnourished);
			}
		}
		catch
		{
			Debug.Log("CATCH! SAVE STATE BONUS FOOD FAILURE");
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