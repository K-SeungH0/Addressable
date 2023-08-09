using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class MapUpLoadDataManager
{
    private static MapUpLoadDataManager instance;

    public static MapUpLoadDataManager Instance
    {
        get
        {
            if (instance == null)
                return instance = new MapUpLoadDataManager();
            return instance;
        }
    }

    public List<Dictionary<string, string>> _trackInfo = new List<Dictionary<string, string>>();
    public string _appVersion = string.Empty;
    public void CSVLoad()
    {
        _trackInfo.Clear();
        _trackInfo = CSVRead();
        Debug.Log("Load함수에서 CSV 파일 Load 완료");
    }

    private static List<Dictionary<string, string>> CSVRead()
    {
        try
        {
            List<Dictionary<string, string>> listData = new List<Dictionary<string, string>>();

            TextAsset resourceText = Resources.Load<TextAsset>("Data/trackInfo");

            // 한 라인 씩 저장
            string[] strLines = resourceText.text.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.None);
            if (strLines.Length <= 1)
                return listData;

            // 키 값 정보
            string[] strKeyTable = strLines[0].Split(',');

            for (int i = 1; i < strLines.Length; ++i)
            {
                if (strLines[i] == "" || strLines[i] == " ")
                    continue;

                string[] strElementTable = strLines[i].Split(',');

                Dictionary<string, string> element = new Dictionary<string, string>();

                for (int k = 0; k < strElementTable.Length; ++k)
                {
                    element.Add(strKeyTable[k], strElementTable[k]);
                }

                listData.Add(element);
            }

            return listData;
        }
        catch (Exception e)
        {
            Debug.Log("$$$$$(error) CSVRead Exception!: " + e.Message);
        }

        return null;
    }

    public static string LoadAppVersion()
    {
        if (Instance._appVersion == string.Empty)
        {
            TextAsset resourceText = Resources.Load<TextAsset>("Data/MapDown_Version");
            string result = resourceText.text.Replace("\n", string.Empty);
            result = result.Replace("\r", string.Empty);
            return result;
        }
        else
            return Instance._appVersion;
    }
}
