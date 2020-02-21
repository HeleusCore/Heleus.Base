using System;
using Heleus.Base;

namespace Heleus.Network.Client
{
	public enum DownloadResultTypes
    {
        Ok,
        Timeout,
        NotFound,
        InvalidChecksum,
        InvalidContentType,
        InvalidSignature,
        Unknown
    }

    public class DownloadTimeoutException : Exception
    {
        public DownloadTimeoutException(Uri endpoint, string path) : base($"DownloadTimeoutException: {(endpoint != null ? new Uri(endpoint, path).ToString() : path)}")
        {

        }
    }

    public class DownloadNotFoundException : Exception
    {
        public DownloadNotFoundException(Uri endpoint, string path) : base($"DownloadNotFoundException: {(endpoint != null ? new Uri(endpoint, path).ToString() : path)}")
        {

        }
    }

    public class DownloadInvalidContentTypeException : Exception
    {
        public DownloadInvalidContentTypeException(Uri endpoint, string path) : base($"DownloadInvalidContentTypeException: {(endpoint != null ? new Uri(endpoint, path).ToString() : path)}")
        {

        }
    }

    public struct Download<T>
    {
        public readonly DownloadResultTypes ResultType;
        public readonly T Data;

        public Download(T data)
        {
            ResultType = DownloadResultTypes.Ok;
            Data = data;
        }

        Download(DownloadResultTypes resultTypes)
        {
            ResultType = resultTypes;
            Data = default(T);
        }

        public static Download<T> HandleException(Exception exception)
        {
            if (exception == null)
                return Unknown;

            Log.IgnoreException(exception);

            if (exception is DownloadTimeoutException)
                return Timeout;
            if (exception is DownloadNotFoundException)
                return NotFound;
            if (exception is DownloadInvalidContentTypeException)
                return InvalidContentType;

            return Unknown;
        }

        public static readonly Download<T> Timeout = new Download<T>(DownloadResultTypes.Timeout);
        public static readonly Download<T> NotFound = new Download<T>(DownloadResultTypes.NotFound);
        public static readonly Download<T> InvalidContentType = new Download<T>(DownloadResultTypes.InvalidContentType);
        public static readonly Download<T> InvalidSignature = new Download<T>(DownloadResultTypes.InvalidSignature);
        public static readonly Download<T> InvalidChecksum = new Download<T>(DownloadResultTypes.InvalidChecksum);
        public static readonly Download<T> Unknown = new Download<T>(DownloadResultTypes.Unknown);
    }
}
