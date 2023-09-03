using System.Security.Cryptography;
using UnityEngine;
using SprobParasiticScug;

public class patch_AbstractCreature
{
    public static void Patch()
    {
        On.AbstractCreature.IsEnteringDen += BP_IsEnteringDen;
        On.ShortcutHandler.CreatureTakeFlight += BPShortcutHandler_CreatureTakeFlight;
        //On.Room.Update +=
        //On.AbstractCreature.Update += AbstractCreature_Update;
        On.Creature.Update += Creature_Update;
        //On.AbstractCreature.IsExitingDen += AbstractCreature_IsExitingDen;
    }

    

    private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
    {
        orig(self, eu);

        if (BellyPlus.parasiticEnabled)
        {
            CheckParasiteEat(self.abstractCreature);
        }
    }

    public static void CheckParasiteEat(AbstractCreature self)
	{
		if (Main.isParasiteControlled(self) && self.vars().controller != null)
		{
			//CHECK IF OUR PARASITE IS HOLDING DOWN GRAB
			Player paras = self.vars().controller;
            

            //DEBUG TOOLS FOR PARASITE CRITTERS
            if (BPOptions.debugTools.Value && paras.input[0].jmp && !paras.input[1].jmp && paras.input[0].thrw)
			{
                Main.AddFood(self.realizedCreature, paras, 1);
                PSFoodValues(self);
            }
            
            //UPDATE OUR CREATURE'S FATNESS
            if (paras.input[0].pckp != paras.input[1].pckp)
                PSFoodValues(self);
        }
	}

    public static bool CheckForParasite(AbstractCreature self)
    {
        return Main.isParasiteControlled(self);
    }

    public static void PSFoodValues(AbstractCreature self)
    {
        if (patch_DLL.CheckFattable(self.realizedCreature) && self.vars() != null)
        {
            int bonusFat = 0;
            int foodFloor = 8 - self.vars().maxFood;

            //LOLOL LET'S ADD THE PARASITE'S FATNESS ON TOP
            if (self.vars().controller != null)
            {
                //int bonusFat = patch_Player.GetOverstuffed(self.vars().controller) / 2;
                Player paras = self.vars().controller;
                bonusFat = Mathf.Max(patch_Player.bellyStats[patch_Player.GetPlayerNum(paras)].myFoodInStomach - paras.slugcatStats.maxFood, 0) / 1; //LIZARDS ALREADY DEVIDE BY LIKE 25
            }
            BellyPlus.myFoodInStomach[BellyPlus.GetRef(self.realizedCreature)] = self.vars().food + foodFloor + bonusFat;

            if (self.vars().controller != null)
                Debug.Log("--PARASITE CREATURE EATING A TASTY SNACK! " + BellyPlus.myFoodInStomach[BellyPlus.GetRef(self.realizedCreature)]);

            //CreatureTemplate.Type.Deer
            if (self.realizedCreature is Lizard liz)
                patch_Lizard.UpdateBellySize(liz);
            else
                patch_Lizard.UpdateChubValue(self.realizedCreature);
        }
    }


    //IN THE PARASITIC MOD, CREATURES NEED TO HAVE THEIR INTERNAL FOOD VALUES UPDATED TO MATCH FATNESS
    /* //I DON'T THINK THIS WORKED VERY WELL
    private static void AbstractCreature_IsExitingDen(On.AbstractCreature.orig_IsExitingDen orig, AbstractCreature self)
    {
        orig(self);
        if (BellyPlus.parasiticEnabled && self.realizedCreature != null)
        {
            InitPSFoodValues(self); //SET OUR INITIAL FOOD VALUE BASED ON ROTUND WORLD RANDOM STARTING FATNESS
            Debug.Log("EXIT DEN!");
        }
    }

    public static void InitPSFoodValues(AbstractCreature self)
    {
        //THIS ONE IS TO SET THEIR INTERNAL FOOD BASED ON OUR FATNESS INSTEAD OF THE OTHER WAY AROUND
        if (patch_DLL.CheckFattable(self.realizedCreature) && self.vars() != null && !CheckForParasite(self))
        {
            self.vars().food = self.vars().maxFood - 4 + patch_Lizard.GetChubValue(self.realizedCreature);
            Debug.Log("STARTING CHUB! " + self.vars().food);
        }
    }
    */

    public static void BPShortcutHandler_CreatureTakeFlight(On.ShortcutHandler.orig_CreatureTakeFlight orig, ShortcutHandler self, Creature creature, AbstractRoomNode.Type type, WorldCoordinate start, WorldCoordinate dest)
    {
        try
        {
            bool isParasitic = false;
            if (BellyPlus.parasiticEnabled)
                isParasitic = CheckForParasite(creature.abstractCreature);

            if (creature is Vulture && patch_DLL.CheckFattable(creature) && !isParasitic)
            {
                if (creature.grasps[0] != null && creature.grasps[0].grabbed is Creature)  // && creature.Template.CreatureRelationship(creature.grasps[0].grabbed as Creature).type == CreatureTemplate.Relationship.Type.Eats)
                {
                    BellyPlus.myFoodInStomach[BellyPlus.GetRef(creature)] += 1;
                    patch_Lizard.ObjUpdateBellySize(creature);
                    Debug.Log("CREATURE DRAGGED PREY OFFSCREEN - EATING A TASTY SNACK! " + BellyPlus.myFoodInStomach[patch_Lizard.GetRef(creature)]);
                }

            }
        }
        catch
        {
            Debug.Log("CATCH! BP CRASH ON TRYING TO LEAVE TO OFFSCREEN DEN! ");
        }
        
		orig.Invoke(self, creature, type, start, dest);
    }
	
	

    public static void BP_IsEnteringDen(On.AbstractCreature.orig_IsEnteringDen orig, AbstractCreature self, WorldCoordinate den)
	{
		try
		{
			bool isParasitic = false;
			if (BellyPlus.parasiticEnabled)
			{
                isParasitic = CheckForParasite(self);
				//PSFoodValues(self);
            }
				

			
			if (self.realizedCreature != null && patch_DLL.CheckFattable(self.realizedCreature) && !isParasitic)
			{
				//OH, I GUESS CICADAS DO THIS TOO!
				//YEEKS DO NOT, THEY CATCH FRUIT SO THIS WON'T RUN. HANDLED IN YEEKSTATE INSTEAD
				if ((self.realizedCreature is Lizard || self.realizedCreature is Cicada || self.realizedCreature is Vulture) && self.abstractAI != null && self.abstractAI.HavePrey())
				{
					Creature mySelf = self.realizedCreature as Creature;
					//BONUS MEAT IF EATING A HEFTY PLAYER
					float fatGained = 2;
					if (mySelf.grasps[0].grabbed is Player player)
						fatGained += Mathf.Min((patch_Player.GetOverstuffed(player) / 2f), 4f);
						
					BellyPlus.myFoodInStomach[BellyPlus.GetRef(mySelf)] += Mathf.CeilToInt(fatGained);
					patch_Lizard.ObjUpdateBellySize(mySelf as Creature); //CICADAS CAN'T DO THIS... WAIT YES THEY CAN!!
					Debug.Log("CREATURE IN DEN - EATING A TASTY SNACK! " + BellyPlus.myFoodInStomach[patch_Lizard.GetRef(mySelf)]);
				}

				//MINI CREATURE UPDATES
				if (self.realizedCreature is DropBug)
				{
					Creature mySelf = self.realizedCreature as Creature;
					int amnt = (BellyPlus.myFoodInStomach[BellyPlus.GetRef(mySelf)] >= 4) ? 1 : 2; //PAST TWO MEALS, SLOW DOWN THE CHONK
					//GAIN 2 IF HUNGRY. ONLY 1 IF FAT
					BellyPlus.myFoodInStomach[BellyPlus.GetRef(mySelf)] += amnt;
					patch_DLL.UpdateBellySize(mySelf as DropBug, amnt);
					Debug.Log("MINI CREATURE IN DEN - EATING A TASTY SNACK! " + BellyPlus.myFoodInStomach[patch_Lizard.GetRef(mySelf)]);
				}
			}
        }
		catch
		{
			Debug.Log("CATCH! BP CRASH ON TRYING TO ENTER DEN! ");
		}

		orig.Invoke(self, den);
	}
}