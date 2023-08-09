using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class MapDownLoadUI : MonoBehaviour
{
    public Text _downText;
    public Slider _downSlider;
    public string _key;
    public List<string> _trackIndex = new List<string>();

    public struct DownloadSizeInfo
    {
        public Text sizeText;
        public float size;
    }
    public DownloadSizeInfo _downloadSizeInfo;

    private Button _downButton;
    private Button _downAbortButton;
    private Button _cacheDeleteButton;

    AsyncOperationHandle _downLoadhandle;
    public void InitUI()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name == "DownLoad")
            {
                _downButton = transform.GetChild(i).GetComponent<Button>();
                _downButton.interactable = true;
                _downButton.onClick.AddListener(BundleDownLoad);
            }
            else if(transform.GetChild(i).name == "DownLoadCancel")
            {
                _downAbortButton = transform.GetChild(i).GetComponent<Button>();
                _downAbortButton.interactable = false;
                _downAbortButton.onClick.AddListener(AddressableManager.Instance.DownCancel);
            }
            else if (transform.GetChild(i).name == "GetSize")
                transform.GetChild(i).GetComponent<Button>().onClick.AddListener(CheckDownloadFileSize);
            else if (transform.GetChild(i).name == "CacheDelete")
            {
                _cacheDeleteButton = transform.GetChild(i).GetComponent<Button>();
                _cacheDeleteButton.interactable = false;
                _cacheDeleteButton.onClick.AddListener(DeleteCache);
            }
            else if (transform.GetChild(i).name == "MapSizeText")
                _downloadSizeInfo.sizeText = transform.GetChild(i).GetComponent<Text>();
            else if (transform.GetChild(i).name == "DefaultDay")
            {
                for (int j = 0; j < _trackIndex.Count; j++)
                {
                    var dataInfo = MapUpLoadDataManager.Instance._trackInfo.Find((x) => x["trackindex"] == _trackIndex[j]);
                    if (dataInfo["time"] != "day") continue;

                    string _name = string.Format("{0} {1}", dataInfo["eng"], dataInfo["time"]);
                    var childGo = transform.GetChild(i);
                    childGo.name = _name;
                    childGo.GetComponent<MapCreateButton>()._trackIndex = _trackIndex[j];
                    childGo.Find("Text").GetComponent<Text>().text = dataInfo["eng"];
                    childGo.Find("Image").GetComponent<RawImage>().texture = AddressableManager.Instance.GetTrackImage(dataInfo["trackindex"]);
                }
            }
            else if (transform.GetChild(i).name == "DefaultNight")
            {
                for (int j = 0; j < _trackIndex.Count; j++)
                {
                    var dataInfo = MapUpLoadDataManager.Instance._trackInfo.Find((x) => x["trackindex"] == _trackIndex[j]);
                    if (dataInfo["time"] == "night" || dataInfo["time"] == "sunset")
                    {
                        string _name = string.Format("{0} {1}", dataInfo["eng"], dataInfo["time"]);
                        var childGo = transform.GetChild(i);
                        childGo.name = _name;
                        childGo.GetComponent<MapCreateButton>()._trackIndex = _trackIndex[j];
                        childGo.Find("Text").GetComponent<Text>().text = dataInfo["eng"];
                        childGo.Find("Image").GetComponent<RawImage>().texture = AddressableManager.Instance.GetTrackImage(dataInfo["trackindex"]);
                    }
                }
            }
            else if (transform.GetChild(i).name == "NameText")
            {
                transform.GetChild(i).GetComponent<Text>().text = MapUpLoadDataManager.Instance._trackInfo.Find((x) => x["trackindex"] == _trackIndex[0])["kor"];
            }
        }

    }
    public void DeleteCache()
    {
        AddressableManager.Instance.ClearCache(_key);

        _downSlider.value = 0f;
        _downText.text = "0.00 %";

        CheckDownloadFileSize();
    }
    public void CheckDownloadFileSize()
    {
        Addressables.GetDownloadSizeAsync(_key).Completed +=
            (AsyncOperationHandle<long> SizeHandle) =>
            {
                var result = SizeHandle.Result / 1024f; // KB
                result /= 1024f; // MB

                _downloadSizeInfo.size = result;

                if (result == 0f)
                {
                    _cacheDeleteButton.interactable = true;
                    _downButton.interactable = false;

                    foreach (var button in transform.GetComponentsInChildren<MapCreateButton>())
                    {
                        button._createButton.interactable = true;
                        button._releaseButton.interactable = false;
                    }
                }
                else
                {
                    _cacheDeleteButton.interactable = false;
                    _downButton.interactable = true;
                    foreach (var button in transform.GetComponentsInChildren<MapCreateButton>())
                    {
                        button._createButton.interactable = false;
                        button._releaseButton.interactable = false;
                    }
                }

                string sizeText = string.Format("{0:0.00} MB", result);
                _downloadSizeInfo.sizeText.text = sizeText;
                Addressables.Release(SizeHandle);
            };
    }

    public void BundleDownLoad()
    {
        if (SimpleDiskUtils.DiskUtils.CheckAvailableSpace() < 1024)
        {
            Debug.Log("저장소가 1GB 미만입니다.");
        }

        if (SimpleDiskUtils.DiskUtils.CheckAvailableSpace() < _downloadSizeInfo.size)
        {
            Debug.Log("저장소가 꽉찼습니다. 확인해주세요.");
            return;
        }

        _downLoadhandle = Addressables.DownloadDependenciesAsync(_key, true);
        StartCoroutine(DownLoadCoroutine());

        _downLoadhandle.Completed +=
        (AsyncOperationHandle Handle) =>
        {
            StopCoroutine(DownLoadCoroutine());

            if (Handle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogWarningFormat("다운로드 실패. 원인 : {0}", Handle.OperationException);
                CheckDownloadFileSize();
            }
            else if (Handle.IsValid())
            {
                _downLoadhandle = Handle;
                DownCompleted();
            }
            else
            {
                Debug.Log("!!!!!!!!!!!!!!!!!!!다운로드 오류");
            }

            //Addressables.Release(Handle);
            Addressables.Release(_downLoadhandle);
        };
    }
    public void DownCompleted()
    {
        // 다운 완료 이벤트 추가
        _downText.text = "다운 완료";
        Debug.Log("다운로드 완료!");
        AddressableManager.Instance.AvailableSpace();
        CheckDownloadFileSize();
    }
    IEnumerator DownLoadCoroutine()
    {
        _downButton.interactable = false;
        _downAbortButton.interactable = true;
        while (!_downLoadhandle.IsDone)
        {
            _downloadSizeInfo.sizeText.text = string.Format("{0:0.00} MB", (_downLoadhandle.GetDownloadStatus().TotalBytes - _downLoadhandle.GetDownloadStatus().DownloadedBytes) / (1024f * 1024f));
            _downText.text = string.Format("{0:0.00}%", _downLoadhandle.GetDownloadStatus().Percent * 100f);
            _downSlider.value = _downLoadhandle.GetDownloadStatus().Percent;

            if (_downLoadhandle.OperationException != null)
            {
                Debug.LogFormat("다운로드 중 오류 발생! {0}", _downLoadhandle.OperationException.Message);
                _downButton.interactable = true;
            }

            yield return new WaitForEndOfFrame();
        }
        _downAbortButton.interactable = false;
    }
}

