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
        
        public NestSimplifierResponse RemapIndex<T>(string index) where T : class
        {
            var resp = _client.Map<T>(m => m.Index(index).AutoMap());
            return new NestSimplifierResponse(resp.IsValid, resp.DebugInformation);
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

        public NestSimplifierResponse InsertMany<T>(string index, List<T> insertObjList) where T : class
        {
            var resp = _client.Bulk(b => b
                    .Index(index)
                    .IndexMany(insertObjList));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
        }

        public NestSimplifierResponse UpsertMany<T>(string index, List<T> updateObjList) where T : class
        {
            var resp = _client
                    .Bulk(b => b.Index(index).UpdateMany<T>(
                        updateObjList,
                        (bulkDescriptor, doc) => bulkDescriptor.Doc(doc).Upsert(doc)));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);

        }

        public NestSimplifierResponse UpdateMany<T>(string index, List<T> updateObjList) where T : class
        {
            string[] changedIds = updateObjList
                .Where(w => GetPropertyValue(w, "ID") != null)
                .Select(s => (string)GetPropertyValue(s, "ID"))
                .ToArray();

            var resp = _client
                    .Bulk(b => b.Index(index).UpdateMany<T>(
                        updateObjList,
                        (bulkDescriptor, doc) => bulkDescriptor.Doc(doc)));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
        }

        public NestSimplifierResponse DeleteWhere<T>(string index, string field, string keyWordContains) where T : class
        {
            var resp = _client.DeleteByQuery<T>(s => 
            s.Index(index)
            .Query(p => p
            .MatchPhrase(M => M
            .Field(field)
            .Query(keyWordContains))));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
        }

        public NestSimplifierResponse DeleteById<T>(string index, string id) where T : class
        {
            var resp = _client.Delete<T>(id, d => d.Index(index));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
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
                .Where(w => w.Name.ToUpper() == property.ToUpper())
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
