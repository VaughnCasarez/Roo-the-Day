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

using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

using AK.Wwise.Unity.Logging;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace AK.Wwise.Unity.OpenXR
{
    /// <summary>
    /// Enables Wwise Motion haptics support for Meta Quest devices via OpenXR.
    /// This feature bridges Unity's OpenXR plugin with the Wwise AkMotion plugin.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = "Wwise OpenXR Motion",
        BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
        Company = "Audiokinetic",
        Desc = "Wwise Motion haptics support via OpenXR for Meta Quest devices.",
        DocumentationLink = "https://www.audiokinetic.com/",
        OpenxrExtensionStrings = "XR_FB_haptic_pcm",
        Version = "1.0.0",
        FeatureId = featureId
    )]
#endif
    public class AkWwiseOpenXRMotionFeature : OpenXRFeature
    {
        public const string featureId = "com.audiokinetic.wwise.openxr.motion";

        // Event that fires when OpenXR Motion is initialized and ready.
        public static event System.Action OnOpenXRMotionInitialized;

        private static bool s_isInitialized = false;
        public static bool IsInitialized => s_isInitialized;

        // Unity creates one feature instance per BuildTargetGroup (Android, Standalone),
        // but only the active build target's instance receives OpenXR callbacks (OnInstanceCreate, OnSessionCreate).
        // We set s_instance in OnInstanceCreate() to identify which instance has valid OpenXR handles.
        private static AkWwiseOpenXRMotionFeature s_instance = null;

        // OpenXR handles
        private ulong m_xrInstance = 0;
        private ulong m_xrSession = 0;
        private ulong m_xrHapticAction = 0;
        private System.IntPtr m_xrGetInstanceProcAddr = System.IntPtr.Zero;

        public static AkWwiseOpenXRMotionFeature Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = OpenXRSettings.Instance?.GetFeature<AkWwiseOpenXRMotionFeature>();
                }
                return s_instance;
            }
        }

        protected override void OnDisable()
        {
            if (s_instance == this) // Only clear s_instance if we're the active instance
            {
                s_instance = null;
            }
            base.OnDisable();
        }

        protected override System.IntPtr HookGetInstanceProcAddr(System.IntPtr func)
        {
            m_xrGetInstanceProcAddr = func;
            return base.HookGetInstanceProcAddr(func);
        }

        // Called when an OpenXR instance is created. Captures the XrInstance handle.
        // Only called for the active build target's feature instance.
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            m_xrInstance = xrInstance;
            s_instance = this; // This instance is the active one - set as singleton
            return base.OnInstanceCreate(xrInstance);
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            m_xrInstance = 0;
            base.OnInstanceDestroy(xrInstance);
        }

        // Called when an OpenXR session is created. Captures the XrSession handle.
        protected override void OnSessionCreate(ulong xrSession)
        {
            m_xrSession = xrSession;
            InputSystem.onAfterUpdate -= OnInputSystemAfterUpdate;
            InputSystem.onAfterUpdate += OnInputSystemAfterUpdate;
            base.OnSessionCreate(xrSession);
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            InputSystem.onAfterUpdate -= OnInputSystemAfterUpdate;
            m_xrSession = 0;
            m_xrHapticAction = 0;
            base.OnSessionDestroy(xrSession);
        }

        // Will be called until the Input System's controller is ready. Then we unregister the callback.
        static void OnInputSystemAfterUpdate()
        {
            if (AkUnitySoundEngine.IsInitialized())
            {
                if (Instance && Instance.SetWwiseMotionOpenXREnvironment())
                {
                    InputSystem.onAfterUpdate -= OnInputSystemAfterUpdate;
                }
            }
        }

        // This method fails if the Unity Input System is not ready and can be called again on the next frame.
        public bool SetWwiseMotionOpenXREnvironment()
        {
            if (m_xrInstance == 0 || m_xrSession == 0)
            {
                WwiseLogger.Error("[AkWwiseOpenXRMotionFeature] XrInstance or XrSession are invalid.");
                return false;
            }

            // Getting the haptic action from any of the two controller is valid since it resolves to the same XrAction.
            if (!TryGetHapticActionHandle("<XRController>{LeftHand}/haptic") && !TryGetHapticActionHandle("<XRController>{RightHand}/haptic"))
            {
                return false;
            }

            if (!AkWwiseOpenXRMotionBridge.Initialize(m_xrGetInstanceProcAddr, m_xrInstance, m_xrSession, m_xrHapticAction))
            {
                WwiseLogger.Error("[AkWwiseOpenXRMotionFeature] Failed to initialize AkMotion bridge");
                return false;
            }

            WwiseLogger.Log("[AkWwiseOpenXRMotionFeature] Wwise Motion OpenXR integration initialized successfully!");
            s_isInitialized = true;

            OnOpenXRMotionInitialized?.Invoke();

            return true;
        }

        // Retrieves the XrAction handle by piggybacking on the controller profile's existing haptic action.
        // It will return false silently while waiting for the Input System to bind controls.
        private bool TryGetHapticActionHandle(string bindingPath)
        {
            bool gotAction = false;

            // This relies on a controller profile (e.g., Meta Quest Touch Pro Controller Profile) being enabled.
            // Note: Any of the hand bindings (LeftHand or RightHand) resolves to the controller profile's unified
            // haptic output, which automatically applies to both controllers. This is handled by the OpenXR runtime.
            var hapticAction = new UnityEngine.InputSystem.InputAction(
                name: "WwiseMotionHaptic",
                type: UnityEngine.InputSystem.InputActionType.PassThrough,
                binding: bindingPath
            );
            hapticAction.Enable();

            // The control count can be 0 when Unity's Input System is not ready yet. This could happen because the controller profile
            // is not loaded, the controllers are not detected or action's path is not bound. We need to try again later.
            if (hapticAction.controls.Count > 0)
            {
                m_xrHapticAction = GetAction(hapticAction); // Get the native XrAction handle from Unity OpenXR

                if (m_xrHapticAction != 0)
                {
                    gotAction = true;
                }
                else
                {
                    WwiseLogger.Error("[AkWwiseOpenXRMotionFeature] Could not get XrAction with control bound.");
                }
            }

            hapticAction.Disable(); // Does not need to be enabled once we have the XrAction handle.

            return gotAction;
        }
    }
}

#endif // AK_ENABLE_OPENXR
