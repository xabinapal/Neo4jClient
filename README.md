### Neo4jClient for .NET 4.5
Forked from https://github.com/Readify/Neo4jClient and updated to target .NET 4.5, Neo4jClient doesn't need anymore Microsoft HTTP Client Libraries from NuGet. That library is a real pain in the ass when creating Azure Projects, because it needs to install the annoying Microsoft.Bcl.Build package, being impossible to compile and publish to Azure without using some hacky workarounds.

---

A .NET client for neo4j. Supports basic CRUD operations, Cypher and Gremlin queries via fluent interfaces, and some indexing operations.

Grab the latest drop straight from the `Neo4jClient` package on [NuGet](http://nuget.org/List/Packages/Neo4jClient).

Read [our wiki doco](https://github.com/Readify/Neo4jClient/wiki).

Watch [our public CI build](http://teamcity.tath.am/project.html?projectId=project11&guest=1).

Licensed under MS-PL. See `LICENSE` in the root of this repository for full
license text.
