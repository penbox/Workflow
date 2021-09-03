using System;
using System.Collections.Generic;
using System.Text;

namespace Workflow.ViewModels
{
    public class WorkflowBaseViewModel
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
    }
}
