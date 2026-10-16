using UnityEngine;

public class SlashSpell : MonoBehaviour
{
    [SerializeField] GameObject slashPrefab;
    [SerializeField] Transform launchPoint;
    [SerializeField] MovementRecognizer movementRecognizer;
    [SerializeField] float launchSpeed = 12f;
    [SerializeField] float lifetime = 5f;

    void OnEnable()
    {
        FireSlash();
        // Reset the spell controller so SpellSpawner can activate it again
        // the next time the Slash gesture is recognized.
        gameObject.SetActive(false);
    }

    void FireSlash()
    {
        if (
            slashPrefab == null
            || launchPoint == null
            || movementRecognizer == null
        )
        {
            Debug.LogWarning(
                "SlashSpell is missing a required reference.",
                this
            );

            return;
        }

        // Use the wand's position and orientation from the middle of the gesture,
        // rather than its position when the trigger is released.
        Vector3 spawnPosition =
            movementRecognizer.GetGestureMidpoint();

        Quaternion spawnRotation =
            movementRecognizer.GetGestureMidRotation();

        GameObject slash = Instantiate(
            slashPrefab,
            spawnPosition,
            spawnRotation
        );

        Rigidbody body =
            slash.GetComponentInChildren<Rigidbody>();

        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = false;
            body.detectCollisions = true;

            body.linearVelocity =
                spawnRotation
                * Vector3.forward
                * launchSpeed;

            body.WakeUp();
        }
        else
        {
            Debug.LogWarning(
                "The slash prefab needs a Rigidbody component.",
                slash
            );
        }

        Destroy(slash, lifetime);
    }
}