using System;
using System.Collections.Generic;
using System.Text;

namespace Argus.EntityFramework
{
	internal class EntityFrameworkAuditSettings
	{
		public MemberMode MemberMode { get; set; } = MemberMode.OptOut;
		public string EventType { get; set; }
		public bool IncludeEventType { get; set; } = true;

		public DatabaseAction TrackActions { get; set; } = DatabaseAction.All;
		public bool IncludeDatabase { get; set; } = true;
		public bool IncludeConnectionID { get; set; } = true;
		public bool IncludeTransactionID { get; set; } = true;
	}
}
