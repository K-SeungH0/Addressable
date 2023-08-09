using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class MapCreateButton : MonoBehaviour
{
    private List<GameObject> _gameObjects = new List<GameObject>();
    [Space]
    private MapDownLoadUI _mapDownUI;

    public Button _createButton;
    public Button _releaseButton;
    public string _trackIndex;

    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name == "Create")
            {
                _createButton = transform.GetChild(i).GetComponent<Button>();
                _createButton.onClick.AddListener(Instantiate);
            }
            else if (transform.GetChild(i).name == "Release")
            {
                _releaseButton = transform.GetChild(i).GetComponent<Button>();
                _releaseButton.onClick.AddListener(ReleaseObject);
            }
        }

        if (_mapDownUI == null)
            _mapDownUI = transform.GetComponentInParent<MapDownLoadUI>();
    }

    public void Instantiate()
    {
        Addressables.GetDownloadSizeAsync(_mapDownUI._key).Completed +=
            (AsyncOperationHandle<long> SizeHandle) =>
            {
                if (SizeHandle.Result > 0)
                    Debug.Log("맵을 다운 받지 않았음.");
                else
                    TrackCreate();

                Addressables.Release(SizeHandle);
            };
    }
    void TrackCreate()
    {
        foreach (var loc in Addressables.ResourceLocators)
        {
            if (loc.Locate(_trackIndex, typeof(object), out var locations))
            {
                if (locations.Count > 1)
                {
                    Debug.LogFormat("{0} Key값이 2개 이상입니다.", gameObject.name);
                }

                _createButton.interactable = false;
                _releaseButton.interactable = true;
                Addressables.InstantiateAsync(locations[0], Vector3.one, Quaternion.identity).Completed +=
                (handle) =>
                {
                    if (handle.OperationException != null)
                        throw handle.OperationException;

                    if (handle.IsValid())
                    {
                        // 생성된 개체의 참조값 캐싱
                        _gameObjects.Add(handle.Result);
                        //Addressables.Release(handle);
                    }

#if UNITY_EDITOR
                    var allRenderer = FindObjectsOfType<Renderer>();
                    for (int i = 0; i < allRenderer.Length; i++)
                    {
                        foreach (var mat in allRenderer[i].materials)
                        {
                            mat.shader = Shader.Find(mat.shader.name);
                        }
                    }
#endif
                };

                return;
            }
        }
        Debug.LogFormat("{0} Key값에 없음.", _trackIndex);
    }
    public void ReleaseObject()
    {
        if (_gameObjects.Count == 0)
            return;

        var index = _gameObjects.Count - 1;
        // InstantiateAsync <-> ReleaseInstance
        // ref count가 0이면 메모리에 GameObject가 언로드된다.
        Addressables.ReleaseInstance(_gameObjects[index]);
        _gameObjects.RemoveAt(index);
        _createButton.interactable = true;
        _releaseButton.interactable = false;
        //Caching.ClearAllCachedVersions();
    }

    private void OnApplicationQuit()
    {
        ReleaseObject();
    }

}