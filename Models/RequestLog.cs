using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;
using Workflow.ViewModels;

namespace Workflow.Models
{
    [Table("request_log")]
    public class RequestLog
    {
        public int Id { get; set; }
        public int Request_Id { get; set; }
        public int Node_Id { get; set; }
        public int Operator { get; set; }
        public DateTime Operate_Time { get; set; }
        public string Remark { get; set; }
        public string Attachment { get; set; }
        public int Step { get; set; }
        public RequestLog() { }

        private readonly IDbConnection _db;
        public RequestLog(IDbConnection db)
        {
            _db = db;
        }

        public void Save(RequestLog requestLog)
        {
            if (requestLog.Id == 0)
            {
                _db.Insert(requestLog);
            }
            else
            {
                _db.Update(requestLog);
            }
        }

        /// <summary>
        /// 获取流程当前节点的用户意见
        /// </summary>
        /// <param name="requestBase">流程</param>
        /// <param name="userId">用户id</param>
        /// <returns>流程当前节点的用户意见</returns>
        public RequestLog Get(RequestBase requestBase, int userId)
        {
            return _db.QueryFirstOrDefault<RequestLog>(
                " select * from request_log where request_id=@requestId and node_id=@nodeId and Operator=@userId order by Id desc limit 1",
                new { requestId = requestBase.Id, nodeId = requestBase.Current_Node_Id, userId });
        }

        /// <summary>
        /// 删除流程当前节点中未填写的用户意见
        /// </summary>
        /// <param name="requestBase">流程</param>
        public void DeleteUnusedLog(RequestBase requestBase)
        {
            _db.Execute("delete from request_log where request_id=@requestId and step=@step",
                new { requestId = requestBase.Id, step = requestBase.Current_Step });
        }

        /// <summary>
        /// 删除流程对应的所有意见
        /// </summary>
        /// <param name="requestId">流程id</param>
        public void Delete(int requestId)
        {
            _db.Execute("delete from request_log where request_id=@requestId", new { requestId });
        }

        /// <summary>
        /// 获取流程当前节点用户的意见
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        /// <returns>流程意见</returns>
        public string GetUserLog(int requestId, int userId)
        {
            return _db.QueryFirstOrDefault<string>(
                "select ifnull(b.remark,'') from request_base a " +
                " left join request_log b on a.id=b.request_id and a.current_step=b.step " +
                " where a.id=@requestId and b.operator=@userId", new { requestId, userId });
        }

        /// <summary>
        /// 获取流程已办理节点意见
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程已办理节点意见</returns>
        public IEnumerable<RequestLogViewModel> GetLog(int requestId)
        {
            var requestLog = _db.Query<RequestLogViewModel>(
                " select b.id,c.name,ifnull(b.remark,'') as remark,b.operate_time,d.name as node_name " +
                " from request_current_operator a " +
                " left join request_log b on a.request_id=b.request_id and a.step=b.step and a.user_id=b.operator " +
                " left join view_user_info c on a.user_id=c.id " +
                " left join workflow_node d on a.node_id=d.id " +
                " where a.request_id=@requestId and status ='处理完毕' order by a.step desc", new { requestId });
            foreach (RequestLogViewModel item in requestLog)
            {
                var requestLogReply = _db.Query<ReturnRequestLogReplyViewModel>("SELECT b.name as user_name,a.reply_time,a.remark,a.attachment from request_log_reply a left join sys_user b on a.user_id=b.id where a.log_id=@logId", new { logId = item.id });
                item.Request_Log_Reply = requestLogReply;
            }
            return requestLog;
        }

        /// <summary>
        /// 获取流程所有节点意见
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程所有节点意见</returns>
        public IEnumerable<RequestLogViewModelV2> GetLogV2(int requestId)
        {
            var workflowId = new RequestBase(_db).Get(requestId).Workflow_Id;
            var requestLog = _db.Query<RequestLogViewModelV2>(
                " select ifnull(a.id,0) as id,c.name,ifnull(a.remark,'') as remark,ifnull(a.attachment,'') as attachment," +
                " date_format(a.operate_time, '%Y-%m-%d %H:%i:%s') as operate_time,d.name as node_name,case when a.step=1 then '(创建)' else case when e.id is null then '添加了评论' else '(同意)' end end as type,d.seq " +
                " from request_log a " +
                " left join view_user_info c on a.operator=c.id " +
                " left join workflow_node d on a.node_id=d.id " +
                " left join request_current_operator e on a.node_id=e.node_id and a.operator=e.user_id and timestampdiff(second,a.operate_time,e.operate_time) between -2 and 2 " +
                " where a.request_id=@requestId order by a.id",
                new { requestId }).ToList();
            foreach (var log in requestLog)
            {
                if (!string.IsNullOrEmpty(log.Attachment))
                {
                    log.Attachments = GetAttachments(log.Attachment).ToList();
                }
            }
            //添加进行中的节点
            var curentNode = _db.Query<RequestLogViewModelV2>(
                " select a.id as id,c.name,'' as remark,'' as attachment, " +
                " if(a.status ='未处理','处理中',a.status) as operate_time,d.name as node_name,d.seq " +
                " from request_current_operator a left join request_base b on a.request_id=b.id " +
                " left join view_user_info c on a.user_id=c.id " +
                " left join workflow_node d on a.node_id=d.id " +
                " where request_id=@requestid and a.step=b.current_step ",
                new { requestId });
            requestLog.AddRange(curentNode);

            if (requestLog.Last().Node_Name != "归档")
            {
                var noneNode = _db.Query<RequestLogViewModelV2>(
                    " select 0,'','','','',name as node_name from workflow_node " +
                    " where workflow_id=@workflowId " +
                    //" and id not in (select distinct node_id from request_current_operator where request_id=@requestId) order by seq",
                    " and seq>@seq order by seq",
                    new { requestId, workflowId, seq = requestLog.Last().Seq });
                requestLog.AddRange(noneNode);

            }

            return requestLog;
        }

        private IEnumerable<RequestLogAttachmentViewModel> GetAttachments(string pid)
        {
            return _db.Query<RequestLogAttachmentViewModel>(
                "select * from sys_attachment where pid=@pid", new { pid });
        }
    }
}
