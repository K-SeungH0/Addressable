using System.Collections.Generic;
using System;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;


public class AddressableManager : MonoBehaviour
{
    private static AddressableManager instance;

    public const string URL = "https://s3.ap-northeast-2.amazonaws.com";
    public const string _bucketName = "이곳에 AWS Bucket 이름 입력";
    
    public Button allDownLoadButton;
    public Button checkCacheButton;
    public Button downAbortButton;

    public GameObject _content;
    public GameObject _defaultUIObject;

    public Text _totalSpace;
    public Text _usedSpace;
    public Text _availableSpace;
    public Slider _spaceSlider;

    private Dictionary<string, List<string>> _key = new Dictionary<string, List<string>>();
    private Dictionary<string, IResourceLocation> _resourceLocator = new Dictionary<string, IResourceLocation>();
    private HashSet<string> _mapHash = new HashSet<string>();
    private Queue<UnityEngine.Networking.UnityWebRequest> currentWebRequest = new Queue<UnityEngine.Networking.UnityWebRequest>();

    public AmazonS3Client _s3Client;
    public string _bundlePath;

    System.Action InitComplete;
    void Awake()
    {
        // 싱글톤
        if (null == instance)
            instance = this;
        else
            Destroy(gameObject);

        // AWS Init
        //_s3Client = new AmazonS3Client(_accessKey, _secretKey, RegionEndpoint.APNortheast2); 

    }
    public static AddressableManager Instance
    {
        get
        {
            if (null == instance)
                return null;
            return instance;
        }
    }
    private void DownAbortAdd()
    {
        Addressables.WebRequestOverride += DownloadAbort;
    }
    private void DownloadAbort(UnityEngine.Networking.UnityWebRequest req)
    {
        if (req.url != null && !req.url.Contains("unitybuiltinshaders"))
            currentWebRequest.Enqueue(req);
    }
    public void DownCancel()
    {
        Debug.LogWarning("다운로드 중단!");
        while (currentWebRequest.Count != 0)
        {
            currentWebRequest.Dequeue().Abort();
        }
    }
    private void Start()
    {
        _bundlePath = string.Format("{0}/AddressableMapData", Application.persistentDataPath);
        CacheFilePathInit();
        if (_totalSpace == null)
            _totalSpace = transform.Find("TotalSpace").GetComponent<Text>();
        if (_usedSpace == null)
            _usedSpace = transform.Find("UsedSpace").GetComponent<Text>();
        if (_availableSpace == null)
            _availableSpace = transform.Find("AvailableSpace").GetComponent<Text>();
        if (_spaceSlider == null)
            _spaceSlider = transform.Find("SpaceSlider").GetComponent<Slider>();
        AvailableSpace();

        MapUpLoadDataManager.Instance.CSVLoad();
        for (int i = 0; i < MapUpLoadDataManager.Instance._trackInfo.Count; i++)
        {
            _mapHash.Add(MapUpLoadDataManager.Instance._trackInfo[i]["eng"]);
        }
        InitComplete += DownAbortAdd;
        InitComplete += InitUI;
        InitComplete += CacheCheck;
        Addressables.InitializeAsync(true).Completed +=
        (handle) =>
        {
            Addressables.ClearResourceLocators();
            ChangeCataLog();
        };

        //foreach (var loadData in MapUpLoadDataManager.Instance._trackInfo)
        //{
        //    if (!_childObject.ContainsKey(loadData["groupname"]))
        //    {
        //        var tempGo = Instantiate(_defaultUIObject, _content.transform);
        //        tempGo.name = loadData["groupname"];
        //        tempGo.transform.Find("NameText").GetComponent<Text>().text = loadData["kor"];
        //        tempGo.GetComponent<MapDownLoadUI>()._key = loadData["groupname"];
        //        InitComplete += tempGo.GetComponent<MapDownLoadUI>().CheckDownloadFileSize;
        //        allDownLoadButton.onClick.AddListener(tempGo.GetComponent<MapDownLoadUI>().BundleDownLoad);
        //        var createButtons = tempGo.GetComponentsInChildren<CreateButton>();

        //        createButtons[0].name = loadData["groupname"] + loadData["time"];
        //        createButtons[0].GetComponentInChildren<RawImage>().texture = 
        //        createButtons[0].transform.Find("Text").GetComponent<Text>().text = string.Format("{0} {1} {2}", loadData["kor"], loadData["time"], loadData["version"]);
        //        _childObject.Add(loadData["groupname"], tempGo);
        //    }
        //    else
        //    {
        //        var tempGo = _childObject[loadData["groupname"]];
        //        var createButtons = tempGo.GetComponentsInChildren<CreateButton>();

        //        createButtons[1].name = loadData["groupname"] + loadData["time"];
        //        createButtons[1].GetComponentInChildren<RawImage>().texture = GetTrackImage(loadData["trackimg"]);
        //        createButtons[1].transform.Find("Text").GetComponent<Text>().text = string.Format("{0} {1} {2}", loadData["kor"], loadData["time"], loadData["version"]);
        //    }
        //}


    }
    private void InitUI()
    {
        foreach (var key in _key)
        {
            var go = Instantiate(_defaultUIObject, _content.transform);
            var goUI = go.GetComponent<MapDownLoadUI>();

            go.SetActive(true);
            go.name = key.Key;

            goUI._key = key.Key;
            goUI._trackIndex = key.Value;
            goUI.InitUI();
            goUI.CheckDownloadFileSize();
        }
    }
    public void ChangeCataLog()
    {
        string platform;
#if UNITY_ANDROID
        platform = "Android";
#elif UNITY_IOS
        platform = "iOS";
#else
        platform = "StandaloneWindows";
#endif
        //foreach (var key in _mapHash)
        //{
        //    if (key == "Daegu" || key == "GyeongJu")
        //    {
        //        string path = string.Format("{0}/{1}/{2}/{3}/catalog_{2}.json", URL, _bucketName, platform, key);
        //        //var path = string.Format("{0}/{1}/{2}/DEV/catalog_{2}.json", URL, _bucketName, platform);
        //        Debug.Log("path : " + path);
        //        var temp = Addressables.LoadContentCatalogAsync(path, true);

        //        temp.Completed +=
        //        (AsyncOperationHandle<IResourceLocator> handle) =>
        //        {
        //            foreach (var loc in handle.Result.Keys)
        //            {
        //                if (handle.Result.Locate(loc.ToString(), typeof(object), out var locations))
        //                {
        //                    // TODO 여기서 night 맵 없는애들 걸러짐 추가해야함.
        //                    if (_mapHash.Contains(loc as string))
        //                    {
        //                        List<string> tempList = new List<string>();

        //                        for (int i = 0; i < locations.Count; i++)
        //                        {
        //                            tempList.Add(locations[i].PrimaryKey);
        //                            if (!_resourceLocator.ContainsKey(locations[i].PrimaryKey))
        //                                _resourceLocator.Add(locations[i].PrimaryKey, locations[i]);
        //                        }

        //                        _key.Add(loc as string, tempList);
        //                    }
        //                }
        //            }
        //            Debug.LogFormat("AWS 서버 맵 갯수 : {0}", _resourceLocator.Count);
        //            Debug.Log("Key 개수 : " + _key.Count);

        //            foreach (var key in _key)
        //            {
        //                Debug.Log("key : " + key);
        //            }
        //        };
        //    }
        //}

        //var path = string.Format("{0}/{1}/{2}/{3}/catalog_{2}.json", URL, _bucketName, platform, "DEV");
        var path = string.Format("{0}/{1}/{2}/DEV/{3}/catalog_{2}_{3}.json", URL, _bucketName, platform, MapUpLoadDataManager.LoadAppVersion());
        Debug.Log("path : " + path);
        var temp = Addressables.LoadContentCatalogAsync(path, true);

        temp.Completed += (handle) =>
        {
            foreach (var loc in handle.Result.Keys)
            {
                if (handle.Result.Locate(loc.ToString(), typeof(object), out var locations))
                {
                    // TODO 여기서 night 맵 없는애들 걸러짐 추가해야함.
                    if (_mapHash.Contains(loc as string))
                    {
                        List<string> tempList = new List<string>();

                        for (int i = 0; i < locations.Count; i++)
                        {
                            tempList.Add(locations[i].PrimaryKey);
                            if (!_resourceLocator.ContainsKey(locations[i].PrimaryKey))
                                _resourceLocator.Add(locations[i].PrimaryKey, locations[i]);
                        }

                        _key.Add(loc as string, tempList);
                    }
                }
            }
            Debug.LogFormat("AWS 서버 맵 갯수 : {0}", _resourceLocator.Count);
            Debug.Log("Key 개수 : " + _key.Count);

            InitComplete?.Invoke();
        };
    }

    public void AvailableSpace()
    {
        _totalSpace.text = string.Format("{0} MB", SimpleDiskUtils.DiskUtils.CheckTotalSpace());
        _usedSpace.text = string.Format("{0} MB", SimpleDiskUtils.DiskUtils.CheckTotalSpace() - SimpleDiskUtils.DiskUtils.CheckAvailableSpace());
        _availableSpace.text = string.Format("{0} MB 사용 가능", SimpleDiskUtils.DiskUtils.CheckAvailableSpace());
        _spaceSlider.value = (SimpleDiskUtils.DiskUtils.CheckTotalSpace() - SimpleDiskUtils.DiskUtils.CheckAvailableSpace()) / (float)SimpleDiskUtils.DiskUtils.CheckTotalSpace();
    }
    private void CacheFilePathInit()
    {
#if UNITY_EDITOR
        // default 캐시 경로 삭제
        // 나중에 default 경로 사용 할 경우 변경 필요
        Caching.defaultCache.ClearCache();
#endif
        DirectoryInfo directory = new DirectoryInfo(_bundlePath);

        if (!directory.Exists)
        {
#if BICYCLE_LOGGING
            Debug.LogWarning("Create MapData Directory");
#endif
            directory.Create();
        }
        var path = Caching.AddCache(_bundlePath);
        Caching.currentCacheForWriting = path;
    }
    void CacheCheck()
    {
        HashSet<string> hashName = new HashSet<string>();
        HashSet<string> bundleName = new HashSet<string>();
        foreach (var locatorInfo in Addressables.ResourceLocators)
        {
            foreach (var key in _key)
            {
                if (locatorInfo.Locate(key.Key, typeof(object), out var locs))
                {
                    if (locs == null)
                        continue;

                    foreach (var loc in locs)
                    {
                        if (loc.HasDependencies)
                        {
                            foreach (var dep in loc.Dependencies)
                            {
                                if (dep.Data is AssetBundleRequestOptions)
                                {
                                    hashName.Add((dep.Data as AssetBundleRequestOptions).Hash);
                                    bundleName.Add((dep.Data as AssetBundleRequestOptions).BundleName);
                                }
                            }
                        }
                    }
                }
            }
        }

        var dirInfo = new DirectoryInfo(Caching.defaultCache.path);
        foreach (var dir in dirInfo.GetDirectories())
        {
            if (!bundleName.Contains(dir.Name)) // Bundle 이름이 바뀐 경우
            {
                Debug.LogWarning("파일 삭제 : " + dir.FullName);
                Directory.Delete(dir.FullName, true);
            }

            foreach (var _dir in dir.GetDirectories()) // Hash 이름이 바뀐 경우
            {
                if (!hashName.Contains(_dir.Name))
                {
                    Debug.LogWarning("파일 삭제 : " + _dir.FullName);
                    Directory.Delete(_dir.FullName, true);
                }
            }
        }
    }

    public void ClearCache(string key)
    {
        foreach (var locatorInfo in Addressables.ResourceLocators)
        {
            if (locatorInfo.Locate(key, typeof(object), out var locs))
            {
                if (locs == null)
                    continue;

                foreach (var loc in locs)
                {
                    if (loc.HasDependencies)
                    {
                        foreach (var dep in loc.Dependencies)
                        {
                            if (!dep.PrimaryKey.Contains("unitybuiltinshaders") && dep.Data is AssetBundleRequestOptions)
                            {
                                string deletePath = string.Format("{0}/{1}", Caching.defaultCache.path, (dep.Data as AssetBundleRequestOptions).BundleName);
                                if (new DirectoryInfo(deletePath).Exists)
                                    Directory.Delete(deletePath, true);

                                //Caching.ClearCachedVersion((dep.Data as AssetBundleRequestOptions).BundleName, Hash128.Parse((dep.Data as AssetBundleRequestOptions).BundleName));
                            }
                        }
                    }
                }
            }
        }

        AvailableSpace();
    }

    public Texture2D GetTrackImage(string trackindex)
    {
        string path = string.Format("TrackImage/trackimg_{0:D2}", int.Parse(trackindex));
        var image = Resources.Load<Texture2D>(path);
        return image != null ? image : null;
    }
    private void OnApplicationQuit()
    {
        Addressables.ClearResourceLocators();
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    //public string PreSigned(string objectKey)
    //{
    //    string urlString;
    //    try
    //    {
    //        objectKey = objectKey.Split(new string[] { _bucketName }, StringSplitOptions.None)[1];
    //        objectKey = objectKey.Substring(1);
    //        GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
    //        {
    //            BucketName = _bucketName,
    //            Key = objectKey,
    //            Expires = DateTime.UtcNow.AddMinutes(10)
    //        };
    //        urlString = _s3Client.GetPreSignedURL(request);
    //        Debug.LogFormat("URL : {0}", urlString);
    //
    //        var key = urlString.Split('?');
    //        
    //        return key[1];
    //    }
    //    catch (AmazonS3Exception e)
    //    {
    //        Debug.LogFormat("Error encountered on server. Message:'{0}' when writing an object", e.Message);
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogFormat("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
    //    }
    //    return string.Empty;
    //}
}