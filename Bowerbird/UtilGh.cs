using System.Collections.Generic;
using System.Linq;
using Grasshopper;

namespace Bowerbird
{
    static class UtilGh
    {
        public static void SetEnum2D<T>(this Grasshopper.Kernel.IGH_DataAccess DA, int index, IEnumerable<IEnumerable<T>> data)
        {
            var tree = new DataTree<T>();

            var basePath = DA.ParameterTargetPath(index);

            foreach (var entry in data.Select((o, i) => new { Index = i, Item = o }))
            {
                var path = basePath.AppendElement(entry.Index);

                tree.AddRange(entry.Item, path);
            }

            DA.SetDataTree(index, tree);
        }

        public static void SetEnum1D<T>(this Grasshopper.Kernel.IGH_DataAccess DA, int index, IEnumerable<T> data)
        {
            var tree = new DataTree<T>();

            var basePath = DA.ParameterTargetPath(index);

            foreach (var entry in data.Select((o, i) => new { Index = i, Item = o }))
            {
                var path = basePath.AppendElement(entry.Index);

                tree.Add(entry.Item, path);
            }

            DA.SetDataTree(index, tree);
        }
    }
}
