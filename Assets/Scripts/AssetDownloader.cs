using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-5)]
public class AssetDownloader : MonoBehaviour
{
    public string[] image360ZipUrls;
    public string[] imageZipUrls;
    public string[] viddo360ZipUrls;

    private string img360CachePath;
    private string imgCachePath;
    private string vid360CachePath;

    public TaskCompletionSource<bool> imgReadyTask = new();
    public TaskCompletionSource<bool> img360ReadyTask = new();
    public TaskCompletionSource<bool> vid360ReadyTask = new();


    void Start()
    {
        img360CachePath = Application.persistentDataPath + "/img360";
        imgCachePath = Application.persistentDataPath + "/img";
        vid360CachePath = Application.persistentDataPath + "/vid360";

        StartCoroutine(LoadCaches());
    }

    private bool isFolderValid(string path)
    {
        if (!Directory.Exists(path)) return false;
        string[] files = Directory.GetFiles(path);
        if (files.Length == 0) return false;
        return true;
    }

    private IEnumerator LoadCaches()
    {
        if (!isFolderValid(imgCachePath))
        {
            Debug.Log($"imgCachePath not found. Downloading cache to {imgCachePath}...");
            yield return DownloadAndUnzipFiles(imgCachePath, imageZipUrls);
            Debug.Log("Download complete");
        }
        else
        {
            Debug.Log($"imgCachePath found at {imgCachePath}");
        }
        imgReadyTask.SetResult(true);
        if (!isFolderValid(img360CachePath))
        {
            Debug.Log($"img360CachePath not found. Downloading cache to {img360CachePath}...");
            yield return DownloadAndUnzipFiles(img360CachePath, image360ZipUrls);
            Debug.Log("Download complete");
        }
        else
        {
            Debug.Log($"img360CachePath found at {img360CachePath}");
        }
        img360ReadyTask.SetResult(true);
        if (!isFolderValid(vid360CachePath))
        {
            Debug.Log($"vid360CachePath not found. Downloading cache to {vid360CachePath}...");
            yield return DownloadAndUnzipFiles(vid360CachePath, viddo360ZipUrls);
            Debug.Log("Download complete");
        }
        else
        {
            Debug.Log($"vid360CachePath found at {vid360CachePath}");
        }
        vid360ReadyTask.SetResult(true);
    }

    private static IEnumerator DownloadAndUnzipFiles(string target, string[] urls)
    {
        string tmp = Path.Combine(Application.persistentDataPath, "tmp");
        if (Directory.Exists(tmp))
        {
            Debug.LogWarning("tmp folder seems to have failed to be deleted in previous run. Deleting it now...");
            Directory.Delete(tmp, true);
        }
        Directory.CreateDirectory(tmp);
        if (Directory.Exists(target)) Directory.Delete(target, true);
        Directory.CreateDirectory(target);
        try
        {
            foreach (string url in urls)
            {
                using UnityWebRequest www = UnityWebRequest.Get(url);
                Debug.Log($"Starting download on {url}...");
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    Debug.Log($"Completed download on {url}. Unzipping...");
                    // unzip file
                    using MemoryStream ms = new MemoryStream(www.downloadHandler.data);
                    using ZipArchive zip = new ZipArchive(ms);
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        string entryPath = Path.Combine(target, entry.FullName);
                        if (File.Exists(entryPath)) File.Delete(entryPath);
                        entry.ExtractToFile(entryPath);
                    }
                }
            }
        }
        finally
        {
            Directory.Delete(tmp, true);
        }
    }

    public Texture2D GetImg(string filename)
    {
        if (!imgReadyTask.Task.IsCompleted)
        {
            Debug.LogWarning($"Img cache not ready yet. Can't read {filename}");
            return null;
        }

        if (imgReadyTask.Task.IsFaulted)
        {
            Debug.LogError(imgReadyTask.Task.Exception);
        }

        string path = Path.Combine(imgCachePath, filename);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }
        else
        {
            Debug.LogWarning("File not found: " + path);
            return null;
        }
    }

    public Texture2D GetImg360(string filename)
    {
        if (!img360ReadyTask.Task.IsCompleted)
        {
            Debug.LogWarning($"Img360 cache not ready yet. Can't read {filename}");
            return null;
        }

        if (img360ReadyTask.Task.IsFaulted)
        {
            Debug.LogError(img360ReadyTask.Task.Exception);
        }

        string path = Path.Combine(img360CachePath, filename);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            return tex;
        }
        else
        {
            Debug.LogWarning("File not found: " + path);
            return null;
        }
    }

    public string GetVidPath(string filename)
    {
        if (!vid360ReadyTask.Task.IsCompleted)
        {
            Debug.LogWarning($"Vid360 cache not ready yet. Can't read {filename}");
            return null;
        }

        if (vid360ReadyTask.Task.IsFaulted)
        {
            Debug.LogError(vid360ReadyTask.Task.Exception);
        }

        string path = Path.Combine(vid360CachePath, filename);
        if (File.Exists(path))
        {
            return path;
        }
        else
        {
            Debug.LogWarning("File not found: " + path);
            return null;
        }
    }

}
