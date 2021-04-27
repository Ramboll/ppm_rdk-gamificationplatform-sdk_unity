using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;

namespace RebarInterop
{
    public class CommonReaderData
    {
        public static RebarSettingsData ReadExitsSettingsData(string _docPathName)
        {
            RebarSettingsData rebarExistsSettingsData = default(RebarSettingsData);

            try
            {
                if (!string.IsNullOrEmpty(_docPathName))
                {
                    string fileName = Path.GetFileNameWithoutExtension(_docPathName);
                    string fileDirectory = Path.GetDirectoryName(_docPathName);

                    string fullPath = Path.Combine(fileDirectory, fileName + ".settings");

                    if (File.Exists(fullPath))
                    {
                        rebarExistsSettingsData = CommonData.DeSerializePathObject<RebarSettingsData>(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
            }

            return rebarExistsSettingsData;
        }

     

    }
}
