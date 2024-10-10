using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

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
        private MongoClient _client = null!;
        private readonly List<string> _temporaryDatabases = new List<string>();

        /// <summary>
        /// Sets up the MongoDB client to connect to the replica set.
        /// Fails the tests if the MongoDB instance is not reachable.
        /// </summary>
        [OneTimeSetUp]
        public void SetUpMongoClient()
        {
            var connectionString = "mongodb://root:example@localhost:27017/?replicaSet=rs0";
            try
            {
                var settings = MongoClientSettings.FromConnectionString(connectionString);
                _client = new MongoClient(settings);

                // Check if the replica set is initialized
                if (!IsReplicaSetInitialized())
                {
                    InitializeReplicaSet();
                }

                // Test connection with a timeout to ensure MongoDB is running
                _client.ListDatabaseNames(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to connect to MongoDB: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up temporary databases created during the test session.
        /// </summary>
        [OneTimeTearDown]
        public void TearDownTempDatabases()
        {
            Task.WhenAll(_temporaryDatabases.Select(db => _client.DropDatabaseAsync(db))).Wait();
        }

        /// <summary>
        /// Creates a temporary database with a unique name for testing purposes.
        /// Databases are automatically registered for cleanup.
        /// </summary>
        /// <returns>A new temporary IMongoDatabase instance.</returns>
        protected IMongoDatabase CreateTemporaryDatabase()
        {
            string dbName = "testdb-" + Guid.NewGuid();
            _temporaryDatabases.Add(dbName);
            return _client.GetDatabase(dbName);
        }

        /// <summary>
        /// Checks if the replica set has already been initialized.
        /// </summary>
        private bool IsReplicaSetInitialized()
        {
            try
            {
                var adminDb = _client.GetDatabase("admin");
                var command = new BsonDocument("replSetGetStatus", 1);
                adminDb.RunCommand<BsonDocument>(command);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Initializes the MongoDB replica set.
        /// </summary>
        private void InitializeReplicaSet()
        {
            var adminDb = _client.GetDatabase("admin");
            var config = new BsonDocument
            {
                { "_id", ReplicaSetName },
                { "members", new BsonArray
                    {
                        new BsonDocument { { "_id", 0 }, { "host", "localhost:27017" } }
                    }
                }
            };

            adminDb.RunCommand<BsonDocument>(new BsonDocument { { "replSetInitiate", config } });
            Thread.Sleep(5000); // Wait for the replica set to initialize
        }
    }
}
