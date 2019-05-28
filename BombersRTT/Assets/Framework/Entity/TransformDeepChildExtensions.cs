using System.Collections.Generic;
using UnityEngine;

namespace Gameframework
{
    public static class TransformDeepChildExtension
    {
        //Breadth-first search
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            var result = aParent.Find(aName);
            if (result != null)
                return result;
            foreach (Transform child in aParent)
            {
                result = child.FindDeepChild(aName);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static T FindDeepChild<T>(this Transform aParent)
        {
            T toReturn;
            // the parent is one ?
            T tempTemplate = aParent.GetComponent<T>();
            toReturn = tempTemplate; // this MAY be null
            Transform child = null;
            for (int i = 0; i < aParent.childCount && tempTemplate == null; ++i)
            {
                child = aParent.GetChild(i);
                tempTemplate = child.GetComponent<T>();
                if (tempTemplate != null)
                    toReturn = tempTemplate;
            }
            return toReturn;
        }

        public static List<Transform> FindDeepChildren(this Transform aParent, string aName)
        {
            List<Transform> toReturn = new List<Transform>();

            // the parent is one ?
            if (aParent.name.Contains(aName))
                toReturn.Add(aParent);

            List<Transform> result;
            foreach (Transform child in aParent)
            {
                result = child.FindDeepChildren(aName);
                if (result != null)
                    toReturn.AddRange(result);
            }
            return toReturn;
        }

        public static List<T> FindDeepChildren<T>(this Transform aParent)
        {
            List<T> toReturn = new List<T>();

            // the parent is one ?
            T tempTemplate = aParent.GetComponent<T>();
            if (tempTemplate != null)
                toReturn.Add(tempTemplate);

            List<T> result;
            foreach (Transform child in aParent)
            {
                result = child.FindDeepChildren<T>();
                if (result != null)
                    toReturn.AddRange(result);
            }
            return toReturn;
        }

        /*
        //Depth-first search
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            foreach(Transform child in aParent)
            {
                if(child.name == aName )
                    return child;
                var result = child.FindDeepChild(aName);
                if (result != null)
                    return result;
            }
            return null;
        }
        */
    }
}
