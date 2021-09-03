using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;

namespace Workflow.Models
{
    [Table("workflow_node_link")]
    public class WorkflowNodeLink
    {
        public int Id { get; set; }
        public int Workflow_Id { get; set; }
        public int Node_Id { get; set; }
        public string Link_Name { get; set; }
        public int Dest_Node_Id { get; set; }
        //public int Condition_Id { get; set; }
        public string Condition { get; set; }
        public string Equation { get; set; }
        public WorkflowNodeLink() { }

        private readonly IDbConnection _db;
        public WorkflowNodeLink(IDbConnection db)
        {
            _db = db;
        }

        /// <summary>
        /// 获取工作流的所有出口
        /// </summary>
        /// <param name="workflowId">工作流id</param>
        /// <returns>工作流的所有出口</returns>
        public List<WorkflowNodeLink> GetList(int workflowId)
        {
            return _db.Query<WorkflowNodeLink>(
                "select * from workflow_node_link where workflow_id=@workflowId", new { workflowId }).ToList();
        }

        /// <summary>
        /// 判断流程是否满足出口条件
        /// </summary>
        /// <param name="formName">业务表</param>
        /// <param name="requestId">流程id</param>
        /// <param name="link">流程出口</param>
        /// <returns>流程是否满足出口条件</returns>
        public bool Check(string formName, int requestId, WorkflowNodeLink link)
        {
            var count = 0;
            if (link.Condition == "Form")
            {
                count = _db.QueryFirstOrDefault<int>($"select count(*) from {formName} where request_id=@requestId and ({link.Equation})", new { requestId });
            }

            if (link.Condition == "Creator")
            {
                count = _db.QueryFirstOrDefault<int>($"select count(*) from request_base a left join view_user_info b on a.creator=b.id where a.id=@requestId and {link.Equation}", new { requestId });
            }

            return count > 0;
        }
    }
}
