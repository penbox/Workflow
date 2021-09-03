using System;
using System.Collections.Generic;
using System.Text;
using Workflow.ViewModels;

namespace Workflow
{
    public interface IWorkflow
    {
        /// <summary>
        /// 获取可创建流程列表
        /// </summary>
        /// <returns>可创建流程列表</returns>
        IEnumerable<WorkflowBaseViewModel> GetWorkflowCreateList();

        /// <summary>
        /// 获取可查询流程类型列表
        /// </summary>
        /// <returns>获取可查询流程类型列表</returns>
        IEnumerable<WorkflowBaseViewModel> GetWorkflowSelectList();

        /// <summary>
        /// 判断用户是否可以创建流程
        /// </summary>
        /// <param name="workflowId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <returns>提示信息</returns>
        string CanCreate(int workflowId, int userId);

        /// <summary>
        /// 获取所有流程列表
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="page"></param>
        /// <param name="workflowId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        PagedList<RequestViewModel> GetRequestList(int rows, int page, int workflowId, int userId);

        /// <summary>
        /// 获取所有用户相关流程列表
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="page">页数</param>
        /// <param name="name">流程名称</param>
        /// <param name="workflowId">工作流id</param>
        /// <param name="beginDate">开始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <param name="userId">用户id</param>
        /// <param name="archive">归档流程</param>
        /// <returns>所有流程列表</returns>
        PagedList<RequestBaseListViewModel> GetAllRequestList(int rows, int page, string name, int workflowId, string beginDate, string endDate, int userId, bool archive);

        /// <summary>
        /// 获取用户待办流程列表
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="page">页数</param>
        /// <param name="name">流程名称</param>
        /// <param name="workflowId">工作流id</param>
        /// <param name="beginDate">开始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <param name="userId">用户id</param>
        /// <param name="draft">草稿流程</param>
        /// <returns>待办流程列表</returns>
        PagedList<RequestBaseListViewModel> GetBacklogRequestList(int rows, int page, string name, int workflowId, string beginDate, string endDate, int userId,bool draft);

        /// <summary>
        /// 获取工作流流程图
        /// </summary>
        /// <param name="id">工作流id</param>
        /// <returns>工作流流程图</returns>
        string GetWorkflowImage(int id);
    }
}
