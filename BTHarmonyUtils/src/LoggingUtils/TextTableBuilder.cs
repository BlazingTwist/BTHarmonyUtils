using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace BTHarmonyUtils.LoggingUtils {

	/// <summary>
	/// Useful for writing Tables to log files.
	/// </summary>
	[PublicAPI]
	public class TextTableBuilder {
     
     		private const int COL_PADDING_LEFT = 1;
     		private const int COL_PADDING_RIGHT = 2;
     		private static readonly string PADDING_STRING_LEFT = new string(' ', COL_PADDING_LEFT);
     		private static readonly string PADDING_STRING_RIGHT = new string(' ', COL_PADDING_RIGHT);
     
     		private readonly List<TableRow> rows = new List<TableRow>();
     		private readonly List<int> columnWidths = new List<int>();
     
     		/// <summary>
     		/// Adds a row to the table.
     		/// </summary>
     		/// <param name="columnText">SingleLine-Strings for each column</param>
     		public void Row(params string[] columnText) {
     			rows.Add(new TextLine(columnText));
     			EnsureColumnWidths(columnText.Length);
     			for (int i = 0; i < columnText.Length; i++) {
     				columnWidths[i] = Math.Max(columnWidths[i], columnText[i].Length);
     			}
     		}
     
     		/// <summary>
     		/// Adds a thin separator-row to the table (e.g.: '|---+---|')
     		/// </summary>
     		public void ThinRowSeparator() {
     			rows.Add(new RowSeparator('-'));
     		}
     
            /// <summary>
            /// Adds a thick separator-row to the table (e.g.: '|===+===|')
            /// </summary>
     		public void ThickRowSeparator() {
     			rows.Add(new RowSeparator('='));
     		}
     
            /// <summary>
            /// Adds a table-end separator-row to the table (e.g.: '\=======/')
            /// </summary>
     		public void EndTable() {
     			rows.Add(new TableEndLine('='));
     		}
     
            /// <summary>
            /// Build the table to a String.
            /// </summary>
            /// <param name="tableIndent">A string to use for indenting the table (e.g.: '  ')</param>
            /// <returns>The String representation of the table.</returns>
     		public string BuildTable(string tableIndent) {
     			int[] colWidthArray = new int[columnWidths.Count];
     			for (int i = 0; i < columnWidths.Count; i++) {
     				colWidthArray[i] = columnWidths[i];
     			}
     
     			StringBuilder builder = new StringBuilder();
     			foreach (TableRow row in rows) {
     				builder.Append(tableIndent);
     				row.Build(builder, colWidthArray);
     			}
     			return builder.ToString();
     		}
     
     		private void EnsureColumnWidths(int numColumns) {
     			for (int i = columnWidths.Count; i < numColumns; i++) {
     				columnWidths.Add(0);
     			}
     		}
     
     
     		private interface TableRow {
     
     			void Build(StringBuilder builder, int[] columnWidths);
     
     		}
     
     		private class RowSeparator : TableRow {
     
     			private const char COL_SEPARATOR_CHAR = '+';
     
     			private readonly char separatorChar;
     
     			public RowSeparator(char separatorChar) {
     				this.separatorChar = separatorChar;
     			}
     
     			public void Build(StringBuilder builder, int[] columnWidths) {
     				string lineSeparatorString = string.Join(
     						"" + COL_SEPARATOR_CHAR,
     						columnWidths.Select(colWidth => new string(separatorChar, COL_PADDING_LEFT + COL_PADDING_RIGHT + colWidth))
     				);
     				builder.Append("|").Append(lineSeparatorString).Append("|\n");
     			}
     
     		}
     
     		private class TableEndLine : TableRow {
     
     			private readonly char separatorChar;
     
     			public TableEndLine(char separatorChar) {
     				this.separatorChar = separatorChar;
     			}
     
     			public void Build(StringBuilder builder, int[] columnWidths) {
     				int totalWidth = -1; // there are 1 less column separators than columns
     				totalWidth += columnWidths.Sum(columnWidth => (COL_PADDING_LEFT + COL_PADDING_RIGHT + columnWidth + 1));
     				builder.Append("\\").Append(new string(separatorChar, totalWidth)).Append("/\n");
     			}
     
     		}
     
     		private class TextLine : TableRow {
     
     			private readonly string[] columnText;
     
     			public TextLine(string[] columnText) {
     				this.columnText = columnText;
     			}
     
     			public void Build(StringBuilder builder, int[] columnWidths) {
     				for (int i = 0; i < columnWidths.Length; i++) {
     					string textContent = i < columnText.Length ? columnText[i] : "";
     					builder.Append("|")
     							.Append(PADDING_STRING_LEFT)
     							.Append(textContent)
     							.Append(new string(' ', columnWidths[i] - textContent.Length))
     							.Append(PADDING_STRING_RIGHT);
     				}
     				builder.Append("|\n");
     			}
     
     		}
     
     	}

}
