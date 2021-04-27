using PatternModifierUI.Common;
using PatternModifierUI.Views;
using RebarInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Utilities;

namespace PatternModifierUI.ViewModels
{
    public class UpdateWindowViewModel : ViewModelBase
    {

        public UpdateWindowViewModel(ReadAndWriteData _dataWriter)
        {
            UpdateWindow = new UpdateWindow();

            UpdateWindow.DataContext = this;

            dataWriter = _dataWriter;

            UpdateViewModel updateViewModel = new UpdateViewModel(this);

            CurrentUpdateView = updateViewModel;

            UpdateBottomViewModel bottomViewModel = new UpdateBottomViewModel(this);

            UpdateBottomView = bottomViewModel;

        }
        private ViewModelBase currentUpdateview;

        public bool IsCheckedAllView { get; private set; }

        public UpdateWindow UpdateWindow;
        private ViewModelBase _updateBottomView;
        public ReadAndWriteData dataWriter;

        public ViewModelBase CurrentUpdateView
        {
            get
            {
                return currentUpdateview;
            }
            set
            {
                currentUpdateview = value;
                OnPropertyChanged("CurrentUpdateView");
            }
        }

        public ViewModelBase UpdateBottomView
        {
            get
            {
                return _updateBottomView;
            }
            set
            {
                _updateBottomView = value;
                OnPropertyChanged("UpdateBottomView");
            }
        }

        public Uri UpdateUriImage { get;  set; }

        public void ApplySettings()
        {
            IsCheckedAllView = (CurrentUpdateView as UpdateViewModel).IsAllViewChecked;

            Dispatcher dispatcher = null;
            var newWindowThread = new Thread(() =>
            {
                UpdateWindow = new UpdateWindow();
                UpdateWindow.DataContext = this;
                ProgressBarViewModel progressBarView = new ProgressBarViewModel(this);

                progressBarView.ProgressMainLabel = IsCheckedAllView ? "Annotate Is Update In All Views" : "Annotate is Update In Active View";

                UpdateWindow.Height = UpdateWindow.Height - 60;
                CurrentUpdateView = progressBarView;
                UpdateWindow.ShowDialog();
            });

            newWindowThread.SetApartmentState(ApartmentState.STA);
            newWindowThread.IsBackground = true;
            newWindowThread.Start();
            while (dispatcher == null)
            {
                RunAnnotateUpdateProcess();
                dispatcher = Dispatcher.FromThread(newWindowThread);
            }

            newWindowThread.Abort();

        }

        private void OnWorkerMethodComplete()
        {
            UpdateWindow.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
            new Action(
            delegate ()
            {
                UpdateWindow.Close();
            }
            ));
        }

        private void RunAnnotateUpdateProcess()
        {
            try
            {
                bool isUpdated = false;

                if (IsCheckedAllView)
                {
                    isUpdated = dataWriter.UpdateElement(true, false);
                }
                else
                {
                    isUpdated = dataWriter.UpdateElement(false, true);
                }

                if (isUpdated && dataWriter.LstSelectedElementDataInfo != null && dataWriter.LstSelectedElementDataInfo.Any())
                {
                    bool isPlacedInDoc = dataWriter.PlaceAnnotationWithRespectiveFamily();

                    dataWriter.ChangeLinePatternForElements();
                }

                OnWorkerMethodComplete();

                if (isUpdated)
                {
                    MessageBox.Show("Annotation Family Updated Successfully...", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (!isUpdated)
                {
                    MessageBox.Show("Annotation Family Not Placed To Update", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ApplyProgress()
        {
            UpdateWindow.Dispatcher.Invoke(() =>
            {
                UpdateWindow.ShowDialog();
            });
        }
    }
}
