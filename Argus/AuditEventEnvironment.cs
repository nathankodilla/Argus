using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Argus
{
	public class AuditEventEnvironment
	{
		[JsonProperty(Order = 1, NullValueHandling = NullValueHandling.Ignore)]
		public string MachineName { get; set; }

		[JsonProperty(Order = 2, NullValueHandling = NullValueHandling.Ignore)]
		public string Username { get; set; }

		[JsonProperty(Order = 3, NullValueHandling = NullValueHandling.Ignore)]
		public string AssemblyName { get; set; }

		[JsonProperty(Order = 4, NullValueHandling = NullValueHandling.Ignore)]
		public string Culture { get; set; }
	}
}
