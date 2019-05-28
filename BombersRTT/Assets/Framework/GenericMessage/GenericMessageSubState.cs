using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Gameframework
{
    public class GenericMessageSubState : BaseSubState
    {
        public static string STATE_NAME = "genericMessageSubState";
        public Text Title = null;
        public Text Message = null;
        public Text TxtButton = null;
        public TextMeshProUGUI TMTitle;
        public TextMeshProUGUI TMMessage;

        public Button FirstButton = null;
        public Button SecondButton = null;
        public Text SecondText = null;

        public Button CloseButton = null;
        public GameObject Btn2Coins = null;
        public GameObject Btn2Purple = null;
        public GameObject Btn2SimBux = null;
        public GameObject InfoBox = null;

        public List<Sprite> ButtonsImg = null;
        public List<Sprite> ButtonsHighlightImg = null;

        public delegate void OnDialogAction();

        public enum eButtonColors
        {
            RED = 0,
            BLUE,
            GREEN,
            BROWN,

            MAX_COLORS
        }

        #region BaseState
        // Use this for initialization
        protected override void Start()
        {
            _stateInfo = new StateInfo(getStateName(), this);
            base.Start();
        }
        protected virtual string getStateName()
        {
            return STATE_NAME;
        }
        #endregion

        #region Public
        public void LateInit(string in_title, string in_message, OnDialogAction in_dialogAction = null, string in_txtButton = "DONE")
        {
            LateInit(in_title, in_message, in_dialogAction, in_txtButton, null, null);
        }

        public void LateInit(string in_title, string in_message, OnDialogAction in_leftDialogAction, string in_leftTxtButton, OnDialogAction in_rightDialogAction, string in_rightTxtButton)
        {
            LateInit(in_title, in_message, in_leftDialogAction, in_leftTxtButton, in_rightDialogAction, in_rightTxtButton, false, null);
        }

        public void LateInit(string in_title, string in_message, OnDialogAction in_leftDialogAction, string in_leftTxtButton, OnDialogAction in_rightDialogAction, string in_rightTxtButton, bool in_showCloseButton, OnDialogAction in_closeDialog)
        {
            InfoBox.SetActive(false);
            TMTitle.text = in_title;
            Message.text = in_message;
            TxtButton.text = in_leftTxtButton;
            m_dialogAction = in_leftDialogAction;

            m_closeDialogAction = in_closeDialog;
            SetAllCurrencyIconsInactive();

            CloseButton.gameObject.SetActive(in_showCloseButton);

            if (in_rightTxtButton != null)
            {
                SecondButton.gameObject.SetActive(true);
                m_secondDialogAction = in_rightDialogAction;
                SecondText.text = in_rightTxtButton;
            }
            else
            {
                SecondButton.gameObject.SetActive(false);
            }
        }

        public void SetInfoBox(string in_infoBoxTxt)
        {
            InfoBox.GetComponentInChildren<Text>().text = in_infoBoxTxt;
            InfoBox.SetActive(true);
        }

        public void SetButtonColors(eButtonColors in_mainColor = eButtonColors.GREEN, eButtonColors in_secondColor = eButtonColors.GREEN)
        {
            Image mainBtn = FirstButton.transform.Find("Button").GetComponentInChildren<Image>();
            mainBtn.sprite = ButtonsImg[(int)in_mainColor];
            Image mainBtnHighlight = mainBtn.transform.Find("Highlight").GetComponentInChildren<Image>();
            mainBtnHighlight.sprite = ButtonsHighlightImg[(int)in_mainColor];

            Image secondBtn = SecondButton.transform.Find("Button").GetComponentInChildren<Image>();
            secondBtn.sprite = ButtonsImg[(int)in_secondColor];
            Image secondBtnHighlight = secondBtn.transform.Find("Highlight").GetComponentInChildren<Image>();
            secondBtnHighlight.sprite = ButtonsHighlightImg[(int)in_secondColor];
        }

        public void OnBottomAction()
        {
            if (m_dialogAction != null)
            {
                m_dialogAction();
            }
            GStateManager.Instance.PopSubState(_stateInfo);
        }

        public void OnSecondButtonAction()
        {
            if (m_secondDialogAction != null)
            {
                m_secondDialogAction();
            }
            GStateManager.Instance.PopSubState(_stateInfo);
        }

        public void OnCloseButtonAction()
        {
            if (m_closeDialogAction != null)
            {
                m_closeDialogAction();
            }
            GStateManager.Instance.PopSubState(_stateInfo);
        }
        #endregion

        #region Private
        private void SetAllCurrencyIconsInactive()
        {
            Btn2Coins.SetActive(false);
            Btn2Purple.SetActive(false);
            Btn2SimBux.SetActive(false);
        }

        private OnDialogAction m_dialogAction = null;
        private OnDialogAction m_secondDialogAction = null;
        private OnDialogAction m_closeDialogAction = null;
        #endregion
    }
}
