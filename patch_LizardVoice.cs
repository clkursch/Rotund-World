
public class patch_LizardVoice
{

    public static void Patch()
    {
		On.LizardVoice.GetMyVoiceTrigger += Lizard_GetMyVoiceTrigger;
	}
	
	public static SoundID Lizard_GetMyVoiceTrigger(On.LizardVoice.orig_GetMyVoiceTrigger orig, LizardVoice self)
	{
		if (self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard )
			return SoundID.None; //NO WHITE LIZARD SOUNDS! WE'RE USING THOSE FILES
		else
			return orig.Invoke(self);
	}

}