using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Workflow.Models;
using Workflow.ViewModels;

namespace Workflow
{
    public class Request : IRequest
    {
        private readonly IDbConnection _db;

        public Request(IDbConnection db)
        {
            this._db = db;
        }

        public int Create(int workflowId, string title, string remark, int userId)
        {
            int requestId;
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var workflowInfo = new WorkflowInfo(_db).Get(workflowId);

                //保存RequestBase
                var requestBase = new RequestBase
                {
                    Workflow_Id = workflowId,
                    Creator = userId,
                    Create_Time = DateTime.Now,
                    Name = title,
                    Current_Node_Id = workflowInfo.GetCreateNode().Id,
                    Current_Node_Type = workflowInfo.GetCreateNode().Type,
                    Status = "草稿",
                    Current_Step = 1
                };
                new RequestBase(_db).Save(requestBase);
                requestId = requestBase.Id;

                //保存RequestLog
                var requestLog = new RequestLog
                {
                    Request_Id = requestBase.Id,//insert是否返回了requestId？
                    Node_Id = requestBase.Current_Node_Id,
                    Operator = requestBase.Creator,
                    Operate_Time = requestBase.Create_Time,
                    Remark = remark,
                    Step = requestBase.Current_Step
                };
                new RequestLog(_db).Save(requestLog);

                //保存RequestCurrentOperator
                var requestCurrentOperator = new RequestCurrentOperator
                {
                    Request_Id = requestBase.Id,
                    Node_Id = requestBase.Current_Node_Id,
                    User_Id = requestBase.Creator,
                    Receive_Time = requestBase.Create_Time,
                    Operate_Time = requestBase.Create_Time,
                    Status = "已查看",
                    Step = requestBase.Current_Step
                };
                new RequestCurrentOperator(_db).Save(requestCurrentOperator);

                transaction.Commit();
            }
            _db.Close();

            return requestId;
        }

        public void Save(int requestId, string title, string remark, int userId)
        {
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);
                if (!string.IsNullOrEmpty(title))
                {
                    requestBase.Name = title;
                    new RequestBase(_db).Save(requestBase);
                }

                var requestLog = new RequestLog(_db).Get(requestBase, userId);
                requestLog.Operate_Time = DateTime.Now;
                requestLog.Remark = remark;
                new RequestLog(_db).Save(requestLog);

                var requestCurrentOperator = new RequestCurrentOperator(_db).Get(requestBase, userId);
                requestCurrentOperator.Status = "已查看";
                requestCurrentOperator.Operate_Time = requestLog.Operate_Time;
                new RequestCurrentOperator(_db).Save(requestCurrentOperator);

                transaction.Commit();
            }
            _db.Close();
        }

        public List<int> Commit(int requestId, int userId)
        {
            var nextOperators = new List<int>();
            var destNodeType = "";
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                var finished = false;
                if (userId != 0)
                {
                    var requestCurrentOperator = new RequestCurrentOperator(_db).Get(requestBase, userId);
                    requestCurrentOperator.Status = "处理完毕";
                    requestCurrentOperator.Operate_Time = DateTime.Now;
                    new RequestCurrentOperator(_db).Save(requestCurrentOperator);
                    finished = new RequestCurrentOperator(_db).IsCurrentNodeFinished(requestCurrentOperator);
                }

                //判断当前节点是否处理完毕
                if (userId == 0 || finished)
                {
                    var workflowInfo = new WorkflowInfo(_db).GetByRequestId(requestId);
                    var nextNodeLink = workflowInfo.GetNextNodeLink(requestBase);
                    var destNode = workflowInfo.GetDestNode(nextNodeLink);

                    destNodeType = destNode.Type;

                    var nodeHandler = new WorkflowNodeHandler(_db).Get(destNode, requestBase);
                    if (nodeHandler.NodeHandler.Count <= 0)
                    {
                        throw new Exception("下一节点操作者错误");
                    }

                    requestBase.Status = nextNodeLink.Link_Name;
                    requestBase.Current_Node_Id = nextNodeLink.Dest_Node_Id;
                    requestBase.Last_Node_Id = nextNodeLink.Node_Id;
                    requestBase.Last_Node_Type = requestBase.Current_Node_Type;
                    requestBase.Current_Step += 1;

                    requestBase.Current_Node_Type = destNode.Type;
                    new RequestBase(_db).Save(requestBase);

                    var status = "";
                    var group = "";
                    if (destNode.Type == "抄送" || destNode.Type == "归档")
                    {
                        status = destNode.Type;
                    }
                    if (destNode.Type == "普通")
                    {
                        status = "未处理";
                        group = nodeHandler.Status;
                    }

                    foreach (var handler in nodeHandler.NodeHandler)
                    {
                        nextOperators.Add(handler);

                        new RequestCurrentOperator(_db).Save(
                            new RequestCurrentOperator
                            {
                                Request_Id = requestBase.Id,
                                Node_Id = destNode.Id,
                                User_Id = handler,
                                Receive_Time = DateTime.Now,
                                Step = requestBase.Current_Step,
                                Status = status,
                                Group = group
                            });
                        if (destNode.Type == "普通")
                        {
                            new RequestLog(_db).Save(
                                new RequestLog
                                {
                                    Request_Id = requestBase.Id,
                                    Node_Id = destNode.Id,
                                    Operator = handler,
                                    Step = requestBase.Current_Step
                                });
                        }
                    }
                    //if (destNode.Type == "归档")
                    //{
                    //    requestBase.Current_Node_Type = "归档";
                    //    //new RequestBase(_db).Save(requestBase);

                    //    foreach (var handler in nodeHandler.NodeHandler)
                    //    {
                    //        new RequestCurrentOperator(_db).Save(
                    //            new RequestCurrentOperator
                    //            {
                    //                Request_Id = requestBase.Id,
                    //                Node_Id = destNode.Id,
                    //                User_Id = handler,
                    //                Receive_Time = DateTime.Now,
                    //                Step = requestBase.Current_Step,
                    //                Status = "归档"
                    //            });
                    //    }
                    //}
                    //else if (destNode.Type == "抄送")
                    //{
                    //    requestBase.Current_Node_Type = "抄送";
                    //    //new RequestBase(_db).Save(requestBase);
                    //}
                    //else
                    //{
                    //    requestBase.Current_Node_Type = "普通";
                    //    //new RequestBase(_db).Save(requestBase);

                    //    foreach (var handler in nodeHandler.NodeHandler)
                    //    {
                    //        new RequestCurrentOperator(_db).Save(
                    //            new RequestCurrentOperator
                    //            {
                    //                Request_Id = requestBase.Id,
                    //                Node_Id = destNode.Id,
                    //                User_Id = handler,
                    //                Receive_Time = DateTime.Now,
                    //                Step = requestBase.Current_Step,
                    //                Status = "未处理",
                    //                Group = nodeHandler.Status
                    //            });
                    //        new RequestLog(_db).Save(
                    //            new RequestLog
                    //            {
                    //                Request_Id = requestBase.Id,
                    //                Node_Id = destNode.Id,
                    //                Operator = handler,
                    //                Step = requestBase.Current_Step
                    //            });
                    //    }
                    //}

                }

                transaction.Commit();
            }
            _db.Close();

            if (destNodeType == "抄送")
            {
                nextOperators.AddRange(Commit(requestId, 0));
            }
            return nextOperators;
        }

        public void RollBack(int requestId)
        {
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                //删除撤回的操作者和意见
                new RequestLog(_db).DeleteUnusedLog(requestBase);
                new RequestCurrentOperator(_db).DeleteUnusedOperator(requestBase);

                //流程上一节点更新为当前节点
                requestBase.Current_Node_Id = requestBase.Last_Node_Id;
                requestBase.Current_Node_Type = requestBase.Last_Node_Type;
                requestBase.Status = "撤回";
                requestBase.Last_Node_Id = new RequestCurrentOperator(_db).GetLastNodeId(requestId, requestBase.Current_Node_Id);
                requestBase.Last_Node_Type = new WorkflowNode(_db).GetNodeType(requestBase.Last_Node_Id);
                requestBase.Current_Step -= 1;
                new RequestBase(_db).Save(requestBase);

                new RequestCurrentOperator(_db).ResetOperator(requestBase);

                transaction.Commit();
            }
            _db.Close();
        }

        public bool CanRollBack(int requestId, int userId)
        {
            var requestBase = new RequestBase(_db).Get(requestId);
            var lastOperator = new RequestCurrentOperator(_db).GetLastOperator(requestBase, userId);

            //当前节点属于普通节点，当前用户属于上一节点操作者
            return lastOperator != null
                   && requestBase.Current_Node_Type == "普通"
                   && requestBase.Last_Node_Type != "抄送";
        }

        public void Delete(int requestId)
        {
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                new RequestBase(_db).Delete(requestId);
                new RequestCurrentOperator(_db).Delete(requestId);
                new RequestLog(_db).Delete(requestId);

                transaction.Commit();
            }
            _db.Close();
        }

        public bool CanDelete(int requestId)
        {
            var requestBase = new RequestBase(_db).Get(requestId);
            return requestBase == null || requestBase.Status == "草稿";
        }

        public RequestBaseViewModel GetBaseInfo(int requestId)
        {
            return new RequestBase(_db).GetBaseInfo(requestId);
        }

        public string GetUserLog(int requestId, int userId)
        {
            return new RequestLog(_db).GetUserLog(requestId, userId);
        }

        public IEnumerable<RequestLogViewModel> GetLog(int requestId)
        {
            return new RequestLog(_db).GetLog(requestId);
        }

        public CurrentNodeInfoViewModel GetCurrentNodeInfo(int requestId)
        {
            return new RequestCurrentOperator(_db).GetCurrentNodeInfo(requestId);
        }

        public string GetUrlByUser(int requestId, int userId)
        {
            return new RequestBase(_db).GetUrlByUser(requestId, userId);
        }
    }
}
