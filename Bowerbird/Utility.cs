using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

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

        public static void SetMenuList<T>(GH_DocumentObject obj, ToolStrip menu, string record, Func<T> getValue, Action<T> setValue) where T : Enum
        {
            foreach (var entry in Enum.GetValues(typeof(T)).Cast<T>())
            {
                var name = entry.ToString();
                var value = Convert.ToInt32(entry);

                GH_DocumentObject.Menu_AppendItem(menu, name, (s, e) =>
                {
                    obj.RecordUndoEvent(record);
                    setValue(entry);
                    obj.ExpireSolution(true);
                }, true, entry.Equals(getValue()));
            }
        }

        public static void SetMenuToggle(GH_DocumentObject obj, ToolStrip menu, string name, Func<bool> getValue, Action<bool> setValue)
        {
            GH_DocumentObject.Menu_AppendItem(menu, name, (s, e) =>
            {
                obj.RecordUndoEvent($"Change {name}");
                setValue(!getValue());
                obj.ExpireSolution(true);
            }, true, getValue());
        }

        public static int GetOrDefault(this GH_IReader reader, string key, int defaultValue = default)
        {
            int value = defaultValue;

            reader.TryGetInt32(key, ref value);

            return value;
        }

        public static void Set(this GH_IWriter writer, string key, bool value)
        {
            writer.SetBoolean(key, value);
        }

        public static void Set<T>(this GH_IWriter writer, string key, T value) where T : Enum
        {
            writer.SetInt32(key, Convert.ToInt32(value));
        }

        public static T GetOrDefault<T>(this GH_IReader reader, string key, T defaultValue = default) where T : Enum
        {
            var value = default(int);

            if (!reader.TryGetInt32(key, ref value))
                return defaultValue;

            return (T)Enum.ToObject(typeof(T), value);
        }

        public static bool GetOrDefault(this GH_IReader reader, string key, bool defaultValue = default)
        {
            var value = default(bool);

            if (!reader.TryGetBoolean(key, ref value))
                return defaultValue;

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertTrue(this bool value)
        {
            Debug.Assert(value);
        }

        public static void SetEnum2D<T>(this IGH_DataAccess DA, int index, IEnumerable<IEnumerable<T>> data)
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

        public static void SetEnum1D<T>(this IGH_DataAccess DA, int index, IEnumerable<T> data)
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

        public static Vector3d Normalized(this Vector3d v)
        {
            return v / v.Length;
        }
    }
}
