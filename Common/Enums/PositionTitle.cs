﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swarmops.Common.Enums
{
    public enum PositionTitle
    {
        Unknown = 0,

        /// <summary>
        /// Use the default title for the position type (kind-of-commercial titles).
        /// </summary>
        Default,

        /// <summary>
        /// Use titles that are typical of a commercial corporation.
        /// </summary>
        Commercial,

        /// <summary>
        /// Use titles that are customized for nonprofit organizations ("secretary general" instead of "ceo", etc).
        /// </summary>
        Nonprofit,

        /// <summary>
        /// For organization that run a government-like structure (President, ministers, etc).
        /// </summary>
        Government,

        /// <summary>
        /// Use medieval titles (kind of humorous, not to be taken too seriously).
        /// </summary>
        Medieval,

        /// <summary>
        /// Totally customized and not stock localized - look up in PositionTitlesCustom table
        /// </summary>
        Custom,

        /// <summary>
        /// These titles are not actual positions, but UX elements (categories, etc)
        /// </summary>
        UxElement

        // Expand with various title names
    }
}
