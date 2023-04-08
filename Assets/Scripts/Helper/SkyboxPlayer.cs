using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class SkyboxPlayer : MonoBehaviour
{
    VideoPlayer videoPlayer;

    /// <summary>
    /// Video links or paths to play. <br/>
    /// Use 'http://' or 'https://' for links, or '/' for paths.
    /// </summary>
    public string[] VideoLinksOrPaths;

    public int DefaultVideoIndex = 0;

    public Texture2D DefaultTexture;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (DefaultTexture != null)
        {
            Graphics.Blit(DefaultTexture, videoPlayer.targetTexture);
        }
        StartVideo(DefaultVideoIndex);
        videoPlayer.Play();
    }

    private void StartVideo(int id)
    {
        string videoLinkOrPath = VideoLinksOrPaths[id];
        if (videoLinkOrPath.StartsWith("http"))
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoLinkOrPath;
        }
        else
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = Resources.Load<VideoClip>(videoLinkOrPath);
        }
    }

    public void PlayVideo(int id)
    {
        videoPlayer.Stop();
        StartVideo(id);
        videoPlayer.Play();
    }
}
