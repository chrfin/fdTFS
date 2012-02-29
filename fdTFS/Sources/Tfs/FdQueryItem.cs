using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace fdTFS.Sources.Tfs
{
    public class FdQueryItem
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        /// <remarks>Documented by CFI, 2011-01-04</remarks>
        public QueryItem Item { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FdQueryItem"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <remarks>Documented by CFI, 2011-01-04</remarks>
        public FdQueryItem(QueryItem item)
        {
            Item = item;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        /// <remarks>Documented by CFI, 2011-01-04</remarks>
        public override string ToString()
        {
            return Item.Name + " (" + Item.Parent.Path + ")";
        }
    }
}
