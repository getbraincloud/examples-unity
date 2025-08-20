// Add this to your brainCloud's App > Design > Cloud Code > Scripts for BrainCloudMarketplace.GetTransactionHistory() to run properly

"use strict";

function main() {
  var response = {};

  // Make sure there is at least searchCriteria to get the profileID and store type for user
  var searchCriteria = data.searchCriteria;

  if (data && searchCriteria && (searchCriteria.type == "googlePlay" || searchCriteria.type == "itunes")) {
    searchCriteria.profileId = bridge.getProfileId(); // Get the user's profile ID from here

    var context = {
      "pagination" : data.pagination,
      "searchCriteria" : searchCriteria,
      "sortCriteria" : data.sortCriteria
    };

    bridge.logDebugJson("Context Built", context);

    var postResults = bridge.getAppStoreServiceProxy().sysGetTransactionsPage(context);
    bridge.logInfoJson("SysGetTransactionPage Post Result", postResults);

    response.success = postResults.status == 200;

    if (response.success) {
      response.transactionPage = postResults.data.results;
    }
    else {
      response.success = false;
      response.errorMessage = "Could not get transaction history.";
    }
  }
  else {
    response.success = false;
    response.errorMessage = "Could not build context to search for transaction history.";
  }

  return response;
}

main();
