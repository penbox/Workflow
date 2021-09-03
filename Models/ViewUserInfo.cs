using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;

namespace Workflow.Models
{
    [Table("view_user_info")]
    public class ViewUserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Sec_Level { get; set; }
        public string Group { get; set; }
        public int Department_Id { get; set; }
        public ViewUserInfo() { }

        private readonly IDbConnection _db;
        public ViewUserInfo(IDbConnection db)
        {
            _db = db;
        }

        public string GetUserByGroup(string group)
        {
            var userIdList = _db.Query<int>(
                $" select id from view_user_info " +
                $" where `group` ='{group}'");
            return string.Join(',', userIdList);
        }

        /// <summary>
        /// 通过部门和安全级别获取用户
        /// </summary>
        /// <param name="departments">部门Id：1,10,20</param>
        /// <param name="secretLevel">安全级别：[10,20]</param>
        /// <returns>部门和安全级别对应用户</returns>
        public string GetUserByDepartmentSecretLevel(string departments, string secretLevel)
        {
            secretLevel = secretLevel.Trim('[').Trim(']').Replace(",", " and ");
            var userIdList = _db.Query<int>(
                $" select id from view_user_info " +
                $" where department_id in ({departments}) " +
                $" and sec_level between {secretLevel}");
            return string.Join(',', userIdList);
        }

        /// <summary>
        /// 通过创建人本人所在部门安全级别获取用户
        /// </summary>
        /// <param name="creator">创建人id</param>
        /// <param name="secretLevel">安全级别：[10,20]</param>
        /// <returns>创建人本人所在部门安全级别对应用户</returns>
        public string GetUserByCreatorDepartmentSecrestLevel(int creator, string secretLevel)
        {
            var departmentId = _db.QueryFirstOrDefault<int>(
                "select department_id from view_user_info where id=@id",
                new { id = creator });

            return GetUserByDepartmentSecretLevel(departmentId.ToString(), secretLevel);
        }

        /// <summary>
        /// 通过创建人本人所在组安全级别获取用户
        /// </summary>
        /// <param name="creator">创建人id</param>
        /// <param name="secretLevel">安全级别：[10,20]</param>
        /// <returns>创建人本人所在组安全级别对应用户</returns>
        public string GetUserByCreatorGroupSecrestLevel(int creator, string secretLevel)
        {
            var group = _db.QueryFirstOrDefault<string>(
                "select `group` from view_user_info where id=@id",
                new { id = creator });

            secretLevel = secretLevel.Trim('[').Trim(']').Replace(",", " and ");
            var userIdList = _db.Query<int>(
                $" select id from view_user_info " +
                $" where locate(@group,`group`)>0 " +
                $" and sec_level between {secretLevel}", new { group });
            return string.Join(',', userIdList);
        }

        /// <summary>
        /// 通过id获取用户信息
        /// </summary>
        /// <param name="id">用户id</param>
        /// <returns>用户信息</returns>
        public ViewUserInfo Get(int id)
        {
            return _db.Get<ViewUserInfo>(id);
        }
    }
}
