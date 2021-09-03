using System;
using System.Collections.Generic;
using System.Text;

namespace Workflow.ViewModels
{
    public class RequestBaseListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Create_Time { get; set; }
        public string Creator { get; set; }
        public string Node_Name { get; set; }
        public string Status { get; set; }
        public string Workflow_Name { get; set; }
        public string Url { get; set; }
        public bool Reply { get; set; }
        public bool By_Reply { get; set; }
    }
}
