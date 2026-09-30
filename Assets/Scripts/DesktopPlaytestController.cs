using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

[RequireComponent(typeof(CharacterController), typeof(XROrigin))]
public class DesktopPlaytestController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Min(1f)] private float eyeHeight = 1.65f;

    private readonly List<Behaviour> suspendedComponents = new List<Behaviour>();
    private CharacterController character;
    private Transform view;
    private Vector3 savedViewPosition;
    private Quaternion savedViewRotation;
    private Vector3 savedCenter;
    private float savedHeight;
    private float pitch;
    private float verticalSpeed;
    private bool desktopActive;
    private bool ownsCursor;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        var camera = GetComponent<XROrigin>().Camera;
        if (camera == null)
        {
            Debug.LogError("Desktop playtesting requires the XR Origin camera.", this);
            enabled = false;
            return;
        }

        view = camera.transform;
    }

    private void Update()
    {
        bool useDesktop = !Application.isMobilePlatform && !XRSettings.isDeviceActive;
        if (useDesktop != desktopActive)
        {
            if (useDesktop)
                BeginDesktop();
            else
                EndDesktop();
        }

        if (!desktopActive || !Application.isFocused || !character.enabled)
            return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            ReleaseCursor();
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ownsCursor = true;
        }

        Vector2 movement = Vector2.zero;
        if (ownsCursor && Cursor.lockState == CursorLockMode.Locked)
        {
            if (mouse != null)
            {
                Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            if (keyboard != null)
            {
                movement.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                movement.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                movement = Vector2.ClampMagnitude(movement, 1f);
            }
        }

        if (character.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        Vector3 velocity = (transform.right * movement.x + transform.forward * movement.y) * moveSpeed;
        velocity.y = verticalSpeed;
        CollisionFlags collisions = character.Move(velocity * Time.deltaTime);
        if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;
    }

    private void BeginDesktop()
    {
        savedViewPosition = view.localPosition;
        savedViewRotation = view.localRotation;
        savedCenter = character.center;
        savedHeight = character.height;

        foreach (var provider in GetComponentsInChildren<LocomotionProvider>(true))
            Suspend(provider);
        foreach (var body in GetComponentsInChildren<XRBodyTransformer>(true))
            Suspend(body);
        Suspend(view.GetComponent<TrackedPoseDriver>());

        view.position = transform.TransformPoint(Vector3.up * eyeHeight);
        view.localRotation = Quaternion.identity;
        character.height = eyeHeight + 0.1f;
        character.center = Vector3.up * (character.height * 0.5f);
        pitch = 0f;
        verticalSpeed = 0f;
        desktopActive = true;
    }

    private void Suspend(Behaviour component)
    {
        if (component != null && component.enabled)
        {
            suspendedComponents.Add(component);
            component.enabled = false;
        }
    }

    private void EndDesktop()
    {
        if (!desktopActive)
            return;

        ReleaseCursor();
        view.localPosition = savedViewPosition;
        view.localRotation = savedViewRotation;
        character.height = savedHeight;
        character.center = savedCenter;
        for (int i = suspendedComponents.Count - 1; i >= 0; i--)
        {
            if (suspendedComponents[i] != null)
                suspendedComponents[i].enabled = true;
        }
        suspendedComponents.Clear();
        desktopActive = false;
    }

    private void ReleaseCursor()
    {
        if (!ownsCursor)
            return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ownsCursor = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            ReleaseCursor();
    }

    private void OnDisable()
    {
        EndDesktop();
    }
}
