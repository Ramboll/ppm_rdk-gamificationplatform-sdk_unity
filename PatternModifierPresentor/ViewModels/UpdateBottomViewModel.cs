using PatternModifierUI.Common;
using PatternModifierUI.Views;
using RebarInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Utilities;

namespace PatternModifierUI.ViewModels
{
    public class UpdateBottomViewModel : ViewModelBase
    {
        private RelayCommand _okCommand;
        private RelayCommand _cancelCommand;

        public UpdateBottomViewModel(UpdateWindowViewModel _mainUpdateVM)
        {
            mainUpdateVm = _mainUpdateVM;

            dataWriter = _mainUpdateVM.dataWriter;

            OkCommand = new RelayCommand(OkCommandButton);

            CancelCommand = new RelayCommand(CancelCommandButton);
        }

        private void CancelCommandButton(object obj)
        {
            mainUpdateVm.UpdateWindow.Close();
        }

        private void OkCommandButton(object obj)
        {
            mainUpdateVm.UpdateWindow.Close();

            (mainUpdateVm as UpdateWindowViewModel).ApplySettings();
        }

        private UpdateWindowViewModel mainUpdateVm;
        private ReadAndWriteData dataWriter;

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

      
    }
}
