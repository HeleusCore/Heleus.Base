using System;
using System.IO;

namespace Heleus.Base
{
	public static class EmbeddedResource
	{
		public static byte[] GetEmbeddedResource<T>(string name)
		{
			var assembly = typeof(T).Assembly;
			var resourceNames = assembly.GetManifestResourceNames();
			foreach (var resource in resourceNames)
			{
				if (resource.EndsWith(name, StringComparison.Ordinal))
				{
					using (var resourceStream = assembly.GetManifestResourceStream(resource))
					{
						using (var memoryStream = new MemoryStream())
						{
							resourceStream.CopyTo(memoryStream);
							return memoryStream.ToArray();
						}
					}
				}
			}

			return null;
		}
	}
}
