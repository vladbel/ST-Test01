using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
        }

        private string _imageSource;
        public string ImageSource
        {
            get
            {
                return _imageSource;
            }

            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public ICommand ImageAction { get; set; }

        private async Task ImageActionAsync()
        {
            ImageSource = "https://smartthings-plus.s3.amazonaws.com/category-icons/garden-icon%402x.png";
            await Task.FromResult(false);
        }
    }
}
