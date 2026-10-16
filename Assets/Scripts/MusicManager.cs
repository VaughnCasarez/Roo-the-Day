using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> musicClips = new();
    [SerializeField] private AudioClip startingClip;

    private readonly List<AudioClip> playlist = new();
    private AudioSource audioSource;
    private AudioClip lastPlayedClip;
    private int nextClipIndex;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        PlayNextClip();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            SkipSong();
        }
        else if (!audioSource.isPlaying)
        {
            PlayNextClip();
        }
    }

    public void SkipSong()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        PlayNextClip();
    }

    private void PlayNextClip()
    {
        if (musicClips.Count == 0)
        {
            return;
        }

        if (nextClipIndex >= playlist.Count)
        {
            RebuildPlaylist();
        }

        if (playlist.Count == 0)
        {
            return;
        }

        AudioClip clip = playlist[nextClipIndex++];
        lastPlayedClip = clip;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void RebuildPlaylist()
    {
        playlist.Clear();

        foreach (AudioClip clip in musicClips)
        {
            if (clip != null && !playlist.Contains(clip))
            {
                playlist.Add(clip);
            }
        }

        for (int index = playlist.Count - 1; index > 0; index--)
        {
            int randomIndex = Random.Range(0, index + 1);
            (playlist[index], playlist[randomIndex]) = (playlist[randomIndex], playlist[index]);
        }

        if (playlist.Count > 1 && playlist[0] == lastPlayedClip)
        {
            (playlist[0], playlist[1]) = (playlist[1], playlist[0]);
        }

        if (lastPlayedClip == null && startingClip != null && playlist.Contains(startingClip))
        {
            playlist.Remove(startingClip);
            playlist.Insert(0, startingClip);
        }

        nextClipIndex = 0;
    }
}
