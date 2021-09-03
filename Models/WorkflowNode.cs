using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;

namespace Workflow.Models
{
    [Table("workflow_node")]
    public class WorkflowNode
    {
        public int Id { get; set; }
        public int Workflow_Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Operator { get; set; }
        public string Next_Action { get; set; }
        public string Url { get; set; }

        public WorkflowNode() { }

        private readonly IDbConnection _db;
        public WorkflowNode(IDbConnection db)
        {
            _db = db;
        }

        public WorkflowNode Get(int id)
        {
            return _db.QuerySingleOrDefault<WorkflowNode>(
                "select * from workflow_node where id=@id", new { id });
        }

        /// <summary>
        /// 获取工作流的所有节点
        /// </summary>
        /// <param name="workflowId">工作流id</param>
        /// <returns>工作流的所有节点</returns>
        public List<WorkflowNode> GetList(int workflowId)
        {
            return _db.Query<WorkflowNode>(
                "select * from workflow_node where workflow_id=@workflowId", new { workflowId }).ToList();
        }

        /// <summary>
        /// 获取节点类型
        /// </summary>
        /// <param name="nodeId">节点id</param>
        /// <returns>节点类型</returns>
        public string GetNodeType(int nodeId)
        {
            return _db.QuerySingleOrDefault<string>(
                "select type from workflow_node where id=@nodeId", new { nodeId });
        }

        public WorkflowNode GetArchiveNode(int workflowId)
        {
            return _db.QueryFirstOrDefault<WorkflowNode>(
                "select * from workflow_node where workflow_id=@workflowId and type='归档'", new { workflowId });
        }
    }
}
