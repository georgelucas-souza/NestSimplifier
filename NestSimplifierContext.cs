using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace NestSimplifier
{
    public class NestSimplifierContext : IDisposable
    {
        private ElasticClient _client;

        private NestConnectionSettings _ConnectionManager;

        public NestSimplifierContext(NestConnectionSettings connectionSettings)
        {
            _ConnectionManager = connectionSettings;

            try
            {
                _client = CreateNewClient();

                var pingClient = _client.Ping();

                if (!pingClient.IsValid)
                {
                    throw new Exception(pingClient.OriginalException.Message);
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Connection Is not valid: {ex.Message}");
            }

        }

        //Implement Functions
        
        public bool RemapIndex<T>(string index) where T : class
        {
            var resp = _client.Map<T>(m => m.Index(index).AutoMap());
            return resp.IsValid;
        }

        public List<T> FindAll<T>(string index) where T : class
        {

            if (_client != null)
            {

                List<T> resultList = new List<T>();

                var searchResult = _client.Search<T>(s => s
                    .Index(index)
                    .From(0).Size(1000)
                    .Query(q => q.MatchAll())
                    .Scroll("5m"));

                if (searchResult.Total > 0)
                {
                    resultList.AddRange(searchResult.Documents);

                    var results = _client.Scroll<T>("10m", searchResult.ScrollId);

                    while (results.Documents.Any())
                    {
                        resultList.AddRange(results.Documents);
                        results = _client.Scroll<T>("10m", searchResult.ScrollId);
                    }
                }

                return resultList;

            }
            else
            {
                return null;
            }
        }

        public List<T> FindListByIds<T>(string index, string[] idList) where T : class
        {

            if (_client != null)
            {

                if ((idList != null) && idList.Count() > 1)
                {
                    var findResponse = _client.Search<T>(s => s
                    .Index(index)
                    .From(0)
                    .Size(idList.Count())
                    .Query(q => q.Ids(i => i.Values(idList)))
                    );

                    if (findResponse.Total > 0)
                    {
                        var result = findResponse.Documents.ToList();
                        return result;
                    }
                    else
                    {
                        return new List<T>();
                    }

                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public bool InsertMany<T>(string index, List<T> insertObjList, out string message) where T : class
        {

            if (_client != null)
            {
                var bulkIndexResponse = _client.Bulk(b => b
                    .Index(index)
                    .IndexMany(insertObjList));

                if (bulkIndexResponse.IsValid && bulkIndexResponse.ApiCall.Success && bulkIndexResponse.DebugInformation.ToLower().Contains("valid nest response"))
                {
                    message = "Success";
                    return true;
                }
                else
                {
                    message = bulkIndexResponse.DebugInformation;
                    return false;
                }
            }
            else
            {
                message = "Cert File does not exists";
                return false;
            }
        }

        public bool UpsertMany<T>(string index, List<T> updateObjList, out string message) where T : class
        {
            var bulkResponse = _client
                    .Bulk(b => b.Index(index).UpdateMany<T>(
                        updateObjList,
                        (bulkDescriptor, doc) => bulkDescriptor.Doc(doc).Upsert(doc)));

            if (bulkResponse.IsValid && bulkResponse.ApiCall.Success && bulkResponse.DebugInformation.ToLower().Contains("valid nest response"))
            {
                message = "Success";
                return true;
            }
            else
            {
                message = bulkResponse.DebugInformation;
                return false;
            }
        }

        public bool UpdateMany<T>(string index, List<T> updateObjList, out string message) where T : class
        {

            string[] changedIds = updateObjList
                .Where(w => GetPropertyValue(w, "ID") != null)
                .Select(s => (string)GetPropertyValue(s, "ID"))
                .ToArray();

            if ((changedIds != null) && changedIds.Count() > 1)
            {

                var bulkResponse = _client
                    .Bulk(b => b.Index(index).UpdateMany<T>(
                        updateObjList,
                        (bulkDescriptor, doc) => bulkDescriptor.Doc(doc)));

                if (bulkResponse.IsValid && bulkResponse.ApiCall.Success && bulkResponse.DebugInformation.ToLower().Contains("valid nest response"))
                {
                    message = "Success";
                    return true;
                }
                else
                {
                    message = bulkResponse.DebugInformation;
                    return false;
                }
            }
            else
            {
                message = "Not Valid object list\nList must have ID as property";
                return false;
            }
        }


        //Class Funtions
        private ElasticClient CreateNewClient()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            IConnectionPool connectionPool;

            if (_ConnectionManager.Uris.Length > 0)
            {
                if (_ConnectionManager.Uris.Length > 1)
                {
                    connectionPool = new SniffingConnectionPool(_ConnectionManager.Uris);
                }
                else
                {
                    connectionPool = new SingleNodeConnectionPool(_ConnectionManager.Uris[0]);
                }
            }
            else
            {
                throw new Exception("Uris can not be null or empty");
            }

            ConnectionSettings settings = new ConnectionSettings(connectionPool);

            if (_ConnectionManager.CertificateFilePath != null)
            {
                X509Certificate certificate = new X509Certificate(_ConnectionManager.CertificateFilePath);
                settings.ServerCertificateValidationCallback(CertificateValidations.AuthorityIsRoot(certificate));
            }
            else
            {
                settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);
            }

            settings.BasicAuthentication(_ConnectionManager.UserName, _ConnectionManager.Password);
            settings.RequestTimeout(_ConnectionManager.ConnectionTimeOut ?? TimeSpan.FromMinutes(1));

            ElasticClient client = new ElasticClient(settings);

            return client;
        }

        private static object GetPropertyValue<T>(T obj, string property) where T : class
        {
            try
            {
                var val = obj.GetType().GetProperties()
                .Where(w => w.Name == property)
                .FirstOrDefault()
                .GetValue(obj);

                return val;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }
}
