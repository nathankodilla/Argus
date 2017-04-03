using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Argus.EntityFramework
{
	public class DatabaseActionEntry
	{
		public string Table { get; set; }

		public Dictionary<string, object> PrimaryKey { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public DatabaseAction DatabaseAction { get; set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<string, FieldValueChange> ColumnValues { get; set; }
		
		[JsonIgnore]
		internal EntityEntry EntityEntry { get; set; }
	}
}
