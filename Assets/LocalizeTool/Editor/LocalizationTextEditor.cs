using UnityEngine;
using UnityEngine.UI;
using Localization;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace UnityEditor.UI
{
	/// <summary>
	/// Editor class used to edit Localization UI Labels.
	/// Author:yaojianlun
	/// </summary>

	[CustomEditor(typeof(LocalizationText), true)]
	[CanEditMultipleObjects]
	public class LocalizationTextEditor : GraphicEditor
	{
		SerializedProperty m_Text;
		SerializedProperty m_LocalizationKey;
		SerializedProperty m_FontData;
		SerializedProperty m_KeyString;
		LocalizationText targetComp;

		static string guidReplacePattern = "guid.+.,";

		protected override void OnEnable()
		{
			base.OnEnable();
			m_Text = serializedObject.FindProperty("m_Text");
			m_LocalizationKey = serializedObject.FindProperty("m_LocalizationKey");
			m_FontData = serializedObject.FindProperty("m_FontData");
			m_KeyString = serializedObject.FindProperty("m_KeyString");

			targetComp = target as LocalizationText;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();


			EditorGUI.BeginChangeCheck();	//减少通过反射获取值的频率 KeyString变化时才获取
			EditorGUILayout.PropertyField(m_KeyString);
			if(EditorGUI.EndChangeCheck()) {
				//EditorGUILayout.PropertyField(m_LocalizationKey);
			
				serializedObject.ApplyModifiedProperties();
				serializedObject.Update();
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_Text);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(m_FontData);
			AppearanceControlsGUI();
			RaycastControlsGUI();
			serializedObject.ApplyModifiedProperties();
		}

		public static void UpgradeToLocalizationText(string assetPath, string textCompGUID, string textCompFileID, string localizeCompGUID, string localizeCompFileID, List<string> jpTextList)
		{
			ClearConsole();
			string formatedGUID = string.Format("guid: {0},", localizeCompGUID);
			string fullPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')), assetPath);
			string[] lines = System.IO.File.ReadAllLines(fullPath);
			string fullText = System.IO.File.ReadAllText(fullPath);

			for (int i = 0; i < lines.Length; i++)
			{
				if(lines[i].Contains(textCompFileID)) {
					int scriptLineIdx = i;	//往下遍历直到找到m_Text
					while(i < lines.Length) {
						
						if(lines[i].Contains("--- !u!"))	//发现下一个控件时跳出
							break;	

						if (JpUtil.PlainText_IsContainsJapanese(lines[i]))
						{
							string jpText = Regex.Unescape(lines[i].Split('\"')[1]).Replace("\n", "\\n");
							jpTextList.Add(jpText);

							//发现了FileID相同 但GUID不同的情况，所以直接把FileID后面的GUID替换
							lines[scriptLineIdx] = Regex.Replace(lines[scriptLineIdx], guidReplacePattern, formatedGUID);
							lines[scriptLineIdx] = lines[scriptLineIdx].Replace(textCompFileID, localizeCompFileID);
							break;
						}
						i++;
					}
				}
			}
			string text = string.Join("\n", lines);
			System.IO.File.WriteAllText(fullPath, text);
			AssetDatabase.Refresh();
		}

		public static void SetLocalizationKey(string assetPath, string localizeCompGUID, string localizeCompFileID) {
			ClearConsole();
			string formatedGUID = string.Format("guid: {0},", localizeCompGUID);
			string fullPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')), assetPath);
			string[] lines = System.IO.File.ReadAllLines(fullPath);
			string fullText = System.IO.File.ReadAllText(fullPath);

			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Contains(localizeCompFileID) && lines[i].Contains(localizeCompGUID))
				{
					while (i < lines.Length)
					{

						if (lines[i].Contains("--- !u!"))   //发现下一个控件时跳出
							break;

						if(lines[i].Contains("m_Text:") && lines[i].Contains("\"")) {
							string mTextValue = Regex.Unescape(lines[i].Split('\"')[1]);
							JpCharCheckerWindow.LocalizeDataPair dataPair = JpCharCheckerWindow.GetDataPair(mTextValue);
							if(dataPair.key != -1) {
									Debug.LogError("Find : " + dataPair.keyString);
									//删除掉可能LocalizationKey
									if(lines[i+1].Contains("m_LocalizationKey:")) lines[i+1] = string.Empty;
									if(lines[i+2].Contains("m_KeyString:")) lines[i+2] = string.Empty;
									string localizationDataFormat = "\n  m_LocalizationKey: {0}\n  m_KeyString: {1}";
									string localizationDataString = string.Format(localizationDataFormat, dataPair.key, Regex.Escape(dataPair.keyString));
									lines[i] = lines[i].Insert(lines[i].Length, localizationDataString);
							}
							else {
								Debug.LogErrorFormat("The string [{0}] cannot find the key:", mTextValue);
							}
						}
						i++;
					}
				}
			}
			string text = string.Join("\n", lines);
			System.IO.File.WriteAllText(fullPath, text);
			AssetDatabase.Refresh();
		}

	#region Menu Options
	public static class MenuOptions
	{
		
	}

		/// <summary>
		/// util func to clear the console
		/// </summary>
		public static void ClearConsole()
		{
			// This simply does "LogEntries.Clear()" the long way:
			var logEntries = System.Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");
			var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			clearMethod.Invoke(null, null);
		}
		#endregion
	}
}