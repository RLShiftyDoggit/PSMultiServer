using System;
using System.Collections.Generic;

using MultiServer.Addons.Org.BouncyCastle.Asn1.Cms;

namespace MultiServer.Addons.Org.BouncyCastle.Cms
{
	/**
	 * Basic generator that just returns a preconstructed attribute table
	 */
	public class SimpleAttributeTableGenerator
		: CmsAttributeTableGenerator
	{
		private readonly AttributeTable attributes;

		public SimpleAttributeTableGenerator(
			AttributeTable attributes)
		{
			this.attributes = attributes;
		}

		public virtual AttributeTable GetAttributes(IDictionary<CmsAttributeTableParameter, object> parameters)
		{
			return attributes;
		}
	}
}
