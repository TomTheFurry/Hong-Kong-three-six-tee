using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

[DefaultExecutionOrder(-4)]
public class AsyncSkyboxManager : MonoBehaviour
{
    public static AsyncSkyboxManager Instance { get; private set; }

    private static string toImg360Name(string baseName) => baseName + "_360.jpg";
    private static string toVid360Name(string baseName) => baseName + ".mp4";

    SkyboxPlayer skyboxPlayer;
    AssetDownloader assetDownloader;

    class TileTasks
    {
        public string name;
        public Task<Texture2D> img360Task;
        public Task<string> vid360Task;
        private CancellationTokenSource canceller = new();
        SkyboxPlayer skyboxPlayer;

        public TileTasks(string assetName, AsyncSkyboxManager self)
        {
            name = assetName;
            skyboxPlayer = self.skyboxPlayer;
            AssetDownloader ad = self.assetDownloader;
            Task t = ad.img360ReadyTask.Task;
            if (t.IsCompleted)
            {
                img360Task = Task.FromResult(ad.GetImg360(toImg360Name(assetName)));
            }
            else
            {
                img360Task = t.ContinueWith(_ => ad.GetImg360(toImg360Name(assetName)), canceller.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            t = ad.vid360ReadyTask.Task;
            if (t.IsCompleted)
            {
                vid360Task = Task.FromResult(ad.GetVidPath(toVid360Name(assetName)));
            }
            else
            {
                vid360Task = t.ContinueWith(_ => ad.GetVidPath(toVid360Name(assetName)), canceller.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        public void Dispose()
        {
            canceller.Cancel();
        }

        public void Update()
        {
            if (img360Task != null && img360Task.IsCompleted)
            {
                skyboxPlayer.StopVideo();
                skyboxPlayer.BlitLoadingTexture(img360Task.Result);
                img360Task = null;
            }
            if (vid360Task != null && vid360Task.IsCompleted)
            {
                skyboxPlayer.PlayVideo(vid360Task.Result);
                vid360Task = null;
            }
        }
    }

    TileTasks currentTileTasks = null;
    bool isDefTask = false;

    public string[] defaults;

    void Start()
    {
        Instance = this;
        skyboxPlayer = FindObjectOfType<SkyboxPlayer>();
        assetDownloader = FindObjectOfType<AssetDownloader>();
        SwitchToTile(null);
        Random.InitState(System.DateTime.Now.Millisecond);
    }

    public void SwitchToTile(GameTile tile)
    {
        string assertName = tile?.AssetName ?? defaults[Random.Range(0, defaults.Length)];

        if (tile == null && isDefTask) return;

        if (currentTileTasks != null && currentTileTasks.name == assertName) return;
        currentTileTasks?.Dispose();

        isDefTask = tile == null;
        currentTileTasks = new TileTasks(assertName, this);
    }


    void Update()
    {
        if (currentTileTasks != null)
        {
            currentTileTasks.Update();
        }
    }

    void OnDestroy()
    {
        if (currentTileTasks != null)
        {
            currentTileTasks.Dispose();
            currentTileTasks = null;
        }
    }
}
