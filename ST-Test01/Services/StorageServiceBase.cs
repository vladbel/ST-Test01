using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace ST_Test01.Services
{
    public abstract class BaseStorageService
    {
        protected abstract StorageFolder Folder { get; }

        protected abstract string FolderName { get; }

        protected BaseStorageService()
        {
        }

        protected virtual async Task<StorageFile> GetFileAsync(string key)
        {
            var folder = await GetSubFolderAsync();
            var files = await folder.GetFilesAsync();
            var filename = GetSafeFileName(key);
            var file = files.FirstOrDefault(f => f.Name.StartsWith(filename));
            return file;
        }

        protected async Task<StorageFolder> GetSubFolderAsync()
        {
            var folders = await Folder.GetFoldersAsync();
            StorageFolder subFolder = folders.FirstOrDefault(f => f.Name == FolderName);
            if (subFolder == null)
                subFolder = await Folder.CreateFolderAsync(FolderName, CreationCollisionOption.OpenIfExists);
            return subFolder;
        }

        private string GetSafeFileName(string unsafeName)
        {
            var encodedKey = System.Net.WebUtility.UrlEncode(unsafeName);
            var hashAlgorithm = global::Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm("MD5");
            var hash = hashAlgorithm.CreateHash();
            var keyBuffer = global::Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(encodedKey, global::Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
            hash.Append(keyBuffer);
            var hashBuffer = hash.GetValueAndReset();
            var safeFileName = global::Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(hashBuffer);
            return safeFileName;
        }

        protected async Task<StorageFile> CreateFileAsync(string name, string extension, byte[] bytes)
        {
            return await CreateFileInternalAsync(name, extension, bytes, CreateFileFromBytesAsync);
        }

        protected async Task<StorageFile> CreateFileAsync(string name, string extension, string text)
        {
            return await CreateFileInternalAsync(name, extension, text, CreateFileFromTextAsync);
        }

        protected async Task<StorageFile> CreateFileAsync(string name, string extension)
        {
            return await CreateFileInternalAsync(name, extension, "", CreateEmptyFileAsync);
        }

        private async Task<StorageFile> CreateFileInternalAsync<DataType>(string name, string extension, 
                                        DataType data,
                                        Func<StorageFolder, string, DataType, Task<StorageFile>> createFileAsyncFunc)
        {
            var folder = await GetSubFolderAsync();
            var filename = GetSafeFileName(name);
            var fullFilename = filename + "." + extension;
            StorageFile file;
            try
            {
                file = await createFileAsyncFunc(folder, fullFilename, data);
                return file;
            }
            catch (UnauthorizedAccessException ex)
            {
                //Sometimes an access denied exception happens when the file is actively being used (like an image being displayed)
                //Since async cannot happen in catch, let this case fall through to create an access exception bypass file
                
            }
            catch (Exception ex)
            {
                //No other exception is expected, should log this, will let the later logic see if a bypass file can get created instead
                
            }
            var accessExceptionBypassFiles = (await folder.GetFilesAsync()).Where(f => f.Name.StartsWith(filename + "_") && f.Name.EndsWith("." + extension));
            foreach (var accessExceptionBypassFile in accessExceptionBypassFiles)
            {
                try { await accessExceptionBypassFile.DeleteAsync(); }
                catch { /* It is still possible a bypass file is an active file, although less likely, either way, it will get cleaned up on a later attempt */ }
            }
            fullFilename = filename + "_" + Guid.NewGuid().ToString("n") + "." + extension;
            try
            {
                file = await createFileAsyncFunc(folder, fullFilename, data);
                return file;
            }
            catch
            {
                //This is not expected, but we don't want a crash.
                return null;
            }
        }

        private async Task<StorageFile> CreateFileFromBytesAsync(StorageFolder folder, string fullFilename, byte[] bytes)
        {
            StorageFile file = await folder.CreateFileAsync(fullFilename, CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteBytesAsync(file, bytes);
            return file;
        }

        private async Task<StorageFile> CreateFileFromTextAsync(StorageFolder folder, string fullFilename, string text)
        {
            StorageFile file = await folder.CreateFileAsync(fullFilename, CreationCollisionOption.GenerateUniqueName);
            await FileIO.WriteTextAsync(file, text);
            return file;
        }

        private async Task<StorageFile> CreateEmptyFileAsync(StorageFolder folder, string fullFilename, string data)
        {
            StorageFile file = await folder.CreateFileAsync(fullFilename, CreationCollisionOption.GenerateUniqueName);
            return file;
        }
    }
}
