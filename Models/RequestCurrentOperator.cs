using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper.Contrib.Extensions;
using System.Text;
using Dapper;
using Workflow.ViewModels;

namespace Workflow.Models
{
    [Table("request_current_operator")]
    public class RequestCurrentOperator
    {
        public int Id { get; set; }
        public int Request_Id { get; set; }
        public int Node_Id { get; set; }
        public int User_Id { get; set; }
        public string Group { get; set; } = "非会签";
        public DateTime Receive_Time { get; set; }
        public DateTime Operate_Time { get; set; }
        public string Status { get; set; }
        public int Step { get; set; }
        public RequestCurrentOperator() { }

        private readonly IDbConnection _db;

        public RequestCurrentOperator(IDbConnection db)
        {
            _db = db;
        }

        public void Save(RequestCurrentOperator requestCurrentOperator)
        {
            if (requestCurrentOperator.Id == 0)
            {
                _db.Insert(requestCurrentOperator);
            }
            else
            {
                _db.Update(requestCurrentOperator);
            }
        }

        /// <summary>
        /// 获取流程当前操作者
        /// </summary>
        /// <param name="requestBase">流程</param>
        /// <param name="userId">用户id</param>
        /// <returns>流程当前操作者</returns>
        public RequestCurrentOperator Get(RequestBase requestBase, int userId)
        {
            return _db.QueryFirstOrDefault<RequestCurrentOperator>(
                "select * from request_current_operator where request_id=@requestId and node_id=@nodeId and user_id=@userId order by id desc limit 1",
                new { requestId = requestBase.Id, nodeId = requestBase.Current_Node_Id, userId });
        }

        /// <summary>
        /// 获取流程当前操作者
        /// </summary>
        /// <param name="requestBase">流程</param>
        /// <returns>流程当前操作者</returns>
        public RequestCurrentOperator Get(RequestBase requestBase)
        {
            return _db.QueryFirstOrDefault<RequestCurrentOperator>(
                "select * from request_current_operator where request_id=@requestId and node_id=@nodeId order by id desc limit 1",
                new { requestId = requestBase.Id, nodeId = requestBase.Current_Node_Id });
        }


        /// <summary>
        /// 获取流程上一节点操作者
        /// </summary>
        /// <param name="requestBase">流程</param>
        /// <param name="userId">用户id</param>
        /// <returns>流程上一节点操作者</returns>
        public RequestCurrentOperator GetLastOperator(RequestBase requestBase, int userId)
        {
            return _db.QueryFirstOrDefault<RequestCurrentOperator>(
                "select * from request_current_operator where request_id=@requestId and node_id=@nodeId and user_id=@userId and step=@step order by id desc limit 1",
                new { requestId = requestBase.Id, nodeId = requestBase.Last_Node_Id, step = requestBase.Current_Step - 1, userId });
        }

        /// <summary>
        /// 当前节点操作者是否全部完成
        /// </summary>
        /// <param name="requestCurrentOperator">当前节点操作者</param>
        /// <returns>当前节点操作者是否全部完成</returns>
        public bool IsCurrentNodeFinished(RequestCurrentOperator requestCurrentOperator)
        {
            if (requestCurrentOperator.Group == "非会签")
            {
                _db.Execute(
                    "update request_current_operator set status='非会签' where request_id=@requestId and node_id=@nodeId and step=@step and status<>'处理完毕'",
                    new
                    {
                        requestId = requestCurrentOperator.Request_Id,
                        nodeId = requestCurrentOperator.Node_Id,
                        step = requestCurrentOperator.Step
                    });
                return true;
            }

            var nodeOperators = _db.Query<RequestCurrentOperator>(
                " select * from request_current_operator where `group`='会签' and status<>'处理完毕' " +
                " and request_id=@requestId" +
                " and node_id=@nodeId " +
                " and step=@step ",
                new
                {
                    requestId = requestCurrentOperator.Request_Id,
                    nodeId = requestCurrentOperator.Node_Id,
                    step = requestCurrentOperator.Step
                });

            return !nodeOperators.Any();
        }

        /// <summary>
        /// 删除流程中未进行操作的操作者
        /// </summary>
        /// <param name="requestBase">流程</param>
        public void DeleteUnusedOperator(RequestBase requestBase)
        {
            _db.Execute("delete from request_current_operator where request_id=@requestId and step=@step"
            , new { requestId = requestBase.Id, step = requestBase.Current_Step });
        }

        /// <summary>
        /// 获取流程上一节点id
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="currentNodeId">当前节点id</param>
        /// <returns>上一节点id</returns>
        public int GetLastNodeId(int requestId, int currentNodeId)
        {
            return _db.QueryFirstOrDefault<int>(
                " select node_id from request_current_operator where " +
                " id = (select max(id) from request_current_operator where request_id=@requestId and " +
                " id < (select min(id) from request_current_operator where request_id=@requestId and node_id=@currentNodeId))",
                new { requestId, currentNodeId });
        }

        /// <summary>
        /// 重置流程的当前节点操作者为未处理状态
        /// </summary>
        /// <param name="requestBase">流程</param>
        public void ResetOperator(RequestBase requestBase)
        {
            _db.Execute(
                " update request_current_operator set status='未处理' " +
                " where request_id=@requestId and node_id=@nodeId and status='处理完毕' and step=@step ",
                new { requestId = requestBase.Id, nodeId = requestBase.Current_Node_Id, step = requestBase.Current_Step });
        }

        /// <summary>
        /// 删除流程对应的所有操作者
        /// </summary>
        /// <param name="requestId">流程id</param>
        public void Delete(int requestId)
        {
            _db.Execute("delete from request_current_operator where request_id=@requestId", new { requestId });
        }

        /// <summary>
        /// 获取流程当前节点相关信息
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <returns>流程当前节点相关信息</returns>
        public CurrentNodeInfoViewModel GetCurrentNodeInfo(int requestId)
        {
            return _db.QueryFirstOrDefault<CurrentNodeInfoViewModel>(
                "select b.name as node,group_concat(c.name) as user,b.workflow_id from request_current_operator a " +
                "left join workflow_node b on a.node_id = b.id " +
                "left join sys_user c on a.user_id = c.id " +
                "where request_id = @requestId and step = (select max(step) from request_current_operator where request_id = @requestId) " +
                "group by node,b.workflow_id", new { requestId });
        }

        /// <summary>
        /// 拒绝流程，设置所有未操作节点为已拒绝状态
        /// </summary>
        /// <param name="requestId"></param>
        public void RefuseOperators(int requestId)
        {
            _db.Execute(
                " update request_current_operator " +
                " set status='已拒绝'" +
                " where request_id=@requestId and (status ='未处理' or status='已查看')",
                new { requestId });
        }

        /// <summary>
        /// 获取流程的所有操作者(不含抄送)
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="noOperator">不返回的操作者</param>
        /// <returns>流程的所有操作者(不含抄送)</returns>
        public List<string> GetAllOperator(int requestId, int noOperator = 0)
        {
            return _db.Query<string>(
                "select distinct b.name from request_current_operator a left join view_user_info b on a.user_id=b.id where request_id=@requestId and a.status<>'抄送' and a.user_id <> @noOperator",
                new { requestId, noOperator }).ToList();
        }

        /// <summary>
        /// 将mention到的人员添加进流程当前操作者表中
        /// </summary>
        /// <param name="requestId">流程id</param>
        /// <param name="userId">用户id</param>
        public void Mention(int requestId, int userId)
        {
            var existUser = _db.QueryFirstOrDefault<int>(
                "select count(*) from request_current_operator where request_id=@requestId and user_id=@userId",
                new { requestId, userId });

            if (existUser == 0)
            {
                var currentOperator = new RequestCurrentOperator
                {
                    Request_Id = requestId,
                    User_Id = userId,
                    Receive_Time = DateTime.Now,
                    Status = "Mention",
                    Step = 0
                };
                _db.Insert(currentOperator);
            }
        }

        /// <summary>
        /// 当前依次审批节点的操作者是否全部完成
        /// </summary>
        /// <param name="requestCurrentOperator">当前节点操作者</param>
        /// <param name="nodeHandler">当前依次审批节点的所有操作者</param>
        /// <returns></returns>
        public bool IsCurrentNodeLineFinished(RequestCurrentOperator requestCurrentOperator, List<int> nodeHandler)
        {
            return nodeHandler.Last() == requestCurrentOperator.User_Id;
        }
    }
}
