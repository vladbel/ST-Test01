
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;

namespace ST_Test01.Services
{
    public  class StorageManagerService : BaseStorageService
    {
        private const string IMAGES_FOLDER_NAME = "images";
        private const string FILE_PREFIX = "ms-appdata:///temp/";

        public StorageManagerService() : base()
        {
        }

        protected override StorageFolder Folder
        {
            get { return ApplicationData.Current.TemporaryFolder; }
        }

        protected override string FolderName
        {
            get { return IMAGES_FOLDER_NAME; }
        }

        async Task<Uri> GetFileAsync(string key)
        {
            var file = await base.GetFileAsync(key);
            return file == null ? null : GetUriFromFile(file);
        }

        public async Task<Uri> CreateFileAsync(string key, string contentType, byte[] bytes)
        {
            var extension = contentType.Split('/').Last().ToLower();
            var file = await base.CreateFileAsync(key, extension, bytes);
            return file == null ? null : GetUriFromFile(file);
        }

        public async Task<StorageFile> CreateFileAsync (string key, string contentType)
        {
            var extension = contentType.Split('/').Last().ToLower();
            var file = await base.CreateFileAsync(key, extension);
            return file;
        }

        public async Task<bool> FileExistsAsync(string localUri)
        {
            var fileName = WebUtility.UrlDecode(
                localUri
                    .Replace(FILE_PREFIX, "")
                    .Replace(FolderName + "/", ""));

            var folder = await GetSubFolderAsync();
            bool fileExists;
            try
            {
                var file = await folder.GetFileAsync(fileName);
                fileExists = file != null;
            }
            catch (Exception e)
            {
                // This shouldn't happen but better safe than sorry.
                fileExists = false;
            }
            return fileExists;
        }

        public Uri GetUriFromFile(IStorageItem file)
        {
            return new Uri(FILE_PREFIX + FolderName + "/" + file.Name);
        }

        public async Task<object> GetFileObjectAsync(string uri)
        {
            var file = await base.GetFileAsync(uri);
            return file;
        }
    }
}

