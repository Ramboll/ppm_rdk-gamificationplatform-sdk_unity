using Autodesk.Revit.DB;

namespace RebarInterop
{
    internal class FamilyOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            familyInUse = false;

            overwriteParameterValues = true;

            return true;
        }


        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            familyInUse = false;

            overwriteParameterValues = true;

            source = FamilySource.Project;

            return true;
        }
    }
}