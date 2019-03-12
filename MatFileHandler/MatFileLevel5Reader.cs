﻿using System.Collections.Generic;
using System.IO;

namespace MatFileHandler
{
    internal static class MatFileLevel5Reader
    {

        /// <summary>
        /// Read a sequence of raw variables from .mat file.
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="subsystemDataOffset">Offset of subsystem data in the file;
        /// we need it because we may encounter it during reading, and
        /// the subsystem data should be parsed in a special way.</param>
        /// <param name="subsystemData">
        /// Link to the current file's subsystem data structure; initially it has dummy value
        /// which will be replaced after we parse the whole subsystem data.</param>
        /// <returns>List of "raw" variables; the actual variables are constructed from them later.</returns>
        internal static List<RawVariable> ReadRawVariables(BinaryReader reader, long subsystemDataOffset, SubsystemData subsystemData)
        {
            var variables = new List<RawVariable>();
            var dataElementReader = new DataElementReader(subsystemData);
            while (true)
            {
                try
                {
                    var position = reader.BaseStream.Position;
                    var dataElement = dataElementReader.Read(reader);
                    if (position == subsystemDataOffset)
                    {
                        var subsystemDataElement = dataElement as IArrayOf<byte>;
                        var newSubsystemData = ReadSubsystemData(subsystemDataElement.Data, subsystemData);
                        subsystemData.Set(newSubsystemData);
                    }
                    else
                    {
                        variables.Add(new RawVariable(position, dataElement));
                    }
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }

            return variables;
        }

        /// <summary>
        /// Read raw variables from a .mat file.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="subsystemDataOffset">Offset to the subsystem data to use (read from the file header).</param>
        /// <returns>Raw variables read.</returns>
        internal static List<RawVariable> ReadRawVariables(BinaryReader reader, long subsystemDataOffset)
        {
            var subsystemData = new SubsystemData();
            return ReadRawVariables(reader, subsystemDataOffset, subsystemData);
        }

        internal static IMatFile ContinueReadingLevel5File(Header header, BinaryReader reader)
        {
            var rawVariables = ReadRawVariables(reader, header.SubsystemDataOffset);
            var variables = new List<IVariable>();
            foreach (var variable in rawVariables)
            {
                var array = variable.DataElement as MatArray;
                if (array is null)
                {
                    continue;
                }

                variables.Add(new MatVariable(
                    array,
                    array.Name,
                    array.Flags.Variable.HasFlag(Variable.IsGlobal)));
            }

            return new MatFile(variables);
        }

        private static SubsystemData ReadSubsystemData(byte[] bytes, SubsystemData subsystemData)
        {
            return SubsystemDataReader.Read(bytes, subsystemData);
        }
    }
}