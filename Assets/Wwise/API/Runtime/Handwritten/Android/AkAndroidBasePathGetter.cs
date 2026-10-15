/*******************************************************************************
The content of this file includes portions of the proprietary AUDIOKINETIC Wwise
Technology released in source code form as part of the game integration package.
The content of this file may not be used without valid licenses to the
AUDIOKINETIC Wwise Technology.
Note that the use of the game engine is subject to the Unity(R) Terms of
Service at https://unity3d.com/legal/terms-of-service
 
License Usage
 
Licensees holding valid licenses to the AUDIOKINETIC Wwise Technology may use
this file in accordance with the end user license agreement provided with the
software or, alternatively, in accordance with the terms contained
in a written agreement between you and Audiokinetic Inc.
Copyright (c) 2026 Audiokinetic Inc.
*******************************************************************************/

﻿#if UNITY_ANDROID && !UNITY_EDITOR
public partial class AkBasePathGetter
{
	static string DefaultPlatformName = "Android";

	public static void AdjustFullBasePathForPlatform(ref string fullBasePath) {}

	public static string GetPersistentDataPath()
	{
		return UnityEngine.Application.persistentDataPath;
	}

	public static bool InitBankExists(string tempSoundBankBasePath)
	{
#if UNITY_EDITOR
		return System.IO.File.Exists(System.IO.Path.Combine(tempSoundBankBasePath, "Init.bnk"));
#else
		// Can't use File.Exists on Android, assume banks are there
		return true;
#endif		
	}

	public static string GetDecodedBankPath()
	{
#if UNITY_EDITOR
		return System.IO.Path.Combine(Instance.SoundBankBasePath, DecodedBankFolder);
#else
		return System.IO.Path.Combine(GetPersistentDataPath(), DecodedBankFolder);
#endif
	}
}
#endif

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
public partial class AkAndroidBasePathGetter
{
	static AkAndroidBasePathGetter()
	{
		AkBasePathGetter.AddTargetPlatform(UnityEditor.BuildTarget.Android, "Android");
	}
}
#endif