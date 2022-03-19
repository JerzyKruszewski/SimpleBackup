using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleBackup.Core
{
    public interface IDatabaseHandler
    {
        public ICollection<TableData> HandleBackup();
        public IDictionary<string, string> GetBackupQueries();
    }
}
