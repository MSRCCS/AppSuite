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
using vHub.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;
using Prajna.Service.CoreServices.Data;

namespace VMHubClientLibrary
{
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
        /// TODO: Write Comment
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

        /// <summary>
        /// Write Comment
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
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

        /// <summary>
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
        /// Get the current gateways that are in service
        /// </summary>
        /// <returns></returns>
        public async Task<List<OneServerInfo>> GetActiveGateways()
        {
            try
            { 
                var buf = await GetService("GetActiveGateways");
                var result = DecodeFromBytes<OneServerMonitoredStatus[]>(buf);

                foreach ( var oneResult in result )
                {
                    this.GatewayCollection[oneResult.HostName] = oneResult;
                }
                return CurrentGatewayList();
            }
            catch ( Exception e)
            {
                var errorString = String.Format("Get Active Gateways fails, current gateway {0}, with exception {1}", this.CurrentGateway, e);
                Debug.WriteLine(errorString );
                return CurrentGatewayList();
            }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <param name="providerID"></param>
        /// <param name="schemaID"></param>
        /// <returns></returns>
        public async Task<Guid[]> GetAllDomainIDs( Guid providerID, Guid schemaID )
        {
            try
            {
                var buf = await GetService("GetAllServiceGuids/" + providerID.ToString() + "/" + schemaID.ToString());

                var result = DecodeFromBytes<Guid[]>(buf);
                return result;

            }
            catch (Exception e)
            {
                var errorString = String.Format("Get GetAllDomainIDs fails, current gateway {0}, with exception {1}", this.CurrentGateway, e);
                Debug.WriteLine(errorString);
                return null;
            }
        }
        /// <summary>
        /// TODO: Write Comment
        /// </summary>
        /// <returns></returns>
        public async Task<RecogEngine[]> GetActiveProviders()
        {
            try
            {
                var buf = await GetService("GetActiveProviders" );
                var result = DecodeFromBytes<RecogEngine[]>(buf);
                return result; 
            }
            catch (Exception e)
            {
                var errorString = String.Format("Get GetActiveProviders fails, current gateway {0}, with exception {1}", this.CurrentGateway, e);
                Debug.WriteLine(errorString);
                return null;
            }
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
                var result = DecodeFromBytes<RecogInstance[]>(buf);
                return result;
            }
            catch (Exception e)
            {
                var errorString = String.Format("Get GetActiveProviders fails, current gateway {0}, with exception {1}", this.CurrentGateway, e);
                Debug.WriteLine(errorString);
                return null;
            }
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

                var result = DecodeFromBytes<RecogReply>(returnedInfo);
                return result;
            }
            catch (Exception e)
            {
                var errorString = String.Format("Get ClassifyServiceAsync fails, current gateway {0}, with exception {1}", this.CurrentGateway, e);
                Debug.WriteLine(errorString);
                return null;
            }

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

/// <summary>
/// TODO: Write Comment
/// </summary>
/// <param name="uriString"></param>
/// <returns></returns>
        public async Task<String> GetWebPage( String uriString )
        {
            using (var httpClient = new HttpClient())
            {
                var theUri = new Uri(this.ServiceURI + uriString );
                using (HttpResponseMessage response = await httpClient.GetAsync(theUri, HttpCompletionOption.ResponseContentRead))
                using (HttpContent content = response.Content)
                {
                    String result = await content.ReadAsStringAsync();
                    return result; 
                }
            }
        }

    }
}

