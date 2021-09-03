using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper.Contrib.Extensions;
using System.Text;
using Dapper;

namespace Workflow.Models
{
    [Table("workflow_node_operator")]
    public class WorkflowNodeOperator
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Is_Sign { get; set; }
        public string Method { get; set; }
        public string Equation { get; set; }
        public string Memo { get; set; }
        public int Workflow_Id { get; set; }
        public WorkflowNodeOperator() { }

        private readonly IDbConnection _db;

        public WorkflowNodeOperator(IDbConnection db)
        {
            _db = db;
        }

        /// <summary>
        /// 通过逗号分隔的OperatorId字符串，获取节点操作者
        /// </summary>
        /// <param name="operatorString">逗号分隔的OperatorId字符串</param>
        /// <returns>节点操作者</returns>
        public List<WorkflowNodeOperator> GetByString(string operatorString)
        {
            return _db.Query<WorkflowNodeOperator>(
                $"select * from workflow_node_operator where id in ({operatorString})").ToList();
        }

        /// <summary>
        /// 获取工作流创建者
        /// </summary>
        /// <param name="workflowId">工作流id</param>
        /// <returns>工作流创建者</returns>
        public WorkflowNodeOperator GetCreator(int workflowId)
        {
            return _db.QueryFirstOrDefault<WorkflowNodeOperator>(
                "select * from workflow_node_operator where workflow_id=@workflowId and type='创建节点操作者'", new { workflowId });
        }
    }
}
