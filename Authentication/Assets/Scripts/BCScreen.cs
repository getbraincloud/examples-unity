using UnityEngine;
using System.Collections;
using BrainCloud.LitJson;
using System.Text;


public abstract class BCScreen : MonoBehaviour
{
    protected BrainCloudWrapper _bc;

    protected MainScene.eBCFunctionType bcFuncType; 

    public string helpMessage { get; protected set; }
    public string helpURL { get; protected set; }

    public void SetFunctionType(MainScene.eBCFunctionType type)
    {
        bcFuncType = type; 
    }

    public MainScene.eBCFunctionType GetFunctionType()
    {
        return bcFuncType; 
    }

    public virtual void Activate() { }

    protected virtual void OnDisable() { }
       
}
