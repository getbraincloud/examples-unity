using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class LogErrors : MonoBehaviour
{
    private string currentError;
    private List<string> _errors = new List<string>();

    internal void Awake() {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived+=HandleLog;
        //Initialize the file
        Write(false,"Starting....");
    }

    private void HandleLog(string logstring, string stackTrace, LogType type) 
    {
        _errors.Add("ENTRY: " + logstring+" STACK: "+stackTrace);
        Write(true,currentError);
    }
    
    //Append enables the overwrite on the file you're writing to
    public void Write(bool append, string addition) {
        //Creates the file
        FileStream sr = File.Open(Path.Combine(Application.persistentDataPath, "ErrorLog.txt"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
        //Saves the file
		sr.Close();
        //Writes to file
		StreamWriter sw = new StreamWriter(Path.Combine(Application.persistentDataPath, "ErrorLog.txt"), append);
        sw.WriteLine(addition);
        //Save the file
		sw.Close();
    }

    private void WriteAllErrors()
    {
        FileStream sr = File.Open(Path.Combine(Application.persistentDataPath, "ErrorLog.txt"), FileMode.OpenOrCreate, FileAccess.ReadWrite);
        //Saves the file
        sr.Close();
        //Writes to file
        StreamWriter sw = new StreamWriter(Path.Combine(Application.persistentDataPath, "ErrorLog.txt"), false);
        foreach(string _str in _errors) {
            sw.WriteLine(_str);
        }
        //Save the file
        sw.Close();
    }

    private void OnApplicationQuit()
    {
        WriteAllErrors();
    }


    private void OnApplicationPause(bool pauseStatus) {
        if(pauseStatus)
        {
            WriteAllErrors();
        }
    }

}
