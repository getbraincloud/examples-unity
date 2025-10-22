using System;
using UnityEngine;

public interface ICameraController {
    /// <summary>
    /// Ran by the GameManager. Call GameManager.GetCameraControl to get execution.
    /// </summary>
    /// <param name="cam">The scene camera.</param>
    /// <returns>Return true to continue controlling the camera, false to release it.</returns>
    bool ControlCamera(Camera cam);
}