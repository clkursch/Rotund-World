using System;
using RWCustom;
using UnityEngine;


public class patch_VirtualMicrophone
{
    public static void Patch()
    {
        On.VirtualMicrophone.NewRoom += BP_NewRoom;
    }

	public static void BP_NewRoom(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
	{
        orig.Invoke(self, room);
		patch_Lizard.refreshSounds = true;
        
	}

}