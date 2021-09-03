using System;
using System.Collections.Generic;
using System.Text;

namespace Workflow.ViewModels
{
    public class RequestLogViewModelV2
    {
        public int id { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public List<RequestLogAttachmentViewModel> Attachments { get; set; } = new List<RequestLogAttachmentViewModel>();
        public string Attachment { get; set; }
        public string Operate_Time { get; set; }
        public string Node_Name { get; set; }
        public string Type { get; set; } = "";
        public decimal Seq { get; set; }
    }
}
