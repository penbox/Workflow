using System;
using System.Collections.Generic;
using System.Text;
using Workflow.ViewModels;

namespace Workflow
{
    public interface IRequest
    {
        /// <summary>
        /// 创建流程
        /// </summary>
        /// <param name="workflowId">工作流id</param>
        /// <param name="title">标题</param>
        /// <param name="remark">签字意见</param>
        /// <param name="userId">用户id</param>
        /// <returns>requestId</returns>
        int Create(int workflowId, string title, string remark, int userId);

        /// <summary>
        /// 保存流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="title">标题</param>
        /// <param name="remark">签字意见</param>
        /// <param name="userId">用户id</param>
        void Save(int requestId, string title, string remark, int userId);

        /// <summary>
        /// 提交流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <returns>下一节点操作人</returns>
        List<int> Commit(int requestId, int userId);

        /// <summary>
        /// 撤回流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        void RollBack(int requestId);

        /// <summary>
        /// 是否可以撤回流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <returns>是否可以撤回流程</returns>
        bool CanRollBack(int requestId, int userId);

        /// <summary>
        /// 删除流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        void Delete(int requestId);

        /// <summary>
        /// 是否可以删除流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>是否可以删除流程</returns>
        bool CanDelete(int requestId);

        /// <summary>
        /// 获取流程基本信息
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程基本信息</returns>
        RequestBaseViewModel GetBaseInfo(int requestId);

        /// <summary>
        /// 获取流程当前节点用户的意见
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <returns>流程意见</returns>
        string GetUserLog(int requestId, int userId);

        /// <summary>
        /// 获取流程已办理节点意见
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程已办理节点意见</returns>
        IEnumerable<RequestLogViewModel> GetLog(int requestId);

        /// <summary>
        ///根据requestId和userId获取url
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        string GetUrlByUser(int requestId, int userId);
    }
}
