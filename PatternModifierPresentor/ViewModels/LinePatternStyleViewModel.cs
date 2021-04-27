using Microsoft.Win32;
using PatternModifierUI.Common;
using RebarInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Utilities;

namespace PatternModifierUI.ViewModels
{
    public class LinePatternStyleViewModel : ViewModelBase
    {
        private MainViewModel mainVM;

        public RelayCommand _bottomColorCommand;
        public RelayCommand _topColorCommand;
        public RelayCommand _rebarColorCommand;

        private RelayCommand _rebarEndBrowseCommand;
        private RelayCommand _rebarInPlaneBrowseCommand;
        private RelayCommand _rebarOutPlaneBrowseCommand;
        private RelayCommand _rebarInclinedBrowseCommand;


        public LineColorInfo SelectedCustomBottomColor { get; set; }
        public LineColorInfo SelectedCustomTopColor { get; set; }
        public LineColorInfo SelectedCustomRebarColor { get; set; }

        private ObservableCollection<LinePatternInfo> _lstProjectLines { get; set; }
        private ObservableCollection<LinePatternInfo> _lstBottomProjectLines { get; set; }

        private ObservableCollection<LineWeightInfo> _lstWeightLines { get; set; }

        public LinePatternStyleViewModel(MainViewModel _mainVm)
        {
            mainVM = _mainVm;


            RebarEndBorderColor = Brushes.DarkGray;
            RebarInPlaneBorderColor = Brushes.DarkGray;
            RebarOutPlaneBorderColor = Brushes.DarkGray;
            RebarInclinedBorderColor = Brushes.DarkGray;

            BottomColorCommandButton = new RelayCommand(BottomColorClick);
            TopColorCommandButton = new RelayCommand(TopColorClick);
            RebarColorCommandButton = new RelayCommand(RebarColorClick);

            RebarEndBrowseCommand = new RelayCommand(RebarEndBrowseClick);
            RebarInPlaneBrowseCommand = new RelayCommand(RebarInPlaneBrowseClick);
            RebarOutPlaneBrowseCommand = new RelayCommand(RebarOutPlaneBrowseClick);
            RebarInclinedBrowseCommand = new RelayCommand(RebarInclinedBrowseClick);

            ReadAndWriteData _dataReader = _mainVm.dataReader;


            LstProjectionLines = new ObservableCollection<LinePatternInfo>(_dataReader.LstLinePatternInfo);


            List<LinePatternInfo> lstBottomLineInfo = _dataReader.LstLinePatternInfo.Where(x => x.Name != "None").Select(x => x).ToList();

            LstBottomProjectionLines = new ObservableCollection<LinePatternInfo>(lstBottomLineInfo);

            LstLineWeights = new ObservableCollection<LineWeightInfo>(_dataReader.LstLineWeightInfo);

            RebarSettingsData rebarExistsSettingsData = default(RebarSettingsData);

            if (mainVM.dataReader != null && !string.IsNullOrEmpty(mainVM.dataReader.UISettingsInfo))
            {
                object RebarUISettingsObj = CommonData.DeserializeObject(typeof(RebarSettingsData), mainVM.dataReader.UISettingsInfo);

                rebarExistsSettingsData = RebarUISettingsObj as RebarSettingsData;
            }

            if (rebarExistsSettingsData != null)
            {
                SelectedBottomLine = LstBottomProjectionLines.Where(x => x.Id.ToString() == rebarExistsSettingsData.BottomRebarData.LinePatternDataInfo.Id.ToString()).Select(x => x).FirstOrDefault();

                SelectedTopLine = LstProjectionLines.Where(x => x.Id.ToString() == rebarExistsSettingsData.TopRebarData.LinePatternDataInfo.Id.ToString()).Select(x => x).FirstOrDefault();

                SelectedRebarLine = LstProjectionLines.Where(x => x.Id.ToString() == rebarExistsSettingsData.FamRebarData.LinePatternDataInfo.Id.ToString()).Select(x => x).FirstOrDefault();

                SelectedBottomWeight = LstLineWeights.Where(x => x.WeightNumber == rebarExistsSettingsData.BottomRebarData.LineWeightDataInfo.WeightNumber).Select(x => x).FirstOrDefault();

                SelectedTopWeight = LstLineWeights.Where(x => x.WeightNumber == rebarExistsSettingsData.TopRebarData.LineWeightDataInfo.WeightNumber).Select(x => x).FirstOrDefault();

                SelectedRebarWeight = LstLineWeights.Where(x => x.WeightNumber == rebarExistsSettingsData.FamRebarData.LineWeightDataInfo.WeightNumber).Select(x => x).FirstOrDefault();


                SelectedBottomColor = (Color)ColorConverter.ConvertFromString(rebarExistsSettingsData.BottomRebarData.LineColorDataInfo.ColorName);


                SelectedBottomColorName = SelectedBottomColor.ToString() == "#00000000" ? "  None" : string.Format("  {0} {1}-{2}-{3}", "RGB", SelectedBottomColor.R.ToString(), SelectedBottomColor.G.ToString(), SelectedBottomColor.B.ToString());

                SelectedTopColor = (Color)ColorConverter.ConvertFromString(rebarExistsSettingsData.TopRebarData.LineColorDataInfo.ColorName);
                SelectedTopColorName = SelectedTopColor.ToString() == "#00000000" ? "  None" : string.Format("  {0} {1}-{2}-{3}", "RGB", SelectedTopColor.R.ToString(), SelectedTopColor.G.ToString(), SelectedTopColor.B.ToString());


                SelectedRebarColor = (Color)ColorConverter.ConvertFromString(rebarExistsSettingsData.FamRebarData.LineColorDataInfo.ColorName);

                SelectedRebarColorName = SelectedRebarColor.ToString() == "#00000000" ? "  None" : string.Format("  {0} {1}-{2}-{3}", "RGB", SelectedRebarColor.R.ToString(), SelectedRebarColor.G.ToString(), SelectedRebarColor.B.ToString());


                if (!string.IsNullOrEmpty(rebarExistsSettingsData.RebarEndPath))
                {
                    RebarEndBrowsePath = rebarExistsSettingsData.RebarEndPath;

                    if (!File.Exists(RebarEndBrowsePath))
                    {
                        RebarEndBorderColor = Brushes.Red;
                    }
                }

                if (!string.IsNullOrEmpty(rebarExistsSettingsData.RebarInPlaneBendPath))
                {
                    RebarInPlaneBrowsePath = rebarExistsSettingsData.RebarInPlaneBendPath;

                    if (!File.Exists(RebarInPlaneBrowsePath))
                    {
                        RebarInPlaneBorderColor = Brushes.Red;
                    }
                }

                if (!string.IsNullOrEmpty(rebarExistsSettingsData.RebarOutPlaneBendPath))
                {
                    RebarOutPlaneBrowsePath = rebarExistsSettingsData.RebarOutPlaneBendPath;

                    if (!File.Exists(RebarOutPlaneBrowsePath))
                    {
                        RebarOutPlaneBorderColor = Brushes.Red;
                    }
                }

                if (!string.IsNullOrEmpty(rebarExistsSettingsData.RebarInclinedPath))
                {
                    RebarInclinedBrowsePath = rebarExistsSettingsData.RebarInclinedPath;

                    if (!File.Exists(RebarInclinedBrowsePath))
                    {
                        RebarInclinedBorderColor = Brushes.Red;
                    }
                }
            }
            else
            {
                SelectedBottomLine = LstBottomProjectionLines.Where(x => x.Name == ConstantValues.DashPatternName).Select(x => x).FirstOrDefault();
                SelectedTopLine = LstProjectionLines.Where(x => x.Name == "None").Select(x => x).FirstOrDefault();
                SelectedRebarLine = LstProjectionLines.Where(x => x.Name == "None").Select(x => x).FirstOrDefault();

                SelectedBottomWeight = LstLineWeights.Where(x => x.WeightNumberInString == "None").Select(x => x).FirstOrDefault();
                SelectedTopWeight = LstLineWeights.Where(x => x.WeightNumberInString == "None").Select(x => x).FirstOrDefault();
                SelectedRebarWeight = LstLineWeights.Where(x => x.WeightNumberInString == "None").Select(x => x).FirstOrDefault();

                Color emptyColor = new Color();

                SelectedBottomColorName = "  None";
                SelectedBottomColor = emptyColor;

                SelectedTopColorName = "  None";
                SelectedTopColor = emptyColor;

                SelectedRebarColorName = "  None";
                SelectedRebarColor = emptyColor;
            }
        }

        private void RebarInclinedBrowseClick(object obj)
        {
            OpenFileDialog openDiag = BrowseFamilyDiag();

            string fileName = openDiag.FileName == null ? string.Empty : openDiag.FileName;

            RebarInclinedBrowsePath = fileName;
        }

        private void RebarOutPlaneBrowseClick(object obj)
        {
            OpenFileDialog openDiag = BrowseFamilyDiag();

            string fileName = openDiag.FileName == null ? string.Empty : openDiag.FileName;

            RebarOutPlaneBrowsePath = fileName;
        }

        private void RebarInPlaneBrowseClick(object obj)
        {
            OpenFileDialog openDiag = BrowseFamilyDiag();

            string fileName = openDiag.FileName == null ? string.Empty : openDiag.FileName;

            RebarInPlaneBrowsePath = fileName;
        }

        private void RebarEndBrowseClick(object obj)
        {
            OpenFileDialog openDiag = BrowseFamilyDiag();

            string fileName = openDiag.FileName == null ? string.Empty : openDiag.FileName;

            RebarEndBrowsePath = fileName;
        }

        public Brush RebarEndBorderColor
        {
            get
            {
                return _rebarEndBorderColor;
            }
            set
            {
                _rebarEndBorderColor = value;

                OnPropertyChanged("RebarEndBorderColor");
            }
        }

        public Brush RebarInPlaneBorderColor
        {
            get
            {
                return _rebarInPlaneBorderColor;
            }
            set
            {
                _rebarInPlaneBorderColor = value;

                OnPropertyChanged("RebarInPlaneBorderColor");
            }
        }

        public Brush RebarOutPlaneBorderColor
        {
            get
            {
                return _rebarOutPlaneBorderColor;
            }
            set
            {
                _rebarOutPlaneBorderColor = value;

                OnPropertyChanged("RebarOutPlaneBorderColor");
            }
        }

        public Brush RebarInclinedBorderColor
        {
            get
            {
                return _rebarInclinedBorderColor;
            }
            set
            {
                _rebarInclinedBorderColor = value;

                OnPropertyChanged("RebarInclinedBorderColor");
            }
        }

        public bool IsInvalidPath { get; set; }

        public string RebarEndBrowsePath
        {
            get
            {
                return _rebarEndBrowsePath;
            }
            set
            {
                _rebarEndBrowsePath = value;

                if (!string.IsNullOrEmpty(_rebarEndBrowsePath) && !File.Exists(_rebarEndBrowsePath))
                {
                    RebarEndBorderColor = Brushes.Red;

                    IsInvalidPath = true;

                }
                else
                {
                    RebarEndBorderColor = Brushes.DarkGray;

                    IsInvalidPath = false;
                }

                OnPropertyChanged("RebarEndBrowsePath");
            }
        }
        public string RebarInPlaneBrowsePath
        {
            get
            {
                return _rebarInPlaneBrowsePath;
            }
            set
            {
                _rebarInPlaneBrowsePath = value;

                if (!string.IsNullOrEmpty(_rebarInPlaneBrowsePath) && !File.Exists(_rebarInPlaneBrowsePath))
                {
                    RebarInPlaneBorderColor = Brushes.Red;

                    IsInvalidPath = true;

                }
                else
                {
                    RebarInPlaneBorderColor = Brushes.DarkGray;

                    IsInvalidPath = false;

                }

                OnPropertyChanged("RebarInPlaneBrowsePath");
            }
        }

        public string RebarOutPlaneBrowsePath
        {
            get
            {
                return _rebarOutPlanePath;
            }
            set
            {
                _rebarOutPlanePath = value;

                if (!string.IsNullOrEmpty(_rebarOutPlanePath) && !File.Exists(_rebarOutPlanePath))
                {
                    RebarOutPlaneBorderColor = Brushes.Red;

                    IsInvalidPath = true;

                }
                else
                {
                    RebarOutPlaneBorderColor = Brushes.DarkGray;

                    IsInvalidPath = false;

                }

                OnPropertyChanged("RebarOutPlaneBrowsePath");
            }
        }

        public string RebarInclinedBrowsePath
        {
            get
            {
                return _rebarInclinedPlanePath;
            }
            set
            {
                _rebarInclinedPlanePath = value;

                if (!string.IsNullOrEmpty(_rebarInclinedPlanePath) && !File.Exists(_rebarInclinedPlanePath))
                {
                    RebarInclinedBorderColor = Brushes.Red;

                    IsInvalidPath = true;
                }
                else
                {
                    RebarInclinedBorderColor = Brushes.DarkGray;

                    IsInvalidPath = false;
                }

                OnPropertyChanged("RebarInclinedBrowsePath");
            }
        }

        private Color GetRedColor()
        {
            Color _color = Color.FromRgb(255, 0, 0);

            return _color;
        }

        public OpenFileDialog BrowseFamilyDiag()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "rfa files (*.rfa)|*.rfa|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.ShowDialog();

            return openFileDialog;
        }


        private void RebarColorClick(object obj)
        {
            System.Windows.Forms.ColorDialog colorDiag = new System.Windows.Forms.ColorDialog();

            System.Windows.Forms.DialogResult diagRes = colorDiag.ShowDialog();

            if (diagRes == System.Windows.Forms.DialogResult.OK)
            {

                SelectedRebarColor = Color.FromRgb(colorDiag.Color.R, colorDiag.Color.G, colorDiag.Color.B);

                SelectedRebarColorName = string.Format("  {0} {1}-{2}-{3}", "RGB", SelectedRebarColor.R.ToString(), SelectedRebarColor.G.ToString(), SelectedRebarColor.B.ToString());
            }
            else if (diagRes == System.Windows.Forms.DialogResult.Cancel)
            {
                Color emptyColor = new Color();
                SelectedRebarColorName = "  None";
                SelectedRebarColor = emptyColor;
            }
        }

        private void TopColorClick(object obj)
        {
            System.Windows.Forms.ColorDialog colorDiag = new System.Windows.Forms.ColorDialog();

            System.Windows.Forms.DialogResult diagRes = colorDiag.ShowDialog();

            if (diagRes == System.Windows.Forms.DialogResult.OK)
            {

                SelectedTopColor = Color.FromRgb(colorDiag.Color.R, colorDiag.Color.G, colorDiag.Color.B);

                SelectedTopColorName = string.Format("  {0} {1}-{2}-{3}", "RGB", SelectedTopColor.R.ToString(), SelectedTopColor.G.ToString(), SelectedTopColor.B.ToString());
            }
            else if (diagRes == System.Windows.Forms.DialogResult.Cancel)
            {
                Color emptyColor = new Color();
                SelectedTopColorName = "  None";
                SelectedTopColor = emptyColor;
            }
        }

        private void BottomColorClick(object obj)
        {
            System.Windows.Forms.ColorDialog colorDiag = new System.Windows.Forms.ColorDialog();

            System.Windows.Forms.DialogResult diagRes = colorDiag.ShowDialog();


            if (diagRes == System.Windows.Forms.DialogResult.OK)
            {

                SelectedBottomColor = Color.FromRgb(colorDiag.Color.R, colorDiag.Color.G, colorDiag.Color.B);

                SelectedBottomColorName = string.Format("  {0} {1}-{2}-{3}", "RGB", SelectedBottomColor.R.ToString(), SelectedBottomColor.G.ToString(), SelectedBottomColor.B.ToString());
            }
            else if (diagRes == System.Windows.Forms.DialogResult.Cancel)
            {
                Color emptyColor = new Color();
                SelectedBottomColorName = "  None";
                SelectedBottomColor = emptyColor;
            }
        }



        public RelayCommand BottomColorCommandButton
        {
            get
            {
                return _bottomColorCommand;
            }
            set
            {
                _bottomColorCommand = value;
                OnPropertyChanged("BottomColorCommandButton");
            }
        }

        public RelayCommand TopColorCommandButton
        {
            get
            {
                return _topColorCommand;
            }
            set
            {
                _topColorCommand = value;
                OnPropertyChanged("TopColorCommandButton");
            }
        }
        public RelayCommand RebarColorCommandButton
        {
            get
            {
                return _rebarColorCommand;
            }
            set
            {
                _rebarColorCommand = value;
                OnPropertyChanged("RebarColorCommandButton");
            }
        }


        public RelayCommand RebarEndBrowseCommand
        {
            get
            {
                return _rebarEndBrowseCommand;
            }
            set
            {
                _rebarEndBrowseCommand = value;
                OnPropertyChanged("RebarEndBrowseCommand");
            }
        }


        public RelayCommand RebarInPlaneBrowseCommand
        {
            get
            {
                return _rebarInPlaneBrowseCommand;
            }
            set
            {
                _rebarInPlaneBrowseCommand = value;
                OnPropertyChanged("RebarInPlaneBrowseCommand");
            }
        }

        public RelayCommand RebarOutPlaneBrowseCommand
        {
            get
            {
                return _rebarOutPlaneBrowseCommand;
            }
            set
            {
                _rebarOutPlaneBrowseCommand = value;
                OnPropertyChanged("RebarOutPlaneBrowseCommand");
            }
        }



        public RelayCommand RebarInclinedBrowseCommand
        {
            get
            {
                return _rebarInclinedBrowseCommand;
            }
            set
            {
                _rebarInclinedBrowseCommand = value;
                OnPropertyChanged("RebarInclinedBrowseCommand");
            }
        }

        private string _selectedBottomColorName;

        public string SelectedBottomColorName
        {
            get
            {
                return _selectedBottomColorName;
            }
            set
            {
                _selectedBottomColorName = value;

                OnPropertyChanged("SelectedBottomColorName");
            }
        }

        private string _selectedTopColorName;

        public string SelectedTopColorName
        {
            get
            {
                return _selectedTopColorName;
            }
            set
            {
                _selectedTopColorName = value;

                OnPropertyChanged("SelectedTopColorName");
            }
        }

        private string _selectedRebarColorName;

        public string SelectedRebarColorName
        {
            get
            {
                return _selectedRebarColorName;
            }
            set
            {
                _selectedRebarColorName = value;

                OnPropertyChanged("SelectedRebarColorName");
            }
        }

        internal void SaveData()
        {
            bool isValidPath = false;

            isValidPath = IsValidPath();

            if (!isValidPath)
            {
                MessageBox.Show("Some File path are Invalid...", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                mainVM.RebarSettingDataInfo = new RebarSettingsData();

                BottomRebarData bottomRebarData = new BottomRebarData();
                bottomRebarData.LinePatternDataInfo = SelectedBottomLine;
                bottomRebarData.LineWeightDataInfo = SelectedBottomWeight;
                bottomRebarData.LineColorDataInfo = SelectedCustomBottomColor;

                TopRebarData topRebarDataInfo = new TopRebarData();
                topRebarDataInfo.LinePatternDataInfo = SelectedTopLine;
                topRebarDataInfo.LineWeightDataInfo = SelectedTopWeight;
                topRebarDataInfo.LineColorDataInfo = SelectedCustomTopColor;

                FamilyRebarInfo famRebarInfoData = new FamilyRebarInfo();
                famRebarInfoData.LinePatternDataInfo = SelectedRebarLine;
                famRebarInfoData.LineWeightDataInfo = SelectedRebarWeight;
                famRebarInfoData.LineColorDataInfo = SelectedCustomRebarColor;


                mainVM.RebarSettingDataInfo.BottomRebarData = bottomRebarData;
                mainVM.RebarSettingDataInfo.TopRebarData = topRebarDataInfo;
                mainVM.RebarSettingDataInfo.FamRebarData = famRebarInfoData;

                mainVM.RebarSettingDataInfo.RebarEndPath = RebarEndBrowsePath;
                mainVM.RebarSettingDataInfo.RebarInPlaneBendPath = RebarInPlaneBrowsePath;
                mainVM.RebarSettingDataInfo.RebarOutPlaneBendPath = RebarOutPlaneBrowsePath;
                mainVM.RebarSettingDataInfo.RebarInclinedPath = RebarInclinedBrowsePath;


                string uiSettingsData = CommonData.SerializeObject(mainVM.RebarSettingDataInfo);

                mainVM.dataReader.UISettingsInfo = uiSettingsData;

                //if (!string.IsNullOrEmpty(mainVM.dataReader.docPathName))
                //{
                //    string fileName = Path.GetFileNameWithoutExtension(mainVM.dataReader.docPathName);
                //    string fileDirectory = Path.GetDirectoryName(mainVM.dataReader.docPathName);

                //    string fullPath = Path.Combine(fileDirectory, fileName + ".settings");

                //    CommonData.SerializePathObject(mainVM.RebarSettingDataInfo, fullPath);
                //}

                mainVM.mainWindow.Close();

                mainVM.IsSettingsSaveSuccessfully = true;
            }
        }

        private bool IsValidPath()
        {
            bool isValidPath = true;

            if (string.IsNullOrEmpty(RebarEndBrowsePath) && string.IsNullOrEmpty(RebarInPlaneBrowsePath) && string.IsNullOrEmpty(RebarOutPlaneBrowsePath) && string.IsNullOrEmpty(RebarInclinedBrowsePath))
            {
                return isValidPath;
            }

            if (!string.IsNullOrEmpty(RebarEndBrowsePath) && !File.Exists(RebarEndBrowsePath))
            {
                isValidPath = false;

                RebarEndBorderColor = Brushes.Red;
            }
            else if (!string.IsNullOrEmpty(RebarInPlaneBrowsePath) && !File.Exists(RebarInPlaneBrowsePath))
            {
                isValidPath = false;

                RebarInclinedBorderColor = Brushes.Red;
            }
            else if (!string.IsNullOrEmpty(RebarOutPlaneBrowsePath) && !File.Exists(RebarOutPlaneBrowsePath))
            {
                isValidPath = false;

                RebarOutPlaneBorderColor = Brushes.Red;

            }
            else if (!string.IsNullOrEmpty(RebarInclinedBrowsePath) && !File.Exists(RebarInclinedBrowsePath))
            {
                isValidPath = false;

                RebarInclinedBorderColor = Brushes.Red;
            }

            return isValidPath;
        }

        internal void ResetData()
        {
            SelectedBottomLine = LstBottomProjectionLines.Where(x => x.Name == ConstantValues.DashPatternName).Select(x => x).FirstOrDefault();

            SelectedTopLine = LstProjectionLines.Where(x => x.Name == "None").Select(x => x).FirstOrDefault(); ;
            SelectedRebarLine = LstProjectionLines.Where(x => x.Name == "None").Select(x => x).FirstOrDefault(); ;

            SelectedBottomWeight = LstLineWeights.Where(x => x.WeightNumberInString == "None").Select(x => x).FirstOrDefault(); ;
            SelectedTopWeight = LstLineWeights.Where(x => x.WeightNumberInString == "None").Select(x => x).FirstOrDefault(); ;
            SelectedRebarWeight = LstLineWeights.Where(x => x.WeightNumberInString == "None").Select(x => x).FirstOrDefault(); ;

            Color emptyColor = new Color();

            SelectedBottomColorName = "  None";
            SelectedBottomColor = emptyColor;

            SelectedTopColorName = "  None";
            SelectedTopColor = emptyColor;

            SelectedRebarColorName = "  None";
            SelectedRebarColor = emptyColor;


            RebarEndBorderColor = Brushes.DarkGray;
            RebarInPlaneBorderColor = Brushes.DarkGray;
            RebarOutPlaneBorderColor = Brushes.DarkGray;
            RebarInclinedBorderColor = Brushes.DarkGray;

            RebarEndBrowsePath = string.Empty;
            RebarInPlaneBrowsePath = string.Empty;
            RebarOutPlaneBrowsePath = string.Empty;
            RebarInclinedBrowsePath = string.Empty;
        }

        public ObservableCollection<LinePatternInfo> LstBottomProjectionLines
        {
            get
            {
                return _lstBottomProjectLines;
            }
            set
            {
                _lstBottomProjectLines = value;
                OnPropertyChanged("LstBottomProjectionLines");
            }
        }

        public ObservableCollection<LinePatternInfo> LstProjectionLines
        {
            get
            {
                return _lstProjectLines;
            }
            set
            {
                _lstProjectLines = value;
                OnPropertyChanged("LstProjectionLines");
            }
        }

        public ObservableCollection<LineWeightInfo> LstLineWeights
        {
            get
            {
                return _lstWeightLines;
            }
            set
            {
                _lstWeightLines = value;
                OnPropertyChanged("LstLineWeights");
            }
        }

        private LinePatternInfo _selectedBottomLine;

        public LinePatternInfo SelectedBottomLine
        {
            get
            {
                return _selectedBottomLine;
            }
            set
            {
                _selectedBottomLine = value;
                OnPropertyChanged("SelectedBottomLine");
            }
        }

        private LinePatternInfo _selectedTopLine;

        public LinePatternInfo SelectedTopLine
        {
            get
            {
                return _selectedTopLine;
            }
            set
            {
                _selectedTopLine = value;
                OnPropertyChanged("SelectedTopLine");
            }
        }

        private LinePatternInfo _selectedRebarLine;

        public LinePatternInfo SelectedRebarLine
        {
            get
            {
                return _selectedRebarLine;
            }
            set
            {
                _selectedRebarLine = value;
                OnPropertyChanged("SelectedRebarLine");
            }
        }


        private LineWeightInfo _selectedBottomWeight;

        public LineWeightInfo SelectedBottomWeight
        {
            get
            {
                return _selectedBottomWeight;
            }
            set
            {
                _selectedBottomWeight = value;
                OnPropertyChanged("SelectedBottomWeight");
            }
        }

        private LineWeightInfo _selectedTopWeight;

        public LineWeightInfo SelectedTopWeight
        {
            get
            {
                return _selectedTopWeight;
            }
            set
            {
                _selectedTopWeight = value;
                OnPropertyChanged("SelectedTopWeight");
            }
        }

        private LineWeightInfo _selectedRebarWeight;

        public LineWeightInfo SelectedRebarWeight
        {
            get
            {
                return _selectedRebarWeight;
            }
            set
            {
                _selectedRebarWeight = value;
                OnPropertyChanged("SelectedRebarWeight");
            }
        }

        private Color _selectedBottomColor;

        public Color SelectedBottomColor
        {
            get
            {
                return _selectedBottomColor;
            }
            set
            {
                _selectedBottomColor = value;

                if (_selectedBottomColor != null)
                {
                    LineColorInfo lineColorInfo = new LineColorInfo();
                    lineColorInfo.RValue = _selectedBottomColor.R;
                    lineColorInfo.GValue = _selectedBottomColor.G;
                    lineColorInfo.BValue = _selectedBottomColor.B;
                    lineColorInfo.ColorName = _selectedBottomColor.ToString();
                    SelectedCustomBottomColor = lineColorInfo;
                }

                OnPropertyChanged("SelectedBottomColor");
            }
        }

        private Color _selectedTopColor;

        public Color SelectedTopColor
        {
            get
            {
                return _selectedTopColor;
            }
            set
            {
                _selectedTopColor = value;

                if (_selectedTopColor != null)
                {
                    LineColorInfo lineColorInfo = new LineColorInfo();
                    lineColorInfo.RValue = _selectedTopColor.R;
                    lineColorInfo.GValue = _selectedTopColor.G;
                    lineColorInfo.BValue = _selectedTopColor.B;
                    lineColorInfo.ColorName = _selectedTopColor.ToString();
                    SelectedCustomTopColor = lineColorInfo;
                }

                OnPropertyChanged("SelectedTopColor");
            }
        }

        private Color _selectedRebarColor;

        private string _rebarEndBrowsePath;
        private string _rebarInPlaneBrowsePath;
        private string _rebarOutPlanePath;
        private string _rebarInclinedPlanePath;

        private Brush _rebarEndBorderColor;
        private Brush _rebarInPlaneBorderColor;
        private Brush _rebarOutPlaneBorderColor;
        private Brush _rebarInclinedBorderColor;

        public Color SelectedRebarColor
        {
            get
            {
                return _selectedRebarColor;
            }
            set
            {
                _selectedRebarColor = value;

                if (_selectedRebarColor != null)
                {
                    LineColorInfo lineColorInfo = new LineColorInfo();
                    lineColorInfo.RValue = _selectedRebarColor.R;
                    lineColorInfo.GValue = _selectedRebarColor.G;
                    lineColorInfo.BValue = _selectedRebarColor.B;
                    lineColorInfo.ColorName = _selectedRebarColor.ToString();
                    SelectedCustomRebarColor = lineColorInfo;
                }
                OnPropertyChanged("SelectedRebarColor");
            }
        }


        public bool _bottomRebarExpanded { get; set; }

        public bool IsBottomRebarBtnExpanded
        {
            get
            {
                return _bottomRebarExpanded;
            }
            set
            {
                _bottomRebarExpanded = value;

                if (!_bottomRebarExpanded)
                {
                    BottomRebarVisiblity = Visibility.Visible;

                    IsBottomRebarGridCollapsed = false;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height + 60;

                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                else
                {
                    BottomRebarVisiblity = Visibility.Collapsed;

                    IsBottomRebarGridCollapsed = true;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height - 60;
                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                OnPropertyChanged("IsBottomRebarBtnExpanded");
            }
        }

        public Visibility _bottomRebarVisiblity { get; set; }

        public Visibility BottomRebarVisiblity
        {
            get
            {
                return _bottomRebarVisiblity;
            }
            set
            {
                _bottomRebarVisiblity = value;
                OnPropertyChanged("BottomRebarVisiblity");
            }
        }

        public bool _isBottomRebarGridCollapsed { get; set; }
        public bool IsBottomRebarGridCollapsed
        {
            get
            {
                return _isBottomRebarGridCollapsed;
            }
            set
            {
                _isBottomRebarGridCollapsed = value;

                OnPropertyChanged("IsBottomRebarGridCollapsed");
            }
        }

        public bool _isTopRebarGridCollapsed { get; set; }
        public bool IsTopRebarGridCollapsed
        {
            get
            {
                return _isTopRebarGridCollapsed;
            }
            set
            {
                _isTopRebarGridCollapsed = value;

                OnPropertyChanged("IsTopRebarGridCollapsed");
            }
        }

        public bool _isRebarSymCollapsed { get; set; }
        public bool IsRebarSymCollapse
        {
            get
            {
                return _isRebarSymCollapsed;
            }
            set
            {
                _isRebarSymCollapsed = value;

                OnPropertyChanged("IsRebarSymCollapse");
            }
        }


        public bool _isFamBrowseCollapse { get; set; }
        public bool IsFamBrowseCollapsed
        {
            get
            {
                return _isFamBrowseCollapse;
            }
            set
            {
                _isFamBrowseCollapse = value;

                OnPropertyChanged("IsFamBrowseCollapsed");
            }
        }
        public bool _topRebarExpanded { get; set; }
        public bool IsTopRebarBtnExpanded
        {
            get
            {
                return _topRebarExpanded;
            }
            set
            {
                _topRebarExpanded = value;

                if (!_topRebarExpanded)
                {
                    TopRebarVisiblity = Visibility.Visible;

                    IsTopRebarGridCollapsed = false;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height + 60;
                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                else
                {
                    TopRebarVisiblity = Visibility.Collapsed;

                    IsTopRebarGridCollapsed = true;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height - 60;
                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                OnPropertyChanged("IsTopRebarBtnExpanded");
            }
        }

        public Visibility _topRebarVisiblity { get; set; }
        public Visibility TopRebarVisiblity
        {
            get
            {
                return _topRebarVisiblity;
            }
            set
            {
                _topRebarVisiblity = value;
                OnPropertyChanged("TopRebarVisiblity");
            }
        }


        public bool _rebarSymbolExpand { get; set; }
        public bool IsRebarSymbolExpanded
        {
            get
            {
                return _rebarSymbolExpand;
            }
            set
            {
                _rebarSymbolExpand = value;

                if (!_rebarSymbolExpand)
                {
                    RebarSymbolVisiblity = Visibility.Visible;

                    IsRebarSymCollapse = false;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height + 60;
                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                }
                else
                {
                    RebarSymbolVisiblity = Visibility.Collapsed;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height - 60;

                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    IsRebarSymCollapse = true;
                }

                OnPropertyChanged("IsRebarSymbolExpanded");
            }
        }


        public Visibility _rebarSymbolVisiblity { get; set; }
        public Visibility RebarSymbolVisiblity
        {
            get
            {
                return _rebarSymbolVisiblity;
            }
            set
            {
                _rebarSymbolVisiblity = value;
                OnPropertyChanged("RebarSymbolVisiblity");
            }
        }

        public bool _famBrowserExpand { get; set; }
        public bool FamilyBrowseSymbolExpand
        {
            get
            {
                return _famBrowserExpand;
            }
            set
            {
                _famBrowserExpand = value;

                if (!_famBrowserExpand)
                {
                    FamBrowserSymVisiblity = Visibility.Visible;

                    IsFamBrowseCollapsed = false;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height + 200;
                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                }
                else
                {
                    FamBrowserSymVisiblity = Visibility.Collapsed;

                    IsFamBrowseCollapsed = true;

                    mainVM.mainWindow.Height = mainVM.mainWindow.Height - 200;
                    mainVM.mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                OnPropertyChanged("FamilyBrowseSymbolExpand");
            }
        }

        public Visibility _famBrowseSymVisiblity { get; set; }
        public Visibility FamBrowserSymVisiblity
        {
            get
            {
                return _famBrowseSymVisiblity;
            }
            set
            {
                _famBrowseSymVisiblity = value;
                OnPropertyChanged("FamBrowserSymVisiblity");
            }
        }
        public SolidColorBrush SourceColor { get; set; } = (SolidColorBrush)new BrushConverter().ConvertFrom("#009DF0 ");
        public Uri SettingsUriPath { get; set; }
    }
}
