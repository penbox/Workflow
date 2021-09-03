using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Workflow.Models;
using Workflow.ViewModels;

namespace Workflow
{
    public class RequestV2 : IRequestV2
    {

        private readonly IDbConnection _db;

        public RequestV2(IDbConnection db)
        {
            this._db = db;
        }

        public int Create(int workflowId, string title, int userId)
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

        public void UpdateTitle(int requestId, string title)
        {
            new RequestBase(_db).UpdateTitle(requestId, title);
        }

        public List<string> Commit(int requestId, int userId, Dictionary<string, Action<int>> dictAction = null)
        {
            var list = new List<string>();
            var destNodeType = "";
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                //创建者不在创建节点提交时收到消息
                var noOperator = 0;
                if (requestBase.Status == "草稿")
                {
                    noOperator = requestBase.Creator;
                }

                var finished = false;
                if (userId != 0)
                {
                    var requestCurrentOperator = new RequestCurrentOperator(_db).Get(requestBase, userId);
                    requestCurrentOperator.Status = "处理完毕";
                    requestCurrentOperator.Operate_Time = DateTime.Now;
                    new RequestCurrentOperator(_db).Save(requestCurrentOperator);

                    if (requestCurrentOperator.Group == "依次审批")
                    {
                        var currentNode = new WorkflowNode(_db).Get(requestBase.Current_Node_Id);
                        var currentNodeHandler = new WorkflowNodeHandler(_db).Get(currentNode, requestBase);
                        finished = currentNodeHandler.NodeHandler.Last() == userId;
                        if (!finished)
                        {
                            var nextUserId = FindNextUserId(userId, currentNodeHandler.NodeHandler.ToArray());
                            new RequestCurrentOperator(_db).Save(
                                new RequestCurrentOperator
                                {
                                    Request_Id = requestBase.Id,
                                    Node_Id = requestBase.Current_Node_Id,
                                    User_Id = nextUserId,
                                    Receive_Time = DateTime.Now,
                                    Step = requestBase.Current_Step,
                                    Status = "未处理",
                                    Group = "依次审批"
                                });
                        }
                    }
                    else
                    {
                        finished = new RequestCurrentOperator(_db).IsCurrentNodeFinished(requestCurrentOperator);
                    }
                }

                //判断当前节点是否处理完毕
                if (userId == 0 || finished)
                {
                    if (dictAction != null)
                    {
                        //执行当前节点下一步操作
                        var currentNode = new WorkflowNode(_db).Get(requestBase.Current_Node_Id);
                        if (!string.IsNullOrEmpty(currentNode.Next_Action) && dictAction.ContainsKey(currentNode.Next_Action))
                        {
                            dictAction[currentNode.Next_Action](requestId);
                        }
                    }

                    var workflowInfo = new WorkflowInfo(_db).GetByRequestId(requestId);
                    var nextNodeLink = workflowInfo.GetNextNodeLink(requestBase);
                    var destNode = workflowInfo.GetDestNode(nextNodeLink);
                    destNodeType = destNode.Type;

                    var nodeHandler = new WorkflowNodeHandler(_db).Get(destNode, requestBase);
                    if (nodeHandler.NodeHandler.Count <= 0)
                    {
                        //如果抄送节点没有操作者，则自动跳转抄送节点后一个节点
                        if (destNodeType == "抄送")
                        {
                            nextNodeLink = workflowInfo.GetCopyToNextNodeLink(requestBase);
                            destNode = workflowInfo.GetDestNode(nextNodeLink);
                            nodeHandler = new WorkflowNodeHandler(_db).Get(destNode, requestBase);
                            if (nodeHandler.NodeHandler.Count <= 0)
                            {
                                throw new Exception("下一节点操作者错误");
                            }
                        }
                        else
                        {
                            throw new Exception("下一节点操作者错误");
                        }
                    }


                    requestBase.Status = nextNodeLink.Link_Name;
                    requestBase.Last_Node_Id = requestBase.Current_Node_Id;
                    requestBase.Current_Node_Id = nextNodeLink.Dest_Node_Id;
                    requestBase.Last_Node_Type = requestBase.Current_Node_Type;
                    requestBase.Current_Node_Type = destNode.Type;
                    requestBase.Current_Step += 1;

                    new RequestBase(_db).Save(requestBase);

                    var status = "";
                    var group = "";
                    if (destNode.Type == "抄送" || destNode.Type == "归档")
                    {
                        status = destNode.Type;
                    }
                    else
                    {
                        status = "未处理";
                        group = nodeHandler.Status;
                    }

                    foreach (var handler in nodeHandler.NodeHandler)
                    {
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

                        //依次审批只创建第一个操作者
                        if (group == "依次审批")
                        {
                            break;
                        }
                    }

                    list = new RequestCurrentOperator(_db).GetAllOperator(requestId, noOperator);
                }

                transaction.Commit();
            }
            _db.Close();

            //如果下一节点是抄送节点，则自动再提交一次
            if (destNodeType == "抄送")
            {
                return Commit(requestId, 0, dictAction);
            }

            return list;
        }

        private int FindNextUserId(int userId, int[] lineUser)
        {
            for (int i = 0; i < lineUser.Length; i++)
            {
                if (userId == lineUser[i] && i + 1 < lineUser.Length)
                {
                    return lineUser[i + 1];
                }
            }
            throw new Exception("没有找到依次审批的下一个操作者");
        }

        public List<string> Reply(int requestId, string remark, string attachment, int userId)
        {
            var list = new List<string>();
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                var requestLog = new RequestLog
                {
                    Request_Id = requestId,
                    Node_Id = requestBase.Current_Node_Id,
                    Operator = userId,
                    Step = requestBase.Current_Step,
                    Operate_Time = DateTime.Now,
                    Remark = remark,
                    Attachment = attachment
                };

                //如果是创建节点
                if (requestLog.Step == 1)
                {
                    requestLog.Id = new RequestBase(_db).SearchDraftLogId(requestLog);
                }
                new RequestLog(_db).Save(requestLog);

                list = new RequestCurrentOperator(_db).GetAllOperator(requestId);

                transaction.Commit();
            }
            _db.Close();
            return list;
        }

        public void SendBack(int requestId, int userId)
        {
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                var requestCurrentOperator = new RequestCurrentOperator(_db).Get(requestBase, userId);
                requestCurrentOperator.Status = "处理完毕";
                requestCurrentOperator.Operate_Time = DateTime.Now;
                new RequestCurrentOperator(_db).Save(requestCurrentOperator);

                var createNode = new WorkflowInfo(_db).Get(requestBase.Workflow_Id).GetCreateNode();

                requestBase.Status = "退回";
                requestBase.Last_Node_Id = requestBase.Current_Node_Id;
                requestBase.Current_Node_Id = createNode.Id;
                requestBase.Last_Node_Type = requestBase.Current_Node_Type;
                requestBase.Current_Node_Type = createNode.Type;
                requestBase.Current_Step += 1;

                new RequestCurrentOperator(_db).Save(
                    new RequestCurrentOperator
                    {
                        Request_Id = requestBase.Id,
                        Node_Id = requestBase.Current_Node_Id,
                        User_Id = requestBase.Creator,
                        Receive_Time = DateTime.Now,
                        Step = requestBase.Current_Step,
                        Status = "未处理",
                        Group = "非会签"
                    });

                new RequestBase(_db).Save(requestBase);

                transaction.Commit();
            }

            _db.Close();
        }

        public void ReturnToStart(int requestId, int userId)
        {
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                if (requestBase.Creator != userId)
                {
                    throw new Exception("没有撤回到创建的权限");
                }

                var requestCurrentOperator = new RequestCurrentOperator(_db).Get(requestBase);
                requestCurrentOperator.Status = "处理完毕";
                requestCurrentOperator.Operate_Time = DateTime.Now;
                new RequestCurrentOperator(_db).Save(requestCurrentOperator);

                var createNode = new WorkflowInfo(_db).Get(requestBase.Workflow_Id).GetCreateNode();

                requestBase.Status = "撤回到创建";
                requestBase.Last_Node_Id = requestBase.Current_Node_Id;
                requestBase.Current_Node_Id = createNode.Id;
                requestBase.Last_Node_Type = requestBase.Current_Node_Type;
                requestBase.Current_Node_Type = createNode.Type;
                requestBase.Current_Step += 1;

                new RequestCurrentOperator(_db).Save(
                    new RequestCurrentOperator
                    {
                        Request_Id = requestBase.Id,
                        Node_Id = requestBase.Current_Node_Id,
                        User_Id = requestBase.Creator,
                        Receive_Time = DateTime.Now,
                        Step = requestBase.Current_Step,
                        Status = "未处理",
                        Group = "非会签"
                    });

                new RequestBase(_db).Save(requestBase);

                transaction.Commit();
            }

            _db.Close();
        }

        public void Refuse(int requestId, int userId, Dictionary<string, Action<int>> dictAction = null)
        {
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                var requestCurrentOperator = new RequestCurrentOperator(_db).Get(requestBase, userId);
                requestCurrentOperator.Status = "已拒绝";
                requestCurrentOperator.Operate_Time = DateTime.Now;
                new RequestCurrentOperator(_db).Save(requestCurrentOperator);

                new RequestBase(_db).RefuseRequestBase(requestId);
                new RequestCurrentOperator(_db).RefuseOperators(requestId);

                if (dictAction != null)
                {
                    var archiveNode = new WorkflowNode(_db).GetArchiveNode(requestBase.Workflow_Id);
                    if (!string.IsNullOrEmpty(archiveNode.Next_Action) && dictAction.ContainsKey(archiveNode.Next_Action))
                    {
                        dictAction[archiveNode.Next_Action](requestId);
                    }
                }

                transaction.Commit();
            }
            _db.Close();
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

        public RequestBaseViewModel GetBaseInfo(int requestId)
        {
            return new RequestBase(_db).GetBaseInfo(requestId);
        }

        public CurrentNodeInfoViewModel GetCurrentNodeInfo(int requestId)
        {
            return new RequestCurrentOperator(_db).GetCurrentNodeInfo(requestId);
        }

        public string GetUrlByUser(int requestId, int userId)
        {
            return new RequestBase(_db).GetUrlByUser(requestId, userId);
        }

        public string GetCanReadUrlByUser(int requestId, int userId)
        {
            return new RequestBase(_db).GetCanReadUrlByUser(requestId, userId);
        }

        public bool CanOperate(int requestId, int userId)
        {
            var requestBase = new RequestBase(_db).Get(requestId);
            var currentOperator = new RequestCurrentOperator(_db).Get(requestBase, userId);
            return (currentOperator != null && requestBase.Status != "归档" && requestBase.Status != "已拒绝");
        }

        public IEnumerable<RequestLogViewModelV2> GetLog(int requestId)
        {
            return new RequestLog(_db).GetLogV2(requestId);
        }

        public string RollBack(int requestId)
        {
            string rtn;
            _db.Open();
            using (var transaction = _db.BeginTransaction())
            {
                var requestBase = new RequestBase(_db).Get(requestId);

                //删除未操作的操作者
                new RequestCurrentOperator(_db).DeleteUnusedOperator(requestBase);

                //流程上一节点更新为当前节点
                requestBase.Current_Node_Id = requestBase.Last_Node_Id;
                requestBase.Current_Node_Type = requestBase.Last_Node_Type;
                requestBase.Last_Node_Id = new RequestCurrentOperator(_db).GetLastNodeId(requestId, requestBase.Current_Node_Id);
                requestBase.Last_Node_Type = new WorkflowNode(_db).GetNodeType(requestBase.Last_Node_Id);
                requestBase.Current_Step -= 1;
                requestBase.Status = requestBase.Current_Step <= 1 ? "草稿" : "撤回";
                new RequestBase(_db).Save(requestBase);

                new RequestCurrentOperator(_db).ResetOperator(requestBase);

                var node = new WorkflowNode(_db).Get(requestBase.Current_Node_Id);

                rtn = node.Type == "创建" ? $"{node.Url}draft" : node.Url;

                transaction.Commit();
            }
            _db.Close();

            return rtn;
        }

        public bool CanRollBack(int requestId, int userId)
        {
            var requestBase = new RequestBase(_db).Get(requestId);
            var lastOperator = new RequestCurrentOperator(_db).GetLastOperator(requestBase, userId);

            //当前节点属于普通节点，当前用户属于上一节点操作者
            return lastOperator != null
                    && (requestBase.Current_Node_Type == "普通" || (requestBase.Current_Node_Type == "创建" && requestBase.Current_Step > 1))
                    && requestBase.Last_Node_Type != "抄送"
                    && requestBase.Status != "已拒绝";
        }

        public string GetFormName(int requestId)
        {
            return new RequestBase(_db).GetFormName(requestId);
        }

        public void Mention(int requestId, int userId)
        {
            new RequestCurrentOperator(_db).Mention(requestId, userId);
        }
    }
}
