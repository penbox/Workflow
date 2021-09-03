using System;
using System.Collections.Generic;
using System.Text;
using Workflow.ViewModels;

namespace Workflow
{
    public interface IRequestV2
    {
        /// <summary>
        /// 创建流程
        /// </summary>
        /// <param name="workflowId">工作流id</param>
        /// <param name="title">标题</param>
        /// <param name="userId">用户id</param>
        /// <returns>Request_Id</returns>
        int Create(int workflowId, string title, int userId);

        /// <summary>
        /// 更新流程标题
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="title">流程标题</param>
        void UpdateTitle(int requestId, string title);

        /// <summary>
        /// 提交流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <param name="dictAction">下一步操作</param>
        /// <returns>流程所有操作者</returns>
        List<string> Commit(int requestId, int userId, Dictionary<string, Action<int>> dictAction);

        /// <summary>
        /// 保存流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="attachment">附件</param>
        /// <param name="remark">签字意见</param>
        /// <param name="userId">用户id</param>
        /// <returns>流程相关人员</returns>
        List<string> Reply(int requestId, string remark, string attachment, int userId);

        /// <summary>
        /// 退回，流程回到起点
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        void SendBack(int requestId, int userId);

        /// <summary>
        /// 撤回到创建，流程回到起点
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="userId"></param>
        void ReturnToStart(int requestId, int userId);

        /// <summary>
        /// 拒绝，流程直接归档
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <param name="dictAction">下一步操作</param>
        void Refuse(int requestId, int userId, Dictionary<string, Action<int>> dictAction);


        /// <summary>
        /// 删除流程
        /// </summary>
        /// <param name="requestId">流程id</param>
        void Delete(int requestId);

        /// <summary>
        /// 获取流程基本信息
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程基本信息</returns>
        RequestBaseViewModel GetBaseInfo(int requestId);

        /// <summary>
        /// 获取流程所有节点及意见
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程已办理节点意见</returns>
        IEnumerable<RequestLogViewModelV2> GetLog(int requestId);

        /// <summary>
        /// 获取流程当前节点相关信息
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程当前节点相关信息</returns>
        CurrentNodeInfoViewModel GetCurrentNodeInfo(int requestId);

        /// <summary>
        /// 根据requestId和userId获取url
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="userId"></param>
        /// <returns>节点Url</returns>
        string GetUrlByUser(int requestId, int userId);

        /// <summary>
        /// 根据requestId和userId获取u可以查看的流程Url
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="userId"></param>
        /// <returns>流程Url</returns>
        string GetCanReadUrlByUser(int requestId, int userId);

        /// <summary>
        /// 判断当前用户可否进行操作
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <returns>可否进行操作</returns>
        bool CanOperate(int requestId, int userId);

        /// <summary>
        /// 是否可撤回
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        bool CanRollBack(int requestId, int userId);

        /// <summary>
        /// 撤回
        /// </summary>
        /// <param name="requestId">流程id</param>
        string RollBack(int requestId);

        /// <summary>
        /// 获取流程对应Form的表名
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>Form表名</returns>
        string GetFormName(int requestId);

        /// <summary>
        /// 将mention到的人员添加进流程当前操作者表中
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        void Mention(int requestId, int userId);
    }
}
