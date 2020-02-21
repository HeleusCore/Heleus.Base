using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Heleus.Base
{
    public class Storage
    {
        public DirectoryInfo Root
        {
            get;
            private set;
        }

        public readonly bool IsWriteable;

        public Storage(string dataPath)
        {
            try
            {
                Root = new DirectoryInfo(dataPath);

                if (!Root.Exists)
                    Root.Create();
				Root.Refresh();

                IsWriteable = IsDirectoryWriteable(Root);
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }
        }

        public FileInfo GetFilePath(string fileName)
        {
            if(Root == null)
            {
                Log.Error("Storage not initialized!");
                return null;
            }

            var filePath = new FileInfo(Path.Combine(Root.FullName, fileName));
            if (!filePath.FullName.StartsWith(Root.FullName, StringComparison.Ordinal))
            {
                Log.Error($"File is not in the storage path {fileName}.");
                return null;
            }

            return filePath;
        }

        public Task<bool> FileExistsAsync(string fileName)
        {
            return Task.Run(() => FileExists(fileName));
        }

        public bool FileExists(string fileName)
        {
            var fi = GetFilePath(fileName);
            if (fi == null)
                return false;

            return fi.Exists;
        }

        public DirectoryInfo GetDirectoryPath(string directoryName)
        {
            if (Root == null)
            {
                Log.Error("Storage not initialized!");
                return null;
            }

            var directoryPath = new DirectoryInfo(Path.Combine(Root.FullName, directoryName));
            if (!directoryPath.FullName.StartsWith(Root.FullName, StringComparison.Ordinal))
            {
                Log.Error($"Directory is not in the storage path {directoryName}.");
                return null;
            }

            return directoryPath;
        }

        public Task<byte[]> ReadFileBytesAsync(string fileName)
        {
            return Task.Run(() => ReadFileBytes(fileName));
        }

        public byte[] ReadFileBytes(string fileName)
        {
            var filePath = GetFilePath(fileName);
            if (filePath == null)
                return null;
            
            try
            {
                if (filePath.Exists)
                    return File.ReadAllBytes(filePath.FullName);
            }
            catch(Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }

            return null;
        }

        public Task<string> ReadFileTextAsync(string fileName)
        {
            return Task.Run(() => ReadFileText(fileName));
        }

        public string ReadFileText(string fileName)
        {
            var filePath = GetFilePath(fileName);
            if (filePath == null)
                return null;

            try
            {
                if (filePath.Exists)
                    return File.ReadAllText(filePath.FullName);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }

            return null;
        }

        public Task<FileStream> FileReadStreamAsync(string fileName)
        {
            return Task.Run(() => FileReadStream(fileName));
        }

        public FileStream FileReadStream(string fileName)
        {
            var filePath = GetFilePath(fileName);
            if (filePath == null)
                return null;

            try
            {
                return new FileStream(filePath.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }

            return null;
        }

        public Task<FileStream> FileStreamAsync(string fileName, FileAccess fileAccess = FileAccess.Read)
        {
            return Task.Run(() => FileStream(fileName, fileAccess));
        }

        public FileStream FileStream(string fileName, FileAccess fileAccess = FileAccess.Read)
        {
            var filePath = GetFilePath(fileName);
            if (filePath == null)
                return null;

            try
            {
                return new FileStream(filePath.FullName, FileMode.OpenOrCreate, fileAccess, FileShare.ReadWrite);
            }
            catch(Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }

            return null;
        }

        public FileStream FileTempStream()
        {
            try
            {
                return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
            }

            return null;
        }

        public Task<bool> WriteFileBytesAsync(string fileName, byte[] data)
        {
            return Task.Run(() => WriteFileBytes(fileName, data));
        }

        public bool WriteFileBytes(string fileName, byte[] data)
        {
            var filePath = GetFilePath(fileName);
            if (filePath == null || data == null)
                return false;

            try
            {
                File.WriteAllBytes(filePath.FullName, data);
                return true;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }

            return false;
        }

        public Task<bool> WriteFileTextAsync(string fileName, string data)
        {
            return Task.Run(() => WriteFileText(fileName, data));
        }

        public bool WriteFileText(string fileName, string data)
        {
            var filePath = GetFilePath(fileName);
            if (filePath == null || data == null)
                return false;

            try
            {
                File.WriteAllText(filePath.FullName, data);
                return true;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }

            return false;
        }

        public void DeleteFile(string fileName)
        {
            var filePath = GetFilePath(fileName);
            if (filePath == null)
                return;

            try
            {
                if (filePath.Exists)
                    File.Delete(filePath.FullName);
            }
            catch(Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }
        }

        public bool MoveFile(string sourceFile, string targetFile, bool overwrite)
        {
            var sourcePath = GetFilePath(sourceFile);
            var targetPath = GetFilePath(targetFile);

            if (sourcePath == null || targetPath == null)
                return false;

            if (!sourcePath.Exists || (!overwrite && targetPath.Exists))
                return false;

            try
            {
                if (targetPath.Exists)
                    File.Delete(targetPath.FullName);

                File.Move(sourcePath.FullName, targetPath.FullName);
                return true;
            }
            catch(Exception ex)
            {
                Log.HandleException(ex);
            }
            return false;
        }

        public void DeleteDirectory(string directoryName)
        {
            var directoryPath = GetDirectoryPath(directoryName);
            if (directoryPath == null || !directoryPath.Exists)
                return;

            try
            {
                Directory.Delete(directoryPath.FullName, true);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }
        }

        public bool CreateDirectory(string directoryName)
        {
            var directoryPath = GetDirectoryPath(directoryName);
            if (directoryPath == null)
                return false;

            try
            {
                Directory.CreateDirectory(directoryPath.FullName);
            }
            catch(Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }
            return IsDirectoryWriteable(directoryPath);
        }

        public bool IsDirectoryWriteable(DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
                return false;

            if (!directoryInfo.Exists)
                return false;

			directoryInfo.Refresh();
            var guid = Guid.NewGuid();
            var testFile = Path.Combine(directoryInfo.FullName, guid.ToString());

            if (File.Exists(testFile))
                return true;

            try
            {
                File.WriteAllText(testFile, guid.ToString());
                var content = File.ReadAllText(testFile);
                File.Delete(testFile);

                if (content == guid.ToString())
                    return true;

            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            return false;
        }

        public FileInfo[] GetFiles(string directoryName,  string pattern)
        {
            var directoryPath = GetDirectoryPath(directoryName);
            if (directoryPath == null || !directoryPath.Exists)
                goto end;

            try
            {
                return directoryPath.GetFiles(pattern);
            }
            catch(Exception ex)
            {
                Log.HandleException(ex, LogLevels.Warning);
            }

            end:
            return new FileInfo[0];
        }
    }
}
