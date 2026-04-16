using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArrayUtility
{
	public static class ArrayUtil
	{
		public static int RandomIndex<T>(this T[] array)
		{
			return Random.Range(0, array.Length);
		}

		public static T RandomElement<T>(this T[] array)
		{
			return array[array.RandomIndex()];
		}
	}
}