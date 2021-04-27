using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace Utilities
{
    public static class CommonData
    {
        public static bool IsOkClicked { get; set; }
        public static bool IsApplyClicked { get; set; }
        public static bool IsClosed { get; set; }
        public static bool IsCommandTriggered { get; set; }

        public static string SerializeObject<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        public static object DeserializeObject(Type toDeserialize, string xmlData)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toDeserialize);

            using (StringReader stringReader = new StringReader(xmlData))
            {
                return xmlSerializer.Deserialize(stringReader);
            }
        }



        public static void SerializePathObject<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null) { return; }

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(fileName);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static T DeSerializePathObject<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(T); }

            T objectOut = default(T);

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);
                string xmlString = xmlDocument.OuterXml;

                using (StringReader read = new StringReader(xmlString))
                {
                    Type outType = typeof(T);

                    XmlSerializer serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(read))
                    {
                        objectOut = (T)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                //Log exception here
            }

            return objectOut;
        }


        public static void WriteSettingInStorageDocument(string settings, string fieldName, string schemaName, string schemaId, Document doc)
        {
            try
            {

                Entity entity = default(Entity);

                DataStorage dataStorage = ReadExistDataStorageFromDocument(out entity, fieldName, schemaName, schemaId, doc);

                if (entity == null)
                {
                    entity = new Entity(GetSchema(fieldName, schemaName, schemaId));
                }

                entity.Set(fieldName, settings);

                dataStorage.SetEntity(entity);

            }
            catch (Exception ex)
            {
            }
        }

        public static DataStorage ReadExistDataStorageFromDocument(out Entity exitsEntity, string fieldName, string schemaName, string schemaId, Document doc)
        {
            exitsEntity = default(Entity);

            DataStorage storageData = default(DataStorage);

            FilteredElementCollector collector =
                new FilteredElementCollector(doc);

            var dataStorages =
                collector.OfClass(typeof(DataStorage));


            foreach (DataStorage dataStorage in dataStorages)
            {
                Entity setingsEntity =
                   dataStorage.GetEntity(GetSchema(fieldName, schemaName, schemaId));

                if (!setingsEntity.IsValid()) continue;


                exitsEntity = setingsEntity;

                storageData = dataStorage;

                break;
            }


            if (storageData == null)
                storageData = DataStorage.Create(doc);

            return storageData;
        }

        public static Entity GetSettingsEntity(string fieldName, string schemaName, string schemaId, Document doc)
        {
            Entity setingsEntity = default(Entity);

            FilteredElementCollector collector =
                new FilteredElementCollector(doc);

            var dataStorages =
                collector.OfClass(typeof(DataStorage));

            foreach (DataStorage dataStorage in dataStorages)
            {
                setingsEntity =
                  dataStorage.GetEntity(GetSchema(fieldName, schemaName, schemaId));

                if (!setingsEntity.IsValid()) continue;

                return setingsEntity;
            }

            return setingsEntity;
        }

        public static Schema GetSchema(string fieldName, string schemaName, string schemaID)
        {
            Guid schemaGuid = new Guid(schemaID);

            Schema schema = Schema.Lookup(schemaGuid);

            if (schema != null) return schema;

            SchemaBuilder schemaBuilder =
                new SchemaBuilder(schemaGuid);

            schemaBuilder.SetSchemaName(schemaName);

            schemaBuilder.AddSimpleField(fieldName, typeof(string));

            return schemaBuilder.Finish();
        }

        public static string ExtractSettingsFromDocument(string fieldName, string schemaName, string schemaId, Document doc)
        {
            var settingsEntity = GetSettingsEntity(fieldName, schemaName, schemaId, doc);

            if (settingsEntity == null
              || !settingsEntity.IsValid())
            {
                return null;
            }

            string storedData = settingsEntity.Get<string>(fieldName);


            return storedData;
        }

    }

    public enum CommandClickType
    {
        None = 0,
        LinePatternStyle = 1,
        PlaceAnnotation = 2,
        Flip = 3,
        Settings = 4,
        Info = 5
    }

    public class WindowHandle : System.Windows.Forms.IWin32Window
    {
        IntPtr _hwnd;

        public WindowHandle(IntPtr h)
        {

            _hwnd = h;
        }

        public IntPtr Handle
        {
            get
            {
                return _hwnd;
            }
        }
    }
}
