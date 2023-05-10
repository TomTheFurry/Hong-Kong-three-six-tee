using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class SkyboxPlayer : MonoBehaviour
{
    VideoPlayer videoPlayer;
    public Texture2D DefaultTexture;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (DefaultTexture != null)
        {
            Graphics.Blit(DefaultTexture, videoPlayer.targetTexture);
        }
    }

    public void PlayVideo(string path)
    {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = path;
        videoPlayer.Play();
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
    }

    public void BlitLoadingTexture(Texture tex)
    {
        if (tex == null)
        {
            Debug.LogWarning("null texture detected. Loading default texture");
            return;
        }

        if (videoPlayer.isPrepared)
        {
            Debug.Log("Skip blitting loading texture as vid player has completed preparing the vid");
        }
        else
        {
            Debug.Log("Blitting loading texture");
            Graphics.Blit(tex, videoPlayer.targetTexture);
        }
    }

    private SurroundSound ss;
    private bool resetIsPlaying = false;
    private void Update()
    {
        SurroundSound s = FindObjectOfType<SurroundSound>();
        if (s == ss)
        {
            videoPlayer.enabled = true;
            if (resetIsPlaying)
            {
                videoPlayer.Play();
            }
            return;
        }
        ss = s;
        resetIsPlaying = videoPlayer.isPlaying;
        videoPlayer.enabled = false;

        if (ss != null)
        {
            videoPlayer.SetTargetAudioSource(0, ss.audioSource);
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            videoPlayer.SetTargetAudioSource(0, null);
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                videoPlayer.Play();
            }
        }
    }
}
