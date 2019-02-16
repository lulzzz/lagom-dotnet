using System;

namespace Akka.Visualize.Utils
{
	public static class StringUtils
	{
		public static string After(this string value, string from, StringComparison comparison = StringComparison.CurrentCulture)
		{
			if (value == null)
				return null;
			int startIndex = value.IndexOf(from, comparison);
			if (startIndex >= 0)
				return value.Substring(startIndex + from.Length);
			else
				return null;
		}

		public static string UpTo(this string value, string end)
		{
			if (value == null)
				return null;

			if (end == null)
				return value;

			int startIndex = value.IndexOf(end);
			if (startIndex > 0)
				return value.Substring(0, startIndex);
			else
				return value;
		}
	}
}
