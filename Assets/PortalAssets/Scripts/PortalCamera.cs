using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

public class PortalCamera : MonoBehaviour
{
    [SerializeField] private Portal[] portals = new Portal[2];
    [SerializeField] private Camera portalCamera;
    [SerializeField] private int iterations = 2; // 7 is far too expensive in VR (x2 eyes)

    private static readonly int LeftTexId = Shader.PropertyToID("_LeftEyeTex");
    private static readonly int RightTexId = Shader.PropertyToID("_RightEyeTex");
    private static readonly int ForcedEyeId = Shader.PropertyToID("_ForcedEye");

    // [portal index, eye index]
    private RenderTexture[,] eyeTextures;
    private Camera mainCamera;
    private int texWidth, texHeight;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();

        portalCamera.enabled = false;
        portalCamera.stereoTargetEye = StereoTargetEyeMask.None; // portal cam is mono, we feed it per-eye data

        eyeTextures = new RenderTexture[portals.Length, 2];
    }

    private void OnEnable() => RenderPipeline.beginCameraRendering += UpdateCamera;
    private void OnDisable() => RenderPipeline.beginCameraRendering -= UpdateCamera;

    private void UpdateCamera(ScriptableRenderContext src, Camera cam)
    {
        if (cam != mainCamera) return; // ignore portalCamera, scene view, etc.
        if (!EnsureTextures()) return;

        bool stereo = XRSettings.enabled && mainCamera.stereoEnabled;

        for (int p = 0; p < portals.Length; p+=2)
        {
            if (!portals[p].Renderer.isVisible) continue;

            Portal inPortal = portals[p];
            Portal outPortal = portals[p+1];

            if (stereo)
            {
                RenderEye(inPortal, outPortal, p, Camera.StereoscopicEye.Left, 0, src);
                RenderEye(inPortal, outPortal, p, Camera.StereoscopicEye.Right, 1, src);

                RenderEye(outPortal, inPortal, p+1, Camera.StereoscopicEye.Left, 0, src);
                RenderEye(outPortal, inPortal, p+1, Camera.StereoscopicEye.Right, 1, src);
            }
            else
            {
                // flatscreen fallback: reuse the left slot
                RenderEye(inPortal, outPortal, p, Camera.StereoscopicEye.Left, 0, src, mono: true);
                RenderEye(outPortal, inPortal, p+1, Camera.StereoscopicEye.Left, 0, src, mono: true);
            }
        }
    }

    private void RenderEye(Portal inPortal, Portal outPortal, int portalIdx,
                           Camera.StereoscopicEye eye, int eyeIdx,
                           ScriptableRenderContext src, bool mono = false)
    {
        // Get this eye's world pose + projection from the main camera.
        Matrix4x4 view = mono ? mainCamera.worldToCameraMatrix
                              : mainCamera.GetStereoViewMatrix(eye);
        Matrix4x4 proj = mono ? mainCamera.projectionMatrix
                              : mainCamera.GetStereoProjectionMatrix(eye);

        Matrix4x4 camToWorld = view.inverse;
        Vector3 eyePos = camToWorld.GetColumn(3);
        Quaternion eyeRot = Quaternion.LookRotation(-camToWorld.GetColumn(2), camToWorld.GetColumn(1));

        portalCamera.targetTexture = eyeTextures[portalIdx, eyeIdx];

        // Tell this portal's material to show the matching eye texture while the
        // (mono) portal camera is rendering nested portals.
        inPortal.Renderer.sharedMaterial.SetFloat(ForcedEyeId, eyeIdx);
        outPortal.Renderer.sharedMaterial.SetFloat(ForcedEyeId, eyeIdx);

        for (int i = iterations - 1; i >= 0; --i)
            RenderIteration(inPortal, outPortal, i, eyePos, eyeRot, proj, src);

        inPortal.Renderer.sharedMaterial.SetFloat(ForcedEyeId, -1f);
        outPortal.Renderer.sharedMaterial.SetFloat(ForcedEyeId, -1f);
    }

    private void RenderIteration(Portal inPortal, Portal outPortal, int iterationID,
                                 Vector3 startPos, Quaternion startRot, Matrix4x4 eyeProj,
                                 ScriptableRenderContext src)
    {
        Transform inT = inPortal.transform;
        Transform outT = outPortal.transform;
        Transform camT = portalCamera.transform;

        camT.SetPositionAndRotation(startPos, startRot);

        for (int i = 0; i <= iterationID; ++i)
        {
            Vector3 relPos = inT.InverseTransformPoint(camT.position);
            relPos = Quaternion.Euler(0f, 180f, 0f) * relPos;
            camT.position = outT.TransformPoint(relPos);

            Quaternion relRot = Quaternion.Inverse(inT.rotation) * camT.rotation;
            relRot = Quaternion.Euler(0f, 180f, 0f) * relRot;
            camT.rotation = outT.rotation * relRot;
        }

        portalCamera.projectionMatrix = eyeProj;

        // Same plane as the original flatscreen code: fixed, no side flipping, no fallback.
        Plane p = new Plane(-outT.forward, outT.position);
        Vector4 clipWorld = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        Vector4 clipCam = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipWorld;

        portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix(clipCam);

        UniversalRenderPipeline.RenderSingleCamera(src, portalCamera);
    }

    private void OnDestroy()
    {
        foreach (var rt in eyeTextures) if (rt) rt.Release();
    }

    private bool TryGetEyeSize(out int w, out int h)
    {
        w = h = 0;

        // Preferred: ask the XR display subsystem directly.
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        foreach (var d in displays)
        {
            if (!d.running || d.GetRenderPassCount() == 0) continue;

            d.GetRenderPass(0, out XRDisplaySubsystem.XRRenderPass pass);
            var desc = pass.renderTargetDesc;
            if (desc.width > 0 && desc.height > 0)
            {
                w = desc.width;
                h = desc.height;
                return true;
            }
        }

        // Fallback: legacy API, only trust it if it's positive.
        if (XRSettings.enabled && XRSettings.eyeTextureWidth > 0)
        {
            w = XRSettings.eyeTextureWidth;
            h = XRSettings.eyeTextureHeight;
            return true;
        }

        // Flatscreen fallback.
        if (!XRSettings.enabled)
        {
            w = mainCamera.pixelWidth;
            h = mainCamera.pixelHeight;
            return w > 0 && h > 0;
        }

        return false; // XR not ready yet
    }

    private bool EnsureTextures()
    {
        if (!TryGetEyeSize(out int w, out int h)) return false;
        if (eyeTextures[0, 0] != null && w == texWidth && h == texHeight) return true;

        texWidth = w;
        texHeight = h;

        for (int p = 0; p < portals.Length; p++)
        {
            for (int e = 0; e < 2; e++)
            {
                if (eyeTextures[p, e] != null) eyeTextures[p, e].Release();

                eyeTextures[p, e] = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                eyeTextures[p, e].Create();
            }

            var mat = portals[p].Renderer.material;
            mat.SetTexture(LeftTexId, eyeTextures[p, 0]);
            mat.SetTexture(RightTexId, eyeTextures[p, 1]);
            mat.SetFloat(ForcedEyeId, -1f);
        }
        return true;
    }
}