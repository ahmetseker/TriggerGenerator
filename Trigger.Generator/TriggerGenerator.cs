using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Trigger.Generator
{
    internal class TriggerGenerator
    {
        private string GetNumericConv(int precision, int scale)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Empty.PadLeft(precision, '9'));
            if (scale > 0)
            {
                sb.AppendFormat(".{0}", string.Empty.PadLeft(scale, '9'));
            }

            return sb.ToString();
        }

        public string BuildTrigger(string ownerName, string tableName)
        {
            return BuildTrigger(ownerName, tableName, false);
        }

        private string RemoveWovels(string src)
        {
            const string wovelChars = "AEIOU";
            string dst = string.Empty;
            foreach (var chr in src.ToCharArray())
            {
                if (wovelChars.IndexOf(chr) == -1)
                {
                    dst += chr;
                }
            }
            return dst;
        }

        private string FixTriggerNameForIdentifierTooLong(string tableName)
        {
            string triggerName = string.Format("TRX_{0}", tableName);

            if (triggerName.Length > 32)
            {
                string tableNameFixed = string.Empty;

                string[] itemList = tableName.Split(new char[] { '_' });

                bool bFirst = true;

                foreach (var item in itemList)
                {
                    string resultItem = item;

                    if (bFirst)
                    {
                        resultItem = RemoveWovels(item);
                        bFirst = false;
                    }
                    else
                    {
                        tableNameFixed += '_';
                    }

                    tableNameFixed += resultItem;
                }

                return tableNameFixed;
            }

            return triggerName;
        }

        public string BuildTrigger(string ownerName, string tableName, bool withPlSql)
        {
            StringBuilder triggerBody = new StringBuilder();
            var triggerName = FixTriggerNameForIdentifierTooLong(tableName);
            triggerBody.AppendFormat(@"CREATE OR REPLACE TRIGGER ""{0}"".""{1}""", ownerName, triggerName);
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  AFTER INSERT OR DELETE OR UPDATE");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  ON {0}", tableName);
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  REFERENCING NEW AS NEW OLD AS OLD");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  FOR EACH ROW");
            triggerBody.AppendLine();
            triggerBody.AppendFormat(" DECLARE");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("    v_table_name VARCHAR2(64);");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("    v_id VARCHAR2(2000);");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("    v_operation VARCHAR2(1);");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("    v_xml CLOB;");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("BEGIN ");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  v_table_name := '{0}';", tableName);
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  IF INSERTING THEN");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("    v_operation := 'I';");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  ELSIF UPDATING THEN ");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("    v_operation := 'U';");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  ELSE");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("    v_operation := 'D';");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  END IF;");
            triggerBody.AppendLine();
            triggerBody.AppendFormat("  v_xml := '';");
            triggerBody.AppendLine();

            StringBuilder xmlBuilderStatements = new StringBuilder();
            xmlBuilderStatements.Append("  v_xml := v_xml || '<table name=''' || v_table_name || '''>';");
            xmlBuilderStatements.AppendLine();

            var primaryKeyName = string.Empty;
            var dataTablePks = DataManager.GetPrimaryKeysForTable(ownerName, tableName);
            var firstRow = dataTablePks.Rows.OfType<DataRow>().FirstOrDefault();
            if (firstRow != null)
                primaryKeyName = firstRow.Field<string>("CONSTRAINT_NAME");

            var pkCols = new HashSet<string>();
            if (!string.IsNullOrWhiteSpace(primaryKeyName))
            {
                var dataTableIndexCols = DataManager.GetIndexColumns(ownerName, primaryKeyName);
                var queryIndexCols = from dr in dataTableIndexCols.Rows.OfType<DataRow>()
                                     select new
                                     {
                                         ColumnName = dr.Field<string>("COLUMN_NAME"),
                                     };

                foreach (var column in queryIndexCols)
                {
                    pkCols.Add(column.ColumnName);
                }
            }

            triggerBody.AppendFormat("  v_id := '';");
            triggerBody.AppendLine();

            StringBuilder idVarContent = new StringBuilder();

            var dataTableColumns = DataManager.GetColumnsForTable(ownerName, tableName);
            var sbColumns = new StringBuilder();

            foreach (DataRow dataRow in dataTableColumns.Rows)
            {
                string columnName = dataRow.Field<string>("COLUMN_NAME");
                string dataType = dataRow.Field<string>("DATATYPE");
                decimal? numericPrecision = dataRow.Field<decimal?>("PRECISION");
                decimal? numericScale = dataRow.Field<decimal?>("SCALE");
                string columnValueFmt = string.Empty;

                if (dataType == "VARCHAR2")
                {
                    columnValueFmt = "DBMS_XMLGEN.CONVERT(NVL({1}.{0},''))";
                }
                else if (dataType == "NUMBER")
                {
                    columnValueFmt = "LTRIM(NVL(TO_CHAR({1}.{0},'";
                    columnValueFmt += GetNumericConv((int)(numericPrecision ?? 0), (int)(numericScale ?? 0));
                    columnValueFmt += "'),''))";
                }
                else if (dataType == "DATE")
                {
                    columnValueFmt = "NVL(TO_CHAR({1}.{0},'yyyy-mm-dd hh24:mi:ss'),'')";
                }

                if (!string.IsNullOrEmpty(columnValueFmt))
                {

                    string columnValueNew = string.Format(columnValueFmt, columnName, ":new");
                    string columnValueOld = string.Format(columnValueFmt, columnName, ":old");

                    sbColumns.AppendFormat("  IF DELETING THEN");
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("    v_xml := v_xml || '<column name=''{0}''>';", columnName);
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("    v_xml := v_xml || '<oldval>' || {0} || '</oldval>';", columnValueOld);
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("  ELSE");
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("    v_xml := v_xml || '<column name=''{0}''>';", columnName);
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("    v_xml := v_xml || '<newval>' || {0} || '</newval>';", columnValueNew);
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("  END IF;");
                    sbColumns.AppendLine();

                    sbColumns.AppendFormat(" IF UPDATING THEN");
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("    v_xml := v_xml || '<oldval>' || {1} || '</oldval>';", columnName, columnValueOld);
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("  END IF;");
                    sbColumns.AppendLine();
                    sbColumns.AppendFormat("  v_xml := v_xml || '</column>';");
                    sbColumns.AppendLine();

                    if (pkCols.Contains(columnName))
                    {
                        idVarContent.AppendFormat("  v_id := v_id || {0};", columnValueOld);
                        idVarContent.AppendLine();
                    }
                }
            }

            xmlBuilderStatements.Append(sbColumns.ToString());
            xmlBuilderStatements.Append("  v_xml := v_xml || '</table>';");
            triggerBody.AppendLine(xmlBuilderStatements.ToString());

            triggerBody.AppendLine(idVarContent.ToString());

            var insertStatement = new StringBuilder();
            insertStatement.AppendFormat(@"  INSERT INTO ""{0}"".""DATA_AUDIT""", ownerName, tableName);
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   (AUDID, TABLENAME, RECID, OROWID, IUDFLAG, AUDDATE, USERNAME, TERMINAL,PROGRAMNAME, XMLCONTENT)");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   VALUES ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   (SYS_GUID(), v_table_name, ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   v_id, ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   :NEW.ROWID, ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   v_operation, ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   SYSDATE, ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   USER, ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   SYS_CONTEXT('USERENV', 'TERMINAL'), ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   SYS_CONTEXT('USERENV', 'MODULE'), ");
            insertStatement.AppendLine();
            insertStatement.AppendFormat("   v_xml); ");

            triggerBody.AppendLine(insertStatement.ToString());

            triggerBody.AppendLine(" END; ");
            triggerBody.AppendLine("-- END PL/SQL BLOCK (do not remove this line) ----------------------------------;");

            return triggerBody.ToString();
        }

        public bool CheckTriggerExists(string ownerName, string tableName)
        {

            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT * FROM USER_TRIGGERS");
            sql.AppendFormat(" WHERE TABLE_OWNER = '{0}' ", ownerName);
            sql.AppendFormat(" AND TABLE_NAME = '{0}' ", tableName);

            DataTable dataTable = DataManager.ExecuteCommand(sql.ToString());

            var query = (from dt in dataTable.AsEnumerable()
                         where dt.Field<string>("TRIGGER_NAME").StartsWith("TRA")
                         || dt.Field<string>("TRIGGER_NAME").StartsWith("TRX")
                         select dt.Field<string>("TRIGGER_NAME")
                        );

            if (query.Count() == 0)
                return false;

            return true;
        }

        public string GenerateAndExecute(string ownerName, string tableName)
        {

            string retVal = string.Empty;

            try
            {
                string triggerDdl = BuildTrigger(ownerName, tableName);
                DataManager.ExecuteNonQuery(triggerDdl);

            }
            catch (Exception ex)
            {
                retVal = ex.Message;
            }

            return retVal;
        }

    }
}
