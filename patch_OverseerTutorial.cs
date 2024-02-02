using OverseerHolograms;

namespace RotundWorld;
public static class patch_OverseerTutorial
{
	public static void Patch()
	{
		
		On.OverseerTutorialBehavior.Update += OverseerTutorialBehavior_Update;
		On.OverseerTutorialBehavior.ctor += OverseerTutorialBehavior_ctor;
	}
	
	public static int stuckTrouble;
	public static int wedgeTrouble;
	public static bool weightTip;
	public static bool staminaTip;
	public static int lastBonusPips;


	private static void OverseerTutorialBehavior_ctor(On.OverseerTutorialBehavior.orig_ctor orig, OverseerTutorialBehavior self, OverseerAI AI)
    {
		orig.Invoke(self, AI);
		stuckTrouble = 0;
		wedgeTrouble = 0;
		weightTip = true; //false; //MEH, THIS ONE IS NOT NEEDED. AND TENDS TO BREAK THE GATES
		staminaTip = false;
	}


	public static void OverseerTutorialBehavior_Update(On.OverseerTutorialBehavior.orig_Update orig, OverseerTutorialBehavior self)
	{
		//REPLICATING 
		if (self.room == null)
			return;
		if (self.room.game.Players[0].realizedCreature == null)
			return;
		if (self.room.game.Players[0].realizedCreature.room != self.room)
			return;
		
		
		
		if (self.player.FoodInStomach >= self.player.slugcatStats.foodToHibernate && self.lastFoodInStomach < self.player.slugcatStats.foodToHibernate)
		{
			//self.TutorialText("Three is enough to hibernate", 10, 120, false);
			//THIS IS A REAL ONE
		}
		else if (self.player.FoodInStomach > self.player.slugcatStats.foodToHibernate && self.lastFoodInStomach <= self.player.slugcatStats.foodToHibernate)
		{
			
			//self.TutorialText("Additional food (above three) is kept for later", 10, 120, false);
		}
		
		//else if (self.player.FoodInStomach == self.player.slugcatStats.maxFood && self.lastFoodInStomach < self.player.slugcatStats.maxFood)
		//{
		//	// self.TutorialText("You are full", 10, 120, false);
		//	self.lastFoodInStomach = self.player.FoodInStomach; //UPDATE THIS SO THE REAL ONE DOESN'T GO OFF
		//}
		
		//EXTRA ONES
		else if (self.player.FoodInStomach > self.player.slugcatStats.foodToHibernate && (self.player.FoodInStomach > self.lastFoodInStomach || BellyPlus.bonusHudPip > lastBonusPips))
        {
			if (self.player.FoodInStomach == self.player.slugcatStats.maxFood && BellyPlus.bonusHudPip == 0) // && BellyPlus.bonusHudPip == 0 && self.player.MaxFoodInStomach > self.player.slugcatStats.maxFood)
			{
				self.TutorialText("Your hunger is satisfied, but you could still eat more...", 10, 120, false);
			}
			
			//THIS ISN'T A THING ANYMORE!
			// else if (self.player.FoodInStomach == self.player.MaxFoodInStomach)
				// self.TutorialText("You are stuffed", 10, 120, false);

			else if (weightTip == false)
			{
				self.TutorialText("Continuing to eat may make it difficult to fit through some gaps", 10, 150, false);
				weightTip = true;
			}

			self.lastFoodInStomach = self.player.FoodInStomach; //AND UPDATE THIS TOO
			lastBonusPips = BellyPlus.bonusHudPip;
		}

		//EH.. THESE END UP JUST BUGGING OUT AT KARMA GATE TUTORIALS MOST OF THE TIME.
		/*
		if (patch_Player.IsStuck(self.player) && stuckTrouble < 800)
			stuckTrouble++;
		else if (stuckTrouble > 0 && stuckTrouble < 90)
			stuckTrouble--;

		if (stuckTrouble == 90)
			self.TutorialText("You're stuck. This can happen when you eat too much. Press Jump to try and force yourself through", 10, 200, false);


		if (patch_Player.ObjIsWedged(self.player) && wedgeTrouble < 800)
			wedgeTrouble++;
		else if (wedgeTrouble > 0 && wedgeTrouble < 90)
			wedgeTrouble--;

		//MEH...
		//if (wedgeTrouble == 90)
		//	self.TutorialText("Stuffing your belly can make travel much more difficult.", 10, 120, false);
		
		
		if (staminaTip == false && (wedgeTrouble > 130 || stuckTrouble > 130) && self.player.lungsExhausted)
		{
			self.TutorialText("Struggling too rapidly can slow down your progress by making you exhausted.", 10, 150, false);
			staminaTip = true;
			BellyPlus.struggleHintGiven = true;
		}
		
		//AND THIS ONE NEVER WORKED
		if (patch_Player.IsStuckOrWedged(self.player) && (wedgeTrouble >= 800 || stuckTrouble >= 800))
        {
			self.overseer.TryAddHologram(OverseerHologram.Message.GetUpOnFirstBox, self.player, float.MaxValue);
		}
		*/

		orig.Invoke(self);
	}

}

