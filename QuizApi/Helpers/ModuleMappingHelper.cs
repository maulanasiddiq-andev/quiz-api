using System.Reflection;
using QuizApi.Constants;

namespace QuizApi.Helpers
{
    public static class ModuleMappingHelper
    {
        public static List<string> GetAllModules()
        {
            var type = typeof(ModuleConstant);

            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            List<FieldInfo> listFields = fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToList();

            List<string> listModule = new();

            foreach (FieldInfo item in listFields)
            {
                string? itemValue = item.GetValue(null)?.ToString();

                if (itemValue != null)
                {
                    listModule.Add(itemValue);
                }

            }

            return listModule;
        }
    }
}