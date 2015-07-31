using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine.UI;
using BrainCloudSlots.Connection;

namespace BrainCloudSlots.RapaNui
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

    public class RapaNuiController : MonoBehaviour
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
            m_slotData = BrainCloudStats.Instance.m_slotsData;
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
            GetComponent<RapaNuiDisplay>().SetPayoutValues(payoutValues);
        }

        IEnumerator SpinAllReels()
        {
            m_allowedToStop = false;
            m_newWinAmount = 0;
            GetComponent<RapaNuiDisplay>().StartSpin();
            
            BrainCloudWrapper.GetBC().ScriptService.RunScript("SpinAndWinRapaNui", "{\"bet\" : " + m_currentBet + ", \"lines\" : " + m_currentLines + ", \"debug\": \""+ m_debugMode+ "\"}", GetSlotsResponse, SlotsFail, null);
            yield return null;
            
        }

        private bool m_isAuto = false;

        public void StopAutoPlay()
        {
            m_isAuto = false;
            GetComponent<RapaNuiDisplay>().m_isAuto = false;
            m_autoSpinToggle.isOn = false;
        }

        IEnumerator AutoPlay(int aSpins)
        {
            int count = 0;
            int totalWin = 0;
            while (count < aSpins)
            {
                count++;
                GetComponent<RapaNuiDisplay>().m_autoSpinsLeft = aSpins-count;
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
            GetComponent<RapaNuiDisplay>().m_spinButton.image.sprite = GetComponent<RapaNuiDisplay>().m_spinButtonSprite;
            GameObject.Find("DialogDisplay").GetComponent<RapaNuiDialogDisplay>().CreateBigWinDialog(count + " spins: " + totalWin.ToString("C0", new System.Globalization.CultureInfo("en-us")), 5);
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
            GetComponent<RapaNuiDisplay>().m_debugMode = m_debugMode;
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
            GetComponent<RapaNuiDisplay>().ReturnToLobby();
        }

        public void GetSlotsResponse(string responseData, object cbObject)
        {
            GetComponent<RapaNuiDisplay>().DataReceived();
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
            GetComponent<RapaNuiDisplay>().m_balance = int.Parse(response["newBalanceBeforeWin"].ToString());
            m_newBalance = int.Parse(response["newBalanceAfterWin"].ToString());
            BrainCloudStats.Instance.m_credits = m_newBalance;
            BrainCloudStats.Instance.m_progressiveJackpot = int.Parse(response["updatedProgressiveJackpot"].ToString());
            GetComponent<RapaNuiDisplay>().m_progressiveJackpot = int.Parse(response["updatedProgressiveJackpot"].ToString());
            if (m_debugMode)
            {
                GetComponent<RapaNuiDisplay>().m_debugNumbers = new int[] { int.Parse(response["reel1Roll"].ToString()), int.Parse(response["reel2Roll"].ToString()), int.Parse(response["reel3Roll"].ToString()), int.Parse(response["reel4Roll"].ToString()), int.Parse(response["reel5Roll"].ToString()) };
            }
            else
            {
                GetComponent<RapaNuiDisplay>().m_debugNumbers = new int[] { 0, 0, 0, 0, 0 };
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
                GetComponent<RapaNuiDisplay>().FlashLine(m_newWinLines[i]);
            }
            m_newWinLines = new int[0];
            GetComponent<RapaNuiDisplay>().SetLastWin(winAmount);
            GetComponent<RapaNuiDisplay>().m_balance = m_newBalance;
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
                GetComponent<RapaNuiDisplay>().StopAllReels();
                return;
            }

            if (m_autoSpinToggle.isOn)
            {
                int autoSpins = int.Parse(m_autoSpinInput.text.ToString());
                if (autoSpins <= 0) autoSpins = 1;
                GetComponent<RapaNuiDisplay>().m_autoSpinsLeft = autoSpins;
                m_isAuto = true;
                GetComponent<RapaNuiDisplay>().m_isAuto = true;
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
            GetComponent<RapaNuiDisplay>().SetLines(m_currentLines);
        }

        public void DecreaseLines()
        {
            if (m_isSpinning || m_isAuto) return;
            if (m_currentLines > 1) m_currentLines--;
            GetComponent<RapaNuiDisplay>().SetLines(m_currentLines);
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
            GetComponent<RapaNuiDisplay>().SetBet(m_currentBet);
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
            GetComponent<RapaNuiDisplay>().SetBet(m_currentBet);
        }

        public void BetMax()
        {
            if (m_isSpinning) return;
            m_currentLines = 25;
            GetComponent<RapaNuiDisplay>().SetLines(m_currentLines);
        }

        public void ShowPayTable()
        {
            m_payTableWindow.SetActive(true);
        }

        public void HidePayTable()
        {
            m_payTableWindow.SetActive(false);
        }

        /*static Dictionary<int, int[]> s_payoutLines = new Dictionary<int, int[]>()
        {
            {0, new int[]{ 5, 6, 7, 8, 9 }},
            {1, new int[]{ 0, 1, 2, 3, 4 }},
            {2, new int[]{ 10,11,12,13,14}},
            {3, new int[]{ 0, 6, 12,8, 4 }},
            {4, new int[]{ 10,6, 2, 8, 14}},
            {5, new int[]{ 5, 1, 2, 3, 9 }},
            {6, new int[]{ 5, 11,12,13,9 }},
            {7, new int[]{ 0, 1, 7, 13,14}},
            {8, new int[]{ 10,11,7, 3, 4 }},
            {9, new int[]{ 5, 11,7, 3, 9 }},
            {10, new int[]{5, 1, 7, 13,9 }},
            {11, new int[]{0, 6, 7, 8, 4 }},
            {12, new int[]{10,6, 7, 8, 14}},
            {13, new int[]{0, 6, 2, 8, 4 }},
            {14, new int[]{10,6, 12,8, 14}},
            {15, new int[]{5, 6, 2, 8, 9 }},
            {16, new int[]{5, 6, 12,8, 9 }},
            {17, new int[]{0, 1, 12,3, 4 }},
            {18, new int[]{10,11,2, 13,14}},
            {19, new int[]{0, 11,12,13,4 }},
            {20, new int[]{10,1, 2, 3, 14}},
            {21, new int[]{5, 11,2, 13,9 }},
            {22, new int[]{5, 1, 12,3, 9 }},
            {23, new int[]{0, 11,2, 13,4 }},
            {24, new int[]{10,1, 12,3, 14}}
        };*/



        /*public int GetWin(int aBet, int aLines = 1)
        {
            SlotSymbol[] lines = new SlotSymbol[15];
            lines[0] = m_displayedSymbols[0][1];
            lines[5] = m_displayedSymbols[0][2];
            lines[10] = m_displayedSymbols[0][3];
            lines[1] = m_displayedSymbols[1][1];
            lines[6] = m_displayedSymbols[1][2];
            lines[11] = m_displayedSymbols[1][3];
            lines[2] = m_displayedSymbols[2][1];
            lines[7] = m_displayedSymbols[2][2];
            lines[12] = m_displayedSymbols[2][3];
            lines[3] = m_displayedSymbols[3][1];
            lines[8] = m_displayedSymbols[3][2];
            lines[13] = m_displayedSymbols[3][3];
            lines[4] = m_displayedSymbols[4][1];
            lines[9] = m_displayedSymbols[4][2];
            lines[14] = m_displayedSymbols[4][3];

            int totalWin = 0;

            for (int i = 0; i < aLines; i++)
            {
                int[] line = s_payoutLines[i];
                SlotSymbol[] symbolLine = new SlotSymbol[5];
                for (int f = 0; f < 5; f++)
                {
                    symbolLine[f] = lines[line[f]];
                }
                int lineWin = GetLineWin(aBet, symbolLine);
                if (lineWin != 0)
                {
                    GetComponent<RapaNuiDisplay>().FlashLine(i);
                }
                totalWin += lineWin;
            }

            return totalWin;
        }*/

        /*int GetLineWin(int aBet, SlotSymbol[] aLineIn)
        {
            SlotSymbol[] aLine = aLineIn;
            int winAmount = 0;


            if ((aLine[0].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[1].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[1].m_value != aLine[0].m_value)
                || (aLine[0].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT && aLine[1].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT) || (aLine[1].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT && aLine[0].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT))
            {
                return 0;//first and second numbers don't match, or one is a jackpot and one isn't
            }
            else if ((aLine[1].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[2].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[1].m_value != aLine[2].m_value)
                || (aLine[1].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT && aLine[2].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT))
            {
                return 0;//second and third numbers don't match, or one is a jackpot and one isn't
            }
            else if ((aLine[0].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[2].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[0].m_value != aLine[2].m_value)
                || (aLine[0].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT && aLine[2].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT))
            {
                return 0;//first and third numbers don't match (second was wild), or one is a jackpot and one isn't
            }

            int numSymbols = 3;


            if (aLine[0].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
            {
                if (aLine[3].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT)
                {
                    //do nothing, jackpots don't go with wilds
                }
                else if (aLine[3] == aLine[1])
                {
                    numSymbols++;
                    if (aLine[4] == aLine[1])
                    {
                        numSymbols++;
                    }
                    else if (aLine[1].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[4] == aLine[2])
                    {
                        numSymbols++;
                    }
                    else if (aLine[1].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[2].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[4] == aLine[3])
                    {
                        numSymbols++;
                    }
                    else if (aLine[1].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[2].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[4].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        numSymbols++;
                    }
                    else if (aLine[1].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[2].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[3].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        numSymbols++;
                    }
                }
                else if (aLine[1].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[3] == aLine[2])
                {
                    numSymbols++;
                    if (aLine[4] == aLine[2])
                    {
                        numSymbols++;
                    }
                    else if (aLine[2].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[4] == aLine[3])
                    {
                        numSymbols++;
                    }
                    else if (aLine[4].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        numSymbols++;
                    }
                    else if (aLine[3].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        numSymbols++;
                    }
                }
                else if (aLine[1].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[3].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[1].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT)
                {
                    numSymbols++;
                    if (aLine[4] == aLine[1] || aLine[4].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        numSymbols++;
                    }

                }
                else if (aLine[1].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[2].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[3].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT)
                {
                    numSymbols++;
                    if (aLine[4] == aLine[3])
                    {
                        numSymbols++;
                    }
                    else if (aLine[4].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        numSymbols++;
                    }
                    else if (aLine[3].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        numSymbols++;
                    }
                }
            }
            else
            {
                if (aLine[3] == aLine[0] || (aLine[3].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[0].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT))
                {
                    numSymbols++;
                    if (aLine[4] == aLine[0] || (aLine[4].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD && aLine[0].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT))
                    {
                        numSymbols++;
                    }
                }
            }

            SlotSymbol bestSymbol = new SlotSymbol(SlotSymbol.eSymbolType.SYMBOL_TYPE_NONE, 0);

            if (aLine[0].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT)
            {
                winAmount = -1 * numSymbols;
            }
            else if (aLine[0].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
            {
                bool symbolFound = false;
                for (int i = 0; i < numSymbols; i++)
                {
                    if (aLine[i].m_symbolType != SlotSymbol.eSymbolType.SYMBOL_TYPE_WILD)
                    {
                        if (aLine[i].m_value > bestSymbol.m_value)
                        {
                            symbolFound = true;
                            bestSymbol = aLine[i];
                        }
                    }
                }
                if (symbolFound)
                {
                    if (aLine[0].m_symbolType == SlotSymbol.eSymbolType.SYMBOL_TYPE_JACKPOT)
                    {
                        winAmount = -1 * numSymbols;
                    }
                    else
                    {
                        winAmount = m_symbolPayouts[bestSymbol][numSymbols];
                    }
                }
                else
                {
                    bestSymbol = SlotSymbol.s_bestSymbol;
                    winAmount = m_symbolPayouts[bestSymbol][numSymbols];
                }
            }
            else
            {
                winAmount = m_symbolPayouts[aLine[0]][numSymbols];
            }
            
            return winAmount * aBet;
        }
         */
    }
}