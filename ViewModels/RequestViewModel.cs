using System;
using System.Collections.Generic;
using System.Text;

namespace Workflow.ViewModels
{
    public class RequestViewModel
    {
        public string Name { get; set; }
        public string Workflow_Name { get; set; }
        public string Creator { get; set; }
        public string Current_Node_Name { get; set; }
        public string Create_Time { get; set; }
    }
}
