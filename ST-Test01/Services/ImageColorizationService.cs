using Lumia.Imaging;
using Lumia.Imaging.Adjustments;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace ST_Test01.Services
{
    public class ImageColorizationService
    {
        private StorageManagerService _fileStorageService;
        private IHttpProxyService _httpProxyService;

        public ImageColorizationService(StorageManagerService fileStorageService,
                                          IHttpProxyService httpProxyService)
        {
            _fileStorageService = fileStorageService;
            _httpProxyService = httpProxyService;
        }

        public async Task<Uri> ApplyColorizationFilterAndSaveAsync(string externalUri)
        {
            var getBytesResponse = await _httpProxyService.GetBytesAsync(new Uri(externalUri), CancellationToken.None);
            var imageBytes = getBytesResponse.Item2;

            // Manipulate image color
            IBuffer inputBuffer = imageBytes.AsBuffer();
            var bufferImageSource = new /*Lumia.Imaging.*/BufferImageSource(inputBuffer);

            var output = new /*Windows.UI.Xaml.Media.Imaging.*/WriteableBitmap((int)bufferImageSource.ImageSize.Width,
                                                                               (int)bufferImageSource.ImageSize.Height);
            await ColorizeImageInternalAsync(bufferImageSource, output);

            bufferImageSource.Dispose();

            var localUri = await SaveImageAsync(output, externalUri);

            return localUri;
        }

        private async Task<Uri> SaveImageAsync(WriteableBitmap writableBitmap, string key)
        {

            //var uri =  await _fileStorageService.CreateFileAsync(key, "png");

            var storageFile = (StorageFile)await _fileStorageService.GetFileObjectAsync(key);

            IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,
                                                                                stream);
            // Get pixels of the WriteableBitmap object 
            Stream pixelStream = writableBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                 BitmapAlphaMode.Straight,
                                 (uint)writableBitmap.PixelWidth,
                                 (uint)writableBitmap.PixelHeight,
                                 96.0,
                                 96.0,
                                 pixels);
            await encoder.FlushAsync();

            stream.Dispose();
            pixelStream.Dispose();

            return _fileStorageService.GetUriFromFile(storageFile);
        }

        private async Task ColorizeImageInternalAsync(/*Lumia.Imaging.*/IImageProvider inputImage, WriteableBitmap output)
        {
            using (var colorizationEffect = new ColorizationEffect(inputImage, new global::Windows.UI.Color() { R = 255, G = 0, B = 255 }, 1.0, 1.0)) // Create effect with the source stream
            using (var renderer = new WriteableBitmapRenderer(colorizationEffect, output))
            {
                await renderer.RenderAsync();
                output.Invalidate();
            }
        }
    }
}
