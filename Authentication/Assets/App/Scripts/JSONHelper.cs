using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrainCloud.JSONHelper
{
    public interface IJSON
    {
        /// <summary>
        /// Get the type of data that this <see cref="IJSON"/> represents as a JSON-styled <see cref="object"/>.
        /// </summary>
        string GetDataType();

        /// <summary>
        /// Get a collection representing this <see cref="IJSON"/> as a JSON-styled Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        Dictionary<string, object> ToJSONObject();

        /// <summary>
        /// Get values from a JSON-styled Dictionary(<see cref="string"/>, <see cref="object"/>) for this <see cref="IJSON"/>.
        /// </summary>
        /// <param name="obj">The Dictionary(<see cref="string"/>, <see cref="object"/>) that the <see cref="IJSON"/>'s values are contained in.</param>
        IJSON FromJSONObject(Dictionary<string, object> obj);
    }

    public static class JSONHelper
    {
        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Constrained to non-nullable value types.</typeparam>
        /// <returns>
        /// <para>If <typeparamref name="T"/> is a <see cref="bool"/>, <see cref="object"/> being a non-zero or a non-null value will return <b>True</b>.
        /// <see cref="string.ToLower()"/> values of "false" or empty values will return <b>False</b>.</para>
        /// <para>If <typeparamref name="T"/> is a number, it will attempt to convert <see cref="object"/> into a number
        /// (i.e., <see cref="float"/> to <see cref="long"/>, <see cref="string"/> will be converted as well). Otherwise it will return <b>0</b>.</para>
        /// </returns>
        public static T ToType<T>(this object self) where T : struct
        {
            if (self is not null)
            {
                if (self is T obj)
                {
                    return obj;
                }

                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        string str = self.ToString().Trim();

                        return (T)((!string.IsNullOrEmpty(str) &&
                                    str != "0" && str.ToLower() != "false") as T?);
                    }

                    return (T)Convert.ChangeType(self, typeof(T));
                }
                catch { } // If conversion fails we silently fail to return a default value
            }

            return default;
        }

        /// <summary>
        /// Cast the <see cref="IJSON"/> <see cref="object"/> into its strongly typed <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Constrained to <see cref="IJSON"/>.</typeparam>
        public static T ToType<T>(this IJSON self) where T : IJSON
        {
            if (self is not null && self is T obj)
            {
                return obj;
            }

            return default;
        }

        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into a Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        public static Dictionary<string, object> ToJSONObject(this object self)
        {
            if (self is not null)
            {
                if (self is Dictionary<string, object> obj)
                {
                    return obj;
                }
                else if (self is string str)
                {
                    return str.Deserialize();
                }
            }

            return null;
        }

        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into a Dictionary(<see cref="string"/>, <see cref="object"/>) array.
        /// </summary>
        /// <returns>The Dictionary(<see cref="string"/>, <see cref="object"/>) array. If the object is null or not an array then the Dictionary array will be empty.</returns>
        public static Dictionary<string, object>[] ToJSONArray(this object self)
        {
            if (self is not null)
            {
                if (self is Dictionary<string, object>[] arr)
                {
                    return arr;
                }
                else if (self is string str)
                {
                    return str.Deserialize().ToJSONArray();
                }
            }

            return new Dictionary<string, object>[0];
        }

        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into an <see cref="IJSON"/> object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Constrained to <see cref="IJSON"/>.</typeparam>
        public static T ToJSONType<T>(this object self) where T : IJSON
        {
            if (self is not null && self is Dictionary<string, object> obj)
            {
                try
                {
                    return (T)Activator.CreateInstance<T>().FromJSONObject(obj);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Unable to convert {self} into type {typeof(T).Name}!\nException: {e}");
                }
            }

            return default;
        }

        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into an array of <see cref="IJSON"/> objects of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Constrained to <see cref="IJSON"/>.</typeparam>
        /// <returns>The <typeparamref name="T"/> array. If the object is null or not an array then the <typeparamref name="T"/> array will be empty.</returns>
        public static T[] ToJSONTypeArray<T>(this object self) where T : IJSON
        {
            if (self is not null && self is object[] objArr)
            {
                try
                {
                    T[] jArr = new T[objArr.Length];

                    for (int i = 0; i < objArr.Length; i++)
                    {
                        jArr[i] = (T)Activator.CreateInstance<T>().FromJSONObject(objArr[i] as Dictionary<string, object>);
                    }

                    return jArr;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Unable to convert array of {self} into an array of type {typeof(T).Name}!\nException: {e}");
                }
            }

            return new T[0];
        }

        /// <summary>
        /// Serializes this <see cref="IDictionary"/> into a JSON-formatted <see cref="string"/>.
        /// </summary>
        public static string Serialize(this IDictionary self)
            => JsonWriter.Serialize(self);

        /// <summary>
        /// Serializes this <see cref="IJSON"/> into a JSON-formatted <see cref="string"/>.
        /// </summary>
        public static string Serialize(this IJSON self)
            => JsonWriter.Serialize(self.ToJSONObject());

        /// <summary>
        /// Deserializes a JSON-formatted string into a Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        public static Dictionary<string, object> Deserialize(this string self)
            => JsonReader.Deserialize<Dictionary<string, object>>(self);

        /// <summary>
        /// Deserializes a JSON-formatted <see cref="string"/> into this <see cref="IJSON"/>.
        /// </summary>
        /// <param name="json">The JSON string that this <see cref="IJSON"/> is contained in.</param>
        public static IJSON Deserialize(this IJSON self, string json)
            => self.FromJSONObject(json.Deserialize());
    }
}
