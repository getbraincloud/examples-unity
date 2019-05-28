using Gameframework;
using UnityEngine;
using UnityEngine.UI;

namespace Gameframework
{
    public class ImageGenericMessageSubState : GenericMessageSubState
    {
        public new static string STATE_NAME = "imageGenericMessageSubState";

        public Image UpperImage = null;

        #region static pushstate helpers
        public static void PushImageGenericMessageSubStateHelper(string title, string message, string in_leftButtonTxt,
            OnDialogAction onLeftAction = null, string in_rightButtonTxt = "", OnDialogAction onRightAction = null,
            eButtonColors mainColor = eButtonColors.GREEN, eButtonColors secondColor = eButtonColors.GREEN,
            bool in_showCloseButton = false, OnDialogAction onCloseAction = null, Image in_image = null)
        {
            GStateManager.InitializeDelegate init = null;
            init = (BaseState state) =>
            {
                GStateManager.Instance.OnInitializeDelegate -= init;
                var messageSubState = state as ImageGenericMessageSubState;
                if (messageSubState != null)
                {
                    Canvas canvas = state.GetComponentInChildren<Canvas>();
                    canvas.sortingOrder = HudHelper.GLOBAL_MSG_SORTING_ORDER;
                    messageSubState.LateInit(title, message, onLeftAction, in_leftButtonTxt, onRightAction, in_rightButtonTxt, in_showCloseButton, onCloseAction, in_image);
                    messageSubState.SetButtonColors(mainColor, secondColor);
                }
            };

            GStateManager.Instance.OnInitializeDelegate += init;
            GStateManager.Instance.PushSubState(STATE_NAME);
        }
        #endregion

        #region Init
        protected override void Start()
        {
            _stateInfo = new StateInfo(getStateName(), this);
            base.Start();
        }

        protected override string getStateName()
        {
            return STATE_NAME;
        }

        public void LateInit(string in_title, string in_message, OnDialogAction in_leftDialogAction, string in_leftTxtButton, OnDialogAction in_rightDialogAction, string in_rightTxtButton, bool in_showCloseButton, OnDialogAction in_closeDialog, Image in_image = null)
        {
            base.LateInit(in_title, in_message, in_leftDialogAction, in_leftTxtButton, in_rightDialogAction, in_rightTxtButton, in_showCloseButton, in_closeDialog);
            SetImage(in_image);
        }
        #endregion

        #region Public 
        public void SetImage(Image in_image)
        {
            if (in_image != null)
                UpperImage = in_image;
        }
        #endregion
    }
}
