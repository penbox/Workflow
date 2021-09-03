using System;
using System.Collections.Generic;
using System.Text;

namespace Workflow.ViewModels
{
    public class PagedList<T>
    {
        public int Count { get; set; }
        public IEnumerable<T> List { get; set; }
    }
}
