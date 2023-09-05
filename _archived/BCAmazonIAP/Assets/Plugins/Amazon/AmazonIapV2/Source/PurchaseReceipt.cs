
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
    public sealed class PurchaseReceipt : Jsonable
    {
        private static AmazonLogger logger = new AmazonLogger("Pi");

        public string ReceiptId{get;set;}
                public long CancelDate{get;set;}
                public long PurchaseDate{get;set;}
                public string Sku{get;set;}
                public string ProductType{get;set;}
        
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
                
                objectDictionary.Add("receiptId", ReceiptId);
                objectDictionary.Add("cancelDate", CancelDate);
                objectDictionary.Add("purchaseDate", PurchaseDate);
                objectDictionary.Add("sku", Sku);
                objectDictionary.Add("productType", ProductType);
                return objectDictionary;
            } 
            catch(System.ApplicationException ex) 
            {
                throw new AmazonException("Error encountered while getting object dictionary", ex);
            }
        }

        public static PurchaseReceipt CreateFromDictionary(Dictionary<string, object> jsonMap) 
        {
            try 
            {
                if (jsonMap == null)
                {
                    return null;
                }

                var request = new PurchaseReceipt();
                
                
                if(jsonMap.ContainsKey("receiptId")) 
                {
                    request.ReceiptId = (string) jsonMap["receiptId"];
                }
                
                if(jsonMap.ContainsKey("cancelDate")) 
                {
                    request.CancelDate = (long) jsonMap["cancelDate"];
                }
                
                if(jsonMap.ContainsKey("purchaseDate")) 
                {
                    request.PurchaseDate = (long) jsonMap["purchaseDate"];
                }
                
                if(jsonMap.ContainsKey("sku")) 
                {
                    request.Sku = (string) jsonMap["sku"];
                }
                
                if(jsonMap.ContainsKey("productType")) 
                {
                    request.ProductType = (string) jsonMap["productType"];
                }

                return request;
            } 
            catch (System.ApplicationException ex) 
            {
                throw new AmazonException("Error encountered while creating Object from dicionary", ex);
            }
        }

        public static PurchaseReceipt CreateFromJson(string jsonMessage)
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
        

        public static Dictionary<string, PurchaseReceipt> MapFromJson(Dictionary<string, object> jsonMap)
        {
            Dictionary<string, PurchaseReceipt> result = new Dictionary<string, PurchaseReceipt>();
            foreach (var entry in jsonMap)
            {
                PurchaseReceipt value = CreateFromDictionary(entry.Value as Dictionary<string, object>);
                result.Add(entry.Key, value);
            }
            return result;
        }
        
        public static List<PurchaseReceipt> ListFromJson(List<object> array)
        {
            List<PurchaseReceipt> result = new List<PurchaseReceipt>();
            foreach (var e in array)
            {
                result.Add(CreateFromDictionary(e as Dictionary<string, object>));
            }
            return result;
        }
    }
}
