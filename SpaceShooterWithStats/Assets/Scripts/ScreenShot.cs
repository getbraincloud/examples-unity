using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;
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

        //name of the file
       fileName = "ScreenShot.png";

        //upload From Memory
        App.Bc.FileService.UploadFileFromMemory("", fileName, true, true, bytes, OnFileUploadSuccess, OnFileUploadError);
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
        Dictionary<string, object> entityIddict = (Dictionary<string, object>)JsonReader.Deserialize(responseData);
        Dictionary<string, object> innerDict = (Dictionary<string, object>)entityIddict["data"];
        Debug.Log(innerDict["fileDetails"]);
    }

    public void OnFileUploadError(int statusCode, int reasonCode, string statusMessage, object cbObject)
    {
        AppendLog("upload Failed " + statusMessage);
    }
}

