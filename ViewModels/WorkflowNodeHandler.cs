using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Workflow.Models;

namespace Workflow.ViewModels
{
    public class WorkflowNodeHandler
    {
        /// <summary>
        /// 节点操作者id列表
        /// </summary>
        public List<int> NodeHandler=new List<int>();

        /// <summary>
        /// 会签/非会签/依次审批
        /// </summary>
        public string Status;

        private readonly IDbConnection _db;

        public WorkflowNodeHandler(IDbConnection db)
        {
            _db = db;
        }

        public WorkflowNodeHandler Get(WorkflowNode node, RequestBase requestBase)
        {
            var nodeOperatorList = new List<List<WorkflowNodeOperator>>();

            if (node.Type == "创建")
            {
                var creator = new WorkflowNodeOperator
                {
                    Method = "Self",
                    Equation = ""
                };

                var list = new List<WorkflowNodeOperator> { creator };
                nodeOperatorList.Add(list);
            }
            else
            {
                //获取"|"分隔的多组操作者，每组操作者是由","分隔的多个OperatorId
                var operatorStrings = node.Operator.Split('|');
                foreach (var operatorString in operatorStrings)
                {
                    nodeOperatorList.Add(new WorkflowNodeOperator(_db).GetByString(operatorString));
                }
            }

            //依次筛选各组操作者
            foreach (var nodeOperators in nodeOperatorList)
            {
                foreach (var op in nodeOperators)
                {
                    var filter = new WorkflowOperatorFilter(_db, op);
                    NodeHandler.AddRange(filter.GetOperator(requestBase));
                }

                //任意一组操作者条件成立则结束筛选
                if (NodeHandler.Count > 0)
                {
                    Status = nodeOperators.First().Is_Sign;
                    break;
                }
            }

            return this;
        }
    }
}
