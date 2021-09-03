using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;
using Workflow.ViewModels;

namespace Workflow.Models
{
    [Table("workflow_base")]
    class WorkflowBase
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Memo { get; set; }
        public string Form_Name { get; set; }
        public string Icon { get; set; }
        public string Image { get; set; }
        public WorkflowBase() { }

        private readonly IDbConnection _db;
        public WorkflowBase(IDbConnection db)
        {
            _db = db;
        }

        public WorkflowBase Get(int id)
        {
            return _db.Get<WorkflowBase>(id);
        }

        /// <summary>
        /// 获取业务表对应人员字段
        /// </summary>
        /// <param name="formName">业务表名称</param>
        /// <param name="hrmField">人员字段</param>
        /// <param name="requestId">流程id</param>
        /// <returns>业务表对应人员字段</returns>
        public string GetUserByFormHrmField(string formName, string hrmField, int requestId)
        {
            return _db.QuerySingleOrDefault<string>(
                $" select {hrmField} from {formName} where request_id={requestId} ");
        }

        /// <summary>
        /// 获取业务表对应部门字段安全级别
        /// </summary>
        /// <param name="formName">业务表名称</param>
        /// <param name="deptField">部门字段</param>
        /// <param name="requestId">流程id</param>
        /// <returns>业务表对应部门字段安全级别</returns>
        public string GetDepartmentByFormDepartmentField(string formName, string deptField, int requestId)
        {
            return _db.QuerySingleOrDefault<string>(
                $" select {deptField} from {formName} where request_id={requestId}");
        }

        /// <summary>
        /// 获取可创建流程列表
        /// </summary>
        /// <returns>可创建流程列表</returns>
        public IEnumerable<WorkflowBaseViewModel> GetWorkflowCreateList()
        {
            var result = _db.Query<WorkflowBaseViewModel>(
                " select a.id,a.type,a.name,b.url,a.icon " +
                " from workflow_base a " +
                " left join workflow_node b on a.id=b.workflow_id and b.type='创建' " +
                " where a.active=1 " +
                " order by a.sort");

            return result;
        }

        /// <summary>
        /// 获取可查询流程类型列表
        /// </summary>
        /// <returns>获取可查询流程类型列表</returns>
        public IEnumerable<WorkflowBaseViewModel> GetWorkflowSelectList()
        {
            var result = _db.Query<WorkflowBaseViewModel>(
                " select a.id,a.type,a.name,b.url,a.icon " +
                " from workflow_base a " +
                " left join workflow_node b on a.id=b.workflow_id and b.type='创建' " +
                " where a.active>0 " +
                " order by a.sort");

            return result;
        }

        public string GetImage(int id)
        {
            var result = _db.QueryFirstOrDefault<string>("select image from workflow_base where id=@id", new { id });
            return result;
        }


        public string GetImageByRequestId(int requestId)
        {
            var result = _db.QueryFirstOrDefault<string>("select image from workflow_base a left join request_base b on a.id=b.workflow_id where b.id=@requestId", new { requestId });
            return result;
        }
    }
}
