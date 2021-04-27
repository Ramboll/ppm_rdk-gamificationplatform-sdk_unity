using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Utilities;

namespace RebarInterop
{
    public class ReadAndWriteData
    {
        public List<LinePatternInfo> LstLinePatternInfo;
        public List<LineWeightInfo> LstLineWeightInfo;
        public CommandClickType CommandButtonType { get; set; }
        public UIApplication uiApp;
        public UIDocument uiDoc;
        public Document doc;

        public Autodesk.Revit.DB.View CurrentActiveView { get; set; }

        public string docPathName;
        public RebarSettingsData RebarSettingsDataInfo { get; set; }
        public string UISettingsInfo { get; set; }
        private List<string> lstAnnotateName = new List<string> { "Line", "Cross", "Circle", "Centre" };

        public List<ElementDataInfo> LstSelectedElementDataInfo { get; set; }
        private Dictionary<ElementId, List<Line>> LstReverseLineInfo { get; set; }
        public List<CornerPointInfo> LstCornerPointInfo { get; set; }
        public List<FamilyInstance> LstTotalFamInstacne { get; set; }

        public List<StorageHelper> LstExistDataStorage = new List<StorageHelper>();

        // private Face PlaneFace { get; set; }

        public ReadAndWriteData(UIApplication _uiApp)
        {
            this.uiApp = _uiApp;
            this.uiDoc = _uiApp.ActiveUIDocument;
            this.doc = _uiApp.ActiveUIDocument.Document;
            this.CurrentActiveView = doc.ActiveView;
            this.docPathName = doc.PathName;
        }

        public List<LinePatternInfo> ReadLinePatternElements()
        {
            LstLinePatternInfo = new List<LinePatternInfo>();

            var flPatternLineCollector = new FilteredElementCollector(doc).OfClass(typeof(LinePatternElement)).ToElements();

            LinePatternInfo emptyLinePattern = CreateLinePattern("None", 0);

            LstLinePatternInfo.Add(emptyLinePattern);

            int solidPatternId = LinePatternElement.GetSolidPatternId().IntegerValue;

            LinePatternInfo solidLinePattern = CreateLinePattern(ConstantValues.SolidPatternName, solidPatternId);

            LstLinePatternInfo.Add(solidLinePattern);


            List<LinePatternInfo> lstlneInfo = new List<LinePatternInfo>();

            foreach (Element lineElement in flPatternLineCollector)
            {
                LinePatternInfo lnePatternInfo = new LinePatternInfo();

                lnePatternInfo.Id = lineElement.Id.IntegerValue;

                lnePatternInfo.Name = lineElement.Name;

                lstlneInfo.Add(lnePatternInfo);
            }

            lstlneInfo = lstlneInfo.OrderBy(x => x.Name).ToList();

            LstLinePatternInfo.AddRange(lstlneInfo);


            return LstLinePatternInfo;
        }

        private static LinePatternInfo CreateLinePattern(string name, int id)
        {
            LinePatternInfo emptyLinePattern = new LinePatternInfo();

            emptyLinePattern.Id = id;

            emptyLinePattern.Name = name;
            return emptyLinePattern;
        }

        public List<LineWeightInfo> ReadLineWeight()
        {
            LstLineWeightInfo = new List<LineWeightInfo>();


            LineWeightInfo emptyLineWeight = new LineWeightInfo();

            emptyLineWeight.WeightNumberInString = "None";

            LstLineWeightInfo.Add(emptyLineWeight);

            for (int i = 0; i < 17; i++)
            {
                if (i == 0)
                    continue;

                LineWeightInfo lineweghtInfo = new LineWeightInfo();

                lineweghtInfo.WeightNumber = i;

                lineweghtInfo.WeightNumberInString = i.ToString();

                LstLineWeightInfo.Add(lineweghtInfo);
            }

            return LstLineWeightInfo;
        }

        public List<ElementDataInfo> ReadSelectedLineElementDataInfo(bool isAllView, bool isActiveView)
        {
            LstSelectedElementDataInfo = new List<ElementDataInfo>();

            try
            {
                List<ElementId> selectedElements = uiDoc.Selection.GetElementIds().ToList();

                if (CurrentActiveView != null && isActiveView && !selectedElements.Any())
                {
                    ElementCategoryFilter filterRebar = new ElementCategoryFilter(BuiltInCategory.OST_Rebar);

                    FilteredElementCollector elemCollector = new FilteredElementCollector(doc, CurrentActiveView.Id);

                    selectedElements = elemCollector.WherePasses(filterRebar).WhereElementIsNotElementType().ToElementIds().ToList();

                    selectedElements = selectedElements.Where(x => LstExistDataStorage.Any(y => y.ElemId == x.IntegerValue)).Select(x => x).ToList();

                    if (selectedElements.Any())
                        LstSelectedElementDataInfo.AddRange(ReadElementFromView(selectedElements, CurrentActiveView));

                    DeleteUnwantedAnnotateFam(new List<StorageHelper>(LstExistDataStorage));

                    return LstSelectedElementDataInfo;
                }
                else if (isAllView && !selectedElements.Any())
                {
                    ElementCategoryFilter filterRebar = new ElementCategoryFilter(BuiltInCategory.OST_Rebar);

                    FilteredElementCollector collector = new FilteredElementCollector(doc);

                    FilteredElementCollector viewCollector = new FilteredElementCollector(doc);
                    viewCollector.OfClass(typeof(Autodesk.Revit.DB.View));

                    foreach (Autodesk.Revit.DB.View _currentView in viewCollector)
                    {
                        if (!FilteredElementCollector.IsViewValidForElementIteration(doc, _currentView.Id))
                        {
                            continue;
                        }

                        if (_currentView is View3D)
                            continue;

                        string currentViewId = _currentView.Id.ToString();


                        FilteredElementCollector elemCollector = new FilteredElementCollector(doc, _currentView.Id);

                        selectedElements = elemCollector.WherePasses(filterRebar).WhereElementIsNotElementType().ToElementIds().ToList();

                        selectedElements = selectedElements.Where(x => LstExistDataStorage.Where(y => y.ElemId == x.IntegerValue && y.LstViewBasedDataInfo.Any(c => c.ViewId == _currentView.Id.IntegerValue)).Any()).Select(x => x).ToList();


                        selectedElements = selectedElements.Where(x => LstExistDataStorage.Any(y => y.ElemId == x.IntegerValue)).Select(x => x).ToList();

                        if (selectedElements.Any())
                            LstSelectedElementDataInfo.AddRange(ReadElementFromView(selectedElements, _currentView));
                    }


                    DeleteUnwantedAnnotateFam(new List<StorageHelper>(LstExistDataStorage));

                    return LstSelectedElementDataInfo;

                }

                //if (!selectedElements.Any())
                //{
                //    System.Windows.Forms.MessageBox.Show("Kindly Select the Rebar-Element To Change the Pattern", "PatternModifier", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return LstSelectedElementDataInfo;
                //}

                LstSelectedElementDataInfo.AddRange(ReadElementFromView(selectedElements, CurrentActiveView));
            }
            catch (Exception ex)
            {
            }

            return LstSelectedElementDataInfo;
        }

        private List<ElementDataInfo> ReadElementFromView(List<ElementId> lstElemIds, Autodesk.Revit.DB.View currentView)
        {
            List<ElementDataInfo> lstElemData = new List<ElementDataInfo>();

            foreach (ElementId elemId in lstElemIds)
            {
                if (LstSelectedElementDataInfo.Any(x => x.ElemID == elemId.IntegerValue && x.CurrentViewId == currentView.Id))
                    continue;

                Element elem = doc.GetElement(elemId);

                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Rebar)
                {
                    ElementDataInfo elemDataInfo = new ElementDataInfo();
                    elemDataInfo.CategoryName = elem.Category.Name;
                    elemDataInfo.ElemName = elem.Name;
                    elemDataInfo.ElemID = elem.Id.IntegerValue;
                    elemDataInfo.CatId = elem.Category.Id.IntegerValue;
                    elemDataInfo.CurrentViewId = currentView.Id;
                    elemDataInfo.CurrentView = currentView;

                    lstElemData.Add(elemDataInfo);
                }
            }

            return lstElemData;
        }

        public bool ModifyLinePattern(LinePatternInfo SelectedLinePatternInfo, Autodesk.Revit.DB.View CurrentView)
        {
            bool IsModified = false;

            List<ElementDataInfo> lstElementDataInfo = ReadSelectedLineElementDataInfo(false, true);

            if (!lstElementDataInfo.Any())
            {
                return IsModified;
            }

            int SelectedLineID = SelectedLinePatternInfo.Id;

            ElementId LineElemId = new ElementId(SelectedLineID);

            List<ElementId> selectedElements = uiDoc.Selection.GetElementIds().ToList();

            Transaction trans = new Transaction(doc, "PatternModifier");

            trans.Start();

            foreach (ElementDataInfo elemData in lstElementDataInfo)
            {
                ElementId elemId = new ElementId(elemData.ElemID);

                OverrideGraphicSettings overrideGS = new OverrideGraphicSettings();

                overrideGS.SetProjectionLinePatternId(LineElemId);

                CurrentView.SetElementOverrides(elemId, overrideGS);
            }

            trans.Commit();

            IsModified = true;

            return IsModified;
        }

        private bool IsFamilyLoaded(Document currentDocument, string filepath, out Family fam)
        {
            fam = default(Family);

            Transaction trans = new Transaction(currentDocument, "LoadFamily");

            bool IsLoaded = false;
            try
            {
                trans.Start();

                IsLoaded = doc.LoadFamily(filepath, new FamilyOptions(), out fam);

                trans.Commit();
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                {
                    trans.Commit();
                }
            }

            return IsLoaded;
        }

        public bool FlipElement()
        {
            LstExistDataStorage = ReadSettings();



            bool isFlipped = false;



            List<ElementDataInfo> lstSelectedElemData = ReadSelectedLineElementDataInfo(false, true);

            List<ElementId> lstElementToDelete = new List<ElementId>();

            if (lstSelectedElemData.Any())
            {
                foreach (ElementDataInfo elemDataInfo in lstSelectedElemData)
                {
                    Autodesk.Revit.DB.View CurrentView = elemDataInfo.CurrentView == null ? doc.ActiveView : elemDataInfo.CurrentView;

                    Plane plane = ReadPlaneFromActiveView(CurrentView);

                    elemDataInfo.PlaneNormalDirection = plane.Normal;

                    StorageHelper existsStorageHelper = LstExistDataStorage.Where(x => x.ElemId == elemDataInfo.ElemID).Select(x => x).FirstOrDefault();

                    if (existsStorageHelper == null)
                        continue;

                    ElementId elemId = new ElementId(elemDataInfo.ElemID);

                    Element elem = doc.GetElement(elemId);

                    if (elem != null)
                    {
                        bool isAnnotatePlaced = false;
                        ViewBasedData viewBasedData = GetViewBasedDataa(existsStorageHelper, elem, CurrentView);

                        if (existsStorageHelper != null && viewBasedData != null)
                        {
                            isAnnotatePlaced = CheckAnnotateFamPlaced(viewBasedData, isAnnotatePlaced);
                        }


                        if (isAnnotatePlaced)
                        {
                            if (viewBasedData.IsFlipped)
                            {
                                elemDataInfo.IsFlipped = false;
                            }
                            else
                            {
                                elemDataInfo.IsFlipped = true;
                            }

                            bool isAssined = AssignRebarElemDataInfo(elemDataInfo);

                            if (isAssined && viewBasedData != null)
                            {
                                isFlipped = true;

                                DeleteExistsAnnotation(viewBasedData);

                                existsStorageHelper.LstViewBasedDataInfo.RemoveAll(x => x == viewBasedData);
                            }
                        }
                    }
                    else
                    {
                        ViewBasedData viewBasedData = GetViewBasedDataa(existsStorageHelper, elem, CurrentView);

                        if (viewBasedData != null)
                        {
                            DeleteExistsAnnotation(viewBasedData);

                            existsStorageHelper.LstViewBasedDataInfo.RemoveAll(x => x == viewBasedData);
                        }
                    }
                }
            }




            return isFlipped;
        }

        public bool UpdateElement(bool isAllView, bool isActiveView)
        {
            LstExistDataStorage = ReadSettings();



            bool isUpdated = false;

            ReadSelectedLineElementDataInfo(isAllView, isActiveView);

            List<ElementId> lstElementToDelete = new List<ElementId>();

            if (LstSelectedElementDataInfo.Any())
            {
                // DeleteUnwantedAnnotateFam(new List<StorageHelper>(LstExistDataStorage));

                foreach (ElementDataInfo elemDataInfo in LstSelectedElementDataInfo)
                {
                    Autodesk.Revit.DB.View CurrentView = elemDataInfo.CurrentView == null ? doc.ActiveView : elemDataInfo.CurrentView;

                    Plane plane = ReadPlaneFromActiveView(CurrentView);

                    elemDataInfo.PlaneNormalDirection = plane.Normal;

                    StorageHelper existsStorageHelper = LstExistDataStorage.Where(x => x.ElemId == elemDataInfo.ElemID).Select(x => x).FirstOrDefault();

                    if (existsStorageHelper == null)
                        continue;

                    ElementId elemId = new ElementId(elemDataInfo.ElemID);

                    Element elem = doc.GetElement(elemId);

                    if (elem != null)
                    {
                        bool isAnnotatePlaced = false;
                        ViewBasedData viewBasedData = GetViewBasedDataa(existsStorageHelper, elem, CurrentView);

                        if (existsStorageHelper != null && viewBasedData != null)
                        {
                            isAnnotatePlaced = CheckAnnotateFamPlaced(viewBasedData, isAnnotatePlaced);
                        }


                        if (isAnnotatePlaced && !string.IsNullOrEmpty(existsStorageHelper.HasCodeData))
                        {
                            elemDataInfo.IsFlipped = viewBasedData.IsFlipped;

                            bool isAssined = false;

                            //if (RebarSettingsDataInfo != null && (!string.IsNullOrEmpty(RebarSettingsDataInfo.RebarEndPath) || !string.IsNullOrEmpty(RebarSettingsDataInfo.RebarInPlaneBendPath) || !string.IsNullOrEmpty(RebarSettingsDataInfo.RebarOutPlaneBendPath) || !string.IsNullOrEmpty(RebarSettingsDataInfo.RebarInclinedPath)))
                            //{
                            isAssined = AssignRebarElemDataInfo(elemDataInfo);
                            //}
                            //else
                            //{
                            //    isAssined = AssignRebarElemDataInfo(elemDataInfo, existsStorageHelper.HasCodeData);
                            //}



                            if (isAssined)
                            {
                                DeleteExistsAnnotation(viewBasedData);

                                existsStorageHelper.LstViewBasedDataInfo.RemoveAll(x => x == viewBasedData);
                            }
                            //else
                            //{
                            //    if (lstTotalFamInstacne == null)
                            //        lstTotalFamInstacne = new List<FamilyInstance>();

                            //    foreach (int famElemId in viewBasedData.lstFamInsId)
                            //    {
                            //        Element famAnnotateElem = GetElement(famElemId);

                            //        if (famAnnotateElem != null && famAnnotateElem is FamilyInstance)
                            //        {
                            //            lstTotalFamInstacne.Add(famAnnotateElem as FamilyInstance);
                            //        }

                            //    }
                            //}


                            // if (isAssined)
                            isUpdated = true;
                        }

                    }
                    else
                    {
                        ViewBasedData viewBasedData = GetViewBasedDataa(existsStorageHelper, elem, CurrentView);

                        if (viewBasedData != null)
                        {
                            DeleteExistsAnnotation(viewBasedData);

                            existsStorageHelper.LstViewBasedDataInfo.RemoveAll(x => x == viewBasedData);
                        }
                    }
                }
            }

            return isUpdated;
        }

        private void DeleteUnwantedAnnotateFam(List<StorageHelper> lstStorageHelper)
        {
            ICollection<ElementId> lstToDeleteItem = new List<ElementId>();

            Transaction trans = new Transaction(doc, "DeleteUnwantedAnnotate");

            try
            {


                foreach (StorageHelper helperStoreData in lstStorageHelper)
                {
                    Element elem = default(Element);

                    elem = GetElement(helperStoreData.ElemId);

                    if (elem == null)
                    {
                        foreach (var viewBasedData in helperStoreData.LstViewBasedDataInfo)
                        {
                            if (viewBasedData != null)
                            {
                                foreach (var famiD in viewBasedData.lstFamInsId)
                                {
                                    Element docElem = GetElement(famiD);

                                    if (docElem != null)
                                    {
                                        lstToDeleteItem.Add(docElem.Id);

                                        // doc.Delete(docElem.Id);
                                    }
                                }
                            }
                        }

                        LstExistDataStorage.RemoveAll(x => x.ElemId == helperStoreData.ElemId);
                    }
                }

                trans.Start();
                doc.Delete(lstToDeleteItem);
                trans.Commit();
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();
            }
        }

        private Element GetElement(int elemID)
        {
            Element elem = default(Element);
            try
            {
                int elemIntId = elemID;

                ElementId elemId = new ElementId(elemIntId);

                elem = doc.GetElement(elemId);
            }
            catch (Exception ex)
            {
            }
            return elem;
        }

        private void DeleteExistsAnnotation(ViewBasedData viewBasedData)
        {
            if (viewBasedData.lstFamInsId != null && viewBasedData.lstFamInsId.Any())
            {
                Transaction trans = new Transaction(doc, "DeleteElem");

                ICollection<ElementId> lstToDeleteItem = new List<ElementId>();

                try
                {

                    foreach (int famInsId in viewBasedData.lstFamInsId)
                    {
                        ElementId famElemId = new ElementId(famInsId);

                        Element famInsElem = doc.GetElement(famElemId);

                        if (famInsElem != null)
                        {
                            lstToDeleteItem.Add(famElemId);
                            //doc.Delete(famElemId);
                        }
                    }

                    trans.Start();
                    doc.Delete(lstToDeleteItem);
                    trans.Commit();
                }
                catch (Exception ex)
                {
                    if (trans.HasStarted())
                    {
                        trans.RollBack();
                    }
                }

            }
        }

        private ViewBasedData GetViewBasedDataa(StorageHelper existsStorageHelper, Element elem, Autodesk.Revit.DB.View CurrentView)
        {
            ViewBasedData viewBasedData = default(ViewBasedData);

            if (existsStorageHelper.ElemId == elem.Id.IntegerValue)
            {
                viewBasedData = existsStorageHelper.LstViewBasedDataInfo != null && existsStorageHelper.LstViewBasedDataInfo.Any() ? existsStorageHelper.LstViewBasedDataInfo.Where(x => x.ViewId == CurrentView.Id.IntegerValue).Select(x => x).FirstOrDefault() : null;
            }

            return viewBasedData;
        }

        private bool CheckAnnotateFamPlaced(ViewBasedData viewBasedData, bool isAnnotatePlaced)
        {
            foreach (int famInsId in viewBasedData.lstFamInsId)
            {
                ElementId famElemId = new ElementId(famInsId);

                Element famInsElem = doc.GetElement(famElemId);

                if (famInsElem != null)
                {
                    isAnnotatePlaced = true;
                    break;
                }
            }

            return isAnnotatePlaced;
        }

        public List<ElementDataInfo> ReadAndFillReinforcementData(bool IsFlipped)
        {
            LstSelectedElementDataInfo = ReadSelectedLineElementDataInfo(false, true);

            LstExistDataStorage = ReadSettings();

            try
            {
                if (!LstSelectedElementDataInfo.Any())
                {
                    return LstSelectedElementDataInfo;
                }


                //foreach (ElementDataInfo rebarElemDataInfo in LstSelectedElementDataInfo)
                //{
                //    rebarElemDataInfo.IsFlipped = IsFlipped;

                //    AssignRebarElemDataInfo(rebarElemDataInfo);
                //}

                foreach (ElementDataInfo elemDataInfo in LstSelectedElementDataInfo)
                {
                    Autodesk.Revit.DB.View CurrentView = elemDataInfo.CurrentView == null ? doc.ActiveView : elemDataInfo.CurrentView;

                    Plane plane = ReadPlaneFromActiveView(CurrentView);

                    elemDataInfo.PlaneNormalDirection = plane.Normal;

                    StorageHelper existsStorageHelper = LstExistDataStorage.Where(x => x.ElemId == elemDataInfo.ElemID).Select(x => x).FirstOrDefault();

                    if (existsStorageHelper != null)
                    {
                        ElementId elemId = new ElementId(elemDataInfo.ElemID);

                        Element elem = doc.GetElement(elemId);

                        if (elem != null)
                        {
                            ViewBasedData viewBasedData = GetViewBasedDataa(existsStorageHelper, elem, CurrentView);

                            bool isAssined = false;

                            elemDataInfo.IsFlipped = IsFlipped;

                            isAssined = AssignRebarElemDataInfo(elemDataInfo);

                            if (isAssined && viewBasedData != null)
                            {
                                DeleteExistsAnnotation(viewBasedData);

                                existsStorageHelper.LstViewBasedDataInfo.RemoveAll(x => x == viewBasedData);
                            }
                        }
                        else
                        {
                            ViewBasedData viewBasedData = GetViewBasedDataa(existsStorageHelper, elem, CurrentView);

                            DeleteExistsAnnotation(viewBasedData);

                            existsStorageHelper.LstViewBasedDataInfo.RemoveAll(x => x == viewBasedData);

                        }
                    }
                    else
                    {
                        elemDataInfo.IsFlipped = IsFlipped;

                        bool isAssined = AssignRebarElemDataInfo(elemDataInfo);
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return LstSelectedElementDataInfo;
        }

        private Plane ReadPlaneFromActiveView(Autodesk.Revit.DB.View CurrentView)
        {
            Plane plane = Plane.CreateByNormalAndOrigin(CurrentView.ViewDirection, CurrentView.Origin);



            return plane;
        }

        private bool AssignRebarElemDataInfo(ElementDataInfo rebarElemDataInfo, string existsHashCodeData = null)
        {
            bool isAssined = false;

            List<XYZ> lstXYZPoints = new List<XYZ>();

            ElementId elemId = new ElementId(rebarElemDataInfo.ElemID);

            Element element = doc.GetElement(elemId);

            Autodesk.Revit.DB.View CurrentView = rebarElemDataInfo.CurrentView == null ? doc.ActiveView : rebarElemDataInfo.CurrentView;

            if (element is RebarInSystem)
            {
                rebarElemDataInfo.RebarElementType = ElementType.RebarSystem;
            }
            else if (element is Rebar)
            {
                rebarElemDataInfo.RebarElementType = ElementType.Rebar;
            }
            else
            {
                return isAssined;
            }

            ChangeHookParameter(rebarElemDataInfo, true);

            string geoHashCodeData = string.Empty;

            BoundingBoxXYZ elemBbox = element.get_BoundingBox(CurrentView);

            rebarElemDataInfo.LstRebarPatternShapeData = ReadRebarLinePatternInfo(rebarElemDataInfo, element, out geoHashCodeData, CurrentView);

            if (rebarElemDataInfo.LstRebarPatternShapeData.Any())
            {
                string boundaryTransformCode = elemBbox.Min.ToString() + elemBbox.Max.ToString() + elemBbox.Transform.Origin.ToString();

                string elemHashCode = String.Concat(geoHashCodeData, boundaryTransformCode);

                rebarElemDataInfo.ElemHashCodeData = elemHashCode;

                if (!string.IsNullOrEmpty(existsHashCodeData) && elemHashCode.Equals(existsHashCodeData))
                {
                    //Already Exists  not to update the same
                }
                else
                {
                    rebarElemDataInfo.LstRebarPatternShapeData = FillRebarShapeDetails(rebarElemDataInfo, elemBbox, CurrentView);

                    isAssined = true;
                }
            }

            ChangeHookParameter(rebarElemDataInfo, false);

            return isAssined;
        }

        private List<RebarShapeData> FillRebarShapeDetails(ElementDataInfo elemDataInfo, BoundingBoxXYZ elemBbox, Autodesk.Revit.DB.View CurrentView)
        {
            List<RebarShapeData> lstRebarShapeData = elemDataInfo.LstRebarPatternShapeData;

            bool isFlipped = elemDataInfo.IsFlipped;

            try
            {
                foreach (RebarShapeData rebarShapeData in lstRebarShapeData)
                {
                    if (rebarShapeData.LstPatternCurves.Any())
                    {
                        List<Line> lstPatternLine = rebarShapeData.LstPatternCurves.Where(x => x is Line).Select(x => x as Line).ToList();

                        List<Arc> lstPatternArc = rebarShapeData.LstPatternCurves.Where(x => x is Arc).Select(x => x as Arc).ToList();

                        List<CurveDataInfo> lstCurveDataInfo = new List<CurveDataInfo>();


                        lstCurveDataInfo.AddRange(GetLineDataInfo(lstPatternLine));

                        lstCurveDataInfo.AddRange(GetArcDataInfo(lstPatternArc));

                        bool isConsolidatedCurves = ConsolidateCurves(lstCurveDataInfo);

                        if (!isConsolidatedCurves)
                            continue;

                        if (lstCurveDataInfo.Any())
                        {
                            CheckAndValidateHookTypeCurve(elemDataInfo, lstCurveDataInfo);

                            bool isPointBasedFamSet = false;

                            foreach (CurveDataInfo currentCurveDataInfo in lstCurveDataInfo)
                            {
                                if (currentCurveDataInfo.CurverType == CurveTypeInfo.Arc)
                                {
                                    continue;
                                }


                                XYZ planeParalelDirection = CurrentView.UpDirection.Normalize();
                                currentCurveDataInfo.StartPointInfo.PlaneParalelDirection = planeParalelDirection;

                                currentCurveDataInfo.EndPointInfo.PlaneParalelDirection = planeParalelDirection;

                                AssingBasicDetailOfCornerPoint(elemDataInfo, rebarShapeData, elemBbox, currentCurveDataInfo, lstCurveDataInfo);

                                XYZ startPoint = currentCurveDataInfo.StartPointInfo.CornerPoint;
                                XYZ endPoint = currentCurveDataInfo.EndPointInfo.CornerPoint;

                                Line currentLine = Line.CreateBound(startPoint, endPoint);

                                if (!currentCurveDataInfo.StartPointInfo.IsAnnotateFamSet || !currentCurveDataInfo.EndPointInfo.IsAnnotateFamSet)
                                {
                                    currentCurveDataInfo.PlaneParalelDirection = planeParalelDirection;

                                    CurveDataInfo startAttachCurveData = default(CurveDataInfo);

                                    CurveDataInfo endAttachCurveData = default(CurveDataInfo);

                                    GetSharedCurveDataInfo(lstCurveDataInfo, currentCurveDataInfo, out startAttachCurveData, out endAttachCurveData);

                                    bool isFamSet = AssignPointBasedFamilyInfo(elemDataInfo, currentCurveDataInfo, startAttachCurveData, endAttachCurveData, lstCurveDataInfo, CurrentView);

                                    if (isFamSet)
                                    {
                                        isPointBasedFamSet = true;
                                    }
                                }
                            }
                            if (isPointBasedFamSet)
                            {
                                rebarShapeData.LstCurveDataInfo = lstCurveDataInfo;
                            }
                        }
                    }
                }

                lstRebarShapeData = lstRebarShapeData.Where(x => x.LstCurveDataInfo != null).Select(x => x).ToList();

            }
            catch (Exception ex)
            {
            }

            return lstRebarShapeData;
        }

        private void CheckAndValidateHookTypeCurve(ElementDataInfo elemDataInfo, List<CurveDataInfo> lstCurveDataInfo)
        {
            if (lstCurveDataInfo.Count == 1 && lstCurveDataInfo[0].RVTCurve is Line)
            {
                CurveDataInfo currentCurveDataInfo = lstCurveDataInfo[0];

                Line currentLineData = lstCurveDataInfo[0].RVTCurve as Line;

                if (elemDataInfo.HookStartId != null && elemDataInfo.HookStartId != ElementId.InvalidElementId)
                {
                    currentCurveDataInfo.StartPointInfo.IsHookType = true;
                }

                if (elemDataInfo.HookEndId != null && elemDataInfo.HookEndId != ElementId.InvalidElementId)
                {
                    currentCurveDataInfo.EndPointInfo.IsHookType = true;
                }
            }
        }

        private void AssingBasicDetailOfCornerPoint(ElementDataInfo elemData, RebarShapeData rebarShapeData, BoundingBoxXYZ elemBbox, CurveDataInfo currentCurveDataInfo, List<CurveDataInfo> lstCurveDataInfo)
        {
            Line refLine = Line.CreateBound(currentCurveDataInfo.StartPointInfo.CornerPoint, currentCurveDataInfo.EndPointInfo.CornerPoint);

            currentCurveDataInfo.StartPointInfo.CurrentViewId = elemData.CurrentViewId;
            currentCurveDataInfo.EndPointInfo.CurrentViewId = elemData.CurrentViewId;

            currentCurveDataInfo.StartPointInfo.HashCodeData = elemData.ElemHashCodeData;
            currentCurveDataInfo.EndPointInfo.HashCodeData = elemData.ElemHashCodeData;

            currentCurveDataInfo.StartPointInfo.ElemID = rebarShapeData.ElemID;
            currentCurveDataInfo.EndPointInfo.ElemID = rebarShapeData.ElemID;

            currentCurveDataInfo.StartPointInfo.IsFlipElement = elemData.IsFlipped;
            currentCurveDataInfo.EndPointInfo.IsFlipElement = elemData.IsFlipped;

            XYZ minPoint;
            XYZ maxPoint;
            XYZ midPoint;

            GetBoundaryMinMax(rebarShapeData.LstPatternCurves, out minPoint, out maxPoint, out midPoint);

            currentCurveDataInfo.StartPointInfo.BoundaryMinPoint = minPoint;
            currentCurveDataInfo.StartPointInfo.BoundaryMaxPoint = maxPoint;
            currentCurveDataInfo.StartPointInfo.BoundaryMidPoint = midPoint;

            currentCurveDataInfo.EndPointInfo.BoundaryMinPoint = minPoint;
            currentCurveDataInfo.EndPointInfo.BoundaryMaxPoint = maxPoint;
            currentCurveDataInfo.EndPointInfo.BoundaryMidPoint = midPoint;

            currentCurveDataInfo.StartPointInfo.RefLine = refLine;
            currentCurveDataInfo.EndPointInfo.RefLine = refLine;


            currentCurveDataInfo.StartPointInfo.ElemBoundingBox = elemBbox;
            currentCurveDataInfo.EndPointInfo.ElemBoundingBox = elemBbox;


            currentCurveDataInfo.StartPointInfo.LstCurveDataInfo = lstCurveDataInfo;
            currentCurveDataInfo.EndPointInfo.LstCurveDataInfo = lstCurveDataInfo;

            currentCurveDataInfo.StartPointInfo.BarsOnNormalSide = rebarShapeData.BarsOnNormalView;
            currentCurveDataInfo.EndPointInfo.BarsOnNormalSide = rebarShapeData.BarsOnNormalView;
        }

        private bool ConsolidateCurves(List<CurveDataInfo> lstCurveDataInfo)
        {
            bool isConsolidated = false;

            try
            {
                List<CurveDataInfo> lstLineCurveDataInfo = lstCurveDataInfo.Where(x => x.RVTCurve is Line).Select(x => x).ToList();

                List<CurveDataInfo> lstArcCurveDataInfo = lstCurveDataInfo.Where(x => x.RVTCurve is Arc).Select(x => x).ToList();

                //CreateTemLineWithCurves(lstPatternLines.Select(x => x as Curve).ToList());

                ConsolidateArcCurve(lstLineCurveDataInfo, lstArcCurveDataInfo);

                //CreateTemLineWithCurves(lstLineCurveDataInfo.Select(x => x.RVTCurve as Curve).ToList());

                // MakeLinesAsCollinear(lstPatternLines);

                ConsolidatedLineCurve(lstLineCurveDataInfo);

                isConsolidated = true;

                lstCurveDataInfo.Clear();

                lstCurveDataInfo.AddRange(lstLineCurveDataInfo);
                lstCurveDataInfo.AddRange(lstArcCurveDataInfo);
            }
            catch (Exception ex)
            {
            }



            return isConsolidated;
        }

        private void MakeLinesAsCollinear(List<Line> lstPatternLines)
        {
            List<Line> lstConsCopyLines = new List<Line>(lstPatternLines);

            foreach (Line _line in lstConsCopyLines)
            {
                List<XYZ> lstCollinPoints = new List<XYZ>();

                XYZ lneStartPoint = _line.GetEndPoint(0);
                XYZ lneEndPoint = _line.GetEndPoint(1);

                lstCollinPoints.Add(lneStartPoint);
                lstCollinPoints.Add(lneEndPoint);

                List<Line> lstCollinearLines = GetCollinearCurves(lstPatternLines, _line);

                Line collinearLine = GetCollinearLine(lstCollinPoints, lstCollinearLines);

                if (collinearLine != null)
                {
                    lstPatternLines.RemoveAll(x => lstCollinearLines.Any(y => y == x));
                    lstPatternLines.Remove(_line);

                    lstPatternLines.Add(collinearLine);
                }
            }
        }

        private Line GetCollinearLine(List<XYZ> lstCollinPoints, List<Line> lstCollinearLines)
        {
            Line collinearLine = default(Line);

            List<XYZ> lstMergeCollinePoints = new List<XYZ>();


            if (lstCollinearLines.Any())
            {

                lstCollinPoints.AddRange(lstCollinearLines.Select(x => x.GetEndPoint(0)).ToList());

                lstCollinPoints.AddRange(lstCollinearLines.Select(x => x.GetEndPoint(1)).ToList());

                foreach (XYZ currentPoint in lstCollinPoints)
                {
                    List<XYZ> lstCollinearMergePoints = lstCollinPoints.Where(x => CheckTwoPointsAreEqual(currentPoint, x)).Select(x => x).ToList();

                    if (lstCollinearMergePoints.Any() && lstCollinearMergePoints.Count == 1)
                    {
                        lstMergeCollinePoints.Add(currentPoint);
                    }
                }

            }

            if (lstMergeCollinePoints.Any() && lstMergeCollinePoints.Count == 2)
            {
                XYZ startPoint = lstMergeCollinePoints[0];
                XYZ endPoint = lstMergeCollinePoints[1];

                collinearLine = Line.CreateBound(startPoint, endPoint);
            }

            return collinearLine;
        }

        private List<Line> GetCollinearCurves(List<Line> lstPatternLines, Line currentLine)
        {
            XYZ lneStartPoint = currentLine.GetEndPoint(0);
            XYZ lneEndPoint = currentLine.GetEndPoint(1);

            XYZ currentLineDirection = currentLine.Direction;

            List<Line> lstAttachedCollinearLines = new List<Line>();

            List<Line> lstDirectionalLines = lstPatternLines.Where(x => CheckTwoPointsAreEqual(currentLineDirection, x.Direction)).Select(x => x).ToList();

            foreach (Line directionalLine in lstDirectionalLines)
            {
                XYZ collinearSP = directionalLine.GetEndPoint(0);
                XYZ collinearEP = directionalLine.GetEndPoint(1);

                if (CheckTwoPointsAreEqual(collinearSP, lneStartPoint) && CheckTwoPointsAreEqual(collinearEP, lneEndPoint))
                {
                    continue;
                }

                if (CheckTwoPointsAreEqual(lneStartPoint, collinearSP) || CheckTwoPointsAreEqual(lneStartPoint, collinearEP) || CheckTwoPointsAreEqual(lneEndPoint, collinearSP) || CheckTwoPointsAreEqual(lneEndPoint, collinearEP))
                {
                    lstAttachedCollinearLines.Add(directionalLine);
                }
            }

            return lstAttachedCollinearLines;
        }

        private Line GetNewLineBasedOnArc(XYZ arcStartPoint, XYZ arcEndPoint, XYZ projectPoint, XYZ lineSP, XYZ lineEP)
        {
            Line newLine = default(Line);

            if (CheckTwoPointsAreEqual(lineSP, arcStartPoint) || CheckTwoPointsAreEqual(lineSP, arcEndPoint))
            {
                newLine = Line.CreateBound(lineEP, projectPoint);
            }
            else if (CheckTwoPointsAreEqual(lineEP, arcStartPoint) || CheckTwoPointsAreEqual(lineEP, arcEndPoint))
            {
                newLine = Line.CreateBound(lineSP, projectPoint);
            }

            return newLine;
        }


        private void ConsolidateArcCurve(List<CurveDataInfo> lstPatternLines, List<CurveDataInfo> lstPatternArc)
        {
            List<CurveDataInfo> lstRemovableCurves = new List<CurveDataInfo>();

            foreach (CurveDataInfo currentCurveDataArc in lstPatternArc)
            {
                Arc currentArc = currentCurveDataArc.RVTCurve as Arc;

                XYZ arcStartPoint = currentArc.GetEndPoint(0);
                XYZ arcEndPoint = currentArc.GetEndPoint(1);

                List<Line> lstAttchedLine = GetAttachedLineWithCurve(lstPatternLines, arcStartPoint, arcEndPoint);

                if (lstAttchedLine.Any() && lstAttchedLine.Count == 2)
                {
                    Line line1 = lstAttchedLine[0];
                    Line line2 = lstAttchedLine[1];


                    XYZ againstPointLineOne = default(XYZ);
                    XYZ againstPointLineTwo = default(XYZ);


                    GetAgainstPoint(arcStartPoint, arcEndPoint, line1, ref againstPointLineOne);

                    GetAgainstPoint(arcStartPoint, arcEndPoint, line2, ref againstPointLineTwo);

                    if (againstPointLineOne != null && againstPointLineTwo != null)
                    {
                        Line extendedLine1 = ExtendLineFromMidPoint(line1);
                        Line extendedLine2 = ExtendLineFromMidPoint(line2);

                        IntersectionResultArray resArray = new IntersectionResultArray();

                        extendedLine1.Intersect(extendedLine2, out resArray);

                        XYZ hitPoint = default(XYZ);

                        if (resArray != null && !resArray.IsEmpty)
                        {
                            List<IntersectionResult> lstIntersectResult = resArray.Cast<IntersectionResult>().ToList();

                            if (lstIntersectResult.Any() && lstIntersectResult.Count == 1)
                            {
                                hitPoint = lstIntersectResult.FirstOrDefault().XYZPoint;
                            }
                        }

                        if (hitPoint != null)
                        {
                            Line newLineOne = Line.CreateBound(hitPoint, againstPointLineOne);
                            Line newLineTwo = Line.CreateBound(hitPoint, againstPointLineTwo);


                            CurveDataInfo curveDataInfo1 = CreateCurveDataPoints(CurveTypeInfo.Line, newLineOne);

                            CurveDataInfo curveDataInfo2 = CreateCurveDataPoints(CurveTypeInfo.Line, newLineTwo);

                            List<CurveDataInfo> lstRemovableLineCurveData = lstPatternLines.Where(x => (x.RVTCurve as Line) == line1 || (x.RVTCurve as Line) == line2).Select(x => x).ToList();

                            lstPatternLines.RemoveAll(x => lstRemovableLineCurveData.Any(y => y == x));

                            lstRemovableCurves.Add(currentCurveDataArc);

                            lstPatternLines.AddRange(new List<CurveDataInfo> { curveDataInfo1, curveDataInfo2 });

                        }
                    }
                }
            }

            lstPatternArc.RemoveAll(x => lstRemovableCurves.Any(y => x == y));
        }

        private void ConsolidatedLineCurve(List<CurveDataInfo> lstPatternLines)
        {

            List<CurveDataInfo> lstNewLinePattern = new List<CurveDataInfo>();
            List<CurveDataInfo> lstToRemovePattern = new List<CurveDataInfo>();


            bool isPatternChanged = true;

            List<CurveDataInfo> lstRemovableLines = new List<CurveDataInfo>();
            List<CurveDataInfo> lstNewLines = new List<CurveDataInfo>();


            foreach (CurveDataInfo curveLineData in lstPatternLines)
            {
                Line currentLine = curveLineData.RVTCurve as Line;

                XYZ currentLineSp = currentLine.GetEndPoint(0);
                XYZ currentLineEp = currentLine.GetEndPoint(1);

                foreach (CurveDataInfo tempCurveDataLine in lstPatternLines)
                {
                    Line tempLine = tempCurveDataLine.RVTCurve as Line;

                    XYZ tempLineSp = tempLine.GetEndPoint(0);
                    XYZ tempLineEp = tempLine.GetEndPoint(1);

                    if (CheckTwoPointsAreEqual(tempLineSp, currentLineSp) && CheckTwoPointsAreEqual(tempLineEp, currentLineEp))
                    {
                        continue;
                    }

                    XYZ hitPoint = default(XYZ);

                    IntersectionResultArray resArray = new IntersectionResultArray();

                    currentLine.Intersect(tempLine, out resArray);

                    if (resArray != null && !resArray.IsEmpty)
                    {
                        List<IntersectionResult> lstIntersectResult = resArray.Cast<IntersectionResult>().ToList();

                        if (lstIntersectResult.Any() && lstIntersectResult.Count == 1)
                        {
                            XYZ hitedPoint = lstIntersectResult.FirstOrDefault().XYZPoint;

                            if (!CheckTwoPointsAreEqual(hitedPoint, tempLineSp) && !CheckTwoPointsAreEqual(hitedPoint, tempLineEp) && !CheckTwoPointsAreEqual(hitedPoint, currentLineSp) && !CheckTwoPointsAreEqual(hitedPoint, currentLineEp))
                            {
                                hitPoint = hitedPoint;
                            }
                        }
                    }
                    if (hitPoint != null)
                    {
                        Line newLineOne = default(Line);
                        Line newLineTwo = default(Line);

                        newLineOne = GetIntersectMaxLine(tempLineSp, tempLineEp, hitPoint);

                        newLineTwo = GetIntersectMaxLine(currentLineSp, currentLineEp, hitPoint);

                        if (newLineOne != null && newLineTwo != null)
                        {
                            CurveDataInfo curveDataInfoOne = CreateCurveDataPoints(CurveTypeInfo.Line, newLineOne);

                            CurveDataInfo curveDataInfoTwo = CreateCurveDataPoints(CurveTypeInfo.Line, newLineTwo);

                            if (!lstRemovableLines.Any(x => CheckTwoLinesAreEqual(x.RVTCurve as Line, currentLine)))
                            {
                                lstRemovableLines.Add(curveLineData);
                            }

                            if (!lstRemovableLines.Any(x => CheckTwoLinesAreEqual(x.RVTCurve as Line, tempLine)))
                            {
                                lstRemovableLines.Add(tempCurveDataLine);
                            }

                            List<CurveDataInfo> lstNewConsolLine = new List<CurveDataInfo> { curveDataInfoOne, curveDataInfoTwo };

                            if (!lstNewLinePattern.Any(x => CheckTwoLinesAreEqual(x.RVTCurve as Line, newLineOne) || CheckTwoLinesAreEqual(x.RVTCurve as Line, newLineTwo)))
                            {
                                lstNewLinePattern.AddRange(lstNewConsolLine);
                            }
                            isPatternChanged = true;
                        }
                    }
                }
            }

            lstPatternLines.RemoveAll(x => lstRemovableLines.Any(y => y == x));

            lstPatternLines.AddRange(lstNewLinePattern);
        }


        private Line GetIntersectMaxLine(XYZ LineSp, XYZ LineEp, XYZ hitPoint)
        {
            Line newLineOne;
            Line testLine1 = Line.CreateBound(hitPoint, LineSp);
            Line testLine2 = Line.CreateBound(hitPoint, LineEp);

            if (testLine1.Length > testLine2.Length)
            {
                newLineOne = testLine1;
            }
            else
            {
                newLineOne = testLine2;
            }

            return newLineOne;
        }

        private void GetAgainstPoint(XYZ StartPoint, XYZ EndPoint, Line currentLine, ref XYZ againstPoint)
        {
            XYZ lineStartPoint = currentLine.GetEndPoint(0);
            XYZ lineEndPoint = currentLine.GetEndPoint(1);

            if (CheckTwoPointsAreEqual(lineStartPoint, StartPoint) || CheckTwoPointsAreEqual(lineEndPoint, StartPoint))
            {
                if (CheckTwoPointsAreEqual(lineStartPoint, StartPoint))
                {
                    againstPoint = lineEndPoint;
                }
                else if (CheckTwoPointsAreEqual(lineEndPoint, StartPoint))
                {
                    againstPoint = lineStartPoint;
                }
            }
            else if (CheckTwoPointsAreEqual(lineStartPoint, EndPoint) || CheckTwoPointsAreEqual(lineEndPoint, EndPoint))
            {
                if (CheckTwoPointsAreEqual(lineStartPoint, EndPoint))
                {
                    againstPoint = lineEndPoint;
                }
                else if (CheckTwoPointsAreEqual(lineEndPoint, EndPoint))
                {
                    againstPoint = lineStartPoint;
                }
            }
        }

        private List<Line> GetAttachedLineWithCurve(List<CurveDataInfo> lstPatternLines, XYZ startPoint, XYZ endPoint)
        {
            List<Line> lstAttchedLine = new List<Line>();

            foreach (CurveDataInfo currentCurveLine in lstPatternLines)
            {
                Line currentLine = currentCurveLine.RVTCurve as Line;

                XYZ lineStartPoint = currentLine.GetEndPoint(0);
                XYZ lineEndPoint = currentLine.GetEndPoint(1);

                if (CheckTwoPointsAreEqual(lineStartPoint, startPoint) || CheckTwoPointsAreEqual(lineEndPoint, startPoint) || CheckTwoPointsAreEqual(lineStartPoint, endPoint) || CheckTwoPointsAreEqual(lineEndPoint, endPoint))
                {
                    lstAttchedLine.Add(currentLine);
                }
            }

            return lstAttchedLine;
        }

        private bool AssignPointBasedFamilyInfo(ElementDataInfo elemDataInfo, CurveDataInfo currentCurveData, CurveDataInfo startPointAttachCurve, CurveDataInfo endPointAttachCurve, List<CurveDataInfo> lstCurveDataInfo, Autodesk.Revit.DB.View CurrentView)
        {
            bool isAnnotateFamSet = false;

            XYZ currentStartPoint = currentCurveData.StartPointInfo.CornerPoint;
            XYZ currentEndPoint = currentCurveData.EndPointInfo.CornerPoint;

            Line currentLine = Line.CreateBound(currentStartPoint, currentEndPoint);

            XYZ lineDirection = currentLine.Direction;

            XYZ planeParalelDirection = currentCurveData.PlaneParalelDirection;

            if (CheckTwoPointsAreEqual(lineDirection, elemDataInfo.PlaneNormalDirection) || CheckTwoPointsAreEqual(lineDirection.Negate(), elemDataInfo.PlaneNormalDirection))
            {
                isAnnotateFamSet = CreateAnnotateForNormalPlaneLine(elemDataInfo, currentCurveData, startPointAttachCurve, endPointAttachCurve);
            }
            else if (CheckTwoPointsAreEqual(lineDirection, CurrentView.RightDirection) || CheckTwoPointsAreEqual(lineDirection.Negate(), CurrentView.RightDirection) || CheckTwoPointsAreEqual(lineDirection, CurrentView.UpDirection) || CheckTwoPointsAreEqual(lineDirection.Negate(), CurrentView.UpDirection))
            {
                //straightLine
                isAnnotateFamSet = CreateAnnotationForParalelToPlaneLine(currentCurveData, startPointAttachCurve, endPointAttachCurve);
            }
            else
            {
                //AngledLine
                isAnnotateFamSet = CreateAnnotationForAngleBasedLine(elemDataInfo, currentCurveData, startPointAttachCurve, endPointAttachCurve, lstCurveDataInfo);
            }

            return isAnnotateFamSet;
        }

        private bool CreateAnnotationForAngleBasedLine(ElementDataInfo elemDataInfo, CurveDataInfo currentCurveData, CurveDataInfo startPointAttachCurve, CurveDataInfo endPointAttachCurve, List<CurveDataInfo> lstCurveDataInfo)
        {
            bool isAnnotateSet = false;

            XYZ currentCurveStartPoint = currentCurveData.StartPointInfo.CornerPoint;
            XYZ currentCurveEndPoint = currentCurveData.EndPointInfo.CornerPoint;

            Line refLine = Line.CreateBound(currentCurveStartPoint, currentCurveEndPoint);

            if (endPointAttachCurve != null)
            {
                XYZ startPoint = endPointAttachCurve.StartPointInfo.CornerPoint;
                XYZ endPoint = endPointAttachCurve.EndPointInfo.CornerPoint;

                Line line = Line.CreateBound(startPoint, endPoint);

                if (!currentCurveData.EndPointInfo.IsAnnotateFamSet && !CheckTwoPointsAreEqual(line.Direction, elemDataInfo.PlaneNormalDirection) && !CheckTwoPointsAreEqual(line.Direction, elemDataInfo.PlaneNormalDirection.Negate()))
                {
                    currentCurveData.EndPointInfo.AnnotateFam = AnnotationFamily.Centre;
                    currentCurveData.EndPointInfo.IsAnnotateFamSet = true;

                    isAnnotateSet = true;
                }
            }

            if (startPointAttachCurve != null)
            {
                XYZ stratPoint = startPointAttachCurve.StartPointInfo.CornerPoint;
                XYZ endPoint = startPointAttachCurve.EndPointInfo.CornerPoint;

                Line line = Line.CreateBound(stratPoint, endPoint);

                if (!currentCurveData.StartPointInfo.IsAnnotateFamSet && !CheckTwoPointsAreEqual(line.Direction, elemDataInfo.PlaneNormalDirection) && !CheckTwoPointsAreEqual(line.Direction, elemDataInfo.PlaneNormalDirection.Negate()))
                {
                    currentCurveData.StartPointInfo.AnnotateFam = AnnotationFamily.Centre;
                    currentCurveData.StartPointInfo.IsAnnotateFamSet = true;

                    isAnnotateSet = true;
                }

            }
            #region MyRegion
            //else if (endPointAttachCurve != null && endPointAttachCurve.RVTCurve is Arc)
            //{
            //    CurveDataInfo startArcAttachmentCurvePoint = default(CurveDataInfo);
            //    CurveDataInfo endArcAttachmentCurvePoint = default(CurveDataInfo);

            //    GetSharedCurveDataInfo(lstCurveDataInfo, endPointAttachCurve, out startArcAttachmentCurvePoint, out endArcAttachmentCurvePoint);


            //    if (startArcAttachmentCurvePoint.RVTCurve is Line)
            //    {
            //        bool isNormalCurve = CheckAttachCurveIsNormalToPlane(currentCurveData, startArcAttachmentCurvePoint);

            //        if (!isNormalCurve)
            //        {
            //            currentCurveData.EndPointInfo.AnnotateFam = AnnotationFamily.Line;
            //            currentCurveData.EndPointInfo.IsAnnotateFamSet = true;

            //            isAnnotateSet = true;
            //        }
            //    }
            //}
            //else if (startPointAttachCurve != null && startPointAttachCurve.RVTCurve is Arc)
            //{
            //    CurveDataInfo startArcAttachmentCurvePoint = default(CurveDataInfo);
            //    CurveDataInfo endArcAttachmentCurvePoint = default(CurveDataInfo);

            //    GetSharedCurveDataInfo(lstCurveDataInfo, startPointAttachCurve, out startArcAttachmentCurvePoint, out endArcAttachmentCurvePoint);


            //    if (startArcAttachmentCurvePoint.RVTCurve is Line)
            //    {
            //        bool isNormalCurve = CheckAttachCurveIsNormalToPlane(currentCurveData, startArcAttachmentCurvePoint);

            //        if (!isNormalCurve)
            //        {
            //            currentCurveData.StartPointInfo.AnnotateFam = AnnotationFamily.Line;
            //            currentCurveData.StartPointInfo.IsAnnotateFamSet = true;

            //            isAnnotateSet = true;
            //        }
            //    }
            //} 
            #endregion
            if (startPointAttachCurve == null)
            {
                currentCurveData.StartPointInfo.AnnotateFam = AnnotationFamily.Line;
                currentCurveData.StartPointInfo.IsAnnotateFamSet = true;

                isAnnotateSet = true;
            }
            if (endPointAttachCurve == null)
            {
                currentCurveData.EndPointInfo.AnnotateFam = AnnotationFamily.Line;
                currentCurveData.EndPointInfo.IsAnnotateFamSet = true;

                isAnnotateSet = true;
            }

            return isAnnotateSet;
        }


        private bool CreateAnnotationForParalelToPlaneLine(CurveDataInfo currentCurveData, CurveDataInfo startPointAttachCurve, CurveDataInfo endPointAttachCurve)
        {
            bool isAnnotateSet = false;

            XYZ currentBaseStartPoint = currentCurveData.StartPointInfo.CornerPoint;
            XYZ currentBaseEndPoint = currentCurveData.EndPointInfo.CornerPoint;

            Line refLine = Line.CreateBound(currentBaseStartPoint, currentBaseEndPoint);

            if (!currentCurveData.StartPointInfo.IsAnnotateFamSet && startPointAttachCurve == null)
            {
                currentCurveData.StartPointInfo.AnnotateFam = AnnotationFamily.Line;
                currentCurveData.StartPointInfo.IsAnnotateFamSet = true;

                isAnnotateSet = true;
            }

            if (!currentCurveData.EndPointInfo.IsAnnotateFamSet && endPointAttachCurve == null)
            {
                currentCurveData.EndPointInfo.AnnotateFam = AnnotationFamily.Line;
                currentCurveData.EndPointInfo.IsAnnotateFamSet = true;

                isAnnotateSet = true;
            }

            return isAnnotateSet;
        }

        private bool CreateAnnotateForNormalPlaneLine(ElementDataInfo elemDataInfo, CurveDataInfo currentCurveData, CurveDataInfo startPointAttachCurve, CurveDataInfo endPointAttachCurve)
        {
            bool isAnnotateFamSet = false;

            if (!currentCurveData.StartPointInfo.IsAnnotateFamSet && startPointAttachCurve == null && endPointAttachCurve != null)
            {
                isAnnotateFamSet = CreateAnnotationForCrossAndCircle(elemDataInfo, currentCurveData.StartPointInfo, endPointAttachCurve);//NormalPlaneLine

            }
            else if (!currentCurveData.EndPointInfo.IsAnnotateFamSet && endPointAttachCurve == null && startPointAttachCurve != null)
            {
                isAnnotateFamSet = CreateAnnotationForCrossAndCircle(elemDataInfo, currentCurveData.EndPointInfo, startPointAttachCurve);//NormalPlaneLine
            }
            else if (!currentCurveData.StartPointInfo.IsAnnotateFamSet && !currentCurveData.EndPointInfo.IsAnnotateFamSet && startPointAttachCurve != null && endPointAttachCurve != null)
            {
                currentCurveData.StartPointInfo.AnnotateFam = AnnotationFamily.Cross;

                currentCurveData.StartPointInfo.IsAnnotateFamSet = true;
                currentCurveData.EndPointInfo.IsAnnotateFamSet = true;

                isAnnotateFamSet = true;
            }



            return isAnnotateFamSet;
        }

        private bool CreateAnnotationForCrossAndCircle(ElementDataInfo elemDataInfo, CornerPointInfo currentPointInfo, CurveDataInfo mergedCurve)
        {
            bool isAnnotationSet = false;

            XYZ mergedLineStartPoint = mergedCurve.StartPointInfo.CornerPoint;

            XYZ mergedLineEndPoint = mergedCurve.EndPointInfo.CornerPoint;

            Line mergePointLine = Line.CreateBound(mergedLineStartPoint, mergedLineEndPoint);

            Line cloneMergedCurve = mergePointLine.Clone() as Line;

            cloneMergedCurve.MakeUnbound();

            XYZ extendPlanePoint = elemDataInfo.PlaneNormalDirection.Multiply(100);
            XYZ neagtePlanePoint = elemDataInfo.PlaneNormalDirection.Negate().Multiply(100);

            XYZ towardsPlanePoint = currentPointInfo.CornerPoint.Add(extendPlanePoint);
            XYZ againstPlane = currentPointInfo.CornerPoint.Add(neagtePlanePoint);

            Line normalToPlane = Line.CreateBound(currentPointInfo.CornerPoint, towardsPlanePoint);

            Line againstToPlane = Line.CreateBound(currentPointInfo.CornerPoint, againstPlane);

            List<Line> lstTempLine = new List<Line> { normalToPlane, againstToPlane, mergePointLine };


            SetComparisonResult normalPlaneRes = normalToPlane.Intersect(cloneMergedCurve);

            SetComparisonResult againstPlaneRes = againstToPlane.Intersect(cloneMergedCurve);


            if (normalPlaneRes == SetComparisonResult.Disjoint && againstPlaneRes == SetComparisonResult.Overlap)
            {
                currentPointInfo.AnnotateFam = AnnotationFamily.Circle;
                currentPointInfo.IsAnnotateFamSet = true;
                isAnnotationSet = true;
            }
            else if (normalPlaneRes == SetComparisonResult.Overlap)
            {
                currentPointInfo.AnnotateFam = AnnotationFamily.Cross;
                currentPointInfo.IsAnnotateFamSet = true;
                isAnnotationSet = true;
            }

            return isAnnotationSet;
        }


        //private XYZ GetDirectionOfPlane(XYZ centrePoint, XYZ maxPoint)
        //{
        //    XYZ PerpendicularDirection;
        //    XYZ extendedPlane = PlaneNormalDirection.Multiply(100);

        //    XYZ extednedPoint = extendedPlane.Add(centrePoint);

        //    Line axis = Line.CreateBound(centrePoint, extednedPoint);

        //    double angle_internal_revit_units = 90 * Math.PI / 180 * (-1);

        //    Transform rot = Transform.Identity;

        //    if (Math.Round(centrePoint.X, 4) == Math.Round(maxPoint.X, 4))
        //    {
        //        rot = Transform.CreateRotationAtPoint(XYZ.BasisX, angle_internal_revit_units, centrePoint);
        //    }
        //    else if (Math.Round(centrePoint.Y, 4) == Math.Round(maxPoint.Y, 4))
        //    {
        //        rot = Transform.CreateRotationAtPoint(XYZ.BasisY, angle_internal_revit_units, centrePoint);
        //    }
        //    else
        //    {
        //        rot = Transform.CreateRotationAtPoint(XYZ.BasisZ, angle_internal_revit_units, centrePoint);
        //    }

        //    XYZ transformed = rot.OfPoint(extednedPoint);

        //    Line newPerpendicularLine = Line.CreateBound(centrePoint, transformed);

        //    PerpendicularDirection = newPerpendicularLine.Direction;
        //    return PerpendicularDirection;
        //}


        private void GetSharedCurveDataInfo(List<CurveDataInfo> lstCurveDataInfo, CurveDataInfo currentCurveData, out CurveDataInfo startPointCurveData, out CurveDataInfo endPointCurveData)
        {
            startPointCurveData = default(CurveDataInfo);

            endPointCurveData = default(CurveDataInfo);

            XYZ startPoint = currentCurveData.StartPointInfo.CornerPoint;
            XYZ endPoint = currentCurveData.EndPointInfo.CornerPoint;

            foreach (CurveDataInfo tempCurveDataInfo in lstCurveDataInfo)
            {
                XYZ tempStartPoint = tempCurveDataInfo.StartPointInfo.CornerPoint;
                XYZ tempEndPoint = tempCurveDataInfo.EndPointInfo.CornerPoint;

                if (CheckTwoPointsAreEqual(startPoint, tempStartPoint) && CheckTwoPointsAreEqual(endPoint, tempEndPoint))
                {
                    continue;
                }

                if (CheckTwoPointsAreEqual(startPoint, tempStartPoint) || CheckTwoPointsAreEqual(startPoint, tempEndPoint))
                {
                    startPointCurveData = tempCurveDataInfo;
                }
                else if (CheckTwoPointsAreEqual(endPoint, tempStartPoint) || CheckTwoPointsAreEqual(endPoint, tempEndPoint))
                {
                    endPointCurveData = tempCurveDataInfo;
                }

                if (startPointCurveData != null && endPointCurveData != null)
                {
                    break;
                }
            }
        }

        private List<CurveDataInfo> GetLineDataInfo(List<Line> lstPatternLine)
        {
            List<CurveDataInfo> lstCurveDataInfo = new List<CurveDataInfo>();

            try
            {
                foreach (Line patternLine in lstPatternLine)
                {
                    CurveDataInfo curveDataInfo = CreateCurveDataPoints(CurveTypeInfo.Line, patternLine);

                    if (curveDataInfo.StartPointInfo != null && curveDataInfo.EndPointInfo != null)
                    {
                        lstCurveDataInfo.Add(curveDataInfo);
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return lstCurveDataInfo;
        }

        private List<CurveDataInfo> GetArcDataInfo(List<Arc> lstPatternArc)
        {
            List<CurveDataInfo> lstCurveDataInfo = new List<CurveDataInfo>();

            try
            {
                if (lstPatternArc.Count == 1)
                {
                    List<XYZ> lstEndPoints = new List<XYZ>();

                    lstEndPoints.Add(lstPatternArc[0].GetEndPoint(0));
                    lstEndPoints.Add(lstPatternArc[0].GetEndPoint(1));

                    CurveDataInfo curveDataInfo = CreateCurveDataPoints(CurveTypeInfo.Arc, lstPatternArc[0]);

                    if (curveDataInfo.StartPointInfo != null && curveDataInfo.EndPointInfo != null)
                    {
                        lstCurveDataInfo.Add(curveDataInfo);
                    }

                    return lstCurveDataInfo;
                }


                foreach (Arc patternArc in lstPatternArc)
                {
                    List<Arc> lstCollinearArc = GetCollinearArcCurves(lstPatternArc, patternArc);

                    if (lstCollinearArc.Any())
                    {
                        List<XYZ> lstEndPoints = GetNonColinearEndPoints(lstCollinearArc);

                        if (lstEndPoints.Any() && lstEndPoints.Count == 2)
                        {
                            CurveDataInfo curveDataInfo = CreateCurveDataPoints(CurveTypeInfo.Arc, lstPatternArc[0]);

                            if (curveDataInfo.StartPointInfo != null && curveDataInfo.EndPointInfo != null)
                            {
                                lstCurveDataInfo.Add(curveDataInfo);
                            }
                        }
                    }
                    else
                    {
                        List<XYZ> lstEndPoints = new List<XYZ> { patternArc.GetEndPoint(0), patternArc.GetEndPoint(1) };

                        CurveDataInfo curveDataInfo = CreateCurveDataPoints(CurveTypeInfo.Arc, patternArc);

                        if (curveDataInfo.StartPointInfo != null && curveDataInfo.EndPointInfo != null)
                        {
                            lstCurveDataInfo.Add(curveDataInfo);
                        }

                    }
                }
            }
            catch (Exception ex)
            {

            }

            return lstCurveDataInfo;
        }

        private CurveDataInfo CreateCurveDataPoints(CurveTypeInfo currentCurveType, Curve rvtCurve)
        {
            XYZ startPoint = rvtCurve.GetEndPoint(0);
            XYZ endPoint = rvtCurve.GetEndPoint(1);

            List<XYZ> lstEndPoints = new List<XYZ> { startPoint, endPoint };

            CurveDataInfo curveDataInfo = new CurveDataInfo();

            CornerPointInfo startPointInfo = new CornerPointInfo();

            startPointInfo.CornerPoint = lstEndPoints[0];

            CornerPointInfo endPointInfo = new CornerPointInfo();

            endPointInfo.CornerPoint = lstEndPoints[1];

            curveDataInfo.StartPointInfo = startPointInfo;
            curveDataInfo.EndPointInfo = endPointInfo;
            curveDataInfo.LstEndPoints = lstEndPoints;
            curveDataInfo.CurverType = currentCurveType;
            curveDataInfo.RVTCurve = rvtCurve;

            curveDataInfo.StraightCurve = Line.CreateBound(startPoint, endPoint);

            return curveDataInfo;
        }

        private List<XYZ> GetNonColinearEndPoints(List<Arc> lstCollinearArc)
        {
            List<XYZ> lstNonColinearEndPoints = new List<XYZ>();

            if (lstCollinearArc.Any() && lstCollinearArc.Count == 1)
            {
                lstNonColinearEndPoints.Add(lstCollinearArc[0].GetEndPoint(0));
                lstNonColinearEndPoints.Add(lstCollinearArc[0].GetEndPoint(1));

                return lstNonColinearEndPoints;
            }

            foreach (Curve actualCurve in lstCollinearArc)
            {
                XYZ actualStartPoint = actualCurve.GetEndPoint(0);
                XYZ actualEndPoint = actualCurve.GetEndPoint(1);

                bool isStartPointNonColinear = false;
                bool isEndPointNonColinear = false;

                foreach (Curve currentCurve in lstCollinearArc)
                {
                    if (currentCurve == actualCurve)
                        continue;

                    XYZ currentCurveStartPoint = currentCurve.GetEndPoint(0);
                    XYZ currentCurveEndPoint = currentCurve.GetEndPoint(1);


                    if (CheckTwoPointsAreEqual(currentCurveStartPoint, actualStartPoint) || CheckTwoPointsAreEqual(currentCurveStartPoint, actualEndPoint))
                    {
                        isStartPointNonColinear = true;
                    }

                    if (CheckTwoPointsAreEqual(currentCurveEndPoint, actualStartPoint) || CheckTwoPointsAreEqual(currentCurveEndPoint, actualEndPoint))
                    {
                        isEndPointNonColinear = true;
                    }

                    if (isStartPointNonColinear && isEndPointNonColinear)
                    {
                        break;
                    }
                }

                if (isStartPointNonColinear && !isEndPointNonColinear)
                {
                    lstNonColinearEndPoints.Add(actualStartPoint);
                }
                else if (!isStartPointNonColinear && isEndPointNonColinear)
                {
                    lstNonColinearEndPoints.Add(actualEndPoint);
                }
            }

            return lstNonColinearEndPoints;
        }

        private List<Arc> GetCollinearArcCurves(List<Arc> lstPatternArc, Arc patternArc)
        {
            XYZ startPoint = patternArc.GetEndPoint(0);
            XYZ endPoint = patternArc.GetEndPoint(1);

            List<Arc> lstCollinearArc = new List<Arc>();

            foreach (Arc temPatternArc in lstPatternArc)
            {
                XYZ tempStartPoint = temPatternArc.GetEndPoint(0);
                XYZ tempEndPoint = temPatternArc.GetEndPoint(1);

                if (CheckTwoPointsAreEqual(tempStartPoint, startPoint) && CheckTwoPointsAreEqual(tempEndPoint, endPoint))
                {
                    continue;
                }

                if (CheckTwoPointsAreEqual(tempStartPoint, startPoint) || CheckTwoPointsAreEqual(tempStartPoint, startPoint))
                {
                    lstCollinearArc.Add(temPatternArc);
                }
            }

            return lstCollinearArc;
        }


        private bool IsLineIsInActiveView(List<Curve> lstActiveViewLines, Curve currentCurve)
        {
            bool isInActiveView = false;

            XYZ currentStartPoint = currentCurve.GetEndPoint(0);
            XYZ currentEndPoint = currentCurve.GetEndPoint(1);

            foreach (Curve actualGeoCurve in lstActiveViewLines)
            {
                if (!actualGeoCurve.IsBound)
                    continue;

                XYZ actualStartPoint = actualGeoCurve.GetEndPoint(0);
                XYZ actualEndPoint = actualGeoCurve.GetEndPoint(1);


                if (CheckTwoPointsAreEqual(actualStartPoint, currentStartPoint) || CheckTwoPointsAreEqual(actualEndPoint, currentEndPoint))
                {
                    isInActiveView = true;

                    break;
                }
            }

            return isInActiveView;
        }

        public bool CheckTwoPointsAreEqual(XYZ Point1, XYZ Point2, bool isCheckInTwoDimension = false)
        {
            bool isSamePoint = false;

            if (isCheckInTwoDimension)
            {
                if ((Math.Round(Point1.X, 3) == Math.Round(Point2.X, 3) && Math.Round(Point1.Y, 3) == Math.Round(Point2.Y, 3)))
                {
                    isSamePoint = true;
                }
            }
            else
            {
                if ((Math.Round(Point1.X, 3) == Math.Round(Point2.X, 3) && Math.Round(Point1.Y, 3) == Math.Round(Point2.Y, 3) && Math.Round(Point1.Z, 3) == Math.Round(Point2.Z, 3)))
                {
                    isSamePoint = true;
                }
            }

            return isSamePoint;
        }


        public bool CheckTwoLinesAreEqual(Line line1, Line line2)
        {
            bool isSamePoint = false;

            XYZ line1Sp = line1.GetEndPoint(0);
            XYZ line1Ep = line1.GetEndPoint(1);

            XYZ line2Sp = line2.GetEndPoint(0);
            XYZ line2Ep = line2.GetEndPoint(1);


            if ((Math.Round(line1Sp.X, 3) == Math.Round(line2Sp.X, 3) && Math.Round(line1Sp.Y, 3) == Math.Round(line2Sp.Y, 3) && Math.Round(line1Sp.Z, 3) == Math.Round(line2Sp.Z, 3)) && (Math.Round(line1Ep.X, 3) == Math.Round(line2Ep.X, 3) && Math.Round(line1Ep.Y, 3) == Math.Round(line2Ep.Y, 3) && Math.Round(line1Ep.Z, 3) == Math.Round(line2Ep.Z, 3)))
            {
                isSamePoint = true;
            }
            else if ((Math.Round(line1Sp.X, 3) == Math.Round(line2Ep.X, 3) && Math.Round(line1Sp.Y, 3) == Math.Round(line2Ep.Y, 3) && Math.Round(line1Sp.Z, 3) == Math.Round(line2Ep.Z, 3)) && (Math.Round(line1Ep.X, 3) == Math.Round(line2Sp.X, 3) && Math.Round(line1Ep.Y, 3) == Math.Round(line2Sp.Y, 3) && Math.Round(line1Ep.Z, 3) == Math.Round(line2Sp.Z, 3)))
            {
                isSamePoint = true;
            }

            return isSamePoint;
        }

        private XYZ GetMidPointOfBoundary(List<Curve> lstCurves)
        {
            List<XYZ> lstOverAllPoints = new List<XYZ>();

            lstOverAllPoints.AddRange(lstCurves.Select(x => x.GetEndPoint(0)));

            lstOverAllPoints.AddRange(lstCurves.Select(x => x.GetEndPoint(1)));

            double MinX = lstOverAllPoints.Min(x => x.X);
            double MinY = lstOverAllPoints.Min(x => x.Y);
            double MinZ = lstOverAllPoints.Min(x => x.Z);

            double MaxX = lstOverAllPoints.Max(x => x.X);
            double MaxY = lstOverAllPoints.Max(x => x.Y);
            double MaxZ = lstOverAllPoints.Max(x => x.Z);

            XYZ MinPoint = new XYZ(MinX, MinY, MinZ);
            XYZ MaxPoint = new XYZ(MaxX, MaxY, MaxZ);

            XYZ midPoint = (MinPoint + MaxPoint) / 2;
            return midPoint;
        }

        private void GetBoundaryMinMax(List<Curve> lstCurves, out XYZ minPoint, out XYZ maxPoint, out XYZ midPoint)
        {
            List<XYZ> lstOverAllPoints = new List<XYZ>();

            lstOverAllPoints.AddRange(lstCurves.Select(x => x.GetEndPoint(0)));

            lstOverAllPoints.AddRange(lstCurves.Select(x => x.GetEndPoint(1)));

            double MinX = lstOverAllPoints.Min(x => x.X);
            double MinY = lstOverAllPoints.Min(x => x.Y);
            double MinZ = lstOverAllPoints.Min(x => x.Z);

            double MaxX = lstOverAllPoints.Max(x => x.X);
            double MaxY = lstOverAllPoints.Max(x => x.Y);
            double MaxZ = lstOverAllPoints.Max(x => x.Z);

            minPoint = new XYZ(MinX, MinY, MinZ);
            maxPoint = new XYZ(MaxX, MaxY, MaxZ);
            midPoint = (minPoint + maxPoint) / 2;

        }

        public List<Line> Create2DBoundaryLines(List<Line> lstLines)
        {
            List<Line> lstBoundaryLines = new List<Line>();

            List<XYZ> lstOverAllPoints = new List<XYZ>();

            lstOverAllPoints.AddRange(lstLines.Select(x => x.GetEndPoint(0)));

            lstOverAllPoints.AddRange(lstLines.Select(x => x.GetEndPoint(1)));

            double MinX = lstOverAllPoints.Min(x => x.X);
            double MinY = lstOverAllPoints.Min(x => x.Y);
            double MinZ = lstOverAllPoints.Min(x => x.Z);

            double MaxX = lstOverAllPoints.Max(x => x.X);
            double MaxY = lstOverAllPoints.Max(x => x.Y);
            double MaxZ = lstOverAllPoints.Max(x => x.Z);

            XYZ MinPoint = new XYZ(MinX, MinY, MinZ);
            XYZ MaxPoint = new XYZ(MaxX, MaxY, MaxZ);

            XYZ midPoint = (MinPoint + MaxPoint) / 2;

            XYZ Point1 = new XYZ(MinPoint.X, MinPoint.Y, MinPoint.Z);
            XYZ Point2 = new XYZ(MinPoint.X, MaxPoint.Y, MaxPoint.Z);
            XYZ Point3 = new XYZ(MaxPoint.X, MaxPoint.Y, MaxPoint.Z);
            XYZ Point4 = new XYZ(MaxPoint.X, MinPoint.Y, MinPoint.Z);

            Line line1 = Line.CreateBound(Point1, Point2);
            Line line2 = Line.CreateBound(Point2, Point3);
            Line line3 = Line.CreateBound(Point3, Point4);
            Line line4 = Line.CreateBound(Point4, Point1);

            lstBoundaryLines = new List<Line> { line1, line2, line3, line4 };

            return lstBoundaryLines;
        }

        private List<RebarShapeData> ReadRebarLinePatternInfo(ElementDataInfo rebarElemDataInfo, Element rebarElement, out string elemGeoHashCode, Autodesk.Revit.DB.View CurrentView)
        {
            elemGeoHashCode = string.Empty;

            List<RebarShapeData> lstRebarPatternShape = new List<RebarShapeData>();

            try
            {
                if (rebarElement is Rebar)
                {

                    Rebar rebarElem = rebarElement as Rebar;

                    List<Curve> lstDefaultPatternLines = new List<Curve>();

                    Line distributionPath = default(Line);

                    bool isBarsOnNormal = false;

                    if (!rebarElem.IsRebarShapeDriven() && rebarElem.IsRebarFreeForm())
                    {
                        //RebarShapeData rebarShapeData;

                        //rebarShapeData = CreateFreeformRebarShapeData(rebarElement, rebarElem, isBarsOnNormal, ref elemGeoHashCode);

                        //lstRebarPatternShape.Add(rebarShapeData);

                        return lstRebarPatternShape;
                    }
                    else
                    {
                        lstDefaultPatternLines = rebarElem.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0).ToList();

                        distributionPath = rebarElem.GetShapeDrivenAccessor().GetDistributionPath();

                        XYZ barNormal = rebarElem.GetShapeDrivenAccessor().Normal;

                        isBarsOnNormal = CheckTwoPointsAreEqual(barNormal, rebarElemDataInfo.PlaneNormalDirection) || CheckTwoPointsAreEqual(barNormal, rebarElemDataInfo.PlaneNormalDirection.Negate()) ? true : false;
                    }

                    XYZ intialMidBoundary = default(XYZ);

                    for (int pattenIndex = 0; pattenIndex < rebarElem.NumberOfBarPositions; pattenIndex++)
                    {

                        bool isBarHidden = rebarElem.IsBarHidden(CurrentView, pattenIndex);

                        if (isBarHidden)
                            continue;

                        RebarShapeData rebarShapeData = new RebarShapeData();

                        rebarShapeData.BarsOnNormalView = isBarsOnNormal;

                        Transform currentPatternTransform = rebarElem.GetShapeDrivenAccessor().GetBarPositionTransform(pattenIndex);

                        List<Curve> lstPatternCurves = ApplyTransformFroRealLines(lstDefaultPatternLines, currentPatternTransform);

                        if (lstPatternCurves.Any())
                        {
                            XYZ midPointBoundary = GetMidPointOfBoundary(lstPatternCurves);

                            if (intialMidBoundary == null)
                            {
                                intialMidBoundary = midPointBoundary;
                            }
                            else
                            {
                                Line regardsThrough = Line.CreateBound(intialMidBoundary, midPointBoundary);


                                if (CheckTwoPointsAreEqual(regardsThrough.Direction, rebarElemDataInfo.PlaneNormalDirection) || CheckTwoPointsAreEqual(regardsThrough.Direction.Negate(), rebarElemDataInfo.PlaneNormalDirection))
                                {
                                    continue;
                                }
                            }

                            rebarShapeData.LstPatternCurves = lstPatternCurves;

                            rebarShapeData.DistributionLinePath = distributionPath;

                            rebarShapeData.ElemID = rebarElement.Id;

                            List<string> lstStartPointCode = lstPatternCurves.Select(x => x.GetEndPoint(0).ToString()).ToList();

                            List<string> lstEndPointCode = lstPatternCurves.Select(x => x.GetEndPoint(1).ToString()).ToList();

                            string startValues = String.Concat(lstStartPointCode);
                            string endValues = String.Concat(lstEndPointCode);

                            elemGeoHashCode = String.Concat(elemGeoHashCode, startValues, endValues);

                            lstRebarPatternShape.Add(rebarShapeData);
                        }

                    }
                }
                else if (rebarElement is RebarInSystem)
                {
                    RebarInSystem rebarInSys = rebarElement as RebarInSystem;

                    Line distributionPath = rebarInSys.GetDistributionPath();

                    List<Curve> lstCenterPatternCurves = rebarInSys.GetCenterlineCurves(false, false, false).ToList();

                    bool isBarsOnNormal = rebarInSys.BarsOnNormalSide;

                    for (int pattenIndex = 0; pattenIndex < rebarInSys.NumberOfBarPositions; pattenIndex++)
                    {
                        bool isBarHidden = rebarInSys.IsBarHidden(CurrentView, pattenIndex);

                        if (isBarHidden)
                            continue;

                        RebarShapeData rebarShapeData = new RebarShapeData();

                        rebarShapeData.BarsOnNormalView = isBarsOnNormal;

                        Transform currentPatternTransform = rebarInSys.GetBarPositionTransform(pattenIndex);

                        List<Curve> lstPatternCurves = ApplyTransformFroRealLines(lstCenterPatternCurves, currentPatternTransform);

                        if (lstPatternCurves.Any())
                        {
                            rebarShapeData.LstPatternCurves = lstPatternCurves;

                            rebarShapeData.DistributionLinePath = distributionPath;

                            rebarShapeData.ElemID = rebarElement.Id;

                            List<string> lstStartPointCode = lstPatternCurves.Select(x => x.GetEndPoint(0).ToString()).ToList();

                            List<string> lstEndPointCode = lstPatternCurves.Select(x => x.GetEndPoint(1).ToString()).ToList();

                            string startValues = String.Concat(lstStartPointCode);
                            string endValues = String.Concat(lstEndPointCode);

                            elemGeoHashCode = String.Concat(elemGeoHashCode, startValues, endValues);

                            lstRebarPatternShape.Add(rebarShapeData);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
            }
            return lstRebarPatternShape;
        }

        private RebarShapeData CreateFreeformRebarShapeData(Element rebarElement, Rebar rebarElem, bool isBarsOnNormal, ref string elemGeoHashCode)
        {
            RebarShapeData rebarShapeData = default(RebarShapeData);

            Line distributionPath = default(Line);

            List<Curve> lstPatternCurves = rebarElem.GetFreeFormAccessor().GetCustomDistributionPath().ToList();

            distributionPath = rebarElem.GetFreeFormAccessor().GetCustomDistributionPath().ToList().FirstOrDefault() as Line;


            rebarShapeData = new RebarShapeData();
            rebarShapeData.BarsOnNormalView = isBarsOnNormal;

            rebarShapeData.LstPatternCurves = lstPatternCurves;

            rebarShapeData.DistributionLinePath = distributionPath;

            rebarShapeData.ElemID = rebarElement.Id;

            List<string> lstStartPointCode = lstPatternCurves.Select(x => x.GetEndPoint(0).ToString()).ToList();

            List<string> lstEndPointCode = lstPatternCurves.Select(x => x.GetEndPoint(1).ToString()).ToList();

            string startValues = String.Concat(lstStartPointCode);
            string endValues = String.Concat(lstEndPointCode);

            elemGeoHashCode = String.Concat(elemGeoHashCode, startValues, endValues);

            return rebarShapeData;
        }

        public bool ChangeHookParameter(ElementDataInfo elemInfo, bool makeNone)
        {
            Transaction trans = new Transaction(doc, "setParam");

            bool isChanged = false;

            try
            {
                trans.Start();

                ElementId elemId = new ElementId(elemInfo.ElemID);

                Element elem = doc.GetElement(elemId);

                if (elem != null)
                {
                    Parameter hookAtStartParam = elem.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_START_TYPE);

                    Parameter hookAtEndParam = elem.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_END_TYPE);

                    if (!makeNone)
                    {
                        ElementId previousHookStartId = elemInfo.HookStartId;
                        ElementId previousHookEndId = elemInfo.HookEndId;

                        hookAtStartParam.Set(previousHookStartId);
                        hookAtEndParam.Set(previousHookEndId);
                    }
                    else
                    {
                        ElementId hookStartElemID = hookAtStartParam.AsElementId();

                        ElementId hookEndElemID = hookAtEndParam.AsElementId();

                        if (hookStartElemID != null && hookStartElemID != ElementId.InvalidElementId)
                        {
                            ElementId noneElemID = ElementId.InvalidElementId;

                            hookAtStartParam.Set(noneElemID);
                        }

                        if (hookEndElemID != null && hookEndElemID != ElementId.InvalidElementId)
                        {
                            ElementId noneElemID = ElementId.InvalidElementId;

                            hookAtEndParam.Set(noneElemID);
                        }

                        elemInfo.HookStartId = hookStartElemID;
                        elemInfo.HookEndId = hookEndElemID;
                    }
                }

                trans.Commit();
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                {
                    trans.RollBack();
                }

            }

            return isChanged;
        }

        private List<Curve> ApplyTransformFroRealLines(List<Curve> lstDefaultPatternLines, Transform currentPatternTransform)
        {

            List<Curve> lstRealLines = new List<Curve>();

            foreach (Curve centerCurve in lstDefaultPatternLines)
            {
                Curve transCurve = centerCurve.CreateTransformed(currentPatternTransform);

                lstRealLines.Add(transCurve);
            }


            return lstRealLines;
        }

        private AnnotationFamily GetDecisionMakeFamForCornerPoint(ElementDataInfo rebarElemDataInfo, Line maxLine, XYZ cornerPoint)
        {
            AnnotationFamily annotateFam = AnnotationFamily.Line;

            try
            {
                Line extendedMaxLine = ExtendLineFromMidPoint(maxLine);

                Line extendedTowardsLine = ExtendLineFromSourcePoint(cornerPoint, rebarElemDataInfo.PlaneNormalDirection);
                SetComparisonResult compareResTowardsPlane = extendedMaxLine.Intersect(extendedTowardsLine);

                Line againstToPlaneLine = ExtendLineFromSourcePoint(cornerPoint, rebarElemDataInfo.PlaneNormalDirection.Negate());
                Line extendedAgainstLine = ExtendLineFromSourcePoint(againstToPlaneLine.GetEndPoint(0), againstToPlaneLine.Direction);
                SetComparisonResult compareResAgainstPlane = extendedMaxLine.Intersect(extendedAgainstLine);

                List<Curve> lstCurves = new List<Curve> { extendedAgainstLine, extendedTowardsLine, extendedMaxLine };
                //CreateTemLineWithCurves(lstCurves);

                if (compareResTowardsPlane == SetComparisonResult.Overlap && compareResAgainstPlane == SetComparisonResult.Disjoint)
                {
                    annotateFam = AnnotationFamily.Cross;//Cross
                }
                else if (compareResTowardsPlane == SetComparisonResult.Disjoint && compareResAgainstPlane == SetComparisonResult.Overlap)
                {
                    annotateFam = AnnotationFamily.Circle;//Circle

                }
                else if (compareResTowardsPlane == SetComparisonResult.Disjoint && compareResAgainstPlane == SetComparisonResult.Disjoint)
                {
                    annotateFam = AnnotationFamily.Line;//Line
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
            }

            return annotateFam;
        }

        private Line ExtendLineFromMidPoint(Line maxLine)
        {
            XYZ midPoint = (maxLine.GetEndPoint(0) + maxLine.GetEndPoint(1)) / 2;

            XYZ normalDir = maxLine.Direction;
            XYZ negateDir = maxLine.Direction.Negate();

            normalDir = normalDir.Multiply(100);
            negateDir = negateDir.Multiply(100);

            XYZ conStartPoint = midPoint.Add(normalDir);
            XYZ conEndPoint = midPoint.Add(negateDir);

            Line newMaxLine2 = Line.CreateBound(conStartPoint, conEndPoint);

            return newMaxLine2;
        }

        private Line ExtendLineFromMidPoint(XYZ midPoint, XYZ direction)
        {
            XYZ normalDir = direction;
            XYZ negateDir = direction.Negate();

            normalDir = normalDir.Multiply(100);
            negateDir = negateDir.Multiply(100);

            XYZ conStartPoint = midPoint.Add(normalDir);
            XYZ conEndPoint = midPoint.Add(negateDir);

            Line newMaxLine2 = Line.CreateBound(conStartPoint, conEndPoint);

            return newMaxLine2;
        }

        private Line ExtendLineFromSourcePoint(XYZ sourcePoint, XYZ direction)
        {
            XYZ distanceDirection = direction.Multiply(100);

            XYZ directionEndPoint = sourcePoint.Add(distanceDirection);

            Line newMaxLine2 = Line.CreateBound(sourcePoint, directionEndPoint);

            return newMaxLine2;
        }



        public bool PlaceAnnotationWithRespectiveFamily()
        {
            bool isPlacedInDoc = false;

            LstTotalFamInstacne = new List<FamilyInstance>();

            Transaction trans = new Transaction(doc, "PlaceAnnotate");

            try
            {
                if (RebarSettingsDataInfo != null)
                {
                    AddUserFamilyInCustomStore(RebarSettingsDataInfo);
                }

                List<FamilySymbol> lstFamSymbol = new List<FamilySymbol>();

                lstFamSymbol = GetAnnotationFamSymFromDoc();

                if (!lstFamSymbol.Any() || lstFamSymbol.Count != lstAnnotateName.Count)
                {
                    LoadFamilyFromAssemblyPath();

                    if (RebarSettingsDataInfo != null)
                    {
                        LoadFamilyFromUserPath(RebarSettingsDataInfo);
                    }

                    lstFamSymbol = GetAnnotationFamSymFromDoc();

                    if (!lstFamSymbol.Any())
                    {
                        return false;
                    }
                }

                LstCornerPointInfo = new List<CornerPointInfo>();

                List<CornerPointInfo> lstStartCornerPointInfo = LstSelectedElementDataInfo.Where(x => x.LstRebarPatternShapeData != null).SelectMany(x => x.LstRebarPatternShapeData).Where(x => x.LstCurveDataInfo != null).SelectMany(y => y.LstCurveDataInfo).Where(z => z.StartPointInfo != null && z.StartPointInfo.IsAnnotateFamSet).Select(z => z.StartPointInfo).ToList();

                List<CornerPointInfo> lstEndCornerPointInfo = LstSelectedElementDataInfo.Where(x => x.LstRebarPatternShapeData != null).SelectMany(x => x.LstRebarPatternShapeData).Where(x => x.LstCurveDataInfo != null).SelectMany(y => y.LstCurveDataInfo).Where(z => z.EndPointInfo != null && z.EndPointInfo.IsAnnotateFamSet).Select(z => z.EndPointInfo).ToList();


                LstCornerPointInfo.AddRange(lstStartCornerPointInfo);
                LstCornerPointInfo.AddRange(lstEndCornerPointInfo);


                var lstGroupPointInfo = LstCornerPointInfo.Where(x => x.AnnotateFam != AnnotationFamily.None).Select(x => x).GroupBy(x => x.AnnotateFam).Select(x => x).ToList();

                trans.Start();

                foreach (var cornerGroupInfo in lstGroupPointInfo)
                {
                    //var groupCornerPointInfo = cornerGroupInfo.ToList().GroupBy(x => x.PlaneParalelDirection).Select(x => x).ToList();

                    List<FamilyInstance> lstFamInsatce = new List<FamilyInstance>();

                    List<CornerPointInfo> lstCornerPoints = cornerGroupInfo.Select(x => x).ToList();

                    FamilySymbol famSym = default(FamilySymbol);

                    famSym = GetRespectiveAnnotateFamily(RebarSettingsDataInfo, lstFamSymbol, cornerGroupInfo.Key);

                    if (famSym != null)
                    {
                        famSym.Activate();

                        lstFamInsatce = PlaceAnnotationInDocument(lstCornerPoints, famSym, cornerGroupInfo.Key);
                    }

                    if (lstFamInsatce.Any())
                    {
                        LstTotalFamInstacne.AddRange(lstFamInsatce);
                    }

                }

                CreateDataStorageForElement(LstCornerPointInfo);

                if (LstTotalFamInstacne.Any())
                {
                    isPlacedInDoc = true;
                }

                doc.Regenerate();

                trans.Commit();
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                isPlacedInDoc = false;
            }



            return isPlacedInDoc;
        }

        private void AddUserFamilyInCustomStore(RebarSettingsData rebarSettingsData)
        {
            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarEndPath) && File.Exists(rebarSettingsData.RebarEndPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarEndPath);

                lstAnnotateName.Add(fileName);
            }

            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarInPlaneBendPath) && File.Exists(rebarSettingsData.RebarInPlaneBendPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarInPlaneBendPath);

                lstAnnotateName.Add(fileName);
            }

            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarOutPlaneBendPath) && File.Exists(rebarSettingsData.RebarOutPlaneBendPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarOutPlaneBendPath);

                lstAnnotateName.Add(fileName);
            }

            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarInclinedPath) && File.Exists(rebarSettingsData.RebarInclinedPath))
            {
                string fileName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarInclinedPath);

                lstAnnotateName.Add(fileName);
            }
        }

        private void LoadFamilyFromUserPath(RebarSettingsData rebarSettingsData)
        {
            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarEndPath) && File.Exists(rebarSettingsData.RebarEndPath))
            {
                Family fam = default(Family);

                bool IsLoaded = IsFamilyLoaded(doc, rebarSettingsData.RebarEndPath, out fam);
            }

            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarInPlaneBendPath) && File.Exists(rebarSettingsData.RebarInPlaneBendPath))
            {
                Family fam = default(Family);

                bool IsLoaded = IsFamilyLoaded(doc, rebarSettingsData.RebarInPlaneBendPath, out fam);
            }

            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarOutPlaneBendPath) && File.Exists(rebarSettingsData.RebarOutPlaneBendPath))
            {
                Family fam = default(Family);

                bool IsLoaded = IsFamilyLoaded(doc, rebarSettingsData.RebarOutPlaneBendPath, out fam);
            }

            if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarInclinedPath) && File.Exists(rebarSettingsData.RebarInclinedPath))
            {
                Family fam = default(Family);

                bool IsLoaded = IsFamilyLoaded(doc, rebarSettingsData.RebarInclinedPath, out fam);
            }
        }

        private void CreateDataStorageForElement(List<CornerPointInfo> lstCornerPointInfo)
        {
            try
            {
                var groupViewCollection = lstCornerPointInfo.GroupBy(x => x.CurrentViewId).ToList();

                RemoveEmptyDataFromStorage();

                foreach (var viewGroupInfo in groupViewCollection)
                {
                    ElementId currentViewID = viewGroupInfo.Key;

                    List<CornerPointInfo> lstElemCornerPoint = viewGroupInfo.ToList();

                    var groupElemCornerPoint = lstElemCornerPoint.GroupBy(x => x.ElemID);

                    foreach (var elemCornerGroupInfo in groupElemCornerPoint)
                    {
                        ElementId elemId = elemCornerGroupInfo.Key;

                        ElementDataInfo rebarElemDataInfo = LstSelectedElementDataInfo.Where(x => x.ElemID == elemId.IntegerValue && x.CurrentViewId == currentViewID).Select(x => x).FirstOrDefault();

                        Autodesk.Revit.DB.View CurrentView = rebarElemDataInfo != null && rebarElemDataInfo.CurrentView != null ? rebarElemDataInfo.CurrentView : doc.ActiveView;


                        bool isFlipped = elemCornerGroupInfo.First().IsFlipElement;
                        string HashCodeData = elemCornerGroupInfo.First().HashCodeData;

                        List<int> lstFamInsID = elemCornerGroupInfo.ToList().Where(x => x.FamInsId != null).Select(x => x.FamInsId.IntegerValue).ToList();

                        StorageHelper existStorage = LstExistDataStorage.Where(x => x.ElemId == elemId.IntegerValue).Select(x => x).FirstOrDefault();

                        if (existStorage != null)
                        {
                            existStorage.HasCodeData = HashCodeData;

                            int viewId = CurrentView.Id.IntegerValue;

                            ViewBasedData existsViewBasedData = existStorage.LstViewBasedDataInfo.Where(x => x.ViewId == viewId).Select(x => x).FirstOrDefault();

                            if (existsViewBasedData != null)
                            {
                                ViewBasedData currentViewBasedData = existsViewBasedData;

                                currentViewBasedData.IsFlipped = isFlipped ? true : false;

                                currentViewBasedData.lstFamInsId = lstFamInsID;

                                currentViewBasedData.ViewId = viewId;

                                existStorage.LstViewBasedDataInfo.RemoveAll(x => x.ViewId == viewId);

                                existStorage.LstViewBasedDataInfo.Add(currentViewBasedData);

                            }
                            else
                            {
                                ViewBasedData currentViewBasedData = new ViewBasedData();

                                currentViewBasedData.IsFlipped = isFlipped ? true : false;

                                currentViewBasedData.lstFamInsId = lstFamInsID;

                                currentViewBasedData.ViewId = viewId;

                                existStorage.LstViewBasedDataInfo.Add(currentViewBasedData);
                            }
                        }
                        else
                        {
                            StorageHelper newstorageHelper = new StorageHelper();

                            newstorageHelper.ElemId = elemId.IntegerValue;

                            newstorageHelper.HasCodeData = HashCodeData;

                            ViewBasedData currentViewBasedData = new ViewBasedData();

                            currentViewBasedData.IsFlipped = isFlipped ? true : false;

                            currentViewBasedData.lstFamInsId = lstFamInsID;

                            int viewId = CurrentView.Id.IntegerValue;

                            currentViewBasedData.ViewId = viewId;

                            newstorageHelper.LstViewBasedDataInfo = new List<ViewBasedData>();

                            newstorageHelper.LstViewBasedDataInfo.Add(currentViewBasedData);

                            LstExistDataStorage.Add(newstorageHelper);
                        }
                    }
                }



                string dataToStore = CommonData.SerializeObject(LstExistDataStorage);

                CommonData.WriteSettingInStorageDocument(dataToStore, ConstantValues.RebarFieldName, ConstantValues.RebarSchemaName, ConstantValues.RebarSchemaGuid, doc);
            }
            catch (Exception ex)
            {
            }
        }

        private void RemoveEmptyDataFromStorage()
        {
            List<StorageHelper> lstRemovableStorage = new List<StorageHelper>();

            foreach (StorageHelper existingData in LstExistDataStorage)
            {
                Element rebarElem = GetElement(existingData.ElemId);

                if (rebarElem == null)
                {
                    lstRemovableStorage.Add(existingData);

                    continue;
                }

                List<ViewBasedData> lstRemovableViewBasedData = new List<ViewBasedData>();

                foreach (ViewBasedData viewData in existingData.LstViewBasedDataInfo)
                {
                    if (!viewData.lstFamInsId.Any())
                    {
                        lstRemovableViewBasedData.Add(viewData);
                    }
                    else
                    {
                        List<int> lstRemovableElemId = new List<int>();

                        foreach (var elemId in viewData.lstFamInsId)
                        {
                            Element elem = GetElement(elemId);

                            if (elem == null)
                            {
                                lstRemovableElemId.Add(elemId);
                            }
                        }

                        viewData.lstFamInsId.RemoveAll(x => lstRemovableElemId.Any(y => y == x));

                        if (!viewData.lstFamInsId.Any())
                        {
                            lstRemovableViewBasedData.Add(viewData);
                        }
                    }
                }

                existingData.LstViewBasedDataInfo.RemoveAll(x => lstRemovableViewBasedData.Any(y => y == x));

                if (!existingData.LstViewBasedDataInfo.Any())
                {
                    lstRemovableStorage.Add(existingData);
                }
            }

            LstExistDataStorage.RemoveAll(x => lstRemovableStorage.Any(y => y == x));
        }

        public void ChangeLinePatternForElements(bool _toChangeFamOnly = false)
        {
            if (!_toChangeFamOnly)
            {
                List<LinePatternInfo> lstLinePatternInfo = ReadLinePatternElements();

                List<ElementDataInfo> lstBottomElementInfo = LstSelectedElementDataInfo.Where(x => x.IsFlipped).Select(x => x).ToList();

                List<ElementDataInfo> lstTopElementInfo = LstSelectedElementDataInfo.Where(x => !x.IsFlipped).Select(x => x).ToList();


                LinePatternInfo BottomLnePatternInfo = default(LinePatternInfo);
                LineWeightInfo BottomLneWeightInfo = default(LineWeightInfo);
                LineColorInfo BottomLineColor = default(LineColorInfo);

                LinePatternInfo TopLnePatternInfo = default(LinePatternInfo);
                LineWeightInfo TopLneWeightInfo = default(LineWeightInfo);
                LineColorInfo TopLineColor = default(LineColorInfo);

                if (lstBottomElementInfo.Any())
                {
                    if (RebarSettingsDataInfo != null && RebarSettingsDataInfo.BottomRebarData != null)
                    {
                        if (RebarSettingsDataInfo.BottomRebarData.LinePatternDataInfo != null)
                        {
                            BottomLnePatternInfo = RebarSettingsDataInfo.BottomRebarData.LinePatternDataInfo;
                        }

                        if (RebarSettingsDataInfo.BottomRebarData.LineWeightDataInfo != null)
                        {
                            BottomLneWeightInfo = RebarSettingsDataInfo.BottomRebarData.LineWeightDataInfo;
                        }

                        if (RebarSettingsDataInfo.BottomRebarData.LineColorDataInfo != null)
                        {
                            BottomLineColor = RebarSettingsDataInfo.BottomRebarData.LineColorDataInfo;
                        }
                    }
                    else
                    {
                        BottomLnePatternInfo = lstLinePatternInfo.Where(x => x.Name.ToLower() == ConstantValues.DashPatternName.ToLower()).Select(x => x).FirstOrDefault();
                    }

                    CreateLinePatternForElem(lstBottomElementInfo, BottomLnePatternInfo, BottomLneWeightInfo, BottomLineColor);
                }

                if (lstTopElementInfo.Any())
                {
                    if (RebarSettingsDataInfo != null && RebarSettingsDataInfo.TopRebarData != null)
                    {
                        if (RebarSettingsDataInfo.TopRebarData.LinePatternDataInfo != null)
                        {
                            TopLnePatternInfo = RebarSettingsDataInfo.TopRebarData.LinePatternDataInfo;
                        }

                        if (RebarSettingsDataInfo.TopRebarData.LineWeightDataInfo != null)
                        {
                            TopLneWeightInfo = RebarSettingsDataInfo.TopRebarData.LineWeightDataInfo;
                        }

                        if (RebarSettingsDataInfo.TopRebarData.LineColorDataInfo != null)
                        {
                            TopLineColor = RebarSettingsDataInfo.TopRebarData.LineColorDataInfo;
                        }
                    }
                    else
                    {
                        TopLnePatternInfo = lstLinePatternInfo.Where(x => x.Name.ToLower() == ConstantValues.SolidPatternName.ToLower()).Select(x => x).FirstOrDefault(); ;
                    }



                    CreateLinePatternForElem(lstTopElementInfo, TopLnePatternInfo, TopLneWeightInfo, TopLineColor);
                }
            }


            if (RebarSettingsDataInfo != null && RebarSettingsDataInfo.FamRebarData != null && LstTotalFamInstacne != null && LstTotalFamInstacne.Any())
            {
                CreateLinePatternForElem(RebarSettingsDataInfo.FamRebarData);
            }
        }

        private void CreateLinePatternForElem(List<ElementDataInfo> lstFlipElementInfo, LinePatternInfo flipLnePattern, LineWeightInfo lineWeight, LineColorInfo colorInfo)
        {
            Transaction trans = new Transaction(doc, "ChangeLine");

            trans.Start();

            try
            {
                foreach (ElementDataInfo elemDataInfo in lstFlipElementInfo)
                {
                    Autodesk.Revit.DB.View CurrentView = elemDataInfo.CurrentView == null ? doc.ActiveView : elemDataInfo.CurrentView;

                    ElementId elemId = new ElementId(elemDataInfo.ElemID);

                    ElementId LineElemId = ElementId.InvalidElementId;

                    if (flipLnePattern != null && flipLnePattern.Name != "None")
                    {
                        LineElemId = new ElementId(flipLnePattern.Id);
                    }

                    OverrideGraphicSettings overrideGS = new OverrideGraphicSettings();

                    if (LineElemId != ElementId.InvalidElementId)
                    {
                        overrideGS.SetProjectionLinePatternId(LineElemId);
                    }

                    if (lineWeight != null && lineWeight.WeightNumberInString != "None")
                        overrideGS.SetProjectionLineWeight(lineWeight.WeightNumber);

                    if (colorInfo != null && (colorInfo.ColorName != "#00000000"))
                        overrideGS.SetProjectionLineColor(new Color(colorInfo.RValue, colorInfo.GValue, colorInfo.BValue));

                    CurrentView.SetElementOverrides(elemId, overrideGS);

                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                {
                    trans.RollBack();
                }
            }

            trans.Commit();
        }

        private void CreateLinePatternForElem(FamilyRebarInfo rebarInfo)
        {
            Transaction trans = new Transaction(doc);

            trans.Start("ChangeLine");

            try
            {
                foreach (Element element in LstTotalFamInstacne)
                {
                    CornerPointInfo cornerPointInfo = LstCornerPointInfo.Where(x => x.FamInsId == element.Id).Select(x => x).FirstOrDefault();

                    if (cornerPointInfo == null)
                    {
                        continue;
                    }

                    Autodesk.Revit.DB.View CurrentView = LstSelectedElementDataInfo.Where(x => x.ElemID == cornerPointInfo.ElemID.IntegerValue).Select(x => x.CurrentView).FirstOrDefault();

                    CurrentView = CurrentView == null ? doc.ActiveView : CurrentView;

                    ElementId elemId = element.Id;

                    OverrideGraphicSettings overrideGS = new OverrideGraphicSettings();


                    if (rebarInfo.LinePatternDataInfo != null && rebarInfo.LinePatternDataInfo.Name != "None")
                    {
                        ElementId LineElemId = new ElementId(rebarInfo.LinePatternDataInfo.Id);

                        overrideGS.SetProjectionLinePatternId(LineElemId);
                    }

                    if (rebarInfo.LineWeightDataInfo.WeightNumberInString != "None")
                        overrideGS.SetProjectionLineWeight(rebarInfo.LineWeightDataInfo.WeightNumber);

                    if (rebarInfo.LineColorDataInfo != null && (rebarInfo.LineColorDataInfo.ColorName != "#00000000"))
                    {
                        overrideGS.SetProjectionLineColor(new Color(rebarInfo.LineColorDataInfo.RValue, rebarInfo.LineColorDataInfo.GValue, rebarInfo.LineColorDataInfo.BValue));
                    }

                    CurrentView.SetElementOverrides(elemId, overrideGS);
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                {
                    trans.RollBack();
                }
            }

            trans.Commit();
        }


        private List<FamilySymbol> GetAnnotationFamSymFromDoc()
        {
            List<FamilySymbol> lstFamSymbol;
            List<Element> annotationFromDocument = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_GenericAnnotation).ToList();

            lstFamSymbol = GetFamilySymbolWithName(annotationFromDocument);
            return lstFamSymbol;
        }

        private void LoadFamilyFromAssemblyPath()
        {
            string famdir = Path.GetDirectoryName(Assembly.LoadFrom(Assembly.GetExecutingAssembly().CodeBase).Location);
            famdir = Path.Combine(famdir, ConstantValues.FamilyFolderName);

            var famDirecInfo = Directory.CreateDirectory(famdir);

            if (famDirecInfo.Exists)
            {
                var lstFamFiles = Directory.GetFiles(famdir, "*.rfa");

                foreach (string famFile in lstFamFiles)
                {
                    string famName = Path.GetFileNameWithoutExtension(famFile);

                    Family fam = default(Family);

                    if (lstAnnotateName.Any(x => x.ToLower().Equals(famName.ToLower())))
                    {
                        bool IsLoaded = IsFamilyLoaded(doc, famFile, out fam);
                    }
                }
            }
        }

        private FamilySymbol GetRespectiveAnnotateFamily(RebarSettingsData rebarSettingsData, List<FamilySymbol> lstFamSymbol, AnnotationFamily annotateFamKey)
        {
            FamilySymbol famSym = default(FamilySymbol);

            if (annotateFamKey == AnnotationFamily.Cross)
            {
                if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarInclinedPath) && File.Exists(rebarSettingsData.RebarInclinedPath))
                {
                    string famName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarInclinedPath);

                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == famName.ToLower()).Select(x => x).FirstOrDefault();
                }

                if (famSym == null)
                {
                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == AnnotationFamily.Cross.ToString().ToLower()).Select(x => x).FirstOrDefault();
                }
            }
            else if (annotateFamKey == AnnotationFamily.Circle)
            {
                if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarOutPlaneBendPath) && File.Exists(rebarSettingsData.RebarOutPlaneBendPath))
                {
                    string famName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarOutPlaneBendPath);

                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == famName.ToLower()).Select(x => x).FirstOrDefault();
                }

                if (famSym == null)
                {
                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == AnnotationFamily.Circle.ToString().ToLower()).Select(x => x).FirstOrDefault();
                }

            }
            else if (annotateFamKey == AnnotationFamily.Centre)
            {
                if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarInclinedPath) && File.Exists(rebarSettingsData.RebarInclinedPath))
                {
                    string famName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarInclinedPath);

                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == famName.ToLower()).Select(x => x).FirstOrDefault();
                }

                if (famSym == null)
                {
                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == AnnotationFamily.Centre.ToString().ToLower()).Select(x => x).FirstOrDefault();
                }

            }
            else
            {
                if (rebarSettingsData != null && !string.IsNullOrEmpty(rebarSettingsData.RebarEndPath) && File.Exists(rebarSettingsData.RebarEndPath))
                {
                    string famName = Path.GetFileNameWithoutExtension(rebarSettingsData.RebarEndPath);

                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == famName.ToLower()).Select(x => x).FirstOrDefault();
                }

                if (famSym == null)
                {
                    famSym = lstFamSymbol.Where(x => x.FamilyName.ToLower() == AnnotationFamily.Line.ToString().ToLower()).Select(x => x).FirstOrDefault();
                }

            }

            return famSym;
        }

        private List<FamilyInstance> PlaceAnnotationInDocument(List<CornerPointInfo> lstCornerPoints, FamilySymbol famSym, AnnotationFamily annotateKey)
        {
            int existsViewID = doc.ActiveView.Id.IntegerValue;


            List<FamilyInstance> lstFamilyInstace = new List<FamilyInstance>();

            try
            {
                var lstDistinctCornerPoints = lstCornerPoints.Distinct(new ItemEqualityComparer()).ToList();

                var groupByViewBased = lstDistinctCornerPoints.GroupBy(x => x.CurrentViewId).ToList();

                foreach (var groupViewCornerPointInfo in groupByViewBased)
                {
                    ElementId currentViewBasedID = groupViewCornerPointInfo.Key;


                    var groupByElemId = groupViewCornerPointInfo.ToList().GroupBy(c => c.ElemID).ToList();

                    foreach (var groupElemCornerPointInfo in groupByElemId)
                    {
                        ElementId currentElemID = groupElemCornerPointInfo.Key;

                        List<CornerPointInfo> lstViewBasedCornerInfo = groupElemCornerPointInfo.ToList();

                        if (!lstViewBasedCornerInfo.Any())
                            continue;

                        LstReverseLineInfo = new Dictionary<ElementId, List<Line>>();

                        ElementDataInfo elemDataInfo = LstSelectedElementDataInfo.Where(x => x.ElemID == currentElemID.IntegerValue && x.CurrentViewId == currentViewBasedID).Select(x => x).FirstOrDefault();

                        if (elemDataInfo == null)
                            continue;

                        XYZ planeNormalDirection = elemDataInfo.PlaneNormalDirection;

                        Autodesk.Revit.DB.View CurrentView = elemDataInfo.CurrentView;


                        CurrentView = CurrentView == null ? doc.ActiveView : CurrentView;

                        //if (CurrentView.Id.IntegerValue != existsViewID)
                        //    ChangeActiveView(CurrentView.Id.IntegerValue);


                        foreach (CornerPointInfo cornerPoint in lstViewBasedCornerInfo)
                        {
                            if (cornerPoint.IsHookType)
                                continue;

                            Outline cropOutLine = default(Outline);

                            if (CurrentView.CropBoxActive)
                            {
                                cropOutLine = GetCropOutLine(CurrentView);
                            }


                            bool isFlip = cornerPoint.IsFlipElement;

                            XYZ placePoint = cornerPoint.CornerPoint;
                            XYZ boundaryMidPoint = cornerPoint.BoundaryMidPoint;

                            BoundingBoxXYZ elemBbox = cornerPoint.ElemBoundingBox;

                            FamilyInstance famIns = default(FamilyInstance);

                            bool isSketchViewEditInActive = IsInSketchViewEditable(cornerPoint);

                            if (cropOutLine != null)
                            {
                                bool isContainsInCropView = IsInCropViewRegion(placePoint, CurrentView, cropOutLine);

                                if (!isContainsInCropView)
                                    continue;
                            }

                            if (annotateKey == AnnotationFamily.Line)
                            {
                                famIns = CreateFamForLine(planeNormalDirection, famSym, cornerPoint, CurrentView, isSketchViewEditInActive);
                            }
                            else if (annotateKey == AnnotationFamily.Centre && !isSketchViewEditInActive)
                            {
                                famIns = CreateFamForCenter(planeNormalDirection, famSym, cornerPoint, CurrentView, placePoint);
                            }
                            else if (annotateKey == AnnotationFamily.Cross)
                            {
                                famIns = CreateFamForCross(planeNormalDirection, famSym, cornerPoint, CurrentView, placePoint);
                            }
                            else if (annotateKey == AnnotationFamily.Circle)
                            {
                                famIns = doc.Create.NewFamilyInstance(placePoint, famSym, CurrentView);
                            }

                            if (famIns != null)
                            {
                                cornerPoint.FamInsId = famIns.Id;

                                lstFamilyInstace.Add(famIns);
                            }
                        }


                    }
                }



                //if (doc.ActiveView.Id.IntegerValue != existsViewID)
                //    ChangeActiveView(existsViewID);
            }
            catch (Exception ex)
            {

            }



            return lstFamilyInstace;
        }

        private void ChangeActiveView(int CurrentViewId)
        {
            Element currentView = GetElement(CurrentViewId);

            if (currentView != null)
            {
                uiDoc.ActiveView = currentView as Autodesk.Revit.DB.View;
            }
        }

        private FamilyInstance CreateFamForCross(XYZ planeNormalDirection, FamilySymbol famSym, CornerPointInfo cornerPoint, Autodesk.Revit.DB.View CurrentView, XYZ placePoint)
        {
            FamilyInstance famIns;
            Line refLine = cornerPoint.RefLine;

            if (CheckTwoPointsAreEqual(refLine.Direction, planeNormalDirection) || CheckTwoPointsAreEqual(refLine.Direction.Negate(), planeNormalDirection))
            {
                List<CurveDataInfo> lstCurveDataInfo = cornerPoint.GetSharedCurveInfo();


                if (lstCurveDataInfo.Any())
                {
                    CurveDataInfo curveData = lstCurveDataInfo.Where(x => x.RVTCurve is Line).Select(x => x).Where(x => !CheckTwoPointsAreEqual((x.RVTCurve as Line).Direction, refLine.Direction) && !CheckTwoPointsAreEqual((x.RVTCurve as Line).Direction.Negate(), refLine.Direction)).Select(x => x).FirstOrDefault();

                    if (curveData != null)
                    {
                        refLine = curveData.RVTCurve as Line;
                    }
                }
                else
                {
                    refLine = null;
                }
            }

            famIns = doc.Create.NewFamilyInstance(placePoint, famSym, CurrentView);

            if (refLine != null && !CheckTwoPointsAreEqual(refLine.Direction, planeNormalDirection) && !CheckTwoPointsAreEqual(refLine.Direction.Negate(), planeNormalDirection))
            {
                XYZ extendedPlane = planeNormalDirection.Multiply(100);

                XYZ extednedPoint = extendedPlane.Add(placePoint);

                Line axis = Line.CreateBound(placePoint, extednedPoint);

                XYZ consolPlacePoint = default(XYZ);

                XYZ rightDirection = CurrentView.RightDirection.Normalize();

                XYZ upDirection = CurrentView.UpDirection.Normalize();

                XYZ downDirection = upDirection.Negate();

                Line consolLine = GetActiveViewBaseLine(refLine, CurrentView, placePoint, out consolPlacePoint, true);

                XYZ consolSP = consolLine.GetEndPoint(0);
                XYZ consolEP = consolLine.GetEndPoint(1);

                XYZ midPoint = consolLine.Evaluate(0.5, true);

                Line upLine = GetLineBasedOnPlane(false, consolPlacePoint, upDirection);


                Line downLine = GetLineBasedOnPlane(false, consolPlacePoint, downDirection);

                Line rightLine = GetLineBasedOnPlane(false, consolPlacePoint, rightDirection);

                double angleDown = CheckTwoPointsAreEqual(consolLine.Direction, downDirection) || CheckTwoPointsAreEqual(consolLine.Direction, downDirection.Negate()) ? 0 : GetAngleOfTwoLines(consolLine, downLine);

                double angleRight = CheckTwoPointsAreEqual(consolLine.Direction, rightDirection) || CheckTwoPointsAreEqual(consolLine.Direction, rightDirection.Negate()) ? 0 : GetAngleOfTwoLines(consolLine, rightLine);

                Line perpConsolLine = CreatePerpendicularLine(consolLine, midPoint, planeNormalDirection);

                double consolRightAngle = GetAngleOfTwoLines(consolLine, rightLine);
                double consolUpWardAngle = GetAngleOfTwoLines(consolLine, upLine);

                // CreateTemLineWithCurves(new List<Curve> { perpConsolLine, rightLine });

                if (!(angleDown != 0 && angleRight == 0) && !(angleRight != 0 && angleDown == 0))
                {

                    double actualAngle = consolRightAngle;

                    double deltRightAng = (180 / Math.PI) * consolRightAngle;

                    double deltUpAng = (180 / Math.PI) * consolUpWardAngle;

                    deltRightAng = Math.Round(deltRightAng, 2);
                    deltUpAng = Math.Round(deltUpAng, 2);

                    if ((deltUpAng <= 90 && deltRightAng >= 90) || (deltRightAng + deltUpAng <= 90))
                    {

                    }
                    else
                    {
                        actualAngle = -actualAngle;
                    }


                    ElementTransformUtils.RotateElement(doc, famIns.Id, axis, actualAngle);
                }
            }

            return famIns;
        }

        private FamilyInstance CreateFamForCenter(XYZ planeNormalDirection, FamilySymbol famSym, CornerPointInfo cornerPoint, Autodesk.Revit.DB.View CurrentView, XYZ placePoint)
        {
            FamilyInstance famIns;
            XYZ extendedPlane = planeNormalDirection.Multiply(100);

            XYZ extednedPoint = extendedPlane.Add(placePoint);

            Line axis = Line.CreateBound(placePoint, extednedPoint);

            XYZ consolPlacePoint = default(XYZ);

            XYZ rightDirection = CurrentView.RightDirection.Normalize();

            XYZ upDirection = CurrentView.UpDirection.Normalize();

            XYZ downDirection = upDirection.Negate();

            Line consolLine = GetActiveViewBaseLine(cornerPoint.RefLine, CurrentView, placePoint, out consolPlacePoint);
            XYZ consolSP = consolLine.GetEndPoint(0);
            XYZ consolEP = consolLine.GetEndPoint(1);

            XYZ midPoint = consolLine.Evaluate(0.5, true);


            Line downLine = GetLineBasedOnPlane(false, consolPlacePoint, downDirection);

            Line rightLine = GetLineBasedOnPlane(false, consolPlacePoint, rightDirection);

            double angleDown = CheckTwoPointsAreEqual(consolLine.Direction, downDirection) || CheckTwoPointsAreEqual(consolLine.Direction, downDirection.Negate()) ? 0 : GetAngleOfTwoLines(consolLine, downLine);

            double angleRight = CheckTwoPointsAreEqual(consolLine.Direction, rightDirection) || CheckTwoPointsAreEqual(consolLine.Direction, rightDirection.Negate()) ? 0 : GetAngleOfTwoLines(consolLine, rightLine);

            Line perpConsolLine = CreatePerpendicularLine(consolLine, midPoint, planeNormalDirection);


            double perpAngle = GetAngleOfTwoLines(perpConsolLine, rightLine);

            if (angleDown != 0 && angleRight == 0)
            {
                double angle = -90 * Math.PI / 180;

                famIns = doc.Create.NewFamilyInstance(placePoint, famSym, CurrentView);

                ElementTransformUtils.RotateElement(doc, famIns.Id, axis, angle);
            }
            else if (angleRight != 0 && angleDown == 0)
            {
                double angle = -180 * Math.PI / 180;

                famIns = doc.Create.NewFamilyInstance(placePoint, famSym, CurrentView);

                ElementTransformUtils.RotateElement(doc, famIns.Id, axis, angle);
            }
            else
            {
                double actualAngle = 0;

                actualAngle = -perpAngle;

                famIns = doc.Create.NewFamilyInstance(consolPlacePoint, famSym, CurrentView);

                ElementTransformUtils.RotateElement(doc, famIns.Id, axis, actualAngle);
            }

            return famIns;
        }



        private FamilyInstance CreateFamForLine(XYZ planeNormalDirection, FamilySymbol famSym, CornerPointInfo cornerPoint, Autodesk.Revit.DB.View CurrentView, bool isSketchViewEditInActive)
        {
            FamilyInstance famIns;

            bool isFlip = cornerPoint.IsFlipElement;
            XYZ boundaryMidPoint = cornerPoint.BoundaryMidPoint;
            BoundingBoxXYZ elemBbox = cornerPoint.ElemBoundingBox;
            XYZ placePoint = cornerPoint.CornerPoint;


            XYZ extendedPlane = planeNormalDirection.Multiply(100);

            XYZ extednedPoint = extendedPlane.Add(placePoint);

            Line axis = Line.CreateBound(placePoint, extednedPoint);

            XYZ consolPlacePoint = default(XYZ);

            XYZ rightDirection = CurrentView.RightDirection.Normalize();

            XYZ upDirection = CurrentView.UpDirection.Normalize();

            XYZ downDirection = upDirection.Negate();


            Line consolLine = GetActiveViewBaseLine(cornerPoint.RefLine, CurrentView, placePoint, out consolPlacePoint);


            famIns = doc.Create.NewFamilyInstance(consolPlacePoint, famSym, CurrentView);

            XYZ consolSP = consolLine.GetEndPoint(0);
            XYZ consolEP = consolLine.GetEndPoint(1);

            XYZ midPoint = (consolLine.GetEndPoint(0) + consolLine.GetEndPoint(1)) / 2;

            double famAngle = 90 * Math.PI / 180;
            double rotateTo = 60 * Math.PI / 180;

            Line downLine = GetLineBasedOnPlane(false, consolPlacePoint, downDirection);

            Line rightLine = GetLineBasedOnPlane(false, consolPlacePoint, rightDirection);

            double angleDown = CheckTwoPointsAreEqual(consolLine.Direction, downDirection) || CheckTwoPointsAreEqual(consolLine.Direction, downDirection.Negate()) ? 0 : GetAngleOfTwoLines(consolLine, downLine);

            double angleRight = CheckTwoPointsAreEqual(consolLine.Direction, rightDirection) || CheckTwoPointsAreEqual(consolLine.Direction, rightDirection.Negate()) ? 0 : GetAngleOfTwoLines(consolLine, rightLine);

            XYZ planeMirrorDirection = default(XYZ);

            if (angleDown != 0 && angleRight == 0)
            {
                bool isInBoundaryLimit = true;

                if (isSketchViewEditInActive)
                {
                    isInBoundaryLimit = IsInBoundaryLimit(boundaryMidPoint, elemBbox, downDirection, rightDirection, planeNormalDirection, isSketchViewEditInActive, cornerPoint.RefLine);
                }
                else
                {
                    bool isRevLineExists = CheckReverseLineExits(consolLine, cornerPoint);

                    if (isRevLineExists)
                    {
                        isInBoundaryLimit = false;
                    }
                }


                isFlip = IsToFlip(isFlip, isInBoundaryLimit);

                planeMirrorDirection = downDirection;

                double actualAngle = rotateTo;

                Line rightMidLine = GetLineBasedOnPlane(false, midPoint, rightDirection);

                //SetComparisonResult compareRes = rightMidLine.Intersect(consolLine);

                Outline outLineStraight = new Outline(rightMidLine.GetEndPoint(0), rightMidLine.GetEndPoint(1));

                Outline outLineAgainst = new Outline(rightMidLine.GetEndPoint(1), rightMidLine.GetEndPoint(0));

                bool isStraightLineContains = outLineStraight.Contains(consolPlacePoint, 0.1);

                bool isAgainstLineContains = outLineAgainst.Contains(consolPlacePoint, 0.1);

                if (isStraightLineContains || isAgainstLineContains)
                {
                    actualAngle = (angleDown + famAngle) - rotateTo;

                    actualAngle = -actualAngle;
                }
                else
                {
                    actualAngle = angleRight - rotateTo;
                }

                ElementTransformUtils.RotateElement(doc, famIns.Id, axis, actualAngle);

            }
            else if (angleRight != 0 && angleDown == 0)
            {
                bool isInBoundaryLimit = true;

                if (isSketchViewEditInActive)
                {
                    isInBoundaryLimit = IsInBoundaryLimit(boundaryMidPoint, elemBbox, downDirection, rightDirection, planeNormalDirection, isSketchViewEditInActive, cornerPoint.RefLine);
                }
                else
                {
                    bool isRevLineExists = CheckReverseLineExits(consolLine, cornerPoint);

                    if (isRevLineExists)
                    {
                        isInBoundaryLimit = false;
                    }
                }


                isFlip = IsToFlip(isFlip, isInBoundaryLimit);

                planeMirrorDirection = rightDirection;

                double actualAngle = rotateTo;

                Line downMidLine = GetLineBasedOnPlane(false, midPoint, downDirection);

                //SetComparisonResult compareRes = downMidLine.Intersect(consolLine);

                Outline outLineStraight = new Outline(downMidLine.GetEndPoint(0), downMidLine.GetEndPoint(1));

                Outline outLineAgainst = new Outline(downMidLine.GetEndPoint(1), downMidLine.GetEndPoint(0));

                bool isStraightLineContains = outLineStraight.Contains(consolPlacePoint, 0.1);

                bool isAgainstLineContains = outLineAgainst.Contains(consolPlacePoint, 0.1);

                if (isStraightLineContains || isAgainstLineContains)
                {
                    actualAngle = famAngle - rotateTo;
                }
                else
                {
                    actualAngle = famAngle - rotateTo;

                    actualAngle = -actualAngle;
                }

                ElementTransformUtils.RotateElement(doc, famIns.Id, axis, actualAngle);

            }
            else
            {

                bool isInBoundaryLimit = true;

                if (!isSketchViewEditInActive)
                {
                    bool isRevLineExists = CheckReverseLineExits(consolLine, cornerPoint);

                    if (isRevLineExists)
                        isInBoundaryLimit = false;
                }

                isFlip = IsToFlip(isFlip, isInBoundaryLimit);


                Line perpConsolLine = CreatePerpendicularLine(consolLine, midPoint, planeNormalDirection);

                planeMirrorDirection = perpConsolLine.Direction;

                double actualAngle = rotateTo;

                actualAngle = -famAngle;

                ElementTransformUtils.RotateElement(doc, famIns.Id, axis, actualAngle);

                bool isToNegateAngle = false;

                Line midForRightLine = GetLineBasedOnPlane(isFlip, midPoint, rightDirection);

                Line downForSPLine = GetLineBasedOnPlane(isFlip, consolSP, downDirection);

                Line downForEPLine = GetLineBasedOnPlane(isFlip, consolEP, downDirection);


                SetComparisonResult resSP = midForRightLine.Intersect(downForSPLine);
                SetComparisonResult resEP = midForRightLine.Intersect(downForEPLine);

                if (resSP == SetComparisonResult.Overlap || resEP == SetComparisonResult.Overlap)
                {
                    isToNegateAngle = true;
                }

                double deltDownAng = (180 / Math.PI) * angleDown;

                if (deltDownAng < 60)
                {
                    double consolDownAng = 60 - deltDownAng;

                    actualAngle = consolDownAng * Math.PI / 180;
                }
                else
                {
                    double consolDownAng = deltDownAng - 60;


                    actualAngle = consolDownAng * Math.PI / 180;

                    if (consolDownAng < 30)
                    {
                        actualAngle = -actualAngle;
                    }
                }

                if (!isToNegateAngle)
                    actualAngle = -actualAngle;

                if (isSketchViewEditInActive)
                {
                    Line consolDownLine = GetLineBasedOnPlane(isFlip, consolPlacePoint, downDirection);

                    bool isFlipInSketchView = IsToFlipInSketchViewPlan(boundaryMidPoint, consolDownLine, rightDirection, downForSPLine, downForEPLine);

                    isFlip = isFlipInSketchView;
                }

                ElementTransformUtils.RotateElement(doc, famIns.Id, axis, actualAngle);

            }

            if (isFlip)
            {
                List<ElementId> lstElemId = new List<ElementId> { famIns.Id };

                ICollection<ElementId> elementsToMirror = lstElemId;

                Plane consolPlane = Plane.CreateByNormalAndOrigin(planeMirrorDirection, consolPlacePoint);

                ElementTransformUtils.MirrorElements(doc, elementsToMirror, consolPlane, false);
            }

            return famIns;
        }

        private bool IsToFlipInSketchViewPlan(XYZ boundaryMidPoint, Line consolDownLine, XYZ rightDirection, Line downForSPLine, Line downForEPLine)
        {
            bool isFlip = false;

            Line extendBoundaryRightLine = ExtendLineFromMidPoint(boundaryMidPoint, rightDirection);

            SetComparisonResult downSpCompareRes = extendBoundaryRightLine.Intersect(downForSPLine);

            SetComparisonResult downEpCompareRes = extendBoundaryRightLine.Intersect(downForEPLine);

            // SetComparisonResult downConsolLineCompareRes = extendBoundaryRightLine.Intersect(consolDownLine);


            bool isInBound = false;

            if (downSpCompareRes == SetComparisonResult.Overlap || downEpCompareRes == SetComparisonResult.Overlap)
            {
                isInBound = true;
            }

            //if (downConsolLineCompareRes == SetComparisonResult.Overlap)
            //{
            //    isInBound = true;
            //}

            if (!isInBound && !isFlip)
            {
                isFlip = true;
            }
            else if (!isInBound && isFlip)
            {
                isFlip = false;
            }

            return isFlip;
        }

        private static void GetMinMaxPoints(List<XYZ> lstDownMergePoints, out XYZ minPoint, out XYZ maxPoint)
        {
            double minX = lstDownMergePoints.Min(x => x.X);
            double minY = lstDownMergePoints.Min(x => x.Y);
            double minZ = lstDownMergePoints.Min(x => x.Z);

            double maxX = lstDownMergePoints.Max(x => x.X);
            double maxY = lstDownMergePoints.Max(x => x.Y);
            double maxZ = lstDownMergePoints.Max(x => x.Z);

            minPoint = new XYZ(minX, minY, minZ);
            maxPoint = new XYZ(maxX, maxY, maxZ);
        }

        private bool SketchPlaneShapesAreInBoundary(XYZ boundaryMidPoint, XYZ rightDirection, XYZ downDirection, Line consolLine)
        {
            bool isInBoundaryLimit = true;

            XYZ extendedRightDirection = rightDirection.Multiply(100);

            XYZ extednedRightPoint = extendedRightDirection.Add(boundaryMidPoint);

            Line rightLineExtend = Line.CreateBound(boundaryMidPoint, extednedRightPoint);

            XYZ extendedDownDirection = downDirection.Multiply(100);

            XYZ extednedDownPoint = extendedDownDirection.Add(boundaryMidPoint);

            Line downLineExtend = Line.CreateBound(boundaryMidPoint, extednedDownPoint);

            Curve cloneConsolCurve = consolLine.Clone();

            Line cloneConsolLine = cloneConsolCurve as Line;

            cloneConsolLine.MakeUnbound();

            SetComparisonResult rightIntersecRes = cloneConsolLine.Intersect(rightLineExtend);

            SetComparisonResult downIntersecRes = cloneConsolLine.Intersect(downLineExtend);

            if (rightIntersecRes == SetComparisonResult.Overlap || downIntersecRes == SetComparisonResult.Overlap)
            {
                isInBoundaryLimit = false;

            }

            return isInBoundaryLimit;
        }

        private bool CheckReverseLineExits(Line consolLine, CornerPointInfo cornerPoint)
        {
            bool revLineExits = false;

            List<Line> lstRevLines = new List<Line>();

            if (LstReverseLineInfo.ContainsKey(cornerPoint.ElemID))
            {
                LstReverseLineInfo.TryGetValue(cornerPoint.ElemID, out lstRevLines);

                Line revLine = lstRevLines.Where(x => (!CheckTwoPointsAreEqual(x.GetEndPoint(0), consolLine.GetEndPoint(0)) && !CheckTwoPointsAreEqual(x.GetEndPoint(1), consolLine.GetEndPoint(1))) && (!CheckTwoPointsAreEqual(x.GetEndPoint(1), consolLine.GetEndPoint(0)) && !CheckTwoPointsAreEqual(x.GetEndPoint(0), consolLine.GetEndPoint(1))) && (CheckTwoPointsAreEqual(x.Direction, consolLine.Direction) || CheckTwoPointsAreEqual(x.Direction.Negate(), consolLine.Direction)) && Math.Round(x.Length, 2) == Math.Round(consolLine.Length, 2)).Select(x => x).FirstOrDefault();

                if (revLine != null)
                {
                    revLineExits = true;

                    lstRevLines.Remove(revLine);
                }
            }
            else
            {
                lstRevLines.Add(consolLine);

                LstReverseLineInfo.Add(cornerPoint.ElemID, lstRevLines);
            }

            return revLineExits;
        }

        private static bool IsToFlip(bool isFlip, bool isInBoundaryLimit)
        {
            if (!isInBoundaryLimit)
            {
                if (isFlip)
                {
                    isFlip = false;
                }
                else
                {
                    isFlip = true;
                }
            }

            return isFlip;
        }

        public Outline GetCropOutLine(Autodesk.Revit.DB.View CurrentView)
        {
            Outline outlineCrop = default(Outline);

            try
            {
                BoundingBoxXYZ cropBoundary = CurrentView.CropBox;


                XYZ viewOrigin = CurrentView.Origin;

                List<CurveLoop> lstCropCurveLoop = CurrentView.GetCropRegionShapeManager().GetCropShape().ToList();

                List<XYZ> lstCoordinatePoints = new List<XYZ>();


                foreach (CurveLoop curveCrop in lstCropCurveLoop)
                {
                    foreach (Curve curve in curveCrop)
                    {
                        lstCoordinatePoints.Add(curve.GetEndPoint(0));
                        lstCoordinatePoints.Add(curve.GetEndPoint(1));
                    }
                }

                double MinX = lstCoordinatePoints.Min(x => x.X);
                double MinY = lstCoordinatePoints.Min(x => x.Y);
                double MinZ = lstCoordinatePoints.Min(x => x.Z);

                double MaxX = lstCoordinatePoints.Max(x => x.X);
                double MaxY = lstCoordinatePoints.Max(x => x.Y);
                double MaxZ = lstCoordinatePoints.Max(x => x.Z);

                XYZ minPoint = new XYZ(MinX, MinY, MinZ);
                XYZ maxPoint = new XYZ(MaxX, MaxY, MaxZ);


                outlineCrop = new Outline(minPoint, maxPoint);
            }
            catch (Exception ex)
            {
            }
            return outlineCrop;
        }

        private bool IsInCropViewRegion(XYZ placePoint, Autodesk.Revit.DB.View CurrentView, Outline cropOutLine)
        {
            XYZ midPoint = (cropOutLine.MinimumPoint + cropOutLine.MaximumPoint) / 2;


            XYZ consolPoint = placePoint;

            if (CheckTwoPointsAreEqual(CurrentView.ViewDirection, XYZ.BasisX) || CheckTwoPointsAreEqual(CurrentView.ViewDirection.Negate(), XYZ.BasisX))
            {
                consolPoint = new XYZ(midPoint.X, placePoint.Y, placePoint.Z);
            }
            else if (CheckTwoPointsAreEqual(CurrentView.ViewDirection, XYZ.BasisY) || CheckTwoPointsAreEqual(CurrentView.ViewDirection.Negate(), XYZ.BasisY))
            {
                consolPoint = new XYZ(placePoint.X, midPoint.Y, placePoint.Z);
            }
            else if (CheckTwoPointsAreEqual(CurrentView.ViewDirection, XYZ.BasisZ) || CheckTwoPointsAreEqual(CurrentView.ViewDirection.Negate(), XYZ.BasisZ))
            {
                consolPoint = new XYZ(placePoint.X, placePoint.Y, midPoint.Z);
            }

            bool isContains = cropOutLine.Contains(consolPoint, 0.1);

            return isContains;
        }

        private bool IsInSketchViewEditable(CornerPointInfo cornerPoint)
        {
            bool isSketchViewEditInActive = false;

            bool isBarOnNormal = cornerPoint.BarsOnNormalSide;

            isSketchViewEditInActive = isBarOnNormal;

            return isSketchViewEditInActive;
        }

        private bool IsInBoundaryLimit(XYZ boundaryMidPoint, BoundingBoxXYZ elemBbox, XYZ downDirection, XYZ rightDirection, XYZ planeNormalDire, bool isInSketchView, Line refLine)
        {
            bool isInBoundaryLimit = true;

            IntersectionResult intersecRes = default(IntersectionResult);

            Line currentRefLine = refLine.Clone() as Line;

            currentRefLine.MakeUnbound();

            intersecRes = currentRefLine.Project(boundaryMidPoint);

            if (intersecRes != null && intersecRes.XYZPoint != null)
            {
                if (!CheckTwoPointsAreEqual(intersecRes.XYZPoint, boundaryMidPoint))
                {
                    Line line = Line.CreateBound(intersecRes.XYZPoint, boundaryMidPoint);

                    if (isInSketchView)
                    {
                        if (!CheckTwoPointsAreEqual(line.Direction, rightDirection) && !CheckTwoPointsAreEqual(line.Direction, downDirection))
                        {
                            isInBoundaryLimit = false;
                        }
                    }
                    else
                    {
                        if (!CheckTwoPointsAreEqual(line.Direction, rightDirection) && !CheckTwoPointsAreEqual(line.Direction, downDirection) && !CheckTwoPointsAreEqual(line.Direction, planeNormalDire))
                        {
                            isInBoundaryLimit = false;
                        }
                    }
                }
            }

            return isInBoundaryLimit;
        }

        private bool IsMidPointGreater(XYZ consolPlacePoint, XYZ midPoint)
        {
            return (Math.Round(midPoint.X, 4) > Math.Round(consolPlacePoint.X, 4) || Math.Round(midPoint.Y, 4) > Math.Round(consolPlacePoint.Y, 4) || Math.Round(midPoint.Z, 4) > Math.Round(consolPlacePoint.Z, 4));
        }

        public Line GetActiveViewBaseLine(Line currentLine, Autodesk.Revit.DB.View CurrentView, XYZ placePoint, out XYZ consolPlacePoint, bool forCross = false)
        {
            consolPlacePoint = default(XYZ);

            XYZ actualPt1 = currentLine.GetEndPoint(0);
            XYZ actualPt2 = currentLine.GetEndPoint(1);

            XYZ pt1 = currentLine.GetEndPoint(0);
            XYZ pt2 = currentLine.GetEndPoint(1);

            var viewFromSection = CurrentView;

            XYZ Origin = placePoint;
            XYZ ViewBasisX = viewFromSection.RightDirection;
            XYZ ViewBasisY = viewFromSection.ViewDirection;
            if (ViewBasisX.X < 0 ^ ViewBasisX.Y < 0)
            {
                double d = pt1.Y;
                pt1 = new XYZ(pt1.X, pt2.Y, pt1.Z);
                pt2 = new XYZ(pt2.X, d, pt2.Z);
            }
            XYZ ToPlane1 = pt1.Add(ViewBasisY.Multiply(
              ViewBasisY.DotProduct(Origin.Subtract(pt1))));

            XYZ ToPlane2 = pt2.Subtract(ViewBasisY.Multiply(
              ViewBasisY.DotProduct(pt2.Subtract(Origin))));

            //XYZ correctionVector = ToPlane2.Subtract(ToPlane1)
            //.Normalize().Multiply(correction);

            XYZ endPoint0 = default(XYZ);
            XYZ endPoint1 = default(XYZ);



            if (forCross || CheckTwoPointsAreEqual(placePoint, actualPt1))
            {
                consolPlacePoint = ToPlane1;

                endPoint0 = ToPlane1;
                endPoint1 = ToPlane2;
            }
            else
            {
                consolPlacePoint = ToPlane2;

                endPoint0 = ToPlane2;
                endPoint1 = ToPlane1;
            }

            return Line.CreateBound(endPoint0, endPoint1);
        }


        private Line GetLineBasedOnPlane(bool isFlip, XYZ placePoint, XYZ viewDirection, double lenght = 100)
        {
            XYZ extendedPlane = isFlip ? viewDirection.Negate().Multiply(lenght) : viewDirection.Multiply(lenght);

            XYZ extednedPoint = extendedPlane.Add(placePoint);

            Line axis = Line.CreateBound(placePoint, extednedPoint);
            return axis;
        }





        public List<StorageHelper> ReadSettings()
        {
            List<StorageHelper> lstStorageHelper = new List<StorageHelper>();

            try
            {
                string storedData = CommonData.ExtractSettingsFromDocument(ConstantValues.RebarFieldName, ConstantValues.RebarSchemaName, ConstantValues.RebarSchemaGuid, doc);

                if (!string.IsNullOrEmpty(storedData))
                {
                    object _lstStorageHelper = CommonData.DeserializeObject(typeof(List<StorageHelper>), storedData);

                    lstStorageHelper = _lstStorageHelper as List<StorageHelper>;
                }
            }
            catch (Exception ex)
            {
            }
            return lstStorageHelper;
        }



        private double GetAngleOfTwoLines(Line refLine, Line angleLine1)
        {
            XYZ vector1 = angleLine1.GetEndPoint(1) - angleLine1.GetEndPoint(0);
            XYZ vector2 = refLine.GetEndPoint(1) - refLine.GetEndPoint(0);
            double tempCheck1 = vector1.AngleTo(vector2);


            return tempCheck1;
        }

        // Calculates the angle formed between two lines
        public double angleBetween2Lines(Line line1, Line line2)
        {
            double slope1 = line1.GetEndPoint(0).Y - line1.GetEndPoint(1).Y / line1.GetEndPoint(0).X - line1.GetEndPoint(1).X;
            double slope2 = line2.GetEndPoint(0).Y - line2.GetEndPoint(1).Y / line2.GetEndPoint(0).X - line2.GetEndPoint(1).X;
            double angle = Math.Atan((slope1 - slope2) / (1 - (slope1 * slope2)));
            return angle;
        }



        // <summary>
        /// Calculates the angle a point is to the origin (0 is to the right)
        /// </summary>
        private double XYToDegrees(XYZ startPoint, XYZ endPoint)
        {
            double x2 = endPoint.X;
            double x1 = startPoint.X;

            double y2 = endPoint.Y;
            double y1 = startPoint.Y;

            var w = x2 - x1;
            var h = y2 - y1;

            var atan = Math.Atan(h / w) / Math.PI * 180;
            if (w < 0 || h < 0)
                atan += 180;
            if (w > 0 && h < 0)
                atan -= 180;
            if (atan < 0)
                atan += 360;

            return atan % 360;
        }

        private Line CreatePerpendicularLine(Line actualLine, XYZ placePoint, XYZ planeNormal)
        {
            Line perpendicularLine = default(Line);

            double checkAngle = 90 * Math.PI / 180;

            Transform trans = Transform.CreateRotationAtPoint(planeNormal, checkAngle, placePoint);

            Curve rotateLine = actualLine.CreateTransformed(trans);

            perpendicularLine = rotateLine as Line;

            return perpendicularLine;
        }

        private List<FamilySymbol> GetFamilySymbolWithName(List<Element> annotationFromDocument)
        {
            List<FamilySymbol> lstFamSymbol = new List<FamilySymbol>();

            foreach (Element annotate in annotationFromDocument)
            {
                AnnotationSymbolType annoSymbol = annotate as AnnotationSymbolType;

                if (annoSymbol != null && lstAnnotateName.Any(x => x.ToLower().Equals(annoSymbol.FamilyName.ToLower())))
                {
                    FamilySymbol familySym = annoSymbol;

                    lstFamSymbol.Add(familySym);

                    if (lstFamSymbol.Count == lstAnnotateName.Count)
                    {
                        break;
                    }
                }
            }
            return lstFamSymbol;
        }


        private void CreateTemLineWithCurves(List<Curve> lstCurve, Autodesk.Revit.DB.View CurrentView)
        {
            //Transaction trans = new Transaction(doc, "CreatePlane");

            // trans.Start();

            foreach (Curve curve in lstCurve)
            {
                CreateTempLine(curve.GetEndPoint(0), curve.GetEndPoint(1), doc, CurrentView, false);
            }

            //  trans.Commit();
        }



        private List<Curve> ReadLineFromActiveViewElement(Element element)
        {
            Transaction trans = new Transaction(doc, "viewChange");


            List<Curve> lstCurves = new List<Curve>();

            try
            {
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.View = element.Document.ActiveView;

                GeometryElement geoElement = element.get_Geometry(opt);

                if (geoElement != null)
                {
                    lstCurves = ReadLinesFromGeoElem(geoElement);
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

            }

            return lstCurves;
        }

        private List<Curve> ReadLinesFromGeoElem(GeometryElement geoElement)
        {
            List<Curve> lstCurves = new List<Curve>();

            foreach (GeometryObject geomObj in geoElement)
            {
                Curve curve = geomObj as Curve;

                if (curve != null)
                {
                    lstCurves.Add(curve);

                    continue;
                }

                #region MyRegion
                Solid solid = geomObj as Solid;

                if (solid != null)
                {
                    List<Edge> lstSolidEdgeArray = solid.Edges.Cast<Edge>().ToList();

                    List<Curve> lstEdgeLine = lstSolidEdgeArray.Where(x => x.AsCurve() != null).Select(x => x.AsCurve()).ToList();

                    lstCurves.AddRange(lstEdgeLine);

                    continue;
                }
                #endregion

                GeometryInstance geomInst = geomObj as GeometryInstance;

                if (geomInst != null)
                {
                    GeometryElement geoSymElement = geomInst.SymbolGeometry;

                    lstCurves.AddRange(ReadLinesFromGeoElem(geoSymElement));
                }
            }

            return lstCurves;
        }

        private void CreateTempLine(XYZ startPoint, XYZ endPoint, Document doc, Autodesk.Revit.DB.View CurrentView, bool isdetailLine = false)
        {
            Line oppline = null;

            try
            {

                oppline = Line.CreateBound(startPoint, endPoint);
                Plane oppplane = Plane.CreateByNormalAndOrigin(startPoint.CrossProduct(endPoint), endPoint);
                SketchPlane opskplane = SketchPlane.Create(doc, oppplane);
                ModelLine opliness = null;
                if (!isdetailLine)
                {
                    opliness = doc.Create.NewModelCurve(oppline, opskplane) as ModelLine;
                }
                else
                {
                    DetailLine detailLine = doc.Create.NewDetailCurve(CurrentView, oppline) as DetailLine;

                    GraphicsStyle gs = detailLine.LineStyle as GraphicsStyle;
                    gs.GraphicsStyleCategory.SetLineWeight(10, GraphicsStyleType.Projection);


                    OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                    ogs.SetProjectionLineColor(new Color(250, 10, 10));
                    CurrentView.SetElementOverrides(detailLine.Id, ogs);

                }

            }
            catch (Exception ex)
            {

            }
        }
    }

    public class ItemEqualityComparer : IEqualityComparer<CornerPointInfo>
    {
        public bool Equals(CornerPointInfo Point1, CornerPointInfo Point2)
        {
            bool isSamePoint = false;

            if (Point1.CurrentViewId == Point2.CurrentViewId && (Math.Round(Point1.CornerPoint.X, 3) == Math.Round(Point2.CornerPoint.X, 3) && Math.Round(Point1.CornerPoint.Y, 3) == Math.Round(Point2.CornerPoint.Y, 3) && Math.Round(Point1.CornerPoint.Z, 3) == Math.Round(Point2.CornerPoint.Z, 3)))
            {
                isSamePoint = true;
            }

            return isSamePoint;
        }

        public int GetHashCode(CornerPointInfo obj)
        {
            string concatValue = string.Concat(obj.CornerPoint.X.ToString(), obj.CornerPoint.Y.ToString(), obj.CornerPoint.Z.ToString());

            return concatValue.GetHashCode();
        }
    }
}




