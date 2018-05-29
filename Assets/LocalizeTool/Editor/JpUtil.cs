using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// util class used to check jp files.
/// </summary>
public class JpUtil {

	private static IEnumerable<char> GetCharsInRange(string text, int min, int max)
	{
		return text.Where(e => e >= min && e <= max);
	}

	public static bool IsContainsJapanese(string text)
	{
		var hiragana = GetCharsInRange(text, 0x3040, 0x309F).Any(); //平仮名
		var katakana = GetCharsInRange(text, 0x30A0, 0x30FF).Any(); //片仮名
		var kanji = GetCharsInRange(text, 0x4E00, 0x9FBF).Any();
		return hiragana || katakana || kanji;
	}

	public static bool PlainText_IsContainsJapanese(string text)
	{
		text = Regex.Unescape(text);
		return IsContainsJapanese(text);
	}

	/// <summary>
	/// Clean comments in text assets.
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static string CleanCommentLines(string text)
	{
		var blockComments = @"/\*(.*?)\*/";
		var lineComments = @"//(.*?)\r?\n";

		string noComments = Regex.Replace(text,
			blockComments + "|" + lineComments , "", RegexOptions.Singleline);

		return noComments;
	}
}
