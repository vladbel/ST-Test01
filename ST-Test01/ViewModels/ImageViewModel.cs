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
                    ImageSource04 = new NotifyTaskCompletion<string>(DownloadAdnApplyFilterAndSave());
                    //ImageSource04.PropertyChanged += ( sender, e) => { OnPropertyChanged("ImageSource04." + e.PropertyName); } ;

                }
             );

            ImageSource03 = "ms-appdata:///temp/images/4bf7efd093c6676777592c2ede9461b6.png";
        }

        private ImageSource _imageSource01;
        public ImageSource ImageSource01
        {
            get
            {
                return _imageSource01;
            }

            set
            {
                _imageSource01 = value;
                OnPropertyChanged();
            }
        }

        private ImageSource _imageSource02;
        public ImageSource ImageSource02
        {
            get
            {
                return _imageSource02;
            }

            set
            {
                _imageSource02 = value;
                OnPropertyChanged();
            }
        }

        private string _imageSource03;
        public string ImageSource03
        {
            get
            {
                return _imageSource03;
            }

            set
            {
                _imageSource03 = value;
                OnPropertyChanged();
            }
        }



        public ICommand ImageAction { get; set; }

        public ICommand SaveAction { get; set; }

        private async Task SaveImageAsync()
        {
            var writeableBitmap = (WriteableBitmap)ImageSource02;

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
            ImageSource03 = fileUri.AbsoluteUri;
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
            ImageSource01 = bitmapImage01;

            // applyed colorization effect

            ImageSource02 = await ProcessImage( result.Item2);
        }


        private async Task<ImageSource> ProcessImage( byte[] inputBytes)
        {
            // Manipulate image color
            IBuffer inputBuffer = inputBytes.AsBuffer();
            var bufferImageSource = new /*Lumia.Imaging.*/BufferImageSource(inputBuffer);


            var output = new /*Windows.UI.Xaml.Media.Imaging.*/WriteableBitmap((int)bufferImageSource.ImageSize.Width,
                                                                               (int)bufferImageSource.ImageSize.Height);
            await ColorizeImageInternalAsync(bufferImageSource, output);

            return output;
        }

        private async Task ColorizeImageInternalAsync(/*Lumia.Imaging.*/IImageProvider inputImage, WriteableBitmap output)
        {
            using (var colorizationEffect = new ColorizationEffect(inputImage, new Windows.UI.Color() { R = 255, G = 0, B = 255 }, 1.0, 1.0)) // Create effect with the source stream
            using (var renderer = new WriteableBitmapRenderer(colorizationEffect, output))
            {
                await renderer.RenderAsync();
                output.Invalidate();
            }
        }

        #region "Async property"

        public ICommand LaunchAsyncProp { get; set; }

        private NotifyTaskCompletion<string> _imageSource04;
        public NotifyTaskCompletion<string> ImageSource04
        {
            get
            {
                return _imageSource04;
            }

            set
            {
                _imageSource04 = value;
                OnPropertyChanged();
            }
        }

        private async Task<string> DownloadAdnApplyFilterAndSave()
        {
            IHttpProxyService httpProxyService = new HttpProxyService();
            var storageService = new StorageManagerService();

            var imageColorizationService = new ImageColorizationService(storageService, httpProxyService);

            var localUri = await imageColorizationService.ApplyColorizationFilterAndSaveAsync("https://smartthings-plus.s3.amazonaws.com/category-icons/garden-icon%402x.png");

            return "ms-appdata:///temp/images/4bf7efd093c6676777592c2ede9461b6.png";

            //return localUri.AbsoluteUri;
        }
        #endregion


    }
}
