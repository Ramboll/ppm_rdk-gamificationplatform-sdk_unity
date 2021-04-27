using PatternModifierUI.Common;
using PatternModifierUI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternModifierUI.ViewModels
{
    public class UpdateViewModel : ViewModelBase
    {
        private bool _isActiveViewChecked;
        private bool _isAllViewChecked;

        public UpdateViewModel(UpdateWindowViewModel mainUpdateVM)
        {
            IsActiveViewChecked = true;
        }

        public bool IsActiveViewChecked
        {
            get
            {
                return _isActiveViewChecked;
            }
            set
            {
                _isActiveViewChecked = value;

                if (_isActiveViewChecked)
                {
                    IsAllViewChecked = false;
                }

                OnPropertyChanged("IsActiveViewChecked");
            }
        }


        public bool IsAllViewChecked
        {
            get
            {
                return _isAllViewChecked;
            }
            set
            {
                _isAllViewChecked = value;

                if (_isAllViewChecked)
                {
                    IsActiveViewChecked = false;
                }

                OnPropertyChanged("IsAllViewChecked");
            }
        }
    }
}
