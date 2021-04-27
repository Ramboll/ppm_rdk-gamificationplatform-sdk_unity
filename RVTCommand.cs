using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PatternModifierUI.ViewModels;
using PatternModifierUI.Views;
using RebarInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Utilities;

namespace RebarAnnotationTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class TopRebarCommand : IExternalCommand
    {
        internal UIApplication uiApplication;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData != null ? commandData.Application : uiApplication;

                if (uiApp.ActiveUIDocument.Document.ActiveView is View3D)
                {
                    MessageBox.Show("3D View Not Supported...", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return Result.Succeeded;
                }

                ReadAndWriteData dataReader = new ReadAndWriteData(uiApp);

                List<ElementDataInfo> lstSelectedElementDataInfo = dataReader.ReadAndFillReinforcementData(false);

                if (lstSelectedElementDataInfo.Any())
                {
                    RebarSettingsData rebarSettingsData = default(RebarSettingsData);

                    string storedData = CommonData.ExtractSettingsFromDocument(ConstantValues.RebarUIFieldName, ConstantValues.RebarUISchemaName, ConstantValues.RebarUISchemaGuid, dataReader.doc);

                    if (!string.IsNullOrEmpty(storedData))
                    {
                        object storedRebarSettings = CommonData.DeserializeObject(typeof(RebarSettingsData), storedData);

                        rebarSettingsData = storedRebarSettings as RebarSettingsData;

                    }

                    dataReader.RebarSettingsDataInfo = rebarSettingsData;


                    bool isPlacedInDoc = dataReader.PlaceAnnotationWithRespectiveFamily();

                    if (isPlacedInDoc)
                    {
                        dataReader.ChangeLinePatternForElements();

                        MessageBox.Show("Annotation Created Successfully", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Annotation Creation Failed...", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Kindly Select the Rebar-Element", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Warning);
                }


            }
            catch (Exception ex)
            {
                return Result.Failed;
            }
            return Result.Succeeded;
        }

    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BottomRebarCommand : IExternalCommand
    {
        internal UIApplication uiApplication;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData != null ? commandData.Application : uiApplication;

                if (uiApp.ActiveUIDocument.Document.ActiveView is View3D)
                {
                    MessageBox.Show("3D View Not Supported...", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Error);

                    return Result.Succeeded;
                }

                ReadAndWriteData dataReader = new ReadAndWriteData(uiApp);

                List<ElementDataInfo> lstSelectedElementDataInfo = dataReader.ReadAndFillReinforcementData(true);

                if (lstSelectedElementDataInfo.Any())
                {
                    RebarSettingsData rebarSettingsData = default(RebarSettingsData);

                    string storedData = CommonData.ExtractSettingsFromDocument(ConstantValues.RebarUIFieldName, ConstantValues.RebarUISchemaName, ConstantValues.RebarUISchemaGuid, dataReader.doc);

                    if (!string.IsNullOrEmpty(storedData))
                    {
                        object storedRebarSettings = CommonData.DeserializeObject(typeof(RebarSettingsData), storedData);

                        rebarSettingsData = storedRebarSettings as RebarSettingsData;

                    }

                    dataReader.RebarSettingsDataInfo = rebarSettingsData;

                    bool isPlacedInDoc = dataReader.PlaceAnnotationWithRespectiveFamily();

                    if (isPlacedInDoc)
                    {
                        dataReader.ChangeLinePatternForElements();

                        MessageBox.Show("Annotation Created Successfully", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Annotation Creation Failed...", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Kindly Select the Rebar-Element", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SettingsCommand : IExternalCommand
    {
        internal static MainViewModel MainVM;
        private WindowHandle _hWndRevit = null;
        internal UIApplication uiApplication;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //MessageBox.Show("Need To Implement...", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Information);
                //return Result.Succeeded;

                CommonData.IsCommandTriggered = true;

                UIApplication uiApp = commandData != null ? commandData.Application : uiApplication;


                ReadAndWriteData dataReader = new ReadAndWriteData(uiApp);

                dataReader.CommandButtonType = CommandClickType.LinePatternStyle;

                dataReader.ReadLinePatternElements();
                dataReader.ReadLineWeight();

                Document doc = uiApp.ActiveUIDocument.Document;

                if (dataReader.LstLinePatternInfo.Any() && dataReader.LstLineWeightInfo.Any())
                {
                    string storedData = CommonData.ExtractSettingsFromDocument(ConstantValues.RebarUIFieldName, ConstantValues.RebarUISchemaName, ConstantValues.RebarUISchemaGuid, doc);

                    if (!string.IsNullOrEmpty(storedData))
                    {
                        dataReader.UISettingsInfo = storedData;
                    }

                    MainVM = new MainViewModel(dataReader);

                    MainVM.mainWindow.ShowDialog();

                    if (MainVM.IsSettingsSaveSuccessfully)
                    {
                        if (!string.IsNullOrEmpty(MainVM.dataReader.UISettingsInfo))
                        {
                            Transaction trans = new Transaction(doc, "storedSettingInDoc");

                            trans.Start();

                            CommonData.WriteSettingInStorageDocument(MainVM.dataReader.UISettingsInfo, ConstantValues.RebarUIFieldName, ConstantValues.RebarUISchemaName, ConstantValues.RebarUISchemaGuid, doc);

                            trans.Commit();
                        }

                        MessageBox.Show("Settings Saved Successfully", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("There is no Line Pattern in the Document to Modify...", "PatternModifier");

                }
            }
            catch (Exception ex)
            {
                return Result.Failed;
            }

            CommonData.IsClosed = true;

            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FlipCommand : IExternalCommand
    {
        internal UIApplication uiApplication;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIApplication uiApp = commandData != null ? commandData.Application : uiApplication;


            if (uiApp.ActiveUIDocument.Document.ActiveView is View3D)
            {
                MessageBox.Show("3D View Not Supported...", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Error);

                return Result.Succeeded;
            }

            ReadAndWriteData dataReader = new ReadAndWriteData(uiApp);




            bool isFlipped = dataReader.FlipElement();

            if (!dataReader.LstSelectedElementDataInfo.Any())
            {
                MessageBox.Show("Kindly Select the Rebar-Element", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Warning);

                return Result.Succeeded;
            }

            if (isFlipped && dataReader.LstSelectedElementDataInfo != null && dataReader.LstSelectedElementDataInfo.Any())
            {
                RebarSettingsData rebarSettingsData = default(RebarSettingsData);

                string storedData = CommonData.ExtractSettingsFromDocument(ConstantValues.RebarUIFieldName, ConstantValues.RebarUISchemaName, ConstantValues.RebarUISchemaGuid, dataReader.doc);

                if (!string.IsNullOrEmpty(storedData))
                {
                    object storedRebarSettings = CommonData.DeserializeObject(typeof(RebarSettingsData), storedData);

                    rebarSettingsData = storedRebarSettings as RebarSettingsData;

                }

                dataReader.RebarSettingsDataInfo = rebarSettingsData;


                bool isPlacedInDoc = dataReader.PlaceAnnotationWithRespectiveFamily();

                if (isPlacedInDoc)
                {
                    dataReader.ChangeLinePatternForElements(true);

                    MessageBox.Show("Flipped Annotation Placed Successfully", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Flipped Annotation Placement Failed", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (!isFlipped)
            {
                MessageBox.Show("Annotation Family Not Placed To Flip", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //dataReader.CommandButtonType = CommandClickType.Flip;

            //MainViewModel MainVM = new MainViewModel(dataReader);

            //MainVM.mainWindow.ShowDialog();

            return Result.Succeeded;
        }

    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UpdateCommand : IExternalCommand
    {
        internal UIApplication uiApplication;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //MessageBox.Show("Need To Implement...", "Rebar Tool", MessageBoxButton.OK, MessageBoxImage.Information);


            //UpdateWindowViewModel updateWinViewModel = new UpdateWindowViewModel();
            //updateWinViewModel.UpdateWindow.ShowDialog();

            UIApplication uiApp = commandData != null ? commandData.Application : uiApplication;


            if (uiApp.ActiveUIDocument.Document.ActiveView is View3D)
            {
                MessageBox.Show("3D View Not Supported...", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Error);

                return Result.Succeeded;
            }

            ReadAndWriteData dataReader = new ReadAndWriteData(uiApp);

            RebarSettingsData rebarSettingsData = default(RebarSettingsData);

            string storedData = CommonData.ExtractSettingsFromDocument(ConstantValues.RebarUIFieldName, ConstantValues.RebarUISchemaName, ConstantValues.RebarUISchemaGuid, dataReader.doc);

            if (!string.IsNullOrEmpty(storedData))
            {
                object storedRebarSettings = CommonData.DeserializeObject(typeof(RebarSettingsData), storedData);

                rebarSettingsData = storedRebarSettings as RebarSettingsData;
            }

            dataReader.RebarSettingsDataInfo = rebarSettingsData;

            if (!uiApp.ActiveUIDocument.Selection.GetElementIds().ToList().Any())
            {
                UpdateWindowViewModel updateMainVM = new UpdateWindowViewModel(dataReader);

                updateMainVM.UpdateWindow.ShowDialog();
            }
            else
            {
                bool isUpdated = dataReader.UpdateElement(false, true);

                if (isUpdated && dataReader.LstSelectedElementDataInfo != null && dataReader.LstSelectedElementDataInfo.Any())
                {
                    bool isPlacedInDoc = dataReader.PlaceAnnotationWithRespectiveFamily();

                    dataReader.ChangeLinePatternForElements();

                    MessageBox.Show("Annotation Family Updated Successfully...", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Information);


                }
                else if (!isUpdated)
                {
                    MessageBox.Show("Annotation Family Not Placed To Update", ConstantValues.TabName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }




            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class InfoCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                System.Diagnostics.Process.Start(ConstantValues.HelpLink);

            }
            catch (Exception ex)
            {
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }

}
