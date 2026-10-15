/*******************************************************************************
The content of this file includes portions of the proprietary AUDIOKINETIC Wwise
Technology released in source code form as part of the game integration package.
The content of this file may not be used without valid licenses to the AUDIOKINETIC
Wwise Technology.
Note that the use of the game engine is subject to the Unity Terms of Service
at https://unity3d.com/legal/terms-of-service.

License Usage

Licensees holding valid licenses to the AUDIOKINETIC Wwise Technology may use
this file in accordance with the end user license agreement provided with the
software or, alternatively, in accordance with the terms contained
in a written agreement between you and Audiokinetic Inc.
Copyright (c) 2026 Audiokinetic Inc.
*******************************************************************************/

#if AK_ENABLE_OPENXR

using AK.Wwise.Unity.Logging;
using UnityEngine;

namespace AK.Wwise.Unity.OpenXR
{
    /// <summary>
    /// Helper component that adds a Wwise Motion output when OpenXR integration is ready.
    ///
    /// IMPORTANT: The Motion output must be added AFTER OpenXR is fully initialized for the
    /// OpenXR Motion sink to be selected. This component handles the correct timing by subscribing
    /// to AkWwiseOpenXRMotionFeature.OnOpenXRMotionInitialized.
    ///
    /// Usage: Attach this component to any persistent GameObject.
    /// It will automatically add the Motion output at the correct time and remove it on destroy.
    ///
    /// For custom implementations, subscribe to AkWwiseOpenXRMotionFeature.OnOpenXRMotionInitialized
    /// and call AkUnitySoundEngine.AddOutput(new AkOutputSettings("Motion"), out deviceId) in your handler.
    /// </summary>
    [AddComponentMenu("Wwise/Motion Output")]
    public class AkWwiseOpenXRMotionOutput : MonoBehaviour
    {
        private ulong m_motionOutputDeviceId = AkUnitySoundEngine.AK_INVALID_OUTPUT_DEVICE_ID;

        [Tooltip("The name of the Motion shareset in your Wwise project.")]
        public string MotionSharesetName = "Motion";

        void Start()
        {
            // Check if already initialized (in case initialization happened before Start)
            if (AkWwiseOpenXRMotionFeature.IsInitialized)
            {
                AddMotionOutput();
            }
            else
            {
                // Subscribe to the event
                AkWwiseOpenXRMotionFeature.OnOpenXRMotionInitialized += AddMotionOutput;
            }
        }

        void OnDestroy()
        {
            // Always unsubscribe to avoid memory leaks
            AkWwiseOpenXRMotionFeature.OnOpenXRMotionInitialized -= AddMotionOutput;

            if (m_motionOutputDeviceId != AkUnitySoundEngine.AK_INVALID_OUTPUT_DEVICE_ID)
            {
                AkUnitySoundEngine.RemoveOutput(m_motionOutputDeviceId);
            }
        }

        private void AddMotionOutput()
        {
            // Unsubscribe immediately since we only want this called once
            AkWwiseOpenXRMotionFeature.OnOpenXRMotionInitialized -= AddMotionOutput;

            if (m_motionOutputDeviceId != AkUnitySoundEngine.AK_INVALID_OUTPUT_DEVICE_ID)
                return; // Already added

            var outputSettings = new AkOutputSettings(MotionSharesetName);
            if (AkUnitySoundEngine.AddOutput(outputSettings, out m_motionOutputDeviceId) == AKRESULT.AK_Success)
            {
                WwiseLogger.LogFormat(LogLevel.Verbose, "[AkWwiseOpenXRMotionOutput] Successfully added Motion output device (ID: {0})", m_motionOutputDeviceId);
            }
            else
            {
                WwiseLogger.Error("[AkWwiseOpenXRMotionOutput] Failed to add Motion output.");
            }
        }
    }
}

#endif // AK_ENABLE_OPENXR
