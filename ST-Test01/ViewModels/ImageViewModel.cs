using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ST_Test01.Services;
using System.Threading;
using System.Diagnostics;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Lumia.Imaging;
using Lumia.Imaging.Adjustments;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage;

namespace ST_Test01.ViewModels
{
    public class ImageViewModel: BaseViewModel
    {

        public ImageViewModel()
        {

            ImageAction = new RelayCommand(
                async () => 
                {
                    await ImageActionAsync();
                }
            );

            SaveAction = new RelayCommand(
                async () =>
                {
                    await SaveImageAsync();
                }
            );
            LaunchAsyncProp = new RelayCommand(
                () =>
                {
                    ImageDownloadAndColorizeTask = new NotifyTaskCompletion<string>(DownloadAdnApplyFilterAndSave());

                }
             );
        }

        private ImageSource _downloadedImage;
        public ImageSource DownloadedImage
        {
            get
            {
                return _downloadedImage;
            }

            set
            {
                _downloadedImage = value;
                OnPropertyChanged();
            }
        }

        private ImageSource _colorizedImage;
        public ImageSource ColorizedImage
        {
            get
            {
                return _colorizedImage;
            }

            set
            {
                _colorizedImage = value;
                OnPropertyChanged();
            }
        }

        private string _savedImage;
        public string SavedImage
        {
            get
            {
                return _savedImage;
            }

            set
            {
                _savedImage = value;
                OnPropertyChanged();
            }
        }



        public ICommand ImageAction { get; set; }

        public ICommand SaveAction { get; set; }

        private async Task SaveImageAsync()
        {
            var writeableBitmap = (WriteableBitmap)ColorizedImage;

            var storageService = new StorageManagerService();


            StorageFile savefile = await storageService.CreateFileAsync(Guid.NewGuid().ToString(), "png");
            if (savefile == null)
                return;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,
                                                                                stream);
            // Get pixels of the WriteableBitmap object 
            Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);

            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                 BitmapAlphaMode.Straight,
                                 (uint)writeableBitmap.PixelWidth,
                                 (uint)writeableBitmap.PixelHeight,
                                 96.0,
                                 96.0,
                                 pixels);
            await encoder.FlushAsync();

            stream.Dispose();

            var fileUri = storageService.GetUriFromFile(savefile);
            SavedImage = fileUri.AbsoluteUri;
        }

        private async Task ImageActionAsync()
        {
            var remoteUri = new Uri("https://smartthings-plus.s3.amazonaws.com/category-icons/garden-icon%402x.png");
            

            // Download image
            IHttpProxyService httpProxyService = new HttpProxyService();

            Tuple<string, byte[]> result = null;
            try
            {
                result = await httpProxyService.GetBytesAsync(remoteUri, cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            // raw image
            var bitmapImage01 = new BitmapImage();
            await bitmapImage01.SetSourceAsync(new MemoryStream(result.Item2).AsRandomAccessStream());
            DownloadedImage = bitmapImage01;

            // applyed colorization effect
            ColorizedImage = await ProcessImage( result.Item2);
        }


        private async Task<ImageSource> ProcessImage( byte[] inputBytes)
        {
            // Manipulate image color
            IBuffer inputBuffer = inputBytes.AsBuffer();

            /*Lumia.Imaging.*/BufferImageSource bufferImageSource = null;
            bufferImageSource = new /*Lumia.Imaging.*/BufferImageSource(inputBuffer);

            var output = new /*Windows.UI.Xaml.Media.Imaging.*/WriteableBitmap((int)bufferImageSource.ImageSize.Width,
                                                                               (int)bufferImageSource.ImageSize.Height);
            await ColorizeImageInternalAsync(bufferImageSource, output);

            return output;
        }

        private async Task ColorizeImageInternalAsync(/*Lumia.Imaging.*/IImageProvider inputImage, 
                                                       WriteableBitmap output)
        {
            // Colorize image with "Purple" effect
            using (var colorizationEffect = new ColorizationEffect(inputImage,
                                                                   new Windows.UI.Color() { R = 255, G = 0, B = 255 }, 1.0, 1.0)) // Create effect with the source stream
            using (var renderer = new WriteableBitmapRenderer(colorizationEffect, output))
            {
                await renderer.RenderAsync();
                output.Invalidate();
            }
        }

        #region "Async property"

        public ICommand LaunchAsyncProp { get; set; }

        private NotifyTaskCompletion<string> _imageDownloadAndColorizeTask;
        public NotifyTaskCompletion<string> ImageDownloadAndColorizeTask
        {
            get
            {
                return _imageDownloadAndColorizeTask;
            }

            set
            {
                _imageDownloadAndColorizeTask = value;
                OnPropertyChanged();
            }
        }

        private async Task<string> DownloadAdnApplyFilterAndSave()
        {
            IHttpProxyService httpProxyService = new HttpProxyService();
            var storageService = new StorageManagerService();

            var imageColorizationService = new ImageColorizationService(storageService, httpProxyService);

            var localUri = await imageColorizationService.ApplyColorizationFilterAndSaveAsync("https://smartthings-plus.s3.amazonaws.com/category-icons/garden-icon%402x.png");

            return localUri.AbsoluteUri;
        }
        #endregion


    }
}
