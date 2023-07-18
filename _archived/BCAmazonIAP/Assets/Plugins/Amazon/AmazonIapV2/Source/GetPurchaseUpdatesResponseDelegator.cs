
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


using System.Collections.Generic;

namespace com.amazon.device.iap.cpt
{
    public sealed class GetPurchaseUpdatesResponseDelegator : IDelegator
    {
        public readonly GetPurchaseUpdatesResponseDelegate responseDelegate;

        public GetPurchaseUpdatesResponseDelegator(GetPurchaseUpdatesResponseDelegate responseDelegate)
        {
            this.responseDelegate = responseDelegate;
        }

        public void ExecuteSuccess()
        {
        }
        
        public void ExecuteSuccess(Dictionary<string, object> objectDictionary)
        {
            responseDelegate(GetPurchaseUpdatesResponse.CreateFromDictionary(objectDictionary));
        }
        
        public void ExecuteError(AmazonException e)
        {
        }
    }
}
