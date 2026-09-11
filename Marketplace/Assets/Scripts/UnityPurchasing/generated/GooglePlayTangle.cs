// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("OogLKDoHDAMgjEKM/QcLCwsPCgkU5C2x589YH3H/vq/5Cl0uaZ1FZvEOUTIPwRekjKHWFWhRSCSzUSRyrjkPyyAEEwtnmh569LPj/zpnHTVahkLy87/9NGrQAl2apaFzFJJTYsbC0p0bpkU0CcB0BpDVBe2ov80MJVkJ78nh7XhhJ3IY1irXUDEDPOl4rLeckLvKWSh27ZGG9prrqAFrgBZGNZQO45Qv2r06BInWPziVBknx+5q67Ag8w4U4V9eWVzNJuPzVvO4DOW3Xq9BIuPrRYnTGNw5DH2yGsRJY9HPfeYt4kROh0QeoDUhm5ysliAsFCjqICwAIiAsLCoIgbkgEza/WGT62i3ME9K+ILVlojqxunaMbN40MXYPeAfEfjQgJCwoL");
        private static int[] order = new int[] { 0,13,6,5,11,6,9,9,9,9,10,11,13,13,14 };
        private static int key = 10;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
