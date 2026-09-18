using UnityEngine;
using UnityEngine.InputSystem;

public class ManipulateSpell : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private Transform leftHandForcePoint;
    [SerializeField] private Transform rightHandForcePoint;

    [Header("Input")]
    [SerializeField] private InputActionReference leftGripAction;
    [SerializeField] private InputActionReference rightGripAction;
    [SerializeField] private InputActionReference leftTriggerAction;

    [Header("Targeting")]
    [SerializeField] private float targetingDistance = 3f;
    [SerializeField] private float targetingRadius = 0.25f;

    [Header("Manipulation")]
    [SerializeField] private float movementMultiplier = 1f;
    [SerializeField] private float depthMovementMultiplier = 1f;

    private Rigidbody targetBody;

    private bool leftGripHeld;
    private bool rightGripHeld;

    private Vector3 leftHandStartPosition;
    private Vector3 rightHandNeutralPosition;

    private bool originalIsKinematic;

    private Quaternion previousHandRotation;

    void OnEnable()
    {
        leftGripAction.action.Enable();
        rightGripAction.action.Enable();
        leftTriggerAction.action.Enable();
    }

    void OnDisable()
    {
        leftGripAction.action.Disable();
        rightGripAction.action.Disable();
        leftTriggerAction.action.Disable();
    }

    void Update()
    {
        bool newLeftGripState =
            leftGripAction.action.IsPressed();

        bool newRightGripState =
            rightGripAction.action.IsPressed();

        if (newLeftGripState && !leftGripHeld)
        {
            TryGrabObject();
        }

        if (!newLeftGripState && leftGripHeld)
        {
            ReleaseObject();
        }

        leftGripHeld = newLeftGripState;

        if (newRightGripState && !rightGripHeld)
        {
            rightHandNeutralPosition =
                rightHandForcePoint.position;
        }

        rightGripHeld = newRightGripState;

        if (leftTriggerAction.action.WasPressedThisFrame())
        {
            if (targetBody != null)
            {
                targetBody.isKinematic =
                    originalIsKinematic;
            }

            gameObject.SetActive(false);
            return;
        }

        if (targetBody == null)
            return;

        if (leftGripHeld)
        {
            MoveObject();
            RotateObject();
        }

        if (rightGripHeld)
        {
            MoveObjectDepth();
        }
    }

    private void TryGrabObject()
    {
        RaycastHit hit;

        if (Physics.SphereCast(
            leftHandForcePoint.position,
            targetingRadius,
            leftHandForcePoint.forward,
            out hit,
            targetingDistance))
        {
            Rigidbody body = hit.rigidbody;

            if (body != null)
            {
                targetBody = body;

                originalIsKinematic = body.isKinematic;

                body.isKinematic = true;

                leftHandStartPosition =
                    leftHandForcePoint.position;

                previousHandRotation =
                    leftHandForcePoint.rotation;
            }
        }
    }

    private void MoveObject()
    {
        Vector3 handMovement =
            leftHandForcePoint.position -
            leftHandStartPosition;

        targetBody.MovePosition(
            targetBody.position +
            handMovement * movementMultiplier
        );

        leftHandStartPosition =
            leftHandForcePoint.position;
    }

    private void RotateObject()
    {
        Vector3 rotationAxis =
            leftHandForcePoint.forward;

        Quaternion handRotationDelta =
            leftHandForcePoint.rotation *
            Quaternion.Inverse(previousHandRotation);

        handRotationDelta.ToAngleAxis(
            out float angle,
            out Vector3 axis
        );

        float direction =
            Vector3.Dot(axis, rotationAxis);

        if (direction < 0)
        {
            angle = -angle;
        }

        targetBody.MoveRotation(
            Quaternion.AngleAxis(
                angle,
                rotationAxis
            ) * targetBody.rotation
        );

        previousHandRotation =
            leftHandForcePoint.rotation;
    }

    private void MoveObjectDepth()
    {
        Vector3 playerToObject =
            targetBody.position -
            leftHandForcePoint.position;

        Vector3 depthDirection =
            playerToObject.normalized;

        Vector3 handOffset =
            rightHandForcePoint.position -
            rightHandNeutralPosition;

        float depthOffset =
            Vector3.Dot(
                handOffset,
                depthDirection
            );

        float distanceFromNeutral =
            Mathf.Abs(depthOffset);

        float deadZone = 0.02f;

        if (distanceFromNeutral <= deadZone)
            return;

        float effectiveDistance =
            distanceFromNeutral - deadZone;

        float movementSpeed =
            effectiveDistance *
            depthMovementMultiplier;

        float direction =
            Mathf.Sign(depthOffset);

        targetBody.MovePosition(
            targetBody.position +
            depthDirection *
            direction *
            movementSpeed *
            Time.deltaTime
        );
    }

    private void ReleaseObject()
    {
        if (targetBody != null)
        {
            targetBody.isKinematic =
                originalIsKinematic;

            targetBody = null;
        }
    }
}