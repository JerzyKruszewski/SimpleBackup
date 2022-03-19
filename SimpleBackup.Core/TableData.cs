using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace SimpleBackup.Core
{
    public class TableData
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public DataTable Data { get; set; }
    }
}
