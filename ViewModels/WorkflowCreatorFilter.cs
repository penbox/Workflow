using System;
using System.Collections.Generic;
using System.Text;
using Workflow.Models;

namespace Workflow.ViewModels
{
    public class WorkflowCreatorFilter
    {
        private readonly Func<ViewUserInfo, bool> _method;
        private readonly string _equation;

        public WorkflowCreatorFilter(WorkflowNodeOperator creator)
        {
            _equation = creator.Equation;
            switch (creator.Method)
            {
                case "Everyone":
                    _method = EveryoneMethod;
                    break;
                case "SecretLevel":
                    _method = SecretLevelMethod;
                    break;
                case "DepartmentId":
                    _method = DepartmentIdMethod;
                    break;
                case "UserId":
                    _method = UserIdMethod;
                    break;
                default:
                    _method = null;
                    break;
            }
        }

        public bool IsCreator(ViewUserInfo user)
        {
            if (_method != null)
            {
                try
                {
                    return _method(user);
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        ///所有人
        private bool EveryoneMethod(ViewUserInfo userInfo)
        {
            return true;
        }

        ///指定用户Id
        ///示例：
        ///用户Id：属于1,10,20
        ///1,10,20
        private bool UserIdMethod(ViewUserInfo userInfo)
        {
            string userList = $",{_equation},";
            string userId = $",{userInfo.Id},";
            return userList.IndexOf(userId) > -1;
        }

        ///指定部门Id
        ///示例：
        ///部门Id：属于1,10,20
        ///1,10,20
        private bool DepartmentIdMethod(ViewUserInfo userInfo)
        {
            string departmentList = $",{_equation},";
            string departmentId = $",{userInfo.Department_Id},";
            return departmentList.IndexOf(departmentId) > -1;
        }

        ///指定安全级别
        ///示例：
        ///安全级别：大于等于1小于20，或等于50
        ///[1,20);[50,50]
        private bool SecretLevelMethod(ViewUserInfo userInfo)
        {
            var secLevelRange = _equation.Split(';');
            for (int i = 0; i < secLevelRange.Length; i++)
            {
                if (IsInRange(secLevelRange[i], userInfo.Sec_Level))
                    return true;
            }
            return false;
        }

        private bool IsInRange(string range, int secretLevel)
        {
            bool begin = false;
            bool end = false;

            int beginSecretLevel = int.Parse(range.Split(',')[0].Trim('(').Trim('['));
            int endSecretLevel = int.Parse(range.Split(',')[1].Trim(')').Trim(']'));

            if (range.StartsWith("(")) begin = secretLevel > beginSecretLevel;
            else if (range.StartsWith("[")) begin = secretLevel >= beginSecretLevel;

            if (range.EndsWith(")")) end = secretLevel < endSecretLevel;
            else if (range.EndsWith("]")) end = secretLevel <= endSecretLevel;

            return begin && end;
        }
    }
}
