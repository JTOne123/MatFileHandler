﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MatFileHandler.Hdf
{
    internal class StructureArray : Array, IStructureArray
    {
        public StructureArray(
            int[] dimensions,
            Dictionary<string, List<IArray>> fields)
            : base(dimensions)
        {
            Fields = fields;
        }

        /// <inheritdoc />
        public IEnumerable<string> FieldNames => Fields.Keys;

        /// <summary>
        /// Gets null: not implemented.
        /// </summary>
        public IReadOnlyDictionary<string, IArray>[] Data => null;

        /// <summary>
        /// Gets a dictionary that maps field names to lists of values.
        /// </summary>
        internal Dictionary<string, List<IArray>> Fields { get; }

        /// <inheritdoc />
        public IArray this[string field, params int[] list]
        {
            get => Fields[field][Dimensions.DimFlatten(list)];
            set => Fields[field][Dimensions.DimFlatten(list)] = value;
        }

        /// <inheritdoc />
        IReadOnlyDictionary<string, IArray> IArrayOf<IReadOnlyDictionary<string, IArray>>.this[params int[] list]
        {
            get => ExtractStructure(Dimensions.DimFlatten(list));
            set => throw new NotSupportedException(
                "Cannot set structure elements via this[params int[]] indexer. Use this[string, int[]] instead.");
        }

        private IReadOnlyDictionary<string, IArray> ExtractStructure(int i)
        {
            return new HdfStructureArrayElement(this, i);
        }

        /// <summary>
        /// Provides access to an element of a structure array by fields.
        /// </summary>
        internal class HdfStructureArrayElement : IReadOnlyDictionary<string, IArray>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="HdfStructureArrayElement"/> class.
            /// </summary>
            /// <param name="parent">Parent structure array.</param>
            /// <param name="index">Index in the structure array.</param>
            internal HdfStructureArrayElement(StructureArray parent, int index)
            {
                Parent = parent;
                Index = index;
            }

            /// <summary>
            /// Gets the number of fields.
            /// </summary>
            public int Count => Parent.Fields.Count;

            /// <summary>
            /// Gets a list of all fields.
            /// </summary>
            public IEnumerable<string> Keys => Parent.Fields.Keys;

            /// <summary>
            /// Gets a list of all values.
            /// </summary>
            public IEnumerable<IArray> Values => Parent.Fields.Values.Select(array => array[Index]);

            private StructureArray Parent { get; }

            private int Index { get; }

            /// <summary>
            /// Gets the value of a given field.
            /// </summary>
            /// <param name="key">Field name.</param>
            /// <returns>The corresponding value.</returns>
            public IArray this[string key] => Parent.Fields[key][Index];

            /// <summary>
            /// Enumerates fieldstructure/value pairs of the dictionary.
            /// </summary>
            /// <returns>All field/value pairs in the structure.</returns>
            public IEnumerator<KeyValuePair<string, IArray>> GetEnumerator()
            {
                foreach (var field in Parent.Fields)
                {
                    yield return new KeyValuePair<string, IArray>(field.Key, field.Value[Index]);
                }
            }

            /// <summary>
            /// Enumerates field/value pairs of the structure.
            /// </summary>
            /// <returns>All field/value pairs in the structure.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            /// <summary>
            /// Checks if the structure has a given field.
            /// </summary>
            /// <param name="key">Field name</param>
            /// <returns>True iff the structure has a given field.</returns>
            public bool ContainsKey(string key) => Parent.Fields.ContainsKey(key);

            /// <summary>
            /// Tries to get the value of a given field.
            /// </summary>
            /// <param name="key">Field name.</param>
            /// <param name="value">Value (or null if the field is not present).</param>
            /// <returns>Success status of the query.</returns>
            public bool TryGetValue(string key, out IArray value)
            {
                var success = Parent.Fields.TryGetValue(key, out var array);
                if (!success)
                {
                    value = default(IArray);
                    return false;
                }
                value = array[Index];
                return true;
            }
        }
    }
}