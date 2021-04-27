using PatternModifierUI.Common;
using PatternModifierUI.Views;
using RebarInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Utilities;

namespace PatternModifierUI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase currentview;
        private ViewModelBase bottomView;
        public MainWindow mainWindow;
        public ReadAndWriteData dataReader;
        public string docPathName;
        public bool IsSettingsSaveSuccessfully;

        public RebarSettingsData RebarSettingDataInfo { get; set; }

        public MainViewModel(ReadAndWriteData _dataReader)
        {
            docPathName = _dataReader.docPathName;

            dataReader = _dataReader;

            mainWindow = new MainWindow();

            mainWindow.DataContext = this;


            if (_dataReader.CommandButtonType == CommandClickType.LinePatternStyle)
            {
                LinePatternStyleViewModel patternViewModel = new LinePatternStyleViewModel(this);

                CurrentView = patternViewModel;
            }
            
            BottomViewModel bottomViewModel = new BottomViewModel(this);

            BottomView = bottomViewModel;

            //SettingUriImage = new Uri("pack://application:,,,/RebarAnnotationTool;Component/packages/Icons/Settings.png", UriKind.RelativeOrAbsolute);

            //CloseUriImage = new Uri("pack://application:,,,/RebarAnnotationTool;Component/packages/Icons/Settings.png", UriKind.RelativeOrAbsolute);

        }

        public object SettingImageSource
        {
            get
            {
                BitmapImage image = new BitmapImage();

                try
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = SettingUriImage;
                    image.EndInit();
                }
                catch
                {
                    return DependencyProperty.UnsetValue;
                }

                return image;
            }
        }

        public object CloseImageSource
        {
            get
            {
                BitmapImage image = new BitmapImage();

                try
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = CloseUriImage;
                    image.EndInit();
                }
                catch
                {
                    return DependencyProperty.UnsetValue;
                }

                return image;
            }
        }

        public ViewModelBase CurrentView
        {
            get
            {
                return currentview;
            }
            set
            {
                currentview = value;
                OnPropertyChanged("CurrentView");
            }
        }

        public ViewModelBase BottomView
        {
            get
            {
                return bottomView;
            }
            set
            {
                bottomView = value;
                OnPropertyChanged("BottomView");
            }
        }

        public Uri SettingUriImage { get;  set; }
        public Uri CloseUriImage { get;  set; }
    }
}
