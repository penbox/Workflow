using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Workflow.Models;
using Workflow.ViewModels;

namespace Workflow
{
    public class Workflow : IWorkflow
    {
        private readonly IDbConnection _db;

        public Workflow(IDbConnection db)
        {
            _db = db;
        }

        /// <summary>
        /// 获取可创建流程列表
        /// </summary>
        /// <returns>可创建流程列表</returns>
        public IEnumerable<WorkflowBaseViewModel> GetWorkflowCreateList()
        {
            return new WorkflowBase(_db).GetWorkflowCreateList();
        }

        /// <summary>
        /// 获取可查询流程类型列表
        /// </summary>
        /// <returns>获取可查询流程类型列表</returns>
        public IEnumerable<WorkflowBaseViewModel> GetWorkflowSelectList()
        {
            return new WorkflowBase(_db).GetWorkflowSelectList();
        }

        /// <summary>
        /// 判断当前用户是否可以创建流程
        /// </summary>
        /// <param name="workflowId">工作流id</param>
        /// <param name="userId">用户id</param>
        /// <returns>"":可以创建;"Memo":不能创建,返回创建节点操作者信息</returns>
        public string CanCreate(int workflowId, int userId)
        {
            var creator = new WorkflowNodeOperator(_db).GetCreator(workflowId);
            var userInfo = new ViewUserInfo(_db).Get(userId);
            var creatorFilter = new WorkflowCreatorFilter(creator);
            if (creatorFilter.IsCreator(userInfo))
            {
                return "";
            }

            return creator.Memo;
        }

        public string GetWorkflowImage(int id)
        {
            return new WorkflowBase(_db).GetImage(id);
        }

        public string GetWorkflowImageByRequestId(int requestId)
        {
            return new WorkflowBase(_db).GetImageByRequestId(requestId);
        }

        public PagedList<RequestViewModel> GetRequestList(int rows, int page, int workflowId, int userId)
        {
            return new RequestBase(_db).GetRequestList(rows, page, workflowId, userId);
        }

        public PagedList<RequestBaseListViewModel> GetAllRequestList(int rows, int page, string name, int workflowId, string beginDate, string endDate, int userId, bool archive)
        {
            return new RequestBase(_db).GetAllRequestList(rows, page, name, workflowId, beginDate, endDate, userId, archive);
        }

        public PagedList<RequestBaseListViewModel> GetBacklogRequestList(int rows, int page, string name, int workflowId, string beginDate, string endDate, int userId, bool draft)
        {
            return new RequestBase(_db).GetBacklogRequestList(rows, page, name, workflowId, beginDate, endDate, userId, draft);
        }
    }
}
