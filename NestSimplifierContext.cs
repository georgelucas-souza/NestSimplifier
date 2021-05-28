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
        public ElasticClient ElasticSearchClient { get; private set; }

        private NestConnectionSettings _ConnectionManager;

        public NestSimplifierContext(NestConnectionSettings connectionSettings)
        {
            _ConnectionManager = connectionSettings;

            try
            {
                ElasticSearchClient = CreateNewClient();

                var pingClient = ElasticSearchClient.Ping();

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

        /// <summary>
        /// Refresh Document properties.
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <returns></returns>
        public NestSimplifierResponse RemapIndex<T>(string index) where T : class
        {
            var resp = ElasticSearchClient.Map<T>(m => m.Index(index).AutoMap());
            return new NestSimplifierResponse(resp.IsValid, resp.DebugInformation);
        }


        /// <summary>
        /// Find all document in Index.
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="forceRetrieveId">Force get HIT id and store in a variable named 'ID', this variable must be declared in your object class as String.</param>
        /// <returns></returns>
        public List<T> FindAll<T>(string index, bool forceRetrieveId = false) where T : class
        {
            List<T> resultList = new List<T>();

            var searchResult = ElasticSearchClient.Search<T>(s => s
                .Index(index)
                .From(0).Size(1000)
                .Query(q => q.MatchAll())
                .Scroll("5m"));

            if (searchResult.Total > 0)
            {
                resultList.AddRange(searchResult.Documents);


                var results = ElasticSearchClient.Scroll<T>("10m", searchResult.ScrollId);

                while (results.Documents.Any())
                {
                    if (forceRetrieveId)
                    {
                        resultList.AddRange(results.Hits.Select(s => s.Source.SetPropertyValue("ID", s.Id)));

                    }
                    else
                    {
                        resultList.AddRange(results.Hits.Select(s => s.Source));
                    }



                    results = ElasticSearchClient.Scroll<T>("10m", searchResult.ScrollId);
                }
            }

            return resultList;
        }



        /// <summary>
        /// Find all document in Index that contains specific ID's
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="idList">Array of ID's.</param>
        /// <param name="forceRetrieveId">Force get HIT id and store in a variable named 'ID', this variable must be declared in your object class as String.</param>
        /// <returns></returns>
        public List<T> FindByListId<T>(string index, string[] idList, bool forceRetrieveId = false) where T : class
        {
            if ((idList != null) && idList.Count() > 0)
            {
                var findResponse = ElasticSearchClient.Search<T>(s => s
                .Index(index)
                .From(0)
                .Size(idList.Count())
                .Query(q => q.Ids(i => i.Values(idList)))
                );

                if (findResponse.Total > 0)
                {
                    if (forceRetrieveId)
                    {
                        var result = findResponse.Hits.Select(s => s.Source.SetPropertyValue("ID", s.Id)).ToList();
                        return result;
                    }
                    else
                    {
                        var result = findResponse.Hits.Select(s => s.Source).ToList();
                        return result;
                    }
                }
                else
                {
                    return new List<T>();
                }

            }
            else
            {
                return new List<T>();
            }
        }



        /// <summary>
        /// Find All document in Index where some field contains specific keywords
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="field">'Column/Field' name.</param>
        /// <param name="keyWordContains">Key word.</param>
        /// <param name="forceRetrieveId">Force get HIT id and store in a variable named 'ID', this variable must be declared in your object class as String.</param>
        /// <returns></returns>
        public List<T> FindWhere<T>(string index, string field, string keyWordContains, bool forceRetrieveId = false) where T : class
        {
            var resp = ElasticSearchClient.Search<T>(s =>
            s.Index(index)
            .Query(p => p
            .MatchPhrase(M => M
            .Field(field)
            .Query(keyWordContains))));

            if ((resp != null) && resp.Hits.Count > 0)
            {
                if (forceRetrieveId)
                {
                    var objList = resp.Hits.Select(s => s.Source.SetPropertyValue("ID", s.Id)).ToList();
                    return objList;
                }
                else
                {
                    var objList = resp.Hits.Select(s => s.Source).ToList();
                    return objList;
                }
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Find document with specific ID
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="id">Document ID</param>
        /// <param name="forceRetrieveId">Force get HIT id and store in a variable named 'ID', this variable must be declared in your object class as String.</param>
        /// <returns></returns>
        public T FindById<T>(string index, string id, bool forceRetrieveId = false) where T : class
        {
            var resp = ElasticSearchClient.Get<T>(id, d => d.Index(index));

            if (forceRetrieveId)
            {
                return resp.Source.SetPropertyValue("ID", resp.Id);
            }
            else
            {
                return resp.Source;
            }
        }


        /// <summary>
        /// Insert a list of object class.
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="insertObjList">Object Class list.</param>
        /// <returns></returns>
        public NestSimplifierResponse InsertMany<T>(string index, List<T> insertObjList) where T : class
        {
            var resp = ElasticSearchClient.Bulk(b => b
                    .Index(index)
                    .IndexMany(insertObjList));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
        }


        /// <summary>
        /// Update OR Insert a Document a list of object class. Assuming ID is known
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="updateObjList">Object Class list.</param>
        /// <returns></returns>
        public NestSimplifierResponse UpsertMany<T>(string index, List<T> updateObjList, bool forceNullIdToInsert = false) where T : class
        {
            if (forceNullIdToInsert)
            {
                NestSimplifierResponse insertResponse = new NestSimplifierResponse(true, string.Empty);
                NestSimplifierResponse UpsertResponse = null;


                List<T> insertList = new List<T>();
                List<T> upsertList = new List<T>();

                foreach (var item in updateObjList)
                {
                    if (item.PropertyNotExistsOrIsNullOrEmpty("ID", true))
                    {
                        insertList.Add(item);
                    }
                    else
                    {
                        upsertList.Add(item);
                    }
                }

                if ((insertList != null) && insertList.Count > 0)
                {
                    insertResponse = InsertMany<T>(index, insertList);
                }

                var respUpsert = ElasticSearchClient
                        .Bulk(b => b.Index(index).UpdateMany<T>(
                            upsertList,
                            (bulkDescriptor, doc) => bulkDescriptor.Doc(doc).Upsert(doc)));

                UpsertResponse = new NestSimplifierResponse((respUpsert.IsValid && respUpsert.ApiCall.Success), respUpsert.DebugInformation);

                return new NestSimplifierResponse((UpsertResponse.IsValid && insertResponse.IsValid), (!string.IsNullOrEmpty(insertResponse.Message) ? "INSERT: " + insertResponse.Message + Environment.NewLine + "UPSERT: " + UpsertResponse.Message : respUpsert.DebugInformation));
            }
            else
            {
                var respUpsert = ElasticSearchClient
                        .Bulk(b => b.Index(index).UpdateMany<T>(
                            updateObjList,
                            (bulkDescriptor, doc) => bulkDescriptor.Doc(doc).Upsert(doc)));

                return new NestSimplifierResponse((respUpsert.IsValid && respUpsert.ApiCall.Success), respUpsert.DebugInformation);
            }
            

        }


        /// <summary>
        /// Update a Document with a list of object class. Assuming ID is known
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="updateObjList">Object Class list.</param>
        /// <returns></returns>
        public NestSimplifierResponse UpdateMany<T>(string index, List<T> updateObjList) where T : class
        {
            string[] changedIds = updateObjList
                .Where(w => w.GetPropertyValue("ID") != null)
                .Select(s => (string)s.GetPropertyValue("ID"))
                .ToArray();

            var resp = ElasticSearchClient
                    .Bulk(b => b.Index(index).UpdateMany<T>(
                        updateObjList,
                        (bulkDescriptor, doc) => bulkDescriptor.Doc(doc)));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
        }


        /// <summary>
        /// Delete all documents whre field contains value
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="field">'Column/Field' name</param>
        /// <param name="keyWordContains">Keyword.</param>
        /// <returns></returns>
        public NestSimplifierResponse DeleteWhere<T>(string index, string field, string keyWordContains) where T : class
        {
            var resp = ElasticSearchClient.DeleteByQuery<T>(s =>
            s.Index(index)
            .Query(p => p
            .MatchPhrase(M => M
            .Field(field)
            .Query(keyWordContains))));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
        }



        /// <summary>
        /// Delete document with specific ID
        /// </summary>
        /// <typeparam name="T">Object Class.</typeparam>
        /// <param name="index">Document Index.</param>
        /// <param name="id">Document ID.</param>
        /// <returns></returns>
        public NestSimplifierResponse DeleteById<T>(string index, string id) where T : class
        {
            var resp = ElasticSearchClient.Delete<T>(id, d => d.Index(index));

            return new NestSimplifierResponse((resp.IsValid && resp.ApiCall.Success), resp.DebugInformation);
        }

        public NestSimplifierResponse DeleteMany<T>(string index, List<T> deleteItemList) where T : class
        {
            var resp = ElasticSearchClient.Bulk(b => b.Index(index).DeleteMany<T>(deleteItemList));

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


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }
}
