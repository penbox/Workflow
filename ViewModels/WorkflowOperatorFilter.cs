using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Workflow.Models;

namespace Workflow.ViewModels
{
    public class WorkflowOperatorFilter
    {
        private readonly Func<RequestBase, List<int>> _method;
        private readonly string _equation;
        private readonly IDbConnection _db;

        public WorkflowOperatorFilter(IDbConnection db, WorkflowNodeOperator op)
        {
            _db = db;
            _equation = op.Equation;
            switch (op.Method)
            {
                case "User"://指定人员
                    _method = UserMethod;
                    break;
                case "Group"://指定角色
                    _method = GroupMethod;
                    break;
                case "DepartmentSecretLevel"://指定部门安全级别
                    _method = DepartmentSecretLevelMethod;
                    break;
                case "Self"://创建人本人
                    _method = SelfMethod;
                    break;
                case "SelfDepartmentSecretLevel": //创建人本人所在部门安全级别
                    _method = SelfDepartmentSecretLevelMethod;
                    break;
                case "SelfGroupSecretLevel": //创建人本人所在部门安全级别
                    _method = SelfGroupSecretLevelMethod;
                    break;
                case "HrmField"://业务表对应人员字段
                    _method = HrmFieldMethod;
                    break;
                case "HrmDepartmentSecretLevel"://业务表对应部门字段安全级别
                    _method = HrmDepartmentSecretLevelMethod;
                    break;
                default:
                    _method = null;
                    break;
            }
        }

        public List<int> GetOperator(RequestBase requestBase)
        {
            return _method?.Invoke(requestBase);
        }

        private static List<int> GetUserList(string userString)
        {
            var userList = new List<int>();
            if (!string.IsNullOrEmpty(userString))
            {
                var user = userString.Split(',');
                for (int i = 0; i < user.Length; i++)
                {
                    userList.Add(int.Parse(user[i]));
                }
            }
            return userList;
        }

        //指定人员
        //示例：
        //人员Id：属于1,10,20
        //equation：1,10,20
        private List<int> UserMethod(RequestBase requestBase)
        {
            return GetUserList(_equation);
        }

        //指定角色
        //示例：
        //角色名称：总经理
        //equation：总经理
        private List<int> GroupMethod(RequestBase requestBase)
        {
            string group = _equation;
            var userString = new ViewUserInfo(_db).GetUserByGroup(group);
            return GetUserList(userString);
        }

        //指定部门安全级别
        //示例：
        //部门Id：属于1,10,20
        //安全级别：大于等于10，小于等于20
        //equation：1,10,20:[10,20]
        private List<int> DepartmentSecretLevelMethod(RequestBase requestBase)
        {
            string department = _equation.Split(':')[0];
            string secretLevel = _equation.Split(':')[1];

            var userString = new ViewUserInfo(_db).GetUserByDepartmentSecretLevel(department, secretLevel);
            return GetUserList(userString);
        }

        //创建人本人
        private List<int> SelfMethod(RequestBase requestBase)
        {
            return GetUserList(requestBase.Creator.ToString());
        }

        //创建人本人所在部门安全级别
        //示例：
        //安全级别：大于等于10，小于等于20
        //equation：[10,20]
        private List<int> SelfDepartmentSecretLevelMethod(RequestBase requestBase)
        {
            var userString =
                new ViewUserInfo(_db).GetUserByCreatorDepartmentSecrestLevel(requestBase.Creator, _equation);
            return GetUserList(userString);
        }

        //创建人本人所在组安全级别
        //示例：
        //安全级别：大于等于10，小于等于20
        //equation：[10,20]
        private List<int> SelfGroupSecretLevelMethod(RequestBase requestBase)
        {
            var userString =
                new ViewUserInfo(_db).GetUserByCreatorGroupSecrestLevel(requestBase.Creator, _equation);
            return GetUserList(userString);
        }

        //业务表对应人员字段
        //示例：
        //业务表对应字段：UserId
        //equation：UserId
        private List<int> HrmFieldMethod(RequestBase requestBase)
        {
            var workflowBase = new WorkflowBase(_db).Get(requestBase.Workflow_Id);
            string userString = new WorkflowBase(_db).GetUserByFormHrmField(workflowBase.Form_Name, _equation, requestBase.Id);

            return GetUserList(userString);
        }

        //业务表对应部门字段安全级别
        //示例：
        //业务表对应部门字段：DepartmentId
        //安全级别：大于等于10，小于等于20
        //equation：DepartmentId:[10,20]
        private List<int> HrmDepartmentSecretLevelMethod(RequestBase requestBase)
        {
            string departmentFieldName = _equation.Split(':')[0];
            string secretLevel = _equation.Split(':')[1];

            var workflowBase = new WorkflowBase(_db).Get(requestBase.Workflow_Id);
            string department =
                new WorkflowBase(_db).GetDepartmentByFormDepartmentField(workflowBase.Form_Name, departmentFieldName, requestBase.Id);

            var userString = new ViewUserInfo(_db).GetUserByDepartmentSecretLevel(department, secretLevel);
            return GetUserList(userString);
        }
    }
}
