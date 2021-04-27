using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RebarInterop
{
    public class ElementDataInfo
    {
        public int ElemID { get; set; }
        public string ElemName { get; set; }

        public string CategoryName { get; set; }
        public int CatId { get; set; }

        public List<RebarShapeData> LstRebarPatternShapeData { get; set; }
        public ElementType RebarElementType { get; set; }

        public int NumberOfIndex { get; set; }
        public List<Curve> LinePatternCurves { get; set; }
        public ElementId HookStartId { get; set; }
        public ElementId HookEndId { get; set; }

        public bool IsFlipped { get; set; }
        public string ElemHashCodeData { get; set; }

        public ElementId CurrentViewId { get; set; }
        public View CurrentView { get; set; }

        public XYZ PlaneNormalDirection { get; set; }
    }

    public class LinePatternInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class LineWeightInfo
    {
        public int WeightNumber { get; set; }


        public string WeightNumberInString { get; set; }
    }

    public class LineColorInfo
    {
        public byte RValue { get; set; }
        public byte GValue { get; set; }
        public byte BValue { get; set; }
        public string HashCodeValue { get; set; }
        public string ColorName { get; set; }
    }

    public class ColorPatternInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string RGBValue { get; set; }
    }

    public class TopRebarData : RebarInfoData
    {
        public TopRebarData()
        {

        }
    }

    public class BottomRebarData : RebarInfoData
    {
        public BottomRebarData()
        {

        }
    }

    public class FamilyRebarInfo : RebarInfoData
    {
        public FamilyRebarInfo()
        {

        }
    }

    public class RebarSettingsData
    {
        public BottomRebarData BottomRebarData { get; set; }
        public TopRebarData TopRebarData { get; set; }
        public FamilyRebarInfo FamRebarData { get; set; }
        public string RebarEndPath { get; set; }
        public string RebarInPlaneBendPath { get; set; }
        public string RebarOutPlaneBendPath { get; set; }
        public string RebarInclinedPath { get; set; }
    }


    public class RebarInfoData
    {
        public LinePatternInfo LinePatternDataInfo { get; set; }
        public LineWeightInfo LineWeightDataInfo { get; set; }
        public LineColorInfo LineColorDataInfo { get; set; }
    }

    public class WeightPatternInfo
    {
        public string Number { get; set; }
        public int Id { get; set; }
    }

    public class RebarShapeData
    {
        public List<Curve> LstPatternCurves { get; set; }
        public List<CurveDataInfo> LstCurveDataInfo { get; set; }
        public Line DistributionLinePath { get; set; }
        public ElementId ElemID { get; set; }
        public bool BarsOnNormalView { get; set; }
    }

    public class CurveDataInfo
    {
        public CornerPointInfo StartPointInfo { get; set; }
        public CornerPointInfo EndPointInfo { get; set; }
        public List<XYZ> LstEndPoints { get; set; }
        public CurveTypeInfo CurverType { get; set; }
        public Curve RVTCurve { get; set; }
        public XYZ PlaneParalelDirection { get; set; }
        public Curve StraightCurve { get; set; }
    }

    public enum ElementType
    {
        Rebar = 0,
        RebarSystem = 1
    }

    public class CornerPointInfo
    {

        public CornerPointInfo()
        {
        }
        public CornerPointInfo(CornerPointInfo _cornerDataInfo)
        {
            CornerPoint = _cornerDataInfo.CornerPoint;
            AnnotateFam = _cornerDataInfo.AnnotateFam;
            PointPlaneDirection = _cornerDataInfo.PointPlaneDirection;
            PlaneParalelDirection = _cornerDataInfo.PlaneParalelDirection;
            BoundaryMidPoint = _cornerDataInfo.BoundaryMidPoint;
            BarsOnNormalSide = _cornerDataInfo.BarsOnNormalSide;
            IsHookType = _cornerDataInfo.IsHookType;
            LstCurveDataInfo = _cornerDataInfo.LstCurveDataInfo;

        }
        public XYZ CornerPoint { get; set; }
        public bool BarsOnNormalSide { get; set; }

        public XYZ BoundaryMidPoint { get; set; }
        public XYZ BoundaryMinPoint { get; set; }
        public XYZ BoundaryMaxPoint { get; set; }
        public List<CurveDataInfo> LstCurveDataInfo { get; set; }

        public AnnotationFamily AnnotateFam { get; set; }
        public bool IsAnnotateFamSet { get; set; }
        public PlaneDirection PointPlaneDirection { get; set; }

        public XYZ PlaneParalelDirection { get; set; }
        public Line RefLine { get; set; }
        public ElementId FamInsId { get; set; }
        public ElementId ElemID { get; internal set; }
        public bool IsFlipElement { get; internal set; }
        public BoundingBoxXYZ ElemBoundingBox { get; set; }

        public List<CurveDataInfo> GetSharedCurveInfo()
        {
            List<CurveDataInfo> lstAttachCurveDataInfo = new List<CurveDataInfo>();

            if (this.CornerPoint == null || !LstCurveDataInfo.Any())
                return lstAttachCurveDataInfo;

            XYZ currentSPPoint = this.RefLine.GetEndPoint(0);
            XYZ currentEPPoint = this.RefLine.GetEndPoint(1);

            foreach (CurveDataInfo tempCurveDataInfo in LstCurveDataInfo)
            {
                if (tempCurveDataInfo.CurverType == CurveTypeInfo.Arc)
                    continue;

                XYZ tempStartPoint = tempCurveDataInfo.StartPointInfo.CornerPoint;
                XYZ tempEndPoint = tempCurveDataInfo.EndPointInfo.CornerPoint;

                if (CheckTwoPointsAreEqual(currentSPPoint, tempStartPoint) || CheckTwoPointsAreEqual(currentSPPoint, tempEndPoint) || CheckTwoPointsAreEqual(currentEPPoint, tempStartPoint) || CheckTwoPointsAreEqual(currentEPPoint, tempEndPoint))
                {

                    lstAttachCurveDataInfo.Add(tempCurveDataInfo);
                }
            }

            return lstAttachCurveDataInfo;
        }

        private bool CheckTwoPointsAreEqual(XYZ Point1, XYZ Point2, bool isCheckInTwoDimension = false)
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


        public bool IsHookType { get; set; }
        public string HashCodeData { get; set; }

        public ElementId CurrentViewId { get; set; }

      
    }

    public enum AnnotationFamily
    {
        None = 0,
        Cross = 1,
        Circle = 2,
        Line = 3,
        Centre = 4
    }

    public enum CurveTypeInfo
    {
        Line = 0,
        Arc = 1
    }

    public enum PlaneDirection
    {
        TowardsPlane = 0,
        AgainstPlane = 1,
    }
}
