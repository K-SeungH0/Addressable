using UnityEngine;
using UnityEditor;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using System.Threading;
using System.Collections.Generic;
using System;
using System.Security.Cryptography;

public class UpLoadProgressEditorWindow : EditorWindow
{
	private static string _bucketName = "이곳에 AWS Bucket 이름 입력";
	private static string _accessKey;
	private static string _secretKey;
	private static bool _isLiveUpLoad;
	private static bool _isDevUpLoad;
	private static bool _isQAUpLoad;
	private static string AppVesrion;
	private readonly string privateKey = "이곳에 AWS에서 받은 Private Key 입력";

	static CancellationTokenSource source;
	private string _directoryPath;

	private float _transferredMB;
	private float _totalMB;

	private string _currentFile;
	private float _transferredMBForCurrentFile;
	private float _totalNumberMBForCurrentFile;
    private AmazonS3Client _s3Client;
	private string _uploadText;


	[MenuItem("맵 패치 Window/맵 업로드 Window")]
	public static void Init()
	{
		UpLoadProgressEditorWindow window = GetWindow<UpLoadProgressEditorWindow>(false, "맵 업로드 Window");
		source = new CancellationTokenSource();
		window.minSize = new Vector2(600f, 430f);
		window.Show();
		window.Reload();
		AppVesrion = AddressableAssetsSettingsGroupEditorCustom.AppVersion;
    }

	private void Reload()
    {
		Debug.Log("Reload");
		_transferredMB = 0f;
		_totalMB = 0f;
		_currentFile = "File";
		_transferredMBForCurrentFile = 0f;
		_totalNumberMBForCurrentFile = 0f;

		_uploadText = "업로드 진행률";
		_directoryPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, @"../")) + "ServerData";
		GetKey();
		EditorApplication.update += InspectorUpdate;
	}

	private void GetKey()
    {
		try
		{
			TextAsset resourceText = Resources.Load<TextAsset>("Data/accessKeys");
			Dictionary<string, string> element = new Dictionary<string, string>();
			var data = resourceText.text;
			byte[] bytes = Convert.FromBase64String(data);
			byte[] keyArray = System.Text.Encoding.UTF8.GetBytes(privateKey);
			
			RijndaelManaged result = new RijndaelManaged();
			byte[] newKeysArray = new byte[16];
			Array.Copy(keyArray, 0, newKeysArray, 0, 16);
			result.Key = newKeysArray;
			result.Mode = CipherMode.ECB;
			result.Padding = PaddingMode.PKCS7;

			ICryptoTransform ct = result.CreateDecryptor();
			byte[] resultArray = ct.TransformFinalBlock(bytes, 0, bytes.Length);
			var keyText = System.Text.Encoding.UTF8.GetString(resultArray);

			string[] strLines = keyText.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.None);

			// 키 값 정보
			string[] strKeyTable = strLines[0].Split(',');

			for (int i = 1; i < strLines.Length; ++i)
			{
				if (strLines[i] == "" || strLines[i] == " ")
					continue;

				string[] strElementTable = strLines[i].Split(',');

				for (int k = 0; k < strElementTable.Length; ++k)
				{
					element.Add(strKeyTable[k], strElementTable[k]);
				}
			}
			_accessKey = element["Access key ID"];
			_secretKey = element["Secret access key"];
			_bucketName = AddressableManager._bucketName;
		}
		catch (Exception e)
		{
			Debug.Log("$$$$$(error) CSVRead Exception!: " + e.Message);
		}
	}

	void OnGUI()
	{
		GUILayout.Space(10);

        var labelGUI = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        };


        GUILayout.Label("AWS Upload Info", labelGUI);
		EditorGUILayout.TextField("BucketName", _bucketName);
		GUILayout.Space(5);
		EditorGUILayout.TextField("AccessKey", _accessKey);
		GUILayout.Space(5);
		EditorGUILayout.PasswordField("SecretKey", _secretKey);
		GUILayout.Space(5);

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.TextField("업로드 경로", _directoryPath);
        if (GUILayout.Button("폴더 열기",GUILayout.Width(100)))
			System.Diagnostics.Process.Start(_directoryPath);
		EditorGUILayout.EndHorizontal();

        var toggleGUI = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        GUILayout.Space(5);
        EditorGUILayout.LabelField("앱 버전", AppVesrion, toggleGUI);

		toggleGUI.fontSize = 15;
		toggleGUI.fontStyle = FontStyle.Bold;

        GUILayout.Space(15);
		
        _isLiveUpLoad = EditorGUILayout.ToggleLeft("Live 업로드", _isLiveUpLoad, toggleGUI);
		GUILayout.Space(5);

		if (_isLiveUpLoad)
		{
			labelGUI.fontSize = 20;
			GUILayout.Label("!!!! 주의 Live 서버 업로드 하려고 합니다. !!!!", labelGUI);
		}
		_isQAUpLoad = EditorGUILayout.ToggleLeft("QA 업로드", _isQAUpLoad, toggleGUI);
		GUILayout.Space(5);
		_isDevUpLoad = EditorGUILayout.ToggleLeft("Dev 업로드", _isDevUpLoad, toggleGUI);

		GUILayout.Space(15);

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		labelGUI.fontSize = 15;
		GUILayout.Label(_uploadText, labelGUI);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		if (_totalMB == 0f)
		{
			ProgressBar(0f, string.Format("{0:0.00}MB / {1:0.00}MB \t {2:0.00}%", _transferredMB, _totalMB, 0));
			ProgressBar(0f, string.Format("{3} {0:0.00}MB / {1:0.00}MB \t {2:0.00}%", _transferredMBForCurrentFile, _totalNumberMBForCurrentFile, 0, _currentFile));
		}
		else
		{
			ProgressBar(_transferredMB / _totalMB, string.Format("{0:0.00}MB / {1:0.00}MB \t {2:0.00}%", _transferredMB, _totalMB, (_transferredMB / _totalMB) * 100));
			ProgressBar(_transferredMBForCurrentFile / _totalNumberMBForCurrentFile, string.Format("{3} \t {0:0.00}MB / {1:0.00}MB \t {2:0.00}%", _transferredMBForCurrentFile, _totalNumberMBForCurrentFile, (_totalNumberMBForCurrentFile / _totalNumberMBForCurrentFile) * 100, _currentFile));
		}
		var buttonGUI = new GUIStyle(GUI.skin.button)
        {
            fontSize = 25,
            fixedHeight = 70f
        };
        GUILayout.BeginHorizontal("box");
		if (GUILayout.Button("Upload", buttonGUI))
		{
			Debug.Log("맵 업로드 시작");
			MapUpload();
		}
		GUILayout.Space(25);
		if (GUILayout.Button("Cancel", buttonGUI))
        {
			source.Cancel();
			_transferredMB = 0f;
			_totalMB = 0f;
		}
		
		GUILayout.EndHorizontal();
	}

	void InspectorUpdate()
	{
		Repaint();
	}
	public async void MapUpload()
	{
		if(!_isDevUpLoad && !_isQAUpLoad && !_isLiveUpLoad)
        {
			Debug.Log("업로드 선택을 하지 않았습니다.");
			return;
        }
		try
		{
			if (_s3Client == null)
				_s3Client = new AmazonS3Client(_accessKey, _secretKey, RegionEndpoint.APNortheast2);

			double _time = EditorApplication.timeSinceStartup;
			var directoryTransferUtility = new TransferUtility(_s3Client);

			
			if (source == null)
				source = new CancellationTokenSource();

			if (_isDevUpLoad)
			{
				var uploadRequest = new TransferUtilityUploadDirectoryRequest
				{
					BucketName = _bucketName,
					Directory = _directoryPath + "/DEV",
					SearchOption = System.IO.SearchOption.AllDirectories,
					CannedACL = S3CannedACL.PublicRead,
					CalculateContentMD5Header = true,
				};
				uploadRequest.UploadDirectoryProgressEvent += (sender, e) =>
				{
					//e.TransferredBytes / e.TotalBytes => 진행률
					_uploadText = "DEV 업로드 진행률";
					_transferredMB = e.TransferredBytes / 1024f / 1024f;
					_totalMB = e.TotalBytes / 1024f / 1024f;
					_currentFile = e.CurrentFile;
					_currentFile = _currentFile.Substring(_currentFile.LastIndexOf("\\") + 1);
					_transferredMBForCurrentFile = e.TransferredBytesForCurrentFile / 1024f / 1024f;
					_totalNumberMBForCurrentFile = e.TotalNumberOfBytesForCurrentFile / 1024f / 1024f;
				};

				await directoryTransferUtility.UploadDirectoryAsync(uploadRequest, source.Token);
			}
			
			if(_isQAUpLoad)
            {
				var uploadRequest = new TransferUtilityUploadDirectoryRequest
				{
					BucketName = _bucketName,
					Directory = _directoryPath + "/QA",
					SearchOption = System.IO.SearchOption.AllDirectories,
					CannedACL = S3CannedACL.PublicRead,
					CalculateContentMD5Header = true,
				};

				uploadRequest.UploadDirectoryProgressEvent += (sender, e) =>
				{
					//e.TransferredBytes / e.TotalBytes => 진행률
					_uploadText = "QA 업로드 진행률";
					_transferredMB = e.TransferredBytes / 1024f / 1024f;
					_totalMB = e.TotalBytes / 1024f / 1024f;
					_currentFile = e.CurrentFile;
					_currentFile = _currentFile.Substring(_currentFile.LastIndexOf("\\") + 1);
					_transferredMBForCurrentFile = e.TransferredBytesForCurrentFile / 1024f / 1024f;
					_totalNumberMBForCurrentFile = e.TotalNumberOfBytesForCurrentFile / 1024f / 1024f;
				};

				await directoryTransferUtility.UploadDirectoryAsync(uploadRequest, source.Token);
			}

			if (_isLiveUpLoad)
			{
				var uploadRequest = new TransferUtilityUploadDirectoryRequest
				{
					BucketName = _bucketName,
					Directory = _directoryPath + "/LIVE",
					SearchOption = System.IO.SearchOption.AllDirectories,
					CannedACL = S3CannedACL.PublicRead,
					CalculateContentMD5Header = true,
				};

				uploadRequest.UploadDirectoryProgressEvent += (sender, e) =>
				{
					//e.TransferredBytes / e.TotalBytes => 진행률
					_uploadText = "LIVE 업로드 진행률";
                    _transferredMB = e.TransferredBytes / 1024f / 1024f;
                    _totalMB = e.TotalBytes / 1024f / 1024f;
                    _currentFile = e.CurrentFile;
					_currentFile = _currentFile.Substring(_currentFile.LastIndexOf("\\") + 1);
					_transferredMBForCurrentFile = e.TransferredBytesForCurrentFile / 1024f / 1024f;
                    _totalNumberMBForCurrentFile = e.TotalNumberOfBytesForCurrentFile / 1024f / 1024f;
                };

				await directoryTransferUtility.UploadDirectoryAsync(uploadRequest, source.Token);
			}

			_uploadText = "업로드 완료";
			Debug.LogFormat("업로드 완료!! 걸린 시간 : {0:0.00}s", (float)EditorApplication.timeSinceStartup - _time);
		}
		catch(System.Threading.Tasks.TaskCanceledException)
        {
			Debug.Log("맵 업로드 취소");
		}
        finally
        {
			if (source != null)
				source.Dispose(); 
			source = new CancellationTokenSource();
		}
	}

	private void ProgressBar(float value, string label)
	{
		Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
		EditorGUI.ProgressBar(rect, value, label);
		EditorGUILayout.Space();
	}

}
