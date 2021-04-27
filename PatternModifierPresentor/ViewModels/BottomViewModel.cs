using PatternModifierUI.Common;
using PatternModifierUI.Properties;
using RebarInterop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Utilities;

namespace PatternModifierUI.ViewModels
{
    public class BottomViewModel : ViewModelBase
    {
        private RelayCommand _resetCommand;
        private RelayCommand _okCommand;
        private RelayCommand _cancelCommand;
        private MainViewModel mainVm;
        public BottomViewModel(MainViewModel _mainVm)
        {
            mainVm = _mainVm;
            ResetCommand = new RelayCommand(ResetCommandButton);
            OkCommand = new RelayCommand(OkCommandButton);
            CancelCommand = new RelayCommand(CancelCommandButton);

            mainVm.mainWindow.Closed += _mainWindow_Closed;

            // Retrieve the resource.
            //Stream currentStream = Assembly.LoadFrom(this.GetType().Assembly.CodeBase).GetManifestResourceStream("RebarAnnotationTool.packages.Icons.Tagline.png");

            UriPath = new Uri("pack://application:,,,/RebarAnnotationTool;Component/packages/Icons/Tagline.png", UriKind.RelativeOrAbsolute);
        }

        public object ImageSource
        {
            get
            {
                BitmapImage image = new BitmapImage();

                try
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = UriPath;
                    image.EndInit();
                }
                catch
                {
                    return DependencyProperty.UnsetValue;
                }

                return image;
            }
        }

        public BitmapImage displayedImage { get; set; }

        public BitmapImage DisplayedImage
        {
            get
            {
                return displayedImage;
            }
            set
            {
                displayedImage = value;

                OnPropertyChanged("DisplayedImage");
            }
        }

        private void _mainWindow_Closed(object sender, EventArgs e)
        {
            CommonData.IsClosed = true;
        }

        private void CancelCommandButton(object obj)
        {
            mainVm.mainWindow.Close();
        }

        public void OkCommandButton(object obj)
        {
            (mainVm.CurrentView as LinePatternStyleViewModel).SaveData();
        }

        public void ResetCommandButton(object obj)
        {
            (mainVm.CurrentView as LinePatternStyleViewModel).ResetData();
        }

        public RelayCommand ResetCommand
        {
            get
            {
                return _resetCommand;
            }
            set
            {
                _resetCommand = value;
                OnPropertyChanged("ResetCommand");
            }
        }

        public RelayCommand OkCommand
        {
            get
            {
                return _okCommand;
            }
            set
            {
                _okCommand = value;
                OnPropertyChanged("OkCommand");
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                return _cancelCommand;
            }
            set
            {
                _cancelCommand = value;
                OnPropertyChanged("CancelCommand");
            }
        }

        public SolidColorBrush SourceColor { get; set; } = (SolidColorBrush)new BrushConverter().ConvertFrom("#009DF0");
        public Uri UriPath { get; private set; }
    }
    public class ImageConverter : IValueConverter
    {
        public object Convert(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new BitmapImage(new Uri(value.ToString()));
        }

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


}
