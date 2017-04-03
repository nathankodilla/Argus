using System;
using System.Collections.Generic;
using System.Text;

namespace Argus.EntityFramework
{
	[Flags]
	public enum DatabaseAction
	{
		Insert = 1,
		Update = 2,
		Delete = 4,
		All = Insert | Update | Delete,
	}
}
