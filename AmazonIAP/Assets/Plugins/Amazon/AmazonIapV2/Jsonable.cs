
/* 
* Copyright 2014 Amazon.com,
* Inc. or its affiliates. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the
* "License"). You may not use this file except in compliance
* with the License. A copy of the License is located at
*
* http://aws.amazon.com/apache2.0/
*
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, either express or implied. See the
* License for the specific language governing permissions and
* limitations under the License.
*/
using System.Collections;
using System.Collections.Generic;

namespace com.amazon.device.iap.cpt 
{
    public abstract class Jsonable 
    {
        public static Dictionary<string, object> unrollObjectIntoMap<T>(Dictionary<string, T> obj) where T:Jsonable  
        {
            Dictionary<string, object> jsonableDict = new Dictionary<string, object>();
            foreach (var entry in obj) 
            {
                jsonableDict.Add (entry.Key, ((Jsonable)entry.Value).GetObjectDictionary());
            }
            return jsonableDict;
        }

        public static List<object> unrollObjectIntoList<T>(List<T> obj) where T:Jsonable
        {
            List<object> jsonableList = new List<object>();
            foreach (Jsonable entry in obj) 
            {
                jsonableList.Add(entry.GetObjectDictionary());
            }
            return jsonableList;
        }

        public abstract Dictionary<string, object> GetObjectDictionary();
        
        public static void CheckForErrors(Dictionary<string, object> jsonMap)
        {
            object error;
            if (jsonMap.TryGetValue("error", out error))
            {
                throw new AmazonException(error as string);
            }
        }   
    }
}

