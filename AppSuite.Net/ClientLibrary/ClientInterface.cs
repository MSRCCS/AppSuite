/*---------------------------------------------------------------------------
	Copyright 2015 Microsoft

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.                                                     

	File: 
		vHub.Interface.fs
  
	Description: 
		Interface for vHub.

	Author:																	
 		Jin Li, Partner Research Manager
 		Microsoft Research, One Microsoft Way
 		Email: jinl@microsoft.com, Tel. (425) 703-8451
    Date:
        May. 2015
	
 ---------------------------------------------------------------------------*/
using System;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Prajna.Services.Vision.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using Prajna.Service.CoreServices.Data;


namespace Prajna.AppLibrary
{
    /// <summary>
    /// State of a service 
    /// </summary>
    public class ServiceState
    {
        /// <summary>
        /// Whether the service is Live at last observation
        /// </summary>
        private Boolean Live;
        /// <summary>
        /// Last tick when the service state is updated
        /// </summary>
        private Int64 Ticks;

        /// <summary>
        /// If this time has passed, rechecked the liveness of the system 
        /// </summary>
        [System.ComponentModel.DefaultValue(30000)]
        public Int32 CheckForLivenessInMilliseconds { get; set; }

        /// <summary>
        /// Initialize an instance of the ServiceState. We always initialize the Liveness to false, 
        /// and the time of check to be one day old. This will trigger rechecking of the state. 
        /// </summary>
        public ServiceState( )
        {
            Live = false;
            Ticks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerDay;
        }
        /// <summary>
        /// Set the state of the liveness of service 
        /// </summary>
        /// <param name="bLive">Boolean state of the liveness of a service </param>
        private void SetState( Boolean bLive )
        {
            Live = bLive;
            Ticks = DateTime.UtcNow.Ticks; 
        }

        public Boolean State
        {
            get 
            {
                return Live; 
            }
            set
            {
                SetState(value);
            }
        }

        public Boolean NeedsToCheck()
        {
            if (Live)
            {
                return false;
            }
            else
            {
                var ticksCur = DateTime.UtcNow.Ticks;
                return ((ticksCur - Ticks) / TimeSpan.TicksPerMillisecond >= CheckForLivenessInMilliseconds);
            }

        }
    }

    /// <summary>
    /// Information about a server 
    /// </summary>
    public class OneServerInfo
    {
        /// <summary>
        /// HostName
        /// </summary>
        [DataMember]
        public String HostName { get; set; }
        /// <summary>
        /// Information of the Host. 
        /// </summary>
        [DataMember]
        public String HostInfo { get; set; }
    }

    /// <summary>
    /// GatewayHttpInterface is the interface of the App Library. It is in charge of all communication function to the gateway.  
    /// </summary>
    public class GatewayHttpInterface 
    {
        /// <summary>
        /// Page to check when validating internet connectivity
        /// </summary>
        private static String DefaultCheckPage = "http://bing.com/";
        private static String DefaultWebInfoPage = "web/Info";
        private static String CheckToken = "bing.com";
        private static String StatusOK = "OK";
        /// <summary>
        /// Key to access local default Gateway
        /// </summary>
        public static String GatewayKey = "DefaultGateway";
        /// <summary>
        /// Key to access local default GatewayCollections
        /// </summary>
        public static String GatewayCollectionKey = "GatewayCollections";
        /// <summary>
        /// Key to access default service provider
        /// </summary>
        public static String ProviderKey = "DefaultProvider";
        /// <summary>
        /// Key to access default domain
        /// </summary>
        public static String DomainKey = "DefaultDomain";

        ///if the tutorial has run
        public static String tutorialShown = "No";
        /// <summary>
        /// Gateway being used
        /// </summary>
        public String CurrentGateway { get; set; }
        /// <summary>
        /// Current Provider 
        /// </summary>
        public RecogEngine CurrentProvider { get; set; }
        /// <summary>
        /// Information of Current Domain
        /// </summary>
        public RecogInstance CurrentDomain { get; set; }
        /// <summary>
        /// Current Schema used
        /// </summary>
        public Guid CurrentSchema { get; set; }
        /// <summary>
        /// Current Distribution used
        /// </summary>
        public Guid CurrentDistribution { get; set; }
        /// <summary>
        /// Current Aggregation used
        /// </summary>
        public Guid CurrentAggregation { get; set; }

        /// <summary>
        /// Public Customer ID that identified the application that accesses the gateway
        /// </summary>
        public Guid CustomerID { get; set; }
        /// <summary>
        /// A secret customer key that is used to identify the customer that accesses the gateway
        /// </summary>
        public String CustomerKey { get; set; }

        /// <summary>
        /// Current status of the gateway
        /// </summary>
        public ConcurrentDictionary<String, OneServerMonitoredStatus> GatewayCollection { get; set; }

        private Int32 LastRtt { get; set; }
        private String ServiceURI { get; set; }

        private String GetTicks()
        {
            return System.DateTime.UtcNow.Ticks.ToString();
        }

        private String GetRtt()
        {
            return "0";
        }

        private ServiceState NetworkStatus;
        private ServiceState GatewayStatus;
        private ServiceState ProviderStatus;
        private ServiceState InstanceStatus;

        /// <summary>
        /// Status Message 
        /// </summary>
        public String ErrorMessage;
        /// <summary>
        /// Last thrown exception message, if any. 
        /// </summary>
        public String ExceptionMessage; 

        /// <summary>
        /// Encode type T to a bytearray via JSon serializer
        /// </summary>
        /// <typeparam name="T">type of value</typeparam>
        /// <param name="value">object to be serialized</param>
        /// <returns>output bytearray </returns>
       public static byte[] EncodeToBytes<T>( T value)
        {
            using (var stream = new MemoryStream())
            {
                DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(T));
                json.WriteObject(stream, value);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Decode a JSON serialized byte array to an object
        /// </summary>
        /// <typeparam name="Ty">Type of the objedct</typeparam>
        /// <param name="buf">Input bytearray </param>
        /// <returns>Decoded object</returns>
        public static Ty DecodeFromBytes<Ty>(byte[] buf)
        {
            if (Object.ReferenceEquals(buf, null) || buf.Length == 0)
            {
                return default(Ty);
            }
            else
            {
                using (var stream = new MemoryStream(buf, 0, buf.Length, false))
                {
                    DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(Ty));
                    var obj = json.ReadObject(stream);
                    return (Ty)obj;
                }
            }
        }
        /// <summary>
        /// Instantiate on a GatewayHttpInterface class with gateway, customerID and customerKey
        /// </summary>
        /// <param name="gateway">Gateway information</param>
        /// <param name="customerID">Customer ID</param>
        /// <param name="customerKey">Customer Key</param>
        public GatewayHttpInterface( String gateway, Guid customerID, String customerKey )
        {
            NetworkStatus = new ServiceState();
            GatewayStatus = new ServiceState();
            ProviderStatus = new ServiceState();
            InstanceStatus = new ServiceState();
            ErrorMessage = StatusOK;
            // Gateway should not contains http:
            Contract.Assert(gateway.IndexOf(@"http:") < 0);
            // Customer key should be at least 10 characters
            Contract.Assert(customerKey.Length > 10);
            // Customer key should not be zero length
            this.CurrentGateway = gateway;
            this.CustomerID = customerID;
            this.CustomerKey = customerKey;
            this.GatewayCollection = new ConcurrentDictionary<String, OneServerMonitoredStatus>(StringComparer.OrdinalIgnoreCase);
            CurrentSchema = Guid.Empty;
            CurrentDistribution = Guid.Empty;
            CurrentAggregation = Guid.Empty;
            this.LastRtt = 0;
        }

        /// <summary>
        /// Form a service URI
        /// ToDo: Implementation security feature of sending customerKey
        /// </summary>
        /// <param name="serviceString"></param>
        /// <returns></returns>
        private Uri FormServiceURI( String serviceString )
        {

            var sb = new StringBuilder(1024);
            sb.Append( @"http://" );
            sb.Append( this.CurrentGateway );
            sb.Append( @"/Vhub/" );
            sb.Append(serviceString);
            sb.Append(@"/");
            sb.Append( this.CustomerID );
            sb.Append( @"/" );
            var ticks = System.DateTime.UtcNow.Ticks;
            sb.Append( ticks.ToString() );
            sb.Append( @"/" );
            sb.Append( this.LastRtt.ToString() );
            sb.Append( @"/" );
            sb.Append(this.CustomerKey);
            return new Uri(sb.ToString());
        }

        private String ServiceInfoUrl()
        {
            var sb = new StringBuilder(1024);
            sb.Append(@"http://");
            sb.Append(this.CurrentGateway);
            sb.Append(@"/");
            sb.Append(DefaultWebInfoPage);
            return sb.ToString();
        }



        /// <summary>
        /// Retrieve a webpage 
        /// </summary>
        /// <param name="uriString">URL</param>
        /// <returns>content of the URL</returns>
        public async Task<String> GetWebPage(String uriString)
        {
            using (var httpClient = new HttpClient())
            {
                var theUri = new Uri(uriString);
                using (HttpResponseMessage response = await httpClient.GetAsync(theUri, HttpCompletionOption.ResponseContentRead))
                using (HttpContent content = response.Content)
                {
                    String result = await content.ReadAsStringAsync();
                    return result;
                }
            }
        }
        /// <summary>
        /// Return internet Connection status. 
        /// </summary>
        /// <param name="bForce">If true, the Internet connection state will be updated. Otherwise,the Internet connection state will be checked
        /// if it hasn't been connected, and sometime has passed from the last state updated. </param>
        /// <returns> Whether the Internet is connected. </returns>
        public async Task<Boolean> CheckInternetConnection(Boolean bForce)
        {
            if ( bForce || NetworkStatus.NeedsToCheck())
            {
                try
                {
                    var webResult = await GetWebPage(DefaultCheckPage);
                    if (String.IsNullOrEmpty(webResult))
                    {
                        NetworkStatus.State = false;
                        ErrorMessage = "Faulty Internet Connection, can't reach any extern site... ";
                    }
                    else if (webResult.IndexOf(CheckToken, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        NetworkStatus.State = true;
                    }
                    else
                    {
                        NetworkStatus.State = false;
                        ErrorMessage = "Faulty Internet Connection, extern site return incorrection information... ";
                    }
                }
                catch (Exception ex )
                {
                    NetworkStatus.State = false;
                    ExceptionMessage = ex.Message; 
                    ErrorMessage = "Network is not connected... ";
                }
            }
            return NetworkStatus.State;
        }

        /// <summary>
        /// Return Current Gateway Liveness Status. 
        /// </summary>
        /// <param name="bForce">If true, the Gateway state will always be updated. Otherwise, the Gateway state will be updated
        /// if the gateway isn't alive, and sometime has passed from the last state updated. </param>
        /// <returns> Whether the Gateway is live. </returns>
        public async Task<Boolean> CheckGatewayStatus(Boolean bForce)
        {
            if (bForce || GatewayStatus.NeedsToCheck())
            {
                try
                {
                    var webInfoPage = ServiceInfoUrl();
                    var webResult = await GetWebPage(webInfoPage);
                    if (String.IsNullOrEmpty(webResult))
                    {
                        GatewayStatus.State = false;
                        ErrorMessage = String.Format("Gateway {0} is faulty ... ", this.CurrentGateway); 
                    }
                    else 
                    {
                        GatewayStatus.State = true;
                    }
                }
                catch (Exception ex)
                {
                    GatewayStatus.State = false;
                    ExceptionMessage = ex.Message; 
                    ErrorMessage = String.Format("Gateway {0} is not online ... ", this.CurrentGateway);
                }

                if (!GatewayStatus.State)
                {
                    var bNetwork = await CheckInternetConnection(true);
                    return false;
                }

            }
            return GatewayStatus.State;
        }

        /// Calling a VHub WebGet service, exception needs to be handled by calling function 
        /// </summary>
        /// <param name="serviceString">Service String sent to the gateway</param>
        /// <returns>A Memory Stream that holds the returned object (usually Json coded) </returns>
        private async Task<byte[]> ExecuteGetService( String serviceString )
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var theUri = this.FormServiceURI(serviceString);
                    using (HttpResponseMessage response = await httpClient.GetAsync(theUri, HttpCompletionOption.ResponseContentRead))
                        if (response.IsSuccessStatusCode)
                        {
                            using (HttpContent content = response.Content)
                            {
                                var byt = await content.ReadAsByteArrayAsync();
                                return byt;
                            }
                        }
                        else
                        {
                            throw new System.InvalidOperationException(
                                String.Format("Code: {0}, Reason:{1}", response.StatusCode,
                                    response.ReasonPhrase));
                        }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("GatewayHttpInterface.ExecuteGetService fails when try to access service string {0}, with exception {1}", serviceString, e);               
                throw;
            }
        }


        /// <summary>
        /// Calling a VHub WebInvoke interface, exception needs to be handled by calling function 
        /// </summary>
        /// <param name="serviceString">Service String sent to the gateway</param>
        /// <param name="buf">additional content (e.g.,image) that will be sent via WebInvoke interface</param>
        /// <returns>A Memory Stream that holds the returned object (usually Json coded)</returns>
        public async Task<byte[]> ExecuteInvokeService(String serviceString, Byte[] buf)
        {
            try
            { 
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var theUri = this.FormServiceURI(serviceString);

                    ByteArrayContent inpContent = new ByteArrayContent(buf);
                    
                    using (HttpResponseMessage response = await httpClient.PostAsync(theUri, inpContent))
                       
                        if (response.IsSuccessStatusCode)
                        {
                            using (HttpContent content = response.Content)
                            {
                                var byt = await content.ReadAsByteArrayAsync();
                                return byt;
                            }
                        }
                        else
                        {
                            throw new System.InvalidOperationException(
                                String.Format("Code: {0}, Reason:{1}", response.StatusCode,
                                    response.ReasonPhrase));
                        }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("GatewayHttpInterface.ExecuteInvokeService fails when try to access service string {0}, with exception {1}", serviceString, e);
                throw;
            }
        }

        /// <summary>
        /// GetService, allow redirect
        /// </summary>
        /// <param name="serviceString"></param>
        /// <returns></returns>
        private async Task<byte[]> GetService( String serviceString )
        {
            return await ExecuteGetService(serviceString);
        }

        /// <summary>
        /// InvokeService, allow redirect
        /// </summary>
        /// <param name="serviceString">Service String sent to the gateway</param>
        /// <param name="buf">additional content (e.g.,image) that will be sent via WebInvoke interface</param>
        /// <returns></returns>
        private async Task<byte[]> InvokeService(String serviceString, Byte[] buf)
        {
            return await ExecuteInvokeService(serviceString, buf);
        }


        private List<OneServerInfo> CurrentGatewayList()
        {
            var lst = new List<OneServerInfo>();
            foreach ( var kv in this.GatewayCollection )
            {
                var serverInfo = new OneServerInfo();
                var hostname = kv.Key;
                var hostinfo = kv.Value;
                serverInfo.HostName = hostname;
                serverInfo.HostInfo = String.Format("Reliability={0:F2},Rtt={1}ms,Perf={2}ms",
                                                        (float)hostinfo.PeriodAlive / (float)hostinfo.PeriodMonitored,
                                                        hostinfo.RttInMs, hostinfo.PerfInMs);
                lst.Add(serverInfo);
            }

            return lst;
        }

        /// <summary>
        /// Get a list of gateways that are in service 
        /// </summary>
        /// <returns>A list of gateways </returns>
        public async Task<List<OneServerInfo>> GetActiveGateways()
        {
            try
            { 
                var buf = await GetService("GetActiveGateways");
                try
                {
                    var result = DecodeFromBytes<OneServerMonitoredStatus[]>(buf);

                    foreach (var oneResult in result)
                    {
                        this.GatewayCollection[oneResult.HostName] = oneResult;
                    }
                    return CurrentGatewayList();
                }
                catch ( Exception ex )
                {
                    ErrorMessage = String.Format("Failed to decode active gateway list from {0}", this.CurrentGateway);
                    ExceptionMessage = ex.Message;
                    throw (new HttpRequestException(ErrorMessage));
                }
            }
            catch ( Exception ex)
            {
                var errorString = String.Format("Current gateway {0} is not functional...", this.CurrentGateway);
                Debug.WriteLine(errorString);
                ErrorMessage = String.Format("Failed to retrieve active gateway list from {0}", this.CurrentGateway);
                ExceptionMessage = ex.Message;
            }
            // Forcing checking gateway
            var bGateway = await this.CheckGatewayStatus(true);
            throw (new HttpRequestException(ErrorMessage));
        }
        /// <summary>
        /// Get all domains for a particular provider and schema 
        /// </summary>
        /// <param name="providerID">provider </param>
        /// <param name="schemaID">schema </param>
        /// <returns></returns>
        public async Task<Guid[]> GetAllDomainIDs( Guid providerID, Guid schemaID )
        {
            try
            {
                var buf = await GetService("GetAllServiceGuids/" + providerID.ToString() + "/" + schemaID.ToString());
                try
                { 
                    var result = DecodeFromBytes<Guid[]>(buf);
                    return result;
                }
                catch( Exception ex)
                {
                    ErrorMessage = String.Format("Failed to decode Domain ID from gateway {0}, with schema {1}", this.CurrentGateway, schemaID);
                    ExceptionMessage = ex.Message;
                    throw (new HttpRequestException(ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                var errorString = String.Format("Get GetAllDomainIDs fails, current gateway {0}, with exception {1}", this.CurrentGateway, ex);
                Debug.WriteLine(errorString);
                ErrorMessage = String.Format("Failed to retrieve domain ID from gateway {0}, schema {1}", this.CurrentGateway, schemaID);
                ExceptionMessage = ex.Message;
            }
            // Forcing checking gateway
            var bGateway = await this.CheckGatewayStatus(true);
            throw (new HttpRequestException(ErrorMessage));
        }

        /// <summary>
        /// Check if a certain provider is live on the gateway
        /// </summary>
        /// <param name="providerID">Provider ID to be checked</param>
        /// <returns>Liveness state </returns>
        public async Task<Boolean> CheckProviderStatus( Guid providerID )
        {
            try
            {
                var engineList = await GetActiveProviders();
                Boolean bExist = false; 
                foreach( var engine in engineList)
                {
                    if (engine.RecogEngineID == providerID)
                    {
                        bExist = true;
                    }
                }
                if (!bExist)
                {
                    ErrorMessage = String.Format("Provider {0} is not live on gateway {1} ... ", providerID, this.CurrentGateway);
                }
                return bExist;
            }
            catch (Exception ex)
            {
                var errorString = String.Format("CheckProviderStatus fails, current gateway {0}...", this.CurrentGateway);
                Debug.WriteLine(errorString);
                ErrorMessage = errorString;
                ExceptionMessage = ex.Message;
            }
            // Forcing checking gateway
            var bGateway = await this.CheckGatewayStatus(true);
            return false; 
        }
        /// <summary>
        /// Check if the default provider is live on the gateway
        /// </summary>
        /// <returns></returns>
        public async Task<Boolean> CheckProviderStatus()
        {
            if ( Object.ReferenceEquals(this.CurrentProvider, null ))
            {
                return false;
            }
            else
            { 
                return await CheckProviderStatus(this.CurrentProvider.RecogEngineID);
            }
        }
        /// <summary>
        /// Get the a list of providers registered on the current gateway 
        /// </summary>
        /// <returns>A list of provider information </returns>
        public async Task<RecogEngine[]> GetActiveProviders()
        {
            try
            {
                var buf = await GetService("GetActiveProviders" );
                try
                {
                    var result = DecodeFromBytes<RecogEngine[]>(buf);
                    return result;
                }
                catch (Exception ex)
                {
                    var errorString = String.Format("GetActiveProviders fails to decode the provider list from current gateway {0}...", this.CurrentGateway);
                    Debug.WriteLine(errorString);
                    ErrorMessage = errorString;
                    ExceptionMessage = ex.Message;
                }
                throw (new HttpRequestException(ErrorMessage));
            }
            catch (Exception ex)
            {
                var errorString = String.Format("Get GetActiveProviders fails, current gateway {0}, with exception {1}", this.CurrentGateway, ex);
                Debug.WriteLine(errorString);
                ErrorMessage = String.Format("Failed to retrieve provider list from current gateway {0}", this.CurrentGateway);
                ExceptionMessage = ex.Message;
            }

            // Forcing checking gateway
            var bGateway = await this.CheckGatewayStatus(true);
            throw (new HttpRequestException(ErrorMessage));
        }


        
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <returns></returns>
        public async Task<RecogInstance[]> GetWorkingInstances()
        {
            try
            {
                var engineName = Guid.Empty.ToString();
                if (!Object.ReferenceEquals(CurrentProvider, null))
                    engineName = CurrentProvider.RecogEngineName;
                var buf = await GetService(@"GetWorkingInstances/" + engineName);
                try
                {
                    var result = DecodeFromBytes<RecogInstance[]>(buf);
                    return result;
                }
                catch ( Exception ex)
                {
                    var errorString = String.Format("Fails to decode the instance list from current gateway {0}, with engine {1}", this.CurrentGateway, engineName);
                    Debug.WriteLine(errorString);
                    ErrorMessage = errorString;
                    ExceptionMessage = ex.Message;
                    throw (new HttpRequestException(ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                var errorString = String.Format("Exception occurs when retrieving the instance list from gateway {0}", this.CurrentGateway);
                Debug.WriteLine(errorString);
                ErrorMessage = errorString;
                ExceptionMessage = ex.Message;
            }
            var bGateway = await this.CheckGatewayStatus(true);
            throw (new HttpRequestException(ErrorMessage));

        }

        /// <summary>
        /// Check if a certain provider is live on the gateway
        /// </summary>
        /// <param name="providerID">Provider ID to be checked</param>
        /// <returns>Liveness state </returns>
        public async Task<Boolean> CheckInstanceStatus()
        {
            try
            {
                var domainID = Guid.Empty;
                if (!Object.ReferenceEquals(CurrentDomain, null))
                {
                    domainID = CurrentDomain.ServiceID;
                }
                var instanceLists = await GetWorkingInstances();
                Boolean bExist = false;
                foreach (var instance in instanceLists)
                {
                    if (instance.ServiceID == domainID)
                    {
                        bExist = true;
                    }
                }
                if (!bExist)
                {
                    ErrorMessage = String.Format("Instance {0} is not live on gateway {1} ... ", domainID, this.CurrentGateway);
                }
                return bExist;
            }
            catch (Exception ex)
            {
                var errorString = String.Format("Fails to retrieve instance list from gateway {0}...", this.CurrentGateway);
                Debug.WriteLine(errorString);
                ErrorMessage = errorString;
                ExceptionMessage = ex.Message;
            }
            // Forcing checking gateway
            var bGateway = await this.CheckGatewayStatus(true);
            return false;
        }

        /// <summary>
        /// Classify the image (stored in buf). 
        /// </summary>
        /// <param name="providerID"> Guid that identifies service provider. </param>
        /// <param name="schemaID"> Guid that identifies schema in use. </param>
        /// <param name="domainID"> Guid that identifies service domain. </param>
        /// <param name="distributionID"> Guid that identifies the distribution policy to be used.</param>
        /// <param name="aggregationID"> Guid that identifies the aggregation policy to be used.</param>
        /// <param name="buf"> Byte array that is the encoded image to be recognized, in .JPG format. </param>
        /// <returns> A RecogReply structure which encapsultes information returned by the recognizer. </returns>
        public async Task<RecogReply> ProcessAsync(Guid providerID, Guid schemaID, Guid domainID, Guid distributionID, Guid aggregationID, Byte[] buf)
        {
            try
            {
                var returnedInfo = await InvokeService("Process/" + providerID.ToString() + "/" + schemaID.ToString() + domainID.ToString() + distributionID.ToString() + aggregationID.ToString(), buf );
                try
                { 
                    var result = DecodeFromBytes<RecogReply>(returnedInfo);
                    return result;
                }
                catch ( Exception )
                {
                    var result = new RecogReply();
                    result.Description = String.Format("{0}B has returned from gateway {1}, but fails to parse the return message", returnedInfo.Length, this.CurrentGateway);
                    return result; 
                }
            }
            catch (Exception ex)
            {
                var errorString = String.Format("Fails to process a request from gateway {0} ... ", this.CurrentGateway);
                Debug.WriteLine(errorString);
                ErrorMessage = errorString;
                ExceptionMessage = ex.Message;
            }
            // Forcing checking gateway
            var bGateway = await this.CheckGatewayStatus(true);
            throw (new HttpRequestException(ErrorMessage));            
        }

        /// <summary>
        /// Process a request, return only the description in the request. 
        /// </summary>
        /// <param name="providerID"> Guid that identifies service provider. </param>
        /// <param name="schemaID"> Guid that identifies schema in use. </param>
        /// <param name="domainID"> Guid that identifies service domain. </param>
        /// <param name="distributionID"> Guid that identifies the distribution policy to be used.</param>
        /// <param name="aggregationID"> Guid that identifies the aggregation policy to be used.</param>
        /// <param name="buf"> Byte array that is the encoded image to be recognized, in .JPG format. </param>
        /// <returns> Description of the request. </returns>
        public async Task<String> ProcessAsyncString(Guid providerID, Guid schemaID, Guid domainID, Guid distributionID, Guid aggregationID, Byte[] buf)
        {
           
            try
            {
                
                var reqService = "Process/" + providerID.ToString() + "/" 
                    + schemaID.ToString() + "/" 
                    + domainID.ToString() + "/" 
                    + distributionID.ToString() + "/" 
                    + aggregationID.ToString();
                var returnedInfo = await InvokeService(reqService, buf);
              
                var result = DecodeFromBytes<RecogReply>(returnedInfo);
                if ( Object.ReferenceEquals(result, null ))
                {
                    var returnLength = 0; 
                    if (!Object.ReferenceEquals(returnedInfo,null))
                    {
                        returnLength = returnedInfo.Length;
                    }
                    return String.Format("Request service {0} of {1}B return {2}B.",
                        reqService, buf.Length, returnLength);
                }
                else
                    return result.Description;
            }
            catch (Exception e)
            {
                
                var errorString = String.Format("Get ClassifyServiceAsync fails, current gateway {0} with request of {1}B, with exception {2}", 
                    this.CurrentGateway, 
                    buf.Length,
                    e.Message);
                Debug.WriteLine(errorString);
                return errorString;
            }

        }
        /// <summary>
        /// Process a certain media request
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public async Task<String> ProcessRequest( Byte[] buf )
        {
            var providerID = Guid.Empty;
            if ( !Object.ReferenceEquals(CurrentProvider,null ))
            {
                providerID = CurrentProvider.RecogEngineID;
            }
            var domainID = Guid.Empty;
            if ( !Object.ReferenceEquals(CurrentDomain, null))
            {
                domainID = CurrentDomain.ServiceID;
            }
            var result = await ProcessAsyncString(providerID, CurrentSchema, domainID, CurrentDistribution, CurrentAggregation, buf);
            return result; 
        }



    }
}

