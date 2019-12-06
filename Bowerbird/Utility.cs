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

            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValues(typeof(T));

            for (int i = 0; i < values.Length; i++)
            {
                var name = names[i];
                var value = (int)values.GetValue(i);    // FIXME: is not the int value
                intParameter.AddNamedValue(name, value);
            }
        }

        public static void SetInputValueList<T>(IGH_Param inputParameter) where T : Enum
        {
            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValues(typeof(T));

            foreach (var valueList in inputParameter.Sources.OfType<GH_ValueList>())
            {
                if (valueList.ListItems.Count == values.Length) // FIXME: check content
                    continue;

                valueList.ListItems.Clear();

                for (int i = 0; i < values.Length; i++)
                {
                    var name = names[i];
                    var value = values.GetValue(i).ToString();    // FIXME: is not the int value
                    valueList.ListItems.Add(new GH_ValueListItem(name, value));
                }

                valueList.ExpireSolution(true);
            }
        }
    }
}
