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
            //ImageSource = "https://smartthings-plus.s3.amazonaws.com/category-icons/music-icon%402x.png";

            ImageAction = new RelayCommand(
                async () => 
                {
                    await ImageActionAsync();
                }
            );

            SaveAction  = new RelayCommand(
                async () =>
                {
                    await SaveImageAsync();
                }
);
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

        private ImageSource _imageSource03;
        public ImageSource ImageSource03
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

            FileSavePicker picker = new FileSavePicker();
            picker.FileTypeChoices.Add("JPG File", new List<string>() { ".jpg" });
            StorageFile savefile = await picker.PickSaveFileAsync();
            if (savefile == null)
                return;
            IRandomAccessStream stream = await savefile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
            // Get pixels of the WriteableBitmap object 
            Stream pixelStream = writeableBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[pixelStream.Length];
            await pixelStream.ReadAsync(pixels, 0, pixels.Length);
            // Save the image file with jpg extension 
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)writeableBitmap.PixelWidth, (uint)writeableBitmap.PixelHeight, 96.0, 96.0, pixels);
            await encoder.FlushAsync();
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


            var output = new /*Windows.UI.Xaml.Media.Imaging.*/WriteableBitmap(120, 120); // size?
            await ColorizeImageInternalAsync(bufferImageSource, output);

            return output;
        }

        private async Task ColorizeImageInternalAsync(/*Lumia.Imaging.*/IImageProvider inputImage, WriteableBitmap output)
        {
            using (var colorizationEffect = new ColorizationEffect(inputImage, new Windows.UI.Color() { R = 0, G = 0, B = 100 }, 1.0, 1.0)) // Create effect with the source stream
            using (var renderer = new WriteableBitmapRenderer(colorizationEffect, output))
            {
                await renderer.RenderAsync();
                output.Invalidate();
            }
        }

        //private static async Task<ImageSource> ColorizeImageInternalAsync01(IImageProvider inputImage)
        //{

        //    // Build the blurry image entirely in memory on the GPU so that
        //    // we can unwind (dispose) the effect graph prior to returning.
        //    IBuffer buffer = null;
        //    using (var colorizationEffect = new ColorizationEffect(inputImage, new Windows.UI.Color() { R = 0, G = 0, B = 100 }, 1.0, 1.0)) // Create effect with the source stream
        //    using (var renderer = new JpegRenderer(colorizationEffect)) // Render to a Jpeg first to get the result image in memory
        //    {
        //        buffer = await renderer.RenderAsync();
        //    }

        //    return await MakeBitmapImage(buffer);
        //}

        private static async Task<BitmapImage> MakeBitmapImage(IBuffer buffer)
        {
            var result = new BitmapImage();
            if (buffer != null)
            {
                await result.SetSourceAsync(buffer.AsStream().AsRandomAccessStream());
            }

            return result;
        }
    }
}
