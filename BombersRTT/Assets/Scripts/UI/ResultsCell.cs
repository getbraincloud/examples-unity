using Gameframework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudUNETExample
{
    public class ResultsCell : BaseBehaviour
    {
        [SerializeField]
        private Image PlayAgainIcon = null;
        [SerializeField]
        private Image QuitIcon = null;
        [SerializeField]
        private TextMeshProUGUI Name = null;
        [SerializeField]
        private TextMeshProUGUI KDRatio = null;
        [SerializeField]
        private TextMeshProUGUI Score = null;
        [SerializeField]
        private TextMeshProUGUI Ping = null;

        public void UpdateDisplay(ResultsData in_data)
        {
            Name.text = in_data._name;
            KDRatio.text = in_data._kdRatio;
            Score.text = in_data._scoreDisplay;
            Ping.text = in_data._pingDisplay;

            PlayAgainIcon.gameObject.SetActive(in_data._displayConfirmPlayAgainAction && in_data._confirmedPlayAgainAction);
            QuitIcon.gameObject.SetActive(in_data._displayConfirmPlayAgainAction && !in_data._confirmedPlayAgainAction);
        }
    }

    #region public ResultsData stuct
    public struct ResultsData
    {
        public ResultsData(string in_name, string in_kdRatio, string in_scoreDisplay, string in_pingDisplay,
                            bool in_displayConfirmPlayAgainAction = false, bool in_confirmedPlayAgainAction = false)
        {
            _name = in_name;
            _kdRatio = in_kdRatio;
            _scoreDisplay = in_scoreDisplay;
            _pingDisplay = in_pingDisplay;

            _displayConfirmPlayAgainAction = in_displayConfirmPlayAgainAction;
            _confirmedPlayAgainAction = in_confirmedPlayAgainAction;
        }
        public string _name;
        public string _kdRatio;
        public string _scoreDisplay;
        public string _pingDisplay;

        public bool _displayConfirmPlayAgainAction;
        public bool _confirmedPlayAgainAction;
    }
    #endregion

}
