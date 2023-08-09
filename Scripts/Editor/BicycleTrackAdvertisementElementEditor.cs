using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using static BicycleTrackAdvertisementElement;
using System.IO;

[CustomEditor(typeof(BicycleTrackAdvertisementElement))]
// 사용자가 이 에디터로 여러 오브젝트를 선택하고 모두 동시에 변경할 수 있음을 Unity에 알립니다.
[CanEditMultipleObjects]        
public class BicycleTrackAdvertisementElementEditor : Editor
{

    BicycleTrackAdvertisementElement _this;

    GameObject _adListOblectPanel;
    List<sAdvertisement> _ad;
    Dictionary<eAdvertisementType, int> _adCunt;


    private MeshRenderer[] renderers;

    private bool _isExporting = false;

    private void OnEnable()
    {
        _this = target as BicycleTrackAdvertisementElement;
        _ad = _this._advertisements;
        _adCunt = _this._dic_AD_TypeCount;

    }



    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        _adListOblectPanel = _this._AD_List_OblectPanel;

        //-----------------------------------
        // Line
        //-----------------------------------
        DrawUILine(new Color(255, 0, 0));


        //-----------------------------------
        // 적용 버튼 생성   GUILayout.Button("버튼이름" , 가로크기, 세로크기)
        //-----------------------------------
        if (GUILayout.Button("Custom Setting", GUILayout.Width(120), GUILayout.Height(30)))
        {
            CustomettingData();
        }

        //-----------------------------------
        // 적용 버튼 생성   GUILayout.Button("버튼이름" , 가로크기, 세로크기)
        //-----------------------------------
        if (GUILayout.Button("Auto Setting", GUILayout.Width(120), GUILayout.Height(30)))
        {
            AutoSettingData();
        }

        //-----------------------------------
        // 적용 버튼 생성   GUILayout.Button("버튼이름" , 가로크기, 세로크기)
        //-----------------------------------
        if (GUILayout.Button("Data Export", GUILayout.Width(220), GUILayout.Height(30)))
        {
            ExportData();
        }

    }
 
    private void CustomettingData()
    {
        DataSetting();
    }

    private void AutoSettingData()
    {
        if (_adListOblectPanel == null)
            return;


        _ad.Clear();
        // 광고 그룹 목록
        for (int groupCount = 0; groupCount< _adListOblectPanel.transform.childCount; groupCount++)
        {
            GameObject groupObject = _adListOblectPanel.transform.GetChild(groupCount).gameObject;

            // 광고 배너 목록
            for (int i = 0; i < groupObject.transform.childCount; i++ )
            {
                string modelobjName = groupObject.transform.GetChild(i).gameObject.name.ToLower();


                var enumarray = Enum.GetValues(typeof(eAdvertisementType));
                foreach (var en in enumarray)
                {

                    // 광고 베너 이름으로 광고타입 설정
                    eAdvertisementType enType = ((eAdvertisementType)en);
                    if (modelobjName.IndexOf(enType.ToString().ToLower()) != -1)
                    {
                        sAdvertisement advertisement = new sAdvertisement();
                        advertisement.ModelObject = groupObject.transform.GetChild(i).gameObject;
                        advertisement.Area = groupCount + 1;     // 광고 배너가 속해 있는 그룹
                        _ad.Add(advertisement);
                        break;
                    }
                }
            }
        }

        DataSetting();
 
    }

    private void DataSetting()
    {
        _adCunt.Clear();
        int nID = 0;
        for (int i = 0; i < _ad.Count; i++)
        {
            if (_ad[i].meshRenderers != null)
                _ad[i].meshRenderers.Clear();
            sAdvertisement temp = new sAdvertisement();
            temp = _ad[i];


            if (temp.ModelObject != null)
            {

                temp.type = eAdvertisementType.None;
                string modelobjName = temp.ModelObject.name.ToLower();
                var enumarray = Enum.GetValues(typeof(eAdvertisementType));
                foreach (var en in enumarray)
                {
                    eAdvertisementType enType = ((eAdvertisementType)en);
                    if (modelobjName.IndexOf(enType.ToString().ToLower()) != -1)
                    {
                        temp.type = enType;
                        break;
                    }
                }



                if (temp.type != eAdvertisementType.None)
                {
                    int nADTypeCount = 0;
                    if (_adCunt.ContainsKey(temp.type) == true)
                    {
                        _adCunt[temp.type] = _adCunt[temp.type] + 1;
                    }
                    else
                    {
                        _adCunt.Add(temp.type, 0);
                    }
                    nADTypeCount = _adCunt[temp.type] * 10;

                    renderers = temp.ModelObject.GetComponentsInChildren<MeshRenderer>();
                    if (renderers != null)
                    {
                        int nADMeshCount = 0;
                        for (int k = 0; k < renderers.Length; k++)
                        {
                            
                            // 같은 문자열이 들어가 있는지 찾기
                            if (renderers[k].name.ToLower().IndexOf(temp.type.ToString().ToLower()) != -1)
                            {
                                nADMeshCount++;
                                nID++;
                                sMeshRendererData data = new sMeshRendererData();
                                data.ID = nID;
                                data.bannerID = ((int)temp.type * 10000) + nADTypeCount + nADMeshCount;
                                data.meshRenderer = renderers[k];
                                if (temp.meshRenderers == null)
                                    temp.meshRenderers = new List<sMeshRendererData>();
                                temp.meshRenderers.Add(data);
                            }
                        }
                    }
                }
            }
            _ad[i] = temp;

        }
    }


    private void ExportData()
    {
        if (_isExporting == true)
        {
            AD_Editrot_Export_End_Popup.OpenPopup();
            return;
        }
        _isExporting = true;


        if (_ad == null || _ad.Count <= 0)
            return;

        string DirPath = Path.GetFullPath(Path.Combine(Application.dataPath, @"../"));
        string fileDirPath = string.Format("{0}Ad_Datas", DirPath);

        DirectoryInfo directory = new DirectoryInfo(fileDirPath);
        if (directory.Exists == false)
        {
            Debug.Log("----- Create User MileageLog Directory");
            directory.Create();
        }


        string path = Path.GetFullPath(Path.Combine(Application.dataPath, @"../"));
        string filePath = string.Format("{0}Ad_Datas/{1}.txt", path, _this.gameObject.name);
        FileInfo fileInfo = new FileInfo(filePath);

        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }

        if (fileInfo.Exists == false)
        {
            FileStream create = fileInfo.Create();
            create.Close();
        }
        FileStream fs = fileInfo.OpenWrite();
        StreamWriter sw = new StreamWriter(fs);
        sw.WriteLine("ID, bannerID, type, Area");


        // 매쉬렌더러 링크가 없을 경우 에러 표시
        List<int> nullMeshRendererList = new List<int>();

        for (int i = 0; i < _ad.Count; i++)
        {
            if (_ad[i].meshRenderers != null)
            {
                for (int j = 0; j < _ad[i].meshRenderers.Count; j++)
                {
                    if (_ad[i].meshRenderers[j].meshRenderer == null)
                        nullMeshRendererList.Add(_ad[i].meshRenderers[j].bannerID);


                    sw.WriteLine(string.Format("{0},{1},{2},{3}", 
                    _ad[i].meshRenderers[j].ID,
                    _ad[i].meshRenderers[j].bannerID,
                    _ad[i].type.ToString(),
                    _ad[i].Area
                    ));
                }
            }
        }

        ModelObjectTypeCount(sw);

        if (nullMeshRendererList.Count != 0)
            NullMeshRendererData(sw, nullMeshRendererList);

        sw.Dispose();
        fs.Dispose();


        _isExporting = false;
        
    }

  
    

    // 광고 타입별 갯수
    private void ModelObjectTypeCount(StreamWriter sw)
    {
        int nTotalObjectCount = 0;
        sw.WriteLine("\n\n ------------------------------\n 광고 타입별 사용수\n");
        var enumarray = Enum.GetValues(typeof(eAdvertisementType));
        foreach (var en in enumarray)
        {
            eAdvertisementType enType = ((eAdvertisementType)en);

            if (enType != eAdvertisementType.None)
            {
                if (_adCunt.ContainsKey(enType) == true)
                {
                    nTotalObjectCount += (_adCunt[enType] + 1);
                    sw.WriteLine(string.Format("{0} : {1} 개", enType.ToString(), _adCunt[enType] + 1)); //  _adCunt[enType] 0개부터 시작하므로 +1 해준다
                }
                else
                {
                    //sw.WriteLine(string.Format("{0} : {1} 개", enType.ToString(), 0));
                }
            }
        }

        sw.WriteLine(string.Format("\n총 사용 수 : {0} 개", nTotalObjectCount));

    }

    private void NullMeshRendererData(StreamWriter sw, List<int> nullData)
    {
        sw.WriteLine("\n\n ------------------------------\n MeshRenderer 의 링크가 없다!!!!!\n");
        for (int i = 0; i < nullData.Count; i++)
            sw.WriteLine(string.Format("bannerID:{0}", nullData[i]));
    }


    public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }

}





//------------------------------------------------------------------------------------------------------------------------
// Define 설정 적용 중 알림 팝업
//------------------------------------------------------------------------------------------------------------------------
public class AD_Editrot_Export_End_Popup : EditorWindow
{
    private static AD_Editrot_Export_End_Popup s_instance = null;
    internal static AD_Editrot_Export_End_Popup instance
    {
        get
        {
            if (s_instance == null)
                s_instance = GetWindow<AD_Editrot_Export_End_Popup>(true, "저장 중");
            return s_instance;
        }
    }

    [SerializeField]
    private Vector2 m_ScrollPosition;

    static public void OpenPopup()
    {
        s_instance = null;
        instance.position = new Rect(Screen.width, Screen.height / 2, 450, 150);
    }

    internal void OnGUI()
    {
        // BeginScrollView ~ EndScrollView 사이의 내용은 스크롤 박스 적용
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        EditorGUILayout.Space(30);
        var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
        centeredStyle.alignment = TextAnchor.UpperCenter;
        GUILayout.Label(new GUIContent("광고 데이터를 파일로 저장 중입니다."), centeredStyle);
        EditorGUILayout.Space(10);
        GUILayout.Label(new GUIContent("저장이 완료될때까지 기다려 주세요."), centeredStyle);
        EditorGUILayout.Space();

        EditorGUILayout.EndScrollView();
    }
}
