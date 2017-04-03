using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Argus.MongoDB
{
	public class MongoDBDataStorageProvider : IArgusDataStorageProvider
	{
		public string ConnectionString { get; set; } = "mongodb://localhost:27017";
		public string Database { get; set; } = "Audit";
		public string Collection { get; set; } = "Events";

		static MongoDBDataStorageProvider()
		{
			ConventionPack pack = new ConventionPack
			{
				new EnumRepresentationConvention(BsonType.String)
			};
			ConventionRegistry.Register("EnumStringConvention", pack, t => true);

			pack = new ConventionPack
			{
				new IgnoreIfNullConvention(true)
			};
			ConventionRegistry.Register("IgnoreNull", pack, t => true);

			BsonClassMap.RegisterClassMap<AuditEvent>(cm =>
			{
				cm.AutoMap();
				cm.MapExtraElementsField(c => c.CustomFields);
			});

			BsonSerializer.RegisterSerializer(typeof(FieldValueChange), new FieldValueChangeSerializer());
		}

		public IMongoCollection<AuditEvent> GetCollection()
		{
			MongoClient client = new MongoClient(ConnectionString);
			IMongoDatabase database = client.GetDatabase(Database);
			return database.GetCollection<AuditEvent>(Collection);
		}

		public void InsertEvent(AuditEvent auditEvent)
		{
			IMongoCollection<AuditEvent> collection = GetCollection();

			collection.InsertOne(auditEvent);
		}

		public IQueryable<AuditEvent> GetEvents()
		{
			return GetCollection().AsQueryable();
		}

		public IQueryable<T> GetEvents<T>() where T : AuditEvent
		{
			MongoClient client = new MongoClient(ConnectionString);
			IMongoDatabase database = client.GetDatabase(Database);
			return database.GetCollection<T>(Collection).AsQueryable();
		}
	}
}
