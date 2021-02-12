﻿using System.Collections.Generic;
using Deptorygen2.Core.Utilities;

namespace Deptorygen2.Core.Interfaces
{
	public interface IServiceProvider
	{
		IEnumerable<TypeName> GetCapableServiceTypes();
	}
}
