using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;

public class patch_SoundImporter
{
	public static void Patch()
	{
		On.SoundLoader.SoundImporter.reloadSounds += SoundImporter_reloadSounds;
		On.SoundLoader.LoadSounds += SoundLoader_LoadSounds;

	}


	//public class SoundImporter : MonoBehaviour
	//{
	public static void SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
	{
		self.errors = new List<string>();
		Debug.Log("CUSTOM Loading sounds");
		self.SoundsFolderPath = Custom.RootFolderDirectory() + Path.DirectorySeparatorChar + "SoundEffects";
		string[] array = File.ReadAllLines(self.SoundsFolderPath + Path.DirectorySeparatorChar + "Sounds.txt");
		self.volume = float.Parse(Regex.Split(array[0], ": ")[1]);
		self.volumeExponent = float.Parse(Regex.Split(array[1], ": ")[1]);
		self.volumeGroups = new List<SoundLoader.VolumeGroup>();
		List<SoundLoader.VolumeGroup> list = new List<SoundLoader.VolumeGroup>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = Regex.Split(array[i], " : ");
			if (array2[0] == "START VOLUME GROUP")
			{
				self.volumeGroups.Add(new SoundLoader.VolumeGroup(array2[1], float.Parse(array2[2])));
				list.Add(self.volumeGroups[self.volumeGroups.Count - 1]);
			}
			if (array2[0] == "END VOLUME GROUP")
			{
				self.VolumeGroupStopRecording(list, array2[1]);
			}
			//else if (array2.Length == 2 && (array2[0].get_Chars(0) != '/' || array2[0].get_Chars(1) != '/'))
			else if (array2.Length == 2 && !array2[0].StartsWith("//")) //HOPE THIS WORKS....
			{
				list2.Add(i);
				self.RecordLineToVolumeGroups(list, i);
			}
		}
		self.soundTriggers = new SoundLoader.SoundTrigger[Enum.GetNames(typeof(SoundID)).Length];
		self.workingTriggers = new bool[Enum.GetNames(typeof(SoundID)).Length];
		List<string> list3 = new List<string>();
		List<bool> list4 = new List<bool>();
		for (int j = 0; j < Enum.GetNames(typeof(SoundID)).Length; j++)
		{
			SoundID soundID = (SoundID)j;
			Debug.Log("--SOUND ID:" + soundID.ToString());
			for (int k = 0; k < list2.Count; k++)
			{
				string[] array3 = Regex.Split(array[list2[k]], " : ");
				string[] array4 = Regex.Split(array3[0], "/");
				if (array3.Length > 0 && string.Compare(array4[0], soundID.ToString(), true) == 0)
				{
					Debug.Log("---SOUND INSTRUCTIONS FOUND:" + soundID.ToString());
					bool flag = false;
					List<SoundLoader.SoundPlayInstruction> list5 = new List<SoundLoader.SoundPlayInstruction>();
					string[] array5 = Regex.Split(array3[1], ", ");
					for (int l = 0; l < array5.Length; l++)
					{
						string text = Regex.Split(array5[l], "/")[0];
						if (string.Compare(text, "SAMENAME", true) == 0)
						{
							text = array4[0];
							Debug.Log("SAMENAME DETECTED? " + soundID.ToString());
						}
						int num = -1;
						for (int m = 0; m < list3.Count; m++)
						{
							Debug.Log("----CHECKING FOR DUPES... " + soundID.ToString());
							if (string.Compare(list3[m], text, true) == 0)
							{
								num = m;
								//Debug.Log("----DUPE FOUND AT!! " + m + " " + soundID.ToString());
								Debug.Log("----ACTUALLY, THIS PROBABLY MEANS NO DUPE FOUND " + m + " " + soundID.ToString());
								break;
							}
						}
						if (num == -1)
						{
							
							if (self.CheckIfFileExistsAsUnityResource(text))
							{
								list3.Add(text);
								list4.Add(true);
								num = list3.Count - 1;
								Debug.Log("-----REGISTERED AS UNITY RESOURCE:" + soundID.ToString());
							}
							else
							{
								if (!self.CheckIfFileExistsAsExternal(text))
								{
									if (text != string.Empty)
									{
										self.errors.Add("Can't find file: " + text);
										Debug.Log("--------CAN'T FIND FILE. WE DIDN'T MAKE IT ON THE LIST! " + text);
									}
									Debug.Log("-----BROKEN EXTERNAL RESOURCE!:" + soundID.ToString());
									flag = true;
									break;
								}
								list3.Add(text);
								list4.Add(false);
								num = list3.Count - 1;
								Debug.Log("-----REGISTERED AS EXTERNAL RESOURCE:" + soundID.ToString());
							}
						}
						list5.Add(new SoundLoader.SoundPlayInstruction(num, array5[l]));
					}
					if (!flag)
					{
						self.soundTriggers[j] = new SoundLoader.SoundTrigger(soundID, list5.ToArray(), self.GroupVolume(list2[k]), self, array4);
						self.workingTriggers[j] = true;
					}
					list2.RemoveAt(k);
					break;
				}
			}
		}
		if (list2.Count > 0)
		{
			self.errors.Add("Non existent triggers:");
			Debug.Log("-------Non existent triggers: ");
			for (int n = 0; n < list2.Count; n++)
			{
				self.errors.Add("     " + array[list2[n]]);
			}
		}
		self.audioClipNames = list3.ToArray();
		self.externalAudio = new AudioClip[self.audioClipNames.Length][];
		self.unityAudio = new AudioClip[self.audioClipNames.Length][];
		self.audioClipsThroughUnity = list4.ToArray();
		self.soundVariations = new int[self.audioClipNames.Length];
		for (int num2 = 0; num2 < self.soundVariations.Length; num2++)
		{
			self.soundVariations[num2] = self.VariationsForSound(self.audioClipNames[num2]);
			if (!self.audioClipsThroughUnity[num2])
			{
				self.externalAudio[num2] = new AudioClip[self.soundVariations[num2]];
			}
			else
			{
				self.unityAudio[num2] = new AudioClip[self.soundVariations[num2]];
			}
		}
		if (self.gameObject != null)
		{
			UnityEngine.Object.Destroy(self.gameObject);
			self.gameObject = null;
		}
		self.gameObject = new GameObject("SoundLoader");
		self.soundImporter = self.gameObject.AddComponent<SoundLoader.SoundImporter>();
		self.soundImporter.Init(self);
		self.startTime = Time.time;
		self.clipsToBeLoaded = 0;
		for (int num3 = 0; num3 < self.externalAudio.Length; num3++)
		{
			if (!self.audioClipsThroughUnity[num3])
			{
				self.clipsToBeLoaded += self.externalAudio[num3].Length;
			}
		}
	}

		

	//public void SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
	public static void SoundImporter_reloadSounds(On.SoundLoader.SoundImporter.orig_reloadSounds orig, SoundLoader.SoundImporter self)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(self.absolutePath);
		self.files = directoryInfo.GetFiles();
		int num = 0;
		Debug.Log("-------MY FILE PATH " + directoryInfo);

		
		for (int j = 0; j < self.owner.audioClipNames.Length; j++)
		{
			Debug.Log("-------audioClipNames... " + self.owner.audioClipNames[j].ToString() );
		}
		 
		 
		 
		 


		foreach (FileInfo fileInfo in self.files)
		{
			Debug.Log("------- ITERATING SOUND FILES " + fileInfo.FullName);
			if (self.validFileType(fileInfo.FullName))
			{
				Debug.Log("-------VALID FILE TYPE DETECTED " + fileInfo.FullName);
				string[] array2 = fileInfo.FullName.Split(new char[]
				{
					'\\'
				});
				string text = array2[array2.Length - 1].Split(new char[]
				{
					'.'
				})[0];
				int num2 = 1;
				bool flag = false;
				if (text.Split(new char[]
				{
					'_'
				}).Length > 1)
				{
					num2 = int.Parse(text.Split(new char[]
					{
						'_'
					})[1]);
					text = text.Split(new char[]
					{
						'_'
					})[0];
					flag = true;
				}
				int num3 = -1;
				for (int j = 0; j < self.owner.audioClipNames.Length; j++)
				{
					Debug.Log("-------COMPARING FILE NAMES... " + fileInfo.FullName + " " + self.owner.audioClipsThroughUnity[j] + " " + string.Compare(self.owner.audioClipNames[j], text, true));
					//OKAY OKAY, SO IT SEEMS LIKE IT NEEDS TO EXIST AS AN AUDIO CLIP NAME, BUT NOT EXIST IN THE UNITY CLIPS FOLDER...
					//ACTUALLY WAIT, NO, IT ACTUALLY SEEMS LIKE IT NEEDS TO EXIST IN NEITHER...
					//MoonGrav.wav.meta RETURNS 0 FOR IT'S OUTPUT. BUT ALSO RETURNS TRUE FOR THE UNITY THING... WAIT MAYBE IT RETURNS 0 CUZ EXISTS
					if (!self.owner.audioClipsThroughUnity[j] && string.Compare(self.owner.audioClipNames[j], text, true) == 0)
					{
						Debug.Log("-------MATCH DETECTED!! " + fileInfo.FullName);
						num3 = j;
						break;
					}
				}
				if (num3 > -1)
				{
					Debug.Log("-------STARTING COROUTINE " + fileInfo.FullName);
					self.StartCoroutine(self.loadFile(fileInfo.FullName, text + ((!flag) ? string.Empty : ("_" + num2)), new IntVector2(num3, num2 - 1)));
					num++;
				}
			}
		}
		self.owner.errors.Add("Initiating import of " + num + " samples");
		Debug.Log("------- Initiating import of " + num + " samples");
	}

		
	//}

}
