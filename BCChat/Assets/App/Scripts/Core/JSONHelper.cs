using BrainCloud.Common;
using BrainCloud.JsonFx.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrainCloud.JSONHelper
{
    /// <summary>
    /// An interface to help with serializing and deserializing JSON data.
    /// </summary>
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
        /// Get the <see cref="object"/> within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>) as type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Constrained to non-nullable value types.</typeparam>
        /// <param name="property">The name of the property that indexes our <typeparamref name="T"/>.</param>
        /// <returns>
        /// <para>For a <see cref="bool"/>, the <paramref name="property"/> value being a non-empty value will return <b>True</b>.
        /// <see cref="string.ToLower()"/> values of "false" or empty values will return <b>False</b>.</para>
        /// <para>If <typeparamref name="T"/> is a number, it will attempt to convert the <paramref name="property"/> value into a number
        /// (i.e., <see cref="float"/> to <see cref="long"/>, <see cref="string"/> can be converted as well). Otherwise it will return <b>0</b>.</para>
        /// </returns>
        public static T GetValue<T>(this IDictionary<string, object> self, string property) where T : struct, IConvertible
        {
            if (self is not null && self.TryGetValue(property, out object obj))
            {
                if (obj is T value) return value;

                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        string str = self.ToString().Trim();

                        return (T)((!string.IsNullOrEmpty(str) &&
                                    str != "0" && str.ToLower() != "false") as T?);
                    }

                    return (T)Convert.ChangeType(obj, typeof(T));
                }
                catch { } // If conversion fails we silently fail to return a default value
            }

            return default;
        }

        /// <summary>
        /// Get the <see cref="object.ToString()"/> value within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// This is useful if the property is a string or we need the property as a string.
        /// </summary>
        /// <param name="property">The name of the property that indexes our <see cref="object"/>.</param>
        /// <returns>The <paramref name="property"/>'s <see cref="object.ToString()"/> value. If it doesn't exist or is null it returns <see cref="string.Empty"/></returns>
        public static string GetString(this IDictionary<string, object> self, string property)
        {
            if (self is not null && self.TryGetValue(property, out object obj) && obj is not null)
            {
                return obj.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Get a <see cref="DateTime"/> value within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <param name="property">The name of the property that indexes our <see cref="DateTime"/>.</param>
        /// <returns>brainCloud stores time in milliseconds. This will return a <see cref="DateTime"/> if the <paramref name="property"/> is a number value.
        /// If it isn't a number value or is null it returns <see cref="DateTime.UnixEpoch"/>.</returns>
        public static DateTime GetDateTime(this IDictionary<string, object> self, string property)
        {
            long t = self.GetValue<long>(property);
            return t > 0 ? Util.BcTimeToDateTime(t) : DateTime.UnixEpoch;
        }

        /// <summary>
        /// Get a <see cref="TimeSpan"/> value within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <param name="property">The name of the property that indexes our <see cref="TimeSpan"/>.</param>
        /// <returns>brainCloud stores time in milliseconds. This should return a <see cref="TimeSpan"/> if the <paramref name="property"/> is a number value.
        /// If it isn't a number value or is null it returns <see cref="TimeSpan.Zero"/>.</returns>
        public static TimeSpan GetTimeSpan(this IDictionary<string, object> self, string property)
            => TimeSpan.FromMilliseconds(self.GetValue<double>(property));

        /// <summary>
        /// Get the <see cref="ACL"/> within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <param name="property">The name of the property that indexes our <see cref="ACL"/>.</param>
        /// <returns>An <see cref="ACL"/> if the <paramref name="property"/> contains an <b>other</b> number value. Otherwise it returns <see cref="ACL.None()"/></returns>
        public static ACL GetACL(this IDictionary<string, object> self, string property)
        {
            if (self is not null && self.TryGetValue(property, out object obj))
            {
                if (obj is Dictionary<string, object> other && other.ContainsKey("other"))
                {
                    return ACL.CreateFromJson(other);
                }
            }

            return ACL.None();
        }

        /// <summary>
        /// Get the Dictionary(<see cref="string"/>, <see cref="object"/>) within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <param name="property">The name of the property that indexes our Dictionary(<see cref="string"/>, <see cref="object"/>.</param>
        /// <returns>The Dictionary(<see cref="string"/>, <see cref="object"/>) that <paramref name="property"/> represents. Otherwise it returns <b>null</b>.</returns>
        public static Dictionary<string, object> GetJSONObject(this IDictionary<string, object> self, string property)
        {
            if (self is not null && self.TryGetValue(property, out object obj))
            {
                if (obj is Dictionary<string, object> map)
                {
                    return map;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the <typeparamref name="T"/> within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <typeparam name="T">Constrained to <see cref="IJSON"/>.</typeparam>
        /// <param name="property">The name of the property that indexes our <see cref="IJSON"/>.</param>
        /// <returns>The <typeparamref name="T"/> object that <paramref name="property"/> represents. Otherwise it returns <b>null</b> or the <b>default</b> value.</returns>
        public static T GetJSONObject<T>(this IDictionary<string, object> self, string property) where T : IJSON
        {
            if (self is not null && self.TryGetValue(property, out object obj))
            {
                if (obj is T jObj)
                {
                    return jObj;
                }
                else if (obj is Dictionary<string, object> map)
                {
                    try
                    {
                        return (T)Activator.CreateInstance<T>().FromJSONObject(map);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Unable to convert {property} into {typeof(T).Name}!\nException: {e}");
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Get the Dictionary(<see cref="string"/>, <see cref="object"/>) array within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <param name="property">The name of the property that indexes our Dictionary(<see cref="string"/>, <see cref="object"/> array.</param>
        /// <returns>The Dictionary(<see cref="string"/>, <see cref="object"/>) array that <paramref name="property"/> represents.
        /// If it is null or doesn't exist it will return an empty Dictionary(<see cref="string"/>, <see cref="object"/>) array.</returns>
        public static Dictionary<string, object>[] GetJSONArray(this IDictionary<string, object> self, string property)
        {
            if (self is not null && self.TryGetValue(property, out object obj))
            {
                if(obj is Dictionary<string, object>[] arr)
                {
                    return arr;
                }
            }

            return new Dictionary<string, object>[0];
        }

        /// <summary>
        /// Get the <typeparamref name="T"/> array within a deserialized JSON Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <typeparam name="T">Constrained to <see cref="IJSON"/>.</typeparam>
        /// <param name="property">The name of the property that indexes our <see cref="IJSON"/> array.</param>
        /// <returns>The <typeparamref name="T"/> that <paramref name="property"/> represents. If it is null or doesn't exist it will return an empty <typeparamref name="T"/> array.</returns>
        public static T[] GetJSONArray<T>(this IDictionary<string, object> self, string property) where T : IJSON
        {
            if (self is not null && self.TryGetValue(property, out object obj))
            {
                if (obj is T[] jObjArr)
                {
                    return jObjArr;
                }
                else if (obj is Dictionary<string, object>[] arr)
                {
                    try
                    {
                        T[] jArr = new T[arr.Length];

                        for (int i = 0; i < arr.Length; i++)
                        {
                            jArr[i] = (T)Activator.CreateInstance<T>().FromJSONObject(arr[i]);
                        }

                        return jArr;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Unable to convert {property} into an array of {typeof(T).Name}!\nException: {e}");
                    }
                }
            }

            return new T[0];
        }

        /// <summary>
        /// Serializes this <see cref="object"/> into a JSON-formatted <see cref="string"/>.
        /// </summary>
        public static string Serialize(this object self) => JsonWriter.Serialize(self);

        /// <summary>
        /// Serializes this <see cref="IJSON"/> into a JSON-formatted <see cref="string"/>.
        /// </summary>
        public static string Serialize(this IJSON self) => JsonWriter.Serialize(self.ToJSONObject());

        /// <summary>
        /// Deserializes a JSON-formatted string into a Dictionary(<see cref="string"/>, <see cref="object"/>).
        /// </summary>
        /// <param name="hierarchy">Go directly to the Dictionary(<see cref="string"/>, <see cref="object"/>) you want by
        /// listing in order the properties of each level of JSON objects.</param>
        public static Dictionary<string, object> Deserialize(this string self, params string[] hierarchy)
        {
            if (hierarchy != null && hierarchy.Length > 0)
            {
                var obj = JsonReader.Deserialize<Dictionary<string, object>>(self);

                int level = 0;
                while (obj.TryGetValue(hierarchy[level], out object child))
                {
                    if (child != null && child is Dictionary<string, object> next)
                    {
                        obj = next;
                    }
                    else
                    {
                        Debug.LogError($"{hierarchy[level]} is not a Dictionary in the hierarchy of the JSON! Returning null...");
                        return null;
                    }

                    if (++level >= hierarchy.Length)
                    {
                        break;
                    }
                }

                if (level < hierarchy.Length)
                {
                    Debug.LogError($"{hierarchy[level]} is not found in the hierarchy of the JSON! Returning null...");
                    return null;
                }

                return obj;
            }

            return JsonReader.Deserialize<Dictionary<string, object>>(self);
        }

        /// <summary>
        /// Deserializes a JSON-formatted <see cref="string"/> into type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Constrained to <see cref="IJSON"/>.</typeparam>
        /// <param name="hierarchy">Go directly to the <see cref="IJSON"/> you want by listing in order the properties of each level of JSON objects.</param>
        /// <returns>The <see cref="IJSON"/> object that the JSON <see cref="string"/> represents.
        /// If the <see cref="string"/> cannot deserialize into type <typeparamref name="T"/> then this return <b>null</b> or <b>default</b>.</returns>
        public static T Deserialize<T>(this string self, params string[] hierarchy) where T : IJSON
        {
            var obj = self.Deserialize(hierarchy);
            if (obj is not null)
            {
                try
                {
                    return (T)Activator.CreateInstance<T>().FromJSONObject(obj);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to deserialize string into {typeof(T).Name}!\nException: {e}");
                }
            }

            return default;
        }
    }
}
