using System;
using System.Collections.Generic;
using System.Text;

namespace Argus.MongoDB
{
	public static class Extensions
	{
		public static ArgusOptions UseMongoDB(this ArgusOptions options, string connectionString = "mongodb://localhost:27017",
			string database = "Audit", string collection = "Events")
		{
			options.StorageProvider = new MongoDBDataStorageProvider()
			{
				ConnectionString = connectionString,
				Collection = collection,
				Database = database
			};
			return options;
		}
	}
}
