using System;

namespace Heleus.Network.Client
{
	public static class PlatformName
	{
		public const string IOS = "IOS";
		public const string ANDROID = "ADR";
		public const string UWP = "UWP";
		public const string MACOS = "MAC";
		public const string GTK = "GTK";
        public const string WPF = "WPF";
        public const string CLI = "CLI";

        public static bool IsIOS(string platform) => platform == IOS;
        public static bool IsAndroid(string platform) => platform == ANDROID;
        public static bool IsUWP(string platform) => platform == UWP;
        public static bool IsMacOS(string platform) => platform == MACOS;
        public static bool IsGTK(string platform) => platform == GTK;
        public static bool IsWPF(string platform) => platform == WPF;
        public static bool IsCLI(string platform) => platform == CLI;

        public static bool IsValid(string platform)
		{
			if (platform == MACOS || platform == IOS || platform == ANDROID || platform == UWP || platform == GTK || platform == WPF || platform == CLI)
				return false;

			return true;
		}
	}
}
