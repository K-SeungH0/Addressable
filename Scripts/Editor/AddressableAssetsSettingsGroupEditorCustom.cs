using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;


using System.Collections.Generic;
using System.Collections;

[CustomEditor(typeof(AddressableManager))]
public class AddressableAssetsSettingsGroupEditorCustom : Editor
{
    struct TrackIndexTime
    {
        public TrackIndexTime(string index, string time, string filename)
        {
            this.index = index;
            this.time = time;
            this.filename = filename;
        }

        public string index;
        public string time;
        public string filename;
    }
    
    private static Dictionary<string, Dictionary<string, string>> mapTimeVersionData = new Dictionary<string, Dictionary<string, string>>();
    private static Dictionary<string, List<TrackIndexTime>> mapKeyData = new Dictionary<string, List<TrackIndexTime>>();
    private static AddressableAssetSettings setting;
    private static AddressableAssetProfileSettings profileSettings;
    private static bool isRenameEnd;
    public static string AppVersion;

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        AppVersion = MapUpLoadDataManager.LoadAppVersion();
        Debug.Log("AppVersion : " + AppVersion);

        setting = AddressableAssetSettingsDefaultObject.GetSettings(false);
        profileSettings = setting.profileSettings;

        var method = profileSettings.GetType().GetMethod("OnAfterDeserialize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        object[] para = new object[1];
        para[0] = setting;
        method.Invoke(profileSettings, para);

        isRenameEnd = false;

        DataLoad(true);
        Setting();
        Debug.Log("Init Custom Editor");
    }

    [MenuItem("맵 패치 Window/CSV 파일 로드")]
    private static void LoadDataInMenu()
    {
        //DataLoad(true);
        OnScriptsReloaded();
    }

    [MenuItem("맵 패치 Window/번들 폴더 열기")]
    private static void OpenFolder()
    {
        //var _directoryPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, @"../")) + UnityEngine.AddressableAssets.Addressables.BuildPath;
        var _directoryPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, @"../")) + "ServerData";
        var dir = new System.IO.DirectoryInfo(_directoryPath);
        if (!dir.Exists)
            dir.Create();

        System.Diagnostics.Process.Start(_directoryPath);
    }
    [MenuItem("맵 패치 Window/캐시 폴더 열기")]
    public static void OpenCacheFolder()
    {
        var _directoryPath = Application.persistentDataPath;
        System.Diagnostics.Process.Start(_directoryPath);
    }
    private static void Setting()
    {
        OnModificationGlobal += OnModificationGlobalCustom;
    }
    #region 안쓰는거
    /*
    private static void CheckName()
    {
        var groups = setting.groups;
        var profileAllNames = profileSettings.GetAllProfileNames();

        for (int i = 0; i < groups.Count; i++)
        {
            if (groups[i].name == PlayerDataGroupName || groups[i].name == "DefaultAssets")
                continue;

            bool isProfileMatching = false;
            for (int j = 0; j < profileAllNames.Count; j++)
            {
                if (groups[i].name == profileAllNames[j])
                {
                    isProfileMatching = true;
                    break;
                }
            }
            if (!isProfileMatching)
            {
                string errorMessage = string.Format("Group 이름 : \"{0}\", Profile에 있는 이름과 매칭 되는 것이 없습니다. 확인해주세요.", groups[i].name);
                Debug.LogWarning(errorMessage);
            }
        }
    }
    */
    #endregion

    private static void DataLoad(bool isEvent = false)
    {

        if (isEvent)
            OnModificationGlobal -= OnModificationGlobalCustom;

        mapTimeVersionData.Clear();
        mapKeyData.Clear();
        MapUpLoadDataManager.Instance.CSVLoad();

        foreach (var data in MapUpLoadDataManager.Instance._trackInfo)
        {
            if (data.ContainsKey("eng") && data.ContainsKey("version"))
            {
                if (mapTimeVersionData.ContainsKey(data["eng"]))
                {
                    mapTimeVersionData[data["eng"]].Add(data["time"], data["version"]);
                }
                else
                {
                    mapTimeVersionData[data["eng"]] = new Dictionary<string, string>
                    {
                        { data["time"], data["version"] }
                    };
                }

                if (mapKeyData.ContainsKey(data["eng"]))
                {

                    mapKeyData[data["eng"]].Add(new TrackIndexTime(data["trackindex"], data["time"], data["filename"]));
                }
                else
                {
                    mapKeyData[data["eng"]] = new List<TrackIndexTime>
                    {
                        new TrackIndexTime(data["trackindex"], data["time"], data["filename"])
                    };
                }

            }
            else
                Debug.Log("CSV 파일에 label, version 데이터 없음.");
        }

        foreach (var data in mapTimeVersionData)
        {
            SetBuildPath(data.Key);
        }

        if (isEvent)
            OnModificationGlobal -= OnModificationGlobalCustom;
    }

    #region 안쓰는거
    //private static void JsonLoad(bool isEvent = false)
    //{
    //    if (isEvent)
    //        OnModificationGlobal -= OnModificationGlobalCustom;
    //
    //    string directoryPath = string.Format("{0}/Assets/AddressableAssetsData/Data", Directory.GetCurrentDirectory());
    //    Dictionary<string, object> _loadData = new Dictionary<string, object>();
    //    if (new DirectoryInfo(directoryPath).Exists)
    //    {
    //        FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
    //        foreach (FileInfo file in files)
    //        {
    //            if (file.Name.Contains("Json") && !file.Name.Contains("meta"))
    //            {
    //                var loadText = File.ReadAllText(file.FullName);
    //                _loadData = Json.Deserialize(loadText) as Dictionary<string, object>;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("Load 실패. 경로에 파일 없음!");
    //        Directory.CreateDirectory(directoryPath);
    //    }
    //    if (_loadData == loadBuildData) return;
    //
    //    loadBuildData.Clear();
    //    loadBuildData = _loadData;
    //
    //    foreach (var dict in loadBuildData)
    //    {
    //        SetBuildPath(dict.Key);
    //    }
    //
    //    Debug.Log("맵 정보 Json 로드 완료.");
    //    if (isEvent)
    //        OnModificationGlobal += OnModificationGlobalCustom;
    //}
    //private static void JsonSave()
    //{
    //    saveBuildData.Clear();
    //    var groupAllNames = setting.groups;
    //    for(int i = 0; i < groupAllNames.Count; i++)
    //    {
    //        if (groupAllNames[i].name == "DefaultAssets" || groupAllNames[i].name == PlayerDataGroupName) 
    //            continue;
    //
    //        if (loadBuildData.ContainsKey(groupAllNames[i].name))
    //            saveBuildData.Add(groupAllNames[i].name, loadBuildData[groupAllNames[i].name] as string);
    //        else
    //            saveBuildData.Add(groupAllNames[i].name, "V1.00");
    //    }
    //

    //    var profileAllNames = profileSettings.GetAllProfileNames();
    //    var activeProfileName = profileSettings.GetProfileName(setting.activeProfileId);

    //    saveBuildData.Clear();

    //    for (int i = 0; i<profileAllNames.Count; i++)
    //    {
    //        if (profileAllNames[i] == "Default")
    //            continue;

    //        if (profileAllNames[i] == activeProfileName)
    //        {
    //            if (loadBuildData.ContainsKey(profileAllNames[i]))
    //                saveBuildData.Add(profileAllNames[i], setting.OverridePlayerVersion);
    //            else
    //                saveBuildData.Add(profileAllNames[i], "V1.00");
    //        }
    //        else
    //{
    //    if (loadBuildData.ContainsKey(profileAllNames[i]))
    //        saveBuildData.Add(profileAllNames[i], loadBuildData[profileAllNames[i]] as string);
    //    else
    //        saveBuildData.Add(profileAllNames[i], "V1.00");
    //}
    //    }


    //string directoryPath = string.Format("{0}/Assets/AddressableAssetsData/Data/Json", Directory.GetCurrentDirectory());
    //var saveJsonStirng = Json.Serialize(saveBuildData);
    //File.WriteAllText(directoryPath, saveJsonStirng);
    //    Debug.Log("맵 정보 Json 세이브 완료.");
    //}
    #endregion

    private static void SetBuildPath(string modificationName)
    {
        var profileId = profileSettings.GetProfileId("Default");
        
        var group = setting.FindGroup(modificationName);
        if (group == null) return;

        mapTimeVersionData.TryGetValue(modificationName, out var paths);
        string path = string.Empty;

        int count = 0;
        foreach (var p in paths)
        {
            path += string.Format("{0}{1}", p.Key, p.Value);

            if (++count != paths.Count)
                path += "_";
        }

        string buildPath = string.Format("ServerData/[BuildTarget]/{0}/{1}", modificationName, path);
        string loadPath = string.Format("{0}/{1}/[BuildTarget]/{2}/{3}", AddressableManager.URL, AddressableManager._bucketName, modificationName, path);
        string kbuildPath = string.Format("{0}.{1}", modificationName, kBuildPath);
        string kloadPath = string.Format("{0}.{1}", modificationName, kLoadPath);

        try
        {
            profileSettings.SetValue(profileId, kbuildPath, buildPath);
            profileSettings.SetValue(profileId, kloadPath, loadPath);
            group.GetSchema<BundledAssetGroupSchema>().BuildPath.SetVariableByName(setting, kbuildPath);
            group.GetSchema<BundledAssetGroupSchema>().LoadPath.SetVariableByName(setting, kloadPath);
        }
        catch
        {
            Debug.LogError(modificationName + "Error");
        }
    }
    private static void OnModificationGlobalCustom(AddressableAssetSettings aas, ModificationEvent m, object o)
    {
        if (o == null)
            return;

        var activeProfileName = profileSettings.GetProfileName(setting.activeProfileId);

        if (m == ModificationEvent.ActiveProfileSet)
        {
            //if (activeProfileName != "Default")
            //{
            //    foreach (var group in setting.groups)
            //    {
            //        if (group.Name == activeProfileName)
            //        {
            //            foreach (var schema in group.Schemas)
            //            {
            //                if (schema is BundledAssetGroupSchema)
            //                {
            //                    var include = schema.GetType().GetProperty("IncludeInBuild");
            //                    include.SetValue(schema, true);
            //                }
            //            }
            //        }
            //        else
            //        {
            //            foreach (var schema in group.Schemas)
            //            {
            //                if (schema is BundledAssetGroupSchema)
            //                {
            //                    var include = schema.GetType().GetProperty("IncludeInBuild");
            //                    include.SetValue(schema, false);
            //                }
            //            }
            //        }
            //    }
            //    var defaultProfileId = profileSettings.GetProfileId("Default");
            //    var activeProfileId = profileSettings.GetProfileId(activeProfileName);
            //    var variableNames = profileSettings.GetVariableNames();
            //    foreach (var _name in variableNames)
            //    {
            //        if (_name == "BuildTarget" || _name == "Local") continue;

            //        var defaultvalue = profileSettings.GetValueByName(defaultProfileId, _name);

            //        if (_name.Contains("LoadPath"))
            //            defaultvalue = defaultvalue.Replace("[BuildTarget]", string.Format("[BuildTarget]/{0}", activeProfileName));
            //        else if (_name.Contains("BuildPath"))
            //            defaultvalue = defaultvalue.Replace("[BuildTarget]", string.Format("[BuildTarget]/{0}", activeProfileName));

            //        profileSettings.SetValue(activeProfileId, _name, defaultvalue);
            //    }
            //}
            if (activeProfileName != "Default")
            {
                var defaultProfileId = profileSettings.GetProfileId("Default");
                var activeProfileId = profileSettings.GetProfileId(activeProfileName);
                var variableNames = profileSettings.GetVariableNames();
                foreach (var _name in variableNames)
                {
                    if (_name == "BuildTarget" || _name.Contains("Local")) continue;
                    var defaultvalue = profileSettings.GetValueByName(defaultProfileId, _name);
                    if (_name.Contains("Remote")) 
                    {
                        if (_name.Contains("LoadPath"))
                            defaultvalue = defaultvalue.Replace("[BuildTarget]", string.Format("[BuildTarget]/{0}/{1}", activeProfileName, MapUpLoadDataManager.LoadAppVersion()));
                        else if (_name.Contains("BuildPath"))
                            defaultvalue = defaultvalue.Replace("[BuildTarget]", string.Format("{0}/[BuildTarget]/{0}/{1}", activeProfileName, MapUpLoadDataManager.LoadAppVersion()));
                    }
                    else
                    {
                        if (_name.Contains("LoadPath"))
                            defaultvalue = defaultvalue.Replace("[BuildTarget]", string.Format("[BuildTarget]/{0}", activeProfileName));
                        else if (_name.Contains("BuildPath"))
                            defaultvalue = defaultvalue.Replace("[BuildTarget]", string.Format("{0}/[BuildTarget]/{0}", activeProfileName));
                    }
                    profileSettings.SetValue(activeProfileId, _name, defaultvalue);
                }
            }
            else
            {
                var activeProfileId = profileSettings.GetProfileId(activeProfileName);
                var variableNames = profileSettings.GetVariableNames();
                foreach (var _name in variableNames)
                {
                    if (!_name.Contains("Remote")) continue;
                    var defaultvalue = string.Empty;

                    if (_name.Contains("LoadPath"))
                        defaultvalue = string.Format("{0}/{1}/[BuildTarget]", AddressableManager.URL, AddressableManager._bucketName);
                    else if (_name.Contains("BuildPath"))
                        defaultvalue = "ServerData/[BuildTarget]";

                    profileSettings.SetValue(activeProfileId, _name, defaultvalue);
                }
            }

            #region 안쓰는거
            //CheckName();
            //string name = profileSettings.GetProfileName(o.ToString());
            ////SaveActiveProfileInfo(name);
            //
            //if (loadBuildData.ContainsKey(activeProfileName))
            //    setting.OverridePlayerVersion = loadBuildData[activeProfileName] as string;
            //else if (activeProfileName != "Default")
            //    Debug.LogWarning("맵 정보 Data에 " + activeProfileName + "이 없습니다. 맵 정보 Save 해주세요.");
            #endregion
        }

        else if (m == ModificationEvent.EntryAdded)
        {
            // 추가된 Asset
            var addedEntry = o as List<AddressableAssetEntry>;

            for (int i = 0; i < addedEntry.Count; i++)
            {
                bool isAdd = false;

                if (mapKeyData.ContainsKey(addedEntry[i].parentGroup.name))
                {
                    foreach (var data in mapKeyData[addedEntry[i].parentGroup.name])
                    {
                        var addedFileName = addedEntry[i].TargetAsset.name;
                        if(addedFileName.Equals(data.filename))
                        {
                            isAdd = true;
                            OnModificationGlobal -= OnModificationGlobalCustom;
                            addedEntry[i].SetAddress(data.index, true);
                            addedEntry[i].SetLabel(addedEntry[i].parentGroup.name, true);
                            OnModificationGlobal += OnModificationGlobalCustom;
                        }
                        else if(addedFileName.ToLower().Equals(data.filename.ToLower() + "_collision"))
                        {
                            isAdd = true;
                            OnModificationGlobal -= OnModificationGlobalCustom;
                            addedEntry[i].SetAddress(data.index + "_collision", true);
                            addedEntry[i].SetLabel(addedEntry[i].parentGroup.name, true);
                            OnModificationGlobal += OnModificationGlobalCustom;
                        }
                    }
                    if(!isAdd)
                    {
                        Debug.LogWarningFormat("{0}그룹에 {1}을 추가하셨습니다. CSV파일 {0}에는 filename {1}이 존재 하지 않습니다.", addedEntry[i].parentGroup.name, addedEntry[i].TargetAsset.name);
                    }
                }
                else
                    Debug.LogWarningFormat("{0}그룹에 {1}을 추가하셨습니다. CSV파일 {0}그룹 내용이 없습니다. 확인해주세요.", addedEntry[i].parentGroup.name, addedEntry[i].TargetAsset.name);

                //mapKeyData[addedEntry[i].parentGroup.name]
            }

            //for (int i = 0; i < addedEntry.Count; i++)
            //{
            //    // 추가된 Asset의 그룹이름, CSV의 eng이름이 있는지 판단.
            //    if (mapKeyData.ContainsKey(addedEntry[i].parentGroup.name))
            //    {
            //        // eng이름이 있다면 하나씩 데이터 확인
            //       foreach (var entryName in mapKeyData[addedEntry[i].parentGroup.name])
            //        {
            //            // 추가된 파일 이름
            //            var addedFileName = addedEntry[i].TargetAsset.name;
            //            if (addedFileName.ToLower().Contains(entryName.time) && addedFileName.Equals(entryName.filename))
            //            {
            //                OnModificationGlobal -= OnModificationGlobalCustom;
            //                addedEntry[i].SetAddress(entryName.index, true);
            //                addedEntry[i].SetLabel(addedEntry[i].parentGroup.name, true);
            //                OnModificationGlobal += OnModificationGlobalCustom;
            //            }
            //            else
            //            {
            //                Debug.LogWarningFormat("{0}그룹에 {1}Asset을 추가하셨습니다. CSV파일 {0}에는 filename {1}이 존재 하지 않습니다.", addedEntry[i].parentGroup.name, addedEntry//[i].TargetAsset.name);
            //            }
            //        }
            //    }
            //    else
            //        Debug.LogWarningFormat("{0}그룹에 {1}Asset을 추가하셨습니다. CSV파일 {0}그룹 내용이 없습니다. 확인해주세요.", addedEntry[i].parentGroup.name, addedEntry[i].TargetAsset.name);
            //}
        }
        else if (m == ModificationEvent.GroupAdded)
        {

        }
        else if (m == ModificationEvent.GroupRenamed)
        {
            if (!isRenameEnd)
            {
                // obejct.Name -> 이름 변경 전 name
                // object.name -> 이름 변경 후 name
                isRenameEnd = true;

                // 라벨 제거
                var removeGroup = o as AddressableAssetGroup;
                if (removeGroup != null && setting.GetLabels().Exists(x => x.Contains(removeGroup.Name)))
                    setting.RemoveLabel(removeGroup.Name, false);
            }
            else
            {

                OnModificationGlobal -= OnModificationGlobalCustom;
                //DataLoad();

                var reNamedGroup = o as AddressableAssetGroup;

                if (!mapKeyData.ContainsKey(reNamedGroup.name))
                    Debug.LogWarning(string.Format("CSV파일에 {0}이 없습니다. 확인해주세요.", reNamedGroup.name));

                foreach (var groupChild in reNamedGroup.entries)
                {
                    if (mapKeyData.ContainsKey(reNamedGroup.name))
                    {
                        if (!mapKeyData[reNamedGroup.name].Exists(x => x.Equals(groupChild.address)))
                        {
                            Debug.LogWarning(string.Format("Group {0}에 {1}이 없습니다. CSV파일 key 또는 Group에 Asset 이름을 확인해주세요.", reNamedGroup.name, groupChild.address));
                        }
                    }
                }

                // 라벨 추가
                if (setting.GetLabels().Exists(x => x.Contains(reNamedGroup.name)) == false)
                    setting.AddLabel(reNamedGroup.name);

                // Path 추가
                profileSettings.CreateValue(string.Format("{0}.{1}", reNamedGroup.name, kBuildPath), null);
                profileSettings.CreateValue(string.Format("{0}.{1}", reNamedGroup.name, kLoadPath), null);

                DataLoad();
                ProfilePathReset();

                isRenameEnd = false;
                OnModificationGlobal += OnModificationGlobalCustom;
            }
        }
        else if (m == ModificationEvent.GroupRemoved)
        {
            OnModificationGlobal -= OnModificationGlobalCustom;

            var removeGroup = o as AddressableAssetGroup;
            if (removeGroup != null && setting.GetLabels().Exists(x => x.Contains(removeGroup.name)))
                setting.RemoveLabel(removeGroup.name, false);

            ProfilePathReset();

            OnModificationGlobal += OnModificationGlobalCustom;
        }
        #region 안쓰는거
        /*
        else if (m == ModificationEvent.ProfileAdded)
        {
            
        }
        else if (m == ModificationEvent.ProfileModified)
        {
            var method = o.GetType().GetMethod("get_profileName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var modificationNameObject = method != null ? method.Invoke(o, null) : "";
            string modificationName = modificationNameObject as string;
        
            JsonSave();
            JsonLoad(true);
        
            OnModificationGlobal -= OnModificationGlobalCustom;
            SetBuildPath(modificationName);
        
            if (setting.groups.Exists(x => x.name.Contains(modificationName)) == false)
            {
                // 그룹 추가
                var grouptemp = setting.GroupTemplateObjects;
                var group = grouptemp[0] as AddressableAssetGroupTemplate;
                var newGroup = setting.CreateGroup(modificationName, false, false, true, null, group.GetTypes());
                group.ApplyToAddressableAssetGroup(newGroup);
        
                // 라벨 추가
                if (setting.GetLabels().Exists(x => x.Contains(modificationName)) == false)
                    setting.AddLabel(modificationName);
            }
            OnModificationGlobal += OnModificationGlobalCustom;
        }
        else if (m == ModificationEvent.ProfileRemoved)
        {
            JsonSave();
            JsonLoad(true);
        }
        */
        #endregion
    }

    private static void ProfilePathReset()
    {
        Dictionary<string, string> dNameId = new Dictionary<string, string>();
        var allVariableNames = profileSettings.GetVariableNames();
        var method = profileSettings.GetType().GetMethod("get_profileEntryNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var profileEntryNames = method != null ? method.Invoke(profileSettings, null) : null;

        var count = profileEntryNames.GetType().GetProperty("Count").GetValue(profileEntryNames);
        var listProfileNames = profileEntryNames.GetType().GetProperty("Item");

        object _value;
        object _id;
        object _name;
        for (int i = 0; i < (int)count; i++)
        {
            if (listProfileNames.GetGetMethod().GetParameters().Length > 0)
            {
                _value = listProfileNames.GetValue(profileEntryNames, new[] { i as object });
                _name = _value.GetType().GetMethod("get_ProfileName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_value, null);
                if (_name as string == kRemoteBuildPath ||
                    _name as string == kLocalLoadPath ||
                    _name as string == kLocalBuildPath ||
                    _name as string == kNewGroupName ||
                    _name as string == kLoadPath ||
                    _name as string == kBuildPath ||
                    _name as string == kRemoteLoadPath)
                    continue;

                _id = _value.GetType().GetMethod("get_Id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_value, null);
                dNameId.Add(_name as string, _id as string);
            }
        }

        var allGruop = setting.groups;
        for (int i = 0; i < allVariableNames.Count; i++)
        {
            if (allVariableNames[i] == kRemoteBuildPath ||
                allVariableNames[i] == kLocalLoadPath ||
                allVariableNames[i] == kLocalBuildPath ||
                allVariableNames[i] == kNewGroupName ||
                allVariableNames[i] == kLoadPath ||
                allVariableNames[i] == kBuildPath ||
                allVariableNames[i] == kRemoteLoadPath)
                continue;

            var index = allVariableNames[i].LastIndexOf('.');

            if (index == -1)
                continue;

            if (!allGruop.Exists(x => x.name.Equals(allVariableNames[i].Substring(0, index))))
            {
                if (dNameId.ContainsKey(allVariableNames[i]))
                {
                    var id = dNameId[allVariableNames[i]];
                    profileSettings.RemoveValue(id);
                }
                else
                {
                    Debug.Log(string.Format("Profile Path에 {0} 이 없습니다.", allVariableNames[i]));
                }
                continue;
            }
        }
    }
    #region 안쓰는거
    /* 
    private static void SaveActiveProfileInfo(string name)
    {
        Debug.Log(string.Format("Acitve Profile : {0}", name));
        try
        {
            string directoryPath = string.Format("{0}/Assets/AddressableAssetsData/AssetGroups/Schemas", Directory.GetCurrentDirectory());
            FileInfo[] files = new DirectoryInfo(directoryPath).GetFiles();
            foreach (FileInfo i in files)
            {
                if (i.Name.Contains("meta") == false && i.Name.Contains("_BundledAssetGroupSchema"))
                {
                    string nowFileText = File.ReadAllText(i.FullName);
                    if (i.Name.Contains(name))
                    {
                        //Active Profile
                        if (nowFileText.Contains("m_IncludeInBuild: 0"))
                        {
                            nowFileText = nowFileText.Replace("m_IncludeInBuild: 0", "m_IncludeInBuild: 1");
                            File.WriteAllText(i.FullName, nowFileText);
                        }
                    }
                    else
                    {
                        //Inactive Profile
                        if (nowFileText.Contains("m_IncludeInBuild: 1"))
                        {
                            nowFileText = nowFileText.Replace("m_IncludeInBuild: 1", "m_IncludeInBuild: 0");
                            File.WriteAllText(i.FullName, nowFileText);
                        }
                    }
                }
            }
            Debug.Log("Build Setting Change 성공");
            AssetDatabase.Refresh();
        }
        catch
        {
            Debug.Log("Save Fail");
        }
    }
    */
    #endregion
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Map Upload"))
        {
            Debug.Log("맵 업로드 Window 출력");
            UpLoadProgressEditorWindow.Init();
        }
    }
}