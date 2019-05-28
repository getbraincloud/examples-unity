using Gameframework;
namespace BrainCloudUNETExample
{
    public class ControlsSubState : BaseSubState
    {
        public static string STATE_NAME = "controls";

        #region BaseState
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region Public 
        #endregion

        #region Private
        #endregion
    }
}
