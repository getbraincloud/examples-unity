using UnityEngine;
using System.Collections;
using BrainCloud.LitJson;
using System.Text;


public abstract class BCScreen : MonoBehaviour
{
    protected BrainCloudWrapper _bc;

    protected BCFuncScreenHandler.eBCFunctionType bcFuncType; 

    public string HelpMessage { get; protected set; }
    public string HelpURL { get; protected set; }

    public void SetFunctionType(BCFuncScreenHandler.eBCFunctionType type)
    {
        bcFuncType = type; 
    }

    public BCFuncScreenHandler.eBCFunctionType GetFunctionType()
    {
        return bcFuncType; 
    }

    public virtual void Activate() { }

    protected virtual void OnDisable() { }
       
}
