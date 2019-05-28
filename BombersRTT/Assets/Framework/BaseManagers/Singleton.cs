namespace Gameframework
{
    public class Singleton
    {
        private Singleton(){}
        static Singleton(){}

        #region Public 
        public static Singleton Instance { get { return GetInstance(); } }
        public static Singleton GetInstance() { return m_instance; }
        #endregion

        #region private
        private static Singleton m_instance = new Singleton();
        #endregion
    }
}
