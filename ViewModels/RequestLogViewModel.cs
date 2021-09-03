using System;
using System.Collections.Generic;
using System.Text;

namespace Workflow.ViewModels
{
    public class RequestLogViewModel
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public string Operate_Time { get; set; }
        public string Node_Name { get; set; }

        public IEnumerable<ReturnRequestLogReplyViewModel> Request_Log_Reply { get; set; }
    }
}
