using PatternModifierUI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatternModifierUI.ViewModels
{
    public class ProgressBarViewModel : ViewModelBase
    {
        private UpdateWindowViewModel mainUpdateVm;
        private string _progressLabel { get; set; }

        public ProgressBarViewModel(UpdateWindowViewModel _mainUpdateVm)
        {
            this.mainUpdateVm = _mainUpdateVm;
        }

        public string ProgressMainLabel
        {
            get
            {
                return _progressLabel;
            }
            set
            {
                _progressLabel = value;

                OnPropertyChanged("ProgressMainLabel");
            }
        }
    }
}
