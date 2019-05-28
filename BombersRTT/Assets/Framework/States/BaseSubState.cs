namespace Gameframework
{
    public class BaseSubState : BaseState
    {
        #region Public 
        virtual public void ExitSubState()
        {
            GStateManager.Instance.PopSubState(_stateInfo);
        }
        #endregion

        #region Protected
        protected override void OnEnter()
        {
            GStateManager stateMgr = GStateManager.Instance;
            stateMgr.RetrieveInitializedDelegates(this);
            stateMgr.OnEnterNewSubState(_stateInfo);
            if (OnInitializeDelegate != null)
                OnInitializeDelegate(this);
        }
        #endregion
    }
}

