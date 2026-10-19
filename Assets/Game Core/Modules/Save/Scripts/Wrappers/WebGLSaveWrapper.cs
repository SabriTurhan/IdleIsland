using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace IslandEscape
{
    public class WebGLSaveWrapper : BaseSaveWrapper
    {
        public void Init(string prefix)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            init(prefix);
#endif
        }

        public override GlobalSave Load(string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string jsonObject = load(fileName);

            // Yandex Games cloud save: the newer of the local and the server copy wins
            YandexGames.CloudSave cloud = YandexGames.LoadCloud();
            if (cloud != null)
            {
                double.TryParse(load(fileName + "_t"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double localTime);
                if (string.IsNullOrEmpty(jsonObject) || cloud.t > localTime)
                {
                    jsonObject = cloud.save;
                    save(fileName, jsonObject);
                    save(fileName + "_t", cloud.t.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            if(!string.IsNullOrEmpty(jsonObject))
            {
                try
                {
                    GlobalSave deserializedObject = JsonUtility.FromJson<GlobalSave>(jsonObject);

                    return deserializedObject;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(ex.Message);
                }
            }
#endif

            return new GlobalSave();
        }

        public override void Save(GlobalSave globalSave, string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string jsonObject = JsonUtility.ToJson(globalSave);

            save(fileName, jsonObject);

            double now = (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
            save(fileName + "_t", now.ToString(System.Globalization.CultureInfo.InvariantCulture));
            YandexGames.SaveCloud(jsonObject, now);
#endif
        }

        public override void Delete(string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            deleteItem(fileName);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string init(string prefix);

        [DllImport("__Internal")]
        private static extern string load(string keyName);

        [DllImport("__Internal")]
        private static extern void save(string keyName, string data);

        [DllImport("__Internal")]
        private static extern void deleteItem(string keyName);
#endif
    }
}
