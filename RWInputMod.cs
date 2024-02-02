using ImprovedInput;
using UnityEngine;
using RWCustom;
using System.Collections.Generic;
using static PlayerMod;

//using static CoopLeash.PlayerMod;

//namespace RotundWorld;

//I DON'T TOTALLY UNDERSTANT HOW THIS WORKS BUT I'M JUST FOLLOWING SHAUMBAUM'S LEAD
public static class PlayerMod
{

    public static readonly int maximum_number_of_players = RainWorld.PlayerObjectBodyColors.Length;
    public static List<InputPackageMod[]> custom_input_list = null!;

    internal static void OnEnable()
    {
        Initialize_Custom_Inputs();
        On.Player.checkInput -= Player_CheckInput;
        On.Player.checkInput += Player_CheckInput;
    }

    public static void Initialize_Custom_Inputs()
    {
        if (custom_input_list != null) return;
        custom_input_list = new();

        for (int player_number = 0; player_number < maximum_number_of_players; ++player_number)
        {
            InputPackageMod[] custom_input = new InputPackageMod[2];
            custom_input[0] = new();
            custom_input[1] = new();
            custom_input_list.Add(custom_input);
        }
    }

    public static bool WantsToFeed(this Player player)
    {
        int player_number = player.playerState.playerNumber;
        if (player_number >= maximum_number_of_players) return false;

        InputPackageMod[] custom_input = custom_input_list[player_number];
        return custom_input[0].feed_btn && !custom_input[1].feed_btn;
    }

    public static bool HoldingFeed(this Player player)
    {
        int player_number = player.playerState.playerNumber;
        if (player_number >= maximum_number_of_players) return false;

        InputPackageMod[] custom_input = custom_input_list[player_number];
        return custom_input[0].feed_btn;
    }

    public static bool WantsBackFood(this Player player)
    {
        int player_number = player.playerState.playerNumber;
        if (player_number >= maximum_number_of_players) return false;

        InputPackageMod[] custom_input = custom_input_list[player_number];
        return custom_input[0].backfood_btn && !custom_input[1].backfood_btn;
    }

    private static void Player_CheckInput(On.Player.orig_checkInput orig, Player player)
    {
        // update player.input first;
        orig(player);

        int player_number = player.playerState.playerNumber;
        if (player_number < 0) return;
        if (player_number >= maximum_number_of_players) return;

        InputPackageMod[] custom_input = custom_input_list[player_number];
        custom_input[1] = custom_input[0];

        if (player.stun == 0 && !player.dead)
        {
            custom_input[0] = RWInputMod.Get_Input(player);
            return;
        }
        custom_input[0] = new();
    }

    public struct InputPackageMod
    {
        public bool feed_btn = false;
        public bool backfood_btn = false;
        public InputPackageMod() { }
    }
}



public static class RWInputMod {

    public static PlayerKeybind feed_keybinding = null!;
	public static PlayerKeybind backfood_keybinding = null!;

    public static void Initialize_Custom_Keybindings() {
        if (feed_keybinding != null) return;

        // initialize after ImprovedInput has;
        feed_keybinding = PlayerKeybind.Register("BellyPlus-Feed", "Rotund World", Custom.rainWorld.inGameTranslator.Translate("Feed Player"), KeyCode.None, KeyCode.None);
		backfood_keybinding = PlayerKeybind.Register("BellyPlus-BackFood", "Rotund World", Custom.rainWorld.inGameTranslator.Translate("Back Food"), KeyCode.None, KeyCode.None);
    }

    public static bool BackFoodBound(Player player) {
        int player_number = player.playerState.playerNumber;
        return !backfood_keybinding.Unbound(player_number);
    }

    public static InputPackageMod Get_Input(Player player) {
        InputPackageMod custom_input = new();
        int player_number = player.playerState.playerNumber;

        if (feed_keybinding.Unbound(player_number)) 
            custom_input.feed_btn = player.input[0].thrw && !player.input[1].thrw && player.input[0].pckp; //player.input[0].mp;
        else
            custom_input.feed_btn = feed_keybinding.CheckRawPressed(player_number);
		
		if (backfood_keybinding.Unbound(player_number)) 
            custom_input.backfood_btn = false;
        else
            custom_input.backfood_btn = backfood_keybinding.CheckRawPressed(player_number);

        return custom_input;
    }
}