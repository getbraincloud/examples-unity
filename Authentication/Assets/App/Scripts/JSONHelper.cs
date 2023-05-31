using BrainCloud.JsonFx.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrainCloud.JSONHelper
{
    public interface IJSON
    {
        /// <summary>
        /// Get the DataType that this <see cref="IJSON"/> represents as a JSON <see cref="object"/>.
        /// </summary>
        string GetDataType();

        /// <summary>
        /// Get a Dictionary representing this <see cref="IJSON"/> as a JSON-styled <see cref="string"/>, <see cref="object"/> pair.
        /// </summary>
        Dictionary<string, object> GetDictionary();

        /// <summary>
        /// Serializes this <see cref="IJSON"/> into a JSON-formatted <see cref="string"/>.
        /// </summary>
        string Serialize();

        /// <summary>
        /// Deserializes a JSON-formatted <see cref="string"/> into, ideally, this <see cref="IJSON"/>.
        /// </summary>
        /// <param name="json">The JSON string that this <see cref="IJSON"/> is contained in.</param>
        /// <returns>The deserialized <typeparamref name="T"/>, which can be used for structs.</returns>
        IJSON Deserialize(string json);

        /// <summary>
        /// Deserializes a JSON-styled <see cref="string"/>, <see cref="object"/> Dictionary pair into, ideally, this <see cref="IJSON"/>.
        /// </summary>
        /// <param name="json">The JSON string that this <see cref="IJSON"/> is contained in.</param>
        /// <returns>The deserialized <typeparamref name="T"/>, which can be used for structs.</returns>
        IJSON Deserialize(Dictionary<string, object> json);
    }

    public static class JSONExtensions
    {
        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Must be a non-nullable value type.</typeparam>
        /// <returns>
        /// <para>If <typeparamref name="T"/> is a <see cref="bool"/>, <see cref="object"/> being non-zero or a non-null value will return <b>True</b>.
        /// <see cref="string.ToLower()"/> values of "false" and otherwise will return <b>False</b>.</para>
        /// <para>If <typeparamref name="T"/> is a number, it will attempt to convert the <see cref="object"/> into a number
        /// (i.e., <see cref="float"/> to <see cref="long"/>, <see cref="string"/> will be converted as well). Otherwise it will return <b>0</b>.</para>
        /// </returns>
        public static T ToType<T>(this object jsonObj) where T : struct
        {
            if (jsonObj is not null)
            {
                if (jsonObj is T typedObj)
                {
                    return typedObj;
                }

                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        string stringObj = jsonObj.ToString().Trim();

                        return (T)((!string.IsNullOrEmpty(stringObj) &&
                                    stringObj != "0" && stringObj.ToLower() != "false") as T?);
                    }

                    return (T)Convert.ChangeType(jsonObj, typeof(T));
                }
                catch { }
            }

            return default;
        }

        /// <summary>
        /// Cast the <see cref="IJSON"/> <see cref="object"/> into its proper type of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Must be an IJSON <see cref="object"/>.</typeparam>
        public static T ToType<T>(this IJSON jsonObj) where T : IJSON
        {
            if (jsonObj is not null && jsonObj is T ijsonObj)
            {
                return ijsonObj;
            }

            return default;
        }

        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into a Dictionary <see cref="string"/>, <see cref="object"/> pair.
        /// </summary>
        public static Dictionary<string, object> ToDictionary(this object jsonObj)
            => jsonObj as Dictionary<string, object>;

        /// <summary>
        /// Cast the deserialized JSON <see cref="object"/> into an <see cref="object"/> array.
        /// </summary>
        public static object[] ToArray(this object jsonObj)
            => jsonObj as object[];

        /// <summary>
        /// Serializes JSON-formatted string into.
        /// </summary>
        public static string Serialize(this IDictionary obj)
            => JsonWriter.Serialize(obj);

        /// <summary>
        /// Deserializes a JSON-formatted string into a Dictionary <see cref="string"/>, <see cref="object"/> pair.
        /// </summary>
        public static Dictionary<string, object> Deserialize(this string json)
            => JsonReader.Deserialize<Dictionary<string, object>>(json);
    }
}
