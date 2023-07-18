
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
using System.Runtime.InteropServices;
using System.Linq;
using com.amazon.device.iap.cpt.json;


namespace com.amazon.device.iap.cpt
{
    public sealed class ProductData : Jsonable
    {
        private static AmazonLogger logger = new AmazonLogger("Pi");

        public string Sku{get;set;}
                public string ProductType{get;set;}
                public string Price{get;set;}
                public string Title{get;set;}
                public string Description{get;set;}
                public string SmallIconUrl{get;set;}
                public CoinsReward CoinsReward{get;set;}

        public string ToJson()
        {
            try
            {
                Dictionary<string, object> toJson = this.GetObjectDictionary();
                return Json.Serialize(toJson);
            }
            catch(System.ApplicationException ex)
            {
                throw new AmazonException("Error encountered while Jsoning", ex);
            }
        }

        public override Dictionary<string, object> GetObjectDictionary() 
        {
            try 
            {
                Dictionary<string, object> objectDictionary = new Dictionary<string, object>();
                
                objectDictionary.Add("sku", Sku);
                objectDictionary.Add("productType", ProductType);
                objectDictionary.Add("price", Price);
                objectDictionary.Add("title", Title);
                objectDictionary.Add("description", Description);
                objectDictionary.Add("smallIconUrl", SmallIconUrl);
                objectDictionary.Add("coinsReward", (CoinsReward != null) ? CoinsReward.GetObjectDictionary() : null);
                return objectDictionary;
            } 
            catch(System.ApplicationException ex) 
            {
                throw new AmazonException("Error encountered while getting object dictionary", ex);
            }
        }

        public static ProductData CreateFromDictionary(Dictionary<string, object> jsonMap) 
        {
            try 
            {
                if (jsonMap == null)
                {
                    return null;
                }

                var request = new ProductData();
                
                
                if(jsonMap.ContainsKey("sku")) 
                {
                    request.Sku = (string) jsonMap["sku"];
                }
                
                if(jsonMap.ContainsKey("productType")) 
                {
                    request.ProductType = (string) jsonMap["productType"];
                }
                
                if(jsonMap.ContainsKey("price")) 
                {
                    request.Price = (string) jsonMap["price"];
                }
                
                if(jsonMap.ContainsKey("title")) 
                {
                    request.Title = (string) jsonMap["title"];
                }
                
                if(jsonMap.ContainsKey("description")) 
                {
                    request.Description = (string) jsonMap["description"];
                }
                
                if(jsonMap.ContainsKey("smallIconUrl")) 
                {
                    request.SmallIconUrl = (string) jsonMap["smallIconUrl"];
                }
                
                if(jsonMap.ContainsKey("coinsReward")) 
                {
                    request.CoinsReward = CoinsReward.CreateFromDictionary(jsonMap["coinsReward"] as Dictionary<string, object>); 
                }

                return request;
            } 
            catch (System.ApplicationException ex) 
            {
                throw new AmazonException("Error encountered while creating Object from dicionary", ex);
            }
        }

        public static ProductData CreateFromJson(string jsonMessage)
        {
            try 
            {
                Dictionary<string, object> jsonMap = Json.Deserialize(jsonMessage) as Dictionary<string, object>;
                Jsonable.CheckForErrors(jsonMap);
                return CreateFromDictionary(jsonMap);
            }
            catch(System.ApplicationException ex)
            {
                throw new AmazonException("Error encountered while UnJsoning", ex);
            }
        }
        

        public static Dictionary<string, ProductData> MapFromJson(Dictionary<string, object> jsonMap)
        {
            Dictionary<string, ProductData> result = new Dictionary<string, ProductData>();
            foreach (var entry in jsonMap)
            {
                ProductData value = CreateFromDictionary(entry.Value as Dictionary<string, object>);
                result.Add(entry.Key, value);
            }
            return result;
        }
        
        public static List<ProductData> ListFromJson(List<object> array)
        {
            List<ProductData> result = new List<ProductData>();
            foreach (var e in array)
            {
                result.Add(CreateFromDictionary(e as Dictionary<string, object>));
            }
            return result;
        }
    }
}
