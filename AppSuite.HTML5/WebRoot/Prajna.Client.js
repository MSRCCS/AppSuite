var EmptyGUID = "00000000-0000-0000-0000-000000000000";
var PrajnaClient = function(gateway, customID, customKey)
{
    this._gateway = gateway;
    this._customID = customID;
    this._customKey = customKey;
    
    this.FormServiceURL = function(serviceString)
    {
        var dt = new Date().getTime();
        var ticks = (dt * 10000) + 621355968000000000;
        var url = "http://" + this._gateway + 
                    "/Vhub/" + serviceString + 
                    "/" + this._customID +
                    "/" + ticks.toString() +
                    "/" + "0" +
                    "/" + this._customKey;
        return url;
    }
    
    this.FormReqServiceString = function(providerID, schemaID, domainID, distributionID, aggregationID)
    {
        var reqService = "Process/" + providerID + "/" 
            + schemaID + "/" 
            + domainID + "/" 
            + distributionID + "/" 
            + aggregationID;
        return reqService;
    }
    
    this.GetActiveProviders = function(callBack)
    {
        var reqUrl = this.FormServiceURL("GetActiveProviders");
        $.get(reqUrl, function (providers) {
            callBack(providers);
        });
    }
    
    this.GetActiveClassifiers = function(callBack)
    {
        var self = this;
        this.GetActiveProviders(function(providers) {
            for (var p in providers)
            {
                var engineName = providers[p].RecogEngineName;
                var reqUrl = self.FormServiceURL("GetWorkingInstances/" + engineName);
                $.get(reqUrl, function (classifiers) {
                    callBack(classifiers);
                });
            }
        });
    }
};