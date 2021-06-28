// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("2QXzrH7yHPxaUDbjHbGAnacLOnkK/uHzc4qyKWRUAz2dX3jl5A/P3DGyvLODMbK5sTGysrN0tIfOMRiKwq9OTXV+uZc7NTpteVA6Z07ScJDnbuD0TKrNAgZlXEmgJS/qxZKUXc/RqY9kLEZevql4myohpwfqMZSyFGBPrdesbNhgphKWlHMLdA4AZ9z4Vc+xaeU14EeJBLxAxACNoea384MxspGDvrW6mTX7NUS+srKytrOwEEHmqsSf9fRq/qTkFFSiVODrAWh1KYClLyNi5z10njIoj85NkUPiVEyxKiXCLFCvcVWmQE0g8e94I3+XwRs2tJazI03oIV2W3apbnI1i+RySB/kuH4AJjOYkyl1fuOtBUYiqIf04dNJ0Q/I5uLGwsrOy");
        private static int[] order = new int[] { 7,5,5,9,5,5,10,9,9,9,13,13,12,13,14 };
        private static int key = 179;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
