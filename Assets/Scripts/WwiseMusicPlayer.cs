using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public class WwiseMusicPlayer : MonoBehaviour
{
    [SerializeField] private AK.Wwise.Event musicEvent;
    private uint playingId;

    private void Start()
    {
        // AkBank runs earlier, at execution order -75.
        if (musicEvent == null || !musicEvent.IsValid())
        {
            Debug.LogError("Assign the Music event to WwiseMusicPlayer.", this);
            return;
        }

        AkUnitySoundEngine.SetState("PlayerLife", "Alive");
        AkUnitySoundEngine.SetState("Music_State", "Gameplay");
        PlayTower();
        playingId = musicEvent.Post(gameObject);
        if (playingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
            Debug.LogError("Music could not start. Check that the Music SoundBank loaded.", this);
    }

    [ContextMenu("Music/Explore")]
    public void PlayExplore()
    {
        if (Application.isPlaying)
            AkUnitySoundEngine.SetSwitch("Gameplay_Status", "Explore", gameObject);
    }

    [ContextMenu("Music/Tower")]
    public void PlayTower()
    {
        if (Application.isPlaying)
            AkUnitySoundEngine.SetSwitch("Gameplay_Status", "Tower", gameObject);
    }

    private void OnDestroy()
    {
        // AkGameObj may already be unregistered when Unity destroys this component.
        // Stop only our playback instance, without using the destroyed game object.
        if (playingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID && AkUnitySoundEngine.IsInitialized())
            AkUnitySoundEngine.ExecuteActionOnPlayingID(AkActionOnEventType.AkActionOnEventType_Stop, playingId);
        playingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    }
}
