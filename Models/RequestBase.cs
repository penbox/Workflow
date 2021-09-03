using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Dapper.Contrib.Extensions;
using System.Text;
using Dapper;
using Workflow.ViewModels;

namespace Workflow.Models
{
    [Table("request_base")]
    public class RequestBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Workflow_Id { get; set; }
        public int Last_Node_Id { get; set; }
        public string Last_Node_Type { get; set; }
        public int Current_Node_Id { get; set; }
        public string Current_Node_Type { get; set; }
        public string Status { get; set; }
        public int Creator { get; set; }
        public DateTime Create_Time { get; set; }
        public int Current_Step { get; set; }
        public RequestBase() { }

        private readonly IDbConnection _db;

        public RequestBase(IDbConnection db)
        {
            _db = db;
        }

        public void Save(RequestBase requestBase)
        {
            if (requestBase.Id == 0)
            {
                _db.Insert(requestBase);
            }
            else
            {
                _db.Update(requestBase);
            }
        }

        public RequestBase Get(int id)
        {
            return _db.Get<RequestBase>(id);
        }

        public void Delete(int id)
        {
            _db.Delete(new RequestBase { Id = id });
        }

        // 获取所有用户相关流程列表
        public PagedList<RequestBaseListViewModel> GetAllRequestList(int rows, int page, string name, int workflowId, string beginDate, string endDate, int userId,bool archive)
        {
            var archiveString = archive ? " and a.status in ('归档','已拒绝')" : " and a.status not in ('归档','已拒绝')";
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                int offset = (page - 1) * rows;

                var list = _db.Query<RequestBaseListViewModel>(
                    " select a.id,a.name,a.create_time,c.name as creator,b.name as node_name,a.status,d.name as workflow_name,e.url " +
                    " from request_base a left join workflow_node b on a.current_node_id=b.id " +
                    " left join view_user_info c on a.creator=c.id " +
                    " left join workflow_base d on a.workflow_id=d.id " +
                    " left join  workflow_node e on a.workflow_id=e.workflow_id and e.type='归档'" +
                    $" where a.id in (select request_id from request_current_operator where user_id=@userId) {archiveString}" +
                    " and a.name like @name " +
                    " and (@workflowId=0 or a.workflow_id=@workflowId) " +
                    " and a.create_time between @beginDate and @endDate " +
                    " order by create_time desc limit @offset,@rows",
                    new { rows, offset, name = $"%{name}%", workflowId, beginDate, endDate, userId }, transaction);
                var count = _db.QueryFirstOrDefault<int>(
                    " select count(*) from request_base a" +
                    $" where a.id in (select request_id from request_current_operator where user_id=@userId) {archiveString}" +
                    " and a.name like @name " +
                    " and (@workflowId=0 or a.workflow_id=@workflowId) " +
                    " and a.create_time between @beginDate and @endDate " +
                    " order by a.create_time desc",
                    new { name = $"%{name}%", workflowId, beginDate, endDate, userId }, transaction);

                foreach (var item in list)
                {
                    var replyResult = _db.QueryFirstOrDefault(
                        "select count(a.remark)>0 as reply,count(b.remark)>0 as by_reply " +
                        " from request_log a " +
                        " left join request_log b on a.node_id =b.node_id and b.request_id=a.request_id and b.remark<>'' and b.operator<>@userId " +
                        " where a.request_id =@requestId and a.operator =@userId and a.remark <>''",
                        new { requestId = item.Id, userId });
                    if (replyResult != null)
                    {
                        item.Reply = replyResult.reply == 1;
                        item.By_Reply = replyResult.by_reply == 1;
                    }
                }

                return new PagedList<RequestBaseListViewModel>
                {
                    List = list,
                    Count = count
                };
            }
        }

        // 获取用户待办流程列表
        public PagedList<RequestBaseListViewModel> GetBacklogRequestList(int rows, int page, string name, int workflowId, string beginDate, string endDate, int userId, bool draft)
        {
            var draftString = draft ? "and a.status='草稿'" : "and a.status<>'草稿'";
            int offset = (page - 1) * rows;
            var list = _db.Query<RequestBaseListViewModel>(
                " select a.id,a.name,a.create_time,c.name as creator,b.name as node_name,b.url,a.status,d.name as workflow_name " +
                " from request_base a left join workflow_node b on a.current_node_id=b.id " +
                " left join view_user_info c on a.creator=c.id " +
                " left join workflow_base d on a.workflow_id=d.id " +
                " where a.id in (select request_id from request_current_operator where user_id=@userId and (status ='未处理' or status='已查看') and step>0) " +
                $" {draftString} " +
                " and a.name like @name " +
                " and (@workflowId=0 or a.workflow_id=@workflowId) " +
                " and a.create_time between @beginDate and @endDate " +
                " order by create_time desc limit @offset,@rows",
                new { rows, offset, name = $"%{name}%", workflowId, beginDate, endDate, userId });
            var count = _db.QueryFirstOrDefault<int>(
                " select count(*) from request_base a" +
                " where a.id in (select request_id from request_current_operator where user_id=@userId and (status ='未处理' or status='已查看') and step>0) " +
                $" {draftString} " +
                " and a.name like @name " +
                " and (@workflowId=0 or a.workflow_id=@workflowId) " +
                " and a.create_time between @beginDate and @endDate " +
                " order by a.create_time desc",
                new { name = $"%{name}%", workflowId, beginDate, endDate, userId });

            return new PagedList<RequestBaseListViewModel>
            {
                List = list,
                Count = count
            };
        }

        // 获取所有流程列表 (unused)
        public PagedList<RequestViewModel> GetRequestList(int rows, int page, int workflowId, int userId)
        {
            int offset = (page - 1) * rows;
            var list = _db.Query<RequestViewModel>(
                " select distinct a.request_id,b.name,c.name as workflow_name,d.name as creator,e.name as current_node_name,b.create_time " +
                " from request_log a " +
                " left join request_base b on a.request_id=b.id " +
                " left join workflow_base c on b.workflow_id=c.id " +
                " left join view_user_info d on b.creator=d.id " +
                " left join workflow_node e on b.current_node_id=e.id " +
                $" where b.workflow_id=@workflowId and a.operator=@userId " +
                " order by b.create_time desc limit @offset,@rows ",
                new { rows, offset, workflowId, userId }).ToList();
            var count = _db.QueryFirstOrDefault<int>(
                " select count(distinct request_id) " +
                " from request_log a " +
                " left join request_base b on a.request_id=b.id " +
                $" where b.workflow_id=@workflowId and a.operator=@userId ",
                new { workflowId, userId });
            return new PagedList<RequestViewModel>
            {
                List = list,
                Count = count
            };
        }

        public RequestBaseViewModel GetBaseInfo(int id)
        {
            var baseInfo = _db.QueryFirstOrDefault<RequestBaseViewModel>(
                "select a.create_time,a.name as title,b.name as creator,c.name as workflow_name,a.status " +
                " from request_base a " +
                " left join view_user_info b on a.creator=b.id " +
                " left join workflow_base c on a.workflow_id=c.id " +
                " where a.id=@id", new { id });
            return baseInfo;
        }

        public string GetUrlByUser(int requestId, int userId)
        {
            var url = _db.QueryFirstOrDefault<string>("select b.url from request_base a " +
                                                           " left join workflow_node b on a.current_node_id = b.id " +
                                                           " left join request_current_operator c on a.id = c.request_id and a.current_step = c.step " +
                                                           " where a.id = @requestId and c.user_id = @userId", new { requestId, userId });
            if (string.IsNullOrEmpty(url))
            {
                url = _db.QueryFirstOrDefault<string>("select b.url from request_base a " +
                                                     " left join workflow_node b on a.workflow_id = b.workflow_id and b.name = '归档'" +
                                                     " where a.id = @requestId", new { requestId });
            }
            return url;
        }

        public string GetCanReadUrlByUser(int requestId, int userId)
        {
            return _db.QueryFirstOrDefault<string>("select c.url from request_current_operator a " +
                                                   " left join request_base b on a.request_id=b.id " +
                                                   " left join workflow_node c on b.workflow_id = c.workflow_id and c.name = '归档' " +
                                                   " where b.id = @requestId and a.user_id=@userId limit 1", new { requestId, userId });
        }

        public void RefuseRequestBase(int requestId)
        {
            _db.Execute(
                " update request_base set status='已拒绝' where id=@requestId ",
                new { requestId });
        }

        public string GetFormName(int requestId)
        {
            return _db.QueryFirstOrDefault<string>(
                "select b.url from request_base a left join workflow_node b on a.workflow_id=b.workflow_id where a.id=@requestId and b.type='归档'",
                new { requestId });
        }

        public int SearchDraftLogId(RequestLog requestLog)
        {
            return _db.QueryFirstOrDefault<int>(
                "select id from request_log where request_id=@request_id and node_id=@node_id and operator=@operator and step=1 order by id desc",
                requestLog);
        }

        public void UpdateTitle(int requestId, string title)
        {
            _db.Execute("update request_base set name=@title where id=@requestId", new { requestId, title });
        }
    }
}
