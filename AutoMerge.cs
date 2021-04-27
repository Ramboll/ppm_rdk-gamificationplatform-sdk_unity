using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RebarAnnotationTool
{
    public class AutoMerge
    {

        public static void Load()
        {
            Assembly CurrentAssembly = Assembly.GetExecutingAssembly();
            string[] AllEmbeddedDll = CurrentAssembly.GetManifestResourceNames().Where(r => r.EndsWith(".dll")).ToArray();

            foreach (string EmbeddedResource in AllEmbeddedDll)
            {
                LoadMannageddll(CurrentAssembly, EmbeddedResource);
            }
        }

      

        private static void LoadMannageddll(Assembly CurrentAssembly, string EmbeddedResource)
        {
            byte[] ba = null;

            using (Stream stream = CurrentAssembly.GetManifestResourceStream(EmbeddedResource))
            {
                // Either the file is not existed or it is not mark as embedded resource
                if (stream == null) throw new Exception(EmbeddedResource + " is not found in Embedded Resources.");

                // Get byte[] from the file from embedded resource
                ba = new byte[(int)stream.Length];
                stream.Read(ba, 0, (int)stream.Length);
                try
                {
                    // Load it into memory
                    Assembly.Load(ba);
                }
                catch (Exception EX)
                {

                    string NamespaceName = CurrentAssembly.GetName().Name; // As Assembly Name & Default Namespace has Same Name
                    string DllName = EmbeddedResource.Replace(NamespaceName + ".Resources.", "");
                    LoadUnMannageddll(DllName, ba);
                }
            }
        }
        private static void LoadUnMannageddll(string DllName, byte[] ba)
        {

            bool fileOk = false;
            string tempFile = "";

            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                string fileHash = BitConverter.ToString(sha1.ComputeHash(ba)).Replace("-", string.Empty); ;

                tempFile = Path.GetTempPath() + DllName;

                if (File.Exists(tempFile))
                {
                    byte[] bb = File.ReadAllBytes(tempFile);
                    string fileHash2 = BitConverter.ToString(sha1.ComputeHash(bb)).Replace("-", string.Empty);

                    if (fileHash == fileHash2) fileOk = true;
                    else fileOk = false;
                }
                else fileOk = false;
            }

            if (!fileOk) System.IO.File.WriteAllBytes(tempFile, ba);

            try { Assembly.LoadFile(tempFile); }
            catch (Exception EX) { }
        }

    }
}
