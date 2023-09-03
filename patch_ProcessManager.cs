using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using On;
using Menu.Remix.MixedUI;
//TO RUN OUR GARBAGE COLLECTOR FOR US
public class patch_ProcessManager
{
	public static void Patch()
	{
		On.ProcessManager.PostSwitchMainProcess += BP_ProcessPatch; //UPDATED FOR DOWNPOUR
		On.OverWorld.WorldLoaded += BP_WorldLoaded;
	}

	public static void BP_ProcessPatch(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager pm, ProcessManager.ProcessID ID)
	{
		orig.Invoke(pm, ID);
		bool flag = pm.currentMainLoop != null && ID != ProcessManager.ProcessID.Game;
		if (flag)
		{
			BellyPlus.GarbageCollect();
		}
	}

	public static void BP_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld world)
	{
		//AbstractRoom abstractRoom = world.reportBackToGate.room.abstractRoom;
		try
		{
			Room myRoom = world.reportBackToGate.room;
			BellyPlus.RefreshDictionaries(myRoom);
		}
		catch (Exception arg)
		{
			//BellyPlus.Logger.LogError(string.Format("Failed to initialize Fat World", arg));
			Debug.Log("ERROR CAUGHT- BP_WORLD LOADED" );
			//throw; ???
		}
		orig.Invoke(world);
	}
}

