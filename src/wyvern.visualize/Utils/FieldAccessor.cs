using System;
using System.Reflection;

namespace Akka.Visualize.Utils
{
	/// <summary>
	/// I wrote this 11 yeas ago :) Still good to use
	/// https://hawkeye.codeplex.com/SourceControl/latest#src/ACorns.Hawkeye.Core/Utils/Accessors/FieldAccesor.cs
	/// </summary>
	internal class FieldAccesor
	{
		private readonly string fieldName;
		private readonly Type targetType;

		private object target;
		private FieldInfo fieldInfo;

		private object value;

		public FieldAccesor(object target, string fieldName)
			: this(target.GetType(), target, fieldName)
		{
		}

		public FieldAccesor(Type targetType, string fieldName)
			: this(targetType, null, fieldName)
		{
		}

		public FieldAccesor(Type targetType, object target, string fieldName)
		{
			this.target = target;
			this.targetType = targetType;
			this.fieldName = fieldName;

			do
			{
				TryReadField(BindingFlags.Default);
				TryReadField(BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				TryReadField(BindingFlags.Static | BindingFlags.FlattenHierarchy);

				TryReadField(BindingFlags.NonPublic | BindingFlags.Instance);

				TryReadField(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				TryReadField(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.GetField);
				TryReadField(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

				TryReadField(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.GetProperty | BindingFlags.IgnoreCase | BindingFlags.IgnoreReturn | BindingFlags.Instance | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty | BindingFlags.SetField | BindingFlags.Static);

				TryReadField(BindingFlags.NonPublic | BindingFlags.Static);
				TryReadField(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.GetField);
				TryReadField(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField);

				if (fieldInfo == null)
				{
					this.targetType = this.targetType.BaseType;
					if (this.targetType == typeof (object))
					{
						// no chance
						break;
					}
				}

			} while (fieldInfo == null);
		}

		private void SearchForField(BindingFlags bindingFlags)
		{
			if (fieldInfo != null)
				return;

			FieldInfo[] allFields = targetType.GetFields(bindingFlags);
			foreach (FieldInfo field in allFields)
			{
				if (field.Name == this.fieldName)
				{
					this.fieldInfo = field;
					return;
				}
			}
		}


		private void TryReadField(BindingFlags bindingFlags)
		{
			if (fieldInfo != null)
				return;
			fieldInfo = targetType.GetField(fieldName, bindingFlags);
			if (fieldInfo == null)
			{
				SearchForField(bindingFlags);
			}
		}

		public object Get()
		{
			value = fieldInfo.GetValue(target);
			return value;
		}

		public object Get(object theTarget)
		{
			return fieldInfo.GetValue(theTarget);
		}

		public void Clear()
		{
			fieldInfo.SetValue(target, null);
		}

		public void Reset()
		{
			fieldInfo.SetValue(target, value);
		}

		public void Set(object newValue)
		{
			fieldInfo.SetValue(target, newValue);
			this.value = newValue;
		}

		public object Target
		{
			get { return target; }
			set { this.target = value; }
		}

		public bool IsValid
		{
			get { return this.fieldInfo != null; }
		}

		public object Value
		{
			get { return this.value; }
		}


		public static object GetValue(object target, string fieldName)
		{
			FieldAccesor field = new FieldAccesor(target, fieldName);
			return field.Get();
		}
	}
}