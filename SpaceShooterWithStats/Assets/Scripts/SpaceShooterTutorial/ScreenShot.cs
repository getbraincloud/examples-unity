
//using System.Collections.Generic;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;
//using BrainCloudUnity.BrainCloudSettingsDLL.BCWrapped.LitJson;
using BrainCloud.JsonFx.Json;

public class ScreenShot : MonoBehaviour
{
    string encodedText;
    string fileName;
    string m_Status;

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            screenShot();
            App.Bc.CustomEntityService.CreateEntity("athletes", "{\"test\": \"Testing\"}", "{\"test\": \"Testing\"}", null, true, OnSuccess_Create, OnError_Create);
            //App.Bc.CustomEntityService.CreateEntity("myType", "{\"test\": \"Testing\"}", "{\"test\": \"Testing\"}", null, true, OnFileUploadSuccess, OnFileUploadError);
            //App.Bc.CustomEntityService.DeleteEntity("mytype", "bc232ceb-911c-4729-84fd-c8268b102e55", 1, OnFileUploadSuccess, OnFileUploadError);
            //point to new portal app and test
        }
    }

    #region Screenshot
    public void screenShot()
    {
        StartCoroutine(UploadPNG());
    }

    IEnumerator UploadPNG()
    {
        // We should only read the screen after all rendering is complete
        yield return new WaitForEndOfFrame();
        AppendLog("in coroutine");

        // Create a texture the size of the screen, RGB24 format
        int width = Screen.width;
        int height = Screen.height;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // Read screen contents into the texture
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        
        //from here you have in in memory and can write to disk, or in this case uplad it from memory
        //encoding to string to use the UploadFileFromMemory API call
        encodedText = System.Convert.ToBase64String(bytes);
 
       fileName = "testerest";

        //upload From Memory
        App.Bc.FileService.UploadFileFromMemory("", fileName, true, true, encodedText, OnFileUploadSuccess, OnFileUploadError);
    }
    #endregion

    private void AppendLog(string log)
    {
        string oldStatus = m_Status;
        m_Status = "\n" + log + "\n" + oldStatus;
        Debug.Log(log);
    }

    public void OnFileUploadSuccess(string responseData, object cbObject)
    {
        AppendLog("file upload request successful!"+responseData);
    }

    public void OnFileUploadError(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        AppendLog("upload Failed " + statusMessage);
    }

    public void OnError_Create(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        AppendLog( "Failed to Connect...\n" + statusMessage + "\n" + reasonCode);
    }

    public void OnSuccess_Create(string responseData, object cbObject)
    {
        //JsonReader.
        //dynamic obj = JsonConvert.DeserializeObject(json);
        //Console.WriteLine(obj.unashamedohio.summonerLevel);
        AppendLog( "Created\n" + responseData);
        Dictionary<string, object> entityIddict = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Debug.Log(entityIddict);
        Debug.Log(entityIddict.Count);
        Dictionary<string, object> innerDict = (Dictionary<string, object>)entityIddict["data"];
        Debug.Log(innerDict);
        Debug.Log(innerDict.Count);
        Debug.Log(innerDict["entityId"]);
        App.Bc.CustomEntityService.DeleteEntity("athletes", (string)innerDict["entityId"], (int)innerDict["version"], OnSuccess_Delete, OnError_Delete);
    }

    public void OnSuccess_Delete(string responseData, object cbObject)
    {
        AppendLog("Deleted!\n" + responseData);
        if (responseData==null)
        {
            AppendLog("is empty!\n");
        }
        else 
        {
            AppendLog("garbage!\n"+responseData.Length);
        }
    }
    public void OnError_Delete(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        AppendLog("Failed to Connect to channel...\n" + statusMessage + "\n" + reasonCode);
    }
}
