using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine.UI;
using BrainCloudSlots.Connection;

namespace BrainCloudSlots.Eggies
{
    public class Reel
    {
        public Dictionary<int, int> m_positionMap;
        public List<SlotSymbol> m_symbols;

        public Reel(List<SlotSymbol> aSymbols, Dictionary<int, int> aPositionMap)
        {
            m_positionMap = aPositionMap;
            m_symbols = aSymbols;
        }

        public int GetRandomPosition()
        {
            return Random.Range(1, m_positionMap.Count + 1);
        }

        public List<SlotSymbol> GetDisplayedSymbols(int aPosition)
        {
            List<SlotSymbol> returnSymbols = new List<SlotSymbol>();
            //int pos = m_positionMap[aPosition];
            int pos = aPosition;
            //returnSymbols.Add(m_symbols[aPosition-2]);

            if (pos == 0)
            {
                returnSymbols.Add(m_symbols[m_symbols.Count - 2]);
            }
            else if (pos == 1)
            {
                returnSymbols.Add(m_symbols[m_symbols.Count - 1]);
            }
            else
            {
                returnSymbols.Add(m_symbols[pos - 2]);
            }

            if (pos == 0)
            {
                returnSymbols.Add(m_symbols[m_symbols.Count - 1]);
            }
            else
            {
                returnSymbols.Add(m_symbols[pos - 1]);
            }

            returnSymbols.Add(m_symbols[pos]);

            if (pos == m_symbols.Count - 1)
            {
                returnSymbols.Add(m_symbols[0]);
            }
            else
            {
                returnSymbols.Add(m_symbols[pos + 1]);
            }

            if (pos == m_symbols.Count - 1)
            {
                returnSymbols.Add(m_symbols[1]);
            }
            else if (pos == m_symbols.Count - 2)
            {
                returnSymbols.Add(m_symbols[0]);
            }
            else
            {
                returnSymbols.Add(m_symbols[pos + 2]);
            }

            return returnSymbols;
        }
    }

    public class SlotSymbol
    {
        public static SlotSymbol s_bestSymbol = null;
        public enum eSymbolType
        {
            SYMBOL_TYPE_NONE,
            SYMBOL_TYPE_NORMAL,
            SYMBOL_TYPE_WILD,
            SYMBOL_TYPE_JACKPOT
        }

        public eSymbolType m_symbolType = eSymbolType.SYMBOL_TYPE_NONE;
        public int m_value = 0;

        public SlotSymbol(eSymbolType aType, int aValue)
        {
            m_symbolType = aType;
            if (aType == eSymbolType.SYMBOL_TYPE_JACKPOT)
            {
                m_value = -1;
            }
            else if (aType == eSymbolType.SYMBOL_TYPE_WILD)
            {
                m_value = -2;
            }
            else
            {
                m_value = aValue;
            }

            if (s_bestSymbol == null || aType == eSymbolType.SYMBOL_TYPE_NORMAL && aValue > s_bestSymbol.m_value) s_bestSymbol = this;

        }

        public override bool Equals(object obj)
        {
            bool isSameType = (m_symbolType == ((SlotSymbol)obj).m_symbolType);
            bool isSameNumber = (m_value == ((SlotSymbol)obj).m_value);

            if (isSameType)
            {
                if (m_symbolType == eSymbolType.SYMBOL_TYPE_NORMAL)
                {
                    if (isSameNumber)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(SlotSymbol a, SlotSymbol b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || (object)b == null)
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(SlotSymbol a, SlotSymbol b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode() ^ m_symbolType.GetHashCode();
        }

        public override string ToString()
        {
            return (m_symbolType.ToString().Substring(12) + "," + m_value + " ");
        }

    }

    public class EggiesController : MonoBehaviour
    {
        private List<Reel> m_reels;
        private Dictionary<int, List<SlotSymbol>> m_displayedSymbols;
        private JsonData m_slotData;

        private bool m_isSpinning = false;
        private bool m_debugMode = false;


        [SerializeField]
        private GameObject m_payTableWindow;

        Dictionary<SlotSymbol, Dictionary<int, int>> m_symbolPayouts = new Dictionary<SlotSymbol, Dictionary<int, int>>();
        
        void Start()
        {
            //m_slotData = JsonMapper.ToObject(Resources.Load<TextAsset>("RapaNuiReelMap").text);
            m_slotData = BrainCloudStats.Instance.m_slotsDataEggies;
            //BrainCloudWrapper.GetBC().GlobalEntityService.CreateEntity("SlotsData", 0, null, m_slotData.ToJson(), null, null, null);
            int[][] payoutValues = new int[8][];
            payoutValues[0] = new int[3];
            payoutValues[1] = new int[3];
            payoutValues[2] = new int[3];
            payoutValues[3] = new int[3];
            payoutValues[4] = new int[3];
            payoutValues[5] = new int[3];
            payoutValues[6] = new int[3];
            payoutValues[7] = new int[2];
            
            int reels = int.Parse(m_slotData["reels"].ToString());
            m_reels = new List<Reel>();
            m_displayedSymbols = new Dictionary<int, List<SlotSymbol>>();

            JsonData payouts = m_slotData["payouts"];

            for (int i = 0; i < reels; i++)
            {
                List<SlotSymbol> symbols = new List<SlotSymbol>();
                Dictionary<int, int> symbolMap = new Dictionary<int, int>();
                JsonData reelMap = m_slotData["reelMaps"]["reel" + (i + 1)];
                for (int j = 0; j < reelMap["symbols"].Count; j++)
                {
                    switch (reelMap["symbols"][j]["type"].ToString())
                    {
                        case "NORMAL":
                            symbols.Add(new SlotSymbol(SlotSymbol.eSymbolType.SYMBOL_TYPE_NORMAL, int.Parse(reelMap["symbols"][j]["value"].ToString())));
                            break;
                        case "WILD":
                            symbols.Add(new SlotSymbol(SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD, 0));
                            break;
                        case "JACKPOT":
                            symbols.Add(new SlotSymbol(SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT, 0));
                            break;
                    }
                }

                for (int j = 0; j < symbols.Count; j++)
                {
                    int numValues = reelMap["virtualMap"]["symbol" + (j + 1)].Count;
                    List<int> values = new List<int>();
                    for (int k = 0; k < numValues; k++)
                    {
                        values.Add(int.Parse(reelMap["virtualMap"]["symbol" + (j + 1)][k].ToString()));
                    }

                    for (int k = 0; k < values.Count; k++)
                    {
                        symbolMap.Add(values[k], j);
                    }
                }
                Reel reel = new Reel(symbols, symbolMap);
                m_reels.Add(reel);
                m_displayedSymbols.Add(i, reel.GetDisplayedSymbols(i));

            }

            for (int i=1;i <= int.Parse(payouts["symbols"].ToString());i++)
            {
                payoutValues[i - 1][0] = int.Parse(payouts["symbol" + i]["v5"].ToString());
                payoutValues[i - 1][1] = int.Parse(payouts["symbol" + i]["v4"].ToString());
                payoutValues[i - 1][2] = int.Parse(payouts["symbol" + i]["v3"].ToString());
                //m_symbolPayouts.Add(new SlotSymbol(SlotSymbol.eSymbolType.SYMBOL_TYPE_NORMAL, int.Parse(payouts["symbol" + i]["value"].ToString())), new Dictionary<int, int>() { { 3, int.Parse(payouts["symbol" + i]["3"].ToString()) }, { 4, int.Parse(payouts["symbol" + i]["4"].ToString()) }, { 5, int.Parse(payouts["symbol" + i]["5"].ToString()) } });
            }
            payoutValues[7][0] = int.Parse(m_slotData["jackpot4"].ToString());
            payoutValues[7][1] = int.Parse(m_slotData["jackpot3"].ToString());
            GetComponent<EggiesDisplay>().SetPayoutValues(payoutValues);
        }

        IEnumerator SpinAllReels()
        {
            m_allowedToStop = false;
            m_newWinAmount = 0;
            GetComponent<EggiesDisplay>().StartSpin();
            
            BrainCloudWrapper.GetBC().ScriptService.RunScript("SpinAndWinEggies", "{\"bet\" : " + m_currentBet + ", \"lines\" : " + m_currentLines + ", \"debug\": \""+ m_debugMode+ "\"}", GetSlotsResponse, SlotsFail, null);
            yield return null;
            
        }

        private bool m_isAuto = false;

        public void StopAutoPlay()
        {
            m_isAuto = false;
            GetComponent<EggiesDisplay>().m_isAuto = false;
            m_autoSpinToggle.isOn = false;
        }

        IEnumerator AutoPlay(int aSpins)
        {
            int count = 0;
            int totalWin = 0;
            while (count < aSpins)
            {
                count++;
                GetComponent<EggiesDisplay>().m_autoSpinsLeft = aSpins - count;
                m_isSpinning = true;
                StartCoroutine("SpinAllReels");
                while (m_isSpinning)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                totalWin += m_newWinAmount;
                yield return new WaitForSeconds(0.5f);

                if (!m_isAuto)
                {
                    break;
                }
                
            }
            Debug.Log("Auto Play Win Amount:" + totalWin + ", over " + count + " spins");
            StopAutoPlay();
            GetComponent<EggiesDisplay>().m_spinButton.image.sprite = GetComponent<EggiesDisplay>().m_spinButtonSprite;
            GameObject.Find("DialogDisplay").GetComponent<EggiesDialogDisplay>().CreateBigWinDialog(count + " spins: " + totalWin.ToString("C0", new System.Globalization.CultureInfo("en-us")), 5);
            yield return null;
        }

        private int m_currentBet = 5;
        private int m_currentLines = 25;

        public void SlotsFail(int a, int b, string responseData, object cbObject)
        {
            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(responseData);
        }

        public void ToggleDebugMode()
        {
            m_debugMode = !m_debugMode;
            GetComponent<EggiesDisplay>().m_debugMode = m_debugMode;
        }

        [SerializeField]
        private GameObject m_notEnoughCreditsWindow;

        public void HideCreditsWindow()
        {
            m_notEnoughCreditsWindow.SetActive(false);
        }

        public void ShowOffersMenu()
        {
            BrainCloudStats.Instance.m_showOffersPage = true;
            GetComponent<EggiesDisplay>().ReturnToLobby();
        }

        public void GetSlotsResponse(string responseData, object cbObject)
        {
            GetComponent<EggiesDisplay>().DataReceived();
            JsonData response = JsonMapper.ToObject(responseData);
            response = response["data"]["response"];

            if (int.Parse(response["status"].ToString()) == 1)
            {
                Debug.Log("You don't have enough credits!");
                m_notEnoughCreditsWindow.SetActive(true);
                StopCoroutine("AutoPlay");
                return;
            }

            SpinReel(0, int.Parse(response["reel1"]["2"].ToString()));
            SpinReel(1, int.Parse(response["reel2"]["2"].ToString()));
            SpinReel(2, int.Parse(response["reel3"]["2"].ToString()));
            SpinReel(3, int.Parse(response["reel4"]["2"].ToString()));
            SpinReel(4, int.Parse(response["reel5"]["2"].ToString()));

            m_newWinAmount = int.Parse(response["winAmount"].ToString());
            m_newWinLines = new int[response["winLines"].Count];
            for (int i=0;i<response["winLines"].Count;i++)
            {
                m_newWinLines[i] = int.Parse(response["winLines"][i].ToString());
            }
            m_allowedToStop = true;
            GetComponent<EggiesDisplay>().m_balance = int.Parse(response["newBalanceBeforeWin"].ToString());
            m_newBalance = int.Parse(response["newBalanceAfterWin"].ToString());
            BrainCloudStats.Instance.m_credits = m_newBalance;
            BrainCloudStats.Instance.m_progressiveJackpot = int.Parse(response["updatedProgressiveJackpot"].ToString());
            GetComponent<EggiesDisplay>().m_progressiveJackpot = int.Parse(response["updatedProgressiveJackpot"].ToString());
            if (m_debugMode)
            {
                GetComponent<EggiesDisplay>().m_debugNumbers = new int[] { int.Parse(response["reel1Roll"].ToString()), int.Parse(response["reel2Roll"].ToString()), int.Parse(response["reel3Roll"].ToString()), int.Parse(response["reel4Roll"].ToString()), int.Parse(response["reel5Roll"].ToString()) };
            }
            else
            {
                GetComponent<EggiesDisplay>().m_debugNumbers = new int[] { 0, 0, 0, 0, 0 };
            }

            JsonData userData = BrainCloudStats.Instance.m_userData["data"];
            userData["lifetimeWins"] = int.Parse(userData["lifetimeWins"].ToString()) + m_newWinAmount;

            if (m_newWinAmount > int.Parse(userData["biggestWin"]["amount"].ToString()))
            {
                userData["biggestWin"]["amount"] = m_newWinAmount;
                userData["biggestWin"]["date"] = System.DateTime.Now.ToShortDateString();
                userData["biggestWin"]["time"] = System.DateTime.Now.ToShortTimeString();
            }

            BrainCloudStats.Instance.m_userData["data"] = userData;
            BrainCloudWrapper.GetBC().EntityService.UpdateEntity(BrainCloudStats.Instance.m_userData["entityId"].ToString(), "userData", userData.ToJson(), null, -1, ProfileUpdated, ProfileUpdateFailed, null);

        }

        public bool m_allowedToStop = false;
        private int m_newWinAmount = 0;
        private int[] m_newWinLines = new int[0];
        private int m_newBalance = 0;

        void SpinReel(int aReel, int aSpin)
        {
            //int spin = m_reels[aReel].GetRandomPosition();
            m_displayedSymbols[aReel] = m_reels[aReel].GetDisplayedSymbols(aSpin);
        }

        public List<SlotSymbol> GetSymbols(int aReel)
        {
            return m_displayedSymbols[aReel];
        }

        public void FinishSpinning()
        {
            m_isSpinning = false;
            int winAmount = m_newWinAmount;
            
            for (int i = 0; i < m_newWinLines.Length;i++)
            {
                GetComponent<EggiesDisplay>().FlashLine(m_newWinLines[i]);
            }
            m_newWinLines = new int[0];
            GetComponent<EggiesDisplay>().SetLastWin(winAmount);
            GetComponent<EggiesDisplay>().m_balance = m_newBalance;
        }
        
        public void ProfileUpdateFailed(int a, int b, string responseData, object cbObject)
        {
            Debug.Log(a);
            Debug.Log(b);
            Debug.Log(responseData);
        }

        public void ProfileUpdated(string responseData, object cbObject)
        {
            JsonData response = JsonMapper.ToObject(responseData);
            BrainCloudStats.Instance.m_userData = response["data"];
        }

        [SerializeField]
        private Toggle m_autoSpinToggle;
        [SerializeField]
        private InputField m_autoSpinInput;

        public void SpinReels()
        {
            if (m_isSpinning)
            {
                GetComponent<EggiesDisplay>().StopAllReels();
                return;
            }

            if (m_autoSpinToggle.isOn)
            {
                int autoSpins = int.Parse(m_autoSpinInput.text.ToString());
                if (autoSpins <= 0) autoSpins = 1;
                GetComponent<EggiesDisplay>().m_autoSpinsLeft = autoSpins;
                m_isAuto = true;
                GetComponent<EggiesDisplay>().m_isAuto = true;
                StartCoroutine(AutoPlay(autoSpins));
            }
            else
            {
                m_isSpinning = true;
                StartCoroutine("SpinAllReels");
            }
        }

        public void IncreaseLines()
        {
            if (m_isSpinning || m_isAuto) return;
            if (m_currentLines < 25) m_currentLines++;
            GetComponent<EggiesDisplay>().SetLines(m_currentLines);
        }

        public void DecreaseLines()
        {
            if (m_isSpinning || m_isAuto) return;
            if (m_currentLines > 1) m_currentLines--;
            GetComponent<EggiesDisplay>().SetLines(m_currentLines);
        }

        public void IncreaseBet()
        {
            if (m_isSpinning || m_isAuto) return;
            switch (m_currentBet)
            {
                case 5:
                    m_currentBet = 10;
                    break;
                case 10:
                    m_currentBet = 25;
                    break;
                case 25:
                    m_currentBet = 50;
                    break;
                case 50:
                    m_currentBet = 100;
                    break;
                case 100:
                    m_currentBet = 250;
                    break;
            }
            GetComponent<EggiesDisplay>().SetBet(m_currentBet);
        }

        public void DecreaseBet()
        {
            if (m_isSpinning || m_isAuto) return;
            switch (m_currentBet)
            {
                case 10:
                    m_currentBet = 5;
                    break;
                case 25:
                    m_currentBet = 10;
                    break;
                case 50:
                    m_currentBet = 25;
                    break;
                case 100:
                    m_currentBet = 50;
                    break;
                case 250:
                    m_currentBet = 100;
                    break;
            }
            GetComponent<EggiesDisplay>().SetBet(m_currentBet);
        }

        public void BetMax()
        {
            if (m_isSpinning) return;
            m_currentLines = 25;
            GetComponent<EggiesDisplay>().SetLines(m_currentLines);
        }

        public void ShowPayTable()
        {
            m_payTableWindow.SetActive(true);
        }

        public void HidePayTable()
        {
            m_payTableWindow.SetActive(false);
        }
    }
}