using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Argus.EntityFramework
{
	public class EntityFrameworkAuditEvent : AuditEvent
	{
		[JsonProperty(Order = 101, NullValueHandling = NullValueHandling.Ignore)]
		public string Database { get; set; }

		[JsonProperty(Order = 102, NullValueHandling = NullValueHandling.Ignore)]
		public string ConnectionID { get; set; }
		
		[JsonProperty(Order = 103, NullValueHandling = NullValueHandling.Ignore)]
		public string TransactionID { get; set; }

		[JsonProperty(Order = 110)]
		public List<DatabaseActionEntry> DatabaseActions { get; set; }
	}
}
