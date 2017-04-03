using System;
using System.Collections.Generic;
using System.Text;

namespace Argus
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class AuditAttribute : Attribute
	{
		public virtual MemberMode Mode { get; set; } = MemberMode.OptOut;
		public virtual string EventType { get; set; }
		public virtual bool IncludeEventType { get; set; } = true;
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class AuditIncludeAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class AuditIgnoreAttribute : Attribute
	{
	}
}
