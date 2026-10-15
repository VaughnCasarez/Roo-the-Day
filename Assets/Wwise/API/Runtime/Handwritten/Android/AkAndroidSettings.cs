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
public partial class AkCommonUserSettings
{
	partial void SetSampleRate(AkPlatformInitSettings settings)
	{
		settings.uSampleRate = m_SampleRate;
	}

	protected partial string GetPluginPath()
	{
		return null;
	}
}
#endif

public class AkAndroidSettings : AkWwiseInitializationSettings.PlatformSettings
{
#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoadMethod]
	private static void AutomaticPlatformRegistration()
	{
		if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
		{
			return;
		}

		RegisterPlatformSettingsClass<AkAndroidSettings>("Android");
	}
#endif // UNITY_EDITOR

	public AkAndroidSettings()
	{
		SetUseGlobalPropertyValue("UserSettings.m_MainOutputSettings.m_PanningRule", false);
		SetUseGlobalPropertyValue("UserSettings.m_MainOutputSettings.m_ChannelConfig.m_ChannelConfigType", false);
		SetUseGlobalPropertyValue("UserSettings.m_MainOutputSettings.m_ChannelConfig.m_ChannelMask", false);
	}

	protected override AkCommonUserSettings GetUserSettings()
	{
		return UserSettings;
	}

	protected override AkCommonAdvancedSettings GetAdvancedSettings()
	{
		return AdvancedSettings;
	}

	protected override AkCommonCommSettings GetCommsSettings()
	{
		return CommsSettings;
	}

	[System.Serializable]
	public class PlatformAdvancedSettings : AkCommonAdvancedSettings
	{
		public enum AudioAPI
		{
			AndroidAudio = 1 << 0,
			OpenSL_ES = 1 << 1
		}
		
		public enum SpatializerAPI
		{
			DolbyAtmos = 1 << 8,
			AndroidSpatializer = 1 << 9
		}

		public enum AudioPath
		{
			Legacy,
			LowLatency,
			Exclusive
		}

		[UnityEngine.Tooltip("Main audio API to allow using for audio output. Leave set to \"Everything\" to let the sink decide the best audio API for the device.")]
		[AkEnumFlag(typeof(AudioAPI))]
		public AudioAPI m_AudioAPI = AudioAPI.AndroidAudio | AudioAPI.OpenSL_ES;
		
		[UnityEngine.Tooltip("Spatializer API to allow using for 3D audio support. Note that Android Spatializer has noticeable latency issues. Disabling all spatializer APIs will effectively disable 3D audio.")]
		[AkEnumFlag(typeof(SpatializerAPI))]
		public SpatializerAPI m_SpatializerAPI = SpatializerAPI.DolbyAtmos;

		[UnityEngine.Tooltip("Which audio path to use. Legacy gives best compatibility with the widest range of devices but noticeably high latency. Exclusive has best latency but has several drawbacks and bad compatibility. LowLatency is a good balance between the two.")]
		public AudioPath m_AudioPath = AudioPath.LowLatency;

		[UnityEngine.Tooltip("Enable system-level verbose logging for the Wwise sink. Useful for troubleshooting audio output problems on some devices.")]
		public bool m_VerboseSink = false;

		public override void CopyTo(AkPlatformInitSettings settings)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			settings.eAudioAPI = (AkAudioAPI)m_AudioAPI | (AkAudioAPI)m_SpatializerAPI;
			settings.eAudioPath = (AkAudioPath)m_AudioPath;
			settings.bVerboseSink = m_VerboseSink;
#endif
		}
	}

	[UnityEngine.HideInInspector]
	public AkCommonUserSettings UserSettings = new AkCommonUserSettings
	{
		m_MainOutputSettings = new AkCommonOutputSettings
		{
			m_PanningRule = AkCommonOutputSettings.PanningRule.Headphones
		},
	};

	[UnityEngine.HideInInspector]
	public PlatformAdvancedSettings AdvancedSettings;

	[UnityEngine.HideInInspector]
	public AkCommonCommSettings CommsSettings;
}
