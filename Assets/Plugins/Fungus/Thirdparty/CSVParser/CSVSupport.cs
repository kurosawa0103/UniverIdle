using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace Fungus
{

	// Some CSV utilities cobbled together from stack overflow answers
	// CSV escape & unescape from http://stackoverflow.com/questions/769621/dealing-with-commas-in-a-csv-file
	// http://answers.unity3d.com/questions/144200/are-there-any-csv-reader-for-unity3d-without-needi.html
	public static class CSVSupport
	{
		public static string Escape( string s )
		{
			s = s.Replace("\n", "\\n");
			
			// 如果文本中包含 {c} 占位符，需要先转义，避免后续处理时误替换
			// 用户如果想在文本中显示 {c} 本身，需要使用 {{c}}
			s = s.Replace(COMMA_PLACEHOLDER_ESCAPED, COMMA_PLACEHOLDER_TEMP);
			
			// 将文本中的逗号替换为 {c} 占位符，避免被 CSV 解析器分割
			// 这样用户在文本中使用逗号时，会自动转换为 {c}，不会被 CSV 分割
			s = s.Replace(",", COMMA_PLACEHOLDER);
			
			// 恢复转义的 {{c}} 为 {c}（这样 {{c}} 会保持为 {{c}}，不会被替换）
			s = s.Replace(COMMA_PLACEHOLDER_TEMP, COMMA_PLACEHOLDER_ESCAPED);
			
			if ( s.Contains( QUOTE ) )
				s = s.Replace( QUOTE, ESCAPED_QUOTE );
			
			// 如果包含逗号或 {c} 占位符，需要用引号包裹
			if ( s.IndexOfAny( CHARACTERS_THAT_MUST_BE_QUOTED ) > -1 || s.Contains(COMMA_PLACEHOLDER))
				s = QUOTE + s + QUOTE;
			
			return s;
		}
		
		public static string Unescape( string s )
		{
			s = s.Replace("\\n", "\n");

			if ( s.StartsWith( QUOTE ) && s.EndsWith( QUOTE ) )
			{
				s = s.Substring( 1, s.Length - 2 );
				
				if ( s.Contains( ESCAPED_QUOTE ) )
					s = s.Replace( ESCAPED_QUOTE, QUOTE );
			}
			
			// 先将转义的 {{c}} 替换为临时占位符，避免被误替换
			s = s.Replace(COMMA_PLACEHOLDER_ESCAPED, COMMA_PLACEHOLDER_TEMP);
			
			// 将 {c} 占位符替换回逗号（显示时恢复）
			s = s.Replace(COMMA_PLACEHOLDER, ",");
			
			// 恢复转义的 {{c}} 为 {c}（如果用户想在文本中显示 {c}）
			s = s.Replace(COMMA_PLACEHOLDER_TEMP, COMMA_PLACEHOLDER);
			
			return s;
		}
				
		private const string QUOTE = "\"";
		private const string ESCAPED_QUOTE = "\"\"";
		private const string COMMA_PLACEHOLDER = "{c}"; // 逗号占位符
		private const string COMMA_PLACEHOLDER_ESCAPED = "{{c}}"; // 转义的 {c}（如果用户想在文本中显示 {c}）
		private const string COMMA_PLACEHOLDER_TEMP = "__COMMA_PLACEHOLDER_TEMP__"; // 临时占位符
		private static char[] CHARACTERS_THAT_MUST_BE_QUOTED = { ',', '"', '\n' };
	}
	
}
