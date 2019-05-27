using Gameframework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrainCloudUNETExample
{
    public class FriendsSubState : BaseSubState
    {
        public static string STATE_NAME = "friends";

        public InputField searchInputField = null;

        public RectTransform FriendsScrollView = null;

        public RectTransform SearchResultsScrollView = null;
        public ScrollRect SearchResultsScrollRect = null;

        [SerializeField]
        private Text NoFriendsAdded = null;
        [SerializeField]
        private Animator NoSearchResults = null;


        #region BaseState
        protected override void OnEnter()
        {
            base.OnEnter();

            m_friendsListItem = new List<FriendsListItem>();
            m_searchResultsListItem = new List<FriendsListItem>();

            m_titleText = transform.FindDeepChild("TitleText").gameObject;

            RemoveAllCellsInView(FriendsScrollView.transform, null);
            NoFriendsAdded.gameObject.SetActive(true);
            NoSearchResults.enabled = false;
            OnRefreshSearchResults();
            OnRefreshFriendsList();
        }

        protected override void Start()
        {
            _stateInfo = new StateInfo(STATE_NAME, this);
            base.Start();
            GEventManager.StartListening(GFriendsManager.ON_FRIENDS_LIST_UPDATED, OnRefreshFriendsList);
            GEventManager.StartListening(GBomberRTTConfigManager.ON_SEARCH_RESULTS_UPDATED, OnRefreshSearchResults);

            GCore.Wrapper.RTTService.RegisterRTTPresenceCallback(OnPresenceCallback);
#if UNITY_WEBGL || UNITY_STANDALONE
            searchInputField.onEndEdit.AddListener(delegate { OnEndEditHelper(); });
#endif
        }

#if UNITY_WEBGL || UNITY_STANDALONE
        private void OnEndEditHelper()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                SearchButton();
        }

        private void Update()
        {
            if (searchInputField.isFocused &&
               (Input.GetKeyDown(KeyCode.Tab)))
            {
                SearchButton();
            }
        }
#endif
        protected override void OnDestroy()
        {
            GEventManager.StopListening(GFriendsManager.ON_FRIENDS_LIST_UPDATED, OnRefreshFriendsList);
            GEventManager.StopListening(GBomberRTTConfigManager.ON_SEARCH_RESULTS_UPDATED, OnRefreshSearchResults);

            // TODO: refactor thise presence callback ONLY in the FriendsMgr, and these two other places listen for updates!
            MainMenuState mainMenu = GStateManager.Instance.CurrentState as MainMenuState;
            if (mainMenu) GCore.Wrapper.RTTService.RegisterRTTPresenceCallback(mainMenu.OnPresenceCallback);

            base.OnDestroy();
        }
        #endregion

        #region Public
        public void InputFieldChanged()
        {
            Text text = searchInputField.GetComponentInChildren<Text>();
            text.color = LIGHT_TEXT;
        }

        public void SearchButton()
        {
            if (!searchInputField.text.Equals("Search for a friend"))
            {
                SearchForFriend();
            }
        }
        #endregion

        #region Private
        private void SearchForFriend()
        {
            m_doSearch = true;
            NoSearchResults.enabled = false;
            GStateManager.Instance.EnableLoadingSpinner(true);
            RemoveAllGlobalItems(m_resultsItems);
            GFriendsManager.Instance.FindUserByUniversalId(searchInputField.text, 30, OnFindUserSuccess);

            // ensure to unselect this
            searchInputField.enabled = false;
            searchInputField.enabled = true;
        }

        private void OnPresenceCallback(string in_message)
        {
            PresenceData presenceData = new PresenceData();
            GFriendsManager.Instance.ParsePresenceCallback(in_message, ref presenceData);
            if (presenceData.ProfileId.Length > 0)
            {
                // Refresh your friend's online status
                if (!UpdateFriendOnlineStatus(m_friendsListItem, presenceData))
                    UpdateFriendOnlineStatus(m_searchResultsListItem, presenceData);
            }
        }

        private bool UpdateFriendOnlineStatus(List<FriendsListItem> in_listItem, PresenceData in_presenceData)
        {
            for (int i = 0; i < in_listItem.Count; ++i)
            {
                if (in_listItem[i].ProfileId.Equals(in_presenceData.ProfileId))
                {
                    in_listItem[i].UpdateOnlineStatus(in_presenceData);
                    return true;
                }
            }
            return false;
        }

        private void OnRefreshFriendsList()
        {
            OnGetPresenceOfFriendsSuccess("", null);
        }

        private void OnRefreshSearchResults()
        {
            if (m_resultsItems != null)
            {
                for (int it = 0; it < m_resultsItems.Count; ++it)
                {
                    if (GFriendsManager.Instance.IsProfileIdInFriendsList(m_resultsItems[it].ProfileId))
                    {
                        m_resultsItems.Remove(m_resultsItems[it]);
                    }
                }
            }
            PopulateFriendsScrollView(m_resultsItems, m_searchResultsListItem, SearchResultsScrollView, true, false);
            NoSearchResults.enabled = m_resultsItems != null && m_resultsItems.Count == 0;
            if (m_doSearch && m_resultsItems != null && m_resultsItems.Count == 0)
                NoSearchResults.Play("NoFriendsFadeOut", 0, -1.0f);
            m_doSearch = false;
            populateRecentlyViewed();
        }

        private FriendsListItem CreateFriendsListItem(Transform in_parent = null)
        {
            FriendsListItem toReturn = null;
            toReturn = (GEntityFactory.Instance.CreateResourceAtPath("Prefabs/UI/friendCell", in_parent.transform)).GetComponent<FriendsListItem>();
            toReturn.transform.SetParent(in_parent);
            toReturn.transform.localScale = Vector3.one;
            return toReturn;
        }

        private void RemoveAllGlobalItems(List<PlayerData> in_friendsItems)
        {
            if (in_friendsItems != null)
                in_friendsItems.Clear();
        }

        private void RemoveAllCellsInView(Transform in_view, List<FriendsListItem> in_friendsListItem = null)
        {
            foreach (Transform child in in_view)
            {
                Destroy(child.gameObject);
            }

            if (in_friendsListItem != null) in_friendsListItem.Clear();
        }

        private void PopulateFriendsScrollView(List<PlayerData> in_friendsItems, List<FriendsListItem> in_friendsListItem, RectTransform in_scrollView, bool in_add, bool in_remove)
        {
            RemoveAllCellsInView(in_scrollView, in_friendsListItem);

            if (in_scrollView != null && in_friendsItems != null)
            {
                for (int i = 0; i < in_friendsItems.Count; ++i)
                {
                    FriendsListItem newItem = CreateFriendsListItem(in_scrollView);
                    newItem.Init(in_friendsItems[i], in_add, in_remove);
                    newItem.transform.localPosition = new Vector3(0.0f, 0.0f);
                    in_friendsListItem.Add(newItem);
                }
            }
        }

        private void OnGetPresenceOfFriendsSuccess(string in_stringData, object in_obj)
        {
            m_friendsItems = GFriendsManager.Instance.Friends;
            PopulateFriendsScrollView(m_friendsItems, m_friendsListItem, FriendsScrollView, false, true);
            NoFriendsAdded.gameObject.SetActive(m_friendsItems == null || (m_friendsItems != null && m_friendsItems.Count == 0));
            OnRefreshSearchResults();
        }

        private void OnFindUserSuccess(string in_stringData, object in_obj)
        {
            m_resultsItems = GFriendsManager.Instance.SearchResults;
            OnRefreshSearchResults();
        }

        private void populateRecentlyViewed()
        {
            GFriendsManager friendsMgr = GFriendsManager.Instance;
            List<RecentlyViewed> recentlyViewed = new List<RecentlyViewed>();
            if (GFriendsManager.Instance.RecentlyViewed != null)
            {
                Array previousMembers = (Array)friendsMgr.RecentlyViewed["previousMembers"];
                foreach (Dictionary<string, object> dict in previousMembers)
                {
                    // Add only the non-serverBot players into the RecentlyViewed list.
                    if (!(dict["ProfileId"] as string).Contains("serverBot"))
                    {
                        if (!friendsMgr.IsProfileIdInFriendsList(dict["ProfileId"] as string))
                            recentlyViewed.Add(new RecentlyViewed(dict));
                    }
                }
            }

            if (recentlyViewed.Count > 0)
            {
                // find the entity for recently viewed, 
                Text newField = GEntityFactory.Instance.CreateObject(m_titleText, Vector3.zero, Quaternion.identity, SearchResultsScrollView.transform).GetComponent<Text>();
                newField.text = "    RECENTLY VIEWED";

                // put them all on
                foreach (RecentlyViewed newData in recentlyViewed)
                {
                    FriendsListItem newItem = CreateFriendsListItem(SearchResultsScrollView.transform);
                    newItem.Init(newData, true, false);
                }
            }
        }

        private Color LIGHT_TEXT = new Color(224, 193, 154, 255);
        private List<FriendsListItem> m_friendsListItem = null;
        private List<PlayerData> m_friendsItems = null;
        private List<FriendsListItem> m_searchResultsListItem = null;
        private List<PlayerData> m_resultsItems = null;
        private bool m_doSearch = false;

        private GameObject m_titleText = null;
        #endregion
    }
}
