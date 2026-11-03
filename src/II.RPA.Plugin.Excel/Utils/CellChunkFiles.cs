using II.RPA.Plugin.Excel.Models;
using II.RPA.WinAutomation.CommonUtility;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace II.RPA.Plugin.Excel.Utils
{
    internal sealed class CellChunk
    {
        public int RowOffset { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        internal object Source;
        internal int SourceRow;
        public int CellCount => checked(RowCount * ColumnCount);

        public object GetCell(int row, int column)
        {
            int r = SourceRow + row;
            if (Source is SerializeData flat) return flat.Data[r * ColumnCount + column];
            if (Source is object[,] matrix) return matrix[r + matrix.GetLowerBound(0), column + matrix.GetLowerBound(1)];
            if (Source is object[][] rows) return rows[r][column];
            if (Source is object[] vector) return vector[r];
            return Source;
        }

        public object[] Cells
        {
            get
            {
                var cells = new object[CellCount];
                for (int r = 0; r < RowCount; r++)
                    for (int c = 0; c < ColumnCount; c++) cells[r * ColumnCount + c] = GetCell(r, c);
                return cells;
            }
        }

        public object[,] ToMatrix()
        {
            object[,] buffer = null;
            return ToMatrix(ref buffer);
        }

        // The caller owns this scratch buffer. A whole source matrix is borrowed
        // without replacing it, so later chunks never overwrite a borrowed source.
        public object[,] ToMatrix(ref object[,] buffer)
        {
            if (Source is object[,] matrix && SourceRow == 0 && matrix.GetLength(0) == RowCount &&
                matrix.GetLowerBound(0) == 0 && matrix.GetLowerBound(1) == 0) return matrix;
            if (buffer == null || buffer.GetLength(0) != RowCount || buffer.GetLength(1) != ColumnCount)
                buffer = new object[RowCount, ColumnCount];
            var values = buffer;
            if (Source is SerializeData flat)
            {
                int index = SourceRow * ColumnCount;
                for (int r = 0; r < RowCount; r++)
                    for (int c = 0; c < ColumnCount; c++) values[r, c] = flat.Data[index++];
            }
            else if (Source is object[,] sourceMatrix)
            {
                int firstRow = SourceRow + sourceMatrix.GetLowerBound(0);
                int firstColumn = sourceMatrix.GetLowerBound(1);
                for (int r = 0; r < RowCount; r++)
                    for (int c = 0; c < ColumnCount; c++) values[r, c] = sourceMatrix[firstRow + r, firstColumn + c];
            }
            else if (Source is object[][] rows)
            {
                for (int r = 0; r < RowCount; r++)
                {
                    var sourceRow = rows[SourceRow + r];
                    for (int c = 0; c < ColumnCount; c++) values[r, c] = sourceRow[c];
                }
            }
            else if (Source is object[] vector)
            {
                for (int r = 0; r < RowCount; r++)
                    for (int c = 0; c < ColumnCount; c++) values[r, c] = vector[SourceRow + r];
            }
            else
            {
                for (int r = 0; r < RowCount; r++)
                    for (int c = 0; c < ColumnCount; c++) values[r, c] = Source;
            }
            return values;
        }
    }

    // Keep existing .part payloads readable by the RPA consumer. The manifest
    // describes their wire shape instead of changing the binary protocol silently.
    internal sealed class CellChunkEntry
    {
        public string FileName { get; set; }
        public string Kind { get; set; }
        public string DataSet { get; set; }
        public int RowOffset { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
    }

    internal sealed class CellChunkManifest
    {
        public int ProtocolVersion { get; set; } = 1;
        public string Compression { get; set; } = "Lz4BlockArray";
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
        public int ChunkCount { get; set; }
        public List<CellChunkEntry> Chunks { get; set; } = new List<CellChunkEntry>();

        [JsonIgnore]
        internal Dictionary<string, int> RowOffsets = new Dictionary<string, int>();
        [JsonIgnore]
        internal DateTime LastTouchUtc = DateTime.UtcNow;

        internal void Add(CellChunkEntry entry)
        {
            RowOffsets.TryGetValue(entry.DataSet, out int offset);
            entry.RowOffset = offset;
            RowOffsets[entry.DataSet] = checked(offset + entry.RowCount);
            Chunks.Add(entry);
            ChunkCount = Chunks.Count;
            if (entry.DataSet == "cells")
            {
                TotalRows = RowOffsets[entry.DataSet];
                TotalColumns = Math.Max(TotalColumns, entry.ColumnCount);
            }
        }

        internal void Recalculate()
        {
            var entries = Chunks.ToArray();
            Chunks.Clear(); RowOffsets.Clear(); TotalRows = TotalColumns = ChunkCount = 0;
            foreach (var entry in entries) Add(entry);
        }
    }

    internal static class CellChunkFiles
    {
        internal static readonly MessagePackSerializerOptions Options =
            ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);
        internal const string OwnerFile = ".excel-transfer";

        public static void Write(string directory, object data, string name, CellChunkManifest manifest)
        {
            var perf = ExcelUtils.Performance;
            long start = perf == null ? 0 : Stopwatch.GetTimestamp();
            var entry = Describe(data);
            entry.FileName = name + ".part";
            entry.DataSet = name.StartsWith("filterData", StringComparison.Ordinal) ? "filterData" :
                name == "rowNos" ? "rowNos" : "cells";
            var path = Path.Combine(directory, entry.FileName);
            var staging = path + ".writing";
            // Compression already buffers each MessagePack value. Keep the
            // buffer bounded to this chunk and write it once, without another
            // 64 KB FileStream buffer or a write for each compressed segment.
            byte[] payload = MessagePackSerializer.Serialize(data, Options);
            if (!ExcelUtils.StreamingEnabled)
                File.WriteAllBytes(staging, payload);
            else using (var stream = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.SequentialScan))
            {
                stream.Write(payload, 0, payload.Length);
            }
            File.Move(staging, path);
            manifest.Add(entry);
            if (DateTime.UtcNow - manifest.LastTouchUtc >= TimeSpan.FromMinutes(1))
            {
                Touch(directory);
                manifest.LastTouchUtc = DateTime.UtcNow;
            }
            perf?.Record("serialize", Stopwatch.GetTimestamp() - start, payload.Length,
                (long)entry.RowCount * entry.ColumnCount, $"{entry.DataSet}:{entry.Kind}:{entry.RowCount}x{entry.ColumnCount}");
        }

        public static void Publish(string directory, CellChunkManifest manifest)
        {
            if (directory == null || manifest.Chunks.Count == 0) return;
            var staging = Path.Combine(directory, "manifest.json.writing");
            using (var writer = new StreamWriter(staging))
                new JsonSerializer().Serialize(writer, manifest);
            File.Move(staging, Path.Combine(directory, "manifest.json"));
        }

        private static CellChunkEntry Describe(object data)
        {
            if (data is SerializeData flat)
                return new CellChunkEntry { Kind = "flat", RowCount = flat.Rows, ColumnCount = flat.Columns };
            if (data is Array array && array.Rank == 2)
                return new CellChunkEntry { Kind = "matrix", RowCount = array.GetLength(0), ColumnCount = array.GetLength(1) };
            if (data is object[][] rows)
                return new CellChunkEntry { Kind = "rows", RowCount = rows.Length, ColumnCount = rows.Length == 0 ? 0 : rows[0].Length };
            if (data is Array vector && vector.Rank == 1)
                return new CellChunkEntry { Kind = "vector", RowCount = vector.Length, ColumnCount = vector.Length == 0 ? 0 : 1 };
            return new CellChunkEntry { Kind = "scalar", RowCount = 1, ColumnCount = 1 };
        }

        public static T Read<T>(string path)
        {
            var perf = ExcelUtils.Performance;
            long start = perf == null ? 0 : Stopwatch.GetTimestamp();
            if (!ExcelUtils.StreamingEnabled)
            {
                byte[] bytes = File.ReadAllBytes(path);
                var value = MessagePackSerializer.Deserialize<T>(bytes, Options);
                perf?.Record("deserialize", Stopwatch.GetTimestamp() - start, bytes.Length, shape: typeof(T).Name);
                return value;
            }
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan))
            {
                var result = MessagePackSerializer.Deserialize<T>(stream, Options);
                perf?.Record("deserialize", Stopwatch.GetTimestamp() - start, stream.Length, shape: typeof(T).Name);
                return result;
            }
        }

        private static string ManifestPath(string input)
        {
            return Directory.Exists(input) ? Path.Combine(input, "manifest.json") : input;
        }

        public static bool IsManifest(string input)
        {
            return Directory.Exists(input) || string.Equals(Path.GetFileName(input), "manifest.json", StringComparison.OrdinalIgnoreCase);
        }

        public static CellChunkManifest LoadManifest(string input)
        {
            CellChunkManifest manifest;
            using (var reader = new StreamReader(ManifestPath(input)))
                manifest = new JsonSerializer().Deserialize<CellChunkManifest>(new JsonTextReader(reader));
            if (manifest == null || manifest.ProtocolVersion != 1 || manifest.Compression != "Lz4BlockArray" ||
                manifest.Chunks == null || manifest.ChunkCount != manifest.Chunks.Count)
                throw new InvalidDataException("Excel 分块清单无效。".I18n("CellChunkFiles_CD14E5C9"));
            int offset = 0, columns = -1;
            foreach (var entry in manifest.Chunks)
            {
                if (entry == null || string.IsNullOrEmpty(entry.FileName) || Path.GetFileName(entry.FileName) != entry.FileName ||
                    entry.FileName.Contains(":") || !entry.FileName.EndsWith(".part", StringComparison.OrdinalIgnoreCase) ||
                    !new[] { "cells", "filterData", "rowNos" }.Contains(entry.DataSet) ||
                    entry.RowCount < 0 || entry.ColumnCount < 0 || entry.RowOffset < 0 ||
                    !new[] { "flat", "matrix", "rows", "vector", "scalar" }.Contains(entry.Kind))
                    throw new InvalidDataException("Excel 分块条目无效。".I18n("CellChunkFiles_8F391EF2"));
                if (entry.DataSet != "cells") continue;
                if (entry.RowOffset != offset || (columns >= 0 && columns != entry.ColumnCount && entry.RowCount > 0))
                    throw new InvalidDataException("Excel 分块的行列信息不一致。".I18n("CellChunkFiles_5ED2BFE7"));
                offset = checked(offset + entry.RowCount);
                if (entry.RowCount > 0) columns = entry.ColumnCount;
            }
            if (offset != manifest.TotalRows || (columns >= 0 && columns != manifest.TotalColumns) ||
                manifest.Chunks.Select(x => x.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Chunks.Count)
                throw new InvalidDataException("Excel 分块的汇总信息不一致。".I18n("CellChunkFiles_B66B6E24"));
            return manifest;
        }

        public static IEnumerable<CellChunk> ReadChunks(string input, int batchSize, bool vector = false)
        {
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (!IsManifest(input))
            {
                // Legacy files contain a single MessagePack value. Stream API
                // removes ReadAllBytes, but this value still requires full decoding.
                object value = vector ? (object)Read<object[]>(input) : Read<object[][]>(input);
                if (value == null) yield break;
                var entry = Describe(value);
                foreach (var chunk in Slice(value, entry, batchSize)) yield return chunk;
                yield break;
            }
            var manifest = LoadManifest(input);
            if (manifest.Chunks.Count > 0 && !manifest.Chunks.Any(x => x.DataSet == "cells"))
                throw new InvalidDataException("此清单包含辅助数据，不包含单元格数据集。".I18n("CellChunkFiles_4E0CDC2C"));
            var directory = Path.GetDirectoryName(Path.GetFullPath(ManifestPath(input)));
            Touch(directory);
            foreach (var entry in manifest.Chunks.Where(x => x.DataSet == "cells"))
            {
                var path = Path.Combine(directory, entry.FileName);
                object value;
                switch (entry.Kind)
                {
                    case "flat": value = Read<SerializeData>(path); break;
                    case "matrix": value = Read<object[,]>(path); break;
                    case "rows": value = Read<object[][]>(path); break;
                    case "vector": value = Read<object[]>(path); break;
                    default: value = Read<object>(path); break;
                }
                foreach (var chunk in Slice(value, entry, batchSize)) yield return chunk;
            }
        }

        // Adapt current manifests for baseline writes too, so both modes accept the same test input.
        // Keep every decoded chunk alive and merge before applying, deliberately retaining full-input memory.
        internal static IEnumerable<CellChunk> ReadWhole(string input, bool vector)
        {
            if (!IsManifest(input))
            {
                object value = vector ? (object)Read<object[]>(input) : Read<object[][]>(input);
                if (value == null) yield break;
                foreach (var chunk in Slice(value, Describe(value), int.MaxValue)) yield return chunk;
                yield break;
            }
            var chunks = ReadChunks(input, int.MaxValue, vector).ToList();
            if (chunks.Count == 0) yield break;
            int rows = chunks.Sum(x => x.RowCount);
            int columns = chunks[0].ColumnCount;
            var values = new object[rows][];
            foreach (var chunk in chunks)
                for (int r = 0; r < chunk.RowCount; r++)
                {
                    var row = values[chunk.RowOffset + r] = new object[columns];
                    for (int c = 0; c < columns; c++) row[c] = chunk.GetCell(r, c);
                }
            yield return new CellChunk { RowCount = rows, ColumnCount = columns, Source = values };
        }

        private static IEnumerable<CellChunk> Slice(object value, CellChunkEntry entry, int batchSize)
        {
            var actual = Describe(value);
            if (actual.Kind != entry.Kind || actual.RowCount != entry.RowCount || actual.ColumnCount != entry.ColumnCount)
                throw new InvalidDataException("Excel 分块与清单不匹配。".I18n("CellChunkFiles_357DB718"));
            var flat = value as SerializeData;
            var rows = value as object[][];
            if (flat != null && (flat.Data == null || flat.Data.Length < checked(flat.Rows * flat.Columns)))
                throw new InvalidDataException("Excel 平铺分块数据不完整。".I18n("CellChunkFiles_AA6BCB0B"));
            if (rows != null && rows.Any(x => x == null || x.Length != entry.ColumnCount))
                throw new InvalidDataException("Excel 输入必须是各行列数相同的二维列表。".I18n("CellChunkFiles_1B31279"));
            for (int start = 0; start < entry.RowCount; start += batchSize)
            {
                int count = Math.Min(batchSize, entry.RowCount - start);
                yield return new CellChunk { RowOffset = entry.RowOffset + start, RowCount = count,
                    ColumnCount = entry.ColumnCount, Source = value, SourceRow = start };
            }
        }

        // Never recursively remove a caller-supplied directory. Delete only the
        // validated files listed by the protocol; leave unrelated files untouched.
        public static void DeleteInput(string input)
        {
            if (!IsManifest(input)) { File.Delete(input); return; }
            var manifest = LoadManifest(input);
            var path = ManifestPath(input);
            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            // The caller consumed cells, not the entire mixed transfer. Keep
            // it intact for other consumers; the expiry sweep owns its cleanup.
            if (manifest.Chunks.Any(x => x.DataSet != "cells")) return;
            foreach (var entry in manifest.Chunks.Where(x => x.DataSet == "cells"))
                File.Delete(Path.Combine(directory, entry.FileName));
            File.Delete(path);
            File.Delete(Path.Combine(directory, OwnerFile));
            if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
        }

        internal static void Touch(string directory)
        {
            string marker = Path.Combine(directory, OwnerFile);
            if (File.Exists(marker)) File.SetLastWriteTimeUtc(marker, DateTime.UtcNow);
        }
    }
}
