using System;

namespace BCFishNet
{
    public static class PlayerListEvents
    {
        public static event Action OnResyncPlayerList;
        public static event Action OnClearAllPlayerList;
    
        public static void RaiseResyncPlayerList()
        {
            OnResyncPlayerList?.Invoke();
        }
    
        public static void RaiseClearAllPlayerList()
        {
            OnClearAllPlayerList?.Invoke();
        }
    }
}
