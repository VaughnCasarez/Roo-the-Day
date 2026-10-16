using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTeleport : MonoBehaviour
{
    [SerializeField] private string sceneName;
    private void OnTriggerEnter(Collider other)
    {
        XROrigin player = other.GetComponentInParent<XROrigin>();
        if (player != null)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
