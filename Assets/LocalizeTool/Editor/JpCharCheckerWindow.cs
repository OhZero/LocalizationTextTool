using UnityEditor;
using UnityEngine;
using UnityEditor.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

/// <summary>
/// Tool used to check out the files contains Japanese chars.
/// </summary>
public class JpCharCheckerWindow : EditorWindow {
	enum SourceType
	{
		Script,
		Prefab,
		All,
	}
	private List<string> m_OutputString = new	List<string>();
	private SourceType m_SourceType = SourceType.Script;
	private string m_FilteredString = string.Empty;
	private List<string> m_FilteredPathList = new List<string>();
	private Vector2 m_ScrollPosition;

	private string m_TextCompGUID = "f70555f144d8491a825f0804e09c671c";
	private string m_TextCompFileID = "708705254";
	private string m_LocalizationCompGUID = "86d91f724295eaf40b1f6551526f00f6";
	private string m_LocalizationCompFileID = "11500000";

	public struct LocalizeDataPair {
		public int key;
		public string keyString;
	}
	private static Dictionary<string, LocalizeDataPair> cacheClientStringDict = new Dictionary<string, LocalizeDataPair>();
	private static LocalizeDataPair emptyDataPair = new	LocalizeDataPair {key=-1, keyString = string.Empty };

	[MenuItem("Tools/Tool Box/Open Jp Checker Window")]
	static void Init()
	{
		JpCharCheckerWindow jpCharCheckerWindow = (JpCharCheckerWindow)GetWindow<JpCharCheckerWindow>();
		jpCharCheckerWindow.Show();
		initCachedDict();
	}

	static void initCachedDict() {
		cacheClientStringDict.Clear();
		string fileName = "ClientString_jpn";
		TextAsset text = Resources.Load<TextAsset>("Common/" + fileName);
		System.IO.StringReader stringReader = new System.IO.StringReader(text.text);
		int index = 0; string[] temp; string line;
		while (stringReader.Peek() > -1)
		{
			line = stringReader.ReadLine();
			if ("" != line)
			{
				temp = line.Split(",".ToCharArray(), 2);
				var dataPair = new LocalizeDataPair();
				dataPair.key = index;
				dataPair.keyString = temp[0];
				cacheClientStringDict[temp[1].Replace("\\n", "\n")] = dataPair;
				index++;
			}
		}
	}

	public static LocalizeDataPair GetDataPair(string str) {
		if(cacheClientStringDict.ContainsKey(str))
			return cacheClientStringDict[str];
		return emptyDataPair;
	}

	private void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		{
			EditorGUILayout.BeginVertical();
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Text Comp File ID:");
				m_TextCompFileID = EditorGUILayout.TextField(m_TextCompFileID);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Text Comp GUID:");
				m_TextCompGUID = EditorGUILayout.TextField(m_TextCompGUID);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Localization Comp File ID:");
				m_LocalizationCompFileID = EditorGUILayout.TextField(m_LocalizationCompFileID);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Localization Comp GUID:");
				m_LocalizationCompGUID = EditorGUILayout.TextField(m_LocalizationCompGUID);
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginHorizontal();
			{
				//m_SourceType = (SourceType)EditorGUILayout.EnumPopup("Select: ",m_SourceType);
				m_FilteredString = EditorGUILayout.TextField(m_FilteredString, GUILayout.MinWidth(100), GUILayout.ExpandHeight(false));
				//Check Button
				if (GUILayout.Button("Check", GUILayout.MinWidth(50), GUILayout.MaxWidth(50)))
				{
					OnCheckButtonCB();
				}
				//if (GUILayout.Button("Calculate Lines", GUILayout.MinWidth(100), GUILayout.MaxWidth(150)))
				//{
				//	OnCalculateButtonCB();
				//}
				////Upgrade Text Button
				if (GUILayout.Button("Upgrade Text", GUILayout.MinWidth(100), GUILayout.MaxWidth(100)))
				{
					if(string.IsNullOrEmpty(m_TextCompFileID) || string.IsNullOrEmpty(m_TextCompGUID) || string.IsNullOrEmpty(m_LocalizationCompFileID) || string.IsNullOrEmpty(m_LocalizationCompGUID)) {
						EditorUtility.DisplayDialog("错误", "文本组件ID不能为空", "好的");
						return;
					}
					OnUpgradeButtonCB();
				}
				if(GUILayout.Button("Set Localization Key", GUILayout.MinWidth(100)))
				{
					OnSetKeyButtonCB();
				}
				if(GUILayout.Button("Ouput Folder", GUILayout.Width(100)))
				{
					OpenOutputFileFolder();
				}
				if(GUILayout.Button("*", GUILayout.Width(20)))
				{
					RefreshDictData();
				}
			}
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndVertical();
		m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, GUILayout.ExpandHeight(true));
		for (int i = 0; i < m_FilteredPathList.Count; i++)
		{
			string path = m_FilteredPathList[i];

			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(path, GUILayout.MinWidth(100)))	//select the file
			{
				Selection.activeObject = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
			}
			if(GUILayout.Button("x", GUILayout.MaxWidth(20))) {
				m_FilteredPathList.Remove(path);
			}
			EditorGUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
	}

	private void OnDestroy() {
		cacheClientStringDict.Clear();
	}

	#region Button Call Back
	private void OnCheckButtonCB()
	{
		CheckFiles(m_FilteredString);
	}

	private void OnCalculateButtonCB() {
		CalculateFileLines(m_FilteredString);
	}

	private void OnUpgradeButtonCB() {
		System.Action<string, int, int> updateProgress = (text, now, max) => {
			EditorUtility.DisplayProgressBar("Upgrade...", text, (float)now / (float)max);
		};
		List<string> jpTextList = new List<string>();
		for (int i = 0; i < m_FilteredPathList.Count; i++)
		{
			string assetPath = m_FilteredPathList[i];
			LocalizationTextEditor.UpgradeToLocalizationText(assetPath, m_TextCompGUID, m_TextCompFileID, m_LocalizationCompGUID, m_LocalizationCompFileID, jpTextList);
			updateProgress(assetPath, i+1, m_FilteredPathList.Count);
		}
		EditorUtility.ClearProgressBar();

		string outputFilePath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')), "output.txt");
		jpTextList = jpTextList.Distinct().ToList<string>();
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		foreach (var str in jpTextList)
		{
			sb.AppendLine(str);
		}
		System.IO.File.AppendAllText(outputFilePath, sb.ToString());
	}

	private void OnSetKeyButtonCB()
	{
		System.Action<string, int, int> updateProgress = (text, now, max) => {
			EditorUtility.DisplayProgressBar("Upgrade...", text, (float)now / (float)max);
		};
		int length = m_FilteredPathList.Count;
		for (int i = 0; i < length; i++)
		{
			string assetPath = m_FilteredPathList[i];
			LocalizationTextEditor.SetLocalizationKey(assetPath, m_LocalizationCompGUID, m_LocalizationCompFileID);
			updateProgress(assetPath, i+1, length);
		}
		EditorUtility.ClearProgressBar();
	}

	private void OpenOutputFileFolder() {
		EditorUtility.OpenFilePanel("Jp Text Output File Folder", Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')), "txt");
	}

	private void RefreshDictData() {
		initCachedDict();
	}

	private void CheckFiles(string filteredStr)
	{
		System.Action<string, int, int> updateProgress = (text, now, max) => {
			EditorUtility.DisplayProgressBar("Check", text, (float)now / (float)max);
		};
		m_FilteredPathList.Clear();
		string[] guids = AssetDatabase.FindAssets(filteredStr);
		int length = guids.Length;
		for (int i = 0; i<length; i++)
		{
			string guid = guids[i];
			string path = AssetDatabase.GUIDToAssetPath(guid);

			string fullPath = Path.Combine(System.Environment.CurrentDirectory, path);
			string text = System.IO.File.ReadAllText(fullPath);
			if(!string.IsNullOrEmpty(text))
			{
				if(fullPath.EndsWith("prefab") || fullPath.EndsWith("unity"))
				{
					if(JpUtil.PlainText_IsContainsJapanese(text))
					{
						m_FilteredPathList.Add(AssetDatabase.GUIDToAssetPath(guid));
						continue;
					}
							
				}
				else    //text or script
				{
					//clear the comment lines
					text = JpUtil.CleanCommentLines(text);
					if (JpUtil.IsContainsJapanese(text))
						m_FilteredPathList.Add(AssetDatabase.GUIDToAssetPath(guid));
				}
			}

			updateProgress(path, (i+1), length);
		}
		//UnityEditor.UI.LocalizationTextEditor.ClearConsole();
		Debug.LogFormat("Total :<color=white>{0}</color> files", m_FilteredPathList.Count);
		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// 统计需要本地化的行数
	/// </summary>
	/// <param name="filteredStr"></param>
	private void CalculateFileLines(string filteredStr) {
		long lineCount = 0;
		m_FilteredPathList.Clear();
		string[] guids = AssetDatabase.FindAssets(filteredStr);
		foreach (var guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (true)
			{
				string fullPath = Path.Combine(System.Environment.CurrentDirectory, path);
				string text = System.IO.File.ReadAllText(fullPath);
				if (!string.IsNullOrEmpty(text))
				{
					if (fullPath.EndsWith("prefab") || fullPath.EndsWith("unity"))
					{
						foreach (var line in text.Split('\n'))
						{
							if (JpUtil.PlainText_IsContainsJapanese(line))
								lineCount++;
						}
					}
					else    //text or script
					{
						//clear the comment lines
						text = JpUtil.CleanCommentLines(text);
						foreach (var line in text.Split('\n'))
						{
							if (JpUtil.IsContainsJapanese(line))
								lineCount++;
						}
					}
				}
			}
		}
		//UnityEditor.UI.LocalizationTextEditor.ClearConsole();
		Debug.LogFormat("Total :<color=white>{0}</color> lines", lineCount);
	}
	#endregion
}
