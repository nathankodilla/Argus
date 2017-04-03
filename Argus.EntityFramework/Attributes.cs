using System;
using System.Collections.Generic;
using System.Text;

namespace Argus.EntityFramework
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class AuditDbContextAttribute : AuditAttribute
	{
		internal EntityFrameworkAuditSettings Settings = new EntityFrameworkAuditSettings();

		public override MemberMode Mode
		{
			get { return Settings.MemberMode; }
			set { Settings.MemberMode = value; }
		}

		public override string EventType
		{
			get { return Settings.EventType; }
			set { Settings.EventType = value; }
		}

		public override bool IncludeEventType
		{
			get { return Settings.IncludeEventType; }
			set { Settings.IncludeEventType = value; }
		}

		public DatabaseAction TrackActions
		{
			get { return Settings.TrackActions; }
			set { Settings.TrackActions = value; }
		}

		public bool IncludeDatabase
		{
			get { return Settings.IncludeDatabase; }
			set { Settings.IncludeDatabase = value; }
		}

		public bool IncludeConnectionID
		{
			get { return Settings.IncludeConnectionID; }
			set { Settings.IncludeConnectionID = value; }
		}

		public bool IncludeTransactionID
		{
			get { return Settings.IncludeTransactionID; }
			set { Settings.IncludeTransactionID = value; }
		}
	}

	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public class AuditDatabaseActionsAttribute : Attribute
	{
		public DatabaseAction TrackActions { get; private set; }

		public AuditDatabaseActionsAttribute(DatabaseAction databaseActions)
		{
			TrackActions = databaseActions;
		}
	}

	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public class AuditIgnoreUpdateAttribute : Attribute
	{
	}
}
