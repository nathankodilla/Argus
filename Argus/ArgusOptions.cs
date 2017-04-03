using System;
using System.Collections.Generic;
using System.Text;

namespace Argus
{
	public interface IArgusOptions
	{
	}

	public class ArgusOptions : IArgusOptions
	{
		public IArgusDataStorageProvider StorageProvider { get; set; }
	}
}
