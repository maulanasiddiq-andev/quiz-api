using System.Reflection;
using QuizApi.Constants;

namespace QuizApi.Helpers
{
    public class ActionModelHelper
    {
        public T AssignCreateModel<T>(T model, string tableName, string userId) where T : new()
        {
            Type type = typeof(T);
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            string modelNameId = tableName + "Id";

            string newId = Guid.NewGuid().ToString("N");
            PropertyInfo? modelId = props.FirstOrDefault(x => x.Name == modelNameId);
            if (modelId != null)
            {
                modelId.SetValue(model, newId, null);
            }

            PropertyInfo? createdTime = props.FirstOrDefault(x => x.Name == "CreatedTime");
            if (createdTime != null)
            {
                createdTime.SetValue(model, DateTime.UtcNow, null);
            }

            PropertyInfo? modifiedTime = props.FirstOrDefault(x => x.Name == "ModifiedTime");
            if (modifiedTime != null)
            {
                modifiedTime.SetValue(model, DateTime.UtcNow, null);
            }

            PropertyInfo? createdBy = props.FirstOrDefault(x => x.Name == "CreatedBy");
            if (createdBy != null && !string.IsNullOrEmpty(userId))
            {
                createdBy.SetValue(model, userId, null);
            }

            PropertyInfo? modifiedBy = props.FirstOrDefault(x => x.Name == "ModifiedBy");
            if (modifiedBy != null && !string.IsNullOrEmpty(userId))
            {
                modifiedBy.SetValue(model, userId, null);
            }
            
            PropertyInfo? recordStatus = props.FirstOrDefault(x => x.Name == "RecordStatus");
            if (recordStatus != null)
            {
                recordStatus.SetValue(model, RecordStatusConstant.Active, null);
            }

            return model;
        }

        public T AssignUpdateModel<T>(T model, string userId = "") where T : new()
        {
            Type type = typeof(T);
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            PropertyInfo? modifiedTime = props.FirstOrDefault(x => x.Name == "ModifiedTime");
            if (modifiedTime != null)
            {
                modifiedTime.SetValue(model, DateTime.UtcNow, null);
            }

            PropertyInfo? modifiedBy = props.FirstOrDefault(x => x.Name == "ModifiedBy");
            if (modifiedBy != null && !string.IsNullOrEmpty(userId))
            {
                modifiedBy.SetValue(model, userId, null);
            }

            return model;
        }

        public T AssignDeleteModel<T>(T model, string userId = "") where T : new()
        {
            Type type = typeof(T);
            PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            PropertyInfo? modifiedTime = props.FirstOrDefault(x => x.Name == "ModifiedTime");
            if (modifiedTime != null)
            {
                modifiedTime.SetValue(model, DateTime.UtcNow, null);
            }

            PropertyInfo? modifiedBy = props.FirstOrDefault(x => x.Name == "ModifiedBy");
            if (modifiedBy != null && !string.IsNullOrEmpty(userId))
            {
                modifiedBy.SetValue(model, userId, null);
            }

            PropertyInfo? recordStatus = props.FirstOrDefault(x => x.Name == "RecordStatus");
            if (recordStatus != null)
            {
                recordStatus.SetValue(model, RecordStatusConstant.Deleted, null);
            }

            return model;
        }
    }
}