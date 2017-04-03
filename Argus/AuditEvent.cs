using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Argus
{
	public class AuditEvent
	{
		[JsonProperty(Order = -100, NullValueHandling = NullValueHandling.Ignore)]
		public string EventType { get; set; }

		[JsonProperty(Order = 1, NullValueHandling = NullValueHandling.Ignore)]
		public AuditEventEnvironment Environment { get; set; }

		[JsonProperty(Order = 2, NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? EventTime { get; set; }

		[JsonExtensionData]
		public Dictionary<string, object> CustomFields { get; set; }
	}
}
