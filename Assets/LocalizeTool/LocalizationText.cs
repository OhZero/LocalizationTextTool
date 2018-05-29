using System;
using UnityEngine.UI;
using UnityEngine;
/// <summary>
/// Custom Text Control used for localization text.
/// Author: yaojianlun
/// </summary>
namespace Localization
{
	public class LocalizationText : Text
	{
		#region Param
		

		[Header("Localization")]
		[SerializeField]
		protected string m_KeyString;

		public string keyString {
			get {return this.m_KeyString; }
			set { this.m_KeyString =  value; }
		}
		#endregion

		#region Override Part
		public override string text
		{
            get
            {
                if (!ClientString.LocalizationDict.ContainsKey(keyString))
                {
                    m_Text = string.Format("[{0}]",m_KeyString);
                }
                else
                {
                    m_Text = ClientString.LocalizationDict[keyString];
                }
                return m_Text;
            }
			set
			{
				if (String.IsNullOrEmpty(value))
				{
					if (String.IsNullOrEmpty(m_Text))
						return;
					m_Text = "";
					SetVerticesDirty();
				}
				else if (m_Text != value)
				{
					m_Text = value;
					SetVerticesDirty();
					SetLayoutDirty();
				}
			}
		}
		#endregion
	}

}
