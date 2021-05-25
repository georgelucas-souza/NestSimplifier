# NestSimplifier



[![Version](https://img.shields.io/nuget/v/Nest.Simplifier.NetLib)](https://www.nuget.org/packages/Nest.Simplifier.NetLib)
[![](https://img.shields.io/github/last-commit/georgelucas-souza/NestSimplifier)]()

This package is an extension to [Elasticsearch | NEST Library](https://github.com/elastic/elasticsearch-net/). 

This package makes the usage of NEST library basic CRUD operations easier.

## Usage

```c#
using NestSimplifier;

...

NestConnectionSettings connectionSettings = new NestConnectionSettings
(
    new[] { new Uri("uri1"), new Uri("uri2"), new Uri("uri...n") },
    "YOUR_CONNECTION_USERNAME",
    "YOUR_CONNECTION_PASSWORD",
    TimeSpan.FromMinutes(5), // Optional: Connection Timeout,
    "YOUR_CERTIFICATE_FILE_PATH" // Optional
);

using (var ctx = new NestSimplifierContext(connectionSettings))
{
    string idx = "YOUR_INDEX_NAME";
    var findAllDocs = ctx.FindAll<YOUR_OBJECT_CLASS>(idx);
    
    //Explore ctx.[STUFF] to see more implementations
    
    //If you want to implement your own queries, get access to client using: ctx.ElastickSearchClient
}
```

## License
MIT
