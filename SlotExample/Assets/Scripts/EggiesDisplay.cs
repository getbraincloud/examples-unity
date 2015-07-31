using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using BrainCloudSlots.Connection;

namespace BrainCloudSlots.Eggies
{
    public class EggiesDisplay : MonoBehaviour
    {
        Dictionary<int, Sprite> m_slotImages;

        int m_currentLines = 25;
        int m_currentBet = 5;
        int m_lastWin = 0;

        [SerializeField]
        private Text m_linesText;

        [SerializeField]
        private Text m_betText;

        [SerializeField]
        private Text m_totalBetText;

        [SerializeField]
        private Text m_lastWinText;

        [SerializeField]
        private GameObject[] m_winningLines;
        [SerializeField]
        private Text[] m_debugNumbersText;

        [SerializeField]
        private Text[] m_payoutValuesText;

        public int[] m_debugNumbers = new int[] {0,0,0,0,0};

        public bool m_debugMode = false;

        public void SetPayoutValues(int[][] aPayoutValues)
        {
            for (int i=0;i<m_payoutValuesText.Length;i++)
            {
                if (i == 7)
                {
                    m_payoutValuesText[i].text = "\n" + aPayoutValues[i][0] + "\n" + aPayoutValues[i][1];
                }
                else
                {
                    m_payoutValuesText[i].text = aPayoutValues[i][0] + "\n" + aPayoutValues[i][1] + "\n" + aPayoutValues[i][2];
                }
            }
        }

        // Use this for initialization
        void Start()
        {
            m_progressiveJackpot = BrainCloudStats.Instance.m_progressiveJackpot;
            m_balance = BrainCloudStats.Instance.m_credits;
            m_displayedSymbols = new Dictionary<int, List<SlotSymbol>>()
            {
                {0, null},
                {1, null},
                {2, null},
                {3, null},
                {4, null},
            };

            m_slotImages = new Dictionary<int, Sprite>();
            m_slotImages.Add(-1, Resources.Load<Sprite>("EG_Symbol_10"));
            m_slotImages.Add(-2, Resources.Load<Sprite>("EG_Symbol_12"));
            m_slotImages.Add(9,  Resources.Load<Sprite>("EG_Symbol_17"));
            m_slotImages.Add(10, Resources.Load<Sprite>("EG_Symbol_16"));
            m_slotImages.Add(11, Resources.Load<Sprite>("EG_Symbol_15"));
            m_slotImages.Add(12, Resources.Load<Sprite>("EG_Symbol_13"));
            m_slotImages.Add(13, Resources.Load<Sprite>("EG_Symbol_11"));
            m_slotImages.Add(14, Resources.Load<Sprite>("EG_Symbol_14"));
            m_slotImages.Add(15, Resources.Load<Sprite>("EG_Symbol_18"));
        }

        public void FlashLine(int aLine)
        {
            m_winningLines[aLine].GetComponent<Animator>().SetBool("Flash", true);
            m_winningLines[aLine].GetComponent<Animator>().SetBool("StopFlashing", false);
        }

        public void StopFlashingLines()
        {
            for (int i=0;i<m_winningLines.Length;i++)
            {
                m_winningLines[i].GetComponent<Animator>().SetBool("Flash", false);
                m_winningLines[i].GetComponent<Animator>().SetBool("StopFlashing", true);
            }
        }

        Dictionary<int, List<SlotSymbol>> m_displayedSymbols;
        public Text m_playerBalance;
        public Text m_playerName;
        public long m_balance = 0;
        public long m_progressiveJackpot = 0;
        public Text m_progressiveJackpotText;
        // Update is called once per frame
        void Update()
        {
            m_playerName.text = BrainCloudStats.Instance.m_userName;
            m_playerBalance.text = m_balance.ToString("C0", new System.Globalization.CultureInfo("en-us"));
            m_progressiveJackpotText.text = m_progressiveJackpot.ToString("C0", new System.Globalization.CultureInfo("en-us"));
            
            m_displayedSymbols[0] = GetComponent<EggiesController>().GetSymbols(0);
            m_displayedSymbols[1] = GetComponent<EggiesController>().GetSymbols(1);
            m_displayedSymbols[2] = GetComponent<EggiesController>().GetSymbols(2);
            m_displayedSymbols[3] = GetComponent<EggiesController>().GetSymbols(3);
            m_displayedSymbols[4] = GetComponent<EggiesController>().GetSymbols(4);
            GameObject.Find("Reel1").transform.GetChild(0).FindChild("Image0").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[0][0].m_value];
            GameObject.Find("Reel1").transform.GetChild(0).FindChild("Image1").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[0][1].m_value];
            GameObject.Find("Reel1").transform.GetChild(0).FindChild("Image2").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[0][2].m_value];
            GameObject.Find("Reel1").transform.GetChild(0).FindChild("Image3").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[0][3].m_value];
            GameObject.Find("Reel1").transform.GetChild(0).FindChild("Image4").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[0][4].m_value];
            
            GameObject.Find("Reel2").transform.GetChild(0).FindChild("Image0").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[1][0].m_value];
            GameObject.Find("Reel2").transform.GetChild(0).FindChild("Image1").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[1][1].m_value];
            GameObject.Find("Reel2").transform.GetChild(0).FindChild("Image2").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[1][2].m_value];
            GameObject.Find("Reel2").transform.GetChild(0).FindChild("Image3").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[1][3].m_value];
            GameObject.Find("Reel2").transform.GetChild(0).FindChild("Image4").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[1][4].m_value];

            GameObject.Find("Reel3").transform.GetChild(0).FindChild("Image0").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[2][0].m_value];
            GameObject.Find("Reel3").transform.GetChild(0).FindChild("Image1").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[2][1].m_value];
            GameObject.Find("Reel3").transform.GetChild(0).FindChild("Image2").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[2][2].m_value];
            GameObject.Find("Reel3").transform.GetChild(0).FindChild("Image3").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[2][3].m_value];
            GameObject.Find("Reel3").transform.GetChild(0).FindChild("Image4").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[2][4].m_value];

            GameObject.Find("Reel4").transform.GetChild(0).FindChild("Image0").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[3][0].m_value];
            GameObject.Find("Reel4").transform.GetChild(0).FindChild("Image1").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[3][1].m_value];
            GameObject.Find("Reel4").transform.GetChild(0).FindChild("Image2").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[3][2].m_value];
            GameObject.Find("Reel4").transform.GetChild(0).FindChild("Image3").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[3][3].m_value];
            GameObject.Find("Reel4").transform.GetChild(0).FindChild("Image4").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[3][4].m_value];

            GameObject.Find("Reel5").transform.GetChild(0).FindChild("Image0").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[4][0].m_value];
            GameObject.Find("Reel5").transform.GetChild(0).FindChild("Image1").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[4][1].m_value];
            GameObject.Find("Reel5").transform.GetChild(0).FindChild("Image2").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[4][2].m_value];
            GameObject.Find("Reel5").transform.GetChild(0).FindChild("Image3").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[4][3].m_value];
            GameObject.Find("Reel5").transform.GetChild(0).FindChild("Image4").GetComponent<Image>().sprite = m_slotImages[m_displayedSymbols[4][4].m_value];
            
            if (m_debugMode)
            {
                for (int i = 0; i < m_debugNumbersText.Length; i++)
                {
                    m_debugNumbersText[i].text = m_debugNumbers[i].ToString();
                }
            }
            else
            {
                for (int i = 0; i < m_debugNumbersText.Length; i++)
                {
                    m_debugNumbersText[i].text = "";
                }
            }
        }

        [SerializeField]
        private InputField m_autoSpinInput;

        private Toggle m_autoSpinToggle;

        public void ToggleAutoPlay(Toggle aIsOn)
        {
            m_autoSpinToggle = aIsOn;
            if (aIsOn.isOn)
            {
                m_autoSpinInput.interactable = true;
                m_autoSpinInput.text = "99";
            }
            else
            {
                m_autoSpinInput.interactable = false;
                m_autoSpinInput.text = "";
            }
        }

        void LateUpdate()
        {
            if (m_isAuto)
            {
                m_autoSpinInput.interactable = false;                
                m_autoSpinInput.text = m_autoSpinsLeft.ToString();
            }
            else if (m_autoSpinToggle == null)
            {
                m_autoSpinInput.interactable = false;
                m_autoSpinInput.text = "";
            }
            else if (m_autoSpinToggle.isOn)
            {
                m_autoSpinInput.interactable = true;
            }
            else
            {
                m_autoSpinInput.interactable = false;
                m_autoSpinInput.text = "";
            }

            m_linesText.text = m_currentLines.ToString();
            m_betText.text = m_currentBet.ToString("C0", new System.Globalization.CultureInfo("en-us"));
            m_totalBetText.text = (m_currentBet * m_currentLines).ToString("C0", new System.Globalization.CultureInfo("en-us"));
            m_lastWinText.text = m_lastWin.ToString("C0", new System.Globalization.CultureInfo("en-us"));
        }

        public void SpinReel(int aReel)
        {
            GameObject.Find("Reel" + (aReel+1)).transform.FindChild("AnimationPanel").GetComponent<Animation>().Play("Spin");
            GameObject.Find("Reel" + (aReel + 1)).transform.FindChild("SpinAnimation").GetComponent<Animation>().Play("ReelSpin");
        }

        IEnumerator StopSpinning(int aReel, bool aAuto)
        {
            yield return new WaitForSeconds(((aAuto) ? 0 : 1) + (((aAuto) ? 0.17f : 0.3f) * aReel));
            GameObject.Find("Reel" + (aReel + 1)).transform.FindChild("AnimationPanel").GetComponent<Animation>().Play("StopSpinning");
            
            GameObject.Find("Reel" + (aReel + 1)).transform.FindChild("SpinAnimation").GetComponent<Animation>().Play("ReelNotSpin");
            if (aReel == 4)
            {
                if (!aAuto)
                {
                    m_spinButton.image.sprite = m_spinButtonSprite;
                }

                GetComponent<EggiesController>().FinishSpinning();
                
            }
        }

        public bool m_allowedToStop = false;
        public Button m_spinButton;
        public Sprite m_spinButtonSprite;
        public Sprite m_stopButtonSprite;
        public bool m_isAuto = false;
        public int m_autoSpinsLeft = 10;

        public void StartSpin()
        {
            m_spinButton.image.sprite = m_stopButtonSprite;
            m_allowedToStop = false;
            m_spinButton.interactable = false;
            StopFlashingLines();
            SpinReel(0);
            SpinReel(1);
            SpinReel(2);
            SpinReel(3);
            SpinReel(4);
        }

        public void DataReceived()
        {
            StartCoroutine(StopSpinning(0, m_isAuto));
            StartCoroutine(StopSpinning(1, m_isAuto));
            StartCoroutine(StopSpinning(2, m_isAuto));
            StartCoroutine(StopSpinning(3, m_isAuto));
            StartCoroutine(StopSpinning(4, m_isAuto));
            m_spinButton.interactable = true;
            m_allowedToStop = true;
        }

        public void StopAllReels()
        {
            if (!m_allowedToStop) return;

            if (m_isAuto)
            {
                m_isAuto = false;
                GetComponent<EggiesController>().StopAutoPlay();
            }
            m_spinButton.image.sprite = m_spinButtonSprite;
            StopAllCoroutines();
            if (GameObject.Find("Reel1").transform.FindChild("AnimationPanel").GetComponent<Animation>().IsPlaying("Spin"))
            {
                GameObject.Find("Reel1").transform.FindChild("AnimationPanel").GetComponent<Animation>().Play("StopSpinning");
                GameObject.Find("Reel1").transform.FindChild("SpinAnimation").GetComponent<Animation>().Play("ReelNotSpin");
            }
            if (GameObject.Find("Reel2").transform.FindChild("AnimationPanel").GetComponent<Animation>().IsPlaying("Spin"))
            {
                GameObject.Find("Reel2").transform.FindChild("AnimationPanel").GetComponent<Animation>().Play("StopSpinning");
                GameObject.Find("Reel2").transform.FindChild("SpinAnimation").GetComponent<Animation>().Play("ReelNotSpin");
            }
            if (GameObject.Find("Reel3").transform.FindChild("AnimationPanel").GetComponent<Animation>().IsPlaying("Spin"))
            {
                GameObject.Find("Reel3").transform.FindChild("AnimationPanel").GetComponent<Animation>().Play("StopSpinning");
                GameObject.Find("Reel3").transform.FindChild("SpinAnimation").GetComponent<Animation>().Play("ReelNotSpin");
            }
            if (GameObject.Find("Reel4").transform.FindChild("AnimationPanel").GetComponent<Animation>().IsPlaying("Spin"))
            {
                GameObject.Find("Reel4").transform.FindChild("AnimationPanel").GetComponent<Animation>().Play("StopSpinning");
                GameObject.Find("Reel4").transform.FindChild("SpinAnimation").GetComponent<Animation>().Play("ReelNotSpin");
            }
            if (GameObject.Find("Reel5").transform.FindChild("AnimationPanel").GetComponent<Animation>().IsPlaying("Spin"))
            {
                GameObject.Find("Reel5").transform.FindChild("AnimationPanel").GetComponent<Animation>().Play("StopSpinning");
                GameObject.Find("Reel5").transform.FindChild("SpinAnimation").GetComponent<Animation>().Play("ReelNotSpin");
            }

            GetComponent<EggiesController>().FinishSpinning();
        }

        public void ReturnToLobby()
        {
            Application.LoadLevel("MainMenu");
        }

        public void SetLines(int aLines)
        {
            m_currentLines = aLines;
        }

        public void SetBet(int aBet)
        {
            m_currentBet = aBet;
        }

        public void SetLastWin(int aLastWin)
        {
            m_lastWin = aLastWin;

            if (m_isAuto) return;


            if (m_lastWin >= 2 * m_currentBet * m_currentLines)
            {
                GameObject.Find("DialogDisplay").GetComponent<EggiesDialogDisplay>().CreateBigWinDialog(aLastWin.ToString("C0", new System.Globalization.CultureInfo("en-us")), 2.6f);
            }
        }
    }
}