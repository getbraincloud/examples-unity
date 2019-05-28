using BrainCloud;
using UnityEngine.UI;

namespace Gameframework
{
    public class IAPMessageSubState : BaseSubState
    {
        public static string STATE_NAME = "IAPMessageSubState";

        #region BaseState
        // Use this for initialization
        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();
        }
        #endregion

        #region Public
        public void LateInit(IAPProduct in_product, SuccessCallback in_success = null, FailureCallback in_fail = null)
        {
            m_product = in_product;
            m_success = in_success;
            m_fail = in_fail;
        }

        public void OnSuccessAction()
        {
#if DEBUG_IAP_ENABLED
            GIAPManager.Instance.ForceSuccess(m_product, m_success, m_fail);
#endif
            GStateManager.Instance.PopSubState(_stateInfo);
        }

        public void OnFailAction()
        {
            if (m_fail != null)
            {
                m_fail.Invoke(-1, -1, "{'reason':'You're testing a fail scenario on the purchase' }", m_product);
            }
            GStateManager.Instance.PopSubState(_stateInfo);
        }

        public void OnRealAction()
        {
            GIAPManager.Instance.BuyProductID(m_product.BrainCloudProductID, m_success, m_fail, true);
            GStateManager.Instance.PopSubState(_stateInfo);
        }

        public void OnCloseDialog()
        {
            GStateManager.Instance.PopSubState(_stateInfo);
        }
        #endregion

        #region Private
        private IAPProduct m_product;
        private SuccessCallback m_success = null;
        private FailureCallback m_fail = null;
        #endregion
    }
}
