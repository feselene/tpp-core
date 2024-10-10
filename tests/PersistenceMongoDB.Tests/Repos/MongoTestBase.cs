using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;
using Persistence;
using PersistenceMongoDB.Serializers;

namespace PersistenceMongoDB.Tests.Repos
{
    /// <summary>
    /// Base class for integration tests that operate on an actual MongoDB server.
    /// Connects to a local MongoDB instance running in replica set mode on localhost:27017.
    /// Provides methods for creating and cleaning up temporary databases for testing.
    /// </summary>
    [Category("IntegrationTest")]
    public abstract class MongoTestBase
    {
        private const string ReplicaSetName = "rs0";
        private static readonly Random Random = new Random();

        private MongoClient _client = null!;
        private readonly List<string> _temporaryDatabases = new List<string>();

        /// <summary>
        /// Sets up the MongoDB client to connect to the replica set.
        /// Fails the tests if the MongoDB instance is not reachable.
        /// </summary>
        [OneTimeSetUp]
        public void SetUpMongoClient()
        {
            // Register custom serializers if needed
            CustomSerializers.RegisterAll();

            try
            {
                // Configure MongoDB client settings to connect in replica set mode
                MongoClientSettings settings = MongoClientSettings
                    .FromConnectionString($"mongodb://localhost:27017/?replicaSet={ReplicaSetName}");
                settings.LinqProvider = LinqProvider.V3;

                // Initialize MongoDB client
                _client = new MongoClient(settings);

                // Test connection with a timeout to ensure MongoDB is running
                bool success = _client.ListDatabaseNamesAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
                if (!success)
                {
                    Assert.Fail("MongoDB instance not available on localhost:27017. Failing integration tests.");
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failing tests due to MongoDB connection failure: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up temporary databases created during the test session.
        /// </summary>
        [OneTimeTearDown]
        public void TearDownTempDatabases()
        {
            // Asynchronously drop all temporary databases
            Task.WhenAll(_temporaryDatabases.Select(db => _client.DropDatabaseAsync(db))).Wait();
        }

        /// <summary>
        /// Creates a temporary database with a unique name for testing purposes.
        /// Databases are automatically registered for cleanup.
        /// </summary>
        /// <returns>A new temporary IMongoDatabase instance.</returns>
        protected IMongoDatabase CreateTemporaryDatabase()
        {
            string dbName = "testdb-" + Random.Next();
            _temporaryDatabases.Add(dbName);
            return _client.GetDatabase(dbName);
        }
    }
}
