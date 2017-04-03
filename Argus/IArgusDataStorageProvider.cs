using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Argus
{
	public interface IArgusDataStorageProvider
	{
		void InsertEvent(AuditEvent auditEvent);
		IQueryable<AuditEvent> GetEvents();
		IQueryable<T> GetEvents<T>() where T : AuditEvent;
	}
}
