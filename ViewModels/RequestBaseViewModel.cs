using System;
using System.Collections.Generic;
using System.Text;

namespace Workflow.ViewModels
{
    public class RequestBaseViewModel
    {
        public string Creator { get; set; }
        public string Create_Time { get; set; }
        public string Title { get; set; }
        public string Workflow_Name { get; set; }
        public string Status { get; set; }
    }
}
