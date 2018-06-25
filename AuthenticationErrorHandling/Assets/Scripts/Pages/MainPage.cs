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

	readonly BrainCloud.FailureCallback failureCallBack = (status, reasonCode, jsonError, cbObject) => {
		lastErrorMessage = reasonCode.ToString();
	};

	private Vector2 scrollPosition = Vector2.zero;
    void OnGUI()
    {
        
	    scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.MinWidth(SIZE.FullScreen().width));

        GUI.enabled = !ErrorHandlingApp.getInstance().hasDialog();

		App.Bc.Client.RegisterGlobalErrorCallback (failureCallBack);


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
	    
	    GUILayout.EndScrollView();
	    
	    GUI.enabled = true;
	 }


}