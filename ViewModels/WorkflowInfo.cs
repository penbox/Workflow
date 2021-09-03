using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Workflow.Models;

namespace Workflow.ViewModels
{
    public class WorkflowInfo
    {
        private WorkflowBase _workflowBase;
        private List<WorkflowNode> _workflowNodes;
        private List<WorkflowNodeLink> _workflowNodeLinks;

        private IDbConnection _db;
        public WorkflowInfo(IDbConnection db)
        {
            _db = db;
        }

        /// <summary>
        /// 获取工作流相关信息
        /// </summary>
        /// <param name="workflowId">工作流id</param>
        /// <returns>工作流相关信息</returns>
        public WorkflowInfo Get(int workflowId)
        {
            _workflowBase = new WorkflowBase(_db).Get(workflowId);
            _workflowNodes = new WorkflowNode(_db).GetList(workflowId);
            _workflowNodeLinks = new WorkflowNodeLink(_db).GetList(workflowId);
            return this;
        }

        /// <summary>
        /// 通过流程id获取工作流相关信息
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>工作流相关信息</returns>
        public WorkflowInfo GetByRequestId(int requestId)
        {
            var requestBase = new RequestBase(_db).Get(requestId);
            return Get(requestBase.Workflow_Id);
        }

        /// <summary>
        /// 获取创建节点
        /// </summary>
        /// <returns>创建节点</returns>
        public WorkflowNode GetCreateNode()
        {
            return _workflowNodes.FirstOrDefault(node => node.Type == "创建");
        }

        /// <summary>
        /// 获取目标节点
        /// </summary>
        /// <param name="nodeLink">节点出口</param>
        /// <returns>目标节点</returns>
        public WorkflowNode GetDestNode(WorkflowNodeLink nodeLink)
        {
            return _workflowNodes.FirstOrDefault(node => node.Id == nodeLink.Dest_Node_Id);
        }

        /// <summary>
        /// 获取下一个出口
        /// </summary>
        /// <param name="requestBase">流程信息</param>
        /// <returns>下一个出口</returns>
        public WorkflowNodeLink GetNextNodeLink(RequestBase requestBase)
        {
            var nodeLinks = _workflowNodeLinks.Where(link => link.Node_Id == requestBase.Current_Node_Id).ToList();
            if (nodeLinks.Count == 1)
            {
                return nodeLinks.First();
            }

            foreach (var link in nodeLinks)
            {
                //var condition = new WorkflowNodeLinkCondition(_db).Get(link.Condition_Id);
                //if (condition != null && condition.Check(_workflowBase.Form_Name, requestBase.Id))
                //{
                //    return link;
                //}
                if (!string.IsNullOrEmpty(link.Condition)
                    && new WorkflowNodeLink(_db).Check(_workflowBase.Form_Name, requestBase.Id, link))
                {
                    return link;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取抄送节点的下一个出口
        /// </summary>
        /// <param name="requestBase">流程信息</param>
        /// <returns>下一个出口</returns>
        public WorkflowNodeLink GetCopyToNextNodeLink(RequestBase requestBase)
        {
            var nodeLink = _workflowNodeLinks.First(link => link.Node_Id == requestBase.Current_Node_Id);
            var copyToNodeLink = _workflowNodeLinks.First(link => link.Node_Id == nodeLink.Dest_Node_Id);

            return copyToNodeLink;
        }
    }
}
