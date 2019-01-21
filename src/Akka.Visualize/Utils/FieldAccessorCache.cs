using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Visualize.Utils
{
	internal static class FieldAccessorCache
	{
		private static ConcurrentDictionary<string, FieldAccesor> _accessors = new ConcurrentDictionary<string, FieldAccesor>();

		public static FieldAccesor Get(Type type, string fieldName)
		{
			var fullName = $"{type.FullName}.{fieldName}";
			FieldAccesor result;
			if (!_accessors.TryGetValue(fullName, out result))
			{
				result = new FieldAccesor(type, fieldName);
				_accessors.TryAdd(fullName, result);
			}
			return result;
		}
	}
}
