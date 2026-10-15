using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.Rendering;

/// <summary>
/// Handles proper teleportation between set portals and rendering of the visuals from its side.
/// </summary>
public class Teleporter : MonoBehaviour
{
    [SerializeField, Tooltip("Assign the portal that sends you back the the portal you came though (like your orange to your blue)")]
    private Teleporter m_linkedPortal;
    [SerializeField] 
    private Camera m_teleporterCamera;

    public bool IsPortalReady { get; set; }

    #region Unity Messages
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPortalReady)
        {
            return;
        }

        XROrigin player = other.GetComponentInParent<XROrigin>();

        if (player == null)
        {
            return;
        }

        Teleport(player);
    }
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += UpdateCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= UpdateCamera;
    }

    #endregion

    // Thank you Unity people
    // Source: https://www.youtube.com/watch?v=TkzASwVgnj8
    private void UpdateCamera(ScriptableRenderContext context, Camera camera)
    {
        if ((camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView) && camera.tag != m_teleporterCamera.tag)
        {
            m_teleporterCamera.projectionMatrix = camera.projectionMatrix; // Match matrices

            var relativePosition = transform.InverseTransformPoint(camera.transform.position);
            relativePosition = Vector3.Scale(relativePosition, new Vector3(-1, 1, -1));
            m_teleporterCamera.transform.position = m_linkedPortal.transform.TransformPoint(relativePosition);

            var relativeRotation = transform.InverseTransformDirection(camera.transform.forward);
            relativeRotation = Vector3.Scale(relativeRotation, new Vector3(-1, 1, -1));
            m_teleporterCamera.transform.forward = m_linkedPortal.transform.TransformDirection(relativeRotation);
        }
    }

    void Teleport(XROrigin player)
    {
        IsPortalReady = false;

        if (m_linkedPortal != null)
        {
            m_linkedPortal.IsPortalReady = false;
        } 

        // This is used to find where the player is compared to the portal so it can have a seamless teleport
        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0;

        // If portal A is rotated in a different direction then portal B, the offset above is calculated
        Quaternion rotation = m_linkedPortal.transform.rotation * Quaternion.Inverse(transform.rotation);
        offset = rotation * offset;

        // Update what the player's new position should be based on the offset calculated above
        Vector3 newPosition = m_linkedPortal.transform.position + offset;

        // Make sure th player is facing the same direction they were facing when entering A as they exit from B
        Quaternion newRotation = rotation * player.transform.rotation;

        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.SetPositionAndRotation(newPosition, newRotation);

        if (controller != null)
        {
            controller.enabled = true;
        }

        // Makes sure the player can't get teleported back immediately since destionation and portals are stacked on each other
        Invoke("ResetPortal", 0.3f);

        if (m_linkedPortal != null)
        {
            m_linkedPortal.Invoke("ResetPortal", 0.3f);
        }
    }

    void ResetPortal()
    {
        IsPortalReady = true;
    }
}