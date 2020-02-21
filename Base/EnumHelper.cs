using System;
using System.Reflection;

namespace Heleus.Base
{
	public static class EnumHelper
	{
		static void CheckEnum<T>()
		{
			if (typeof(T).GetTypeInfo().BaseType != typeof(Enum))
				throw new ArgumentException("T must be of type System.Enum");
		}

		public static T[] EnumValues<T>()
		{
			CheckEnum<T> ();
			return (T[])Enum.GetValues(typeof(T));
		}

		public static string[] EnumNames<T>()
		{
			CheckEnum<T> ();
			return Enum.GetNames (typeof(T));
		}

		public static bool IsEnumValue<T>(T value)
		{
			CheckEnum<T> ();
			var values = EnumValues<T> ();
			for (int i = 0; i < values.Length; i++) {
				if (values [i].Equals(value))
					return true;
			}
			return false;
		}

		public static T StringToEnum<T>(string name, T defaultValue)
		{
			CheckEnum<T> ();
			var values = EnumValues<T> ();
			for(int i = 0; i < values.Length; i++) {

				T value = values[i];
				string currentName = Enum.GetName (typeof(T), value);
				if (currentName == name)
					return value;
			}
			return defaultValue;
		}
	}
}

