using UnityEngine;
using Unity.XR.CoreUtils;

public class Teleporter : MonoBehaviour
{
    [SerializeField] Transform destination; // Assign your destination to calculate where player should teleport
    [SerializeField] Teleporter linkedPortal; // Assign the portal that sends you back the the portal you came though (like your orange to your blue)

    bool portalReady = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!portalReady)
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

    void Teleport(XROrigin player)
    {
        portalReady = false;

        if (linkedPortal != null)
        {
            linkedPortal.portalReady = false;
        } 

        // This is used to find where the player is compared to the portal so it can have a seamless teleport
        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0;

        // If portal A is rotated in a different direction then portal B, the offset above is calculated
        Quaternion rotation = destination.rotation * Quaternion.Inverse(transform.rotation);
        offset = rotation * offset;

        // Update what the player's new position should be based on the offset calculated above
        Vector3 newPosition = destination.position + offset;

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

        if (linkedPortal != null)
        {
            linkedPortal.Invoke("ResetPortal", 0.3f);
        }
    }

    void ResetPortal()
    {
        portalReady = true;
    }
}