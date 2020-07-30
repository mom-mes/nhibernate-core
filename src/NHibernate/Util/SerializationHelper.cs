using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NHibernate.Util
{
	public static partial class SerializationHelper
	{
		public static byte[] Serialize(object obj)
		{
			var formatter = CreateFormatter();
			using (var stream = new MemoryStream())
			{
				formatter.Serialize(stream, obj);
				return stream.ToArray();
			}
		}

		public static object Deserialize(byte[] data)
		{
			var formatter = CreateFormatter();
			using (var stream = new MemoryStream(data))
			{
				formatter.Binder = NetCoreNetFrameworkBinder.Instance;
				return formatter.Deserialize(stream);
			}
		}

		internal static void AddValueArray<T>(this SerializationInfo info, string name, T[] values)
		{
			info.AddValue($"{name}.Length", values?.Length);
			if (values == null)
				return;

			for (var i = 0; i < values.Length; i++)
				info.AddValue($"{name}[{i}]", values[i]);
		}

		internal static T[] GetValueArray<T>(this SerializationInfo info, string name)
		{
			var length = info.GetValue<int?>($"{name}.Length");
			if (length == null)
				return null;

			var result = new T[length.Value];
			for (var i = 0; i < result.Length; i++)
				result[i] = info.GetValue<T>($"{name}[{i}]");
			return result;
		}

		internal static T GetValue<T>(this SerializationInfo info, string name)
		{
			return (T)info.GetValue(name, typeof(T));
		}

		public static BinaryFormatter CreateFormatter()
		{
			return new BinaryFormatter
			{
				SurrogateSelector = new SurrogateSelector()
			};
		}
	}

	public class NetCoreNetFrameworkBinder : SerializationBinder
	{
		internal const string NETFX_VERSION = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
		internal const string NETCORE_VERSION = "System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";

		public static readonly bool IsRunningOnNetFramework = typeof(string).AssemblyQualifiedName.Contains("mscorlib");

		public override System.Type BindToType(string assemblyName, string typeName)
		{
			if (IsRunningOnNetFramework)
			{
				typeName = typeName.Replace(NETCORE_VERSION, NETFX_VERSION);
			}
			else
			{
				typeName = typeName.Replace(NETFX_VERSION, NETCORE_VERSION);
			}

			return System.Type.GetType(typeName);

		}

		public static NetCoreNetFrameworkBinder Instance { get; } = new NetCoreNetFrameworkBinder();
	}

	internal static class PlatformExtensions
	{
		public static string FixPlatformTypeName(this string typeName)
		{
			if (NetCoreNetFrameworkBinder.IsRunningOnNetFramework)
			{
				typeName = typeName.Replace(NetCoreNetFrameworkBinder.NETCORE_VERSION, NetCoreNetFrameworkBinder.NETFX_VERSION);
			}
			else
			{
				typeName = typeName.Replace(NetCoreNetFrameworkBinder.NETFX_VERSION, NetCoreNetFrameworkBinder.NETCORE_VERSION);
			}

			return typeName;
		}
	}
}