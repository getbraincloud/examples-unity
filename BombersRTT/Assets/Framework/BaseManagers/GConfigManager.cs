using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameframework
{
    #region Public Structs
    #endregion

    public class GConfigManager : SingletonBehaviour<GConfigManager>
    {
        #region Public Static
        static public float ReadFloatSafely(Dictionary<string, object> in_dict, string in_key)
        {
            float toReturn = 0.0f;
            if (in_dict.ContainsKey(in_key))
            {
                toReturn = ReadFloatSafely(in_dict[in_key]);
            }
            return toReturn;
        }

        static public float ReadFloatSafely(object in_object)
        {
            try
            {
                return (float)(double)in_object;
            }
            catch (Exception)
            {

                try
                {
                    return (float)(int)in_object;
                }
                catch (Exception)
                {
                    try
                    {
                        return (float)in_object;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            short s = Convert.ToInt16(in_object);
                            return Convert.ToSingle(s);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                return float.Parse(in_object as string);
                            }
                            catch (Exception)
                            {
                                return 0.0f;
                            }
                        }   
                    }
                }
            }
        }


        static public int ReadIntSafely(Dictionary<string, object> in_dict, string in_key)
        {
            int toReturn = 0;
            if (in_dict.ContainsKey(in_key))
            {
                toReturn = ReadIntSafely(in_dict[in_key]);
            }
            return toReturn;
        }

        static public int ReadIntSafely(object in_object)
        {
            try
            {
                return (int)in_object;
            }
            catch (Exception)
            {
                try
                {
                    return int.Parse(in_object as string);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        static public bool ReadBoolSafely(Dictionary<string, object> in_dict, string in_key)
        {
            bool toReturn = false;
            if (in_dict.ContainsKey(in_key))
            {
                toReturn = ReadBoolSafely(in_dict[in_key]);
            }
            return toReturn;
        }

        static public bool ReadBoolSafely(object in_object)
        {
            try
            {
                return (bool)in_object;
            }
            catch (Exception)
            {
                try
                {
                    return bool.Parse(in_object as string);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static string GetAbsoluteFileListURLFromShortName(string in_shortName)
        {
            return Instance.GetAbsoluteFileListURLFromShortName2(in_shortName);
        }
        public static bool GetBoolValue(string in_key)
        {
            return Instance.getBoolValuePrivate(in_key);
        }
        public static string GetStringValue(string in_key)
        {
            return Instance.getStringValuePrivate(in_key);
        }
        public static int GetIntValue(string in_key)
        {
            return Instance.getIntValuePrivate(in_key);
        }
        public static float GetFloatValue(string in_key)
        {
            return Instance.getFloatValuePrivate(in_key);
        }
        #endregion

        #region Public
        public string GetAbsoluteFileListURLFromShortName2(string in_shortName)
        {
            string urlToReturn = in_shortName;
            foreach (var item in m_fileListDetails)
            {
                if (item["shortName"] as string == in_shortName)
                {
                    urlToReturn = item["absoluteUrl"] as string;
                    break;
                }
            }

            return urlToReturn;
        }

        private bool getBoolValuePrivate(string in_key)
        {
            bool bToReturn = false;
            if (m_objectConfig.ContainsKey(in_key))
            {
                try
                {
                    bToReturn = (bool)m_objectConfig[in_key];
                }
                catch (System.Exception)
                {
                    bToReturn = bool.Parse((string)m_objectConfig[in_key]);
                }
            }

            return bToReturn;
        }

        private string getStringValuePrivate(string in_key)
        {
            string sToReturn = "";
            if (m_objectConfig.ContainsKey(in_key))
            {
                sToReturn = (string)m_objectConfig[in_key];
            }

            return sToReturn;
        }

        private int getIntValuePrivate(string in_key)
        {
            int iToReturn = -1;
            if (m_objectConfig.ContainsKey(in_key))
            {
                try
                {
                    iToReturn = (int)m_objectConfig[in_key];
                }
                catch (System.Exception)
                {
                    iToReturn = int.Parse((string)m_objectConfig[in_key]);
                }
            }

            return iToReturn;
        }

        private float getFloatValuePrivate(string in_key)
        {
            float fToReturn = -1.0f;
            if (m_objectConfig.ContainsKey(in_key))
            {
                try
                {
                    fToReturn = (float)m_objectConfig[in_key];
                }
                catch (System.Exception)
                {
                    fToReturn = float.Parse((string)m_objectConfig[in_key]);
                }
            }

            return fToReturn;
        }

        public void OnReadBrainCloudProperties(string in_jsonString, object in_object)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_jsonString);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            for (int index = 0; index < jsonData.Count; index++)
            {
                var item = jsonData.ElementAt(index);
                m_objectConfig[item.Key] = ((Dictionary<string, object>)item.Value)[BrainCloudConsts.JSON_VALUE];
            }

            GEventManager.TriggerEvent("OnConfigPropertiesRead");
        }

        public void OnReadGlobalFileList(string in_jsonString, object in_object)
        {
            Dictionary<string, object> jsonMessage = (Dictionary<string, object>)JsonReader.Deserialize(in_jsonString);
            Dictionary<string, object> jsonData = (Dictionary<string, object>)jsonMessage[BrainCloudConsts.JSON_DATA];
            Array array = (Array)jsonData["fileDetails"];
            m_fileListDetails.Clear();
            for (int index = 0; index < array.Length; index++)
            {
                var item = array.GetValue(index) as Dictionary<string, object>;
                m_fileListDetails.Add(item);
            }

            GEventManager.TriggerEvent("OnGlobalFilesRead");
        }
        #endregion

        #region Private
        private Dictionary<string, object> m_objectConfig = new Dictionary<string, object>();
        private List<Dictionary<string, object>> m_fileListDetails = new List<Dictionary<string, object>>();
        #endregion
    }
}
