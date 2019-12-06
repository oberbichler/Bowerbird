using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using System;
using System.Linq;

namespace Bowerbird
{
    static class Utility
    {
        public static void AddNamedValues<T>(IGH_Param inputParameter) where T : Enum
        {
            var intParameter = inputParameter as Param_Integer;

            foreach (var entry in Enum.GetValues(typeof(T)).Cast<T>())
            {
                var name = entry.ToString();
                var value = Convert.ToInt32(entry);
                intParameter.AddNamedValue(name, value);
            }
        }

        public static void SetInputValueList<T>(IGH_Param inputParameter) where T : Enum
        {
            var values = Enum.GetValues(typeof(T));

            foreach (var valueList in inputParameter.Sources.OfType<GH_ValueList>())
            {
                if (valueList.ListItems.Count == values.Length) // FIXME: check content
                    continue;

                valueList.ListItems.Clear();

                foreach (var entry in Enum.GetValues(typeof(T)).Cast<T>())
                {
                    var name = entry.ToString();
                    var value = Convert.ToInt32(entry).ToString();
                    valueList.ListItems.Add(new GH_ValueListItem(name, value));
                }

                valueList.ExpireSolution(true);
            }
        }
    }
}
