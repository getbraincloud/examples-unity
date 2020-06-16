
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
    public sealed class GetPurchaseUpdatesResponse : Jsonable
    {
        private static AmazonLogger logger = new AmazonLogger("Pi");

        public string RequestId{get;set;}
                public AmazonUserData AmazonUserData{get;set;}
        public List<PurchaseReceipt> Receipts{get;set;}
                public string Status{get;set;}
                public bool HasMore{get;set;}
        
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
                
                objectDictionary.Add("requestId", RequestId);
                objectDictionary.Add("amazonUserData", (AmazonUserData != null) ? AmazonUserData.GetObjectDictionary() : null);
                objectDictionary.Add("receipts", (Receipts != null) ? Jsonable.unrollObjectIntoList(Receipts) : null);
                objectDictionary.Add("status", Status);
                objectDictionary.Add("hasMore", HasMore);
                return objectDictionary;
            } 
            catch(System.ApplicationException ex) 
            {
                throw new AmazonException("Error encountered while getting object dictionary", ex);
            }
        }

        public static GetPurchaseUpdatesResponse CreateFromDictionary(Dictionary<string, object> jsonMap) 
        {
            try 
            {
                if (jsonMap == null)
                {
                    return null;
                }

                var request = new GetPurchaseUpdatesResponse();
                
                
                if(jsonMap.ContainsKey("requestId")) 
                {
                    request.RequestId = (string) jsonMap["requestId"];
                }
                
                if(jsonMap.ContainsKey("amazonUserData")) 
                {
                    request.AmazonUserData = AmazonUserData.CreateFromDictionary(jsonMap["amazonUserData"] as Dictionary<string, object>); 
                }
                
                if(jsonMap.ContainsKey("receipts")) 
                {
                    request.Receipts = PurchaseReceipt.ListFromJson(jsonMap["receipts"] as List<object>); 
                }
                
                if(jsonMap.ContainsKey("status")) 
                {
                    request.Status = (string) jsonMap["status"];
                }
                
                if(jsonMap.ContainsKey("hasMore")) 
                {
                    request.HasMore = (bool) jsonMap["hasMore"];
                }

                return request;
            } 
            catch (System.ApplicationException ex) 
            {
                throw new AmazonException("Error encountered while creating Object from dicionary", ex);
            }
        }

        public static GetPurchaseUpdatesResponse CreateFromJson(string jsonMessage)
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
        

        public static Dictionary<string, GetPurchaseUpdatesResponse> MapFromJson(Dictionary<string, object> jsonMap)
        {
            Dictionary<string, GetPurchaseUpdatesResponse> result = new Dictionary<string, GetPurchaseUpdatesResponse>();
            foreach (var entry in jsonMap)
            {
                GetPurchaseUpdatesResponse value = CreateFromDictionary(entry.Value as Dictionary<string, object>);
                result.Add(entry.Key, value);
            }
            return result;
        }
        
        public static List<GetPurchaseUpdatesResponse> ListFromJson(List<object> array)
        {
            List<GetPurchaseUpdatesResponse> result = new List<GetPurchaseUpdatesResponse>();
            foreach (var e in array)
            {
                result.Add(CreateFromDictionary(e as Dictionary<string, object>));
            }
            return result;
        }
    }
}
