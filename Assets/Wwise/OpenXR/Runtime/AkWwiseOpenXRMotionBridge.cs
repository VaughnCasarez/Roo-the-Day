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

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR.NativeTypes;

using AK.Wwise.Unity.Logging;

namespace AK.Wwise.Unity.OpenXR
{
    /// <summary>
    /// P/Invoke bridge to native AkMotion OpenXR functions.
    /// Provides C# bindings to pass OpenXR function pointers and handles to the Wwise Motion plugin.
    /// </summary>
    public static class AkWwiseOpenXRMotionBridge
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate XrResult XrGetInstanceProcAddrDelegate(ulong instance, IntPtr name, ref IntPtr function);

        [DllImport("AkMotion", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AkMotionInitializeOpenXRFunctions(IntPtr xrResultToString, IntPtr xrEnumerateInstanceExtensionProperties, IntPtr xrStringToPath, IntPtr xrGetInstanceProcAddr, IntPtr xrApplyHapticFeedback);

        [DllImport("AkMotion", CallingConvention = CallingConvention.Cdecl)]
        private static extern void AkMotionSetOpenXRHapticAction(ulong xrInstance, ulong xrSession, ulong xrAction);

        private static IntPtr GetOpenXRFunction(IntPtr xrGetInstanceProcAddr, ulong instance, string functionName)
        {  
            var getProcAddr = Marshal.GetDelegateForFunctionPointer<XrGetInstanceProcAddrDelegate>(xrGetInstanceProcAddr);
            IntPtr functionPtr = IntPtr.Zero;
            IntPtr namePtr = Marshal.StringToHGlobalAnsi(functionName);

            try
            {
                XrResult result = getProcAddr(instance, namePtr, ref functionPtr);
                if (result != XrResult.Success)
                {
                    WwiseLogger.LogFormat(LogLevel.Warning, "[AkWwiseOpenXRMotionBridge] Failed to get function pointer for {0}: {1}", functionName, result);
                    return IntPtr.Zero;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(namePtr);
            }

            return functionPtr;
        }

        // Initializes the Wwise Motion plugin with OpenXR function pointers and handles.
        // This must be called after OpenXR initialization and after the Wwise sound engine has loaded Init.bnk.
        public static bool Initialize(IntPtr xrGetInstanceProcAddr, ulong xrInstance, ulong xrSession, ulong xrAction)
        {
            if (xrGetInstanceProcAddr == IntPtr.Zero || xrInstance == 0 || xrSession == 0 || xrAction == 0)
            {
                WwiseLogger.Error("[AkWwiseOpenXRMotionBridge] Invalid parameters: all handles must be non-zero");
                return false;
            }

            try
            {
                IntPtr xrResultToString = GetOpenXRFunction(xrGetInstanceProcAddr, xrInstance, "xrResultToString");
                IntPtr xrEnumerateInstanceExtensionProperties = GetOpenXRFunction(xrGetInstanceProcAddr, xrInstance, "xrEnumerateInstanceExtensionProperties");
                IntPtr xrStringToPath = GetOpenXRFunction(xrGetInstanceProcAddr, xrInstance, "xrStringToPath");
                IntPtr xrApplyHapticFeedback = GetOpenXRFunction(xrGetInstanceProcAddr, xrInstance, "xrApplyHapticFeedback");

                if (xrResultToString == IntPtr.Zero || xrEnumerateInstanceExtensionProperties == IntPtr.Zero || xrStringToPath == IntPtr.Zero || xrApplyHapticFeedback == IntPtr.Zero)
                {
                    WwiseLogger.Error("[AkWwiseOpenXRMotionBridge] Failed to retrieve required OpenXR function pointers");
                    return false;
                }

                AkMotionInitializeOpenXRFunctions(xrResultToString, xrEnumerateInstanceExtensionProperties, xrStringToPath, xrGetInstanceProcAddr, xrApplyHapticFeedback);
                WwiseLogger.Verbose("[AkWwiseOpenXRMotionBridge] Passed OpenXR function pointers to AkMotion");

                AkMotionSetOpenXRHapticAction(xrInstance, xrSession, xrAction);
                WwiseLogger.Verbose("[AkWwiseOpenXRMotionBridge] Passed OpenXR handles to AkMotion");

                return true;
            }
            catch (Exception ex)
            {
                WwiseLogger.LogFormat(LogLevel.Error, "[AkWwiseOpenXRMotionBridge] Exception during initialization: {0}", ex.Message);
                return false;
            }
        }
    }
}

#endif // AK_ENABLE_OPENXR
