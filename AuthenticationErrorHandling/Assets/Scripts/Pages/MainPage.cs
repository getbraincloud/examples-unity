using UnityEngine;

/*
 * Main page for this Error Handling Example App
 */

public class MainPage : MonoBehaviour
{
	public static string lastErrorMessage = "";

    public ScreenNameSection m_screenNameSection = new ScreenNameSection();

    public LoginValuesSection m_loginValueSection = new LoginValuesSection();


    public WrapperInformationSection m_wrapperInformationSection = new WrapperInformationSection();
    public WrapperIdValuesSection m_wrapperIdValuesSection = new WrapperIdValuesSection();
    public WrapperLoginSection m_wrapperLoginSection = new WrapperLoginSection();

    public InformationSection m_informationSection = new InformationSection();
    public IdValuesSection m_idValuesSection = new IdValuesSection();
    public LoginSection m_loginSection = new LoginSection();
    public AttachIdenititySection m_attachIdentitySection = new AttachIdenititySection();
    public DetachIdenititySection m_detachIdentitySection = new DetachIdenititySection();
    public MergeIdenititySection m_mergeIdentitySection = new MergeIdenititySection();

	BrainCloud.FailureCallback failureCallBack = new BrainCloud.FailureCallback((int status, int reasonCode, string jsonError, object cbObject) => {
		lastErrorMessage = reasonCode.ToString();

	});

	public void FailureCallback(int status, int reasonCode, string jsonError, object cbObject) {
		lastErrorMessage = reasonCode.ToString();
	}


    void Start()
    {
        BrainCloudWrapper.Initialize();
    }

	void OnDestroy() {
		
	}

    void OnGUI()
    {
        GUILayout.BeginArea(SIZE.FullScreen());

        GUI.enabled = !ErrorHandlingApp.getInstance().hasDialog();

        GUILayout.BeginArea(SIZE.Page());


		BrainCloudWrapper.GetBC().RegisterGlobalErrorCallback (failureCallBack);


		GUILayout.BeginVertical("debugInfo", GUI.skin.box);

		GUILayout.Label("---");
		GUILayout.FlexibleSpace();

		if (!lastErrorMessage.Equals ("")) {
			GUILayout.Label("Last Error Code: " + lastErrorMessage, GUI.skin.box);

		}


        m_loginValueSection.Display();

        GUILayout.EndVertical();

        GUILayout.BeginVertical("brainCloudWrapper", GUI.skin.box);
        GUILayout.Label("---");
        GUILayout.FlexibleSpace();
        m_wrapperInformationSection.Display();
        m_wrapperIdValuesSection.Display();
        m_wrapperLoginSection.Display();
        GUILayout.EndVertical();

        GUILayout.BeginVertical("brainCloud", GUI.skin.box);
        GUILayout.Label("---");
        GUILayout.FlexibleSpace();
        m_informationSection.Display();
        m_idValuesSection.Display();
        m_loginSection.Display();
        m_attachIdentitySection.Display();
        m_detachIdentitySection.Display();
        m_mergeIdentitySection.Display();
        m_screenNameSection.Display();
        GUILayout.EndVertical();

        GUILayout.EndArea();

        GUILayout.EndArea();
    }
	    GUI.enabled = true;


}