using UnityEngine;
using UnityEngine.XR;

public class RiftSProjectileShooter : MonoBehaviour
{
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform launchPoint;
    [SerializeField] XRNode inputSource = XRNode.LeftHand;
    [SerializeField] float launchSpeed = 12f;
    [SerializeField] float fireCooldown = 0.25f;

    InputDevice inputDevice;
    bool wasPressed;
    float nextFireTime;

    void Update()
    {
        if (!inputDevice.isValid)
        {
            inputDevice = InputDevices.GetDeviceAtXRNode(inputSource);
        }

        if (!inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool isPressed))
        {
            return;
        }

        if (isPressed && !wasPressed && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireCooldown;
        }

        wasPressed = isPressed;
    }

    void Fire()
    {
        if (projectilePrefab == null || launchPoint == null)
        {
            Debug.LogWarning("RiftSProjectileShooter needs a projectile prefab and launch point.", this);
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, launchPoint.position, launchPoint.rotation);

        Rigidbody body = projectile.GetComponentInChildren<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = false;
            body.detectCollisions = true;
            body.linearVelocity = launchPoint.forward * launchSpeed;
            body.WakeUp();
        }
        else
        {
            Debug.LogWarning("The projectile prefab needs a Rigidbody component.", projectile);
        }
    }
}