//----------------------------------------------------
// brainCloud client source code
// Copyright 2026 bitHeads, inc.
//----------------------------------------------------
#if ((UNITY_5_3_OR_NEWER) && !UNITY_WEBPLAYER && (!UNITY_IOS || ENABLE_IL2CPP)) || UNITY_2018_3_OR_NEWER
#define USE_WEB_REQUEST //Comment out to force use of old WWW class on Unity 5.3+
#else
#define DOT_NET
#endif

using System;
using System.Collections;
using System.Collections.Generic;
#if DOT_NET
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
#endif
#if USE_WEB_REQUEST
using UnityEngine.Networking;
#endif
using BrainCloud.JsonFx.Json;

/// <summary>
/// S2S service for brainCloud Global File V3 operations.
///
/// Access via BrainCloudS2S.GlobalFileV3 after calling Init().
///
/// File upload is a two-step process:
///   1. SYS_PREPARE_UPLOAD is sent via the S2S dispatcher and returns an uploadId + uploadUrl.
///   2. The file bytes are POSTed as multipart/form-data to the upload endpoint.
///      All metadata (gameId, uploadId, etc.) travels as URL query parameters; the server
///      reads them via request.getParameter() and expects only the file bytes in the
///      multipart "file" field.
/// </summary>
public class BrainCloudS2SGlobalFileV3
{
    private static string DEFAULT_S2S_UPLOAD_URL = "https://api.braincloudservers.com/s2suploader/globalfile/upload";

    public string UploadURL { get; private set; }

    private BrainCloudS2S _s2s;
    private ArrayList _fileUploadQueue = new ArrayList();

#if DOT_NET
    private static readonly HttpClient _httpClient = new HttpClient();
#endif

    private struct S2SFileUploadRequest
    {
#if DOT_NET
        public Task<string> task;
#endif
#if USE_WEB_REQUEST
        public UnityWebRequest request;
#endif
        public BrainCloudS2S.S2SCallback callback;
    }

    public BrainCloudS2SGlobalFileV3(BrainCloudS2S s2s)
    {
        _s2s = s2s;
    }

    /// <summary>
    /// Derives the upload endpoint URL from the S2S dispatcher URL set during Init().
    /// Called automatically by BrainCloudS2S.Init().
    /// </summary>
    public void Init(string serverUrl)
    {
        UploadURL = serverUrl.Contains("s2sdispatcher")
            ? serverUrl.Replace("s2sdispatcher", "s2suploader/globalfile/upload")
            : DEFAULT_S2S_UPLOAD_URL;
    }

    // -----------------------------------------------------------------------
    // File Info / Query
    // -----------------------------------------------------------------------

    /// <summary>Returns metadata for a global file identified by its fileId.</summary>
    public void SysGetFileInfo(string fileId, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_GET_FILE_INFO" },
            { "data", new Dictionary<string, object> {
                { "fileId", fileId }
            }}
        }, callback);
    }

    /// <summary>Returns metadata for a global file identified by folder path and filename.</summary>
    public void SysGetFileInfoSimple(string folderPath, string filename, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_GET_FILE_INFO_SIMPLE" },
            { "data", new Dictionary<string, object> {
                { "folderPath", folderPath },
                { "filename", filename }
            }}
        }, callback);
    }

    /// <summary>Returns true if a file with the given name exists in the specified folder.</summary>
    public void SysCheckFilenameExists(string folderPath, string filename, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_CHECK_FILENAME_EXISTS" },
            { "data", new Dictionary<string, object> {
                { "folderPath", folderPath },
                { "filename", filename }
            }}
        }, callback);
    }

    /// <summary>Returns true if a file exists at the given full path (e.g. "/folder/sub/file.txt").</summary>
    public void SysCheckFullpathFilenameExists(string fullpathFilename, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_CHECK_FULLPATH_FILENAME_EXISTS" },
            { "data", new Dictionary<string, object> {
                { "fullPathFilename", fullpathFilename }
            }}
        }, callback);
    }

    /// <summary>Returns the CDN URL for the global file identified by fileId.</summary>
    public void SysGetGlobalCDNUrl(string fileId, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_GET_GLOBAL_CDN_URL" },
            { "data", new Dictionary<string, object> {
                { "fileId", fileId }
            }}
        }, callback);
    }

    /// <summary>
    /// Lists all global files under the given folder path.
    /// Pass folderPath="" and recurse=true to list the entire tree.
    /// </summary>
    public void SysGetGlobalFileList(string folderPath, bool recurse, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_GET_GLOBAL_FILE_LIST" },
            { "data", new Dictionary<string, object> {
                { "folderPath", folderPath },
                { "recurse", recurse }
            }}
        }, callback);
    }

    // -----------------------------------------------------------------------
    // File Management
    // -----------------------------------------------------------------------

    /// <summary>
    /// Moves a file from a user's personal cloud storage into the global file system.
    /// </summary>
    public void SysMoveToGlobalFile(string userProfileId, string userCloudPath, string userCloudFilename,
        string globalTreeId, string globalFilename, bool overwriteIfPresent, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_MOVE_TO_GLOBAL_FILE" },
            { "data", new Dictionary<string, object> {
                { "userProfileId", userProfileId },
                { "userCloudPath", userCloudPath },
                { "userCloudFilename", userCloudFilename },
                { "globalTreeId", globalTreeId },
                { "globalFilename", globalFilename },
                { "overwriteIfPresent", overwriteIfPresent }
            }}
        }, callback);
    }

    /// <summary>Copies a global file to another folder, optionally with a new name.</summary>
    /// <param name="version">Pass -1 to copy the latest version.</param>
    /// <param name="treeVersion">Current version of the destination tree; pass -1 to skip version check.</param>
    public void SysCopyGlobalFile(string fileId, int version, string newTreeId, int treeVersion,
        string newFilename, bool overwriteIfPresent, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_COPY_GLOBAL_FILE" },
            { "data", new Dictionary<string, object> {
                { "fileId", fileId },
                { "version", version },
                { "newTreeId", newTreeId },
                { "treeVersion", treeVersion },
                { "newFilename", newFilename },
                { "overwriteIfPresent", overwriteIfPresent }
            }}
        }, callback);
    }

    /// <summary>Moves a global file to another folder, optionally with a new name.</summary>
    /// <param name="version">Pass -1 to move the latest version.</param>
    /// <param name="treeVersion">Current version of the destination tree; pass -1 to skip version check.</param>
    public void SysMoveGlobalFile(string fileId, int version, string newTreeId, int treeVersion,
        string newFilename, bool overwriteIfPresent, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_MOVE_GLOBAL_FILE" },
            { "data", new Dictionary<string, object> {
                { "fileId", fileId },
                { "version", version },
                { "newTreeId", newTreeId },
                { "treeVersion", treeVersion },
                { "newFilename", newFilename },
                { "overwriteIfPresent", overwriteIfPresent }
            }}
        }, callback);
    }

    /// <summary>Deletes a single global file. Pass version=-1 to delete without a version check.</summary>
    public void SysDeleteGlobalFile(string fileId, int version, string filename, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_DELETE_GLOBAL_FILE" },
            { "data", new Dictionary<string, object> {
                { "fileId", fileId },
                { "version", version },
                { "filename", filename }
            }}
        }, callback);
    }

    /// <summary>
    /// Deletes all global files in the specified folder.
    /// Set recurse=true to also delete files in sub-folders.
    /// </summary>
    /// <param name="treeVersion">Current tree version; pass -1 to skip version check.</param>
    public void SysDeleteGlobalFiles(string treeId, string folderPath, int treeVersion, bool recurse,
        BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_DELETE_GLOBAL_FILES" },
            { "data", new Dictionary<string, object> {
                { "treeId", treeId },
                { "folderPath", folderPath },
                { "treeVersion", treeVersion },
                { "recurse", recurse }
            }}
        }, callback);
    }

    // -----------------------------------------------------------------------
    // Folder Management
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a new folder at the given path.
    /// Set createInterimDirectories=true to auto-create any missing parent folders.
    /// </summary>
    /// <param name="treeVersion">Current tree version; pass -1 to skip version check.</param>
    public void SysCreateFolder(string folderPath, int treeVersion, string name, string desc,
        bool createInterimDirectories, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_CREATE_FOLDER" },
            { "data", new Dictionary<string, object> {
                { "folderPath", folderPath },
                { "treeVersion", treeVersion },
                { "name", name },
                { "desc", desc },
                { "createInterimDirectories", createInterimDirectories }
            }}
        }, callback);
    }

    /// <summary>Moves a folder to a new path, optionally renaming it.</summary>
    /// <param name="treeVersion">Current tree version; pass -1 to skip version check.</param>
    public void SysMoveFolder(string treeId, int treeVersion, string newFolderPath, string updatedName,
        bool createInterimDirectories, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_MOVE_FOLDER" },
            { "data", new Dictionary<string, object> {
                { "treeId", treeId },
                { "treeVersion", treeVersion },
                { "newFolderPath", newFolderPath },
                { "updatedName", updatedName },
                { "createInterimDirectories", createInterimDirectories }
            }}
        }, callback);
    }

    /// <summary>Renames a folder in place.</summary>
    /// <param name="treeVersion">Current tree version; pass -1 to skip version check.</param>
    public void SysRenameFolder(string treeId, int treeVersion, string updatedName, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_RENAME_FOLDER" },
            { "data", new Dictionary<string, object> {
                { "treeId", treeId },
                { "treeVersion", treeVersion },
                { "updatedName", updatedName }
            }}
        }, callback);
    }

    /// <summary>Resolves the treeId for a folder given its full path (e.g. "/folder/sub/").</summary>
    public void SysLookupFolder(string fullFolderPath, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_LOOKUP_FOLDER" },
            { "data", new Dictionary<string, object> {
                { "fullFolderPath", fullFolderPath }
            }}
        }, callback);
    }

    /// <summary>
    /// Deletes a folder. Set force=true to also delete any files and sub-folders inside it.
    /// </summary>
    /// <param name="treeVersion">Current tree version; pass -1 to skip version check.</param>
    public void SysDeleteFolder(string treeId, string folderPath, int treeVersion, bool force,
        BrainCloudS2S.S2SCallback callback)
    {
        _s2s.Request(new Dictionary<string, object> {
            { "service", "globalFileV3" },
            { "operation", "SYS_DELETE_FOLDER" },
            { "data", new Dictionary<string, object> {
                { "treeId", treeId },
                { "folderPath", folderPath },
                { "treeVersion", treeVersion },
                { "force", force }
            }}
        }, callback);
    }

    // -----------------------------------------------------------------------
    // Upload
    // -----------------------------------------------------------------------

    /// <summary>
    /// Uploads a file to the brainCloud Global File V3 system via S2S.
    ///
    /// Internally performs SYS_PREPARE_UPLOAD to obtain an uploadId, then POSTs the
    /// file bytes to the upload endpoint. The callback receives the upload response
    /// JSON on success, or an error JSON on failure.
    /// </summary>
    /// <param name="treeId">Folder tree ID (e.g. "_root_" for the root folder; use SysLookupFolder to resolve sub-folders)</param>
    /// <param name="filename">Name of the file as it will appear in brainCloud</param>
    /// <param name="overwriteIfPresent">When true, replaces any existing file with the same name</param>
    /// <param name="fileData">File content as a byte array</param>
    /// <param name="callback">Invoked with the upload result JSON string</param>
    public void UploadGlobalFile(string treeId, string filename, bool overwriteIfPresent, byte[] fileData, BrainCloudS2S.S2SCallback callback)
    {
        _s2s.LogString("[GlobalFileV3] Preparing upload: " + filename + " (" + fileData.Length + " bytes) treeId=" + treeId);

        string prepareJson = "{\"service\":\"globalFileV3\",\"operation\":\"SYS_PREPARE_UPLOAD\",\"data\":{" +
            "\"treeId\":\"" + treeId + "\"," +
            "\"filename\":\"" + filename + "\"," +
            "\"overwriteIfPresent\":" + (overwriteIfPresent ? "true" : "false") + "," +
            "\"fileSize\":" + fileData.Length + "}}";

        _s2s.Request(prepareJson, (responseString) =>
        {
            Dictionary<string, object> response = null;
            try
            {
                response = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
            }
            catch (Exception e)
            {
                _s2s.LogString("[GlobalFileV3] Failed to parse SYS_PREPARE_UPLOAD response: " + e.Message);
                callback?.Invoke(responseString);
                return;
            }

            if (response == null || !response.ContainsKey("data"))
            {
                _s2s.LogString("[GlobalFileV3] SYS_PREPARE_UPLOAD failed: " + responseString);
                callback?.Invoke(responseString);
                return;
            }

            var data = (Dictionary<string, object>)response["data"];
            if (!data.ContainsKey("fileDetails"))
            {
                _s2s.LogString("[GlobalFileV3] SYS_PREPARE_UPLOAD missing fileDetails: " + responseString);
                callback?.Invoke(responseString);
                return;
            }

            var fileDetails = (Dictionary<string, object>)data["fileDetails"];
            if (!fileDetails.ContainsKey("uploadId"))
            {
                _s2s.LogString("[GlobalFileV3] SYS_PREPARE_UPLOAD missing uploadId: " + responseString);
                callback?.Invoke(responseString);
                return;
            }

            string uploadId = (string)fileDetails["uploadId"];
            string absoluteUploadUrl = BuildAbsoluteUploadUrl(fileDetails, uploadId);

            _s2s.LogString("[GlobalFileV3] Uploading to: " + absoluteUploadUrl);
            SendFileUpload(absoluteUploadUrl, filename, fileData, callback);
        });
    }

    /// <summary>
    /// Polls in-flight uploads and fires their callbacks when complete.
    /// Called each tick by BrainCloudS2S.RunCallbacks().
    /// </summary>
    public void RunCallbacks()
    {
        for (int i = _fileUploadQueue.Count - 1; i >= 0; i--)
        {
            S2SFileUploadRequest req = (S2SFileUploadRequest)_fileUploadQueue[i];

#if DOT_NET
            if (req.task.IsFaulted || req.task.IsCanceled)
            {
                string errorMsg = req.task.Exception?.GetBaseException().Message ?? "Upload was cancelled";
                _s2s.LogString("[GlobalFileV3] Upload failed: " + errorMsg);

                _fileUploadQueue.RemoveAt(i);
                req.callback?.Invoke("{\"status\":900,\"status_message\":\"" + errorMsg.Replace("\"", "\\\"") + "\"}");

            }
            else if (req.task.IsCompleted)
            {
                _fileUploadQueue.RemoveAt(i);
                _s2s.LogString("[GlobalFileV3] Upload complete. Response: " + req.task.Result);
                req.callback?.Invoke(req.task.Result);
            }
#endif
#if USE_WEB_REQUEST
            if (!string.IsNullOrEmpty(req.request.error))
            {
                _s2s.LogString("[GlobalFileV3] Upload failed: " + req.request.error);
                req.request.Dispose();
                _fileUploadQueue.RemoveAt(i);
                req.callback?.Invoke("{\"status\":900,\"status_message\":\"" + req.request.error + "\"}");
                
            }
            else if (req.request.isDone)
            {
                string responseText = req.request.downloadHandler.text;
                _s2s.LogString("[GlobalFileV3] Upload complete. Response: " + responseText);
                req.request.Dispose();
                _fileUploadQueue.RemoveAt(i);
                
                req.callback?.Invoke(responseText);
                
            }
#endif
        }
    }

    /// <summary>
    /// Cancels and clears any pending uploads.
    /// Called automatically by BrainCloudS2S.Disconnect().
    /// </summary>
    public void Disconnect()
    {
        _fileUploadQueue.Clear();
    }

    // Constructs an absolute upload URL from the uploadUrl field in the prepare response.
    // The server returns a relative path with all query params pre-populated; we just need
    // to prefix it with the server's scheme + host.
    private string BuildAbsoluteUploadUrl(Dictionary<string, object> fileDetails, string uploadId)
    {
        if (fileDetails.ContainsKey("uploadUrl"))
        {
            string relativeUrl = (string)fileDetails["uploadUrl"];
            if (relativeUrl.StartsWith("http"))
                return relativeUrl;

#if DOT_NET
            Uri baseUri = new Uri(_s2s.ServerURL);
            return baseUri.Scheme + "://" + baseUri.Host + relativeUrl;
#endif
        }

        // Fallback: construct URL manually from our computed UploadURL
        return UploadURL + "?gameId=" + _s2s.AppId + "&uploadId=" + uploadId;
    }

    // Starts the async upload and enqueues it for polling in RunCallbacks().
    // uploadUrl must be absolute with all metadata as query parameters.
    // Only the file bytes travel as multipart form data under the "file" field.
    private void SendFileUpload(string uploadUrl, string filename, byte[] fileData, BrainCloudS2S.S2SCallback callback)
    {
        S2SFileUploadRequest req = new S2SFileUploadRequest();
        req.callback = callback;

#if DOT_NET
        req.task = DoFileUploadAsync(uploadUrl, filename, fileData);
#endif
#if USE_WEB_REQUEST
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", fileData, filename, "application/octet-stream"));
        req.request = UnityWebRequest.Post(uploadUrl, formData);
        req.request.SendWebRequest();
#endif

        _fileUploadQueue.Add(req);
    }

#if DOT_NET
    private async Task<string> DoFileUploadAsync(string uploadUrl, string filename, byte[] fileData)
    {
        try
        {
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileData);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", filename);

            HttpResponseMessage response = await _httpClient.PostAsync(uploadUrl, content);
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            return "{\"status\":900,\"status_message\":\"File upload failed: " + e.Message.Replace("\"", "\\\"") + "\"}";
        }
    }
#endif
}
