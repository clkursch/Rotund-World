using BepInEx;
using UnityEngine;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;


public class BPOptions : OptionInterface
{


    public BPOptions()
    {
        //BPOptions.hardMode = this.config.Bind<bool>("hardMode", true, new ConfigurableInfo("(This is just info I guess)", null, "", new object[] { "something idk" }));

        BPOptions.hardMode = this.config.Bind<bool>("hardMode", false);
        BPOptions.holdShelterDoor = this.config.Bind<bool>("holdShelterDoor", false);
        BPOptions.backFoodStorage = this.config.Bind<bool>("backFoodStorage", false);
        BPOptions.easilyWinded = this.config.Bind<bool>("easilyWinded", false);
        BPOptions.extraTime = this.config.Bind<bool>("extraTime", true);
        BPOptions.hudHints = this.config.Bind<bool>("hudHints", true);
        BPOptions.fatArmor = this.config.Bind<bool>("fatArmor", true);
        BPOptions.slugSlams = this.config.Bind<bool>("slugSlams", false);
        //BPOptions.dietNeedles = this.config.Bind<bool>("dietNeedles", false);
        BPOptions.detachNeedles = this.config.Bind<bool>("detachNeedles", false);
		BPOptions.detachablePopcorn = this.config.Bind<bool>("detachablePopcorn", true);
		BPOptions.foodLoverPerk = this.config.Bind<bool>("foodLoverPerk", false);
        BPOptions.visualsOnly = this.config.Bind<bool>("visualsOnly", false);

        BPOptions.debugTools = this.config.Bind<bool>("debugTools", false);
        BPOptions.debugLogs = this.config.Bind<bool>("debugLogs", false);
        BPOptions.blushEnabled = this.config.Bind<bool>("blushEnabled", false);
        BPOptions.bpDifficulty = this.config.Bind<float>("bpDifficulty", 0f, new ConfigAcceptableRange<float>(-5f, 5f));
        BPOptions.sfxVol = this.config.Bind<float>("sfxVol", 0.1f, new ConfigAcceptableRange<float>(0f, 0.4f));
        BPOptions.startThresh = this.config.Bind<int>("startThresh", 2, new ConfigAcceptableRange<int>(-4, 8));//(0, 4)
		BPOptions.gapVariance = this.config.Bind<float>("gapVariance", 1.0f, new ConfigAcceptableRange<float>(0.5f, 1.75f));
        BPOptions.jokeContent1 = this.config.Bind<bool>("jokeContent1", true);
		
		BPOptions.fatP1 = this.config.Bind<bool>("fatP1", true);
		BPOptions.fatP2 = this.config.Bind<bool>("fatP2", true);
		BPOptions.fatP3 = this.config.Bind<bool>("fatP3", true);
		BPOptions.fatP4 = this.config.Bind<bool>("fatP4", true);
		BPOptions.fatLiz = this.config.Bind<bool>("fatLiz", true);
		BPOptions.fatMice = this.config.Bind<bool>("fatMice", true);
		BPOptions.fatScavs = this.config.Bind<bool>("fatScavs", true);
		BPOptions.fatSquids = this.config.Bind<bool>("fatSquids", true);
		BPOptions.fatNoots = this.config.Bind<bool>("fatNoots", true);
		BPOptions.fatCentis = this.config.Bind<bool>("fatCentis", true);
		BPOptions.fatDll = this.config.Bind<bool>("fatDll", true);
		BPOptions.fatVults = this.config.Bind<bool>("fatVults", true);
		BPOptions.fatMiros = this.config.Bind<bool>("fatMiros", true);
		BPOptions.fatWigs = this.config.Bind<bool>("fatWigs", true);
		BPOptions.fatEels = this.config.Bind<bool>("fatEels", true);
        BPOptions.fatPups = this.config.Bind<bool>("fatPups", true);

    }



    public static Configurable<bool> blushEnabled;
    public static Configurable<bool> debugTools;
    public static Configurable<bool> holdShelterDoor;
    public static Configurable<bool> backFoodStorage;
    public static Configurable<bool> hardMode;
    public static Configurable<bool> easilyWinded;
    public static Configurable<bool> extraTime;
    public static Configurable<bool> hudHints;
    public static Configurable<bool> fatArmor;
    public static Configurable<bool> slugSlams;
    public static Configurable<bool> debugLogs;
    public static Configurable<float> bpDifficulty;
    public static Configurable<float> sfxVol;
    public static Configurable<int> startThresh;
	public static Configurable<float> gapVariance;
    public static Configurable<bool> detachNeedles;
    public static Configurable<bool> visualsOnly;
    public static Configurable<bool> jokeContent1;
	public static Configurable<bool> detachablePopcorn;
	public static Configurable<bool> foodLoverPerk;
	
	public static Configurable<bool> fatP1;
	public static Configurable<bool> fatP2;
	public static Configurable<bool> fatP3;
	public static Configurable<bool> fatP4;
	public static Configurable<bool> fatLiz;
	public static Configurable<bool> fatMice;
	public static Configurable<bool> fatScavs;
	public static Configurable<bool> fatSquids;
	public static Configurable<bool> fatNoots;
	public static Configurable<bool> fatCentis;
	public static Configurable<bool> fatDll;
	public static Configurable<bool> fatVults;
	public static Configurable<bool> fatMiros;
	public static Configurable<bool> fatWigs;
	public static Configurable<bool> fatEels;
    public static Configurable<bool> fatPups;
    //Lizard
    //Lantern Mice
    //Scavengers

    //Squidcada
    //Noodle Flies
    //Centipedes

    //DLL
    //Vultures
    //Miros Birds

    //Dropwigs
    //Leviathan


    public override void Update()
    {
        base.Update();

        //this.Tabs[0].items
        
        if (this.chkBoxVisOnly != null)
        {
            if (this.chkBoxVisOnly.GetValueBool() == true)
            {
                this.diffSlide.greyedOut = true;
                this.opLab1.Hidden = true;
                this.opLab2.Hidden = true;
                for (int i = 0; i < myBoxes.Length; i++)
                {
                    if (myBoxes[i] != null)
                        myBoxes[i].greyedOut = true;
                }
            }
            else
            {
                this.diffSlide.greyedOut = false;
                this.opLab1.Hidden = false;
                this.opLab2.Hidden = false;
                for (int i = 0; i < myBoxes.Length; i++)
                {
                    if (myBoxes[i] != null)
                        myBoxes[i].greyedOut = false;
                }
            }
        }
        
    }

    public static string BPTranslate(string t)
    {
        return OptionInterface.Translate(t); //this.manager.rainWorld.inGameTranslator.BPTranslate(t);
    }


    public OpFloatSlider diffSlide;
    public OpCheckBox chkBoxVisOnly;

    public OpCheckBox chkBoxslugSlams;
    public OpCheckBox chkBoxNeedles;

    public OpLabel opLab1;
    public OpLabel opLab2;

    public static OpCheckBox[] myBoxes;


    public override void Initialize()
    {
        base.Initialize();

        // OpTab opTab = new OpTab(this, "Options");
        this.Tabs = new OpTab[]
        {
            //opTab
			new OpTab(this, BPTranslate("Options")),
            new OpTab(this, BPTranslate("Misc")),
			new OpTab(this, BPTranslate("Creatures")),
            new OpTab(this, BPTranslate("Info"))
        };



        //Tabs = new OpTab[1];
        //Tabs[0] = new OpTab("Options");

        float lineCount = 530;

        Tabs[0].AddItems(new OpLabel(175f, lineCount + 50, BPTranslate("Hover over a setting to read more info about it")));


        //OpLabel opLabel = new OpLabel(new Vector2(100f, opRect.size.y - 25f), new Vector2(30f, 25f), ":(", FLabelAlignment.Left, true, null)
        //OpCheckBox opCheckBox = new OpCheckBox(this.config as Configurable<bool>, posX, posY)
        //this.numberPlayersSlider = new OpSliderTick(menu.oi.config.Bind<int>("_cosmetic", Custom.rainWorld.options.JollyPlayerCount, new ConfigAcceptableRange<int>(1, 4)), this.playerSelector[0].pos + new Vector2((float)num / 2f, 130f), (int)(this.playerSelector[3].pos - this.playerSelector[0].pos).x, false);
        // Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, "Makes squeezing through pipes even more difficult for fatter creatures") { description = "This is My Text" });

        //Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, "Makes squeezing through pipes even more difficult for fatter creatures") { description = "This is My Text" });
        // Tabs[0].AddItems(new OpSlider(BPOptions.bpDifficulty, new Vector2(50f, lineCount), 50, false));
        this.diffSlide = new OpFloatSlider(BPOptions.bpDifficulty, new Vector2(55f, lineCount - 0), 250, 0, false);
        Tabs[0].AddItems(this.diffSlide, new OpLabel(50f, lineCount - 15, BPTranslate("Pipe Size Difficulty")) { bumpBehav = this.diffSlide.bumpBehav, description = BPTranslate("Sets the average difficulty for squeezing through pipes, and the impact weight has on your agility") });

        Tabs[0].AddItems(this.opLab1 = new OpLabel(15f, lineCount + 5, BPTranslate("Wide")) { description = BPTranslate("Easy") });
        Tabs[0].AddItems(this.opLab2 = new OpLabel(320f, lineCount + 5, BPTranslate("Snug")) { description = BPTranslate("Hard") });

        //OpCheckBox chkBox5 = new OpCheckBox(BPOptions.hardMode, new Vector2(15f, lineCount));
        //Tabs[0].AddItems(chkBox5, new OpLabel(45f, lineCount, "Snug Pipes") { bumpBehav = chkBox5.bumpBehav });



        /*
		OpFloatSlider agilSlide = new OpFloatSlider(BPOptions.agilityDiff, new Vector2(55f, lineCount - 0), 250, 0, false);
		Tabs[0].AddItems(agilSlide, new OpLabel(50f, lineCount - 15, BPTranslate("Agility Penalty")) { bumpBehav = agilSlide.bumpBehav, description = BPTranslate("Your weight has a more noticeable impact on your ability to run, climb and jump") });

        Tabs[0].AddItems(new OpLabel(15f, lineCount + 5, BPTranslate("Easy")) );
        Tabs[0].AddItems(new OpLabel(320f, lineCount +5, BPTranslate("Hard")) );
		*/


        string dscVisuals = BPTranslate("Removes all gameplay changes except visual ones");
        this.chkBoxVisOnly = new OpCheckBox(BPOptions.visualsOnly, new Vector2(15f + 425, lineCount));
        Tabs[0].AddItems(this.chkBoxVisOnly, new OpLabel(45f + 425, lineCount, BPTranslate("Visuals only")) { bumpBehav = this.chkBoxVisOnly.bumpBehav, description = dscVisuals });
        this.chkBoxVisOnly.description = dscVisuals;
        //chkBoxVisOnly.Hidden = true;



        float indenting = 250f;

        lineCount -= 70;
        OpSlider threshSlide = new OpSlider(BPOptions.startThresh, new Vector2(55f, lineCount - 0), 150, false);
        string dscThresh = BPTranslate("Offset how much food you need to eat to get fat. Lower values will make you fat earlier.");
        // Tabs[0].AddItems(threshSlide, new OpLabel(50f, lineCount - 15, BPTranslate("Starting threshold")) { bumpBehav = threshSlide.bumpBehav, description = BPTranslate("Sets how close to full you must be before eating food will add weight. Lower values will make you fat earlier.") });
        Tabs[0].AddItems(threshSlide, new OpLabel(50f, lineCount - 15, BPTranslate("Starting threshold")) { bumpBehav = threshSlide.bumpBehav, description = dscThresh });
        Tabs[0].AddItems(new OpLabel(15f, lineCount + 5, BPTranslate("Early")) { description = BPTranslate("You will start getting fat before your belly is full.") });
        Tabs[0].AddItems(new OpLabel(220f, lineCount + 5, BPTranslate("Late")) { description = BPTranslate("You won't start getting fat until your belly is full") });
        threshSlide.description = dscThresh;


        OpFloatSlider varianceSlide = new OpFloatSlider(BPOptions.gapVariance, new Vector2(350f, lineCount - 0), 150, 1, false);
        dscThresh = BPTranslate("Determines how wide the range of gap sizes can be. Wider variety makes easy gaps easier and harder gaps harder");
        Tabs[0].AddItems(varianceSlide, new OpLabel(varianceSlide.pos.x + 25f, lineCount - 15, BPTranslate("Pipe size variety")) { bumpBehav = varianceSlide.bumpBehav, description = dscThresh });
        Tabs[0].AddItems(new OpLabel(varianceSlide.pos.x - 45f, lineCount + 5, BPTranslate("Similar")) { description = BPTranslate("Gap sizes will be similar to each other") });
        Tabs[0].AddItems(new OpLabel(varianceSlide.pos.x + 160f, lineCount + 5, BPTranslate("Diverse")) { description = BPTranslate("Gap sizes will vary widely") });
        varianceSlide.description = dscThresh;


        


        //Pipes are less snug and easier to wiggle through, even when very fat
        //Makes squeezing through pipes even more difficult for fatter creatures
        //Snug Pipes
        lineCount -= 60;
        string dsc5 = BPTranslate("Outgrowing pipes is more punishing on how long it takes to wiggle through");
        //Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, BPTranslate("Outgrowing pipes is more punishing on how long it takes to wiggle through")) );
        OpCheckBox chkBox5 = new OpCheckBox(BPOptions.hardMode, new Vector2(15f, lineCount));
        Tabs[0].AddItems(chkBox5, new OpLabel(45f, lineCount, BPTranslate("Unforgiving Gap Sizes")) { bumpBehav = chkBox5.bumpBehav, description = dsc5 });
        chkBox5.description = dsc5;


        string dscArmor = BPTranslate("Increase resistance to bites based on how fat you are");
        OpCheckBox chkBoxArmor = new OpCheckBox(BPOptions.fatArmor, new Vector2(15f + indenting, lineCount));
        Tabs[0].AddItems(chkBoxArmor, new OpLabel(45f + indenting, lineCount, BPTranslate("Fat Armor")) { bumpBehav = chkBoxArmor.bumpBehav, description = dscArmor });
        chkBoxArmor.description = dscArmor;





        lineCount -= 40;
        string dsc6 = BPTranslate("Your weight has a more noticeable impact on your ability to run, climb and jump");
        //Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, BPTranslate("Your weight has a more noticeable impact on your ability to run, climb and jump")) );
        OpCheckBox chkBox6 = new OpCheckBox(BPOptions.easilyWinded, new Vector2(15f, lineCount));
        Tabs[0].AddItems(chkBox6, new OpLabel(45f, lineCount, BPTranslate("Easily Winded")) { bumpBehav = chkBox6.bumpBehav, description = dsc6 });
        chkBox6.description = dsc6;
		
		OpCheckBox mpBox1;
        string dscCorn = BPTranslate("Popcorn plants can be torn from their stems");
		mpBox1 = new OpCheckBox(BPOptions.detachablePopcorn, new Vector2(15f + indenting, lineCount));
		Tabs[0].AddItems(mpBox1, new OpLabel(45f + indenting, lineCount, BPTranslate("Detachable Popcorn Plants")) { bumpBehav = mpBox1.bumpBehav, description = dscCorn });
		mpBox1.description = dscCorn;





        lineCount -= 40;
        string dsc4 = BPTranslate("Double-tap the Grab button to store an edible item on your back like a spear");
        //Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, BPTranslate("Double-tap the Grab button to store an edible item on your back like a spear")) );
        OpCheckBox chkBox4 = new OpCheckBox(BPOptions.backFoodStorage, new Vector2(15f, lineCount));
        Tabs[0].AddItems(chkBox4, new OpLabel(45f, lineCount, BPTranslate("Back Food Storage")) { bumpBehav = chkBox4.bumpBehav, description = dsc4 });
        chkBox4.description = dsc4;



        OpCheckBox mpBox2;
        string dscFood = BPTranslate("Allows the player to eat all food types for their full value");
		mpBox2 = new OpCheckBox(BPOptions.foodLoverPerk, new Vector2(15f + indenting, lineCount));
		Tabs[0].AddItems(mpBox2, new OpLabel(45f + indenting, lineCount, BPTranslate("Food Lover")) { bumpBehav = mpBox2.bumpBehav, description = dscFood });
		mpBox2.description = dscFood;


        lineCount -= 40;
        string dsc7 = BPTranslate("Slightly increase the cycle timer to account for the slowdowns");
        //Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, BPTranslate("Slightly increase the cycle timer to account for the slowdowns")) );
        OpCheckBox chkBox7 = new OpCheckBox(BPOptions.extraTime, new Vector2(15f, lineCount));
        Tabs[0].AddItems(chkBox7, new OpLabel(45f, lineCount, BPTranslate("Extra Cycle Time")) { bumpBehav = chkBox7.bumpBehav, description = dsc7 });
        chkBox7.description = dsc7;
		
		
		if (ModManager.MSC)
        {
            string dscSlams = BPTranslate("All slugcats can do Gourmand's body slam, if fat enough") + " (" + BPTranslate("an audio queue will play" + ")");
            this.chkBoxslugSlams = new OpCheckBox(BPOptions.slugSlams, new Vector2(15f + indenting, lineCount));
            Tabs[0].AddItems(this.chkBoxslugSlams, new OpLabel(45f + indenting, lineCount, BPTranslate("Slug Slams")) { bumpBehav = this.chkBoxslugSlams.bumpBehav, description = dscSlams });
            this.chkBoxslugSlams.description = dscSlams;
        }
        else
        {
            BPOptions.slugSlams.Value = false;
        }


        lineCount -= 40;
        string dscHints = BPTranslate("Occasionally show in-game hints related to controls and mechanics of the mod");
        //Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, "Occasionally show in-game hints related to controls and mechanics of the mod") ); //{ description = "This is My Text" }
        OpCheckBox chkBoxHints = new OpCheckBox(BPOptions.hudHints, new Vector2(15f, lineCount));
        Tabs[0].AddItems(chkBoxHints, new OpLabel(45f, lineCount, BPTranslate("Hud Hints")) { bumpBehav = chkBoxHints.bumpBehav, description = dscHints });
        chkBoxHints.description = dscHints;
		
		
		if (ModManager.MSC)
        {
            //string dscNeedles = BPTranslate("Spearmaster's needles will gain less food when your belly is full");
            //Diet Needles
            string dscNeedles = BPTranslate("When Spearmaster is full, switching hands (double tap grab) will detatch your needles");
            this.chkBoxNeedles = new OpCheckBox(BPOptions.detachNeedles, new Vector2(15f + indenting, lineCount));
            Tabs[0].AddItems(this.chkBoxNeedles, new OpLabel(45f + indenting, lineCount, BPTranslate("Detachable Needles")) { bumpBehav = this.chkBoxNeedles.bumpBehav, description = dscNeedles });
            this.chkBoxNeedles.description = dscNeedles;
        }
		

        myBoxes = new OpCheckBox[8];
        myBoxes[0] = chkBox5;
        myBoxes[1] = chkBoxArmor;
        myBoxes[2] = chkBox6;
        myBoxes[3] = chkBox4;
        myBoxes[4] = chkBox7;
        myBoxes[5] = chkBoxHints;
        if (ModManager.MSC)
        {
            myBoxes[6] = this.chkBoxslugSlams;
            myBoxes[7] = this.chkBoxNeedles;
        }



        //EHH, WE CAN DO WTHIS WITH SANDBOX MODE
        //OpCheckBox chkBox3 = new OpCheckBox(50f, 250f, "noRain", false);
        //      Tabs[0].AddItems(chkBox3,
        //          new OpLabel(50f, 280f, "No Rain") { bumpBehav = chkBox2.bumpBehav });

        //lineCount -= 65;
        //Tabs[0].AddItems(new OpLabel(50f, lineCount - 20f, "If enabled; shelter doors won't automatically close unless a player holds down to sleep") { description = "This is My Text" });
        //OpCheckBox chkBox3 = new OpCheckBox(BPOptions.holdShelterDoor, new Vector2(15f, lineCount));
        //Tabs[0].AddItems(chkBox3, new OpLabel(45f, lineCount, "Hold Shelter Doors") { bumpBehav = chkBox3.bumpBehav });







        //------------------ NEW TAB FOR OTHER STUFF



        lineCount = 550;
        Tabs[1].AddItems(new OpLabel(50f, lineCount - 20f, BPTranslate("Press Throw + Jump to add food pips. Press Crouch + Throw + Jump to subtract")));
        OpCheckBox chkBox2 = new OpCheckBox(BPOptions.debugTools, new Vector2(15f, lineCount));
        Tabs[1].AddItems(chkBox2, new OpLabel(45f, lineCount, BPTranslate("Debug Tools")) { bumpBehav = chkBox2.bumpBehav });

        lineCount -= 65;
        Tabs[1].AddItems(new OpLabel(50f, lineCount - 20f, BPTranslate("Development logs. For development things")));
        OpCheckBox chkLogs = new OpCheckBox(BPOptions.debugLogs, new Vector2(15f, lineCount));
        Tabs[1].AddItems(chkLogs, new OpLabel(45f, lineCount, BPTranslate("Debug Logs")) { bumpBehav = chkLogs.bumpBehav });


        lineCount -= 65;
        Tabs[1].AddItems(new OpLabel(50f, lineCount - 20f, BPTranslate("Adds a panting and red-faced visual effect when struggling for long periods of time")));
        OpCheckBox chkExample = new OpCheckBox(BPOptions.blushEnabled, new Vector2(15f, lineCount));
        Tabs[1].AddItems(chkExample, new OpLabel(45f, lineCount, BPTranslate("Exhaustion FX")) { bumpBehav = chkExample.bumpBehav });
        Tabs[1].AddItems(new OpLabel(50f, lineCount - 35f, BPTranslate("(can cause some visual glitches with held items)")));


        lineCount -= 95;
        OpFloatSlider sfxSlide = new OpFloatSlider(BPOptions.sfxVol, new Vector2(45f, lineCount - 25), 300, 1, false);
        Tabs[1].AddItems(sfxSlide, new OpLabel(45f, lineCount + 15, BPTranslate("Squeeze SFX Volume")) { bumpBehav = sfxSlide.bumpBehav, description = BPTranslate("Volume of the squeeze sound effect when slugcats are stuck") });
        Tabs[1].AddItems(new OpLabel(50f, lineCount - 40f, BPTranslate("(If the sfx is too soft or played without headphones)")));

        lineCount -= 80;
        Tabs[1].AddItems(new OpLabel(50f, lineCount, BPTranslate("Tip: The squeeze sfx pitch hints how close you are to popping free")));
        lineCount -= 20;
        Tabs[1].AddItems(new OpLabel(50f, lineCount, BPTranslate("Spending lots of stamina when you are close to freedom can help you pop through early!")));


        for (int j = 0; j < 3; j++)
        {
            int descLine = 155;
            Tabs[j].AddItems(new OpLabel(25f, descLine + 25f, "--- MOD FEATURES ---"));
            // Tabs[0].AddItems(new OpLabel(25f, descLine, "Press up against stuck creatures to push them. Grab them to pull"));
            // descLine -= 20;
            Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("Press against stuck creatures to push them. Grab them and move backwards to pull")));
            descLine -= 20;
            Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("Press Jump while pushing or pulling to strain harder and spend stamina")));
            descLine -= 20;
            Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("Pivot dash, belly slide, or charge-jump into stuck creatures to ram them")));
            descLine -= 20;
            Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("Spending stamina too quickly can make you exhausted and slow down progress")));
            descLine -= 30;
            //descLine -= 20;
            // Tabs[0].AddItems(new OpLabel(25f, 140f, "Certain fruits can be used to slicken up stuck creatures. Push against them with fruit in hand to apply"));
            Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("Certain fruits can be used to slicken up stuck creatures. (blue fruit, slime mold, mushrooms, etc)")));
            descLine -= 20;
            Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("While stuck, tab the Grab button to smear fruit on yourself")));
            descLine -= 20;
            Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("Push against stuck creatures with fruit in hand and press Jump to smear it on them")));
            descLine -= 25;
            if (ModManager.JollyCoop)
                Tabs[j].AddItems(new OpLabel(25f, descLine, BPTranslate("Hold Grab + Throw while holding food next to a co-op partner to feed them")));
        }
		
		
		
		
		//SIIIIGH.... FIIIIINE....
		
		int critTab = 2;
		float xPad = 30f;
		float yPad = 3f;
		//Slugcat
		
		//Lizard
		//Lantern Mice
		//Scavengers
		
		//Squidcada
		//Noodle Flies
		//Centipedes
		
		//DLL
		//Vultures
		//Miros Birds
		
		//Dropwigs
		//Leviathan


		Tabs[critTab].AddItems(new OpLabel(125f, 575f, BPTranslate("Select which creatures can become fat"), bigText: true));

        lineCount = 515;
        int baseMargin = 65;
        int margin = baseMargin;
		string dsc = "";

        Tabs[critTab].AddItems(new UIelement[]
        {
            new OpRect(new Vector2(0, lineCount - 15), new Vector2(600, 55))
        });

        OpCheckBox pBox1;
		dsc = BPTranslate("Player") + " 1";
		Tabs[critTab].AddItems(new UIelement[]
		{
			pBox1 = new OpCheckBox(BPOptions.fatP1, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(pBox1.pos.x + xPad, pBox1.pos.y + yPad, dsc)
			{description = dsc}  //bumpBehav = chkBox5.bumpBehav, 
		});
		
		margin += 125;
		OpCheckBox pBox2;
		dsc = BPTranslate("Player") + " 2";
		Tabs[critTab].AddItems(new UIelement[]
		{
			pBox2 = new OpCheckBox(BPOptions.fatP2, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(pBox2.pos.x + xPad, pBox2.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		margin += 125;
		OpCheckBox pBox3;
		dsc = BPTranslate("Player") + " 3";
		Tabs[critTab].AddItems(new UIelement[]
		{
			pBox3 = new OpCheckBox(BPOptions.fatP3, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(pBox3.pos.x + xPad, pBox3.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		margin += 125;
		OpCheckBox pBox4;
		dsc = BPTranslate("Player") + " 4";
		Tabs[critTab].AddItems(new UIelement[]
		{
			pBox4 = new OpCheckBox(BPOptions.fatP4, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(pBox4.pos.x + xPad, pBox4.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		
		//---------------- crits-------------
		margin = baseMargin;
		lineCount -= 75;
		float linePadding = 50f;

        Tabs[critTab].AddItems(new UIelement[]
        {
            new OpRect(new Vector2(0, lineCount - 165), new Vector2(600, 200))
        });

        OpCheckBox critBox1;
		dsc = BPTranslate("Lizard"); //creaturetype-GreenLizard
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox1 = new OpCheckBox(BPOptions.fatLiz, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox1.pos.x + xPad, critBox1.pos.y + yPad, dsc)
			{description = dsc}  //bumpBehav = chkBox5.bumpBehav, 
		});
		
		
		margin += 175;
		OpCheckBox critBox2;
		dsc = BPTranslate("creaturetype-LanternMouse");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox2 = new OpCheckBox(BPOptions.fatMice, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox2.pos.x + xPad, critBox2.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		
		margin += 175;
		OpCheckBox critBox3;
		dsc = BPTranslate("creaturetype-Scavenger");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox3 = new OpCheckBox(BPOptions.fatScavs, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox3.pos.x + xPad, critBox3.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		
		margin = baseMargin;
		lineCount -= linePadding;
		OpCheckBox critBox4;
		dsc = BPTranslate("creaturetype-CicadaA");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox4 = new OpCheckBox(BPOptions.fatSquids, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox4.pos.x + xPad, critBox4.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		margin += 175;
		OpCheckBox critBox5;
		dsc = BPTranslate("creaturetype-BigNeedleWorm");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox5 = new OpCheckBox(BPOptions.fatNoots, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox5.pos.x + xPad, critBox5.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		margin += 175;
		OpCheckBox critBox6;
		dsc = BPTranslate("creaturetype-Centipede");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox6 = new OpCheckBox(BPOptions.fatCentis, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox6.pos.x + xPad, critBox6.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		//DLL
		//Vultures
		//Miros Birds
		margin = baseMargin;
		lineCount -= linePadding;
		OpCheckBox critBox7;
		dsc = BPTranslate("creaturetype-DaddyLongLegs");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox7 = new OpCheckBox(BPOptions.fatDll, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox7.pos.x + xPad, critBox7.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		margin += 175;
		OpCheckBox critBox8;
		dsc = BPTranslate("creaturetype-Vulture");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox8 = new OpCheckBox(BPOptions.fatVults, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox8.pos.x + xPad, critBox8.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		margin += 175;
		OpCheckBox critBox9;
		dsc = BPTranslate("creaturetype-MirosBird");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox9 = new OpCheckBox(BPOptions.fatMiros, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox9.pos.x + xPad, critBox9.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		//Dropwigs
		//Leviathan
		margin = baseMargin;
		lineCount -= linePadding;
		OpCheckBox critBox10;
		dsc = BPTranslate("creaturetype-DropBug");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox10 = new OpCheckBox(BPOptions.fatWigs, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox10.pos.x + xPad, critBox10.pos.y + yPad, dsc)
			{description = dsc}
		});
		
		
		margin += 175;
		OpCheckBox critBox11;
		dsc = BPTranslate("creaturetype-BigEel");
		Tabs[critTab].AddItems(new UIelement[]
		{
			critBox11 = new OpCheckBox(BPOptions.fatEels, new Vector2(margin, lineCount))
			{description = dsc},
			new OpLabel(critBox11.pos.x + xPad, critBox11.pos.y + yPad, dsc)
			{description = dsc}
		});


        if (ModManager.MSC)
        {
            margin += 175;
            OpCheckBox critBox12;
            dsc = BPTranslate("creaturetype-SlugNPC");
            Tabs[critTab].AddItems(new UIelement[]
            {
                critBox12 = new OpCheckBox(BPOptions.fatPups, new Vector2(margin, lineCount))
                {description = dsc},
                new OpLabel(critBox12.pos.x + xPad, critBox12.pos.y + yPad, dsc)
                {description = dsc}
            });
        }
        else
        {
            BPOptions.fatPups.Value = true;
        }





        //------------------ ANOTHER NEW TAB FOR OTHER STUFF

        int infoTab = 3;
        lineCount = 550;

        Tabs[infoTab].AddItems(new OpLabel(55f, lineCount, BPTranslate("If you run into any bugs or issues, let me know so I can fix them!")));

        lineCount -= 35;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("Message me on discord: WillowWisp#3565 ")));
        lineCount -= 25;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("Or ping @WillowWisp on the rain world Discord Server in the modding-support channel")));
        lineCount -= 25;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("Or leave a comment on the mod workshop page in the Bug Reporting discussion!")));
        lineCount -= 25;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("Or anywhere you want please I'm begging and sobbing please report your bugs")));

        lineCount -= 50;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("Super bonus points if you include your error log files (if they generated)")));
        lineCount -= 25;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("located in: /Program Files (86)/Steam/steamapps/common/Rain World/exceptionLog.txt")));


        lineCount -= 50;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("Leave feedback on the mod page and rate the mod if you enjoy it!")));
        lineCount -= 25;
        Tabs[infoTab].AddItems(new OpLabel(35f, lineCount, BPTranslate("If you post footage, send me a link! I'd love to see! :D")));


    }

}